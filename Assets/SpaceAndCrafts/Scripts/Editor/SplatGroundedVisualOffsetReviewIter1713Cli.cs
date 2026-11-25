#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Direct-clip, non-Play-Mode review of Iter1711 sole-to-platform alignment.
    /// Creates a clearly labeled review-only prefab only when Idle and Land-settle
    /// foot-weighted sole heights support one stable vertical correction.
    /// </summary>
    public static class SplatGroundedVisualOffsetReviewIter1713Cli
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.fbx";
        private const string ReviewPrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/ReviewOnly/Splat_Iter1711_GroundedVisualOffset_ReviewOnly.prefab";
        private const string OutputDirectory = "Logs/splat-grounded-visual-offset-review-iter1713";
        private const float StablePoseTolerance = 0.03f;
        private const int Resolution = 900;
        private static Camera _camera;
        private static RenderTexture _renderTexture;

        [MenuItem("Space & Crafts/Review Iter1711 Grounded Visual Offset (No Play Mode)")]
        public static void Capture()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Cannot load Iter1711 Splat FBX: " + ModelPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            var idle = clips.FirstOrDefault(c => c.name.EndsWith("Splat_Idle", StringComparison.OrdinalIgnoreCase));
            var land = clips.FirstOrDefault(c => c.name.EndsWith("Splat_Land", StringComparison.OrdinalIgnoreCase));
            if (idle == null || land == null)
                throw new InvalidOperationException($"Expected embedded Idle and Land clips; found: {string.Join(", ", clips.Select(c => c.name))}");

            Directory.CreateDirectory(OutputDirectory);
            var report = new StringBuilder();
            report.AppendLine("Scope: isolated Editor direct-clip visual review only; no Play Mode and no production scene/prefab changes.");
            report.AppendLine($"source={ModelPath}");
            report.AppendLine($"supportTopY=0; toleranceForOneOffset={StablePoseTolerance:F3}m");
            report.AppendLine($"idleClip={idle.name} length={idle.length:F3}s landClip={land.name} length={land.length:F3}s");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var stage = new GameObject("Iter1713 temporary visual-offset review stage");
            var holder = new GameObject("Splat Iter1711 review-only visual root");
            holder.transform.SetParent(stage.transform, false);
            var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null) throw new InvalidOperationException("Failed to instantiate Iter1711 FBX for direct clip sampling.");
            visual.name = "Unmodified Iter1711 FBX visual child";
            visual.transform.SetParent(holder.transform, false);
            var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            var sourceBounds = CalculateBounds(visual);
            var motorRootOffsetY = -sourceBounds.min.y;
            report.AppendLine($"motorRootOffsetYFromSourceBounds={motorRootOffsetY:F5}m sourceBoundsMinY={sourceBounds.min.y:F5}m");

            var bindPose = visual.GetComponentsInChildren<Transform>(true).ToDictionary(
                t => t, t => (t.localPosition, t.localRotation, t.localScale));
            var idleMeasurement = SampleAndMeasure(visual, bindPose, idle, idle.length * 0.5f);
            var landMeasurement = SampleAndMeasure(visual, bindPose, land, land.length * 0.98f);
            AppendMeasurement(report, "idle-mid", idleMeasurement);
            AppendMeasurement(report, "land-settle", landMeasurement);

            var stable = idleMeasurement.HasFootVertices && landMeasurement.HasFootVertices
                && Mathf.Abs(idleMeasurement.FootMinY - landMeasurement.FootMinY) <= StablePoseTolerance;
            report.AppendLine($"stableSingleOffset={stable} reason={(idleMeasurement.HasFootVertices && landMeasurement.HasFootVertices ? "pose delta " + Mathf.Abs(idleMeasurement.FootMinY - landMeasurement.FootMinY).ToString("F5") + "m" : "no foot-weighted vertices could be isolated")}");

            SetupReviewEnvironment(report, stage.transform);
            holder.transform.localPosition = Vector3.up * motorRootOffsetY;
            var correction = 0f;
            if (stable)
            {
                correction = -motorRootOffsetY - (idleMeasurement.FootMinY + landMeasurement.FootMinY) * 0.5f;
                visual.transform.localPosition = Vector3.up * correction;
                report.AppendLine($"reviewPrefabOffsetY={correction:F5}m; source FBX is unchanged");
                CreateReviewPrefab(holder, correction, report);
                AssetDatabase.SaveAssets();
            }
            else
            {
                report.AppendLine("review prefab not created because a single correction is not supported by Idle and Land settle measurements.");
            }

            try
            {
                SampleAndMeasure(visual, bindPose, idle, idle.length * 0.5f);
                if (stable) visual.transform.localPosition += Vector3.up * correction;
                CapturePose("idle-mid", holder.transform, visual.transform, idle, idle.length * 0.5f, report);
                SampleAndMeasure(visual, bindPose, land, land.length * 0.98f);
                if (stable) visual.transform.localPosition += Vector3.up * correction;
                CapturePose("land-settle", holder.transform, visual.transform, land, land.length * 0.98f, report);
            }
            finally
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "alignment-review.txt"), report.ToString());
                Debug.Log("[Splat Iter1711 grounded offset review]\n" + report);
                EditorApplication.Exit(0);
            }
        }

        private static Measurement SampleAndMeasure(GameObject visual, Dictionary<Transform, (Vector3, Quaternion, Vector3)> bindPose, AnimationClip clip, float time)
        {
            foreach (var pair in bindPose)
            {
                pair.Key.localPosition = pair.Value.Item1;
                pair.Key.localRotation = pair.Value.Item2;
                pair.Key.localScale = pair.Value.Item3;
            }
            clip.SampleAnimation(visual, Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length - 0.001f)));
            return Measure(visual);
        }

        private static Measurement Measure(GameObject visual)
        {
            var result = new Measurement { RenderMinY = float.PositiveInfinity, FootMinY = float.PositiveInfinity };
            foreach (var skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skinned.sharedMesh;
                if (mesh == null || skinned.bones == null) continue;
                var weights = mesh.boneWeights;
                var footIndices = new HashSet<int>();
                for (var i = 0; i < skinned.bones.Length; i++)
                {
                    var bone = skinned.bones[i];
                    if (bone == null) continue;
                    var lower = bone.name.ToLowerInvariant();
                    if (lower.Contains("foot") || lower.Contains("toe") || lower.Contains("ankle")) footIndices.Add(i);
                }
                if (footIndices.Count == 0) continue;

                var baked = new Mesh { name = "Iter1713 transient foot measurement" };
                try
                {
                    skinned.BakeMesh(baked);
                    var vertices = baked.vertices;
                    foreach (var vertex in vertices)
                        result.RenderMinY = Mathf.Min(result.RenderMinY, skinned.transform.TransformPoint(vertex).y);
                    for (var i = 0; i < Mathf.Min(vertices.Length, weights.Length); i++)
                    {
                        var weight = weights[i];
                        if (!HasFootWeight(weight, footIndices)) continue;
                        result.FootMinY = Mathf.Min(result.FootMinY, skinned.transform.TransformPoint(vertices[i]).y);
                        result.FootVertexCount++;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SkinnedMeshRenderer)))
                result.RenderMinY = Mathf.Min(result.RenderMinY, renderer.bounds.min.y);
            result.HasFootVertices = result.FootVertexCount > 0 && !float.IsInfinity(result.FootMinY);
            if (float.IsPositiveInfinity(result.RenderMinY)) result.RenderMinY = float.NaN;
            if (float.IsPositiveInfinity(result.FootMinY)) result.FootMinY = float.NaN;
            return result;
        }

        private static bool HasFootWeight(BoneWeight weight, HashSet<int> indices)
        {
            return (weight.weight0 > 0.05f && indices.Contains(weight.boneIndex0))
                || (weight.weight1 > 0.05f && indices.Contains(weight.boneIndex1))
                || (weight.weight2 > 0.05f && indices.Contains(weight.boneIndex2))
                || (weight.weight3 > 0.05f && indices.Contains(weight.boneIndex3));
        }

        private static void AppendMeasurement(StringBuilder report, string name, Measurement m)
        {
            report.AppendLine($"{name}: footWeightedSoleMinY={m.FootMinY:F5}m vertices={m.FootVertexCount} sourceRenderMinY={m.RenderMinY:F5}m");
        }

        private static void CreateReviewPrefab(GameObject holder, float visualCorrection, StringBuilder report)
        {
            const string folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/ReviewOnly";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711", "ReviewOnly");
            var prefabRoot = new GameObject("Splat Iter1711 Grounded Visual Offset REVIEW ONLY");
            var visualCopy = UnityEngine.Object.Instantiate(holder.transform.GetChild(0).gameObject);
            if (visualCopy == null) throw new InvalidOperationException("Unable to create review-only prefab visual instance.");
            visualCopy.transform.SetParent(prefabRoot.transform, false);
            visualCopy.transform.localPosition = Vector3.up * visualCorrection;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ReviewPrefabPath);
            UnityEngine.Object.DestroyImmediate(prefabRoot);
            report.AppendLine("reviewPrefab=" + ReviewPrefabPath);
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Iter1711 FBX has no renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void SetupReviewEnvironment(StringBuilder report, Transform stage)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Temporary review support plane (top Y=0)";
            plane.transform.SetParent(stage, false);
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = Vector3.one * 0.4f;
            plane.GetComponent<Collider>().enabled = false;
            SetColor(plane.GetComponent<Renderer>(), new Color(0.33f, 0.39f, 0.48f));

            var lightObject = new GameObject("Temporary review key light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.58f, 0.64f);

            var cameraObject = new GameObject("Temporary review camera");
            cameraObject.transform.position = new Vector3(2.1f, 1.45f, 5.0f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.92f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.71f, 0.79f, 0.88f);
            camera.orthographic = true;
            camera.orthographicSize = 1.28f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 100f;
            _camera = camera;
            _renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
            camera.targetTexture = _renderTexture;
            report.AppendLine("captureCamera=orthographic, stage platform plane top=0");
        }

        private static void CapturePose(string label, Transform holder, Transform visual, AnimationClip clip, float time, StringBuilder report)
        {
            var camera = _camera;
            if (camera == null) return;
            var baked = Measure(visual.gameObject);
            report.AppendLine($"offsetPose={label} clip={clip.name} sampleTime={time:F3} correctedFootMinY={baked.FootMinY:F5}m correctedRenderMinY={baked.RenderMinY:F5}m");
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = _renderTexture });
            else camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            image.Apply(false, false);
            var path = Path.Combine(OutputDirectory, label + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            report.AppendLine("image=" + path);
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
        }

        private static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var material = new Material(shader) { name = "Iter1713 temporary review support material" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
        }

        private sealed class Measurement
        {
            public float RenderMinY;
            public float FootMinY;
            public int FootVertexCount;
            public bool HasFootVertices;
        }

    }
}
#endif
