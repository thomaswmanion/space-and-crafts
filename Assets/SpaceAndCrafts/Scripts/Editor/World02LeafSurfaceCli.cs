#if UNITY_EDITOR
using System;
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Applies a muted resonant-strata material to the World 02 traversal pads.</summary>
    public static class World02LeafSurfaceCli
    {
        private static readonly Color SurfaceTint = new Color(0.32f, 0.36f, 0.52f, 1f);
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_World02_ResonantStrata.mat";
        private const string MeshSuffix = "_World02LeafSurfaceUV";

        [MenuItem("Space & Crafts/Refresh World 02 Resonant Cave Pad Surface")]
        public static void RefreshWorld02LeafPadsIter1548()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("No supported shader found for cave surface.");
                material = new Material(shader) { name = "Mat_World02_ResonantStrata" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.color = SurfaceTint;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", SurfaceTint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.3f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
            EditorUtility.SetDirty(material);

            var filters = UnityEngine.Object.FindObjectsByType<MeshFilter>();
            var updated = 0;
            for (var i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                var pad = filter != null ? filter.transform.parent : null;
                if (pad == null || filter.gameObject.name != "disc"
                    || !pad.name.StartsWith("ART_TraversalPad_", StringComparison.Ordinal))
                    continue;

                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                if (!mesh.name.EndsWith(MeshSuffix, StringComparison.Ordinal))
                {
                    mesh = UnityEngine.Object.Instantiate(mesh);
                    mesh.name = $"{mesh.name}{MeshSuffix}";
                    filter.sharedMesh = mesh;
                }

                OrganicMeshFactory.SetPlanarTopDownUVs(mesh, StableRotation(pad.name));
                EditorUtility.SetDirty(mesh);
                EditorUtility.SetDirty(filter);

                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(renderer);
                }
                updated++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 02 resonant cave pads] material={MaterialPath}, pads={updated}, sceneSaved={saved}.");
            if (Application.isBatchMode) EditorApplication.Exit(saved && updated > 0 ? 0 : 1);
        }

        private static float StableRotation(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return (uint)hash % 360u;
            }
        }
    }
}
#endif
