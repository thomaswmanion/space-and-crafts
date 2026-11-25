#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Moves only the copied scene's workbench status text clear of the Meshy prop.</summary>
    public static class StarfoldWorkbenchLabelLayoutReviewIter2215
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2212.unity";
        private const string CapturePath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2215/frame.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2215/REVIEW.txt";

        [MenuItem("Space & Crafts/Review Workbench Status Label Layout (Iter2215)")]
        public static void CaptureReview()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before the isolated review.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var label = GameObject.Find("Inventory")?.GetComponent<TextMesh>();
            if (label == null) throw new System.InvalidOperationException("Workshop inventory/status TextMesh not found.");
            var oldPosition = label.transform.localPosition;
            label.transform.localPosition = new Vector3(-4.3f, oldPosition.y, oldPosition.z);

            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied WorkshopHub MainCamera missing.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(CapturePath))!);
            File.WriteAllText(Abs(ReportPath), new StringBuilder()
                .AppendLine("Starfold Workbench copied-room status-label composition — Iter2215")
                .AppendLine("ReviewOnly scene; production WorkshopHub unchanged.")
                .AppendLine($"Inventory label moved from {oldPosition} to {label.transform.localPosition}; text remains '{label.text}'.")
                .AppendLine($"Camera={camera.transform.position}, rotation={camera.transform.rotation.eulerAngles}, FOV={camera.fieldOfView:F1}.")
                .AppendLine("Still capture checks only visible composition; no production integration, interaction, collision, runtime, build or human play.")
                .ToString());
            Capture(camera, Abs(CapturePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[StarfoldWorkbenchLabelLayoutReviewIter2215] isolated label-layout capture saved.");
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
