#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Creates a review-only Sproutlet prefab/controller from the existing
    /// same-hierarchy Generic Meshy rig and clips. Does not edit a game scene.
    /// </summary>
    public static class BuildSproutletReviewPrefabIter2228
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001";
        private const string Review = Root + "/Review";
        private const string RigPath = Root + "/Sproutlet_Rigged.fbx";
        private const string ActionPath = Root + "/Sproutlet_Actions.fbx";
        private const string WalkPath = Root + "/Sproutlet_Walk.fbx";
        private const string RunPath = Root + "/Sproutlet_Run.fbx";
        private const string ControllerPath = Review + "/Sproutlet_Review_Iter2228.controller";
        private const string PrefabPath = Review + "/Sproutlet_Review_Iter2228.prefab";

        [MenuItem("Space & Crafts/Assets/Build Sproutlet Review Prefab Iter2228")]
        public static void Build()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) != null
                || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing Sproutlet Iter2228 review assets.");

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (model == null) throw new InvalidOperationException("Missing Sproutlet rigged FBX: " + RigPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Listen", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            var idleClip = FindClip(ActionPath, "idle");
            var listenClip = FindClip(ActionPath, "listening_gesture");
            var jumpClip = FindClip(ActionPath, "happy_jump");
            var hitClip = FindClip(ActionPath, "hit_reaction");
            var walkClip = FindClip(WalkPath, "walking_man");
            var runClip = FindClip(RunPath, "running");
            var missing = new[] { idleClip, listenClip, jumpClip, hitClip, walkClip, runClip }
                .Count(clip => clip == null);
            if (missing != 0) throw new InvalidOperationException($"Sproutlet clips missing: {missing}; controller was not completed.");

            var idle = machine.AddState("Idle");
            idle.motion = idleClip;
            machine.defaultState = idle;

            var locomotionTree = new BlendTree
            {
                name = "Sproutlet Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false
            };
            locomotionTree.AddChild(walkClip, 0.22f);
            locomotionTree.AddChild(runClip, 0.82f);
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            var locomotion = machine.AddState("Walk Run");
            locomotion.motion = locomotionTree;

            var idleToMove = idle.AddTransition(locomotion);
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.12f;
            idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.10f, "MoveSpeed");
            var moveToIdle = locomotion.AddTransition(idle);
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.16f;
            moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "MoveSpeed");

            AddOneShot(machine, idle, "Listening", listenClip, "Listen");
            AddOneShot(machine, idle, "Happy Jump", jumpClip, "Jump");
            AddOneShot(machine, idle, "Hit Reaction", hitClip, "Hit");

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Sproutlet rigged FBX.");
            instance.name = "Sproutlet_Review_Iter2228";
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out var saved);
            UnityEngine.Object.DestroyImmediate(instance);
            if (!saved || prefab == null) throw new InvalidOperationException("Failed to save Sproutlet review prefab.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SproutletReview2228] Created review-only prefab {PrefabPath}; controller={ControllerPath}; Generic same-path motions (Idle, Listen, Jump, Hit, Walk, Run); no scene edited.");
        }

        private static AnimationClip FindClip(string assetPath, string fragment)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null
                    && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)
                    && clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AddOneShot(AnimatorStateMachine machine, AnimatorState idle,
            string name, AnimationClip clip, string trigger)
        {
            var state = machine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = false;

            var enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.10f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            var exit = state.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 0.96f;
            exit.duration = 0.16f;
        }
    }
}
#endif
