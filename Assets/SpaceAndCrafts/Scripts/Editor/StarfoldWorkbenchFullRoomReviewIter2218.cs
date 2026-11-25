#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Fresh full-room composition candidate using the persistent material and isolated-review lighting.</summary>
    public static class StarfoldWorkbenchFullRoomReviewIter2218
    {
        private const string SourceScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ReviewScene = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2218.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2218/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2218/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Starfold Full Room Setup (Iter2218)")]
        public static void BuildReview()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            if (!File.Exists(Abs(SourceScene))) throw new FileNotFoundException("WorkshopHub missing", SourceScene);
            if (File.Exists(Abs(ReviewScene)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScene) != null)
                throw new System.InvalidOperationException($"Review scene already exists: {ReviewScene}");

            Directory.CreateDirectory(Path.GetDirectoryName(Abs(ReviewScene))!);
            File.Copy(Abs(SourceScene), Abs(ReviewScene));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ReviewScene, OpenSceneMode.Single);
            var placeholder = GameObject.Find("ART_Workbench");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (placeholder == null || model == null || material == null)
                throw new System.InvalidOperationException("Placeholder, imported FBX, or saved material missing.");
            var targetPosition = placeholder.transform.localPosition;
            var targetRotation = placeholder.transform.localRotation;
            var parent = placeholder.transform.parent;
            var probe = Object.Instantiate(model);
            var bounds = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            var rootLocalBounds = TransformBounds(bounds, model.transform.worldToLocalMatrix);
            Object.DestroyImmediate(probe);

            var normalized = 2.0f / bounds.size.x;
            var scale = model.transform.localScale * normalized; // retain 100x imported model root scale
            var instance = Object.Instantiate(model, parent);
            instance.name = "ART_Workbench_Starfold_Meshy_Iter2218_REVIEW";
            instance.transform.localScale = scale;
            instance.transform.localRotation = targetRotation * Quaternion.Euler(0f, 180f, 0f);
            var floor = GameObject.Find("ART_WorkshopFloor");
            var floorTop = floor != null && floor.GetComponent<Collider>() != null ? floor.GetComponent<Collider>().bounds.max.y : 0.0625f;
            instance.transform.localPosition = new Vector3(
                targetPosition.x - rootLocalBounds.center.x * scale.x,
                floorTop - rootLocalBounds.min.y * scale.y,
                targetPosition.z - rootLocalBounds.center.z * scale.z);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
            Object.DestroyImmediate(placeholder);

            var status = GameObject.Find("Inventory")?.GetComponent<TextMesh>();
            if (status != null)
            {
                var old = status.transform.localPosition;
                status.transform.localPosition = new Vector3(-4.3f, old.y, old.z);
            }

            var point = GameObject.Find("ART_WorkshopKeyLight")?.GetComponent<Light>();
            if (point != null) point.enabled = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.42f, 1f);
            RenderSettings.ambientIntensity = 1f;
            AddDirectional("ART_ReviewKey_Iter2218", new Color(1f, 0.87f, 0.76f), 1.8f, Quaternion.Euler(32f, -25f, 0f));
            AddDirectional("ART_ReviewFill_Iter2218", new Color(0.67f, 0.82f, 1f), 0.75f, Quaternion.Euler(24f, 145f, 0f));

            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied WorkshopHub MainCamera missing.");
            camera.transform.position = new Vector3(0f, 3.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.3f));
            camera.fieldOfView = 48f;
            EditorSceneManager.SaveScene(scene, ReviewScene);

            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            var report = new StringBuilder();
            report.AppendLine("Starfold workbench full-room comparison — Iter2218");
            report.AppendLine($"Copied source scene {SourceScene}; production scene and bootstrap untouched.");
            report.AppendLine($"Imported world bounds={bounds}; target 2.0m width; applied root scale={scale}; rotation Y=180; floor top={floorTop:F3}m.");
            report.AppendLine($"Material={material.name}, BaseMap={material.GetTexture("_BaseMap")?.name}; status label position={(status != null ? status.transform.localPosition.ToString() : "missing")}.");
            report.AppendLine($"Neutral fill plus warm/cool directional lights. Camera={camera.transform.position}, rotation={camera.transform.rotation.eulerAngles}, FOV={camera.fieldOfView:F1}.");
            report.AppendLine("Review-only still capture, not runtime, gameplay, interaction/contact, build, or human-play evidence.");
            File.WriteAllText(Abs(ReportPath), report.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchFullRoomReviewIter2218] copied full-room capture saved; production scene unchanged.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void AddDirectional(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = rotation;
        }

        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var c = m.MultiplyPoint3x4(b.center); var e = b.extents;
            var x = m.MultiplyVector(Vector3.right * e.x); var y = m.MultiplyVector(Vector3.up * e.y); var z = m.MultiplyVector(Vector3.forward * e.z);
            return new Bounds(c, 2f * new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)));
        }
        private static void Capture(Camera camera, string path)
        {
            var previous = RenderTexture.active; var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(false, false); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
#endif
