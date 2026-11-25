using System;
using System.Collections.Generic;
using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Session inventory on the player (or LevelSession bridge).
    /// </summary>
    public sealed class InventoryComponent : MonoBehaviour
    {
        private readonly HashSet<string> _owned = new();

        public event Action<CollectibleDefinition> ItemAdded;

        public IReadOnlyCollection<string> OwnedIds => _owned;

        private void Awake()
        {
            if (!PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                return;
            }

            var progress = PlayerProgressSave.Current;
            AddSaved(progress.acquiredComponentIds);
            AddSaved(progress.discoveryBadgeIds);
        }

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += HandleAcquired;
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= HandleAcquired;
        }

        public bool Has(string id) => !string.IsNullOrWhiteSpace(id) && _owned.Contains(id);

        public bool HasAll(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                if (!Has(id))
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleAcquired(CollectibleDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return;
            }

            if (!_owned.Add(definition.Id))
            {
                return;
            }

            ItemAdded?.Invoke(definition);
        }

        private void AddSaved(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _owned.Add(id);
                }
            }
        }
    }
}
