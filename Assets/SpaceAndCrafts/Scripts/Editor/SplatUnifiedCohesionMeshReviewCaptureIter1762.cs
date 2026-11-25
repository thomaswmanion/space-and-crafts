#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Captures isolated review-only Animator states from Iter1762. This creates
    /// temporary stage objects only and does not open or save a gameplay scene.
    /// </summary>
    public static class SplatUnifiedCohesionMeshReviewCaptureIter1762
    {
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1762/Splat_Iter1762_CohesionMesh_ReviewOnly.prefab";
        private const string OutputPath = "Logs/score-comparison/splat-unified-cohesion-mesh-review-iter1762";
        private const int Resolution = 640;
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");

        [MenuItem("Space & Crafts/Capture Isolated Splat Iter1762 Design Review")]
        public static void Capture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing Iter1762 ReviewOnly prefab: " + PrefabPath);
            Directory.CreateDirectory(OutputPath);
            var report = new StringBuilder();
            report.AppendLine("Iter1762 ReviewOnly prefab; direct Animator state samples only.");
            report.AppendLine("No SplatMotor, live gameplay, transition event, production scene, build, test, or human play is involved.");
            report.AppendLine("After Animator sampling, transform scales are restored to this variant's authored bind proportions, matching SplatMeshyAnimatorDriver's scale guard.");

            GameObject instance = null;
            GameObject ground = null;
            GameObject keyObject = null;
            GameObject fillObject = null;
            GameObject cameraObject = null;
            Material groundMaterial = null;
            RenderTexture renderTexture = null;
            Texture2D image = null;
            var previousActive = RenderTexture.active;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Iter1762 ReviewOnly prefab.");
                instance.hideFlags = HideFlags.HideAndDontSave;
                var animator = instance.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("Iter1762 prefab is missing a valid Animator, Avatar, or controller.");
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    renderer.updateWhenOffscreen = true;

                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var bindScales = transforms.Select(t => t.localScale).ToArray();
                var handL = FindUnique(instance.transform, "Hand_L");
                var handR = FindUnique(instance.transform, "Hand_R");
                var footL = FindUnique(instance.transform, "Foot_L");
                var footR = FindUnique(instance.transform, "Foot_R");
                report.AppendLine($"bindScales: Hand_L={handL.localScale:F3}; Hand_R={handR.localScale:F3}; Foot_L={footL.localScale:F3}; Foot_R={footR.localScale:F3}");

                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.hideFlags = HideFlags.HideAndDontSave;
                ground.name = "Temporary Iter1762 review support";
                ground.transform.position = new Vector3(0f, -0.002f, 0f);
                ground.transform.localScale = Vector3.one * 3f;
                groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(0.16f, 0.23f, 0.28f, 1f),
                };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                keyObject = new GameObject("Temporary Iter1762 review key light") { hideFlags = HideFlags.HideAndDontSave };
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.25f;
                key.color = new Color(1f, 0.94f, 0.88f);
                keyObject.transform.rotation = Quaternion.Euler(34f, -24f, 0f);

                fillObject = new GameObject("Temporary Iter1762 review fill light") { hideFlags = HideFlags.HideAndDontSave };
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.45f;
                fill.color = new Color(0.73f, 0.83f, 1f);
                fillObject.transform.rotation = Quaternion.Euler(18f, 155f, 0f);

                cameraObject = new GameObject("Temporary Iter1762 review camera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.aspect = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.055f, 0.085f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);

                var poses = new[]
                {
                    (Name: "idle", State: "Locomotion", Speed: 0f, Time: 0.25f, Views: new[] { "front", "side" }),
                    (Name: "land", State: "Land", Speed: 0f, Time: 0.25f, Views: new[] { "front", "side" }),
                    (Name: "run", State: "Locomotion", Speed: 2f, Time: 0.25f, Views: new[] { "side" }),
                    (Name: "jump", State: "Jump", Speed: 0f, Time: 0.52f, Views: new[] { "side" }),
                };
                foreach (var pose in poses)
                {
                    animator.Rebind();
                    animator.speed = 1f;
                    animator.ResetTrigger("Jump");
                    animator.ResetTrigger("Land");
                    animator.SetFloat(MoveSpeedId, pose.Speed);
                    animator.Play(pose.State, 0, pose.Time);
                    animator.Update(1f / 60f);
                    animator.speed = 0f;
                    for (var i = 0; i < transforms.Length; i++)
                        if (transforms[i] != null) transforms[i].localScale = bindScales[i];
                    Physics.SyncTransforms();

                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    var bounds = skinned.Length > 0 ? skinned[0].bounds : new Bounds(Vector3.zero, Vector3.one);
                    for (var i = 1; i < skinned.Length; i++) bounds.Encapsulate(skinned[i].bounds);
                    var target = bounds.center;
                    target.y = Mathf.Max(0.85f, target.y);
                    camera.orthographicSize = Mathf.Max(1.25f, bounds.extents.y * 1.4f);

                    var bottom = MeasureLowestSkinnedPoint(skinned);
                    report.AppendLine($"pose={pose.Name}; stateHash={state.shortNameHash}; normalizedTime={state.normalizedTime:F3}; " +
                        $"clips=[{string.Join(",", Array.ConvertAll(clipInfo, info => info.clip != null ? info.clip.name : "null"))}]; " +
                        $"lowestSkinnedPointY={bottom:F4}; supportY=-0.002; gap={bottom + 0.002f:F4}; bounds={bounds}");

                    foreach (var view in pose.Views)
                    {
                        var offset = view == "front" ? new Vector3(0.05f, 0.60f, 4.4f) : new Vector3(4.4f, 0.60f, 0.05f);
                        cameraObject.transform.position = target + offset;
                        cameraObject.transform.LookAt(target);
                        camera.Render();
                        RenderTexture.active = renderTexture;
                        image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                        image.Apply(false, false);
                        var file = Path.Combine(OutputPath, pose.Name + "-" + view + ".png");
                        File.WriteAllBytes(file, image.EncodeToPNG());
                    }
                }

                File.WriteAllText(Path.Combine(OutputPath, "review.txt"), report.ToString());
                Debug.Log("[Splat Iter1762] Saved isolated ReviewOnly proportions/palette stills: " + OutputPath);
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (instance != null) Object.DestroyImmediate(instance);
                if (ground != null) Object.DestroyImmediate(ground);
                if (keyObject != null) Object.DestroyImmediate(keyObject);
                if (fillObject != null) Object.DestroyImmediate(fillObject);
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
                EditorApplication.Exit(0);
            }
        }

        private static Transform FindUnique(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one {name} bone, found {matches.Length}.");
            return matches[0];
        }

        private static float MeasureLowestSkinnedPoint(SkinnedMeshRenderer[] renderers)
        {
            var lowest = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;
                var baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    renderer.BakeMesh(baked);
                    var vertices = baked.vertices;
                    for (var i = 0; i < vertices.Length; i++)
                        lowest = Mathf.Min(lowest, renderer.transform.TransformPoint(vertices[i]).y);
                }
                finally { Object.DestroyImmediate(baked); }
            }
            return float.IsPositiveInfinity(lowest) ? float.NaN : lowest;
        }
    }
}
#endif
