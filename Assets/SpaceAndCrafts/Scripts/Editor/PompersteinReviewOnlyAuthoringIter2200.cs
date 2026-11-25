#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Builds an isolated Pomperstein import/animation review candidate; never edits production scenes.</summary>
    public static class PompersteinReviewOnlyAuthoringIter2200
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/Pomperstein/Meshy_Iter1997";
        private const string RigPath = Root + "/Pomperstein_Rigged.fbx";
        private const string ActionsPath = Root + "/Pomperstein_Actions.fbx";
        private const string WalkPath = Root + "/Pomperstein_Walk.fbx";
        private const string RunPath = Root + "/Pomperstein_Run.fbx";
        private const string TexturePath = Root + "/Pomperstein_base_color.png";
        private const string Output = Root + "/UnityCandidate_ReviewOnly_Iter2200";
        private const string ControllerPath = Output + "/Pomperstein_ReviewOnly.controller";
        private const string PrefabPath = Output + "/Pomperstein_ReviewOnly.prefab";
        private const string MaterialPath = Output + "/Pomperstein_ReviewOnly.mat";

        [MenuItem("Space & Crafts/Author Pomperstein ReviewOnly (Iter 2200)")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                throw new InvalidOperationException("Pomperstein Iter2200 review output already exists; refusing to overwrite.");

            Configure(RigPath, ModelImporterAvatarSetup.CreateFromThisModel, null);
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (rig == null) throw new FileNotFoundException("Missing Pomperstein rig FBX", RigPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null) throw new InvalidOperationException("Rig FBX did not produce a valid Avatar.");

            Configure(ActionsPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            Configure(WalkPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            Configure(RunPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            ConfigureLoop(ActionsPath, name => Has(name, "Idle"));
            ConfigureLoop(WalkPath, name => Has(name, "Walk"));
            ConfigureLoop(RunPath, name => Has(name, "Run"));

            var idle = RequireClip(ActionsPath, "Idle");
            var alert = RequireClip(ActionsPath, "Alert");
            var slam = RequireClip(ActionsPath, "Charged Ground Slam");
            var hit = RequireClip(ActionsPath, "Hit Reaction");
            var dead = RequireClip(ActionsPath, "Dead");
            var standUp = RequireClip(ActionsPath, "Stand Up 1");
            var walk = RequireClip(WalkPath, "Walk");
            var run = RequireClip(RunPath, "Run");
            var clips = new[] { idle, alert, slam, hit, dead, standUp, walk, run };

            EnsureFolder(Output);
            var report = new StringBuilder();
            report.AppendLine("Pomperstein Iteration 2200 isolated Unity import review.");
            report.AppendLine("Structural import/path compatibility is not proof of deformation, timing, contact, combat, or gameplay quality.");
            report.AppendLine($"Rig={RigPath}; Avatar={avatar.name}; valid={avatar.isValid}; human={avatar.isHuman}");
            report.AppendLine($"Transforms={rig.GetComponentsInChildren<Transform>(true).Length}; skinnedRenderers={rig.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
            foreach (var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"Skin {skin.name}: vertices={skin.sharedMesh?.vertexCount ?? 0}; bones={skin.bones?.Length ?? 0}");
            var rigPaths = rig.GetComponentsInChildren<Transform>(true)
                .Select(transform => AnimationUtility.CalculateTransformPath(transform, rig.transform)).ToHashSet(StringComparer.Ordinal);
            var exactAll = true;
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip).Where(binding => binding.type == typeof(Transform)).ToArray();
                var missing = bindings.Select(binding => binding.path).Distinct(StringComparer.Ordinal).Where(path => !rigPaths.Contains(path)).ToArray();
                var exact = bindings.Length > 0 && missing.Length == 0;
                exactAll &= exact;
                report.AppendLine($"Clip {clip.name}: duration={clip.length:F2}s; transformBindings={bindings.Length}; missingPaths={missing.Length}; exact={exact}");
                if (missing.Length > 0) report.AppendLine("  Missing: " + string.Join(", ", missing));
            }
            if (!exactAll)
            {
                File.WriteAllText(Output + "/import-path-review.txt", report + "STOP: binding paths do not match; no review prefab/controller created.\n", Encoding.UTF8);
                AssetDatabase.Refresh();
                throw new InvalidOperationException("Pomperstein clip/rig transform paths do not match exactly.");
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Slam", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Recover", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = State(machine, "Idle", idle, new Vector3(100, 0));
            var walkState = State(machine, "Walk", walk, new Vector3(320, 0));
            var runState = State(machine, "Run", run, new Vector3(540, 0));
            var alertState = State(machine, "Alert", alert, new Vector3(760, -160));
            var slamState = State(machine, "GroundSlam", slam, new Vector3(760, -40));
            var hitState = State(machine, "Hit", hit, new Vector3(760, 80));
            var deadState = State(machine, "Dead", dead, new Vector3(980, 200));
            var standState = State(machine, "StandUp", standUp, new Vector3(1200, 200));
            machine.defaultState = idleState;
            AddFloat(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloat(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddFloat(walkState, runState, "MoveSpeed", 1.25f, AnimatorConditionMode.Greater);
            AddFloat(runState, walkState, "MoveSpeed", 1.25f, AnimatorConditionMode.Less);
            AddTrigger(machine, alertState, "Alert");
            AddTrigger(machine, slamState, "Slam");
            AddTrigger(machine, hitState, "Hit");
            AddTrigger(machine, deadState, "Defeat");
            AddTrigger(deadState, standState, "Recover");
            AddExit(alertState, idleState, 0.92f);
            AddExit(slamState, idleState, 0.94f);
            AddExit(hitState, idleState, 0.9f);
            AddExit(standState, idleState, 0.9f);

            var material = CreateMaterial();
            var instance = PrefabUtility.InstantiatePrefab(rig) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Pomperstein review model.");
            try
            {
                instance.name = "Pomperstein_ReviewOnly_Iter2200";
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0) throw new InvalidOperationException("Rig has no skinned mesh renderer.");
                foreach (var skin in skins) skin.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }

            report.AppendLine($"Controller/prefab/material: {ControllerPath} / {PrefabPath} / {MaterialPath}");
            report.AppendLine("Review state graph includes the genuine Stand Up 1 clip after Dead; it does not author or claim enemy AI or an encounter. Production scenes untouched.");
            File.WriteAllText(Output + "/import-path-review.txt", report.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Pomperstein Iter2200] isolated Generic rig and action controller authored; no production scene modified.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Configure(string path, ModelImporterAvatarSetup setup, Avatar avatar)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing FBX importer", path);
            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic) { importer.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (importer.avatarSetup != setup) { importer.avatarSetup = setup; changed = true; }
            if (setup == ModelImporterAvatarSetup.CopyFromOther && importer.sourceAvatar != avatar) { importer.sourceAvatar = avatar; changed = true; }
            if (!importer.importAnimation) { importer.importAnimation = true; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static void ConfigureLoop(string path, Func<string, bool> shouldLoop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing clip FBX importer", path);
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            var changed = false;
            foreach (var clip in clips)
            {
                var loop = shouldLoop(clip.name);
                if (clip.loopTime != loop || clip.loopPose != loop) { clip.loopTime = loop; clip.loopPose = loop; changed = true; }
            }
            if (changed) { importer.clipAnimations = clips; importer.SaveAndReimport(); }
        }

        private static AnimationClip RequireClip(string path, string expected)
        {
            var available = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(candidate => candidate != null && !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var normalized = Normalize(expected);
            var clip = available.FirstOrDefault(candidate => Normalize(candidate.name).EndsWith(normalized, StringComparison.Ordinal))
                ?? available.OrderBy(candidate => candidate.name.Length).FirstOrDefault(candidate => Has(candidate.name, expected));
            if (clip == null) throw new InvalidOperationException("Missing clip containing '" + expected + "' at " + path + ". Found: " + string.Join(", ", available.Select(item => item.name)));
            if (clip.length <= 0f || AnimationUtility.GetCurveBindings(clip).Length == 0)
                throw new InvalidOperationException("Clip is empty: " + clip.name);
            return clip;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            var state = machine.AddState(name, position); state.motion = motion; state.writeDefaultValues = true; return state;
        }
        private static void AddTrigger(AnimatorStateMachine machine, AnimatorState target, string parameter)
        {
            var transition = machine.AddAnyStateTransition(target); transition.hasExitTime = false; transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }
        private static void AddTrigger(AnimatorState source, AnimatorState target, string parameter)
        {
            var transition = source.AddTransition(target); transition.hasExitTime = false; transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }
        private static void AddFloat(AnimatorState source, AnimatorState target, string parameter, float threshold, AnimatorConditionMode mode)
        {
            var transition = source.AddTransition(target); transition.hasExitTime = false; transition.duration = 0.14f;
            transition.AddCondition(mode, threshold, parameter);
        }
        private static void AddExit(AnimatorState source, AnimatorState target, float exitTime)
        {
            var transition = source.AddTransition(target); transition.hasExitTime = true; transition.exitTime = exitTime; transition.duration = 0.1f;
        }
        private static bool Has(string value, string token) => Normalize(value).Contains(Normalize(token), StringComparison.Ordinal);
        private static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        private static Material CreateMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new FileNotFoundException("Missing Pomperstein base-color texture", TexturePath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a supported review shader.");
            var material = new Material(shader) { name = "Pomperstein_ReviewOnly_Iter2200" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
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
