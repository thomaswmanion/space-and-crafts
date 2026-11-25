#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds an isolated review-only prefab/controller for the Iter1791 Meshy Splat.
    /// It does not modify the production player, scene, bootstrap, or current prefab.
    /// </summary>
    public static class SplatMeshyIter1791ReviewAssetAuthoring
    {
        private const string SourceRoot =
            "Assets/SpaceAndCrafts/Art/Characters/Splat";
        private const string RigPath = SourceRoot + "/MeshyRig_Iter1791/rigged_character.fbx";
        private const string WalkPath = SourceRoot + "/MeshyRig_Iter1791/walking_fbx.fbx";
        private const string RunPath = SourceRoot + "/MeshyRig_Iter1791/running_fbx.fbx";
        private const string IdleJumpPath = SourceRoot + "/MeshyActions_Iter1791/idle_jump.fbx";
        private const string LandingPath = SourceRoot + "/MeshyActions_Iter1791/landing_upright.fbx";
        private const string CatalogActionsPath = SourceRoot + "/MeshyActions_Iter1791/jump_lands_fbx.fbx";
        private const string PersonalityActionsPath = SourceRoot + "/MeshyActions_Iter1800/splat_idle_variations.fbx";
        private const string ActionSetPath = SourceRoot + "/MeshyActions_Iter1800/splat_action_set.fbx";
        private const string FistPumpPath = SourceRoot + "/MeshyActions_Iter1800/victory_fist_pump.fbx";
        private const string CuriosityIdlePath = SourceRoot + "/MeshyActions_Iter1810/splat_curiosity_idle.fbx";
        private const string ReviewFolder = SourceRoot + "/MeshyRig_Iter1791/ReviewOnly";
        private const string ControllerPath = ReviewFolder + "/Splat_Meshy_Iter1791_ReviewOnly.controller";
        private const string PrefabPath = ReviewFolder + "/Splat_Meshy_Iter1791_ReviewOnly.prefab";
        private const float TargetCharacterHeight = 1.15f;

        [MenuItem("Space & Crafts/Author Iter1791 Splat Meshy Review Assets")]
        public static void AuthorReviewAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureAssetFolder(ReviewFolder);

            foreach (var path in new[] { RigPath, WalkPath, RunPath, IdleJumpPath, LandingPath,
                         CatalogActionsPath, PersonalityActionsPath, ActionSetPath, FistPumpPath, CuriosityIdlePath })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    throw new InvalidOperationException("Missing Iter1791 Splat FBX: " + path);
                }
            }

            ConfigureRigImporter(RigPath, null, importAnimation: false).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid)
            {
                throw new InvalidOperationException("Iter1791 rig FBX did not produce a valid Generic Avatar.");
            }

            ConfigureRigImporter(WalkPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRigImporter(RunPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRigImporter(IdleJumpPath, avatar, importAnimation: true).SaveAndReimport();

            var landingImporter = ConfigureRigImporter(LandingPath, avatar, importAnimation: true);
            // Meshy's retargeted animation FBX stores root translations in
            // centimeters. Scale those keys down for Unity's meter-based rig.
            // The candidate Animator still disables root motion.
            landingImporter.globalScale = 0.01f;
            landingImporter.SaveAndReimport();
            ConfigureRigImporter(CatalogActionsPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRigImporter(PersonalityActionsPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRigImporter(ActionSetPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRigImporter(FistPumpPath, avatar, importAnimation: true).SaveAndReimport();
            var curiosityIdleImporter = ConfigureRigImporter(CuriosityIdlePath, avatar, importAnimation: true);
            // Meshy retargeted FBXs encode root offsets in centimeters; keep the
            // review sample on Unity's meter scale and let the controller own motion.
            curiosityIdleImporter.globalScale = 0.01f;
            curiosityIdleImporter.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // The original generated Idle turns the face almost into profile,
            // hiding one eye in a front-on player view. Idle_7 is a calmer,
            // face-forward base pose; the existing gentle idle pulse and
            // periodic Friendly Hello supply personality without body stretch.
            var idle = FindClip(PersonalityActionsPath, "Idle_7");
            var jump = FindClip(IdleJumpPath, "Regular_Jump", "Jump");
            var walk = FindClip(WalkPath, "walking", "walk");
            var run = FindClip(RunPath, "running", "run");
            // Both generated landing attempts roll/fall backward on this cute
            // short-legged design. Author a restrained no-scale landing on the
            // exact imported rig instead of shipping either bad retarget.
            var rigModel = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            var land = CreateAuthoredLandingClip(rigModel);
            var friendlyHello = FindClip(PersonalityActionsPath, "Idle_15");
            var victory = FindClip(FistPumpPath, "Victory_Fist_Pump");
            var controller = CreateController(idle, walk, run, jump, land, friendlyHello, victory);
            CreateReviewPrefab(avatar, controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Space & Crafts] Created the Iter1791 Splat Meshy review-only prefab/controller. Production player and scenes were not changed.");
        }

        private static ModelImporter ConfigureRigImporter(string path, Avatar sourceAvatar, bool importAnimation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Unity could not import Iter1791 FBX: " + path);
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = sourceAvatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = importAnimation;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            if (sourceAvatar == null)
            {
                // The source FBX supplies the mesh, materials, and the single
                // Generic Avatar used by each compatible animation FBX.
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            }
            else
            {
                // Animation FBXs are used only as clip sources; their model
                // objects are never instantiated into the review prefab.
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                foreach (var clip in clips)
                {
                    clip.loopTime = ShouldLoopClip(path, clip.name);
                    clip.loopPose = clip.loopTime;
                }

                importer.clipAnimations = clips;
            }

            return importer;
        }

        private static bool ShouldLoopClip(string path, string clipName)
        {
            if (path == WalkPath || path == RunPath)
            {
                return true;
            }

            if (path == PersonalityActionsPath)
            {
                return string.Equals(clipName, "Idle_7", StringComparison.OrdinalIgnoreCase);
            }

            return path == IdleJumpPath &&
                   (clipName ?? string.Empty).IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static AnimationClip FindClip(string path, params string[] candidates)
        {
            var clips = LoadClips(path);
            foreach (var candidate in candidates)
            {
                var exact = clips.FirstOrDefault(clip => string.Equals(
                    clip.name, candidate, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }

                var contained = clips.FirstOrDefault(clip => clip.name.IndexOf(
                    candidate, StringComparison.OrdinalIgnoreCase) >= 0);
                if (contained != null)
                {
                    return contained;
                }
            }

            throw new InvalidOperationException(
                $"No matching animation in {path}. Imported clips: {string.Join(", ", clips.Select(clip => clip.name))}");
        }

        private static AnimationClip FindOnlyClip(string path)
        {
            var clips = LoadClips(path);
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one landing clip in {path}, found {clips.Length}: {string.Join(", ", clips.Select(clip => clip.name))}");
            }

            return clips[0];
        }

        private static AnimationClip[] LoadClips(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

        private static AnimationClip CreateAuthoredLandingClip(GameObject rigModel)
        {
            if (rigModel == null)
            {
                throw new InvalidOperationException("Could not load the Iter1791 rig model for the authored landing pose.");
            }

            const string clipPath = ReviewFolder + "/Splat_Meshy_Iter1791_AuthoredSoftLand.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            var clip = new AnimationClip
            {
                name = "Splat_AuthoredSoftLand_Iter1791",
                frameRate = 30f,
                wrapMode = WrapMode.Once,
            };
            var hierarchy = rigModel.GetComponentsInChildren<Transform>(true);
            var spine = FindNamedBone(hierarchy, "Spine");
            var leftArm = FindNamedBone(hierarchy, "LeftArm", "LeftUpperArm");
            var rightArm = FindNamedBone(hierarchy, "RightArm", "RightUpperArm");
            var leftUpperLeg = FindNamedBone(hierarchy, "LeftUpLeg", "LeftUpperLeg");
            var rightUpperLeg = FindNamedBone(hierarchy, "RightUpLeg", "RightUpperLeg");
            var leftLowerLeg = FindNamedBone(hierarchy, "LeftLeg", "LeftLowerLeg");
            var rightLowerLeg = FindNamedBone(hierarchy, "RightLeg", "RightLowerLeg");

            // A shallow three-beat cushion: impact, quick rebound, settle.
            // Small joint rotations keep the round shell readable and never
            // scale it; locomotion remains fully controlled by the Animator.
            var times = new[] { 0f, 0.07f, 0.16f, 0.32f };
            // Keep Hips and the shell's root translation fixed so landing cannot
            // sink or shift the round body. The grounded sole correction handles contact.
            AddRotationCurve(clip, rigModel.transform, spine, times, new Vector3(0f, 0f, 0f),
                new Vector3(-10f, 0f, 0f), new Vector3(-3f, 0f, 0f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, leftArm, times, Vector3.zero,
                new Vector3(0f, 0f, -20f), new Vector3(0f, 0f, -7f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, rightArm, times, Vector3.zero,
                new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, 7f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, leftUpperLeg, times, Vector3.zero,
                new Vector3(-9f, 0f, 0f), new Vector3(-3f, 0f, 0f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, rightUpperLeg, times, Vector3.zero,
                new Vector3(-9f, 0f, 0f), new Vector3(-3f, 0f, 0f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, leftLowerLeg, times, Vector3.zero,
                new Vector3(16f, 0f, 0f), new Vector3(5f, 0f, 0f), Vector3.zero);
            AddRotationCurve(clip, rigModel.transform, rightLowerLeg, times, Vector3.zero,
                new Vector3(16f, 0f, 0f), new Vector3(5f, 0f, 0f), Vector3.zero);

            AssetDatabase.CreateAsset(clip, clipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Transform FindNamedBone(Transform[] hierarchy, params string[] names)
        {
            foreach (var name in names)
            {
                var found = hierarchy.FirstOrDefault(transform =>
                    string.Equals(transform.name, name, StringComparison.OrdinalIgnoreCase));
                if (found != null) return found;
            }
            return null;
        }

        private static void AddRotationCurve(AnimationClip clip, Transform root, Transform bone,
            float[] times, Vector3 idleEuler, Vector3 impactEuler, Vector3 reboundEuler, Vector3 settleEuler)
        {
            if (bone == null) return;
            var bind = bone.localRotation;
            var rotations = new[]
            {
                bind * Quaternion.Euler(idleEuler),
                bind * Quaternion.Euler(impactEuler),
                bind * Quaternion.Euler(reboundEuler),
                bind * Quaternion.Euler(settleEuler),
            };
            for (var i = 1; i < rotations.Length; i++)
            {
                if (Quaternion.Dot(rotations[i - 1], rotations[i]) < 0f)
                {
                    var q = rotations[i];
                    rotations[i] = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                }
            }

            var path = AnimationUtility.CalculateTransformPath(bone, root);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"), MakeCurve(times, rotations, q => q.x));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"), MakeCurve(times, rotations, q => q.y));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"), MakeCurve(times, rotations, q => q.z));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"), MakeCurve(times, rotations, q => q.w));
        }

        private static AnimationCurve MakeCurve(float[] times, Quaternion[] values, Func<Quaternion, float> component)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(times[i], component(values[i]));
            }
            return new AnimationCurve(keys);
        }

        private static AnimatorController CreateController(
            AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump, AnimationClip land,
            AnimationClip friendlyHello, AnimationClip victory)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Personality", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PersonalityIdle", AnimatorControllerParameterType.Int);
            controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            machine.states = Array.Empty<ChildAnimatorState>();

            var locomotionBlend = new BlendTree
            {
                name = "Splat Meshy Iter1791 Idle Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(locomotionBlend, controller);
            locomotionBlend.AddChild(idle, 0f);
            locomotionBlend.AddChild(walk, 0.45f);
            locomotionBlend.AddChild(run, 2f);

            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = locomotionBlend;
            machine.defaultState = locomotion;
            var jumpState = machine.AddState("Jump");
            jumpState.motion = jump;
            var landState = machine.AddState("Land");
            landState.motion = land;

            var helloState = machine.AddState("Friendly Hello");
            helloState.motion = friendlyHello;
            var personality = locomotion.AddTransition(helloState);
            personality.hasExitTime = false;
            personality.duration = 0.16f;
            personality.AddCondition(AnimatorConditionMode.If, 0f, "Personality");
            personality.AddCondition(AnimatorConditionMode.Equals, 1, "PersonalityIdle");
            personality.AddCondition(AnimatorConditionMode.Less, 0.12f, "MoveSpeed");

            var returnFromHello = helloState.AddTransition(locomotion);
            returnFromHello.hasExitTime = true;
            returnFromHello.exitTime = 0.95f;
            returnFromHello.duration = 0.18f;

            var victoryState = machine.AddState("Victory Fist Pump");
            victoryState.motion = victory;
            // World completion can happen while Splat is airborne or in the
            // interaction one-shot. Make the victory action interrupt those
            // states too, while suppressing a self-transition if completion
            // is raised twice in one scene.
            var celebrate = machine.AddAnyStateTransition(victoryState);
            celebrate.hasExitTime = false;
            celebrate.duration = 0.08f;
            celebrate.canTransitionToSelf = false;
            celebrate.AddCondition(AnimatorConditionMode.If, 0f, "Victory");

            var victoryRecover = victoryState.AddTransition(locomotion);
            victoryRecover.hasExitTime = true;
            victoryRecover.exitTime = 0.95f;
            victoryRecover.duration = 0.12f;

            var takeoff = machine.AddAnyStateTransition(jumpState);
            takeoff.hasExitTime = false;
            takeoff.duration = 0.08f;
            takeoff.canTransitionToSelf = false;
            takeoff.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            var touchdown = jumpState.AddTransition(landState);
            touchdown.hasExitTime = false;
            touchdown.duration = 0.08f;
            touchdown.AddCondition(AnimatorConditionMode.If, 0f, "Land");

            var recover = landState.AddTransition(locomotion);
            recover.hasExitTime = true;
            recover.exitTime = 0.88f;
            recover.duration = 0.12f;
            return controller;
        }


        private static void CreateReviewPrefab(Avatar avatar, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (model == null)
            {
                throw new InvalidOperationException("Iter1791 rig FBX did not import as a model prefab.");
            }

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate the Iter1791 Meshy Splat FBX.");
            }

            instance.name = "Splat_Meshy_Iter1791_ReviewOnly";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException("Iter1791 Meshy Splat FBX has no renderers.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // Fit only the review prefab to the actual Arrival capsule height,
            // then seat its lowest rendered point at local ground zero. This is
            // a preview alignment, not a production collision/contact claim.
            var fit = TargetCharacterHeight / Mathf.Max(0.01f, bounds.size.y);
            var originalScale = instance.transform.localScale;
            instance.transform.localScale = originalScale * fit;
            instance.transform.localPosition = new Vector3(0f, -bounds.min.y * fit, 0f);

            var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            SeatRenderedSolesToOrigin(instance, renderers);
            animator.Rebind();
            animator.Update(0f);

            var driver = instance.GetComponent<SplatMeshyAnimatorDriver>() ??
                         instance.AddComponent<SplatMeshyAnimatorDriver>();
            var serializedDriver = new SerializedObject(driver);
            serializedDriver.FindProperty("animator").objectReferenceValue = animator;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            var personalityDriver = instance.GetComponent<SplatMeshyIdlePersonalityDriver>() ??
                                    instance.AddComponent<SplatMeshyIdlePersonalityDriver>();
            var serializedPersonalityDriver = new SerializedObject(personalityDriver);
            serializedPersonalityDriver.FindProperty("animator").objectReferenceValue = animator;
            serializedPersonalityDriver.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static void SeatRenderedSolesToOrigin(GameObject instance, Renderer[] renderers)
        {
            var lowest = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer skinned) || skinned.sharedMesh == null)
                {
                    if (renderer != null && renderer.enabled)
                        lowest = Mathf.Min(lowest, renderer.bounds.min.y);
                    continue;
                }

                var baked = new Mesh { name = "Temporary Iter1791 Splat bind-pose seat" };
                try
                {
                    skinned.BakeMesh(baked);
                    foreach (var vertex in baked.vertices)
                        lowest = Mathf.Min(lowest, skinned.transform.TransformPoint(vertex).y);
                }
                finally
                {
                    Object.DestroyImmediate(baked);
                }
            }

            if (!float.IsInfinity(lowest))
            {
                // The FBX's initial Renderer.bounds min is stale for its skin
                // until Animator evaluation. Fit from the baked pose instead.
                instance.transform.position += Vector3.up * -lowest;
            }
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
