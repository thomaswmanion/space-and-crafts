#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Imports the Iter1835 Meshy run alternatives onto Splat's existing rig for capture-only review.</summary>
    public static class SplatMeshyRunCandidateReviewIter1835
    {
        public const string ClipPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1835/Splat_RunFast_AltSet_Iter1835.fbx";
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/rigged_character.fbx";

        [MenuItem("Space & Crafts/Prepare Iter1835 Splat Run Alternatives")]
        public static void PrepareClips()
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (importer == null || avatar == null)
                throw new InvalidOperationException("Run alternatives FBX or the Iter1791 Splat Avatar is missing.");

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
            var clips = LoadClips();
            if (clips.Length < 3)
                throw new InvalidOperationException($"Expected three Meshy run alternatives; imported {clips.Length}.");
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Debug.Log($"[Splat run alternatives] clip={clip.name}, duration={clip.length:F3}s, curveBindings={bindings.Length}.");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void OverrideRunForCapture(Animator animator, string requestedClipName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("The Splat Meshy Animator/controller is missing.");
            var clips = LoadClips();
            var newRun = clips.FirstOrDefault(clip => clip.name.IndexOf(requestedClipName,
                StringComparison.OrdinalIgnoreCase) >= 0);
            if (newRun == null)
                throw new InvalidOperationException($"Could not find Meshy run clip containing '{requestedClipName}'. Imported: {string.Join(", ", clips.Select(clip => clip.name))}");

            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController)
            {
                name = "Iter1835 Meshy run candidate capture-only override",
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
            Debug.Log($"[Splat run alternatives] capture-only override: {source.name} -> {newRun.name} ({newRun.length:F2}s); saved controller unchanged.");
        }

        private static AnimationClip[] LoadClips() => AssetDatabase.LoadAllAssetsAtPath(ClipPath)
            .OfType<AnimationClip>().Where(clip => clip != null && clip.length > 0.01f).ToArray();
    }
}
#endif
