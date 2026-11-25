#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Reviews the Iter1992 Meshy character with its Generic rig by sampling
    /// exact-path imported clips through a review-only Animator Controller.
    /// </summary>
    public static class CaptureSplatIter1992GenericReview
    {
        private const string AssetRoot = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1992/";
        private const string RigPath = AssetRoot + "Splat_Iter1992_Rigged.fbx";
        private const string TexturePath = AssetRoot + "Splat_Iter1992_base_color.png";
        private const string ReviewFolder = AssetRoot + "UnityReview_Iter2076";
        private const string OutputFolder = "Logs/score-comparison/splat-meshy-generic-rig-review-iter2076";
        private const string RootNeutralizedOutputFolder = "Logs/score-comparison/splat-meshy-generic-root-neutralized-review-iter2077-final";
        private const int Width = 480;
        private const int Height = 520;
        private static readonly float[] SampleFractions = { 0f, 0.2f, 0.4f, 0.6f, 0.8f };
        private static readonly string[] ClipPaths =
        {
            AssetRoot + "Splat_Iter1992_IdleBreath.fbx",
            AssetRoot + "Splat_Iter1992_Walk.fbx",
            AssetRoot + "Splat_Iter1992_Run.fbx",
            AssetRoot + "Splat_Iter1992_IdleAndRegularJump.fbx",
            AssetRoot + "Splat_Iter1992_HopLand.fbx",
        };

        [MenuItem("Space & Crafts/Audit Splat Iteration 1992 Rig and Clip Paths (Iter 2076)")]
        public static void AuditImportPaths()
        {
            var outputPath = ReviewFolder + "/import-structure-review.txt";
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder();
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (rig == null) throw new FileNotFoundException("Missing Iter1992 rig FBX", RigPath);

            var rigImporter = AssetImporter.GetAtPath(RigPath) as ModelImporter;
            report.AppendLine($"Rig importer: animationType={rigImporter?.animationType.ToString() ?? "missing"}; importAnimation={rigImporter?.importAnimation.ToString() ?? "missing"}; avatarSetup={rigImporter?.avatarSetup.ToString() ?? "missing"}.");
            var avatars = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().ToArray();
            report.AppendLine($"Avatar assets={avatars.Length}; " + string.Join("; ", avatars.Select(avatar => $"{avatar.name}: valid={avatar.isValid}, human={avatar.isHuman}")));
            var animators = rig.GetComponentsInChildren<Animator>(true);
            report.AppendLine($"Animator components={animators.Length}; " + string.Join("; ", animators.Select(animator => $"{animator.name}: avatar={(animator.avatar != null ? animator.avatar.name : "none")}, valid={animator.avatar != null && animator.avatar.isValid}, human={animator.avatar != null && animator.avatar.isHuman}")));

            var hierarchyPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transform in rig.GetComponentsInChildren<Transform>(true))
                hierarchyPaths.Add(AnimationUtility.CalculateTransformPath(transform, rig.transform));
            report.AppendLine($"Rig hierarchy transforms={hierarchyPaths.Count}; skinnedMeshes={rig.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}.");
            foreach (var transform in rig.GetComponentsInChildren<Transform>(true))
            {
                var path = AnimationUtility.CalculateTransformPath(transform, rig.transform);
                report.AppendLine($"bone {path} [{transform.name}] localPosition={transform.localPosition:F4}");
            }

            foreach (var clipPath in ClipPaths)
            {
                var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
                report.AppendLine($"Clip importer {clipPath}: animationType={importer?.animationType.ToString() ?? "missing"}; importAnimation={importer?.importAnimation.ToString() ?? "missing"}; avatarSetup={importer?.avatarSetup.ToString() ?? "missing"}.");
                var clips = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var transformBindings = bindings.Where(binding => binding.type == typeof(Transform)
                        && (binding.propertyName.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0
                            || binding.propertyName.IndexOf("Rotation", StringComparison.OrdinalIgnoreCase) >= 0
                            || binding.propertyName.IndexOf("Scale", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    var exact = transformBindings.Count(binding => hierarchyPaths.Contains(binding.path));
                    report.AppendLine($"Clip {clip.name}: {clip.length:F3}s; bindings={bindings.Length}; transformBindings={transformBindings.Length}; exactRigPathMatches={exact}/{transformBindings.Length}.");
                    foreach (var binding in transformBindings.Where(binding => !hierarchyPaths.Contains(binding.path)).Take(12))
                        report.AppendLine($"  MISSING PATH {binding.path} :: {binding.propertyName}");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(outputPath, report.ToString());
            Debug.Log($"[Splat Iter1992 Rig Audit] Wrote import and clip-path evidence to {outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Capture Splat Iteration 1992 Generic-Rig Review (Iter 2076)")]
        public static void Capture()
        {
            Directory.CreateDirectory(Path.GetFullPath(OutputFolder));
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder();
            var failurePath = Path.Combine(Path.GetFullPath(OutputFolder), "CAPTURE_FAILURE.txt");
            if (File.Exists(failurePath)) File.Delete(failurePath);
            var tempMaterials = new List<Material>();
            RenderTexture target = null;
            Texture2D scratch = null;
            GameObject instance = null;
            Scene reviewScene = default;
            var oldScene = SceneManager.GetActiveScene();
            var exitCode = 0;

            try
            {
                var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                if (rigAsset == null) throw new FileNotFoundException("Missing Iter1992 rig FBX", RigPath);
                var rigImporter = AssetImporter.GetAtPath(RigPath) as ModelImporter;
                if (rigImporter == null || rigImporter.animationType != ModelImporterAnimationType.Generic)
                    throw new InvalidOperationException("Iter1992 rig is not imported as a Generic rig.");
                var rigPaths = new HashSet<string>(rigAsset.GetComponentsInChildren<Transform>(true)
                    .Select(transform => AnimationUtility.CalculateTransformPath(transform, rigAsset.transform)), StringComparer.Ordinal);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (texture == null) throw new FileNotFoundException("Missing Iter1992 base-color map", TexturePath);

                var samples = new List<ClipSample>();
                var controllerPath = ReviewFolder + "/Splat_Iter1992_GenericReview.controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                    AssetDatabase.DeleteAsset(controllerPath);
                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var stateMachine = controller.layers[0].stateMachine;

                foreach (var clipPath in ClipPaths)
                {
                    var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
                    report.AppendLine($"Importer {clipPath}: animationType={importer?.animationType.ToString() ?? "missing"}; importAnimation={importer?.importAnimation.ToString() ?? "missing"}; avatarSetup={importer?.avatarSetup.ToString() ?? "missing"}.");
                    if (importer == null || importer.animationType != ModelImporterAnimationType.Generic)
                        throw new InvalidOperationException("Expected a Generic animation importer for " + clipPath);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>()
                        .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    foreach (var clip in clips)
                    {
                        var transformBindings = AnimationUtility.GetCurveBindings(clip)
                            .Where(binding => binding.type == typeof(Transform)
                                && (binding.propertyName.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0
                                    || binding.propertyName.IndexOf("Rotation", StringComparison.OrdinalIgnoreCase) >= 0
                                    || binding.propertyName.IndexOf("Scale", StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToArray();
                        var exactMatches = transformBindings.Count(binding => rigPaths.Contains(binding.path));
                        report.AppendLine($"Clip {clipPath}/{clip.name}: length={clip.length:F3}s; looping={clip.isLooping}; transformBindings={transformBindings.Length}; exactRigPathMatches={exactMatches}/{transformBindings.Length}.");
                        if (transformBindings.Length == 0 || exactMatches != transformBindings.Length)
                            throw new InvalidOperationException($"Clip {clip.name} does not bind exactly to the candidate rig ({exactMatches}/{transformBindings.Length}).");
                        var stateName = MakeUniqueStateName(stateMachine, Path.GetFileNameWithoutExtension(clipPath) + "_" + clip.name);
                        var state = stateMachine.AddState(stateName);
                        state.motion = clip;
                        state.writeDefaultValues = true;
                        if (stateMachine.defaultState == null) stateMachine.defaultState = state;
                        samples.Add(new ClipSample(stateName, clip.name, clip.length, clip, false));
                        report.AppendLine($"Clip {clipPath}/{clip.name}: length={clip.length:F3}s; looping={clip.isLooping}; bindings={AnimationUtility.GetCurveBindings(clip).Length}.");

                        if (clipPath == ClipPaths[0] || clipPath == ClipPaths[4])
                        {
                            AppendRootTranslationSummary(report, clip);
                            var neutralized = CreateRootNeutralizedReviewClip(clip, rigAsset, controller);
                            var correctedStateName = MakeUniqueStateName(stateMachine, stateName + "_ROOT_NEUTRALIZED");
                            var correctedState = stateMachine.AddState(correctedStateName);
                            correctedState.motion = neutralized;
                            correctedState.writeDefaultValues = true;
                            samples.Add(new ClipSample(correctedStateName, clip.name + " (root translation neutralized for review only)", clip.length, neutralized, true));
                        }
                    }
                    if (clips.Length == 0) throw new InvalidOperationException("No animation clips imported from " + clipPath);
                }

                if (samples.Count == 0) throw new InvalidOperationException("No Iter1992 clips were imported.");
                AssetDatabase.SaveAssets();
                reviewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                instance = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, reviewScene);
                if (instance == null) throw new InvalidOperationException("Could not instantiate the Iter1992 rig model.");
                instance.name = "Splat_Iter1992_ReviewOnly";
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.enabled = true;
                // Generic clips bind by the exact Transform paths checked above;
                // no Humanoid Avatar is required or synthesized here.
                animator.avatar = null;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;

                var material = CreateTexturedMaterial(texture);
                tempMaterials.Add(material);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                }

                AddDirectionalLight(reviewScene, "ReviewKey", new Vector3(35f, -30f, 0f), 1.15f, new Color(1f, 0.96f, 0.91f));
                AddDirectionalLight(reviewScene, "ReviewFill", new Vector3(15f, 145f, 0f), 0.55f, new Color(0.74f, 0.84f, 1f));
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.45f, 1f);
                RenderSettings.skybox = null;

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "ReviewFloorTopY0";
                floor.transform.position = new Vector3(0f, -0.05f, 0f);
                floor.transform.localScale = new Vector3(4f, 0.10f, 4f);
                SceneManager.MoveGameObjectToScene(floor, reviewScene);
                var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = "SplatIter1992ReviewFloor"
                };
                SetColor(floorMaterial, new Color(0.31f, 0.35f, 0.41f, 1f));
                tempMaterials.Add(floorMaterial);
                floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    name = "SplatIter1992GenericReviewRT"
                };
                target.Create();
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                var cameraObject = new GameObject("SplatIter1992GenericReviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, reviewScene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.72f, 0.78f, 0.85f, 1f);
                camera.orthographic = true;
                camera.aspect = Width / (float)Height;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.targetTexture = target;

                animator.Rebind();
                animator.Update(0f);
                // The initial state can itself contain a bad root translation;
                // derive the camera from the model origin so that displacement
                // remains visible instead of silently reframing it out of view.
                var center = instance.transform.position + Vector3.up * 0.75f;
                camera.orthographicSize = 1.75f;
                report.AppendLine("Review frame: Unity Animator with the candidate's Generic rig and exact binding paths; root motion disabled; camera locked to model origin at y=0.75; neutral floor top y=0.");
                report.AppendLine("Foot minimum is a proxy from vertices weighted to bones with Foot/Toe/Ankle in the name; it does not simulate platform collision.");

                foreach (var sample in samples)
                {
                    if (sample.RootTranslationNeutralized) continue;
                    var safeState = SafeName(sample.StateName);
                    foreach (var fraction in SampleFractions)
                    {
                        animator.Rebind();
                        animator.Play(sample.StateName, 0, fraction);
                        animator.Update(0f);
                        Physics.SyncTransforms();
                        var bounds = GetRendererBounds(instance);
                        var footMin = MeasureFootWeightedMinY(instance, out var footVertices, out var footBones);
                        report.AppendLine($"Pose {safeState}_{Mathf.RoundToInt(fraction * 100f):D3}: normalized={fraction:F2}; renderMinY={bounds.min.y:F4}; footWeightedMinY={footMin:F4}; footVertices={footVertices}; footBones=[{string.Join(",", footBones)}]; rendererBounds={bounds.center:F3}/{bounds.size:F3}.");

                        camera.transform.position = center + new Vector3(0.04f, 0.12f, 5f);
                        camera.transform.LookAt(center);
                        camera.Render();
                        RenderTexture.active = target;
                        scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        scratch.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(Path.GetFullPath(OutputFolder), $"{safeState}_{Mathf.RoundToInt(fraction * 100f):D3}_front.png"), scratch.EncodeToPNG());
                    }
                }

                File.WriteAllText(Path.Combine(Path.GetFullPath(OutputFolder), "REVIEW.txt"),
                    "Splat Iter1992 Generic Animator clip review. One frame per normalized sample; this is not continuous playback, gameplay, collision, or human play. Root-neutralized samples are temporary review-only variants, not production clips.\n" + report);
                Directory.CreateDirectory(Path.GetFullPath(RootNeutralizedOutputFolder));
                foreach (var sample in samples.Where(sample => sample.RootTranslationNeutralized))
                {
                    foreach (var fraction in SampleFractions)
                    {
                        animator.Rebind();
                        animator.Play(sample.StateName, 0, fraction);
                        animator.Update(0f);
                        var bounds = GetRendererBounds(instance);
                        var footMin = MeasureFootWeightedMinY(instance, out var footVertices, out var footBones);
                        report.AppendLine($"Root-neutralized pose {SafeName(sample.StateName)}_{Mathf.RoundToInt(fraction * 100f):D3}: renderMinY={bounds.min.y:F4}; footWeightedMinY={footMin:F4}; rendererBounds={bounds.center:F3}/{bounds.size:F3}.");
                        camera.transform.position = center + new Vector3(0.04f, 0.12f, 5f);
                        camera.transform.LookAt(center);
                        camera.Render();
                        RenderTexture.active = target;
                        scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        scratch.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(Path.GetFullPath(RootNeutralizedOutputFolder), $"{SafeName(sample.StateName)}_{Mathf.RoundToInt(fraction * 100f):D3}_front.png"), scratch.EncodeToPNG());
                    }
                }
                File.WriteAllText(Path.Combine(Path.GetFullPath(RootNeutralizedOutputFolder), "REVIEW.txt"),
                    "Temporary, non-production diagnostic: the root Hips local-position curves were held at the rig bind-pose value while other imported curves were retained. These static samples do not establish in-game animation quality or collision contact.\n" + report);
                AssetDatabase.Refresh();
                var diagnosticCount = samples.Count(sample => sample.RootTranslationNeutralized);
                Debug.Log($"[Splat Iter1992 Generic Rig Review] Captured {samples.Count - diagnosticCount} original clips × {SampleFractions.Length} normalized samples in {OutputFolder}, plus {diagnosticCount} root-neutralized review-only variants in {RootNeutralizedOutputFolder}; no production prefab or scene changed.");
            }
            catch (Exception exception)
            {
                File.WriteAllText(failurePath, exception + "\n" + report);
                Debug.LogException(exception);
                exitCode = 2;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                foreach (var material in tempMaterials) if (material != null) UnityEngine.Object.DestroyImmediate(material);
                if (scratch != null) UnityEngine.Object.DestroyImmediate(scratch);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                RenderTexture.active = null;
                if (reviewScene.IsValid()) EditorSceneManager.CloseScene(reviewScene, true);
                if (oldScene.IsValid()) EditorSceneManager.SetActiveScene(oldScene);
            }
            EditorApplication.Exit(exitCode);
        }

        private static string MakeUniqueStateName(AnimatorStateMachine machine, string baseName)
        {
            var candidate = SafeName(baseName);
            var suffix = 2;
            while (machine.states.Any(entry => entry.state.name == candidate)) candidate = SafeName(baseName) + "_" + suffix++;
            return candidate;
        }

        private static string SafeName(string value) => string.Concat(value.Select(character =>
            char.IsLetterOrDigit(character) || character == '_' ? character : '_'));

        private static AnimationClip CreateRootNeutralizedReviewClip(AnimationClip source, GameObject rig, AnimatorController controller)
        {
            var hips = rig.transform.Find("Armature/Hips");
            if (hips == null) throw new InvalidOperationException("Expected the Iter1992 Generic rig path Armature/Hips.");
            var clip = new AnimationClip
            {
                name = source.name + "_RootTranslationNeutralized_REVIEW_ONLY",
                frameRate = source.frameRate,
                wrapMode = source.wrapMode,
                hideFlags = HideFlags.HideInHierarchy
            };
            AnimationUtility.SetAnimationClipSettings(clip, AnimationUtility.GetAnimationClipSettings(source));
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null) continue;
                if (binding.type == typeof(Transform)
                    && binding.path == "Armature/Hips"
                    && binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                {
                    var axis = binding.propertyName.Substring(binding.propertyName.Length - 1);
                    var rest = axis == "x" ? hips.localPosition.x : axis == "y" ? hips.localPosition.y : hips.localPosition.z;
                    curve = new AnimationCurve(new Keyframe(0f, rest), new Keyframe(source.length, rest));
                    curve.preWrapMode = WrapMode.ClampForever;
                    curve.postWrapMode = WrapMode.ClampForever;
                }
                else
                {
                    curve = new AnimationCurve(curve.keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode };
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            AssetDatabase.AddObjectToAsset(clip, controller);
            return clip;
        }

        private static void AppendRootTranslationSummary(StringBuilder report, AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(binding =>
                binding.type == typeof(Transform) && binding.path == "Armature/Hips"
                && binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal)))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0) continue;
                var values = curve.keys.Select(key => key.value).ToArray();
                report.AppendLine($"  Hips root curve {binding.propertyName}: keys={curve.length}; range=[{values.Min():F4},{values.Max():F4}]; first={curve.Evaluate(0f):F4}; last={curve.Evaluate(clip.length):F4}.");
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Material CreateTexturedMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No lit shader found for the review material.");
            var material = new Material(shader) { name = "SplatIter1992ReviewBaseColor" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            SetColor(material, Color.white);
            return material;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void AddDirectionalLight(Scene scene, string name, Vector3 euler, float intensity, Color color)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            go.transform.rotation = Quaternion.Euler(euler);
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer != null && renderer.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Review character has no enabled renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static float MeasureFootWeightedMinY(GameObject root, out int vertexCount, out string[] footBones)
        {
            var minY = float.PositiveInfinity;
            vertexCount = 0;
            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin == null || skin.sharedMesh == null || skin.bones == null) continue;
                var mesh = skin.sharedMesh;
                var weights = mesh.boneWeights;
                if (weights == null || weights.Length == 0) continue;
                var indices = new HashSet<int>();
                for (var i = 0; i < skin.bones.Length; i++)
                {
                    var bone = skin.bones[i];
                    if (bone == null) continue;
                    var lower = bone.name.ToLowerInvariant();
                    if (!lower.Contains("foot") && !lower.Contains("toe") && !lower.Contains("ankle")) continue;
                    indices.Add(i);
                    names.Add(bone.name);
                }
                if (indices.Count == 0) continue;
                var baked = new Mesh { name = "SplatIter1992FootContactBake" };
                try
                {
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices;
                    for (var i = 0; i < Mathf.Min(vertices.Length, weights.Length); i++)
                    {
                        var weight = weights[i];
                        if (!WeightedToFoot(weight, indices)) continue;
                        minY = Mathf.Min(minY, skin.transform.TransformPoint(vertices[i]).y);
                        vertexCount++;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            footBones = names.ToArray();
            return float.IsPositiveInfinity(minY) ? float.NaN : minY;
        }

        private static bool WeightedToFoot(BoneWeight weight, HashSet<int> indices) =>
            weight.weight0 > 0.05f && indices.Contains(weight.boneIndex0)
            || weight.weight1 > 0.05f && indices.Contains(weight.boneIndex1)
            || weight.weight2 > 0.05f && indices.Contains(weight.boneIndex2)
            || weight.weight3 > 0.05f && indices.Contains(weight.boneIndex3);

        private sealed class ClipSample
        {
            public readonly string StateName;
            public readonly string ClipName;
            public readonly float Length;
            public readonly AnimationClip Clip;
            public readonly bool RootTranslationNeutralized;

            public ClipSample(string stateName, string clipName, float length, AnimationClip clip, bool rootTranslationNeutralized)
            {
                StateName = stateName;
                ClipName = clipName;
                Length = length;
                Clip = clip;
                RootTranslationNeutralized = rootTranslationNeutralized;
            }
        }
    }
}
#endif
