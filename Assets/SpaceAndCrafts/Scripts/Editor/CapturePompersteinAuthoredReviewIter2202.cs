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
    /// <summary>Staged Animator review for Pomperstein's existing rig/actions; production scenes remain untouched.</summary>
    public static class CapturePompersteinAuthoredReviewIter2202
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Enemies/Pomperstein/Meshy_Iter1997/UnityCandidate_ReviewOnly_Iter2202/Pomperstein_ReviewOnly.prefab";
        private const string Output = "Logs/score-comparison/pomperstein-authored-ground-slam-review-iter2202";
        private const int CellWidth = 480;
        private const int CellHeight = 540;
        private const float Step = 1f / 30f;

        [MenuItem("Space & Crafts/Capture Pomperstein Authored Slam Comparison (Iter 2202 ReviewOnly)")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing isolated Pomperstein Iter2202 review prefab", PrefabPath);
            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            Texture2D sheet = null;
            var report = new StringBuilder();
            var tiles = new List<Color[]>();
            report.AppendLine("Pomperstein Iteration 2202 — staged Animator comparison of Meshy and Blender-authored slam clips.");
            report.AppendLine("Sequence: Idle → Meshy Charged Ground Slam (sample) → Idle → Authored Slam transition/windup/impact/brace/recovery → Idle. Recovery/locomotion remain on their Iter2200 source clips.");
            report.AppendLine("Review-only Animator.Update capture; no AI, combat logic, collision/platform physics, game camera, build, or human play.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f, 1f);
                RenderSettings.skybox = null;
                AddLight(scene, "ReviewKey", new Vector3(32f, -28f, 0f), 1.2f, new Color(1f, 0.95f, 0.88f));
                AddLight(scene, "ReviewFill", new Vector3(18f, 145f, 0f), 0.65f, new Color(0.78f, 0.86f, 1f));
                var cameraObject = new GameObject("PompersteinReviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.105f, 0.13f, 0.18f, 1f);
                camera.orthographic = true;
                camera.aspect = CellWidth / (float)CellHeight;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Pomperstein review prefab.");
                instance.name = "Pomperstein_TransitionReviewOnly_Iter2200";
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("Review prefab is missing a valid Animator/Avatar/controller.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;
                animator.Rebind();
                animator.Update(0f);
                var idleBounds = RendererBounds(instance);
                var idleFeet = WeightedBootBottoms(animator);
                var floorY = float.IsNaN(Min(idleFeet.left, idleFeet.right)) ? idleBounds.min.y : Min(idleFeet.left, idleFeet.right);

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "NeutralGroundReference_NoPhysicsClaim";
                floor.transform.position = new Vector3(0f, floorY, 0f);
                floor.transform.localScale = Vector3.one * 2f;
                SceneManager.MoveGameObjectToScene(floor, scene);
                camera.orthographicSize = Mathf.Max(idleBounds.size.y * 0.72f, idleBounds.size.x / (2f * camera.aspect) * 1.28f);
                var center = idleBounds.center;
                cameraObject.transform.position = center + new Vector3(0f, 0.04f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);
                target = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(CellWidth, CellHeight, TextureFormat.RGBA32, false);
                camera.Render();
                RenderTexture.active = null;
                animator.Rebind();
                animator.Update(0f);
                var idleVertices = BakeVertices(animator);

                CaptureStage("01_Idle", 0.2f);
                animator.SetTrigger("Slam"); Advance(animator, 1.45f); CaptureStage("02_MeshySlamMid", 0f);
                AdvanceToIdle(animator, 4f);
                animator.SetTrigger("AuthoredSlam"); Advance(animator, 0.06f); CaptureStage("03_AuthoredSlamEntry", 0f);
                Advance(animator, 0.20f); CaptureStage("04_AuthoredWindup", 0f);
                Advance(animator, 0.23f); CaptureStage("05_ArmsRaised", 0f);
                Advance(animator, 0.23f); CaptureStage("06_ImpactApproach", 0f);
                Advance(animator, 0.10f); CaptureStage("07_Impact", 0f);
                Advance(animator, 0.20f); CaptureStage("08_Brace", 0f);
                Advance(animator, 0.35f); CaptureStage("09_Recovery", 0f);
                Advance(animator, 0.40f); CaptureStage("10_AuthoredLate", 0f);
                AdvanceToIdle(animator, 3.0f); CaptureStage("11_RecoveryIdle", 0f);

                const int columns = 4;
                var rows = (tiles.Count + columns - 1) / columns;
                sheet = new Texture2D(CellWidth * columns, CellHeight * rows, TextureFormat.RGBA32, false);
                for (var i = 0; i < tiles.Count; i++)
                {
                    sheet.SetPixels((i % columns) * CellWidth, (rows - 1 - i / columns) * CellHeight,
                        CellWidth, CellHeight, tiles[i]);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "Pomperstein_AnimatorSequence.png"), sheet.EncodeToPNG());
                report.AppendLine();
                report.AppendLine("Floor reference is initial idle minimum vertex y with >=25% foot/toe skin weight. Weighted vertices and renderer bounds are pose proxies only; neither simulates platform collisions.");
                report.AppendLine("Authored Ground Slam came from the review-only Blender FBX in UnityCandidate_ReviewOnly_Iter2202; it is not Meshy-generated. Original Meshy Ground Slam remains in the controller for a direct sampled comparison. No production encounter is claimed.");
                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"), report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log("[Pomperstein Iter2202] staged Meshy/authored Ground Slam comparison saved; review prefab only, no production scene modified.");
                if (Application.isBatchMode) EditorApplication.Exit(0);

                void CaptureStage(string label, float extraAdvance)
                {
                    if (extraAdvance > 0f) Advance(animator, extraAdvance);
                    var current = animator.GetCurrentAnimatorStateInfo(0);
                    var next = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0).shortNameHash.ToString() : "none";
                    var bounds = RendererBounds(instance);
                    var feet = WeightedBootBottoms(animator);
                    var deformedVertices = BakeVertices(animator);
                    var deformation = VertexDisplacement(idleVertices, deformedVertices);
                    var low = Min(feet.left, feet.right);
                    var footDelta = float.IsNaN(low) ? float.NaN : low - floorY;
                    report.AppendLine($"{label}: hash={current.shortNameHash}; normalized={current.normalizedTime:F3}; transitioning={animator.IsInTransition(0)}; next={next}; weightedFeet=({feet.left:F3},{feet.right:F3}); lowestFootDelta={Fmt(footDelta)}m; boundsMinY={bounds.min.y:F3}; meshDisplacementMax={deformation.max:F4}m/rms={deformation.rms:F4}m; joints={JointPose(instance)}.");
                    RenderBakedPose(camera, instance);
                    RenderTexture.active = target;
                    scratch.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0);
                    scratch.Apply();
                    tiles.Add(scratch.GetPixels());
                    File.WriteAllBytes(Path.Combine(Output, label + ".png"), scratch.EncodeToPNG());
                }
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

        private static void AdvanceToIdle(Animator animator, float timeout)
        {
            for (var elapsed = 0f; elapsed < timeout; elapsed += Step)
            {
                Advance(animator, Step);
                if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("Idle")) return;
            }
        }
        private static void Advance(Animator animator, float seconds)
        {
            for (var i = 0; i < Mathf.Max(1, Mathf.CeilToInt(seconds / Step)); i++) animator.Update(Step);
        }
        private static Bounds RendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible renderer.");
            var bounds = renderers[0].bounds; for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds); return bounds;
        }
        private static Vector3[] BakeVertices(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(item => item.sharedMesh != null);
            if (skin == null) throw new InvalidOperationException("No skinned mesh available for the deformation review.");
            var baked = new Mesh { name = "PompersteinPoseAudit_Iter2202" };
            try { skin.BakeMesh(baked); return baked.vertices; }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }
        private static void RenderBakedPose(Camera camera, GameObject instance)
        {
            var skin = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(item => item.sharedMesh != null);
            if (skin == null) throw new InvalidOperationException("No skinned mesh available for the pose capture.");
            var baked = new Mesh { name = "PompersteinRenderedPose_Iter2202" };
            GameObject proxy = null;
            var wasEnabled = skin.enabled;
            try
            {
                skin.BakeMesh(baked);
                proxy = new GameObject("BakedPoseCaptureOnly_Iter2202");
                proxy.transform.SetParent(skin.transform.parent, false);
                proxy.transform.localPosition = skin.transform.localPosition;
                proxy.transform.localRotation = skin.transform.localRotation;
                proxy.transform.localScale = skin.transform.localScale;
                proxy.AddComponent<MeshFilter>().sharedMesh = baked;
                proxy.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
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
        private static (float max, float rms) VertexDisplacement(Vector3[] reference, Vector3[] current)
        {
            if (reference == null || reference.Length != current.Length) return (float.NaN, float.NaN);
            var max = 0f; var sum = 0.0;
            for (var i = 0; i < reference.Length; i++)
            {
                var delta = Vector3.Distance(reference[i], current[i]);
                max = Mathf.Max(max, delta); sum += delta * delta;
            }
            return (max, Mathf.Sqrt((float)(sum / reference.Length)));
        }
        private static string JointPose(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            var names = new[] { "LeftArm", "RightArm", "LeftForeArm", "RightForeArm", "Spine", "Spine01", "Spine02" };
            return string.Join(";", names.Select(name =>
            {
                var joint = transforms.FirstOrDefault(item => item.name == name);
                return joint == null ? name + "=missing" : name + "=" + joint.localRotation.eulerAngles.ToString("F1");
            }));
        }
        private static (float left, float right) WeightedBootBottoms(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(item => item.sharedMesh != null);
            if (skin == null) return (float.NaN, float.NaN);
            var leftBones = new HashSet<int>(); var rightBones = new HashSet<int>();
            for (var i = 0; i < skin.bones.Length; i++)
            {
                var name = skin.bones[i]?.name;
                if (name == "LeftFoot" || name == "LeftToeBase") leftBones.Add(i);
                if (name == "RightFoot" || name == "RightToeBase") rightBones.Add(i);
            }
            if (leftBones.Count == 0 || rightBones.Count == 0) return (float.NaN, float.NaN);
            var weights = skin.sharedMesh.boneWeights;
            var baked = new Mesh { name = "PompersteinTransitionPoseBake_Iter2200" };
            try
            {
                skin.BakeMesh(baked); var vertices = baked.vertices;
                if (vertices.Length != weights.Length) return (float.NaN, float.NaN);
                var left = float.PositiveInfinity; var right = float.PositiveInfinity;
                for (var i = 0; i < vertices.Length; i++)
                {
                    var y = skin.transform.TransformPoint(vertices[i]).y;
                    if (Weight(weights[i], leftBones) >= 0.25f) left = Mathf.Min(left, y);
                    if (Weight(weights[i], rightBones) >= 0.25f) right = Mathf.Min(right, y);
                }
                return (float.IsPositiveInfinity(left) ? float.NaN : left, float.IsPositiveInfinity(right) ? float.NaN : right);
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }
        private static float Weight(BoneWeight weight, HashSet<int> indices)
        {
            var sum = 0f;
            if (indices.Contains(weight.boneIndex0)) sum += weight.weight0;
            if (indices.Contains(weight.boneIndex1)) sum += weight.weight1;
            if (indices.Contains(weight.boneIndex2)) sum += weight.weight2;
            if (indices.Contains(weight.boneIndex3)) sum += weight.weight3;
            return sum;
        }
        private static float Min(float a, float b) => float.IsNaN(a) ? b : float.IsNaN(b) ? a : Mathf.Min(a, b);
        private static string Fmt(float value) => float.IsNaN(value) ? "missing" : value.ToString("+0.000;-0.000;0.000");
        private static void AddLight(Scene scene, string name, Vector3 rotation, float intensity, Color color)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene);
            var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = intensity; light.color = color;
            go.transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
#endif
