#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports and stages the Meshy 7.1 Splat rig and Iter1853 retargets as
    /// isolated ReviewOnly assets. This authoring path never touches production
    /// Splat, a production controller, or a game scene.
    /// </summary>
    public static class SplatMeshyIter1853ReviewOnlyAuthoring
    {
        private const string Root =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1847/Models/Splat_Proportions_Meshy71";
        private const string RigPath = Root + "/Rigged/rigged_character.fbx";
        private const string WalkPath = Root + "/Rigged/walking_fbx.fbx";
        private const string RunPath = Root + "/Rigged/running_fbx.fbx";
        private const string IdlePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_CustomIdle.fbx";
        private const string JumpLandPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_JumpLand.fbx";
        private const string CelebrationPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_BuildCelebration.fbx";
        private const string BaseColorPath =
            Root + "/Splat_Proportions_Meshy71_textures/base_color.png";

        private const string Rig75Path =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/Splat_Meshy71_75k_Rigged/Splat_Meshy71_75k_Rigged.fbx";
        private const string Walk75Path =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/Splat_Meshy71_75k_Rigged/Splat_Meshy71_75k_Walk.fbx";
        private const string Run75Path =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/Splat_Meshy71_75k_Rigged/Splat_Meshy71_75k_Run.fbx";
        private const string Idle75Path =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_75k_CustomIdle.fbx";
        private const string JumpLand75Path =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_75k_JumpLand.fbx";
        private const string Celebration75Path =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyActions_Iter1853/Splat_Meshy71_75k_BuildCelebration.fbx";
        private const string BaseColor75Path =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/Splat_Meshy71_75k_textures/base_color.png";

        private const string ReviewFolder = Root + "/ReviewOnly_Iter1853";
        private const string ReviewFolder75 =
            "Assets/SpaceAndCrafts/Art/Commissioned/CraftCosmos/Iter1853/Models/Splat_Meshy71_75k_Rigged/ReviewOnly_Iter1853";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1853-reviewonly/import-and-pose-review.txt";

        [MenuItem("Space & Crafts/Prepare Splat Meshy Iter1853 ReviewOnly")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sources = new[]
            {
                RigPath, WalkPath, RunPath, IdlePath, JumpLandPath, CelebrationPath, BaseColorPath,
                Rig75Path, Walk75Path, Run75Path, Idle75Path, JumpLand75Path, Celebration75Path, BaseColor75Path,
            };
            foreach (var path in sources)
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    throw new InvalidOperationException("Missing Iter1853 review source asset: " + path);

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
            var report = new List<string>
            {
                "Splat Meshy 7.1 Iter1853 ReviewOnly import and sample report",
                "Both the 289k/Iter1847 proportions rig and the Iter1853 75k rig are staged independently with their matching action-file family and each rig's own Generic Avatar.",
                "This report does not demonstrate live Animator playback, player controls, gameplay transitions, root-motion behavior, or physical foot contact.",
                "No corrected candidate is staged: the action Hips rotation differs from the source rig rest orientation by about 80 degrees, and a trial constant Y/Z translation normalization moved the toe anchors away from the floor.",
                "Production Splat prefab, Animator Controller, and game scenes were not modified.",
            };
            var candidate289 = PrepareCandidate("289k", RigPath, WalkPath, RunPath, IdlePath, JumpLandPath,
                CelebrationPath, BaseColorPath, ReviewFolder, "Splat_Meshy71_289k_Iter1853_ReviewOnly");
            report.AddRange(candidate289.report);
            var candidate75 = PrepareCandidate("75k", Rig75Path, Walk75Path, Run75Path, Idle75Path,
                JumpLand75Path, Celebration75Path, BaseColor75Path, ReviewFolder75,
                "Splat_Meshy71_75k_Iter1853_ReviewOnly");
            report.AddRange(candidate75.report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllLines(ReportPath, report);
            AppendStaticPoseDiagnostics(report, "289k", candidate289.rigPath, candidate289.materialPath,
                new[] { ("Idle", candidate289.idle), ("Walk", candidate289.walk), ("Run", candidate289.run),
                    ("JumpLand", candidate289.jumpLand), ("BuildCelebration", candidate289.celebration) });
            AppendStaticPoseDiagnostics(report, "75k", candidate75.rigPath, candidate75.materialPath,
                new[] { ("Idle", candidate75.idle), ("Walk", candidate75.walk), ("Run", candidate75.run),
                    ("JumpLand", candidate75.jumpLand), ("BuildCelebration", candidate75.celebration) });
            File.WriteAllLines(ReportPath, report);

            Debug.Log("[Splat Meshy Iter1853] Separate 289k and 75k ReviewOnly import/controller/prefab staging is ready. Production player and scenes untouched.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static (string rigPath, string materialPath, AnimationClip idle, AnimationClip walk,
            AnimationClip run, AnimationClip jumpLand, AnimationClip celebration, List<string> report)
            PrepareCandidate(string label, string rigPath, string walkPath, string runPath, string idlePath,
                string jumpPath, string celebrationPath, string texturePath, string reviewFolder, string objectName)
        {
            Directory.CreateDirectory(reviewFolder);
            var controllerPath = reviewFolder + "/" + objectName + ".controller";
            var materialPath = reviewFolder + "/" + objectName + ".mat";
            var prefabPath = reviewFolder + "/" + objectName + ".prefab";
            // Keep the rig's clip importer enabled so Unity serializes its
            // Generic Avatar alongside the model hierarchy.
            ConfigureModel(rigPath, null, true).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>()
                .FirstOrDefault(value => value != null && value.isValid);
            if (avatar == null || !avatar.isValid)
                throw new InvalidOperationException($"The {label} Splat rig did not produce a valid Generic Avatar.");

            foreach (var path in new[] { walkPath, runPath, idlePath, jumpPath, celebrationPath })
                ConfigureModel(path, avatar, true).SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var idle = FindClip(idlePath, "idle");
            var walk = FindClip(walkPath, "walk");
            var run = FindClip(runPath, "run");
            var jumpLand = FindClip(jumpPath, "jump", "land");
            var celebration = FindClip(celebrationPath, "celebr", "build");
            var controller = CreateController(idle, walk, run, jumpLand, celebration, controllerPath, objectName);
            var material = CreateReviewMaterial(texturePath, materialPath, objectName);
            CreateReviewPrefab(rigPath, prefabPath, objectName, avatar, controller, material);

            var report = BuildImportReport(label, rigPath, prefabPath, controllerPath, avatar, new[]
            {
                (idlePath, idle), (walkPath, walk), (runPath, run), (jumpPath, jumpLand), (celebrationPath, celebration),
            }).ToList();
            return (rigPath, materialPath, idle, walk, run, jumpLand, celebration, report);
        }


        private static ModelImporter ConfigureModel(string path, Avatar avatar, bool importAnimation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Unity could not import FBX: " + path);

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = avatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = importAnimation;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            // Meshy retarget FBXs encode the rig's translation keys in cm,
            // unlike the meter-scaled rig and walk/run exports.
            importer.globalScale = importAnimation &&
                (path == IdlePath || path == JumpLandPath || path == CelebrationPath ||
                 path == Idle75Path || path == JumpLand75Path || path == Celebration75Path)
                ? 0.01f
                : 1f;

            if (importAnimation)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    var name = (clip.name ?? string.Empty).ToLowerInvariant();
                    clip.loopTime = path == WalkPath || path == RunPath || path == Walk75Path || path == Run75Path ||
                                    path == IdlePath || path == Idle75Path || name.Contains("idle");
                    clip.loopPose = clip.loopTime;
                }
                importer.clipAnimations = clips;
            }
            return importer;
        }

        private static AnimationClip FindClip(string path, params string[] fragments)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => clip != null && clip.length > 0.01f).ToArray();
            var takes = clips.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var fragment in fragments)
            {
                var found = takes.FirstOrDefault(clip =>
                    clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                if (found != null) return found;
            }
            // Meshy retarget exports name the actual take "retarget_clip" and
            // include a second __preview__retarget_clip subclip. Pick the sole
            // non-preview take when semantic labels are absent.
            if (takes.Length == 1) return takes[0];
            throw new InvalidOperationException(
                $"Missing clip [{string.Join(", ", fragments)}] in {path}. Imported: {string.Join(", ", clips.Select(clip => clip.name))}");
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk,
            AnimationClip run, AnimationClip jumpLand, AnimationClip buildCelebration,
            string controllerPath, string objectName)
        {
            DeleteIfExists(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("JumpLand", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("BuildCelebration", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            var locomotionBlend = new BlendTree
            {
                name = objectName + " Idle-Walk-Run ReviewOnly",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(locomotionBlend, controller);
            locomotionBlend.AddChild(idle, 0f);
            locomotionBlend.AddChild(walk, 0.5f);
            locomotionBlend.AddChild(run, 2f);

            var locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = locomotionBlend;
            stateMachine.defaultState = locomotion;
            var jump = stateMachine.AddState("JumpLand");
            jump.motion = jumpLand;
            var celebration = stateMachine.AddState("BuildCelebration");
            celebration.motion = buildCelebration;

            var toJump = locomotion.AddTransition(jump);
            toJump.hasExitTime = false;
            toJump.duration = 0.08f;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "JumpLand");
            var jumpBack = jump.AddTransition(locomotion);
            jumpBack.hasExitTime = true;
            jumpBack.exitTime = 0.95f;
            jumpBack.duration = 0.12f;

            var toCelebration = stateMachine.AddAnyStateTransition(celebration);
            toCelebration.hasExitTime = false;
            toCelebration.duration = 0.10f;
            toCelebration.AddCondition(AnimatorConditionMode.If, 0f, "BuildCelebration");
            var celebrateBack = celebration.AddTransition(locomotion);
            celebrateBack.hasExitTime = true;
            celebrateBack.exitTime = 0.95f;
            celebrateBack.duration = 0.12f;
            return controller;
        }

        private static Material CreateReviewMaterial(string texturePath, string materialPath, string objectName)
        {
            DeleteIfExists(materialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported lit shader is available for the ReviewOnly material.");
            var material = new Material(shader) { name = objectName + " Material" };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException("Missing Splat base-color texture: " + texturePath);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.04f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void CreateReviewPrefab(string rigPath, string prefabPath, string objectName,
            Avatar avatar, AnimatorController controller, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (model == null) throw new InvalidOperationException("Rigged Splat FBX failed to load as a model prefab: " + rigPath);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Splat into a Preview Scene.");
                instance.name = objectName;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Rigged Splat has no renderers.");
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();

                var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Directory.CreateDirectory(ReviewFolder);
                DeleteIfExists(prefabPath);
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                UnityEngine.Object.DestroyImmediate(instance);
                if (prefab == null) throw new InvalidOperationException("Could not save isolated Splat ReviewOnly prefab.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static IEnumerable<string> BuildImportReport(string label, string rigPath, string prefabPath,
            string controllerPath, Avatar avatar,
            IEnumerable<(string path, AnimationClip clip)> clips)
        {
            var lines = new List<string>
            {
                "Candidate=" + label,
                "Avatar valid=" + avatar.isValid + "; avatar name=" + avatar.name,
                "Source rig path=" + rigPath + "; Unity asset GUID=" + AssetDatabase.AssetPathToGUID(rigPath),
                "Review prefab path=" + prefabPath + "; Unity asset GUID=" + AssetDatabase.AssetPathToGUID(prefabPath),
                "Controller path=" + controllerPath + "; Unity asset GUID=" + AssetDatabase.AssetPathToGUID(controllerPath),
                "Clip import summary:",
            };
            foreach (var item in clips)
            {
                var importer = AssetImporter.GetAtPath(item.path) as ModelImporter;
                var transformBindings = AnimationUtility.GetCurveBindings(item.clip)
                    .Where(binding => binding.type == typeof(Transform)).ToArray();
                var scaleCurves = transformBindings.Count(binding =>
                    binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal));
                var avatarName = importer != null && importer.sourceAvatar != null
                    ? importer.sourceAvatar.name
                    : "none";
                lines.Add($"{Path.GetFileName(item.path)} | clip={item.clip.name} | {item.clip.length:F3}s | " +
                          $"Generic={importer != null && importer.animationType == ModelImporterAnimationType.Generic} | " +
                          $"avatarSetup={importer?.avatarSetup} | sourceAvatar={avatarName} | " +
                          $"globalScale={importer?.globalScale:F3} | " +
                          $"transformCurves={transformBindings.Length} | transformScaleCurves={scaleCurves} | " +
                          $"GUID={AssetDatabase.AssetPathToGUID(item.path)}");
            }
            return lines;
        }

        private static void AppendStaticPoseDiagnostics(List<string> report, string label, string rigPath,
            string materialPath, IEnumerable<(string name, AnimationClip clip)> actions)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (model == null || material == null)
                throw new InvalidOperationException($"Review-only {label} model/material missing before static pose diagnostics.");

            report.Add($"Static SampleAnimation diagnostics for {label} (not Animator playback/contact proof):");
            var baselineObject = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (baselineObject == null) throw new InvalidOperationException("Could not instantiate rig baseline.");
            Bounds baselineBounds;
            Vector3? baselineHipsPosition;
            Vector3? baselineArmatureEuler;
            Vector3? baselineHipsEuler;
            Vector3? baselineLeftToe;
            Vector3? baselineRightToe;
            try
            {
                baselineBounds = CalculateBounds(baselineObject);
                baselineArmatureEuler = baselineObject.transform.Find("Armature")?.localEulerAngles;
                baselineHipsEuler = baselineObject.transform.Find("Armature/Hips")?.localEulerAngles;
                baselineHipsPosition = baselineObject.transform.Find("Armature/Hips")?.localPosition;
                baselineLeftToe = baselineObject.transform.Find("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase")?.position;
                baselineRightToe = baselineObject.transform.Find("Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase")?.position;
            }
            finally { UnityEngine.Object.DestroyImmediate(baselineObject); }
            report.Add($"Rig baseline before clips: boundsCenter={baselineBounds.center}, extents={baselineBounds.extents}, boundsMinY={baselineBounds.min.y:F4}, Armature.localEuler={baselineArmatureEuler?.ToString() ?? "missing"}, Armature/Hips.localEuler={baselineHipsEuler?.ToString() ?? "missing"}, Armature/Hips.localPosition={baselineHipsPosition?.ToString() ?? "missing"}, leftToeWorld={baselineLeftToe?.ToString() ?? "missing"}, rightToeWorld={baselineRightToe?.ToString() ?? "missing"}.");
            foreach (var action in actions)
            {
                foreach (var fraction in new[] { 0.15f, 0.50f, 0.85f })
                {
                    var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (instance == null) throw new InvalidOperationException("Could not instantiate model for static pose diagnostics.");
                    try
                    {
                        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                            renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                        var animator = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>(true);
                        if (animator != null) animator.enabled = false;
                        instance.transform.position = Vector3.zero;
                        instance.transform.rotation = Quaternion.identity;
                        action.clip.SampleAnimation(instance, Mathf.Clamp(action.clip.length * fraction, 0f,
                            Mathf.Max(0f, action.clip.length - 0.0001f)));
                        var bounds = CalculateBounds(instance);
                        var hipsPosition = instance.transform.Find("Armature/Hips")?.localPosition;
                        var armatureEuler = instance.transform.Find("Armature")?.localEulerAngles;
                        var hipsEuler = instance.transform.Find("Armature/Hips")?.localEulerAngles;
                        var leftToe = instance.transform.Find("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase")?.position;
                        var rightToe = instance.transform.Find("Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase")?.position;
                        var transformBindings = AnimationUtility.GetCurveBindings(action.clip)
                            .Where(binding => binding.type == typeof(Transform)).ToArray();
                        var matchedPaths = transformBindings.Count(binding =>
                            string.IsNullOrEmpty(binding.path) || instance.transform.Find(binding.path) != null);
                        var scaleCurves = transformBindings.Count(binding =>
                            binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal));
                        report.Add($"{action.name}@{fraction:P0} ({action.clip.length:F3}s clip): " +
                                   $"boundsCenter={bounds.center}, extents={bounds.extents}, boundsMinY={bounds.min.y:F4}, " +
                                   $"Armature.localEuler={armatureEuler?.ToString() ?? "missing"}, Armature/Hips.localEuler={hipsEuler?.ToString() ?? "missing"}, " +
                                   $"Armature/Hips.localPosition={hipsPosition?.ToString() ?? "missing"}, " +
                                   $"leftToeWorld={leftToe?.ToString() ?? "missing"}, rightToeWorld={rightToe?.ToString() ?? "missing"}, " +
                                   $"transformPaths={matchedPaths}/{transformBindings.Length}, scaleCurves={scaleCurves}.");
                        if (Mathf.Approximately(fraction, 0.50f))
                        {
                            foreach (var translation in DescribeTranslationCurves(action.clip))
                                report.Add("  positionCurve " + translation);
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                }
            }
        }

        private static IEnumerable<string> DescribeTranslationCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal))
                .ToArray();
            foreach (var group in bindings.GroupBy(binding => binding.path))
            {
                if (group.Key != "Armature" && group.Key != "Armature/Hips") continue;
                var channels = new List<string>();
                foreach (var binding in group)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0) continue;
                    channels.Add(binding.propertyName.Substring("m_LocalPosition.".Length) +
                                 $"={curve.Evaluate(0f):F3}/{curve.Evaluate(clip.length * 0.5f):F3}/{curve.Evaluate(clip.length):F3}m");
                }
                if (channels.Count == 0) continue;
                yield return $"path='{group.Key}' " + string.Join(", ", channels);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Splat sample has no renderers to measure.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif
