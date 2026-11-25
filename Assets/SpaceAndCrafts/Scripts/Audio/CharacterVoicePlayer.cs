using System.Collections.Generic;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Audio
{
    /// <summary>
    /// Character voice barks. Prefers baked Resources VO; falls back to formant chirps.
    /// </summary>
    public sealed class CharacterVoicePlayer : MonoBehaviour
    {
        [SerializeField] private SplatMotor motor;
        [SerializeField] private float voiceVolume = 0.55f;

        private AudioSource _source;
        private float _nextFootVoice;
        private AudioClip _voJump;
        private AudioClip _voLand;
        private AudioClip _voCollect;
        private AudioClip _voCraft;
        private AudioClip _voComplete;
        private AudioClip _voHurt;
        private AudioClip _voWarn;
        private AudioClip _voEffort;
        private readonly Dictionary<(float startHz, float endHz, float duration, float sourceVolume), AudioClip>
            _generatedBarks = new();

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0.35f;
            _source.volume = voiceVolume;
            _voJump = Resources.Load<AudioClip>("Audio/Voice/VO_Jump");
            _voLand = Resources.Load<AudioClip>("Audio/Voice/VO_Land");
            _voCollect = Resources.Load<AudioClip>("Audio/Voice/VO_Collect");
            _voCraft = Resources.Load<AudioClip>("Audio/Voice/VO_Craft");
            _voComplete = Resources.Load<AudioClip>("Audio/Voice/VO_Complete");
            _voHurt = Resources.Load<AudioClip>("Audio/Voice/VO_Hurt");
            _voWarn = Resources.Load<AudioClip>("Audio/Voice/VO_Warn");
            _voEffort = Resources.Load<AudioClip>("Audio/Voice/VO_Effort");
        }

        private void OnEnable()
        {
            if (motor != null)
            {
                motor.Jumped += OnJump;
                motor.Landed += OnLand;
            }

            GameEvents.CollectibleAcquired += OnCollect;
            GameEvents.BuildCompleted += OnCraft;
            GameEvents.LevelCompleted += OnComplete;
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.Jumped -= OnJump;
                motor.Landed -= OnLand;
            }

            GameEvents.CollectibleAcquired -= OnCollect;
            GameEvents.BuildCompleted -= OnCraft;
            GameEvents.LevelCompleted -= OnComplete;
        }

        private void Update()
        {
            if (motor == null || !motor.IsGrounded)
            {
                return;
            }

            var speed = motor.PlanarVelocity.magnitude;
            if (speed < 3.5f || Time.time < _nextFootVoice)
            {
                return;
            }

            if (_voEffort != null)
            {
                _source.PlayOneShot(_voEffort, 0.22f);
            }
            else
            {
                PlayBark(380f, 520f, 0.07f, 0.22f);
            }

            _nextFootVoice = Time.time + Mathf.Lerp(0.55f, 0.32f, Mathf.InverseLerp(3.5f, 7.5f, speed));
        }

        public void PlayHurt()
        {
            if (_voHurt != null)
            {
                _source.PlayOneShot(_voHurt, 0.45f);
            }
            else
            {
                PlayBark(220f, 160f, 0.16f, 0.45f);
            }
        }

        public void PlayWarn()
        {
            if (_voWarn != null)
            {
                _source.PlayOneShot(_voWarn, 0.4f);
            }
            else
            {
                PlayBark(480f, 620f, 0.12f, 0.4f);
            }
        }

        private void OnJump()
        {
            if (_voJump != null)
            {
                _source.PlayOneShot(_voJump, 0.4f);
            }
            else
            {
                PlayPhrase(new[] { (520f, 780f, 0.08f), (700f, 820f, 0.06f) }, 0.4f);
            }
        }

        private void OnLand()
        {
            if (_voLand != null)
            {
                _source.PlayOneShot(_voLand, 0.35f);
            }
            else
            {
                PlayPhrase(new[] { (340f, 260f, 0.07f), (280f, 220f, 0.05f) }, 0.35f);
            }
        }

        private void OnCollect(CollectibleDefinition _)
        {
            if (_voCollect != null)
            {
                _source.PlayOneShot(_voCollect, 0.42f);
            }
            else
            {
                PlayPhrase(new[] { (600f, 760f, 0.07f), (760f, 920f, 0.08f), (880f, 980f, 0.06f) }, 0.42f);
            }
        }

        private void OnCraft(string _)
        {
            if (_voCraft != null)
            {
                _source.PlayOneShot(_voCraft, 0.5f);
            }
            else
            {
                PlayPhrase(new[] { (440f, 560f, 0.09f), (560f, 720f, 0.1f), (720f, 980f, 0.12f) }, 0.5f);
            }
        }

        private void OnComplete()
        {
            if (_voComplete != null)
            {
                _source.PlayOneShot(_voComplete, 0.55f);
            }
            else
            {
                PlayPhrase(new[] { (500f, 680f, 0.1f), (680f, 860f, 0.1f), (860f, 1100f, 0.14f) }, 0.55f);
            }
        }

        private void PlayPhrase((float start, float end, float dur)[] syllables, float volume)
        {
            if (_source == null || syllables == null || syllables.Length == 0)
            {
                return;
            }

            StartCoroutine(PhraseRoutine(syllables, volume));
        }

        private System.Collections.IEnumerator PhraseRoutine((float start, float end, float dur)[] syllables, float volume)
        {
            for (var i = 0; i < syllables.Length; i++)
            {
                var s = syllables[i];
                PlayBark(s.start, s.end, s.dur, volume);
                yield return new WaitForSeconds(s.dur * 0.85f);
            }
        }

        private void PlayBark(float startHz, float endHz, float duration, float volume)
        {
            if (_source == null)
            {
                return;
            }

            var key = (startHz, endHz, duration, volume);
            if (!_generatedBarks.TryGetValue(key, out var clip) || clip == null)
            {
                clip = MakeFormantChirp($"Voice_{startHz:0}", startHz, endHz, duration);
                _generatedBarks[key] = clip;
            }
            _source.PlayOneShot(clip, volume);
        }

        private void OnDestroy()
        {
            if (_source != null) _source.Stop();
            foreach (var clip in _generatedBarks.Values)
            {
                if (clip != null) Destroy(clip);
            }
            _generatedBarks.Clear();
        }

        private static AudioClip MakeFormantChirp(string name, float startHz, float endHz, float duration)
        {
            const int sampleRate = 22050;
            var samples = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)(samples - 1);
                var freq = Mathf.Lerp(startHz, endHz, t * t);
                // PlayBark applies the requested voice level in PlayOneShot. Keep
                // the cached waveform normalized so fallback VO isn't attenuated
                // twice (sample amplitude × one-shot volume × AudioSource volume).
                var env = Mathf.Sin(t * Mathf.PI) * 0.72f;
                var a = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate));
                var b = Mathf.Sin(2f * Mathf.PI * (freq * 1.6f) * (i / (float)sampleRate)) * 0.35f;
                data[i] = (a + b) * env;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
