#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds an isolated Unity prefab/controller from the already-commissioned
    /// Chromatic Regent FBX. It intentionally does not modify any game scene.
    /// </summary>
    public static class ChromaticRegentCandidateBuilder
    {
        private const string SourceFbx =
            "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/ChromaticRegent_AnimationBundle.fbx";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/UnityCandidate";
        private const string ControllerPath = OutputFolder + "/ChromaticRegent.controller";
        private const string PrefabPath = OutputFolder + "/ChromaticRegent_AnimatedCandidate.prefab";

        [MenuItem("Space & Crafts/Build Isolated Chromatic Regent Animation Candidate")]
        public static void Build()
        {
            var importer = AssetImporter.GetAtPath(SourceFbx) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing model importer for: {SourceFbx}");

            var importerChanged = false;
            if (importer.animationType != ModelImporterAnimationType.Generic
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || !importer.importAnimation)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importerChanged = true;
            }

            var sourceClips = importer.defaultClipAnimations;
            for (var i = 0; i < sourceClips.Length; i++)
            {
                var name = sourceClips[i].name;
                var shouldLoop = name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0;
                if (sourceClips[i].loopTime != shouldLoop || sourceClips[i].loopPose != shouldLoop)
                {
                    sourceClips[i].loopTime = shouldLoop;
                    sourceClips[i].loopPose = shouldLoop;
                    importerChanged = true;
                }
            }
            if (importerChanged)
            {
                importer.clipAnimations = sourceClips;
                importer.SaveAndReimport();
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            if (source == null)
                throw new InvalidOperationException($"Missing commissioned FBX model: {SourceFbx}");

            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbx)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Debug.Log("[Regent candidate] imported clips: " + string.Join(", ", clips.Select(clip => clip.name)));
            var avatar = AssetDatabase.LoadAllAssetsAtPath(SourceFbx)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Unity did not create a valid Generic Avatar for the Regent rig.");
            Debug.Log($"[Regent candidate] valid Generic Avatar: {avatar.name}");

            var idle = FindClip(clips, "Idle");
            var attack = FindClip(clips, "Attack");
            var hit = FindClip(clips, "Hit");
            var defeat = FindClip(clips, "Knockdown");
            var walk = FindClip(clips, "Walk");
            var run = FindClip(clips, "Run");
            if (idle == null || walk == null || run == null || attack == null || hit == null || defeat == null)
                throw new InvalidOperationException("Required Regent Idle/Walk/Run/Attack/Hit/Knockdown clips were not imported.");

            EnsureFolder(OutputFolder);
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.DeleteAsset(PrefabPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            var stateMachine = controller.layers[0].stateMachine;
            stateMachine.states = Array.Empty<ChildAnimatorState>();
            stateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();

            var idleState = AddState(stateMachine, "Idle", idle, new Vector3(250, 60));
            var walkState = AddState(stateMachine, "Walk", walk, new Vector3(250, 180));
            var runState = AddState(stateMachine, "Run", run, new Vector3(250, 300));
            var attackState = AddState(stateMachine, "Attack", attack, new Vector3(500, 0));
            var hitState = AddState(stateMachine, "Hit", hit, new Vector3(500, 120));
            var defeatState = AddState(stateMachine, "Knockdown", defeat, new Vector3(500, 240));
            stateMachine.defaultState = idleState;

            AddFloatTransition(idleState, walkState, "MoveSpeed", 0.1f,
                AnimatorConditionMode.Greater, 0.12f);
            AddFloatTransition(walkState, idleState, "MoveSpeed", 0.1f,
                AnimatorConditionMode.Less, 0.12f);
            AddFloatTransition(walkState, runState, "MoveSpeed", 1.1f,
                AnimatorConditionMode.Greater, 0.12f);
            AddFloatTransition(runState, walkState, "MoveSpeed", 1.1f,
                AnimatorConditionMode.Less, 0.12f);
            AddTriggerTransition(stateMachine, attackState, "Attack", 0.08f);
            AddTriggerTransition(stateMachine, hitState, "Hit", 0.05f);
            AddTriggerTransition(stateMachine, defeatState, "Defeat", 0.06f);
            AddExitToIdle(attackState, idleState, 0.92f, 0.10f);
            AddExitToIdle(hitState, idleState, 0.80f, 0.08f);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate the imported Regent FBX.");
            instance.name = "ChromaticRegent_AnimatedCandidate";
            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Regent candidate] created isolated prefab={PrefabPath}; controller={ControllerPath}; states=Idle/Walk/Run/Attack/Hit/Knockdown. Scene files unchanged.");
        }

        private static AnimationClip FindClip(IEnumerable<AnimationClip> clips, string token)
        {
            var available = clips.Where(clip => clip != null).ToArray();
            var exactRigClip = "|ChromaticRegent_" + token;
            var exact = available.FirstOrDefault(clip =>
                clip.name.EndsWith(exactRigClip, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            // Some deliveries contain both a generic BeHit_FlyUp and the
            // Regent-authored Hit motion. Prefer the named rig clip above;
            // retain token fallback for older bundles with different naming.
            return available.FirstOrDefault(clip =>
                clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name,
            AnimationClip clip, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState destination,
            string parameter, float duration)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitToIdle(AnimatorState source, AnimatorState idle,
            float exitTime, float duration)
        {
            var transition = source.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
        }

        private static void AddFloatTransition(AnimatorState source, AnimatorState destination,
            string parameter, float threshold, AnimatorConditionMode mode, float duration)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
