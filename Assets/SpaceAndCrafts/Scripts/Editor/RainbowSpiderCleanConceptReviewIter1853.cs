#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Stages the Iter1853 clean-concept spider beside the current Rainbow
    /// Spider review prefab at the prefab's rendered gameplay scale. This is
    /// an isolated visual review and never saves a production scene.
    /// </summary>
    public static class RainbowSpiderCleanConceptReviewIter1853
    {
        private const string CandidateModelPath =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/RainbowSpider_Meshy71/RainbowSpider_Meshy71_CleanConcept.fbx";
        private const string CandidateTexturePath =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/RainbowSpider_Meshy71/RainbowSpider_Meshy71_CleanConcept_textures/base_color.png";
        private const string ReferencePrefabPath =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";
        private const string ReviewFolder =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/RainbowSpider_Meshy71/ReviewOnly";
        private const string PrefabPath = ReviewFolder + "/RainbowSpider_Meshy71_CleanConcept_ReviewOnly.prefab";
        private const string MaterialPath = ReviewFolder + "/RainbowSpider_Meshy71_CleanConcept_ReviewOnly.mat";
        private const string OutputFolder = "Logs/score-comparison/rainbow-spider-cleanconcept-iter1853";
        private const int CellSize = 768;

        [MenuItem("Space & Crafts/Review Rainbow Spider Clean Concept Iter1853")]
        public static void Capture()
        {
            try
            {
                RunCapture();
                Debug.Log("[Rainbow Spider Iter1853] ReviewOnly prefab and staged visual captures saved under " + OutputFolder + ". Production prefabs/scenes untouched.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void RunCapture()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var candidateModel = AssetDatabase.LoadAssetAtPath<GameObject>(CandidateModelPath);
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePrefabPath);
            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(CandidateTexturePath);
            if (candidateModel == null) throw new InvalidOperationException("Missing candidate FBX: " + CandidateModelPath);
            if (referencePrefab == null) throw new InvalidOperationException("Missing existing Rainbow Spider review prefab: " + ReferencePrefabPath);
            if (baseColor == null) throw new InvalidOperationException("Missing clean-concept base-color map: " + CandidateTexturePath);

            EnsureFolder(ReviewFolder);
            Directory.CreateDirectory(OutputFolder);
            var material = CreateOrUpdateMaterial(baseColor);
            // Batch mode starts with an untitled scene; use Single so no
            // additive-scene operation depends on an unsaved base scene.
            var stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var report = new StringBuilder();
            report.AppendLine("Rainbow Spider Meshy 7.1 Clean Concept — Iter1853 ReviewOnly staging");
            report.AppendLine("Capture: isolated Unity editor-camera stills, compared at the current review-prefab rendered height and seated on a temporary flat stage.");
            report.AppendLine("This is not gameplay, animation, collision, build, test, or human-play verification. No production prefab, controller, behavior, or scene was modified.");
            report.AppendLine("Candidate source: " + CandidateModelPath);
            report.AppendLine("Candidate texture: " + CandidateTexturePath);
            report.AppendLine("Reference prefab and scale source: " + ReferencePrefabPath);
            report.AppendLine("ReviewOnly prefab: " + PrefabPath);
            report.AppendLine();

            var candidateRoot = PrefabUtility.InstantiatePrefab(candidateModel, stage) as GameObject;
            if (candidateRoot == null) throw new InvalidOperationException("Could not instantiate clean-concept FBX.");
            candidateRoot.name = "RainbowSpider_Meshy71_CleanConcept_ReviewOnly";
            ApplyMaterial(candidateRoot, material);
            var candidateRenderers = candidateRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled).ToArray();
            if (candidateRenderers.Length == 0) throw new InvalidOperationException("Clean-concept FBX contains no enabled renderers.");
            var candidateBounds = BoundsOf(candidateRenderers);
            var candidateHeight = Mathf.Max(0.001f, candidateBounds.size.y);

            var referenceRoot = PrefabUtility.InstantiatePrefab(referencePrefab, stage) as GameObject;
            if (referenceRoot == null) throw new InvalidOperationException("Could not instantiate current review prefab.");
            referenceRoot.name = "RainbowSpider_CurrentReviewPrefab_ScaleReference";
            foreach (var animator in referenceRoot.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            var referenceRenderers = referenceRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled).ToArray();
            if (referenceRenderers.Length == 0) throw new InvalidOperationException("Existing review prefab contains no enabled renderers.");
            var referenceBounds = BoundsOf(referenceRenderers);
            var referenceHeight = Mathf.Max(0.001f, referenceBounds.size.y);

            var matchScale = referenceHeight / candidateHeight;
            candidateRoot.transform.localScale = candidateRoot.transform.localScale * matchScale;
            candidateBounds = BoundsOf(candidateRenderers);
            SeatAtGround(candidateRoot.transform, candidateBounds);
            SeatAtGround(referenceRoot.transform, referenceBounds);

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(candidateRoot, PrefabPath);
            if (savedPrefab == null) throw new InvalidOperationException("Could not save the candidate ReviewOnly prefab.");

            report.AppendLine(Describe("Clean concept", candidateModel, candidateRoot, candidateRenderers, candidateBounds, matchScale));
            report.AppendLine(Describe("Current scale reference", referencePrefab, referenceRoot, referenceRenderers, referenceBounds, 1f));
            report.AppendLine("Scale/readability: matched-height candidate width is " + (candidateBounds.size.x / Mathf.Max(0.001f, referenceBounds.size.x) * 100f).ToString("F1") + "% of the reference width. At the same rendered height the face reads clearly; the broad body and low, integrated legs make its spider silhouette rounder and less leg-forward.");
            report.AppendLine("Hierarchy/material note: this static candidate receives one ReviewOnly material using the delivered base-color texture across its renderers. See counts below; no skeleton or animation is inferred from appearance.");
            report.AppendLine("Scale note: candidate uniform scale was set to " + matchScale.ToString("F5") + " so aggregate enabled-renderer height matches the reference prefab. Both are only bottom-seated against the temporary stage for comparison.");
            report.AppendLine("Bounds in the Describe lines are sampled before ground seating; the staged images move the complete roots so those aggregate lower renderer bounds touch the temporary plane. This is not per-foot contact evidence.");
            report.AppendLine("Gameplay caveat: no collider is added, no enemy brain or animator is wired, and staged bottom seating does not establish individual foot contact or platform behavior.");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TemporaryReviewFloor_NotSaved";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = Vector3.one * 4f;
            var floorRenderer = floor.GetComponent<Renderer>();
            var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            SetMaterialColor(floorMaterial, new Color(0.22f, 0.25f, 0.30f, 1f));
            floorRenderer.sharedMaterial = floorMaterial;

            AddLight("ReviewKey", new Vector3(28f, 145f, -18f), 0.68f);
            AddLight("ReviewFill", new Vector3(-22f, 160f, 8f), 0.24f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.19f);

            var cameraObject = new GameObject("ReviewCamera_NotSaved");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 80f;
            camera.orthographicSize = referenceHeight * 0.72f;
            camera.allowHDR = true;

            var cameraAngles = new[] { ("front", 0f), ("three-quarter", 38f), ("side", 90f) };
            var candidateFrames = new List<Texture2D>();
            var referenceFrames = new List<Texture2D>();
            foreach (var (label, angle) in cameraAngles)
            {
                candidateRoot.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                candidateRoot.transform.position = Vector3.zero;
                candidateBounds = BoundsOf(candidateRenderers);
                SeatAtGround(candidateRoot.transform, candidateBounds);
                candidateFrames.Add(RenderFrame(camera, candidateRoot, referenceHeight, label, true));
                File.WriteAllBytes(Path.Combine(OutputFolder, "cleanconcept_" + label.Replace('-', '_') + ".png"), candidateFrames[candidateFrames.Count - 1].EncodeToPNG());

                referenceRoot.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                referenceRoot.transform.position = new Vector3(2.1f, 0f, 0f);
                referenceBounds = BoundsOf(referenceRenderers);
                SeatAtGround(referenceRoot.transform, referenceBounds);
                referenceFrames.Add(RenderFrame(camera, referenceRoot, referenceHeight, label, false));
                File.WriteAllBytes(Path.Combine(OutputFolder, "reference_" + label.Replace('-', '_') + ".png"), referenceFrames[referenceFrames.Count - 1].EncodeToPNG());
            }

            var contactSheet = ComposeSheet(candidateFrames, referenceFrames);
            var sheetPath = Path.Combine(OutputFolder, "rainbow_spider_cleanconcept_contact_sheet.png");
            File.WriteAllBytes(sheetPath, contactSheet.EncodeToPNG());
            report.AppendLine("Staged images (candidate front / three-quarter / side; then current prefab front / three-quarter / side):");
            report.AppendLine(string.Join(Environment.NewLine, cameraAngles.Select(angle => "  cleanconcept_" + angle.Item1.Replace('-', '_') + ".png")));
            report.AppendLine(string.Join(Environment.NewLine, cameraAngles.Select(angle => "  reference_" + angle.Item1.Replace('-', '_') + ".png")));
            report.AppendLine("Contact sheet: " + sheetPath);
            File.WriteAllText(Path.Combine(OutputFolder, "review-report.txt"), report.ToString());

            foreach (var frame in candidateFrames) UnityEngine.Object.DestroyImmediate(frame);
            foreach (var frame in referenceFrames) UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(contactSheet);
            UnityEngine.Object.DestroyImmediate(floorMaterial);
            // Retain the scene-independent prefab only; the stage is never saved.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string Describe(string label, GameObject source, GameObject instance, Renderer[] renderers, Bounds bounds, float appliedScale)
        {
            var meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
            var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var meshCount = meshFilters.Count(filter => filter.sharedMesh != null) + skinned.Count(renderer => renderer.sharedMesh != null);
            var triangleCount = meshFilters.Where(filter => filter.sharedMesh != null).Sum(filter => CountTriangles(filter.sharedMesh))
                + skinned.Where(renderer => renderer.sharedMesh != null).Sum(renderer => CountTriangles(renderer.sharedMesh));
            var materials = renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null)
                .Select(material => material.name).Distinct().ToArray();
            var bones = instance.GetComponentsInChildren<Transform>(true).Select(transform => transform.name).ToArray();
            var avatars = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<Avatar>().ToArray();
            var clips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            return label + ": source=" + AssetDatabase.GetAssetPath(source)
                + "; meshFilters=" + meshFilters.Length + "; skinnedRenderers=" + skinned.Length
                + "; enabledRenderers=" + renderers.Length + "; meshCount=" + meshCount
                + "; triangles=" + triangleCount + "; transformCount=" + bones.Length
                + "; avatarCount=" + avatars.Length + "; clipCount=" + clips.Length
                + "; rendererBounds(m)=" + bounds.size.ToString("F3") + "; rendererMinY(m)=" + bounds.min.y.ToString("F3")
                + "; appliedReviewScale=" + appliedScale.ToString("F5")
                + "; materials=[" + string.Join(", ", materials) + "]"
                + "; transforms=[" + string.Join(", ", bones.Take(36)) + (bones.Length > 36 ? ", …" : "") + "]"
                + "; clips=[" + string.Join(", ", clips.Select(clip => clip.name + " " + clip.length.ToString("F2") + "s")) + "]";
        }

        private static int CountTriangles(Mesh mesh)
        {
            var triangles = 0;
            for (var index = 0; index < mesh.subMeshCount; index++) triangles += (int)(mesh.GetIndexCount(index) / 3);
            return triangles;
        }

        private static Material CreateOrUpdateMaterial(Texture2D baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a Lit shader.");
            if (material == null)
            {
                material = new Material(shader) { name = "RainbowSpider_Meshy71_CleanConcept_ReviewOnly" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.02f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.24f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void SeatAtGround(Transform root, Bounds bounds)
        {
            root.position += Vector3.down * bounds.min.y;
        }

        private static Texture2D RenderFrame(Camera camera, GameObject model, float referenceHeight, string label, bool candidate)
        {
            var bounds = BoundsOf(model.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray());
            camera.orthographicSize = referenceHeight * 0.78f;
            var x = candidate ? 0f : 2.1f;
            var center = new Vector3(x, bounds.center.y, 0f);
            camera.transform.position = center + new Vector3(0f, referenceHeight * 0.08f, 4.4f);
            camera.transform.LookAt(center);
            var texture = new Texture2D(CellSize, CellSize, TextureFormat.RGB24, false);
            var target = new RenderTexture(CellSize, CellSize, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, CellSize, CellSize), 0, 0);
            texture.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            return texture;
        }

        private static Texture2D ComposeSheet(List<Texture2D> candidates, List<Texture2D> references)
        {
            var sheet = new Texture2D(CellSize * 3, CellSize * 2, TextureFormat.RGB24, false);
            var labels = new[] { "front", "three-quarter", "side" };
            for (var row = 0; row < 2; row++)
            {
                var frames = row == 0 ? candidates : references;
                for (var column = 0; column < 3; column++)
                    sheet.SetPixels(column * CellSize, (1 - row) * CellSize, CellSize, CellSize, frames[column].GetPixels());
            }
            sheet.Apply();
            return sheet;
        }

        private static Light AddLight(string name, Vector3 rotation, float intensity)
        {
            var gameObject = new GameObject(name);
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            gameObject.transform.rotation = Quaternion.Euler(rotation);
            return light;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
#endif
