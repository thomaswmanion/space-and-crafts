#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Creates two ReviewOnly room scenes that differ only in the workbench material assignment.</summary>
    public static class StarfoldWorkbenchMaterialAblationIter2219
    {
        private const string SourceScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string AtlasMaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string OutputRoot = "Logs/score-comparison/workshop-starfold-workbench-material-iter2219";

        [MenuItem("Space & Crafts/Compare Starfold Materials in Workshop (Iter2219)")]
        public static void CapturePair()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Material>(AtlasMaterialPath);
            if (model == null || atlas == null) throw new System.InvalidOperationException("FBX or saved atlas material missing.");

            var probe = Object.Instantiate(model);
            var bounds = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            Object.DestroyImmediate(probe);
            var rootBounds = TransformBounds(bounds, model.transform.worldToLocalMatrix);
            var scale = model.transform.localScale * (2.0f / bounds.size.x);

            var report = new StringBuilder();
            report.AppendLine("Starfold Workbench room material A/B — Iter2219");
            report.AppendLine("Only material assignment differs; both copied scenes preserve the source hub's lighting, tilt camera, placement, root scale, status-label layout, and other scene objects.");
            report.AppendLine($"FBX bounds={bounds}; normalized root scale={scale}; renderer local bounds={rootBounds}; atlas={atlas.GetTexture("_BaseMap")?.name}.");

            RenderCopy("embedded", null, "workbench_embedded.png", report, root, Abs, model, bounds, rootBounds, scale);
            RenderCopy("atlas", atlas, "workbench_atlas.png", report, root, Abs, model, bounds, rootBounds, scale);

            Directory.CreateDirectory(Path.GetDirectoryName(Abs(OutputRoot + "/REVIEW.txt"))!);
            File.WriteAllText(Abs(OutputRoot + "/REVIEW.txt"), report.ToString());
            Debug.Log("[StarfoldWorkbenchMaterialAblationIter2219] paired original-lighting room captures saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void RenderCopy(string label, Material material, string captureName, StringBuilder report,
            string root, System.Func<string, string> abs, GameObject model, Bounds worldBounds, Bounds rootBounds, Vector3 scale)
        {
            var scenePath = $"Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_Starfold_{label}_Iter2219.unity";
            var sourcePath = abs(SourceScene);
            var destPath = abs(scenePath);
            if (File.Exists(destPath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                throw new System.InvalidOperationException($"Review scene already exists: {scenePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(sourcePath, destPath);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var placeholder = GameObject.Find("ART_Workbench");
            if (placeholder == null) throw new System.InvalidOperationException("Copied source scene lacks ART_Workbench.");
            var position = placeholder.transform.localPosition;
            var rotation = placeholder.transform.localRotation;
            var parent = placeholder.transform.parent;
            var floor = GameObject.Find("ART_WorkshopFloor");
            var floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
            var floorTop = floorCollider != null ? floorCollider.bounds.max.y : 0.0625f;

            var instance = Object.Instantiate(model, parent);
            instance.name = $"ART_Workbench_Starfold_{label}_REVIEW";
            instance.transform.localScale = scale;
            instance.transform.localRotation = rotation * Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localPosition = new Vector3(position.x - rootBounds.center.x * scale.x,
                floorTop - rootBounds.min.y * scale.y,
                position.z - rootBounds.center.z * scale.z);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (material != null) renderer.sharedMaterial = material;
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
            }
            Object.DestroyImmediate(placeholder);
            var status = GameObject.Find("Inventory")?.GetComponent<TextMesh>();
            if (status != null) status.transform.localPosition = new Vector3(-4.3f, status.transform.localPosition.y, status.transform.localPosition.z);
            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied hub MainCamera missing.");
            camera.transform.position = new Vector3(0f, 3.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.3f));
            camera.fieldOfView = 48f;
            EditorSceneManager.SaveScene(scene, scenePath);

            var capturePath = OutputRoot + "/" + captureName;
            Directory.CreateDirectory(Path.GetDirectoryName(abs(capturePath))!);
            Capture(camera, abs(capturePath));
            report.AppendLine($"{label}: scene={scenePath}; material={(material != null ? material.name : "imported embedded Material.001")}; capture={capturePath}; bounds={BoundsOf(instance)}; floor={floorTop:F3}.");
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var rr = go.GetComponentsInChildren<Renderer>(true);
            if (rr.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            return rr.Select(r => r.bounds).Aggregate(Union);
        }
        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var c = m.MultiplyPoint3x4(b.center); var e = b.extents;
            var x = m.MultiplyVector(Vector3.right * e.x); var y = m.MultiplyVector(Vector3.up * e.y); var z = m.MultiplyVector(Vector3.forward * e.z);
            return new Bounds(c, 2f * new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x), Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y), Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
        }
        private static void Capture(Camera camera, string path)
        {
            var old = RenderTexture.active; var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(false,false); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { camera.targetTexture = null; RenderTexture.active = old; Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
#endif
