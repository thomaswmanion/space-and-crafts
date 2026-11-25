#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds an isolated review prefab/controller from the existing Adhesive
    /// Cartographer Meshy FBX bundle. It deliberately does not edit a scene,
    /// attach combat AI, or promote the model into production content.
    /// </summary>
    public static class AdhesiveCartographerReviewOnlyAuthoringIter2069
    {
        private const string SourcePath =
            "Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/AdhesiveCartographer_AnimationSet.fbx";
        private const string WalkPath =
            "Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/AdhesiveCartographer_Walk.fbx";
        private const string RunPath =
            "Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/AdhesiveCartographer_Run.fbx";
        private const string AvatarSourcePath =
            "Assets/SpaceAndCrafts/Art/Characters/Vesper/Meshy_Iter1872/Rigged/Vesper_Iter1872_Rigged.fbx";
        private const string TexturePath =
            "Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/AdhesiveCartographer_Textured_base_color.png";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/UnityCandidate_ReviewOnly_Iter2069";
        private const string ControllerPath = OutputFolder + "/AdhesiveCartographer_ReviewOnly.controller";
        private const string PrefabPath = OutputFolder + "/AdhesiveCartographer_ReviewOnly.prefab";
        private const string MaterialPath = OutputFolder + "/AdhesiveCartographer_ReviewOnly.mat";
        private const string ReportPath = OutputFolder + "/import-structure-review.txt";

        [MenuItem("Space & Crafts/Author Adhesive Cartographer ReviewOnly (Iter 2069)")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                throw new InvalidOperationException(
                    "Adhesive Cartographer Iter2069 ReviewOnly output exists; refusing to overwrite it.");

            ConfigureClipLoops(SourcePath, name => EndsWithToken(name, "Idle"));
            ConfigureClipLoops(WalkPath, name => EndsWithToken(name, "Walk"));
            ConfigureClipLoops(RunPath, name => EndsWithToken(name, "Run"));

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null) throw new FileNotFoundException("Missing animation-bundle FBX", SourcePath);

            var sourceAssets = AssetDatabase.LoadAllAssetsAtPath(SourcePath);
            // The animation FBX's importer metadata explicitly references this
            // Meshy rig. Its own embedded Avatar was invalid in the first
            // headless authoring pass, so use the declared source asset directly.
            var avatar = AssetDatabase.LoadAllAssetsAtPath(AvatarSourcePath)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null)
                throw new InvalidOperationException($"No valid Avatar found in the FBX declared by the action bundle ({AvatarSourcePath}); leave the candidate unpromoted.");

            var idle = RequireClip(SourcePath, "Idle");
            var alert = RequireClip(SourcePath, "Alert");
            var attack = RequireClip(SourcePath, "Attack");
            var rollDodge = RequireClip(SourcePath, "Roll Dodge");
            var hit = RequireClip(SourcePath, "Hit Reaction");
            var dead = RequireClip(SourcePath, "Dead");
            var standUp = RequireClip(SourcePath, "Stand Up 1");
            var walk = RequireClip(WalkPath, "Walk");
            var run = RequireClip(RunPath, "Run");
            var reviewClips = new[] { idle, alert, walk, run, attack, rollDodge, hit, dead, standUp };
            var bindingReport = BuildBindingCoverageReport(source.transform, reviewClips, out var exactBindings);

            EnsureFolder(OutputFolder);
            if (!exactBindings)
            {
                File.WriteAllText(ReportPath, BuildImportReport(sourceAssets, avatar, reviewClips, bindingReport)
                    + "\nSTOP: one or more clips have missing transform paths; no review prefab/controller was created.\n", Encoding.UTF8);
                AssetDatabase.Refresh();
                throw new InvalidOperationException("One or more clips do not target the exact imported rig hierarchy. See " + ReportPath);
            }

            var reviewMaterial = CreateReviewMaterial();
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("RollDodge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Recover", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, new Vector3(100, 0));
            var walkState = AddState(machine, "Walk", walk, new Vector3(300, 0));
            var runState = AddState(machine, "Run", run, new Vector3(500, 0));
            var alertState = AddState(machine, "Alert", alert, new Vector3(700, -180));
            var attackState = AddState(machine, "Attack", attack, new Vector3(700, -60));
            var dodgeState = AddState(machine, "Roll Dodge", rollDodge, new Vector3(700, 60));
            var hitState = AddState(machine, "Hit Reaction", hit, new Vector3(700, 180));
            var deadState = AddState(machine, "Dead", dead, new Vector3(700, 300));
            var standUpState = AddState(machine, "Stand Up", standUp, new Vector3(950, 300));
            machine.defaultState = idleState;

            AddFloat(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloat(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddFloat(walkState, runState, "MoveSpeed", 1.25f, AnimatorConditionMode.Greater);
            AddFloat(runState, walkState, "MoveSpeed", 1.25f, AnimatorConditionMode.Less);
            AddTrigger(machine, alertState, "Alert");
            AddTrigger(machine, attackState, "Attack");
            AddTrigger(machine, dodgeState, "RollDodge");
            AddTrigger(machine, hitState, "Hit");
            AddTrigger(machine, deadState, "Defeat");
            AddTrigger(deadState, standUpState, "Recover");
            AddExit(alertState, idleState, 0.94f);
            AddExit(attackState, idleState, 0.94f);
            AddExit(dodgeState, idleState, 0.94f);
            AddExit(hitState, idleState, 0.88f);
            AddExit(standUpState, idleState, 0.94f);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate source FBX for review prefab.");
            try
            {
                instance.name = "AdhesiveCartographer_ReviewOnly_Iter2069";
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0) throw new InvalidOperationException("Animation bundle prefab has no SkinnedMeshRenderer.");
                foreach (var skin in skins) skin.sharedMaterial = reviewMaterial;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath, BuildImportReport(sourceAssets, avatar, reviewClips, bindingReport), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[AdhesiveCartographer Iter2069] isolated ReviewOnly prefab/controller authored at {OutputFolder}; no production scene or AI modified.");
        }

        private static AnimationClip RequireClip(string path, string suffix)
        {
            var normalizedSuffix = NormalizeClipName(suffix);
            var availableClips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(candidate => candidate != null && !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var clip = availableClips.FirstOrDefault(candidate => NormalizeClipName(candidate.name).EndsWith(
                    normalizedSuffix, StringComparison.Ordinal));
            // Meshy names its single-clip locomotion files after the source
            // motion (for example, "walking_man|baselayer"), not Walk/Run.
            // The asset path supplies the state meaning; preserve the actual
            // imported clip name in the report.
            if (clip == null && availableClips.Length == 1 && (path == WalkPath || path == RunPath))
                clip = availableClips[0];
            if (clip == null)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var names = availableClips.Length == 0 ? "none" : string.Join(", ", availableClips.Select(candidate => candidate.name));
                throw new InvalidOperationException($"Required clip ending '{suffix}' was not imported from {path}. Available clips: {names}; importAnimation={importer?.importAnimation.ToString() ?? "missing"}, avatarSetup={importer?.avatarSetup.ToString() ?? "missing"}.");
            }
            if (clip.length <= 0f || AnimationUtility.GetCurveBindings(clip).Length == 0)
                throw new InvalidOperationException($"Clip '{clip.name}' is empty or has no transform bindings; do not promote this asset.");
            return clip;
        }

        private static string NormalizeClipName(string value) =>
            new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        private static Material CreateReviewMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new FileNotFoundException("Missing Meshy base-color texture", TexturePath);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find URP/Lit or Standard shader for isolated review material.");
            var material = new Material(shader) { name = "AdhesiveCartographer_ReviewOnly" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.24f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.24f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void ConfigureClipLoops(string path, Func<string, bool> shouldLoop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing model importer", path);
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            var changed = false;
            foreach (var clip in clips)
            {
                var loop = shouldLoop(clip.name);
                if (clip.loopTime == loop && clip.loopPose == loop) continue;
                clip.loopTime = loop;
                clip.loopPose = loop;
                changed = true;
            }
            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static bool EndsWithToken(string value, string token) =>
            value.Equals(token, StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("|" + token, StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("_" + token, StringComparison.OrdinalIgnoreCase);

        private static string BuildBindingCoverageReport(Transform root, AnimationClip[] clips, out bool allExact)
        {
            var rigPaths = root.GetComponentsInChildren<Transform>(true)
                .Select(transform => AnimationUtility.CalculateTransformPath(transform, root))
                .ToHashSet(StringComparer.Ordinal);
            var report = new StringBuilder();
            allExact = true;
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type == typeof(Transform))
                    .ToArray();
                var missingPaths = bindings.Select(binding => binding.path)
                    .Distinct(StringComparer.Ordinal)
                    .Where(path => !rigPaths.Contains(path))
                    .ToArray();
                var matched = bindings.Length - bindings.Count(binding => !rigPaths.Contains(binding.path));
                var clipExact = bindings.Length > 0 && missingPaths.Length == 0;
                allExact &= clipExact;
                report.AppendLine($"Binding paths {clip.name}: transformBindings={bindings.Length}, exact={matched}, missingPaths={missingPaths.Length}, exactCoverage={(bindings.Length == 0 ? 0f : matched / (float)bindings.Length):P1}");
                if (missingPaths.Length > 0)
                    report.AppendLine("  Missing paths: " + string.Join(", ", missingPaths));
                if (bindings.Length == 0)
                    report.AppendLine("  No Transform bindings found.");
            }
            return report.ToString();
        }

        private static string BuildImportReport(UnityEngine.Object[] sourceAssets, Avatar avatar,
            AnimationClip[] clips, string bindingReport)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Adhesive Cartographer — Iteration 2069 isolated review candidate");
            builder.AppendLine("This is structural import metadata generated by the Unity Editor menu action.");
            builder.AppendLine("It is not proof of rendered appearance, clip playback, deformation, foot contact, AI, or player quality.");
            builder.AppendLine($"Source: {SourcePath}");
            builder.AppendLine($"Avatar source: {AvatarSourcePath}");
            builder.AppendLine($"Review material: {MaterialPath}; texture={TexturePath}");
            builder.AppendLine($"Valid Avatar: {avatar.name}; human={avatar.isHuman}");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source != null)
            {
                var skins = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                builder.AppendLine($"Source hierarchy: transforms={source.GetComponentsInChildren<Transform>(true).Length}, skinnedRenderers={skins.Length}");
                foreach (var skin in skins)
                    builder.AppendLine($"Skin {skin.name}: mesh={skin.sharedMesh?.name ?? "missing"}, vertices={skin.sharedMesh?.vertexCount ?? 0}, bones={skin.bones?.Length ?? 0}, rootBone={skin.rootBone?.name ?? "missing"}");
            }
            builder.AppendLine($"Imported source objects: {sourceAssets.Length}");
            builder.Append(bindingReport);
            foreach (var clip in clips)
            {
                var scale = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type == typeof(Transform)
                        && binding.propertyName.IndexOf("Scale", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(binding =>
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var keys = curve != null ? curve.keys : Array.Empty<Keyframe>();
                        return keys.Length == 0 ? null : $"{binding.path}:{binding.propertyName}[{keys.Min(key => key.value):F3},{keys.Max(key => key.value):F3}]";
                    })
                    .Where(summary => summary != null)
                    .ToArray();
                builder.AppendLine($"Clip {clip.name}: duration={clip.length:F2}s, bindings={AnimationUtility.GetCurveBindings(clip).Length}, scaleCurves={(scale.Length == 0 ? "none" : string.Join(";", scale))}");
            }
            builder.AppendLine("Animator transitions are configured and runtime root motion is disabled for this review-only prefab. Inspect visual playback before any production promotion.");
            return builder.ToString();
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = motion;
            return state;
        }

        private static void AddTrigger(AnimatorStateMachine machine, AnimatorState destination, string parameter)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddTrigger(AnimatorState source, AnimatorState destination, string parameter)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddFloat(AnimatorState source, AnimatorState destination, string parameter,
            float threshold, AnimatorConditionMode mode)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.14f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddExit(AnimatorState source, AnimatorState destination, float exitTime)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
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
