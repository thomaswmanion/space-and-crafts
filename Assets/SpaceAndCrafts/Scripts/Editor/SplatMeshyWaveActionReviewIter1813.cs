#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Imports and temporarily previews Meshy's Wave One Hand on Splat's existing rig.</summary>
    public static class SplatMeshyWaveActionReviewIter1813
    {
        public const string ClipPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1813/splat_wave_one_hand.fbx";
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/rigged_character.fbx";
        private const string ControllerPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/ReviewOnly/Splat_Meshy_Iter1791_ReviewOnly.controller";

        [MenuItem("Space & Crafts/Prepare Iter1813 Splat Wave Review")]
        public static void PrepareClip()
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (importer == null || avatar == null)
                throw new System.InvalidOperationException("Wave FBX or approved Splat Avatar is missing.");

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var clips = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            if (clips.Length == 0)
                throw new System.InvalidOperationException("Meshy Wave FBX imported without an AnimationClip.");
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Debug.Log($"[Splat wave review] imported={clip.name}, duration={clip.length:F2}s, curveBindings={bindings.Length}, rootMotion=false.");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void OverrideFriendlyHelloForCapture(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new System.InvalidOperationException("The active Splat Animator/controller is missing.");
            var wave = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && clip.length > 0.01f);
            if (wave == null)
                throw new System.InvalidOperationException("The imported Meshy Wave One Hand clip is missing.");

            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController)
            {
                name = "Iter1813 Wave One Hand capture-only override",
                hideFlags = HideFlags.HideAndDontSave,
            };
            var clipOverrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(clipOverrides);
            var found = false;
            for (var i = 0; i < clipOverrides.Count; i++)
            {
                if (clipOverrides[i].Key == null || clipOverrides[i].Key.name != "Idle_15") continue;
                clipOverrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(
                    clipOverrides[i].Key, wave);
                found = true;
                break;
            }
            if (!found)
            {
                Object.DestroyImmediate(overrideController);
                throw new System.InvalidOperationException("Could not locate the Friendly Hello source clip in the active controller.");
            }

            overrideController.ApplyOverrides(clipOverrides);
            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;
            animator.Rebind();
            Debug.Log($"[Splat wave review] temporary capture override uses {wave.name} ({wave.length:F2}s) for Friendly Hello; the saved player/controller were not changed.");
        }

        [MenuItem("Space & Crafts/Promote Iter1813 Splat Wave One Hand")]
        public static void PromoteWaveToFriendlyHello()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var wave = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && clip.length > 0.01f);
            if (controller == null || wave == null)
                throw new System.InvalidOperationException("The saved Splat controller or reviewed Meshy wave clip is missing.");
            var state = controller.layers[0].stateMachine.states
                .Select(child => child.state).FirstOrDefault(candidate => candidate != null && candidate.name == "Friendly Hello");
            if (state == null)
                throw new System.InvalidOperationException("The saved Splat controller has no Friendly Hello state.");
            state.motion = wave;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Splat wave review] Promoted {wave.name} ({wave.length:F2}s) into the existing Friendly Hello state. Existing state transitions and player rig references were retained.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
#endif
