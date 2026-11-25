using System.Collections.Generic;
using SpaceAndCrafts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Session registry for claimed secret wonders — feeds quests and curiosity payoffs.
    /// </summary>
    public sealed class SecretWonderRegistry : MonoBehaviour
    {
        private readonly HashSet<string> _claimed = new();

        public int ClaimedCount => _claimed.Count;
        public IReadOnlyCollection<string> ClaimedIds => _claimed;

        public static SecretWonderRegistry Ensure()
        {
            var existing = FindAnyObjectByType<SecretWonderRegistry>();
            if (existing != null)
            {
                existing.RestoreClaimedFromSave();
                return existing;
            }

            var host = GameObject.Find("ArrivalSystems")
                ?? GameObject.Find("WorldSystems")
                ?? new GameObject("SYS_Secrets");
            var registry = host.GetComponent<SecretWonderRegistry>() ?? host.AddComponent<SecretWonderRegistry>();
            registry.RestoreClaimedFromSave();
            return registry;
        }

        private void OnEnable()
        {
            GameEvents.LevelRestartRequested += HandleRestart;
            RestoreClaimedFromSave();
        }

        private void OnDisable()
        {
            GameEvents.LevelRestartRequested -= HandleRestart;
        }

        public bool TryClaim(string wonderId)
        {
            if (string.IsNullOrWhiteSpace(wonderId) || _claimed.Contains(wonderId))
            {
                return false;
            }

            _claimed.Add(wonderId);
            GameEvents.RaiseSecretWonderClaimed(wonderId);
            return true;
        }

        public bool HasClaimed(string wonderId) => _claimed.Contains(wonderId);

        private void RestoreClaimedFromSave()
        {
            if (!PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                return;
            }

            var savedIds = PlayerProgressSave.Current.claimedWonderIds;
            if (savedIds == null || savedIds.Count == 0)
            {
                return;
            }

            // The save stores wonders across worlds. Restore only IDs represented
            // by beacons in this active scene so one world's discoveries cannot
            // pre-complete another world's optional quest.
            var scene = SceneManager.GetActiveScene();
            var beacons = FindObjectsByType<SecretWonderBeacon>();
            for (var i = 0; i < beacons.Length; i++)
            {
                var beacon = beacons[i];
                if (beacon == null || beacon.gameObject.scene != scene)
                {
                    continue;
                }

                if (savedIds.Contains(beacon.WonderId))
                {
                    _claimed.Add(beacon.WonderId);
                }
            }
        }

        private void HandleRestart()
        {
            _claimed.Clear();
        }
    }
}
