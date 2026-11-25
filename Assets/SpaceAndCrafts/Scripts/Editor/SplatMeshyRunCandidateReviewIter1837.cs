#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Capture-only import/override for the Iter1837 planted run on Splat's Iter1836 rig.</summary>
    public static class SplatMeshyRunCandidateReviewIter1837
    {
        public const string ClipName = "__iter1837_planted_run__";
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat";
        private const string CandidateFolder = Folder + "/MeshyImageCandidate_Iter1836_ReviewOnly";
        private const string RigPath = CandidateFolder + "/Rigged_Character_Iter1836.fbx";
        private const string PrefabPath = CandidateFolder + "/Splat_Iter1836_ReviewOnly.prefab";
        private const string RunPath = Folder + "/MeshyActions_Iter1837_ReviewOnly/Splat_Run_InPlace_Planted_Iter1837.fbx";

        [MenuItem("Space & Crafts/Prepare Iter1837 Planted Run on Splat Iter1836 Rig")]
        public static void PrepareClip()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var rigImporter = AssetImporter.GetAtPath(RigPath) as ModelImporter;
            var runImporter = AssetImporter.GetAtPath(RunPath) as ModelImporter;
            if (rigImporter == null || runImporter == null)
                throw new InvalidOperationException("Iter1836 rig or Iter1837 planted-run FBX is missing.");

            if (rigImporter.animationType != ModelImporterAnimationType.Generic
                || rigImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || rigImporter.optimizeGameObjects)
            {
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = true;
                rigImporter.optimizeGameObjects = false;
                rigImporter.importCameras = false;
                rigImporter.importLights = false;
                rigImporter.SaveAndReimport();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Iter1836 rig has no valid Generic Avatar.");

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (rigPrefab != null)
            {
                var rigBones = rigPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(skin => skin != null)
                    .SelectMany(skin => skin.bones ?? Array.Empty<Transform>())
                    .Where(bone => bone != null)
                    .Select(bone => bone.name)
                    .Distinct()
                    .ToArray();
                Debug.Log("[Splat Iter1837] Iter1836 rig bone names: " + string.Join(", ", rigBones));
            }

            runImporter.animationType = ModelImporterAnimationType.Generic;
            runImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            runImporter.sourceAvatar = avatar;
            runImporter.importAnimation = true;
            runImporter.optimizeGameObjects = false;
            runImporter.importCameras = false;
            runImporter.importLights = false;
            runImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            var importedClips = runImporter.clipAnimations;
            if (importedClips == null || importedClips.Length == 0)
                importedClips = runImporter.defaultClipAnimations;
            foreach (var clip in importedClips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
            }
            runImporter.clipAnimations = importedClips;
            runImporter.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var clips = LoadClips();
            if (clips.Length == 0)
                throw new InvalidOperationException("Iter1837 FBX imported without animation clips.");
            foreach (var clip in clips)
            {
                Debug.Log($"[Splat Iter1837] clip={clip.name}, duration={clip.length:F3}s, bindings={AnimationUtility.GetCurveBindings(clip).Length}, loop={AnimationUtility.GetAnimationClipSettings(clip).loopTime}.");
            }
            Debug.Log("[Splat Iter1837] Run clip is imported onto the Iter1836 rig only; no production prefab or scene changed.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void ReplacePlayerVisualForCapture(GameObject playerRoot)
        {
            if (playerRoot == null) throw new InvalidOperationException("Splat_TEMP root is missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("The Iter1836 ReviewOnly prefab must be prepared before capture.");

            var oldVisual = playerRoot.transform.Find(SplatMeshyVisualMode.MeshyVisualRootName);
            if (oldVisual != null)
            {
                // Hide it from SplatMeshyVisualMode.ResolveVisual before Unity's
                // deferred Destroy completes; otherwise the review component can
                // bind to this inactive production rig for one capture frame.
                oldVisual.name = "ART_MeshySplatVisual_Iter1837_Replaced";
                oldVisual.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(oldVisual.gameObject);
            }

            var candidate = UnityEngine.Object.Instantiate(prefab, playerRoot.transform, false);
            candidate.name = SplatMeshyVisualMode.MeshyVisualRootName;
            var animator = candidate.GetComponent<Animator>() ?? candidate.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("The Iter1836 candidate prefab has no Animator.");
            animator.enabled = true;
            animator.applyRootMotion = false;
            var driver = candidate.GetComponent<SplatMeshyAnimatorDriver>()
                ?? candidate.gameObject.AddComponent<SplatMeshyAnimatorDriver>();
            driver.enabled = true;
            animator.Rebind();
            Debug.Log("[Splat Iter1837] Replaced only the in-memory Arrival capture's Meshy visual with Iter1836 ReviewOnly prefab; Splat_TEMP motor/capsule retained.");
        }

        public static void OverrideRunForCapture(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Iter1836 candidate Animator/controller is missing.");
            var candidateClip = LoadClips().FirstOrDefault();
            if (candidateClip == null) throw new InvalidOperationException("Iter1837 run clip is not imported.");

            var controller = new AnimatorOverrideController(animator.runtimeAnimatorController)
            {
                name = "Iter1837 planted run capture-only override",
                hideFlags = HideFlags.HideAndDontSave,
            };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            var index = overrides.FindIndex(pair => pair.Key != null
                && (pair.Key.name.IndexOf("running", StringComparison.OrdinalIgnoreCase) >= 0
                    || pair.Key.name.Equals("Run", StringComparison.OrdinalIgnoreCase)));
            if (index < 0)
            {
                UnityEngine.Object.DestroyImmediate(controller);
                throw new InvalidOperationException("Could not find Iter1836 locomotion Run clip in the controller.");
            }
            var source = overrides[index].Key;
            overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(source, candidateClip);
            controller.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.Rebind();
            Debug.Log($"[Splat Iter1837] capture-only run override: {source.name} -> {candidateClip.name} ({candidateClip.length:F2}s).");
        }

        private static AnimationClip[] LoadClips() => AssetDatabase.LoadAllAssetsAtPath(RunPath)
            .OfType<AnimationClip>().Where(clip => clip != null && clip.length > 0.01f).ToArray();
    }
}
#endif
