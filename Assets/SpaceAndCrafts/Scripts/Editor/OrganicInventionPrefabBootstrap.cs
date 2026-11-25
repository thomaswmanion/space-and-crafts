#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Bakes organic invention visual prefabs for worlds 1–10 (replaceable by commissioned finals).
    /// </summary>
    public static class OrganicInventionPrefabBootstrap
    {
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/Inventions";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        private static readonly (string id, string file, System.Action<Transform, System.Action<GameObject, string, Color, float, float>> build)[] Jobs =
        {
            ("boing_o_matic", "BoingOMatic_Organic.prefab", OrganicBoingBuilder.Build),
            ("vine_spooler", "VineSpooler_Organic.prefab", OrganicInventionBuilders.BuildVineSpooler),
            ("junk_magnet_gauntlet", "JunkMagnetGauntlet_Organic.prefab", OrganicInventionBuilders.BuildJunkMagnetGauntlet),
            ("paint_bubble_raft", "PaintBubbleRaft_Organic.prefab", OrganicInventionBuilders.BuildPaintBubbleRaft),
            ("windup_bridge_bug", "WindUpBridgeBug_Organic.prefab", OrganicInventionBuilders.BuildWindUpBridgeBug),
            ("heat_tape_torch", "HeatTapeTorch_Organic.prefab", OrganicInventionBuilders.BuildHeatTapeTorch),
            ("boom_dish_redirector", "BoomDishRedirector_Organic.prefab", OrganicInventionBuilders.BuildBoomDishRedirector),
            ("vacuum_skimmer", "VacuumSkimmer_Organic.prefab", OrganicInventionBuilders.BuildVacuumSkimmer),
            ("star_seed_planter", "StarSeedPlanter_Organic.prefab", OrganicInventionBuilders.BuildStarSeedPlanter),
            ("glass_sail_shield", "GlassSailShield_Organic.prefab", OrganicInventionBuilders.BuildGlassSailShield),
        };

        [MenuItem("Space & Crafts/Bake Organic Invention Prefabs")]
        public static void Bootstrap()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MatPath);
            var baked = 0;
            for (var i = 0; i < Jobs.Length; i++)
            {
                var job = Jobs[i];
                var root = new GameObject($"ART_{job.id}");
                try
                {
                    job.build(root.transform, ApplyMat);
                    var idle = root.AddComponent<SpaceAndCrafts.Gameplay.InventionIdlePresenter>();
                    idle.Configure(job.id);
                    var path = $"{PrefabFolder}/{job.file}";
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    baked++;
                    Debug.Log($"[Space & Crafts] Baked invention prefab: {path}");
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Organic invention prefabs baked ({baked}/{Jobs.Length}).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
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
