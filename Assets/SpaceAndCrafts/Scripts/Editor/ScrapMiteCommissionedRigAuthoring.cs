#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SpaceAndCrafts.AI;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds a transform-pivot rig from the disconnected parts of the commissioned
    /// Meshy Scrap Mite mesh. It does not generate geometry or consume Meshy credits.
    /// </summary>
    public static class ScrapMiteCommissionedRigAuthoring
    {
        private const string SourcePrefabPath = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/ScrapMite_URP.prefab";
        private const string MeshFolder = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/ScrapMite/RiggedParts";
        private const string PrefabFolder = "Assets/Resources/SpaceAndCrafts/Enemies";
        private const string PrefabPath = PrefabFolder + "/ScrapMite_Rigged.prefab";
        private static readonly Vector3 VisualSeat = Vector3.up * 0.5f;

        [MenuItem("Space & Crafts/Build Commissioned Scrap Mite Pivot Rig (Iter959)")]
        public static void BuildIter959()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            var sourceFilter = source != null ? source.GetComponent<MeshFilter>() : null;
            var sourceRenderer = source != null ? source.GetComponent<MeshRenderer>() : null;
            var sourceMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (sourceMesh == null || sourceRenderer == null || sourceRenderer.sharedMaterial == null)
            {
                Debug.LogError($"[Scrap Mite rig] Missing mesh or material in {SourcePrefabPath}.");
                EditorApplication.Exit(2);
                return;
            }

            EnsureFolder(MeshFolder);
            EnsureFolder(PrefabFolder);
            foreach (var assetPath in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(assetPath);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            }

            var meshRoots = FindMeshIslands(sourceMesh);
            var vertices = sourceMesh.vertices;
            var normals = sourceMesh.normals;
            var uvs = new List<Vector2>();
            sourceMesh.GetUVs(0, uvs);
            var root = new GameObject("ScrapMite_Rigged");
            try
            {
                var visual = new GameObject("ART_MiteVisualRoot").transform;
                visual.SetParent(root.transform, false);
                visual.localPosition = VisualSeat;

                var bodyPivot = new Vector3(0f, -0.1564f, 0.1030f);
                var headPivot = new Vector3(0f, 0.1378f, 0.1493f);
                var body = CreateBone(visual, "ART_MiteBody", bodyPivot);
                var shell = CreateBone(visual, "ART_MiteShell", new Vector3(0f, 0.0092f, -0.1543f));
                // The imported rear shell dominates the head and hides the eyes
                // through most patrol turns. Keep the commissioned shell intact,
                // but reduce its pivot uniformly so the face and limbs read more
                // clearly without changing the Meshy geometry or rig proportions.
                shell.localScale = Vector3.one * 0.82f;
                var head = CreateBone(body, "ART_MiteHead", headPivot - bodyPivot);
                var leftLegPivot = new Vector3(-0.1543f, -0.14f, 0.12f);
                var rightLegPivot = new Vector3(0.1604f, -0.14f, 0.12f);
                var leftLeg = CreateBone(visual, "ART_MiteLeg_0", leftLegPivot);
                var rightLeg = CreateBone(visual, "ART_MiteLeg_1", rightLegPivot);
                var leftArmPivot = new Vector3(-0.12f, -0.13f, 0.12f);
                var rightArmPivot = new Vector3(0.12f, -0.13f, 0.12f);
                var leftArm = CreateBone(body, "ART_MiteArm_L", leftArmPivot - bodyPivot);
                var rightArm = CreateBone(body, "ART_MiteArm_R", rightArmPivot - bodyPivot);
                // The imported shin/foot islands sit around y=-0.47 m and have
                // about 0.10 m height. Give each lower segment a real ankle pivot
                // near the existing thigh/foot overlap instead of rotating the
                // entire two-island leg as one stiff piece.
                var leftFoot = CreateBone(leftLeg, "ART_MiteFoot_L", new Vector3(-0.0657f, -0.25f, 0f));
                var rightFoot = CreateBone(rightLeg, "ART_MiteFoot_R", new Vector3(0.0596f, -0.25f, 0f));
                var antenna = CreateBone(head, "ART_MiteAntenna", new Vector3(0f, 0.31f, 0.24f) - headPivot);
                var leftEye = CreateBone(head, "ART_MiteEye", new Vector3(-0.0966f, 0.1718f, 0.2948f) - headPivot);
                var rightEye = CreateBone(head, "ART_MiteEye", new Vector3(0.1052f, 0.1714f, 0.2951f) - headPivot);
                var leftPupil = CreateBone(leftEye, "ART_MitePupil", new Vector3(-0.0699f, 0.2890f, 0.1814f)
                    - new Vector3(-0.0966f, 0.1718f, 0.2948f));
                var rightPupil = CreateBone(rightEye, "ART_MitePupil", new Vector3(0.0790f, 0.2894f, 0.1815f)
                    - new Vector3(0.1052f, 0.1714f, 0.2951f));

                for (var islandIndex = 0; islandIndex < meshRoots.Count; islandIndex++)
                {
                    var rootIndex = meshRoots[islandIndex];
                    var triangleList = meshRoots.Triangles[rootIndex];
                    var partPivot = meshRoots.Pivots[islandIndex];
                    var partMesh = BuildPartMesh(source, sourceFilter, sourceMesh, vertices, normals,
                        uvs, triangleList, partPivot, islandIndex);
                    var meshPath = $"{MeshFolder}/ScrapMitePart_{islandIndex:00}.asset";
                    AssetDatabase.CreateAsset(partMesh, meshPath);
                    var partParent = ParentForPart(islandIndex, body, shell, head, leftLeg, rightLeg,
                        leftFoot, rightFoot, leftArm, rightArm, antenna, leftEye, rightEye, leftPupil, rightPupil);
                    AddPart(partParent, partMesh, sourceRenderer.sharedMaterial, partPivot);
                }

                root.AddComponent<ScrapMiteCommissionedAnimator>();
                AssetDatabase.SaveAssets();
                AssetDatabase.DeleteAsset(PrefabPath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Scrap Mite rig] Iteration 959 authored {meshRoots.Count} source-mesh islands with pivot-local vertices, side-matched leg pivots, and articulated ankle pivots from {SourcePrefabPath}; prefab saved at {PrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Diagnose Scrap Mite Source Islands (Iter955)")]
        public static void DiagnoseSourceIslandsIter955()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            var sourceFilter = source != null ? source.GetComponent<MeshFilter>() : null;
            var mesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (mesh == null)
            {
                Debug.LogError($"[Scrap Mite islands] Missing source mesh in {SourcePrefabPath}.");
                EditorApplication.Exit(2);
                return;
            }

            var islands = FindMeshIslands(mesh);
            var vertices = mesh.vertices;
            for (var islandIndex = 0; islandIndex < islands.Count; islandIndex++)
            {
                var rootIndex = islands[islandIndex];
                var triangles = islands.Triangles[rootIndex];
                var bounds = new Bounds(vertices[triangles[0]], Vector3.zero);
                for (var i = 1; i < triangles.Count; i++) bounds.Encapsulate(vertices[triangles[i]]);
                Debug.Log($"[Scrap Mite islands] index={islandIndex:D2}, pivotMeters={islands.Pivots[islandIndex]:F3}, "
                    + $"sourceBounds={bounds.center:F3}/{bounds.size:F3}, triangles={triangles.Count / 3}.");
            }

            Debug.Log($"[Scrap Mite islands] source={SourcePrefabPath}, vertices={vertices.Length}, "
                + $"triangleCount={mesh.triangles.Length / 3}, disconnectedIslands={islands.Count}.");
            EditorApplication.Exit(0);
        }

        private sealed class MeshIslandSet : List<int>
        {
            public readonly Dictionary<int, List<int>> Triangles = new();
            public readonly List<Vector3> Pivots = new();
        }

        private static MeshIslandSet FindMeshIslands(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var parent = new int[vertices.Length];
            var welded = new Dictionary<Vector3Int, int>(vertices.Length);
            for (var i = 0; i < vertices.Length; i++)
            {
                parent[i] = i;
                var v = vertices[i];
                var key = new Vector3Int(Mathf.RoundToInt(v.x * 100000f),
                    Mathf.RoundToInt(v.y * 100000f), Mathf.RoundToInt(v.z * 100000f));
                if (welded.TryGetValue(key, out var existing)) Union(parent, i, existing);
                else welded.Add(key, i);
            }

            var triangles = mesh.triangles;
            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                Union(parent, triangles[i], triangles[i + 1]);
                Union(parent, triangles[i], triangles[i + 2]);
            }

            var byRoot = new Dictionary<int, List<int>>();
            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                var root = Find(parent, triangles[i]);
                if (!byRoot.TryGetValue(root, out var list)) byRoot.Add(root, list = new List<int>());
                list.Add(triangles[i]);
                list.Add(triangles[i + 1]);
                list.Add(triangles[i + 2]);
            }

            var roots = new List<int>(byRoot.Keys);
            roots.Sort((a, b) => byRoot[b].Count.CompareTo(byRoot[a].Count));
            var result = new MeshIslandSet();
            for (var i = 0; i < roots.Count; i++)
            {
                var bounds = new Bounds(vertices[byRoot[roots[i]][0]], Vector3.zero);
                for (var j = 1; j < byRoot[roots[i]].Count; j++)
                    bounds.Encapsulate(vertices[byRoot[roots[i]][j]]);
                // The source prefab root scale is 100; store part geometry in metres.
                result.Add(roots[i]);
                result.Triangles.Add(roots[i], byRoot[roots[i]]);
                result.Pivots.Add(bounds.center * 100f);
            }
            return result;
        }

        private static Mesh BuildPartMesh(GameObject source, MeshFilter sourceFilter, Mesh sourceMesh,
            Vector3[] vertices, Vector3[] normals, List<Vector2> uvs, List<int> triangles,
            Vector3 partPivotMeters, int islandIndex)
        {
            var remap = new Dictionary<int, int>();
            var partVertices = new List<Vector3>();
            var partNormals = new List<Vector3>();
            var partUvs = new List<Vector2>();
            var partTriangles = new List<int>(triangles.Count);
            for (var i = 0; i < triangles.Count; i++)
            {
                var sourceIndex = triangles[i];
                if (!remap.TryGetValue(sourceIndex, out var partIndex))
                {
                    partIndex = partVertices.Count;
                    remap.Add(sourceIndex, partIndex);
                    var rootLocalMeters = (sourceFilter.transform.TransformPoint(vertices[sourceIndex])
                        - source.transform.position);
                    // Store vertices relative to this island's pivot so the
                    // authored anatomy bone can rotate the intact source island
                    // without translating the rest pose away from its Meshy pose.
                    partVertices.Add(rootLocalMeters - partPivotMeters);
                    if (normals.Length == vertices.Length)
                    {
                        partNormals.Add(source.transform.InverseTransformDirection(
                            sourceFilter.transform.TransformDirection(normals[sourceIndex])).normalized);
                    }
                    if (uvs.Count == vertices.Length) partUvs.Add(uvs[sourceIndex]);
                }
                partTriangles.Add(partIndex);
            }

            var part = new Mesh { name = $"ScrapMitePart_{islandIndex:00}" };
            part.SetVertices(partVertices);
            part.SetTriangles(partTriangles, 0);
            if (partNormals.Count == partVertices.Count) part.SetNormals(partNormals);
            else part.RecalculateNormals();
            if (partUvs.Count == partVertices.Count) part.SetUVs(0, partUvs);
            part.RecalculateBounds();
            return part;
        }

        private static Transform ParentForPart(int index, Transform body, Transform shell, Transform head,
            Transform leftLeg, Transform rightLeg, Transform leftFoot, Transform rightFoot,
            Transform leftArm, Transform rightArm, Transform antenna,
            Transform leftEye, Transform rightEye, Transform leftPupil, Transform rightPupil)
        {
            switch (index)
            {
                case 0:
                case 20: return shell;
                // The commissioned mesh's positive-X leg islands belong to the
                // positive-X pivot in this +Z-facing rig.
                // The previous index-only mapping sent each leg to the opposite
                // pivot, crossing the gait and pulling the separated export pieces
                // through the body.
                case 1:
                    return rightLeg;
                case 2:
                    return leftLeg;
                case 7: return rightFoot;
                case 8: return leftFoot;
                case 3:
                case 12: return head;
                case 4: return leftArm;
                case 5: return rightArm;
                case 6:
                case 11:
                case 13:
                case 14:
                case 19:
                case 24:
                case 26:
                case 27: return body;
                case 9: return leftEye;
                case 10: return rightEye;
                case 15:
                case 16:
                case 17:
                case 18: return antenna;
                case 21: return leftPupil;
                case 22: return rightPupil;
                case 23: return leftArm;
                case 25: return rightArm;
                case 28: return rightArm;
                case 29: return leftArm;
                default: return body;
            }
        }

        private static Transform CreateBone(Transform parent, string name, Vector3 localPosition)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = localPosition;
            return bone;
        }

        private static void AddPart(Transform parent, Mesh mesh, Material material, Vector3 pivotMeters)
        {
            var part = new GameObject(mesh.name);
            part.transform.SetParent(parent, false);
            // Parent bones are positioned in the rig's visual-root coordinate
            // space. Convert the original island pivot into that parent's local
            // space; vertices inside the part mesh are already pivot-local.
            part.transform.localPosition = parent.InverseTransformPoint(pivotMeters);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static int Find(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }

        private static void Union(int[] parent, int left, int right)
        {
            var leftRoot = Find(parent, left);
            var rightRoot = Find(parent, right);
            if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
