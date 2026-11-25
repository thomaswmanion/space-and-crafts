#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Audits the rig, imported action clips, and controller actually assigned to
    /// Arrival Splat. This keeps candidate/legacy assets out of runtime claims.
    /// </summary>
    public static class SplatRuntimeAssetAudit
    {
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1858/Splat_Iter1858_Rigged.fbx";
        private const string ActionBundlePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1858/Splat_Iter1858_ActionBundle.fbx";
        private const string JumpLandBundlePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1858/Splat_Iter1858_JumpLandBundle.fbx";
        private const string ControllerPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1858/Splat_Iter1858_Playable.controller";

        public static void AuditArrivalSplat()
        {
            var failed = false;
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (rig == null || controller == null)
            {
                Debug.LogError($"[SplatRuntimeAssetAudit] Missing active rig/controller: rig={rig != null}, controller={controller != null}.");
                failed = true;
            }
            else
            {
                var skins = rig.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var avatars = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().ToArray();
                var bones = rig.GetComponentsInChildren<Transform>(true);
                Debug.Log($"[SplatRuntimeAssetAudit] Active Iter1858 rig: skins={skins.Length}, avatars={avatars.Length}, transforms={bones.Length}.");
                foreach (var avatar in avatars)
                    Debug.Log($"[SplatRuntimeAssetAudit] Avatar {avatar.name}: valid={avatar.isValid}, human={avatar.isHuman}.");
                foreach (var skin in skins)
                    Debug.Log($"[SplatRuntimeAssetAudit] Skin {skin.name}: mesh={skin.sharedMesh?.name ?? "missing"}, bones={skin.bones?.Length ?? 0}, root={skin.rootBone?.name ?? "missing"}.");

                foreach (var parameter in controller.parameters)
                    Debug.Log($"[SplatRuntimeAssetAudit] Parameter {parameter.name}: {parameter.type}.");
                foreach (var layer in controller.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        var clipNames = state.state.motion is BlendTree tree
                            ? string.Join(",", tree.children.Select(child => child.motion != null ? child.motion.name : "missing"))
                            : state.state.motion != null ? state.state.motion.name : "none";
                        Debug.Log($"[SplatRuntimeAssetAudit] Controller state {layer.name}/{state.state.name}: {clipNames}.");
                    }
                }
            }

            foreach (var path in new[] { ActionBundlePath, JumpLandBundlePath })
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    .ToArray();
                Debug.Log($"[SplatRuntimeAssetAudit] Imported clips from {path}: {clips.Length}.");
                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    Debug.Log($"[SplatRuntimeAssetAudit] Clip {clip.name}: length={clip.length:F2}s, loop={clip.isLooping}, bindings={bindings.Length}.");
                }
                if (clips.Length == 0) failed = true;
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(failed ? 1 : 0);
        }

        public static void IntegrateDiscoveryAndGatewayListening()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ActionBundlePath).OfType<AnimationClip>().ToArray();
            var alert = clips.FirstOrDefault(clip => clip != null && clip.name == "Alert");
            if (controller == null || alert == null)
            {
                Debug.LogError("[SplatRuntimeAssetAudit] Could not load the active controller or the imported Alert clip.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var stateMachine = controller.layers[0].stateMachine;
            var locomotion = stateMachine.states.Select(entry => entry.state)
                .FirstOrDefault(state => state.name == "Locomotion");
            var victory = stateMachine.states.Select(entry => entry.state)
                .FirstOrDefault(state => state.name == "Victory");
            if (locomotion == null || victory == null)
            {
                Debug.LogError("[SplatRuntimeAssetAudit] Active controller is missing Locomotion or Victory state.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            EnsureTriggerParameter(controller, "Oops");
            EnsureTriggerParameter(controller, "Discover");
            EnsureTriggerParameter(controller, "Listen");
            EnsureTriggerParameter(controller, "Victory");

            var oops = GetOrAddState(stateMachine, "Oops", new Vector3(500f, -70f, 0f));
            oops.motion = alert;
            oops.speed = 2f;
            EnsureAnyStateTrigger(stateMachine, oops, "Oops");
            EnsureExitTo(stateMachine, oops, locomotion, 0.9f);

            var discover = GetOrAddState(stateMachine, "Discover", new Vector3(500f, 60f, 0f));
            discover.motion = alert;
            discover.speed = 2f;
            EnsureAnyStateTrigger(stateMachine, discover, "Discover");
            EnsureExitTo(stateMachine, discover, locomotion, 0.9f);

            var listen = GetOrAddState(stateMachine, "Listen", new Vector3(500f, 190f, 0f));
            listen.motion = alert;
            listen.speed = 1f;
            EnsureAnyStateTrigger(stateMachine, listen, "Listen");
            // The gateway reply may arrive after player movement has resumed.
            // Let its reward reaction take priority from locomotion and airtime too.
            EnsureAnyStateTrigger(stateMachine, victory, "Victory");
            EnsureTriggerTransition(listen, victory, "Victory");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[SplatRuntimeAssetAudit] Integrated Oops and Discover (Alert, 2x speed, return to Locomotion), Listen, and an Any State Victory reaction into the active Iter1858 controller. Oops/Discover currently reuse Alert pending dedicated authored clips.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void EnsureTriggerParameter(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(parameter => parameter.name == name)) return;
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState GetOrAddState(AnimatorStateMachine stateMachine, string name, Vector3 position)
        {
            var existing = stateMachine.states.Select(entry => entry.state)
                .FirstOrDefault(state => state.name == name);
            if (existing != null) return existing;
            return stateMachine.AddState(name, position);
        }

        private static void EnsureAnyStateTrigger(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
        {
            if (stateMachine.anyStateTransitions.Any(transition => transition.destinationState == destination
                && transition.conditions.Any(condition => condition.parameter == trigger))) return;
            var transition = stateMachine.AddAnyStateTransition(destination);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = true;
        }

        private static void EnsureTriggerTransition(AnimatorState source, AnimatorState destination, string trigger)
        {
            if (source.transitions.Any(transition => transition.destinationState == destination
                && transition.conditions.Any(condition => condition.parameter == trigger))) return;
            var transition = source.AddTransition(destination);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.hasFixedDuration = true;
        }

        private static void EnsureExitTo(AnimatorStateMachine _, AnimatorState source, AnimatorState destination, float exitTime)
        {
            if (source.transitions.Any(transition => transition.destinationState == destination && transition.hasExitTime)) return;
            var transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.12f;
            transition.hasFixedDuration = true;
        }
    }
}
#endif
