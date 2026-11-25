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
    /// <summary>
    /// Review-only staged Animator transition capture for the existing Sellsword candidate.
    /// It deliberately never fires the unsafe Spin trigger and never touches production scenes.
    /// </summary>
    public static class CaptureSnipSnapSellswordTransitionsIter2193
    {
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/UnityCandidate_ReviewOnly_Iter2073/SnipSnapSellsword_ReviewOnly.prefab";
        private const string Output = "Logs/score-comparison/snip-snap-sellsword-transition-review-iter2193";
        private const int CellWidth = 480;
        private const int CellHeight = 540;
        private const float Step = 1f / 30f;

        [MenuItem("Space & Crafts/Capture Sellsword Animator Transitions (Iter 2193 ReviewOnly)")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing isolated Sellsword review prefab", PrefabPath);

            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            Texture2D sheet = null;
            var report = new StringBuilder();
            report.AppendLine("Snip-Snap Sellsword Iteration 2193 — isolated staged Animator transition capture.");
                report.AppendLine("Sequence: Idle → Walk → Run → decelerate → Idle → Prep → Attack → Hit → Dead. Action entry and later-action samples are captured; Spin is deliberately never triggered.");
            report.AppendLine("Animator.Update staging only: no gameplay loop, AI, physics/collider contact, ordinary gameplay camera, build, or human play.");
            var tiles = new List<Color[]>();

            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f, 1f);
                RenderSettings.skybox = null;
                AddLight(scene, "ReviewKey", new Vector3(32f, -28f, 0f), 1.2f, new Color(1f, 0.95f, 0.88f));
                AddLight(scene, "ReviewFill", new Vector3(18f, 145f, 0f), 0.65f, new Color(0.78f, 0.86f, 1f));

                var cameraObject = new GameObject("SellswordTransitionReviewCamera");
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
                if (instance == null) throw new InvalidOperationException("Could not instantiate isolated review prefab.");
                instance.name = "SnipSnapSellsword_TransitionReviewOnly_Iter2193";
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("Review prefab has no valid Animator, controller, or Avatar.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;
                animator.Rebind();
                animator.Update(0f);

                var idleBounds = RendererBounds(instance);
                var idleBoots = GetWeightedBootBottoms(animator);
                var floorY = MinAvailable(idleBoots.leftY, idleBoots.rightY);
                if (float.IsNaN(floorY)) throw new InvalidOperationException("Could not identify weighted foot/toe vertices for the floor reference.");
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "NeutralGroundReference_NoPhysicsClaim";
                floor.transform.position = new Vector3(0f, floorY, 0f);
                floor.transform.localScale = Vector3.one * 2f;
                SceneManager.MoveGameObjectToScene(floor, scene);
                camera.orthographicSize = Mathf.Max(idleBounds.size.y * 0.72f, idleBounds.size.x / (2f * camera.aspect) * 1.28f);
                var targetCenter = idleBounds.center;
                cameraObject.transform.position = targetCenter + new Vector3(0f, 0.04f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(targetCenter - cameraObject.transform.position, Vector3.up);

                target = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(CellWidth, CellHeight, TextureFormat.RGBA32, false);
                // Warm the shader/texture path before saving the first (Idle) tile.
                camera.Render();
                RenderTexture.active = null;
                animator.Rebind();
                animator.Update(0f);

                CaptureStage("01_Idle", 0.2f);
                animator.SetFloat("MoveSpeed", 0.7f);
                Advance(animator, 0.42f);
                CaptureStage("02_Walk", 0f);
                animator.SetFloat("MoveSpeed", 1.8f);
                Advance(animator, 0.42f);
                CaptureStage("03_Run", 0f);
                animator.SetFloat("MoveSpeed", 0f);
                Advance(animator, 0.5f);
                CaptureStage("04_DecelerateToIdle", 0f);

                animator.SetTrigger("Prep");
                Advance(animator, 0.04f);
                CaptureStage("05_PrepEntry", 0f);
                Advance(animator, 1.7f);
                CaptureStage("06_PrepMid", 0f);
                AdvanceToIdle(animator, 4f);
                animator.SetTrigger("Attack");
                Advance(animator, 0.04f);
                CaptureStage("07_AttackEntry", 0f);
                Advance(animator, 1.2f);
                CaptureStage("08_AttackMid", 0f);
                AdvanceToIdle(animator, 4f);
                animator.SetTrigger("Hit");
                Advance(animator, 0.04f);
                CaptureStage("09_HitEntry", 0f);
                Advance(animator, 0.7f);
                CaptureStage("10_HitMid", 0f);
                AdvanceToIdle(animator, 4f);
                animator.SetTrigger("Defeat");
                Advance(animator, 0.04f);
                CaptureStage("11_DeadEntry", 0f);
                Advance(animator, 2.2f);
                CaptureStage("12_DeadMid", 0f);

                const int columns = 3;
                var rows = (tiles.Count + columns - 1) / columns;
                sheet = new Texture2D(CellWidth * columns, CellHeight * rows, TextureFormat.RGBA32, false);
                for (var i = 0; i < tiles.Count; i++)
                {
                    var x = i % columns;
                    var y = rows - 1 - i / columns;
                    sheet.SetPixels(x * CellWidth, y * CellHeight, CellWidth, CellHeight, tiles[i]);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "Sellsword_AnimatorTransitionSequence.png"), sheet.EncodeToPNG());
                report.AppendLine();
                report.AppendLine("Ground reference is the initial idle minimum y of vertices with at least 25% combined LeftFoot/LeftToeBase or RightFoot/RightToeBase skin weight. This is a weighted-vertex sole proxy, not collision/platform contact. Root motion is disabled. The controller contains a Spin transition for other reviews; this capture never sets the Spin trigger.");
                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"), report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log("[Sellsword Iter2193] staged transition strip saved; review prefab only, Spin not triggered, production content untouched.");
                if (Application.isBatchMode) EditorApplication.Exit(0);

                void CaptureStage(string label, float extraAdvance)
                {
                    if (extraAdvance > 0f) Advance(animator, extraAdvance);
                    var current = animator.GetCurrentAnimatorStateInfo(0);
                    var next = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0).shortNameHash.ToString() : "none";
                    var bounds = RendererBounds(instance);
                    var boots = GetWeightedBootBottoms(animator);
                    var lowBoot = MinAvailable(boots.leftY, boots.rightY);
                    var footDelta = float.IsNaN(lowBoot) ? float.NaN : lowBoot - floorY;
                    report.AppendLine($"{label}: currentHash={current.shortNameHash}; normalized={current.normalizedTime:F3}; transitioning={animator.IsInTransition(0)}; nextHash={next}; leftWeightedBootY={boots.leftY:F3}; rightWeightedBootY={boots.rightY:F3}; lowestBootDelta={Fmt(footDelta)} m vs idle weighted-vertex floor; rendererMinY={bounds.min.y:F3}; rendererDelta={bounds.min.y - idleBounds.min.y:+0.000;-0.000;0.000} m (coarse proxy).");
                    camera.Render();
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

        private static void AdvanceToIdle(Animator animator, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                Advance(animator, Step);
                elapsed += Step;
                if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("Idle"))
                    return;
            }
        }

        private static void Advance(Animator animator, float seconds)
        {
            var frames = Mathf.Max(1, Mathf.CeilToInt(seconds / Step));
            for (var i = 0; i < frames; i++) animator.Update(Step);
        }

        private static (float leftY, float rightY) GetWeightedBootBottoms(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer => renderer != null && renderer.sharedMesh != null);
            if (skin == null) return (float.NaN, float.NaN);
            var left = new HashSet<int>();
            var right = new HashSet<int>();
            for (var i = 0; i < skin.bones.Length; i++)
            {
                var bone = skin.bones[i];
                if (bone == null) continue;
                if (bone.name == "LeftFoot" || bone.name == "LeftToeBase") left.Add(i);
                if (bone.name == "RightFoot" || bone.name == "RightToeBase") right.Add(i);
            }
            if (left.Count == 0 || right.Count == 0) return (float.NaN, float.NaN);
            var weights = skin.sharedMesh.boneWeights;
            var baked = new Mesh { name = "SellswordTransitionPoseBake_Iter2193" };
            try
            {
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length != weights.Length) return (float.NaN, float.NaN);
                var leftMin = float.PositiveInfinity;
                var rightMin = float.PositiveInfinity;
                for (var i = 0; i < vertices.Length; i++)
                {
                    var worldY = skin.transform.TransformPoint(vertices[i]).y;
                    if (BoneWeightFor(weights[i], left) >= 0.25f) leftMin = Mathf.Min(leftMin, worldY);
                    if (BoneWeightFor(weights[i], right) >= 0.25f) rightMin = Mathf.Min(rightMin, worldY);
                }
                return (float.IsPositiveInfinity(leftMin) ? float.NaN : leftMin,
                    float.IsPositiveInfinity(rightMin) ? float.NaN : rightMin);
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        private static float BoneWeightFor(BoneWeight weight, HashSet<int> indices)
        {
            var total = 0f;
            if (indices.Contains(weight.boneIndex0)) total += weight.weight0;
            if (indices.Contains(weight.boneIndex1)) total += weight.weight1;
            if (indices.Contains(weight.boneIndex2)) total += weight.weight2;
            if (indices.Contains(weight.boneIndex3)) total += weight.weight3;
            return total;
        }

        private static float MinAvailable(float a, float b)
        {
            if (float.IsNaN(a)) return b;
            if (float.IsNaN(b)) return a;
            return Mathf.Min(a, b);
        }

        private static string Fmt(float value) => float.IsNaN(value)
            ? "missing"
            : value.ToString("+0.000;-0.000;0.000");

        private static Bounds RendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible renderers in review candidate.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AddLight(Scene scene, string name, Vector3 rotation, float intensity, Color color)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            gameObject.transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
#endif
