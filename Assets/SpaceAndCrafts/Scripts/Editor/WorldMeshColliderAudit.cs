using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Audits instantiated world model meshes and their collider coverage.</summary>
    public static class WorldMeshColliderAudit
    {
        private const string WorldSceneFolder = "Assets/SpaceAndCrafts/Scenes/Worlds";
        private const int CameraOccluderLayer = 31;

        public static void Run()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { WorldSceneFolder });
            Array.Sort(sceneGuids, StringComparer.Ordinal);
            var report = new StringBuilder();
            report.AppendLine("Instantiated world mesh collider audit");
            report.AppendLine("Collider coverage includes colliders on the mesh object or its ancestors.");

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                AuditScene(scene, report);
            }

            var outputPath = Path.Combine("Logs", "WorldMeshColliderAudit.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Logs");
            File.WriteAllText(outputPath, report.ToString());
            Debug.Log($"[WorldMeshColliderAudit] Wrote {outputPath}\n{report}");
        }

        private static void AuditScene(Scene scene, StringBuilder report)
        {
            var allMeshes = 0;
            var meshesWithCollider = 0;
            var meshesWithoutCollider = 0;
            var artMeshesWithoutCollider = 0;
            var cameraOccluders = 0;
            var missingArtColliderNames = new List<string>();

            foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (filter == null || filter.sharedMesh == null || filter.gameObject.scene != scene)
                    continue;
                var renderer = filter.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                    continue;

                allMeshes++;
                if (filter.gameObject.layer == CameraOccluderLayer)
                    cameraOccluders++;

                if (HasColliderInHierarchy(filter.transform))
                {
                    meshesWithCollider++;
                }
                else
                {
                    meshesWithoutCollider++;
                    if (filter.name.StartsWith("ART_", StringComparison.OrdinalIgnoreCase))
                    {
                        artMeshesWithoutCollider++;
                        if (missingArtColliderNames.Count < 40)
                            missingArtColliderNames.Add(filter.name);
                    }
                }
            }

            report.AppendLine($"{scene.name}: renderedMeshes={allMeshes}, meshesWithCollider={meshesWithCollider}, " +
                $"meshesWithoutCollider={meshesWithoutCollider}, artMeshesWithoutCollider={artMeshesWithoutCollider}, " +
                $"cameraOccluderMeshes={cameraOccluders}");
            foreach (var name in missingArtColliderNames)
                report.AppendLine($"  ART WITHOUT COLLIDER (review walkability): {name}");
        }

        private static bool HasColliderInHierarchy(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.GetComponent<Collider>() != null)
                    return true;
            }
            return false;
        }
    }
}
