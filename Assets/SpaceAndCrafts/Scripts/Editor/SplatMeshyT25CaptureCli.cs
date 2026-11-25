using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Offline review renders for the Iteration 1820 Meshy T2.5 Splat candidate.
    /// These sample clips through a temporary Animator on the imported avatar;
    /// they are not a replacement for motor-driven gameplay review.
    /// </summary>
    public static class SplatMeshyT25CaptureCli
    {
        private const string CandidateRoot = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_T2_5_APose_Iter1820_ReviewOnly";
        private const string ModelPath = CandidateRoot + "/Splat_APose_Rigged_Iter1820.fbx";
        private const string ActionBundlePath = CandidateRoot + "/Splat_APose_IdleJumpWave_Iter1820.fbx";
        private const string ExtraIdlePath = CandidateRoot + "/Splat_APose_Idle7_Iter1820.fbx";
        private const string WalkPath = CandidateRoot + "/Splat_APose_Walking_Iter1820.fbx";
        private const string RunPath = CandidateRoot + "/Splat_APose_Running_Iter1820.fbx";
        private const string LandPath = CandidateRoot + "/Splat_APose_SoftLand_Iter1820.fbx";
        private const string TexturePath = CandidateRoot + "/Splat_APose_T2_5_Iter1820_base_color.png";
        private const string DiveLandPath = CandidateRoot + "/Splat_APose_DiveLand_Iter1820.fbx";
        private const string JumpOpenPath = CandidateRoot + "/Splat_APose_JumpOpen_Iter1821.fbx";
        private const string HappyJumpPath = CandidateRoot + "/Splat_APose_HappyJump_Iter1822.fbx";
        private const string SwiftHopPath = CandidateRoot + "/Splat_APose_SwiftHop_Iter1823.fbx";
        private const string ReadyAnticipationPath = CandidateRoot + "/Splat_APose_ReadyAnticipation_Iter1827.fbx";
        private const string GameplayReviewFolder = CandidateRoot + "/ReviewOnly_Iter1826";
        private const string ReadyAnticipationLockedPath = GameplayReviewFolder + "/Splat_T25_MeshyReadyAnticipation_RootLocked_Iter1827.anim";
        private const string OutputRoot = "Logs/score-comparison/splat-meshy-t25-ready-anticipation-unity-iter1827";
        private const string ControllerPath = CandidateRoot + "/ReviewOnly_Iter1826/Splat_T25_ClipReview_Iter1826.controller";

        public static void CaptureSplatMeshyT25APoseReadyAnticipationIter1827()
        {
            Directory.CreateDirectory(OutputRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Meshy exported these FBXs with Humanoid import settings and no
            // source avatar. The candidate was therefore rendering bind poses.
            // Match the project's established Meshy pipeline: build one
            // Generic Avatar from the game mesh, then copy it into every action
            // FBX so their identically named bone curves can actually animate it.
            var rigImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (rigImporter == null) throw new InvalidOperationException("Missing Splat review rig importer.");
            if (rigImporter.animationType != ModelImporterAnimationType.Generic
                || rigImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || !rigImporter.importAnimation || rigImporter.optimizeGameObjects)
            {
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.sourceAvatar = null;
                rigImporter.importAnimation = true;
                rigImporter.optimizeGameObjects = false;
                rigImporter.importCameras = false;
                rigImporter.importLights = false;
                rigImporter.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>()
                .FirstOrDefault(a => a != null && a.isValid);
            if (sourceAvatar == null) throw new InvalidOperationException("Generic Splat review Avatar did not import.");
            foreach (var animationPath in new[] { ActionBundlePath, ExtraIdlePath, WalkPath, RunPath,
                         LandPath, DiveLandPath, JumpOpenPath, HappyJumpPath, SwiftHopPath,
                         ReadyAnticipationPath }.Distinct())
            {
                var animationImporter = AssetImporter.GetAtPath(animationPath) as ModelImporter;
                if (animationImporter == null) continue;
                if (animationImporter.animationType == ModelImporterAnimationType.Generic
                    && animationImporter.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
                    && animationImporter.sourceAvatar == sourceAvatar && animationImporter.importAnimation
                    && !animationImporter.optimizeGameObjects) continue;
                animationImporter.animationType = ModelImporterAnimationType.Generic;
                animationImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                animationImporter.sourceAvatar = sourceAvatar;
                animationImporter.importAnimation = true;
                animationImporter.optimizeGameObjects = false;
                animationImporter.importCameras = false;
                animationImporter.importLights = false;
                animationImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                animationImporter.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var candidateModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var authoredFeelClips = CreateGameplayFeelClips(candidateModel);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("Iter1820 Splat rigged FBX did not import: " + ModelPath);
                EditorApplication.Exit(2);
                return;
            }

            var clips = new List<AnimationClip>();
            AddClips(ActionBundlePath, clips);
            AddClips(ExtraIdlePath, clips);
            AddClips(WalkPath, clips);
            AddClips(RunPath, clips);
            AddClips(LandPath, clips);
            AddClips(DiveLandPath, clips);
            AddClips(JumpOpenPath, clips);
            AddClips(HappyJumpPath, clips);
            AddClips(SwiftHopPath, clips);
            AddRootLockedReadyAnticipation(clips);
            clips.AddRange(authoredFeelClips);
            foreach (var clip in clips)
            {
                var curveCount = AnimationUtility.GetCurveBindings(clip).Length
                    + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
                Debug.Log($"SPLAT_T25_CLIP name={clip.name} length={clip.length:0.###} curves={curveCount} frameRate={clip.frameRate:0.##}");
                if (AssetDatabase.GetAssetPath(clip) == ReadyAnticipationPath)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                                 .Where(b => b.propertyName.Contains("Position", StringComparison.OrdinalIgnoreCase)))
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var values = curve.keys.Select(k => k.value).ToArray();
                        if (values.Length == 0) continue;
                        Debug.Log($"SPLAT_T25_READY_POSITION path={binding.path} property={binding.propertyName} min={values.Min():0.###} max={values.Max():0.###} keys={values.Length}");
                    }
                }
            }

            var controllerFolder = Path.GetDirectoryName(ControllerPath);
            if (!AssetDatabase.IsValidFolder(controllerFolder))
                AssetDatabase.CreateFolder(CandidateRoot, Path.GetFileName(controllerFolder));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            // Keep a reused review controller in sync with the current clip
            // list. Previously its serialized states could be stale, making
            // Animator.Play silently leave the character in bind pose.
            var reviewStateMachine = controller.layers[0].stateMachine;
            for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                var clip = clips[clipIndex];
                var safeName = new string(clip.name.Where(char.IsLetterOrDigit).ToArray());
                var stateName = $"Review_{clipIndex:00}_{safeName}";
                var state = reviewStateMachine.states.Select(s => s.state)
                    .FirstOrDefault(s => s != null && s.name == stateName);
                if (state == null) state = reviewStateMachine.AddState(stateName);
                state.motion = clip;
            }
            if (clips.Count > 0 && reviewStateMachine.defaultState == null)
                reviewStateMachine.defaultState = reviewStateMachine.states[0].state;
            var controllerLayers = controller.layers;
            controllerLayers[0].defaultWeight = 1f;
            controller.layers = controllerLayers;
            AssetDatabase.SaveAssets();

            var priorScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not instantiate Iter1820 Splat rigged model.");
                EditorApplication.Exit(3);
                return;
            }

            instance.name = "Splat_Meshy_T2_5_Iter1820_ReviewOnly";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = instance.AddComponent<Animator>();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
            if (avatar == null)
                avatar = BuildReviewHumanoidAvatar(instance);
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.Rebind();
            animator.Update(0f);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Debug.Log($"SPLAT_T25_MODEL renderers={renderers.Length} skins={skins.Length} animator={(animator != null)} avatar={(avatar != null ? avatar.name : "missing")} avatarValid={(avatar != null && avatar.isValid)} controller={(controller != null ? controller.name : "missing")}");
            foreach (var skin in skins)
                if (skin != null && skin.bones != null)
                    Debug.Log("SPLAT_T25_BONES " + string.Join("|", skin.bones.Select(b => b != null ? AnimationUtility.CalculateTransformPath(b, instance.transform) : "null")));
            var importedAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            Debug.Log("SPLAT_T25_IMPORTED_SUBASSETS " + string.Join("|", importedAssets.Select(a => a != null ? a.name + ":" + a.GetType().Name : "null")));
            foreach (var skin in skins)
                Debug.Log($"SPLAT_T25_SKIN name={skin.name} bones={(skin.bones == null ? 0 : skin.bones.Length)} bounds={skin.bounds}");

            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (baseColor != null)
            {
                var characterMaterial = MakeMaterial(Color.white);
                if (characterMaterial.HasProperty("_BaseMap")) characterMaterial.SetTexture("_BaseMap", baseColor);
                if (characterMaterial.HasProperty("_BaseColor")) characterMaterial.SetColor("_BaseColor", Color.white);
                if (characterMaterial.HasProperty("_MainTex")) characterMaterial.SetTexture("_MainTex", baseColor);
                if (characterMaterial.HasProperty("_Color")) characterMaterial.SetColor("_Color", Color.white);
                if (characterMaterial.HasProperty("_Smoothness")) characterMaterial.SetFloat("_Smoothness", 0.12f);
                foreach (var skin in skins) skin.sharedMaterial = characterMaterial;
            }
            else Debug.LogWarning("Iter1820 base color texture was not imported: " + TexturePath);

            var bindSoleY = MeasureLowerMeshRegion(skins);
            if (!float.IsNaN(bindSoleY)) instance.transform.position += Vector3.up * (0.01f - bindSoleY);
            var allTransforms = instance.GetComponentsInChildren<Transform>(true);
            var localPositions = allTransforms.Select(t => t.localPosition).ToArray();
            var localRotations = allTransforms.Select(t => t.localRotation).ToArray();
            var localScales = allTransforms.Select(t => t.localScale).ToArray();

            var bounds = CalculateBounds(renderers);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Review Platform";
            var reviewSupportY = MeasureLowerMeshRegion(skins) - 0.005f;
            floor.transform.position = new Vector3(bounds.center.x, reviewSupportY, bounds.center.z);
            floor.transform.localScale = Vector3.one * 2f;
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.sharedMaterial = MakeMaterial(new Color(0.34f, 0.39f, 0.48f));

            CreateLight("Key", bounds.center + new Vector3(-2.0f, 3.2f, -3.0f), 920f, 3.2f);
            CreateLight("Fill", bounds.center + new Vector3(2.8f, 1.6f, -1.2f), 500f, 2.4f);
            CreateLight("Rim", bounds.center + new Vector3(0.6f, 2.4f, 3.0f), 760f, 2.0f);

            var cameraObject = new GameObject("Splat T2.5 Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.18f, 0.25f);
            camera.fieldOfView = 33f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            var target = bounds.center + Vector3.up * (bounds.size.y * 0.06f);
            camera.transform.position = target + new Vector3(1.25f, 0.72f, 3.7f);
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);

            var width = 900;
            var height = 900;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;

            var preferred = new[] { "Splat_T25_AuthoredIdle", "Splat_T25_AuthoredJump",
                "Splat_T25_AuthoredLand", "Splat_T25_AuthoredRun", "Idle_7", "Regular_Jump", "Wave_One_Hand" };
            var sequence = new List<(AnimationClip clip, float phase)>();
            foreach (var preferredName in preferred)
            {
                var clip = clips.FirstOrDefault(c => string.Equals(c.name, preferredName, StringComparison.OrdinalIgnoreCase)
                    || c.name.Replace(' ', '_').Equals(preferredName.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase));
                if (clip != null)
                    sequence.Add((clip, preferredName.StartsWith("Idle", StringComparison.OrdinalIgnoreCase) ? 0.52f : 0.38f));
            }
            foreach (var clip in clips)
                if (!sequence.Any(s => s.clip == clip)) sequence.Add((clip, 0.35f));

            var frame = 0;
            foreach (var (clip, _) in sequence)
            {
                var phases = clip.name.Contains("AuthoredIdle", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0.05f, 0.26f, 0.50f, 0.74f, 0.95f }
                    : clip.name.Contains("AuthoredLand", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0.08f, 0.28f, 0.58f, 0.82f, 0.98f }
                    : clip.name.Contains("AuthoredJump", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0.08f, 0.26f, 0.48f, 0.72f, 0.94f }
                    : clip.name.Contains("AuthoredRun", StringComparison.OrdinalIgnoreCase)
                        ? new[] { 0.04f, 0.28f, 0.53f, 0.78f, 0.96f }
                    : clip.name.Contains("Dive", StringComparison.OrdinalIgnoreCase)
                    || clip.name.Contains("Land", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0.04f, 0.16f, 0.30f, 0.44f, 0.58f, 0.72f, 0.88f, 0.97f }
                    : clip.name.Contains("Jump", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0.06f, 0.24f, 0.48f, 0.74f, 0.94f }
                    : clip.name.Contains("Idle", StringComparison.OrdinalIgnoreCase)
                        ? new[] { 0.05f, 0.35f, 0.70f, 0.95f }
                        : clip.name.Contains("Wave", StringComparison.OrdinalIgnoreCase)
                            ? new[] { 0.12f, 0.38f, 0.68f, 0.90f }
                            : new[] { 0.25f, 0.65f };
                for (var phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                {
                    for (var i = 0; i < allTransforms.Length; i++)
                    {
                        allTransforms[i].localPosition = localPositions[i];
                        allTransforms[i].localRotation = localRotations[i];
                        allTransforms[i].localScale = localScales[i];
                    }
                    var sampleTime = Mathf.Clamp(clip.length * phases[phaseIndex], 0f, Mathf.Max(0f, clip.length - 0.0001f));
                    var clipIndex = sequence.FindIndex(s => s.clip == clip);
                    var controllerStateName = $"Review_{clipIndex:00}_{new string(clip.name.Where(char.IsLetterOrDigit).ToArray())}";
                    var fullStatePath = "Base Layer." + controllerStateName;
                    var stateHash = Animator.StringToHash(fullStatePath);
                    var normalizedTime = clip.length > 0f ? sampleTime / clip.length : 0f;
                    if (animator.HasState(0, stateHash))
                    {
                        animator.enabled = true;
                        animator.Play(stateHash, 0, normalizedTime);
                        animator.Update(0f);
                    }
                    else
                    {
                        // Some Meshy FBXs import action clips as generic bone
                        // curves instead of Humanoid states. Their paths match
                        // this candidate's skeleton, so sample those curves
                        // directly rather than silently capturing the bind pose.
                        animator.enabled = false;
                        clip.SampleAnimation(instance, sampleTime);
                        Debug.LogWarning($"SPLAT_T25_DIRECT_SAMPLE clip={clip.name} state={fullStatePath} hash={stateHash}");
                    }
                    // Match the live SplatMeshyAnimatorDriver safety rule: keep
                    // every imported bind scale so a clip cannot stretch the shell.
                    for (var i = 0; i < allTransforms.Length; i++)
                        allTransforms[i].localScale = localScales[i];
                    foreach (var skin in skins)
                        if (skin != null) skin.updateWhenOffscreen = true;
                    // Batch-mode camera renders can retain a previous skinning
                    // buffer after SampleAnimation. Bake the sampled bone pose to
                    // a temporary static proxy so each still represents this clip.
                    var baked = new List<(SkinnedMeshRenderer skin, GameObject proxy, Mesh mesh)>();
                    foreach (var skin in skins)
                    {
                        if (skin == null || !skin.enabled) continue;
                        var bakedMesh = new Mesh { name = "Splat pose capture" };
                        skin.BakeMesh(bakedMesh);
                        var proxy = new GameObject("Splat baked pose capture");
                        proxy.transform.SetParent(skin.transform.parent, false);
                        proxy.transform.localPosition = skin.transform.localPosition;
                        proxy.transform.localRotation = skin.transform.localRotation;
                        proxy.transform.localScale = skin.transform.localScale;
                        proxy.AddComponent<MeshFilter>().sharedMesh = bakedMesh;
                        proxy.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                        skin.enabled = false;
                        baked.Add((skin, proxy, bakedMesh));
                    }
                    camera.Render();
                    RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    var safeName = new string(clip.name.Where(char.IsLetterOrDigit).ToArray());
                    var filePath = Path.Combine(OutputRoot, $"frame_{frame:00}_{safeName}_{phaseIndex:00}.png");
                    File.WriteAllBytes(filePath, texture.EncodeToPNG());
                    foreach (var capture in baked)
                    {
                        capture.skin.enabled = true;
                        UnityEngine.Object.DestroyImmediate(capture.proxy);
                        UnityEngine.Object.DestroyImmediate(capture.mesh);
                    }
                    var soleY = MeasureLowerMeshRegion(skins);
                    var soleGap = float.IsNaN(soleY) ? float.NaN : soleY - floor.transform.position.y;
                    Debug.Log($"SPLAT_T25_FRAME path={filePath} clip={clip.name} sample={sampleTime:0.###}/{clip.length:0.###} rootPosition={instance.transform.position} rootScale={instance.transform.lossyScale} lowerMeshGap={soleGap:0.####}m");
                    frame++;
                }
            }

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(floorRenderer.sharedMaterial);
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
            if (priorScene.IsValid() && priorScene.isLoaded) EditorSceneManager.SetActiveScene(priorScene);
            Debug.Log($"SPLAT_T25_CAPTURE_COMPLETE frames={frame} output={OutputRoot}; staged asset preview only, no gameplay/motor contact claimed.");
            EditorApplication.Exit(0);
        }

        private static Avatar BuildReviewHumanoidAvatar(GameObject rigRoot)
        {
            var humanBones = new[]
            {
                Human("Hips", "Hips"),
                // This Meshy rig names its central spine bones in reverse
                // numeric order: Hips -> Spine02 -> Spine01 -> Spine -> neck.
                Human("Spine", "Spine02"),
                Human("Chest", "Spine01"),
                Human("UpperChest", "Spine"),
                Human("Neck", "neck"),
                Human("Head", "Head"),
                Human("LeftShoulder", "LeftShoulder"),
                Human("RightShoulder", "RightShoulder"),
                Human("LeftUpperArm", "LeftArm"),
                Human("RightUpperArm", "RightArm"),
                Human("LeftLowerArm", "LeftForeArm"),
                Human("RightLowerArm", "RightForeArm"),
                Human("LeftHand", "LeftHand"),
                Human("RightHand", "RightHand"),
                Human("LeftUpperLeg", "LeftUpLeg"),
                Human("RightUpperLeg", "RightUpLeg"),
                Human("LeftLowerLeg", "LeftLeg"),
                Human("RightLowerLeg", "RightLeg"),
                Human("LeftFoot", "LeftFoot"),
                Human("RightFoot", "RightFoot"),
                Human("LeftToes", "LeftToeBase"),
                Human("RightToes", "RightToeBase")
            };
            var skeleton = rigRoot.GetComponentsInChildren<Transform>(true)
                .Select(t => new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition,
                    rotation = t.localRotation,
                    scale = t.localScale
                }).ToArray();
            var description = new HumanDescription
            {
                human = humanBones,
                skeleton = skeleton,
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.0f,
                legStretch = 0.0f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            return AvatarBuilder.BuildHumanAvatar(rigRoot, description);
        }

        private static List<AnimationClip> CreateGameplayFeelClips(GameObject rig)
        {
            var folder = GameplayReviewFolder;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(CandidateRoot, "ReviewOnly_Iter1826");
            var bones = rig.GetComponentsInChildren<Transform>(true);
            Transform Find(string name) => bones.FirstOrDefault(b => string.Equals(b.name, name, StringComparison.OrdinalIgnoreCase));
            var clips = new List<AnimationClip>();

            AddClip("Splat_T25_AuthoredIdle", 2.8f, true, new[] { 0f, 0.7f, 1.4f, 2.1f, 2.8f }, new()
            {
                ["Head"] = new[] { Vector3.zero, new Vector3(0f, 2f, -1f), Vector3.zero, new Vector3(0f, -2f, 1f), Vector3.zero },
                ["LeftArm"] = new[] { Vector3.zero, new Vector3(0f, 0f, -2f), Vector3.zero, new Vector3(0f, 0f, 2f), Vector3.zero },
                ["RightArm"] = new[] { Vector3.zero, new Vector3(0f, 0f, 2f), Vector3.zero, new Vector3(0f, 0f, -2f), Vector3.zero },
            });
            AddClip("Splat_T25_AuthoredRun", 0.6f, true, new[] { 0f, 0.15f, 0.30f, 0.45f, 0.60f }, new()
            {
                ["Spine"] = Enumerable.Repeat(new Vector3(-3f, 0f, 0f), 5).ToArray(),
                ["LeftArm"] = new[] { new Vector3(8f,0f,-24f), new Vector3(4f,0f,-8f), new Vector3(-8f,0f,24f), new Vector3(-4f,0f,8f), new Vector3(8f,0f,-24f) },
                ["RightArm"] = new[] { new Vector3(-8f,0f,24f), new Vector3(-4f,0f,8f), new Vector3(8f,0f,-24f), new Vector3(4f,0f,-8f), new Vector3(-8f,0f,24f) },
                ["LeftUpLeg"] = new[] { new Vector3(-30f,0f,0f), new Vector3(-8f,0f,0f), new Vector3(30f,0f,0f), new Vector3(8f,0f,0f), new Vector3(-30f,0f,0f) },
                ["RightUpLeg"] = new[] { new Vector3(30f,0f,0f), new Vector3(8f,0f,0f), new Vector3(-30f,0f,0f), new Vector3(-8f,0f,0f), new Vector3(30f,0f,0f) },
                ["LeftLeg"] = new[] { new Vector3(12f,0f,0f), new Vector3(22f,0f,0f), Vector3.zero, Vector3.zero, new Vector3(12f,0f,0f) },
                ["RightLeg"] = new[] { Vector3.zero, Vector3.zero, new Vector3(12f,0f,0f), new Vector3(22f,0f,0f), Vector3.zero },
                ["LeftFoot"] = new[] { new Vector3(-5f,0f,0f), new Vector3(-10f,0f,0f), new Vector3(4f,0f,0f), new Vector3(7f,0f,0f), new Vector3(-5f,0f,0f) },
                ["RightFoot"] = new[] { new Vector3(4f,0f,0f), new Vector3(7f,0f,0f), new Vector3(-5f,0f,0f), new Vector3(-10f,0f,0f), new Vector3(4f,0f,0f) },
            });
            AddClip("Splat_T25_AuthoredJump", 0.5f, false, new[] { 0f, 0.08f, 0.20f, 0.34f, 0.5f }, new()
            {
                ["Spine"] = new[] { Vector3.zero, new Vector3(4f,0f,0f), new Vector3(-3f,0f,0f), new Vector3(-2f,0f,0f), Vector3.zero },
                ["LeftArm"] = new[] { Vector3.zero, new Vector3(0f,0f,-10f), new Vector3(0f,0f,-32f), new Vector3(0f,0f,-20f), Vector3.zero },
                ["RightArm"] = new[] { Vector3.zero, new Vector3(0f,0f,10f), new Vector3(0f,0f,32f), new Vector3(0f,0f,20f), Vector3.zero },
                ["LeftUpLeg"] = new[] { Vector3.zero, new Vector3(-8f,0f,0f), new Vector3(18f,0f,0f), new Vector3(4f,0f,0f), Vector3.zero },
                ["RightUpLeg"] = new[] { Vector3.zero, new Vector3(-8f,0f,0f), new Vector3(18f,0f,0f), new Vector3(4f,0f,0f), Vector3.zero },
                ["LeftLeg"] = new[] { Vector3.zero, new Vector3(10f,0f,0f), new Vector3(18f,0f,0f), new Vector3(4f,0f,0f), Vector3.zero },
                ["RightLeg"] = new[] { Vector3.zero, new Vector3(10f,0f,0f), new Vector3(18f,0f,0f), new Vector3(4f,0f,0f), Vector3.zero },
            });
            AddClip("Splat_T25_AuthoredLand", 0.34f, false, new[] { 0f, 0.07f, 0.16f, 0.25f, 0.34f }, new()
            {
                ["Spine"] = new[] { Vector3.zero, new Vector3(-8f,0f,0f), new Vector3(-3f,0f,0f), Vector3.zero, Vector3.zero },
                ["LeftArm"] = new[] { Vector3.zero, new Vector3(0f,0f,-25f), new Vector3(0f,0f,-10f), Vector3.zero, Vector3.zero },
                ["RightArm"] = new[] { Vector3.zero, new Vector3(0f,0f,25f), new Vector3(0f,0f,10f), Vector3.zero, Vector3.zero },
                ["LeftUpLeg"] = new[] { Vector3.zero, new Vector3(-9f,0f,0f), new Vector3(-3f,0f,0f), Vector3.zero, Vector3.zero },
                ["RightUpLeg"] = new[] { Vector3.zero, new Vector3(-9f,0f,0f), new Vector3(-3f,0f,0f), Vector3.zero, Vector3.zero },
                ["LeftLeg"] = new[] { Vector3.zero, new Vector3(16f,0f,0f), new Vector3(5f,0f,0f), Vector3.zero, Vector3.zero },
                ["RightLeg"] = new[] { Vector3.zero, new Vector3(16f,0f,0f), new Vector3(5f,0f,0f), Vector3.zero, Vector3.zero },
            });
            return clips;

            void AddClip(string name, float length, bool loop, float[] times,
                Dictionary<string, Vector3[]> rotations)
            {
                var path = folder + "/" + name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null) AssetDatabase.DeleteAsset(path);
                clip = new AnimationClip { name = name, frameRate = 30f, wrapMode = loop ? WrapMode.Loop : WrapMode.Once };
                clip.legacy = false;
                foreach (var (boneName, offsets) in rotations)
                {
                    var bone = Find(boneName);
                    if (bone == null || offsets.Length != times.Length) continue;
                    var rest = bone.localRotation;
                    var quats = offsets.Select(e => rest * Quaternion.Euler(e)).ToArray();
                    SetQuaternion(AnimationUtility.CalculateTransformPath(bone, rig.transform), quats);
                }
                clip.EnsureQuaternionContinuity();
                AssetDatabase.CreateAsset(clip, path);
                clips.Add(clip);

                void SetQuaternion(string bindingPath, Quaternion[] quats)
                {
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bindingPath, typeof(Transform), "m_LocalRotation.x"), Curve(times, quats, q => q.x));
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bindingPath, typeof(Transform), "m_LocalRotation.y"), Curve(times, quats, q => q.y));
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bindingPath, typeof(Transform), "m_LocalRotation.z"), Curve(times, quats, q => q.z));
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bindingPath, typeof(Transform), "m_LocalRotation.w"), Curve(times, quats, q => q.w));
                }
            }
        }

        private static AnimationCurve Curve(float[] times, Quaternion[] values, Func<Quaternion, float> component)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(times[i], component(values[i]));
            return new AnimationCurve(keys);
        }

        private static HumanBone Human(string humanName, string boneName)
        {
            return new HumanBone
            {
                humanName = humanName,
                boneName = boneName,
                limit = new HumanLimit { useDefaultValues = true }
            };
        }

        private static void AddClips(string path, List<AnimationClip> output)
        {
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                if (clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) output.Add(clip);
        }

        private static void AddRootLockedReadyAnticipation(List<AnimationClip> output)
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(ReadyAnticipationPath)
                .OfType<AnimationClip>().FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
            if (source == null) return;

            // Meshy's retarget animates the candidate's Hips position 30–37 m
            // above bind. Preserve its joint rotations, but let the game motor
            // own travel by removing only those root-bone translation curves.
            var path = ReadyAnticipationLockedPath;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
            var candidate = UnityEngine.Object.Instantiate(source);
            candidate.name = "Splat_T25_MeshyReadyAnticipation_RootLocked_Iter1827";
            var removed = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(candidate)
                         .Where(b => string.Equals(b.path, "Armature/Hips", StringComparison.Ordinal)
                                     && b.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal)).ToArray())
            {
                AnimationUtility.SetEditorCurve(candidate, binding, null);
                removed++;
            }
            AssetDatabase.CreateAsset(candidate, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"SPLAT_T25_READY_ROOT_LOCKED source={source.name} removedHipsPositionCurves={removed} path={path}");
            output.Add(candidate);
        }

        private static Bounds CalculateBounds(Renderer[] renderers)
        {
            var found = false;
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
        }

        // Approximate sole clearance by tracking the lower 8% of the imported
        // skinned mesh through each sampled pose. This is a staged geometry
        // diagnostic only; the capture has no player motor or gameplay surface.
        private static float MeasureLowerMeshRegion(SkinnedMeshRenderer[] skins)
        {
            var lowest = float.PositiveInfinity;
            foreach (var skin in skins)
            {
                if (skin == null || skin.sharedMesh == null) continue;
                var source = skin.sharedMesh.vertices;
                var sourceBounds = skin.sharedMesh.bounds;
                var cutoff = sourceBounds.min.y + sourceBounds.size.y * 0.08f;
                var baked = new Mesh();
                skin.BakeMesh(baked);
                var posed = baked.vertices;
                var count = Mathf.Min(source.Length, posed.Length);
                for (var i = 0; i < count; i++)
                {
                    if (source[i].y > cutoff) continue;
                    lowest = Mathf.Min(lowest, skin.transform.TransformPoint(posed[i]).y);
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return float.IsInfinity(lowest) ? float.NaN : lowest;
        }

        private static Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            return material;
        }

        private static void CreateLight(string name, Vector3 position, float intensity, float size)
        {
            var lightObject = new GameObject(name);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Rectangle;
            light.intensity = intensity;
            light.range = 20f;
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.LookRotation(-position.normalized, Vector3.up);
            light.areaSize = new Vector2(size, size);
        }
    }
}
