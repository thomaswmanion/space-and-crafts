using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Ordered catalog of the first ten worlds.
    /// </summary>
    [CreateAssetMenu(menuName = "Space & Crafts/World Catalog", fileName = "WorldCatalog")]
    public sealed class WorldCatalog : ScriptableObject
    {
        public List<WorldDefinition> worlds = new();

        public WorldDefinition GetByIndex(int index)
        {
            for (var i = 0; i < worlds.Count; i++)
            {
                if (worlds[i] != null && worlds[i].worldIndex == index)
                {
                    return worlds[i];
                }
            }

            return null;
        }

        public WorldDefinition GetById(string worldId)
        {
            for (var i = 0; i < worlds.Count; i++)
            {
                if (worlds[i] != null && worlds[i].worldId == worldId)
                {
                    return worlds[i];
                }
            }

            return null;
        }
    }
}
