#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Imports the isolated Iter1461 Meshy-derived spider rig for Unity review.
    /// It creates only a candidate prefab/controller and never edits a scene.
    /// </summary>
    public static class RainbowSpiderCandidateBuilder
    {
        private const string Root =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461";
        private const string SourceFbx = Root + "/RainbowSpider_RigCandidate.fbx";
        private const string Output = Root + "/UnityCandidate";
        private const string ControllerPath = Output + "/RainbowKingdomSpider.controller";
        private const string PrefabPath = Output + "/RainbowKingdomSpider_AnimatedCandidate.prefab";
        private const string MaterialPath = Output + "/RainbowKingdomSpider.mat";
        private const string BaseColorPath = Root + "/Spider_BaseColor.png";
        private const string NormalPath = Root + "/Spider_Normal.png";

        [MenuItem("Space & Crafts/Inspect Rainbow Spider Rig Bones (Iter1553)")]
        public static void InspectRigBonesIter1553()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException($"Missing candidate prefab: {PrefabPath}");
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Candidate prefab has no Animator.");
            var hierarchy = prefab.GetComponentsInChildren<Transform>(true);
            var names = hierarchy.Where(bone => bone != null)
                .Select(bone => $"{AnimationUtility.CalculateTransformPath(bone, animator.transform)}@p{bone.localPosition:F3}/r{bone.localRotation.eulerAngles:F1}");
            var clips = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.Select(clip => $"{clip.name}:{clip.length:F2}s")
                : Enumerable.Empty<string>();
            Debug.Log($"[Rainbow Spider rig inspection] bones=[{string.Join("; ", names)}], clips=[{string.Join("; ", clips)}].");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Build Isolated Rainbow Spider Animation Candidate")]
        public static void Build()
        {
            ConfigureTextureImporter(NormalPath, TextureImporterType.NormalMap, false);
            ConfigureTextureImporter(BaseColorPath, TextureImporterType.Default, true);
            var importer = AssetImporter.GetAtPath(SourceFbx) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing model importer: {SourceFbx}");

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var sourceClips = importer.defaultClipAnimations;
            foreach (var clip in sourceClips)
            {
                var loop = clip.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0
                    || clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0;
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = sourceClips;
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(SourceFbx)
                .OfType<Avatar>().FirstOrDefault(candidate => candidate != null && candidate.isValid);
            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbx).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (model == null || avatar == null || !avatar.isValid || baseColor == null || normal == null)
                throw new InvalidOperationException("Unity did not import the Generic Spider Avatar and Meshy color/normal maps.");

            var idle = FindClip(clips, "Idle");
            var walk = FindClip(clips, "Walk");
            var attack = FindClip(clips, "Attack");
            var hit = FindClip(clips, "Hit");
            var defeat = FindClip(clips, "Defeat");
            if (idle == null || walk == null || attack == null || hit == null || defeat == null)
                throw new InvalidOperationException("Imported clips must include Idle, Walk, Attack, Hit, and Defeat.");

            EnsureFolder(Output);
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.DeleteAsset(PrefabPath);
            AssetDatabase.DeleteAsset(MaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No compatible Lit shader was found.");
            var spiderMaterial = new Material(shader) { name = "RainbowKingdomSpider_MeshyPBR" };
            // Preserve the authored Meshy body and limb colors in the live game.
            // A previous purple-only override hid this cohesive texture and made
            // the spider read like a flat collection of toy parts.
            if (spiderMaterial.HasProperty("_BaseMap")) spiderMaterial.SetTexture("_BaseMap", baseColor);
            if (spiderMaterial.HasProperty("_MainTex")) spiderMaterial.SetTexture("_MainTex", baseColor);
            // The saved scene-camera capture showed the pale Meshy albedo
            // washing out under the saturated World 01 fill. Attenuate the
            // neutral tint slightly so the imported color map keeps detail
            // without changing its hue or replacing the cohesive texture.
            var spiderTint = new Color(0.78f, 0.78f, 0.78f, 1f);
            if (spiderMaterial.HasProperty("_BaseColor")) spiderMaterial.SetColor("_BaseColor", spiderTint);
            if (spiderMaterial.HasProperty("_Color")) spiderMaterial.SetColor("_Color", spiderTint);
            if (spiderMaterial.HasProperty("_MetallicGlossMap")) spiderMaterial.SetTexture("_MetallicGlossMap", null);
            if (spiderMaterial.HasProperty("_BumpMap")) spiderMaterial.SetTexture("_BumpMap", normal);
            // A broad soft highlight keeps the organic shell from reading like
            // hard plastic under the World 02 sun and colored fill lights.
            if (spiderMaterial.HasProperty("_Smoothness")) spiderMaterial.SetFloat("_Smoothness", 0.16f);
            if (spiderMaterial.HasProperty("_Metallic")) spiderMaterial.SetFloat("_Metallic", 0.02f);
            // URP Lit names this variant METALLICSPECGLOSSMAP. The legacy
            // Standard-shader keyword is invalid here and does not control
            // the packed metallic/smoothness texture.
            spiderMaterial.DisableKeyword("_METALLICSPECGLOSSMAP");
            spiderMaterial.EnableKeyword("_NORMALMAP");
            AssetDatabase.CreateAsset(spiderMaterial, MaterialPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("WalkRate", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, new Vector3(250, 40));
            var walkState = AddState(machine, "Walk", walk, new Vector3(250, 160));
            walkState.speedParameterActive = true;
            walkState.speedParameter = "WalkRate";
            var attackState = AddState(machine, "Attack", attack, new Vector3(510, -40));
            var hitState = AddState(machine, "Hit", hit, new Vector3(510, 80));
            var defeatState = AddState(machine, "Defeat", defeat, new Vector3(510, 200));
            machine.defaultState = idleState;
            AddFloatTransition(idleState, walkState, "MoveSpeed", 0.1f, AnimatorConditionMode.Greater);
            AddFloatTransition(walkState, idleState, "MoveSpeed", 0.1f, AnimatorConditionMode.Less);
            AddTriggerTransition(machine, attackState, "Attack");
            AddTriggerTransition(machine, hitState, "Hit");
            AddTriggerTransition(machine, defeatState, "Defeat");
            AddExitToIdle(attackState, idleState, 0.9f);
            AddExitToIdle(hitState, idleState, 0.85f);

            var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (modelInstance == null)
                throw new InvalidOperationException("Could not instantiate the imported spider FBX.");
            modelInstance.name = "RainbowKingdomSpider_AnimatedCandidate";
            var visualRoot = new GameObject("ART_MeshyEnemyVisual");
            modelInstance.transform.SetParent(visualRoot.transform, false);
            // The AI root is authored near the body's center, while the measured
            // eight toe targets sit 0.421 m below its sampled World 02 support.
            // Leave the final 0.14 m to ScrapMite's bounded visual seat correction.
            visualRoot.transform.localPosition = Vector3.up * 0.281f;
            var animator = modelInstance.GetComponent<Animator>() ?? modelInstance.AddComponent<Animator>();
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = spiderMaterial;
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visualRoot.AddComponent<RainbowSpiderAnimatorDriver>();
            PrefabUtility.SaveAsPrefabAsset(visualRoot, PrefabPath);
            UnityEngine.Object.DestroyImmediate(visualRoot);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Spider candidate] saved isolated prefab={PrefabPath}; controller={ControllerPath}; Meshy base-color and normal maps; valid avatar; no scenes modified.");
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string token) =>
            clips.FirstOrDefault(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

        private static void ConfigureTextureImporter(string path, TextureImporterType textureType, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing texture importer: {path}");
            if (importer.textureType == textureType && importer.sRGBTexture == srgb) return;
            importer.textureType = textureType;
            importer.sRGBTexture = srgb;
            importer.SaveAndReimport();
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name,
            AnimationClip clip, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddFloatTransition(AnimatorState source, AnimatorState target,
            string parameter, float threshold, AnimatorConditionMode mode)
        {
            var transition = source.AddTransition(target);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState target,
            string parameter)
        {
            var transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitToIdle(AnimatorState source, AnimatorState idle, float exitTime)
        {
            var transition = source.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
