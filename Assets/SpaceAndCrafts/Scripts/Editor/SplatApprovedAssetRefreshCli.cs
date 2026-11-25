#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Persists the current approved Splat builder output into its reusable prefab
    /// and the two active gameplay scenes without rebuilding unrelated scene art.
    /// </summary>
    public static class SplatApprovedAssetRefreshCli
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private static readonly string[] ScenePaths =
        {
            "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity",
        };

        public static void Refresh()
        {
            try
            {
                RefreshPrefab();
                for (var i = 0; i < ScenePaths.Length; i++)
                    RefreshScene(ScenePaths[i]);
                AssetDatabase.SaveAssets();
                Debug.Log("[Approved Splat refresh] Updated reusable prefab and active gameplay scene visuals.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RefreshPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                SplatVisualRepair.Repair(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Approved Splat refresh] Prefab marker={FindMarker(root.transform)}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RefreshScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            GameObject splat = null;
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == "Splat_TEMP")
                {
                    splat = roots[i];
                    break;
                }
            }

            if (splat == null)
                throw new System.InvalidOperationException($"Could not find Splat_TEMP root in {scenePath}.");

            SplatVisualRepair.Repair(splat.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException($"Could not save refreshed scene {scenePath}.");
            Debug.Log($"[Approved Splat refresh] Scene={scenePath}, marker={FindMarker(splat.transform)}.");
        }

        private static string FindMarker(Transform root)
        {
            var marker = root != null ? root.Find("ART_SplatVisual/" + OrganicSplatBuilder.FaceVersionMarkerName) : null;
            return marker != null ? marker.name : "missing";
        }
    }
}
#endif
