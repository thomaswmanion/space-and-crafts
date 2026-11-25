using System;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Checkpoint respawn for falls and soft knocks. Keeps Arrival restartable without a full save system.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private float invulnerabilityDuration = 1.1f;
        [SerializeField] private float knockbackSpeed = 10f;
        [SerializeField] private float emergencyToastCooldown = 1.25f;
        [SerializeField] private float emergencyFallMargin = 6f;

        private CharacterController _controller;
        private SplatMotor _motor;
        private FallVolume _fallVolume;
        private Vector3 _checkpointPosition;
        private Quaternion _checkpointRotation;
        private bool _hasCheckpoint;
        private string _checkpointId;
        private float _invulnerableUntil;
        private float _nextEmergencyToastTime;
        private int _knockbackEventCountForDiagnostics;
        private float _lastKnockbackSpeedForDiagnostics;
        private const float EmergencySkyMargin = 64f;

        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public Vector3 CheckpointPosition => _checkpointPosition;
        public float EmergencyFallMargin => Mathf.Max(1f, emergencyFallMargin);
        public int KnockbackEventCountForDiagnostics => _knockbackEventCountForDiagnostics;
        public float LastKnockbackSpeedForDiagnostics => _lastKnockbackSpeedForDiagnostics;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _motor = GetComponent<SplatMotor>();
            _fallVolume = FindAnyObjectByType<FallVolume>();
            // Prefer authored shelf spawn so a bad first-frame overlap never becomes the checkpoint.
            var shelf = GameObject.Find("ART_Checkpoint_Shelf");
            if (shelf != null)
            {
                SetCheckpoint(shelf.transform.position + Vector3.up * 0.7f, shelf.transform.rotation);
                _controller.enabled = false;
                transform.SetPositionAndRotation(_checkpointPosition, _checkpointRotation);
                _controller.enabled = true;
                _motor?.HardResetState();
            }
            else
            {
                SetCheckpoint(transform.position, transform.rotation);
            }

            if (PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                if (RestoreSavedCheckpoint())
                {
                    _controller.enabled = false;
                    transform.SetPositionAndRotation(_checkpointPosition, _checkpointRotation);
                    _controller.enabled = true;
                    _motor?.HardResetState();
                }
            }
        }

        private void LateUpdate()
        {
            // Keep intended high launches playable, but make the fall rescue relative to
            // the active checkpoint so worlds at any vertical offset recover consistently.
            var y = transform.position.y;
            if (_fallVolume == null)
            {
                _fallVolume = FindAnyObjectByType<FallVolume>();
            }

            var emergencyRescueY = _checkpointPosition.y - EmergencyFallMargin;
            if (_fallVolume != null
                && _fallVolume.TryGetEmergencyRescueY(transform.position, emergencyRescueY, out var scenicRescueY))
            {
                // Over Arrival's water, let the fjord trigger deliver its splash before
                // the backup rescue. Everywhere outside that footprint stays forgiving.
                emergencyRescueY = scenicRescueY;
            }

            if (y > _checkpointPosition.y + EmergencySkyMargin)
            {
                RespawnAtCheckpoint();
            }
            else if (y < emergencyRescueY)
            {
                RespawnAtCheckpoint();
                if (Time.time >= _nextEmergencyToastTime)
                {
                    GameplayFeedback.Toast("Back on the trail.", playBlip: false);
                    TempSfxPlayer.PlayLand();
                    _nextEmergencyToastTime = Time.time + emergencyToastCooldown;
                }
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Long world routes use small authored traversal pads instead of
            // checkpoint volumes. Record a safe landing on those pads so a later
            // fall resumes local progress rather than sending Splat to the entry.
            if (hit == null || hit.collider == null || hit.normal.y < 0.65f
                || _motor == null || _motor.VerticalVelocity > 0.5f)
            {
                return;
            }

            var surface = hit.collider.transform;
            while (surface != null && !surface.name.StartsWith("ART_TraversalPad", StringComparison.Ordinal))
            {
                surface = surface.parent;
            }

            if (surface != null)
            {
                // Save at the pad center rather than the player's contact point,
                // which may be near an edge and lead to an immediate repeat fall.
                var safePosition = surface.position;
                // The player may have landed on a different-height edge or slope.
                // Match the checkpoint to the pad's actual center top so a later
                // rescue cannot respawn in midair or intersect the platform.
                if (TryGetTraversalPadTop(surface, safePosition, out var padTopY))
                {
                    var controllerScaleY = transform.lossyScale.y;
                    var controllerBottomOffset = (_controller.center.y - _controller.height * 0.5f)
                        * controllerScaleY;
                    safePosition.y = padTopY - controllerBottomOffset + _controller.skinWidth;
                }
                else
                {
                    // Keep the previously safe landing height if this pad has no
                    // queryable collider (for example, during an editor transition).
                    safePosition.y = transform.position.y;
                }

                var checkpointId = "pad:" + CheckpointVolume.BuildHierarchyPath(surface);
                if (SetCheckpoint(safePosition, transform.rotation, checkpointId))
                {
                    TempCraftSparkBurst.Spawn(safePosition + Vector3.up * 0.45f, 6, 0.3f);
                    TempSfxPlayer.PlayToastBlip();
                }
            }
        }

        private static bool TryGetTraversalPadTop(Transform pad, Vector3 center, out float topY)
        {
            var origin = center + Vector3.up * 8f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 24f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var closestDistance = float.PositiveInfinity;
            topY = 0f;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.distance >= closestDistance)
                {
                    continue;
                }

                var owner = hit.collider.transform;
                while (owner != null && owner != pad)
                {
                    owner = owner.parent;
                }

                if (owner != pad)
                {
                    continue;
                }

                closestDistance = hit.distance;
                topY = hit.point.y;
            }

            return !float.IsPositiveInfinity(closestDistance);
        }

        /// <returns>True when the checkpoint pose changed enough to matter.</returns>
        public bool SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            return SetCheckpoint(position, rotation, null);
        }

        public bool SetCheckpoint(Vector3 position, Quaternion rotation, string stableCheckpointId)
        {
            var currentXZ = new Vector2(_checkpointPosition.x, _checkpointPosition.z);
            var nextXZ = new Vector2(position.x, position.z);
            var sameCheckpointColumn = Vector2.Distance(currentXZ, nextXZ) < 0.35f
                && Mathf.Abs(_checkpointPosition.y - position.y) <= 0.75f;
            var sameIdentity = string.Equals(_checkpointId, stableCheckpointId, StringComparison.Ordinal);
            if (_hasCheckpoint && sameCheckpointColumn && sameIdentity)
            {
                return false;
            }

            _checkpointPosition = position;
            _checkpointRotation = rotation;
            _hasCheckpoint = true;
            _checkpointId = stableCheckpointId;
            if (!string.IsNullOrWhiteSpace(stableCheckpointId))
            {
                PlayerProgressSave.RecordActiveCheckpoint(SceneManager.GetActiveScene().name, stableCheckpointId);
            }

            return true;
        }

        private bool RestoreSavedCheckpoint()
        {
            var stableId = PlayerProgressSave.GetActiveCheckpointId(SceneManager.GetActiveScene().name);
            if (string.IsNullOrWhiteSpace(stableId))
            {
                return false;
            }

            if (stableId.StartsWith("volume:", StringComparison.Ordinal))
            {
                var volumes = FindObjectsByType<CheckpointVolume>();
                for (var i = 0; i < volumes.Length; i++)
                {
                    var volume = volumes[i];
                    if (volume == null || volume.StableId != stableId)
                    {
                        continue;
                    }

                    var pose = volume.Pose;
                    SetCheckpoint(pose.position, pose.rotation, stableId);
                    return true;
                }
            }
            else if (stableId.StartsWith("pad:", StringComparison.Ordinal))
            {
                var padPath = stableId.Substring("pad:".Length);
                var pad = CheckpointVolume.FindTransformByHierarchyPath(padPath);
                if (pad == null)
                {
                    return false;
                }

                var safePosition = pad.position;
                if (TryGetTraversalPadTop(pad, safePosition, out var padTopY))
                {
                    var controllerScaleY = transform.lossyScale.y;
                    var controllerBottomOffset = (_controller.center.y - _controller.height * 0.5f)
                        * controllerScaleY;
                    safePosition.y = padTopY - controllerBottomOffset + _controller.skinWidth;
                    SetCheckpoint(safePosition, pad.rotation, stableId);
                    return true;
                }
            }

            return false;
        }

        public void RespawnAtCheckpoint()
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(_checkpointPosition, _checkpointRotation);
            _controller.enabled = true;
            if (_motor != null)
            {
                _motor.HardResetState();
            }

            SplatMeshyAnimatorDriver.FindForPlayer(transform)?.ResetAfterRespawn();

            _invulnerableUntil = Time.time + invulnerabilityDuration;
            GameEvents.RaisePlayerRespawned();
        }

        public void ApplyKnockback(Vector3 worldDirection, float speed = -1f)
        {
            if (IsInvulnerable)
            {
                return;
            }

            if (_motor != null)
            {
                var dir = worldDirection.sqrMagnitude > 0.01f ? worldDirection.normalized : -transform.forward;
                dir.y = Mathf.Max(0.35f, dir.y);
                var launchSpeed = speed > 0f ? speed : knockbackSpeed;
                var knockbackDirection = dir.normalized;
                _motor.ApplyLaunch(knockbackDirection * launchSpeed);
                _knockbackEventCountForDiagnostics++;
                _lastKnockbackSpeedForDiagnostics = launchSpeed;
                var animator = GetComponent<SplatSkeletonAnimator>();
                if (animator != null)
                {
                    animator.PlayHitReaction(knockbackDirection);
                }
            }

            _invulnerableUntil = Time.time + invulnerabilityDuration;
        }
    }
}
