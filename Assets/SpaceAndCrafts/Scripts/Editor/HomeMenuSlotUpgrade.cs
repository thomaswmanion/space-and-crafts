#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Upgrades HomeMenu: modern OnGUI title, 3 save slots, story flow. Strips antique TextMesh chrome.
    /// </summary>
    public static class HomeMenuSlotUpgrade
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/HomeMenu.unity";

        [MenuItem("Space & Crafts/Upgrade Home Menu Save Slots")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("ART_HomeMenu");
            if (root == null)
            {
                Debug.LogError("[Space & Crafts] ART_HomeMenu missing — run Bootstrap Home Menu first.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            var controller = root.GetComponent<HomeMenuController>() ?? root.AddComponent<HomeMenuController>();
            var slotBoard = root.GetComponent<SaveSlotBoard>() ?? root.AddComponent<SaveSlotBoard>();

            // OnGUI owns the menu — destroy world TextMesh chrome.
            foreach (var childName in new[] { "Title", "Tagline", "CTA", "Watermark", "SaveSlot", "WorldSelect" })
            {
                var child = root.transform.Find(childName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var worldBoard = root.GetComponent<WorldSelectBoard>();
            if (worldBoard != null)
            {
                Object.DestroyImmediate(worldBoard);
            }

            var pitch = root.GetComponent<HomeCommercialPitch>();
            if (pitch != null)
            {
                Object.DestroyImmediate(pitch);
            }

            var hero = root.transform.Find("ART_TitleSplat");
            if (hero != null)
            {
                SplatVisualRepair.Repair(hero);
            }

            var so = new SerializedObject(controller);
            so.FindProperty("saveSlots").objectReferenceValue = slotBoard;
            so.FindProperty("playSceneName").stringValue = "RainbowWorld_Arrival";
            so.FindProperty("openWithStoryOnNewGame").boolValue = true;
            var worldSelectProp = so.FindProperty("worldSelect");
            if (worldSelectProp != null)
            {
                worldSelectProp.objectReferenceValue = null;
            }

            if (so.FindProperty("titleRoot") != null)
            {
                so.FindProperty("titleRoot").objectReferenceValue = null;
            }

            if (so.FindProperty("ctaRoot") != null)
            {
                so.FindProperty("ctaRoot").objectReferenceValue = null;
            }

            if (so.FindProperty("heroRoot") != null && hero != null)
            {
                so.FindProperty("heroRoot").objectReferenceValue = hero;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var slotSo = new SerializedObject(slotBoard);
            if (slotSo.FindProperty("label") != null)
            {
                slotSo.FindProperty("label").objectReferenceValue = null;
            }

            slotSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureBuildScene(ScenePath, 0);
            EnsureBuildScene("Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity", 1);
            EnsureBuildScene("Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity", 2);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Home menu upgraded: modern OnGUI title + slots + story.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureBuildScene(string path, int index)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Insert(Mathf.Clamp(index, 0, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
