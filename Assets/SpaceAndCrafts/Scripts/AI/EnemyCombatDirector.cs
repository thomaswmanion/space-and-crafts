using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Shared combat-start layer: aggro count → music intensity + CombatStarted/Ended.
    /// </summary>
    public sealed class EnemyCombatDirector : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("EnemyCombatDirector.Update");
        [SerializeField] private float releaseHoldSeconds = 1.4f;

        private int _aggroCount;
        private float _releaseAt = -1f;
        private bool _inCombat;
        private WorldMusicDirector _music;

        public static EnemyCombatDirector Ensure()
        {
            var existing = FindAnyObjectByType<EnemyCombatDirector>();
            if (existing != null)
            {
                return existing;
            }

            var host = GameObject.Find("ArrivalSystems")
                ?? GameObject.Find("WorldSystems")
                ?? new GameObject("SYS_Combat");
            return host.GetComponent<EnemyCombatDirector>() ?? host.AddComponent<EnemyCombatDirector>();
        }

        private void Awake()
        {
            _music = FindAnyObjectByType<WorldMusicDirector>();
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            if (_inCombat && _aggroCount <= 0 && _releaseAt > 0f && Time.time >= _releaseAt)
            {
                EndCombat();
            }
        }

        public void NotifyAggroEnter(Object source)
        {
            _aggroCount++;
            _releaseAt = -1f;
            if (!_inCombat)
            {
                _inCombat = true;
                GameEvents.RaiseCombatStarted();
            }

            PushMusic(Mathf.Clamp01(0.45f + _aggroCount * 0.18f));
        }

        public void NotifyAggroExit(Object source)
        {
            _aggroCount = Mathf.Max(0, _aggroCount - 1);
            if (_aggroCount <= 0)
            {
                _releaseAt = Time.time + releaseHoldSeconds;
                PushMusic(0.2f);
            }
            else
            {
                PushMusic(Mathf.Clamp01(0.45f + _aggroCount * 0.18f));
            }
        }

        public void NotifyEnemyDefeated(Object source)
        {
            if (_aggroCount > 0)
            {
                NotifyAggroExit(source);
            }
        }

        private void EndCombat()
        {
            _inCombat = false;
            _releaseAt = -1f;
            PushMusic(0f);
            GameEvents.RaiseCombatEnded();
        }

        private void PushMusic(float intensity)
        {
            if (_music == null)
            {
                _music = FindAnyObjectByType<WorldMusicDirector>();
            }

            _music?.SetCombatIntensity(intensity);
        }
    }
}
