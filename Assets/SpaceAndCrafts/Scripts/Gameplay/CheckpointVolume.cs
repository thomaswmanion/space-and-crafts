using System.Collections.Generic;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointVolume : MonoBehaviour
    {
        [SerializeField] private Transform checkpointPose;
        [SerializeField] private string checkpointId;

        public Transform Pose => checkpointPose != null ? checkpointPose : transform;
        public string StableId => "volume:" + (string.IsNullOrWhiteSpace(checkpointId)
            ? BuildHierarchyPath(transform)
            : checkpointId);

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null)
            {
                return;
            }

            var pose = Pose;
            if (!respawn.SetCheckpoint(pose.position, pose.rotation, StableId))
            {
                return;
            }

            GameEvents.RaiseCheckpointReached(checkpointId);
            GameplayFeedback.Toast("Checkpoint!");
            TempCraftSparkBurst.Spawn(pose.position + Vector3.up * 0.5f, 6, 0.3f);
        }

        public static string BuildHierarchyPath(Transform node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            var names = new List<string>();
            for (var current = node; current != null; current = current.parent)
            {
                names.Add($"{EscapePathPart(current.name)}#{current.GetSiblingIndex()}");
            }

            names.Reverse();
            return string.Join("/", names);
        }

        public static Transform FindTransformByHierarchyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindTransformRecursive(roots[i].transform, path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindTransformRecursive(Transform candidate, string path)
        {
            if (candidate != null && BuildHierarchyPath(candidate) == path)
            {
                return candidate;
            }

            if (candidate == null)
            {
                return null;
            }

            for (var i = 0; i < candidate.childCount; i++)
            {
                var found = FindTransformRecursive(candidate.GetChild(i), path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string EscapePathPart(string value) =>
            (value ?? string.Empty).Replace("%", "%25").Replace("/", "%2F");
    }
}
