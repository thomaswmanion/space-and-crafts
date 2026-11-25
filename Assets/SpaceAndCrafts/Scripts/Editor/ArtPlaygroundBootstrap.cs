#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// TEMP ART material language board: cosmic + handmade samples for Arrival art direction.
    /// </summary>
    public static class ArtPlaygroundBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Development/ArtPlayground.unity";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap ArtPlayground (Material Language)")]
        public static void Bootstrap()
        {
            PlaygroundSceneBootstrap.Bootstrap();
            EnsureFolder(MatPath);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoots();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.75f, 0.8f, 0.9f);
            RenderSettings.fogDensity = 0.01f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(1f, 0.85f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.4f, 0.4f);

            var world = new GameObject("TEMP_ART_ArtWorld");
            var key = new GameObject("TEMP_ART_KeyLight");
            key.transform.SetParent(world.transform, false);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "TEMP_ART_GalleryFloor";
            floor.transform.SetParent(world.transform, false);
            floor.transform.localScale = new Vector3(20f, 0.15f, 20f);
            ApplyMat(floor, "TEMP_Mat_GalleryFloor", new Color(0.35f, 0.38f, 0.45f), 0.2f, 0.05f);

            var samples = new (string name, Color color, float smooth, float metal, PrimitiveType shape)[]
            {
                ("Cardboard", new Color(0.78f, 0.58f, 0.32f), 0.15f, 0.0f, PrimitiveType.Cube),
                ("Foil", new Color(0.85f, 0.88f, 0.95f), 0.9f, 0.95f, PrimitiveType.Cylinder),
                ("PaintSplash", new Color(1f, 0.35f, 0.55f), 0.55f, 0.05f, PrimitiveType.Sphere),
                ("TranslucentPlastic", new Color(0.45f, 0.85f, 1f), 0.85f, 0.1f, PrimitiveType.Cube),
                ("Clay", new Color(0.72f, 0.45f, 0.85f), 0.25f, 0.0f, PrimitiveType.Sphere),
                ("Tape", new Color(1f, 0.86f, 0.3f), 0.45f, 0.05f, PrimitiveType.Cylinder),
                ("BoltMetal", new Color(0.55f, 0.58f, 0.62f), 0.7f, 0.85f, PrimitiveType.Capsule),
                ("CosmicGlass", new Color(0.55f, 0.35f, 0.95f), 0.95f, 0.2f, PrimitiveType.Sphere),
            };

            for (var i = 0; i < samples.Length; i++)
            {
                var s = samples[i];
                var x = (i % 4) * 3.2f - 4.8f;
                var z = (i / 4) * 3.5f - 1.5f;

                var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pedestal.name = $"TEMP_ART_Pedestal_{s.name}";
                pedestal.transform.SetParent(world.transform, false);
                pedestal.transform.position = new Vector3(x, 0.35f, z);
                pedestal.transform.localScale = new Vector3(1.1f, 0.35f, 1.1f);
                ApplyMat(pedestal, "TEMP_Mat_Pedestal", new Color(0.25f, 0.28f, 0.32f), 0.3f, 0.1f);

                var sample = GameObject.CreatePrimitive(s.shape);
                sample.name = $"TEMP_ART_Sample_{s.name}";
                sample.transform.SetParent(world.transform, false);
                sample.transform.position = new Vector3(x, 1.15f, z);
                sample.transform.localScale = Vector3.one * 0.9f;
                Object.DestroyImmediate(sample.GetComponent<Collider>());
                ApplyMat(sample, $"TEMP_Mat_Lang_{s.name}", s.color, s.smooth, s.metal);

                var label = new GameObject($"TEMP_ART_LABEL_{s.name}");
                label.transform.SetParent(world.transform, false);
                label.transform.position = new Vector3(x, 1.9f, z);
            }

            var cam = new GameObject("ArtCamera");
            cam.transform.position = new Vector3(0f, 3.2f, -9f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            var camera = cam.AddComponent<UnityEngine.Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.62f, 0.78f);
            cam.AddComponent<AudioListener>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] ArtPlayground material language board ready (TEMP ART samples).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ClearRoots()
        {
            foreach (var name in new[] { "TEMP_ART_ArtWorld", "ArtCamera", "Main Camera", "Directional Light" })
            {
                var existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
