#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Renders an isolated review of the Meshy Splat candidate on a flat support.
    /// It does not modify any gameplay scene, prefab, or runtime asset.
    /// </summary>
    public static class SplatMeshyFootSeatedCandidateReviewIter1364Cli
    {
        private const string CandidatePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1364/Splat_Meshy_Iter1337_FootSeated.fbx";
        private const string IdleJumpPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/animation_fbx.fbx";
        private const string WalkPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/FBX/walking_fbx.fbx";
        private const string RunPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/FBX/running_fbx.fbx";
        private const string LandingPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/LandingAnimation/animation_fbx.fbx";
        private const string JointIdlePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350/Joint_Idle.anim";
        private const string JointJumpPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350/Joint_Regular_Jump.anim";
        private const string JointLandPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350/Joint_Land.anim";
        private const string JointWalkPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350/Joint_Walk.anim";
        private const string JointRunPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350/Joint_Run.anim";
        private const int Resolution = 640;

        [MenuItem("Space & Crafts/Review Foot-Seated Splat Candidate on Flat Support")]
        public static void Capture()
        {
            var outputDirectory = Path.GetFullPath("Logs/splat-foot-seated-candidate-review-iter1364");
            Directory.CreateDirectory(outputDirectory);
            var report = new StringBuilder();
            report.AppendLine("Splat Meshy candidate isolated preview; not a gameplay test or active-scene render.");
            report.AppendLine("Joint_ clips are locally authored shoulder-root and limb rotations; torso/head transforms and all scale/position tracks stay at bind pose.");
            report.AppendLine("Iteration 1364 FBX reweights four lower boot-shell islands to LeftFoot/RightFoot; based on the Iteration 1347 face-seated candidate.");
            report.AppendLine("Support plane top: world Y = 0. Renderer and foot-weighted vertex bounds are measured after each sampled clip pose.");
            report.AppendLine("Camera target is fixed at the candidate root + 0.75 m; renderer bounds do not steer framing.");
            report.AppendLine("Baked snapshots retain the source bounds center at one-to-one scale; the numeric foot-weighted sole heights are authoritative for plane contact because the conservative skinned bounds are wider than the baked mesh.");

            var candidateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            if (candidateAsset == null)
            {
                throw new InvalidOperationException("Missing isolated candidate prefab: " + CandidatePath);
            }

            var samples = new[]
            {
                new ClipSample("BindPose", string.Empty, new[] { 0f }),
                new ClipSample("Idle", IdleJumpPath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Regular_Jump", IdleJumpPath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Walk", WalkPath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Run", RunPath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Joint_Idle", JointIdlePath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Joint_Regular_Jump", JointJumpPath, new[] { 0.2f, 0.5f, 0.8f }),
                new ClipSample("Joint_Land", JointLandPath, new[] { 0f, 0.25f, 0.55f, 1f }),
                new ClipSample("Joint_Walk", JointWalkPath, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }),
                new ClipSample("Joint_Run", JointRunPath, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }),
                new ClipSample("retarget_clip", LandingPath, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }),
            };

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(candidateAsset);
            // Match the sizing and rest-height overrides from the existing
            // face-seated candidate prefab. A bare FBX has a 2x unit mismatch.
            instance.transform.localPosition = new Vector3(-0.026434852f, 0.33827597f, 0.0059605613f);
            instance.transform.localScale = Vector3.one * 0.52879435f;
            var bindPose = instance.GetComponentsInChildren<Transform>(true).ToDictionary(
                transform => transform,
                transform => (Position: transform.localPosition, Rotation: transform.localRotation, Scale: transform.localScale));
            var previousActive = RenderTexture.active;
            var oldScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var created = new List<Object>();
            var renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            var stage = new GameObject("SplatCandidateReviewStage");
            var cameraObject = new GameObject("SplatCandidateReviewCamera");
            var lightObject = new GameObject("SplatCandidateReviewKeyLight");
            try
            {
                renderTexture.Create();
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.enabled = false;
                }

                var driver = instance.GetComponentInChildren<SpaceAndCrafts.Player.SplatMeshyAnimatorDriver>(true);
                if (driver != null)
                {
                    driver.enabled = false;
                }

                var support = GameObject.CreatePrimitive(PrimitiveType.Cube);
                support.name = "FlatSupportTopY0";
                support.transform.position = new Vector3(0f, -0.075f, 0f);
                support.transform.localScale = new Vector3(4f, 0.15f, 4f);
                support.transform.SetParent(stage.transform, true);
                SetMaterial(support.GetComponent<Renderer>(), new Color(0.34f, 0.39f, 0.45f, 1f), created);

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.73f, 0.79f, 0.86f, 1f);
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 50f;
                camera.targetTexture = renderTexture;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.94f, 0.83f, 1f);
                light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                foreach (var sample in samples)
                {
                    var clip = string.IsNullOrEmpty(sample.Path) ? null : FindClip(sample.Path, sample.ClipName);
                    if (clip != null) AppendTransformCurves(report, clip);
                    foreach (var fraction in sample.Fractions)
                    {
                        var time = clip == null ? 0f : clip.length * fraction;
                        // Sampling a sparse candidate clip (for example one
                        // which intentionally keeps the torso at bind pose)
                        // must not inherit the previous clip's torso or root
                        // pose. Reset every child transform before each sample.
                        foreach (var entry in bindPose)
                        {
                            entry.Key.localPosition = entry.Value.Position;
                            entry.Key.localRotation = entry.Value.Rotation;
                            entry.Key.localScale = entry.Value.Scale;
                        }
                        clip?.SampleAnimation(instance, time);
                        Physics.SyncTransforms();
                        var measured = Measure(instance);
                        if (sample.ClipName.StartsWith("Joint_", StringComparison.Ordinal))
                        {
                            var transforms = instance.GetComponentsInChildren<Transform>(true);
                            foreach (var boneName in new[] { "LeftUpLeg", "RightUpLeg", "LeftArm", "RightArm" })
                            {
                                var bone = transforms.FirstOrDefault(transform => transform.name == boneName);
                                if (bone != null)
                                    report.AppendLine($"pose {sample.ClipName}_{Mathf.RoundToInt(fraction * 100f):D3} {boneName}.localEuler={bone.localEulerAngles:F2}");
                            }
                        }
                        var sampleName = $"{sample.ClipName}_{Mathf.RoundToInt(fraction * 100f):D3}";
                        report.AppendLine(
                            $"{sampleName}: t={time:F3}s renderMinY={measured.RenderMinY:F4}m " +
                            $"renderCenter={measured.RenderBounds.center:F3} renderSize={measured.RenderBounds.size:F3} " +
                            $"footWeightedMinY={measured.FootMinY:F4}m footVertices={measured.FootVertexCount} " +
                            $"footBoneNames=[{string.Join(",", measured.FootBoneNames)}]");

                        var bakedRenderers = BakeSkinnedRenderersForPreview(instance);
                        try
                        {
                            var previewTarget = instance.transform.position + Vector3.up * 0.75f;
                            foreach (var view in new[]
                            {
                                new View("front", new Vector3(0.05f, 0.27f, 3.6f)),
                                new View("quarter", new Vector3(-2.4f, 0.32f, 3.2f)),
                            })
                            {
                                camera.transform.position = previewTarget + view.Position;
                                camera.transform.LookAt(previewTarget);
                                camera.Render();
                                RenderTexture.active = renderTexture;
                                image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                                image.Apply(false, false);
                                var outputPath = Path.Combine(outputDirectory, $"{sampleName}_{view.Name}.png");
                                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                            }
                        }
                        finally
                        {
                            foreach (var baked in bakedRenderers)
                            {
                                if (baked.Renderer != null) baked.Renderer.enabled = true;
                                if (baked.PreviewObject != null) Object.DestroyImmediate(baked.PreviewObject);
                                if (baked.Mesh != null) Object.DestroyImmediate(baked.Mesh);
                            }
                        }
                    }
                }

                File.WriteAllText(Path.Combine(outputDirectory, "review.txt"), report.ToString());
                Debug.Log("[Splat Meshy candidate review] Saved isolated flat-support frames and measurements to " + outputDirectory);
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (instance != null) Object.DestroyImmediate(instance);
                if (stage != null) Object.DestroyImmediate(stage);
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                if (lightObject != null) Object.DestroyImmediate(lightObject);
                foreach (var item in created)
                {
                    if (item != null) Object.DestroyImmediate(item);
                }
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (oldScene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(oldScene);
                }
            }

            EditorApplication.Exit(0);
        }

        private static AnimationClip FindClip(string path, string clipName)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(candidate => !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var clip = clips.FirstOrDefault(candidate =>
                string.Equals(candidate.name, clipName, StringComparison.OrdinalIgnoreCase));
            if (clip == null)
            {
                clip = clips.FirstOrDefault(candidate =>
                    candidate.name.IndexOf(clipName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return clip ?? throw new InvalidOperationException($"Missing clip {clipName} in {path}");
        }

        private static void AppendTransformCurves(StringBuilder report, AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.propertyName.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0
                    || binding.propertyName.IndexOf("Scale", StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0) continue;
                var minimum = curve.keys.Min(key => key.value);
                var maximum = curve.keys.Max(key => key.value);
                report.AppendLine(
                    $"curve {clip.name}/{binding.path}/{binding.propertyName}: " +
                    $"keys={curve.length} min={minimum:F4} max={maximum:F4}");
            }
        }

        private static ContactMeasure Measure(GameObject root)
        {
            var result = new ContactMeasure
            {
                RenderMinY = float.PositiveInfinity,
                FootMinY = float.PositiveInfinity,
                FootBoneNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase),
                RenderBounds = new Bounds(),
            };
            var hasBounds = false;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                result.RenderMinY = Mathf.Min(result.RenderMinY, renderer.bounds.min.y);
                if (!hasBounds)
                {
                    result.RenderBounds = renderer.bounds;
                    hasBounds = true;
                }
                else result.RenderBounds.Encapsulate(renderer.bounds);
                if (!(renderer is SkinnedMeshRenderer skinned) || skinned.sharedMesh == null)
                {
                    continue;
                }

                var mesh = skinned.sharedMesh;
                var weights = mesh.boneWeights;
                var bones = skinned.bones;
                if (weights == null || weights.Length == 0 || bones == null || bones.Length == 0)
                {
                    continue;
                }

                var footIndices = new HashSet<int>();
                for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    var bone = bones[boneIndex];
                    if (bone == null || !IsFootBone(bone.name)) continue;
                    footIndices.Add(boneIndex);
                    result.FootBoneNames.Add(bone.name);
                }
                if (footIndices.Count == 0) continue;

                var baked = new Mesh { name = "SplatCandidateContactBake" };
                try
                {
                    skinned.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var count = Mathf.Min(vertices.Length, weights.Length);
                    for (var vertexIndex = 0; vertexIndex < count; vertexIndex++)
                    {
                        var weight = weights[vertexIndex];
                        if (!HasFootWeight(weight, footIndices)) continue;
                        var world = skinned.transform.TransformPoint(vertices[vertexIndex]);
                        result.FootMinY = Mathf.Min(result.FootMinY, world.y);
                        result.FootVertexCount++;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(baked);
                }
            }

            if (float.IsPositiveInfinity(result.RenderMinY)) result.RenderMinY = float.NaN;
            if (float.IsPositiveInfinity(result.FootMinY)) result.FootMinY = float.NaN;
            return result;
        }

        private static List<BakedPreviewRenderer> BakeSkinnedRenderersForPreview(GameObject root)
        {
            // This imported rig has a ~0.005 local-to-world scale while the BakeMesh snapshot is
            // already character-sized. Recenter the temporary mesh at the review root for visual
            // pose comparison only; do not infer support contact from this normalized image.
            var bakedRenderers = new List<BakedPreviewRenderer>();
            foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned == null || skinned.sharedMesh == null) continue;
                var bakedMesh = new Mesh { name = "SplatCandidatePreviewBake" };
                skinned.BakeMesh(bakedMesh, false);
                var previewObject = new GameObject("PreviewBake_" + skinned.name);
                previewObject.transform.rotation = skinned.transform.rotation;
                previewObject.transform.position = skinned.bounds.center
                    - previewObject.transform.rotation * bakedMesh.bounds.center;
                previewObject.transform.localScale = Vector3.one;
                previewObject.AddComponent<MeshFilter>().sharedMesh = bakedMesh;
                var meshRenderer = previewObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = skinned.sharedMaterials;
                meshRenderer.shadowCastingMode = skinned.shadowCastingMode;
                meshRenderer.receiveShadows = skinned.receiveShadows;
                Debug.Log($"[Splat preview bake] {skinned.name} verts={bakedMesh.vertexCount} meshBounds={bakedMesh.bounds.center:F3}/{bakedMesh.bounds.size:F3} rendererBounds={meshRenderer.bounds.center:F3}/{meshRenderer.bounds.size:F3} sourceBounds={skinned.bounds.center:F3}/{skinned.bounds.size:F3} sourceWorld={skinned.transform.position:F3} sourceScale={skinned.transform.lossyScale:F5}");
                bakedRenderers.Add(new BakedPreviewRenderer(skinned, previewObject, bakedMesh));
                skinned.enabled = false;
            }
            return bakedRenderers;
        }

        private static bool IsFootBone(string name)
        {
            var lower = name.ToLowerInvariant();
            return lower.Contains("foot") || lower.Contains("toe") || lower.Contains("ankle");
        }

        private static bool HasFootWeight(BoneWeight weight, HashSet<int> footIndices)
        {
            return (weight.weight0 > 0.05f && footIndices.Contains(weight.boneIndex0))
                || (weight.weight1 > 0.05f && footIndices.Contains(weight.boneIndex1))
                || (weight.weight2 > 0.05f && footIndices.Contains(weight.boneIndex2))
                || (weight.weight3 > 0.05f && footIndices.Contains(weight.boneIndex3));
        }

        private static void SetMaterial(Renderer renderer, Color color, List<Object> created)
        {
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var material = new Material(shader) { name = "SplatReviewFlatSupport" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            created.Add(material);
            renderer.sharedMaterial = material;
        }

        private readonly struct ClipSample
        {
            public readonly string ClipName;
            public readonly string Path;
            public readonly float[] Fractions;

            public ClipSample(string clipName, string path, float[] fractions)
            {
                ClipName = clipName;
                Path = path;
                Fractions = fractions;
            }
        }

        private readonly struct View
        {
            public readonly string Name;
            public readonly Vector3 Position;

            public View(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }
        }

        private readonly struct BakedPreviewRenderer
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly GameObject PreviewObject;
            public readonly Mesh Mesh;

            public BakedPreviewRenderer(SkinnedMeshRenderer renderer, GameObject previewObject, Mesh mesh)
            {
                Renderer = renderer;
                PreviewObject = previewObject;
                Mesh = mesh;
            }
        }

        private struct ContactMeasure
        {
            public float RenderMinY;
            public Bounds RenderBounds;
            public float FootMinY;
            public int FootVertexCount;
            public SortedSet<string> FootBoneNames;
        }
    }
}
#endif
