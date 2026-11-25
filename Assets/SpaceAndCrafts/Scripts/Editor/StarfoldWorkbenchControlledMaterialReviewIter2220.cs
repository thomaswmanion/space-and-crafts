#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Compares embedded and atlas workbench materials in identical copied-room lighting.</summary>
    public static class StarfoldWorkbenchControlledMaterialReviewIter2220
    {
        private const string SourceScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string AtlasMaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string OutputRoot = "Logs/score-comparison/workshop-starfold-workbench-material-iter2220";

        [MenuItem("Space & Crafts/Controlled Starfold Material Pair (Iter2220)")]
        public static void CapturePair()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(projectRoot, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Material>(AtlasMaterialPath);
            if (model == null || atlas == null) throw new System.InvalidOperationException("FBX or saved atlas material missing.");

            var probe = Object.Instantiate(model);
            var bounds = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            Object.DestroyImmediate(probe);
            var localBounds = TransformBounds(bounds, model.transform.worldToLocalMatrix);
            var scale = model.transform.localScale * (2.0f / bounds.size.x);
            var report = new StringBuilder();
            report.AppendLine("Controlled Starfold Workbench room material A/B — Iter2220");
            report.AppendLine("The two images use identical source scene, camera, position, scale, floor, ambient, and point light. Only the model's material differs.");
            report.AppendLine($"FBX world bounds={bounds}; imported scale={model.transform.localScale}; normalized root scale={scale}; atlas={atlas.GetTexture("_BaseMap")?.name}.");

            RenderCopy("embedded", null, "workbench_embedded.png", report, Abs, model, bounds, localBounds, scale);
            RenderCopy("atlas", atlas, "workbench_atlas.png", report, Abs, model, bounds, localBounds, scale);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(OutputRoot + "/REVIEW.txt"))!);
            File.WriteAllText(Abs(OutputRoot + "/REVIEW.txt"), report.ToString());
            Debug.Log("[StarfoldWorkbenchControlledMaterialReviewIter2220] controlled A/B room captures saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void RenderCopy(string label, Material material, string captureName, StringBuilder report,
            System.Func<string, string> abs, GameObject model, Bounds worldBounds, Bounds localBounds, Vector3 scale)
        {
            var scenePath = $"Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_Starfold_{label}_Iter2220.unity";
            var sceneFullPath = abs(scenePath);
            if (File.Exists(sceneFullPath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                throw new System.InvalidOperationException($"Review scene already exists: {scenePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(sceneFullPath)!);
            File.Copy(abs(SourceScene), sceneFullPath);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.30f, 1f);
            RenderSettings.ambientIntensity = 1f;
            foreach (var light in Object.FindObjectsByType<Light>())
            {
                if (light.gameObject.name == "ART_WorkshopKeyLight")
                {
                    light.enabled = true;
                    light.type = LightType.Point;
                    light.color = Color.white;
                    light.intensity = 1.2f;
                    light.range = 18f;
                }
                else light.enabled = false;
            }

            var placeholder = GameObject.Find("ART_Workbench");
            if (placeholder == null) throw new System.InvalidOperationException("WorkshopHub placeholder missing.");
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
            instance.transform.localPosition = new Vector3(position.x - localBounds.center.x * scale.x,
                floorTop - localBounds.min.y * scale.y,
                position.z - localBounds.center.z * scale.z);
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
            if (camera == null) throw new System.InvalidOperationException("MainCamera missing.");
            camera.transform.position = new Vector3(0f, 3.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.3f));
            camera.fieldOfView = 48f;
            EditorSceneManager.SaveScene(scene, scenePath);

            var relativeCapture = OutputRoot + "/" + captureName;
            Directory.CreateDirectory(Path.GetDirectoryName(abs(relativeCapture))!);
            Capture(camera, abs(relativeCapture));
            report.AppendLine($"{label}: scene={scenePath}; material={(material != null ? material.name : "embedded Material.001")}; capture={relativeCapture}; bounds={BoundsOf(instance)}; floorTop={floorTop:F3}; ambient={RenderSettings.ambientLight}; key={GameObject.Find("ART_WorkshopKeyLight")?.GetComponent<Light>()?.intensity}.");
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var r = go.GetComponentsInChildren<Renderer>(true);
            return r.Length > 0 ? r.Select(x => x.bounds).Aggregate(Union) : new Bounds(go.transform.position, Vector3.zero);
        }
        private static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var c=m.MultiplyPoint3x4(b.center); var e=b.extents; var x=m.MultiplyVector(Vector3.right*e.x); var y=m.MultiplyVector(Vector3.up*e.y); var z=m.MultiplyVector(Vector3.forward*e.z);
            return new Bounds(c, 2f*new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
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
