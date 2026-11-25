#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Isolates Meshy FBX rendering/orientation from the Workshop room and its floor.</summary>
    public static class StarfoldWorkbenchFocusedReviewIter2211
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/StarfoldWorkbench_Focused_Iter2211.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench_base_color.png";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-focused-iter2211/front_back.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-focused-iter2211/REVIEW.txt";

        [MenuItem("Space & Crafts/Focus Review Starfold Workbench Import (Iter2211)")]
        public static void BuildReview()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new InvalidOperationException("Unity project root could not be resolved.");
            string Absolute(string path) => Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before the isolated review.");
            if (File.Exists(Absolute(ScenePath)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException($"Review scene already exists: {ScenePath}");

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (model == null || texture == null) throw new InvalidOperationException("Meshy model or base-color map failed to load.");
            var probe = UnityEngine.Object.Instantiate(model);
            var sourceRenderers = probe.GetComponentsInChildren<Renderer>(true);
            if (sourceRenderers.Length == 0) throw new InvalidOperationException("Imported FBX has no renderers.");
            var sourceBounds = sourceRenderers.Select(r => r.bounds).Aggregate(Union);
            if (sourceBounds.size.x < 0.1f || sourceBounds.size.y < 0.1f)
                throw new InvalidOperationException($"Imported FBX bounds are unexpectedly small: {sourceBounds}");
            UnityEngine.Object.DestroyImmediate(probe);
            var sourceBoundsInModelRoot = TransformBounds(sourceBounds, model.transform.worldToLocalMatrix);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureAssetFolder("Assets/SpaceAndCrafts/Scenes/ReviewOnly");
            var root = new GameObject("StarfoldWorkbench_FocusedReview_Iter2209");
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.15f, 1f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 50f;
            camera.transform.position = new Vector3(0f, 1.25f, -5.2f);
            camera.transform.LookAt(new Vector3(0f, 0.72f, 0f));

            var key = new GameObject("Review Key Light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.4f;
            key.transform.rotation = Quaternion.Euler(35f, -28f, 0f);
            var fill = new GameObject("Review Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No Lit shader found.");
            var material = new Material(shader) { name = "StarfoldWorkbench_FocusedReview" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

            var targetWidth = 1.65f;
            var scale = targetWidth / sourceBounds.size.x;
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("Starfold Workbench isolated FBX review — Iter2211");
            diagnostic.AppendLine($"Source world bounds: {sourceBounds}; source model root scale={model.transform.localScale}; " +
                $"source bounds in model-root coordinates={sourceBoundsInModelRoot}; target width={targetWidth}; " +
                $"applied root scale={model.transform.localScale * scale}; renderers={sourceRenderers.Length}");
            diagnostic.AppendLine("The Meshy FBX has a 100× root scale and ~0.01-unit mesh bounds. The applied scale preserves that authored import scale while normalizing the world width.");
            diagnostic.AppendLine("Two directly instantiated copies use opposite Y orientations; custom material uses the existing base-color map.");
            diagnostic.AppendLine("No production scene or gameplay state was changed.");

            for (var i = 0; i < 2; i++)
            {
                var instance = UnityEngine.Object.Instantiate(model, root.transform);
                instance.name = i == 0 ? "StarfoldWorkbench_Front0" : "StarfoldWorkbench_Front180";
                var appliedScale = model.transform.localScale * scale;
                instance.transform.localScale = appliedScale;
                instance.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 0f : 180f, 0f);
                instance.transform.localPosition = new Vector3((i == 0 ? -1.15f : 1.15f) - sourceBoundsInModelRoot.center.x * appliedScale.x,
                    -sourceBoundsInModelRoot.min.y * appliedScale.y,
                    -sourceBoundsInModelRoot.center.z * appliedScale.z);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                    if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
                    diagnostic.AppendLine($"copy={i}; renderer={renderer.name}; type={renderer.GetType().Name}; enabled={renderer.enabled}; " +
                        $"bounds={renderer.bounds}; localBounds={renderer.localBounds}; " +
                        $"mesh={(renderer.GetComponent<MeshFilter>()?.sharedMesh?.name ?? (renderer as SkinnedMeshRenderer)?.sharedMesh?.name ?? "none")}; " +
                        $"material={renderer.sharedMaterial?.name}; shader={renderer.sharedMaterial?.shader?.name}; " +
                        $"texture={(renderer.sharedMaterial?.mainTexture != null ? renderer.sharedMaterial.mainTexture.name : "none")}");
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(CapturePath)));
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(ReportPath)));
            File.WriteAllText(Absolute(ReportPath), diagnostic.ToString() +
                $"Capture: {CapturePath}\n" +
                "This is a close staged import/orientation view only. It does not establish workshop-camera readability, collision, interaction, production integration, a build, a gameplay test, or human play.\n");
            Capture(camera, Absolute(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchFocusedReviewIter2211] focused asset import capture and renderer diagnostics saved; production scene untouched.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Bounds Union(Bounds a, Bounds b)
        {
            a.Encapsulate(b.min);
            a.Encapsulate(b.max);
            return a;
        }

        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var center = matrix.MultiplyPoint3x4(local.center);
            var extents = local.extents;
            var x = matrix.MultiplyVector(Vector3.right * extents.x);
            var y = matrix.MultiplyVector(Vector3.up * extents.y);
            var z = matrix.MultiplyVector(Vector3.forward * extents.z);
            var worldExtents = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(center, worldExtents * 2f);
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Capture(Camera camera, string path)
        {
            var previous = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
