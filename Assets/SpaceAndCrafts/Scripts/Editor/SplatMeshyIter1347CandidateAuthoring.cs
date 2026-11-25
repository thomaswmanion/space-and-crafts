#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds a separate Splat preview candidate from the locally reweighted
    /// Meshy FBX and locally authored joint-rotation clips. It does not change
    /// the active player prefab, scene, or controller.
    /// </summary>
    public static class SplatMeshyIter1347CandidateAuthoring
    {
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1347/Splat_Meshy_Iter1337_FaceSeated.fbx";
        private const string ClipRoot =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1346";
        private const string MaterialPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Materials/Splat_Meshy_Iter1337_URP.mat";
        private const string ControllerPath =
            "Assets/SpaceAndCrafts/Resources/Characters/SplatMeshyIter1347FaceSeated.controller";
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_Meshy_Iter1347_FaceSeated_Candidate.prefab";

        [MenuItem("Space & Crafts/Author Splat Meshy Iteration 1347 Face Seated Candidate")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(RigPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Reweighted Splat FBX did not import through ModelImporter.");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid)
                throw new InvalidOperationException("The reweighted Splat FBX did not produce a valid Generic Avatar.");
            var clips = new
            {
                Idle = LoadClip("Joint_Idle"),
                Walk = LoadClip("Joint_Walk"),
                Run = LoadClip("Joint_Run"),
                Jump = LoadClip("Joint_Regular_Jump"),
                Land = LoadClip("Joint_Land"),
            };
            var controller = CreateController(clips.Idle, clips.Walk, clips.Run, clips.Jump, clips.Land);
            CreatePrefab(avatar, controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Created separate Iteration 1347 face-seated Splat candidate. Active player/scenes unchanged.");
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk,
            AnimationClip run, AnimationClip jump, AnimationClip land)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var blend = new BlendTree
            {
                name = "Splat Meshy Joint Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(idle, 0f);
            blend.AddChild(walk, 0.45f);
            blend.AddChild(run, 2f);
            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = blend;
            machine.defaultState = locomotion;
            var jumpState = machine.AddState("Jump");
            jumpState.motion = jump;
            var landState = machine.AddState("Landing");
            landState.motion = land;

            var toJump = machine.AddAnyStateTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0.08f;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var toLand = jumpState.AddTransition(landState);
            toLand.hasExitTime = false;
            toLand.duration = 0.08f;
            toLand.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            var recover = landState.AddTransition(locomotion);
            recover.hasExitTime = true;
            recover.exitTime = 0.95f;
            recover.duration = 0.09f;
            return controller;
        }

        private static void CreatePrefab(Avatar avatar, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (model == null) throw new InvalidOperationException("The reweighted Splat FBX did not import as a model prefab.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Splat_Meshy_Iter1347_FaceSeated_Candidate";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException("The reweighted model has no renderers.");
            }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var fit = 1.5f / Mathf.Max(0.01f, bounds.size.y);
            instance.transform.localScale = Vector3.one * fit;
            instance.transform.localPosition = new Vector3(
                -bounds.center.x * fit,
                -bounds.min.y * fit,
                -bounds.center.z * fit);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new InvalidOperationException("Missing Splat URP material: " + MaterialPath);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0) materials = new[] { material };
                else for (var index = 0; index < materials.Length; index++) materials[index] = material;
                renderer.sharedMaterials = materials;
            }
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (instance.GetComponent<SplatMeshyAnimatorDriver>() == null)
                instance.AddComponent<SplatMeshyAnimatorDriver>();
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static AnimationClip LoadClip(string name)
        {
            var path = ClipRoot + "/" + name + ".anim";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path)
                ?? throw new InvalidOperationException("Missing locally authored clip: " + path);
        }
    }
}
#endif
