#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds URP Lit materials from Meshy PBR texture folders and assigns them to commissioned FBX + live scene instances.
    /// </summary>
    public static class MeshyUrpMaterialFix
    {
        private const string Commissioned = "Assets/SpaceAndCrafts/Art/Commissioned";
        private const string ArrivalScene = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";

        private static readonly (string id, string folder)[] Assets =
        {
            ("Splat_Hero", "Hero"),
            ("BoingOMatic", "Inventions"),
            ("ScrapMite", "Enemies"),
            ("ScrapFloater", "Enemies"),
            ("ScrapRoller", "Enemies"),
            ("ScrapStormSentinel", "Enemies"),
            ("RainbowGateway", "Landmarks"),
            ("RainbowCraftTree", "Environments"),
            ("ScrapRockCluster", "Environments"),
            ("PowerCrystalPickup", "Inventions"),
        };

        [MenuItem("Space & Crafts/Fix Meshy URP Materials")]
        public static void Fix()
        {
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[Space & Crafts] No URP Lit / Standard shader found.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            var fixedCount = 0;
            foreach (var (id, folder) in Assets)
            {
                var mat = BuildMaterial(id, folder, shader);
                if (mat == null)
                {
                    Debug.LogWarning($"[Space & Crafts] Missing Meshy textures for {folder}/{id}");
                    continue;
                }

                ApplyToModelAsset($"{Commissioned}/{folder}/{id}.fbx", mat);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-apply on whatever is already in Arrival (showcase + swaps).
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            foreach (var (id, folder) in Assets)
            {
                var matPath = $"{Commissioned}/{folder}/{id}/{id}_URP.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    continue;
                }

                ApplyToNamedRoots(id, mat);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Meshy URP materials fixed: {fixedCount}/{Assets.Length}.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static Material BuildMaterial(string id, string folder, Shader shader)
        {
            var texDir = $"{Commissioned}/{folder}/{id}";
            var basePath = $"{texDir}/base_color.png";
            if (!File.Exists(basePath))
            {
                return null;
            }

            ConfigureTextureImporter($"{texDir}/base_color.png", srgb: true, normal: false);
            ConfigureTextureImporter($"{texDir}/normal.png", srgb: false, normal: true);
            ConfigureTextureImporter($"{texDir}/metallic.png", srgb: false, normal: false);
            ConfigureTextureImporter($"{texDir}/roughness.png", srgb: false, normal: false);
            AssetDatabase.ImportAsset($"{texDir}/base_color.png", ImportAssetOptions.ForceUpdate);
            if (File.Exists($"{texDir}/normal.png"))
            {
                AssetDatabase.ImportAsset($"{texDir}/normal.png", ImportAssetOptions.ForceUpdate);
            }

            var maskPath = $"{texDir}/metallic_smoothness.png";
            BuildMetallicSmoothnessMask($"{texDir}/metallic.png", $"{texDir}/roughness.png", maskPath);
            ConfigureTextureImporter(maskPath, srgb: false, normal: false);
            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);

            var matPath = $"{texDir}/{id}_URP.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = $"{id}_URP" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/base_color.png");
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/normal.png");
            var maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", baseMap);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", baseMap);
            }

            if (normalMap != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalMap);
                }

                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale"))
                {
                    mat.SetFloat("_BumpScale", 1f);
                }
            }

            if (maskMap != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", maskMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 1f);
                }

                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 1f);
                }
            }
            else
            {
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 0.15f);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0.55f);
                }
            }

            // Stylized readability: slightly brighter base.
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureTextureImporter(string assetPath, bool srgb, bool normal)
        {
            if (!File.Exists(assetPath))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                // First import may not have created meta yet.
                AssetDatabase.ImportAsset(assetPath);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }

            if (importer == null)
            {
                return;
            }

            importer.sRGBTexture = srgb;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static void BuildMetallicSmoothnessMask(string metallicPath, string roughnessPath, string outPath)
        {
            Texture2D metallic = null;
            Texture2D roughness = null;
            if (File.Exists(metallicPath))
            {
                metallic = LoadPngUncompressed(metallicPath);
            }

            if (File.Exists(roughnessPath))
            {
                roughness = LoadPngUncompressed(roughnessPath);
            }

            var w = metallic != null ? metallic.width : roughness != null ? roughness.width : 4;
            var h = metallic != null ? metallic.height : roughness != null ? roughness.height : 4;
            var mask = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var m = metallic != null ? metallic.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h).r : 0.1f;
                    var r = roughness != null ? roughness.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h).r : 0.45f;
                    var smoothness = 1f - r;
                    // URP MetallicGlossMap: R=metallic, A=smoothness
                    mask.SetPixel(x, y, new Color(m, m, m, smoothness));
                }
            }

            mask.Apply();
            File.WriteAllBytes(outPath, mask.EncodeToPNG());
            Object.DestroyImmediate(mask);
            if (metallic != null)
            {
                Object.DestroyImmediate(metallic);
            }

            if (roughness != null)
            {
                Object.DestroyImmediate(roughness);
            }
        }

        private static Texture2D LoadPngUncompressed(string path)
        {
            var data = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            tex.LoadImage(data, markNonReadable: false);
            return tex;
        }

        private static void ApplyToModelAsset(string fbxPath, Material mat)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null)
            {
                return;
            }

            // Remap importer materials to our URP lit so prefab instances pick it up.
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                var map = importer.GetExternalObjectMap();
                // Force reimport then assign on instance renderers; map remaps are brittle across Meshy names.
                importer.SaveAndReimport();
            }

            // Also stamp materials onto any already-loaded mesh renderers under the model asset hierarchy
            // by creating a silent temp instance (source asset itself is not mutable).
            var temp = Object.Instantiate(model);
            try
            {
                var rends = temp.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    var shared = r.sharedMaterials;
                    for (var i = 0; i < shared.Length; i++)
                    {
                        shared[i] = mat;
                    }

                    r.sharedMaterials = shared;
                }
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }

            // Persist by writing a materials override prefab next to FBX for reliable scene use.
            var overridePath = Path.ChangeExtension(fbxPath, null) + "_URP.prefab";
            var instance = Object.Instantiate(model);
            instance.name = Path.GetFileNameWithoutExtension(fbxPath) + "_URP";
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    shared[i] = mat;
                }

                r.sharedMaterials = shared;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, overridePath);
            Object.DestroyImmediate(instance);
        }

        private static void ApplyToNamedRoots(string id, Material mat)
        {
            foreach (var go in Object.FindObjectsByType<Transform>())
            {
                if (go == null)
                {
                    continue;
                }

                var n = go.name;
                var hit = n == $"ART_Meshy_{id}"
                          || (n == "ART_MeshySplatVisual" && id == "Splat_Hero")
                          || (n == "ART_MeshyBoingVisual" && id == "BoingOMatic")
                          || (n == "ART_MeshyGatewayVisual" && id == "RainbowGateway")
                          || (n == "ART_MeshyBossVisual" && id == "ScrapStormSentinel")
                          || (n == "ART_MeshyCrystalVisual" && id == "PowerCrystalPickup");

                if (!hit && n == "ART_MeshyEnemyVisual")
                {
                    var parent = go.parent;
                    hit = parent != null && (
                        (id == "ScrapMite" && parent.GetComponent<SpaceAndCrafts.AI.ScrapMite>() != null)
                        || (id == "ScrapFloater" && parent.GetComponent<SpaceAndCrafts.AI.ScrapFloater>() != null)
                        || (id == "ScrapRoller" && parent.GetComponent<SpaceAndCrafts.AI.ScrapRoller>() != null));
                }

                if (!hit)
                {
                    continue;
                }

                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var shared = r.sharedMaterials;
                    for (var i = 0; i < shared.Length; i++)
                    {
                        shared[i] = mat;
                    }

                    r.sharedMaterials = shared;
                    EditorUtility.SetDirty(r);
                }
            }
        }
    }
}
#endif
