#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports the approved Iteration 1337 Meshy rig and motion into a separate
    /// candidate prefab/controller. It never swaps the active player or scene.
    /// </summary>
    public static class SplatMeshyIter1337CandidateAuthoring
    {
        private const string WorkRoot =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f";
        private const string RigPath = WorkRoot + "/FBX/rigged_character.fbx";
        private const string WalkPath = WorkRoot + "/FBX/walking_fbx.fbx";
        private const string RunPath = WorkRoot + "/FBX/running_fbx.fbx";
        private const string IdleJumpPath = WorkRoot + "/animation_fbx.fbx";
        private const string LandingPath = WorkRoot + "/LandingAnimation/animation_fbx.fbx";
        private const string BaseColorPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_Meshy_Iter1337_textures/base_color.png";
        private const string MaterialPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/Materials/Splat_Meshy_Iter1337_URP.mat";
        private const string ControllerPath =
            "Assets/SpaceAndCrafts/Resources/Characters/SplatMeshyIter1337.controller";
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_Meshy_Iter1337_Candidate.prefab";
        // Iteration 1340's sampled Idle bounds placed the lowest rendered mesh
        // 0.142 m above a flat support surface. Keep this correction isolated to
        // the Meshy candidate until contact is reviewed in the active player rig.
        private const float CandidateIdleContactOffset = 0.142f;

        [MenuItem("Space & Crafts/Author Splat Meshy Iteration 1337 Candidate")]
        public static void AuthorCandidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var path in new[] { RigPath, WalkPath, RunPath, IdleJumpPath, LandingPath })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    throw new InvalidOperationException("Missing Meshy Splat source FBX: " + path);
                }
            }

            var rigImporter = ConfigureRig(RigPath, null, importAnimation: false);
            rigImporter.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid)
            {
                throw new InvalidOperationException("The Meshy Splat FBX did not produce a valid Generic Avatar.");
            }

            ConfigureRig(WalkPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRig(RunPath, avatar, importAnimation: true).SaveAndReimport();
            ConfigureRig(IdleJumpPath, avatar, importAnimation: true).SaveAndReimport();
            var landingImporter = ConfigureRig(LandingPath, avatar, importAnimation: true);
            // Meshy's retargeted landing FBX preserves translation keys in
            // centimeter-scale values, unlike the rig's metre-scale bind pose.
            // Convert only this animation import so the candidate does not jump
            // tens of metres when the landing state begins.
            landingImporter.globalScale = 0.01f;
            landingImporter.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var idle = FindClip(IdleJumpPath, "Idle");
            var jump = FindClip(IdleJumpPath, "Regular_Jump", "Jump");
            var walk = FindClip(WalkPath, "walking_man", "walking", "walk");
            var run = FindClip(RunPath, "running_man", "running", "run");
            var controller = CreateController(idle, walk, run, jump);
            CreateCandidatePrefab(avatar, controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Created the separate Splat Meshy Iteration 1337 candidate prefab/controller. Active player and scenes were not changed.");
        }

        private static ModelImporter ConfigureRig(string path, Avatar sourceAvatar, bool importAnimation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Unity could not import Meshy FBX: " + path);
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = sourceAvatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = importAnimation;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;

            if (importAnimation)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                for (var i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = ClipsShouldLoop(clips[i].name, path);
                    clips[i].loopPose = clips[i].loopTime;
                }

                importer.clipAnimations = clips;
            }

            return importer;
        }

        private static bool ClipsShouldLoop(string clipName, string path)
        {
            var name = (clipName ?? string.Empty).ToLowerInvariant();
            if (path == IdleJumpPath)
            {
                return name.Contains("idle");
            }

            return path == WalkPath || path == RunPath;
        }

        private static AnimationClip FindClip(string path, params string[] candidates)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var candidate in candidates)
            {
                var exact = clips.FirstOrDefault(c => string.Equals(
                    c.name, candidate, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }

                var contained = clips.FirstOrDefault(c => c.name.IndexOf(
                    candidate, StringComparison.OrdinalIgnoreCase) >= 0);
                if (contained != null)
                {
                    return contained;
                }
            }

            throw new InvalidOperationException(
                $"No expected animation clip at {path}. Imported clips: {string.Join(", ", clips.Select(c => c.name))}");
        }

        private static AnimatorController CreateController(
            AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump)
        {
            var folder = System.IO.Path.GetDirectoryName(ControllerPath)?.Replace('\\', '/');
            EnsureAssetFolder(folder);
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;

            var blend = new BlendTree
            {
                name = "Splat Meshy Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(idle, 0f);
            blend.AddChild(walk, 0.45f);
            blend.AddChild(run, 2f);

            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = blend;
            machine.defaultState = locomotion;
            var jumpState = machine.AddState("Jump");
            jumpState.motion = jump;

            var toJump = machine.AddAnyStateTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0.10f;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            // The Meshy landing retarget is visibly supine and drives its root
            // below the support plane in this rig, so keep it out of the
            // candidate controller. Blend from the actual airborne pose back
            // into movement when the motor confirms contact instead.
            var touchdownBlend = jumpState.AddTransition(locomotion);
            touchdownBlend.hasExitTime = false;
            touchdownBlend.duration = 0.12f;
            touchdownBlend.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            return controller;
        }

        private static void CreateCandidatePrefab(Avatar avatar, AnimatorController controller)
        {
            var folder = System.IO.Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
            EnsureAssetFolder(folder);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (model == null)
            {
                throw new InvalidOperationException("The Meshy rigged FBX did not import as a prefab source.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Splat_Meshy_Iter1337_Candidate";
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException("The Meshy rigged model has no renderers.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var characterMaterial = LoadOrCreateCharacterMaterial();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    materials = new[] { characterMaterial };
                }
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = characterMaterial;
                    }
                }

                renderer.sharedMaterials = materials;
            }

            var fit = 1.5f / Mathf.Max(0.01f, bounds.size.y);
            instance.transform.localScale = Vector3.one * fit;
            instance.transform.localPosition = new Vector3(
                0f, -bounds.min.y * fit - CandidateIdleContactOffset, 0f);

            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (instance.GetComponent<SplatMeshyAnimatorDriver>() == null)
            {
                instance.AddComponent<SplatMeshyAnimatorDriver>();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static Material LoadOrCreateCharacterMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing approved Splat base-color texture: " + BaseColorPath);
            }

            var folder = System.IO.Path.GetDirectoryName(MaterialPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EnsureAssetFolder(folder);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No lit shader is available for the Splat candidate material.");
                }

                material = new Material(shader) { name = "Splat_Meshy_Iter1337_URP" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.24f);
            }
            else
            {
                material.mainTexture = texture;
                material.color = Color.white;
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.24f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
