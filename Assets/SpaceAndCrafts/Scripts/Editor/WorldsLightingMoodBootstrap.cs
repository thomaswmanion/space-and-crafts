#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Gives Worlds 02–10 distinct lighting moods (key + fog/sky tint) so identity reads immediately.
    /// Idempotent: finds ART_WorldLighting and rebuilds children. Does not touch Arrival.
    /// </summary>
    public static class WorldsLightingMoodBootstrap
    {
        private const string LightingRootName = "ART_WorldLighting";

        private struct Mood
        {
            public string Label;
            public Color KeyColor;
            public float KeyIntensity;
            public Vector3 KeyEuler;
            public AmbientMode AmbientMode;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public Color AmbientFlat;
            public Color FogColor;
            public float FogStart;
            public float FogEnd;
            public bool HasAccentPoint;
            public Color AccentColor;
            public float AccentIntensity;
            public float AccentRange;
            public Vector3 AccentLocalPos;
        }

        [MenuItem("Space & Crafts/Bootstrap Worlds 02-10 Lighting Mood")]
        public static void Bootstrap()
        {
            var catalog = WorldCatalogBootstrap.SeedCatalog();
            var upgraded = 0;

            for (var worldIndex = 2; worldIndex <= 10; worldIndex++)
            {
                var def = catalog.GetByIndex(worldIndex);
                string scenePath;
                string displayName;
                if (def != null && !string.IsNullOrEmpty(def.scenePath))
                {
                    scenePath = def.scenePath;
                    displayName = def.displayName;
                }
                else
                {
                    scenePath = KnownScenePath(worldIndex);
                    displayName = $"World {worldIndex:00}";
                }

                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip lighting mood — missing scene for world {worldIndex}: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ApplyLightingMood(worldIndex);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                upgraded++;
                Debug.Log($"[Space & Crafts] Lighting mood applied: {displayName} ({MoodFor(worldIndex).Label})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Worlds 02–10 lighting mood complete ({upgraded} scenes).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Space & Crafts/Refresh World 02 Beeping Caves Lighting")]
        public static void RefreshWorld02FernLightingIter1552()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyLightingMood(2);
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 02 fern lighting] applied, sceneSaved={saved}.");
            if (Application.isBatchMode) EditorApplication.Exit(saved ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Refresh World 02 Resonant Cave Sky")]
        public static void RefreshWorld02FernSkyIter1556()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyLightingMood(2);
            var sky = EnsureWorld02FernSkybox();
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 02 fern sky Iter1556] skybox={sky != null}, sceneSaved={saved}.");
            if (Application.isBatchMode) EditorApplication.Exit(saved && sky != null ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Refresh World 02 Resonant Cave Ground")]
        public static void RefreshWorld02FernGroundIter1557()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyLightingMood(2);
            var sky = EnsureWorld02FernSkybox();
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 02 fern ground Iter1557] skybox={sky != null}, sceneSaved={saved}.");
            if (Application.isBatchMode) EditorApplication.Exit(saved && sky != null ? 0 : 1);
        }

        private static Material EnsureWorld02FernSkybox()
        {
            const string path = "Assets/SpaceAndCrafts/Materials/TempArt/World02_ResonantCaveSky.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader == null) return null;
                material = new Material(shader) { name = "World02_ResonantCaveSky" };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.30f, 0.36f, 0.62f));
            if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.11f, 0.13f, 0.24f));
            if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 1.35f);
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 0.72f);
            if (material.HasProperty("_SunDisk")) material.SetFloat("_SunDisk", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Apply mood to the active scene. Safe to call from other world bootstraps.</summary>
        public static void ApplyLightingMood(int worldIndex)
        {
            if (worldIndex < 2 || worldIndex > 10)
            {
                return;
            }

            var mood = MoodFor(worldIndex);
            DisableOrphanDirectionalLights();

            var root = GameObject.Find(LightingRootName);
            if (root == null)
            {
                root = new GameObject(LightingRootName);
            }

            ClearChildren(root.transform);

            var keyGo = new GameObject("ART_KeyLight");
            keyGo.transform.SetParent(root.transform, false);
            keyGo.transform.rotation = Quaternion.Euler(mood.KeyEuler);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = mood.KeyColor;
            key.intensity = mood.KeyIntensity;
            key.shadows = LightShadows.Soft;

            if (mood.HasAccentPoint)
            {
                var accentGo = new GameObject("ART_AccentPoint");
                accentGo.transform.SetParent(root.transform, false);
                accentGo.transform.localPosition = mood.AccentLocalPos;
                var accent = accentGo.AddComponent<Light>();
                accent.type = LightType.Point;
                accent.color = mood.AccentColor;
                accent.intensity = mood.AccentIntensity;
                accent.range = mood.AccentRange;
                accent.shadows = LightShadows.None;
            }

            ApplyRenderSettings(mood);

            OrganicWorldHorizonBuilder.Build(root.transform, worldIndex, mood.FogColor, ApplyHorizonMat);
        }

        private static void ApplyHorizonMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
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
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            // Reset the World 02 bowl's previous lighting experiment; only the skybox is tuned here.
            if (assetName == "Mat_HorizonBowl_W02")
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (litShader != null) mat.shader = litShader;
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

        private static void ApplyRenderSettings(Mood mood)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = mood.FogColor;
            RenderSettings.fogStartDistance = mood.FogStart;
            RenderSettings.fogEndDistance = mood.FogEnd;

            RenderSettings.ambientMode = mood.AmbientMode;
            if (mood.AmbientMode == AmbientMode.Flat)
            {
                RenderSettings.ambientLight = mood.AmbientFlat;
            }
            else
            {
                RenderSettings.ambientSkyColor = mood.AmbientSky;
                RenderSettings.ambientEquatorColor = mood.AmbientEquator;
                RenderSettings.ambientGroundColor = mood.AmbientGround;
            }

            RenderSettings.subtractiveShadowColor = Color.Lerp(mood.FogColor, new Color(0.2f, 0.18f, 0.25f), 0.45f);
        }

        private static Mood MoodFor(int worldIndex)
        {
            // Landmark sits near (0, 0.2, 10); accent point lights that region.
            var landmarkGlow = new Vector3(0f, 3.5f, 10f);

            switch (worldIndex)
            {
                case 2: // resonant cave indigo-cyan
                    return new Mood
                    {
                        Label = "resonant cave indigo-cyan",
                        KeyColor = new Color(0.74f, 0.82f, 1f),
                        KeyIntensity = 0.9f,
                        KeyEuler = new Vector3(48f, -25f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(0.25f, 0.30f, 0.52f),
                        AmbientEquator = new Color(0.31f, 0.34f, 0.52f),
                        AmbientGround = new Color(0.09f, 0.10f, 0.19f),
                        FogColor = new Color(0.20f, 0.24f, 0.40f),
                        FogStart = 14f,
                        FogEnd = 62f,
                        HasAccentPoint = true,
                        AccentColor = new Color(0.34f, 0.86f, 1f),
                        AccentIntensity = 1.15f,
                        AccentRange = 13f,
                        AccentLocalPos = landmarkGlow
                    };
                case 3: // marsh cyan
                    return new Mood
                    {
                        Label = "marsh cyan",
                        KeyColor = new Color(0.7f, 0.95f, 1f),
                        KeyIntensity = 1.05f,
                        KeyEuler = new Vector3(35f, 40f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(0.45f, 0.85f, 0.95f),
                        AmbientEquator = new Color(0.55f, 0.75f, 0.7f),
                        AmbientGround = new Color(0.15f, 0.3f, 0.32f),
                        FogColor = new Color(0.45f, 0.78f, 0.85f),
                        FogStart = 12f,
                        FogEnd = 70f,
                        HasAccentPoint = true,
                        AccentColor = new Color(0.35f, 1f, 0.95f),
                        AccentIntensity = 1.6f,
                        AccentRange = 14f,
                        AccentLocalPos = landmarkGlow
                    };
                case 4: // bazaar magenta-warm
                    return new Mood
                    {
                        Label = "bazaar magenta-warm",
                        KeyColor = new Color(1f, 0.7f, 0.85f),
                        KeyIntensity = 1.2f,
                        KeyEuler = new Vector3(42f, -55f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(1f, 0.55f, 0.8f),
                        AmbientEquator = new Color(1f, 0.75f, 0.45f),
                        AmbientGround = new Color(0.35f, 0.2f, 0.28f),
                        FogColor = new Color(0.85f, 0.5f, 0.7f),
                        FogStart = 15f,
                        FogEnd = 75f,
                        HasAccentPoint = true,
                        AccentColor = new Color(1f, 0.4f, 0.75f),
                        AccentIntensity = 1.8f,
                        AccentRange = 11f,
                        AccentLocalPos = landmarkGlow
                    };
                case 5: // crater amber
                    return new Mood
                    {
                        Label = "crater amber",
                        KeyColor = new Color(1f, 0.78f, 0.35f),
                        KeyIntensity = 1.25f,
                        KeyEuler = new Vector3(55f, 20f, 0f),
                        AmbientMode = AmbientMode.Flat,
                        AmbientFlat = new Color(0.85f, 0.55f, 0.25f),
                        AmbientSky = new Color(0.95f, 0.7f, 0.35f),
                        AmbientEquator = new Color(0.9f, 0.55f, 0.25f),
                        AmbientGround = new Color(0.35f, 0.2f, 0.1f),
                        FogColor = new Color(0.9f, 0.65f, 0.3f),
                        FogStart = 20f,
                        FogEnd = 90f,
                        HasAccentPoint = true,
                        AccentColor = new Color(1f, 0.55f, 0.15f),
                        AccentIntensity = 1.5f,
                        AccentRange = 13f,
                        AccentLocalPos = landmarkGlow
                    };
                case 6: // frost ice-blue
                    return new Mood
                    {
                        Label = "frost ice-blue",
                        KeyColor = new Color(0.85f, 0.95f, 1f),
                        KeyIntensity = 1.1f,
                        KeyEuler = new Vector3(28f, -10f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(0.7f, 0.9f, 1f),
                        AmbientEquator = new Color(0.75f, 0.85f, 0.95f),
                        AmbientGround = new Color(0.25f, 0.35f, 0.45f),
                        FogColor = new Color(0.7f, 0.88f, 0.98f),
                        FogStart = 10f,
                        FogEnd = 65f,
                        HasAccentPoint = true,
                        AccentColor = new Color(0.55f, 0.9f, 1f),
                        AccentIntensity = 1.3f,
                        AccentRange = 12f,
                        AccentLocalPos = landmarkGlow
                    };
                case 7: // echo violet
                    return new Mood
                    {
                        Label = "echo violet",
                        KeyColor = new Color(0.85f, 0.65f, 1f),
                        KeyIntensity = 1.05f,
                        KeyEuler = new Vector3(38f, 70f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(0.65f, 0.4f, 0.95f),
                        AmbientEquator = new Color(0.75f, 0.5f, 0.9f),
                        AmbientGround = new Color(0.2f, 0.12f, 0.3f),
                        FogColor = new Color(0.55f, 0.35f, 0.75f),
                        FogStart = 14f,
                        FogEnd = 72f,
                        HasAccentPoint = true,
                        AccentColor = new Color(0.75f, 0.35f, 1f),
                        AccentIntensity = 1.7f,
                        AccentRange = 14f,
                        AccentLocalPos = landmarkGlow
                    };
                case 8: // tide teal
                    return new Mood
                    {
                        Label = "tide teal",
                        KeyColor = new Color(0.55f, 0.95f, 0.9f),
                        KeyIntensity = 1.15f,
                        KeyEuler = new Vector3(45f, -70f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(0.35f, 0.8f, 0.85f),
                        AmbientEquator = new Color(0.4f, 0.7f, 0.65f),
                        AmbientGround = new Color(0.12f, 0.28f, 0.3f),
                        FogColor = new Color(0.3f, 0.65f, 0.7f),
                        FogStart = 16f,
                        FogEnd = 80f,
                        HasAccentPoint = true,
                        AccentColor = new Color(0.2f, 1f, 0.85f),
                        AccentIntensity = 1.55f,
                        AccentRange = 13f,
                        AccentLocalPos = landmarkGlow
                    };
                case 9: // nursery soft pink-star
                    return new Mood
                    {
                        Label = "nursery soft pink-star",
                        KeyColor = new Color(1f, 0.85f, 0.95f),
                        KeyIntensity = 0.95f,
                        KeyEuler = new Vector3(32f, 15f, 0f),
                        AmbientMode = AmbientMode.Flat,
                        AmbientFlat = new Color(0.95f, 0.7f, 0.85f),
                        AmbientSky = new Color(1f, 0.75f, 0.9f),
                        AmbientEquator = new Color(0.9f, 0.7f, 1f),
                        AmbientGround = new Color(0.35f, 0.25f, 0.45f),
                        FogColor = new Color(0.9f, 0.7f, 0.85f),
                        FogStart = 22f,
                        FogEnd = 95f,
                        HasAccentPoint = true,
                        AccentColor = new Color(1f, 0.9f, 0.65f),
                        AccentIntensity = 1.9f,
                        AccentRange = 15f,
                        AccentLocalPos = landmarkGlow + new Vector3(0f, 1.5f, 0f)
                    };
                case 10: // cathedral stained-warm
                    return new Mood
                    {
                        Label = "cathedral stained-warm",
                        KeyColor = new Color(1f, 0.82f, 0.55f),
                        KeyIntensity = 1.3f,
                        KeyEuler = new Vector3(58f, -40f, 0f),
                        AmbientMode = AmbientMode.Trilight,
                        AmbientSky = new Color(1f, 0.75f, 0.4f),
                        AmbientEquator = new Color(0.95f, 0.45f, 0.55f),
                        AmbientGround = new Color(0.35f, 0.22f, 0.3f),
                        FogColor = new Color(0.75f, 0.5f, 0.4f),
                        FogStart = 18f,
                        FogEnd = 88f,
                        HasAccentPoint = true,
                        AccentColor = new Color(1f, 0.55f, 0.35f),
                        AccentIntensity = 2f,
                        AccentRange = 16f,
                        AccentLocalPos = landmarkGlow
                    };
                default:
                    return new Mood
                    {
                        Label = "neutral",
                        KeyColor = Color.white,
                        KeyIntensity = 1f,
                        KeyEuler = new Vector3(40f, -30f, 0f),
                        AmbientMode = AmbientMode.Flat,
                        AmbientFlat = Color.gray,
                        FogColor = Color.gray,
                        FogStart = 20f,
                        FogEnd = 80f
                    };
            }
        }

        private static void DisableOrphanDirectionalLights()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type != LightType.Directional)
                {
                    continue;
                }

                if (IsUnderLightingRoot(light.transform))
                {
                    continue;
                }

                // Default empty-scene / leftover key lights — disable so mood owns the look.
                light.enabled = false;
                if (light.gameObject.name == "Directional Light")
                {
                    Object.DestroyImmediate(light.gameObject);
                }
            }
        }

        private static bool IsUnderLightingRoot(Transform t)
        {
            while (t != null)
            {
                if (t.name == LightingRootName)
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
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
    }
}
#endif
