#if UNITY_EDITOR
using System.IO;
using SpaceAndCrafts.Core;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Seeds WorldDefinition assets + WorldCatalog for the first ten worlds.
    /// </summary>
    public static class WorldCatalogBootstrap
    {
        private const string Folder = "Assets/SpaceAndCrafts/ScriptableObjects/Worlds";
        private const string CatalogPath = Folder + "/WorldCatalog.asset";

        [MenuItem("Space & Crafts/Bootstrap Ten-World Catalog")]
        public static void Bootstrap()
        {
            SeedCatalog();
            Debug.Log("[Space & Crafts] WorldCatalog seeded with 10 worlds.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Seed without exiting batchmode (safe to call from other bootstraps).</summary>
        public static WorldCatalog SeedCatalog()
        {
            EnsureFolder(Folder);
            var catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorldCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.worlds.Clear();
            catalog.worlds.Add(Upsert(1, "rainbow_arrival", "Rainbow Kingdom — The Arrival",
                "Cosmic handmade shelf where junk becomes joy.",
                "boing_o_matic", "Boing-O-Matic",
                "Spring + dish + crystal launch invention.",
                "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity",
                new Color(0.72f, 0.45f, 0.95f), systemsPlayable: true, visualsMeetBar: false));
            catalog.worlds.Add(Upsert(2, "corkscrew_canopy", "Beeping Caves Kingdom — Chime Hollows",
                "Resonant crystal caves answer with bright two-note beeps; winding chambers curl around safe ledges.",
                "vine_spooler", "Pingline Spooler",
                "A tuning-fork spool pings resonant anchors, then reels Splat toward a safe ledge.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity",
                new Color(0.40f, 0.52f, 0.92f), false, false));
            catalog.worlds.Add(Upsert(3, "magnet_marsh", "Leaf Lands — Magnet Marsh",
                "A leafy foil wetland where magnetic tides gather useful craft scraps.",
                "junk_magnet_gauntlet", "Junk Magnet Gauntlet",
                "Magnetic glove that yanks junk and cores.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity",
                new Color(0.45f, 0.7f, 0.95f), false, false));
            catalog.worlds.Add(Upsert(4, "bubble_bazaar", "Bubble Bazaar",
                "Paint-market islands drifting on soap-film roads.",
                "paint_bubble_raft", "Paint Bubble Raft",
                "Rideable paint bubble for soft flight lanes.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity",
                new Color(1f, 0.45f, 0.75f), false, false));
            catalog.worlds.Add(Upsert(5, "clockwork_crater", "Clockwork Crater",
                "Toy astronomy gears jammed in a crater workshop.",
                "windup_bridge_bug", "Wind-Up Bridge Bug",
                "Clockwork bug that unfolds into bridges.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity",
                new Color(0.95f, 0.7f, 0.3f), false, false));
            catalog.worlds.Add(Upsert(6, "frosted_filament", "Mountain Lands — Frosted Filament",
                "High in the Mountain Lands, icy craft threads stretch between frozen looms.",
                "heat_tape_torch", "Heat-Tape Torch",
                "Warm tape beam that melts freeze locks.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity",
                new Color(0.65f, 0.9f, 1f), false, false));
            catalog.worlds.Add(Upsert(7, "echo_atelier", "Echo Atelier",
                "A studio where sound shapes solid platforms.",
                "boom_dish_redirector", "Boom Dish Redirector",
                "Redirect sonic blasts into resonators.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity",
                new Color(0.85f, 0.55f, 1f), false, false));
            catalog.worlds.Add(Upsert(8, "tide_tinker_yard", "Tide Tinker Yard",
                "Ship-breaking shallows full of useful wreckage.",
                "vacuum_skimmer", "Vacuum Skimmer",
                "Suck debris, spit it as traversal ammo.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity",
                new Color(0.3f, 0.75f, 0.85f), false, false));
            catalog.worlds.Add(Upsert(9, "nebula_nursery", "Nebula Nursery",
                "Soft star-garden where seeds grow platforms.",
                "star_seed_planter", "Star Seed Planter",
                "Plant glowing seeds that sprout temporary paths.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity",
                new Color(0.55f, 0.45f, 1f), false, false));
            catalog.worlds.Add(Upsert(10, "scrap_cathedral", "Scrap Cathedral",
                "Stained-glass junk temple lit by rainbow beams.",
                "glass_sail_shield", "Glass Sail Shield",
                "Parry and redirect light into crystal hearts.",
                "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity",
                new Color(1f, 0.85f, 0.45f), false, false));

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return catalog;
        }

        private static WorldDefinition Upsert(
            int index,
            string id,
            string display,
            string fantasy,
            string inventionId,
            string inventionName,
            string inventionBlurb,
            string scenePath,
            Color accent,
            bool systemsPlayable,
            bool visualsMeetBar)
        {
            var path = $"{Folder}/World_{index:00}_{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<WorldDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<WorldDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.worldIndex = index;
            def.worldId = id;
            def.displayName = display;
            def.fantasyBlurb = fantasy;
            def.inventionId = inventionId;
            def.inventionDisplayName = inventionName;
            def.inventionBlurb = inventionBlurb;
            def.scenePath = scenePath;
            def.accentColor = accent;
            def.systemsPlayable = systemsPlayable;
            def.visualsMeetBar = visualsMeetBar;
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
