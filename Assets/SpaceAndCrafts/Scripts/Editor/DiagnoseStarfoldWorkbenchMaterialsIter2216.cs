#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only report of Starfold Workbench mesh UV and material state after scene reload.</summary>
    public static class DiagnoseStarfoldWorkbenchMaterialsIter2216
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/WorkshopHub_StarfoldWorkbench_Iter2212.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench_base_color.png";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-room-iter2216/MATERIAL_DIAGNOSTIC.txt";

        [MenuItem("Space & Crafts/Diagnose Starfold Workbench UV Materials (Iter2216)")]
        public static void Diagnose()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(root, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before read-only asset diagnostics.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine("Starfold Workbench texture/UV material diagnostic — Iter2216 (read-only)");
            sb.AppendLine($"Scene: {scene.path}");
            var sceneObject = GameObject.Find("ART_Workbench_Starfold_Meshy_Iter2212_REVIEW");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var savedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SpaceAndCrafts/Materials/ReviewOnly/StarfoldWorkbench_Iter2212.mat");
            sb.AppendLine($"Texture: path={AssetDatabase.GetAssetPath(texture)}; imported size={(texture != null ? $"{texture.width}x{texture.height}" : "missing")}; colorSpace={(texture != null && texture.isDataSRGB ? "sRGB" : "linear/unknown")}; wrapU={(texture != null ? texture.wrapModeU.ToString() : "n/a")}; wrapV={(texture != null ? texture.wrapModeV.ToString() : "n/a")}.");
            ReportObject("source FBX", source, sb);
            ReportObject("reopened scene instance", sceneObject, sb);
            if (savedMaterial != null)
            {
                sb.AppendLine($"Saved material: {AssetDatabase.GetAssetPath(savedMaterial)}; shader={savedMaterial.shader?.name}; BaseMap={savedMaterial.GetTexture("_BaseMap")?.name ?? "none"}; MainTex={savedMaterial.GetTexture("_MainTex")?.name ?? "none"}; baseColor={savedMaterial.GetColor("_BaseColor")}; keywords={string.Join(",", savedMaterial.shaderKeywords)}.");
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            foreach (var asset in allAssets.Where(a => a is Material))
            {
                var mat = (Material)asset;
                sb.AppendLine($"Embedded material: name={mat.name}; shader={mat.shader?.name}; BaseMap={mat.GetTexture("_BaseMap")?.name ?? "none"}; MainTex={mat.GetTexture("_MainTex")?.name ?? "none"}; baseColor={(mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white)}.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(ReportPath))!);
            File.WriteAllText(Abs(ReportPath), sb.ToString());
            Debug.Log("[DiagnoseStarfoldWorkbenchMaterialsIter2216] read-only UV/material report saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void ReportObject(string label, GameObject go, StringBuilder sb)
        {
            if (go == null) { sb.AppendLine($"{label}: missing"); return; }
            sb.AppendLine($"{label}: path={AssetDatabase.GetAssetPath(go)}; rootScale={go.transform.localScale}; rendererBounds={BoundsOf(go)}.");
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine($" renderer={renderer.name}; type={renderer.GetType().Name}; mats=[{string.Join(" | ", renderer.sharedMaterials.Select(m => m == null ? "null" : $"{m.name} ({m.shader?.name}; base={m.GetTexture("_BaseMap")?.name ?? "none"}; main={m.GetTexture("_MainTex")?.name ?? "none"})"))}]");
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh ?? (renderer as SkinnedMeshRenderer)?.sharedMesh;
                if (mesh == null) { sb.AppendLine("  mesh: missing"); continue; }
                sb.AppendLine($"  mesh={mesh.name}; vertices={mesh.vertexCount}; submeshes={mesh.subMeshCount}; triangles={mesh.triangles.Length / 3}; uv0={UvSummary(mesh.uv)}; uv1={UvSummary(mesh.uv2)}; uv2={UvSummary(mesh.uv3)}; normals={mesh.normals.Length}; tangents={mesh.tangents.Length}.");
            }
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static string UvSummary(Vector2[] uv)
        {
            if (uv == null || uv.Length == 0) return "empty";
            var min = uv[0]; var max = uv[0];
            for (var i = 1; i < uv.Length; i++) { min = Vector2.Min(min, uv[i]); max = Vector2.Max(max, uv[i]); }
            return $"count={uv.Length}; min={min}; max={max}; outside01={uv.Count(v => v.x < 0 || v.x > 1 || v.y < 0 || v.y > 1)}";
        }
    }
}
#endif
