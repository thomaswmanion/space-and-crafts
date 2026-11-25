#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Creates a versioned, isolated Iter1426 prefab wired to the exact authored Regent Hit clip.</summary>
    public static class ChromaticRegentIter1426ExactHitReviewOnlyAuthoringIter1770
    {
        private const string SourceFbx = "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1426/ChromaticRegent_AnimationBundle.fbx";
        private const string OutputFolder = "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1426/UnityCandidate_ReviewOnly_Iter1770_ExactHit";
        private const string ControllerPath = OutputFolder + "/ChromaticRegent_Iter1426_ExactHit_ReviewOnly.controller";
        private const string PrefabPath = OutputFolder + "/ChromaticRegent_Iter1426_ExactHit_ReviewOnly.prefab";
        private const string ReportPath = OutputFolder + "/review-report.txt";

        [MenuItem("Space & Crafts/Author Regent Iter1770 Exact Hit ReviewOnly")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                throw new InvalidOperationException("Iter1770 exact-Hit ReviewOnly output already exists; refusing to overwrite it.");

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            if (source == null) throw new FileNotFoundException("Missing Iter1426 source FBX", SourceFbx);
            var assets = AssetDatabase.LoadAllAssetsAtPath(SourceFbx);
            var avatar = assets.OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
            if (avatar == null) throw new InvalidOperationException("Iter1426 Generic Avatar is missing or invalid.");
            var clips = assets.OfType<AnimationClip>().Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            AnimationClip Exact(string suffix) => clips.FirstOrDefault(c => string.Equals(c.name, suffix, StringComparison.Ordinal) || c.name.EndsWith("|" + suffix, StringComparison.Ordinal));
            var idle = Exact("ChromaticRegent_Idle");
            var walk = Exact("ChromaticRegent_Walk");
            var run = Exact("ChromaticRegent_Run");
            var attack = Exact("ChromaticRegent_Attack");
            var hit = Exact("ChromaticRegent_Hit");
            var knockdown = Exact("ChromaticRegent_Knockdown");
            if (new[] { idle, walk, run, attack, hit, knockdown }.Any(c => c == null))
                throw new InvalidOperationException("Missing one or more exact ChromaticRegent_* source clips.");

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
            AddFloat(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloat(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddFloat(walkState, runState, "MoveSpeed", 1.1f, AnimatorConditionMode.Greater);
            AddFloat(runState, walkState, "MoveSpeed", 1.1f, AnimatorConditionMode.Less);
            AddTrigger(machine, attackState, "Attack", 0.08f);
            AddTrigger(machine, hitState, "Hit", 0.05f);
            AddTrigger(machine, downState, "Defeat", 0.06f);
            AddExit(attackState, idleState, 0.92f, 0.10f);
            AddExit(hitState, idleState, 0.80f, 0.08f);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Iter1426 FBX for review prefab.");
            try
            {
                instance.name = "ChromaticRegent_Iter1426_ExactHit_ReviewOnly";
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath,
                "Iter1770 Exact Hit ReviewOnly authoring record\n" +
                $"Source FBX: {SourceFbx}\nPrefab: {PrefabPath}\nController: {ControllerPath}\n" +
                $"Exact Hit state motion: {hit.name}\nValid Avatar: {avatar.name}\n" +
                "The Hit state explicitly references ChromaticRegent_Hit; all outputs are isolated ReviewOnly assets. Production prefab/scene untouched.\n");
            AssetDatabase.Refresh();
            Debug.Log($"[Regent Iter1770] authored isolated exact-hit prefab at {PrefabPath}; Hit motion={hit.name}; production untouched.");
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip, Vector3 position)
        { var s = machine.AddState(name, position); s.motion = clip; return s; }
        private static void AddTrigger(AnimatorStateMachine machine, AnimatorState destination, string parameter, float duration)
        { var t = machine.AddAnyStateTransition(destination); t.hasExitTime = false; t.hasFixedDuration = true; t.duration = duration; t.AddCondition(AnimatorConditionMode.If, 0f, parameter); }
        private static void AddFloat(AnimatorState source, AnimatorState destination, string parameter, float threshold, AnimatorConditionMode mode)
        { var t = source.AddTransition(destination); t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.12f; t.AddCondition(mode, threshold, parameter); }
        private static void AddExit(AnimatorState source, AnimatorState destination, float exitTime, float duration)
        { var t = source.AddTransition(destination); t.hasExitTime = true; t.exitTime = exitTime; t.hasFixedDuration = true; t.duration = duration; }
        private static void EnsureFolder(string path)
        { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
#endif
