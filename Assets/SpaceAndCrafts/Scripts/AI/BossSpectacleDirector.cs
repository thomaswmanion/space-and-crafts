using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Galaxy-leaning boss spectacle layer: camera punch, music surge, sparks, look beat.
    /// </summary>
    public static class BossSpectacleDirector
    {
        public static void PlayPhaseSurge(Transform bossRoot, int phase)
        {
            if (bossRoot == null)
            {
                return;
            }

            TempCraftSparkBurst.Spawn(bossRoot.position + Vector3.up * 1.2f, 10 + phase * 4, 0.4f + phase * 0.1f);
            TempSfxPlayer.PlaySlam();
            SpawnArenaPulse(bossRoot.position, 2.4f + phase * 0.55f);

            var cam = Object.FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.12f + phase * 0.05f);
                cam.GuideLookAt(bossRoot.position + Vector3.up * 2f, 0.9f, 16f + phase * 2f);
            }

            StoryBeatDirector.Ensure().PlayLookAt(bossRoot, 0.55f);

            var music = Object.FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.SetCombatIntensity(0.45f + phase * 0.2f);
            }
        }

        public static void PlayClearFanfare(Transform bossRoot)
        {
            if (bossRoot == null)
            {
                return;
            }

            TempCraftSparkBurst.Spawn(bossRoot.position + Vector3.up * 1.5f, 18, 0.65f);
            var music = Object.FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.SetCombatIntensity(0.15f);
                music.PlayCompleteStinger();
            }

            var cam = Object.FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.2f);
            }
        }

        /// <summary>Readable ground telegraph ring that expands then fades.</summary>
        private static void SpawnArenaPulse(Vector3 center, float radius)
        {
            var go = new GameObject("FX_BossArenaPulse");
            go.transform.position = center + Vector3.up * 0.05f;
            var mesh = OrganicMeshFactory.CreateMeshObject(
                "pulse",
                OrganicMeshFactory.Torus(Mathf.Max(0.6f, radius * 0.45f), 0.06f, 28, 8),
                go.transform,
                Vector3.zero,
                new Vector3(1f, 0.2f, 1f),
                null);
            var runner = go.AddComponent<ArenaPulseRunner>();
            runner.Configure(mesh.transform, radius, 0.7f);
        }

        private sealed class ArenaPulseRunner : MonoBehaviour
        {
            private Transform _ring;
            private float _life;
            private float _duration;
            private Vector3 _startScale;

            public void Configure(Transform ring, float targetRadius, float duration)
            {
                _ring = ring;
                _duration = Mathf.Max(0.2f, duration);
                _life = _duration;
                _startScale = ring.localScale;
                ring.localScale = _startScale * 0.35f;
            }

            private void Update()
            {
                _life -= Time.deltaTime;
                var t = 1f - Mathf.Clamp01(_life / _duration);
                if (_ring != null)
                {
                    _ring.localScale = Vector3.Lerp(_startScale * 0.35f, _startScale * 1.8f, t);
                }

                if (_life <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
