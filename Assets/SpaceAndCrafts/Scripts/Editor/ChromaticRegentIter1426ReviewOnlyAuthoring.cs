#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports the already-authored Iter1426 Regent hit revision as an isolated
    /// Unity review prefab. It never edits the Arrival scene or production prefab.
    /// </summary>
    public static class ChromaticRegentIter1426ReviewOnlyAuthoring
    {
        private const string SourceFbx =
            "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1426/ChromaticRegent_AnimationBundle.fbx";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1426/UnityCandidate_ReviewOnly";
        private const string ControllerPath = OutputFolder + "/ChromaticRegent_Iter1426_ReviewOnly.controller";
        private const string PrefabPath = OutputFolder + "/ChromaticRegent_Iter1426_ReviewOnly.prefab";
        private const string ReportPath = OutputFolder + "/review-report.txt";

        [MenuItem("Space & Crafts/Author Chromatic Regent Iter1426 ReviewOnly")]
        public static void BuildReviewOnly()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null
                || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                throw new InvalidOperationException("Iter1426 ReviewOnly output already exists; refusing to overwrite it.");

            var importer = AssetImporter.GetAtPath(SourceFbx) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing model importer for {SourceFbx}");

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var sourceClips = importer.defaultClipAnimations;
            for (var i = 0; i < sourceClips.Length; i++)
            {
                var clipName = sourceClips[i].name;
                var looping = Contains(clipName, "Idle") || Contains(clipName, "Walk") || Contains(clipName, "Run");
                sourceClips[i].loopTime = looping;
                sourceClips[i].loopPose = looping;
            }
            importer.clipAnimations = sourceClips;
            importer.SaveAndReimport();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            if (source == null)
                throw new InvalidOperationException($"Missing source GameObject: {SourceFbx}");

            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbx)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(SourceFbx)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Unity did not create a valid Generic Avatar for Iter1426.");

            var idle = FindClip(clips, "Idle");
            var walk = FindClip(clips, "Walk");
            var run = FindClip(clips, "Run");
            var attack = FindClip(clips, "Attack");
            var hit = FindClip(clips, "Hit");
            var knockdown = FindClip(clips, "Knockdown");
            if (idle == null || walk == null || run == null || attack == null || hit == null || knockdown == null)
                throw new InvalidOperationException("Missing one or more Idle/Walk/Run/Attack/Hit/Knockdown clips.");

            EnsureFolder(OutputFolder);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, new Vector3(100, 0));
            var walkState = AddState(machine, "Walk", walk, new Vector3(300, 0));
            var runState = AddState(machine, "Run", run, new Vector3(500, 0));
            var attackState = AddState(machine, "Attack", attack, new Vector3(700, 0));
            var hitState = AddState(machine, "Hit", hit, new Vector3(700, 120));
            var downState = AddState(machine, "Knockdown", knockdown, new Vector3(700, 240));
            machine.defaultState = idleState;
            AddFloatTransition(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater, 0.12f);
            AddFloatTransition(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less, 0.12f);
            AddFloatTransition(walkState, runState, "MoveSpeed", 1.1f, AnimatorConditionMode.Greater, 0.12f);
            AddFloatTransition(runState, walkState, "MoveSpeed", 1.1f, AnimatorConditionMode.Less, 0.12f);
            AddTriggerTransition(machine, attackState, "Attack", 0.08f);
            AddTriggerTransition(machine, hitState, "Hit", 0.05f);
            AddTriggerTransition(machine, downState, "Defeat", 0.06f);
            AddExitToIdle(attackState, idleState, 0.92f, 0.10f);
            AddExitToIdle(hitState, idleState, 0.80f, 0.08f);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate Iter1426 FBX.");
            instance.name = "ChromaticRegent_Iter1426_ReviewOnly";
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath,
                "ReviewOnly authoring record\n" +
                $"Source FBX: {SourceFbx}\nPrefab: {PrefabPath}\nController: {ControllerPath}\n" +
                $"Valid Avatar: {avatar.name}\nClips: {string.Join(", ", clips.Select(clip => clip.name))}\n" +
                "This candidate is isolated: no production prefab or scene was modified.\n");
            AssetDatabase.Refresh();
            Debug.Log($"[Regent Iter1426 ReviewOnly] created {PrefabPath} with a valid Avatar and Idle/Walk/Run/Attack/Hit/Knockdown states. Production scene unchanged.");
        }

        private static bool Contains(string value, string token) =>
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static AnimationClip FindClip(IEnumerable<AnimationClip> clips, string token) =>
            clips.FirstOrDefault(clip => Contains(clip.name, token));

        private static AnimatorState AddState(AnimatorStateMachine machine, string name,
            AnimationClip clip, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = clip;
            return state;
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine,
            AnimatorState destination, string parameter, float duration)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
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

        private static void AddExitToIdle(AnimatorState source, AnimatorState idle,
            float exitTime, float duration)
        {
            var transition = source.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
