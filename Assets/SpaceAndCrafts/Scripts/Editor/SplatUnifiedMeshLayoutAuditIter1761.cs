#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only topology, renderer, palette and skin-weight inventory for the Iter1717 FBX.</summary>
    public static class SplatUnifiedMeshLayoutAuditIter1761
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/Splat_UnifiedRig_Candidate_Iter1717.fbx";

        public static void Inspect()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Missing Iter1717 source FBX: " + ModelPath);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            Debug.Log($"[Splat mesh layout Iter1761] source={ModelPath}; isFbxBinary=true; importerReadWrite={importer?.isReadable}; preserveHierarchy={importer?.preserveHierarchy}; maxBones={importer?.maxBonesPerVertex}");

            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"[Splat mesh layout Iter1761] root={model.name}; skinnedRendererCount={renderers.Length}; totalRendererCount={model.GetComponentsInChildren<Renderer>(true).Length}");
            foreach (var renderer in renderers)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    Debug.Log($"[Splat mesh layout Iter1761] renderer={renderer.name}; mesh=null");
                    continue;
                }

                var submesh = new StringBuilder();
                for (var i = 0; i < mesh.subMeshCount; i++)
                    submesh.Append($"[{i}:{mesh.GetTopology(i)},indices={mesh.GetIndexCount(i)}]");
                var palette = SummarizeColors(mesh);
                var boneNames = renderer.bones == null
                    ? "none"
                    : string.Join(",", renderer.bones.Select(b => b != null ? b.name : "null"));
                Debug.Log($"[Splat mesh layout Iter1761] renderer={renderer.name}; mesh={mesh.name}; verts={mesh.vertexCount}; subMeshes={mesh.subMeshCount}; submeshInfo={submesh}; bounds={mesh.bounds}; hasColor={mesh.HasVertexAttribute(VertexAttribute.Color)}; colors={palette}; materials={renderer.sharedMaterials.Length}; materialNames={string.Join(",", renderer.sharedMaterials.Select(m => m != null ? m.name : "null"))}; boneCount={renderer.bones.Length}; rootBone={renderer.rootBone?.name ?? "null"}; bones={boneNames}");
                SummarizeWeights(renderer);
            }

            foreach (var name in new[] { "Body", "Arm_L", "Hand_L", "Arm_R", "Hand_R", "Leg_L", "Foot_L", "Leg_R", "Foot_R" })
            {
                var found = model.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
                foreach (var t in found)
                    Debug.Log($"[Splat mesh layout Iter1761] bone={name}; path={Path(t)}; localPosition={t.localPosition:F4}; localScale={t.localScale:F4}");
            }
            EditorApplication.Exit(0);
        }

        private static string SummarizeColors(Mesh mesh)
        {
            if (!mesh.HasVertexAttribute(VertexAttribute.Color)) return "none";
            var colors = mesh.colors32;
            var bins = new Dictionary<string, int>();
            foreach (var c in colors)
            {
                var key = $"{(c.r / 16) * 16:X2}{(c.g / 16) * 16:X2}{(c.b / 16) * 16:X2}";
                bins.TryGetValue(key, out var n);
                bins[key] = n + 1;
            }
            return string.Join(",", bins.OrderByDescending(p => p.Value).Take(10).Select(p => $"#{p.Key}:{p.Value}"));
        }

        private static void SummarizeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null || renderer.bones == null) return;
            using NativeArray<byte> perVertex = mesh.GetBonesPerVertex();
            using NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            var totals = new float[renderer.bones.Length];
            var influenced = new int[renderer.bones.Length];
            var strong = new int[renderer.bones.Length];
            var index = 0;
            for (var vertex = 0; vertex < perVertex.Length; vertex++)
            {
                for (var j = 0; j < perVertex[vertex] && index < weights.Length; j++, index++)
                {
                    var w = weights[index];
                    if (w.boneIndex < 0 || w.boneIndex >= totals.Length || w.weight <= 0f) continue;
                    totals[w.boneIndex] += w.weight;
                    influenced[w.boneIndex]++;
                    if (w.weight >= 0.25f) strong[w.boneIndex]++;
                }
            }
            var summary = Enumerable.Range(0, totals.Length)
                .Where(i => totals[i] > 0f)
                .OrderByDescending(i => totals[i])
                .Take(16)
                .Select(i => $"{renderer.bones[i]?.name ?? "null"}:total={totals[i]:F1},verts={influenced[i]},w>=.25={strong[i]}");
            Debug.Log($"[Splat mesh layout Iter1761] weights renderer={renderer.name}; {string.Join(";", summary)}");
        }

        private static string Path(Transform t)
        {
            var parts = new Stack<string>();
            while (t != null) { parts.Push(t.name); t = t.parent; }
            return string.Join("/", parts);
        }
    }
}
#endif
