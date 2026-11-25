#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Splat's Workshop hub — evolves with PlayerProgressSave (Odyssey Cap-kingdom energy).
    /// </summary>
    public static class WorkshopHubBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";

        [MenuItem("Space & Crafts/Bootstrap Workshop Hub")]
        public static void Bootstrap()
        {
            EnsureFolder("Assets/SpaceAndCrafts/Scenes/Menus");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("ART_WorkshopHub");
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2.2f, -8f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();

            var keyGo = new GameObject("ART_WorkshopKeyLight");
            keyGo.transform.SetParent(root.transform, false);
            keyGo.transform.position = new Vector3(2f, 5f, -2f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Point;
            key.range = 18f;
            key.intensity = 1.2f;

            CreateFloor(root.transform);
            var tiers = new Transform[6];
            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = new GameObject($"ART_WorkshopTier_{i + 1}");
                tier.transform.SetParent(root.transform, false);
                tier.transform.localPosition = new Vector3(-4.5f + i * 1.8f, 0.6f, 2.5f);
                var shelf = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Shelf_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.7f, 0.25f, 0.03f),
                    tier.transform,
                    Vector3.zero,
                    new Vector3(1.4f, 2.2f + i * 0.15f, 0.7f),
                    null);
                ApplyMat(shelf, $"Mat_WorkshopShelf_{i}", new Color(0.55f, 0.4f, 0.28f), 0.35f, 0.05f);
                var trophy = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Trophy_{i}",
                    OrganicMeshFactory.FacetedStar(0.28f, 0.11f, 0.08f, 5),
                    tier.transform,
                    new Vector3(0f, 1.4f, 0f),
                    Vector3.one * (0.6f + i * 0.05f),
                    null);
                ApplyMat(trophy, $"Mat_WorkshopTrophy_{i}", Color.HSVToRGB(i / 6f, 0.55f, 1f), 0.6f, 0.2f);
                tiers[i] = tier.transform;
                if (i > 0)
                {
                    tier.SetActive(false);
                }
            }

            var workbench = OrganicMeshFactory.CreateMeshObject(
                "ART_Workbench",
                OrganicMeshFactory.SoftBlob(3, 1.1f, 0.35f, 0.04f),
                root.transform,
                new Vector3(0f, 0.55f, 0f),
                new Vector3(2.4f, 1f, 1.4f),
                null);
            ApplyMat(workbench, "Mat_WorkshopBench", new Color(0.45f, 0.55f, 0.7f), 0.45f, 0.15f);

            OrganicSplatBuilder.BuildVisualRoot(
                CreateChild(root.transform, "ART_HubSplat", new Vector3(2.2f, 0.2f, -1f)).transform,
                ApplyMat);

            WorldMusicDirector.Ensure(1);

            var status = CreateWorldText(
                root.transform,
                "Status",
                "Splat's Workshop",
                new Vector3(0f, 3.6f, 0f),
                0.09f,
                new Color(1f, 0.92f, 0.6f));
            var inventory = CreateWorldText(
                root.transform,
                "Inventory",
                "Workbench empty",
                new Vector3(-3.2f, 2.4f, 0f),
                0.05f,
                new Color(0.85f, 0.95f, 1f));
            inventory.anchor = TextAnchor.UpperLeft;
            inventory.alignment = TextAlignment.Left;

            var worldLabel = CreateWorldText(
                root.transform,
                "WorldSelect",
                "World  <  01 Rainbow Kingdom  >",
                new Vector3(0f, -1.6f, 0f),
                0.07f,
                new Color(0.75f, 0.95f, 1f));
            var board = root.AddComponent<WorldSelectBoard>();
            var boardSo = new SerializedObject(board);
            boardSo.FindProperty("label").objectReferenceValue = worldLabel;
            boardSo.FindProperty("worldCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<WorldCatalog>("Assets/SpaceAndCrafts/ScriptableObjects/Worlds/WorldCatalog.asset");
            // Match real world scene filenames.
            SetStringArray(boardSo, "sceneNames", new[]
            {
                "RainbowWorld_Arrival",
                "World02_CorkscrewCanopy",
                "World03_MagnetMarsh",
                "World04_BubbleBazaar",
                "World05_ClockworkCrater",
                "World06_FrostedFilament",
                "World07_EchoAtelier",
                "World08_TideTinkerYard",
                "World09_NebulaNursery",
                "World10_ScrapCathedral",
            });
            SetStringArray(boardSo, "worldNames", new[]
            {
                "01 Rainbow Kingdom",
                "02 Beeping Caves Kingdom",
                "03 Leaf Lands",
                "04 Bubble Bazaar",
                "05 Clockwork Crater",
                "06 Mountain Lands",
                "07 Echo Atelier",
                "08 Tide Tinker Yard",
                "09 Nebula Nursery",
                "10 Scrap Cathedral",
            });
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldText(
                root.transform,
                "CTA",
                "JUMP / ENTER — Launch world   ·   H — Home   ·   F9 — Reset save",
                new Vector3(0f, -2.2f, 0f),
                0.055f,
                new Color(1f, 0.65f, 0.85f));

            var director = root.AddComponent<WorkshopHubDirector>();
            director.Configure(tiers, status, inventory, key);
            root.AddComponent<WorkshopHubController>();
            var bridgeHost = new GameObject("SYS_ProgressSave");
            bridgeHost.AddComponent<ProgressSaveBridge>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath, 2);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Workshop hub bootstrapped: {ScenePath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static GameObject CreateChild(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        private static void CreateFloor(Transform parent)
        {
            var floor = OrganicMeshFactory.CreateMeshObject(
                "ART_WorkshopFloor",
                OrganicMeshFactory.SoftBlob(2, 1.5f, 0.12f, 0.02f),
                parent,
                Vector3.zero,
                new Vector3(16f, 0.35f, 12f),
                null);
            ApplyMat(floor, "Mat_WorkshopFloor", new Color(0.22f, 0.24f, 0.3f), 0.3f, 0.05f);
            floor.AddComponent<BoxCollider>();
        }

        private static TextMesh CreateWorldText(Transform parent, string name, string content, Vector3 localPos, float charSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = content;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = charSize;
            tm.fontSize = 64;
            tm.color = color;
            return tm;
        }

        private static void SetStringArray(SerializedObject so, string prop, string[] values)
        {
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).stringValue = values[i];
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var cur = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }

                cur = next;
            }
        }

        private static void AddToBuildSettings(string scenePath, int index)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            while (scenes.Count <= index)
            {
                scenes.Add(new EditorBuildSettingsScene());
            }

            scenes.Insert(Mathf.Clamp(index, 0, scenes.Count), new EditorBuildSettingsScene(scenePath, true));
            // Deduplicate empties
            scenes.RemoveAll(s => string.IsNullOrEmpty(s.path));
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
