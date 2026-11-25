#if UNITY_EDITOR
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class CollectibleNamingValidateMenu
    {
        [MenuItem("Space & Crafts/Validate Collectible Naming Helpers")]
        public static void Validate()
        {
            Debug.Assert(CollectibleNaming.PrettyId("component_spring") == "spring");
            Debug.Assert(CollectibleNaming.PrettyId("material_rainbow_goo") == "rainbow goo");
            var owned = new System.Collections.Generic.HashSet<string> { "component_spring" };
            var msg = CollectibleNaming.BuildMissingMessage(
                new[] { "component_spring", "component_metal_dish" },
                owned.Contains);
            Debug.Assert(msg.Contains("metal dish"));
            Debug.Assert(!msg.Contains("spring"));
            Debug.Log("[Space & Crafts] CollectibleNaming helpers OK.");
        }
    }
}
#endif
