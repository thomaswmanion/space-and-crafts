#if UNITY_EDITOR
using System.Text;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Batchable readiness check for Worlds 1–10: Splat, organic mesh presence, invention/build site, boss.
    /// </summary>
    public static class TenWorldsReadinessValidator
    {
        private const string CatalogPath = "Assets/SpaceAndCrafts/ScriptableObjects/Worlds/WorldCatalog.asset";

        private static readonly string[] KnownScenePaths =
        {
            "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity",
        };

        private static readonly string[][] InventionNameHints =
        {
            new[] { "ART_Boing", "Boing" },
            new[] { "ART_VineSpooler", "ART_vine_spooler" },
            new[] { "ART_JunkMagnetGauntlet", "ART_junk_magnet_gauntlet" },
            new[] { "ART_PaintBubbleRaft", "ART_paint_bubble_raft" },
            new[] { "ART_WindUpBridgeBug", "ART_windup_bridge_bug" },
            new[] { "ART_HeatTapeTorch", "ART_heat_tape_torch" },
            new[] { "ART_BoomDishRedirector", "ART_boom_dish_redirector" },
            new[] { "ART_VacuumSkimmer", "ART_vacuum_skimmer" },
            new[] { "ART_StarSeedPlanter", "ART_star_seed_planter" },
            new[] { "ART_GlassSailShield", "ART_glass_sail_shield" },
        };

        [MenuItem("Space & Crafts/Validate Ten Worlds Readiness")]
        public static void ValidateMenu()
        {
            var ok = ValidateAll(out var report);
            if (ok)
            {
                Debug.Log("[Space & Crafts] Ten Worlds Readiness PASS\n" + report);
            }
            else
            {
                Debug.LogError("[Space & Crafts] Ten Worlds Readiness FAIL\n" + report);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        public static bool ValidateAll(out string report)
        {
            var sb = new StringBuilder();
            var allPass = true;
            var catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);

            for (var worldIndex = 1; worldIndex <= 10; worldIndex++)
            {
                var scenePath = ResolveScenePath(catalog, worldIndex);
                var worldPass = ValidateWorld(worldIndex, scenePath, out var worldReport);
                sb.AppendLine(worldPass
                    ? $"PASS World {worldIndex:00}: {worldReport}"
                    : $"FAIL World {worldIndex:00}: {worldReport}");
                allPass &= worldPass;
            }

            report = sb.ToString();
            return allPass;
        }

        private static string ResolveScenePath(WorldCatalog catalog, int worldIndex)
        {
            if (catalog != null)
            {
                var def = catalog.GetByIndex(worldIndex);
                if (def != null && !string.IsNullOrEmpty(def.scenePath))
                {
                    return def.scenePath;
                }
            }

            var i = worldIndex - 1;
            return i >= 0 && i < KnownScenePaths.Length ? KnownScenePaths[i] : null;
        }

        private static bool ValidateWorld(int worldIndex, string scenePath, out string detail)
        {
            var issues = new StringBuilder();
            if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
            {
                detail = $"missing scene ({scenePath ?? "null"})";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                detail = "could not open scene";
                return false;
            }

            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                issues.Append("missing Splat_TEMP; ");
            }

            if (!HasOrganicMeshPresence())
            {
                issues.Append("no Organic_/ART_ MeshFilter; ");
            }

            if (worldIndex >= 2)
            {
                if (!HasInventionOrBuildSite(worldIndex))
                {
                    issues.Append("missing invention or BuildSite; ");
                }
            }

            if (worldIndex == 1)
            {
                var boss = GameObject.Find("ART_WorldBossExam")
                           ?? GameObject.Find("ART_ScrapStormSentinel")
                           ?? FindByNameContains("ScrapStorm");
                if (boss == null)
                {
                    issues.Append("missing ART_WorldBossExam or ScrapStorm; ");
                }
            }

            if (issues.Length == 0)
            {
                detail = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                return true;
            }

            detail = issues.ToString().TrimEnd(' ', ';');
            return false;
        }

        private static bool HasOrganicMeshPresence()
        {
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
            for (var i = 0; i < filters.Length; i++)
            {
                var mf = filters[i];
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }

                if (mf.sharedMesh.name.StartsWith("Organic_") || mf.gameObject.name.StartsWith("ART_"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasInventionOrBuildSite(int worldIndex)
        {
            if (FindByExactName("ART_BuildSite") != null || FindByExactName("ART_VineBuildSite") != null)
            {
                return true;
            }

            if (Object.FindAnyObjectByType<BuildSite>(FindObjectsInactive.Include) != null)
            {
                return true;
            }

            var hintIndex = worldIndex - 1;
            if (hintIndex >= 0 && hintIndex < InventionNameHints.Length)
            {
                var hints = InventionNameHints[hintIndex];
                for (var h = 0; h < hints.Length; h++)
                {
                    if (FindByExactName(hints[h]) != null || FindByNameContains(hints[h]) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static GameObject FindByExactName(string name)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == name)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static GameObject FindByNameContains(string fragment)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name.Contains(fragment))
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }
    }
}
#endif
