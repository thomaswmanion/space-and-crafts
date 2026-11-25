using SpaceAndCrafts.Player;
using SpaceAndCrafts.Core;
using UnityEngine;
using Unity.Profiling;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// Third-person platforming camera: player-controlled orbit, smooth follow, collision.
    /// Does not own locomotion; SplatMotor reads this transform's yaw.
    /// </summary>
    public sealed class PlatformingCameraRig : MonoBehaviour
    {
        private static readonly ProfilerMarker LateUpdateMarker = new ProfilerMarker("PlatformingCameraRig.LateUpdate");
        [SerializeField] private CameraSettings settings;
        [SerializeField] private SplatInputReader input;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private UnityEngine.Camera gameCamera;
        [SerializeField] private CameraCollision collision;
        [SerializeField, Range(0f, 18f)] private float airborneLandingReadabilityTilt = 10f;
        [SerializeField, Min(0.01f)] private float airborneTiltSmoothTime = 0.16f;

        private float _yaw;
        private float _pitch = 9f;
        private Vector3 _followVelocity;
        private float _currentDistance;
        private float _guideUntil;
        private float _guideYaw;
        private float _guidePitch;
        private Vector3 _punchOffset;
        private Vector2 _lookSmoothed;
        private Vector2 _lookVelocity;
        private SplatMotor _motor;
        private PlayerLandingShadow _landingShadow;
        private float _autoYawHold;
        private float _mouseLookSensitivity;
        private float _gamepadLookMultiplier;
        private float _airborneTilt;
        private float _airborneTiltVelocity;

        public float OrbitDistanceForDiagnostics => _currentDistance;
        public float CameraArmDistanceForDiagnostics => cameraPivot != null && gameCamera != null
            ? Vector3.Distance(cameraPivot.position, gameCamera.transform.position)
            : 0f;

        private void Awake()
        {
            if (followTarget != null)
            {
                _yaw = followTarget.eulerAngles.y;
                _motor = followTarget.GetComponentInParent<SplatMotor>();
                _landingShadow = _motor != null ? _motor.GetComponent<PlayerLandingShadow>() : null;
            }

            // Keep the approach sightline above ground clutter; airborne tilt
            // adds downward landing readability when Splat leaves a platform.
            _pitch = 9f;
            _currentDistance = settings != null ? settings.Distance : 7f;
            _mouseLookSensitivity = PlayerCameraPreferences.GetMouseLookSensitivity(
                settings != null ? settings.MouseLookSensitivity : 0.11f);
            _gamepadLookMultiplier = PlayerCameraPreferences.GetGamepadLookMultiplier(
                settings != null ? settings.GamepadLookMultiplier : 1f);
        }

        private void OnEnable()
        {
            GameEvents.PlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            GameEvents.PlayerRespawned -= HandlePlayerRespawned;
        }

        private void HandlePlayerRespawned()
        {
            if (followTarget == null)
            {
                return;
            }

            // A checkpoint teleport should recenter immediately instead of sweeping
            // the camera across the distance from the fall location.
            transform.position = followTarget.position;
            // PlayerRespawn restores the saved checkpoint facing before it raises
            // this event. Recenter orbit yaw too, so the return view is not left
            // looking back toward the ledge Splat fell from.
            _yaw = followTarget.eulerAngles.y;
            _followVelocity = Vector3.zero;
            _punchOffset = Vector3.zero;
            _lookSmoothed = Vector2.zero;
            _lookVelocity = Vector2.zero;
            _guideUntil = 0f;
            _autoYawHold = 0f;
        }

        private void Start()
        {
            _pitch = Mathf.Clamp(_pitch, settings != null ? settings.MinPitch : -18f, 28f);
            if (_currentDistance < 5f)
            {
                _currentDistance = settings != null ? settings.Distance : 7f;
            }
        }

        /// <summary>Briefly blends orbit toward a world point (level-complete gateway tease).</summary>
        public void GuideLookAt(Vector3 worldPoint, float duration, float pitchDegrees = 18f)
        {
            var to = worldPoint - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
            {
                return;
            }

            _guideYaw = Quaternion.LookRotation(to.normalized, Vector3.up).eulerAngles.y;
            _guidePitch = pitchDegrees;
            _guideUntil = Time.time + Mathf.Max(0.1f, duration);
            _autoYawHold = 0f;
        }

        /// <summary>TEMP impact punch for slam readability. Decays in LateUpdate.</summary>
        public void Punch(float intensity = 0.22f)
        {
            var kick = Random.insideUnitSphere * Mathf.Max(0.01f, intensity);
            kick.z *= 0.35f;
            _punchOffset += kick;
        }

        private void LateUpdate()
        {
            using var markerScope = LateUpdateMarker.Auto();
            if (settings == null || followTarget == null || cameraPivot == null || gameCamera == null)
            {
                return;
            }

            if (Time.time < _guideUntil)
            {
                _yaw = Mathf.LerpAngle(_yaw, _guideYaw, 1f - Mathf.Exp(-3f * Time.deltaTime));
                _pitch = Mathf.Lerp(_pitch, _guidePitch, 1f - Mathf.Exp(-3f * Time.deltaTime));
            }
            else if (input != null)
            {
                if (input.LookIsMouseDelta)
                {
                    // Mouse delta is a per-frame displacement, not a look rate. Apply it
                    // directly so sensitivity does not change with frame rate or stick shaping.
                    _lookSmoothed = Vector2.zero;
                    _lookVelocity = Vector2.zero;
                    _yaw += input.Look.x * _mouseLookSensitivity;
                    var pitchRatio = settings.PitchSensitivity / Mathf.Max(0.01f, settings.OrbitSensitivity);
                    _pitch -= input.Look.y * _mouseLookSensitivity * pitchRatio;
                }
                else
                {
                    var lookScale = input.LookIsGamepadStick ? _gamepadLookMultiplier : 1f;
                    var raw = input.Look * lookScale;
                    var lookSmooth = Mathf.Max(0.0001f, settings.LookSmoothTime);
                    _lookSmoothed = Vector2.SmoothDamp(_lookSmoothed, raw, ref _lookVelocity, lookSmooth);
                    _yaw += _lookSmoothed.x * settings.OrbitSensitivity * Time.deltaTime;
                    _pitch -= _lookSmoothed.y * settings.PitchSensitivity * Time.deltaTime;
                }

                _pitch = Mathf.Clamp(_pitch, settings.MinPitch, settings.MaxPitch);
                ApplyAutoYaw(Time.deltaTime);
            }

            var targetPos = followTarget.position;
            // Subtle look-ahead softens tracking when Splat is sprinting.
            if (_motor != null)
            {
                targetPos += _motor.PlanarVelocity * 0.09f;
            }

            var currentFollowPosition = transform.position;
            var horizontalCurrent = new Vector3(currentFollowPosition.x, 0f, currentFollowPosition.z);
            var horizontalTarget = new Vector3(targetPos.x, 0f, targetPos.z);
            var horizontalVelocity = new Vector3(_followVelocity.x, 0f, _followVelocity.z);
            var horizontalPosition = Vector3.SmoothDamp(
                horizontalCurrent,
                horizontalTarget,
                ref horizontalVelocity,
                settings.FollowSmoothTime);

            // Large jump and touchdown velocities made the shared X/Y/Z damping
            // leave Splat near the bottom crop for several frames after landing.
            // Keep the deliberate planar camera ease while letting vertical motion
            // recenter promptly enough to preserve the full character silhouette.
            var verticalVelocity = _followVelocity.y;
            var verticalFollowSmoothTime = Mathf.Max(0.0001f,
                Mathf.Min(settings.FollowSmoothTime, 0.05f));
            var verticalPosition = Mathf.SmoothDamp(
                currentFollowPosition.y,
                targetPos.y,
                ref verticalVelocity,
                verticalFollowSmoothTime);

            transform.position = new Vector3(
                horizontalPosition.x,
                verticalPosition,
                horizontalPosition.z);
            _followVelocity = new Vector3(
                horizontalVelocity.x,
                verticalVelocity,
                horizontalVelocity.z);

            // Aim at Splat's body instead of his feet. This leaves room around the
            // silhouette and reduces the amount of nearby floor that fills the frame.
            cameraPivot.localPosition = Vector3.up * settings.FocusHeight;
            if (_landingShadow == null && _motor != null)
            {
                _landingShadow = _motor.GetComponent<PlayerLandingShadow>();
            }

            var shadowHeight = _landingShadow != null && _landingShadow.IsVisibleForDiagnostics
                ? _landingShadow.VerticalGapForDiagnostics
                : 0f;
            var shadowReadability01 = Mathf.Clamp01(shadowHeight / 4f);
            var targetAirborneTilt = _motor != null && !_motor.IsGrounded
                ? airborneLandingReadabilityTilt * shadowReadability01
                : 0f;
            _airborneTilt = Mathf.SmoothDamp(_airborneTilt, targetAirborneTilt,
                ref _airborneTiltVelocity, airborneTiltSmoothTime);
            var framedPitch = Mathf.Clamp(_pitch + _airborneTilt,
                settings.MinPitch, settings.MaxPitch);
            cameraPivot.rotation = Quaternion.Euler(framedPitch, _yaw, 0f);

            var desiredDistance = settings.Distance;
            if (collision != null)
            {
                desiredDistance = collision.ResolveDistance(desiredDistance, settings.HorizontalOffset);
            }

            // Pull-in snappier than ease-out so smoke/walls read immediately.
            var distanceRate = desiredDistance < _currentDistance - 0.05f ? 22f : 12f;
            _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, 1f - Mathf.Exp(-distanceRate * Time.deltaTime));
            _punchOffset = Vector3.Lerp(_punchOffset, Vector3.zero, 1f - Mathf.Exp(-16f * Time.deltaTime));
            gameCamera.transform.localPosition = new Vector3(settings.HorizontalOffset,
                settings.Height, -_currentDistance) + _punchOffset;
            // Height lifts the camera mount above its orbit pivot. Inheriting the
            // pivot's rotation with that offset aimed the optical center well above
            // Splat, pinning him to the bottom of the screen. Keep the mount height,
            // but aim the lens back at the pivot so character and route share a frame.
            var cameraToFocus = cameraPivot.position - gameCamera.transform.position;
            if (cameraToFocus.sqrMagnitude > 0.0001f)
            {
                gameCamera.transform.rotation = Quaternion.LookRotation(cameraToFocus, Vector3.up);
            }

            collision?.UpdateOcclusionFade();

            var speed01 = 0f;
            if (_motor != null)
            {
                speed01 = Mathf.Clamp01(_motor.PlanarVelocity.magnitude / 8f);
            }

            var fovBreath = settings.FieldOfView + speed01 * 2.5f;
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, fovBreath, 1f - Mathf.Exp(-6f * Time.deltaTime));

            // Soft height settle when speeding up — keeps framing readable without bob noise.
            var heightTarget = settings.Height + speed01 * 0.15f;
            var local = gameCamera.transform.localPosition;
            local.y = Mathf.Lerp(local.y, heightTarget, 1f - Mathf.Exp(-5f * Time.deltaTime));
            gameCamera.transform.localPosition = local;
        }

        private void ApplyAutoYaw(float dt)
        {
            if (!settings.AutoYawEnabled || input == null)
            {
                _autoYawHold = 0f;
                return;
            }

            var lookMag = input.Look.magnitude;
            var moveMag = input.Move.magnitude;
            if (lookMag >= settings.AutoYawLookCancel || moveMag < settings.AutoYawMoveThreshold)
            {
                _autoYawHold = 0f;
                return;
            }

            _autoYawHold += dt;
            if (_autoYawHold < settings.AutoYawDelay)
            {
                return;
            }

            Vector3 facing;
            if (_motor != null && _motor.PlanarVelocity.sqrMagnitude > 0.25f)
            {
                facing = _motor.PlanarVelocity;
            }
            else
            {
                // Fall back to camera-relative move intent when velocity is still winding up.
                var forward = transform.forward;
                var right = transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                facing = forward * input.Move.y + right * input.Move.x;
            }

            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f)
            {
                return;
            }

            var desiredYaw = Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y;
            var strength = Mathf.Max(0.01f, settings.AutoYawStrength);
            _yaw = Mathf.LerpAngle(_yaw, desiredYaw, 1f - Mathf.Exp(-strength * dt));
        }

    }
}
