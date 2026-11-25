using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Editor
{
    public static class WorldCameraMigration
    {
        private const int CameraOccluderLayer = 31;

        [MenuItem("Space & Crafts/Worlds/Add World 03 Camera Occluders")]
        public static void AddWorld03CameraOccluders()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var added = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!transform.name.StartsWith("ART_FoilArch_")) continue;
                    var filter = transform.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    transform.gameObject.layer = CameraOccluderLayer;
                    var meshCollider = transform.GetComponent<MeshCollider>();
                    if (meshCollider == null) meshCollider = Undo.AddComponent<MeshCollider>(transform.gameObject);
                    meshCollider.sharedMesh = filter.sharedMesh;
                    meshCollider.convex = false;
                    EditorUtility.SetDirty(transform.gameObject);
                    EditorUtility.SetDirty(meshCollider);
                    added++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldCameraMigration] World 03 camera occluders assigned: {added} ART_FoilArch meshes on layer {CameraOccluderLayer}.");
        }

        [MenuItem("Space & Crafts/Worlds/Add World 10 Camera Occluders")]
        public static void AddWorld10CameraOccluders()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var added = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!transform.name.StartsWith("ART_CathedralArch_")) continue;
                    var filter = transform.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    transform.gameObject.layer = CameraOccluderLayer;
                    var meshCollider = transform.GetComponent<MeshCollider>();
                    if (meshCollider == null) meshCollider = Undo.AddComponent<MeshCollider>(transform.gameObject);
                    meshCollider.sharedMesh = filter.sharedMesh;
                    meshCollider.convex = false;
                    EditorUtility.SetDirty(transform.gameObject);
                    EditorUtility.SetDirty(meshCollider);
                    added++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldCameraMigration] World 10 camera occluders assigned: {added} ART_CathedralArch meshes on layer {CameraOccluderLayer}.");
        }

        [MenuItem("Space & Crafts/Worlds/Widen World 03 Intermediate Traversal Pads")]
        public static void WidenWorld03IntermediateTraversalPads()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name != "ART_TraversalPad_1" && transform.name != "ART_TraversalPad_2") continue;
                    var scale = transform.localScale;
                    scale.x = 1.3f;
                    scale.z = 1.3f;
                    transform.localScale = scale;
                    EditorUtility.SetDirty(transform.gameObject);
                    changed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldCameraMigration] Widened {changed} World 03 intermediate pads to 2.34 m in each horizontal axis.");
        }

        [MenuItem("Space & Crafts/Worlds/Ensure Cameras Worlds 03-10")]
        public static void EnsureWorldCameras03To10()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CameraSettings>("Assets/SpaceAndCrafts/ScriptableObjects/CameraSettings.asset");
            if (settings == null) throw new System.InvalidOperationException("CameraSettings asset is missing.");
            for (var i = 3; i <= 10; i++)
            {
                var path = AssetDatabase.FindAssets($"World{i:00}", new[] { "Assets/SpaceAndCrafts/Scenes/Worlds" });
                string scenePath = null;
                foreach (var guid in path)
                {
                    var candidate = AssetDatabase.GUIDToAssetPath(guid);
                    if (candidate.EndsWith(".unity") && candidate.Contains($"World{i:00}_")) { scenePath = candidate; break; }
                }
                if (scenePath == null) throw new System.InvalidOperationException($"World {i:00} scene not found.");
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var splat = FindSplat(scene);
                if (splat == null) throw new System.InvalidOperationException($"World {i:00} has no Splat_TEMP.");
                var existing = Object.FindAnyObjectByType<PlatformingCameraRig>();
                if (existing == null)
                {
                    var rig = new GameObject("CameraRig");
                    rig.transform.position = splat.transform.position;
                    var pivot = new GameObject("Pivot");
                    pivot.transform.SetParent(rig.transform, false);
                    var cameraObject = new GameObject("GameCamera");
                    cameraObject.transform.SetParent(pivot.transform, false);
                    var camera = cameraObject.AddComponent<Camera>();
                    camera.tag = "MainCamera";
                    cameraObject.AddComponent<AudioListener>();
                    var collision = rig.AddComponent<CameraCollision>();
                    Set(collision, "settings", settings);
                    Set(collision, "followTarget", splat.transform);
                    Set(collision, "cameraTransform", camera.transform);
                    var cameraRig = rig.AddComponent<PlatformingCameraRig>();
                    Set(cameraRig, "settings", settings);
                    Set(cameraRig, "input", splat.GetComponent<SplatInputReader>());
                    Set(cameraRig, "followTarget", splat.transform);
                    Set(cameraRig, "cameraPivot", pivot.transform);
                    Set(cameraRig, "gameCamera", camera);
                    Set(cameraRig, "collision", collision);
                    var motor = splat.GetComponent<SplatMotor>();
                    if (motor != null) Set(motor, "cameraTransform", pivot.transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                else
                {
                    var rigObject = existing.gameObject;
                    var pivot = rigObject.transform.Find("Pivot");
                    var camera = rigObject.GetComponentInChildren<Camera>(true);
                    var collision = rigObject.GetComponent<CameraCollision>();
                    Set(existing, "settings", settings);
                    Set(existing, "input", splat.GetComponent<SplatInputReader>());
                    Set(existing, "followTarget", splat.transform);
                    if (pivot != null) Set(existing, "cameraPivot", pivot);
                    if (camera != null) Set(existing, "gameCamera", camera);
                    if (collision != null)
                    {
                        Set(collision, "settings", settings);
                        Set(collision, "followTarget", splat.transform);
                        if (camera != null) Set(collision, "cameraTransform", camera.transform);
                    }
                    var motor = splat.GetComponent<SplatMotor>();
                    if (motor != null && pivot != null) Set(motor, "cameraTransform", pivot);
                    EditorUtility.SetDirty(existing);
                    if (collision != null) EditorUtility.SetDirty(collision);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[WorldCameraMigration] World {i:00} camera ready: {scenePath}");
            }
            AssetDatabase.SaveAssets();
        }

        private static GameObject FindSplat(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms) if (t.name == "Splat_TEMP") return t.gameObject;
            }
            return null;
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
