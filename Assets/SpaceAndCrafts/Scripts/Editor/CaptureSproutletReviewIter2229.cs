#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Stages fixed Animator poses from the Sproutlet review prefab; never edits a game scene.</summary>
    public static class CaptureSproutletReviewIter2229
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Review/Sproutlet_Review_Iter2228.prefab";
        private const string Output = "Logs/score-comparison/sproutlet-review-iter2229";
        private const int Width = 420;
        private const int Height = 460;
        private const float Step = 1f / 30f;
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static string activeOutputFolder = Output;
        private static Material activeReviewMaterial;

        [MenuItem("Space & Crafts/Capture Sproutlet Review Sequence Iter2229")]
        public static void Capture() => CaptureFrom(PrefabPath, Output);

        [MenuItem("Space & Crafts/Capture Textured Sproutlet Review Iter2231")]
        public static void CaptureTexturedIter2231() => CaptureFrom(
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Review/Sproutlet_Review_Textured_Iter2231.prefab",
            "Logs/score-comparison/sproutlet-review-iter2231-textured",
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Review/Sproutlet_ReviewMaterial_Iter2231.mat");

        private static void CaptureFrom(string prefabPath, string outputFolder, string forcedReviewMaterialPath = null)
        {
            activeOutputFolder = outputFolder;
            Directory.CreateDirectory(activeOutputFolder);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing Sproutlet review prefab.", prefabPath);
            var forcedReviewMaterial = string.IsNullOrEmpty(forcedReviewMaterialPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Material>(forcedReviewMaterialPath);
            if (!string.IsNullOrEmpty(forcedReviewMaterialPath) && forcedReviewMaterial == null)
                throw new FileNotFoundException("Missing forced review material.", forcedReviewMaterialPath);
            activeReviewMaterial = forcedReviewMaterial;

            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            Texture2D sheet = null;
            var tiles = new List<Color[]>();
            var report = new StringBuilder();
            report.AppendLine("Sproutlet — staged Generic Animator pose review.");
            report.AppendLine($"Prefab source: {prefabPath}.");
                report.AppendLine("Review-only Animator.Play/Update samples; no gameplay, AI, dialogue, collider/platform physics, production scene, build, or human play.");
                if (forcedReviewMaterial != null)
                    report.AppendLine($"Capture-only baked-mesh proxy uses fixed review material {forcedReviewMaterial.name} from {forcedReviewMaterialPath} on every pose; the source prefab remains unchanged.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.54f, 1f);
                RenderSettings.skybox = null;
                AddLight(scene, "ReviewKey", new Vector3(32f, -25f, 0f), 1.2f, new Color(1f, 0.96f, 0.90f));
                AddLight(scene, "ReviewFill", new Vector3(18f, 145f, 0f), 0.65f, new Color(0.78f, 0.86f, 1f));

                var cameraObject = new GameObject("SproutletReviewCamera_Iter2229");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 1f);
                camera.orthographic = true;
                camera.aspect = Width / (float)Height;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Sproutlet review prefab.");
                instance.name = "Sproutlet_ReviewCaptureOnly_Iter2229";
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("Sproutlet review prefab is missing its Animator/controller.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;

                SetState(animator, "Base Layer.Idle", 0.12f);
                var idleBounds = RendererBounds(instance);
                var idleFeet = LowestFootProxies(animator);
                var floorY = float.IsNaN(idleFeet.bottom) ? idleBounds.min.y : idleFeet.bottom;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "VisualGroundReference_NoColliderClaim";
                floor.transform.position = new Vector3(0f, floorY, 0f);
                floor.transform.localScale = Vector3.one * 2f;
                SceneManager.MoveGameObjectToScene(floor, scene);

                var center = idleBounds.center;
                camera.orthographicSize = Mathf.Max(idleBounds.size.y * 0.70f, idleBounds.size.x / (2f * camera.aspect) * 1.30f);
                cameraObject.transform.position = center + new Vector3(0f, 0.02f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

                if (activeReviewMaterial != null)
                {
                    // Warm the review shader/atlas before saving the first pose; Unity's initial
                    // immediate camera render can otherwise capture its gray fallback material.
                    RenderBakedPose(camera, instance, activeReviewMaterial);
                    report.AppendLine("One discarded capture-only render warmed the fixed review material before the first saved pose.");
                }

                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "01_Idle", "Base Layer.Idle", 0.12f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "02_Listen_Start", "Base Layer.Listening", 0.10f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "03_Listen_Mid", "Base Layer.Listening", 0.50f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "04_Listen_Late", "Base Layer.Listening", 0.90f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "05_Jump_Start", "Base Layer.Happy Jump", 0.08f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "06_Jump_Lift", "Base Layer.Happy Jump", 0.35f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "07_Jump_Late", "Base Layer.Happy Jump", 0.82f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "08_Hit_Mid", "Base Layer.Hit Reaction", 0.45f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "09_Walk", "Base Layer.Walk Run", 0.30f, 0.22f);
                CaptureState(animator, camera, instance, target, scratch, tiles, report, floorY, "10_Run", "Base Layer.Walk Run", 0.30f, 0.82f);

                const int columns = 5;
                var rows = (tiles.Count + columns - 1) / columns;
                sheet = new Texture2D(Width * columns, Height * rows, TextureFormat.RGBA32, false);
                for (var i = 0; i < tiles.Count; i++)
                    sheet.SetPixels((i % columns) * Width, (rows - 1 - i / columns) * Height, Width, Height, tiles[i]);
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(activeOutputFolder, "Sproutlet_AnimatorSequence.png"), sheet.EncodeToPNG());
                report.AppendLine();
                report.AppendLine("Foot values are min-Y positions of baked mesh vertices whose aggregate skin weights to bones containing Foot/Toe exceed 0.20; they are sparse geometry proxies, not true sole or collider contact.");
                report.AppendLine("Neutral floor is set to the initial Idle lowest foot proxy. Values above/below it describe staged pose geometry only.");
                report.AppendLine("The current asset import reports Happy Jump 9.83 s and Listening Gesture 9.33 s. Normalized samples above do not establish player-facing action timing or continuous transitions.");
                File.WriteAllText(Path.Combine(activeOutputFolder, "REVIEW.txt"), report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log($"[SproutletReviewCapture] Captured {tiles.Count} staged poses to {activeOutputFolder}; no production scene changed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(activeOutputFolder, "CAPTURE_FAILURE.txt"), exception + Environment.NewLine + report, Encoding.UTF8);
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
            finally
            {
                if (sheet != null) UnityEngine.Object.DestroyImmediate(sheet);
                if (scratch != null) UnityEngine.Object.DestroyImmediate(scratch);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                RenderTexture.active = null;
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
                activeReviewMaterial = null;
            }
        }

        private static void CaptureState(Animator animator, Camera camera, GameObject instance, RenderTexture target,
            Texture2D scratch, List<Color[]> tiles, StringBuilder report, float floorY, string label,
            string state, float normalizedTime, float moveSpeed = 0f)
        {
            animator.SetFloat(MoveSpeedId, moveSpeed);
            animator.Play(state, 0, normalizedTime);
            animator.Update(Step);
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var feet = LowestFootProxies(animator);
            var delta = float.IsNaN(feet.bottom) ? float.NaN : feet.bottom - floorY;
            var motion = current.IsName(state) ? "requested-state" : $"state-mismatch:{current.shortNameHash}";
            report.AppendLine($"{label}: {motion}; time={current.normalizedTime:F3}; footProxyLeft={Fmt(feet.left)}m; footProxyRight={Fmt(feet.right)}m; lowestMinusIdleFloor={Fmt(delta)}m; bounds={RendererBounds(instance).size}.");
            RenderBakedPose(camera, instance, activeReviewMaterial);
            RenderTexture.active = target;
            scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            scratch.Apply();
            tiles.Add(scratch.GetPixels());
            File.WriteAllBytes(Path.Combine(activeOutputFolder, label + ".png"), scratch.EncodeToPNG());
        }

        private static void SetState(Animator animator, string state, float normalizedTime)
        {
            animator.Rebind();
            animator.Play(state, 0, normalizedTime);
            animator.Update(Step);
        }

        private static (float left, float right, float bottom) LowestFootProxies(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(item => item.sharedMesh != null && item.bones != null);
            if (skin == null) return (float.NaN, float.NaN, float.NaN);
            var mesh = skin.sharedMesh;
            var weights = mesh.boneWeights;
            var baked = new Mesh { name = "SproutletFootProxy_Iter2229" };
            try
            {
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                var left = float.PositiveInfinity;
                var right = float.PositiveInfinity;
                for (var i = 0; i < Math.Min(vertices.Length, weights.Length); i++)
                {
                    var weight = weights[i];
                    Accumulate(weight.boneIndex0, weight.weight0, vertices[i]);
                    Accumulate(weight.boneIndex1, weight.weight1, vertices[i]);
                    Accumulate(weight.boneIndex2, weight.weight2, vertices[i]);
                    Accumulate(weight.boneIndex3, weight.weight3, vertices[i]);
                }
                var bottom = Mathf.Min(left, right);
                return (float.IsPositiveInfinity(left) ? float.NaN : left,
                    float.IsPositiveInfinity(right) ? float.NaN : right,
                    float.IsPositiveInfinity(bottom) ? float.NaN : bottom);

                void Accumulate(int boneIndex, float boneWeight, Vector3 localPoint)
                {
                    if (boneWeight < 0.20f || boneIndex < 0 || boneIndex >= skin.bones.Length) return;
                    var bone = skin.bones[boneIndex];
                    if (bone == null) return;
                    var boneName = bone.name;
                    if (boneName.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) < 0
                        && boneName.IndexOf("Toe", StringComparison.OrdinalIgnoreCase) < 0) return;
                    var worldY = skin.transform.TransformPoint(localPoint).y;
                    if (boneName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0
                        || boneName.IndexOf("_L", StringComparison.OrdinalIgnoreCase) >= 0)
                        left = Mathf.Min(left, worldY);
                    else if (boneName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0
                        || boneName.IndexOf("_R", StringComparison.OrdinalIgnoreCase) >= 0)
                        right = Mathf.Min(right, worldY);
                    else
                    {
                        left = Mathf.Min(left, worldY);
                        right = Mathf.Min(right, worldY);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        private static Bounds RendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No enabled renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void RenderBakedPose(Camera camera, GameObject instance, Material reviewMaterial)
        {
            var skin = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(item => item.sharedMesh != null);
            if (skin == null) throw new InvalidOperationException("No skinned mesh for pose capture.");
            var baked = new Mesh { name = "SproutletPoseCapture_Iter2229" };
            GameObject proxy = null;
            var wasEnabled = skin.enabled;
            try
            {
                skin.BakeMesh(baked);
                proxy = new GameObject("BakedPoseCaptureOnly_Iter2229");
                proxy.transform.SetParent(skin.transform.parent, false);
                proxy.transform.localPosition = skin.transform.localPosition;
                proxy.transform.localRotation = skin.transform.localRotation;
                proxy.transform.localScale = skin.transform.localScale;
                proxy.AddComponent<MeshFilter>().sharedMesh = baked;
                var proxyRenderer = proxy.AddComponent<MeshRenderer>();
                proxyRenderer.sharedMaterials = reviewMaterial != null
                    ? Enumerable.Repeat(reviewMaterial, baked.subMeshCount).ToArray()
                    : skin.sharedMaterials;
                skin.enabled = false;
                camera.Render();
            }
            finally
            {
                skin.enabled = wasEnabled;
                if (proxy != null) UnityEngine.Object.DestroyImmediate(proxy);
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void AddLight(Scene scene, string name, Vector3 euler, float intensity, Color color)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.transform.rotation = Quaternion.Euler(euler);
            var light = obj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
        }

        private static string Fmt(float value) => float.IsNaN(value) ? "n/a" : value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
