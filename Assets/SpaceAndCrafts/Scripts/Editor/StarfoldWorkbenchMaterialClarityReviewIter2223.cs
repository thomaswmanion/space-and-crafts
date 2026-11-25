#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Provides a neutral, uncluttered, same-frame material comparison for the existing workbench.</summary>
    public static class StarfoldWorkbenchMaterialClarityReviewIter2223
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string AtlasMaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/StarfoldWorkbench_MaterialPair_Iter2223.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2223/unlabeled_pair.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2223/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Starfold Materials (Clear Iter2223)")]
        public static void CapturePair()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            if (File.Exists(Abs(ScenePath)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.InvalidOperationException($"Review scene already exists: {ScenePath}");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Material>(AtlasMaterialPath);
            if (model == null || atlas == null) throw new System.InvalidOperationException("Workbench FBX or atlas material missing.");

            var probe = Object.Instantiate(model);
            var b = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            Object.DestroyImmediate(probe);
            var rootBounds = TransformBounds(b, model.transform.worldToLocalMatrix);
            var factor = Mathf.Min(1.65f / b.size.x, 1.4f / b.size.y);
            var scale = model.transform.localScale * factor;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.38f, 1f);
            RenderSettings.ambientIntensity = 1f;
            AddDirectional("Key", new Color(1f, 0.92f, 0.82f), 0.55f, Quaternion.Euler(35f, -25f, 0f));
            AddDirectional("Fill", new Color(0.72f, 0.85f, 1f), 0.22f, Quaternion.Euler(20f, 145f, 0f));

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.055f, 0.095f, 1f);
            camera.fieldOfView = 31f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 30f;
            camera.transform.position = new Vector3(0f, 1.78f, -5.5f);
            camera.transform.LookAt(new Vector3(0f, 0.70f, 0f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Neutral Review Ground";
            ground.transform.position = new Vector3(0f, -0.02f, 0f);
            ground.transform.localScale = new Vector3(6f, 1f, 2f);
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var groundMaterial = new Material(unlitShader);
            if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", new Color(0.11f, 0.13f, 0.20f));
            if (groundMaterial.HasProperty("_Color")) groundMaterial.SetColor("_Color", new Color(0.11f, 0.13f, 0.20f));
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var report = new StringBuilder();
            report.AppendLine("Starfold Workbench same-frame material A/B — Iter2223");
            report.AppendLine("Left is the source FBX's embedded material; right is the saved atlas material. Same mesh, scale, camera, ground, and lights.");
            report.AppendLine($"Source bounds={b}; normalized scale={scale}; predicted dimensions {b.size.x*factor:F2} x {b.size.y*factor:F2} x {b.size.z*factor:F2} m.");
            report.AppendLine($"Ambient={RenderSettings.ambientLight}; directional intensities=0.55/0.22 with shadows off; camera={camera.transform.position}/{camera.transform.rotation.eulerAngles}; FOV={camera.fieldOfView:F1}.");

            for (var i = 0; i < 2; i++)
            {
                var instance = Object.Instantiate(model);
                instance.name = i == 0 ? "A_EmbeddedMaterial_Left" : "B_AtlasMaterial_Right";
                instance.transform.localScale = scale;
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                instance.transform.localPosition = new Vector3(i == 0 ? -1.12f : 1.12f,
                    -rootBounds.min.y * scale.y,
                    -rootBounds.center.z * scale.z);
                var rr = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in rr) if (i == 1) renderer.sharedMaterial = atlas;
                report.AppendLine($"{(i == 0 ? "Left" : "Right")}: material={(rr.FirstOrDefault()?.sharedMaterial?.name ?? "none")}; BaseMap={rr.FirstOrDefault()?.sharedMaterial?.GetTexture("_BaseMap")?.name ?? "none"}; bounds={BoundsOf(instance)}.");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            report.AppendLine("Static close-up capture only. No workshop-context claim, interaction, collision/contact, build, gameplay or human play.");
            File.WriteAllText(Abs(ReportPath), report.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchMaterialClarityReviewIter2223] uncluttered same-frame A/B capture saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void AddDirectional(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name); var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.color = color; light.intensity = intensity; light.shadows = LightShadows.None; go.transform.rotation = rotation;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); return rs.Length == 0 ? new Bounds(go.transform.position, Vector3.zero) : rs.Select(r => r.bounds).Aggregate(Union);
        }
        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var c=m.MultiplyPoint3x4(b.center); var e=b.extents; var x=m.MultiplyVector(Vector3.right*e.x); var y=m.MultiplyVector(Vector3.up*e.y); var z=m.MultiplyVector(Vector3.forward*e.z);
            return new Bounds(c,2f*new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
        }
        private static void Capture(Camera camera, string path)
        {
            var old=RenderTexture.active; var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32); var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(false,false); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
#endif
