#if UNITY_EDITOR
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Rethemes the existing, locked World 02 scene in place. Internal scene paths,
    /// world ID, invention ID, and serialized component classes remain stable.
    /// </summary>
    public static class World02BeepingCavesRethemeIter2243
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string SurfaceMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_World02_ResonantStrata.mat";
        private static readonly Color CaveSurface = new Color(0.32f, 0.36f, 0.52f, 1f);

        [MenuItem("Space & Crafts/Retheme World 02 as Beeping Caves Kingdom (Iter 2243)")]
        public static void Retheme()
        {
            WorldCatalogBootstrap.SeedCatalog();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var surface = GetOrCreateSurfaceMaterial();

            var floor = GameObject.Find("ART_CanopyFloor");
            if (floor != null)
            {
                floor.name = "ART_ResonantCaveFloor";
                AssignMaterial(floor, surface);
            }

            var landmark = GameObject.Find("ART_WorldLandmark");
            if (landmark != null)
                OrganicLandmarkBuilders.BuildForWorld(2, landmark.transform, new Color(0.43f, 0.52f, 0.95f), ApplyMaterial);

            var spooler = GameObject.Find("ART_VineSpooler");
            if (spooler != null)
                OrganicInventionBuilders.BuildPinglineSpooler(spooler.transform, ApplyMaterial);

            var roots = Object.FindObjectsByType<Transform>();
            var pads = 0;
            var leafMeshesRenamed = 0;
            foreach (var root in roots)
            {
                if (root == null || !root.name.StartsWith("ART_TraversalPad_", System.StringComparison.Ordinal)) continue;
                foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                    if (mesh == null || !mesh.name.Contains("World02LeafSurfaceUV")) continue;
                    mesh.name = mesh.name.Replace("World02LeafSurfaceUV", "World02ResonantStrata");
                    EditorUtility.SetDirty(mesh);
                    leafMeshesRenamed++;
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    AssignMaterial(renderer.gameObject, surface);
                    pads++;
                }
            }

            var spoolPickup = GameObject.Find("ART_Pickup_component_vine_reel");
            if (spoolPickup != null)
            {
                var spoolMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/SpaceAndCrafts/Materials/TempArt/Mat_Pickup_component_vine_reel.mat");
                if (spoolMaterial != null)
                {
                    var spoolColor = new Color(0.47f, 0.62f, 0.93f, 1f);
                    spoolMaterial.color = spoolColor;
                    if (spoolMaterial.HasProperty("_BaseColor")) spoolMaterial.SetColor("_BaseColor", spoolColor);
                    if (spoolMaterial.HasProperty("_Color")) spoolMaterial.SetColor("_Color", spoolColor);
                    EditorUtility.SetDirty(spoolMaterial);
                    foreach (var renderer in spoolPickup.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterial = spoolMaterial;
                }
            }

            foreach (var vista in Object.FindObjectsByType<WorldOpeningVista>())
            {
                var so = new SerializedObject(vista);
                so.FindProperty("toast").stringValue = "Beeping Caves Kingdom — ping a line to a safe ledge!";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var teachPad in Object.FindObjectsByType<InventionTeachPad>())
            {
                var so = new SerializedObject(teachPad);
                so.FindProperty("successToast").stringValue = "Pingline Spooler — ping, latch, and zip!";
                so.FindProperty("lockedToast").stringValue = "Build the Pingline Spooler first!";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var exam in Object.FindObjectsByType<WorldBossExam>())
            {
                var so = new SerializedObject(exam);
                so.FindProperty("introToast").stringValue = "The cave chimes are out of tune — follow the pulse!";
                so.FindProperty("clearToast").stringValue = "The cave rings answer — a passage to Leaf Lands opens!";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var banner in Object.FindObjectsByType<TempArtBanner>())
                banner.SetLabel("TEMP ART — Beeping Caves Kingdom graybox; resonant cave finals and Pingline Spooler art still needed");

            foreach (var text in Object.FindObjectsByType<TextMesh>())
            {
                if (text.text.Contains("Corkscrew Canopy") || text.text.Contains("Vine Spooler"))
                    text.text = "TEMP ART — Beeping Caves Kingdom graybox; resonant cave finals and Pingline Spooler art still needed";
            }

            WorldsLightingMoodBootstrap.ApplyLightingMood(2);
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 02 retheme] saved={saved}; padRenderersRethemed={pads}; legacyLeafMeshesRenamed={leafMeshesRenamed}; stable IDs and scene path retained.");
            if (Application.isBatchMode) EditorApplication.Exit(saved ? 0 : 1);
        }

        private static Material GetOrCreateSurfaceMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SurfaceMaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new System.InvalidOperationException("No supported cave-surface shader found.");
                material = new Material(shader) { name = "Mat_World02_ResonantStrata" };
                AssetDatabase.CreateAsset(material, SurfaceMaterialPath);
            }
            material.color = CaveSurface;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", CaveSurface);
            if (material.HasProperty("_Color")) material.SetColor("_Color", CaveSurface);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.32f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            const string folder = "Assets/SpaceAndCrafts/Materials/TempArt";
            var path = $"{folder}/{assetName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new System.InvalidOperationException("No supported landmark shader found.");
                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            AssignMaterial(go, material);
        }

        private static void AssignMaterial(GameObject go, Material material)
        {
            var renderer = go != null ? go.GetComponent<Renderer>() : null;
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
#endif
