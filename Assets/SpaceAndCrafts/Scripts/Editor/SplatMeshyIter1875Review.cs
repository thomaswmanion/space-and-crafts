#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Imports and renders the Iter1875 action expansion on Splat's Iter1869 avatar.</summary>
    public static class SplatMeshyIter1875Review
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1869";
        private const string ReviewFolder = Folder + "/ReviewOnly_Iter1875";
        private const string RigPath = Folder + "/Rigged/Splat_Iter1869_Rigged.fbx";
        private const string TexturePath = Folder + "/Splat_Iter1869_250k_textures/base_color.png";
        private const string WalkPath = Folder + "/Rigged/Splat_Iter1869_Walk.fbx";
        private const string RunPath = Folder + "/Rigged/Splat_Iter1869_Run.fbx";
        private const string NewActionsPath = Folder + "/AnimationCandidates/Iter1875/Splat_Iter1875_Actions.fbx";
        private const string CelebrationPath = Folder + "/AnimationCandidates/Iter1875/Splat_Iter1875_InventionCelebration.fbx";
        private static readonly string[] ActionPaths = { NewActionsPath, CelebrationPath };
        private const string MaterialPath = ReviewFolder + "/Splat_Iter1875_Actions_ReviewOnly.mat";
        private const string ControllerPath = ReviewFolder + "/Splat_Iter1875_Actions_ReviewOnly.controller";
        private const string PrefabPath = ReviewFolder + "/Splat_Iter1875_Actions_ReviewOnly.prefab";
        private const string CapturePath = "Logs/score-comparison/splat-meshy-iter1875-actions/animator-review.png";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1875-actions/animator-review.txt";
        private const int TileSize = 512;
        private const int Columns = 4;

        public static void PrepareAndCapture()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var path in new[] { RigPath, TexturePath, WalkPath, RunPath }.Concat(ActionPaths))
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        throw new InvalidOperationException("Missing Splat action review asset: " + path);
                Directory.CreateDirectory(ReviewFolder);

                var rigImporter = RequireImporter(RigPath);
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = false;
                rigImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
                if (avatar == null) throw new InvalidOperationException("Splat Iter1869 Meshy rig did not import a valid Avatar.");

                var clipPaths = new[] { WalkPath, RunPath }.Concat(ActionPaths).ToArray();
                foreach (var path in clipPaths)
                {
                    var importer = RequireImporter(path);
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var clips = clipPaths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(clip => clip.name, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToArray();
                if (clips.Length == 0) throw new InvalidOperationException("Meshy FBXs imported without animation clips.");

                var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                var stateMachine = controller.layers[0].stateMachine;
                var ordered = clips.OrderBy(clip => clip.name.Equals("Idle", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(clip => clip.name, StringComparer.OrdinalIgnoreCase).ToArray();
                var states = new List<AnimatorState>();
                foreach (var clip in ordered)
                {
                    var state = stateMachine.AddState(clip.name);
                    state.motion = clip;
                    states.Add(state);
                }
                stateMachine.defaultState = states[0];
                EditorUtility.SetDirty(controller);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (model == null || texture == null) throw new InvalidOperationException("Rigged mesh or base-color texture could not be loaded.");
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"))
                    { name = "Splat_Iter1875_Actions_ReviewOnly" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(material);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var splat = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (splat == null) throw new InvalidOperationException("Could not stage the Iter1869 rigged model.");
                splat.name = "Splat_Iter1875_Actions_ReviewOnly";
                foreach (var renderer in splat.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
                var renderers = splat.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
                var bounds = BoundsOf(renderers);
                var scale = 1.446f / Mathf.Max(.001f, bounds.size.y);
                splat.transform.localScale *= scale;
                bounds = BoundsOf(renderers);
                splat.transform.position = new Vector3(0f, -bounds.min.y, 0f);
                var animator = splat.GetComponentInChildren<Animator>(true) ?? splat.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (PrefabUtility.SaveAsPrefabAsset(splat, PrefabPath) == null)
                    throw new InvalidOperationException("Could not save the isolated ReviewOnly prefab.");

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "DiagnosticSupport_NotSaved";
                floor.transform.position = new Vector3(0f, -.06f, 0f);
                floor.transform.localScale = new Vector3(2.2f, .12f, 1.4f);
                var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                SetColor(floorMaterial, new Color(.16f, .20f, .26f));
                floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.25f, .27f, .30f);
                AddLight(new Vector3(25f, 125f, -18f), .85f);
                AddLight(new Vector3(-30f, 18f, 48f), .32f);
                var camera = new GameObject("ReviewCamera_NotSaved").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .10f, .14f, 1f);
                camera.orthographic = true;
                camera.allowHDR = true;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 80f;
                camera.orthographicSize = 1.10f;
                camera.transform.position = new Vector3(0f, .76f, 4.6f);
                camera.transform.LookAt(new Vector3(0f, .20f, 0f));

                var samples = new List<Tuple<AnimationClip, float>>();
                var actionExpansionClips = ActionPaths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()).ToArray();
                foreach (var clip in ordered)
                {
                    var times = actionExpansionClips.Any(candidate => string.Equals(candidate.name, clip.name, StringComparison.OrdinalIgnoreCase))
                        ? new[] { .12f, .30f, .50f, .70f, .88f }
                        : new[] { .42f };
                    foreach (var time in times) samples.Add(Tuple.Create(clip, time));
                }
                var rows = Mathf.CeilToInt(samples.Count / (float)Columns);
                var sheet = new Texture2D(Columns * TileSize, rows * TileSize, TextureFormat.RGB24, false);
                var report = new StringBuilder();
                report.AppendLine("Splat Iter1875 action-expansion Animator capture on the genuine Iter1869 Meshy Generic avatar.");
                report.AppendLine("Source: approved-design three-view Meshy 7.1 Splat; 250,000-target-triangle remesh (actual output face count not reported), preserved PBR textures, genuine rig, free Walk/Run, prior action bundle, Iter1875 catalog action expansion, and one custom invention-completion celebration.");
                report.AppendLine("The ten catalog expansion clips and custom celebration are sampled at five normalized times each with root motion disabled; this is an isolated pose review, not motor-driven gameplay.");
                report.AppendLine("This isolated editor Animator preview is not game Play Mode or proof of motor timing, actual sole contact, varied-platform behavior, build, test, or human play.");
                report.AppendLine("Animator Avatar valid=" + animator.avatar.isValid + "; root motion=" + animator.applyRootMotion + "; rendered character height=" + bounds.size.y.ToString("F3") + "m.");

                for (var i = 0; i < samples.Count; i++)
                {
                    var sampledClip = samples[i].Item1;
                    var normalizedTime = samples[i].Item2;
                    animator.Play(sampledClip.name, 0, normalizedTime);
                    animator.Update(.0166667f);
                    var target = new RenderTexture(TileSize, TileSize, 24, RenderTextureFormat.ARGB32);
                    var tile = new Texture2D(TileSize, TileSize, TextureFormat.RGB24, false);
                    camera.targetTexture = target;
                    camera.Render();
                    var poseBounds = BoundsOf(renderers);
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, TileSize, TileSize), 0, 0);
                    tile.Apply();
                    sheet.SetPixels((i % Columns) * TileSize, (rows - 1 - i / Columns) * TileSize, TileSize, TileSize, tile.GetPixels());
                    report.AppendLine(sampledClip.name + ": duration=" + sampledClip.length.ToString("F2") + "s; aggregateMinY=" + poseBounds.min.y.ToString("F3") + "m; aggregateHeight=" + poseBounds.size.y.ToString("F3") + "m; transformCurves=" + AnimationUtility.GetCurveBindings(sampledClip).Length + "; normalized sample=" + normalizedTime.ToString("F2") + ".");
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(tile);
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(CapturePath)!);
                File.WriteAllBytes(CapturePath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                File.WriteAllText(ReportPath, report.ToString());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(report.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static ModelImporter RequireImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Expected FBX ModelImporter at " + path);
            return importer;
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) throw new InvalidOperationException("No visible renderers on the Meshy character.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void AddLight(Vector3 euler, float intensity)
        {
            var go = new GameObject("ReviewLight_NotSaved");
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
#endif

