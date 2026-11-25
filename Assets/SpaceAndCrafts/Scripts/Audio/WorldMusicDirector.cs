using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.Audio
{
    /// <summary>
    /// Per-world ambient BGM. Prefers baked Resources clips; falls back to rich procedural loop.
    /// </summary>
    public sealed class WorldMusicDirector : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("WorldMusicDirector.Update");
        [SerializeField] private int worldIndex = 1;
        [SerializeField] private float volume = 0.22f;

        private AudioSource _source;
        private AudioSource _combat;
        private AudioClip _loop;
        private AudioClip _combatBed;
        private bool _ownsLoop;
        private bool _ownsCombat;
        private float _combatIntensity;
        private float _combatTarget;

        public static WorldMusicDirector Ensure(int worldIndex)
        {
            var existing = FindAnyObjectByType<WorldMusicDirector>();
            if (existing != null)
            {
                existing.Configure(worldIndex);
                return existing;
            }

            var go = new GameObject("SYS_WorldMusic");
            var director = go.AddComponent<WorldMusicDirector>();
            director.Configure(worldIndex);
            return director;
        }

        public void Configure(int index)
        {
            worldIndex = Mathf.Clamp(index, 1, 10);
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.loop = true;
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }

            _source.volume = volume;
            if (_ownsLoop && _loop != null)
            {
                Destroy(_loop);
            }

            if (_ownsCombat && _combatBed != null)
            {
                Destroy(_combatBed);
            }

            _loop = Resources.Load<AudioClip>($"Audio/Music/BGM_World{worldIndex:00}");
            _ownsLoop = false;
            if (_loop == null)
            {
                _loop = BuildLoopForWorld(worldIndex);
                _ownsLoop = true;
            }

            _combatBed = Resources.Load<AudioClip>("Audio/Music/BGM_CombatBed");
            _ownsCombat = false;
            if (_combatBed == null)
            {
                _combatBed = BuildCombatBed(worldIndex);
                _ownsCombat = true;
            }

            EnsureCombatSource();
            _source.clip = _loop;
            _combat.clip = _combatBed;
            if (!_source.isPlaying)
            {
                _source.Play();
            }

            if (!_combat.isPlaying)
            {
                _combat.volume = 0f;
                _combat.Play();
            }
        }

        public void SetCombatIntensity(float intensity01)
        {
            _combatTarget = Mathf.Clamp01(intensity01);
            if (_source == null)
            {
                return;
            }

            _source.pitch = Mathf.Lerp(1f, 1.03f, _combatTarget);
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            _combatIntensity = Mathf.MoveTowards(_combatIntensity, _combatTarget, Time.deltaTime * 1.4f);
            if (_source != null)
            {
                _source.volume = Mathf.Lerp(volume, volume * 0.72f, _combatIntensity);
            }

            if (_combat != null)
            {
                _combat.volume = volume * 0.9f * _combatIntensity;
            }
        }

        private void EnsureCombatSource()
        {
            if (_combat != null)
            {
                return;
            }

            _combat = gameObject.AddComponent<AudioSource>();
            _combat.loop = true;
            _combat.playOnAwake = false;
            _combat.spatialBlend = 0f;
            _combat.volume = 0f;
        }

        public void PlayCompleteStinger()
        {
            var stinger = Resources.Load<AudioClip>("Audio/Music/BGM_Stinger_Complete");
            if (stinger != null && _source != null)
            {
                _source.PlayOneShot(stinger, 0.7f);
            }
        }

        public void PlayCraftStinger()
        {
            var stinger = Resources.Load<AudioClip>("Audio/Music/BGM_Stinger_Craft");
            if (stinger != null && _source != null)
            {
                _source.PlayOneShot(stinger, 0.55f);
            }
        }

        private void Start()
        {
            if (_source == null || _source.clip == null)
            {
                Configure(worldIndex);
            }
        }

        private static AudioClip BuildCombatBed(int worldIndex)
        {
            const float duration = 4f;
            const int sampleRate = 22050;
            var samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];
            var root = MidiToHz(40 + (worldIndex % 5));
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var edge = Mathf.Clamp01(Mathf.Min(t, duration - t) / 0.15f);
                var pulse = Mathf.Sin(t * Mathf.PI * 6f);
                var kick = Mathf.Sin(2f * Mathf.PI * root * t) * 0.35f * Mathf.Max(0f, pulse);
                var hiss = (Mathf.PerlinNoise(t * 40f, worldIndex) - 0.5f) * 0.08f;
                data[i] = (kick + hiss) * edge * 0.55f;
            }

            var clip = AudioClip.Create("BGM_CombatBed_Rich", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildLoopForWorld(int worldIndex)
        {
            var rootMidi = 45 + (worldIndex - 1);
            var major = worldIndex % 2 == 1;
            var third = major ? 4 : 3;
            var root = MidiToHz(rootMidi);
            var thirdHz = MidiToHz(rootMidi + third);
            var fifthHz = MidiToHz(rootMidi + 7);
            var octave = root * 2f;
            var arp = new[] { root, thirdHz, fifthHz, octave, fifthHz, thirdHz };

            const float duration = 8f;
            const int sampleRate = 22050;
            var samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];
            var bpm = 92f + worldIndex * 2f;
            var beat = 60f / bpm;

            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var edge = Mathf.Clamp01(Mathf.Min(t, duration - t) / 0.2f);
                var bassEnv = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f / (beat * 2f));
                var bass = Mathf.Sin(2f * Mathf.PI * root * 0.5f * t) * 0.28f * bassEnv;
                var pad = Mathf.Sin(2f * Mathf.PI * root * t) * 0.12f
                           + Mathf.Sin(2f * Mathf.PI * thirdHz * t) * 0.1f
                           + Mathf.Sin(2f * Mathf.PI * fifthHz * t) * 0.09f;
                var step = Mathf.FloorToInt((t / (beat * 0.5f)) % arp.Length);
                var arpPhase = (t % (beat * 0.5f)) / (beat * 0.5f);
                var arpEnv = Mathf.Sin(Mathf.Clamp01(arpPhase) * Mathf.PI) * 0.7f;
                var sparkle = Mathf.Sin(2f * Mathf.PI * arp[step] * 2f * t) * 0.07f * arpEnv;
                data[i] = (bass + pad + sparkle) * edge * 0.7f;
            }

            var clip = AudioClip.Create($"BGM_World{worldIndex:00}_Rich", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float MidiToHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }
}
