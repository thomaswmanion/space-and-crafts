#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Renders embedded and atlas materials side-by-side in the same clean scene/frame.</summary>
    public static class StarfoldWorkbenchMaterialSideBySideIter2222
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string AtlasMaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/StarfoldWorkbench_MaterialPair_Iter2222.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2222/side_by_side.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2222/REVIEW.txt";

        [MenuItem("Space & Crafts/Compare Starfold Materials Side-by-Side (Iter2222)")]
        public static void CapturePair()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(projectRoot, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            if (File.Exists(Abs(ScenePath)) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.InvalidOperationException($"Review scene already exists: {ScenePath}");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Material>(AtlasMaterialPath);
            if (model == null || atlas == null) throw new System.InvalidOperationException("Workbench FBX or atlas material missing.");
            var probe = Object.Instantiate(model);
            var worldBounds = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            Object.DestroyImmediate(probe);
            var rootBounds = TransformBounds(worldBounds, model.transform.worldToLocalMatrix);
            var widthFit = 1.65f / worldBounds.size.x;
            var heightFit = 1.4f / worldBounds.size.y;
            var scale = model.transform.localScale * Mathf.Min(widthFit, heightFit);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.11f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 30f;
            camera.transform.position = new Vector3(0f, 1.75f, -6.0f);
            camera.transform.LookAt(new Vector3(0f, 0.72f, 0f));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.47f, 1f);
            RenderSettings.ambientIntensity = 1f;
            AddDirectional("Review Key", new Color(1f, 0.9f, 0.8f), 1.2f, Quaternion.Euler(35f, -25f, 0f));
            AddDirectional("Review Fill", new Color(0.72f, 0.85f, 1f), 0.55f, Quaternion.Euler(20f, 145f, 0f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Review Ground";
            ground.transform.position = new Vector3(0f, 0f, 0f);
            ground.transform.localScale = new Vector3(8f, 1f, 3f);
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMaterial.color = new Color(0.16f, 0.19f, 0.27f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var sb = new StringBuilder();
            sb.AppendLine("Starfold Workbench same-frame material comparison — Iter2222");
            sb.AppendLine("Two copies use the same imported geometry, scale, orientation, lights, ground and camera in one scene. Only material differs.");
            sb.AppendLine($"Original world bounds={worldBounds}; preserved root scale={scale}; estimated render bounds=1.65m wide x {worldBounds.size.y * Mathf.Min(widthFit, heightFit):F2}m high; camera={camera.transform.position}/{camera.transform.rotation.eulerAngles}.");

            for (var i = 0; i < 2; i++)
            {
                var instance = Object.Instantiate(model);
                instance.name = i == 0 ? "A_EmbeddedMaterial" : "B_AtlasMaterial";
                instance.transform.localScale = scale;
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                instance.transform.localPosition = new Vector3(i == 0 ? -1.15f : 1.15f,
                    -rootBounds.min.y * scale.y,
                    -rootBounds.center.z * scale.z);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (i == 1) renderer.sharedMaterial = atlas;
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                }
                sb.AppendLine($"{(i == 0 ? "A" : "B")}: material={(i == 0 ? renderers.FirstOrDefault()?.sharedMaterial?.name ?? "none" : atlas.name)}; shader={renderers.FirstOrDefault()?.sharedMaterial?.shader?.name}; bounds={BoundsOf(instance)}.");
                var label = new GameObject(i == 0 ? "Label A" : "Label B");
                label.transform.position = new Vector3(i == 0 ? -1.15f : 1.15f, 2.25f, 0f);
                var text = label.AddComponent<TextMesh>();
                text.text = i == 0 ? "EMBEDDED" : "ATLAS";
                text.characterSize = 0.12f;
                text.fontSize = 56;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(1f, 0.88f, 0.62f);
                label.transform.rotation = camera.transform.rotation;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            sb.AppendLine("Capture is a close static asset comparison only; it is not a workshop/gameplay view, interaction/contact check, build, or human play.");
            File.WriteAllText(Abs(ReportPath), sb.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchMaterialSideBySideIter2222] same-frame material comparison saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void AddDirectional(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name); var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.color = color; l.intensity = intensity; l.shadows = LightShadows.Soft; go.transform.rotation = rotation;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            return rs.Length > 0 ? rs.Select(r => r.bounds).Aggregate(Union) : new Bounds(go.transform.position, Vector3.zero);
        }
        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var c=m.MultiplyPoint3x4(b.center); var e=b.extents; var x=m.MultiplyVector(Vector3.right*e.x); var y=m.MultiplyVector(Vector3.up*e.y); var z=m.MultiplyVector(Vector3.forward*e.z);
            return new Bounds(c,2f*new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
        }
        private static void Capture(Camera camera, string path)
        {
            var previous=RenderTexture.active; var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32); var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(false,false); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { camera.targetTexture=null; RenderTexture.active=previous; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
#endif
