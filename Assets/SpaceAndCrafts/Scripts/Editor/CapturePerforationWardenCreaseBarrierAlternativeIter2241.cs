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
    /// <summary>Captures fixed Animator samples of the isolated Perforation Warden review prefab.</summary>
    public static class CapturePerforationWardenCreaseBarrierAlternativeIter2241
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Review/PerforationWarden_Review_Iter2236.prefab";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Review/PerforationWarden_Review_Iter2236.mat";
        private const string Output = "Logs/score-comparison/perforation-warden-crease-barrier-iter2241";
        private const int Width = 360;
        private const int Height = 420;
        private const float Step = 1f / 30f;
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");

        [MenuItem("Space & Crafts/Capture Perforation Warden Review Iter2241")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (prefab == null || material == null) throw new FileNotFoundException("Missing review prefab/material.");

            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            Texture2D sheet = null;
            var tiles = new List<Color[]>();
            var report = new StringBuilder();
            report.AppendLine("Perforation Warden — staged Generic Animator pose review.");
            report.AppendLine($"Review prefab: {PrefabPath}; capture material override: {material.name}.");
            report.AppendLine("Animator.Play/Update samples on a temporary review scene. No production scene, AI, gameplay, collision, build, or human play.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.49f, 0.51f, 0.56f, 1f);
                RenderSettings.skybox = null;
                AddLight(scene, "ReviewKey", new Vector3(28f, -32f, 0f), 1.05f, new Color(1f, 0.96f, 0.90f));
                AddLight(scene, "ReviewFill", new Vector3(15f, 150f, 0f), 0.55f, new Color(0.80f, 0.88f, 1f));

                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate review prefab.");
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("Review prefab has no Animator/controller.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var skin in skins) skin.updateWhenOffscreen = true;
                if (skins.Length != 1) throw new InvalidOperationException($"Expected one skinned renderer, found {skins.Length}.");

                SetState(animator, "Base Layer.Idle", 0.12f);
                var idleBounds = RendererBounds(instance);
                var feet = LowestFootProxies(animator);
                var floorY = float.IsNaN(feet.bottom) ? idleBounds.min.y : feet.bottom;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "VisualGroundReference_NoColliderClaim";
                floor.transform.position = new Vector3(0f, floorY, 0f);
                floor.transform.localScale = Vector3.one * 1.2f;
                SceneManager.MoveGameObjectToScene(floor, scene);

                var cameraObject = new GameObject("PerforationWardenReviewCamera_Iter2241");
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
                var center = idleBounds.center;
                camera.orthographicSize = Mathf.Max(idleBounds.size.y * 0.70f, idleBounds.size.x / (2f * camera.aspect) * 1.20f);
                cameraObject.transform.position = center + new Vector3(0f, 0.04f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);

                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                RenderBakedPose(camera, instance, material); // warm the shader/atlas before saving the first frame
                report.AppendLine("One discarded baked-pose render warmed the capture material before the first saved sample.");

                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "01_Idle_Reference", "Base Layer.Idle", 0.12f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "02_Delivered_Slam_Windup", "Base Layer.JumpSlam", 0.24f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "03_Delivered_Slam_Contact", "Base Layer.JumpSlam", 0.76f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "04_CreaseBarrier_Start", "Base Layer.CreaseBarrier_Alternative", 0.00f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "05_CreaseBarrier_Anticipation", "Base Layer.CreaseBarrier_Alternative", 0.18f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "06_CreaseBarrier_Impact", "Base Layer.CreaseBarrier_Alternative", 0.40f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "07_CreaseBarrier_Hold", "Base Layer.CreaseBarrier_Alternative", 0.56f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "08_CreaseBarrier_Recovery", "Base Layer.CreaseBarrier_Alternative", 0.82f);
                Sample(animator, camera, instance, target, scratch, tiles, report, floorY, material, "09_CreaseBarrier_End", "Base Layer.CreaseBarrier_Alternative", 1.00f);
                const int columns = 6;
                var rows = (tiles.Count + columns - 1) / columns;
                sheet = new Texture2D(Width * columns, Height * rows, TextureFormat.RGBA32, false);
                for (var i = 0; i < tiles.Count; i++)
                    sheet.SetPixels((i % columns) * Width, (rows - 1 - i / columns) * Height, Width, Height, tiles[i]);
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "PerforationWarden_AnimatorSequence.png"), sheet.EncodeToPNG());
                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"), report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log($"[PerforationWardenCreaseBarrierCapture2240] Saved {tiles.Count} staged pose captures to {Output}; scene unchanged.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(Output, "CAPTURE_FAILURE.txt"), exception + Environment.NewLine + report, Encoding.UTF8);
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
            }
        }

        private static void Sample(Animator animator, Camera camera, GameObject instance, RenderTexture target,
            Texture2D scratch, List<Color[]> tiles, StringBuilder report, float floorY, Material material,
            string label, string state, float time, float moveSpeed = 0f)
        {
            animator.SetFloat(MoveSpeedId, moveSpeed);
            animator.Play(state, 0, time);
            animator.Update(Step);
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var feet = LowestFootProxies(animator);
            var delta = float.IsNaN(feet.bottom) ? float.NaN : feet.bottom - floorY;
            var stateResult = current.IsName(state) ? "requested-state" : $"state-mismatch:{current.shortNameHash}";
            report.AppendLine($"{label}: {stateResult}; time={current.normalizedTime:F3}; lowestFootProxyMinusIdleFloor={Fmt(delta)}m; left={Fmt(feet.left)}m; right={Fmt(feet.right)}m; bounds={RendererBounds(instance).size}.");
            RenderBakedPose(camera, instance, material);
            RenderTexture.active = target;
            scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            scratch.Apply();
            tiles.Add(scratch.GetPixels());
            File.WriteAllBytes(Path.Combine(Output, label + ".png"), scratch.EncodeToPNG());
        }

        private static void SetState(Animator animator, string state, float time)
        {
            animator.Rebind();
            animator.Play(state, 0, time);
            animator.Update(Step);
        }

        private static (float left, float right, float bottom) LowestFootProxies(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(item => item.sharedMesh && item.bones != null);
            if (skin == null) return (float.NaN, float.NaN, float.NaN);
            var weights = skin.sharedMesh.boneWeights;
            var baked = new Mesh { name = "PerforationWardenFootProxy_Iter2241" };
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
                return (float.IsPositiveInfinity(left) ? float.NaN : left,
                    float.IsPositiveInfinity(right) ? float.NaN : right,
                    Mathf.Min(left, right));

                void Accumulate(int index, float weight, Vector3 localPoint)
                {
                    if (weight < 0.20f || index < 0 || index >= skin.bones.Length) return;
                    var bone = skin.bones[index];
                    if (bone == null) return;
                    var name = bone.name;
                    if (name.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) < 0
                        && name.IndexOf("Toe", StringComparison.OrdinalIgnoreCase) < 0) return;
                    var y = skin.transform.TransformPoint(localPoint).y;
                    if (name.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("_L", StringComparison.OrdinalIgnoreCase) >= 0)
                        left = Mathf.Min(left, y);
                    else if (name.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("_R", StringComparison.OrdinalIgnoreCase) >= 0)
                        right = Mathf.Min(right, y);
                    else { left = Mathf.Min(left, y); right = Mathf.Min(right, y); }
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

        private static void RenderBakedPose(Camera camera, GameObject instance, Material material)
        {
            var skin = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(item => item.sharedMesh);
            if (skin == null) throw new InvalidOperationException("No skinned Warden mesh for capture.");
            var baked = new Mesh { name = "PerforationWardenPoseCapture_Iter2241" };
            GameObject proxy = null;
            var wasEnabled = skin.enabled;
            try
            {
                skin.BakeMesh(baked);
                proxy = new GameObject("BakedPoseCaptureOnly_Iter2241");
                proxy.transform.SetParent(skin.transform.parent, false);
                proxy.transform.localPosition = skin.transform.localPosition;
                proxy.transform.localRotation = skin.transform.localRotation;
                proxy.transform.localScale = skin.transform.localScale;
                proxy.AddComponent<MeshFilter>().sharedMesh = baked;
                var proxyRenderer = proxy.AddComponent<MeshRenderer>();
                proxyRenderer.sharedMaterials = Enumerable.Repeat(material, baked.subMeshCount).ToArray();
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

        private static string Fmt(float value) => float.IsNaN(value) ? "n/a" : value.ToString("+0.0000;-0.0000;0.0000");
    }
}
#endif


