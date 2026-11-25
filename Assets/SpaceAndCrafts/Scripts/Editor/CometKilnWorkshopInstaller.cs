#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Installs the existing textured Comet Kiln as non-interactive Home Workshop dressing.
    /// </summary>
    public static class CometKilnWorkshopInstaller
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CometKiln/Meshy_Iter1955/CometKiln_Textured.fbx";
        private const string AlbedoPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CometKiln/Meshy_Iter1955/CometKiln_Textured_base_color.png";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/CometKiln.prefab";
        private const string MaterialFolder = "Assets/SpaceAndCrafts/Materials/Environment";
        private const string MaterialPath = MaterialFolder + "/Mat_CometKiln_Albedo.mat";
        private const string RootName = "ART_CometKiln";
        private const float TargetWorldWidth = 0.44f;

        [MenuItem("Space & Crafts/Install Comet Kiln in Workshop")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath))
                    throw new FileNotFoundException("WorkshopHub scene or Comet Kiln FBX is missing.");

                Directory.CreateDirectory(PrefabFolder);
                AssetDatabase.Refresh();

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new System.InvalidOperationException("Could not open WorkshopHub scene.");

                if (GameObject.Find(RootName) != null)
                {
                    Debug.Log("[CometKiln] Already present in WorkshopHub; leaving it unchanged.");
                    Finish(0);
                    return;
                }

                var bench = GameObject.Find("ART_Workbench");
                var benchRenderer = bench != null ? bench.GetComponent<Renderer>() : null;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (benchRenderer == null || model == null)
                    throw new System.InvalidOperationException("Could not locate the workbench renderer or imported Comet Kiln FBX.");

                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not instantiate the imported Comet Kiln FBX.");
                }

                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                if (!TryGetRendererBounds(instance, out var bounds) || bounds.size.x <= 0.001f)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Imported kiln has no measurable visible renderers.");
                }

                root.transform.localScale = Vector3.one * (TargetWorldWidth / bounds.size.x);
                if (!TryGetRendererBounds(instance, out bounds))
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not measure kiln after applying display scale.");
                }

                var benchBounds = benchRenderer.bounds;
                // Place it on the clear right side, opposite the left-anchored 0.72 m Starlight Rail.
                var targetCenter = new Vector3(
                    benchBounds.center.x + benchBounds.size.x * 0.30f,
                    benchBounds.max.y,
                    benchBounds.center.z + benchBounds.size.z * 0.18f);
                root.transform.position += new Vector3(
                    targetCenter.x - bounds.center.x,
                    targetCenter.y - bounds.min.y,
                    targetCenter.z - bounds.center.z);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not save the reusable Comet Kiln prefab.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new System.InvalidOperationException("Could not save WorkshopHub after installing the Comet Kiln.");

                if (TryGetRendererBounds(connected, out var finalBounds))
                {
                    Debug.Log($"[CometKiln] Installed {PrefabPath}; bounds {finalBounds.size:F3} m; base Y {finalBounds.min.y:F3}, workbench top Y {benchBounds.max.y:F3}. Center X offset from bench {finalBounds.center.x - benchBounds.center.x:F3} m. No collider or interaction was added.");
                }

                Finish(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[CometKiln] Install failed: " + exception);
                Finish(1);
            }
        }

        [MenuItem("Space & Crafts/Repair Comet Kiln Material")]
        public static void RepairMaterial()
        {
            GameObject prefabContents = null;
            try
            {
                if (!File.Exists(PrefabPath) || !File.Exists(AlbedoPath))
                    throw new FileNotFoundException("Workshop prefab or Comet Kiln base-color texture is missing.");

                Directory.CreateDirectory(MaterialFolder);
                AssetDatabase.Refresh();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (texture == null || shader == null)
                    throw new System.InvalidOperationException("Could not load the kiln base-color texture or URP/Lit shader.");

                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = "Mat_CometKiln_Albedo" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                    material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                }

                material.shader = shader;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
                EditorUtility.SetDirty(material);

                prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
                if (prefabContents == null)
                    throw new System.InvalidOperationException("Could not open the Comet Kiln prefab for material repair.");

                var renderers = prefabContents.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new System.InvalidOperationException("Comet Kiln prefab contains no renderers.");

                for (var i = 0; i < renderers.Length; i++)
                {
                    var slotCount = Mathf.Max(1, renderers[i].sharedMaterials.Length);
                    var materials = new Material[slotCount];
                    for (var slot = 0; slot < slotCount; slot++) materials[slot] = material;
                    renderers[i].sharedMaterials = materials;
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath, out var success);
                if (saved == null || !success)
                    throw new System.InvalidOperationException("Could not save the Comet Kiln material override to its prefab.");

                AssetDatabase.SaveAssets();
                Debug.Log($"[CometKiln] Applied Meshy base-color atlas to {renderers.Length} prefab renderer(s), {MaterialPath}; set non-metallic matte URP Lit. No scene/collision/interaction behavior changed.");
                Finish(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[CometKiln] Material repair failed: " + exception);
                Finish(1);
            }
            finally
            {
                if (prefabContents != null)
                    PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static bool TryGetRendererBounds(GameObject gameObject, out Bounds bounds)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static void Finish(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
}
#endif
