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
    public static class SplatMeshyStableMotionAuthoringCli
    {
        private const string CandidatePath =
            "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_Meshy_Iter1337_Candidate.prefab";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1346";
        private const string ReportPath = "Logs/splat-meshy-joint-motion-iter1346.txt";

        [MenuItem("Space & Crafts/Author Splat Meshy Joint Motion Candidates")]
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
                    "Locally authored joint-rotation candidates on Meshy's Generic skeleton.",
                    "Only limb bones rotate; hips/spine/head and all body scale/position tracks are absent.",
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
                    Track("LeftArm", Vector3.forward, 0, 2, 0, -1, 0),
                    Track("RightArm", Vector3.forward, 0, -2, 0, 1, 0),
                    Track("LeftForeArm", Vector3.up, 0, 1.5f, 0, -1.5f, 0),
                    Track("RightForeArm", Vector3.up, 0, -1.5f, 0, 1.5f, 0)),
                new("Walk", walkTimes, true,
                    Track("LeftUpLeg", Vector3.right, -18, -8, 18, 8, -18),
                    Track("RightUpLeg", Vector3.right, 18, 8, -18, -8, 18),
                    Track("LeftLeg", Vector3.right, 12, 5, 0, 5, 12),
                    Track("RightLeg", Vector3.right, 0, 5, 12, 5, 0),
                    Track("LeftArm", Vector3.up, 10, 5, -10, -5, 10),
                    Track("RightArm", Vector3.up, -10, -5, 10, 5, -10),
                    Track("LeftForeArm", Vector3.forward, -4, 0, 4, 0, -4),
                    Track("RightForeArm", Vector3.forward, 4, 0, -4, 0, 4)),
                new("Run", runTimes, true,
                    Track("LeftUpLeg", Vector3.right, -27, -12, 27, 12, -27),
                    Track("RightUpLeg", Vector3.right, 27, 12, -27, -12, 27),
                    Track("LeftLeg", Vector3.right, 20, 5, 0, 5, 20),
                    Track("RightLeg", Vector3.right, 0, 5, 20, 5, 0),
                    Track("LeftArm", Vector3.up, 18, 8, -18, -8, 18),
                    Track("RightArm", Vector3.up, -18, -8, 18, 8, -18),
                    Track("LeftForeArm", Vector3.forward, -8, 0, 8, 0, -8),
                    Track("RightForeArm", Vector3.forward, 8, 0, -8, 0, 8)),
                new("Regular_Jump", jumpTimes, false,
                    Track("LeftUpLeg", Vector3.right, 0, -10, -14, 0),
                    Track("RightUpLeg", Vector3.right, 0, 10, 14, 0),
                    Track("LeftLeg", Vector3.right, 0, 12, 18, 0),
                    Track("RightLeg", Vector3.right, 0, 12, 18, 0),
                    Track("LeftArm", Vector3.forward, 0, -16, -24, 0),
                    Track("RightArm", Vector3.forward, 0, 16, 24, 0),
                    Track("LeftForeArm", Vector3.forward, 0, -8, -12, 0),
                    Track("RightForeArm", Vector3.forward, 0, 8, 12, 0)),
                new("Land", landTimes, false,
                    Track("LeftUpLeg", Vector3.right, 0, 0, 0, 0),
                    Track("RightUpLeg", Vector3.right, 0, 0, 0, 0),
                    Track("LeftLeg", Vector3.right, 8, 12, 6, 0),
                    Track("RightLeg", Vector3.right, 8, 12, 6, 0),
                    Track("LeftArm", Vector3.forward, -10, -16, -8, 0),
                    Track("RightArm", Vector3.forward, 10, 16, 8, 0),
                    Track("LeftForeArm", Vector3.forward, -4, -7, -3, 0),
                    Track("RightForeArm", Vector3.forward, 4, 7, 3, 0)),
            };
        }

        private static JointTrack Track(string bone, Vector3 axis, params float[] angles) =>
            new(bone, axis, angles);

        private static bool IsTargetBone(string name) =>
            name is "LeftArm" or "LeftForeArm" or "LeftHand" or "RightArm" or "RightForeArm" or "RightHand" or
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
