#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Installs the existing Meshy Orbit Desk Lamp as warm, static WorkshopHub dressing.</summary>
    public static class OrbitDeskLampWorkshopInstaller
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitDeskLamp/Meshy_Iter1882/Model/model.fbx";
        private const string AlbedoPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitDeskLamp/Meshy_Iter1882/Model/texture_0_base_color.png";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/OrbitDeskLamp.prefab";
        private const string MaterialFolder = "Assets/SpaceAndCrafts/Materials/Environment";
        private const string MaterialPath = MaterialFolder + "/Mat_OrbitDeskLamp_Albedo.mat";
        private const string RootName = "ART_OrbitDeskLamp";
        private const float TargetWorldHeight = 0.82f;

        [MenuItem("Space & Crafts/Install Orbit Desk Lamp in Workshop")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath) || !File.Exists(AlbedoPath))
                    throw new FileNotFoundException("WorkshopHub, Meshy lamp FBX, or albedo texture is missing.");

                Directory.CreateDirectory(PrefabFolder);
                Directory.CreateDirectory(MaterialFolder);
                AssetDatabase.Refresh();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new System.InvalidOperationException("Could not open WorkshopHub.");

                var existing = GameObject.Find(RootName);
                if (existing != null)
                {
                    RepairMaterialAndLight(existing);
                    Save(scene, existing);
                    Finish(0);
                    return;
                }

                var bench = GameObject.Find("ART_Workbench");
                var benchRenderer = bench != null ? bench.GetComponent<Renderer>() : null;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (benchRenderer == null || model == null)
                    throw new System.InvalidOperationException("Could not find the workbench renderer or imported FBX.");

                var material = CreateMaterial();
                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not instantiate the imported Orbit Desk Lamp.");
                }

                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                ApplyMaterial(instance, material);
                if (!TryGetBounds(instance, out var bounds) || bounds.size.y <= 0.001f)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("The imported lamp has no measurable visible renderers.");
                }

                root.transform.localScale = Vector3.one * (TargetWorldHeight / bounds.size.y);
                if (!TryGetBounds(instance, out bounds))
                    throw new System.InvalidOperationException("Could not measure the scaled lamp.");

                var benchBounds = benchRenderer.bounds;
                // Use the center-front gap between the already-installed left rail and right kiln.
                var targetCenter = new Vector3(benchBounds.center.x, benchBounds.max.y, benchBounds.center.z - benchBounds.size.z * 0.20f);
                root.transform.position += new Vector3(targetCenter.x - bounds.center.x, targetCenter.y - bounds.min.y, targetCenter.z - bounds.center.z);
                AddWarmLight(root.transform, bounds);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not save reusable Orbit Desk Lamp prefab.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new System.InvalidOperationException("Could not save WorkshopHub after installing the lamp.");

                if (TryGetBounds(connected, out var finalBounds))
                    Debug.Log($"[OrbitDeskLamp] Installed reusable Meshy prefab {PrefabPath}; rendered mesh bounds {finalBounds.size:F3} m, base Y {finalBounds.min.y:F3}; bench top Y {benchBounds.max.y:F3}. Added one warm point light, no collider or interaction.");
                Finish(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[OrbitDeskLamp] Install failed: " + exception);
                Finish(1);
            }
        }

        private static Material CreateMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (texture == null || shader == null)
                throw new System.InvalidOperationException("Could not load lamp albedo or URP/Lit shader.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Mat_OrbitDeskLamp_Albedo" };
                AssetDatabase.CreateAsset(material, MaterialPath);
                material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.16f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
        }

        private static void RepairMaterialAndLight(GameObject root)
        {
            var material = CreateMaterial();
            ApplyMaterial(root, material);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            AddWarmLight(root.transform, bounds);
        }

        private static void AddWarmLight(Transform parent, Bounds worldBounds)
        {
            const string lightName = "SYS_OrbitDeskLampGlow";
            var lightTransform = parent.Find(lightName);
            var lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject(lightName);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = new Vector3(worldBounds.center.x, worldBounds.max.y - worldBounds.size.y * 0.30f, worldBounds.center.z);
            var lamp = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.76f, 0.48f);
            lamp.intensity = 1.05f;
            lamp.range = 3.8f;
            lamp.shadows = LightShadows.None;
        }

        private static bool TryGetBounds(GameObject target, out Bounds bounds)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static void Save(Scene scene, GameObject root)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Could not save WorkshopHub.");
            Debug.Log("[OrbitDeskLamp] Refreshed existing lamp material/light and saved WorkshopHub.");
        }

        private static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
#endif
