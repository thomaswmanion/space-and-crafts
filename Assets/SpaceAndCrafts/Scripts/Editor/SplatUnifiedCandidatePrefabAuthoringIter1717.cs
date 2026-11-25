#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Packages the Iter1717 locally-authored FBX and its embedded animation
    /// takes as a separate review-only prefab. Never edits a production asset.
    /// </summary>
    public static class SplatUnifiedCandidatePrefabAuthoringIter1717
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/Splat_UnifiedRig_Candidate_Iter1717.fbx";
        private const string ReviewFolder = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1722";
        private const string ControllerPath = ReviewFolder + "/Splat_Iter1722_ReviewOnly.controller";
        private const string AvatarPath = ReviewFolder + "/Splat_Iter1722_ReviewOnly_Avatar.asset";
        private const string PrefabPath = ReviewFolder + "/Splat_Iter1722_ReviewOnly.prefab";
        private const string ReportPath = "Logs/score-comparison/splat-unified-candidate-prefab-iter1722/prefab-review.txt";

        [MenuItem("Space & Crafts/Author Isolated Splat Iter1722 Review Prefab")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing Iter1722 review prefab: " + PrefabPath);

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null || !importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic)
                throw new InvalidOperationException("Iter1717 FBX must import as a Generic animated model before packaging.");

            ConfigureReviewClipLoops(importer);
            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null || importer == null)
                throw new InvalidOperationException("Iter1717 FBX could not be reloaded after applying review clip loop settings.");

            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var idle = FindClip(clips, "Idle");
            var run = FindClip(clips, "Run");
            var jump = FindClip(clips, "Jump");
            var land = FindClip(clips, "Land");
            if (idle == null || run == null || jump == null || land == null)
                throw new InvalidOperationException("Iter1717 FBX must contain embedded Idle, Run, Jump, and Land clips; found: " + string.Join(", ", clips.Select(c => c.name)));

            Directory.CreateDirectory(ReviewFolder);
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing review controller: " + ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var locomotion = new BlendTree
            {
                name = "Iter1722 Idle Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(locomotion, controller);
            locomotion.AddChild(idle, 0f);
            locomotion.AddChild(run, 2f);

            var locomotionState = AddState(machine, "Locomotion", locomotion, new Vector3(180, 0));
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

            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            if (avatar == null)
            {
                var avatarScene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var avatarRoot = new GameObject("Splat Iter1722 Avatar Root");
                    SceneManager.MoveGameObjectToScene(avatarRoot, avatarScene);
                    var avatarModel = PrefabUtility.InstantiatePrefab(model, avatarScene) as GameObject;
                    if (avatarModel == null)
                        throw new InvalidOperationException("Could not instantiate the Iter1717 FBX root to build a Generic Avatar.");
                    avatarModel.transform.SetParent(avatarRoot.transform, false);
                    avatar = AvatarBuilder.BuildGenericAvatar(avatarRoot, string.Empty);
                    if (avatar == null || !avatar.isValid)
                        throw new InvalidOperationException("Iter1717 FBX has no valid Avatar subasset and its hierarchy did not produce a valid Generic Avatar.");
                    avatar.name = "Splat Iter1722 Review Only Avatar";
                    AssetDatabase.CreateAsset(avatar, AvatarPath);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(avatarScene);
                }
                AssetDatabase.SaveAssets();
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            }
            if (avatar == null || !avatar.isValid)
                throw new InvalidOperationException("The persisted Iter1721 Generic Avatar could not be loaded as a valid Avatar asset.");

            // Match the exact wrapper→FBX-root hierarchy used to build the Avatar.
            // Importing its asset completes before this final preview scene exists.
            var reviewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = new GameObject("Splat Iter1722 Review Only");
                SceneManager.MoveGameObjectToScene(instance, reviewScene);
                var modelInstance = PrefabUtility.InstantiatePrefab(model, reviewScene) as GameObject;
                if (modelInstance == null) throw new InvalidOperationException("Could not instantiate Iter1717 FBX root for the review prefab.");
                modelInstance.transform.SetParent(instance.transform, false);
                var animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = instance.GetComponent<SplatMeshyAnimatorDriver>() ?? instance.AddComponent<SplatMeshyAnimatorDriver>();
                var serializedDriver = new SerializedObject(driver);
                var animatorField = serializedDriver.FindProperty("animator");
                if (animatorField == null) throw new InvalidOperationException("SplatMeshyAnimatorDriver animator field was not found.");
                animatorField.objectReferenceValue = animator;
                serializedDriver.ApplyModifiedPropertiesWithoutUndo();

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Unity failed to save the Iter1717 review prefab.");
                AssetDatabase.SaveAssets();
                var reportPath = Path.GetFullPath(ReportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? "Logs");
                File.WriteAllText(reportPath,
                    $"prefab={PrefabPath}\nmodel={ModelPath}\ncontroller={ControllerPath}\navatar={avatar.name} valid={avatar.isValid} rootMatch=true\n" +
                    $"clips={idle.name};{run.name};{jump.name};{land.name}\n" +
                    $"clipLooping=Idle:{idle.isLooping};Run:{run.isLooping};Jump:{jump.isLooping};Land:{land.isLooping}\n" +
                    "driver=SplatMeshyAnimatorDriver; parameters=MoveSpeed(float),Jump(trigger),Land(trigger)\n" +
                    "transitions=Idle/Run blend;Any->Jump;Jump->Land;Land->Locomotion after 0.90 exit\n" +
                    "scope=ReviewOnly; source FBX unchanged; no production prefab or scene modified\n" +
                    "note=Standalone prefab has no SplatMotor parent; motor-driven run/jump/land events require a review harness parent. Animator parameters remain available for direct review.\n");
                Debug.Log("[Splat Iter1722] Authored isolated ReviewOnly prefab: " + PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(reviewScene);
            }
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string suffix) =>
            clips.FirstOrDefault(clip => clip.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        private static void ConfigureReviewClipLoops(ModelImporter importer)
        {
            var settings = importer.clipAnimations;
            if (settings == null || settings.Length == 0)
                settings = importer.defaultClipAnimations;
            if (settings == null || settings.Length == 0)
                throw new InvalidOperationException("Iter1717 FBX did not expose clip import settings.");

            var changed = false;
            foreach (var clip in settings)
            {
                var shouldLoop = clip.name.EndsWith("Idle", StringComparison.OrdinalIgnoreCase)
                    || clip.name.EndsWith("Run", StringComparison.OrdinalIgnoreCase);
                if (clip.loopTime != shouldLoop)
                {
                    clip.loopTime = shouldLoop;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = settings;
                importer.SaveAndReimport();
            }
        }

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
