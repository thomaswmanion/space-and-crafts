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
    /// <summary>Isolated additive upper-body slam review; preserves the lower-body base animation.</summary>
    public static class PompersteinUpperBodyReviewIter2203
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/Pomperstein/Meshy_Iter1997";
        private const string RigPath = Root + "/Pomperstein_Rigged.fbx";
        private const string ActionPath = Root + "/UnityCandidate_ReviewOnly_Iter2202/Pomperstein_GroundSlam_Authored_Iter2202.fbx";
        private const string ActionClipToken = "Pomperstein Authored GroundSlam Iter2202";
        private const string IdleToken = "Idle";
        private const string WalkToken = "Walk";
        private const string RunToken = "Run";
        private const string MeshySlamToken = "Charged Ground Slam";
        private const string Output = Root + "/UnityCandidate_ReviewOnly_Iter2203";
        private const string ControllerPath = Output + "/Pomperstein_UpperBody_ReviewOnly.controller";
        private const string PrefabPath = Output + "/Pomperstein_UpperBody_ReviewOnly.prefab";
        private const string MaskPath = Output + "/Pomperstein_UpperBody_ReviewOnly.mask";
        private const string ActionAnimPath = Output + "/Pomperstein_GroundSlam_Additive_ReviewOnly.anim";
        private const string EmptyAnimPath = Output + "/AdditiveLayerEmpty_ReviewOnly.anim";
        private const string MaterialPath = Root + "/UnityCandidate_ReviewOnly_Iter2202/Pomperstein_ReviewOnly.mat";

        [MenuItem("Space & Crafts/Author Pomperstein Upper-Body Slam Review (Iter 2203)")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null)
                throw new InvalidOperationException("Iteration 2203 review output exists; refusing to overwrite.");

            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (rig == null) throw new FileNotFoundException("Pomperstein source rig missing", RigPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(item => item != null && item.isValid);
            if (avatar == null) throw new InvalidOperationException("Pomperstein rig has no valid Generic Avatar.");
            var idle = RequireClip(Root + "/Pomperstein_Actions.fbx", IdleToken);
            var walk = RequireClip(Root + "/Pomperstein_Walk.fbx", WalkToken);
            var run = RequireClip(Root + "/Pomperstein_Run.fbx", RunToken);
            var meshSlam = RequireClip(Root + "/Pomperstein_Actions.fbx", MeshySlamToken);
            var authoredSource = RequireClip(ActionPath, ActionClipToken);
            var spine = rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "Spine");
            if (spine == null) throw new InvalidOperationException("Could not find Spine root for the upper-body mask.");

            EnsureFolder(Output);
            var mask = new AvatarMask { name = "Pomperstein_UpperBody_ReviewOnly_Iter2203" };
            mask.AddTransformPath(spine, true);
            AssetDatabase.CreateAsset(mask, MaskPath);

            var additive = UnityEngine.Object.Instantiate(authoredSource);
            additive.name = "Pomperstein_GroundSlam_Additive_ReviewOnly_Iter2203";
            AnimationUtility.SetAdditiveReferencePose(additive, idle, 0.20f);
            AssetDatabase.CreateAsset(additive, ActionAnimPath);
            var empty = new AnimationClip { name = "AdditiveLayerEmpty_ReviewOnly_Iter2203" };
            AssetDatabase.CreateAsset(empty, EmptyAnimPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Slam", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AuthoredSlam", AnimatorControllerParameterType.Trigger);
            var baseMachine = controller.layers[0].stateMachine;
            var idleState = State(baseMachine, "Idle", idle, new Vector3(100, 0));
            var walkState = State(baseMachine, "Walk", walk, new Vector3(320, 0));
            var runState = State(baseMachine, "Run", run, new Vector3(540, 0));
            var meshSlamState = State(baseMachine, "MeshyGroundSlam", meshSlam, new Vector3(760, 100));
            baseMachine.defaultState = idleState;
            AddFloat(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloat(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddFloat(walkState, runState, "MoveSpeed", 1.25f, AnimatorConditionMode.Greater);
            AddFloat(runState, walkState, "MoveSpeed", 1.25f, AnimatorConditionMode.Less);
            AddAny(baseMachine, meshSlamState, "Slam");
            AddExit(meshSlamState, idleState, 0.94f);

            controller.AddLayer("UpperBodySlamAdditive");
            var layers = controller.layers;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Additive;
            layers[1].defaultWeight = 1f;
            layers[1].avatarMask = mask;
            layers[1].iKPass = false;
            controller.layers = layers;
            var upperMachine = controller.layers[1].stateMachine;
            var emptyState = State(upperMachine, "Empty", empty, new Vector3(100, 0));
            var upperSlamState = State(upperMachine, "AuthoredGroundSlam", additive, new Vector3(360, 0));
            upperMachine.defaultState = emptyState;
            AddAny(upperMachine, upperSlamState, "AuthoredSlam");
            AddExit(upperSlamState, emptyState, 0.94f);

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new FileNotFoundException("Existing review material missing", MaterialPath);
            var instance = PrefabUtility.InstantiatePrefab(rig) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Pomperstein rig.");
            try
            {
                instance.name = "Pomperstein_UpperBody_ReviewOnly_Iter2203";
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }

            var bindings = AnimationUtility.GetCurveBindings(additive).Where(binding => binding.type == typeof(Transform)).ToArray();
            var maskPaths = Enumerable.Range(0, mask.transformCount).Select(mask.GetTransformPath).ToArray();
            var report = new StringBuilder();
            report.AppendLine("Pomperstein Iteration 2203 — review-only additive upper-body action.");
            report.AppendLine("Base layer retains Idle/Walk/Run and Meshy Slam. Authored action is additive against Idle at t=0.20s and masked recursively from Spine, leaving Hips and both legs to the base layer.");
            report.AppendLine($"Avatar={avatar.name}; valid={avatar.isValid}; human={avatar.isHuman}; actionBindings={bindings.Length}; exactPaths={bindings.All(item => rig.GetComponentsInChildren<Transform>(true).Any(t => AnimationUtility.CalculateTransformPath(t, rig.transform) == item.path))}");
            report.AppendLine("Mask root: " + AnimationUtility.CalculateTransformPath(spine, rig.transform));
            report.AppendLine("Mask paths: " + string.Join(", ", maskPaths));
            report.AppendLine("Controller / prefab / mask / additive clip: " + ControllerPath + " / " + PrefabPath + " / " + MaskPath + " / " + ActionAnimPath);
            report.AppendLine("This is an isolated Animator review configuration, not production integration or proof of contact, gameplay, build, or human play.");
            File.WriteAllText(Output + "/authoring-review.txt", report.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Pomperstein Iter2203] additive upper-body review candidate created; production scenes untouched.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static AnimationClip RequireClip(string path, string token)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(item => item != null).ToArray();
            var normalized = Normalize(token);
            var clip = clips.FirstOrDefault(item => Normalize(item.name).Contains(normalized, StringComparison.Ordinal));
            if (clip == null) throw new InvalidOperationException("Missing clip '" + token + "' at " + path + "; found " + string.Join(", ", clips.Select(item => item.name)));
            if (clip.length <= 0 || AnimationUtility.GetCurveBindings(clip).Length == 0) throw new InvalidOperationException("Clip has no animation curves: " + clip.name);
            return clip;
        }
        private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        { var state = machine.AddState(name, position); state.motion = motion; state.writeDefaultValues = false; return state; }
        private static void AddAny(AnimatorStateMachine machine, AnimatorState target, string parameter)
        { var transition = machine.AddAnyStateTransition(target); transition.hasExitTime = false; transition.duration = 0.08f; transition.AddCondition(AnimatorConditionMode.If, 0f, parameter); }
        private static void AddFloat(AnimatorState from, AnimatorState to, string parameter, float threshold, AnimatorConditionMode mode)
        { var transition = from.AddTransition(to); transition.hasExitTime = false; transition.duration = 0.14f; transition.AddCondition(mode, threshold, parameter); }
        private static void AddExit(AnimatorState from, AnimatorState to, float exitTime)
        { var transition = from.AddTransition(to); transition.hasExitTime = true; transition.exitTime = exitTime; transition.duration = 0.1f; }
        private static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        private static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path).Replace('\\', '/'); var name = Path.GetFileName(path); if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent); AssetDatabase.CreateFolder(parent, name); }
    }
}
#endif
