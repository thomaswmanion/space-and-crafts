#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Imports one Meshy in-place run on the existing Iter1791 Splat rig for capture-only review.</summary>
    public static class SplatMeshyRunFast3ReviewIter1834
    {
        public const string ClipPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1834/Splat_RunFast3_InPlace_Iter1834.fbx";
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/rigged_character.fbx";

        [MenuItem("Space & Crafts/Prepare Iter1834 Splat RunFast3 Review")]
        public static void PrepareClip()
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (importer == null || avatar == null)
                throw new InvalidOperationException("RunFast3 FBX or the Iter1791 Splat Avatar is missing.");

            if (importer.animationType != ModelImporterAnimationType.Generic
                || importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther
                || importer.sourceAvatar != avatar || !importer.importAnimation
                || importer.optimizeGameObjects)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.optimizeGameObjects = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            if (clips.Length == 0)
                throw new InvalidOperationException("Meshy RunFast3 imported without an AnimationClip.");
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Debug.Log($"[Splat RunFast3 review] clip={clip.name}, duration={clip.length:F3}s, curveBindings={bindings.Length}, sourceAvatar={avatar.name}.");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void OverrideRunForCapture(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("The Splat Meshy Animator/controller is missing.");
            var newRun = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && clip.length > 0.01f);
            if (newRun == null)
                throw new InvalidOperationException("Imported Meshy RunFast3 clip is missing.");

            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController)
            {
                name = "Iter1834 RunFast3 capture-only override",
                hideFlags = HideFlags.HideAndDontSave,
            };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);
            var runIndex = overrides.FindIndex(pair => pair.Key != null
                && (pair.Key.name.IndexOf("running", StringComparison.OrdinalIgnoreCase) >= 0
                    || pair.Key.name.Equals("Run", StringComparison.OrdinalIgnoreCase)));
            if (runIndex < 0)
            {
                UnityEngine.Object.DestroyImmediate(overrideController);
                throw new InvalidOperationException("Could not find the current Run clip in Splat's BlendTree.");
            }

            var source = overrides[runIndex].Key;
            overrides[runIndex] = new KeyValuePair<AnimationClip, AnimationClip>(source, newRun);
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;
            animator.Rebind();
            Debug.Log($"[Splat RunFast3 review] temporary BlendTree override: {source.name} -> {newRun.name} ({newRun.length:F2}s); saved controller unchanged.");
        }
    }
}
#endif
