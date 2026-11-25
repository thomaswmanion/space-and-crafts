#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Compares Iter1711 baked normals and lighting response without entering Play Mode.</summary>
    public static class SplatIter1711ShadingDiagnosticCli
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.fbx";
        private const string OutputDirectory = "Logs/splat-iter1711-shading-diagnostic";
        private const int Resolution = 900;
        private static Camera _camera;
        private static Light _key;
        private static RenderTexture _target;

        [MenuItem("Space & Crafts/Diagnose Iter1711 Pose Shading (No Play Mode)")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutputDirectory);
            var report = new StringBuilder();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Missing Iter1711 source FBX.");
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            var idle = clips.First(c => c.name.EndsWith("Splat_Idle", StringComparison.OrdinalIgnoreCase));
            var land = clips.First(c => c.name.EndsWith("Splat_Land", StringComparison.OrdinalIgnoreCase));
            report.AppendLine("scope=isolated direct AnimationClip sampling and lighting diagnosis; no Play Mode, no source or production asset mutation");
            report.AppendLine($"importerNormals={importer?.importNormals} importerTangents={importer?.importTangents} animationType={importer?.animationType}");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null) throw new InvalidOperationException("Could not instantiate source FBX.");
            visual.name = "Iter1711 shading diagnostic clone";
            var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            var bindPose = visual.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));

            SetupEnvironment();
            report.AppendLine($"cameraWorld={_camera.transform.position:F3}; cameraForward={_camera.transform.forward:F3}; keySourceDirection={(-_key.transform.forward):F3}; sourceToCameraDot={Vector3.Dot(-_key.transform.forward, _camera.transform.forward * -1f):F3}");
            foreach (var (clip, binding) in new[] { idle, land }
                .SelectMany(clip => AnimationUtility.GetCurveBindings(clip).Select(binding => (clip, binding)))
                .Where(item => item.binding.propertyName.StartsWith("m_Local", StringComparison.Ordinal)))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null) continue;
                var values = string.Join(",", curve.keys.Select(k => k.value.ToString("F3")));
                report.AppendLine($"transformCurve clip={clip.name} path={binding.path} property={binding.propertyName} keys={values}");
            }

            Sample(visual, bindPose, idle, idle.length * 0.5f);
            AppendPose(report, visual, "idle-mid");
            CapturePose("idle-behind-light.png", report);
            var oldRotation = _key.transform.rotation;
            _key.transform.rotation = Quaternion.Euler(32f, 180f, 0f); // directional source moved to camera (+Z) side
            report.AppendLine($"frontFillSourceDirection={(-_key.transform.forward):F3}; frontFillSourceToCameraDot={Vector3.Dot(-_key.transform.forward, _camera.transform.forward * -1f):F3}");
            CapturePose("idle-front-light.png", report);
            _key.transform.rotation = oldRotation;

            Sample(visual, bindPose, land, land.length * 0.98f);
            AppendPose(report, visual, "land-settle");
            CapturePose("land-behind-light.png", report);
            _key.transform.rotation = Quaternion.Euler(32f, 180f, 0f);
            CapturePose("land-front-light.png", report);

            try
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "shading-diagnostic.txt"), report.ToString());
                Debug.Log("[Iter1711 shading diagnostic]\n" + report);
            }
            finally
            {
                _camera.targetTexture = null;
                RenderTexture.active = null;
                _target.Release();
                UnityEngine.Object.DestroyImmediate(_target);
                EditorApplication.Exit(0);
            }
        }

        private static void Sample(GameObject visual, System.Collections.Generic.Dictionary<Transform, (Vector3, Quaternion, Vector3)> bindPose, AnimationClip clip, float time)
        {
            foreach (var entry in bindPose)
            {
                entry.Key.localPosition = entry.Value.Item1;
                entry.Key.localRotation = entry.Value.Item2;
                entry.Key.localScale = entry.Value.Item3;
            }
            clip.SampleAnimation(visual, Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length - 0.001f)));
        }

        private static void AppendPose(StringBuilder report, GameObject visual, string pose)
        {
            foreach (var skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var shared = skinned.sharedMesh;
                if (shared == null) continue;
                var baked = new Mesh { name = "Iter1711 diagnostic baked pose" };
                try
                {
                    skinned.BakeMesh(baked);
                    var normals = baked.normals;
                    var tangents = baked.tangents;
                    var matrix = skinned.transform.localToWorldMatrix.inverse.transpose;
                    var towardCamera = -_camera.transform.forward;
                    var towardLight = -_key.transform.forward;
                    var cameraDot = 0f;
                    var lightDot = 0f;
                    var forwardFacing = 0;
                    var lightFacing = 0;
                    var positiveScale = skinned.transform.lossyScale;
                    var avgWorldNormal = Vector3.zero;
                    for (var i = 0; i < normals.Length; i++)
                    {
                        var n = matrix.MultiplyVector(normals[i]).normalized;
                        var dc = Vector3.Dot(n, towardCamera);
                        var dl = Vector3.Dot(n, towardLight);
                        cameraDot += dc;
                        lightDot += dl;
                        avgWorldNormal += n;
                        if (dc > 0f) forwardFacing++;
                        if (dl > 0f) lightFacing++;
                    }
                    if (normals.Length > 0)
                    {
                        cameraDot /= normals.Length;
                        lightDot /= normals.Length;
                        avgWorldNormal /= normals.Length;
                    }
                    report.AppendLine($"pose={pose} renderer={skinned.name} vertices={baked.vertexCount} normalCount={normals.Length} tangentCount={tangents.Length} rootWorldRot={skinned.transform.rotation.eulerAngles:F1} lossyScale={positiveScale:F4} determinant={skinned.transform.localToWorldMatrix.determinant:F5} avgNormalWorld={avgWorldNormal.normalized:F3} avgDotCamera={cameraDot:F3} percentFacingCamera={(normals.Length > 0 ? 100f * forwardFacing / normals.Length : 0f):F1}% avgDotKey={lightDot:F3} percentFacingKey={(normals.Length > 0 ? 100f * lightFacing / normals.Length : 0f):F1}% sharedNormals={shared.normals.Length} sharedTangents={shared.tangents.Length}");
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
        }

        private static void SetupEnvironment()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Temporary shading review support plane";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = Vector3.one * 0.4f;
            floor.GetComponent<Collider>().enabled = false;
            var cameraObject = new GameObject("Temporary shading review camera");
            cameraObject.transform.position = new Vector3(2.1f, 1.45f, 5f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.92f, 0f));
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.71f, 0.79f, 0.88f);
            _camera.orthographic = true;
            _camera.orthographicSize = 1.28f;
            _camera.nearClipPlane = 0.02f;
            _camera.farClipPlane = 100f;
            _target = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _camera.targetTexture = _target;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.78f, 0.78f);
            var lightObject = new GameObject("Temporary shading review key light");
            _key = lightObject.AddComponent<Light>();
            _key.type = LightType.Directional;
            _key.intensity = 0.75f;
            lightObject.transform.rotation = Quaternion.Euler(32f, -27f, 0f);
        }

        private static void CapturePose(string fileName, StringBuilder report)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(_camera, new RenderPipeline.StandardRequest { destination = _target });
            else _camera.Render();
            RenderTexture.active = _target;
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(Path.Combine(OutputDirectory, fileName), image.EncodeToPNG());
            report.AppendLine("image=" + fileName);
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = null;
        }
    }
}
#endif
