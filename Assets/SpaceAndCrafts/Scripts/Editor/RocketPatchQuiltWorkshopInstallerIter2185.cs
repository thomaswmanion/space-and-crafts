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
    /// <summary>Installs the existing world-route quilt as static WorkshopHub decor.</summary>
    public static class RocketPatchQuiltWorkshopInstallerIter2185
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/Meshy_Iter1920/Remesh30k/RocketPatchQuilt_30k.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/Meshy_Iter1920/Remesh30k/RocketPatchQuilt_30k_textures/base_color.png";
        private const string OutputFolder = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/UnityIntegration_Iter2185";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/RocketPatchQuilt.prefab";
        private const string MaterialPath = OutputFolder + "/Mat_RocketPatchQuilt_Albedo.mat";
        private const string ReportPath = OutputFolder + "/placement-review.txt";
        private const string RootName = "ART_RocketPatchQuilt";
        private const float TargetWorldWidth = 2.2f;
        private const float DisplayBottomY = 1.15f;
        private const float DisplayDepthZ = 2.1f;

        [MenuItem("Space & Crafts/Install Rocket-Patch Quilt in Workshop (Iter 2185)")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath) || !File.Exists(TexturePath))
                    throw new FileNotFoundException("WorkshopHub, quilt FBX, or color texture is missing.");

                EnsureFolder(OutputFolder);
                EnsureFolder(PrefabFolder);
                AssetDatabase.Refresh();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Could not open WorkshopHub.");
                var oldRoot = GameObject.Find(RootName);
                if (oldRoot != null)
                    UnityEngine.Object.DestroyImmediate(oldRoot); // repeatable placement iteration; replace only this named candidate

                var hub = GameObject.Find("ART_WorkshopHub");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (hub == null || model == null || texture == null)
                    throw new InvalidOperationException("Could not resolve hub root, FBX, or base-color texture.");

                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(hub.transform, false);
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException("Could not instantiate Rocket-Patch Quilt FBX.");
                }
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 || !TryGetBounds(instance, out var bounds) || bounds.size.x <= 0.001f)
                    throw new InvalidOperationException("Quilt has no measurable visible renderer bounds.");
                var material = CreateOrUpdateMaterial(texture);
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();

                // Source FBX is Z-up: rotate its broad X/Z surface into Unity's X/Y wall plane.
                // The source's front is -Y; this orientation points it toward the hub camera at -Z.
                root.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
                root.transform.localScale = Vector3.one * (TargetWorldWidth / bounds.size.x);
                if (!TryGetBounds(instance, out bounds))
                    throw new InvalidOperationException("Could not measure quilt after display scaling.");
                root.transform.position += new Vector3(-bounds.center.x,
                    DisplayBottomY - bounds.min.y,
                    DisplayDepthZ - bounds.center.z);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null)
                    throw new InvalidOperationException("Could not save the reusable quilt prefab.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save WorkshopHub after installing the quilt.");

                TryGetBounds(connected, out var finalBounds);
                var report = new StringBuilder();
                report.AppendLine("Iteration 2185 static Meshy Rocket-Patch Quilt placement in WorkshopHub.");
                report.AppendLine("No collider, collectible state, unlock logic, interaction, or reward behavior was added.");
                report.AppendLine($"Model: {ModelPath}; base color: {TexturePath}; prefab: {PrefabPath}");
                report.AppendLine($"Final world bounds size={finalBounds.size:F3}; center={finalBounds.center:F3}; targetWidth={TargetWorldWidth:F3}m; requested bottomY={DisplayBottomY:F3}m; depthZ={DisplayDepthZ:F3}m.");
                report.AppendLine("The source FBX is Z-up and is rotated into a vertical wall-hanging orientation. The 30k remesh target is not an independently measured triangle count. Placement must be visually reviewed in the standard WorkshopHub capture before acceptance.");
                File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                AssetDatabase.SaveAssets();
                Debug.Log("[RocketPatchQuilt Iter2185] Installed static world-route quilt in WorkshopHub; " + report.ToString().Replace("\n", " | "));
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[RocketPatchQuilt Iter2185] Install failed: " + exception);
                Finish(1);
            }
        }

        private static Material CreateOrUpdateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a lit shader for the quilt.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Mat_RocketPatchQuilt_Albedo" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
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
