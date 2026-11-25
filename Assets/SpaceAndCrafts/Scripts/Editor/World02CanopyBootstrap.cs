#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// First pass graybox for World 2 — Beeping Caves Kingdom + Pingline Spooler teach loop.
    /// Visuals use organic meshes; still not commissioned finals.
    /// </summary>
    public static class World02CanopyBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string DataPath = "Assets/SpaceAndCrafts/ScriptableObjects";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap World 02 Beeping Caves Kingdom")]
        public static void Bootstrap()
        {
            WorldCatalogBootstrap.SeedCatalog();
            EnsureFolder("Assets/SpaceAndCrafts/Scenes/Worlds");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("ART_World02_CorkscrewCanopy"); // Stable scene hook; display name is Beeping Caves Kingdom.
            BuildResonantCave(world.transform);

            var systems = new GameObject("World02Systems");
            systems.AddComponent<LevelSession>();
            systems.AddComponent<LevelFlowController>();
            systems.AddComponent<GameplayToastHud>();
            systems.AddComponent<AudioSource>();
            systems.AddComponent<TempSfxPlayer>();
            systems.AddComponent<CollectibleHud>();
            systems.AddComponent<InteractPromptHud>();

            var motorSettings = AssetDatabase.LoadAssetAtPath<SplatMotorSettings>($"{DataPath}/SplatMotorSettings.asset");
            var cameraSettings = AssetDatabase.LoadAssetAtPath<CameraSettings>($"{DataPath}/CameraSettings.asset");
            var splatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject splat;
            if (splatPrefab != null)
            {
                splat = (GameObject)PrefabUtility.InstantiatePrefab(splatPrefab);
                PrefabUtility.UnpackPrefabInstance(splat, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                splat = new GameObject("Splat_TEMP");
                splat.AddComponent<CharacterController>();
                OrganicSplatBuilder.BuildVisualRoot(splat.transform, ApplyMat);
                splat.AddComponent<SplatInputReader>();
                splat.AddComponent<SplatMotor>();
                splat.AddComponent<SplatFeelPresenter>();
                splat.AddComponent<SplatInteractor>();
                splat.AddComponent<InventoryComponent>();
            }

            splat.name = "Splat_TEMP";
            splat.transform.position = new Vector3(0f, 1.2f, -2f);
            if (splat.GetComponent<PlayerRespawn>() == null)
            {
                splat.AddComponent<PlayerRespawn>();
            }

            var cameraRig = CreateCameraRig(splat.transform, cameraSettings);
            WirePlayer(splat, cameraRig, motorSettings, cameraSettings);

            var spool = new GameObject("ART_VineSpooler");
            spool.transform.position = new Vector3(0f, 1f, 6f);
            OrganicInventionBuilders.BuildPinglineSpooler(spool.transform, ApplyMat);
            var vine = spool.AddComponent<VineSpooler>();
            vine.AttachOwner(splat.transform);
            var trigger = spool.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;

            PlaceAnchor(world.transform, new Vector3(0f, 8f, 14f));
            PlaceAnchor(world.transform, new Vector3(-6f, 10f, 20f));
            PlaceAnchor(world.transform, new Vector3(6f, 12f, 26f));

            var banner = new GameObject("TEMP_ART_BANNER_WORLD02");
            banner.transform.position = new Vector3(0f, 3f, -4f);
            banner.AddComponent<TempArtBanner>().SetLabel(
                "TEMP ART — Beeping Caves Kingdom graybox (resonant cave + Pingline Spooler); replace with authored cave assets");

            WorldsLightingMoodBootstrap.ApplyLightingMood(2);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] World02 Beeping Caves Kingdom bootstrap complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void BuildResonantCave(Transform parent)
        {
            var floor = OrganicMeshFactory.CreateMeshObject(
                "ART_ResonantCaveFloor",
                OrganicMeshFactory.SoftBlob(2, 1f, 0.2f, 0.02f),
                parent,
                Vector3.zero,
                new Vector3(10f, 1f, 10f),
                null);
            ApplyMat(floor, "Mat_ResonantCaveFloor", new Color(0.16f, 0.19f, 0.30f), 0.58f, 0.12f);
            floor.AddComponent<BoxCollider>().size = Vector3.one;

            var landmark = new GameObject("ART_WorldLandmark");
            landmark.transform.SetParent(parent, false);
            landmark.transform.localPosition = new Vector3(0f, 0.2f, 10f);
            OrganicLandmarkBuilders.BuildForWorld(2, landmark.transform, new Color(0.43f, 0.52f, 0.95f), ApplyMat);
        }

        private static void PlaceAnchor(Transform parent, Vector3 pos)
        {
            var anchor = OrganicMeshFactory.CreateMeshObject(
                "ART_GrappleAnchor",
                OrganicMeshFactory.Torus(0.4f, 0.08f, 28, 12),
                parent,
                pos,
                Vector3.one,
                null);
            ApplyMat(anchor, "Mat_GrappleAnchor", new Color(0.85f, 0.7f, 0.3f), 0.3f, 0.4f);
            var col = anchor.AddComponent<SphereCollider>();
            col.radius = 0.5f;
        }

        private static GameObject CreateCameraRig(Transform follow, CameraSettings settings)
        {
            var rig = new GameObject("CameraRig");
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(rig.transform, false);
            var camGo = new GameObject("GameCamera");
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var collision = rig.AddComponent<CameraCollision>();
            var cso = new SerializedObject(collision);
            cso.FindProperty("settings").objectReferenceValue = settings;
            cso.FindProperty("followTarget").objectReferenceValue = follow;
            cso.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            cso.ApplyModifiedPropertiesWithoutUndo();
            var rigComp = rig.AddComponent<PlatformingCameraRig>();
            var input = Object.FindAnyObjectByType<SplatInputReader>();
            var so = new SerializedObject(rigComp);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("input").objectReferenceValue = input;
            so.FindProperty("followTarget").objectReferenceValue = follow;
            so.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            so.FindProperty("gameCamera").objectReferenceValue = cam;
            so.FindProperty("collision").objectReferenceValue = collision;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        private static void WirePlayer(GameObject splat, GameObject cameraRig, SplatMotorSettings motorSettings, CameraSettings cameraSettings)
        {
            var inputReader = splat.GetComponent<SplatInputReader>();
            var motor = splat.GetComponent<SplatMotor>();
            if (motor != null)
            {
                var so = new SerializedObject(motor);
                so.FindProperty("settings").objectReferenceValue = motorSettings;
                so.FindProperty("input").objectReferenceValue = inputReader;
                so.FindProperty("cameraTransform").objectReferenceValue = cameraRig.transform.Find("Pivot");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            _ = cameraSettings;
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            EnsureFolder(MatPath);
            var path = $"{MatPath}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            EditorUtility.SetDirty(mat);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
