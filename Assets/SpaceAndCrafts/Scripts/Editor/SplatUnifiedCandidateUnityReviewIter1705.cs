#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Isolated noninteractive import/render review for the Iteration 1705 Splat FBX.
    /// Does not modify the active prefab, production scenes, or candidate source file.
    /// </summary>
    public static class SplatUnifiedCandidateUnityReviewIter1705
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.fbx";
        private const string OutputDirectory = "Logs/score-comparison/splat-unified-candidate-unity-import-iter1705";

        public static void Capture()
        {
            Directory.CreateDirectory(OutputDirectory);
            var log = new StringBuilder();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Unity ModelImporter is unavailable for {ModelPath}");

            var importerSettingsChanged = false;
            if (!importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.importAnimation = true;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importerSettingsChanged = true;
            }

            // Idle and Run are sustained gameplay states; Jump and Land remain
            // one-shot poses. Keep those semantics in the FBX importer metadata
            // so a future AnimatorController does not freeze after one cycle.
            var clipSettings = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            for (var i = 0; i < clipSettings.Length; i++)
            {
                var clipName = clipSettings[i].name ?? string.Empty;
                var shouldLoop = clipName.EndsWith("Idle", StringComparison.OrdinalIgnoreCase) ||
                                 clipName.EndsWith("Run", StringComparison.OrdinalIgnoreCase);
                if (clipSettings[i].loopTime != shouldLoop || clipSettings[i].loopPose != shouldLoop)
                {
                    clipSettings[i].loopTime = shouldLoop;
                    clipSettings[i].loopPose = shouldLoop;
                    importerSettingsChanged = true;
                }
            }

            if (importerSettingsChanged)
            {
                importer.clipAnimations = clipSettings;
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
                throw new InvalidOperationException($"Unity could not load model asset {ModelPath}");

            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
            if (clips.Length == 0)
                throw new InvalidOperationException("The Unity FBX importer returned no animation clips.");

            log.AppendLine($"model={ModelPath}");
            log.AppendLine($"animationType={importer.animationType} importAnimation={importer.importAnimation} avatarSetup={importer.avatarSetup}");
            log.AppendLine($"clips={clips.Length}");
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                log.AppendLine($"clip={clip.name} length={clip.length:F3}s frameRate={clip.frameRate:F2} loop={clip.isLooping} curveBindings={bindings.Length}");
                foreach (var binding in bindings.Take(4))
                    log.AppendLine($"  binding={binding.path}:{binding.propertyName}");
                var shoulderBinding = bindings.FirstOrDefault(binding =>
                    binding.path.EndsWith("Arm_L", StringComparison.Ordinal) &&
                    binding.propertyName == "m_LocalRotation.x");
                if (!string.IsNullOrEmpty(shoulderBinding.path))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, shoulderBinding);
                    if (curve != null)
                        log.AppendLine($"  Arm_L.rotation.x samples={curve.Evaluate(0f):F3},{curve.Evaluate(clip.length * 0.25f):F3},{curve.Evaluate(clip.length * 0.5f):F3},{curve.Evaluate(clip.length * 0.75f):F3}");
                }
            }

            // Use a disposable empty scene in this batch process. Some SRPs skip
            // cameras in PreviewScenes when issuing StandardRequest renders.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Unity could not instantiate the isolated candidate preview.");

            var anim = instance.GetComponentInChildren<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            var importedAvatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (importedAvatar != null) anim.avatar = importedAvatar;
            anim.applyRootMotion = false;
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var boneCount = renderers.Sum(r => r.bones?.Length ?? 0);
            var meshCount = instance.GetComponentsInChildren<MeshFilter>(true).Length + renderers.Length;
            log.AppendLine($"animator={(anim != null)} avatar={(anim.avatar != null ? anim.avatar.name : "none")} avatarValid={(anim.avatar != null && anim.avatar.isValid)} skinnedRenderers={renderers.Length} meshCount={meshCount} summedSkinnedBones={boneCount}");
            foreach (var transform in instance.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Splat_UnifiedRig" || t.name == "Arm_L"))
                log.AppendLine($"hierarchy={AnimationUtility.CalculateTransformPath(transform, instance.transform)}");

            ApplyPreviewMaterials(instance);
            var bounds = CalculateBounds(instance);
            log.AppendLine($"boundsCenter={bounds.center} boundsSize={bounds.size} boundsMinY={bounds.min.y:F4}");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Review floor (capture only)";
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.015f, bounds.center.z);
            floor.transform.localScale = Vector3.one * Mathf.Max(0.5f, Mathf.Max(bounds.size.x, bounds.size.z));
            floor.GetComponent<Collider>().enabled = false;
            SetColor(floor.GetComponent<Renderer>(), new Color(0.12f, 0.15f, 0.23f));

            var lightObject = new GameObject("Review key light (capture only)");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.88f, 0.91f, 1.0f);
            lightObject.transform.rotation = Quaternion.Euler(34f, -28f, 0f);

            var cameraObject = new GameObject("Review camera (capture only)");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.068f, 0.12f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(0.8f, bounds.size.y * 0.72f);
            var target = bounds.center + Vector3.up * (bounds.size.y * 0.04f);
            // Blender authors the face toward -Y; the FBX axis conversion maps
            // its front to Unity +Z, so stage from +Z for a face-facing view.
            cameraObject.transform.position = target + new Vector3(1.7f, 1.1f, 4.8f).normalized * (bounds.size.magnitude * 2.3f);
            cameraObject.transform.LookAt(target);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;

            var rt = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            camera.targetTexture = rt;
            var texture = new Texture2D(768, 768, TextureFormat.RGBA32, false);
            try
            {
                AnimationMode.StartAnimationMode();
                var samples = clips.SelectMany(clip =>
                {
                    var name = clip.name;
                    if (name.EndsWith("Jump", StringComparison.OrdinalIgnoreCase))
                        return new[] { (clip, "apex", 0.52f) };
                    if (name.EndsWith("Land", StringComparison.OrdinalIgnoreCase))
                        return new[] { (clip, "impact", 0.25f), (clip, "settle", 0.98f) };
                    if (name.EndsWith("Run", StringComparison.OrdinalIgnoreCase))
                        return new[] { (clip, "left", 0.03f), (clip, "right", 0.53f) };
                    return new[] { (clip, "idle", 0.25f) };
                }).ToArray();
                foreach (var sample in samples)
                {
                    var clip = sample.clip;
                    var sampleTime = Mathf.Clamp(clip.length * sample.Item3, 0f, Mathf.Max(0f, clip.length - 0.001f));
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(instance, clip, sampleTime);
                    AnimationMode.EndSampling();
                    // The direct clip sampler applies imported FBX transform
                    // curves to this isolated hierarchy after Editor sampling.
                    clip.SampleAnimation(instance, sampleTime);
                    var leftArm = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Arm_L");
                    if (leftArm != null) log.AppendLine($"pose={sample.clip.name} Arm_L.rotation={leftArm.localRotation.eulerAngles}");
                    var bakedRenderers = new System.Collections.Generic.List<SkinnedMeshRenderer>();
                    var bakedObjects = new System.Collections.Generic.List<GameObject>();
                    var bakedMeshes = new System.Collections.Generic.List<Mesh>();
                    var boundsProbe = new Bounds();
                    var hasBoundsProbe = false;
                    foreach (var skinned in renderers)
                    {
                        var bakedMesh = new Mesh { name = skinned.name + " candidate pose" };
                        skinned.BakeMesh(bakedMesh);
                        bakedMesh.RecalculateBounds();
                        bakedMeshes.Add(bakedMesh);
                        var proxy = new GameObject(skinned.name + " baked review pose");
                        proxy.transform.SetParent(skinned.transform.parent, false);
                        proxy.transform.localPosition = skinned.transform.localPosition;
                        proxy.transform.localRotation = skinned.transform.localRotation;
                        proxy.transform.localScale = skinned.transform.localScale;
                        proxy.AddComponent<MeshFilter>().sharedMesh = bakedMesh;
                        var proxyRenderer = proxy.AddComponent<MeshRenderer>();
                        proxyRenderer.sharedMaterials = skinned.sharedMaterials;
                        skinned.enabled = false;
                        bakedRenderers.Add(skinned);
                        bakedObjects.Add(proxy);
                        var worldBounds = proxyRenderer.bounds;
                        if (!hasBoundsProbe) { boundsProbe = worldBounds; hasBoundsProbe = true; }
                        else boundsProbe.Encapsulate(worldBounds);
                    }
                    if (hasBoundsProbe) log.AppendLine($"bakedPoseBounds={boundsProbe.center}/{boundsProbe.size}");
                    if (GraphicsSettings.currentRenderPipeline != null)
                    {
                        var request = new RenderPipeline.StandardRequest { destination = rt };
                        RenderPipeline.SubmitRenderRequest(camera, request);
                    }
                    else
                    {
                        camera.Render();
                    }
                    RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    texture.Apply();
                    var output = Path.Combine(OutputDirectory, SafeName(clip.name) + "-" + sample.Item2 + ".png");
                    File.WriteAllBytes(output, texture.EncodeToPNG());
                    log.AppendLine($"render={output} sampleTime={sampleTime:F3}s");
                    foreach (var skinned in bakedRenderers) skinned.enabled = true;
                    foreach (var bakedObject in bakedObjects) UnityEngine.Object.DestroyImmediate(bakedObject);
                    foreach (var bakedMesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                RenderTexture.active = null;
                camera.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(rt);
            }

            var reportPath = Path.Combine(OutputDirectory, "unity-import-review.txt");
            File.WriteAllText(reportPath, log.ToString());
            Debug.Log($"[Splat candidate review] Wrote {reportPath}\n{log}");
            EditorApplication.Exit(0);
        }

        private static void ApplyPreviewMaterials(GameObject instance)
        {
            var vertexShader = Shader.Find("SpaceAndCrafts/Preview Vertex Color");
            var violet = new Color(0.34f, 0.12f, 0.69f);
            var ink = new Color(0.018f, 0.012f, 0.028f);
            var eye = new Color(0.10f, 0.035f, 0.20f);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.name;
                var color = violet;
                if (name.StartsWith("Splat_UnifiedBodyAndLimbs", StringComparison.Ordinal))
                {
                    if (vertexShader != null)
                        renderer.sharedMaterial = new Material(vertexShader) { name = "Splat candidate vertex-color preview" };
                    continue;
                }
                if (name.StartsWith("EyeCatch_", StringComparison.Ordinal)) color = Color.white;
                else if (name.StartsWith("Eye_", StringComparison.Ordinal)) color = eye;
                else if (name.StartsWith("Splat friendly smile", StringComparison.Ordinal)) color = ink;
                SetColor(renderer, color);
            }
        }

        private static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            renderer.sharedMaterial = new Material(shader) { color = color, name = "Splat candidate review material" };
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Candidate has no renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static string SafeName(string value)
        {
            var chars = value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-').ToArray();
            return new string(chars);
        }
    }
}
#endif
