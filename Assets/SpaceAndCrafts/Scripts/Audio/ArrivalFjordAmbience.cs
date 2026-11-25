using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Audio
{
    /// <summary>
    /// Quiet procedural wind and distant water wash for Arrival's fjord.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ArrivalFjordAmbience : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const int ClipLengthSeconds = 2;
        private const int ChannelCount = 2;

        [SerializeField, Range(0f, 0.25f)] private float targetVolume = 0.12f;
        [SerializeField, Min(0.1f)] private float fadeInSeconds = 2f;

        private AudioSource _source;
        private AudioClip _clip;
        private readonly ChannelState[] _channels =
        {
            new ChannelState(0x91e10da5u),
            new ChannelState(0x6d2b79f5u),
        };
        private long _samplePosition;

        private struct ChannelState
        {
            public uint randomState;
            public float windLow;
            public float windMid;
            public float waterLow;

            public ChannelState(uint seed)
            {
                randomState = seed;
                windLow = 0f;
                windMid = 0f;
                waterLow = 0f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (FindAnyObjectByType<ArrivalFjordAmbience>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalFjordAmbience");
            go.AddComponent<ArrivalFjordAmbience>();
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.priority = 180;
            _source.volume = 0f;
            _clip = AudioClip.Create(
                "Arrival_Fjord_Wind_And_Water",
                SampleRate * ClipLengthSeconds,
                ChannelCount,
                SampleRate,
                true,
                FillSamples);
            _source.clip = _clip;
            _source.Play();
            StartCoroutine(FadeIn());
        }

        private void FillSamples(float[] data)
        {
            for (var i = 0; i + 1 < data.Length; i += ChannelCount)
            {
                var seconds = _samplePosition / (double)SampleRate;
                var left = SampleChannel(ref _channels[0], seconds);
                var right = SampleChannel(ref _channels[1], seconds + 0.37);
                data[i] = left;
                data[i + 1] = right;
                _samplePosition++;
            }
        }

        private static float SampleChannel(ref ChannelState state, double seconds)
        {
            state.randomState = unchecked(state.randomState * 1664525u + 1013904223u);
            var noise = ((state.randomState >> 8) / 8388607.5f) - 1f;

            state.windLow += (noise - state.windLow) * 0.035f;
            state.windMid += (noise - state.windMid) * 0.19f;
            state.waterLow += (noise - state.waterLow) * 0.075f;

            var windBand = state.windLow * 0.72f + (state.windMid - state.windLow) * 0.28f;
            var waterWash = noise - state.waterLow;
            var gust = 0.68
                + 0.2 * System.Math.Sin(seconds * 0.21)
                + 0.09 * System.Math.Sin(seconds * 0.47 + 1.2);
            var swell = 0.64
                + 0.24 * System.Math.Sin(seconds * 0.31 + 0.8)
                + 0.1 * System.Math.Sin(seconds * 0.13 + 2.4);

            return (float)(windBand * gust * 0.18 + waterWash * swell * 0.035);
        }

        private IEnumerator FadeIn()
        {
            var duration = Mathf.Max(0.1f, fadeInSeconds);
            var elapsed = 0f;
            while (elapsed < duration && _source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _source.volume = targetVolume * Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            if (_source != null)
            {
                _source.volume = targetVolume;
            }
        }

        private void OnDestroy()
        {
            if (_clip != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_clip);
                }
                else
                {
                    DestroyImmediate(_clip);
                }
            }
        }
    }
}
