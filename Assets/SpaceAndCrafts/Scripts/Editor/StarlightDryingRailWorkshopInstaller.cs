#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Installs one existing Meshy workshop prop as a reusable, static workshop decoration.
    /// It does not add colliders or interactable behavior.
    /// </summary>
    public static class StarlightDryingRailWorkshopInstaller
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarlightDryingRail/Meshy_Iter1958/StarlightDryingRail_Textured.fbx";
        private const string PrefabFolder = "Assets/SpaceAndCrafts/Prefabs/HomeWorkshop";
        private const string PrefabPath = PrefabFolder + "/StarlightDryingRail.prefab";
        private const string RootName = "ART_StarlightDryingRail";
        private const float TargetWorldWidth = 0.72f;

        [MenuItem("Space & Crafts/Install Starlight Drying Rail in Workshop")]
        public static void Install()
        {
            try
            {
                if (!File.Exists(ScenePath) || !File.Exists(ModelPath))
                {
                    throw new FileNotFoundException("Workshop scene or Starlight Drying Rail FBX is missing.");
                }

                Directory.CreateDirectory(PrefabFolder);
                AssetDatabase.Refresh();

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    throw new System.InvalidOperationException("Could not open WorkshopHub scene.");
                }

                var bench = GameObject.Find("ART_Workbench");
                var benchRenderer = bench != null ? bench.GetComponent<Renderer>() : null;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (benchRenderer == null || model == null)
                {
                    throw new System.InvalidOperationException("Could not locate ART_Workbench renderer or imported FBX model.");
                }

                var existing = GameObject.Find(RootName);
                if (existing != null)
                {
                    Debug.Log("[StarlightDryingRail] Already present in WorkshopHub; leaving it unchanged.");
                    Finish(0);
                    return;
                }

                var root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
                if (instance == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not instantiate the imported Starlight Drying Rail FBX.");
                }

                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                if (!TryGetRendererBounds(instance, out var bounds) || bounds.size.x <= 0.001f)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Imported prop has no measurable visible renderers.");
                }

                root.transform.localScale = Vector3.one * (TargetWorldWidth / bounds.size.x);
                if (!TryGetRendererBounds(instance, out bounds))
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not measure prop after applying its display scale.");
                }

                var workbenchBounds = benchRenderer.bounds;
                var targetCenter = new Vector3(
                    workbenchBounds.center.x - workbenchBounds.size.x * 0.24f,
                    workbenchBounds.max.y,
                    workbenchBounds.center.z + workbenchBounds.size.z * 0.18f);
                root.transform.position += new Vector3(
                    targetCenter.x - bounds.center.x,
                    targetCenter.y - bounds.min.y,
                    targetCenter.z - bounds.center.z);

                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
                if (connected == null)
                {
                    Object.DestroyImmediate(root);
                    throw new System.InvalidOperationException("Could not save the reusable workshop prefab.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new System.InvalidOperationException("WorkshopHub could not be saved after installing the decoration.");
                }

                if (TryGetRendererBounds(connected, out var finalBounds))
                {
                    Debug.Log($"[StarlightDryingRail] Installed reusable prefab at {PrefabPath}; world bounds {finalBounds.size:F3} m; base Y {finalBounds.min.y:F3}, workbench top Y {workbenchBounds.max.y:F3}. No collider or interaction was added.");
                }
                else
                {
                    Debug.Log($"[StarlightDryingRail] Installed reusable prefab at {PrefabPath}; no renderer bounds were available after saving.");
                }

                Finish(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[StarlightDryingRail] Install failed: " + exception);
                Finish(1);
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
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static void Finish(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
#endif
