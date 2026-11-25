using SpaceAndCrafts.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Audio
{
    /// <summary>
    /// Gameplay SFX. Prefers baked Resources clips; falls back to procedural tones.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class TempSfxPlayer : MonoBehaviour
    {
        public static TempSfxPlayer Instance { get; private set; }

        public int JumpCuePlayCountForDiagnostics { get; private set; }
        public int LandCuePlayCountForDiagnostics { get; private set; }
        public int GroundPoundCuePlayCountForDiagnostics { get; private set; }
        public int FootstepCuePlayCountForDiagnostics { get; private set; }
        public int RelayHitCuePlayCountForDiagnostics { get; private set; }
        public int OuchCuePlayCountForDiagnostics { get; private set; }
        public bool HasMovementCueClipsForDiagnostics => _jump != null && _land != null && _foot != null;
        public bool AudioSourceReadyForDiagnostics => _source != null;

        private AudioSource _source;
        private AudioClip _collect;
        private AudioClip _collectSpring;
        private AudioClip _collectDish;
        private AudioClip _collectCrystal;
        private AudioClip _craft;
        private AudioClip _boing;
        private AudioClip _toast;
        private AudioClip _complete;
        private AudioClip _jump;
        private AudioClip _land;
        private AudioClip _splash;
        private AudioClip _pingline;
        private AudioClip _foot;
        private AudioClip _warn;
        private AudioClip _slam;
        private AudioClip _ouch;
        private AudioClip _expose;
        private AudioClip _relayHit;
        private readonly List<AudioClip> _generatedClips = new();
        // Craft pieces use a fixed eight-note palette. Cache these generated
        // clips so repeated builds reuse the same audio assets instead of
        // allocating an AudioClip on every weld event.
        private readonly AudioClip[] _weldSnaps = new AudioClip[8];

        private void Awake()
        {
            Instance = this;
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _collect = LoadOrTone("Audio/Sfx/SFX_Collect", "SFX_Collect", 880f, 0.08f, 0.25f);
            // The three Arrival components form a small rising C-major motif.
            // Separate harmonic profiles make the spring feel bouncy, the dish
            // metallic, and the crystal clear without adding more HUD noise.
            _collectSpring = TrackGenerated(MakeCollectChime("SFX_Collect_Spring", 523.25f, 0.12f, 2.02f, 3.01f, 0.18f, 0.07f, 24f));
            _collectDish = TrackGenerated(MakeCollectChime("SFX_Collect_Dish", 659.25f, 0.16f, 2.76f, 5.42f, 0.24f, 0.12f, 28f));
            _collectCrystal = TrackGenerated(MakeCollectChime("SFX_Collect_Crystal", 783.99f, 0.23f, 2f, 3.01f, 0.28f, 0.14f, 14f));
            _craft = LoadOrTone("Audio/Sfx/SFX_Craft", "SFX_Craft", 523.25f, 0.18f, 0.3f);
            _boing = LoadOrTone("Audio/Sfx/SFX_Boing", "SFX_Boing", 220f, 0.16f, 0.45f, 440f);
            _toast = LoadOrTone("Audio/Sfx/SFX_Toast", "SFX_Toast", 660f, 0.05f, 0.15f);
            _complete = LoadOrTone("Audio/Sfx/SFX_Complete", "SFX_Complete", 392f, 0.28f, 0.35f, 784f);
            _jump = LoadOrTone("Audio/Sfx/SFX_Jump", "SFX_Jump", 320f, 0.07f, 0.22f, 520f);
            _land = LoadOrTone("Audio/Sfx/SFX_Land", "SFX_Land", 180f, 0.06f, 0.28f, 90f);
            _splash = Resources.Load<AudioClip>("Audio/Sfx/SFX_Splash");
            if (_splash == null) _splash = TrackGenerated(MakeSplash("SFX_Splash_Fallback"));
            _pingline = TrackGenerated(MakePingline("SFX_Pingline_TwoNote"));
            _foot = LoadOrTone("Audio/Sfx/SFX_Foot", "SFX_Foot", 140f, 0.035f, 0.18f);
            _warn = LoadOrTone("Audio/Sfx/SFX_Warn", "SFX_Warn", 240f, 0.12f, 0.28f, 180f);
            _slam = LoadOrTone("Audio/Sfx/SFX_Slam", "SFX_Slam", 90f, 0.14f, 0.4f, 60f);
            _ouch = LoadOrTone("Audio/Sfx/SFX_Ouch", "SFX_Ouch", 420f, 0.09f, 0.28f, 180f);
            _expose = LoadOrTone("Audio/Sfx/SFX_Expose", "SFX_Expose", 480f, 0.14f, 0.28f, 720f);
            _relayHit = TrackGenerated(MakeCollectChime("SFX_RelayHit", 587.33f, 0.16f,
                2f, 3.01f, 0.32f, 0.14f, 17f));
        }

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += HandleCollect;
            GameEvents.BuildCompleted += HandleBuild;
            GameEvents.LevelCompleted += HandleComplete;
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= HandleCollect;
            GameEvents.BuildCompleted -= HandleBuild;
            GameEvents.LevelCompleted -= HandleComplete;
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _generatedClips.Count; i++)
            {
                if (_generatedClips[i] != null) Destroy(_generatedClips[i]);
            }
            _generatedClips.Clear();
            for (var i = 0; i < _weldSnaps.Length; i++) _weldSnaps[i] = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void PlayToastBlip()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._toast, 0.35f);
            }
        }

        public static void PlayBoing()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._boing, 0.55f);
            }
        }

        public static void PlayJump()
        {
            if (Instance != null)
            {
                Instance.JumpCuePlayCountForDiagnostics++;
                Instance.Play(Instance._jump, 0.32f);
            }
        }

        public static void PlayLand()
        {
            if (Instance != null)
            {
                Instance.LandCuePlayCountForDiagnostics++;
                Instance.Play(Instance._land, 0.38f);
            }
        }

        public static void PlaySplash()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._splash, 0.34f);
            }
        }

        /// <summary>Short two-note cave signal used when the Pingline Spooler finds an anchor.</summary>
        public static void PlayPingline()
        {
            if (Instance != null) Instance.Play(Instance._pingline, 0.42f);
        }

        /// <summary>Scale the landing cue to the measured impact without generating a new sound.</summary>
        public static void PlayLandImpact(float impactSpeed, bool groundPound = false)
        {
            if (Instance == null) return;
            Instance.LandCuePlayCountForDiagnostics++;
            var weight = Mathf.InverseLerp(2f, 14f, Mathf.Clamp(impactSpeed, 0f, 28f));
            var volume = Mathf.Lerp(0.18f, 0.50f, weight) + (groundPound ? 0.08f : 0f);
            Instance.Play(Instance._land, Mathf.Clamp(volume, 0.18f, 0.58f));
            if (groundPound)
            {
                Instance.GroundPoundCuePlayCountForDiagnostics++;
                // Layer the low slam beneath the ordinary contact transient so
                // a ground-pound still reads as a landing, with extra weight.
                Instance.Play(Instance._slam, Mathf.Lerp(0.24f, 0.48f, weight));
            }
        }

        public static void PlayFootstep()
        {
            if (Instance != null)
            {
                Instance.FootstepCuePlayCountForDiagnostics++;
                Instance.Play(Instance._foot, 0.22f);
            }
        }

        public static void PlayWarn()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._warn, 0.45f);
            }
        }

        public static void PlaySlam()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._slam, 0.55f);
            }
        }

        public static void PlayOuch()
        {
            if (Instance != null)
            {
                Instance.OuchCuePlayCountForDiagnostics++;
                Instance.Play(Instance._ouch, 0.4f);
            }
        }

        public static void PlayExpose()
        {
            if (Instance != null)
            {
                Instance.Play(Instance._expose, 0.45f);
            }
        }

        public static void PlayRelayHit()
        {
            if (Instance != null)
            {
                Instance.RelayHitCuePlayCountForDiagnostics++;
                Instance.Play(Instance._relayHit, 0.62f);
            }
        }

        /// <summary>
        /// TEMP ascending weld chirp for craft piece snaps. Big craft stinger stays on BuildCompleted.
        /// </summary>
        public static void PlayWeldSnap(int pieceIndex)
        {
            if (Instance == null)
            {
                return;
            }

            var step = Mathf.Clamp(pieceIndex, 0, 7);
            var clip = Instance._weldSnaps[step];
            if (clip == null)
            {
                var freq = 392f * Mathf.Pow(2f, step / 12f);
                clip = Instance.TrackGenerated(MakeTone($"SFX_Weld_{step}", freq, 0.07f, 0.3f, freq * 1.22f));
                Instance._weldSnaps[step] = clip;
            }
            Instance.Play(clip, 0.38f);
        }

        private void HandleCollect(Gameplay.CollectibleDefinition definition)
        {
            var clip = definition != null
                ? (definition.Id switch
                {
                    "component_spring" => _collectSpring,
                    "component_metal_dish" => _collectDish,
                    "component_power_crystal" => _collectCrystal,
                    _ => null
                })
                : null;
            Play(clip != null ? clip : _collect, 0.4f);
        }

        private void HandleBuild(string _) => Play(_craft, 0.5f);

        private void HandleComplete()
        {
            Play(_complete, 0.6f);
            var music = FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.PlayCompleteStinger();
            }
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(clip, volume);
        }

        private AudioClip LoadOrTone(
            string resourcePath, string name, float freq, float duration, float volume, float slideTo = -1f)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            return clip != null ? clip : TrackGenerated(MakeTone(name, freq, duration, volume, slideTo));
        }

        private AudioClip TrackGenerated(AudioClip clip)
        {
            if (clip != null) _generatedClips.Add(clip);
            return clip;
        }

        private static AudioClip MakeTone(string name, float freq, float duration, float volume, float slideTo = -1f)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var f = slideTo > 0f ? Mathf.Lerp(freq, slideTo, t / duration) : freq;
                var envelope = Mathf.Clamp01(1f - t / duration);
                envelope *= envelope;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * volume * envelope;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeSplash(string name)
        {
            const int sampleRate = 22050;
            const float duration = 0.32f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            uint randomState = 0x4f1bbcdcu;
            var filteredNoise = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                randomState = randomState * 1664525u + 1013904223u;
                var whiteNoise = ((randomState >> 8) / 8388607.5f) - 1f;
                // A quick, soft low-pass splash avoids the harsh hiss of raw noise.
                filteredNoise += (whiteNoise - filteredNoise) * 0.22f;
                var fade = Mathf.Clamp01(1f - t / duration);
                var envelope = fade * fade;
                var bubblePhase = 2f * Mathf.PI * (255f * t - 210f * t * t);
                var bubble = Mathf.Sin(bubblePhase) * Mathf.Exp(-18f * t) * 0.12f;
                data[i] = (filteredNoise * 0.26f + bubble) * envelope;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakePingline(string name)
        {
            const int sampleRate = 22050;
            const float duration = 0.34f;
            const float noteLength = 0.105f;
            const float gap = 0.035f;
            const float first = 587.33f; // D5
            const float second = 880f;   // A5
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var local = t < noteLength ? t : t >= noteLength + gap ? t - noteLength - gap : -1f;
                if (local < 0f || local >= noteLength) continue;
                var freq = t < noteLength ? first : second;
                var attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(local / 0.009f));
                var release = Mathf.Clamp01((noteLength - local) / 0.038f);
                var envelope = attack * release * Mathf.Exp(-5f * local);
                var phase = 2f * Mathf.PI * freq * local;
                data[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.13f) * envelope * 0.34f;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeCollectChime(string name, float fundamental, float duration,
            float secondPartial, float thirdPartial, float secondWeight, float thirdWeight, float decay)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var fade = Mathf.Clamp01(1f - t / duration);
                var envelope = fade * fade * Mathf.Exp(-decay * t);
                var phase = 2f * Mathf.PI * fundamental * t;
                var tone = Mathf.Sin(phase)
                    + Mathf.Sin(phase * secondPartial) * secondWeight
                    + Mathf.Sin(phase * thirdPartial) * thirdWeight;
                data[i] = tone * envelope * 0.42f;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
