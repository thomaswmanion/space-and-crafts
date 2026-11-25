#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Promotes the reviewed Meshy Splat visual into the saved player prefab and
    /// unpacked World 1 Arrival scene while retaining the existing physics root.
    /// </summary>
    public static class PromoteSplatMeshyVisualCli
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string CandidatePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/ReviewOnly/Splat_Meshy_Iter1791_ReviewOnly.prefab";
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";

        [MenuItem("Space & Crafts/Promote Reviewed Meshy Splat Visual")]
        public static void Promote()
        {
            var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            if (candidate == null)
            {
                Fail("Reviewed Meshy Splat prefab is missing: " + CandidatePath);
                return;
            }

            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefab == null)
            {
                Fail("Could not load Splat player prefab: " + PrefabPath);
                return;
            }

            try
            {
                Install(prefab, candidate);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                Fail("Splat_TEMP was not found in saved Arrival scene.");
                return;
            }

            Install(splat, candidate);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Promoted reviewed Meshy Splat into the saved player prefab and Arrival scene. Existing motor and capsule remain on Splat_TEMP.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Install(GameObject root, GameObject candidate)
        {
            var oldMeshy = root.transform.Find(SplatMeshyVisualMode.MeshyVisualRootName);
            if (oldMeshy != null)
            {
                Object.DestroyImmediate(oldMeshy.gameObject);
            }

            var visual = PrefabUtility.InstantiatePrefab(candidate, root.transform) as GameObject;
            if (visual == null)
            {
                throw new System.InvalidOperationException("Could not instantiate reviewed Splat Meshy prefab beneath the player root.");
            }

            visual.name = SplatMeshyVisualMode.MeshyVisualRootName;
            visual.transform.SetParent(root.transform, false);
            var mode = root.GetComponent<SplatMeshyVisualMode>() ?? root.AddComponent<SplatMeshyVisualMode>();
            mode.enabled = true;

            var presenter = root.GetComponent<SplatFeelPresenter>();
            if (presenter != null)
            {
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("motor").objectReferenceValue = root.GetComponent<SplatMotor>();
                serialized.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var interactor = root.GetComponent<SplatInteractor>();
            if (interactor != null)
            {
                var serialized = new SerializedObject(interactor);
                var input = root.GetComponent<SplatInputReader>();
                serialized.FindProperty("input").objectReferenceValue = input;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            SplatVisualRepair.Repair(root.transform);
            EditorUtility.SetDirty(root);
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Space & Crafts] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
#endif
