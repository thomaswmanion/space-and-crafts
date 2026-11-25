#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Promotes the reviewed Iter1856 Meshy model onto the real player prefab while
    /// preserving its CharacterController, SplatMotor, input, and rollback visuals.
    /// </summary>
    public static class PromoteSplatIter1856PlayerVisual
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1856";
        private const string PlayerPrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string ReviewPrefabPath = Root + "/ReviewOnly/Splat_Iter1856_Rigged_ReviewOnly.prefab";
        private const string RigPath = Root + "/Splat_Iter1856_Rigged.fbx";
        private const string WalkPath = Root + "/Splat_Iter1856_Walk.fbx";
        private const string RunPath = Root + "/Splat_Iter1856_Run.fbx";
        private const string MainActionsPath = Root + "/Splat_Iter1856_ActionBundle.fbx";
        private const string AdventureActionsPath = Root + "/Splat_Iter1856_ActionBundle_Adventure.fbx";
        private const string ControllerPath = Root + "/Splat_Iter1856_Playable.controller";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1856-production-prefab/promotion-report.txt";

        [MenuItem("Space & Crafts/Promote Splat Iter1856 Player Visual")]
        public static void Promote()
        {
            var prefabContents = (GameObject)null;
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var path in new[] { ReviewPrefabPath, RigPath, WalkPath, RunPath, MainActionsPath, AdventureActionsPath, PlayerPrefabPath })
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        throw new InvalidOperationException("Missing promotion source: " + path);

                var avatar = ConfigureRigAndAnimations();
                var controller = BuildController();
                prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                var gameplayController = prefabContents.GetComponent<CharacterController>();
                var motor = prefabContents.GetComponent<SplatMotor>();
                if (gameplayController == null || motor == null)
                    throw new InvalidOperationException("Splat_TEMP is missing its gameplay collider or motor; refusing promotion.");

                var mode = prefabContents.GetComponent<SplatMeshyVisualMode>();
                if (mode == null || !mode.enabled)
                    throw new InvalidOperationException("Meshy visual mode is not enabled on the player prefab.");

                const string visualName = SplatMeshyVisualMode.MeshyVisualRootName;
                var rollbackName = "ART_MeshySplatVisual_Rollback_Iter1791";
                var duplicatePrevious = prefabContents.transform.Find("ART_MeshySplatVisual_Rollback_Iter1856_Previous");
                if (duplicatePrevious != null) UnityEngine.Object.DestroyImmediate(duplicatePrevious.gameObject);

                GameObject visual = null;
                for (var i = prefabContents.transform.childCount - 1; i >= 0; i--)
                {
                    var child = prefabContents.transform.GetChild(i);
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                    var sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                    var isIter1856 = string.Equals(sourcePath, ReviewPrefabPath, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(sourcePath, RigPath, StringComparison.OrdinalIgnoreCase);
                    var isIter1791 = sourcePath.IndexOf("Meshy_Iter1791", StringComparison.OrdinalIgnoreCase) >= 0
                        || sourcePath.IndexOf("MeshyRig_Iter1791", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isIter1856)
                    {
                        if (visual == null)
                        {
                            visual = child.gameObject;
                        }
                        else
                        {
                            // A previous promotion attempt left another copy of
                            // the same exact model; retain only one nested visual.
                            UnityEngine.Object.DestroyImmediate(child.gameObject);
                        }
                    }
                    else if (isIter1791)
                    {
                        child.gameObject.name = rollbackName;
                        child.gameObject.SetActive(false);
                    }
                }

                if (visual == null)
                    visual = (GameObject)PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(ReviewPrefabPath), prefabContents.transform);
                if (visual == null) throw new InvalidOperationException("Could not instance the Iter1856 rigged prefab.");
                visual.name = visualName;
                visual.SetActive(true);
                visual.transform.localRotation = Quaternion.identity;

                var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
                if (animator == null) throw new InvalidOperationException("Iter1856 prefab does not contain an Animator.");
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // Preserve the Meshy candidate's matched scale, then seat its
                // aggregate silhouette against the existing controller support.
                var renderers = visual.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r != null && r.enabled).ToArray();
                if (renderers.Length == 0) throw new InvalidOperationException("Iter1856 prefab has no enabled renderers.");
                var lowest = renderers.Min(r => r.bounds.min.y);
                var support = gameplayController.bounds.min.y;
                var correction = support - lowest;
                visual.transform.localPosition = new Vector3(0f, visual.transform.localPosition.y + correction, 0f);

                var driver = visual.GetComponent<SplatMeshyAnimatorDriver>() ?? visual.AddComponent<SplatMeshyAnimatorDriver>();
                var driverData = new SerializedObject(driver);
                driverData.FindProperty("animator").objectReferenceValue = animator;
                driverData.FindProperty("motor").objectReferenceValue = motor;
                driverData.ApplyModifiedPropertiesWithoutUndo();

                var personality = visual.GetComponent<SplatMeshyIdlePersonalityDriver>() ?? visual.AddComponent<SplatMeshyIdlePersonalityDriver>();
                var personalityData = new SerializedObject(personality);
                personalityData.FindProperty("animator").objectReferenceValue = animator;
                personalityData.FindProperty("motor").objectReferenceValue = motor;
                personalityData.ApplyModifiedPropertiesWithoutUndo();

                var backup = "Logs/score-comparison/splat-meshy-iter1856-production-prefab/Splat_TEMP.before-Iter1856.prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                if (!File.Exists(backup)) File.Copy(PlayerPrefabPath, backup, false);
                if (PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath) == null)
                    throw new InvalidOperationException("Saving the updated player prefab failed.");

                var report = new StringBuilder();
                report.AppendLine("Iter1856 production prefab asset promotion");
                report.AppendLine("Replaced only the nested Meshy visual. The existing CharacterController, SplatMotor, input, interactor, and other player-root components remain on Splat_TEMP.");
                report.AppendLine("Rollback visual is retained as inactive child: " + rollbackName + ".");
                report.AppendLine("Player direct children after cleanup: " + string.Join("; ", Enumerable.Range(0, prefabContents.transform.childCount).Select(i =>
                {
                    var child = prefabContents.transform.GetChild(i);
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                    return child.name + " active=" + child.gameObject.activeSelf + " source=" + (source != null ? AssetDatabase.GetAssetPath(source) : "local");
                })));
                report.AppendLine("New visual: " + visualName + "; Avatar valid=" + avatar.isValid + "; type=Generic; controller=" + controller.name + "; rigged SkinnedMeshRenderers=" + visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length + ".");
                report.AppendLine("Initial aggregate-renderer seating correction=" + correction.ToString("F4") + " m against the existing CharacterController support plane. Per-frame foot correction remains owned by SplatVisualRepair.");
                report.AppendLine("Animator root motion disabled; Meshy driver and idle personality driver are assigned to the nested visual and reference the real player motor.");
                report.AppendLine("Controller motions: " + string.Join(", ", controller.animationClips.Select(c => c.name).Distinct().OrderBy(n => n)));
                report.AppendLine("This edits the production prefab asset. It is not a build, game-play test, level render, varied-platform check, or human play verification.");
                File.WriteAllText(ReportPath, report.ToString());
                AssetDatabase.SaveAssets();
                Debug.Log("[Splat Iter1856] promoted nested Meshy visual and animation controller onto Splat_TEMP; collider/motor retained.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (prefabContents != null) PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static Avatar ConfigureRigAndAnimations()
        {
            var rigImporter = RequireModelImporter(RigPath);
            rigImporter.animationType = ModelImporterAnimationType.Generic;
            rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            rigImporter.importAnimation = true;
            rigImporter.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
            if (avatar == null) throw new InvalidOperationException("Iter1856 did not import a valid Generic Avatar.");

            foreach (var path in new[] { WalkPath, RunPath, MainActionsPath, AdventureActionsPath })
            {
                var importer = RequireModelImporter(path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return avatar;
        }

        private static AnimatorController BuildController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = new[]
            {
                Param("MoveSpeed", AnimatorControllerParameterType.Float), Param("Jump", AnimatorControllerParameterType.Trigger),
                Param("Land", AnimatorControllerParameterType.Trigger), Param("Victory", AnimatorControllerParameterType.Trigger),
                Param("Personality", AnimatorControllerParameterType.Trigger), Param("PersonalityIdle", AnimatorControllerParameterType.Int),
                Param("Pickup", AnimatorControllerParameterType.Trigger), Param("Dodge", AnimatorControllerParameterType.Trigger),
                Param("AirAttack", AnimatorControllerParameterType.Trigger), Param("RunJumpRoll", AnimatorControllerParameterType.Trigger),
                Param("Hit", AnimatorControllerParameterType.Trigger)
            };

            var walk = FindClip(WalkPath, "walking_man");
            var run = FindClip(RunPath, "running");
            var all = AssetDatabase.LoadAllAssetsAtPath(MainActionsPath).OfType<AnimationClip>()
                .Concat(AssetDatabase.LoadAllAssetsAtPath(AdventureActionsPath).OfType<AnimationClip>()).ToArray();
            var idle = Find(all, "Idle");
            var jump = Find(all, "Regular_Jump", "Regular Jump");
            var land = Find(all, "Jumping_Down", "Jumping Down");
            var victory = Find(all, "Victory_Cheer", "Victory Cheer");
            var wave = Find(all, "Big_Wave_Hello", "Big Wave Hello");
            var pickup = Find(all, "Male_Bend_Over_Pick_Up", "Bend Over Pick Up");
            var hit = Find(all, "Hit_Reaction", "Hit Reaction");
            var dodge = Find(all, "Quick_Step_and_Spin_Dodge", "Quick Step and Spin Dodge");
            var airAttack = Find(all, "Jumping_Punch", "Jumping Punch");
            var roll = Find(all, "Run_Jump_and_Roll", "Run Jump and Roll");
            foreach (var (name, clip) in new[] { ("Walk", walk), ("Run", run), ("Idle", idle), ("Jump", jump), ("Land", land), ("Victory", victory), ("Wave", wave), ("Pickup", pickup), ("Hit", hit), ("Dodge", dodge), ("AirAttack", airAttack), ("RunJumpRoll", roll) })
                if (clip == null) throw new InvalidOperationException("Required Iter1856 action clip was not found: " + name);

            var machine = controller.layers[0].stateMachine;
            if (machine.states.Length > 0) machine.RemoveState(machine.states[0].state);
            var locomotion = machine.AddState("Locomotion");
            var blend = new BlendTree { name = "Iter1856 Idle Walk Run", blendType = BlendTreeType.Simple1D, blendParameter = "MoveSpeed", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            blend.AddChild(idle, 0f); blend.AddChild(walk, .45f); blend.AddChild(run, 2f);
            AssetDatabase.AddObjectToAsset(blend, controller);
            locomotion.motion = blend;
            machine.defaultState = locomotion;

            var jumpState = machine.AddState("Jump"); jumpState.motion = jump;
            var landState = machine.AddState("Land"); landState.motion = land; landState.speed = 2.6f;
            var victoryState = machine.AddState("Victory"); victoryState.motion = victory;
            var waveState = machine.AddState("Personality"); waveState.motion = wave;
            var pickupState = machine.AddState("Pickup"); pickupState.motion = pickup;
            var hitState = machine.AddState("Hit"); hitState.motion = hit;
            var dodgeState = machine.AddState("Dodge"); dodgeState.motion = dodge;
            var attackState = machine.AddState("AirAttack"); attackState.motion = airAttack;
            var rollState = machine.AddState("RunJumpRoll"); rollState.motion = roll;

            var toJump = locomotion.AddTransition(jumpState); toJump.hasExitTime = false; toJump.duration = .08f; toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var toLand = jumpState.AddTransition(landState); toLand.hasExitTime = false; toLand.duration = .07f; toLand.offset = .5f; toLand.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            ExitAtEnd(landState, locomotion, .94f);
            AddTriggeredAction(machine, locomotion, victoryState, "Victory", .96f);
            AddTriggeredAction(machine, locomotion, waveState, "Personality", .96f);
            AddTriggeredAction(machine, locomotion, pickupState, "Pickup", .93f);
            AddTriggeredAction(machine, locomotion, hitState, "Hit", .96f);
            AddTriggeredAction(machine, locomotion, dodgeState, "Dodge", .92f);
            AddTriggeredAction(machine, locomotion, attackState, "AirAttack", .92f);
            AddTriggeredAction(machine, locomotion, rollState, "RunJumpRoll", .92f);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddTriggeredAction(AnimatorStateMachine machine, AnimatorState locomotion, AnimatorState action, string parameter, float exitTime)
        {
            var transition = locomotion.AddTransition(action);
            transition.hasExitTime = false; transition.duration = .1f; transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            ExitAtEnd(action, locomotion, exitTime);
        }

        private static void ExitAtEnd(AnimatorState source, AnimatorState destination, float exitTime)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = true; transition.exitTime = exitTime; transition.duration = .12f;
        }

        private static AnimatorControllerParameter Param(string name, AnimatorControllerParameterType type) => new AnimatorControllerParameter { name = name, type = type };
        private static AnimationClip FindClip(string path, params string[] names) => Find(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray(), names);
        private static AnimationClip Find(AnimationClip[] clips, params string[] names) => clips.FirstOrDefault(c => c != null
            && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
            && names.Any(n => c.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0));
        private static ModelImporter RequireModelImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter ?? throw new InvalidOperationException("Not a model importer: " + path);
    }
}
#endif
