#if UNITY_EDITOR
using System.Collections.Generic;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
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
    /// Grayboxes RainbowWorld_Arrival to the ARRIVAL_PLAN beat sheet.
    /// TEMP ART only — not finished visuals.
    /// </summary>
    public static class ArrivalGrayboxBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string DataPath = "Assets/SpaceAndCrafts/ScriptableObjects";
        private const string CollectiblePath = "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";
        private const float ArrivalFallRecoverySpan = ArrivalFjordBackdrop.FjordCatchSpan;
        private const float CheckpointRingRadius = 0.70f;
        private const float CheckpointRingTube = 0.016f;
        private static readonly Color CheckpointRingColor = new(0.46f, 0.38f, 0.62f);

        [MenuItem("Space & Crafts/Refresh Splat Visual in Saved Arrival")]
        public static void RefreshSplatVisualInSavedArrival()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Space & Crafts] Missing Arrival scene: {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                Debug.LogError("[Space & Crafts] Splat_TEMP was not found in the saved Arrival scene.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            EnsureSplatCommercialPresentation(splat);
            var visual = SplatMeshyVisualMode.IsEnabledOn(splat.transform)
                ? splat.transform.Find(SplatMeshyVisualMode.MeshyVisualRootName)
                : splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (!SplatMeshyVisualMode.IsEnabledOn(splat.transform)
                && !OrganicSplatBuilder.ContainsBone(visual, OrganicSplatBuilder.FaceVersionMarkerName))
            {
                Debug.LogError("[Space & Crafts] Approved Splat visual marker missing after refresh.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            if (SplatMeshyVisualMode.IsEnabledOn(splat.transform) && !HasUsableMeshyVisual(visual))
            {
                Debug.LogError("[Space & Crafts] Opt-in Meshy Splat visual is missing its rigged renderer/controller.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[Space & Crafts] Could not load Splat prefab: {PrefabPath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            try
            {
                EnsureSplatCommercialPresentation(prefabRoot);
                var meshMode = SplatMeshyVisualMode.IsEnabledOn(prefabRoot.transform);
                var prefabVisual = meshMode
                    ? prefabRoot.transform.Find(SplatMeshyVisualMode.MeshyVisualRootName)
                    : prefabRoot.transform.Find(OrganicSplatBuilder.VisualRootName);
                if ((!meshMode && !OrganicSplatBuilder.ContainsBone(prefabVisual, OrganicSplatBuilder.FaceVersionMarkerName))
                    || (meshMode && !HasUsableMeshyVisual(prefabVisual)))
                {
                    Debug.LogError("[Space & Crafts] Approved Splat visual marker missing after prefab refresh.");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Saved Arrival Splat visual refreshed to the approved design source.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Bootstrap Arrival Graybox (TEMP ART)")]
        public static void Bootstrap()
        {
            PlaygroundSceneBootstrap.Bootstrap();

            var motorSettings = AssetDatabase.LoadAssetAtPath<SplatMotorSettings>($"{DataPath}/SplatMotorSettings.asset");
            var cameraSettings = AssetDatabase.LoadAssetAtPath<CameraSettings>($"{DataPath}/CameraSettings.asset");
            // Refresh via CreateCollectible so pickup refs serialize reliably (LoadAssetAtPath left null defs in scene).
            var spring = CreateCollectible(
                "component_spring",
                "Spring",
                "Bouncy coil scrap. Feels like it wants to launch something.",
                CollectibleCategory.Component);
            var dish = CreateCollectible(
                "component_metal_dish",
                "Metal Dish",
                "Shiny bowl that wants to become a launcher cup.",
                CollectibleCategory.Component);
            var crystal = CreateCollectible(
                "component_power_crystal",
                "Power Crystal",
                "Humming scrap crystal — the spark for inventions.",
                CollectibleCategory.Component);
            var primaryMaterial = CreateCollectible(
                "material_rainbow_goo",
                "Rainbow Goo",
                "Primary teach-pickup. Squishy cosmic craft matter.",
                CollectibleCategory.Material);
            var badge = CreateCollectible(
                "discovery_badge_secret_nook",
                "Curious Nook Badge",
                "Proof you poked around Launch Gulch.",
                CollectibleCategory.DiscoveryBadge);
            var splatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssetDatabase.SaveAssets();
            // Re-load after SaveAssets — in-memory CreateAsset refs can go fake-null in batchmode.
            spring = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_spring.asset");
            dish = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_metal_dish.asset");
            crystal = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_power_crystal.asset");
            primaryMaterial = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/material_rainbow_goo.asset");
            badge = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/discovery_badge_secret_nook.asset");

            if (motorSettings == null || cameraSettings == null || spring == null || dish == null || crystal == null ||
                primaryMaterial == null || badge == null || splatPrefab == null)
            {
                Debug.LogError("[Space & Crafts] Run PlayerPlayground + MechanicsPlayground bootstraps first (missing deps).");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoots();
            ApplyAtmosphere();
            EnsureSceneInBuildSettings(ScenePath);

            // OpenScene can invalidate previously loaded SO refs in batchmode — reload again.
            spring = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_spring.asset");
            dish = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_metal_dish.asset");
            crystal = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/component_power_crystal.asset");
            primaryMaterial = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/material_rainbow_goo.asset");
            badge = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>($"{CollectiblePath}/discovery_badge_secret_nook.asset");
            if (spring == null || dish == null || crystal == null || primaryMaterial == null || badge == null)
            {
                throw new System.InvalidOperationException("CollectibleDefinition assets failed to load after OpenScene.");
            }

            var world = new GameObject("TEMP_ART_ArrivalWorld");
            CreateLighting(world.transform);

            // Zones along +Z with gateway silhouette always visible ahead.
            CreateArrivalShelf(world.transform);
            CreatePaintfallGlade(world.transform, spring, primaryMaterial);
            CreateJunkArchWorkshop(world.transform, dish, crystal);
            CreateLaunchGulch(world.transform);
            CreateSecretNook(world.transform, badge);
            CreateGatewayTerrace(world.transform);
            EnsureZoneLightingMoods(world.transform);
            var dormantRenderers = CreateDistantRainbowTease(world.transform);
            var systems = new GameObject("ArrivalSystems");
            systems.AddComponent<LevelSession>();
            systems.AddComponent<LevelFlowController>();
            systems.AddComponent<GameplayToastHud>();
            systems.AddComponent<AudioSource>();
            systems.AddComponent<TempSfxPlayer>();
            var collectHud = systems.AddComponent<CollectibleHud>();
            var promptHud = systems.AddComponent<InteractPromptHud>();
            var questDirector = systems.AddComponent<WorldQuestDirector>();
            var questHud = systems.AddComponent<QuestBoardHud>();
            systems.AddComponent<EnemyCombatDirector>();
            systems.AddComponent<SecretWonderRegistry>();
            var reveal = systems.AddComponent<GatewayRevealPresenter>();
            var vista = systems.AddComponent<GatewayCompleteVista>();
            systems.AddComponent<ArrivalOpeningVista>();
            systems.AddComponent<CraftAdmireBeat>();
            var revealSo = new SerializedObject(reveal);
            var arr = revealSo.FindProperty("dormantGatewayRenderers");
            arr.arraySize = dormantRenderers.Count;
            for (var i = 0; i < dormantRenderers.Count; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = dormantRenderers[i];
            }

            revealSo.ApplyModifiedPropertiesWithoutUndo();

            var splat = (GameObject)PrefabUtility.InstantiatePrefab(splatPrefab);
            splat.name = "Splat_TEMP";
            splat.transform.position = new Vector3(0f, 1.2f, -2f);
            PrefabUtility.UnpackPrefabInstance(splat, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            if (splat.GetComponent<PlayerRespawn>() == null)
            {
                splat.AddComponent<PlayerRespawn>();
            }

            EnsureSplatCommercialPresentation(splat);
            SpaceAndCrafts.Audio.WorldMusicDirector.Ensure(1);
            SpaceAndCrafts.Core.PerformanceBudgetGate.Ensure();

            var cameraRig = CreateCameraRig(splat.transform, cameraSettings, splat.GetComponent<SplatInputReader>());
            WirePlayer(splat, cameraRig, motorSettings, cameraSettings);
            PatchCameraSettingsRefs(cameraRig, cameraSettings);

            collectHud.BindInventory(splat.GetComponent<InventoryComponent>());
            promptHud.Bind(splat.GetComponent<SplatInteractor>());
            var gatewayFocus = GameObject.Find("TEMP_ART_MultiGatewayReveal");
            vista.Bind(cameraRig.GetComponent<PlatformingCameraRig>(), gatewayFocus != null ? gatewayFocus.transform : null);

            // Persistent zone banner so graybox never reads as finished.
            var banner = new GameObject("TEMP_ART_BANNER_WORLD");
            banner.transform.SetParent(world.transform, false);
            banner.transform.position = new Vector3(0f, 4.5f, 8f);
            var bannerComp = banner.AddComponent<TempArtBanner>();
            bannerComp.SetLabel("TEMP ART — Arrival graybox");

            // Build site inventory wiring after splat exists.
            var buildSite = Object.FindAnyObjectByType<BuildSite>();
            var pathStart = splat.transform.position;
            pathStart.y = 0f;
            var pathEnd = buildSite != null ? buildSite.transform.position : pathStart + Vector3.forward * 22f;
            pathEnd.y = 0f;
            CreateFallVolume(world.transform, Vector3.Lerp(pathStart, pathEnd, 0.5f));

            if (buildSite != null)
            {
                var so = new SerializedObject(buildSite);
                so.FindProperty("inventory").objectReferenceValue = splat.GetComponent<InventoryComponent>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            ArrivalOdysseyBootstrap.ApplyToOpenScene();
            questHud.Bind(questDirector);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] RainbowWorld_Arrival graybox built (TEMP ART). Enemy + boss + restart/secret + HUD + Odyssey systems wired.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static void RemoveFloatingScrapFieldFromSavedArrivalIter1522()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Space & Crafts] Missing Arrival scene: {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var field = GameObject.Find("ART_FloatingScrapField");
            if (field != null)
            {
                Object.DestroyImmediate(field);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Removed the non-interactive floating scrap decoration from the saved Arrival scene and future graybox bootstraps.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void ReplaceDistantGrayboxGatewayAssembliesWithMeshyAnchorsIter1524()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("TEMP_ART_MultiGatewayReveal");
            if (root == null)
            {
                root = new GameObject("TEMP_ART_MultiGatewayReveal");
                root.transform.position = new Vector3(0f, 4f, 96f);
            }

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            var offsets = new[]
            {
                new Vector3(-14f, 0f, 0f),
                new Vector3(14f, 2f, 4f),
                new Vector3(0f, 5f, 10f),
            };
            var names = new[]
            {
                "TEMP_ART_RainbowGateway_DormantA",
                "TEMP_ART_RainbowGateway_DormantB",
                "TEMP_ART_RainbowGateway_DormantC",
            };
            for (var i = 0; i < offsets.Length; i++)
            {
                var anchor = new GameObject(names[i]);
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.localPosition = offsets[i];
            }

            var presenter = Object.FindAnyObjectByType<GatewayRevealPresenter>();
            if (presenter != null)
            {
                var serialized = new SerializedObject(presenter);
                var renderers = serialized.FindProperty("dormantGatewayRenderers");
                renderers.arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Replaced the saved distant graybox gateways with empty Meshy placement anchors; runtime arrival dressing supplies commissioned models.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Refresh only the serialized Arrival checkpoint marker presentation, preserving
        /// the current scene layout and checkpoint trigger setup.
        /// </summary>
        public static void RefreshCheckpointMarkerPresentationIter971()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var checkpointMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MatPath}/Mat_Checkpoint.mat");
            if (checkpointMaterial == null)
            {
                Debug.LogError("[Space & Crafts] Mat_Checkpoint.mat is missing; checkpoint refresh aborted.");
                EditorApplication.Exit(1);
                return;
            }

            var checkpoints = Object.FindObjectsByType<CheckpointVolume>(FindObjectsInactive.Include);
            var refreshed = 0;
            for (var i = 0; i < checkpoints.Length; i++)
            {
                var checkpoint = checkpoints[i];
                if (checkpoint == null) continue;
                var filter = checkpoint.GetComponent<MeshFilter>();
                var renderer = checkpoint.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null) continue;

                filter.sharedMesh = OrganicMeshFactory.Torus(
                    CheckpointRingRadius, CheckpointRingTube, 36, 12);
                renderer.sharedMaterial = checkpointMaterial;
                EditorUtility.SetDirty(filter);
                EditorUtility.SetDirty(renderer);
                refreshed++;
            }

            checkpointMaterial.color = CheckpointRingColor;
            if (checkpointMaterial.HasProperty("_BaseColor"))
                checkpointMaterial.SetColor("_BaseColor", CheckpointRingColor);
            if (checkpointMaterial.HasProperty("_Color"))
                checkpointMaterial.SetColor("_Color", CheckpointRingColor);
            if (checkpointMaterial.HasProperty("_Smoothness"))
                checkpointMaterial.SetFloat("_Smoothness", 0.24f);
            if (checkpointMaterial.HasProperty("_Metallic"))
                checkpointMaterial.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(checkpointMaterial);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Space & Crafts] Arrival checkpoint marker refresh: checkpoints={refreshed}, sceneSaved={saved}, radius={CheckpointRingRadius:F3}, tube={CheckpointRingTube:F3}, color={CheckpointRingColor}.");
            EditorApplication.Exit(saved && refreshed > 0 ? 0 : 1);
        }

        private static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.46f, 0.59f, 0.68f);
            RenderSettings.fogDensity = 0.005f;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 190f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.7f, 1f);
            RenderSettings.ambientEquatorColor = new Color(1f, 0.75f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.45f, 0.4f);
        }

        private static void ClearRoots()
        {
            foreach (var name in new[]
                     {
                         "TEMP_ART_ArrivalWorld",
                         "ArrivalSystems",
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

        private static void CreateLighting(Transform parent)
        {
            var key = new GameObject("TEMP_ART_KeyLight");
            key.transform.SetParent(parent, false);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        /// <summary>
        /// Distinct per-zone Point/Spot moods under ART_ZoneLighting. Idempotent rebuild.
        /// </summary>
        private static void EnsureZoneLightingMoods(Transform worldRoot)
        {
            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_ArrivalShelf",
                pointColor: new Color(1f, 0.82f, 0.45f),
                pointIntensity: 2.4f,
                pointRange: 14f,
                pointLocal: new Vector3(0f, 4.5f, 1f),
                spotColor: new Color(1f, 0.9f, 0.55f),
                spotIntensity: 1.8f,
                spotRange: 18f,
                spotAngle: 55f,
                spotLocal: new Vector3(2f, 6f, -2f),
                spotEuler: new Vector3(50f, -20f, 0f));

            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_PaintfallGlade",
                pointColor: new Color(1f, 0.25f, 0.75f),
                pointIntensity: 2.8f,
                pointRange: 12f,
                pointLocal: new Vector3(-2f, 3.5f, 1f),
                spotColor: new Color(0.95f, 0.35f, 0.9f),
                spotIntensity: 2.0f,
                spotRange: 16f,
                spotAngle: 60f,
                spotLocal: new Vector3(4f, 5.5f, -1f),
                spotEuler: new Vector3(55f, 200f, 0f));

            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_JunkArchWorkshop",
                pointColor: new Color(1f, 0.7f, 0.25f),
                pointIntensity: 2.6f,
                pointRange: 13f,
                pointLocal: new Vector3(0f, 3.8f, 2f),
                spotColor: new Color(1f, 0.55f, 0.15f),
                spotIntensity: 2.1f,
                spotRange: 15f,
                spotAngle: 50f,
                spotLocal: new Vector3(-3f, 6f, 0f),
                spotEuler: new Vector3(45f, 35f, 0f));

            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_LaunchGulch",
                pointColor: new Color(0.75f, 0.88f, 1f),
                pointIntensity: 2.5f,
                pointRange: 14f,
                pointLocal: new Vector3(0f, 4f, 4f),
                spotColor: new Color(0.85f, 0.92f, 1f),
                spotIntensity: 1.9f,
                spotRange: 18f,
                spotAngle: 45f,
                spotLocal: new Vector3(3f, 5.5f, 8f),
                spotEuler: new Vector3(40f, -160f, 0f));

            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_SecretNook",
                pointColor: new Color(0.3f, 0.64f, 1f),
                pointIntensity: 0.65f,
                pointRange: 5f,
                pointLocal: new Vector3(0f, 2.4f, 0.2f),
                spotColor: new Color(0.62f, 0.52f, 1f),
                spotIntensity: 0.45f,
                spotRange: 6f,
                spotAngle: 55f,
                spotLocal: new Vector3(-1.2f, 3.2f, -1f),
                spotEuler: new Vector3(55f, 25f, 0f));

            PlaceZoneLighting(worldRoot, "TEMP_ART_Zone_GatewayTerrace",
                pointColor: new Color(0.55f, 0.75f, 1f),
                pointIntensity: 2.3f,
                pointRange: 16f,
                pointLocal: new Vector3(0f, 5f, 2f),
                spotColor: new Color(0.7f, 0.55f, 1f),
                spotIntensity: 2.2f,
                spotRange: 20f,
                spotAngle: 65f,
                spotLocal: new Vector3(-2f, 7f, -3f),
                spotEuler: new Vector3(48f, 15f, 0f),
                extraRainbowCool: true);
        }

        private static void PlaceZoneLighting(
            Transform worldRoot,
            string zoneName,
            Color pointColor,
            float pointIntensity,
            float pointRange,
            Vector3 pointLocal,
            Color spotColor,
            float spotIntensity,
            float spotRange,
            float spotAngle,
            Vector3 spotLocal,
            Vector3 spotEuler,
            bool extraRainbowCool = false)
        {
            var zone = worldRoot.Find(zoneName);
            if (zone == null)
            {
                var found = GameObject.Find(zoneName);
                zone = found != null ? found.transform : null;
            }

            if (zone == null)
            {
                Debug.LogWarning($"[Space & Crafts] Zone lighting skipped — missing {zoneName}");
                return;
            }

            var existing = zone.Find("ART_ZoneLighting");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("ART_ZoneLighting");
            root.transform.SetParent(zone, false);
            root.transform.localPosition = Vector3.zero;

            var pointGo = new GameObject("ART_ZonePoint");
            pointGo.transform.SetParent(root.transform, false);
            pointGo.transform.localPosition = pointLocal;
            var point = pointGo.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = pointColor;
            point.intensity = pointIntensity;
            point.range = pointRange;
            point.shadows = LightShadows.None;

            var spotGo = new GameObject("ART_ZoneSpot");
            spotGo.transform.SetParent(root.transform, false);
            spotGo.transform.localPosition = spotLocal;
            spotGo.transform.localRotation = Quaternion.Euler(spotEuler);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = spotColor;
            spot.intensity = spotIntensity;
            spot.range = spotRange;
            spot.spotAngle = spotAngle;
            spot.shadows = LightShadows.None;

            if (extraRainbowCool)
            {
                var accents = new[]
                {
                    (new Color(1f, 0.35f, 0.45f), new Vector3(-4f, 3.5f, 3f)),
                    (new Color(0.35f, 0.95f, 0.55f), new Vector3(4f, 3.8f, 2f)),
                    (new Color(0.4f, 0.65f, 1f), new Vector3(0f, 4.2f, 5f)),
                };
                for (var i = 0; i < accents.Length; i++)
                {
                    var accentGo = new GameObject($"ART_ZoneRainbowAccent_{i}");
                    accentGo.transform.SetParent(root.transform, false);
                    accentGo.transform.localPosition = accents[i].Item2;
                    var accent = accentGo.AddComponent<Light>();
                    accent.type = LightType.Point;
                    accent.color = accents[i].Item1;
                    accent.intensity = 1.35f;
                    accent.range = 9f;
                    accent.shadows = LightShadows.None;
                }
            }
        }

        private static void CreateArrivalShelf(Transform parent)
        {
            var zone = Zone(parent, "TEMP_ART_Zone_ArrivalShelf", new Vector3(0f, 0f, 0f));
            // Material language: clay base + tape/paint nursery steps (ArtPlayground).
            Platform(zone, "ShelfFloor", new Vector3(0f, 0f, 0f), new Vector3(18f, 0.4f, 14f), new Color(0.72f, 0.45f, 0.85f), 0.25f, 0f);
            Platform(zone, "NurseryA", new Vector3(-4f, 0.6f, 3f), new Vector3(2.2f, 0.4f, 2.2f), new Color(1f, 0.35f, 0.55f), 0.55f, 0.05f);
            Platform(zone, "NurseryB", new Vector3(-1.5f, 1.1f, 5f), new Vector3(2.2f, 0.4f, 2.2f), new Color(1f, 0.86f, 0.3f), 0.45f, 0.05f);
            Platform(zone, "NurseryC", new Vector3(1.5f, 1.6f, 7f), new Vector3(2.2f, 0.4f, 2.2f), new Color(0.45f, 0.85f, 1f), 0.85f, 0.1f);
            Label(zone, "TEMP_ART_LABEL_Try_These_Steps", new Vector3(-4f, 2.1f, 3f));
            Label(zone, "TEMP_ART_LABEL_Jump_Up", new Vector3(-1.5f, 2.6f, 5f));
            Label(zone, "TEMP_ART_LABEL_Keep_Going", new Vector3(1.5f, 3.1f, 7f));
            CreateCheckpoint(zone, new Vector3(0f, 0.5f, -3f), "Checkpoint_Shelf");
            DressClayBits(zone, new Vector3(5f, 0.4f, -4f));
            DressClayBits(zone, new Vector3(-6f, 0.4f, 2f));
            // Opening vista frame: rail + cosmic backdrop so Shelf reads as an overlook toward the gateway.
            CreateOpeningVista(zone);
            Label(zone, "TEMP_ART_LABEL_ArrivalShelf", new Vector3(0f, 3f, -4f));
            Label(zone, "TEMP_ART_LABEL_Look_Ahead_Rainbow", new Vector3(0f, 4.5f, 5f));
        }

        private static void CreateOpeningVista(Transform shelf)
        {
            Platform(shelf, "VistaRailL", new Vector3(-5.5f, 0.9f, 6.2f), new Vector3(0.35f, 1.4f, 3.5f), new Color(0.85f, 0.7f, 0.45f), 0.4f, 0.1f);
            Platform(shelf, "VistaRailR", new Vector3(5.5f, 0.9f, 6.2f), new Vector3(0.35f, 1.4f, 3.5f), new Color(0.85f, 0.7f, 0.45f), 0.4f, 0.1f);
            Platform(shelf, "VistaLedge", new Vector3(0f, 0.55f, 6.5f), new Vector3(8f, 0.25f, 1.2f), new Color(0.95f, 0.8f, 0.55f), 0.35f, 0.05f);

            // Soft cosmic plane (ribbon sheet), not a bare Unity Quad.
            var backdrop = OrganicMeshFactory.CreateMeshObject(
                "ART_CosmicBackdrop",
                OrganicMeshFactory.RibbonPlane(14f, 7f, 0.35f, 28, 14, 0.4f),
                shelf,
                new Vector3(0f, 6f, 18f),
                new Vector3(2f, 1f, 1f),
                null);
            backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ApplyMat(backdrop, "Mat_CosmicBackdrop", new Color(0.35f, 0.45f, 0.95f), 0.1f, 0.05f);

            for (var i = 0; i < 5; i++)
            {
                var star = OrganicMeshFactory.CreateMeshObject(
                    $"ART_VistaStar_{i}",
                    OrganicMeshFactory.FacetedStar(0.22f + (i % 3) * 0.05f, 0.09f, 0.06f, 5),
                    shelf,
                    new Vector3(-8f + i * 4f, 7.5f + (i % 2) * 1.2f, 16.5f),
                    Vector3.one,
                    null);
                ApplyMat(star, "Mat_VistaStar", new Color(1f, 0.95f, 0.75f), 0.05f, 0.1f);
            }

            var fill = new GameObject("TEMP_ART_VistaFillLight");
            fill.transform.SetParent(shelf, false);
            fill.transform.localPosition = new Vector3(2f, 4f, -2f);
            fill.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            var light = fill.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.65f, 0.75f, 1f);
            light.intensity = 0.35f;
            light.shadows = LightShadows.None;
        }

        private static void CreatePaintfallGlade(Transform parent, CollectibleDefinition spring, CollectibleDefinition primaryMaterial)
        {
            var zone = Zone(parent, "TEMP_ART_Zone_PaintfallGlade", new Vector3(0f, 0f, 18f));
            Platform(zone, "GladeFloor", Vector3.zero, new Vector3(16f, 0.4f, 12f), new Color(0.55f, 0.35f, 0.95f), 0.35f, 0.05f);
            Platform(zone, "PaintPool", new Vector3(-3f, 0.25f, 1f), new Vector3(3f, 0.15f, 2f), new Color(1f, 0.35f, 0.55f), 0.7f, 0.05f);
            CreatePickup(zone, primaryMaterial, new Vector3(-3f, 0.85f, 1f), new Color(1f, 0.45f, 0.75f), new Vector3(0.85f, 0.85f, 0.85f));
            CreatePickup(zone, spring, new Vector3(3f, 0.8f, 0f), new Color(1f, 0.85f, 0.25f), new Vector3(1.15f, 1.3f, 1.15f));
            // ARRIVAL_PLAN teach beat: short bounce foreshadows Boing-O-Matic before craft.
            CreateSpringTeachPad(zone, new Vector3(4.5f, 0.45f, 1.5f));
            CreateScrapMite(zone, new Vector3(0f, 0.7f, 3f));
            CreateScrapMite(zone, new Vector3(-2.5f, 0.7f, 4f));
            CreateScrapMite(zone, new Vector3(2.5f, 0.7f, 2.2f));
            CreateCheckpoint(zone, new Vector3(0f, 0.5f, -4f), "Checkpoint_Glade");
            DressPaintDrips(zone, new Vector3(-4.5f, 0.3f, -2f));
            DressPaintDrips(zone, new Vector3(5f, 0.3f, 3f));
            // Zone landmark: paint that falls upward (Rainbow Kingdom identity, TEMP).
            CreateUpwardPaintfall(zone, new Vector3(5.5f, 0.4f, -2.5f));
            Label(zone, "TEMP_ART_LABEL_PaintfallGlade", new Vector3(0f, 3f, -4f));
            Label(zone, "TEMP_ART_LABEL_Paint_Falls_Up", new Vector3(5.5f, 6.5f, -2.5f));
        }

        private static void CreateJunkArchWorkshop(Transform parent, CollectibleDefinition dish, CollectibleDefinition crystal)
        {
            var zone = Zone(parent, "TEMP_ART_Zone_JunkArchWorkshop", new Vector3(0f, 0f, 34f));
            Platform(zone, "WorkshopFloor", Vector3.zero, new Vector3(16f, 0.4f, 14f), new Color(0.78f, 0.58f, 0.32f), 0.15f, 0f);
            // Open center so Arrival Shelf can sightline the Gateway Terrace rainbow.
            Platform(zone, "ArchLeft", new Vector3(-5.5f, 2.5f, 2f), new Vector3(1.2f, 5f, 1.2f), new Color(0.55f, 0.58f, 0.62f), 0.7f, 0.85f);
            Platform(zone, "ArchRight", new Vector3(5.5f, 2.5f, 2f), new Vector3(1.2f, 5f, 1.2f), new Color(0.55f, 0.58f, 0.62f), 0.7f, 0.85f);
            Platform(zone, "ArchTopLeft", new Vector3(-4f, 5.2f, 2f), new Vector3(4f, 1f, 1.2f), new Color(0.78f, 0.58f, 0.32f), 0.2f, 0.05f);
            Platform(zone, "ArchTopRight", new Vector3(4f, 5.2f, 2f), new Vector3(4f, 1f, 1.2f), new Color(0.78f, 0.58f, 0.32f), 0.2f, 0.05f);

            CreatePickup(zone, dish, new Vector3(-3.5f, 0.7f, -2f), new Color(0.75f, 0.8f, 0.9f), new Vector3(1.4f, 0.85f, 1.4f));
            CreatePickup(zone, crystal, new Vector3(3.5f, 1f, -1f), new Color(0.32f, 0.78f, 0.98f), new Vector3(1.05f, 0.85f, 1.05f));

            CreateBuildSite(zone, new Vector3(0f, 0.75f, 3f));
            CreateAbandonedInvention(zone, new Vector3(-5f, 0.9f, 4.5f));
            CreateCheckpoint(zone, new Vector3(0f, 0.5f, -5f), "Checkpoint_Workshop");
            DressCardboardJunk(zone, new Vector3(-3f, 0.5f, 4.5f));
            DressCardboardJunk(zone, new Vector3(4f, 0.5f, -3.5f));
            DressBoltPile(zone, new Vector3(2.5f, 0.4f, 5f));
            Label(zone, "TEMP_ART_LABEL_JunkArchWorkshop", new Vector3(0f, 3.5f, -5f));
        }

        private static void CreateLaunchGulch(Transform parent)
        {
            var zone = Zone(parent, "TEMP_ART_Zone_LaunchGulch", new Vector3(0f, 0f, 50f));
            Platform(zone, "GulchNear", new Vector3(0f, 0f, -2f), new Vector3(10f, 0.4f, 6f), new Color(0.85f, 0.88f, 0.95f), 0.9f, 0.95f);
            // Gap that requires Boing-O-Matic (too far for normal jump)
            Platform(zone, "GulchFar", new Vector3(0f, 2.5f, 10f), new Vector3(10f, 0.4f, 8f), new Color(0.45f, 0.85f, 1f), 0.85f, 0.1f);
            CreateCheckpoint(zone, new Vector3(0f, 0.5f, -2f), "Checkpoint_GulchNear");
            // The fast spring arc crosses this ledge about 0.8 m from the marker;
            // keep the visual ring compact but give the trigger a reliable catch radius.
            CreateCheckpoint(zone, new Vector3(0f, 3f, 10f), "Checkpoint_GulchFar", 3f);
            // Unlocks after craft so the critical-path launch lives at the gap, not only at the workshop.
            CreateUnlockableBoingPad(zone, new Vector3(0f, 0.55f, 0.5f), new Vector3(0f, 0.55f, 1f), 30f, "Gulch");

            // Invisible trigger volume — no mesh (not a visible primitive).
            var hint = new GameObject("ART_InventionRequiredHint");
            hint.transform.SetParent(zone, false);
            hint.transform.localPosition = new Vector3(0f, 1f, -1f);
            var hintCol = hint.AddComponent<BoxCollider>();
            hintCol.isTrigger = true;
            hintCol.size = new Vector3(8f, 2f, 3f);
            hint.AddComponent<InventionRequiredHint>();

            DressFoilScrap(zone, new Vector3(-3.5f, 0.4f, -3f));
            DressFoilScrap(zone, new Vector3(3.5f, 2.9f, 11f));
            // Curiosity spur: visible side ledge toward Secret Nook (east of gulch).
            Platform(zone, "CuriositySpur", new Vector3(6.5f, 0.8f, 1f), new Vector3(3.2f, 0.3f, 2.2f), new Color(1f, 0.9f, 0.45f), 0.4f, 0.05f);
            var tease = OrganicMeshFactory.CreateMeshObject(
                "ART_SecretTeaseGlow",
                OrganicMeshFactory.SoftBlob(3, 0.28f, 1f, 0.04f),
                zone,
                new Vector3(7.5f, 1.8f, 1.2f),
                Vector3.one,
                null);
            ApplyMat(tease, "Mat_SecretTeaseGlow", new Color(0.4f, 1f, 0.85f), 0.1f, 0.2f);
            Label(zone, "TEMP_ART_LABEL_LaunchGulch_NeedsBoing", new Vector3(0f, 3f, 2f));
            Label(zone, "TEMP_ART_LABEL_Something_Over_There", new Vector3(6.5f, 2.4f, 1f));
        }

        private static void CreateSecretNook(Transform parent, CollectibleDefinition badge)
        {
            var zone = Zone(parent, "TEMP_ART_Zone_SecretNook", new Vector3(12f, 2.5f, 58f));
            Platform(zone, "NookFloor", Vector3.zero, new Vector3(5f, 0.35f, 5f), new Color(1f, 0.86f, 0.3f), 0.45f, 0.05f);
            // Alcove walls: readable optional space, not a bare floating pad.
            Platform(zone, "NookWallBack", new Vector3(0f, 1.4f, 2.2f), new Vector3(4.5f, 2.6f, 0.35f), new Color(0.18f, 0.27f, 0.43f), 0.35f, 0.05f);
            Platform(zone, "NookWallL", new Vector3(-2.3f, 1.2f, 0.4f), new Vector3(0.35f, 2.2f, 3.2f), new Color(0.15f, 0.23f, 0.38f), 0.35f, 0.05f);
            Platform(zone, "NookWallR", new Vector3(2.3f, 1.2f, 0.4f), new Vector3(0.35f, 2.2f, 3.2f), new Color(0.15f, 0.23f, 0.38f), 0.35f, 0.05f);
            // Pedestal + star ring: curiosity payoff reads as a shrine, not a lone pickup.
            Platform(zone, "NookPedestal", new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.55f, 1.4f), new Color(0.2f, 0.23f, 0.39f), 0.35f, 0.1f);
            CreatePickup(zone, badge, new Vector3(0f, 1.35f, 0f), new Color(0.4f, 1f, 0.85f), new Vector3(0.7f, 0.7f, 0.7f));
            for (var i = 0; i < 6; i++)
            {
                var ang = i * 60f * Mathf.Deg2Rad;
                var star = OrganicMeshFactory.CreateMeshObject(
                    $"ART_NookStar_{i}",
                    OrganicMeshFactory.FacetedStar(0.18f, 0.07f, 0.05f, 5),
                    zone,
                    new Vector3(Mathf.Cos(ang) * 1.6f, 1.8f + (i % 2) * 0.25f, Mathf.Sin(ang) * 1.6f),
                    Vector3.one,
                    null);
                ApplyMat(star, "Mat_NookStar", new Color(0.55f, 1f, 0.9f), 0.15f, 0.15f);
            }

            var glow = new GameObject("TEMP_ART_NookGlow");
            glow.transform.SetParent(zone, false);
            glow.transform.localPosition = new Vector3(0f, 1.6f, 0.5f);
            var point = glow.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(0.28f, 0.78f, 1f);
            point.intensity = 0.4f;
            point.range = 3.2f;

            Label(zone, "TEMP_ART_LABEL_OptionalSecret", new Vector3(0f, 2.8f, 0f));
            Label(zone, "TEMP_ART_LABEL_You_Found_It", new Vector3(0f, 3.4f, -1.2f));
        }

        private static void CreateGatewayTerrace(Transform parent)
        {
            // Start past GulchFar (ends ~z=64) so floors don't overlap the launch landing.
            var zone = Zone(parent, "TEMP_ART_Zone_GatewayTerrace", new Vector3(0f, 2.5f, 74f));
            // Matte violet stone keeps the boss arena in the Rainbow palette;
            // the previous near-mirror finish washed the color toward tan under
            // the Arrival lighting.
            var terraceFloor = Platform(zone, "TerraceFloor", Vector3.zero, new Vector3(18f, 0.5f, 14f), new Color(0.8f, 0.08f, 0.95f), 0.24f, 0.02f);
            var terraceMaterial = terraceFloor.GetComponent<Renderer>()?.sharedMaterial;
            if (terraceMaterial != null && terraceMaterial.HasProperty("_EmissionColor"))
            {
                // The cool-violet base swatch rendered olive under the Arrival
                // equator/ground fill. A restrained self-lit bias keeps the
                // broad boss deck in the Rainbow palette without flattening
                // its stone shading or turning it into a glowing stage.
                terraceMaterial.EnableKeyword("_EMISSION");
                terraceMaterial.SetColor("_EmissionColor", new Color(0.12f, 0.01f, 0.15f, 1f));
                EditorUtility.SetDirty(terraceMaterial);
            }
            // Arena ring: frames the invention exam without boxing the player in.
            Platform(zone, "ArenaRingN", new Vector3(0f, 0.7f, 5.8f), new Vector3(12f, 0.35f, 0.7f), new Color(0.7f, 0.4f, 0.95f), 0.5f, 0.15f);
            Platform(zone, "ArenaRingS", new Vector3(0f, 0.55f, -4.2f), new Vector3(10f, 0.3f, 0.55f), new Color(0.65f, 0.38f, 0.9f), 0.5f, 0.15f);
            // Lower the side lips to 22 cm so they read as deck edging rather
            // than foreground barricades and leave the Regent clear in the
            // approach view. Their colliders still meet the terrace floor.
            Platform(zone, "ArenaRingL", new Vector3(-7.5f, 0.36f, 0.5f), new Vector3(0.24f, 0.22f, 8f), new Color(0.6f, 0.35f, 0.85f), 0.45f, 0.15f);
            Platform(zone, "ArenaRingR", new Vector3(7.5f, 0.36f, 0.5f), new Vector3(0.24f, 0.22f, 8f), new Color(0.6f, 0.35f, 0.85f), 0.45f, 0.15f);
            // Move the relay encounter into the playable center of the deck so
            // the full slam warning ring stays on safe footing, not the far edge.
            CreateScrapStormBoss(zone, new Vector3(0f, 1.5f, 1f));
            // Keep the catch spring on the approach lip so the lower Gulch
            // launch can reach its trigger before meeting the terrace wall.
            CreateTerraceBoingPad(zone, new Vector3(0f, 0.6f, -7.5f));
            CreateCheckpoint(zone, new Vector3(0f, 0.6f, -6f), "Checkpoint_Terrace");

            // Primary gateway landmark
            // Tall primary rainbow for Shelf sightline (open workshop arch leaves center view clear).
            CreateRainbowArch(zone, "TEMP_ART_RainbowGateway_Primary", new Vector3(0f, 1.5f, 6f), 14f, true);
            var beacon = Platform(zone, "GatewayBeacon", new Vector3(0f, 10f, 6f), new Vector3(0.8f, 8f, 0.8f), new Color(1f, 0.85f, 0.35f));
            Object.DestroyImmediate(beacon.GetComponent<Collider>());
            Label(zone, "TEMP_ART_LABEL_GatewayTerrace", new Vector3(0f, 4f, -6f));
        }

        private static List<Renderer> CreateDistantRainbowTease(Transform parent)
        {
            // Keep the reveal beyond the terrace, but in front of the distant
            // fjord ridge so the three portal silhouettes clear the skyline.
            var root = Zone(parent, "TEMP_ART_MultiGatewayReveal", new Vector3(0f, 5f, 82f));
            // These are placement anchors only. ArrivalMeshyPathDress fills them
            // with cohesive commissioned Rainbow Gateway models at runtime and
            // wires those renderers into the post-boss progression reveal.
            var offsets = new[]
            {
                new Vector3(-14f, 0f, 0f),
                new Vector3(14f, 2f, 4f),
                new Vector3(0f, 5f, 10f),
            };
            var names = new[]
            {
                "TEMP_ART_RainbowGateway_DormantA",
                "TEMP_ART_RainbowGateway_DormantB",
                "TEMP_ART_RainbowGateway_DormantC",
            };
            for (var i = 0; i < offsets.Length; i++)
            {
                var anchor = new GameObject(names[i]);
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.localPosition = offsets[i];
            }

            return new List<Renderer>();
        }

        private static List<Renderer> CreateRainbowArch(Transform parent, string name, Vector3 localPos, float radius, bool primary)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            // Keep the Arrival portal a readable landmark without letting its
            // near-side pillar consume the boss approach camera. The old full
            // scale made a single arch rail dominate the foreground silhouette.
            root.transform.localScale = Vector3.one * (primary ? 0.60f : 1f);
            var renderers = new List<Renderer>();

            var colors = new[]
            {
                new Color(1f, 0.25f, 0.3f),
                new Color(1f, 0.55f, 0.15f),
                new Color(1f, 0.9f, 0.2f),
                new Color(0.3f, 0.85f, 0.4f),
                new Color(0.25f, 0.55f, 1f),
                new Color(0.55f, 0.35f, 0.95f)
            };

            // Organic tube arch (not cube segments).
            var thickness = primary ? 0.55f : 0.32f;
            var arch = OrganicMeshFactory.CreateMeshObject(
                "ART_GatewayArchTube",
                OrganicMeshFactory.ArchGateway(radius * 1.05f, radius * 0.95f, thickness, primary ? 36 : 24),
                root.transform,
                new Vector3(0f, 0.2f, 0f),
                Vector3.one,
                null);
            var archColor = primary ? colors[4] : colors[4] * 0.45f;
            archColor.a = 1f;
            ApplyMat(arch, primary ? "Mat_GatewayArch_P" : "Mat_GatewayArch_D", archColor, 0.3f, 0.25f);
            var archRenderer = arch.GetComponent<Renderer>();
            if (archRenderer != null)
            {
                renderers.Add(archRenderer);
            }

            // Color accent rings along the arch for rainbow read.
            var accents = primary ? 6 : 4;
            for (var i = 0; i < accents; i++)
            {
                var t = i / (float)(accents - 1);
                var angle = Mathf.Lerp(-70f, 70f, t) * Mathf.Deg2Rad;
                var x = Mathf.Sin(angle) * radius * 0.92f;
                var y = Mathf.Cos(angle) * radius * 0.92f + 0.5f;
                var bead = OrganicMeshFactory.CreateMeshObject(
                    $"ART_RainbowBead_{i}",
                    OrganicMeshFactory.SoftBlob(2, primary ? 0.28f : 0.16f, 1f, 0.03f),
                    root.transform,
                    new Vector3(x, y, 0f),
                    Vector3.one,
                    null);
                var c = colors[i % colors.Length];
                if (!primary)
                {
                    c *= 0.45f;
                    c.a = 1f;
                }

                ApplyMat(bead, $"Mat_ArrivalRainbowBead_{i}_{(primary ? "P" : "D")}", c, 0.25f, 0.3f);
                var beadRenderer = bead.GetComponent<Renderer>();
                if (beadRenderer != null)
                {
                    renderers.Add(beadRenderer);
                }
            }

            var pillarScale = primary ? new Vector3(1.2f, radius * 0.5f, 1.0f) : new Vector3(0.65f, radius * 0.38f, 0.55f);
            var pillarY = pillarScale.y * 0.5f;
            var pillarX = Mathf.Sin(75f * Mathf.Deg2Rad) * radius;
            AddGatewayOrganicPillar(root.transform, "ART_GatewayPillarL", new Vector3(-pillarX, pillarY, 0f), pillarScale,
                new Color(0.55f, 0.5f, 0.7f), primary, renderers);
            AddGatewayOrganicPillar(root.transform, "ART_GatewayPillarR", new Vector3(pillarX, pillarY, 0f), pillarScale,
                new Color(0.55f, 0.5f, 0.7f), primary, renderers);

            var keystone = OrganicMeshFactory.CreateMeshObject(
                "ART_GatewayKeystone",
                OrganicMeshFactory.CrystalFacet(primary ? 1.2f : 0.7f, primary ? 0.45f : 0.28f, 7),
                root.transform,
                new Vector3(0f, radius + 0.55f, 0f),
                Vector3.one,
                null);
            var keyColor = primary ? new Color(1f, 0.95f, 0.55f) : new Color(0.45f, 0.42f, 0.35f);
            ApplyMat(keystone, primary ? "Mat_GatewayKeystone_P" : "Mat_GatewayKeystone_D", keyColor, 0.2f, 0.55f);
            var keyRenderer = keystone.GetComponent<Renderer>();
            if (keyRenderer != null)
            {
                renderers.Add(keyRenderer);
            }

            if (primary)
            {
                var frame = OrganicMeshFactory.CreateMeshObject(
                    "ART_GatewayThreshold",
                    OrganicMeshFactory.SoftBlob(2, 1f, 0.22f, 0.02f),
                    root.transform,
                    new Vector3(0f, 0.25f, 0.35f),
                    new Vector3(radius * 0.95f, 0.72f, 0.9f),
                    null);
                var terraceRenderer = parent.Find("ART_TerraceFloor")?.GetComponent<Renderer>();
                var thresholdRenderer = frame.GetComponent<Renderer>();
                if (terraceRenderer != null && thresholdRenderer != null)
                {
                    // Match the broad gateway sill to the arena top. Its inherited
                    // parent/root offsets otherwise leave a long visual bar floating
                    // above the terrace in the boss approach.
                    var sillClearance = thresholdRenderer.bounds.min.y - terraceRenderer.bounds.max.y;
                    if (sillClearance > 0.02f && sillClearance < 2f)
                    {
                        frame.transform.position += Vector3.down * sillClearance;
                    }
                }
                ApplyMat(frame, "Mat_GatewayThreshold", new Color(0.56f, 0.45f, 0.68f), 0.3f, 0.35f);
                var frameRenderer = frame.GetComponent<Renderer>();
                if (frameRenderer != null)
                {
                    renderers.Add(frameRenderer);
                }
            }

            return renderers;
        }

        private static void AddGatewayOrganicPillar(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            bool primary,
            List<Renderer> renderers)
        {
            var go = OrganicMeshFactory.CreateMeshObject(
                name,
                OrganicMeshFactory.SoftBlob(2, 0.5f, 1.6f, 0.04f),
                parent,
                localPos,
                scale,
                null);
            if (!primary)
            {
                color *= 0.5f;
                color.a = 1f;
            }

            ApplyMat(go, $"{name}_Mat_{(primary ? "P" : "D")}", color, 0.35f, 0.2f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
        }

        private const string RainbowSpiderPrefabPath =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";

        private static bool TryAttachCommissionedRainbowSpider(Transform miteRoot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RainbowSpiderPrefabPath);
            if (prefab == null) return false;
            var visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual == null) return false;
            visual.name = "ART_MeshyEnemyVisual";
            visual.transform.SetParent(miteRoot, false);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            if (visual.GetComponentInChildren<SkinnedMeshRenderer>(true) == null
                || visual.GetComponentInChildren<Animator>(true) == null
                || visual.GetComponentInChildren<SpaceAndCrafts.AI.RainbowSpiderAnimatorDriver>(true) == null)
            {
                Object.DestroyImmediate(visual);
                return false;
            }
            return true;
        }
        private static void CreateScrapMite(Transform parent, Vector3 localPos)
        {
            var mite = new GameObject("ART_ScrapMite");
            mite.transform.SetParent(parent, false);
            mite.transform.localPosition = localPos;

            if (!TryAttachCommissionedRainbowSpider(mite.transform))
            {
                OrganicScrapMiteBuilder.Build(mite.transform, ApplyMat);
            }

            var trigger = mite.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.35f, 0f);
            trigger.size = new Vector3(1.2f, 0.9f, 1.2f);

            var pointA = new GameObject("PatrolA");
            pointA.transform.SetParent(parent, false);
            pointA.transform.position = mite.transform.position + Vector3.left * 2.5f;
            var pointB = new GameObject("PatrolB");
            pointB.transform.SetParent(parent, false);
            pointB.transform.position = mite.transform.position + Vector3.right * 2.5f;

            var ai = mite.AddComponent<ScrapMite>();
            var so = new SerializedObject(ai);
            so.FindProperty("pointA").objectReferenceValue = pointA.transform;
            so.FindProperty("pointB").objectReferenceValue = pointB.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            Label(mite.transform, "ART_LABEL_ScrapMite", new Vector3(0f, 1.3f, 0f));
        }

        private static void CreateScrapStormBoss(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_ScrapStormSentinel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            OrganicScrapStormBuilder.Build(root.transform, out var body, out var weakGo, out var dangerRing, ApplyMat);

            var weak = weakGo.AddComponent<BossWeakPoint>();
            var weakSo = new SerializedObject(weak);
            weakSo.FindProperty("visualRoot").objectReferenceValue = weakGo;
            weakSo.ApplyModifiedPropertiesWithoutUndo();

            var boss = root.AddComponent<ScrapStormSentinel>();
            var bossSo = new SerializedObject(boss);
            bossSo.FindProperty("body").objectReferenceValue = body;
            bossSo.FindProperty("weakPoint").objectReferenceValue = weak;
            bossSo.FindProperty("dangerRing").objectReferenceValue = dangerRing;
            bossSo.FindProperty("telegraphDuration").floatValue = 1.15f;
            bossSo.FindProperty("weakPointWindow").floatValue = 3.2f;
            var phaseMitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RainbowSpiderPrefabPath);
            if (phaseMitePrefab != null)
            {
                bossSo.FindProperty("commissionedPhaseMiteVisualPrefab").objectReferenceValue = phaseMitePrefab;
            }
            bossSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(
                GetUnityEvent(weak, "hit"),
                boss.NotifyWeakPointHit);

            Label(root.transform, "ART_LABEL_MiniBoss_ScrapStorm", new Vector3(0f, 3.5f, 0f));
        }

        private static void CreateSpringTeachPad(Transform parent, Vector3 localPos)
        {
            var pad = new GameObject("ART_SpringTeachPad");
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = localPos;

            var dish = OrganicMeshFactory.CreateMeshObject(
                "ART_TeachSpringDish",
                OrganicMeshFactory.DishBowl(0.7f, 0.2f, 10, 36),
                pad.transform,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(dish, "Mat_TeachSpringDish", new Color(1f, 0.9f, 0.35f), 0.35f, 0.45f);

            var coil = OrganicMeshFactory.CreateMeshObject(
                "ART_TeachSpringCoil",
                OrganicMeshFactory.SpringHelix(0.45f, 0.22f, 0.04f, 4, 20),
                pad.transform,
                new Vector3(0f, -0.28f, 0f),
                Vector3.one,
                null);
            ApplyMat(coil, "Mat_TeachSpringCoil", new Color(1f, 0.7f, 0.2f), 0.4f, 0.2f);

            var trigger = pad.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.95f;
            trigger.center = new Vector3(0f, 0.35f, 0f);
            var boing = pad.AddComponent<BoingOMatic>();
            var so = new SerializedObject(boing);
            so.FindProperty("launchSpeed").floatValue = 14f;
            so.FindProperty("launchDirection").vector3Value = Vector3.up;
            so.FindProperty("toastOnLaunch").boolValue = true;
            so.FindProperty("launchToast").stringValue = "Springy! Collect parts for a bigger Boing!";
            so.ApplyModifiedPropertiesWithoutUndo();
            Label(pad.transform, "TEMP_ART_LABEL_Try_This_Spring_Bounce", new Vector3(0f, 1.4f, 0f));
        }

        private static void CreateTerraceBoingPad(Transform parent, Vector3 localPos)
        {
            CreateUnlockableBoingPad(parent, localPos, new Vector3(0f, 0.38f, 1f), 28f, "Terrace", 2.1f);
        }

        private static void CreateUnlockableBoingPad(
            Transform parent,
            Vector3 localPos,
            Vector3 launchDirection,
            float launchSpeed,
            string label,
            float triggerRadius = 1.15f)
        {
            var unlockerGo = new GameObject($"TEMP_ART_{label}BoingUnlocker");
            unlockerGo.transform.SetParent(parent, false);
            unlockerGo.transform.localPosition = localPos;

            var pad = new GameObject($"TEMP_ART_{label}BoingOMatic");
            pad.transform.SetParent(unlockerGo.transform, false);
            pad.transform.localPosition = Vector3.zero;
            pad.SetActive(false);

            TempBoingSilhouette.Build(pad.transform, ApplyMat);

            var trigger = pad.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = triggerRadius;
            trigger.center = new Vector3(0f, 0.15f, 0f);
            var boing = pad.AddComponent<BoingOMatic>();
            var boingSo = new SerializedObject(boing);
            boingSo.FindProperty("launchSpeed").floatValue = launchSpeed;
            boingSo.FindProperty("launchDirection").vector3Value = launchDirection;
            boingSo.ApplyModifiedPropertiesWithoutUndo();

            var ghost = OrganicMeshFactory.CreateMeshObject(
                $"ART_{label}BoingGhost",
                OrganicMeshFactory.DishBowl(0.75f, 0.12f, 8, 28),
                unlockerGo.transform,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(ghost, $"Mat_{label}BoingGhost", new Color(0.75f, 0.8f, 0.95f, 0.35f), 0.1f, 0.05f);

            var unlocker = unlockerGo.AddComponent<BoingPadUnlocker>();
            var unlockSo = new SerializedObject(unlocker);
            unlockSo.FindProperty("padRoot").objectReferenceValue = pad;
            unlockSo.FindProperty("ghostRoot").objectReferenceValue = ghost;
            unlockSo.FindProperty("showToast").boolValue = label == "Gulch";
            unlockSo.FindProperty("unlockToast").stringValue = "Launch Gulch Boing pad online!";
            unlockSo.ApplyModifiedPropertiesWithoutUndo();
            Label(unlockerGo.transform, $"TEMP_ART_LABEL_{label}BoingPad", new Vector3(0f, 1.5f, 0f));
        }

        private static void CreateCheckpoint(Transform parent, Vector3 localPos, string name,
            float triggerRadius = 0.9f)
        {
            var go = OrganicMeshFactory.CreateMeshObject(
                $"ART_{name}",
                OrganicMeshFactory.Torus(CheckpointRingRadius, CheckpointRingTube, 36, 12),
                parent,
                localPos,
                Vector3.one,
                null);
            ApplyMat(go, "Mat_Checkpoint", CheckpointRingColor, 0.24f, 0f);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = triggerRadius;
            var checkpoint = go.AddComponent<CheckpointVolume>();
            var so = new SerializedObject(checkpoint);
            so.FindProperty("checkpointPose").objectReferenceValue = go.transform;
            so.FindProperty("checkpointId").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateFallVolume(Transform parent, Vector3 fjordCenter)
        {
            // Invisible water catch — collider only, no mesh.
            var go = new GameObject("ART_FallVolume");
            go.transform.SetParent(parent, false);
            // Runtime FallVolume follows the active checkpoint; this editor pose matches
            // the opening checkpoint until its first LateUpdate alignment.
            fjordCenter.y = -6.6f;
            go.transform.position = fjordCenter;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            // Keep the trigger inside the scenic water footprint so every fall it catches
            // lands over water; PlayerRespawn remains the global backup outside this square.
            col.size = new Vector3(ArrivalFallRecoverySpan, 2f, ArrivalFallRecoverySpan);
            go.AddComponent<FallVolume>();
        }

        private static CollectibleDefinition CreateCollectible(
            string id,
            string displayName,
            string description,
            CollectibleCategory category)
        {
            EnsureFolder(CollectiblePath);
            var path = $"{CollectiblePath}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
            if (existing == null)
            {
                // Broken/mismatched script on disk asset: replace so LoadAssetAtPath<T> works.
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                {
                    AssetDatabase.DeleteAsset(path);
                }

                existing = ScriptableObject.CreateInstance<CollectibleDefinition>();
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static UnityEngine.Events.UnityEvent GetUnityEvent(UnityEngine.Object target, string fieldName)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName);
            so.ApplyModifiedPropertiesWithoutUndo();
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (UnityEngine.Events.UnityEvent)field?.GetValue(target);
        }

        private static void CreateBuildSite(Transform parent, Vector3 localPos)
        {
            var site = OrganicMeshFactory.CreateMeshObject(
                "ART_BuildSite",
                OrganicMeshFactory.SoftBlob(2, 1.1f, 0.35f, 0.03f),
                parent,
                localPos,
                new Vector3(1.2f, 1f, 1.2f),
                null);
            ApplyMat(site, "Mat_ArrivalBuildSite", new Color(0.7f, 0.5f, 0.3f), 0.6f, 0.05f);
            var siteCol = site.AddComponent<BoxCollider>();
            siteCol.size = new Vector3(2.2f, 1f, 2.2f);

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
            boingSo.FindProperty("launchSpeed").floatValue = 26f;
            boingSo.FindProperty("launchDirection").vector3Value = new Vector3(0f, 0.85f, 1f);
            boingSo.FindProperty("cooldown").floatValue = 0.5f;
            boingSo.ApplyModifiedPropertiesWithoutUndo();

            var idle = invention.AddComponent<SpaceAndCrafts.Gameplay.InventionIdlePresenter>();
            idle.Configure("boing_o_matic");

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
            buildSo.FindProperty("assembledInventionRoot").objectReferenceValue = invention;
            // Match sequential snap + sputter timing in BuildAssemblyPresenter (~2.4s).
            buildSo.FindProperty("assemblyDuration").floatValue = 2.4f;
            buildSo.ApplyModifiedPropertiesWithoutUndo();

            var field = typeof(BuildSite).GetField("assemblyStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var started = (UnityEngine.Events.UnityEvent)field?.GetValue(buildSite);
            if (started != null)
            {
                UnityEventTools.AddPersistentListener(started, presenter.PlayAssembly);
            }
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

        private static void CreatePickup(Transform parent, CollectibleDefinition definition, Vector3 localPos, Color color, Vector3 scale)
        {
            if (definition == null)
            {
                throw new System.InvalidOperationException("CreatePickup requires a CollectibleDefinition.");
            }

            var go = new GameObject($"ART_Pickup_{definition.Id}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var coreMesh = definition.Id switch
            {
                "component_spring" => OrganicMeshFactory.SoftBlob(3, 0.35f, 0.7f, 0.04f),
                "component_metal_dish" => OrganicMeshFactory.DishBowl(0.55f, 0.28f, 12, 36),
                "component_power_crystal" => OrganicMeshFactory.CrystalFacet(0.7f, 0.2f, 6),
                "discovery_badge_secret_nook" => OrganicMeshFactory.FacetedStar(0.4f, 0.16f, 0.1f, 5),
                _ => OrganicMeshFactory.SoftBlob(3, 0.4f, 0.85f, 0.06f)
            };
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = coreMesh;
            var coreRenderer = go.AddComponent<MeshRenderer>();
            if (definition.Id == "component_spring")
            {
                // The detailed coil is the spring's silhouette. A generic tall
                // blob behind it reads like a neon rod at normal camera distance.
                coreRenderer.enabled = false;
            }
            ApplyMat(go, $"Mat_Pickup_{definition.Id}", color, 0.35f, 0.25f);
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;
            var pickup = go.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            var defProp = so.FindProperty("definition");
            defProp.objectReferenceValue = definition;
            so.FindProperty("collectOnTriggerEnter").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pickup);
            EditorUtility.SetDirty(go);
            if (defProp.objectReferenceValue == null)
            {
                throw new System.InvalidOperationException($"Failed to assign CollectibleDefinition '{definition.Id}' on pickup.");
            }

            go.AddComponent<PickupMotion>();
            TempCollectibleSilhouette.Decorate(go.transform, definition.Id, color, ApplyMat);
            var name = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Id : definition.DisplayName;
            Label(go.transform, $"TEMP_ART_LABEL_Pickup_{name.Replace(' ', '_')}", new Vector3(0f, 1.35f / Mathf.Max(scale.y, 0.2f), 0f));
        }

        private static Transform Zone(Transform parent, string name, Vector3 worldPos)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = worldPos;
            return zone.transform;
        }

        private static GameObject Platform(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            float smoothness = 0.4f,
            float metallic = 0.1f)
        {
            // Organic subdivided slab (not a bare Unity cube) — still replaceable by commissioned kit.
            var go = new GameObject($"ART_{name}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = LoadOrCreateChamferMesh();
            go.AddComponent<MeshRenderer>();
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            box.center = Vector3.zero;
            ApplyMat(go, $"Mat_Arrival_{name}", color, smoothness, metallic);
            return go;
        }

        private static Mesh LoadOrCreateChamferMesh()
        {
            EnsureFolder(MatPath);
            const string path = MatPath + "/ART_Mesh_OrganicSlab.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            // Force refresh from older TEMP chamfer asset if present.
            const string legacy = MatPath + "/TEMP_Mesh_ChamferBox.asset";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(legacy)))
            {
                AssetDatabase.DeleteAsset(legacy);
            }

            mesh = OrganicSlabMesh.Create(0.11f, 4, 0.014f);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void DressClayBits(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_Prop_ClayBits");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            for (var i = 0; i < 3; i++)
            {
                var blob = OrganicMeshFactory.CreateMeshObject(
                    $"ART_ClayBlob_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.28f - i * 0.04f, 0.7f, 0.08f),
                    root.transform,
                    new Vector3(i * 0.45f - 0.45f, 0.2f, (i % 2) * 0.3f),
                    Vector3.one,
                    null);
                ApplyMat(blob, "Mat_Lang_Clay", new Color(0.72f, 0.45f, 0.85f), 0.25f, 0f);
            }
        }

        private static void DressPaintDrips(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_Prop_PaintDrips");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            for (var i = 0; i < 4; i++)
            {
                var drip = OrganicMeshFactory.CreateMeshObject(
                    $"ART_PaintDrip_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.12f, 1.6f + i * 0.15f, 0.05f),
                    root.transform,
                    new Vector3(i * 0.35f - 0.5f, 0.35f + i * 0.1f, 0f),
                    Vector3.one,
                    null);
                var c = Color.HSVToRGB((0.9f + i * 0.05f) % 1f, 0.75f, 1f);
                ApplyMat(drip, $"Mat_PaintDrip_{i}", c, 0.65f, 0.05f);
            }
        }

        private static void CreateAbandonedInvention(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_AbandonedInvention");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = Quaternion.Euler(0f, 25f, -8f);

            var baseDish = OrganicMeshFactory.CreateMeshObject(
                "ART_AbandonedDish",
                OrganicMeshFactory.DishBowl(0.7f, 0.2f, 10, 36),
                root.transform,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(baseDish, "Mat_AbandonedDish", new Color(0.65f, 0.7f, 0.78f), 0.55f, 0.7f);

            var coil = OrganicMeshFactory.CreateMeshObject(
                "ART_AbandonedCoil",
                OrganicMeshFactory.SpringHelix(0.7f, 0.22f, 0.04f, 5, 22),
                root.transform,
                new Vector3(0.15f, 0.45f, 0f),
                Vector3.one,
                null);
            coil.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            ApplyMat(coil, "Mat_AbandonedCoil", new Color(1f, 0.7f, 0.25f), 0.4f, 0.2f);

            var cracked = OrganicMeshFactory.CreateMeshObject(
                "ART_AbandonedCrystalShard",
                OrganicMeshFactory.CrystalFacet(0.7f, 0.18f, 6),
                root.transform,
                new Vector3(-0.35f, 0.7f, 0.1f),
                Vector3.one,
                null);
            cracked.transform.localRotation = Quaternion.Euler(20f, 40f, 15f);
            ApplyMat(cracked, "Mat_AbandonedCrystal", new Color(0.85f, 0.35f, 0.75f), 0.7f, 0.1f);

            Label(root.transform, "ART_LABEL_Someone_Tried_Building", new Vector3(0f, 1.8f, 0f));
        }

        private static void CreateUpwardPaintfall(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_UpwardPaintfall");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            var basin = OrganicMeshFactory.CreateMeshObject(
                "ART_PaintfallBasin",
                OrganicMeshFactory.DishBowl(1.1f, 0.22f, 10, 36),
                root.transform,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(basin, "Mat_PaintfallBasin", new Color(1f, 0.4f, 0.65f), 0.55f, 0.08f);

            for (var i = 0; i < 5; i++)
            {
                var stream = OrganicMeshFactory.CreateMeshObject(
                    $"ART_PaintfallStream_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.14f - i * 0.01f, 2.2f + i * 0.2f, 0.04f),
                    root.transform,
                    new Vector3((i - 2) * 0.28f, 2.2f + i * 0.35f, 0f),
                    Vector3.one,
                    null);
                var c = Color.HSVToRGB((0.85f + i * 0.04f) % 1f, 0.8f, 1f);
                ApplyMat(stream, $"Mat_PaintfallStream_{i}", c, 0.7f, 0.05f);
            }

            var tip = OrganicMeshFactory.CreateMeshObject(
                "ART_PaintfallCloud",
                OrganicMeshFactory.SoftBlob(3, 0.85f, 0.55f, 0.08f),
                root.transform,
                new Vector3(0f, 6.2f, 0f),
                Vector3.one,
                null);
            ApplyMat(tip, "Mat_PaintfallCloud", new Color(1f, 0.55f, 0.85f), 0.45f, 0.1f);
        }

        private static void DressCardboardJunk(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_Prop_CardboardJunk");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            for (var i = 0; i < 3; i++)
            {
                var box = OrganicMeshFactory.CreateMeshObject(
                    $"ART_CardboardBox_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.32f, 0.65f, 0.03f),
                    root.transform,
                    new Vector3(i * 0.55f - 0.55f, 0.35f + i * 0.15f, i * 0.1f),
                    new Vector3(1.2f, 0.9f, 1f),
                    null);
                box.transform.localRotation = Quaternion.Euler(0f, 15f * i, 8f * i);
                ApplyMat(box, "Mat_Lang_Cardboard", new Color(0.78f, 0.58f, 0.32f), 0.15f, 0f);
            }

            var tape = OrganicMeshFactory.CreateMeshObject(
                "ART_TapeRoll",
                OrganicMeshFactory.Torus(0.2f, 0.05f, 28, 12),
                root.transform,
                new Vector3(0.2f, 0.85f, -0.2f),
                Vector3.one,
                null);
            tape.transform.localRotation = Quaternion.Euler(90f, 0f, 20f);
            ApplyMat(tape, "Mat_Lang_Tape", new Color(1f, 0.86f, 0.3f), 0.45f, 0.05f);
        }

        private static void DressBoltPile(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_Prop_BoltPile");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            for (var i = 0; i < 5; i++)
            {
                var bolt = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Bolt_{i}",
                    OrganicMeshFactory.RibbedCylinder(0.35f, 0.05f, 4, 0.01f, 16, 8),
                    root.transform,
                    new Vector3((i % 3) * 0.25f - 0.25f, 0.2f, (i / 3) * 0.25f),
                    Vector3.one,
                    null);
                bolt.transform.localRotation = Quaternion.Euler(20f * i, 40f * i, 10f);
                ApplyMat(bolt, "Mat_Lang_BoltMetal", new Color(0.55f, 0.58f, 0.62f), 0.7f, 0.85f);
            }
        }

        private static void DressFoilScrap(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("ART_Prop_FoilScrap");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            for (var i = 0; i < 3; i++)
            {
                var foil = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Foil_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.28f - i * 0.04f, 0.22f, 0.04f),
                    root.transform,
                    new Vector3(i * 0.4f - 0.4f, 0.15f, (i % 2) * 0.2f),
                    Vector3.one,
                    null);
                ApplyMat(foil, "Mat_Lang_Foil", new Color(0.85f, 0.88f, 0.95f), 0.9f, 0.95f);
            }
        }

        private static void Label(Transform parent, string name, Vector3 localPos)
        {
            var label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPos;
            var readable = name
                .Replace("TEMP_ART_LABEL_", string.Empty)
                .Replace('_', ' ');
            var banner = label.AddComponent<TempArtBanner>();
            banner.SetLabel($"TEMP · {readable}");
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

            var interactor = splat.GetComponent<SplatInteractor>();
            if (interactor != null)
            {
                var so = new SerializedObject(interactor);
                so.FindProperty("input").objectReferenceValue = inputReader;
                so.FindProperty("overlapOrigin").objectReferenceValue = splat.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            _ = cameraSettings;
        }

        private static void PatchCameraSettingsRefs(GameObject cameraRig, CameraSettings cameraSettings)
        {
            // Direct assignment via SerializedObject + immediate scene YAML is unreliable in batchmode;
            // also assign through SerializedObject and log if still null.
            foreach (var mb in cameraRig.GetComponents<MonoBehaviour>())
            {
                var so = new SerializedObject(mb);
                var prop = so.FindProperty("settings");
                if (prop == null)
                {
                    continue;
                }

                prop.objectReferenceValue = cameraSettings;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mb);
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
            cam.backgroundColor = new Color(0.62f, 0.74f, 0.95f);
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();

            var collision = rig.AddComponent<CameraCollision>();
            var collisionSo = new SerializedObject(collision);
            collisionSo.FindProperty("settings").objectReferenceValue = settings;
            collisionSo.FindProperty("followTarget").objectReferenceValue = follow;
            collisionSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            collisionSo.ApplyModifiedPropertiesWithoutUndo();

            var rigComp = rig.AddComponent<PlatformingCameraRig>();
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

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            EnsureFolder(MatPath);
            var path = $"{MatPath}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = assetName == "Mat_SplatEyeGlint"
                    ? Shader.Find("Universal Render Pipeline/Unlit")
                    : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            var expectedShader = assetName == "Mat_SplatEyeGlint"
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (expectedShader != null && mat.shader != expectedShader)
            {
                mat.shader = expectedShader;
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

        private static void EnsureSplatCommercialPresentation(GameObject splat)
        {
            if (splat == null)
            {
                return;
            }

            var motor = splat.GetComponent<SplatMotor>();
            var meshMode = SplatMeshyVisualMode.IsEnabledOn(splat.transform);
            var visual = meshMode
                ? splat.transform.Find(SplatMeshyVisualMode.MeshyVisualRootName)
                : splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (meshMode)
            {
                // Preserve the existing player root, motor and capsule while the
                // explicitly opted-in skinned Meshy candidate supplies the visual.
                SplatVisualRepair.Repair(splat.transform);
            }
            // Rebuild visual if prefab still has pre-skeleton hierarchy (no Bone_Hips).
            if (!meshMode && (visual == null
                || !OrganicSplatBuilder.ContainsBone(visual, OrganicSplatBuilder.HipsName)
                || !OrganicSplatBuilder.ContainsBone(visual, OrganicSplatBuilder.FaceVersionMarkerName)))
            {
                if (visual != null)
                {
                    Object.DestroyImmediate(visual.gameObject);
                }

                visual = OrganicSplatBuilder.BuildVisualRoot(splat.transform, ApplyMat);
            }

            var feel = splat.GetComponent<SplatFeelPresenter>();
            if (feel != null)
            {
                var feelSo = new SerializedObject(feel);
                feelSo.FindProperty("motor").objectReferenceValue = motor;
                feelSo.FindProperty("visualRoot").objectReferenceValue = visual;
                feelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var skeleton = splat.GetComponent<SplatSkeletonAnimator>() ?? splat.AddComponent<SplatSkeletonAnimator>();
            var skSo = new SerializedObject(skeleton);
            skSo.FindProperty("motor").objectReferenceValue = motor;
            skSo.FindProperty("visualRoot").objectReferenceValue = meshMode
                ? splat.transform.Find(OrganicSplatBuilder.VisualRootName)
                : visual;
            skSo.ApplyModifiedPropertiesWithoutUndo();
            if (meshMode)
            {
                skeleton.enabled = false;
            }

            var voice = splat.GetComponent<CharacterVoicePlayer>() ?? splat.AddComponent<CharacterVoicePlayer>();
            var voiceSo = new SerializedObject(voice);
            voiceSo.FindProperty("motor").objectReferenceValue = motor;
            voiceSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasUsableMeshyVisual(Transform visual)
        {
            if (visual == null) return false;
            var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || animator.runtimeAnimatorController == null) return false;
            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.vertexCount >= 3
                    && renderer.bones != null && renderer.bones.Length > 0) return true;
            }
            return false;
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
