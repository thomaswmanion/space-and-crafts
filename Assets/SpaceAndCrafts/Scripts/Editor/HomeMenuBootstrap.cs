#if UNITY_EDITOR
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Exciting title / home menu — first commercial packaging surface.
    /// World-space TextMesh (no uGUI package required). Press Jump/Enter to play.
    /// </summary>
    public static class HomeMenuBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/HomeMenu.unity";

        [MenuItem("Space & Crafts/Bootstrap Home Menu")]
        public static void Bootstrap()
        {
            EnsureFolder("Assets/SpaceAndCrafts/Scenes/Menus");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("ART_HomeMenu");
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 1.6f, -6.5f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.08f, 0.22f);
            camGo.AddComponent<AudioListener>();

            var hero = new GameObject("ART_TitleSplat");
            hero.transform.SetParent(root.transform, false);
            hero.transform.position = new Vector3(0f, 0.2f, 0f);
            SpaceAndCrafts.Art.OrganicSplatBuilder.BuildVisualRoot(hero.transform, ApplyMat);

            var gate = SpaceAndCrafts.Art.OrganicMeshFactory.CreateMeshObject(
                "ART_TitleGateway",
                SpaceAndCrafts.Art.OrganicMeshFactory.ArchGateway(3.2f, 4.2f, 0.4f, 28),
                root.transform,
                new Vector3(0f, 0.1f, 4f),
                Vector3.one,
                null);
            ApplyMat(gate, "Mat_TitleGateway", new Color(0.95f, 0.55f, 1f), 0.25f, 0.15f);

            WorldMusicDirector.Ensure(1);

            var title = CreateWorldText(root.transform, "Title", "SPACE & CRAFTS", new Vector3(0f, 3.3f, 0f), 0.18f, new Color(1f, 0.92f, 0.55f));
            CreateWorldText(root.transform, "Tagline", "Craft wild inventions. Bounce across Our Way Galaxy.", new Vector3(0f, 2.7f, 0f), 0.07f, new Color(0.9f, 0.85f, 1f));
            var slotLabel = CreateWorldText(root.transform, "SaveSlot", "Slot 1: Empty — New Game", new Vector3(0f, 2.15f, 0f), 0.065f, new Color(0.75f, 0.95f, 1f));
            var slotBoard = root.AddComponent<SaveSlotBoard>();
            var slotSo = new SerializedObject(slotBoard);
            slotSo.FindProperty("label").objectReferenceValue = slotLabel;
            slotSo.ApplyModifiedPropertiesWithoutUndo();

            var worldLabel = CreateWorldText(root.transform, "WorldSelect", "World  <  01 Rainbow Kingdom  >", new Vector3(0f, 1.55f, 0f), 0.05f, new Color(0.65f, 0.85f, 1f));
            var worldBoard = root.AddComponent<WorldSelectBoard>();
            var worldSo = new SerializedObject(worldBoard);
            worldSo.FindProperty("label").objectReferenceValue = worldLabel;
            worldSo.ApplyModifiedPropertiesWithoutUndo();

            var cta = CreateWorldText(root.transform, "CTA", "← → SAVE SLOT · JUMP / ENTER NEW GAME OR CONTINUE", new Vector3(0f, -1.4f, 0f), 0.07f, new Color(1f, 0.55f, 0.85f));
            CreateWorldText(root.transform, "Watermark", "New Game plays the opening story, then Arrival", new Vector3(0f, -2.1f, 0f), 0.045f, new Color(1f, 1f, 1f, 0.45f));

            var controller = root.AddComponent<HomeMenuController>();
            root.AddComponent<HomeCommercialPitch>();
            var so = new SerializedObject(controller);
            so.FindProperty("titleRoot").objectReferenceValue = title.transform;
            so.FindProperty("heroRoot").objectReferenceValue = hero.transform;
            so.FindProperty("ctaRoot").objectReferenceValue = cta.transform;
            so.FindProperty("playSceneName").stringValue = "RainbowWorld_Arrival";
            so.FindProperty("worldSelect").objectReferenceValue = null;
            so.FindProperty("saveSlots").objectReferenceValue = slotBoard;
            so.FindProperty("openWithStoryOnNewGame").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath, 0);
            AddToBuildSettings("Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity", 1);
            AddToBuildSettings("Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity", 2);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Home menu bootstrapped: {ScenePath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
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

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
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

            scenes.Insert(Mathf.Clamp(index, 0, scenes.Count), new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
