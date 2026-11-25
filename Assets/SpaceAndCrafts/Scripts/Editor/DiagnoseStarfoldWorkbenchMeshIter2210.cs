#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Reports imported FBX mesh vertex/transform data without rendering or scene mutation.</summary>
    public static class DiagnoseStarfoldWorkbenchMeshIter2210
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/Model/StarfoldWorkbench.fbx";
        private const string ReportPath = "Logs/score-comparison/workshop-starfold-workbench-focused-iter2210/MESH_DIAGNOSTIC.txt";

        [MenuItem("Space & Crafts/Diagnose Starfold Workbench Mesh (Iter2210)")]
        public static void Run()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            var absoluteReport = Path.Combine(projectRoot, ReportPath.Replace('/', Path.DirectorySeparatorChar));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new FileNotFoundException($"Missing imported model: {ModelPath}");
            var instance = UnityEngine.Object.Instantiate(model);
            var output = new StringBuilder();
            output.AppendLine("Starfold Workbench imported mesh diagnostic — Iter2210");
            output.AppendLine($"Model: {ModelPath}");
            output.AppendLine($"Instance root scale={instance.transform.localScale}; position={instance.transform.position}");
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            output.AppendLine($"Renderer count={renderers.Length}; renderer-bound union={renderers.Select(r => r.bounds).Aggregate(Union)}");
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                output.AppendLine($"MeshFilter path={AnimationUtility.CalculateTransformPath(filter.transform, instance.transform)}; " +
                    $"verts={mesh.vertexCount}; submeshes={mesh.subMeshCount}; meshBounds={mesh.bounds}; " +
                    $"localScale={filter.transform.localScale}; lossyScale={filter.transform.lossyScale}; " +
                    $"position={filter.transform.position}; worldTransformBounds={TransformBounds(mesh.bounds, filter.transform.localToWorldMatrix)}");
            }
            foreach (var renderer in renderers)
            {
                output.AppendLine($"Renderer path={AnimationUtility.CalculateTransformPath(renderer.transform, instance.transform)}; " +
                    $"type={renderer.GetType().Name}; enabled={renderer.enabled}; rendererBounds={renderer.bounds}; " +
                    $"localBounds={renderer.localBounds}; lossyScale={renderer.transform.lossyScale}; " +
                    $"materials={string.Join(",", renderer.sharedMaterials.Select(m => m != null ? m.name : "null"))}");
            }
            foreach (var transform in instance.GetComponentsInChildren<Transform>(true))
                output.AppendLine($"Transform {AnimationUtility.CalculateTransformPath(transform, instance.transform)}: " +
                    $"localPosition={transform.localPosition}; localScale={transform.localScale}; localEuler={transform.localEulerAngles}");
            UnityEngine.Object.DestroyImmediate(instance);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteReport));
            File.WriteAllText(absoluteReport, output.ToString());
            Debug.Log($"[DiagnoseStarfoldWorkbenchMeshIter2210] wrote {absoluteReport}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Bounds Union(Bounds a, Bounds b)
        {
            a.Encapsulate(b.min);
            a.Encapsulate(b.max);
            return a;
        }

        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var center = matrix.MultiplyPoint3x4(local.center);
            var extents = local.extents;
            var x = matrix.MultiplyVector(Vector3.right * extents.x);
            var y = matrix.MultiplyVector(Vector3.up * extents.y);
            var z = matrix.MultiplyVector(Vector3.forward * extents.z);
            var worldExtents = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(center, worldExtents * 2f);
        }
    }
}
#endif
