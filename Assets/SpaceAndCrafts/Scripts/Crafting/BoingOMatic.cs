using System.Collections;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// Launch invention for Arrival. Used for traversal first, mini-boss second.
    /// </summary>
    public sealed class BoingOMatic : MonoBehaviour
    {
        [SerializeField] private float launchSpeed = 22f;
        [SerializeField] private Vector3 launchDirection = Vector3.up;
        [SerializeField] private float cooldown = 0.35f;
        [SerializeField] private string inventionId = "boing_o_matic";
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool toastOnLaunch = true;
        [SerializeField] private string launchToast = "BOING!";
        [SerializeField] private UnityEvent launched;

        private float _nextReadyTime;
        private Vector3 _visualRestScale = Vector3.one;
        private bool _taughtOnce;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _visualRestScale = visualRoot.localScale;

            // Meshy FBX×100 + local SphereCollider.radius ≈ 180m world trigger — OnTriggerStay
            // launch-storms Splat and inflates jump characterization (~18m gain). Playable pads are ~1–2m.
            if (TryGetInsaneWorldTriggerRadius(out var worldRadius))
            {
                Debug.LogWarning(
                    $"[BoingOMatic] Disabling '{name}' — world trigger radius {worldRadius:0.0}m is not playable.");
                enabled = false;
                foreach (var col in GetComponents<Collider>())
                {
                    col.enabled = false;
                }
            }
        }

        private bool TryGetInsaneWorldTriggerRadius(out float worldRadius)
        {
            worldRadius = 0f;
            var sphere = GetComponent<SphereCollider>();
            if (sphere == null || !sphere.enabled)
            {
                return false;
            }

            var lossy = transform.lossyScale;
            var avg = (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f;
            worldRadius = sphere.radius * avg;
            return worldRadius > 8f;
        }

        /// <summary>Flavor from BuildSite recipe variants (turbo / standard / etc).</summary>
        public void ApplyRecipeFlavor(BuildRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (recipe.launchSpeedOverride > 0f)
            {
                launchSpeed = recipe.launchSpeedOverride;
            }

            if (!string.IsNullOrWhiteSpace(recipe.inventionId))
            {
                inventionId = recipe.inventionId;
            }

            if (!string.IsNullOrWhiteSpace(recipe.launchToast))
            {
                launchToast = recipe.launchToast;
            }

            if (visualRoot != null && recipe.accentTint != Color.white)
            {
                foreach (var r in visualRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || r.sharedMaterial == null)
                    {
                        continue;
                    }

                    // Instance material so we don't tint every Boing in the project.
                    var mat = r.material;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", recipe.accentTint);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.color = recipe.accentTint;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other) => TryLaunch(other);

        // Stay disabled: oversized Meshy triggers were re-launching every cooldown while overlapping.

        private void TryLaunch(Collider other)
        {
            if (Time.time < _nextReadyTime)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            var direction = transform.TransformDirection(launchDirection.normalized);
            var meshAnimator = SplatMeshyAnimatorDriver.FindForPlayer(motor.transform);
            if (meshAnimator != null)
            {
                // Select the invention action before ApplyCraftedLaunch raises
                // SplatMotor.Jumped synchronously; the driver can then suppress
                // the competing generic Jump trigger for this exact launch.
                meshAnimator.TriggerUseInvention();
            }
            motor.ApplyCraftedLaunch(direction * launchSpeed);
            GameEvents.RaiseInventionUsed(inventionId);
            _nextReadyTime = Time.time + cooldown;
            launched?.Invoke();
            TempSfxPlayer.PlayBoing();
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.18f);
            }

            // First launch teaches; later launches stay visual so the streak owns the wow.
            if (toastOnLaunch && !_taughtOnce && !string.IsNullOrEmpty(launchToast))
            {
                GameplayFeedback.Toast(launchToast);
                _taughtOnce = true;
            }

            StopAllCoroutines();
            StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            var elapsed = 0f;
            const float down = 0.08f;
            const float up = 0.18f;
            while (elapsed < down)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / down;
                visualRoot.localScale = Vector3.Lerp(_visualRestScale, Vector3.Scale(_visualRestScale, new Vector3(1.25f, 0.55f, 1.25f)), t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < up)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / up;
                visualRoot.localScale = Vector3.Lerp(
                    Vector3.Scale(_visualRestScale, new Vector3(1.25f, 0.55f, 1.25f)),
                    Vector3.Scale(_visualRestScale, new Vector3(0.9f, 1.35f, 0.9f)),
                    t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, _visualRestScale, elapsed / 0.2f);
                yield return null;
            }

            visualRoot.localScale = _visualRestScale;
        }
    }
}
