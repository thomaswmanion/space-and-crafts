#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Capture-only review of the custom Meshy jump/land motion on the matching Iter1855 rig.</summary>
    public static class SplatJumpLandCandidatePreviewIter1855
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855";
        private const string RigPath = Folder + "/Splat_Iter1855_Rigged.fbx";
        private const string ClipPath = Folder + "/AnimationCandidates/JumpLand_CustomPrime_01/Splat_Iter1855_JumpLand_CustomPrime.fbx";
        private const string TexturePath = Folder + "/Splat_Apose_Meshy71_textures/base_color.png";
        private const string MaterialPath = Folder + "/ReviewOnly/Splat_Iter1855_Rigged_ReviewOnly.mat";
        private const string OutputPath = "Logs/score-comparison/splat-meshy-iter1855-reviewonly/splat-custom-jumpland-prime-iter1856.png";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1855-reviewonly/custom-jumpland-prime-iter1856-review.txt";

        [MenuItem("Space & Crafts/Review Splat Iter1855 Custom Jump Land Motion")]
        public static void Capture()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var rigImporter = AssetImporter.GetAtPath(RigPath) as ModelImporter;
                if (rigImporter == null) throw new InvalidOperationException("Missing rig: " + RigPath);
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = true;
                rigImporter.SaveAndReimport();

                var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
                if (avatar == null) throw new InvalidOperationException("No valid Iter1855 Generic Avatar.");
                var clipImporter = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
                if (clipImporter == null) throw new InvalidOperationException("Missing retargeted custom motion: " + ClipPath);
                clipImporter.animationType = ModelImporterAnimationType.Generic;
                clipImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                clipImporter.sourceAvatar = avatar;
                clipImporter.importAnimation = true;
                clipImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                var clip = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                    .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (model == null || clip == null || material == null)
                    throw new InvalidOperationException("Could not load rig, clip, or ReviewOnly material.");

                var stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var baseRoot = PrefabUtility.InstantiatePrefab(model, stage) as GameObject;
                if (baseRoot == null) throw new InvalidOperationException("Could not instantiate Splat rig.");
                foreach (var renderer in baseRoot.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
                foreach (var animator in baseRoot.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                var renderers = baseRoot.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                var bounds = BoundsOf(renderers);
                baseRoot.transform.localScale *= 1.446f / Mathf.Max(.001f, bounds.size.y);
                bounds = BoundsOf(renderers);
                baseRoot.transform.position += Vector3.down * bounds.min.y;
                baseRoot.SetActive(false);
                var sampleParent = new GameObject("JumpLandRootOffset_NotSaved");
                sampleParent.transform.position = baseRoot.transform.position;

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "ReviewFloor_NotSaved";
                floor.transform.localScale = Vector3.one * 4f;
                floor.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(.28f, .31f, .36f));
                var camera = new GameObject("CandidateCamera_NotSaved").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .10f, .14f, 1f);
                camera.orthographic = true;
                camera.allowHDR = true;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 80f;
                camera.orthographicSize = 1.08f;
                AddLight(new Vector3(22f, 130f, -20f), .78f);
                AddLight(new Vector3(-35f, 12f, 50f), .30f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.23f, .25f, .29f);

                var phases = new[] { 0f, .12f, .25f, .38f, .52f, .66f, .80f, .94f };
                var sheet = new Texture2D(1536, 1536, TextureFormat.RGB24, false);
                var sampleNotes = new StringBuilder();
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var curvePaths = bindings.Select(b => b.path).Distinct().ToArray();
                var allPathsExist = curvePaths.All(p => string.IsNullOrEmpty(p) || baseRoot.transform.Find(p) != null);
                sampleNotes.AppendLine("Top-level local-position curves (values are source clip units):");
                foreach (var binding in bindings.Where(b => b.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal)
                    && (string.IsNullOrEmpty(b.path) || b.path.Split('/').Length <= 2)).OrderBy(b => b.path.Length).ThenBy(b => b.propertyName))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    sampleNotes.AppendLine("  path='" + binding.path + "' property=" + binding.propertyName + " values=["
                        + string.Join(", ", new[] { 0f, .25f, .50f, .75f, 1f }.Select(t => curve.Evaluate(clip.length * t).ToString("F3"))) + "]");
                }
                sampleNotes.AppendLine("Pose-only seating note: each frame is vertically offset by its aggregate renderer minimum so the silhouette stays visible. This intentionally removes the sampled root offset and cannot demonstrate root trajectory or sole contact.");
                for (var i = 0; i < phases.Length; i++)
                {
                    var sample = UnityEngine.Object.Instantiate(baseRoot, stage) as GameObject;
                    if (sample == null) throw new InvalidOperationException("Could not instantiate sample frame.");
                    sample.name = "JumpLandSample_NotSaved_" + i;
                    sample.transform.SetParent(sampleParent.transform, false);
                    sample.SetActive(true);
                    sample.transform.localPosition = Vector3.zero;
                    clip.SampleAnimation(sample, clip.length * phases[i]);
                    var b = BoundsOf(sample.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray());
                    sampleParent.transform.position += Vector3.down * b.min.y;
                    camera.transform.position = new Vector3(0f, .74f, 4.6f);
                    camera.transform.LookAt(new Vector3(0f, .74f, 0f));
                    var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
                    var tile = new Texture2D(512, 512, TextureFormat.RGB24, false);
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    tile.Apply();
                    sheet.SetPixels((i % 3) * 512, (2 - i / 3) * 512, 512, 512, tile.GetPixels());
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(tile);
                    sampleNotes.AppendLine("phase=" + phases[i].ToString("F2") + ", t=" + (clip.length * phases[i]).ToString("F3") + "s, aggregate bounds=" + b.size.ToString("F3") + "m, raw centerY=" + b.center.y.ToString("F3") + "m, raw lowestRendererY=" + b.min.y.ToString("F3") + "m, normalized rootWorldPosition=" + sample.transform.position.ToString("F3") + "m");
                    sampleParent.transform.position = baseRoot.transform.position;
                    UnityEngine.Object.DestroyImmediate(sample);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
                File.WriteAllBytes(OutputPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);

                var report = new StringBuilder();
                report.AppendLine("Splat Iter1855 custom Meshy jump/land review — Iter1856");
                report.AppendLine("Rig: " + RigPath + "; Avatar valid=" + avatar.isValid + ", type=Generic, human=" + avatar.isHuman + ".");
                report.AppendLine("Retargeted FBX: " + ClipPath + "; clip=" + clip.name + ", length=" + clip.length.ToString("F3") + "s, bindings=" + AnimationUtility.GetCurveBindings(clip).Length + ", pathsExist=" + allPathsExist + ".");
                report.AppendLine("Eight frame captures are direct AnimationClip.SampleAnimation pose samples under the Unity Editor renderer, not runtime Animator playback, production gameplay, or human play.");
                report.AppendLine("World-height values derive from aggregate Renderer.bounds and can be affected by animation/import bounds; they do not prove sole contact or root-motion behavior.");
                report.AppendLine(sampleNotes.ToString());
                report.AppendLine("Capture: " + OutputPath + ". Production prefab/controller/scene remain untouched. No build or gameplay test performed.");
                File.WriteAllText(ReportPath, report.ToString());
                Debug.Log(report.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static Bounds BoundsOf(Renderer[] rs)
        {
            if (rs == null || rs.Length == 0) throw new InvalidOperationException("No visible character renderers.");
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        private static Material MakeMaterial(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            SetColor(m, c);
            return m;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        private static void AddLight(Vector3 direction, float intensity)
        {
            var go = new GameObject("ReviewLight_NotSaved");
            go.transform.rotation = Quaternion.LookRotation(-direction.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, .94f, .88f);
        }
    }
}
#endif
