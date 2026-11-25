#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Isolated rig, walk/run, contact diagnostics and matched still comparison for Splat Iter1856.</summary>
    public static class SplatMeshyIter1856ReviewOnly
    {
        private const string Folder = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1856";
        private const string RigPath = Folder + "/Splat_Iter1856_Rigged.fbx";
        private const string WalkPath = Folder + "/Splat_Iter1856_Walk.fbx";
        private const string RunPath = Folder + "/Splat_Iter1856_Run.fbx";
        private const string ActionsPath = Folder + "/Splat_Iter1856_ActionBundle.fbx";
        private const string TexturePath = Folder + "/Splat_Iter1856_GameReady_base_color.png";
        private const string PreviousPrefab = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855/ReviewOnly/Splat_Iter1855_Rigged_ReviewOnly.prefab";
        private const string ReviewFolder = Folder + "/ReviewOnly";
        private const string PrefabPath = ReviewFolder + "/Splat_Iter1856_Rigged_ReviewOnly.prefab";
        private const string MaterialPath = ReviewFolder + "/Splat_Iter1856_Rigged_ReviewOnly.mat";
        private const string ControllerPath = ReviewFolder + "/Splat_Iter1856_WalkRun_ReviewOnly.controller";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1856-reviewonly/import-and-motion-pose-review.txt";
        private const string ComparePath = "Logs/score-comparison/splat-meshy-iter1856-reviewonly/splat-iter1855-vs-1856-matched-scale.png";
        private const int Width = 1536;
        private const int Height = 768;

        [MenuItem("Space & Crafts/Review Splat Meshy Iter1856 Rig and Walk Run")]
        public static void Prepare()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var path in new[] { RigPath, WalkPath, RunPath, ActionsPath, TexturePath, PreviousPrefab })
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        throw new InvalidOperationException("Missing Iter1856 review source: " + path);
                EnsureFolder(ReviewFolder);
                var stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var modelImporter = RequireImporter(RigPath);
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.importAnimation = true;
                modelImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
                if (avatar == null) throw new InvalidOperationException("Iter1856 rig did not produce a valid Avatar.");
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
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                var walk = FindClip(WalkPath);
                var run = FindClip(RunPath);
                var actions = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<AnimationClip>()
                    .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
                var prior = AssetDatabase.LoadAssetAtPath<GameObject>(PreviousPrefab);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (model == null || walk == null || run == null || prior == null || texture == null || actions.Length == 0)
                    throw new InvalidOperationException("Missing FBX model/clip, Iter1856 prefab or texture.");

                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "Splat_Iter1856_Rigged_ReviewOnly" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                ApplyTexture(material, texture);
                EditorUtility.SetDirty(material);
                var controller = MakeController(walk, run, actions);

                var candidate = PrefabUtility.InstantiatePrefab(model, stage) as GameObject;
                var comparison = PrefabUtility.InstantiatePrefab(prior, stage) as GameObject;
                if (candidate == null || comparison == null) throw new InvalidOperationException("Could not stage both candidates.");
                candidate.name = "Splat_Iter1856_Rigged_ReviewOnly";
                comparison.name = "Splat_Iter1856_Rigged_ReviewOnly_Comparison";
                foreach (var a in comparison.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                ApplyMaterial(candidate, material);
                var cr = Renderers(candidate);
                var pr = Renderers(comparison);
                if (cr.Length == 0 || pr.Length == 0) throw new InvalidOperationException("A staged candidate has no renderer.");
                var priorBounds = BoundsOf(pr);
                const float targetHeight = 1.446f;
                var newScale = targetHeight / Mathf.Max(.001f, BoundsOf(cr).size.y);
                var oldScale = targetHeight / Mathf.Max(.001f, priorBounds.size.y);
                candidate.transform.localScale *= newScale;
                comparison.transform.localScale *= oldScale;
                SeatAndPosition(candidate, cr, -0.92f);
                SeatAndPosition(comparison, pr, 0.92f);
                var animator = candidate.GetComponent<Animator>() ?? candidate.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (PrefabUtility.SaveAsPrefabAsset(candidate, PrefabPath) == null)
                    throw new InvalidOperationException("Could not save Iter1856 ReviewOnly prefab.");

                var report = new StringBuilder();
                report.AppendLine("Splat Meshy Iter1856 rigged candidate — isolated review");
                report.AppendLine("Iter1856 is the 30k-target smart-topology game-model candidate; Iter1855 is shown at identical rendered height 1.446m. Both roots are bottom-seated on one temporary floor and rendered in one GPU still.");
                report.AppendLine("No production prefab, controller, scene, or game behavior changed. This is not a build, gameplay test, animation playback, or human-play verification.");
                report.AppendLine("Avatar: valid=" + avatar.isValid + ", human=" + avatar.isHuman + ", type=Generic; transforms=" + candidate.GetComponentsInChildren<Transform>(true).Length + ", SkinnedMeshRenderers=" + candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                report.AppendLine("Iter1856 matched scale=" + newScale.ToString("F5") + ", aggregate bounds before seat=" + BoundsOf(cr).size.ToString("F3") + "m.");
                report.AppendLine("Iter1855 comparison matched scale=" + oldScale.ToString("F5") + ", aggregate bounds before seat=" + priorBounds.size.ToString("F3") + "m.");
                report.AppendLine("Walk=" + ClipSummary(walk) + "; Run=" + ClipSummary(run));
                report.AppendLine("Action bundle imported with Generic/CopyFromOther, pairedAvatar=" + avatar.name + "; clips=" + actions.Length + "; names=[" + string.Join(", ", actions.Select(c => c.name)) + "]");
                report.AppendLine("Review controller parameters matching SplatMeshyAnimatorDriver: MoveSpeed (float), Jump (trigger), Land (trigger), Victory (trigger), Personality (trigger), PersonalityIdle (int). Locomotion state is named Locomotion; Jump/Land, Victory and Personality action states map from bundle clips where present. This controller is ReviewOnly and not production-wired.");
                report.AppendLine("Static SampleAnimation diagnostics (not live playback proof):");
                AppendSamples(report, candidate, walk);
                AppendSamples(report, candidate, run);
                foreach (var action in actions) AppendSamples(report, candidate, action);
                report.AppendLine("Ground-contact limitation: the prefab root is seated from aggregate renderer bounds; the sampled foot/toe-weighted vertex minima are only regions influenced by those bones, not sole-specific geometry. Some values conflict with the rendered floor contact, so neither metric proves planted soles. Runtime Animator playback, varied platform contact and IK/foot correction remain unverified.");
                report.AppendLine("Review prefab: " + PrefabPath);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
                CapturePair(candidate, comparison);
                report.AppendLine("Matched-scale three-quarter comparison capture: " + ComparePath);
                report.AppendLine("Static sampled action-frame capture: " + CaptureActionFrames(model, actions) + ". This is a one-frame-per-clip diagnostic, not Animator playback or game footage.");
                File.WriteAllText(ReportPath, report.ToString());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[Splat Iter1856] rig/walk/run candidate and matched capture staged ReviewOnly.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void CapturePair(GameObject left, GameObject right)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ComparePath)!);
            foreach (var animator in left.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var animator in right.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            left.transform.rotation = right.transform.rotation = Quaternion.Euler(0, 38f, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TemporaryCompareFloor_NotSaved";
            floor.transform.localScale = Vector3.one * 4f;
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            SetColor(floorMat, new Color(.22f, .25f, .30f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            AddLight(new Vector3(28, 145, -18), .72f);
            AddLight(new Vector3(-22, 160, 8), .25f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.16f, .17f, .19f);
            var camera = new GameObject("MatchedCamera_NotSaved").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .12f, .16f, 1f);
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.nearClipPlane = .02f;
            camera.farClipPlane = 80f;
            camera.orthographicSize = 1.18f;
            camera.transform.position = new Vector3(0, .80f, 5.2f);
            camera.transform.LookAt(new Vector3(0, .72f, 0));
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            File.WriteAllBytes(ComparePath, image.EncodeToPNG());
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
            left.SetActive(false);
            right.SetActive(false);
        }

        private static void AppendSamples(StringBuilder report, GameObject source, AnimationClip clip)
        {
            var paths = AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(Transform)).Select(b => b.path).Distinct().ToArray();
            var sample = UnityEngine.Object.Instantiate(source);
            var renderers = Renderers(sample);
            var toes = sample.GetComponentsInChildren<Transform>(true).Where(t => t.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            foreach (var fraction in new[] { .15f, .5f, .85f })
            {
                clip.SampleAnimation(sample, clip.length * fraction);
                var bounds = BoundsOf(renderers);
                var soleSamples = SampleSoleVertices(sample);
                report.AppendLine($"  {clip.name}@{fraction:P0}: aggregateMinY={bounds.min.y:F4}m, bounds={bounds.size:F3}m, foot/toe bones=[{string.Join("; ", toes.Select(t => t.name + "=" + t.position.ToString("F3")))}], foot/toe-weighted vertices (not sole-specific)=[{soleSamples}], transformPaths={paths.Length}, allPathsExist={paths.All(p => string.IsNullOrEmpty(p) || source.transform.Find(p) != null)}");
            }
            UnityEngine.Object.DestroyImmediate(sample);
        }

        private static string SampleSoleVertices(GameObject root)
        {
            var results = new System.Collections.Generic.List<string>();
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null || renderer.bones == null) continue;
                var mesh = renderer.sharedMesh;
                var weights = mesh.boneWeights;
                if (weights == null || weights.Length != mesh.vertexCount) continue;
                var baked = new Mesh { name = "SplatSoleDiagnostic_NotSaved" };
                renderer.BakeMesh(baked, true);
                var vertices = baked.vertices;
                foreach (var side in new[] { "Left", "Right" })
                {
                    var boneIndices = Enumerable.Range(0, renderer.bones.Length)
                        .Where(i => renderer.bones[i] != null && renderer.bones[i].name.StartsWith(side, StringComparison.OrdinalIgnoreCase)
                            && (renderer.bones[i].name.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) >= 0
                                || renderer.bones[i].name.IndexOf("Toe", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    if (boneIndices.Length == 0) { results.Add(side + "=no foot/toe bone"); continue; }
                    var selected = new System.Collections.Generic.List<float>();
                    for (var v = 0; v < weights.Length && v < vertices.Length; v++)
                    {
                        var bw = weights[v];
                        var total = 0f;
                        if (boneIndices.Contains(bw.boneIndex0)) total += bw.weight0;
                        if (boneIndices.Contains(bw.boneIndex1)) total += bw.weight1;
                        if (boneIndices.Contains(bw.boneIndex2)) total += bw.weight2;
                        if (boneIndices.Contains(bw.boneIndex3)) total += bw.weight3;
                        if (total >= .35f) selected.Add(renderer.transform.TransformPoint(vertices[v]).y);
                    }
                    results.Add(side + "minY=" + (selected.Count == 0 ? "no weighted verts" : selected.Min().ToString("F4") + "m, count=" + selected.Count));
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return results.Count == 0 ? "unavailable (no legacy skin weights/renderer)" : string.Join("; ", results);
        }

        private static string CaptureActionFrames(GameObject source, AnimationClip[] actions)
        {
            var samples = new[]
            {
                (label: "Idle 50%", clip: FindAction(actions, "Idle"), fraction: .50f),
                (label: "Jumping_Down 35%", clip: FindAction(actions, "Jumping_Down", "Jumping Down"), fraction: .35f),
                (label: "Regular_Jump 50%", clip: FindAction(actions, "Regular_Jump", "Regular Jump", "Jump"), fraction: .50f)
            };
            if (samples.Any(s => s.clip == null))
                return "not captured: one of Idle, Jumping_Down, or Regular_Jump was not present in the bundle";
            var stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (model == null || material == null) return "not captured: rigged model or ReviewOnly material could not be loaded";
            var baseRoot = PrefabUtility.InstantiatePrefab(model, stage) as GameObject;
            if (baseRoot == null) return "not captured: could not instantiate the rigged model";
            ApplyMaterial(baseRoot, material);
            var baseRenderers = Renderers(baseRoot);
            var scale = 1.446f / Mathf.Max(.001f, BoundsOf(baseRenderers).size.y);
            baseRoot.transform.localScale *= scale;
            var initialBounds = BoundsOf(baseRenderers);
            baseRoot.transform.position += Vector3.down * initialBounds.min.y;
            baseRoot.SetActive(false);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ActionFrameFloor_NotSaved";
            floor.transform.localScale = Vector3.one * 4f;
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            SetColor(floorMat, new Color(.22f, .25f, .30f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            AddLight(new Vector3(28, 145, -18), .72f);
            AddLight(new Vector3(-22, 160, 8), .25f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.16f, .17f, .19f);

            var camera = new GameObject("SampledActionCamera_NotSaved").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .12f, .16f, 1f);
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.nearClipPlane = .02f;
            camera.farClipPlane = 80f;
            camera.orthographicSize = 1.18f;
            var sheet = new Texture2D(1536, 512, TextureFormat.RGB24, false);
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = UnityEngine.Object.Instantiate(baseRoot, stage) as GameObject;
                if (sample == null) throw new InvalidOperationException("Could not instantiate sampled action frame.");
                sample.name = "SampledAction_NotSaved_" + i;
                sample.SetActive(true);
                sample.transform.position = new Vector3(0f, baseRoot.transform.position.y, 0f);
                foreach (var animator in sample.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                var clip = samples[i].clip;
                clip.SampleAnimation(sample, clip.length * samples[i].fraction);
                var b = BoundsOf(Renderers(sample));
                var center = new Vector3(0f, b.center.y, 0f);
                camera.transform.position = center + new Vector3(0f, .06f, 5f);
                camera.transform.LookAt(center);
                var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
                var tile = new Texture2D(512, 512, TextureFormat.RGB24, false);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                tile.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                tile.Apply();
                sheet.SetPixels(i * 512, 0, 512, 512, tile.GetPixels());
                camera.targetTexture = null;
                RenderTexture.active = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(tile);
                UnityEngine.Object.DestroyImmediate(sample);
            }
            sheet.Apply();
            var path = "Logs/score-comparison/splat-meshy-iter1855-reviewonly/splat-iter1855-sampled-idle-jump-land.png";
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            return path;
        }

        private static AnimatorController MakeController(AnimationClip walk, AnimationClip run, AnimationClip[] actions)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = "MoveSpeed", type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = "Jump", type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = "Land", type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = "Victory", type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = "Personality", type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = "PersonalityIdle", type = AnimatorControllerParameterType.Int }
            };
            var machine = controller.layers[0].stateMachine;
            if (machine.states.Length > 0) machine.RemoveState(machine.states[0].state);
            var locomotion = machine.AddState("Locomotion");
            var idle = FindAction(actions, "Idle");
            var blend = new BlendTree { name = "Iter1856 Idle Walk Run", blendType = BlendTreeType.Simple1D, blendParameter = "MoveSpeed", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            blend.AddChild(idle ?? walk, 0f);
            blend.AddChild(walk, 0.45f);
            blend.AddChild(run, 2f);
            AssetDatabase.AddObjectToAsset(blend, controller);
            locomotion.motion = blend;
            machine.defaultState = locomotion;
            var jump = FindAction(actions, "Regular_Jump", "Regular Jump", "happy_jump_m", "Happy_Jump_M", "Jump");
            var land = FindAction(actions, "Jumping_Down", "Jumping Down", "Vault_and_Land", "Vault and Land", "Land");
            var victory = FindAction(actions, "Victory_Cheer", "Victory Cheer", "Motivational_Cheer", "Motivational Cheer");
            var personality = FindAction(actions, "Big_Wave_Hello", "Big Wave Hello", "Personality");
            var jumpState = jump == null ? null : machine.AddState("Jump");
            if (jumpState != null) jumpState.motion = jump;
            var landState = land == null ? null : machine.AddState("Land");
            if (landState != null) { landState.motion = land; landState.speed = 2.6f; }
            var victoryState = victory == null ? null : machine.AddState("Victory");
            if (victoryState != null) victoryState.motion = victory;
            var personalityState = personality == null ? null : machine.AddState("Personality");
            if (personalityState != null) personalityState.motion = personality;
            if (jumpState != null)
            {
                var toJump = locomotion.AddTransition(jumpState); toJump.hasExitTime = false; toJump.duration = .08f; toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
                if (landState != null)
                {
                    var toLand = jumpState.AddTransition(landState); toLand.hasExitTime = false; toLand.duration = .07f; toLand.offset = .50f; toLand.AddCondition(AnimatorConditionMode.If, 0f, "Land");
                    var finishLand = landState.AddTransition(locomotion); finishLand.hasExitTime = true; finishLand.exitTime = .94f; finishLand.duration = .12f;
                }
            }
            if (victoryState != null)
            {
                var toVictory = locomotion.AddTransition(victoryState); toVictory.hasExitTime = false; toVictory.duration = .10f; toVictory.AddCondition(AnimatorConditionMode.If, 0f, "Victory");
                var finishVictory = victoryState.AddTransition(locomotion); finishVictory.hasExitTime = true; finishVictory.exitTime = .96f; finishVictory.duration = .12f;
            }
            if (personalityState != null)
            {
                var toPersonality = locomotion.AddTransition(personalityState); toPersonality.hasExitTime = false; toPersonality.duration = .10f; toPersonality.AddCondition(AnimatorConditionMode.If, 0f, "Personality");
                var finishPersonality = personalityState.AddTransition(locomotion); finishPersonality.hasExitTime = true; finishPersonality.exitTime = .96f; finishPersonality.duration = .12f;
            }
            return controller;
        }

        private static AnimationClip FindAction(AnimationClip[] actions, params string[] names) => actions.FirstOrDefault(c => names.Any(n => string.Equals(c.name, n, StringComparison.OrdinalIgnoreCase)));

        private static string ClipSummary(AnimationClip clip) => $"{clip.name} ({clip.length:F3}s, {AnimationUtility.GetCurveBindings(clip).Count(b => b.type == typeof(Transform))} transform curves)";
        private static AnimationClip FindClip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        private static ModelImporter RequireImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter ?? throw new InvalidOperationException("Not a ModelImporter: " + path);
        private static Renderer[] Renderers(GameObject root) => root.GetComponentsInChildren<Renderer>(true).Where(r => r != null && r.enabled).ToArray();
        private static Bounds BoundsOf(Renderer[] rs) { var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b; }
        private static void SeatAndPosition(GameObject root, Renderer[] rs, float x) { var b = BoundsOf(rs); root.transform.position += Vector3.down * b.min.y; root.transform.position = new Vector3(x, root.transform.position.y, 0f); }
        private static void ApplyMaterial(GameObject root, Material m) { foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = Enumerable.Repeat(m, Mathf.Max(1, r.sharedMaterials.Length)).ToArray(); }
        private static void ApplyTexture(Material m, Texture2D t) { if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t); if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white); if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white); }
        private static void SetColor(Material m, Color c) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c); }
        private static void AddLight(Vector3 rotation, float intensity) { var go = new GameObject("ReviewLight_NotSaved"); var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = intensity; go.transform.rotation = Quaternion.Euler(rotation); }
        private static void EnsureFolder(string path) { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
#endif
