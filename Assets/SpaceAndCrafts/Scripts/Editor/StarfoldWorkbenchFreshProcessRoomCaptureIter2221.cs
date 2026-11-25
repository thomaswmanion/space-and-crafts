#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Renders one pre-staged material candidate per fresh Unity process to remove scene-switch carryover.</summary>
    public static class StarfoldWorkbenchFreshProcessRoomCaptureIter2221
    {
        private const string EmbeddedScene = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_Starfold_embedded_Iter2220.unity";
        private const string AtlasScene = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_Starfold_atlas_Iter2220.unity";
        private const string OutputRoot = "Logs/score-comparison/workshop-starfold-workbench-material-iter2221";

        [MenuItem("Space & Crafts/Capture Embedded Material Room (Iter2221)")]
        public static void CaptureEmbedded() => Capture(EmbeddedScene, "embedded.png");

        [MenuItem("Space & Crafts/Capture Atlas Material Room (Iter2221)")]
        public static void CaptureAtlas() => Capture(AtlasScene, "atlas.png");

        private static void Capture(string scenePath, string imageName)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(projectRoot, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before the isolated capture.");
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
            var camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("Copied WorkshopHub MainCamera missing.");
            camera.transform.position = new Vector3(0f, 3.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.3f));
            camera.fieldOfView = 48f;

            var prop = GameObject.Find("ART_Workbench_Starfold_" + (imageName == "embedded.png" ? "embedded" : "atlas") + "_REVIEW");
            if (prop == null) throw new System.InvalidOperationException("Staged workbench missing.");
            var renderer = prop.GetComponentInChildren<MeshRenderer>();
            if (renderer == null) throw new System.InvalidOperationException("Staged workbench renderer missing.");
            var report = new StringBuilder();
            report.AppendLine("Fresh-process controlled room capture — Iter2221");
            report.AppendLine($"Scene={scene.path}; capture={imageName}; renderer material={renderer.sharedMaterial?.name}; shader={renderer.sharedMaterial?.shader?.name}; BaseMap={renderer.sharedMaterial?.GetTexture("_BaseMap")?.name ?? "none"}; root scale={prop.transform.localScale}; bounds={renderer.bounds}.");
            report.AppendLine($"Ambient={RenderSettings.ambientMode} {RenderSettings.ambientLight}, intensity={RenderSettings.ambientIntensity}; lights={string.Join(" | ", Object.FindObjectsByType<Light>().Select(l => $"{l.name}: enabled={l.enabled}, color={l.color}, intensity={l.intensity}, range={l.range}, position={l.transform.position}"))}.");
            report.AppendLine($"Camera={camera.transform.position}; rotation={camera.transform.rotation.eulerAngles}; FOV={camera.fieldOfView}; this process is separate from the other material capture.");
            report.AppendLine("Still Unity camera capture only; no production changes, runtime, gameplay, interaction/collision, build, or human play.");

            Directory.CreateDirectory(Path.GetDirectoryName(Abs(OutputRoot + "/" + imageName))!);
            File.WriteAllText(Abs(OutputRoot + "/" + Path.ChangeExtension(imageName, ".txt")), report.ToString());
            CaptureCamera(camera, Abs(OutputRoot + "/" + imageName));
            AssetDatabase.SaveAssets();
            Debug.Log($"[StarfoldWorkbenchFreshProcessRoomCaptureIter2221] saved {imageName} from a fresh process.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            var old = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(false,false); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = old; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
#endif
