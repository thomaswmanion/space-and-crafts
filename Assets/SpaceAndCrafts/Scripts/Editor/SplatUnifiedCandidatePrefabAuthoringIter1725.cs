#if UNITY_EDITOR
using System;
using System.IO;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Builds a diagnostic prefab with Animator on the FBX clip root.</summary>
    public static class SplatUnifiedCandidatePrefabAuthoringIter1725
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/Splat_UnifiedRig_Candidate_Iter1717.fbx";
        private const string ControllerPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1722/Splat_Iter1722_ReviewOnly.controller";
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1725";
        private const string AvatarPath = Folder + "/Splat_Iter1725_ModelRoot_Avatar.asset";
        private const string PrefabPath = Folder + "/Splat_Iter1725_ModelRoot_ReviewOnly.prefab";
        private const string ReportPath = "Logs/score-comparison/splat-unified-candidate-prefab-iter1725/prefab-review.txt";

        [MenuItem("Space & Crafts/Author Isolated Splat Iter1725 Model-Root Review Prefab")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing Iter1725 prefab: " + PrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (model == null || controller == null)
                throw new InvalidOperationException("Iter1717 model or Iter1722 controller is missing.");

            Directory.CreateDirectory(Folder);
            Avatar avatar;
            var avatarScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var modelRoot = PrefabUtility.InstantiatePrefab(model, avatarScene) as GameObject;
                if (modelRoot == null) throw new InvalidOperationException("Could not instantiate the Iter1717 FBX root.");
                avatar = AvatarBuilder.BuildGenericAvatar(modelRoot, string.Empty);
                if (avatar == null || !avatar.isValid)
                    throw new InvalidOperationException("Could not build valid Avatar rooted at the FBX clip root.");
                avatar.name = "Splat Iter1725 Model Root Avatar";
                AssetDatabase.CreateAsset(avatar, AvatarPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(avatarScene); }
            AssetDatabase.SaveAssets();
            avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            if (avatar == null || !avatar.isValid) throw new InvalidOperationException("Saved Iter1725 Avatar is invalid.");

            var prefabScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(model, prefabScene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate the Iter1717 model for review.");
                var animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = instance.AddComponent<SplatMeshyAnimatorDriver>();
                var serializedDriver = new SerializedObject(driver);
                serializedDriver.FindProperty("animator").objectReferenceValue = animator;
                serializedDriver.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Could not save the Iter1725 review-only prefab.");
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? "Logs");
                File.WriteAllText(ReportPath,
                    $"prefab={PrefabPath}\nmodelRootAndAnimator={instance.name}\navatar={avatar.name} valid={avatar.isValid}\n" +
                    "controller=Iter1722 review-only controller\n" +
                    "purpose=diagnose imported Generic clip transform paths by aligning Animator root with FBX animation root\n" +
                    "scope=ReviewOnly; no production prefab, scene, or source FBX modified\n");
                Debug.Log("[Splat Iter1725] Saved model-root Animator diagnostic prefab: " + PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(prefabScene); }
        }
    }
}
#endif
