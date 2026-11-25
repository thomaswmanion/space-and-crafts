#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Checks a simple workshop-lighting refinement only in the isolated review scene.</summary>
    public static class StarfoldWorkbenchLightingReviewIter2213
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2212.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2213/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2213/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Starfold Workbench Room Lighting (Iter2213)")]
        public static void CaptureReview()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before the isolated review.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.31f, 0.42f, 1f);
            RenderSettings.ambientIntensity = 1.25f;

            var keyGo = GameObject.Find("ART_WorkshopKeyLight");
            if (keyGo == null) throw new System.InvalidOperationException("Copied WorkshopHub key light not found.");
            var key = keyGo.GetComponent<Light>();
            if (key == null) throw new System.InvalidOperationException("Copied WorkshopHub key light has no Light component.");
            key.color = new Color(1f, 0.89f, 0.74f);
            key.intensity = 2.7f;
            key.range = 22f;

            var fillGo = new GameObject("ART_ReviewWorkbenchFillLight_Iter2213");
            fillGo.transform.position = new Vector3(-2.6f, 3.4f, -3.1f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.65f, 0.84f, 1f);
            fill.intensity = 2.0f;
            fill.range = 10f;
            fill.shadows = LightShadows.None;

            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied WorkshopHub MainCamera missing.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            var report = new StringBuilder();
            report.AppendLine("Starfold Workbench copied-room lighting capture — Iter2213");
            report.AppendLine("Only the ReviewOnly scene was edited; production WorkshopHub is unchanged.");
            report.AppendLine($"Ambient: Flat {RenderSettings.ambientLight}, intensity={RenderSettings.ambientIntensity:F2}.");
            report.AppendLine($"Key: {key.type}, color={key.color}, intensity={key.intensity:F2}, range={key.range:F1}.");
            report.AppendLine($"Cool fill: {fill.color}, intensity={fill.intensity:F2}, range={fill.range:F1}.");
            report.AppendLine($"Camera position={camera.transform.position}, rotation={camera.transform.rotation.eulerAngles}, FOV={camera.fieldOfView:F1}.");
            report.AppendLine("This is a staged saved-camera render, not a runtime lighting/performance check or human play.");
            File.WriteAllText(Abs(ReportPath), report.ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchLightingReviewIter2213] isolated workshop lighting capture saved.");
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
