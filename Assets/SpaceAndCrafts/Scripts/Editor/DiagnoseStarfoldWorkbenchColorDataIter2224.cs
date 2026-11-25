#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only inspection of vertex colors and material property overrides in the source FBX.</summary>
    public static class DiagnoseStarfoldWorkbenchColorDataIter2224
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/ReviewOnly/StarfoldWorkbench_MaterialPair_Iter2223.unity";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-material-iter2224/COLOR_DIAGNOSTIC.txt";

        [MenuItem("Space & Crafts/Diagnose Starfold Vertex Colors (Iter2224)")]
        public static void Diagnose()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) throw new System.InvalidOperationException("Project root unavailable.");
            string Abs(string p) => Path.Combine(projectRoot, p.Replace('/', Path.DirectorySeparatorChar));
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before read-only diagnostics.");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var sb = new StringBuilder("Starfold FBX color data and property-block diagnostic — Iter2224 (read-only)\n");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sourceAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            foreach (var asset in sourceAssets)
                sb.AppendLine($"FBX subasset: name={asset.name}; type={asset.GetType().Name}; path={AssetDatabase.GetAssetPath(asset)}.");
            var materials = sourceAssets.OfType<Material>().ToArray();
            sb.AppendLine($"FBX embedded materials: {materials.Length}");
            foreach (var m in materials)
            {
                sb.AppendLine($"material={m.name}; shader={m.shader?.name}; BaseMap={m.GetTexture("_BaseMap")?.name ?? "none"}; MainTex={m.GetTexture("_MainTex")?.name ?? "none"}; BaseColor={(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white)}; keywords={string.Join(",", m.shaderKeywords)}.");
                sb.AppendLine(" texture properties: " + string.Join(" | ", m.GetTexturePropertyNames().Select(n => $"{n}={(m.GetTexture(n)?.name ?? "none")}")));
            }

            if (model != null)
            {
                var probe = Object.Instantiate(model);
                foreach (var renderer in probe.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    var colors = mesh.colors32;
                    var uniqueColors = colors.Length == 0 ? 0 : colors.Distinct().Count();
                    sb.AppendLine($"source mesh={mesh.name}; vertices={mesh.vertexCount}; vertexColorCount={colors.Length}; distinctVertexColors={uniqueColors}; firstColor={(colors.Length > 0 ? colors[0].ToString() : "none")}; additionalVertexStreams={(renderer.additionalVertexStreams != null ? renderer.additionalVertexStreams.name : "none")}.");
                    sb.AppendLine($"source renderer sharedMaterials={string.Join(" | ", renderer.sharedMaterials.Select(x => $"{x.name} (BaseMap={x.GetTexture("_BaseMap")?.name ?? "none"}; MainTex={x.GetTexture("_MainTex")?.name ?? "none"})"))}.");
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    var baseMapId = Shader.PropertyToID("_BaseMap");
                    var mainTexId = Shader.PropertyToID("_MainTex");
                    sb.AppendLine($"renderer MaterialPropertyBlock: empty={block.isEmpty}; hasBaseMap={block.HasTexture(baseMapId)}; BaseMap={(block.GetTexture(baseMapId)?.name ?? "none")}; hasMainTex={block.HasTexture(mainTexId)}; MainTex={(block.GetTexture(mainTexId)?.name ?? "none")}.");
                }
                Object.DestroyImmediate(probe);
            }

            var review = GameObject.Find("B_AtlasMaterial_Right")?.GetComponentInChildren<MeshRenderer>();
            if (review != null)
            {
                var block = new MaterialPropertyBlock();
                review.GetPropertyBlock(block);
                var baseMapId = Shader.PropertyToID("_BaseMap");
                sb.AppendLine($"review atlas renderer: material={review.sharedMaterial?.name}; BaseMap={review.sharedMaterial?.GetTexture("_BaseMap")?.name ?? "none"}; propertyBlockEmpty={block.isEmpty}; propertyBlockBaseMap={block.GetTexture(baseMapId)?.name ?? "none"}; propertyBlockHasBaseMap={block.HasTexture(baseMapId)}.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(ReportPath))!);
            File.WriteAllText(Abs(ReportPath), sb.ToString());
            Debug.Log("[DiagnoseStarfoldWorkbenchColorDataIter2224] read-only color/property report saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
#endif
