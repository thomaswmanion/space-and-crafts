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
    /// <summary>Authors a grounded, review-only crease-barrier clip on the existing Generic Warden rig.</summary>
    public static class AuthorPerforationWardenCreaseBarrierAlignedIter2242
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Review/";
        private const string PrefabPath = Root + "PerforationWarden_Review_Iter2236.prefab";
        private const string ControllerPath = Root + "PerforationWarden_Review_Iter2236.controller";
        private const string ClipPath = Root + "PerforationWarden_CreaseBarrier_Aligned_Iter2242.anim";
        private static readonly float[] Times = { 0f, 0.18f, 0.40f, 0.56f, 0.82f, 1f };

        [MenuItem("Space & Crafts/Assets/Author Warden Crease Barrier Aligned Iter2242")]
        public static void Author()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ClipPath) != null)
                throw new InvalidOperationException("CreaseBarrier clip already exists; refusing to overwrite.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (prefab == null || controller == null) throw new FileNotFoundException("Warden review prefab/controller missing.");

            Scene scene = default;
            GameObject instance = null;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate review prefab.");
                var animator = instance.GetComponent<Animator>();
                if (animator == null) throw new InvalidOperationException("Review prefab Animator missing.");
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Play("Base Layer.Idle", 0, 0.12f);
                animator.Update(1f / 30f);

                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var rigRoot = animator.transform;
                var paths = transforms.ToDictionary(
                    transform => AnimationUtility.CalculateTransformPath(transform, rigRoot),
                    transform => transform,
                    StringComparer.Ordinal);
                var required = new[] { "Armature/Hips", "Armature/Hips/Spine02", "Armature/Hips/Spine02/Spine01", "Armature/Hips/Spine02/Spine01/Spine", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm" };
                var missing = required.Where(path => !paths.ContainsKey(path)).ToArray();
                if (missing.Length > 0) throw new InvalidOperationException($"Warden rig paths missing: {string.Join("|", missing)}");

                var rotations = new Dictionary<string, float[]>
                {
                    ["Armature/Hips/Spine02"] = new[] { 0f, 6f, -12f, -8f, -3f, 0f },
                    ["Armature/Hips/Spine02/Spine01"] = new[] { 0f, 8f, -18f, -12f, -4f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine"] = new[] { 0f, 4f, -10f, -6f, -2f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm"] = new[] { 0f, -18f, 16f, 12f, 0f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm"] = new[] { 0f, 25f, -28f, -20f, 0f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm"] = new[] { 0f, 18f, -16f, -12f, 0f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm"] = new[] { 0f, -25f, 28f, 20f, 0f, 0f },
                    ["Armature/Hips/Spine02/Spine01/Spine/neck/Head"] = new[] { 0f, -4f, 10f, 8f, 0f, 0f },
                };

                var clip = new AnimationClip
                {
                    name = "PerforationWarden_CreaseBarrier_Aligned_Iter2242",
                    frameRate = 30f,
                    legacy = false,
                    wrapMode = WrapMode.ClampForever
                };
                foreach (var pair in paths)
                {
                    SetVectorCurve(clip, pair.Key, "localPosition", component => GetPosition(pair.Value.localPosition, component));
                    var deltas = rotations.TryGetValue(pair.Key, out var values) ? values : new float[Times.Length];
                    SetRotationCurves(clip, pair.Key, pair.Value.localRotation, deltas, AxisFor(pair.Key));
                }

                AssetDatabase.CreateAsset(clip, ClipPath);
                var stateMachine = controller.layers[0].stateMachine;
                if (stateMachine.states.Any(state => state.state.name == "CreaseBarrier_Aligned"))
                    throw new InvalidOperationException("Controller already contains CreaseBarrier_Aligned state.");
                var state = stateMachine.AddState("CreaseBarrier_Aligned");
                state.motion = clip;
                state.writeDefaultValues = false;
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[WardenCreaseBarrierAligned2242] Authored review-only {ClipPath}; duration={clip.length:F2}s; keys={Times.Length}; locked feet, hips, root, and non-target transforms at sampled Idle pose; clip assigned to review controller only.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static float GetPosition(Vector3 position, int component) => component switch
        {
            0 => position.x,
            1 => position.y,
            _ => position.z
        };

        private static void SetVectorCurve(AnimationClip clip, string path, string property, Func<int, float> get)
        {
            var suffixes = new[] { ".x", ".y", ".z" };
            for (var component = 0; component < 3; component++)
            {
                var curve = new AnimationCurve(Times.Select(time => new Keyframe(time, get(component))).ToArray());
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property + suffixes[component]);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static void SetRotationCurves(AnimationClip clip, string path, Quaternion baseline, float[] degrees, Vector3 axis)
        {
            var x = new AnimationCurve(); var y = new AnimationCurve(); var z = new AnimationCurve(); var w = new AnimationCurve();
            for (var i = 0; i < Times.Length; i++)
            {
                var q = (baseline * Quaternion.AngleAxis(degrees[i], axis)).normalized;
                x.AddKey(new Keyframe(Times[i], q.x)); y.AddKey(new Keyframe(Times[i], q.y));
                z.AddKey(new Keyframe(Times[i], q.z)); w.AddKey(new Keyframe(Times[i], q.w));
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.x"), x);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.z"), z);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.w"), w);
        }

        private static Vector3 AxisFor(string path)
        {
            // Do not use Contains("Arm"): every rig path begins with "Armature".
            // The torso folds around its horizontal axis; only named arm/forearm joints swing around depth.
            if (path.Contains("/LeftShoulder/LeftArm") || path.Contains("/RightShoulder/RightArm")
                || path.EndsWith("/LeftForeArm", StringComparison.Ordinal)
                || path.EndsWith("/RightForeArm", StringComparison.Ordinal)) return Vector3.forward;
            return Vector3.right;
        }
    }
}
#endif

