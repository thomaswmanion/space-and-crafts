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
    /// Graphics-enabled, matched PreviewScene render of the existing 1408 and
    /// 1426 isolated Regent prefabs/controllers. No game scene or production
    /// prefab is changed; this is a clip-state render, not encounter testing.
    /// </summary>
    public static class ChromaticRegentMatchedHitPreviewIter1769
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses";
        private const string Fbx1408 = Root + "/20261101_chromatic-regent-animation-bundle-iter1408/ChromaticRegent_AnimationBundle.fbx";
        private const string Prefab1408 = Root + "/20261101_chromatic-regent-animation-bundle-iter1408/UnityCandidate/ChromaticRegent_AnimatedCandidate.prefab";
        private const string Fbx1426 = Root + "/20261101_chromatic-regent-animation-bundle-iter1426/ChromaticRegent_AnimationBundle.fbx";
        private const string Prefab1426 = Root + "/20261101_chromatic-regent-animation-bundle-iter1426/UnityCandidate_ReviewOnly_Iter1770_ExactHit/ChromaticRegent_Iter1426_ExactHit_ReviewOnly.prefab";
        private const string Fbx1773 = Root + "/20261101_chromatic-regent-animation-bundle-iter1773/ChromaticRegent_AnimationBundle.fbx";
        private const string Prefab1773 = Root + "/20261101_chromatic-regent-animation-bundle-iter1426/UnityCandidate_ReviewOnly_Iter1773_BalancedReaction/ChromaticRegent_Iter1773_BalancedReaction_ReviewOnly.prefab";
        private const string Output = "Logs/score-comparison/regent-hit-matched-unity-preview-iter1774-threeway";
        private const int Width = 1200;
        private const int Height = 900;
        private static readonly float[] NormalizedTimes = { 0.05f, 0.25f, 0.50f, 0.75f, 0.95f };

        private sealed class Candidate
        {
            public string Label;
            public string FbxPath;
            public string PrefabPath;
            public GameObject Prefab;
            public AnimatorController Controller;
            public AnimationClip HitClip;
            public AnimatorState HitState;
        }

        [MenuItem("Space & Crafts/Capture Matched Regent Hit Preview Iter1769")]
        public static void Capture()
        {
            Directory.CreateDirectory(Path.Combine(Output, "iter1408"));
            Directory.CreateDirectory(Path.Combine(Output, "iter1426_exact_hit_reviewonly"));
            Directory.CreateDirectory(Path.Combine(Output, "iter1773_balanced_reviewonly"));
            Directory.CreateDirectory(Path.Combine(Output, "paired_1408_vs_1426"));
            Directory.CreateDirectory(Path.Combine(Output, "paired_1408_vs_1773"));
            Directory.CreateDirectory(Path.Combine(Output, "paired_1426_vs_1773"));
            Directory.CreateDirectory(Path.Combine(Output, "paired"));
            var report = new StringBuilder();
            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            try
            {
                var candidates = new[]
                {
                    LoadCandidate("Iter1408", Fbx1408, Prefab1408),
                    LoadCandidate("Iter1426 ExactHit ReviewOnly", Fbx1426, Prefab1426),
                    LoadCandidate("Iter1773 Balanced ReviewOnly", Fbx1773, Prefab1773)
                };

                // Isolated empty editor scene for renders. NewPreviewScene
                // cameras are excluded from Camera.Render in this Unity version.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("RegentHitPreviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.13f, 0.15f, 0.20f, 1f);
                camera.orthographic = true;
                camera.aspect = Width / (float)Height;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                AddDirectionalLight(scene, "KeyLight", new Vector3(34f, -28f, 0f), 1.2f, new Color(1f, 0.94f, 0.86f));
                AddDirectionalLight(scene, "FillLight", new Vector3(18f, 145f, 0f), 0.58f, new Color(0.74f, 0.82f, 1f));
                RenderSettings.ambientLight = new Color(0.44f, 0.45f, 0.50f, 1f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.skybox = null;

                // Find shared framing across both imported rigs and all requested
                // Hit samples before writing any image.
                var overallBounds = new Bounds(Vector3.zero, Vector3.zero);
                var hasBounds = false;
                foreach (var candidate in candidates)
                foreach (var normalizedTime in NormalizedTimes)
                {
                    var sample = InstantiateAndSample(scene, candidate, normalizedTime, out var animator, out var activeState);
                    var bounds = GetRendererBounds(sample);
                    if (!hasBounds) { overallBounds = bounds; hasBounds = true; }
                    else overallBounds.Encapsulate(bounds);
                    report.AppendLine($"Bounds sample: {candidate.Label} t={normalizedTime:0.00}; state={activeState}; center={bounds.center}; size={bounds.size}");
                    UnityEngine.Object.DestroyImmediate(sample);
                }
                if (!hasBounds) throw new InvalidOperationException("No renderable character bounds found.");

                camera.orthographicSize = Mathf.Max(overallBounds.size.y * 0.59f,
                    overallBounds.size.x / (2f * camera.aspect) * 1.18f);
                var lookAt = overallBounds.center;
                cameraObject.transform.position = lookAt + new Vector3(0f, 0.06f, 8f);
                cameraObject.transform.rotation = Quaternion.LookRotation(lookAt - cameraObject.transform.position, Vector3.up);
                AddGround(scene, 0f, overallBounds.size.x * 2.6f);
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    name = "RegentHitPreviewRenderTexture"
                };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

                foreach (var normalizedTime in NormalizedTimes)
                {
                    var first = RenderCandidate(scene, camera, target, scratch, candidates[0], normalizedTime,
                        Path.Combine(Output, "iter1408", $"Hit_{normalizedTime:0.00}.png"), report);
                    var exact = RenderCandidate(scene, camera, target, scratch, candidates[1], normalizedTime,
                        Path.Combine(Output, "iter1426_exact_hit_reviewonly", $"Hit_{normalizedTime:0.00}.png"), report);
                    var balanced = RenderCandidate(scene, camera, target, scratch, candidates[2], normalizedTime,
                        Path.Combine(Output, "iter1773_balanced_reviewonly", $"Hit_{normalizedTime:0.00}.png"), report);
                    WriteSideBySide(first, exact,
                        Path.Combine(Output, "paired_1408_vs_1426", $"Hit_{normalizedTime:0.00}_left1408_right1426.png"));
                    WriteSideBySide(first, balanced,
                        Path.Combine(Output, "paired_1408_vs_1773", $"Hit_{normalizedTime:0.00}_left1408_right1773.png"));
                    WriteSideBySide(exact, balanced,
                        Path.Combine(Output, "paired_1426_vs_1773", $"Hit_{normalizedTime:0.00}_left1426_right1773.png"));
                }

                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"),
                    "Chromatic Regent Iter1408 vs Iter1426 exact-Hit vs Iter1773 balanced Hit-clip render\n" +
                    "Unity graphics-enabled batch isolated empty preview scene; same camera, orthographic framing, light rig, output size, fixed y=0 floor reference, and normalized Hit controller-state times.\n" +
                    "Three pair folders compare Iter1408 vs Iter1426, Iter1408 vs Iter1773, and Iter1426 vs Iter1773.\n" +
                    "Animator.Play(\"Hit\") sampled each candidate's own isolated controller state; no scene or production prefab was changed.\n" +
                    "This is a rendered clip-state comparison, not a build, test, combat encounter, or human-play review.\n\n" + report);
                AssetDatabase.Refresh();
                Debug.Log($"[RegentHit Iter1769] rendered {NormalizedTimes.Length * candidates.Length} matched controller-state samples to {Output}; production assets untouched.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(Output, "CAPTURE_FAILURE.txt"), exception.ToString() + "\n" + report);
                Debug.LogException(exception);
                EditorApplication.Exit(2);
            }
            finally
            {
                if (scratch != null) UnityEngine.Object.DestroyImmediate(scratch);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Candidate LoadCandidate(string label, string fbxPath, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException($"Missing isolated prefab: {prefabPath}");
            var controller = AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<AnimatorController>().FirstOrDefault();
            if (controller == null)
            {
                var animator = prefab.GetComponentInChildren<Animator>(true);
                controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
            }
            if (controller == null) throw new InvalidOperationException($"Missing AnimatorController on {prefabPath}");
            var layer = controller.layers.FirstOrDefault();
            var hitState = layer != null ? layer.stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Hit") : null;
            if (hitState == null || hitState.motion == null)
                throw new InvalidOperationException($"Controller at {prefabPath} has no Hit state/motion.");
            var hitClip = hitState.motion as AnimationClip;
            if (hitClip == null) throw new InvalidOperationException($"Hit state at {prefabPath} has no direct AnimationClip motion.");
            if ((label.StartsWith("Iter1426", StringComparison.Ordinal) || label.StartsWith("Iter1773", StringComparison.Ordinal)) &&
                !hitClip.name.EndsWith("|ChromaticRegent_Hit", StringComparison.Ordinal))
                throw new InvalidOperationException($"{label} Hit state resolved to '{hitClip.name}', not exact ChromaticRegent_Hit.");
            return new Candidate { Label = label, FbxPath = fbxPath, PrefabPath = prefabPath, Prefab = prefab,
                Controller = controller, HitClip = hitClip, HitState = hitState };
        }

        private static GameObject InstantiateAndSample(Scene scene, Candidate candidate, float normalizedTime,
            out Animator animator, out string activeState)
        {
            var instance = PrefabUtility.InstantiatePrefab(candidate.Prefab, scene) as GameObject;
            if (instance == null) throw new InvalidOperationException($"Could not instantiate {candidate.PrefabPath}");
            instance.name = candidate.Label.Replace(' ', '_') + "_HitPreview";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 1.4952573f;
            animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException($"No Animator found in {candidate.PrefabPath}");
            animator.runtimeAnimatorController = candidate.Controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            animator.Rebind();
            animator.Play("Hit", 0, normalizedTime);
            animator.Update(0.0001f);
            var info = animator.GetCurrentAnimatorStateInfo(0);
            activeState = info.IsName("Hit") ? "Hit" : $"unexpected hash={info.fullPathHash}";
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (!info.IsName("Hit") || clipInfo.Length == 0)
                throw new InvalidOperationException($"Animator did not evaluate Hit controller state for {candidate.Label} at t={normalizedTime:0.00}.");
            foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skinned.updateWhenOffscreen = true;
            return instance;
        }

        private static Bounds GetRendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(r => r != null && r.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException($"No enabled renderers on {instance.name}");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static byte[] RenderCandidate(Scene scene, Camera camera, RenderTexture target, Texture2D scratch,
            Candidate candidate, float normalizedTime, string filePath, StringBuilder report)
        {
            var instance = InstantiateAndSample(scene, candidate, normalizedTime, out var animator, out var activeState);
            try
            {
                camera.Render();
                var prior = RenderTexture.active;
                RenderTexture.active = target;
                scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                scratch.Apply(false, false);
                RenderTexture.active = prior;
                var png = scratch.EncodeToPNG();
                File.WriteAllBytes(filePath, png);
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                var seconds = normalizedTime * candidate.HitClip.length;
                report.AppendLine($"Rendered: {candidate.Label} state={activeState}; stateMotion={candidate.HitClip.name}; controllerClip={string.Join(",", clips.Select(c => c.clip.name))}; normalized={normalizedTime:0.00}; clipSeconds={seconds:0.000}; file={filePath}");
                return png;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void WriteSideBySide(byte[] leftPng, byte[] rightPng, string filePath)
        {
            var left = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var right = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pair = new Texture2D(Width * 2, Height, TextureFormat.RGBA32, false);
            try
            {
                if (!left.LoadImage(leftPng) || !right.LoadImage(rightPng))
                    throw new InvalidOperationException("Could not decode one of the rendered PNGs.");
                pair.SetPixels(0, 0, Width, Height, left.GetPixels());
                pair.SetPixels(Width, 0, Width, Height, right.GetPixels());
                pair.Apply(false, false);
                File.WriteAllBytes(filePath, pair.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(left);
                UnityEngine.Object.DestroyImmediate(right);
                UnityEngine.Object.DestroyImmediate(pair);
            }
        }

        private static void AddDirectionalLight(Scene scene, string name, Vector3 euler, float intensity, Color color)
        {
            var lightObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(euler);
        }

        private static void AddGround(Scene scene, float y, float width)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(ground, scene);
            ground.name = "ReviewOnly_GroundReference";
            ground.transform.position = new Vector3(0f, y, 0f);
            ground.transform.localScale = new Vector3(Mathf.Max(0.25f, width / 10f), 1f, 0.30f);
            var collider = ground.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            var renderer = ground.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (renderer != null && shader != null)
            {
                var material = new Material(shader) { name = "ReviewOnly_GroundReference_Mat" };
                material.color = new Color(0.27f, 0.30f, 0.36f, 1f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
                renderer.sharedMaterial = material;
            }
        }
    }
}
#endif
