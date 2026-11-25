#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Builds isolated review-only assets for Perforation Warden without touching scenes.</summary>
    public static class BuildPerforationWardenReviewIter2236
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/";
        private const string SourceRoot = Root + "Meshy_Iter1951/";
        private const string Review = Root + "Review/";
        private const string PrefabPath = Review + "PerforationWarden_Review_Iter2236.prefab";
        private const string ControllerPath = Review + "PerforationWarden_Review_Iter2236.controller";
        private const string MaterialPath = Review + "PerforationWarden_Review_Iter2236.mat";
        private const string TexturePath = SourceRoot + "PerforationWarden_Textured_base_color.png";
        private const string RiggedPath = SourceRoot + "PerforationWarden_Rigged.fbx";
        private const string ActionsPath = SourceRoot + "PerforationWarden_Actions.fbx";
        private const string WalkPath = SourceRoot + "PerforationWarden_Walking.fbx";
        private const string RunPath = SourceRoot + "PerforationWarden_Running.fbx";

        [MenuItem("Space & Crafts/Assets/Build Perforation Warden Review Iter2236")]
        public static void Build()
        {
            Directory.CreateDirectory(Review);
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("Review prefab already exists; refusing to overwrite it.");
            // A failed first authoring attempt may leave only these uniquely named partial assets.
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.DeleteAsset(MaterialPath);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RiggedPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (model == null || texture == null) throw new InvalidOperationException("Missing rigged model or atlas.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<AnimationClip>()
                .Where(clip => clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var idle = FindClip(clips, "Idle");
            var walk = LoadOnlyClip(WalkPath);
            var run = LoadOnlyClip(RunPath);
            var alert = FindClip(clips, "Alert");
            var attack = FindClip(clips, "Attack");
            var slam = FindClip(clips, "Jump_and_Slam_Back_Down");
            var hit = FindClip(clips, "Hit_Reaction");
            var dead = FindClip(clips, "Dead");
            var recover = FindClip(clips, "Stand_Up1");
            var required = new[] { idle, walk, run, alert, attack, slam, hit, dead, recover };
            if (required.Any(clip => clip == null)) throw new InvalidOperationException("One or more expected review clips are missing.");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported Lit shader found.");
            var material = new Material(shader) { name = "PerforationWarden_Review_Iter2236" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;
            var walkRun = new BlendTree
            {
                name = "WalkRun_ReviewOnly",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            walkRun.AddChild(walk, 0f);
            walkRun.AddChild(run, 1f);
            AssetDatabase.AddObjectToAsset(walkRun, ControllerPath);
            machine.AddState("WalkRun").motion = walkRun;
            machine.AddState("Alert").motion = alert;
            machine.AddState("Attack").motion = attack;
            machine.AddState("JumpSlam").motion = slam;
            machine.AddState("Hit").motion = hit;
            machine.AddState("Dead").motion = dead;
            machine.AddState("StandUp").motion = recover;
            EditorUtility.SetDirty(controller);

            var modelInstance = UnityEngine.Object.Instantiate(model);
            if (modelInstance == null) throw new InvalidOperationException("Could not clone Warden rigged FBX.");
            modelInstance.name = "PerforationWardenModelSource_Iter2236";
            var instance = new GameObject("PerforationWarden_Review_Iter2236");
            instance.transform.localPosition = modelInstance.transform.localPosition;
            instance.transform.localRotation = modelInstance.transform.localRotation;
            instance.transform.localScale = modelInstance.transform.localScale;
            while (modelInstance.transform.childCount > 0)
                modelInstance.transform.GetChild(0).SetParent(instance.transform, false);
            UnityEngine.Object.DestroyImmediate(modelInstance);
            var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(item => item.sharedMesh).ToArray();
            if (skins.Length != 1)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw new InvalidOperationException($"Expected one skinned renderer, found {skins.Length}.");
            }
            skins[0].sharedMaterial = material;
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            if (animator == null) throw new InvalidOperationException("Could not add Animator to the cloned Warden model.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out var success);
            UnityEngine.Object.DestroyImmediate(instance);
            if (!success || saved == null) throw new InvalidOperationException("Failed to save Warden review prefab.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PerforationWardenReview2236] Created review-only prefab/controller/material at {Review}; rig=Generic; clips={string.Join(",", required.Select(clip => clip.name))}; texture={texture.width}x{texture.height}; scenes unchanged.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string name) =>
            clips.FirstOrDefault(clip => string.Equals(clip.name, name, StringComparison.OrdinalIgnoreCase));

        private static AnimationClip LoadOnlyClip(string path) => AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>().FirstOrDefault(clip => clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
    }
}
#endif
