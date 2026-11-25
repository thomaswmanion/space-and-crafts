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
    /// <summary>Imports and stages the Iter1854 Meshy-rigged Splat and its paired walk/run motions in isolation.</summary>
    public static class SplatMeshyIter1854ReviewOnly
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1854";
        private const string ModelPath = Folder + "/Splat_Iter1854_Rigged.fbx";
        private const string WalkPath = Folder + "/Splat_Iter1854_Walk.fbx";
        private const string RunPath = Folder + "/Splat_Iter1854_Run.fbx";
        private const string ActionsPath = Folder + "/Splat_Iter1854_ActionBundle.fbx";
        private const string TexturePath = Folder + "/Splat_ProportionRefinement_Meshy71_textures/base_color.png";
        private const string ReferencePath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string ReviewFolder = Folder + "/ReviewOnly";
        private const string PrefabPath = ReviewFolder + "/Splat_Iter1854_Rigged_ReviewOnly.prefab";
        private const string MaterialPath = ReviewFolder + "/Splat_Iter1854_Rigged_ReviewOnly.mat";
        private const string ControllerPath = ReviewFolder + "/Splat_Iter1854_WalkRun_ReviewOnly.controller";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1854-reviewonly/import-and-motion-pose-review.txt";

        [MenuItem("Space & Crafts/Review Splat Meshy Iter1854 Rig and Walk Run")]
        public static void Prepare()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var path in new[] { ModelPath, WalkPath, RunPath, ActionsPath, TexturePath, ReferencePath })
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        throw new InvalidOperationException("Missing Iter1854 review source: " + path);
                EnsureFolder(ReviewFolder);
                var stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var rigImporter = RequireImporter(ModelPath);
                rigImporter.animationType = ModelImporterAnimationType.Generic;
                rigImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                rigImporter.importAnimation = true;
                rigImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>()
                    .FirstOrDefault(a => a != null && a.isValid);
                if (avatar == null) throw new InvalidOperationException("Meshy rig did not produce a valid Generic Avatar.");

                foreach (var path in new[] { WalkPath, RunPath, ActionsPath })
                {
                    var importer = RequireImporter(path);
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var walk = FindClip(WalkPath);
                var run = FindClip(RunPath);
                var actions = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<AnimationClip>()
                    .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
                var reference = AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (model == null || reference == null || texture == null || walk == null || run == null || actions.Length == 0)
                    throw new InvalidOperationException("Could not load rigged model, paired walk/run clips, reference, or texture.");

                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    { name = "Splat_Iter1854_Rigged_ReviewOnly" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                SetTexture(material, texture);
                EditorUtility.SetDirty(material);
                var controller = CreateController(walk, run, actions);
                var candidate = PrefabUtility.InstantiatePrefab(model, stage) as GameObject;
                var comparison = PrefabUtility.InstantiatePrefab(reference, stage) as GameObject;
                if (candidate == null || comparison == null) throw new InvalidOperationException("Candidate instantiation failed.");
                candidate.name = "Splat_Iter1854_Rigged_ReviewOnly";
                comparison.name = "Splat_TEMP_Reference_NotSaved";
                foreach (var animator in comparison.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                ApplyMaterial(candidate, material);
                var candidateRenderers = Renderers(candidate);
                var referenceRenderers = Renderers(comparison);
                if (candidateRenderers.Length == 0 || referenceRenderers.Length == 0)
                    throw new InvalidOperationException("A staged candidate has no renderer.");
                var candidateBounds = BoundsOf(candidateRenderers);
                var referenceBounds = BoundsOf(referenceRenderers);
                var scale = referenceBounds.size.y / Mathf.Max(.001f, candidateBounds.size.y);
                candidate.transform.localScale *= scale;
                candidateBounds = BoundsOf(candidateRenderers);
                candidate.transform.position += Vector3.down * candidateBounds.min.y;
                var animatorOnRoot = candidate.GetComponent<Animator>();
                if (animatorOnRoot == null) animatorOnRoot = candidate.AddComponent<Animator>();
                animatorOnRoot.avatar = avatar;
                animatorOnRoot.runtimeAnimatorController = controller;
                animatorOnRoot.applyRootMotion = false;
                animatorOnRoot.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var saved = PrefabUtility.SaveAsPrefabAsset(candidate, PrefabPath);
                if (saved == null) throw new InvalidOperationException("Could not save ReviewOnly prefab.");

                var report = new StringBuilder();
                report.AppendLine("Splat Meshy Iter1854 rigged candidate — ReviewOnly import and static motion diagnostics");
                report.AppendLine("The earlier static MeshFilter candidate is superseded for review by this rigged FBX candidate; the original generated FBX is retained as source.");
                report.AppendLine("No production prefab, controller, scene, or gameplay behavior was changed.");
                report.AppendLine("No build, game test, in-game playback, or human-play verification was performed.");
                report.AppendLine("Avatar: valid=" + avatar.isValid + ", human=" + avatar.isHuman + ", type=Generic");
                report.AppendLine("Rig hierarchy transforms=" + candidate.GetComponentsInChildren<Transform>(true).Length + ", skinned renderers=" + candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length + ", matched review scale=" + scale.ToString("F5") + ", candidate bounds before seat=" + candidateBounds.size.ToString("F3") + "m");
                report.AppendLine("Current Splat_TEMP renderer height=" + referenceBounds.size.y.ToString("F3") + "m. Candidate root is aggregate-bottom-seated to y=0; this is not evidence that both individual feet touch a platform.");
                report.AppendLine("Walk clip=" + ClipSummary(WalkPath, walk, avatar));
                report.AppendLine("Run clip=" + ClipSummary(RunPath, run, avatar));
                report.AppendLine("Action bundle=" + ActionsPath + "; clips=" + actions.Length + "; names=[" + string.Join(", ", actions.Select(c => c.name)) + "]");
                report.AppendLine("Static SampleAnimation results (not live Animator playback/contact proof):");
                AppendPoseDiagnostics(report, candidate, candidateRenderers, avatar, walk);
                AppendPoseDiagnostics(report, candidate, candidateRenderers, avatar, run);
                foreach (var action in actions) AppendPoseDiagnostics(report, candidate, candidateRenderers, avatar, action);
                report.AppendLine("Candidate prefab: " + PrefabPath);
                report.AppendLine("Review controller has a no-motion stand state, walk/run motion states driven by Speed, and one trigger per action-bundle clip; review only. No supplied idle or jump/land animation was assumed unless named in the bundle.");
                CaptureStills(candidate, comparison, referenceBounds.size.y);
                report.AppendLine("Candidate-only ReviewOnly pose stills saved under " + Path.GetDirectoryName(ReportPath) + "; Splat_TEMP was excluded because its nested visual pieces appeared vertically offset in the first comparison capture. The images are static editor-camera images, not live animation or gameplay evidence.");
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
                File.WriteAllText(ReportPath, report.ToString());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[Splat Iter1854] Paired rig/walk/run ReviewOnly candidate staged.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void CaptureStills(GameObject candidate, GameObject reference, float height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
            foreach (var animator in candidate.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var animator in reference.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            // Splat_TEMP has nested visual pieces that did not align with the top-level root in this temporary stage.
            reference.SetActive(false);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TemporaryReviewFloor_NotSaved";
            floor.transform.localScale = Vector3.one * 4f;
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            SetColor(floorMat, new Color(.22f, .25f, .30f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            AddLight(new Vector3(28, 145, -18), .72f);
            AddLight(new Vector3(-22, 160, 8), .25f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.16f, .17f, .19f);
            var cameraObject = new GameObject("ReviewCamera_NotSaved");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .12f, .16f, 1f);
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.nearClipPlane = .02f;
            camera.farClipPlane = 80f;
            foreach (var angle in new[] { ("front", 0f), ("three-quarter", 38f), ("side", 90f) })
            {
                candidate.transform.rotation = Quaternion.Euler(0, angle.Item2, 0);
                reference.transform.rotation = Quaternion.Euler(0, angle.Item2, 0);
                CaptureFrame(camera, candidate, "candidate_" + angle.Item1.Replace('-', '_'), height);
            }
        }

        private static void CaptureFrame(Camera camera, GameObject root, string file, float height)
        {
            var rs = Renderers(root);
            var bounds = BoundsOf(rs);
            camera.orthographicSize = Mathf.Max(.1f, height * .73f);
            var center = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
            camera.transform.position = center + new Vector3(0, height * .08f, 5f);
            camera.transform.LookAt(center);
            var target = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(768, 768, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(ReportPath)!, file + ".png"), image.EncodeToPNG());
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }

        private static ModelImporter RequireImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter
            ?? throw new InvalidOperationException("Not a model importer: " + path);
        private static AnimationClip FindClip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

        private static AnimatorController CreateController(AnimationClip walk, AnimationClip run, AnimationClip[] actions)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null) AssetDatabase.DeleteAsset(ControllerPath);
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = new[] { new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float } }
                .Concat(actions.Select((clip, index) => new AnimatorControllerParameter { name = "Action" + index, type = AnimatorControllerParameterType.Trigger })).ToArray();
            var machine = controller.layers[0].stateMachine;
            if (machine.states.Length > 0) machine.RemoveState(machine.states[0].state);
            var stand = machine.AddState("Stand — no supplied idle clip");
            var walking = machine.AddState("Walk"); walking.motion = walk;
            var running = machine.AddState("Run"); running.motion = run;
            machine.defaultState = stand;
            foreach (var item in actions.Select((clip, index) => (clip, index)))
            {
                var actionState = machine.AddState("Action " + item.clip.name);
                actionState.motion = item.clip;
                var enterAction = stand.AddTransition(actionState);
                enterAction.hasExitTime = false;
                enterAction.duration = .08f;
                enterAction.AddCondition(AnimatorConditionMode.If, 0f, "Action" + item.index);
                var returnToStand = actionState.AddTransition(stand);
                returnToStand.hasExitTime = true;
                returnToStand.exitTime = .96f;
                returnToStand.duration = .12f;
            }
            var toWalk = stand.AddTransition(walking); toWalk.hasExitTime = false; toWalk.duration = .15f; toWalk.AddCondition(AnimatorConditionMode.Greater, .05f, "Speed");
            var toStand = walking.AddTransition(stand); toStand.hasExitTime = false; toStand.duration = .15f; toStand.AddCondition(AnimatorConditionMode.Less, .05f, "Speed");
            var toRun = walking.AddTransition(running); toRun.hasExitTime = false; toRun.duration = .16f; toRun.AddCondition(AnimatorConditionMode.Greater, 1.7f, "Speed");
            var toWalkFromRun = running.AddTransition(walking); toWalkFromRun.hasExitTime = false; toWalkFromRun.duration = .16f; toWalkFromRun.AddCondition(AnimatorConditionMode.Less, 1.7f, "Speed");
            return controller;
        }

        private static void AppendPoseDiagnostics(StringBuilder report, GameObject rigRoot, Renderer[] renderers, Avatar avatar, AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var transformPaths = bindings.Where(b => b.type == typeof(Transform)).Select(b => b.path).Distinct().ToArray();
            var allPathsExist = transformPaths.All(path => string.IsNullOrEmpty(path) || rigRoot.transform.Find(path) != null);
            var sampleRoot = UnityEngine.Object.Instantiate(rigRoot);
            sampleRoot.name = "PoseSample_NotSaved";
            var sampleRenderers = Renderers(sampleRoot);
            var feet = sampleRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct().ToArray();
            var hips = sampleRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("Hips", StringComparison.OrdinalIgnoreCase));
            foreach (var fraction in new[] { .15f, .5f, .85f })
            {
                clip.SampleAnimation(sampleRoot, clip.length * fraction);
                var bounds = BoundsOf(sampleRenderers);
                var footText = feet.Length == 0 ? "none" : string.Join("; ", feet.Select(f => f.name + "=" + f.position.ToString("F3")));
                report.AppendLine($"  {clip.name}@{fraction:P0}: boundsMinY={bounds.min.y:F4}m, boundsSize={bounds.size:F3}m, hips={(hips == null ? "not found" : hips.localPosition.ToString("F3"))}, feet/toes[{feet.Length}]={footText}, transformPaths={transformPaths.Length}, allPathsExist={allPathsExist}");
            }
            UnityEngine.Object.DestroyImmediate(sampleRoot);
        }

        private static string ClipSummary(string path, AnimationClip clip, Avatar avatar)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var paths = bindings.Where(b => b.type == typeof(Transform)).Select(b => b.path).Distinct().ToArray();
            var hasAvatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Any(a => a != null && a.isValid);
            return $"{clip.name}, length={clip.length:F3}s, legacy={clip.legacy}, transformCurves={paths.Length}, clipHasValidAvatar={hasAvatar}, pairedAvatar={avatar.name}";
        }

        private static Material SetTexture(Material material, Texture2D texture)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            return material;
        }
        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
        private static void AddLight(Vector3 rotation, float intensity) { var go = new GameObject("ReviewLight_NotSaved"); var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = intensity; go.transform.rotation = Quaternion.Euler(rotation); }
        private static void ApplyMaterial(GameObject root, Material material) { foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray(); }
        private static Renderer[] Renderers(GameObject root) => root.GetComponentsInChildren<Renderer>(true).Where(r => r != null && r.enabled).ToArray();
        private static Bounds BoundsOf(Renderer[] rs) { var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b; }
        private static void EnsureFolder(string path) { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
#endif
