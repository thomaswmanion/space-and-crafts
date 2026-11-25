#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Renders isolated imported spider clips for asset review; no game scene is saved.</summary>
    public static class CaptureRainbowSpiderCandidatePreview
    {
        private const string Root =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461";
        private const string FbxPath = Root + "/RainbowSpider_RigCandidate.fbx";
        private const string PrefabPath = Root + "/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";
        private const string Output = "Logs/unity-rainbow-spider-candidate-iter1461";

        [MenuItem("Space & Crafts/Capture Rainbow Spider Candidate Clip Previews")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            var previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>().ToArray();
            if (prefab == null) throw new InvalidOperationException("Candidate prefab is missing.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewScene);
            instance.name = "RainbowSpider_ReviewOnly";
            instance.transform.position = new Vector3(0f, 0.64f, 0f);
            var animator = instance.GetComponentInChildren<Animator>(true);
            var clipRoot = animator != null ? animator.gameObject : instance;

            var cameraObject = new GameObject("ReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.2f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 1.35f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            camera.transform.position = new Vector3(0f, 1.15f, 4.2f);
            camera.transform.LookAt(new Vector3(0f, 0.75f, 0f));

            AddDirectionalLight("Key", new Vector3(22f, 180f, 0f), 0.48f);
            AddDirectionalLight("Fill", new Vector3(-18f, 155f, 0f), 0.22f);
            RenderSettings.ambientLight = new Color(0.24f, 0.24f, 0.28f);

            var targets = new (string token, float normalizedTime)[]
            {
                ("Idle", 0.30f), ("Walk", 0.35f), ("Attack", 0.66f),
                ("Hit", 0.42f), ("Defeat", 0.80f)
            };
            var rt = new RenderTexture(960, 960, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            foreach (var (token, normalizedTime) in targets)
            {
                var clip = clips.FirstOrDefault(candidate =>
                    candidate != null && candidate.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                if (clip == null) throw new InvalidOperationException($"Missing imported clip: {token}");
                clip.SampleAnimation(clipRoot, Mathf.Clamp01(normalizedTime) * clip.length);
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Output, $"Spider_{token}_UnityPreview.png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.active = null;
            }
            camera.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(previewScene, true);
            Debug.Log($"[Spider candidate] rendered {targets.Length} isolated Unity clip previews to {Output}; no game scene saved.");
        }

        private static void AddDirectionalLight(string name, Vector3 rotation, float intensity)
        {
            var lightObject = new GameObject(name);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            lightObject.transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
#endif
