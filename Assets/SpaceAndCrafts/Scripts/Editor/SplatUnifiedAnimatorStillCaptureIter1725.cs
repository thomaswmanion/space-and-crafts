#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Renders isolated Animator state samples from the Iter1725 model-root review prefab.
    /// This does not create or modify a gameplay scene or production asset.
    /// </summary>
    public static class SplatUnifiedAnimatorStillCaptureIter1725
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1725/Splat_Iter1725_ModelRoot_ReviewOnly.prefab";
        private const string OutputPath = "Logs/score-comparison/splat-unified-animator-stills-iter1725";
        private const string VertexColorShaderPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/SplatCandidateVertexColorPreview.shader";
        private const int Resolution = 640;
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");

        [MenuItem("Space & Crafts/Capture Isolated Splat Iter1725 Animator Stills")]
        public static void Capture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing Iter1725 Splat review prefab: " + PrefabPath);

            Directory.CreateDirectory(OutputPath);
            var report = new StringBuilder();
            report.AppendLine("Isolated review prefab; direct Animator state samples only.");
            report.AppendLine("No SplatMotor, live gameplay, transition event, game scene, build, or human play is involved.");
            report.AppendLine("Animator culling is forced to AlwaysAnimate so the headless off-camera sampling cannot suppress bone updates.");
            report.AppendLine("The unified candidate body uses the existing Iter1705 vertex-color preview shader in this capture only; imported material assignment does not display its authored limb palette reliably.");
            var previousActiveScene = SceneManager.GetActiveScene();
            GameObject instance = null;
            GameObject ground = null;
            GameObject keyObject = null;
            GameObject fillObject = null;
            GameObject cameraObject = null;
            Material groundMaterial = null;
            Material vertexColorMaterial = null;
            RenderTexture renderTexture = null;
            Texture2D image = null;
            var previousActive = RenderTexture.active;

            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate the Iter1722 review prefab.");
                instance.hideFlags = HideFlags.HideAndDontSave;
                var animator = instance.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("Iter1725 prefab is missing a valid Animator, Avatar, or controller.");
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var vertexColorShader = AssetDatabase.LoadAssetAtPath<Shader>(VertexColorShaderPath);
                if (vertexColorShader == null)
                    throw new InvalidOperationException("Missing existing review vertex-color shader: " + VertexColorShaderPath);
                vertexColorMaterial = new Material(vertexColorShader)
                {
                    name = "Temporary Splat Iter1725 vertex-color preview",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                foreach (var skinnedRenderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinnedRenderer.updateWhenOffscreen = true;
                    if (skinnedRenderer.name == "Splat_UnifiedBodyAndLimbs")
                        skinnedRenderer.sharedMaterial = vertexColorMaterial;
                }

                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.hideFlags = HideFlags.HideAndDontSave;
                ground.name = "Temporary review support (not an asset)";
                ground.transform.position = new Vector3(0f, -0.002f, 0f);
                ground.transform.localScale = Vector3.one * 3f;
                var groundRenderer = ground.GetComponent<Renderer>();
                groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                groundMaterial.color = new Color(0.16f, 0.23f, 0.28f, 1f);
                groundRenderer.sharedMaterial = groundMaterial;

                keyObject = new GameObject("Temporary Splat review key light");
                keyObject.hideFlags = HideFlags.HideAndDontSave;
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.25f;
                key.color = new Color(1f, 0.94f, 0.88f);
                keyObject.transform.rotation = Quaternion.Euler(34f, -24f, 0f);

                fillObject = new GameObject("Temporary Splat review fill light");
                fillObject.hideFlags = HideFlags.HideAndDontSave;
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.45f;
                fill.color = new Color(0.73f, 0.83f, 1f);
                fillObject.transform.rotation = Quaternion.Euler(18f, 155f, 0f);

                cameraObject = new GameObject("Temporary Splat review camera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1.45f;
                camera.aspect = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.055f, 0.085f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.cullingMask = ~0;
                renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);

                var views = new[]
                {
                    (Name: "front", X: 0.05f, Z: 4.4f),
                    (Name: "back", X: 0.05f, Z: -4.4f),
                    (Name: "side", X: 4.4f, Z: 0.05f),
                };
                var poses = new[]
                {
                    (Name: "idle", State: "Locomotion", Speed: 0f, Time: 0.25f),
                    (Name: "run", State: "Locomotion", Speed: 2f, Time: 0.25f),
                    (Name: "jump", State: "Jump", Speed: 0f, Time: 0.52f),
                    (Name: "land", State: "Land", Speed: 0f, Time: 0.25f),
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
                    Physics.SyncTransforms();
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    var clipSummary = clipInfo.Length > 0 && clipInfo[0].clip != null
                        ? SummarizeClipCurves(clipInfo[0].clip)
                        : "no active clip";
                    var sampledBones = SummarizeSampledBones(instance.transform);
                    var rendererSummary = SummarizeRenderers(instance.transform);
                    var skinWeights = SummarizeMainSkinWeights(instance.transform);
                    var visibleRenderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (visibleRenderers.Length == 0)
                        throw new InvalidOperationException("Iter1722 prefab has no renderers to capture.");
                    var renderBounds = visibleRenderers[0].bounds;
                    for (var i = 1; i < visibleRenderers.Length; i++)
                        renderBounds.Encapsulate(visibleRenderers[i].bounds);
                    var target = renderBounds.center;
                    target.y = Mathf.Max(0.85f, target.y);
                    camera.orthographicSize = Mathf.Max(1.25f, renderBounds.extents.y * 1.4f);
                    cameraObject.transform.position = target + new Vector3(0.05f, 0.60f, 4.4f);
                    cameraObject.transform.LookAt(target);
                    report.AppendLine($"pose={pose.Name}; requestedState={pose.State}; normalizedTime={pose.Time:F2}; " +
                        $"evaluatedHash={stateInfo.shortNameHash}; evaluatedTime={stateInfo.normalizedTime:F3}; " +
                        $"clips=[{string.Join(",", Array.ConvertAll(clipInfo, info => info.clip != null ? $"{info.clip.name}:{info.weight:F2}" : "null"))}]; " +
                        $"curveAudit=[{clipSummary}]; " +
                        $"sampledBones=[{sampledBones}]; " +
                        $"renderers=[{rendererSummary}]; " +
                        $"skinWeights=[{skinWeights}]; " +
                        $"avatarValid={animator.avatar.isValid}; " +
                        $"rendererBounds={renderBounds}; root={instance.name}");

                    foreach (var view in views)
                    {
                        cameraObject.transform.position = target + new Vector3(view.X, 0.60f, view.Z);
                        cameraObject.transform.LookAt(target);
                        camera.Render();
                        RenderTexture.active = renderTexture;
                        image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                        image.Apply(false, false);
                        var file = Path.Combine(OutputPath, pose.Name + "-" + view.Name + ".png");
                        File.WriteAllBytes(file, image.EncodeToPNG());
                    }
                }

                File.WriteAllText(Path.Combine(OutputPath, "review.txt"), report.ToString());
                Debug.Log("[Splat Iter1725] Saved isolated Animator stills: " + OutputPath);
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
                if (vertexColorMaterial != null) Object.DestroyImmediate(vertexColorMaterial);
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
                // The captured scene is the pre-existing active scene so that
                // Camera.Render sees its objects. All temporary roots use
                // HideAndDontSave and are destroyed above; never save the scene.
            }
        }

        private static string SummarizeClipCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var animatedCurves = 0;
            var maxRange = 0f;
            var maxPath = "";
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.keys.Length < 2)
                    continue;
                var min = float.PositiveInfinity;
                var max = float.NegativeInfinity;
                foreach (var key in curve.keys)
                {
                    min = Mathf.Min(min, key.value);
                    max = Mathf.Max(max, key.value);
                }
                var range = max - min;
                if (range > 0.00001f)
                {
                    animatedCurves++;
                    if (range > maxRange)
                    {
                        maxRange = range;
                        maxPath = binding.path + "/" + binding.propertyName;
                    }
                }
            }
            return $"{clip.name};length={clip.length:F3}s;bindings={bindings.Length};varyingCurves={animatedCurves};maxKeyRange={maxRange:F5};largest={maxPath}";
        }

        private static string SummarizeSampledBones(Transform root)
        {
            var paths = new[]
            {
                "Splat_UnifiedRig/Root/Hips/Body",
                "Splat_UnifiedRig/Root/Hips/Leg_L",
                "Splat_UnifiedRig/Root/Hips/Leg_R",
                "Splat_UnifiedRig/Root/Hips/Body/Arm_L",
                "Splat_UnifiedRig/Root/Hips/Body/Arm_R",
            };
            var values = new string[paths.Length];
            for (var i = 0; i < paths.Length; i++)
            {
                var bone = root.Find(paths[i]);
                values[i] = bone == null
                    ? paths[i] + "=missing"
                    : paths[i].Substring(paths[i].LastIndexOf('/') + 1) + "=euler:" + bone.localEulerAngles.ToString("F2") + ",pos:" + bone.localPosition.ToString("F3");
            }
            return string.Join("|", values);
        }

        private static string SummarizeRenderers(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var values = new string[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SkinnedMeshRenderer skinned)
                {
                    values[i] = $"{skinned.name}:Skinned(bones={skinned.bones.Length},updateOffscreen={skinned.updateWhenOffscreen})";
                }
                else
                {
                    values[i] = renderers[i].name + ":" + renderers[i].GetType().Name;
                }
            }
            return string.Join("|", values);
        }

        private static string SummarizeMainSkinWeights(Transform root)
        {
            var renderer = Array.Find(root.GetComponentsInChildren<SkinnedMeshRenderer>(true), r => r.name == "Splat_UnifiedBodyAndLimbs");
            if (renderer == null || renderer.sharedMesh == null)
                return "main skinned body missing";
            var bones = renderer.bones;
            var totals = new double[bones.Length];
            foreach (var weight in renderer.sharedMesh.boneWeights)
            {
                if (weight.boneIndex0 >= 0 && weight.boneIndex0 < totals.Length) totals[weight.boneIndex0] += weight.weight0;
                if (weight.boneIndex1 >= 0 && weight.boneIndex1 < totals.Length) totals[weight.boneIndex1] += weight.weight1;
                if (weight.boneIndex2 >= 0 && weight.boneIndex2 < totals.Length) totals[weight.boneIndex2] += weight.weight2;
                if (weight.boneIndex3 >= 0 && weight.boneIndex3 < totals.Length) totals[weight.boneIndex3] += weight.weight3;
            }
            var result = new string[bones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                result[i] = $"{bone?.name ?? "null"}:{(bones.Length > 0 && renderer.sharedMesh.vertexCount > 0 ? totals[i] / renderer.sharedMesh.vertexCount : 0d):F3}";
            }
            return string.Join(",", result);
        }
    }
}
#endif
