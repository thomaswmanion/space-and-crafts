#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds an isolated room-scale review of the existing Meshy Starfold Workbench.
    /// It never changes the production WorkshopHub scene.
    /// </summary>
    public static class StarfoldWorkbenchReviewIter2208
    {
        private const string SourceScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ReviewScene = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2208.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench_base_color.png";
        private const string CandidateRoot = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/UnityCandidate_ReviewOnly_Iter2208";
        private const string MaterialPath = CandidateRoot + "/StarfoldWorkbench_ReviewOnly.mat";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-review-iter2208/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-review-iter2208/REVIEW.txt";

        [MenuItem("Space & Crafts/Build Starfold Workbench Review (Iter2208)")]
        public static void BuildReview()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                throw new InvalidOperationException("Could not determine the Unity project root.");
            string Absolute(string path) => Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before building the review-only candidate.");
            if (File.Exists(Absolute(ReviewScene)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScene) != null)
                throw new InvalidOperationException($"Review scene already exists; refusing to overwrite it: {ReviewScene}");
            if (!File.Exists(Absolute(SourceScene)) || !File.Exists(Absolute(ModelPath)) || !File.Exists(Absolute(TexturePath)))
                throw new FileNotFoundException("Workshop scene or Starfold Workbench source files are missing.");

            EnsureAssetFolder("Assets/SpaceAndCrafts/Scenes/ReviewOnly");
            EnsureAssetFolder(CandidateRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(CapturePath)));
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(ReportPath)));
            var backupPath = ReportPath.Replace("REVIEW.txt", "WorkshopHub_before.unity");
            File.Copy(Absolute(SourceScene), Absolute(backupPath), false);
            File.Copy(Absolute(SourceScene), Absolute(ReviewScene), false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var textureImporter = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (textureImporter == null) throw new InvalidOperationException("Starfold Workbench base-color texture did not import.");
            if (textureImporter.textureType != TextureImporterType.Default || !textureImporter.sRGBTexture)
            {
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.sRGBTexture = true;
                textureImporter.SaveAndReimport();
            }

            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (sourceModel == null || baseColor == null)
                throw new InvalidOperationException("Unity could not load the workbench model or base-color texture.");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No Lit shader is available.");
            var material = new Material(shader) { name = "StarfoldWorkbench_ReviewOnly" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);
            // Expand the imported model hierarchy into ordinary scene objects; the
            // previous nested-prefab candidate kept stale near-zero renderer bounds.
            var modelProbe = UnityEngine.Object.Instantiate(sourceModel);
            if (modelProbe == null) throw new InvalidOperationException("Could not instantiate the source FBX for bounds measurement.");
            var probeRenderers = modelProbe.GetComponentsInChildren<Renderer>(true);
            if (probeRenderers.Length == 0) throw new InvalidOperationException("The source FBX has no renderers.");
            var sourceBounds = probeRenderers.Select(r => r.bounds).Aggregate(Union);
            if (sourceBounds.size.x <= 0.001f || sourceBounds.size.y <= 0.001f || sourceBounds.size.z <= 0.001f)
                throw new InvalidOperationException($"Unexpected source model bounds: {sourceBounds}");
            UnityEngine.Object.DestroyImmediate(modelProbe);

            // Fit the actual Meshy silhouette into a compact ~2.4 m-wide tabletop
            // prop while preserving its authored proportions.
            var scale = Mathf.Min(2.4f / sourceBounds.size.x, 1.45f / sourceBounds.size.y);
            var scene = EditorSceneManager.OpenScene(ReviewScene, OpenSceneMode.Single);
            var hub = GameObject.Find("ART_WorkshopHub");
            var placeholder = hub != null ? hub.transform.Find("ART_Workbench") : null;
            if (hub == null || placeholder == null)
                throw new InvalidOperationException("Could not find ART_WorkshopHub/ART_Workbench in the copied review scene.");
            var placement = new Vector3(0f, 0.18f, 0f); // top of the existing 0.35m floor
            UnityEngine.Object.DestroyImmediate(placeholder.gameObject);
            var instance = UnityEngine.Object.Instantiate(sourceModel, hub.transform);
            if (instance == null) throw new InvalidOperationException("Could not instantiate the workbench model into the review scene.");
            instance.transform.localPosition = new Vector3(
                placement.x - sourceBounds.center.x * scale,
                placement.y - sourceBounds.min.y * scale,
                placement.z - sourceBounds.center.z * scale);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;
            instance.name = "ART_StarfoldWorkbench_ReviewOnly";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                // Imported local bounds remain tiny even though Unity's source renderer
                // reports a 1.9 m-wide object. Restore a conservative, transformed model
                // bound so camera culling cannot hide the Meshy prop.
                var modelToRenderer = renderer.transform.worldToLocalMatrix * instance.transform.localToWorldMatrix;
                renderer.localBounds = TransformBounds(sourceBounds, modelToRenderer);
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            if (renderers.Length == 0)
                throw new InvalidOperationException("The imported candidate has no renderers in the review scene.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var renderBounds = renderers.Select(r => TransformBounds(r.localBounds,
                instance.transform.localToWorldMatrix * rendererModelRelativeToInstance(r, instance))).Aggregate(Union);
            if (renderBounds.size.x < 0.1f || renderBounds.size.y < 0.1f || renderBounds.size.z < 0.1f)
                throw new InvalidOperationException($"Candidate mesh-derived world bounds are implausibly small after fit: {renderBounds}");

            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("The review scene has no MainCamera.");
            var report = $"Starfold Workbench room-scale review (Iter2208)\n" +
                         $"Review scene: {ReviewScene}\nProduction scene: unchanged ({SourceScene})\n" +
                         $"Source Meshy FBX: {ModelPath}\nSource bounds before fit: {sourceBounds}\n" +
                         $"Uniform scale: {scale:F4}\nCandidate world bounds after fit: {renderBounds}\n" +
                         $"Room position: {placement}\nCamera: {camera.name} at {camera.transform.position}, FOV {camera.fieldOfView:F1}\n" +
                         $"Capture: {CapturePath}\n" +
                         "This is a static prop and staged editor scene capture only. No collider, assembly mechanic, production scene change, build, play test, or human play is established.\n";
            File.WriteAllText(Absolute(ReportPath), report);
            Capture(camera, CapturePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchReviewIter2208] direct model candidate, scene and capture created; production WorkshopHub was not modified.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Bounds Union(Bounds a, Bounds b)
        {
            a.Encapsulate(b.min);
            a.Encapsulate(b.max);
            return a;
        }

        private static Matrix4x4 rendererModelRelativeToInstance(Renderer renderer, GameObject instance)
        {
            return instance.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
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
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                var absolutePath = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());
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
