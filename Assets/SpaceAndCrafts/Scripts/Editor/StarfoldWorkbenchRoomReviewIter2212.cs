#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Stages the existing Meshy workbench in a copied WorkshopHub for saved-camera review.</summary>
    public static class StarfoldWorkbenchRoomReviewIter2212
    {
        private const string SourceScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ReviewScene = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2212.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench_base_color.png";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2212/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2212/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Starfold Workbench in Copied Workshop (Iter2212)")]
        public static void BuildReview()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new InvalidOperationException("Unity project root could not be resolved.");
            string Absolute(string path) => Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before the isolated review.");
            if (File.Exists(Absolute(ReviewScene)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScene) != null)
                throw new InvalidOperationException($"Review scene already exists: {ReviewScene}");
            if (!File.Exists(Absolute(SourceScene))) throw new FileNotFoundException("WorkshopHub source scene is missing.", SourceScene);

            // Copy only the scene file so Unity assigns this review scene its own asset GUID.
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(ReviewScene))!);
            File.Copy(Absolute(SourceScene), Absolute(ReviewScene));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ReviewScene, OpenSceneMode.Single);

            var placeholder = GameObject.Find("ART_Workbench");
            if (placeholder == null) throw new InvalidOperationException("Copied WorkshopHub has no ART_Workbench placeholder.");
            var parent = placeholder.transform.parent;
            var targetLocalPosition = placeholder.transform.localPosition;
            var targetLocalRotation = placeholder.transform.localRotation;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (model == null || texture == null) throw new InvalidOperationException("Workbench FBX or texture failed to load.");

            var probe = UnityEngine.Object.Instantiate(model);
            var renderers = probe.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Workbench FBX has no renderers.");
            var sourceWorldBounds = renderers.Select(r => r.bounds).Aggregate(Union);
            var sourceRootBounds = TransformBounds(sourceWorldBounds, model.transform.worldToLocalMatrix);
            UnityEngine.Object.DestroyImmediate(probe);
            if (sourceWorldBounds.size.x < 0.1f || sourceWorldBounds.size.y < 0.1f)
                throw new InvalidOperationException($"Unexpected imported bounds: {sourceWorldBounds}");

            EnsureFolder("Assets/SpaceAndCrafts/Materials");
            EnsureFolder("Assets/SpaceAndCrafts/Materials/ReviewOnly");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No Lit shader is available.");
            var material = new Material(shader) { name = "StarfoldWorkbench_Iter2212_Review" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);

            var targetWidth = 2.0f;
            var normalization = targetWidth / sourceWorldBounds.size.x;
            var instanceScale = model.transform.localScale * normalization; // Preserve the FBX's 100× root scale.
            var instance = UnityEngine.Object.Instantiate(model, parent);
            instance.name = "ART_Workbench_Starfold_Meshy_Iter2212_REVIEW";
            instance.transform.localScale = instanceScale;
            instance.transform.localRotation = targetLocalRotation * Quaternion.Euler(0f, 180f, 0f); // drawer faces room camera at -Z

            // Match the old workbench center in X/Z and place the model's lowest point on the measured floor top.
            var floor = GameObject.Find("ART_WorkshopFloor");
            var floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
            var floorTop = floorCollider != null ? floorCollider.bounds.max.y : 0.18f;
            var bottomOffset = sourceRootBounds.min.y * instanceScale.y;
            instance.transform.localPosition = new Vector3(
                targetLocalPosition.x - sourceRootBounds.center.x * instanceScale.x,
                floorTop - bottomOffset,
                targetLocalPosition.z - sourceRootBounds.center.z * instanceScale.z);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
            }
            UnityEngine.Object.DestroyImmediate(placeholder);

            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new InvalidOperationException("Copied WorkshopHub has no MainCamera.");
            EditorSceneManager.SaveScene(scene, ReviewScene);
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(CapturePath))!);
            var report = new StringBuilder();
            report.AppendLine("Starfold Workbench copied-room saved-camera review — Iter2212");
            report.AppendLine($"Source scene copied: {SourceScene}; production scene was not opened for writing.");
            report.AppendLine($"FBX source bounds={sourceWorldBounds}; imported root scale={model.transform.localScale}; root-local bounds={sourceRootBounds}.");
            report.AppendLine($"Target world width={targetWidth:F2}m; applied root scale={instanceScale}; yaw=180 degrees; floor top={floorTop:F3}m.");
            report.AppendLine($"Review instance bounds={GetBounds(instance)}; material={material.name}; base-color={texture.name}.");
            report.AppendLine($"Capture uses the copied scene's Main Camera at {camera.transform.position} / {camera.transform.rotation.eulerAngles}.");
            report.AppendLine("The copied scene only stages a static visual candidate. It does not establish interaction, collision support, build launch, gameplay, or human play.");
            File.WriteAllText(Absolute(ReportPath), report.ToString());
            Capture(camera, Absolute(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchRoomReviewIter2212] copied WorkshopHub staged and saved-camera image captured; production scene untouched.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            return renderers.Select(r => r.bounds).Aggregate(Union);
        }

        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }

        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var center = matrix.MultiplyPoint3x4(local.center);
            var extents = local.extents;
            var x = matrix.MultiplyVector(Vector3.right * extents.x);
            var y = matrix.MultiplyVector(Vector3.up * extents.y);
            var z = matrix.MultiplyVector(Vector3.forward * extents.z);
            var worldExtents = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(center, worldExtents * 2f);
        }

        private static void EnsureFolder(string path)
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
