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
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Captures isolated controller-state poses for the Iter2073
    /// Snip-Snap Sellsword review prefab. This is a pose/render review, not gameplay.
    /// </summary>
    public static class CaptureSnipSnapSellswordReviewIter2073
    {
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/UnityCandidate_ReviewOnly_Iter2073/SnipSnapSellsword_ReviewOnly.prefab";
        private const string Output = "Logs/score-comparison/snip-snap-sellsword-animation-review-iter2073";
        private const string MotionSweepOutput = "Logs/score-comparison/snip-snap-sellsword-animation-sweep-iter2073";
        private const float PreviewYawDegrees = 0f;
        private const int Width = 640;
        private const int Height = 720;
        private const int SweepCellWidth = 320;
        private const int SweepCellHeight = 360;
        private static readonly float[] SweepTimes = { 0f, 0.2f, 0.4f, 0.6f, 0.8f };
        private static readonly (string state, float time)[] Samples =
        {
            ("Idle", 0.25f), ("Walk", 0.35f), ("Run", 0.35f),
            ("Prep", 0.45f), ("Attack", 0.55f), ("Hit", 0.50f),
            ("Dead", 0.85f), ("Spin", 0.55f),
        };

        [MenuItem("Space & Crafts/Capture Snip-Snap Sellsword ReviewOnly Poses (Iter 2073)")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing isolated review prefab", PrefabPath);

            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            var report = new StringBuilder();
            report.AppendLine("Snip-Snap Sellsword Iteration 2073 isolated controller-state pose renders.");
            report.AppendLine("These images show sampled poses from controller clips, not transitions, movement, enemy AI, platform landing, or human play.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("SnipSnapSellswordReviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.105f, 0.13f, 0.18f, 1f);
                camera.orthographic = true;
                camera.aspect = Width / (float)Height;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                AddDirectionalLight(scene, "KeyLight", new Vector3(32f, -28f, 0f), 1.15f,
                    new Color(1f, 0.94f, 0.86f));
                AddDirectionalLight(scene, "FillLight", new Vector3(18f, 145f, 0f), 0.6f,
                    new Color(0.76f, 0.84f, 1f));
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.43f, 1f);
                RenderSettings.skybox = null;

                var union = new Bounds(Vector3.zero, Vector3.zero);
                var hasBounds = false;
                var boundsByState = new Dictionary<string, Bounds>();
                foreach (var sample in Samples)
                {
                    var instance = InstantiateAndSample(scene, prefab, sample.state, sample.time,
                        out var animator, out var activeState);
                    var bounds = GetRendererBounds(instance);
                    var activeClip = animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip;
                    var objectReferenceBindings = activeClip == null
                        ? Array.Empty<EditorCurveBinding>()
                        : AnimationUtility.GetObjectReferenceCurveBindings(activeClip);
                    var objectReferenceSummary = objectReferenceBindings.Length == 0
                        ? "none"
                        : string.Join(",", objectReferenceBindings.Select(binding =>
                            $"{binding.path}:{binding.propertyName}"));
                    if (!hasBounds) { union = bounds; hasBounds = true; }
                    else union.Encapsulate(bounds);
                    boundsByState[sample.state] = bounds;
                    report.AppendLine($"Sample {sample.state} t={sample.time:0.00}: evaluated={activeState}; clip={activeClip?.name ?? "none"}; objectReferenceCurves={objectReferenceSummary}; boundsMinY={bounds.min.y:F3}; boundsHeight={bounds.size.y:F3}; visibleSkins={animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                if (!hasBounds) throw new InvalidOperationException("No renderable states found.");

                var referenceFloorY = boundsByState["Idle"].min.y;
                report.AppendLine($"Review-only floor reference: y={referenceFloorY:F3}, calibrated to the sampled Idle lowest-renderer bound; this is a pose proxy, not measured boot-sole contact.");
                foreach (var sample in Samples)
                    report.AppendLine($"Pose floor delta {sample.state}: {boundsByState[sample.state].min.y - referenceFloorY:+0.000;-0.000;0.000} m (renderer-bound proxy).");

                var center = union.center;
                camera.orthographicSize = Mathf.Max(union.size.y * 0.68f,
                    union.size.x / (2f * camera.aspect) * 1.22f);
                cameraObject.transform.position = center + new Vector3(0f, 0.08f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "NeutralGroundReference";
                floor.transform.position = new Vector3(0f, referenceFloorY, 0f);
                floor.transform.localScale = Vector3.one * 2f;
                SceneManager.MoveGameObjectToScene(floor, scene);

                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    name = "SnipSnapSellswordReviewRenderTexture"
                };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                var tiles = new Color[Samples.Length][];

                for (var index = 0; index < Samples.Length; index++)
                {
                    var sample = Samples[index];
                    var instance = InstantiateAndSample(scene, prefab, sample.state, sample.time,
                        out _, out _);
                    camera.Render();
                    RenderTexture.active = target;
                    scratch.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    scratch.Apply();
                    var pixels = scratch.GetPixels();
                    tiles[index] = pixels;
                    var safeName = string.Concat(sample.state.Select(character =>
                        char.IsLetterOrDigit(character) ? character : '_'));
                    File.WriteAllBytes(Path.Combine(Output, $"{index + 1:00}_{safeName}.png"), scratch.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                const int columns = 3;
                var rows = (Samples.Length + columns - 1) / columns;
                var sheet = new Texture2D(Width * columns, Height * rows, TextureFormat.RGBA32, false);
                for (var index = 0; index < tiles.Length; index++)
                {
                    var column = index % columns;
                    var rowFromTop = index / columns;
                    var rowFromBottom = rows - 1 - rowFromTop;
                    sheet.SetPixels(column * Width, rowFromBottom * Height, Width, Height, tiles[index]);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "SnipSnapSellsword_3x3_PoseSheet.png"), sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);

                camera.targetTexture = null;
                RenderTexture.active = null;
                File.WriteAllText(Path.Combine(Output, "REVIEW.txt"),
                    "Rows, left to right: Idle / Walk / Run; Prep / Attack / Hit; Dead / Spin.\n" +
                    "Same imported review prefab, controller, camera, lighting, and fixed normalized state times. The neutral floor is calibrated to the Idle sample's lowest renderer bound; this is only a pose proxy, not sole contact measurement.\n" +
                    "A single sampled pose cannot verify animation playback quality, transitions, foot contact through motion, AI, gameplay, or human feel.\n\n" + report);
                AssetDatabase.Refresh();
                Debug.Log($"[SnipSnapSellsword Iter2073] rendered {Samples.Length} isolated controller-state pose images to {Output}; production scene untouched.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(Output, "CAPTURE_FAILURE.txt"), exception + "\n" + report);
                Debug.LogException(exception);
                EditorApplication.Exit(2);
            }
            finally
            {
                if (scratch != null) UnityEngine.Object.DestroyImmediate(scratch);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                RenderTexture.active = null;
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Space & Crafts/Capture Snip-Snap Sellsword Five-Point Motion Sweep (Iter 2073)")]
        public static void CaptureMotionSweep()
        {
            Directory.CreateDirectory(MotionSweepOutput);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing isolated review prefab", PrefabPath);

            Scene scene = default;
            RenderTexture target = null;
            Texture2D scratch = null;
            var report = new StringBuilder();
            report.AppendLine("Snip-Snap Sellsword Iteration 2073 five-point normalized-time clip sampling.");
            report.AppendLine("Fresh isolated Animator instance per sample; applyRootMotion=false; controller transitions, physics contact and player input are not exercised.");
            report.AppendLine("Foot/toe positions are rig-joint pivot proxies, not boot-sole points. Renderer-bound deltas are whole-character proxies, not contact measurements.");
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("SnipSnapSellswordMotionSweepCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.105f, 0.13f, 0.18f, 1f);
                camera.orthographic = true;
                camera.aspect = SweepCellWidth / (float)SweepCellHeight;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                AddDirectionalLight(scene, "KeyLight", new Vector3(32f, -28f, 0f), 1.15f,
                    new Color(1f, 0.94f, 0.86f));
                AddDirectionalLight(scene, "FillLight", new Vector3(18f, 145f, 0f), 0.6f,
                    new Color(0.76f, 0.84f, 1f));
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.43f, 1f);
                RenderSettings.skybox = null;

                var stateNames = Samples.Select(sample => sample.state).Distinct().ToArray();
                var frames = stateNames.ToDictionary(state => state, _ => new List<SweepFrame>());
                var union = new Bounds(Vector3.zero, Vector3.zero);
                var hasBounds = false;
                foreach (var state in stateNames)
                foreach (var normalizedTime in SweepTimes)
                {
                    var instance = InstantiateAndSample(scene, prefab, state, normalizedTime,
                        out var animator, out var activeState);
                    var bounds = GetRendererBounds(instance);
                    var activeClip = animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip;
                    var armature = instance.transform.Find("Armature");
                    var hips = instance.transform.Find("Armature/Hips");
                    var leftFoot = instance.transform.Find("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");
                    var leftToe = instance.transform.Find("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase");
                    var rightFoot = instance.transform.Find("Armature/Hips/RightUpLeg/RightLeg/RightFoot");
                    var rightToe = instance.transform.Find("Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase");
                    var bootBounds = GetBoneWeightedBootBottoms(animator);
                    frames[state].Add(new SweepFrame
                    {
                        NormalizedTime = normalizedTime,
                        Bounds = bounds,
                        ClipName = activeClip != null ? activeClip.name : "none",
                        ArmatureLocalYaw = armature != null ? armature.localEulerAngles.y : float.NaN,
                        HipsLocalYaw = hips != null ? hips.localEulerAngles.y : float.NaN,
                        LeftFoot = WorldPosition(leftFoot),
                        LeftToe = WorldPosition(leftToe),
                        RightFoot = WorldPosition(rightFoot),
                        RightToe = WorldPosition(rightToe),
                        LeftBootBottomY = bootBounds.leftY,
                        RightBootBottomY = bootBounds.rightY,
                    });
                    if (!hasBounds) { union = bounds; hasBounds = true; }
                    else union.Encapsulate(bounds);
                    if (!string.Equals(activeState, state, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Requested {state} at t={normalizedTime:F2}, evaluated {activeState}.");
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                if (!hasBounds) throw new InvalidOperationException("No renderable sweep samples found.");

                var idleReference = frames["Idle"].First(frame => Mathf.Approximately(frame.NormalizedTime, 0.2f));
                var referenceFloorY = MinAvailable(idleReference.LeftBootBottomY, idleReference.RightBootBottomY);
                var floorSource = "Idle t=0.20 mesh vertices with >=25% foot/toe bone weight";
                if (float.IsNaN(referenceFloorY))
                {
                    referenceFloorY = idleReference.Bounds.min.y;
                    floorSource = "fallback: Idle t=0.20 whole-renderer bound; no weighted boot vertices found";
                }
                report.AppendLine($"Review floor y={referenceFloorY:F3} from {floorSource}; weighted baked vertices estimate soles but do not simulate collision.");
                foreach (var state in stateNames)
                foreach (var frame in frames[state])
                    report.AppendLine($"{state} t={frame.NormalizedTime:F2} clip={frame.ClipName} boundsMinY={frame.Bounds.min.y:F3} boundsFloorDelta={frame.Bounds.min.y - referenceFloorY:+0.000;-0.000;0.000}m armatureLocalYaw={Fmt(frame.ArmatureLocalYaw)} hipsLocalYaw={Fmt(frame.HipsLocalYaw)} leftFootXYZ={Fmt(frame.LeftFoot)} leftToeXYZ={Fmt(frame.LeftToe)} leftBootBottomY={Fmt(frame.LeftBootBottomY)} leftBootFloorDelta={FmtDelta(frame.LeftBootBottomY, referenceFloorY)} rightFootXYZ={Fmt(frame.RightFoot)} rightToeXYZ={Fmt(frame.RightToe)} rightBootBottomY={Fmt(frame.RightBootBottomY)} rightBootFloorDelta={FmtDelta(frame.RightBootBottomY, referenceFloorY)}");

                var center = union.center;
                camera.orthographicSize = Mathf.Max(union.size.y * 0.68f,
                    union.size.x / (2f * camera.aspect) * 1.22f);
                cameraObject.transform.position = center + new Vector3(0f, 0.08f, 5f);
                cameraObject.transform.rotation = Quaternion.LookRotation(center - cameraObject.transform.position, Vector3.up);

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "IdleCalibratedRendererBoundsReferenceOnly";
                floor.transform.position = new Vector3(0f, referenceFloorY, 0f);
                floor.transform.localScale = Vector3.one * 2f;
                SceneManager.MoveGameObjectToScene(floor, scene);

                target = new RenderTexture(SweepCellWidth, SweepCellHeight, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    name = "SnipSnapSellswordMotionSweepRenderTexture"
                };
                target.Create();
                camera.targetTexture = target;
                scratch = new Texture2D(SweepCellWidth, SweepCellHeight, TextureFormat.RGBA32, false);

                foreach (var state in stateNames)
                {
                    var tiles = new List<Color[]>();
                    foreach (var normalizedTime in SweepTimes)
                    {
                        var instance = InstantiateAndSample(scene, prefab, state, normalizedTime,
                            out _, out _);
                        camera.Render();
                        RenderTexture.active = target;
                        scratch.ReadPixels(new Rect(0, 0, SweepCellWidth, SweepCellHeight), 0, 0);
                        scratch.Apply();
                        tiles.Add(scratch.GetPixels());
                        UnityEngine.Object.DestroyImmediate(instance);
                    }

                    var strip = new Texture2D(SweepCellWidth * SweepTimes.Length, SweepCellHeight,
                        TextureFormat.RGBA32, false);
                    for (var index = 0; index < tiles.Count; index++)
                        strip.SetPixels(index * SweepCellWidth, 0, SweepCellWidth, SweepCellHeight, tiles[index]);
                    strip.Apply();
                    var safeName = string.Concat(state.Select(character =>
                        char.IsLetterOrDigit(character) ? character : '_'));
                    File.WriteAllBytes(Path.Combine(MotionSweepOutput, safeName + "_FivePointSweep.png"),
                        strip.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(strip);
                }

                camera.targetTexture = null;
                RenderTexture.active = null;
                File.WriteAllText(Path.Combine(MotionSweepOutput, "REVIEW.txt"),
                    "Each strip reads left-to-right at normalized clip times 0.00 / 0.20 / 0.40 / 0.60 / 0.80.\n" +
                    "Every sample uses a fresh Animator instance and the same imported review prefab/controller, view, lighting, and floor calibrated to the Idle t=0.20 lowest mesh vertex receiving at least 25% combined left/right foot/toe bone weight (renderer bound fallback only if no such vertices exist).\n" +
                    "Foot/toe pivots and weighted baked-vertex bottoms are geometry proxies; the latter are closer to boot soles but still do not simulate collision/platform contact. Animator.Play samples states directly, so crossfades/transitions, continuous runtime playback, root motion, platform physics, AI, gameplay, and human play are not verified.\n\n" + report);
                AssetDatabase.Refresh();
                Debug.Log($"[SnipSnapSellsword Iter2073] rendered five-point normalized-time strips for {stateNames.Length} isolated clips to {MotionSweepOutput}; production scene untouched.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(MotionSweepOutput, "CAPTURE_FAILURE.txt"), exception + "\n" + report);
                Debug.LogException(exception);
                EditorApplication.Exit(2);
            }
            finally
            {
                if (scratch != null) UnityEngine.Object.DestroyImmediate(scratch);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                RenderTexture.active = null;
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private sealed class SweepFrame
        {
            public float NormalizedTime;
            public Bounds Bounds;
            public string ClipName;
            public float ArmatureLocalYaw, HipsLocalYaw;
            public Vector3 LeftFoot, LeftToe, RightFoot, RightToe;
            public float LeftBootBottomY, RightBootBottomY;
        }

        private static Vector3 WorldPosition(Transform transform) => transform != null
            ? transform.position
            : new Vector3(float.NaN, float.NaN, float.NaN);
        private static string Fmt(float value) => float.IsNaN(value) ? "missing" : value.ToString("F3");
        private static string Fmt(Vector3 value) => float.IsNaN(value.x)
            ? "missing"
            : $"({value.x:F3},{value.y:F3},{value.z:F3})";
        private static float MinAvailable(float left, float right)
        {
            if (float.IsNaN(left)) return right;
            if (float.IsNaN(right)) return left;
            return Mathf.Min(left, right);
        }
        private static string FmtDelta(float value, float floorY) => float.IsNaN(value)
            ? "missing"
            : $"{value - floorY:+0.000;-0.000;0.000}m";

        private static (float leftY, float rightY) GetBoneWeightedBootBottoms(Animator animator)
        {
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer => renderer != null && renderer.sharedMesh != null);
            if (skin == null) return (float.NaN, float.NaN);

            var bones = skin.bones;
            var leftBoneIndices = new HashSet<int>();
            var rightBoneIndices = new HashSet<int>();
            for (var index = 0; index < bones.Length; index++)
            {
                if (bones[index] == null) continue;
                if (bones[index].name == "LeftFoot" || bones[index].name == "LeftToeBase")
                    leftBoneIndices.Add(index);
                if (bones[index].name == "RightFoot" || bones[index].name == "RightToeBase")
                    rightBoneIndices.Add(index);
            }
            if (leftBoneIndices.Count == 0 || rightBoneIndices.Count == 0)
                return (float.NaN, float.NaN);

            var sourceMesh = skin.sharedMesh;
            var weights = sourceMesh.boneWeights;
            var baked = new Mesh { name = "SnipSnapSellsword_SamplePoseBake" };
            try
            {
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length != weights.Length) return (float.NaN, float.NaN);
                var leftMin = float.PositiveInfinity;
                var rightMin = float.PositiveInfinity;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    var weight = weights[vertexIndex];
                    var leftWeight = BoneWeightFor(weight, leftBoneIndices);
                    var rightWeight = BoneWeightFor(weight, rightBoneIndices);
                    var worldY = skin.transform.TransformPoint(vertices[vertexIndex]).y;
                    if (leftWeight >= 0.25f) leftMin = Mathf.Min(leftMin, worldY);
                    if (rightWeight >= 0.25f) rightMin = Mathf.Min(rightMin, worldY);
                }
                return (float.IsPositiveInfinity(leftMin) ? float.NaN : leftMin,
                    float.IsPositiveInfinity(rightMin) ? float.NaN : rightMin);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static float BoneWeightFor(BoneWeight weight, HashSet<int> boneIndices)
        {
            var total = 0f;
            if (boneIndices.Contains(weight.boneIndex0)) total += weight.weight0;
            if (boneIndices.Contains(weight.boneIndex1)) total += weight.weight1;
            if (boneIndices.Contains(weight.boneIndex2)) total += weight.weight2;
            if (boneIndices.Contains(weight.boneIndex3)) total += weight.weight3;
            return total;
        }

        private static GameObject InstantiateAndSample(Scene scene, GameObject prefab, string state,
            float normalizedTime, out Animator animator, out string activeState)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate the review prefab.");
            instance.name = "SnipSnapSellsword_" + state.Replace(' ', '_') + "_PoseReview";
            // Keep the isolated preview at the FBX's unmodified forward axis.
            // This angle sweep is review-only; it does not change the source or prefab.
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, PreviewYawDegrees, 0f));
            instance.transform.localScale = Vector3.one;
            animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isValid)
                throw new InvalidOperationException("Review prefab is missing a valid Avatar or Animator Controller.");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Play(Animator.StringToHash(state), 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0.016f);
            var info = animator.GetCurrentAnimatorStateInfo(0);
            activeState = info.IsName(state) ? state : $"unexpected hash={info.fullPathHash}";
            if (!info.IsName(state)) throw new InvalidOperationException($"Animator did not evaluate state '{state}'.");
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.updateWhenOffscreen = true;
            return instance;
        }

        private static Bounds GetRendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException($"No enabled renderers on {instance.name}.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void AddDirectionalLight(Scene scene, string name, Vector3 rotation,
            float intensity, Color color)
        {
            var lightObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            lightObject.transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
#endif

