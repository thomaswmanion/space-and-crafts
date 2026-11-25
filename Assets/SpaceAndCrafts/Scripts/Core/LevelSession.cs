using System;
using System.Collections.Generic;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Thin per-level progress holder. Intentionally serializable-friendly later.
    /// Not a god GameManager — Arrival can host one of these in the scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private List<string> acquiredCollectibleIds = new();
        [SerializeField] private List<string> completedInventionIds = new();
        [SerializeField] private bool levelCompleted;

        public IReadOnlyList<string> AcquiredCollectibleIds => acquiredCollectibleIds;
        public IReadOnlyList<string> CompletedInventionIds => completedInventionIds;
        public bool LevelCompleted => levelCompleted;

        public event Action SessionChanged;

        private void Awake()
        {
            if (!PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                return;
            }

            var progress = PlayerProgressSave.Current;
            AddUnique(acquiredCollectibleIds, progress.acquiredComponentIds);
            AddUnique(acquiredCollectibleIds, progress.discoveryBadgeIds);
            AddUnique(completedInventionIds, progress.unlockedInventionIds);
        }

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += HandleCollectibleAcquired;
            GameEvents.BuildCompleted += HandleBuildCompleted;
            GameEvents.LevelCompleted += HandleLevelCompleted;
            GameEvents.LevelRestartRequested += HandleRestartRequested;
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= HandleCollectibleAcquired;
            GameEvents.BuildCompleted -= HandleBuildCompleted;
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            GameEvents.LevelRestartRequested -= HandleRestartRequested;
        }

        public bool HasCollectible(string id) => acquiredCollectibleIds.Contains(id);

        public bool HasInvention(string id)
        {
            for (var i = 0; i < completedInventionIds.Count; i++)
            {
                if (InventionCompatibility.Satisfies(id, completedInventionIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleCollectibleAcquired(CollectibleDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return;
            }

            if (acquiredCollectibleIds.Contains(definition.Id))
            {
                return;
            }

            acquiredCollectibleIds.Add(definition.Id);
            SessionChanged?.Invoke();
        }

        private void HandleBuildCompleted(string inventionId)
        {
            if (string.IsNullOrWhiteSpace(inventionId) || completedInventionIds.Contains(inventionId))
            {
                return;
            }

            completedInventionIds.Add(inventionId);
            SessionChanged?.Invoke();
        }

        private void HandleLevelCompleted()
        {
            levelCompleted = true;
            SessionChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            acquiredCollectibleIds.Clear();
            completedInventionIds.Clear();
            levelCompleted = false;
            SessionChanged?.Invoke();
        }

        private static void AddUnique(List<string> destination, IEnumerable<string> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var id in source)
            {
                if (!string.IsNullOrWhiteSpace(id) && !destination.Contains(id))
                {
                    destination.Add(id);
                }
            }
        }
    }
}
