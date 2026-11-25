#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Creates empty labeled development scenes. Run once after opening the project.
    /// </summary>
    public static class PlaygroundSceneBootstrap
    {
        private const string DevPath = "Assets/SpaceAndCrafts/Scenes/Development";
        private const string ArrivalPath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld";

        [MenuItem("Space & Crafts/Bootstrap Development Scenes")]
        public static void Bootstrap()
        {
            EnsureFolder("Assets/SpaceAndCrafts");
            EnsureFolder("Assets/SpaceAndCrafts/Scenes");
            EnsureFolder(DevPath);
            EnsureFolder(ArrivalPath);

            CreateEmptyScene($"{DevPath}/PlayerPlayground.unity", "PlayerPlayground");
            CreateEmptyScene($"{DevPath}/MechanicsPlayground.unity", "MechanicsPlayground");
            CreateEmptyScene($"{DevPath}/ArtPlayground.unity", "ArtPlayground");
            CreateEmptyScene($"{ArrivalPath}/RainbowWorld_Arrival.unity", "RainbowWorld_Arrival");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Space & Crafts] Development scenes created. Next: wire Splat TEMP ART prefab in PlayerPlayground.");
        }

        private static void CreateEmptyScene(string assetPath, string rootName)
        {
            if (System.IO.File.Exists(assetPath))
            {
                Debug.Log($"[Space & Crafts] Scene already exists: {assetPath}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            _ = new GameObject($"TEMP_SCENE_ROOT_{rootName}");
            EditorSceneManager.SaveScene(scene, assetPath);
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
