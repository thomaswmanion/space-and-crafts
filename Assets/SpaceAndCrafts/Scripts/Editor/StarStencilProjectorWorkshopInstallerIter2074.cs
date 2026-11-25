#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Places the existing Meshy projector as a static WorkshopHub prop only.</summary>
    public static class StarStencilProjectorWorkshopInstallerIter2074
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarStencilProjector/Meshy_Iter1971/StarStencilProjector.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarStencilProjector/Meshy_Iter1971/StarStencilProjector_base_color.png";
        private const string OutputFolder = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarStencilProjector/UnityIntegration_Iter2074";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/StarStencilProjector.prefab";
        private const string MaterialPath = OutputFolder + "/Mat_StarStencilProjector_Albedo.mat";
        private const string ReportPath = OutputFolder + "/placement-review.txt";
        private const string RootName = "ART_StarStencilProjector";
        private const float TargetWorldWidth = 0.52f;

        [MenuItem("Space & Crafts/Install Star-Stencil Projector in Workshop (Iter 2074)")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath) || !File.Exists(TexturePath))
                    throw new FileNotFoundException("Workshop scene, projector FBX, or base-color texture is missing.");

                EnsureFolder(OutputFolder);
                EnsureFolder(PrefabFolder);
                AssetDatabase.Refresh();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Could not open WorkshopHub.");

                var existing = GameObject.Find(RootName);
                if (existing != null)
                {
                    Debug.Log("[StarStencilProjector Iter2074] Already present; leaving it unchanged.");
                    Finish(0);
                    return;
                }

                var bench = GameObject.Find("ART_Workbench");
                var benchRenderer = bench != null ? bench.GetComponent<Renderer>() : null;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (benchRenderer == null || model == null || texture == null)
                    throw new InvalidOperationException("Could not resolve the workbench renderer, FBX, or texture.");

                var material = LoadOrCreateMaterial(texture);
                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException("Could not instantiate StarStencilProjector FBX.");
                }
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 || !TryGetBounds(instance, out var bounds) || bounds.size.x <= 0.001f)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException("Projector has no measurable visible renderer bounds.");
                }
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();

                root.transform.localScale = Vector3.one * (TargetWorldWidth / bounds.size.x);
                if (!TryGetBounds(instance, out bounds))
                    throw new InvalidOperationException("Could not measure projector after display scaling.");

                var benchBounds = benchRenderer.bounds;
                // Keep the two existing tools at the front-left/right. This
                // compact projector sits centered at the back of the worktop.
                var target = new Vector3(benchBounds.center.x, benchBounds.max.y,
                    benchBounds.center.z - benchBounds.size.z * 0.22f);
                root.transform.position += new Vector3(target.x - bounds.center.x,
                    target.y - bounds.min.y, target.z - bounds.center.z);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException("Could not save the reusable projector prefab.");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save WorkshopHub after installing the projector.");

                TryGetBounds(connected, out var finalBounds);
                var report = new StringBuilder();
                report.AppendLine("Iteration 2074 static Meshy Star-Stencil Projector placement.");
                report.AppendLine("No collider, interaction, projection light, badge progression, or game logic was added.");
                report.AppendLine($"Model: {ModelPath}; texture: {TexturePath}; prefab: {PrefabPath}");
                report.AppendLine($"Final world bounds size={finalBounds.size:F3}; bottomY={finalBounds.min.y:F3}; workbenchTopY={benchBounds.max.y:F3}; targetWidth={TargetWorldWidth:F3}m.");
                report.AppendLine($"Worktop center offset: x={finalBounds.center.x - benchBounds.center.x:F3}m, z={finalBounds.center.z - benchBounds.center.z:F3}m.");
                report.AppendLine("Static art placement is not a rendered or human usability review; inspect at WorkshopHub camera scale before retaining.");
                File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                AssetDatabase.SaveAssets();
                Debug.Log("[StarStencilProjector Iter2074] connected reusable static prefab in WorkshopHub; no collider/interaction added. " + report.ToString().Replace("\n", " | "));
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[StarStencilProjector Iter2074] Install failed: " + exception);
                Finish(1);
            }
        }

        private static Material LoadOrCreateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a lit shader for the projector.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Mat_StarStencilProjector_Albedo" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool TryGetBounds(GameObject gameObject, out Bounds bounds)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { bounds = default; return false; }
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
#endif
