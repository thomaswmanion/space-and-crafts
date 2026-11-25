#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Tests camera framing and neutral lighting on the isolated workshop review only.</summary>
    public static class StarfoldWorkbenchCompositionReviewIter2214
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2212.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2214/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2214/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Workshop Framing (Iter2214)")]
        public static void CaptureReview()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before the isolated review.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.43f, 0.52f, 1f);
            RenderSettings.ambientIntensity = 1.0f;
            var key = GameObject.Find("ART_WorkshopKeyLight")?.GetComponent<Light>();
            if (key == null) throw new System.InvalidOperationException("Workshop key light missing.");
            key.color = Color.white;
            key.intensity = 2.1f;
            key.range = 22f;
            var fill = GameObject.Find("ART_ReviewWorkbenchFillLight_Iter2213")?.GetComponent<Light>();
            if (fill != null) Object.DestroyImmediate(fill.gameObject);
            var fillGo = new GameObject("ART_ReviewWorkbenchFillLight_Iter2214");
            fillGo.transform.position = new Vector3(-2.2f, 2.8f, -2.2f);
            fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.78f, 0.89f, 1f);
            fill.intensity = 2.4f;
            fill.range = 12f;
            fill.shadows = LightShadows.None;

            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied WorkshopHub MainCamera missing.");
            camera.transform.position = new Vector3(0f, 3.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.3f));
            camera.fieldOfView = 48f;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            var report = new StringBuilder();
            report.AppendLine("Starfold Workbench copied-room composition review — Iter2214");
            report.AppendLine("ReviewOnly copy of WorkshopHub; production scene is unchanged.");
            report.AppendLine($"Camera={camera.transform.position}, rotation={camera.transform.rotation.eulerAngles}, FOV={camera.fieldOfView:F1}.");
            report.AppendLine($"Ambient flat={RenderSettings.ambientLight}, intensity={RenderSettings.ambientIntensity:F1}; key intensity={key.intensity:F1}; cool fill intensity={fill.intensity:F1}.");
            report.AppendLine("This staged still checks composition/visibility only. No gameplay, camera control, interaction, collider support, build or human play was run.");
            File.WriteAllText(Abs(ReportPath), report.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchCompositionReviewIter2214] isolated composition capture saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
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
