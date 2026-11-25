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
    /// <summary>Connects the existing Meshy Orbit Postbox to Splat's workshop as static story dressing.</summary>
    public static class OrbitPostboxWorkshopInstallerIter2191
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitPostbox/UnityImport_Iter2191/OrbitPostbox_30k.fbx";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitPostbox/Meshy_Iter1906/Model/OrbitPostbox_30k_textures/base_color.png";
        private const string OutputFolder = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitPostbox/UnityIntegration_Iter2191";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/OrbitPostbox.prefab";
        private const string MaterialPath = OutputFolder + "/Mat_OrbitPostbox_Albedo.mat";
        private const string ReportPath = OutputFolder + "/placement-review.txt";
        private const string RootName = "ART_OrbitPostbox";
        private const float TargetHeight = 1.25f;

        [MenuItem("Space & Crafts/Install Orbit Postbox in Workshop (Iter 2191)")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath) || !File.Exists(TexturePath))
                    throw new FileNotFoundException("WorkshopHub, converted Orbit Postbox FBX, or base-color atlas is missing.");
                EnsureFolder(OutputFolder);
                EnsureFolder(PrefabFolder);
                AssetDatabase.Refresh();

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Could not open WorkshopHub.");
                var hub = GameObject.Find("ART_WorkshopHub");
                var floor = GameObject.Find("ART_WorkshopFloor");
                var floorRenderer = floor != null ? floor.GetComponent<Renderer>() : null;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (hub == null || floorRenderer == null || model == null || texture == null)
                    throw new InvalidOperationException("Could not resolve workshop root/floor, imported FBX, or color atlas.");

                var oldRoot = GameObject.Find(RootName);
                if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(hub.transform, false);
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Orbit Postbox FBX.");
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 || !TryGetBounds(instance, out var bounds) || bounds.size.y <= 0.001f)
                    throw new InvalidOperationException("Orbit Postbox has no measurable visible renderer bounds.");
                var material = CreateOrUpdateMaterial(texture);
                foreach (var renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();

                // The story panel becomes visible when the Z-up source front is quarter-turned toward the hub camera.
                root.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
                root.transform.localScale = Vector3.one * (TargetHeight / bounds.size.y);
                if (!TryGetBounds(instance, out bounds)) throw new InvalidOperationException("Could not measure scaled mailbox.");

                var floorTop = floorRenderer.bounds.max.y;
                // Keep the mailbox in the open foreground, clear of Splat's right-side idle and the rear route quilt.
                var targetCenter = new Vector3(-0.9f, 0f, -1.1f);
                root.transform.position += new Vector3(targetCenter.x - bounds.center.x,
                    floorTop - bounds.min.y, targetCenter.z - bounds.center.z);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null) throw new InvalidOperationException("Could not save reusable Orbit Postbox prefab.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save WorkshopHub.");

                TryGetBounds(connected, out var finalBounds);
                var report = new StringBuilder();
                report.AppendLine("Iteration 2191: existing Meshy Orbit Postbox, converted locally from the GLB, placed as static WorkshopHub story dressing.");
                report.AppendLine("Intended narrative: Splat can send handmade postcards and world souvenirs to his brother Squat. No mail interaction, message state, animation, reward, collider, or progression logic was added.");
                report.AppendLine($"Source: {ModelPath}; source Meshy GLB triangles measured in Blender: 24,135; color atlas: {TexturePath}; prefab: {PrefabPath}");
                report.AppendLine($"World bounds={finalBounds.size:F3}; center={finalBounds.center:F3}; floorTopY={floorTop:F3}; targetHeight={TargetHeight:F2}m; no collider added.");
                report.AppendLine("Placement and front-facing orientation require review in a normal WorkshopHub camera capture. Asset integration alone does not establish readable play, story comprehension, or increased score.");
                File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                AssetDatabase.SaveAssets();
                Debug.Log("[OrbitPostbox Iter2191] Installed static postbox. " + report.ToString().Replace("\n", " | "));
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OrbitPostbox Iter2191] Install failed: " + exception);
                Finish(1);
            }
        }

        [MenuItem("Space & Crafts/Remove Failed Orbit Postbox Placement (Iter 2191)")]
        public static void RemoveFailedPlacement()
        {
            try
            {
                if (!File.Exists(ScenePath)) throw new FileNotFoundException("WorkshopHub scene is missing.");
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var oldRoot = GameObject.Find(RootName);
                if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save WorkshopHub after removing failed placement.");

                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.DeleteAsset(MaterialPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[OrbitPostbox Iter2191] Removed the failed scene placement, prefab, and generated material; the original Meshy GLB remains untouched.");
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OrbitPostbox Iter2191] Cleanup failed: " + exception);
                Finish(1);
            }
        }

        private static Material CreateOrUpdateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Could not find a lit shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Mat_OrbitPostbox_Albedo" };
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
