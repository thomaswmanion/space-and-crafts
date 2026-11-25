#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Upgrades Worlds 02–10 in place with unique organic landmarks + invention meshes.
    /// </summary>
    public static class WorldsVisualUpgradeBootstrap
    {
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        private static readonly (int index, string inventionId, string[] inventionObjectNames)[] FallbackJobs =
        {
            (2, "vine_spooler", new[] { "ART_VineSpooler", "ART_vine_spooler" }),
            (3, "junk_magnet_gauntlet", new[] { "ART_JunkMagnetGauntlet", "ART_junk_magnet_gauntlet" }),
            (4, "paint_bubble_raft", new[] { "ART_PaintBubbleRaft", "ART_paint_bubble_raft" }),
            (5, "windup_bridge_bug", new[] { "ART_WindUpBridgeBug", "ART_windup_bridge_bug" }),
            (6, "heat_tape_torch", new[] { "ART_HeatTapeTorch", "ART_heat_tape_torch" }),
            (7, "boom_dish_redirector", new[] { "ART_BoomDishRedirector", "ART_boom_dish_redirector" }),
            (8, "vacuum_skimmer", new[] { "ART_VacuumSkimmer", "ART_vacuum_skimmer" }),
            (9, "star_seed_planter", new[] { "ART_StarSeedPlanter", "ART_star_seed_planter" }),
            (10, "glass_sail_shield", new[] { "ART_GlassSailShield", "ART_glass_sail_shield" }),
        };

        [MenuItem("Space & Crafts/Upgrade Worlds 02-10 Organic Visuals")]
        public static void Bootstrap()
        {
            var catalog = WorldCatalogBootstrap.SeedCatalog();
            var upgraded = 0;

            for (var worldIndex = 2; worldIndex <= 10; worldIndex++)
            {
                var def = catalog.GetByIndex(worldIndex);
                string scenePath;
                string displayName;
                string inventionId;
                string inventionName;
                Color accent;
                if (def != null && !string.IsNullOrEmpty(def.scenePath))
                {
                    scenePath = def.scenePath;
                    displayName = def.displayName;
                    inventionId = def.inventionId;
                    inventionName = def.inventionDisplayName;
                    accent = def.accentColor;
                }
                else
                {
                    var fb = FallbackJobs[worldIndex - 2];
                    scenePath = KnownScenePath(worldIndex);
                    displayName = $"World {worldIndex:00}";
                    inventionId = fb.inventionId;
                    inventionName = fb.inventionId;
                    accent = Color.gray;
                }

                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip visual upgrade — missing scene for world {worldIndex}: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                UpgradeWorldScene(worldIndex, displayName, inventionId, inventionName, accent);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                upgraded++;
                Debug.Log($"[Space & Crafts] Organic visuals upgraded: {displayName} ({OrganicLandmarkBuilders.FantasyLabel(worldIndex)})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Worlds 02–10 organic visual upgrade complete ({upgraded} scenes).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void UpgradeWorldScene(
            int worldIndex,
            string displayName,
            string inventionId,
            string inventionName,
            Color accent)
        {
            var artRoot = FindArtRoot(worldIndex) ?? new GameObject($"ART_World{worldIndex:00}");
            EnsureLandmark(artRoot.transform, worldIndex, accent);
            EnsureInventionVisual(worldIndex, inventionId, artRoot.transform);
            RelabelBanners(displayName, inventionName, worldIndex);
        }

        private static GameObject FindArtRoot(int worldIndex)
        {
            var candidates = new[]
            {
                $"ART_World{worldIndex:00}",
                "ART_World02_CorkscrewCanopy",
                $"ART_corkscrew_canopy",
                $"ART_magnet_marsh",
                $"ART_bubble_bazaar",
                $"ART_clockwork_crater",
                $"ART_frosted_filament",
                $"ART_echo_atelier",
                $"ART_tide_tinker_yard",
                $"ART_nebula_nursery",
                $"ART_scrap_cathedral"
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var go = GameObject.Find(candidates[i]);
                if (go != null)
                {
                    return go;
                }
            }

            // Any ART_* root in scene hierarchy
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name.StartsWith("ART_World") || roots[i].name.StartsWith("ART_corkscrew") ||
                    roots[i].name.StartsWith("ART_magnet") || roots[i].name.StartsWith("ART_bubble") ||
                    roots[i].name.StartsWith("ART_clockwork") || roots[i].name.StartsWith("ART_frosted") ||
                    roots[i].name.StartsWith("ART_echo") || roots[i].name.StartsWith("ART_tide") ||
                    roots[i].name.StartsWith("ART_nebula") || roots[i].name.StartsWith("ART_scrap"))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void EnsureLandmark(Transform artRoot, int worldIndex, Color accent)
        {
            // Remove generic single-mesh landmark leftover from scaffold.
            var old = FindDeep(artRoot, "ART_WorldLandmark");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            // World02 canopy bootstrap used free-floating vine trunks; fold into landmark root.
            if (worldIndex == 2)
            {
                RemoveNamedChildren(artRoot, "ART_VineTrunk_");
                RemoveNamedChildren(artRoot, "ART_CanopyCrown");
            }

            var landmark = new GameObject("ART_WorldLandmark");
            landmark.transform.SetParent(artRoot, false);
            landmark.transform.localPosition = new Vector3(0f, 0.2f, 10f);
            OrganicLandmarkBuilders.BuildForWorld(worldIndex, landmark.transform, accent, ApplyMat);
        }

        private static void EnsureInventionVisual(int worldIndex, string inventionId, Transform artRoot)
        {
            var names = InventionObjectNames(worldIndex, inventionId);
            GameObject invention = null;
            for (var i = 0; i < names.Length; i++)
            {
                invention = GameObject.Find(names[i]);
                if (invention != null)
                {
                    break;
                }
            }

            if (invention == null)
            {
                invention = FindDeep(artRoot, names[0]);
            }

            if (invention == null)
            {
                var site = GameObject.Find("ART_BuildSite") ?? GameObject.Find("ART_VineBuildSite");
                invention = new GameObject(names[0]);
                if (site != null)
                {
                    invention.transform.SetParent(site.transform, false);
                    invention.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                }
                else
                {
                    invention.transform.SetParent(artRoot, false);
                    invention.transform.localPosition = new Vector3(0f, 1f, 4f);
                }
            }

            invention.name = names[0];
            var wasActive = invention.activeSelf;
            invention.SetActive(true);
            OrganicInventionBuilders.BuildForWorld(worldIndex, invention.transform, ApplyMat);
            invention.SetActive(wasActive);
        }

        private static string[] InventionObjectNames(int worldIndex, string inventionId)
        {
            for (var i = 0; i < FallbackJobs.Length; i++)
            {
                if (FallbackJobs[i].index == worldIndex)
                {
                    return FallbackJobs[i].inventionObjectNames;
                }
            }

            return new[] { $"ART_{inventionId}" };
        }

        private static void RelabelBanners(string displayName, string inventionName, int worldIndex)
        {
            var fantasy = OrganicLandmarkBuilders.FantasyLabel(worldIndex);
            var label =
                $"TEMP ART — {displayName} organic upgrade ({fantasy} + {inventionName}); replaceable by commissioned finals";

            var banners = Object.FindObjectsByType<TempArtBanner>(FindObjectsInactive.Include);
            if (banners != null && banners.Length > 0)
            {
                for (var i = 0; i < banners.Length; i++)
                {
                    banners[i].SetLabel(label);
                    if (!banners[i].gameObject.name.Contains("TEMP_ART_BANNER"))
                    {
                        banners[i].gameObject.name = "TEMP_ART_BANNER";
                    }
                }

                return;
            }

            var bannerGo = GameObject.Find("TEMP_ART_BANNER") ?? GameObject.Find($"TEMP_ART_BANNER_WORLD{worldIndex:00}");
            if (bannerGo == null)
            {
                bannerGo = new GameObject("TEMP_ART_BANNER");
                bannerGo.transform.position = new Vector3(0f, 3f, -4f);
            }
            else
            {
                bannerGo.name = "TEMP_ART_BANNER";
            }

            var banner = bannerGo.GetComponent<TempArtBanner>() ?? bannerGo.AddComponent<TempArtBanner>();
            banner.SetLabel(label);
        }

        private static void RemoveNamedChildren(Transform parent, string namePrefix)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(namePrefix))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static GameObject FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root.gameObject;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string KnownScenePath(int worldIndex)
        {
            switch (worldIndex)
            {
                case 2: return "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
                case 3: return "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";
                case 4: return "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity";
                case 5: return "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity";
                case 6: return "Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity";
                case 7: return "Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity";
                case 8: return "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity";
                case 9: return "Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity";
                case 10: return "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity";
                default: return null;
            }
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            EnsureFolder(MatPath);
            var path = $"{MatPath}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            EditorUtility.SetDirty(mat);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
