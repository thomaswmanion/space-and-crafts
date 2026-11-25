#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Creates an isolated review prefab and pose sheet for the Meshy Vesper candidate.</summary>
    public static class VesperMeshyIter1872Review
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Vesper/Meshy_Iter1872";
        private const string Review = Folder + "/ReviewOnly";
        private const string Rig = Folder + "/Rigged/Vesper_Iter1872_Rigged.fbx";
        // The rig was built from the 250k remesh, whose UV layout has its own maps.
        private const string Texture = Folder + "/Vesper_Iter1872_250k_textures/base_color.png";
        private static readonly string[] ClipFiles =
        {
            Folder + "/Rigged/Vesper_Iter1872_Walk.fbx",
            Folder + "/Rigged/Vesper_Iter1872_Run.fbx",
            Folder + "/AnimationCandidates/Vesper_Iter1872_MapPoint.fbx",
            Folder + "/AnimationCandidates/Vesper_Iter1872_CatalogActions.fbx"
        };
        private const string Material = Review + "/Vesper_Iter1872_ReviewOnly.mat";
        private const string Controller = Review + "/Vesper_Iter1872_ReviewOnly.controller";
        private const string Prefab = Review + "/Vesper_Iter1872_ReviewOnly.prefab";
        private const string Capture = "Logs/score-comparison/vesper-meshy-iter1872/animator-review.png";
        private const string Report = "Logs/score-comparison/vesper-meshy-iter1872/animator-review.txt";
        private const int Tile = 448;
        private const int Columns = 4;

        public static void PrepareAndCapture()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadMainAssetAtPath(Rig) == null || AssetDatabase.LoadMainAssetAtPath(Texture) == null)
                    throw new InvalidOperationException("Missing Vesper candidate or base-color map.");
                foreach (var path in ClipFiles)
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null) throw new InvalidOperationException("Missing animation: " + path);

                Directory.CreateDirectory(Review);
                var rigImporter = RequireImporter(Rig);
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = false;
                rigImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(Rig).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
                if (avatar == null) throw new InvalidOperationException("The Meshy Vesper rig did not import a valid Avatar.");
                foreach (var path in ClipFiles)
                {
                    var importer = RequireImporter(path);
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var clips = ClipFiles.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(c => c.name, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                    .OrderBy(c => c.name.Equals("Idle", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(c => c.name, StringComparer.OrdinalIgnoreCase).ToArray();
                if (clips.Length == 0) throw new InvalidOperationException("No Vesper candidate clips imported.");
                var controller = AnimatorController.CreateAnimatorControllerAtPath(Controller);
                var machine = controller.layers[0].stateMachine;
                var states = clips.Select(c => { var s = machine.AddState(c.name); s.motion = c; return s; }).ToArray();
                machine.defaultState = states.FirstOrDefault(s => s.name.Equals("Idle", StringComparison.OrdinalIgnoreCase)) ?? states[0];
                EditorUtility.SetDirty(controller);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Rig);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Texture);
                if (model == null || texture == null) throw new InvalidOperationException("Vesper model or texture failed to load.");
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Material);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "Vesper_Iter1872_ReviewOnly" };
                    AssetDatabase.CreateAsset(mat, Material);
                }
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(mat);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var actor = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (actor == null) throw new InvalidOperationException("Could not instantiate Vesper in review scene.");
                actor.name = "Vesper_Iter1872_ReviewOnly";
                foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = mat;
                var renderers = actor.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                var bounds = BoundsOf(renderers);
                actor.transform.localScale *= 1.65f / Mathf.Max(.001f, bounds.size.y);
                bounds = BoundsOf(renderers);
                actor.transform.position = new Vector3(0, -bounds.min.y, 0);
                var animator = actor.GetComponentInChildren<Animator>(true) ?? actor.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                AddLight(new Vector3(30, 110, -25), .9f);
                AddLight(new Vector3(-25, 30, 50), .35f);
                var camera = new GameObject("VesperReviewCamera_NotSaved").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .10f, .14f, 1);
                camera.orthographic = true;
                camera.orthographicSize = .98f;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 80;
                camera.transform.position = new Vector3(0, .83f, 4.8f);
                camera.transform.LookAt(new Vector3(0, .78f, 0));

                var samples = clips.SelectMany(c => new[] { .22f, .50f, .78f }.Select(t => Tuple.Create(c, t))).ToArray();
                var rows = Mathf.CeilToInt(samples.Length / (float)Columns);
                var sheet = new Texture2D(Columns * Tile, rows * Tile, TextureFormat.RGB24, false);
                var report = new StringBuilder();
                report.AppendLine("Vesper Iter1872 isolated Meshy Unity Editor Animator pose sheet; ReviewOnly.");
                report.AppendLine("Source: project VesperSignalPortrait.png → Meshy 7.1 textured model → 250k-target triangle remesh → Generic rig; free Walk/Run; five catalog clips and custom map-pointing motion.");
                report.AppendLine("Not production-scene integration, camera-sequencing review, gameplay, formal test, build or human play. Root motion disabled for this static review scene.");
                report.AppendLine("Avatar valid=" + animator.avatar.isValid + "; clips=" + clips.Length + "; character sampled height=" + bounds.size.y.ToString("F3") + "m.");
                for (var i = 0; i < samples.Length; i++)
                {
                    var clip = samples[i].Item1;
                    var time = samples[i].Item2;
                    animator.Play(clip.name, 0, time);
                    animator.Update(.0166667f);
                    var target = new RenderTexture(Tile, Tile, 24, RenderTextureFormat.ARGB32);
                    var tile = new Texture2D(Tile, Tile, TextureFormat.RGB24, false);
                    camera.targetTexture = target;
                    camera.Render();
                    var poseBounds = BoundsOf(renderers);
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, Tile, Tile), 0, 0);
                    tile.Apply();
                    sheet.SetPixels((i % Columns) * Tile, (rows - 1 - i / Columns) * Tile, Tile, Tile, tile.GetPixels());
                    report.AppendLine(clip.name + ": " + time.ToString("F2") + " normalized; aggregate renderer height=" + poseBounds.size.y.ToString("F3") + "m; transform curves=" + AnimationUtility.GetCurveBindings(clip).Length + ".");
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(tile);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Capture)!);
                File.WriteAllBytes(Capture, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                File.WriteAllText(Report, report.ToString());
                PrefabUtility.SaveAsPrefabAsset(actor, Prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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

        private static ModelImporter RequireImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter
            ?? throw new InvalidOperationException("Expected ModelImporter: " + path);

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            if (renderers.Length == 0) throw new InvalidOperationException("Vesper has no enabled renderers.");
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        private static void AddLight(Vector3 euler, float intensity)
        {
            var go = new GameObject("VesperReviewLight_NotSaved");
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
        }
    }
}
#endif
