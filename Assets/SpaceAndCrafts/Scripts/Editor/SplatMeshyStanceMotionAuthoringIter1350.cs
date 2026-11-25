#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Authors an isolated set of joint-rotation clips for Meshy's candidate
    /// skeleton. Unlike the delivered takes, these clips do not translate the
    /// hips/spine or scale the body. They are review assets only, not gameplay
    /// integrations.
    /// </summary>
    public static class SplatMeshyStanceMotionAuthoringIter1350
    {
        private const string CandidatePath =
            "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_Meshy_Iter1347_FaceSeated_Candidate.prefab";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1350";
        private const string ReportPath = "Logs/splat-meshy-joint-motion-iter1350.txt";

        [MenuItem("Space & Crafts/Author Splat Meshy Stance Motion Candidates")]
        public static void Generate()
        {
            var candidateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            if (candidateAsset == null)
                throw new InvalidOperationException("Missing isolated Splat candidate: " + CandidatePath);

            EnsureFolder(OutputFolder);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(candidateAsset);
            try
            {
                var bones = instance.GetComponentsInChildren<Transform>(true)
                    .Where(transform => IsTargetBone(transform.name))
                    .ToDictionary(transform => transform.name, StringComparer.Ordinal);
                var definitions = BuildDefinitions();
                var report = new List<string>
                {
                    "Locally authored joint-rotation candidates on the face-weight-repaired Meshy Generic skeleton.",
                    "Shoulder roots and limbs rotate; hips/spine/head/body scale and position tracks are absent.",
                    "Candidate clips are not assigned to the active player or gameplay scenes."
                };

                foreach (var definition in definitions)
                {
                    var clip = CreateClip(definition, bones);
                    var outputPath = OutputFolder + "/Joint_" + definition.Name + ".anim";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath) != null)
                        AssetDatabase.DeleteAsset(outputPath);
                    AssetDatabase.CreateAsset(clip, outputPath);
                    report.Add($"{clip.name}: length={clip.length:F3}s loop={definition.Loop} " +
                        $"jointTracks={definition.Tracks.Count} scaleTracks=0 positionTracks=0");
                }

                File.WriteAllLines(ReportPath, report);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[Splat joint motion] Authored Idle/Walk/Run/Jump/Land rotation candidates under " + OutputFolder);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }

            EditorApplication.Exit(0);
        }

        private static AnimationClip CreateClip(ClipDefinition definition, Dictionary<string, Transform> bones)
        {
            var clip = new AnimationClip
            {
                name = "Joint_" + definition.Name,
                frameRate = 30f,
                wrapMode = definition.Loop ? WrapMode.Loop : WrapMode.ClampForever,
                legacy = false,
            };

            foreach (var track in definition.Tracks)
            {
                if (!bones.TryGetValue(track.Bone, out var bone) || bone.parent == null)
                    throw new InvalidOperationException("Missing animation bone: " + track.Bone);
                var relativePath = AnimationUtility.CalculateTransformPath(bone, bone.root);
                var localAxis = Quaternion.Inverse(bone.parent.rotation) * track.WorldAxis.normalized;
                var bindRotation = bone.localRotation;
                var components = new List<float>[4] { new(), new(), new(), new() };
                for (var index = 0; index < definition.Times.Length; index++)
                {
                    var keyRotation = Quaternion.AngleAxis(track.Angles[index], localAxis) * bindRotation;
                    components[0].Add(keyRotation.x);
                    components[1].Add(keyRotation.y);
                    components[2].Add(keyRotation.z);
                    components[3].Add(keyRotation.w);
                }

                var properties = new[]
                {
                    "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
                };
                for (var axis = 0; axis < properties.Length; axis++)
                {
                    var keys = new Keyframe[definition.Times.Length];
                    for (var key = 0; key < keys.Length; key++)
                        keys[key] = new Keyframe(definition.Times[key], components[axis][key]);
                    var curve = new AnimationCurve(keys)
                    {
                        preWrapMode = definition.Loop ? WrapMode.Loop : WrapMode.ClampForever,
                        postWrapMode = definition.Loop ? WrapMode.Loop : WrapMode.ClampForever,
                    };
                    AnimationUtility.SetEditorCurve(clip,
                        EditorCurveBinding.FloatCurve(relativePath, typeof(Transform), properties[axis]), curve);
                }
            }

            clip.EnsureQuaternionContinuity();
            return clip;
        }

        private static List<ClipDefinition> BuildDefinitions()
        {
            var walkTimes = new[] { 0f, 0.2f, 0.4f, 0.6f, 0.8f };
            var runTimes = new[] { 0f, 0.14f, 0.28f, 0.42f, 0.56f };
            var idleTimes = new[] { 0f, 0.9f, 1.8f, 2.7f, 3.6f };
            var jumpTimes = new[] { 0f, 0.12f, 0.4f, 0.75f };
            var landTimes = new[] { 0f, 0.08f, 0.2f, 0.36f };

            return new List<ClipDefinition>
            {
                new("Idle", idleTimes, true,
                    Track("LeftShoulder", Vector3.forward, 16, 18, 16, 14, 16),
                    Track("RightShoulder", Vector3.forward, -16, -18, -16, -14, -16),
                    Track("LeftArm", Vector3.forward, 2, 0, -2, 0, 2),
                    Track("RightArm", Vector3.forward, -2, 0, 2, 0, -2)),
                new("Walk", walkTimes, true,
                    Track("LeftShoulder", Vector3.forward, 25, 19, 11, 19, 25),
                    Track("RightShoulder", Vector3.forward, -11, -19, -25, -19, -11),
                    Track("LeftUpLeg", Vector3.right, -20, -8, 20, 8, -20),
                    Track("RightUpLeg", Vector3.right, 20, 8, -20, -8, 20),
                    Track("LeftLeg", Vector3.right, 11, 6, 0, 6, 11),
                    Track("RightLeg", Vector3.right, 0, 6, 11, 6, 0),
                    Track("LeftArm", Vector3.forward, 8, 3, -8, -3, 8),
                    Track("RightArm", Vector3.forward, -8, -3, 8, 3, -8),
                    Track("LeftForeArm", Vector3.forward, -5, -8, -5, -2, -5),
                    Track("RightForeArm", Vector3.forward, 5, 8, 5, 2, 5)),
                new("Run", runTimes, true,
                    Track("LeftShoulder", Vector3.forward, 30, 20, 10, 20, 30),
                    Track("RightShoulder", Vector3.forward, -10, -20, -30, -20, -10),
                    Track("LeftUpLeg", Vector3.right, -28, -12, 28, 12, -28),
                    Track("RightUpLeg", Vector3.right, 28, 12, -28, -12, 28),
                    Track("LeftLeg", Vector3.right, 18, 9, 2, 9, 18),
                    Track("RightLeg", Vector3.right, 2, 9, 18, 9, 2),
                    Track("LeftArm", Vector3.forward, 12, 4, -12, -4, 12),
                    Track("RightArm", Vector3.forward, -12, -4, 12, 4, -12),
                    Track("LeftForeArm", Vector3.forward, -7, -11, -7, -3, -7),
                    Track("RightForeArm", Vector3.forward, 7, 11, 7, 3, 7)),
                new("Regular_Jump", jumpTimes, false,
                    Track("LeftShoulder", Vector3.forward, 16, 4, -2, 16),
                    Track("RightShoulder", Vector3.forward, -16, -4, 2, -16),
                    Track("LeftUpLeg", Vector3.right, 0, -11, -15, 0),
                    Track("RightUpLeg", Vector3.right, 0, 11, 15, 0),
                    Track("LeftLeg", Vector3.right, 0, 12, 18, 0),
                    Track("RightLeg", Vector3.right, 0, 12, 18, 0),
                    Track("LeftArm", Vector3.forward, 0, -14, -20, 0),
                    Track("RightArm", Vector3.forward, 0, 14, 20, 0)),
                new("Land", landTimes, false,
                    Track("LeftShoulder", Vector3.forward, 17, 25, 14, 16),
                    Track("RightShoulder", Vector3.forward, -17, -25, -14, -16),
                    Track("LeftUpLeg", Vector3.right, 0, 0, -2, 0),
                    Track("RightUpLeg", Vector3.right, 0, 0, 2, 0),
                    Track("LeftLeg", Vector3.right, 7, 14, 6, 0),
                    Track("RightLeg", Vector3.right, 7, 14, 6, 0),
                    Track("LeftArm", Vector3.forward, -4, -11, -3, 0),
                    Track("RightArm", Vector3.forward, 4, 11, 3, 0),
                    Track("LeftForeArm", Vector3.forward, -3, -7, -2, 0),
                    Track("RightForeArm", Vector3.forward, 3, 7, 2, 0)),
            };
        }

        private static JointTrack Track(string bone, Vector3 axis, params float[] angles) =>
            new(bone, axis, angles);

        private static bool IsTargetBone(string name) =>
            name is "LeftShoulder" or "RightShoulder" or "LeftArm" or "LeftForeArm" or "LeftHand" or "RightArm" or "RightForeArm" or "RightHand" or
                "LeftUpLeg" or "LeftLeg" or "LeftFoot" or "LeftToeBase" or "RightUpLeg" or "RightLeg" or
                "RightFoot" or "RightToeBase";

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

        private sealed class ClipDefinition
        {
            public readonly string Name;
            public readonly float[] Times;
            public readonly bool Loop;
            public readonly List<JointTrack> Tracks;

            public ClipDefinition(string name, float[] times, bool loop, params JointTrack[] tracks)
            {
                Name = name;
                Times = times;
                Loop = loop;
                Tracks = tracks.ToList();
                if (Tracks.Any(track => track.Angles.Length != Times.Length))
                    throw new InvalidOperationException("Joint key count must match time count in " + name);
            }
        }

        private sealed class JointTrack
        {
            public readonly string Bone;
            public readonly Vector3 WorldAxis;
            public readonly float[] Angles;

            public JointTrack(string bone, Vector3 worldAxis, float[] angles)
            {
                Bone = bone;
                WorldAxis = worldAxis;
                Angles = angles;
            }
        }
    }
}
#endif
