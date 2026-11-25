using System.Collections;
using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// Per-world opening cinematic: brief GuideLookAt toward landmark / boss tease.
    /// </summary>
    public sealed class WorldOpeningVista : MonoBehaviour
    {
        [SerializeField] private PlatformingCameraRig cameraRig;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private float delay = 0.35f;
        [SerializeField] private float duration = 2.0f;
        [SerializeField] private float pitchDegrees = 14f;
        [SerializeField] private string toast = "A new world in Our Way Galaxy awaits…";

        public void Bind(PlatformingCameraRig rig, Transform target, string openingToast = null)
        {
            cameraRig = rig;
            lookTarget = target;
            if (!string.IsNullOrEmpty(openingToast))
            {
                toast = openingToast;
            }
        }

        private void Start()
        {
            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<PlatformingCameraRig>();
            }

            if (lookTarget == null)
            {
                lookTarget = ResolveDefaultTarget();
            }

            if (cameraRig == null || lookTarget == null)
            {
                return;
            }

            StartCoroutine(GuideAfterDelay());
        }

        private static Transform ResolveDefaultTarget()
        {
            var names = new[]
            {
                "ART_WorldLandmark",
                "ART_Landmark",
                "ART_WorldBossExam",
                "ART_Zone_BossLane",
            };
            for (var i = 0; i < names.Length; i++)
            {
                var go = GameObject.Find(names[i]);
                if (go != null)
                {
                    return go.transform;
                }
            }

            // Fall back to any object with Landmark in the name.
            var all = Object.FindObjectsByType<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name.Contains("Landmark") || all[i].name.Contains("BossExam"))
                {
                    return all[i];
                }
            }

            return null;
        }

        private IEnumerator GuideAfterDelay()
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (!string.IsNullOrEmpty(toast))
            {
                UI.GameplayFeedback.Toast(toast);
            }

            cameraRig.GuideLookAt(lookTarget.position + Vector3.up * 3.5f, duration, pitchDegrees);
            StoryBeatDirector.Ensure().PlayLookAt(lookTarget, duration * 0.4f);
        }
    }
}
