#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Isolated same-angle original-vs-grade spider comparison. Never changes production assets.</summary>
    public static class RainbowSpiderSoftSpectrumPreviewCaptureIter1783
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider.mat";
        private const string CandidateTexturePath = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/ReviewOnly_Iter1779/Spider_BaseColor_SoftSpectrum_ReviewOnly.png";
        private const string OutputPath = "Logs/score-comparison/spider-soft-spectrum-render-review-iter1783";
        private const int Width = 1400;
        private const int Height = 800;

        [MenuItem("Space & Crafts/Capture Spider Soft Spectrum Review Iter1783")]
        public static void Capture()
        {
            if (Directory.Exists(OutputPath)) throw new InvalidOperationException("Refusing to overwrite existing Iter1783 capture directory.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var candidateTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CandidateTexturePath);
            if (prefab == null || sourceMaterial == null || candidateTexture == null)
                throw new InvalidOperationException("Spider prefab, source material, or Iter1779 candidate texture is missing.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var allocated = new System.Collections.Generic.List<Object>();
            var previousActive = RenderTexture.active;
            try
            {
                var original = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                var graded = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (original == null || graded == null) throw new InvalidOperationException("Could not instantiate existing spider prefab twice.");
                original.name = "Spider original material (temporary review)";
                graded.name = "Spider soft-spectrum material (temporary review)";
                original.transform.position = new Vector3(-1.3f, 0f, 0f);
                graded.transform.position = new Vector3(1.3f, 0f, 0f);
                foreach (var go in new[] { original, graded })
                {
                    foreach (var driver in go.GetComponentsInChildren<MonoBehaviour>(true)) driver.enabled = false;
                    foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    var animator = go.GetComponentInChildren<Animator>(true);
                    if (animator != null) { animator.speed = 0f; animator.Update(0f); }
                }

                var originalMat = new Material(sourceMaterial) { name = "Spider original runtime copy (temporary)" };
                var candidateMat = new Material(sourceMaterial) { name = "Spider soft-spectrum runtime copy (temporary)" };
                allocated.Add(originalMat); allocated.Add(candidateMat);
                if (candidateMat.HasProperty("_BaseMap")) candidateMat.SetTexture("_BaseMap", candidateTexture);
                else if (candidateMat.HasProperty("_MainTex")) candidateMat.SetTexture("_MainTex", candidateTexture);
                else throw new InvalidOperationException("Spider shader has no supported base-color texture property.");
                ApplyMaterial(original, originalMat);
                ApplyMaterial(graded, candidateMat);
                var renderers = original.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Existing spider prefab instantiated without any renderers.");
                var aggregateBounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) aggregateBounds.Encapsulate(renderer.bounds);

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                SceneManager.MoveGameObjectToScene(floor, scene);
                floor.name = "Temporary neutral review floor";
                floor.transform.position = new Vector3(0f, -0.02f, 0f);
                floor.transform.localScale = Vector3.one * 2.5f;
                var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.12f, 0.16f, 0.20f, 1f) };
                allocated.Add(floorMat); floor.GetComponent<Renderer>().sharedMaterial = floorMat;

                var keyObject = new GameObject("Temporary review key light");
                SceneManager.MoveGameObjectToScene(keyObject, scene);
                var key = keyObject.AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.1f; key.color = new Color(1f, .94f, .88f);
                keyObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
                var fillObject = new GameObject("Temporary review fill light");
                SceneManager.MoveGameObjectToScene(fillObject, scene);
                var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .55f; fill.color = new Color(.73f, .84f, 1f);
                fillObject.transform.rotation = Quaternion.Euler(18f, 150f, 0f);
                var cameraObject = new GameObject("Temporary review camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.aspect = (float)Width / Height; camera.orthographicSize = 2.0f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .050f, .070f, 1f);
                camera.nearClipPlane = .03f; camera.farClipPlane = 50f;
                var aim = new Vector3(0f, -.1f, 0f);
                camera.transform.position = aim + new Vector3(3.8f, 3.0f, 5.3f);
                camera.transform.LookAt(aim);

                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                rt.Create(); allocated.Add(rt); camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                allocated.Add(image); image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply(false, false);

                Directory.CreateDirectory(OutputPath);
                File.WriteAllBytes(Path.Combine(OutputPath, "original-left_soft-spectrum-right.png"), image.EncodeToPNG());
                var report = new StringBuilder();
                report.AppendLine("Iter1783 isolated editor render; production prefab/material/texture unchanged.");
                report.AppendLine("Left = source material; right = temporary runtime copy using the Iter1779 sat 0.58 / contrast 0.82 review-only texture.");
                report.AppendLine("Both copies use the existing animated spider prefab at the same transform scale and camera; Animator driver disabled, no AI/gameplay, static prefab pose only.");
                report.AppendLine($"camera position={camera.transform.position}; lookAt={aim}; ortho={camera.orthographicSize:F2}; resolution={Width}x{Height}; spider bounds={aggregateBounds}.");
                report.AppendLine($"source texture={AssetDatabase.GetAssetPath(sourceMaterial.GetTexture(sourceMaterial.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex"))}; candidate={CandidateTexturePath}");
                File.WriteAllText(Path.Combine(OutputPath, "review.txt"), report.ToString());
                Debug.Log("[Spider Iter1783] Saved isolated material-grade comparison: " + OutputPath);
            }
            finally
            {
                RenderTexture.active = previousActive;
                foreach (var obj in allocated) if (obj != null) Object.DestroyImmediate(obj);
                // The batch-mode scene is intentionally unsaved and discarded when this helper exits.
                EditorApplication.Exit(0);
            }
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }
    }
}
#endif
