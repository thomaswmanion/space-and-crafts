#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Renders the saved Unity material after a fresh scene/asset reload, independent of room lighting.</summary>
    public static class StarfoldWorkbenchPersistedMaterialReviewIter2217
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat";
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/StarfoldWorkbench_PersistedMaterial_Iter2217.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2217/front_back.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2217/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Persisted Starfold Material (Iter2217)")]
        public static void CaptureReview()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before isolated review.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null || File.Exists(Abs(ScenePath)))
                throw new System.InvalidOperationException($"Review scene already exists: {ScenePath}");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(MaterialPath, ImportAssetOptions.ForceSynchronousImport);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (model == null || material == null) throw new System.InvalidOperationException("FBX or saved material missing.");

            var probe = Object.Instantiate(model);
            var bounds = probe.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate(Union);
            Object.DestroyImmediate(probe);
            var normalized = 1.65f / bounds.size.x;
            var rootBounds = TransformBounds(bounds, model.transform.worldToLocalMatrix);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureReviewFolder();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.12f, 1f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 30f;
            camera.transform.position = new Vector3(0f, 1.6f, -5.2f);
            camera.transform.LookAt(new Vector3(0f, 0.75f, 0f));

            AddDirectionalLight("Review Key", new Color(1f, 0.87f, 0.76f), 1.8f, Quaternion.Euler(32f, -25f, 0f));
            AddDirectionalLight("Review Fill", new Color(0.67f, 0.82f, 1f), 0.75f, Quaternion.Euler(24f, 145f, 0f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.42f, 1f);
            RenderSettings.ambientIntensity = 1f;

            var sb = new StringBuilder();
            sb.AppendLine("Persisted Starfold Workbench material after Unity reimport — Iter2217");
            sb.AppendLine($"FBX bounds={bounds}; source root scale={model.transform.localScale}; normalized root scale={model.transform.localScale * normalized}; material={material.name}; shader={material.shader?.name}; BaseMap={material.GetTexture("_BaseMap")?.name}; BaseColor={material.GetColor("_BaseColor")}.");
            for (var i = 0; i < 2; i++)
            {
                var instance = Object.Instantiate(model);
                instance.name = i == 0 ? "Workbench_ImportOrientation0" : "Workbench_DrawerFacingCamera_180";
                instance.transform.localScale = model.transform.localScale * normalized;
                instance.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 0f : 180f, 0f);
                instance.transform.localPosition = new Vector3((i == 0 ? -1.15f : 1.15f) - rootBounds.center.x * instance.transform.localScale.x,
                    -rootBounds.min.y * instance.transform.localScale.y,
                    -rootBounds.center.z * instance.transform.localScale.z);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    sb.AppendLine($"copy={i}; mesh={mesh?.name}; materials={renderer.sharedMaterials.Length}; bounds={renderer.bounds}; localUV0={mesh?.uv.Length}; vertexCount={mesh?.vertexCount}; triangleCount={(mesh != null ? mesh.triangles.Length / 3 : 0)}.");
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            sb.AppendLine($"Camera at {camera.transform.position}, rotation {camera.transform.rotation.eulerAngles}; two orientations shown side-by-side.");
            sb.AppendLine("Staged static import/material view only; no production change, interaction, build, gameplay, or human play.");
            File.WriteAllText(Abs(ReportPath), sb.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchPersistedMaterialReviewIter2217] fresh-reimport isolated material render saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void AddDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
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

        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var center = matrix.MultiplyPoint3x4(local.center);
            var e = local.extents;
            var x = matrix.MultiplyVector(Vector3.right * e.x);
            var y = matrix.MultiplyVector(Vector3.up * e.y);
            var z = matrix.MultiplyVector(Vector3.forward * e.z);
            return new Bounds(center, 2f * new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)));
        }

        private static void EnsureReviewFolder()
        {
            const string path = "Assets/SpaceAndCrafts/Scenes/ReviewOnly";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Scenes", "ReviewOnly");
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
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
