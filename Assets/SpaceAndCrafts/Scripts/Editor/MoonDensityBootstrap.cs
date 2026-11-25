#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Places extra SecretWonder beacons across worlds 02–10 for curiosity density.
    /// </summary>
    public static class MoonDensityBootstrap
    {
        private static readonly string[] Scenes =
        {
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

        [MenuItem("Space & Crafts/Bootstrap Moon Density (Worlds 02-10)")]
        public static void Bootstrap()
        {
            for (var w = 0; w < Scenes.Length; w++)
            {
                var path = Scenes[w];
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip moon density — missing {path}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var packName = "ART_MoonDensityPack";
                var old = GameObject.Find(packName);
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                }

                var pack = new GameObject(packName);
                for (var i = 0; i < 3; i++)
                {
                    var ang = (i / 3f) * Mathf.PI * 2f;
                    var pos = new Vector3(Mathf.Cos(ang) * (5f + i), 1.2f, 8f + Mathf.Sin(ang) * 4f);
                    var go = OrganicMeshFactory.CreateMeshObject(
                        $"ART_Wonder_w{w + 2}_{i}",
                        OrganicMeshFactory.FacetedStar(0.32f, 0.12f, 0.08f, 5),
                        pack.transform,
                        pos,
                        Vector3.one * 0.75f,
                        null);
                    ApplyMat(go, $"Mat_MoonWonder_{w}_{i}", Color.HSVToRGB((w * 0.08f + i * 0.05f) % 1f, 0.55f, 1f), 0.55f, 0.15f);
                    var beacon = go.AddComponent<SecretWonderBeacon>();
                    beacon.Configure($"wonder_w{w + 2}_{i}", $"World {w + 2:00} hush #{i + 1}!");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Space & Crafts] Moon density: world {w + 2:00} +3 wonders");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Moon density bootstrapped across worlds 02–10.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            const string matFolder = "Assets/SpaceAndCrafts/Materials/TempArt";
            if (!AssetDatabase.IsValidFolder(matFolder))
            {
                AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Materials", "TempArt");
            }

            var path = $"{matFolder}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}
#endif
