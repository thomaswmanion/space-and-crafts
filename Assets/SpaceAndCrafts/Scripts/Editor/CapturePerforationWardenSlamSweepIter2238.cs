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
    /// <summary>Samples the existing Warden slam evenly to locate likely contact timing for review.</summary>
    public static class CapturePerforationWardenSlamSweepIter2238
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Review/PerforationWarden_Review_Iter2236.prefab";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Review/PerforationWarden_Review_Iter2236.mat";
        private const string Output = "Logs/score-comparison/perforation-warden-slam-sweep-iter2238";
        private const int Width = 300;
        private const int Height = 380;
        private const float Step = 1f / 30f;
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");

        [MenuItem("Space & Crafts/Capture Perforation Warden Slam Sweep Iter2238")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (prefab == null || material == null) throw new FileNotFoundException("Missing isolated review prefab/material.");
            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            Texture2D sheet = null;
            var tiles = new List<Color[]>();
            var report = new StringBuilder();
            report.AppendLine("Perforation Warden — staged Jump and Slam normalized-time sweep.");
            report.AppendLine("Review Animator only; weighted foot/toe mesh vertices are geometry proxies, not sole/collider contact. No gameplay, physics, AI, production scene, build, or human play.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.49f, 0.51f, 0.56f, 1f);
                RenderSettings.skybox = null;
                AddLight(scene, "ReviewKey", new Vector3(28f, -32f, 0f), 1.05f, new Color(1f, 0.96f, 0.90f));
                AddLight(scene, "ReviewFill", new Vector3(15f, 150f, 0f), 0.55f, new Color(0.80f, 0.88f, 1f));
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Warden review prefab.");
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null) throw new InvalidOperationException("Warden review Animator/controller missing.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skin == null || skin.sharedMesh == null) throw new InvalidOperationException("Warden review mesh missing.");
                skin.updateWhenOffscreen = true;

                SetState(animator, "Base Layer.Idle", 0.12f);
                var idleBounds = skin.bounds;
                var idleFeet = LowestFootProxies(animator);
                var floorY = float.IsNaN(idleFeet.bottom) ? idleBounds.min.y : idleFeet.bottom;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "VisualFloorReference_NoColliderClaim";
                floor.transform.position = new Vector3(0f, floorY, 0f);
                floor.transform.localScale = Vector3.one * 1.2f;
                SceneManager.MoveGameObjectToScene(floor, scene);
                var cameraObject = new GameObject("WardenSlamSweepCamera_Iter2238");
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
                camera.orthographicSize = Mathf.Max(idleBounds.size.y * 0.78f, idleBounds.size.x / (2f * camera.aspect) * 1.25f);
                cameraObject.transform.position = center + new Vector3(0f, 0.04f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                RenderBakedPose(camera, instance, material);
                report.AppendLine($"Floor reference is Idle weighted-foot proxy at world Y {floorY:F4}; 19 samples at 0.05 normalized-time intervals.");

                for (var index = 1; index <= 19; index++)
                {
                    var t = index * 0.05f;
                    animator.SetFloat(MoveSpeedId, 0f);
                    animator.Play("Base Layer.JumpSlam", 0, t);
                    animator.Update(Step);
                    var current = animator.GetCurrentAnimatorStateInfo(0);
                    var feet = LowestFootProxies(animator);
                    var delta = float.IsNaN(feet.bottom) ? float.NaN : feet.bottom - floorY;
                    var bounds = RendererBounds(instance);
                    var label = $"Slam_{index:00}_t{t:F2}";
                    report.AppendLine($"{label}: {(current.IsName("Base Layer.JumpSlam") ? "requested-state" : "state-mismatch")}; footDelta={Fmt(delta)}m; left={Fmt(feet.left - floorY)}m; right={Fmt(feet.right - floorY)}m; bounds={bounds.size}.");
                    RenderBakedPose(camera, instance, material);
                    RenderTexture.active = target;
                    scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    scratch.Apply();
                    tiles.Add(scratch.GetPixels());
                    File.WriteAllBytes(Path.Combine(Output, label + ".png"), scratch.EncodeToPNG());
                }

                const int columns = 5;
                var rows = (tiles.Count + columns - 1) / columns;
                sheet = new Texture2D(Width * columns, Height * rows, TextureFormat.RGBA32, false);
                for (var i = 0; i < tiles.Count; i++) sheet.SetPixels((i % columns) * Width, (rows - 1 - i / columns) * Height, Width, Height, tiles[i]);
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "PerforationWarden_SlamSweep.png"), sheet.EncodeToPNG());
                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"), report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log($"[PerforationWardenSlamSweep2238] Captured {tiles.Count} normalized times to {Output}; no production scene changed.");
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
            var baked = new Mesh { name = "PerforationWardenSlamFootProxy_Iter2238" };
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
                return (float.IsPositiveInfinity(left) ? float.NaN : left, float.IsPositiveInfinity(right) ? float.NaN : right, Mathf.Min(left, right));
                void Accumulate(int index, float weight, Vector3 point)
                {
                    if (weight < 0.20f || index < 0 || index >= skin.bones.Length) return;
                    var bone = skin.bones[index];
                    if (bone == null) return;
                    var name = bone.name;
                    if (name.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("Toe", StringComparison.OrdinalIgnoreCase) < 0) return;
                    var y = skin.transform.TransformPoint(point).y;
                    if (name.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("_L", StringComparison.OrdinalIgnoreCase) >= 0) left = Mathf.Min(left, y);
                    else if (name.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("_R", StringComparison.OrdinalIgnoreCase) >= 0) right = Mathf.Min(right, y);
                    else { left = Mathf.Min(left, y); right = Mathf.Min(right, y); }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        private static Bounds RendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void RenderBakedPose(Camera camera, GameObject instance, Material material)
        {
            var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var baked = new Mesh { name = "PerforationWardenSlamCapture_Iter2238" };
            GameObject proxy = null;
            var wasEnabled = skin.enabled;
            try
            {
                skin.BakeMesh(baked);
                proxy = new GameObject("BakedPoseCaptureOnly_Iter2238");
                proxy.transform.SetParent(skin.transform.parent, false);
                proxy.transform.localPosition = skin.transform.localPosition;
                proxy.transform.localRotation = skin.transform.localRotation;
                proxy.transform.localScale = skin.transform.localScale;
                proxy.AddComponent<MeshFilter>().sharedMesh = baked;
                proxy.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, baked.subMeshCount).ToArray();
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
