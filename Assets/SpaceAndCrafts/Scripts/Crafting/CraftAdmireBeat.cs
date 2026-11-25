using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// TEMP personality beat: after craft, Splat admires the invention briefly.
    /// </summary>
    public sealed class CraftAdmireBeat : MonoBehaviour
    {
        [SerializeField] private float delay = 0.3f;
        [SerializeField] private float lookDuration = 1.75f;
        [SerializeField] private float pitchDegrees = 18f;
        [SerializeField] private string admireToast = "Ooh — look at that Boing-O-Matic!";

        private void OnEnable()
        {
            GameEvents.BuildCompleted += HandleBuildCompleted;
        }

        private void OnDisable()
        {
            GameEvents.BuildCompleted -= HandleBuildCompleted;
        }

        private void HandleBuildCompleted(string inventionId)
        {
            StartCoroutine(AdmireRoutine(inventionId));
        }

        private IEnumerator AdmireRoutine(string inventionId)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var focus = FindInventionFocus();
            if (focus != null)
            {
                TempCraftSparkBurst.Spawn(focus.position + Vector3.up * 0.6f, 8, 0.35f);
                var idle = focus.GetComponentInChildren<InventionIdlePresenter>()
                           ?? focus.GetComponent<InventionIdlePresenter>();
                if (idle != null)
                {
                    idle.PulseActivate();
                }

                var cam = FindAnyObjectByType<PlatformingCameraRig>();
                if (cam != null)
                {
                    cam.GuideLookAt(focus.position + Vector3.up * 1.2f, lookDuration, pitchDegrees);
                    cam.Punch(0.14f);
                }

                StoryBeatDirector.Ensure().PlayLookAt(focus, lookDuration * 0.45f);
            }

            GameplayFeedback.Toast(
                InventionCompatibility.Satisfies("boing_o_matic", inventionId)
                    ? admireToast
                    : $"Ooh — {inventionId} looks ready!");
            var music = FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.PlayCraftStinger();
            }
        }

        private static Transform FindInventionFocus()
        {
            var idle = Object.FindObjectsByType<InventionIdlePresenter>();
            for (var i = 0; i < idle.Length; i++)
            {
                if (idle[i] != null && idle[i].gameObject.activeInHierarchy)
                {
                    return idle[i].transform;
                }
            }

            var invention = GameObject.Find("ART_BoingOMatic") ?? GameObject.Find("TEMP_ART_BoingOMatic");
            if (invention != null && invention.activeInHierarchy)
            {
                return invention.transform;
            }

            var site = FindAnyObjectByType<BuildSite>();
            return site != null ? site.transform : null;
        }
    }
}
