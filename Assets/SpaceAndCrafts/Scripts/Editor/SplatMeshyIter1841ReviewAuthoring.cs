#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds an isolated review-only prefab for the Iter1841 Meshy Splat model.
    /// Does not modify the active player, production prefabs, or game scenes.
    /// </summary>
    public static class SplatMeshyIter1841ReviewAuthoring
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1841";
        private const string RigPath = Root + "/rigged_character.fbx";
        private const string WalkPath = Root + "/walking_fbx.fbx";
        private const string RunPath = Root + "/running_fbx.fbx";
        private const string SwiftRunPath = Root + "/ReviewOnly/Splat_Iter1841_SwiftRun_Retargeted.fbx";
        private const string ActionPath = Root + "/animation_fbx.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Splat_Meshy_Iter1841_base_color.png";
        private const string ReviewFolder = Root + "/ReviewOnly";
        private const string MaterialPath = ReviewFolder + "/Splat_Iter1841_ReviewOnly.mat";
        private const string ControllerPath = ReviewFolder + "/Splat_Iter1841_ReviewOnly.controller";
        private const string PrefabPath = ReviewFolder + "/Splat_Iter1841_ReviewOnly.prefab";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1841-import-review/import-review.txt";

        [MenuItem("Space & Crafts/Prepare Isolated Splat Meshy Iter1841 Review")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var path in new[] { RigPath, WalkPath, RunPath, ActionPath, SwiftRunPath, TexturePath })
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    throw new InvalidOperationException("Missing Iter1841 Meshy asset: " + path);

            ConfigureImporter(RigPath, null, false).SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(value => value != null && value.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Rigged FBX did not create a valid Generic Avatar.");

            ConfigureImporter(WalkPath, avatar, true).SaveAndReimport();
            ConfigureImporter(RunPath, avatar, true).SaveAndReimport();
            ConfigureImporter(SwiftRunPath, avatar, true).SaveAndReimport();
            ConfigureImporter(ActionPath, avatar, true).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var imported = new[] { RigPath, WalkPath, RunPath, SwiftRunPath, ActionPath }
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => clip != null && clip.length > 0.01f)
                    .Select(clip => $"{Path.GetFileName(path)} | {clip.name} | {clip.length:F3}s | " +
                                    $"scaleCurves={AnimationUtility.GetCurveBindings(clip).Count(binding => binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal))}"))
                .ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
            File.WriteAllLines(ReportPath, new[]
            {
                "Splat Meshy Iter1841 isolated import review",
                "This report describes Unity FBX importer results only; it is not gameplay or human-play verification.",
                "Avatar valid: " + avatar.isValid,
                "Clip imports:",
            }.Concat(imported));

            Directory.CreateDirectory(ReviewFolder);
            var idle = FindClip(ActionPath, "Idle");
            var jump = FindClip(ActionPath, "Happy_Jump_M", "Happy Jump", "Jump");
            var land = FindClip(ActionPath, "Vault_and_Land", "Vault and Land", "Dive_Down_and_Land");
            var stand = FindClip(ActionPath, "Stand_Up1", "Stand Up1", "Stand Up 1");
            var walk = FindClip(WalkPath, "walking");
            var run = FindClip(RunPath, "running");
            var swiftRun = FindClipOrFirst(SwiftRunPath, "run", "motion", "animation");
            var controller = CreateController(idle, walk, run, swiftRun, jump, land, stand);
            CreatePrefab(avatar, controller, CreateReviewMaterial());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Splat Iter1841] Review-only Generic Avatar, controller, and prefab created; production player/scenes untouched. " + PrefabPath);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Capture Isolated Splat Meshy Iter1841 Poses")]
        public static void CaptureReview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing review prefab; run Prepare first.");
            var samples = new[]
            {
                ("idle", FindClip(ActionPath, "Idle"), 0.15f), ("idle", FindClip(ActionPath, "Idle"), 0.50f),
                ("idle", FindClip(ActionPath, "Idle"), 0.85f),
                ("walk", FindClip(WalkPath, "walking"), 0.25f), ("walk", FindClip(WalkPath, "walking"), 0.75f),
                ("run", FindClip(RunPath, "running"), 0.25f), ("run", FindClip(RunPath, "running"), 0.75f),
                ("run_base", FindClip(RunPath, "running"), 0.00f), ("run_base", FindClip(RunPath, "running"), 0.125f),
                ("run_base", FindClip(RunPath, "running"), 0.25f), ("run_base", FindClip(RunPath, "running"), 0.375f),
                ("run_base", FindClip(RunPath, "running"), 0.50f), ("run_base", FindClip(RunPath, "running"), 0.625f),
                ("run_base", FindClip(RunPath, "running"), 0.75f), ("run_base", FindClip(RunPath, "running"), 0.875f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.00f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.125f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.25f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.375f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.50f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.625f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.75f),
                ("run_swift", FindClipOrFirst(SwiftRunPath, "run", "motion", "animation"), 0.875f),
                ("jump", FindClip(ActionPath, "happy_jump_m"), 0.25f), ("jump", FindClip(ActionPath, "happy_jump_m"), 0.65f),
                ("land", FindClip(ActionPath, "Vault_and_Land"), 0.35f), ("land", FindClip(ActionPath, "Vault_and_Land"), 0.75f),
                ("standup", FindClip(ActionPath, "Stand_Up1"), 0.35f), ("standup", FindClip(ActionPath, "Stand_Up1"), 0.75f),
            };
            var output = Path.GetFullPath("Logs/score-comparison/splat-meshy-iter1841");
            Directory.CreateDirectory(output);
            // This method runs in a separate -batchmode Unity process, so use a
            // fresh transient scene for reliable camera.Render output; no project
            // scene is opened from or saved to the user's editor session.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate review prefab.");
            var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var transformSnapshot = instance.GetComponentsInChildren<Transform>(true).ToDictionary(
                item => item, item => (item.localPosition, item.localRotation, item.localScale));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Iter1841 review-only contact plane";
            ground.transform.position = new Vector3(0f, -0.06f, 0f); ground.transform.localScale = new Vector3(4f, 0.12f, 4f);
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMaterial.color = new Color(0.40f, 0.48f, 0.56f); ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            var cameraObject = new GameObject("Iter1841 Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32); target.Create();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.77f, 0.84f, 0.92f);
            camera.orthographic = true; camera.orthographicSize = 0.88f; camera.targetTexture = target; camera.nearClipPlane = 0.02f; camera.farClipPlane = 20f;
            var lightObject = new GameObject("Iter1841 Review Key Light");
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f); lightObject.transform.rotation = Quaternion.Euler(35f, -28f, 0f);
            var oldAmbient = RenderSettings.ambientLight; var oldAmbientMode = RenderSettings.ambientMode;
            var oldActive = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
            var report = new System.Collections.Generic.List<string>
            {
                "Splat Meshy Iter1841 isolated Unity pose capture. Staged editor-sampled stills only; not gameplay or human-play verification.",
                "The low-Y measurement is the minimum animated Renderer.bounds above a flat review plane; it does not prove motor-driven contact on varied platforms.",
                "Clip | Sample | MinY (m) | RootScaleKeys",
            };
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.64f);
                for (var i = 0; i < samples.Length; i++)
                {
                    foreach (var pose in transformSnapshot)
                    { pose.Key.localPosition = pose.Value.localPosition; pose.Key.localRotation = pose.Value.localRotation; pose.Key.localScale = pose.Value.localScale; }
                    var sample = samples[i]; sample.Item2.SampleAnimation(instance, Mathf.Clamp01(sample.Item3) * sample.Item2.length);
                    foreach (var pose in transformSnapshot) pose.Key.localScale = pose.Value.localScale;
                    var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
                    for (var r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                    var scaleKeys = AnimationUtility.GetCurveBindings(sample.Item2).Count(binding => binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal));
                    report.Add($"{sample.Item1} | {sample.Item3:P0} | {bounds.min.y:F3} | {scaleKeys}");
                    var views = new[] { ("front", new Vector3(0.06f, 0.07f, 3.8f)), ("threequarter", new Vector3(-2.35f, 0.16f, 3.35f)) };
                    for (var view = 0; view < views.Length; view++)
                    {
                        camera.transform.position = new Vector3(0f, 0.72f, 0f) + views[view].Item2;
                        camera.transform.LookAt(new Vector3(0f, 0.72f, 0f)); camera.Render(); RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(output, $"{i:00}_{sample.Item1}_{Mathf.RoundToInt(sample.Item3 * 100f):D3}_{views[view].Item1}.png"), image.EncodeToPNG());
                    }
                }
                File.WriteAllLines(Path.Combine(output, "review.txt"), report);
                Debug.Log("[Splat Iter1841] Saved isolated pose frames under " + output);
            }
            finally
            {
                RenderTexture.active = oldActive; RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientMode = oldAmbientMode;
                Object.DestroyImmediate(instance); Object.DestroyImmediate(ground); Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); Object.DestroyImmediate(image);
                target.Release(); Object.DestroyImmediate(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static ModelImporter ConfigureImporter(string path, Avatar avatar, bool importAnimation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Unity could not import FBX: " + path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = avatar == null ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = importAnimation;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = avatar == null ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
            if (importAnimation)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    var name = (clip.name ?? "").ToLowerInvariant();
                    clip.loopTime = path == WalkPath || path == RunPath || path == SwiftRunPath || name.Contains("idle");
                    clip.loopPose = clip.loopTime;
                }
                importer.clipAnimations = clips;
            }
            return importer;
        }

        private static AnimationClip FindClip(string path, params string[] fragments)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            foreach (var fragment in fragments)
            {
                var found = clips.FirstOrDefault(clip => clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                if (found != null) return found;
            }
            throw new InvalidOperationException($"Missing clip [{string.Join(", ", fragments)}] in {path}. Imported: {string.Join(", ", clips.Select(clip => clip.name))}");
        }

        private static AnimationClip FindClipOrFirst(string path, params string[] fragments)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            foreach (var fragment in fragments)
            {
                var found = clips.FirstOrDefault(clip => clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                if (found != null) return found;
            }
            if (clips.Length > 0) return clips[0];
            throw new InvalidOperationException($"No animation clip imported from {path}.");
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip swiftRun,
            AnimationClip jump, AnimationClip land, AnimationClip stand)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (var parameter in new[] { "MoveSpeed" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
            foreach (var parameter in new[] { "Jump", "Land", "StandUp" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;
            var blend = new BlendTree { name = "Iter1841 Idle-Walk-Run", blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(idle, 0f); blend.AddChild(walk, 0.45f); blend.AddChild(run, 2f);
            if (swiftRun != null) blend.AddChild(swiftRun, 3f);
            var locomotion = sm.AddState("Locomotion"); locomotion.motion = blend; sm.defaultState = locomotion;
            var jumpState = sm.AddState("Jump"); jumpState.motion = jump;
            var landState = sm.AddState("Land"); landState.motion = land;
            var standState = sm.AddState("Stand Up"); standState.motion = stand;
            var toJump = locomotion.AddTransition(jumpState); toJump.hasExitTime = false; toJump.duration = 0.08f;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var toLand = jumpState.AddTransition(landState); toLand.hasExitTime = false; toLand.duration = 0.07f;
            toLand.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            var recover = landState.AddTransition(locomotion); recover.hasExitTime = true; recover.exitTime = 0.9f; recover.duration = 0.12f;
            var toStand = sm.AddAnyStateTransition(standState); toStand.hasExitTime = false; toStand.duration = 0.1f;
            toStand.AddCondition(AnimatorConditionMode.If, 0f, "StandUp");
            var standRecover = standState.AddTransition(locomotion); standRecover.hasExitTime = true; standRecover.exitTime = 0.9f; standRecover.duration = 0.12f;
            return controller;
        }

        private static Material CreateReviewMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "Splat_Iter1841_ReviewOnly" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.color = Color.white;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePrefab(Avatar avatar, AnimatorController controller, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (model == null) throw new InvalidOperationException("Rigged Iter1841 FBX failed to load as a model prefab.");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate rigged character into Preview Scene.");
                instance.name = "Splat_Iter1841_ReviewOnly";
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("No renderers in rigged character.");
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var scale = 1.5f / Mathf.Max(0.01f, bounds.size.y);
                instance.transform.localScale = Vector3.one * scale;
                instance.transform.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
                var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
                animator.avatar = avatar; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Update(0f);
                var folder = Path.GetDirectoryName(PrefabPath)!.Replace('\\', '/');
                Directory.CreateDirectory(folder);
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                Object.DestroyImmediate(instance);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
#endif
