#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds MechanicsPlayground craft loop: 3 components → BuildSite → Boing-O-Matic.
    /// TEMP ART only.
    /// </summary>
    public static class MechanicsPlaygroundBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Development/MechanicsPlayground.unity";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string DataPath = "Assets/SpaceAndCrafts/ScriptableObjects";
        private const string CollectiblePath = "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";
        private const string InputActionsPath = "Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions";

        [MenuItem("Space & Crafts/Bootstrap MechanicsPlayground (Craft Loop)")]
        public static void Bootstrap()
        {
            PlaygroundSceneBootstrap.Bootstrap();

            EnsureFolder(CollectiblePath);
            EnsureFolder(MatPath);

            var motorSettings = AssetDatabase.LoadAssetAtPath<SplatMotorSettings>($"{DataPath}/SplatMotorSettings.asset");
            var cameraSettings = AssetDatabase.LoadAssetAtPath<CameraSettings>($"{DataPath}/CameraSettings.asset");
            if (motorSettings == null || cameraSettings == null)
            {
                Debug.LogError("[Space & Crafts] Run PlayerPlayground bootstrap first (motor/camera settings missing).");
                return;
            }

            var springDef = CreateCollectible("component_spring", "Spring", "Bouncy coil scrap. Feels like it wants to launch something.");
            var dishDef = CreateCollectible("component_metal_dish", "Metal Dish", "Shiny dish. Perfect Boing-O-Matic seat.");
            var crystalDef = CreateCollectible("component_power_crystal", "Power Crystal", "Humming craft energy. Do not lick.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoots();
            ApplyAtmosphere();

            var world = new GameObject("TEMP_ART_MechanicsWorld");
            CreateGround(world.transform);
            CreateLaunchCourse(world.transform);

            var splatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (splatPrefab == null)
            {
                Debug.LogError($"[Space & Crafts] Missing prefab: {PrefabPath}");
                return;
            }

            var splat = (GameObject)PrefabUtility.InstantiatePrefab(splatPrefab);
            splat.name = "Splat_TEMP";
            splat.transform.position = new Vector3(0f, 0.9f, 0f);
            PrefabUtility.UnpackPrefabInstance(splat, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var cameraRig = CreateCameraRig(splat.transform, cameraSettings, splat.GetComponent<SplatInputReader>());
            WireMotorCamera(splat, cameraRig, motorSettings, cameraSettings);

            CreatePickup(world.transform, springDef, new Vector3(-4.5f, 0.7f, 3f), new Color(1f, 0.85f, 0.25f), new Vector3(0.55f, 0.35f, 0.55f));
            CreatePickup(world.transform, dishDef, new Vector3(0f, 0.55f, 5f), new Color(0.75f, 0.8f, 0.9f), new Vector3(1.1f, 0.25f, 1.1f));
            CreatePickup(world.transform, crystalDef, new Vector3(4.5f, 0.85f, 3f), new Color(0.95f, 0.35f, 0.85f), new Vector3(0.45f, 1.1f, 0.45f));

            CreateBuildSite(world.transform, splat.GetComponent<InventoryComponent>());

            var systems = new GameObject("MechanicsSystems");
            systems.AddComponent<GameplayToastHud>();
            systems.AddComponent<AudioSource>();
            systems.AddComponent<TempSfxPlayer>();
            var collectHud = systems.AddComponent<CollectibleHud>();
            collectHud.BindInventory(splat.GetComponent<InventoryComponent>());
            var promptHud = systems.AddComponent<InteractPromptHud>();
            promptHud.Bind(splat.GetComponent<SplatInteractor>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] MechanicsPlayground craft loop ready: collect 3 → interact at BuildSite → ride Boing-O-Matic. TEMP ART.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.78f, 0.72f, 0.88f);
            RenderSettings.fogDensity = 0.015f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.55f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(1f, 0.8f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.4f, 0.35f);
        }

        private static void ClearRoots()
        {
            foreach (var name in new[]
                     {
                         "TEMP_ART_MechanicsWorld",
                         "MechanicsSystems",
                         "Splat_TEMP",
                         "CameraRig",
                         "Main Camera",
                         "Directional Light"
                     })
            {
                var existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }
        }

        private static void CreateGround(Transform parent)
        {
            var ground = OrganicMeshFactory.CreateMeshObject(
                "ART_CraftPad",
                OrganicMeshFactory.SoftBlob(3, 1f, 0.18f, 0.03f),
                parent,
                Vector3.zero,
                new Vector3(11f, 1f, 11f),
                null);
            ApplyMat(ground, "Mat_CraftPad", new Color(0.45f, 0.55f, 0.7f), 0.35f, 0.1f);
            ground.AddComponent<BoxCollider>().size = Vector3.one;

            var key = new GameObject("ART_KeyLight");
            key.transform.SetParent(parent, false);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, -40f, 0f);
        }

        private static void CreateLaunchCourse(Transform parent)
        {
            var pad = OrganicMeshFactory.CreateMeshObject(
                "ART_LaunchTarget",
                OrganicSlabMesh.Create(0.1f, 3, 0.01f),
                parent,
                new Vector3(0f, 6.5f, 14f),
                new Vector3(4f, 0.5f, 4f),
                null);
            ApplyMat(pad, "Mat_LaunchTarget", new Color(0.35f, 0.9f, 0.65f), 0.4f, 0.15f);
            pad.AddComponent<BoxCollider>().size = Vector3.one;

            var pillar = OrganicMeshFactory.CreateMeshObject(
                "ART_LaunchPillar",
                OrganicMeshFactory.RibbedCylinder(3.2f, 0.45f, 8, 0.04f, 28, 10),
                parent,
                new Vector3(0f, 3.2f, 14f),
                Vector3.one,
                null);
            ApplyMat(pillar, "Mat_LaunchPillar", new Color(0.7f, 0.55f, 0.4f), 0.55f, 0.05f);
            pillar.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
        }

        private static void CreatePickup(
            Transform parent,
            CollectibleDefinition definition,
            Vector3 position,
            Color color,
            Vector3 scale)
        {
            var go = new GameObject($"ART_Pickup_{definition.Id}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var core = definition.Id switch
            {
                "component_spring" => OrganicMeshFactory.SpringHelix(0.5f, 0.22f, 0.04f, 4, 24),
                "component_metal_dish" => OrganicMeshFactory.DishBowl(0.45f, 0.12f, 10, 32),
                "component_power_crystal" => OrganicMeshFactory.CrystalFacet(0.7f, 0.2f, 6),
                _ => OrganicMeshFactory.SoftBlob(3, 0.35f, 0.85f, 0.05f)
            };
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = core;
            go.AddComponent<MeshRenderer>();
            ApplyMat(go, $"Mat_Pickup_{definition.Id}", color, 0.35f, 0.25f);

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.55f;

            var pickup = go.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("collectOnTriggerEnter").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pickup);
            EditorUtility.SetDirty(go);
            go.AddComponent<PickupMotion>();
            TempCollectibleSilhouette.Decorate(go.transform, definition.Id, color, ApplyMat);
        }

        private static void CreateBuildSite(Transform parent, InventoryComponent inventory)
        {
            var site = OrganicMeshFactory.CreateMeshObject(
                "ART_BuildSite",
                OrganicMeshFactory.SoftBlob(2, 1f, 0.35f, 0.03f),
                parent,
                new Vector3(0f, 0.55f, 8.5f),
                new Vector3(1.2f, 1f, 1.2f),
                null);
            ApplyMat(site, "Mat_BuildSite", new Color(0.72f, 0.52f, 0.3f), 0.65f, 0.05f);
            site.AddComponent<BoxCollider>().size = new Vector3(2f, 1f, 2f);

            var ghostRoot = new GameObject("ART_AssemblyGhosts");
            ghostRoot.transform.SetParent(site.transform, false);
            ghostRoot.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var ghostA = CreateGhost(ghostRoot.transform, "GhostSpring", new Vector3(-0.35f, 0.2f, 0f), new Vector3(0.25f, 0.18f, 0.25f), new Color(1f, 0.85f, 0.25f));
            var ghostB = CreateGhost(ghostRoot.transform, "GhostDish", new Vector3(0f, 0.1f, 0.1f), new Vector3(0.45f, 0.12f, 0.45f), new Color(0.75f, 0.8f, 0.9f));
            var ghostC = CreateGhost(ghostRoot.transform, "GhostCrystal", new Vector3(0.35f, 0.25f, 0f), new Vector3(0.18f, 0.4f, 0.18f), new Color(0.95f, 0.35f, 0.85f));

            var invention = new GameObject("ART_BoingOMatic");
            invention.transform.SetParent(site.transform, false);
            invention.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            invention.SetActive(false);

            TempBoingSilhouette.Build(invention.transform, ApplyMat);

            var trigger = invention.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.15f;
            trigger.center = new Vector3(0f, 0.15f, 0f);
            var boing = invention.AddComponent<BoingOMatic>();
            var boingSo = new SerializedObject(boing);
            boingSo.FindProperty("launchSpeed").floatValue = 24f;
            boingSo.FindProperty("launchDirection").vector3Value = new Vector3(0f, 1f, 0.85f);
            boingSo.FindProperty("cooldown").floatValue = 0.5f;
            boingSo.ApplyModifiedPropertiesWithoutUndo();

            var presenter = site.AddComponent<BuildAssemblyPresenter>();
            var presenterSo = new SerializedObject(presenter);
            var pieces = presenterSo.FindProperty("pieceRoots");
            pieces.arraySize = 3;
            pieces.GetArrayElementAtIndex(0).objectReferenceValue = ghostA.transform;
            pieces.GetArrayElementAtIndex(1).objectReferenceValue = ghostB.transform;
            pieces.GetArrayElementAtIndex(2).objectReferenceValue = ghostC.transform;
            presenterSo.FindProperty("inventionRoot").objectReferenceValue = invention;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var buildSite = site.AddComponent<BuildSite>();
            var buildSo = new SerializedObject(buildSite);
            buildSo.FindProperty("inventory").objectReferenceValue = inventory;
            // Presenter owns enabling the invention for the pop animation.
            buildSo.FindProperty("assembledInventionRoot").objectReferenceValue = null;
            buildSo.FindProperty("assemblyDuration").floatValue = 2.1f;
            buildSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(GetAssemblyStartedEvent(buildSite), presenter.PlayAssembly);

            EditorUtility.SetDirty(buildSite);
            EditorUtility.SetDirty(presenter);
        }

        private static UnityEngine.Events.UnityEvent GetAssemblyStartedEvent(BuildSite buildSite)
        {
            var so = new SerializedObject(buildSite);
            // Force Unity to create the event instance, then read via reflection for wiring.
            so.FindProperty("assemblyStarted");
            so.ApplyModifiedPropertiesWithoutUndo();
            var field = typeof(BuildSite).GetField(
                "assemblyStarted",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (UnityEngine.Events.UnityEvent)field?.GetValue(buildSite);
        }

        private static GameObject CreateGhost(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var ghost = OrganicMeshFactory.CreateMeshObject(
                $"ART_{name}",
                OrganicMeshFactory.SoftBlob(2, 0.35f, 0.85f, 0.04f),
                parent,
                localPos,
                scale,
                null);
            ApplyMat(ghost, $"Mat_{name}", color, 0.3f, 0.2f);
            ghost.SetActive(false);
            return ghost;
        }

        private static void WireMotorCamera(
            GameObject splat,
            GameObject cameraRig,
            SplatMotorSettings motorSettings,
            CameraSettings cameraSettings)
        {
            var inputReader = splat.GetComponent<SplatInputReader>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (inputReader != null && inputAsset != null)
            {
                var so = new SerializedObject(inputReader);
                so.FindProperty("inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var motor = splat.GetComponent<SplatMotor>();
            if (motor != null)
            {
                var so = new SerializedObject(motor);
                so.FindProperty("settings").objectReferenceValue = motorSettings;
                so.FindProperty("input").objectReferenceValue = inputReader;
                so.FindProperty("cameraTransform").objectReferenceValue = cameraRig.transform.Find("Pivot");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(motor);
            }

            var interactor = splat.GetComponent<SplatInteractor>();
            if (interactor != null)
            {
                var so = new SerializedObject(interactor);
                so.FindProperty("input").objectReferenceValue = inputReader;
                so.FindProperty("overlapOrigin").objectReferenceValue = splat.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(interactor);
            }

            var rig = cameraRig.GetComponent<PlatformingCameraRig>();
            if (rig != null)
            {
                var so = new SerializedObject(rig);
                so.FindProperty("settings").objectReferenceValue = cameraSettings;
                so.FindProperty("input").objectReferenceValue = inputReader;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rig);
            }

            var collision = cameraRig.GetComponent<CameraCollision>();
            if (collision != null)
            {
                var so = new SerializedObject(collision);
                so.FindProperty("settings").objectReferenceValue = cameraSettings;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(collision);
            }

            if (motor != null && new SerializedObject(motor).FindProperty("settings").objectReferenceValue == null)
            {
                Debug.LogError("[Space & Crafts] SplatMotor.settings failed to wire.");
            }

            if (rig != null && new SerializedObject(rig).FindProperty("settings").objectReferenceValue == null)
            {
                Debug.LogError("[Space & Crafts] PlatformingCameraRig.settings failed to wire.");
            }
        }

        private static GameObject CreateCameraRig(Transform follow, CameraSettings settings, SplatInputReader input)
        {
            var rig = new GameObject("CameraRig");
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(rig.transform, false);

            var camGo = new GameObject("GameCamera");
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.48f, 0.75f);
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();

            var collision = rig.AddComponent<CameraCollision>();
            var collisionSo = new SerializedObject(collision);
            SetObjectRef(collisionSo, "settings", settings);
            SetObjectRef(collisionSo, "followTarget", follow);
            SetObjectRef(collisionSo, "cameraTransform", camGo.transform);
            collisionSo.ApplyModifiedPropertiesWithoutUndo();

            var rigComp = rig.AddComponent<PlatformingCameraRig>();
            var so = new SerializedObject(rigComp);
            SetObjectRef(so, "settings", settings);
            SetObjectRef(so, "input", input);
            SetObjectRef(so, "followTarget", follow);
            SetObjectRef(so, "cameraPivot", pivot.transform);
            SetObjectRef(so, "gameCamera", cam);
            SetObjectRef(so, "collision", collision);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Batchmode SerializedObject refs to CameraSettings have been flaky; force via YAML-safe dirty cycle.
            EditorUtility.SetDirty(collision);
            EditorUtility.SetDirty(rigComp);
            EditorUtility.SetDirty(rig);

            return rig;
        }

        private static void SetObjectRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[Space & Crafts] Missing serialized property '{propertyName}' on {so.targetObject}.");
                return;
            }

            prop.objectReferenceValue = value;
        }

        private static CollectibleDefinition CreateCollectible(string id, string displayName, string description)
        {
            var path = $"{CollectiblePath}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<CollectibleDefinition>();
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").enumValueIndex = (int)CollectibleCategory.Component;
            so.FindProperty("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            var path = $"{MatPath}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
