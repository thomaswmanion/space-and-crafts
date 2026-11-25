using SpaceAndCrafts.Art;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Mid-route kingdom "wow" — camera tease + toast when Splat enters a scenic spur.
    /// Thickens level design beyond traversal geometry alone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KingdomMomentTrigger : MonoBehaviour
    {
        [SerializeField] private string momentId = "kingdom_moment";
        [SerializeField] private string toast = "What a view!";
        [SerializeField] private Transform lookTarget;
        [SerializeField] private float lookSeconds = 1.4f;
        [SerializeField] private float pitchDegrees = 16f;

        private bool _fired;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_fired)
            {
                return;
            }

            if (other.GetComponentInParent<SplatMotor>() == null)
            {
                return;
            }

            _fired = true;
            var focus = lookTarget != null ? lookTarget : transform;
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.GuideLookAt(focus.position + Vector3.up * 2f, lookSeconds, pitchDegrees);
            }

            StoryBeatDirector.Ensure().PlayLookAt(focus, lookSeconds * 0.5f);
            TempCraftSparkBurst.Spawn(focus.position + Vector3.up * 0.4f, 6, 0.3f);
            if (!string.IsNullOrEmpty(toast))
            {
                GameplayFeedback.Toast(toast);
            }

            Debug.Log($"[KingdomMoment] {momentId}");
        }
    }
}
