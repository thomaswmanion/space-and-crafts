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
    /// <summary>
    /// Builds an isolated AnimatorController for the locally authored Splat FBX.
    /// It does not alter Splat_TEMP, the active player, or any production scene.
    /// </summary>
    public static class SplatUnifiedCandidateControllerAuthoringIter1708
    {
        private const string ModelPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.fbx";
        private const string ControllerPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.controller";
        private const string ReportPath =
            "Logs/score-comparison/splat-unified-candidate-controller-iter1708/controller-review.txt";

        [MenuItem("Space & Crafts/Author Isolated Unified Splat Controller Iter1708")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null || !importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic)
                throw new InvalidOperationException("Candidate FBX is not imported with Generic animations enabled.");

            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var idle = FindClip(clips, "Idle");
            var run = FindClip(clips, "Run");
            var jump = FindClip(clips, "Jump");
            var land = FindClip(clips, "Land");
            if (idle == null || run == null || jump == null || land == null)
                throw new InvalidOperationException("Expected imported Idle/Run/Jump/Land clips on the candidate FBX.");
            if (!idle.isLooping || !run.isLooping || jump.isLooping || land.isLooping)
                throw new InvalidOperationException("Candidate clip loop flags must be Idle/Run=true and Jump/Land=false.");

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            machine.states = Array.Empty<ChildAnimatorState>();
            machine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
            var locomotionBlend = new BlendTree
            {
                name = "Splat Unified Idle Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(locomotionBlend, controller);
            locomotionBlend.AddChild(idle, 0f);
            locomotionBlend.AddChild(run, 2f);

            var locomotionState = AddState(machine, "Locomotion", locomotionBlend, new Vector3(180, 0));
            var jumpState = AddState(machine, "Jump", jump, new Vector3(420, 100));
            var landState = AddState(machine, "Land", land, new Vector3(680, 100));
            machine.defaultState = locomotionState;

            var takeoff = machine.AddAnyStateTransition(jumpState);
            takeoff.hasExitTime = false;
            takeoff.duration = 0.08f;
            takeoff.canTransitionToSelf = false;
            takeoff.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            var touchdown = jumpState.AddTransition(landState);
            touchdown.hasExitTime = false;
            touchdown.duration = 0.07f;
            touchdown.AddCondition(AnimatorConditionMode.If, 0f, "Land");

            var recover = landState.AddTransition(locomotionState);
            recover.hasExitTime = true;
            recover.exitTime = 0.90f;
            recover.duration = 0.10f;

            AssetDatabase.SaveAssets();
            var report = new StringBuilder();
            report.AppendLine($"controller={ControllerPath}");
            report.AppendLine($"model={ModelPath}");
            report.AppendLine($"parameters=MoveSpeed(float),Jump(trigger),Land(trigger)");
            report.AppendLine($"clips={idle.name} loop={idle.isLooping}; {run.name} loop={run.isLooping}; {jump.name} loop={jump.isLooping}; {land.name} loop={land.isLooping}");
            report.AppendLine("transitions=Locomotion[Idle@0,Run@2](MoveSpeed),Any->Jump(Jump),Jump->Land(Land),Land->Locomotion(after 0.90 exit; MoveSpeed selects Idle or Run)");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Logs");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[Splat unified candidate] authored isolated controller; active player and scenes unchanged.\n" + report);
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string suffix) =>
            clips.FirstOrDefault(clip => clip.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

    }
}
#endif
