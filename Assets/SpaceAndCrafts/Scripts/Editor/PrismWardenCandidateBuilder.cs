#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports the Meshy Prism Warden into a review-only, genuinely rigged Unity prefab.
    /// It does not modify any scene or production enemy prefab.
    /// </summary>
    public static class PrismWardenCandidateBuilder
    {
        private const string Root =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/Iter1867_PrismWarden";
        private const string RigPath = Root + "/Rigged/PrismWarden_Rigged.fbx";
        private const string WalkPath = Root + "/Rigged/PrismWarden_Walk.fbx";
        private const string RunPath = Root + "/Rigged/PrismWarden_Run.fbx";
        private const string ActionsPath = Root + "/Rigged/PrismWarden_Actions.fbx";
        private const string TexturePath = Root + "/PrismWarden_smart30k_base_color.png";
        private const string Output = Root + "/UnityCandidate";
        private const string ControllerPath = Output + "/PrismWarden_ReviewOnly.controller";
        private const string PrefabPath = Output + "/PrismWarden_ReviewOnly.prefab";
        private const string MaterialPath = Output + "/PrismWarden_ReviewOnly.mat";

        [MenuItem("Space & Crafts/Build Prism Warden Review Candidate (Iter1867)")]
        public static void Build()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var path in new[] { RigPath, WalkPath, RunPath, ActionsPath, TexturePath })
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        throw new InvalidOperationException("Missing Iter1867 asset: " + path);

                var rigImporter = RequireModelImporter(RigPath);
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = false;
                rigImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                    .FirstOrDefault(candidate => candidate != null && candidate.isValid);
                if (avatar == null) throw new InvalidOperationException("Meshy rig did not import as a valid Generic Avatar.");

                foreach (var path in new[] { WalkPath, RunPath, ActionsPath })
                {
                    var importer = RequireModelImporter(path);
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.importAnimation = true;
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        var looping = clip.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0
                            || clip.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0
                            || clip.name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0;
                        clip.loopTime = looping;
                        clip.loopPose = looping;
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var allClips = new[] { WalkPath, RunPath, ActionsPath }
                    .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(clip => clip.name, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First()).ToArray();

                var idle = FindClip(allClips, "Idle_10") ?? FindClip(allClips, "Idle");
                var walk = FindClip(allClips, "walking");
                var run = FindClip(allClips, "running");
                var telegraph = FindClip(allClips, "Charged_Spell_Cast");
                var attack = FindClip(allClips, "Charged_Ground_Slam");
                var hit = FindClip(allClips, "Hit_Reaction_1") ?? FindClip(allClips, "Hit_Reaction");
                var knockdown = FindClip(allClips, "Knock_Down");
                var recovery = FindClip(allClips, "Rightward_Spin");
                var defeat = FindClip(allClips, "Dead");
                var required = new[] { idle, walk, run, telegraph, attack, hit, knockdown, recovery, defeat };
                if (required.Any(clip => clip == null))
                    throw new InvalidOperationException("Required Meshy clips were not found. Imported: "
                        + string.Join(", ", allClips.Select(clip => clip.name)));

                EnsureFolder(Output);
                AssetDatabase.DeleteAsset(ControllerPath);
                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.DeleteAsset(MaterialPath);

                var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                foreach (var parameter in new[] { "MoveSpeed", "WalkRate" })
                    controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
                foreach (var parameter in new[] { "Telegraph", "Attack", "Hit", "Knockdown", "Recover", "Defeat" })
                    controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);

                var machine = controller.layers[0].stateMachine;
                var idleState = AddState(machine, "Idle", idle, new Vector3(250, 30));
                var locomotionState = machine.AddState("Locomotion", new Vector3(250, 155));
                var locomotion = new BlendTree
                {
                    name = "PrismWarden_WalkRun_ReviewOnly",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = "MoveSpeed",
                    useAutomaticThresholds = false
                };
                locomotion.AddChild(walk, 0.25f);
                locomotion.AddChild(run, 1.0f);
                AssetDatabase.AddObjectToAsset(locomotion, controller);
                locomotionState.motion = locomotion;
                locomotionState.speedParameterActive = true;
                locomotionState.speedParameter = "WalkRate";
                var telegraphState = AddState(machine, "Telegraph", telegraph, new Vector3(530, -125));
                var attackState = AddState(machine, "Attack", attack, new Vector3(750, -125));
                var hitState = AddState(machine, "Hit", hit, new Vector3(530, 30));
                var knockdownState = AddState(machine, "Knockdown", knockdown, new Vector3(530, 185));
                var recoveryState = AddState(machine, "Recovery", recovery, new Vector3(750, 185));
                var defeatState = AddState(machine, "Defeat", defeat, new Vector3(530, 340));
                machine.defaultState = idleState;

                AddFloatTransition(idleState, locomotionState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
                AddFloatTransition(locomotionState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
                AddTriggerTransition(machine, telegraphState, "Telegraph");
                AddTriggerTransition(machine, attackState, "Attack");
                AddTriggerTransition(machine, hitState, "Hit");
                AddTriggerTransition(machine, knockdownState, "Knockdown");
                AddTriggerTransition(machine, recoveryState, "Recover");
                AddTriggerTransition(machine, defeatState, "Defeat");
                AddExitToIdle(telegraphState, idleState, .9f);
                AddExitToIdle(attackState, idleState, .9f);
                AddExitToIdle(hitState, idleState, .85f);
                AddExitToIdle(knockdownState, idleState, .95f);
                AddExitToIdle(recoveryState, idleState, .95f);
                EditorUtility.SetDirty(controller);

                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("No supported lit shader was found.");
                var material = new Material(shader) { name = "PrismWarden_MeshyN1_ReviewOnly" };
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .04f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .26f);
                AssetDatabase.CreateAsset(material, MaterialPath);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                if (model == null) throw new InvalidOperationException("Rigged FBX did not import as a model prefab.");
                var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (visual == null) throw new InvalidOperationException("Could not instantiate the rigged character.");
                visual.name = "ART_MeshyEnemyVisual";
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers) renderer.sharedMaterial = material;
                var animator = visual.GetComponentInChildren<Animator>(true) ?? visual.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                visual.AddComponent<SpaceAndCrafts.AI.PrismWardenAnimatorDriver>();
                if (PrefabUtility.SaveAsPrefabAsset(visual, PrefabPath) == null)
                    throw new InvalidOperationException("Could not save review-only prefab.");
                UnityEngine.Object.DestroyImmediate(visual);
                AssetDatabase.SaveAssets();
                Debug.Log("[Prism Warden] review-only prefab/controller saved. Generic Avatar valid="
                    + avatar.isValid + "; imported clips=" + string.Join(", ", allClips.Select(clip => clip.name))
                    + "; scene and production enemies unchanged.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static ModelImporter RequireModelImporter(string path) =>
            AssetImporter.GetAtPath(path) as ModelImporter
            ?? throw new InvalidOperationException("Missing FBX model importer: " + path);

        private static AnimationClip FindClip(AnimationClip[] clips, string token) =>
            clips.FirstOrDefault(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

        private static AnimatorState AddState(AnimatorStateMachine machine, string name,
            AnimationClip clip, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddFloatTransition(AnimatorState source, AnimatorState target,
            string parameter, float threshold, AnimatorConditionMode mode)
        {
            var transition = source.AddTransition(target);
            transition.hasExitTime = false;
            transition.duration = .12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState target,
            string parameter)
        {
            var transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = .08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitToIdle(AnimatorState source, AnimatorState idle, float exitTime)
        {
            var transition = source.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = .1f;
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
