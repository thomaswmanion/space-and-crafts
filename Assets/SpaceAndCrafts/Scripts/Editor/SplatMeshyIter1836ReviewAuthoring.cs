#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports the approved-design Meshy Splat model and its matching rig/locomotion
    /// into a completely isolated review prefab. Production assets are untouched.
    /// </summary>
    public static class SplatMeshyIter1836ReviewAuthoring
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyImageCandidate_Iter1836_ReviewOnly";
        private const string RigPath = Folder + "/Rigged_Character_Iter1836.fbx";
        private const string WalkPath = Folder + "/Walking_Iter1836.fbx";
        private const string RunPath = Folder + "/Running_Iter1836.fbx";
        private const string ActionPath = Folder + "/Actions_Iter1836.fbx";
        private const string IdlePath = Folder + "/Idle_7_Iter1836.fbx";
        private const string ExistingIdleSourcePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1800/splat_idle_variations.fbx";
        private const string RawIdlePath = Folder + "/Splat_Idle7_Iter1836_Raw_ReviewOnly.anim";
        private const string ReusedIdlePath = Folder + "/Splat_Idle7_FaceForward_Iter1836_ReviewOnly.anim";
        private const string BaseColorPath = Folder + "/Splat_ApprovedDesign_Iter1836_Remesh300k_textures/base_color.png";
        private const string ReviewMaterialPath = Folder + "/Splat_Iter1836_ReviewOnly.mat";
        private const string ControllerPath = Folder + "/Splat_Iter1836_ReviewOnly.controller";
        private const string PrefabPath = Folder + "/Splat_Iter1836_ReviewOnly.prefab";
        private const float TargetHeight = 1.15f;

        [MenuItem("Space & Crafts/Prepare Splat Iter1836 Review Only")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var path in new[] { RigPath, WalkPath, RunPath, ActionPath, IdlePath })
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    throw new InvalidOperationException("Missing Iter1836 Meshy Splat file: " + path);

            ConfigureImporter(RigPath, null, false).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(value => value != null && value.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Iter1836 rig FBX did not create a valid Generic Avatar.");

            foreach (var path in new[] { WalkPath, RunPath, ActionPath, IdlePath })
                ConfigureImporter(path, avatar, true).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var previousIdle = FindClip(ExistingIdleSourcePath, "Idle_7");
            AssetDatabase.DeleteAsset(RawIdlePath);
            AssetDatabase.DeleteAsset(ReusedIdlePath);
            var reusedIdle = new AnimationClip { name = "Idle_7_OriginalMeshy_Raw_ReviewOnly" };
            EditorUtility.CopySerialized(previousIdle, reusedIdle);
            AssetDatabase.CreateAsset(reusedIdle, RawIdlePath);
            var faceForwardIdle = CropLoopingIdle(reusedIdle, reusedIdle.length * 0.84f, reusedIdle.length * 0.99f);
            AssetDatabase.CreateAsset(faceForwardIdle, ReusedIdlePath);

            var idle = FindClip(ReusedIdlePath, "Splat_Idle7_FaceForward_Iter1836_ReviewOnly");
            var wave = FindClip(ActionPath, "Wave_One_Hand", "Wave One Hand");
            var jump = FindClip(ActionPath, "Happy_jump_m", "Happy_Jump_M");
            var walk = FindClip(WalkPath, "walking");
            var run = FindClip(RunPath, "running");
            foreach (var path in new[] { RigPath, WalkPath, RunPath, ActionPath, IdlePath })
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => clip != null && clip.length > 0.01f).ToArray();
                Debug.Log($"[Splat Iter1836] imported {Path.GetFileName(path)} clips={string.Join(", ", clips.Select(clip => clip.name + "(" + clip.length.ToString("F2") + "s)"))}");
            }

            var controller = CreateController(idle, walk, run, jump, wave);
            CreateReviewPrefab(avatar, controller, CreateReviewMaterial());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Splat Iter1836] Created review-only prefab/controller; production assets and scenes were not changed.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Capture Splat Iter1836 Review Frames")]
        public static void CaptureReview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing review prefab; run Prepare first.");
            var output = Path.GetFullPath("Logs/score-comparison/splat-meshy-image-iter1836");
            Directory.CreateDirectory(output);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate review prefab.");

            var clips = new[]
            {
                ("idle_forward", FindClip(ReusedIdlePath, "Splat_Idle7_FaceForward_Iter1836_ReviewOnly"), 0.22f),
                ("idle_forward", FindClip(ReusedIdlePath, "Splat_Idle7_FaceForward_Iter1836_ReviewOnly"), 0.52f),
                ("idle_forward", FindClip(ReusedIdlePath, "Splat_Idle7_FaceForward_Iter1836_ReviewOnly"), 0.82f),
                ("wave", FindClip(ActionPath, "Wave_One_Hand", "Wave One Hand"), 0.35f),
                ("wave", FindClip(ActionPath, "Wave_One_Hand", "Wave One Hand"), 0.72f),
                ("walk", FindClip(WalkPath, "walking"), 0.25f),
                ("walk", FindClip(WalkPath, "walking"), 0.72f),
                ("run", FindClip(RunPath, "running"), 0.25f),
                ("run", FindClip(RunPath, "running"), 0.72f),
                ("jump", FindClip(ActionPath, "Happy_jump_m", "Happy_Jump_M"), 0.35f),
                ("jump", FindClip(ActionPath, "Happy_jump_m", "Happy_Jump_M"), 0.72f),
            };

            var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            var transformSnapshot = instance.GetComponentsInChildren<Transform>(true).ToDictionary(
                item => item, item => (item.localPosition, item.localRotation, item.localScale));
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Review-only flat contact surface";
            ground.transform.position = new Vector3(0f, -0.06f, 0f);
            ground.transform.localScale = new Vector3(4f, 0.12f, 4f);
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMaterial.color = new Color(0.36f, 0.43f, 0.52f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var cameraObject = new GameObject("Splat Iter1836 Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.77f, 0.84f, 0.92f);
            camera.orthographic = true;
            camera.orthographicSize = 0.83f;
            camera.targetTexture = target;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 20f;
            var lightObject = new GameObject("Splat Iter1836 Review Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightObject.transform.rotation = Quaternion.Euler(35f, -28f, 0f);
            var oldAmbient = RenderSettings.ambientLight;
            var oldAmbientMode = RenderSettings.ambientMode;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
            var report = new List<string>
            {
                "Iter1836 is a staged Meshy character preview. These are editor-sampled still poses, not gameplay or human-play verification.",
                "Source model was generated from Splat_ApprovedCapEyePortrait.png, remeshed to a 150k quad target (about 300k triangles), and rigged by Meshy.",
            };
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.64f);
                for (var i = 0; i < clips.Length; i++)
                {
                    foreach (var pose in transformSnapshot)
                    {
                        pose.Key.localPosition = pose.Value.localPosition;
                        pose.Key.localRotation = pose.Value.localRotation;
                        pose.Key.localScale = pose.Value.localScale;
                    }
                    instance.transform.position = Vector3.zero;
                    instance.transform.rotation = Quaternion.identity;
                    var sample = clips[i];
                    var sampleTime = Mathf.Clamp01(sample.Item3) * sample.Item2.length;
                    sample.Item2.SampleAnimation(instance, sampleTime);
                    foreach (var pose in transformSnapshot) pose.Key.localScale = pose.Value.localScale;

                    var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
                    for (var r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                    var footHeight = bounds.min.y;
                    var viewOffsets = new[] { new Vector3(0.06f, 0.07f, 3.8f), new Vector3(-2.35f, 0.16f, 3.35f) };
                    for (var view = 0; view < viewOffsets.Length; view++)
                    {
                        camera.transform.position = new Vector3(0f, 0.575f, 0f) + viewOffsets[view];
                        camera.transform.LookAt(new Vector3(0f, 0.575f, 0f));
                        camera.Render();
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
                        image.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(output, $"{i:00}_{sample.Item1}_{Mathf.RoundToInt(sample.Item3 * 100f):D3}_{(view == 0 ? "front" : "threequarter")}.png"), image.EncodeToPNG());
                    }
                    report.Add($"{i:00} {sample.Item1} {sample.Item2.name} at {sample.Item3:P0} (sampleTime={sampleTime:F3}s, clipLength={sample.Item2.length:F3}s); boundsMinY={footHeight:F3}m; scaleCurves={AnimationUtility.GetCurveBindings(sample.Item2).Count(binding => binding.propertyName.StartsWith("m_LocalScale"))}");
                }
                File.WriteAllLines(Path.Combine(output, "review.txt"), report);
                Debug.Log("[Splat Iter1836] Saved review-only pose frames under " + output);
            }
            finally
            {
                RenderTexture.active = oldActive;
                RenderSettings.ambientLight = oldAmbient;
                RenderSettings.ambientMode = oldAmbientMode;
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(ground);
                Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static ModelImporter ConfigureImporter(string path, Avatar avatar, bool animation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Not an FBX model: " + path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = avatar == null ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = animation;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = avatar == null
                ? ModelImporterMaterialImportMode.ImportViaMaterialDescription
                : ModelImporterMaterialImportMode.None;
            if (avatar != null)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    var loop = path == WalkPath || path == RunPath || path == IdlePath
                        || clip.name.Equals("Idle", StringComparison.OrdinalIgnoreCase);
                    clip.loopTime = loop;
                    clip.loopPose = loop;
                }
                importer.clipAnimations = clips;
            }
            return importer;
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump, AnimationClip wave)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Personality", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var blend = new BlendTree { name = "Iter1836 Idle Walk Run", blendType = BlendTreeType.Simple1D, blendParameter = "MoveSpeed", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(idle, 0f);
            blend.AddChild(walk, 0.45f);
            blend.AddChild(run, 2f);
            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = blend;
            machine.defaultState = locomotion;
            var jumpState = machine.AddState("Happy Jump");
            jumpState.motion = jump;
            var jumpIn = locomotion.AddTransition(jumpState);
            jumpIn.hasExitTime = false;
            jumpIn.duration = 0.08f;
            jumpIn.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var jumpOut = jumpState.AddTransition(locomotion);
            jumpOut.hasExitTime = true;
            jumpOut.exitTime = 0.93f;
            jumpOut.duration = 0.14f;
            var hello = machine.AddState("Friendly Wave");
            hello.motion = wave;
            var waveIn = locomotion.AddTransition(hello);
            waveIn.hasExitTime = false;
            waveIn.duration = 0.14f;
            waveIn.AddCondition(AnimatorConditionMode.If, 0f, "Personality");
            var waveOut = hello.AddTransition(locomotion);
            waveOut.hasExitTime = true;
            waveOut.exitTime = 0.94f;
            waveOut.duration = 0.16f;
            return controller;
        }

        private static Material CreateReviewMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            if (texture == null) throw new InvalidOperationException("Missing remeshed Splat base-color texture: " + BaseColorPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ReviewMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = "Splat_Iter1836_ReviewOnly",
                };
                AssetDatabase.CreateAsset(material, ReviewMaterialPath);
            }
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.color = Color.white;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
            return material;
        }

        private static void CreateReviewPrefab(Avatar avatar, AnimatorController controller, Material reviewMaterial)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Iter1836 rigged FBX.");
            instance.name = "Splat_Iter1836_ReviewOnly";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Iter1836 model has no renderers.");
            foreach (var renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(reviewMaterial, renderer.sharedMaterials.Length).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var fit = TargetHeight / Mathf.Max(0.01f, bounds.size.y);
            instance.transform.localScale *= fit;
            instance.transform.localPosition = new Vector3(0f, -bounds.min.y * fit, 0f);
            instance.transform.localRotation = Quaternion.identity;
            var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static AnimationClip FindClip(string path, params string[] names)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            foreach (var name in names)
            {
                var match = clips.FirstOrDefault(clip => clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null) return match;
            }
            throw new InvalidOperationException($"Missing clip [{string.Join(", ", names)}] in {path}. Imported: {string.Join(", ", clips.Select(clip => clip.name))}");
        }

        private static AnimationClip CropLoopingIdle(AnimationClip source, float start, float end)
        {
            if (source == null || end <= start || start < 0f || end > source.length)
                throw new InvalidOperationException("Invalid source window for the face-forward idle clip.");
            var clip = new AnimationClip
            {
                name = "Splat_Idle7_FaceForward_Iter1836_ReviewOnly",
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop,
            };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var keys = new List<Keyframe> { new Keyframe(0f, sourceCurve.Evaluate(start)) };
                foreach (var key in sourceCurve.keys)
                {
                    if (key.time > start && key.time < end)
                    {
                        var shifted = key;
                        shifted.time -= start;
                        keys.Add(shifted);
                    }
                }
                keys.Add(new Keyframe(end - start, sourceCurve.Evaluate(end)));
                var curve = new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = WrapMode.Loop,
                    postWrapMode = WrapMode.Loop,
                };
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding)
                    .Where(key => key.time >= start && key.time <= end)
                    .Select(key => new ObjectReferenceKeyframe { time = key.time - start, value = key.value })
                    .ToArray();
                if (keys.Length > 0) AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = end - start;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            return clip;
        }
    }
}
#endif
