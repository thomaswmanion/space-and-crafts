using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Invisible safety trigger under the level; missed falls return to the active checkpoint.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class FallVolume : MonoBehaviour
    {
        [SerializeField] private float toastCooldown = 1.25f;
        private Collider _collider;
        private Renderer _fjordWaterRenderer;
        private float _nextToastTime;

        /// <summary>Reports a real trigger rescue and whether the player landed over the fjord catch.</summary>
        public event System.Action<bool> PlayerRescued;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void LateUpdate() => PositionAtFjordSurface();

        /// <summary>
        /// Lets the water trigger deliver its splash before the emergency backup. Outside
        /// the scenic catch footprint, the normal checkpoint-relative rescue still applies.
        /// </summary>
        public bool TryGetEmergencyRescueY(Vector3 playerPosition, float defaultRescueY, out float rescueY)
        {
            PositionAtFjordSurface();
            rescueY = defaultRescueY;
            if (!IsOverFjordCatch(playerPosition) || _collider == null)
            {
                return false;
            }

            rescueY = Mathf.Min(defaultRescueY, _collider.bounds.min.y - 1.5f);
            return true;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null)
            {
                return;
            }

            var landedInFjord = TryGetFjordSurface(respawn.transform.position, out var splashPoint);
            respawn.RespawnAtCheckpoint();
            if (landedInFjord)
            {
                ArrivalFjordSplashEffect.Spawn(splashPoint);
            }

            if (Time.time >= _nextToastTime)
            {
                GameplayFeedback.Toast(landedInFjord
                    ? "Splash! Back on the trail."
                    : "Whoops — back to checkpoint!", playBlip: false);
                if (landedInFjord)
                {
                    TempSfxPlayer.PlaySplash();
                }
                else
                {
                    TempSfxPlayer.PlayLand();
                }
                _nextToastTime = Time.time + toastCooldown;
            }

            PlayerRescued?.Invoke(landedInFjord);
        }

        private bool TryGetFjordSurface(Vector3 fallPosition, out Vector3 surfacePoint)
        {
            CacheFjordWater();
            if (_fjordWaterRenderer == null || !IsOverFjordCatch(fallPosition))
            {
                surfacePoint = default;
                return false;
            }

            var bounds = _fjordWaterRenderer.bounds;
            surfacePoint = new Vector3(fallPosition.x, bounds.center.y + 0.08f, fallPosition.z);
            return true;
        }

        private bool IsOverFjordCatch(Vector3 position)
        {
            if (_collider == null || _fjordWaterRenderer == null)
            {
                return false;
            }

            var catchBounds = _collider.bounds;
            var waterBounds = _fjordWaterRenderer.bounds;
            return Mathf.Abs(position.x - catchBounds.center.x) <= catchBounds.extents.x
                && Mathf.Abs(position.z - catchBounds.center.z) <= catchBounds.extents.z
                && Mathf.Abs(position.x - waterBounds.center.x) <= waterBounds.extents.x
                && Mathf.Abs(position.z - waterBounds.center.z) <= waterBounds.extents.z;
        }

        private void PositionAtFjordSurface()
        {
            CacheFjordWater();
            if (_fjordWaterRenderer == null)
            {
                return;
            }

            var position = transform.position;
            var catchY = _fjordWaterRenderer.bounds.center.y - 0.9f;
            if (!Mathf.Approximately(position.y, catchY))
            {
                position.y = catchY;
                transform.position = position;
            }
        }

        private void CacheFjordWater()
        {
            if (_fjordWaterRenderer != null || !gameObject.scene.name.Contains("Arrival"))
            {
                return;
            }

            var water = GameObject.Find("ART_FjordWater");
            if (water != null)
            {
                _fjordWaterRenderer = water.GetComponent<Renderer>();
            }
        }
    }
}
