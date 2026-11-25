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
    /// Imports the already-downloaded Snip-Snap Sellsword rig/actions and makes
    /// a contained review prefab. No production scene or enemy AI is modified.
    /// </summary>
    public static class SnipSnapSellswordReviewOnlyAuthoringIter2073
    {
        private const string RigPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/Rigged/SnipSnapSellsword_Rig.fbx";
        private const string ActionPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/Animations/SnipSnapSellsword_CombatSet.fbx";
        private const string WalkPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/Animations/SnipSnapSellsword_Walk.fbx";
        private const string RunPath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/Animations/SnipSnapSellsword_Run.fbx";
        private const string TexturePath =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/Model/SnipSnapSellsword_base_color.png";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/UnityCandidate_ReviewOnly_Iter2073";
        private const string ControllerPath = OutputFolder + "/SnipSnapSellsword_ReviewOnly.controller";
        private const string PrefabPath = OutputFolder + "/SnipSnapSellsword_ReviewOnly.prefab";
        private const string MaterialPath = OutputFolder + "/SnipSnapSellsword_ReviewOnly.mat";
        private const string ReportPath = OutputFolder + "/import-structure-review.txt";

        [MenuItem("Space & Crafts/Author Snip-Snap Sellsword ReviewOnly (Iter 2073)")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                throw new InvalidOperationException("Snip-Snap Iter2073 review output already exists; refusing to overwrite it.");

            ConfigureGenericImporter(RigPath, ModelImporterAvatarSetup.CreateFromThisModel, null);
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (rig == null) throw new FileNotFoundException("Missing rigged model FBX", RigPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid);
            if (avatar == null) throw new InvalidOperationException("Snip-Snap's FBX did not produce a valid review Avatar.");

            ConfigureGenericImporter(ActionPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            ConfigureGenericImporter(WalkPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            ConfigureGenericImporter(RunPath, ModelImporterAvatarSetup.CopyFromOther, avatar);
            ConfigureLoops(ActionPath, name => Has(name, "Idle"));
            ConfigureLoops(WalkPath, name => Has(name, "Walk"));
            ConfigureLoops(RunPath, name => Has(name, "Run"));

            var idle = RequireClip(ActionPath, "Idle");
            var prep = RequireClip(ActionPath, "Boxing Guard Prep Straight Punch");
            var attack = RequireClip(ActionPath, "Attack");
            var hit = RequireClip(ActionPath, "Hit Reaction");
            var dead = RequireClip(ActionPath, "Dead");
            // Unity's importer labels the task's final action Rightward_Spin.
            // Do not assume that it contains a get-up/recovery phase.
            var spin = RequireClip(ActionPath, "Rightward Spin");
            var walk = RequireClip(WalkPath, "Walk");
            var run = RequireClip(RunPath, "Run");
            var clips = new[] { idle, walk, run, prep, attack, hit, dead, spin };
            var rigPaths = rig.GetComponentsInChildren<Transform>(true)
                .Select(transform => AnimationUtility.CalculateTransformPath(transform, rig.transform))
                .ToHashSet(StringComparer.Ordinal);
            var report = new StringBuilder();
            report.AppendLine("Snip-Snap Sellsword — Iteration 2073 isolated Unity import review.");
            report.AppendLine("Structural import evidence only; this is not proof of natural playback, skin quality, ground contact, AI, gameplay, or human play.");
            report.AppendLine($"Rig: {RigPath}");
            report.AppendLine($"Avatar: {avatar.name}; human={avatar.isHuman}; valid={avatar.isValid}");
            report.AppendLine($"Hierarchy transforms={rig.GetComponentsInChildren<Transform>(true).Length}; skins={rig.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
            foreach (var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"Skin {skin.name}: mesh={skin.sharedMesh?.name ?? "missing"}; vertices={skin.sharedMesh?.vertexCount ?? 0}; bones={skin.bones?.Length ?? 0}");

            var allExact = true;
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip).Where(binding => binding.type == typeof(Transform)).ToArray();
                var missing = bindings.Select(binding => binding.path).Distinct(StringComparer.Ordinal)
                    .Where(path => !rigPaths.Contains(path)).ToArray();
                var matched = bindings.Length - bindings.Count(binding => !rigPaths.Contains(binding.path));
                var clipExact = bindings.Length > 0 && missing.Length == 0;
                allExact &= clipExact;
                report.AppendLine($"Clip {clip.name}: duration={clip.length:F2}s; allBindings={AnimationUtility.GetCurveBindings(clip).Length}; transformBindings={bindings.Length}; matchingTransformPaths={matched}; missing={missing.Length}; exact={clipExact}");
                if (missing.Length > 0) report.AppendLine("  Missing rig paths: " + string.Join(", ", missing));
            }

            EnsureFolder(OutputFolder);
            if (!allExact)
            {
                File.WriteAllText(ReportPath, report + "STOP: clip transform paths do not exactly target the imported rig. No review prefab/controller created.\n", Encoding.UTF8);
                AssetDatabase.Refresh();
                throw new InvalidOperationException("Clip/rig path preflight failed; see " + ReportPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Prep", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Spin", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, new Vector3(100, 0));
            var walkState = AddState(machine, "Walk", walk, new Vector3(300, 0));
            var runState = AddState(machine, "Run", run, new Vector3(500, 0));
            var prepState = AddState(machine, "Prep", prep, new Vector3(700, -160));
            var attackState = AddState(machine, "Attack", attack, new Vector3(700, -40));
            var hitState = AddState(machine, "Hit", hit, new Vector3(700, 80));
            var deadState = AddState(machine, "Dead", dead, new Vector3(700, 200));
            var spinState = AddState(machine, "Spin", spin, new Vector3(950, 200));
            machine.defaultState = idleState;
            AddFloat(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloat(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddFloat(walkState, runState, "MoveSpeed", 1.25f, AnimatorConditionMode.Greater);
            AddFloat(runState, walkState, "MoveSpeed", 1.25f, AnimatorConditionMode.Less);
            AddTrigger(machine, prepState, "Prep");
            AddTrigger(machine, attackState, "Attack");
            AddTrigger(machine, hitState, "Hit");
            AddTrigger(machine, deadState, "Defeat");
            AddTrigger(machine, spinState, "Spin");
            AddExit(prepState, idleState, 0.92f);
            AddExit(attackState, idleState, 0.94f);
            AddExit(hitState, idleState, 0.88f);
            AddExit(spinState, idleState, 0.94f);

            var material = CreateReviewMaterial();
            var instance = PrefabUtility.InstantiatePrefab(rig) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate the Snip-Snap rig for isolated review.");
            try
            {
                instance.name = "SnipSnapSellsword_ReviewOnly_Iter2073";
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0) throw new InvalidOperationException("Snip-Snap rig has no skinned mesh renderer.");
                foreach (var skin in skins) skin.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }

            report.AppendLine($"Review material: {MaterialPath}; texture={TexturePath}");
            report.AppendLine($"Review prefab/controller: {PrefabPath} / {ControllerPath}");
            report.AppendLine("All eight imported clips target the exact generic rig transform paths. The package does not import a distinct Stand Up/Recovery clip; Rightward_Spin is kept as Spin and is not assumed to be a recovery. Runtime root motion is disabled; production scene untouched.");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Snip-Snap Iter2073] created review-only prefab/controller and exact-path report; no production scene or AI changes.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void ConfigureGenericImporter(string path, ModelImporterAvatarSetup avatarSetup, Avatar avatar)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing FBX importer", path);
            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic) { importer.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (importer.avatarSetup != avatarSetup) { importer.avatarSetup = avatarSetup; changed = true; }
            if (avatarSetup == ModelImporterAvatarSetup.CopyFromOther && importer.sourceAvatar != avatar) { importer.sourceAvatar = avatar; changed = true; }
            if (!importer.importAnimation) { importer.importAnimation = true; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static void ConfigureLoops(string path, Func<string, bool> loopPredicate)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing clip importer", path);
            var clipAnimations = importer.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0) clipAnimations = importer.defaultClipAnimations;
            var changed = false;
            foreach (var clip in clipAnimations)
            {
                var loop = loopPredicate(clip.name);
                if (clip.loopTime != loop || clip.loopPose != loop) { clip.loopTime = loop; clip.loopPose = loop; changed = true; }
            }
            if (changed) { importer.clipAnimations = clipAnimations; importer.SaveAndReimport(); }
        }

        private static AnimationClip RequireClip(string path, string token)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var target = Normalize(token);
            var found = clips.FirstOrDefault(clip => Normalize(clip.name).EndsWith(target, StringComparison.Ordinal))
                ?? clips.OrderBy(clip => clip.name.Length).FirstOrDefault(clip => Normalize(clip.name).Contains(target, StringComparison.Ordinal));
            if (found == null) throw new InvalidOperationException($"Clip '{token}' missing from {path}. Available: {string.Join(", ", clips.Select(clip => clip.name))}");
            if (found.length <= 0f || AnimationUtility.GetCurveBindings(found).Length == 0) throw new InvalidOperationException($"Clip '{found.name}' is empty.");
            return found;
        }

        private static Material CreateReviewMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new FileNotFoundException("Missing base-color texture", TexturePath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a review shader.");
            var material = new Material(shader) { name = "SnipSnapSellsword_ReviewOnly" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddTrigger(AnimatorStateMachine machine, AnimatorState destination, string parameter)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false; transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddTrigger(AnimatorState source, AnimatorState destination, string parameter)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false; transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddFloat(AnimatorState source, AnimatorState destination, string parameter, float threshold, AnimatorConditionMode mode)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false; transition.duration = 0.14f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddExit(AnimatorState source, AnimatorState destination, float exitTime)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = true; transition.exitTime = exitTime; transition.duration = 0.1f;
        }

        private static bool Has(string value, string token) => Normalize(value).Contains(Normalize(token), StringComparison.Ordinal);
        private static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
