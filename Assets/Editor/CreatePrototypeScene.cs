using SpaceAndCrafts.CameraSystem;
using SpaceAndCrafts.Collectibles;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Enemies;
using SpaceAndCrafts.Environment;
using SpaceAndCrafts.Interaction;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    public static class CreatePrototypeScene
    {
        private const string ScenePath = "Assets/Scenes/World01_Test.unity";
        private const string HomeScenePath = "Assets/Scenes/Home.unity";
        private static int playerLayer;
        private static int phaseWallLayer;
        private static int enemyLayer;

        [MenuItem("Space & Crafts/Create Rainbow Kingdom")]
        public static void Create()
        {
            playerLayer = EnsureLayer("Player");
            phaseWallLayer = EnsureLayer("PhaseWall");
            enemyLayer = EnsureLayer("Enemy");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureRendering();
            Materials materials = new Materials();

            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<GameHud>();
            systems.AddComponent<GameQualitySettings>();
            systems.AddComponent<PauseMenu>();

            CreateWorld(materials);
            GameObject player = CreateSplat(materials, new Vector3(0f, 1.1f, -14f));
            CreateCamera(player.transform);
            CreateLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            CreateHomeMenuScene(materials);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HomeScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"Created the Space & Crafts home screen and Splat's Rainbow Kingdom at {ScenePath}");
        }

        private static void CreateHomeMenuScene(Materials m)
        {
            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject menuSystems = new GameObject("Home Menu");
            menuSystems.AddComponent<HomeMenuController>();
            menuSystems.AddComponent<GameQualitySettings>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 53f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.11f);
            cameraObject.transform.position = new Vector3(0f, 5f, -21f);
            cameraObject.transform.LookAt(new Vector3(0f, 2.8f, 0f));
            cameraObject.AddComponent<AudioListener>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            GameObject sunObject = new GameObject("Menu Key Light");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.6f;
            sun.color = new Color(1f, 0.86f, 0.72f);
            sunObject.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            RenderSettings.ambientLight = new Color(0.25f, 0.3f, 0.5f);

            GameObject planet = CreatePrimitive(PrimitiveType.Sphere, "Rainbow World", new Vector3(0f, -5.5f, 4f), new Vector3(19f, 5f, 13f), m.Grass, null);
            Object.DestroyImmediate(planet.GetComponent<Collider>());

            GameObject displaySplat = new GameObject("Menu Splat");
            displaySplat.transform.position = new Vector3(-7.5f, 1.4f, 2f);
            GameObject body = CreateLocalPrimitive(PrimitiveType.Sphere, "Splat Body", displaySplat.transform, Vector3.zero, new Vector3(2.5f, 2f, 2.2f), m.SplatPurple);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Left Eye", displaySplat.transform, new Vector3(-0.48f, 0.35f, -1.02f), new Vector3(0.5f, 0.62f, 0.28f), m.White);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Right Eye", displaySplat.transform, new Vector3(0.48f, 0.35f, -1.02f), new Vector3(0.5f, 0.62f, 0.28f), m.White);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Left Pupil", displaySplat.transform, new Vector3(-0.48f, 0.35f, -1.25f), new Vector3(0.17f, 0.22f, 0.1f), m.SplatDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Right Pupil", displaySplat.transform, new Vector3(0.48f, 0.35f, -1.25f), new Vector3(0.17f, 0.22f, 0.1f), m.SplatDark);
            foreach (Collider collider in displaySplat.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            MenuFloat splatFloat = displaySplat.AddComponent<MenuFloat>();
            splatFloat.Configure(new Vector3(0f, 9f, 0f), 0.38f, 1.25f, 0f);

            GameObject menuShip = new GameObject("Menu Rainbow Rider");
            menuShip.transform.position = new Vector3(7.5f, 3.5f, 5f);
            menuShip.transform.rotation = Quaternion.Euler(8f, -25f, 0f);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Ship Hull", menuShip.transform, Vector3.zero, new Vector3(3.5f, 1.25f, 4.5f), m.ShipSilver);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Cockpit", menuShip.transform, new Vector3(0f, 0.8f, -0.2f), new Vector3(1.8f, 1f, 2.1f), m.WaterBlue);
            CreateLocalPrimitive(PrimitiveType.Cube, "Left Wing", menuShip.transform, new Vector3(-3f, -0.1f, 0f), new Vector3(3.2f, 0.25f, 2f), m.Rainbow[5]);
            CreateLocalPrimitive(PrimitiveType.Cube, "Right Wing", menuShip.transform, new Vector3(3f, -0.1f, 0f), new Vector3(3.2f, 0.25f, 2f), m.Rainbow[6]);
            foreach (Collider collider in menuShip.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            MenuFloat shipFloat = menuShip.AddComponent<MenuFloat>();
            shipFloat.Configure(new Vector3(0f, -5f, 0f), 0.55f, 0.8f, 1.7f);

            for (int segment = 0; segment < 34; segment++)
            {
                float angle = segment * Mathf.PI * 2f / 34f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 13f, 2.5f + Mathf.Sin(angle * 2f) * 2f, 8f + Mathf.Sin(angle) * 5f);
                GameObject rainbowPiece = CreatePrimitive(PrimitiveType.Sphere, $"Menu Rainbow {segment + 1:00}", position, Vector3.one * 0.85f, m.Rainbow[segment % 7], null);
                Object.DestroyImmediate(rainbowPiece.GetComponent<Collider>());
            }

            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene, HomeScenePath);
        }

        private static void CreateWorld(Materials m)
        {
            GameObject world = new GameObject("Rainbow Kingdom");
            CreateSpaceBackdrop(m, world.transform);
            GameObject island = CreatePrimitive(PrimitiveType.Cylinder, "Rainbow Island", new Vector3(0f, -1.2f, 15f), new Vector3(110f, 1.2f, 110f), m.Grass, world.transform);
            Object.DestroyImmediate(island.GetComponent<Collider>());
            BoxCollider islandCollider = island.AddComponent<BoxCollider>();
            islandCollider.size = new Vector3(1f, 2f, 1f);
            GameObject islandEdge = CreatePrimitive(PrimitiveType.Cylinder, "Island Edge", new Vector3(0f, -1.75f, 15f), new Vector3(112f, 0.5f, 112f), m.Soil, world.transform);
            Object.DestroyImmediate(islandEdge.GetComponent<Collider>());

            CreatePath(m, world.transform);
            CreateRainbowArch(new Vector3(0f, 0f, 20f), m.Rainbow, world.transform);
            CreateTrees(m, world.transform);
            CreateClouds(m, world.transform);
            CreatePlatforms(m, world.transform);
            CreateCrystals(m, world.transform);
            CreateSignsAndBounceFlowers(m, world.transform);
            CreateLandscaping(m, world.transform);
            CreateWorldDetail(m, world.transform);
            CreateCombatCourse(m, world.transform);

            CreatePrimitive(PrimitiveType.Cylinder, "Crystal Plaza", new Vector3(0f, 0.05f, 7f), new Vector3(10f, 0.12f, 10f), m.White, world.transform);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector3 position = new Vector3(Mathf.Sin(angle) * 6f, 0.65f, 7f + Mathf.Cos(angle) * 6f);
                GameObject plazaColor = CreatePrimitive(PrimitiveType.Sphere, $"Plaza Color {i + 1}", position, new Vector3(1.2f, 0.25f, 1.2f), m.Rainbow[i % m.Rainbow.Length], world.transform);
                Object.DestroyImmediate(plazaColor.GetComponent<Collider>());
            }
        }

        private static GameObject CreateSplat(Materials m, Vector3 position)
        {
            GameObject root = new GameObject("Player_Splat");
            root.transform.position = position;
            root.tag = "Player";
            root.layer = playerLayer;

            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 1.65f;
            character.radius = 0.62f;
            character.center = new Vector3(0f, -0.05f, 0f);
            character.stepOffset = 0.35f;

            PlayerController3D controller = root.AddComponent<PlayerController3D>();
            PlayerAbilities abilities = root.AddComponent<PlayerAbilities>();
            root.AddComponent<PlayerHealth>();
            PlayerCombat combat = root.AddComponent<PlayerCombat>();
            root.AddComponent<PlayerStomp>();
            root.AddComponent<PlayerFallReset>();
            root.AddComponent<PlayerInteractor>();

            GameObject visual = new GameObject("Splat Visual");
            visual.transform.SetParent(root.transform, false);

            GameObject body = CreateLocalPrimitive(PrimitiveType.Sphere, "Purple Blob Body", visual.transform, new Vector3(0f, -0.05f, 0f), new Vector3(1.35f, 1.05f, 1.18f), m.SplatPurple);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            GameObject leftLeg = CreateLocalPrimitive(PrimitiveType.Capsule, "Left Leg", visual.transform, new Vector3(-0.42f, -0.63f, 0.02f), new Vector3(0.3f, 0.5f, 0.3f), m.SplatPurple);
            GameObject rightLeg = CreateLocalPrimitive(PrimitiveType.Capsule, "Right Leg", visual.transform, new Vector3(0.42f, -0.63f, 0.02f), new Vector3(0.3f, 0.5f, 0.3f), m.SplatPurple);
            GameObject leftFoot = CreateLocalPrimitive(PrimitiveType.Sphere, "Left Foot", leftLeg.transform, new Vector3(0f, -0.48f, 0.18f), new Vector3(0.62f, 0.3f, 0.85f), m.SplatDark);
            GameObject rightFoot = CreateLocalPrimitive(PrimitiveType.Sphere, "Right Foot", rightLeg.transform, new Vector3(0f, -0.48f, 0.18f), new Vector3(0.62f, 0.3f, 0.85f), m.SplatDark);
            GameObject leftArm = CreateLocalPrimitive(PrimitiveType.Capsule, "Left Arm", visual.transform, new Vector3(-0.88f, -0.02f, 0.02f), new Vector3(0.28f, 0.62f, 0.28f), m.SplatPurple);
            leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, -68f);
            GameObject rightArm = CreateLocalPrimitive(PrimitiveType.Capsule, "Right Arm", visual.transform, new Vector3(0.88f, -0.02f, 0.02f), new Vector3(0.28f, 0.62f, 0.28f), m.SplatPurple);
            rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, 68f);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Left Hand", leftArm.transform, new Vector3(0f, -0.68f, 0f), new Vector3(0.4f, 0.4f, 0.4f), m.SplatPurple);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Right Hand", rightArm.transform, new Vector3(0f, -0.68f, 0f), new Vector3(0.4f, 0.4f, 0.4f), m.SplatPurple);

            CreateEye(visual.transform, "Left", new Vector3(-0.3f, 0.2f, 0.55f), m);
            CreateEye(visual.transform, "Right", new Vector3(0.3f, 0.2f, 0.55f), m);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Smile", visual.transform, new Vector3(0f, -0.2f, 0.59f), new Vector3(0.24f, 0.07f, 0.06f), m.SplatDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Head Tuft", visual.transform, new Vector3(-0.26f, 0.65f, 0f), new Vector3(0.25f, 0.45f, 0.25f), m.SplatPurple);

            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }

            foreach (Transform child in visual.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = playerLayer;
            }

            abilities.Configure(visual.GetComponentsInChildren<Renderer>());
            GameObject muzzle = new GameObject("Water Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0.55f, 0f, 0.75f);
            combat.SetMuzzle(muzzle.transform);

            GameObject heldWaterGun = new GameObject("Held Water Gun");
            heldWaterGun.transform.SetParent(visual.transform, false);
            heldWaterGun.transform.localPosition = new Vector3(0.72f, -0.03f, 0.52f);
            CreateLocalPrimitive(PrimitiveType.Cube, "Held Blaster", heldWaterGun.transform, Vector3.zero, new Vector3(0.65f, 0.24f, 0.9f), m.WaterBlue);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Held Tank", heldWaterGun.transform, new Vector3(0f, 0.28f, -0.12f), new Vector3(0.25f, 0.25f, 0.25f), m.White);
            foreach (Collider gunCollider in heldWaterGun.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(gunCollider);
            }
            combat.SetWaterGunVisual(heldWaterGun);

            SplatVisualMotion motion = root.AddComponent<SplatVisualMotion>();
            motion.Configure(controller, body.transform, visual.transform, leftArm.transform, rightArm.transform, leftLeg.transform, rightLeg.transform, combat);
            return root;
        }

        private static void CreateEye(Transform parent, string side, Vector3 position, Materials m)
        {
            CreateLocalPrimitive(PrimitiveType.Sphere, $"{side} Eye", parent, position, new Vector3(0.31f, 0.38f, 0.18f), m.White);
            CreateLocalPrimitive(PrimitiveType.Sphere, $"{side} Pupil", parent, position + new Vector3(0f, 0f, 0.105f), new Vector3(0.105f, 0.14f, 0.07f), m.SplatDark);
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.055f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraObject.AddComponent<AudioListener>();
            PlatformCameraFollow follow = cameraObject.AddComponent<PlatformCameraFollow>();
            follow.SetTarget(target);
            cameraObject.transform.position = target.position + new Vector3(0f, 5f, -8f);
            cameraObject.transform.LookAt(target.position + Vector3.up);
        }

        private static void CreateLighting()
        {
            GameObject sunObject = new GameObject("Rainbow Sun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.92f, 0.76f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;
            sun.shadowBias = 0.035f;
            sun.shadowNormalBias = 0.35f;
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.68f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.42f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.16f, 0.2f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.055f, 0.075f, 0.18f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 120f;
        }

        private static void CreateSpaceBackdrop(Materials m, Transform parent)
        {
            GameObject space = new GameObject("Outer Space Backdrop");
            space.transform.SetParent(parent);
            GameObject dome = CreatePrimitive(PrimitiveType.Sphere, "Deep Space Sky", new Vector3(0f, 30f, 18f), Vector3.one * 165f, m.SpaceSky, space.transform);
            Object.DestroyImmediate(dome.GetComponent<Collider>());

            for (int i = 0; i < 115; i++)
            {
                float azimuth = i * 2.399963f;
                float elevation = 0.12f + ((i * 37) % 70) / 100f;
                float radius = 112f + (i % 6) * 6f;
                Vector3 direction = new Vector3(Mathf.Cos(azimuth) * Mathf.Cos(elevation), Mathf.Sin(elevation), Mathf.Sin(azimuth) * Mathf.Cos(elevation));
                Vector3 position = new Vector3(0f, 10f, 18f) + direction * radius;
                float size = i % 17 == 0 ? 0.9f : i % 7 == 0 ? 0.48f : 0.22f;
                Material starMaterial = i % 11 == 0 ? m.StarBlue : i % 13 == 0 ? m.StarPink : m.StarWhite;
                GameObject star = CreatePrimitive(PrimitiveType.Sphere, $"Star {i + 1:000}", position, Vector3.one * size, starMaterial, space.transform);
                Object.DestroyImmediate(star.GetComponent<Collider>());
            }

            GameObject planet = CreatePrimitive(PrimitiveType.Sphere, "Distant Violet Planet", new Vector3(-82f, 48f, 78f), Vector3.one * 13f, m.Rainbow[5], space.transform);
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            GameObject ring = CreatePrimitive(PrimitiveType.Cylinder, "Violet Planet Rings", new Vector3(-82f, 48f, 78f), new Vector3(20f, 0.18f, 20f), m.StarPink, space.transform);
            ring.transform.rotation = Quaternion.Euler(68f, 15f, 12f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            GameObject moon = CreatePrimitive(PrimitiveType.Sphere, "Rainbow Moon", new Vector3(76f, 62f, 92f), Vector3.one * 10f, m.Rainbow[4], space.transform);
            Object.DestroyImmediate(moon.GetComponent<Collider>());

            for (int band = 0; band < 5; band++)
            {
                GameObject nebula = CreatePrimitive(PrimitiveType.Capsule, $"Nebula Cloud {band + 1}", new Vector3(-55f + band * 29f, 70f + Mathf.Sin(band) * 9f, 105f + band * 4f), new Vector3(12f, 3.2f, 7f), m.Rainbow[(band + 4) % 7], space.transform);
                nebula.transform.rotation = Quaternion.Euler(0f, 0f, 72f + band * 9f);
                Object.DestroyImmediate(nebula.GetComponent<Collider>());
            }

            for (int i = 0; i < 9; i++)
            {
                Vector3 start = new Vector3(-95f + i * 18f, 85f + (i % 3) * 12f, 88f + i * 3f);
                GameObject shootingStar = new GameObject($"Shooting Star {i + 1:00}");
                shootingStar.transform.SetParent(space.transform);
                shootingStar.transform.position = start;
                shootingStar.transform.rotation = Quaternion.Euler(38f, -22f, -52f);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Star Head", shootingStar.transform, Vector3.zero, Vector3.one * 0.65f, i % 2 == 0 ? m.StarWhite : m.StarBlue);
                CreateLocalPrimitive(PrimitiveType.Capsule, "Star Trail", shootingStar.transform, new Vector3(-2.4f, 1.4f, 0f), new Vector3(0.18f, 2.8f, 0.18f), i % 2 == 0 ? m.StarBlue : m.StarPink);
                foreach (Collider c in shootingStar.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                SpaceBackdropActor motion = shootingStar.AddComponent<SpaceBackdropActor>();
                motion.Configure(SpaceBackdropActor.MotionType.ShootingStar, new Vector3(22f, -10f, -2f), start, 8f + i * 0.6f, i * 0.81f);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 start = new Vector3(95f, 58f + i * 14f, 105f + i * 5f);
                GameObject comet = new GameObject($"Rainbow Comet {i + 1:00}");
                comet.transform.SetParent(space.transform);
                comet.transform.position = start;
                GameObject head = CreateLocalPrimitive(PrimitiveType.Sphere, "Comet Head", comet.transform, Vector3.zero, Vector3.one * (1.6f + i * 0.2f), m.StarWhite);
                for (int tail = 0; tail < 6; tail++)
                {
                    CreateLocalPrimitive(PrimitiveType.Sphere, $"Comet Tail {tail + 1}", comet.transform, new Vector3(2.2f + tail * 1.5f, tail * 0.3f, 0f), Vector3.one * (1.1f - tail * 0.12f), m.Rainbow[(tail + i) % 7]);
                }
                foreach (Collider c in comet.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                SpaceBackdropActor motion = comet.AddComponent<SpaceBackdropActor>();
                motion.Configure(SpaceBackdropActor.MotionType.Comet, new Vector3(-13f, -2.5f, 0f), start, 16f + i * 2f, i * 2.3f, head.GetComponent<Renderer>());
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3 dronePosition = new Vector3(-72f + i * 28f, 42f + (i % 2) * 16f, 92f + i * 4f);
                GameObject drone = new GameObject($"Background Laser Robot {i + 1:00}");
                drone.transform.SetParent(space.transform);
                drone.transform.position = dronePosition;
                CreateLocalPrimitive(PrimitiveType.Cube, "Drone Body", drone.transform, Vector3.zero, new Vector3(4.5f, 2.8f, 2f), m.EnemyMetal);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Drone Red Eye", drone.transform, new Vector3(0f, 0.3f, -1.05f), new Vector3(1.1f, 0.55f, 0.25f), m.EnemyEye);
                CreateLocalPrimitive(PrimitiveType.Cube, "Left Wing", drone.transform, new Vector3(-3.3f, 0f, 0f), new Vector3(2.6f, 0.35f, 1.4f), m.BossMetal);
                CreateLocalPrimitive(PrimitiveType.Cube, "Right Wing", drone.transform, new Vector3(3.3f, 0f, 0f), new Vector3(2.6f, 0.35f, 1.4f), m.BossMetal);
                GameObject laser = CreateLocalPrimitive(PrimitiveType.Cube, "Background Laser Beam", drone.transform, new Vector3(0f, -8f, 0f), new Vector3(0.28f, 14f, 0.28f), i % 2 == 0 ? m.EnemyEye : m.StarBlue);
                foreach (Collider c in drone.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                SpaceBackdropActor droneMotion = drone.AddComponent<SpaceBackdropActor>();
                droneMotion.Configure(SpaceBackdropActor.MotionType.RobotDrone, Vector3.zero, dronePosition, 10f, i * 1.17f);
                SpaceBackdropActor laserMotion = laser.AddComponent<SpaceBackdropActor>();
                laserMotion.Configure(SpaceBackdropActor.MotionType.Laser, Vector3.zero, Vector3.zero, 2f, i * 0.19f, laser.GetComponent<Renderer>());
            }
        }

        private static void CreateWorldDetail(Materials m, Transform parent)
        {
            GameObject details = new GameObject("World Art Detail");
            details.transform.SetParent(parent);

            for (int i = 0; i < 48; i++)
            {
                float angle = i * 2.39996f;
                float radius = 13f + (i % 9) * 4.4f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.18f, 13f + Mathf.Sin(angle) * radius);
                if (Mathf.Abs(position.x) < 7f && position.z > -15f && position.z < 35f) position.x += position.x < 0f ? -8f : 8f;
                Vector3 scale = new Vector3(0.45f + (i % 3) * 0.18f, 0.35f + (i % 4) * 0.12f, 0.5f + (i % 5) * 0.1f);
                GameObject rock = CreatePrimitive(PrimitiveType.Sphere, $"Weathered Rock {i + 1:00}", position, scale, i % 3 == 0 ? m.CastleStone : m.Soil, details.transform);
                rock.transform.rotation = Quaternion.Euler(i * 17f, i * 31f, i * 11f);
                Object.DestroyImmediate(rock.GetComponent<Collider>());
            }

            for (int i = 0; i < 70; i++)
            {
                float angle = i * 1.73f;
                float radius = 10f + (i % 13) * 3.3f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.28f, 14f + Mathf.Sin(angle) * radius);
                if (Mathf.Abs(position.x) < 5f && position.z > -15f && position.z < 34f) position.x += position.x < 0f ? -6f : 6f;
                GameObject grass = CreatePrimitive(PrimitiveType.Capsule, $"Tall Rainbow Grass {i + 1:00}", position, new Vector3(0.12f, 0.38f + (i % 4) * 0.08f, 0.12f), i % 6 == 0 ? m.Rainbow[3] : m.HillGrass, details.transform);
                grass.transform.rotation = Quaternion.Euler((i % 5) * 3f, i * 47f, (i % 7 - 3) * 4f);
                Object.DestroyImmediate(grass.GetComponent<Collider>());
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector3 bannerPosition = new Vector3(side * 18f, 6.5f, 37f + i * 4f);
                    CreatePrimitive(PrimitiveType.Cylinder, $"Castle Banner Pole {side}-{i}", bannerPosition, new Vector3(0.12f, 2.8f, 0.12f), m.ScissorSteel, details.transform);
                    GameObject banner = CreatePrimitive(PrimitiveType.Cube, $"Royal Rainbow Banner {side}-{i}", bannerPosition + new Vector3(side * -0.5f, 0.4f, 0f), new Vector3(0.9f, 2.2f, 0.12f), m.Rainbow[(i + (side > 0 ? 2 : 0)) % m.Rainbow.Length], details.transform);
                    Object.DestroyImmediate(banner.GetComponent<Collider>());
                }
            }
        }

        private static void CreatePath(Materials m, Transform parent)
        {
            for (int i = 0; i < 18; i++)
            {
                float z = -13f + i * 1.8f;
                float x = Mathf.Sin(i * 0.55f) * 2.1f;
                GameObject pathPiece = CreatePrimitive(PrimitiveType.Cylinder, $"Rainbow Path {i + 1:00}", new Vector3(x, 0.08f, z), new Vector3(2.4f, 0.1f, 2.4f), m.Rainbow[i % m.Rainbow.Length], parent);
                Object.DestroyImmediate(pathPiece.GetComponent<Collider>());
            }

            for (int i = 0; i < 15; i++)
            {
                float z = 19.5f + i * 1.05f;
                GameObject approachPiece = CreatePrimitive(PrimitiveType.Cylinder, $"Castle Approach {i + 1:00}", new Vector3(0f, 0.1f, z), new Vector3(3.2f, 0.12f, 3.2f), m.Rainbow[(i + 3) % m.Rainbow.Length], parent);
                Object.DestroyImmediate(approachPiece.GetComponent<Collider>());
            }
        }

        private static void CreateRainbowArch(Vector3 origin, Material[] rainbow, Transform parent)
        {
            GameObject arch = new GameObject("Grand Rainbow Arch");
            arch.transform.SetParent(parent);
            for (int color = 0; color < rainbow.Length; color++)
            {
                float radius = 7.2f - color * 0.48f;
                for (int segment = 0; segment <= 16; segment++)
                {
                    float angle = Mathf.PI * segment / 16f;
                    Vector3 position = origin + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                    GameObject archPiece = CreatePrimitive(PrimitiveType.Sphere, $"Rainbow {color + 1}-{segment + 1}", position, Vector3.one * 0.62f, rainbow[color], arch.transform);
                    Object.DestroyImmediate(archPiece.GetComponent<Collider>());
                }
            }
        }

        private static void CreateTrees(Materials m, Transform parent)
        {
            Vector3[] positions = {
                new Vector3(-13f, 0f, -8f), new Vector3(13f, 0f, -6f), new Vector3(-16f, 0f, 3f),
                new Vector3(16f, 0f, 6f), new Vector3(-13f, 0f, 17f), new Vector3(14f, 0f, 19f),
                new Vector3(-8f, 0f, 27f), new Vector3(9f, 0f, 29f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject tree = new GameObject($"Candy Tree {i + 1}");
                tree.transform.SetParent(parent);
                CreatePrimitive(PrimitiveType.Cylinder, "Trunk", positions[i] + Vector3.up * 1.35f, new Vector3(0.65f, 1.35f, 0.65f), m.Soil, tree.transform);
                CreatePrimitive(PrimitiveType.Sphere, "Crown", positions[i] + Vector3.up * 3.25f, new Vector3(2.5f, 2.1f, 2.5f), m.Rainbow[(i + 2) % m.Rainbow.Length], tree.transform);
                CreatePrimitive(PrimitiveType.Sphere, "Crown Puff", positions[i] + new Vector3(1.1f, 3.1f, 0f), new Vector3(1.7f, 1.6f, 1.7f), m.Rainbow[(i + 3) % m.Rainbow.Length], tree.transform);
            }
        }

        private static void CreateClouds(Materials m, Transform parent)
        {
            Vector3[] positions = { new Vector3(-15f, 13f, 25f), new Vector3(12f, 16f, 34f), new Vector3(-20f, 11f, -2f) };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject cloud = new GameObject($"Cloud {i + 1}");
                cloud.transform.SetParent(parent);
                for (int puff = 0; puff < 5; puff++)
                {
                    Vector3 offset = new Vector3((puff - 2) * 1.3f, puff % 2 * 0.65f, 0f);
                    GameObject part = CreatePrimitive(PrimitiveType.Sphere, "Puff", positions[i] + offset, new Vector3(2f, 1.3f, 1.5f), m.White, cloud.transform);
                    Object.DestroyImmediate(part.GetComponent<Collider>());
                }
            }
        }

        private static void CreatePlatforms(Materials m, Transform parent)
        {
            CreatePrimitive(PrimitiveType.Cube, "Jump Platform Pink", new Vector3(-8f, 1.2f, 5f), new Vector3(4f, 0.5f, 4f), m.Rainbow[5], parent);
            CreatePrimitive(PrimitiveType.Cube, "Jump Platform Blue", new Vector3(-11f, 2.8f, 9f), new Vector3(4f, 0.5f, 4f), m.Rainbow[4], parent);
            CreatePrimitive(PrimitiveType.Cube, "Jump Platform Yellow", new Vector3(-8f, 4.5f, 13f), new Vector3(5f, 0.5f, 4f), m.Rainbow[2], parent);
            CreatePrimitive(PrimitiveType.Cube, "Lookout", new Vector3(10f, 3f, 12f), new Vector3(7f, 0.7f, 7f), m.White, parent);
            CreatePrimitive(PrimitiveType.Cylinder, "Lookout Column", new Vector3(10f, 1.25f, 12f), new Vector3(2f, 1.25f, 2f), m.Soil, parent);
        }

        private static void CreateCrystals(Materials m, Transform parent)
        {
            Vector3[] positions = {
                new Vector3(0f, 1.2f, -8f), new Vector3(2f, 1.2f, -3f), new Vector3(-2f, 1.2f, 1f),
                new Vector3(4f, 1.2f, 7f), new Vector3(-8f, 2.5f, 5f), new Vector3(-11f, 4.1f, 9f),
                new Vector3(-8f, 5.8f, 13f), new Vector3(10f, 4.4f, 12f), new Vector3(0f, 1.2f, 25f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject crystal = CreatePrimitive(PrimitiveType.Sphere, $"Crystal {i + 1:00}", positions[i], new Vector3(0.55f, 0.9f, 0.55f), m.Crystal, parent);
                crystal.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                crystal.GetComponent<Collider>().isTrigger = true;
                crystal.AddComponent<CrystalCollectible>();
            }
        }

        private static void CreateSignsAndBounceFlowers(Materials m, Transform parent)
        {
            CreateSign("Welcome Sign", new Vector3(-3.5f, 0.9f, -11f), "Welcome, Splat! Follow the rainbow path and collect the crystals.", m, parent);
            CreateSign("Arch Sign", new Vector3(5f, 0.9f, 18f), "The Grand Rainbow protects this kingdom from General Snip's shadows.", m, parent);
            CreateBounceFlower("Bounce Flower One", new Vector3(6.5f, 0.35f, 4f), m, parent);
            CreateBounceFlower("Bounce Flower Two", new Vector3(-4f, 0.35f, 22f), m, parent);
        }

        private static void CreateLandscaping(Materials m, Transform parent)
        {
            Vector3[] hills = {
                new Vector3(-34f, 2f, -5f), new Vector3(35f, 3f, 2f), new Vector3(-38f, 4f, 28f),
                new Vector3(39f, 3f, 34f), new Vector3(-31f, 5f, 57f), new Vector3(30f, 4f, 67f)
            };
            for (int i = 0; i < hills.Length; i++)
            {
                CreatePrimitive(PrimitiveType.Sphere, $"Soft Hill {i + 1}", hills[i], new Vector3(14f, 7f + i % 3, 13f), m.HillGrass, parent);
                GameObject hillCap = CreatePrimitive(PrimitiveType.Sphere, "Hill Color Patch", hills[i] + new Vector3(0f, 3.5f, 0f), new Vector3(8f, 2.2f, 7f), m.Rainbow[(i + 3) % m.Rainbow.Length], parent);
                Object.DestroyImmediate(hillCap.GetComponent<Collider>());
            }

            GameObject sapphirePond = CreatePrimitive(PrimitiveType.Cylinder, "Sapphire Pond", new Vector3(-23f, 0.08f, 15f), new Vector3(13f, 0.08f, 10f), m.WaterBlue, parent);
            Object.DestroyImmediate(sapphirePond.GetComponent<Collider>());
            GameObject castlePond = CreatePrimitive(PrimitiveType.Cylinder, "Castle Reflection Pond", new Vector3(24f, 0.08f, 25f), new Vector3(10f, 0.08f, 8f), m.WaterBlue, parent);
            Object.DestroyImmediate(castlePond.GetComponent<Collider>());

            for (int i = 0; i < 30; i++)
            {
                float angle = i * 2.39996f;
                float radius = 12f + (i % 6) * 4.2f;
                Vector3 position = new Vector3(Mathf.Sin(angle) * radius, 0.32f, 12f + Mathf.Cos(angle) * radius);
                GameObject flower = CreatePrimitive(PrimitiveType.Sphere, $"Rainbow Flower {i + 1:00}", position, new Vector3(0.45f, 0.24f, 0.45f), m.Rainbow[i % m.Rainbow.Length], parent);
                Object.DestroyImmediate(flower.GetComponent<Collider>());
            }

            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 22f + (i / 2) * 3.6f;
                GameObject lamp = new GameObject($"Rainbow Lamp {i + 1}");
                lamp.transform.SetParent(parent);
                CreatePrimitive(PrimitiveType.Cylinder, "Lamp Post", new Vector3(side * 5.2f, 1.4f, z), new Vector3(0.22f, 1.4f, 0.22f), m.ShipSilver, lamp.transform);
                GameObject glow = CreatePrimitive(PrimitiveType.Sphere, "Lamp Glow", new Vector3(side * 5.2f, 3f, z), Vector3.one * 0.65f, m.CageEnergy, lamp.transform);
                Object.DestroyImmediate(glow.GetComponent<Collider>());
                Light light = glow.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = m.Rainbow[i % m.Rainbow.Length].GetColor("_BaseColor");
                light.range = 6f;
                light.intensity = 1.4f;
                light.shadows = LightShadows.None;
            }
        }

        private static void CreateCombatCourse(Materials m, Transform parent)
        {
            GameObject course = new GameObject("Level 1 Combat Course");
            course.transform.SetParent(parent);

            CreateWaterGun(new Vector3(5f, 1f, -4f), m, course.transform);

            Vector3[] groundEnemies = {
                new Vector3(-4f, 0.9f, -7f), new Vector3(4f, 0.9f, -6f),
                new Vector3(-5f, 0.9f, -1f), new Vector3(4f, 0.9f, 1f), new Vector3(7f, 0.9f, 3f),
                new Vector3(-6f, 0.9f, 7f), new Vector3(5f, 0.9f, 8f), new Vector3(9f, 0.9f, 10f),
                new Vector3(-5f, 0.9f, 15f), new Vector3(4f, 0.9f, 16f), new Vector3(8f, 0.9f, 18f),
                new Vector3(-7f, 0.9f, 22f), new Vector3(7f, 0.9f, 22f)
            };
            for (int i = 0; i < groundEnemies.Length; i++)
            {
                CreateEnemy($"Scrap Minion {i + 1:00}", groundEnemies[i], false, i % 4 == 3 ? 3 : 2, m, course.transform);
            }

            Vector3[] flyingEnemies = {
                new Vector3(-2f, 3.4f, -2f), new Vector3(7f, 4f, 6f), new Vector3(-8f, 4.3f, 11f),
                new Vector3(3f, 4.5f, 14f), new Vector3(-4f, 4f, 20f), new Vector3(6f, 4.5f, 25f)
            };
            for (int i = 0; i < flyingEnemies.Length; i++)
            {
                CreateEnemy($"Doom Orb {i + 1:00}", flyingEnemies[i], true, 2, m, course.transform);
            }

            Vector3[] scissorEnemies = {
                new Vector3(-9f, 1.1f, -4f), new Vector3(9f, 1.1f, -1f),
                new Vector3(-8f, 1.1f, 5f), new Vector3(8f, 1.1f, 12f),
                new Vector3(-11f, 1.1f, 17f), new Vector3(11f, 1.1f, 20f),
                new Vector3(-12f, 1.1f, 28f), new Vector3(12f, 1.1f, 29f),
                new Vector3(-16f, 3.5f, 39f), new Vector3(16f, 3.5f, 42f)
            };
            for (int i = 0; i < scissorEnemies.Length; i++)
            {
                CreateScissorEnemy($"Scissor Minion {i + 1:00}", scissorEnemies[i], i >= 8, m, course.transform);
            }

            Vector3[] pencilEnemies = {
                new Vector3(-2f, 1.15f, 2f), new Vector3(6f, 1.15f, 5f),
                new Vector3(-7f, 1.15f, 12f), new Vector3(2f, 1.15f, 20f),
                new Vector3(-10f, 1.15f, 26f), new Vector3(10f, 1.15f, 31f),
                new Vector3(-8f, 4.25f, 45f), new Vector3(8f, 4.25f, 46f)
            };
            for (int i = 0; i < pencilEnemies.Length; i++)
            {
                CreatePencilEnemy($"Pencil Punk {i + 1:00}", pencilEnemies[i], m, course.transform);
            }

            CreatePhaseWall("Phase Wall One", new Vector3(0f, 1.8f, 4.5f), new Vector3(11f, 3.6f, 0.6f), m, course.transform);
            CreatePhaseWall("Phase Wall Two", new Vector3(0f, 2.1f, 18.5f), new Vector3(13f, 4.2f, 0.6f), m, course.transform);
            CreatePrimitive(PrimitiveType.Cube, "Arena Rail Left", new Vector3(-10f, 1f, 11f), new Vector3(0.7f, 2f, 13f), m.Rainbow[4], course.transform);
            CreatePrimitive(PrimitiveType.Cube, "Arena Rail Right", new Vector3(10f, 1f, 11f), new Vector3(0.7f, 2f, 13f), m.Rainbow[5], course.transform);
            CreateRainbowCastle(m, course.transform);
        }

        private static void CreateRainbowCastle(Materials m, Transform parent)
        {
            GameObject castle = new GameObject("Rainbow Castle Boss Arena");
            castle.transform.SetParent(parent);

            CreatePrimitive(PrimitiveType.Cube, "Great Castle Floor", new Vector3(0f, 0.35f, 49f), new Vector3(46f, 0.7f, 34f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Great Back Wall", new Vector3(0f, 8f, 65.5f), new Vector3(46f, 16f, 1.2f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Left Castle Wall", new Vector3(-22.5f, 6f, 49f), new Vector3(1.2f, 12f, 34f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Right Castle Wall", new Vector3(22.5f, 6f, 49f), new Vector3(1.2f, 12f, 34f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Left Front Wing", new Vector3(-14f, 5f, 32.5f), new Vector3(17f, 10f, 1.2f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Right Front Wing", new Vector3(14f, 5f, 32.5f), new Vector3(17f, 10f, 1.2f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Central Keep", new Vector3(0f, 10f, 59f), new Vector3(22f, 20f, 12f), m.CastleStone, castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Keep Roof", new Vector3(0f, 20.6f, 59f), new Vector3(25f, 1.2f, 15f), m.Rainbow[5], castle.transform);

            for (int towerSide = -1; towerSide <= 1; towerSide += 2)
            {
                float x = towerSide * 20f;
                CreatePrimitive(PrimitiveType.Cylinder, towerSide < 0 ? "Left Rainbow Tower" : "Right Rainbow Tower", new Vector3(x, 8f, 62f), new Vector3(8f, 8f, 8f), m.Rainbow[towerSide < 0 ? 4 : 6], castle.transform);
                CreatePrimitive(PrimitiveType.Cylinder, "Tower Roof", new Vector3(x, 16.5f, 62f), new Vector3(9f, 0.7f, 9f), m.Rainbow[2], castle.transform);
            }

            for (int color = 0; color < 7; color++)
            {
                CreatePrimitive(PrimitiveType.Cube, $"Castle Rainbow Banner {color + 1}", new Vector3(-7.5f + color * 2.5f, 12f, 52.7f), new Vector3(1.8f, 4f, 0.25f), m.Rainbow[color], castle.transform);
            }

            CreatePrimitive(PrimitiveType.Cube, "Left Grand Stair", new Vector3(-7f, 1.2f, 38f), new Vector3(7f, 2.4f, 8f), m.Rainbow[4], castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Right Grand Stair", new Vector3(7f, 1.2f, 38f), new Vector3(7f, 2.4f, 8f), m.Rainbow[6], castle.transform);
            CreatePrimitive(PrimitiveType.Cube, "Upper Great Hall", new Vector3(0f, 2.75f, 50f), new Vector3(24f, 1f, 20f), m.White, castle.transform);

            EnemyHealth boss = CreateRobotBoss(new Vector3(0f, 4.6f, 49f), m, castle.transform);
            RainbowKingHostage king = CreateRainbowKing(new Vector3(-13f, 4.8f, 56f), m, castle.transform, boss);
            CreateGoal(new Vector3(8f, 4.3f, 59f), m, castle.transform);
            CreateSpaceship(new Vector3(32f, 2f, 45f), king, m, parent);

            CreateSign("Castle Warning", new Vector3(-7f, 1.25f, 30f), "The Rainbow Warden has captured the King inside the Great Hall! Clear the kingdom, storm the castle, and free him!", m, castle.transform);
        }

        private static EnemyHealth CreateRobotBoss(Vector3 position, Materials m, Transform parent)
        {
            GameObject root = new GameObject("BOSS - The Rainbow Warden");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.layer = enemyLayer;

            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 3.5f;
            character.radius = 1.25f;
            character.center = Vector3.zero;
            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(14);
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            brain.Configure(false, 1.65f, 15f);

            GameObject visual = new GameObject("Giant Robot Visual");
            visual.transform.SetParent(root.transform, false);
            CreateLocalPrimitive(PrimitiveType.Cube, "Armored Body", visual.transform, new Vector3(0f, 0f, 0f), new Vector3(2.5f, 2.2f, 1.65f), m.BossMetal);
            CreateLocalPrimitive(PrimitiveType.Cube, "Robot Head", visual.transform, new Vector3(0f, 1.55f, 0.05f), new Vector3(1.8f, 1.15f, 1.35f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Left Boss Eye", visual.transform, new Vector3(-0.42f, 1.68f, 0.75f), new Vector3(0.34f, 0.22f, 0.14f), m.EnemyEye);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Right Boss Eye", visual.transform, new Vector3(0.42f, 1.68f, 0.75f), new Vector3(0.34f, 0.22f, 0.14f), m.EnemyEye);
            CreateLocalPrimitive(PrimitiveType.Cube, "Left Crusher Arm", visual.transform, new Vector3(-1.75f, 0.05f, 0f), new Vector3(0.8f, 2.4f, 0.9f), m.BossMetal);
            CreateLocalPrimitive(PrimitiveType.Cube, "Right Crusher Arm", visual.transform, new Vector3(1.75f, 0.05f, 0f), new Vector3(0.8f, 2.4f, 0.9f), m.BossMetal);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Left Tank Tread", visual.transform, new Vector3(-0.75f, -1.35f, 0f), new Vector3(0.72f, 0.65f, 0.72f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Right Tank Tread", visual.transform, new Vector3(0.75f, -1.35f, 0f), new Vector3(0.72f, 0.65f, 0.72f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Cube, "Rainbow Power Core", visual.transform, new Vector3(0f, 0f, 0.88f), new Vector3(0.7f, 0.8f, 0.16f), m.Rainbow[5]);

            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = enemyLayer;
            }
            return health;
        }

        private static void CreateSpaceship(Vector3 position, RainbowKingHostage king, Materials m, Transform parent)
        {
            GameObject ship = new GameObject("Rainbow Rider Spaceship");
            ship.transform.SetParent(parent);
            ship.transform.position = position;
            ship.transform.rotation = Quaternion.Euler(0f, -55f, 0f);
            BoxCollider interactionZone = ship.AddComponent<BoxCollider>();
            interactionZone.isTrigger = true;
            interactionZone.size = new Vector3(7f, 4f, 9f);
            RainbowSpaceship travel = ship.AddComponent<RainbowSpaceship>();
            travel.Configure(king);

            CreateLocalPrimitive(PrimitiveType.Sphere, "Ship Hull", ship.transform, Vector3.zero, new Vector3(5f, 1.8f, 6.5f), m.ShipSilver);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Glass Cockpit", ship.transform, new Vector3(0f, 1.25f, 0.6f), new Vector3(2.8f, 1.5f, 3.2f), m.WaterBlue);
            CreateLocalPrimitive(PrimitiveType.Cube, "Left Rainbow Wing", ship.transform, new Vector3(-4.2f, -0.2f, 0f), new Vector3(4.5f, 0.35f, 3.2f), m.Rainbow[5]);
            CreateLocalPrimitive(PrimitiveType.Cube, "Right Rainbow Wing", ship.transform, new Vector3(4.2f, -0.2f, 0f), new Vector3(4.5f, 0.35f, 3.2f), m.Rainbow[6]);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Left Rainbow Engine", ship.transform, new Vector3(-2.3f, -0.2f, -3.5f), new Vector3(1f, 1.5f, 1f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Right Rainbow Engine", ship.transform, new Vector3(2.3f, -0.2f, -3.5f), new Vector3(1f, 1.5f, 1f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Ship Star Emblem", ship.transform, new Vector3(0f, 0f, 3.35f), new Vector3(1f, 1f, 0.2f), m.Rainbow[2]);

            foreach (Collider shipCollider in ship.GetComponentsInChildren<Collider>())
            {
                if (shipCollider != interactionZone)
                {
                    Object.DestroyImmediate(shipCollider);
                }
            }

            Vector3[] routeDirections = {
                new Vector3(1f, 0f, 0.2f).normalized,
                new Vector3(0.7f, 0f, 0.72f).normalized,
                new Vector3(0.15f, 0f, 1f).normalized,
                new Vector3(-0.55f, 0f, 0.84f).normalized
            };
            for (int route = 0; route < routeDirections.Length; route++)
            {
                GameObject rainbowRoute = new GameObject($"Rainbow Route to World {route + 2}");
                rainbowRoute.transform.SetParent(parent);
                for (int segment = 0; segment < 18; segment++)
                {
                    float t = segment / 17f;
                    Vector3 routePosition = position
                        + routeDirections[route] * (8f + segment * 2.7f)
                        + Vector3.up * (2f + Mathf.Sin(t * Mathf.PI) * (7f + route));
                    GameObject routePiece = CreatePrimitive(
                        PrimitiveType.Sphere,
                        $"Route {route + 2} Segment {segment + 1:00}",
                        routePosition,
                        Vector3.one * 1.35f,
                        m.Rainbow[(segment + route) % m.Rainbow.Length],
                        rainbowRoute.transform);
                    Object.DestroyImmediate(routePiece.GetComponent<Collider>());
                }
            }
        }

        private static RainbowKingHostage CreateRainbowKing(Vector3 position, Materials m, Transform parent, EnemyHealth boss)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Hostage Dais", position + new Vector3(0f, -1.35f, 0f), new Vector3(2.8f, 0.45f, 2.8f), m.BossMetal, parent);
            GameObject king = new GameObject("Rainbow King Hostage");
            king.transform.SetParent(parent);
            king.transform.position = position;
            CreateLocalPrimitive(PrimitiveType.Sphere, "King Body", king.transform, Vector3.zero, new Vector3(1.65f, 1.9f, 1.4f), m.KingBlue);
            CreateLocalPrimitive(PrimitiveType.Sphere, "King Face", king.transform, new Vector3(0f, 0.35f, 0.68f), new Vector3(1.02f, 1.02f, 0.48f), m.White);
            CreateLocalPrimitive(PrimitiveType.Sphere, "King Left Eye", king.transform, new Vector3(-0.28f, 0.5f, 1.08f), new Vector3(0.14f, 0.18f, 0.09f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "King Right Eye", king.transform, new Vector3(0.28f, 0.5f, 1.08f), new Vector3(0.14f, 0.18f, 0.09f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Golden Crown", king.transform, new Vector3(0f, 1.62f, 0f), new Vector3(1.05f, 0.4f, 1.05f), m.Rainbow[2]);

            GameObject beacon = new GameObject("Hostage Spotlight");
            beacon.transform.SetParent(parent);
            beacon.transform.position = position + new Vector3(0f, 5f, 0f);
            Light hostageLight = beacon.AddComponent<Light>();
            hostageLight.type = LightType.Point;
            hostageLight.color = Color.cyan;
            hostageLight.range = 14f;
            hostageLight.intensity = 3.5f;
            hostageLight.shadows = LightShadows.None;

            foreach (Collider kingCollider in king.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(kingCollider);
            }

            GameObject cage = new GameObject("Rainbow King Energy Cage");
            cage.transform.SetParent(parent);
            cage.transform.position = position;
            for (int bar = 0; bar < 8; bar++)
            {
                float angle = bar * Mathf.PI * 0.25f;
                Vector3 barPosition = position + new Vector3(Mathf.Sin(angle) * 2.15f, 0.35f, Mathf.Cos(angle) * 2.15f);
                GameObject energyBar = CreatePrimitive(PrimitiveType.Cube, $"Energy Bar {bar + 1}", barPosition, new Vector3(0.24f, 4.5f, 0.24f), m.CageEnergy, cage.transform);
                energyBar.layer = phaseWallLayer;
            }
            GameObject cageFloor = CreatePrimitive(PrimitiveType.Cylinder, "Energy Cage Floor", position + new Vector3(0f, -1.65f, 0f), new Vector3(2.4f, 0.15f, 2.4f), m.CageEnergy, cage.transform);
            GameObject cageRoof = CreatePrimitive(PrimitiveType.Cylinder, "Energy Cage Roof", position + new Vector3(0f, 2.65f, 0f), new Vector3(2.4f, 0.15f, 2.4f), m.CageEnergy, cage.transform);
            cageFloor.layer = phaseWallLayer;
            cageRoof.layer = phaseWallLayer;

            if (boss != null)
            {
                Vector3 bossPosition = boss.transform.position + Vector3.up * 1.5f;
                Vector3 kingPosition = position + Vector3.up * 1.65f;
                Vector3 link = kingPosition - bossPosition;
                GameObject powerTether = CreatePrimitive(PrimitiveType.Cube, "Boss Hostage Power Tether", (bossPosition + kingPosition) * 0.5f, new Vector3(0.18f, 0.18f, link.magnitude), m.EnemyEye, cage.transform);
                powerTether.transform.rotation = Quaternion.LookRotation(link.normalized);
                powerTether.layer = phaseWallLayer;
            }

            RainbowKingHostage hostage = king.AddComponent<RainbowKingHostage>();
            hostage.Configure(boss, cage);
            return hostage;
        }

        private static void CreateEnemy(string name, Vector3 position, bool flying, int health, Materials m, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.layer = enemyLayer;

            CharacterController character = root.AddComponent<CharacterController>();
            character.height = flying ? 1.2f : 1.5f;
            character.radius = flying ? 0.62f : 0.52f;
            character.center = Vector3.zero;

            EnemyHealth enemyHealth = root.AddComponent<EnemyHealth>();
            enemyHealth.Configure(health);
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            brain.Configure(flying, flying ? 2.7f : 2.1f, flying ? 11f : 8f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Transform leftMover = null;
            Transform rightMover = null;
            if (flying)
            {
                CreateLocalPrimitive(PrimitiveType.Sphere, "Orb Body", visual.transform, Vector3.zero, new Vector3(1.3f, 1.05f, 1.05f), m.EnemyDark);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Left Red Eye", visual.transform, new Vector3(-0.28f, 0.1f, 0.5f), new Vector3(0.23f, 0.18f, 0.12f), m.EnemyEye);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Right Red Eye", visual.transform, new Vector3(0.28f, 0.1f, 0.5f), new Vector3(0.23f, 0.18f, 0.12f), m.EnemyEye);
            }
            else
            {
                CreateLocalPrimitive(PrimitiveType.Cube, "Robot Body", visual.transform, Vector3.zero, new Vector3(1.05f, 1.1f, 0.85f), m.EnemyMetal);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Red Eye", visual.transform, new Vector3(0f, 0.15f, 0.46f), new Vector3(0.36f, 0.2f, 0.1f), m.EnemyEye);
                leftMover = CreateLocalPrimitive(PrimitiveType.Cube, "Left Tread", visual.transform, new Vector3(-0.38f, -0.67f, 0f), new Vector3(0.35f, 0.35f, 0.8f), m.EnemyDark).transform;
                rightMover = CreateLocalPrimitive(PrimitiveType.Cube, "Right Tread", visual.transform, new Vector3(0.38f, -0.67f, 0f), new Vector3(0.35f, 0.35f, 0.8f), m.EnemyDark).transform;
            }

            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = enemyLayer;
            }
            EnemyVisualMotion motion = root.AddComponent<EnemyVisualMotion>();
            motion.Configure(character, visual.transform, leftMover, rightMover, flying, false);
        }

        private static void CreateScissorEnemy(string name, Vector3 position, bool flying, Materials m, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.layer = enemyLayer;

            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.72f;
            character.center = Vector3.zero;
            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(3);
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            brain.Configure(flying, flying ? 3.1f : 2.45f, flying ? 13f : 10f);

            GameObject visual = new GameObject("Scissor Visual");
            visual.transform.SetParent(root.transform, false);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Scissor Hinge", visual.transform, Vector3.zero, new Vector3(0.62f, 0.62f, 0.48f), m.EnemyDark);
            GameObject upperBlade = CreateLocalPrimitive(PrimitiveType.Cube, "Upper Blade", visual.transform, new Vector3(0.72f, 0.38f, 0f), new Vector3(1.65f, 0.25f, 0.38f), m.ScissorSteel);
            upperBlade.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            GameObject lowerBlade = CreateLocalPrimitive(PrimitiveType.Cube, "Lower Blade", visual.transform, new Vector3(0.72f, -0.38f, 0f), new Vector3(1.65f, 0.25f, 0.38f), m.ScissorSteel);
            lowerBlade.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Upper Handle", visual.transform, new Vector3(-0.72f, 0.5f, 0f), new Vector3(0.72f, 0.62f, 0.45f), m.ScissorHandle);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Lower Handle", visual.transform, new Vector3(-0.72f, -0.5f, 0f), new Vector3(0.72f, 0.62f, 0.45f), m.ScissorHandle);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Scissor Red Eye", visual.transform, new Vector3(0f, 0.12f, 0.48f), new Vector3(0.25f, 0.18f, 0.11f), m.EnemyEye);

            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = enemyLayer;
            }
            EnemyVisualMotion motion = root.AddComponent<EnemyVisualMotion>();
            motion.Configure(character, visual.transform, upperBlade.transform, lowerBlade.transform, flying, true);
        }

        private static void CreatePencilEnemy(string name, Vector3 position, Materials m, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.layer = enemyLayer;

            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 2.3f;
            character.radius = 0.55f;
            character.center = new Vector3(0f, 0.15f, 0f);
            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(3);
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            brain.Configure(false, 2.65f, 10f);

            GameObject visual = new GameObject("Pencil Enemy Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject body = CreateLocalPrimitive(PrimitiveType.Cylinder, "Yellow Pencil Body", visual.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.62f, 1.35f, 0.62f), m.Rainbow[2]);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Pink Eraser", visual.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.67f, 0.2f, 0.67f), m.Rainbow[6]);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Metal Eraser Band", visual.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.7f, 0.12f, 0.7f), m.ScissorSteel);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Wooden Pencil Tip", visual.transform, new Vector3(0f, -1.3f, 0f), new Vector3(0.55f, 0.48f, 0.55f), m.Soil);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Graphite Point", visual.transform, new Vector3(0f, -1.65f, 0f), new Vector3(0.24f, 0.25f, 0.24f), m.EnemyDark);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Left Angry Eye", visual.transform, new Vector3(-0.22f, 0.5f, 0.58f), new Vector3(0.13f, 0.14f, 0.08f), m.EnemyEye);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Right Angry Eye", visual.transform, new Vector3(0.22f, 0.5f, 0.58f), new Vector3(0.13f, 0.14f, 0.08f), m.EnemyEye);
            Transform leftFoot = CreateLocalPrimitive(PrimitiveType.Sphere, "Left Pencil Foot", visual.transform, new Vector3(-0.38f, -1.55f, 0f), new Vector3(0.42f, 0.22f, 0.58f), m.EnemyDark).transform;
            Transform rightFoot = CreateLocalPrimitive(PrimitiveType.Sphere, "Right Pencil Foot", visual.transform, new Vector3(0.38f, -1.55f, 0f), new Vector3(0.42f, 0.22f, 0.58f), m.EnemyDark).transform;

            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = enemyLayer;
            }
            EnemyVisualMotion motion = root.AddComponent<EnemyVisualMotion>();
            motion.Configure(character, visual.transform, leftFoot, rightFoot, false, false);
        }

        private static void CreateWaterGun(Vector3 position, Materials m, Transform parent)
        {
            GameObject pickup = new GameObject("Water Gun Pickup");
            pickup.transform.SetParent(parent);
            pickup.transform.position = position;
            BoxCollider trigger = pickup.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.8f, 1.8f, 1.8f);
            pickup.AddComponent<WaterGunPickup>();
            CreateLocalPrimitive(PrimitiveType.Cube, "Blue Blaster", pickup.transform, Vector3.zero, new Vector3(1.3f, 0.45f, 0.45f), m.WaterBlue);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Water Tank", pickup.transform, new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.45f, 0.45f, 0.45f), m.White);
            CreateLocalPrimitive(PrimitiveType.Cube, "Orange Handle", pickup.transform, new Vector3(-0.15f, -0.45f, 0f), new Vector3(0.32f, 0.7f, 0.32f), m.Rainbow[1]);
            foreach (Collider childCollider in pickup.GetComponentsInChildren<Collider>())
            {
                if (childCollider != trigger)
                {
                    Object.DestroyImmediate(childCollider);
                }
            }
        }

        private static void CreatePhaseWall(string name, Vector3 position, Vector3 scale, Materials m, Transform parent)
        {
            GameObject wall = CreatePrimitive(PrimitiveType.Cube, name, position, scale, m.PhaseWall, parent);
            wall.layer = phaseWallLayer;
        }

        private static void CreateGoal(Vector3 position, Materials m, Transform parent)
        {
            GameObject goal = new GameObject("Rainbow Star Gate");
            goal.transform.SetParent(parent);
            goal.transform.position = position;
            BoxCollider trigger = goal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 3f, 1.5f);
            goal.AddComponent<LevelGoal>();
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Left Pillar", goal.transform, new Vector3(-1.6f, 0f, 0f), new Vector3(0.6f, 1.8f, 0.6f), m.Rainbow[2]);
            CreateLocalPrimitive(PrimitiveType.Cylinder, "Right Pillar", goal.transform, new Vector3(1.6f, 0f, 0f), new Vector3(0.6f, 1.8f, 0.6f), m.Rainbow[2]);
            CreateLocalPrimitive(PrimitiveType.Sphere, "Goal Star", goal.transform, new Vector3(0f, 1.6f, 0f), new Vector3(1.25f, 1.25f, 0.5f), m.Rainbow[2]);
            foreach (Collider childCollider in goal.GetComponentsInChildren<Collider>())
            {
                if (childCollider != trigger)
                {
                    Object.DestroyImmediate(childCollider);
                }
            }
        }

        private static void CreateSign(string name, Vector3 position, string message, Materials m, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            CreateLocalPrimitive(PrimitiveType.Cube, "Post", root.transform, new Vector3(0f, -0.25f, 0f), new Vector3(0.25f, 1.5f, 0.25f), m.Soil);
            GameObject board = CreateLocalPrimitive(PrimitiveType.Cube, "Board", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(2.4f, 1.1f, 0.25f), m.Rainbow[2]);
            SignPost sign = board.AddComponent<SignPost>();
            sign.SetMessage(message);
        }

        private static void CreateBounceFlower(string name, Vector3 position, Materials m, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            GameObject center = CreateLocalPrimitive(PrimitiveType.Sphere, "Flower Center", root.transform, Vector3.zero, new Vector3(1.1f, 0.35f, 1.1f), m.Rainbow[2]);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 petalPosition = new Vector3(Mathf.Sin(angle) * 0.85f, 0f, Mathf.Cos(angle) * 0.85f);
                CreateLocalPrimitive(PrimitiveType.Sphere, "Petal", root.transform, petalPosition, new Vector3(0.8f, 0.22f, 0.55f), m.Rainbow[(i + 4) % m.Rainbow.Length]);
            }
            center.AddComponent<BounceFlower>();
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent);
            result.transform.position = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static GameObject CreateLocalPrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            result.transform.localScale = localScale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null && material.shader != urpLit)
            {
                material.shader = urpLit;
            }
            material.SetColor("_BaseColor", color);
            bool metal = name.Contains("Metal") || name.Contains("Armor") || name.Contains("Silver") || name.Contains("Steel");
            bool glossy = name.Contains("Water") || name.Contains("Crystal") || name.Contains("Energy") || name.Contains("Eye");
            material.SetFloat("_Metallic", metal ? 0.78f : 0.02f);
            material.SetFloat("_Smoothness", glossy ? 0.78f : metal ? 0.58f : name.Contains("Stone") || name.Contains("Soil") ? 0.12f : 0.3f);
            material.SetFloat("_OcclusionStrength", 1f);
            if (name.Contains("Crystal") || name.Contains("Energy") || name.Contains("Enemy Red Eye"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSpaceMaterial(string name, Color color, float emission)
        {
            Material material = CreateMaterial(name, color);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureRendering()
        {
            const string rendererPath = "Assets/Settings/RainbowRenderer.asset";
            const string pipelinePath = "Assets/Settings/RainbowURP.asset";
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "Rainbow Renderer";
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "Rainbow URP";
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 65f;
            pipeline.mainLightShadowmapResolution = 2048;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/RainbowPostProcessing.asset");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Rainbow Post Processing";
                AssetDatabase.CreateAsset(profile, "Assets/Settings/RainbowPostProcessing.asset");
            }

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.Override(0.38f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.62f);
            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.saturation.Override(10f);
            color.contrast.Override(7f);
            color.postExposure.Override(0.08f);
            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.13f);
            vignette.smoothness.Override(0.48f);
            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);

            GameObject volumeObject = new GameObject("Rainbow Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                return component;
            }
            return profile.Add<T>(true);
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
            {
                return existing;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int index = 6; index < 32; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return index;
                }
            }

            throw new System.InvalidOperationException($"No free Unity layer is available for {layerName}.");
        }

        private sealed class Materials
        {
            public readonly Material Grass = CreateMaterial("Rainbow Grass", new Color(0.28f, 0.78f, 0.34f));
            public readonly Material Soil = CreateMaterial("Warm Soil", new Color(0.42f, 0.22f, 0.15f));
            public readonly Material White = CreateMaterial("Cloud White", new Color(0.96f, 0.98f, 1f));
            public readonly Material SplatPurple = CreateMaterial("Splat Purple", new Color(0.48f, 0.16f, 0.86f));
            public readonly Material SplatDark = CreateMaterial("Splat Dark", new Color(0.09f, 0.035f, 0.16f));
            public readonly Material Crystal = CreateMaterial("Crystal Glow", new Color(0.05f, 0.95f, 1f));
            public readonly Material EnemyDark = CreateMaterial("Doom Orb Dark", new Color(0.06f, 0.07f, 0.13f));
            public readonly Material EnemyMetal = CreateMaterial("Minion Metal", new Color(0.32f, 0.38f, 0.46f));
            public readonly Material EnemyEye = CreateMaterial("Enemy Red Eye", new Color(1f, 0.03f, 0.08f));
            public readonly Material WaterBlue = CreateMaterial("Water Gun Blue", new Color(0.03f, 0.52f, 1f));
            public readonly Material PhaseWall = CreateMaterial("Phase Wall", new Color(0.45f, 0.12f, 0.78f));
            public readonly Material CastleStone = CreateMaterial("Rainbow Castle Stone", new Color(0.78f, 0.82f, 0.92f));
            public readonly Material BossMetal = CreateMaterial("Boss Armor", new Color(0.22f, 0.26f, 0.34f));
            public readonly Material KingBlue = CreateMaterial("Rainbow King Blue", new Color(0.18f, 0.48f, 0.95f));
            public readonly Material CageEnergy = CreateMaterial("Energy Cage", new Color(0.15f, 0.95f, 1f));
            public readonly Material ShipSilver = CreateMaterial("Rainbow Rider Silver", new Color(0.7f, 0.76f, 0.88f));
            public readonly Material HillGrass = CreateMaterial("Soft Hill Grass", new Color(0.18f, 0.62f, 0.27f));
            public readonly Material ScissorSteel = CreateMaterial("Scissor Steel", new Color(0.72f, 0.78f, 0.88f));
            public readonly Material ScissorHandle = CreateMaterial("Scissor Handle", new Color(0.78f, 0.08f, 0.18f));
            public readonly Material SpaceSky = CreateSpaceMaterial("Deep Space", new Color(0.004f, 0.006f, 0.035f), 0.15f);
            public readonly Material StarWhite = CreateSpaceMaterial("Starlight White", new Color(0.9f, 0.96f, 1f), 3.5f);
            public readonly Material StarBlue = CreateSpaceMaterial("Starlight Blue", new Color(0.18f, 0.65f, 1f), 4f);
            public readonly Material StarPink = CreateSpaceMaterial("Starlight Pink", new Color(1f, 0.2f, 0.72f), 3.5f);
            public readonly Material[] Rainbow = {
                CreateMaterial("Rainbow Red", new Color(1f, 0.2f, 0.24f)),
                CreateMaterial("Rainbow Orange", new Color(1f, 0.48f, 0.12f)),
                CreateMaterial("Rainbow Yellow", new Color(1f, 0.88f, 0.1f)),
                CreateMaterial("Rainbow Green", new Color(0.2f, 0.82f, 0.35f)),
                CreateMaterial("Rainbow Blue", new Color(0.1f, 0.52f, 1f)),
                CreateMaterial("Rainbow Indigo", new Color(0.34f, 0.22f, 0.85f)),
                CreateMaterial("Rainbow Pink", new Color(1f, 0.28f, 0.72f))
            };
        }
    }
}
