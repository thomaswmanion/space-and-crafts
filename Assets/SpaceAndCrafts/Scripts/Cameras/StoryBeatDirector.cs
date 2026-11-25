using System.Collections;
using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// Lightweight story beat sequencer (stand-in until Timeline cutscenes).
    /// Runs a short hold → ease look → release for openings, craft, and boss intros.
    /// </summary>
    public sealed class StoryBeatDirector : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float defaultHold = 1.4f;
        [SerializeField] private float easeSeconds = 0.85f;

        private Coroutine _running;

        public static StoryBeatDirector Ensure()
        {
            var existing = FindAnyObjectByType<StoryBeatDirector>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("SYS_StoryBeatDirector");
            return go.AddComponent<StoryBeatDirector>();
        }

        public void PlayLookAt(Transform target, float holdSeconds = -1f)
        {
            if (target == null)
            {
                return;
            }

            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(LookRoutine(target, holdSeconds > 0f ? holdSeconds : defaultHold));
        }

        private IEnumerator LookRoutine(Transform target, float hold)
        {
            var rig = FindAnyObjectByType<PlatformingCameraRig>();
            if (rig != null)
            {
                // Soft punch + brief framing nudge sells the beat without Timeline.
                rig.Punch(0.05f);
            }

            var cam = Camera.main;
            if (cam == null)
            {
                yield break;
            }

            var start = cam.transform.rotation;
            var elapsed = 0f;
            while (elapsed < easeSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / easeSeconds);
                var look = Quaternion.LookRotation((target.position - cam.transform.position).normalized, Vector3.up);
                cam.transform.rotation = Quaternion.Slerp(start, look, t * 0.55f);
                yield return null;
            }

            yield return new WaitForSeconds(hold);

            elapsed = 0f;
            var held = cam.transform.rotation;
            while (elapsed < easeSeconds * 0.75f)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / (easeSeconds * 0.75f));
                cam.transform.rotation = Quaternion.Slerp(held, start, t);
                yield return null;
            }

            _running = null;
        }
    }
}
