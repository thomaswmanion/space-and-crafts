#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Bakes organic ScrapMite + ScrapStorm visual prefabs (replaceable by commissioned finals).
    /// </summary>
    public static class OrganicEnemyPrefabBootstrap
    {
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/Enemies";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bake Organic Enemy Prefabs")]
        public static void Bootstrap()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MatPath);

            Bake("ScrapMite_Organic.prefab", root => OrganicScrapMiteBuilder.Build(root, ApplyMat));
            Bake("ScrapFloater_Organic.prefab", root => OrganicScrapFloaterBuilder.Build(root, ApplyMat));
            Bake("ScrapRoller_Organic.prefab", root => OrganicScrapRollerBuilder.Build(root, ApplyMat));
            Bake("ScrapStormSentinel_Organic.prefab", root =>
            {
                OrganicScrapStormBuilder.Build(root, out _, out _, out _, ApplyMat);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Organic enemy prefabs baked (Mite + Floater + Roller + Storm).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Bake(string fileName, System.Action<Transform> build)
        {
            var root = new GameObject(fileName.Replace(".prefab", ""));
            try
            {
                build(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{fileName}");
                Debug.Log($"[Space & Crafts] Baked enemy prefab: {PrefabFolder}/{fileName}");
            }
            finally
            {
                Object.DestroyImmediate(root);
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
