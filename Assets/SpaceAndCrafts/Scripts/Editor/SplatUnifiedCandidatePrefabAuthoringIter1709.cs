#if UNITY_EDITOR
using System.IO;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Packages the Iter1705 candidate FBX, Avatar, controller, and Splat motor
    /// bridge as a separate review prefab. It never edits Splat_TEMP or a scene.
    /// </summary>
    public static class SplatUnifiedCandidatePrefabAuthoringIter1709
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.fbx";
        private const string ControllerPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.controller";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.prefab";
        private const string ReportPath = "Logs/score-comparison/splat-unified-candidate-prefab-iter1709/prefab-review.txt";

        [MenuItem("Space & Crafts/Author Isolated Unified Splat Prefab Iter1709")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (model == null || controller == null || avatar == null || !avatar.isValid)
                throw new System.InvalidOperationException("Candidate FBX, controller, or valid Avatar is unavailable.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new System.InvalidOperationException($"Refusing to overwrite existing candidate prefab: {PrefabPath}");

            // Work in an unsaved empty scene so no production scene or prefab
            // receives a temporary hierarchy change.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
                throw new System.InvalidOperationException("Could not instantiate the candidate FBX root.");
            instance.name = "Splat Unified Candidate Iter1705";

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = instance.GetComponent<SplatMeshyAnimatorDriver>();
            if (driver == null)
                driver = instance.AddComponent<SplatMeshyAnimatorDriver>();
            var serializedDriver = new SerializedObject(driver);
            serializedDriver.FindProperty("animator").objectReferenceValue = animator;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException("Unity failed to save the isolated candidate prefab.");

            var reportPath = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? "Logs");
            File.WriteAllText(reportPath,
                $"prefab={PrefabPath}\nmodel={ModelPath}\ncontroller={ControllerPath}\navatar={avatar.name} valid={avatar.isValid}\n" +
                $"animatorController={animator.runtimeAnimatorController.name}\nrootMotion={animator.applyRootMotion}\n" +
                $"driver={driver.GetType().Name} motor=resolved-from-parent-at-runtime\n" +
                "scope=candidate-only; not connected to Splat_TEMP or a production scene\n");
            Debug.Log($"[Splat candidate] Authored isolated prefab {PrefabPath}; active character and scenes unchanged.");
            EditorApplication.Exit(0);
        }
    }
}
#endif
