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
    /// Samples the imported Meshy Splat clips on an isolated review prefab and
    /// writes matched stills/contact evidence. No production scene or prefab is saved.
    /// </summary>
    public static class SplatMeshyIter1791ActionPreviewCli
    {
        private const string CandidatePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/ReviewOnly/Splat_Meshy_Iter1791_ReviewOnly.prefab";
        private const string ActionPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1791/idle_jump.fbx";
        private const string WalkPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/walking_fbx.fbx";
        private const string RunPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/running_fbx.fbx";
        private const string LandingPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1791/landing_upright.fbx";
        private const string CatalogActionsPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1791/jump_lands_fbx.fbx";
        private const string PersonalityActionsPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1800/splat_idle_variations.fbx";
        private const string ActionSetPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1800/splat_action_set.fbx";
        private const string FistPumpPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1800/victory_fist_pump.fbx";
        private const string CuriosityIdlePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1810/splat_curiosity_idle.fbx";
        private const string AuthoredLandingPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyRig_Iter1791/ReviewOnly/Splat_Meshy_Iter1791_AuthoredSoftLand.anim";
        // Keep the tall contact sheet under Unity's maximum texture dimension
        // now that the three personality-gesture variants are sampled too.
        private const int Resolution = 480;
        private const int ContactSheetRows = 8;

        [MenuItem("Space & Crafts/Capture Iter1791 Splat Meshy Action Previews")]
        public static void Capture()
        {
            var outputDirectory = Path.GetFullPath("Logs/score-comparison/splat-meshy-actions-iter1811");
            Directory.CreateDirectory(outputDirectory);
            var report = new StringBuilder();
            report.AppendLine("Isolated static animation samples for the Iter1791 Splat Meshy review prefab plus Iter1800 Meshy idle variants.");
            report.AppendLine("These are editor-sampled still frames, not a gameplay test, human play, or proof of transition quality.");
            report.AppendLine("The sample pose uses the review controller's no-root-motion/no-scale-stretch intent: after each clip sample, every transform's localScale is restored to its bind value.");
            report.AppendLine("The flat support top is world Y=0; the review prefab's authored seating offset is preserved. Whole skinned-mesh minima come from BakeMesh; grounded stills are statically reseated, not varied-platform gameplay proof.");
            report.AppendLine("Contact sheet rows follow the order recorded below; left column is front, right column is three-quarter.");

            var candidateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            if (candidateAsset == null)
                throw new InvalidOperationException("Missing Iter1791 review-only prefab: " + CandidatePath);

            var samples = new[]
            {
                new ClipSample("Idle", ActionPath, "Idle", 0.50f),
                new ClipSample("Walk", WalkPath, "walk", 0.25f),
                new ClipSample("Walk", WalkPath, "walk", 0.75f),
                new ClipSample("Run", RunPath, "run", 0.25f),
                new ClipSample("Run", RunPath, "run", 0.75f),
                new ClipSample("Jump", ActionPath, "Regular_Jump", 0.35f),
                new ClipSample("Jump", ActionPath, "Regular_Jump", 0.75f),
                new ClipSample("RejectedLand", LandingPath, null, 0.20f),
                new ClipSample("RejectedLand", LandingPath, null, 0.60f),
                new ClipSample("RejectedLand", LandingPath, null, 0.90f),
                new ClipSample("AuthoredLand", AuthoredLandingPath, null, 0.22f),
                new ClipSample("AuthoredLand", AuthoredLandingPath, null, 0.50f),
                new ClipSample("AuthoredLand", AuthoredLandingPath, null, 0.90f),
                new ClipSample("VaultLand", CatalogActionsPath, "Vault", 0.25f),
                new ClipSample("VaultLand", CatalogActionsPath, "Vault", 0.70f),
                new ClipSample("JumpDown", CatalogActionsPath, "Jumping", 0.25f),
                new ClipSample("JumpDown", CatalogActionsPath, "Jumping", 0.70f),
                new ClipSample("IdleVariant247", PersonalityActionsPath, "Idle_7", 0.22f),
                new ClipSample("IdleVariant247", PersonalityActionsPath, "Idle_7", 0.52f),
                new ClipSample("IdleVariant247", PersonalityActionsPath, "Idle_7", 0.82f),
                new ClipSample("IdleVariant250", PersonalityActionsPath, "Idle_10", 0.22f),
                new ClipSample("IdleVariant250", PersonalityActionsPath, "Idle_10", 0.52f),
                new ClipSample("IdleVariant250", PersonalityActionsPath, "Idle_10", 0.82f),
                new ClipSample("IdleVariant599", PersonalityActionsPath, "Idle_15", 0.22f),
                new ClipSample("IdleVariant599", PersonalityActionsPath, "Idle_15", 0.52f),
                new ClipSample("IdleVariant599", PersonalityActionsPath, "Idle_15", 0.82f),
                new ClipSample("PunchCandidate", ActionSetPath, "Punch_Forward", 0.28f),
                new ClipSample("PunchCandidate", ActionSetPath, "Punch_Forward", 0.58f),
                new ClipSample("PunchCandidate", ActionSetPath, "Punch_Forward", 0.88f),
                new ClipSample("HitCandidate", ActionSetPath, "Hit_Reaction", 0.28f),
                new ClipSample("HitCandidate", ActionSetPath, "Hit_Reaction", 0.58f),
                new ClipSample("HitCandidate", ActionSetPath, "Hit_Reaction", 0.88f),
                new ClipSample("VictoryCandidate", ActionSetPath, "Victory_Cheer", 0.28f),
                new ClipSample("VictoryCandidate", ActionSetPath, "Victory_Cheer", 0.58f),
                new ClipSample("VictoryCandidate", ActionSetPath, "Victory_Cheer", 0.88f),
                new ClipSample("FistPumpCandidate", FistPumpPath, "Victory_Fist_Pump", 0.22f),
                new ClipSample("FistPumpCandidate", FistPumpPath, "Victory_Fist_Pump", 0.52f),
                new ClipSample("FistPumpCandidate", FistPumpPath, "Victory_Fist_Pump", 0.82f),
                new ClipSample("CuriosityIdleCandidate", CuriosityIdlePath, null, 0.20f),
                new ClipSample("CuriosityIdleCandidate", CuriosityIdlePath, null, 0.50f),
                new ClipSample("CuriosityIdleCandidate", CuriosityIdlePath, null, 0.80f),
            };

            var instance = PrefabUtility.InstantiatePrefab(candidateAsset) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate Iter1791 review-only prefab.");

            var originalActive = RenderTexture.active;
            var originalAmbientMode = RenderSettings.ambientMode;
            var originalAmbientLight = RenderSettings.ambientLight;
            var originalAmbientIntensity = RenderSettings.ambientIntensity;
            var renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGB24, false);
            var sheetCount = (samples.Length + ContactSheetRows - 1) / ContactSheetRows;
            var contactSheets = new Texture2D[sheetCount];
            for (var sheet = 0; sheet < sheetCount; sheet++)
            {
                var rows = Mathf.Min(ContactSheetRows, samples.Length - sheet * ContactSheetRows);
                contactSheets[sheet] = new Texture2D(Resolution * 2, Resolution * rows, TextureFormat.RGB24, false);
            }
            var cameraObject = new GameObject("Iter1791 Splat Action Review Camera");
            var lightObject = new GameObject("Iter1791 Splat Action Review Key Light");
            var support = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var createdMaterials = new List<Material>();
            try
            {
                renderTexture.Create();
                // Keep the review prefab's authored seat offset. Resetting its
                // world position here discards the saved foot-to-floor fit and
                // made the earlier contact report misleading.
                instance.transform.rotation = Quaternion.identity;
                var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
                if (animator != null)
                    animator.enabled = false;
                var driver = instance.GetComponent<SpaceAndCrafts.Player.SplatMeshyAnimatorDriver>() ??
                             instance.GetComponentInChildren<SpaceAndCrafts.Player.SplatMeshyAnimatorDriver>(true);
                if (driver != null)
                    driver.enabled = false;

                var skinnedForRigReport = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
                if (skinnedForRigReport != null)
                {
                    report.AppendLine("skin bones=" + string.Join(", ", skinnedForRigReport.bones
                        .Where(bone => bone != null).Select(bone => bone.name)));
                }

                var bindPose = instance.GetComponentsInChildren<Transform>(true).ToDictionary(
                    transform => transform,
                    transform => (Position: transform.localPosition,
                                  Rotation: transform.localRotation,
                                  Scale: transform.localScale));

                support.name = "Temporary Review Support (Top Y=0)";
                support.transform.position = new Vector3(0f, -0.075f, 0f);
                support.transform.localScale = new Vector3(4f, 0.15f, 4f);
                SetMaterial(support.GetComponent<Renderer>(), new Color(0.34f, 0.39f, 0.45f, 1f), createdMaterials);

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.73f, 0.79f, 0.86f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 0.82f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 50f;
                camera.targetTexture = renderTexture;

                var key = lightObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.15f;
                key.color = new Color(1f, 0.94f, 0.84f, 1f);
                lightObject.transform.rotation = Quaternion.Euler(36f, -28f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.48f, 0.51f, 0.58f, 1f);
                RenderSettings.ambientIntensity = 1f;

                for (var row = 0; row < samples.Length; row++)
                {
                    var sample = samples[row];
                    var clip = sample.ClipName == null
                        ? FindOnlyClip(sample.Path)
                        : FindClip(sample.Path, sample.ClipName);
                    var time = Mathf.Clamp01(sample.Fraction) * clip.length;

                    foreach (var pose in bindPose)
                    {
                        pose.Key.localPosition = pose.Value.Position;
                        pose.Key.localRotation = pose.Value.Rotation;
                        pose.Key.localScale = pose.Value.Scale;
                    }

                    clip.SampleAnimation(instance, time);

                    // Mirrors SplatMeshyAnimatorDriver.LateUpdate: preserve each
                    // imported bind scale to prevent animation scale keys from
                    // stretching the one-shell body or changing the cap/feet.
                    foreach (var pose in bindPose)
                        pose.Key.localScale = pose.Value.Scale;

                    var preContactMeasure = Measure(instance);
                    var groundCorrection = 0f;
                    if (sample.Label != "RejectedLand" && sample.Label != "Jump"
                        && !float.IsNaN(preContactMeasure.MeshMinY))
                    {
                        // Use the fresh baked pose for these offline stills. In
                        // Play Mode the driver reads current SkinnedMeshRenderer
                        // bounds after Animator evaluation.
                        groundCorrection = Mathf.Clamp(-preContactMeasure.MeshMinY, -0.12f, 0.12f);
                        instance.transform.position += Vector3.up * groundCorrection;
                    }

                    Physics.SyncTransforms();
                    var measure = Measure(instance);
                    var delta = MeasurePoseDelta(bindPose);
                    var rowLabel = $"row{row:00} {sample.Label} clip={clip.name} fraction={sample.Fraction:F2} time={time:F3}s";
                    report.AppendLine(rowLabel +
                        $" wholeMeshMinY={measure.MeshMinY:F4}m " +
                        $"footInfluencedMinY={measure.FootMinY:F4}m footInfluencedVertices={measure.FootVertexCount} " +
                        $"groundRootCorrection={groundCorrection:F4}m " +
                        $"maxBoneRotationDelta={delta.MaxRotation:F1}deg maxBonePositionDelta={delta.MaxPosition:F4}m " +
                        $"animScaleCurves={CountScaleCurves(clip)}");

                    var target = Vector3.up * 0.575f;
                    var views = new[]
                    {
                        new View("front", new Vector3(0.06f, 0.07f, 4.0f)),
                        new View("threequarter", new Vector3(-2.65f, 0.16f, 3.4f)),
                    };
                    for (var column = 0; column < views.Length; column++)
                    {
                        camera.transform.position = target + views[column].Offset;
                        camera.transform.LookAt(target);
                        camera.Render();
                        RenderTexture.active = renderTexture;
                        image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                        image.Apply(false, false);
                        var filename = $"{sample.Label.ToLowerInvariant()}_{Mathf.RoundToInt(sample.Fraction * 100f):D3}_{views[column].Name}.png";
                        File.WriteAllBytes(Path.Combine(outputDirectory, filename), image.EncodeToPNG());
                        var sheetIndex = row / ContactSheetRows;
                        var sheetRowCount = Mathf.Min(ContactSheetRows, samples.Length - sheetIndex * ContactSheetRows);
                        var sheetRow = row % ContactSheetRows;
                        contactSheets[sheetIndex].SetPixels(column * Resolution, (sheetRowCount - sheetRow - 1) * Resolution,
                            Resolution, Resolution, image.GetPixels());
                    }
                }

                for (var sheet = 0; sheet < contactSheets.Length; sheet++)
                {
                    contactSheets[sheet].Apply(false, false);
                    var filename = $"contact-sheet-{sheet + 1:D2}.png";
                    File.WriteAllBytes(Path.Combine(outputDirectory, filename), contactSheets[sheet].EncodeToPNG());
                    report.AppendLine("contact-sheet=" + filename + "; each row is one sample, front left / three-quarter right.");
                }
                report.AppendLine("source prefab=" + CandidatePath);
                report.AppendLine("source FBXs=" + string.Join("; ", new[] { ActionPath, WalkPath, RunPath,
                    LandingPath, PersonalityActionsPath, ActionSetPath, FistPumpPath, CuriosityIdlePath }));
                File.WriteAllText(Path.Combine(outputDirectory, "review.txt"), report.ToString());
                Debug.Log("[Splat Meshy Iter1791 preview] Saved sampled clip frames to " + outputDirectory);
            }
            finally
            {
                RenderTexture.active = originalActive;
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;
                RenderSettings.ambientIntensity = originalAmbientIntensity;
                foreach (var material in createdMaterials)
                    if (material != null) Object.DestroyImmediate(material);
                if (support != null) Object.DestroyImmediate(support);
                if (instance != null) Object.DestroyImmediate(instance);
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                if (lightObject != null) Object.DestroyImmediate(lightObject);
                if (image != null) Object.DestroyImmediate(image);
                foreach (var contactSheet in contactSheets)
                    if (contactSheet != null) Object.DestroyImmediate(contactSheet);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
            }
        }

        private static AnimationClip FindClip(string path, string query)
        {
            var clips = LoadClips(path);
            var clip = clips.FirstOrDefault(candidate =>
                string.Equals(candidate.name, query, StringComparison.OrdinalIgnoreCase));
            if (clip == null)
                clip = clips.FirstOrDefault(candidate =>
                    candidate.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            return clip ?? throw new InvalidOperationException(
                $"Could not find '{query}' in {path}. Available clips: {string.Join(", ", clips.Select(candidate => candidate.name))}");
        }

        private static AnimationClip FindOnlyClip(string path)
        {
            var clips = LoadClips(path);
            return clips.Length == 1
                ? clips[0]
                : throw new InvalidOperationException(
                    $"Expected one clip in {path}, found {clips.Length}: {string.Join(", ", clips.Select(candidate => candidate.name))}");
        }

        private static AnimationClip[] LoadClips(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

        private static int CountScaleCurves(AnimationClip clip) => AnimationUtility.GetCurveBindings(clip)
            .Count(binding => binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0);

        private static ContactMeasure Measure(GameObject root)
        {
            var minimum = float.PositiveInfinity;
            var footMinimum = float.PositiveInfinity;
            var footVertices = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!(renderer is SkinnedMeshRenderer skinned) || skinned.sharedMesh == null)
                {
                    minimum = Mathf.Min(minimum, renderer.bounds.min.y);
                    continue;
                }

                var mesh = skinned.sharedMesh;
                var weights = mesh.boneWeights;
                var bones = skinned.bones;
                if (weights == null || bones == null || weights.Length == 0 || bones.Length == 0)
                    continue;
                var footIndices = new HashSet<int>();
                for (var i = 0; i < bones.Length; i++)
                {
                    var name = bones[i] != null ? bones[i].name : string.Empty;
                    if (name.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("ankle", StringComparison.OrdinalIgnoreCase) >= 0)
                        footIndices.Add(i);
                }
                if (footIndices.Count == 0) continue;

                var baked = new Mesh { name = "Iter1791 temporary contact sample" };
                try
                {
                    skinned.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var count = Mathf.Min(vertices.Length, weights.Length);
                    for (var i = 0; i < count; i++)
                    {
                        var worldVertex = skinned.transform.TransformPoint(vertices[i]);
                        minimum = Mathf.Min(minimum, worldVertex.y);
                        var weight = weights[i];
                        if (FootInfluence(weight, footIndices) < 0.5f) continue;
                        footMinimum = Mathf.Min(footMinimum, worldVertex.y);
                        footVertices++;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(baked);
                }
            }

            return new ContactMeasure(
                float.IsPositiveInfinity(minimum) ? float.NaN : minimum,
                float.IsPositiveInfinity(footMinimum) ? float.NaN : footMinimum,
                footVertices);
        }

        private static PoseDeltaMeasure MeasurePoseDelta(
            IReadOnlyDictionary<Transform, (Vector3 Position, Quaternion Rotation, Vector3 Scale)> bindPose)
        {
            var maxRotation = 0f;
            var maxPosition = 0f;
            foreach (var pose in bindPose)
            {
                if (pose.Key == null) continue;
                maxRotation = Mathf.Max(maxRotation, Quaternion.Angle(pose.Value.Rotation, pose.Key.localRotation));
                maxPosition = Mathf.Max(maxPosition, Vector3.Distance(pose.Value.Position, pose.Key.localPosition));
            }
            return new PoseDeltaMeasure(maxRotation, maxPosition);
        }

        private static float FootInfluence(BoneWeight weight, HashSet<int> footIndices)
        {
            var influence = 0f;
            if (footIndices.Contains(weight.boneIndex0)) influence += weight.weight0;
            if (footIndices.Contains(weight.boneIndex1)) influence += weight.weight1;
            if (footIndices.Contains(weight.boneIndex2)) influence += weight.weight2;
            if (footIndices.Contains(weight.boneIndex3)) influence += weight.weight3;
            return influence;
        }

        private static void SetMaterial(Renderer renderer, Color color, List<Material> created)
        {
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var material = new Material(shader) { name = "Iter1791 Splat Review Support" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            created.Add(material);
            renderer.sharedMaterial = material;
        }

        private readonly struct ClipSample
        {
            public readonly string Label;
            public readonly string Path;
            public readonly string ClipName;
            public readonly float Fraction;

            public ClipSample(string label, string path, string clipName, float fraction)
            {
                Label = label;
                Path = path;
                ClipName = clipName;
                Fraction = fraction;
            }
        }

        private readonly struct View
        {
            public readonly string Name;
            public readonly Vector3 Offset;
            public View(string name, Vector3 offset) { Name = name; Offset = offset; }
        }

        private readonly struct ContactMeasure
        {
            public readonly float MeshMinY;
            public readonly float FootMinY;
            public readonly int FootVertexCount;
            public ContactMeasure(float meshMinY, float footMinY, int footVertexCount)
            { MeshMinY = meshMinY; FootMinY = footMinY; FootVertexCount = footVertexCount; }
        }

        private readonly struct PoseDeltaMeasure
        {
            public readonly float MaxRotation;
            public readonly float MaxPosition;
            public PoseDeltaMeasure(float maxRotation, float maxPosition)
            {
                MaxRotation = maxRotation;
                MaxPosition = maxPosition;
            }
        }
    }
}
#endif
