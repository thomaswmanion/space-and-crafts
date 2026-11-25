#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Identifies per-clip material changes and captures a nonproduction palette-locked comparison.</summary>
    public static class SplatIter1711PaletteDiagnosticCli
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.fbx";
        private const string ReviewPrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/ReviewOnly/Splat_Iter1711_GroundedPalette_ReviewOnly.prefab";
        private const string OutputDirectory = "Logs/splat-iter1711-palette-diagnostic";
        private const string VertexColorShaderPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/SplatCandidateVertexColorPreview.shader";
        private const int Resolution = 900;

        private sealed class RendererPalette
        {
            public Renderer Renderer;
            public Material[] Materials;
            public ColorState[] Colors;
        }
        private readonly struct ColorState
        {
            public readonly bool HasBase, HasColor;
            public readonly Color Base, Color;
            public ColorState(Material material)
            {
                HasBase = material != null && material.HasProperty("_BaseColor");
                HasColor = material != null && material.HasProperty("_Color");
                Base = HasBase ? material.GetColor("_BaseColor") : default;
                Color = HasColor ? material.GetColor("_Color") : default;
            }
            public void Apply(Material material)
            {
                if (material == null) return;
                if (HasBase && material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Base);
                if (HasColor && material.HasProperty("_Color")) material.SetColor("_Color", Color);
            }
        }

        [MenuItem("Space & Crafts/Diagnose Iter1711 Idle Land Palette Differences")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutputDirectory);
            var report = new StringBuilder();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Missing Iter1711 FBX: " + ModelPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            var idle = clips.First(c => c.name.EndsWith("Splat_Idle", StringComparison.OrdinalIgnoreCase));
            var land = clips.First(c => c.name.EndsWith("Splat_Land", StringComparison.OrdinalIgnoreCase));

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null) throw new InvalidOperationException("Could not instantiate candidate FBX.");
            visual.name = "Iter1711 material diagnostic clone";
            var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);

            report.AppendLine("scope=isolated Editor direct-clip material diagnostic; original FBX/material assets are not assigned for mutation");
            report.AppendLine($"model={ModelPath}; idle={idle.name}; land={land.name}");
            AppendBindings(report, idle);
            AppendBindings(report, land);

            var palettes = new List<RendererPalette>();
            foreach (var renderer in renderers)
            {
                var clones = renderer.sharedMaterials.Select(m => m != null ? new Material(m) { name = m.name + " (Iter1711 diagnostic clone)" } : null).ToArray();
                renderer.sharedMaterials = clones;
                var palette = new RendererPalette { Renderer = renderer, Materials = clones, Colors = clones.Select(m => new ColorState(m)).ToArray() };
                palettes.Add(palette);
                AppendRendererMaterials(report, "baseline", palette);
            }

            SetupCameraAndSupport();
            var currentBindingsBefore = CaptureBindings(renderers);
            idle.SampleAnimation(visual, idle.length * 0.5f);
            AppendSampleState(report, "idle", renderers, palettes, currentBindingsBefore);
            RestorePalette(palettes);
            Capture("idle-palette-locked.png");

            var currentBindingsIdle = CaptureBindings(renderers);
            land.SampleAnimation(visual, land.length * 0.98f);
            AppendSampleState(report, "land-settle", renderers, palettes, currentBindingsIdle);
            RestorePalette(palettes);
            Capture("land-palette-locked.png");

            var materialCurves = new List<(AnimationClip clip, EditorCurveBinding binding)>();
            foreach (var clip in new[] { idle, land })
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                    .Where(b => b.propertyName.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0
                        || b.propertyName.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0
                        || b.propertyName.IndexOf("m_Materials", StringComparison.OrdinalIgnoreCase) >= 0))
                    materialCurves.Add((clip, binding));
            }
            var refCurves = new[] { idle, land }.SelectMany(c => AnimationUtility.GetObjectReferenceCurveBindings(c).Select(b => (clip: c, binding: b))).ToArray();
            var clipMaterialAnimation = materialCurves.Count > 0 || refCurves.Length > 0;
            report.AppendLine($"clipContainsMaterialOrColorBindings={clipMaterialAnimation} floatCurves={materialCurves.Count} objectReferenceCurves={refCurves.Length}");

            if (clipMaterialAnimation)
            {
                var reviewRoot = new GameObject("Splat Iter1711 Grounded Palette REVIEW ONLY");
                var reviewVisual = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (reviewVisual == null) throw new InvalidOperationException("Could not instantiate original FBX for the palette-only review variant.");
                reviewVisual.name = "Palette-locked Iter1711 visual snapshot (no controller)";
                reviewVisual.transform.SetParent(reviewRoot.transform, false);
                reviewVisual.transform.localPosition = Vector3.zero;
                // Use authored preview vertex colors for the white body / violet limbs,
                // but keep this override confined to the review-only prefab.
                ApplyVertexColorReviewMaterials(reviewVisual, report);
                PrefabUtility.SaveAsPrefabAsset(reviewRoot, ReviewPrefabPath);
                UnityEngine.Object.DestroyImmediate(reviewRoot);
                report.AppendLine("reviewOnlyPrefab=" + ReviewPrefabPath);
            }

            try
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "palette-diagnostic.txt"), report.ToString());
                Debug.Log("[Iter1711 material diagnostic]\n" + report);
            }
            finally
            {
                foreach (var palette in palettes)
                    foreach (var material in palette.Materials)
                        if (material != null) UnityEngine.Object.DestroyImmediate(material);
                EditorApplication.Exit(0);
            }
        }

        private static void AppendBindings(StringBuilder report, AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.propertyName.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0
                    || binding.propertyName.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0
                    || binding.propertyName.IndexOf("m_Materials", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    var values = curve != null && curve.length > 0
                        ? string.Join(",", curve.keys.Select(k => k.value.ToString("F3")))
                        : "none";
                    report.AppendLine($"floatMaterialBinding clip={clip.name} path={binding.path} property={binding.propertyName} keys={values}");
                }
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                report.AppendLine($"objectBinding clip={clip.name} path={binding.path} property={binding.propertyName} values={string.Join(",", keys.Select(k => k.value != null ? k.value.name : "null"))}");
            }
        }

        private static void AppendRendererMaterials(StringBuilder report, string phase, RendererPalette p)
        {
            for (var i = 0; i < p.Renderer.sharedMaterials.Length; i++)
            {
                var m = p.Renderer.sharedMaterials[i];
                var color = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString()
                    : m != null && m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "n/a";
                report.AppendLine($"material phase={phase} renderer={p.Renderer.name} slot={i} name={(m != null ? m.name : "null")} shader={(m != null && m.shader != null ? m.shader.name : "none")} color={color}");
            }
        }

        private static Dictionary<Renderer, Material[]> CaptureBindings(IEnumerable<Renderer> renderers)
            => renderers.ToDictionary(r => r, r => r.sharedMaterials.ToArray());

        private static void AppendSampleState(StringBuilder report, string phase, Renderer[] renderers, List<RendererPalette> palette, Dictionary<Renderer, Material[]> before)
        {
            foreach (var p in palette)
            {
                var current = p.Renderer.sharedMaterials;
                var old = before.TryGetValue(p.Renderer, out var items) ? items : Array.Empty<Material>();
                var different = current.Length != old.Length || current.Where((m, i) => i >= old.Length || m != old[i]).Any();
                report.AppendLine($"sample={phase} renderer={p.Renderer.name} materialReferencesChanged={different}");
                AppendRendererMaterials(report, phase + "-pre-restore", p);
            }
        }

        private static void RestorePalette(List<RendererPalette> palettes)
        {
            foreach (var p in palettes)
            {
                p.Renderer.sharedMaterials = p.Materials;
                for (var i = 0; i < Mathf.Min(p.Materials.Length, p.Colors.Length); i++) p.Colors[i].Apply(p.Materials[i]);
            }
        }

        private static void SetupCameraAndSupport()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Material review support plane";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = Vector3.one * 0.4f;
            floor.GetComponent<Collider>().enabled = false;
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", new Color(0.38f, 0.43f, 0.52f));
            else if (floorMat.HasProperty("_Color")) floorMat.SetColor("_Color", new Color(0.38f, 0.43f, 0.52f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.78f, 0.78f);
            var lightObject = new GameObject("Material review key light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            lightObject.transform.rotation = Quaternion.Euler(32f, -27f, 0f);
            var cameraObject = new GameObject("Material review camera");
            cameraObject.transform.position = new Vector3(2.1f, 1.45f, 5f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.92f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.71f, 0.79f, 0.88f);
            camera.orthographic = true;
            camera.orthographicSize = 1.28f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 100f;
            camera.targetTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture.Create();
        }

        private static void Capture(string fileName)
        {
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null) throw new InvalidOperationException("Review camera missing.");
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = camera.targetTexture });
            else camera.Render();
            RenderTexture.active = camera.targetTexture;
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(Path.Combine(OutputDirectory, fileName), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = null;
        }

        private static void ApplyVertexColorReviewMaterials(GameObject reviewVisual, StringBuilder report)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(VertexColorShaderPath);
            if (shader == null) { report.AppendLine("vertexColorReviewShader=missing"); return; }
            const string reviewMaterialPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/ReviewOnly/Splat_Iter1711_PaletteReviewOnly.mat";
            var reviewMaterial = AssetDatabase.LoadAssetAtPath<Material>(reviewMaterialPath);
            if (reviewMaterial == null)
            {
                reviewMaterial = new Material(shader) { name = "Splat Iter1711 review-only vertex palette" };
                AssetDatabase.CreateAsset(reviewMaterial, reviewMaterialPath);
            }
            foreach (var renderer in reviewVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Splat_UnifiedBodyAndLimbs", StringComparison.Ordinal))
                {
                    renderer.sharedMaterial = reviewMaterial;
                }
            }
            report.AppendLine("reviewOnlyPaletteOverride=existing Iter1705 vertex-color preview shader on body/limb renderer");
        }
    }
}
#endif
