#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Builds PlayerPlayground with charming TEMP ART (not final). Cosmic + handmade vibe.
    /// </summary>
    public static class PlayerPlaygroundBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Development/PlayerPlayground.unity";
        private const string DataPath = "Assets/SpaceAndCrafts/ScriptableObjects";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";
        private const string InputActionsPath = "Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions";

        [MenuItem("Space & Crafts/Bootstrap PlayerPlayground (TEMP ART)")]
        public static void Bootstrap()
        {
            PlaygroundSceneBootstrap.Bootstrap();

            EnsureFolder(DataPath);
            EnsureFolder(PrefabPath);
            EnsureFolder(MatPath);

            var motorSettings = LoadOrCreateSettings<SplatMotorSettings>($"{DataPath}/SplatMotorSettings.asset");
            ApplyMotorFeelDefaults(motorSettings);
            var cameraSettings = LoadOrCreateSettings<CameraSettings>($"{DataPath}/CameraSettings.asset");
            ApplyCameraFeelDefaults(cameraSettings);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearGameplayRoots();
            ApplyAtmosphere();

            var world = new GameObject("TEMP_ART_World");
            CreateGround(world.transform);
            CreatePlaySteps(world.transform);
            CreateJumpRulers(world.transform);
            CreateScrapIslands(world.transform);
            CreateRainbowArchLandmark(world.transform);
            CreateWorkshopBits(world.transform);

            CreateKeyLight(world.transform);

            var splat = CreateSplat(motorSettings);
            var cameraRig = CreateCameraRig(splat.transform, cameraSettings);

            WireInputAndMotor(splat, cameraRig, motorSettings);
            CreateFallVolume(world.transform);

            var systems = new GameObject("PlaygroundSystems");
            systems.AddComponent<GameplayToastHud>();
            systems.AddComponent<AudioSource>();
            systems.AddComponent<TempSfxPlayer>();
            var collectHud = systems.AddComponent<CollectibleHud>();
            collectHud.BindInventory(splat.GetComponent<InventoryComponent>());

            var banner = new GameObject("TEMP_ART_BANNER_PLAYGROUND");
            banner.transform.SetParent(world.transform, false);
            banner.transform.position = new Vector3(0f, 3.5f, 6f);
            banner.AddComponent<TempArtBanner>().SetLabel("TEMP ART — PlayerPlayground");

            PrefabUtility.SaveAsPrefabAsset(splat, $"{PrefabPath}/Splat_TEMP.prefab");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] PlayerPlayground rebuilt with charming TEMP ART. Still TEMP — not final models.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void CreateFallVolume(Transform parent)
        {
            var go = new GameObject("ART_FallVolume");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, -6f, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(60f, 2f, 60f);
            go.AddComponent<FallVolume>();
        }

        private static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.92f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.7f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(1f, 0.82f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.45f, 0.4f);
            RenderSettings.subtractiveShadowColor = new Color(0.35f, 0.3f, 0.45f);
        }

        private static void CreateKeyLight(Transform parent)
        {
            var root = new GameObject("TEMP_ART_Lighting");
            root.transform.SetParent(parent, false);

            var keyGo = new GameObject("TEMP_ART_KeyLight");
            keyGo.transform.SetParent(root.transform, false);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.93f, 0.82f);
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
            keyGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            var fillGo = new GameObject("TEMP_ART_FillLight");
            fillGo.transform.SetParent(root.transform, false);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.7f, 1f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(15f, 140f, 0f);
        }

        private static void CreateGround(Transform parent)
        {
            var ground = OrganicMeshFactory.CreateMeshObject(
                "ART_PlayPad",
                OrganicMeshFactory.SoftBlob(3, 1f, 0.18f, 0.03f),
                parent,
                Vector3.zero,
                new Vector3(14f, 1f, 14f),
                null);
            ApplyMat(ground, "Mat_FoamMint", new Color(0.55f, 0.85f, 0.72f), 0.35f, 0.1f);
            ground.AddComponent<BoxCollider>().size = Vector3.one;

            var ring = OrganicMeshFactory.CreateMeshObject(
                "ART_PlayPadRing",
                OrganicMeshFactory.Torus(7.2f, 0.18f, 56, 14),
                parent,
                new Vector3(0f, 0.08f, 0f),
                Vector3.one,
                null);
            ApplyMat(ring, "Mat_TapeYellow", new Color(1f, 0.86f, 0.35f), 0.55f, 0.2f);
        }

        private static void CreatePlaySteps(Transform parent)
        {
            var root = new GameObject("ART_PlaySteps");
            root.transform.SetParent(parent, false);

            var palette = new[]
            {
                new Color(1f, 0.45f, 0.42f),
                new Color(1f, 0.72f, 0.28f),
                new Color(0.45f, 0.85f, 0.55f),
                new Color(0.35f, 0.7f, 1f),
                new Color(0.75f, 0.5f, 1f)
            };

            for (var i = 0; i < palette.Length; i++)
            {
                var block = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Step_{i}",
                    OrganicSlabMesh.Create(0.1f, 3, 0.012f),
                    root.transform,
                    new Vector3(5f + i * 2.4f, 0.45f + i * 0.4f, 1.5f),
                    new Vector3(2.1f, 0.9f + i * 0.55f, 2.1f),
                    null);
                block.transform.rotation = Quaternion.Euler(0f, i * 8f - 16f, 0f);
                ApplyMat(block, $"Mat_Step_{i}", palette[i], 0.4f, 0.15f);
                block.AddComponent<BoxCollider>().size = Vector3.one;

                var sticker = OrganicMeshFactory.CreateMeshObject(
                    $"ART_Sticker_{i}",
                    OrganicMeshFactory.SoftBlob(2, 0.35f, 0.25f, 0.02f),
                    block.transform,
                    new Vector3(0f, 0.52f, 0f),
                    new Vector3(0.55f, 0.2f, 0.55f),
                    null);
                ApplyMat(sticker, $"Mat_Sticker_{i}", Color.Lerp(palette[i], Color.white, 0.35f), 0.25f, 0.1f);
            }
        }

        private static void CreateJumpRulers(Transform parent)
        {
            var root = new GameObject("ART_JumpRulers");
            root.transform.SetParent(parent, false);

            // Short / medium / long gaps for jump + coyote feel checks (labels aid human sign-off).
            PlaceJumpPad(root.transform, new Vector3(-6f, 0.35f, -6f), 2.2f, new Color(0.95f, 0.55f, 0.35f), "Gap ~1.0");
            PlaceJumpPad(root.transform, new Vector3(-2.8f, 0.35f, -6f), 2.2f, new Color(0.95f, 0.75f, 0.3f), "Gap ~1.4");
            PlaceJumpPad(root.transform, new Vector3(1.2f, 0.55f, -6.4f), 2.4f, new Color(0.45f, 0.8f, 0.95f), "Up +0.2");
            PlaceJumpPad(root.transform, new Vector3(5.2f, 0.85f, -6.8f), 2.6f, new Color(0.7f, 0.5f, 0.95f), "Up +0.5 / JumpH 2.4");
            WorldLabel(root.transform, "TEMP_ART_LABEL_JumpFeelLane", new Vector3(-0.5f, 2.2f, -6f), "TEMP Jump feel lane — aim clean landings");
        }

        private static void PlaceJumpPad(Transform parent, Vector3 pos, float width, Color color, string label)
        {
            var pad = OrganicMeshFactory.CreateMeshObject(
                "ART_JumpPad",
                OrganicMeshFactory.SoftBlob(3, 0.55f, 0.35f, 0.04f),
                parent,
                pos,
                new Vector3(width * 0.85f, 0.7f, 1.4f),
                null);
            ApplyMat(pad, $"Mat_JumpPad_{color.GetHashCode():X}", color, 0.4f, 0.1f);
            var col = pad.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            WorldLabel(pad.transform, $"ART_LABEL_{label.Replace(' ', '_').Replace('~', 'g').Replace('/', '_')}", new Vector3(0f, 1.2f, 0f), label);
        }

        private static void WorldLabel(Transform parent, string name, Vector3 localPos, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.12f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
        }

        private static void CreateScrapIslands(Transform parent)
        {
            var root = new GameObject("TEMP_ART_ScrapIslands");
            root.transform.SetParent(parent, false);

            PlaceIsland(root.transform, new Vector3(-8f, 1.2f, 6f), new Vector3(3.2f, 0.5f, 2.4f), new Color(0.92f, 0.78f, 0.55f));
            PlaceIsland(root.transform, new Vector3(-4f, 2.4f, 10f), new Vector3(2.4f, 0.4f, 2.4f), new Color(0.85f, 0.55f, 0.75f));
            PlaceIsland(root.transform, new Vector3(8f, 1.8f, -7f), new Vector3(2.8f, 0.45f, 2.2f), new Color(0.55f, 0.8f, 0.95f));
        }

        private static void PlaceIsland(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var island = OrganicMeshFactory.CreateMeshObject(
                "ART_Island",
                OrganicMeshFactory.SoftBlob(3, 0.7f, 0.35f, 0.05f),
                parent,
                pos,
                scale,
                null);
            island.transform.rotation = Quaternion.Euler(0f, pos.x * 5f, 0f);
            ApplyMat(island, $"Mat_Island_{color.GetHashCode():X}", color, 0.45f, 0.08f);
            island.AddComponent<BoxCollider>().size = Vector3.one;
        }

        private static void CreateRainbowArchLandmark(Transform parent)
        {
            var root = new GameObject("ART_RainbowGateway_Landmark");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0f, 0f, 22f);

            var colors = new[]
            {
                new Color(1f, 0.25f, 0.3f),
                new Color(1f, 0.55f, 0.15f),
                new Color(1f, 0.9f, 0.2f),
                new Color(0.3f, 0.85f, 0.4f),
                new Color(0.25f, 0.55f, 1f),
                new Color(0.55f, 0.35f, 0.95f)
            };

            // Multi-hue organic arch (not cube segments).
            var arch = OrganicMeshFactory.CreateMeshObject(
                "ART_RainbowArch",
                OrganicMeshFactory.ArchGateway(7.5f, 6.5f, 0.55f, 36),
                root.transform,
                new Vector3(0f, 1.2f, 0f),
                Vector3.one,
                null);
            ApplyMat(arch, "Mat_RainbowArchCore", colors[3], 0.3f, 0.25f);

            for (var i = 0; i < colors.Length; i++)
            {
                var t = (i + 0.5f) / colors.Length;
                var angle = Mathf.Lerp(-70f, 70f, t) * Mathf.Deg2Rad;
                var radius = 7.5f;
                var bead = OrganicMeshFactory.CreateMeshObject(
                    $"ART_RainbowBead_{i}",
                    OrganicMeshFactory.SoftBlob(3, 0.35f, 0.85f, 0.03f),
                    root.transform,
                    new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius + 1.2f, 0f),
                    Vector3.one,
                    null);
                ApplyMat(bead, $"Mat_Rainbow_{i}", colors[i], 0.25f, 0.35f);
            }
        }

        private static void CreateWorkshopBits(Transform parent)
        {
            var root = new GameObject("ART_WorkshopBits");
            root.transform.SetParent(parent, false);

            var crate = OrganicMeshFactory.CreateMeshObject(
                "ART_CardboardCrate",
                OrganicMeshFactory.SoftBlob(3, 0.55f, 0.85f, 0.04f),
                root.transform,
                new Vector3(-3.2f, 0.55f, -2.5f),
                new Vector3(1.3f, 1.1f, 1.3f),
                null);
            crate.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            ApplyMat(crate, "Mat_Cardboard", new Color(0.78f, 0.58f, 0.32f), 0.7f, 0.05f);
            crate.AddComponent<BoxCollider>().size = Vector3.one;

            var spool = OrganicMeshFactory.CreateMeshObject(
                "ART_FoilSpool",
                OrganicMeshFactory.Torus(0.4f, 0.12f, 36, 14),
                root.transform,
                new Vector3(-4.5f, 0.45f, -1.2f),
                Vector3.one,
                null);
            spool.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            ApplyMat(spool, "Mat_Foil", new Color(0.85f, 0.88f, 0.95f), 0.05f, 0.85f);

            var puddle = OrganicMeshFactory.CreateMeshObject(
                "ART_PaintPuddle",
                OrganicMeshFactory.SoftBlob(3, 0.7f, 0.18f, 0.05f),
                root.transform,
                new Vector3(2.8f, 0.08f, -3.5f),
                new Vector3(1.6f, 0.5f, 1.1f),
                null);
            ApplyMat(puddle, "Mat_PaintPuddle", new Color(1f, 0.4f, 0.65f), 0.55f, 0.05f);
        }

        private static void ClearGameplayRoots()
        {
            foreach (var name in new[]
                     {
                         "TEMP_ART_World",
                         "TEMP_ART_Ground_Graybox",
                         "TEMP_ART_JumpRulers",
                         "TEMP_ART_KeyLight",
                         "TEMP_ART_FillLight",
                         "TEMP_ART_Lighting",
                         "PlaygroundSystems",
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

        private static GameObject CreateSplat(SplatMotorSettings motorSettings)
        {
            var splat = new GameObject("Splat_TEMP");
            splat.tag = "Player";
            splat.transform.position = new Vector3(0f, 0.9f, 0f);

            var controller = splat.AddComponent<CharacterController>();
            controller.height = 1.15f;
            controller.radius = 0.48f;
            controller.center = new Vector3(0f, 0.55f, 0f);

            var visualRootTf = OrganicSplatBuilder.BuildVisualRoot(splat.transform, ApplyMat);
            var label = new GameObject("TEMP_ART_LABEL_NOT_FINAL");
            label.transform.SetParent(visualRootTf, false);
            var banner = label.AddComponent<TempArtBanner>();
            banner.SetLabel("TEMP · Organic mesh stand-in (commission finals)");

            var input = splat.AddComponent<SplatInputReader>();
            var motor = splat.AddComponent<SplatMotor>();
            var feel = splat.AddComponent<SplatFeelPresenter>();
            var skeleton = splat.AddComponent<SplatSkeletonAnimator>();
            var voice = splat.AddComponent<SpaceAndCrafts.Audio.CharacterVoicePlayer>();
            splat.AddComponent<SplatInteractor>();
            splat.AddComponent<InventoryComponent>();
            splat.AddComponent<PlayerRespawn>();

            var feelSo = new SerializedObject(feel);
            feelSo.FindProperty("motor").objectReferenceValue = motor;
            feelSo.FindProperty("visualRoot").objectReferenceValue = visualRootTf;
            feelSo.ApplyModifiedPropertiesWithoutUndo();

            var skelSo = new SerializedObject(skeleton);
            skelSo.FindProperty("motor").objectReferenceValue = motor;
            skelSo.FindProperty("visualRoot").objectReferenceValue = visualRootTf;
            skelSo.ApplyModifiedPropertiesWithoutUndo();

            var voiceSo = new SerializedObject(voice);
            voiceSo.FindProperty("motor").objectReferenceValue = motor;
            voiceSo.ApplyModifiedPropertiesWithoutUndo();

            _ = motorSettings;
            _ = input;

            return splat;
        }

        private static void WireInputAndMotor(GameObject splat, GameObject cameraRig, SplatMotorSettings motorSettings)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            var inputReader = splat.GetComponent<SplatInputReader>();
            if (inputReader != null && inputAsset != null)
            {
                var so = new SerializedObject(inputReader);
                so.FindProperty("inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inputReader);
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
        }

        private static void ApplyMotorFeelDefaults(SplatMotorSettings settings)
        {
            // Commercial weight: snappier start/stop, slightly higher top speed, crisp jump.
            settings.MaxSpeed = 8.0f;
            settings.Acceleration = 62f;
            settings.Deceleration = 95f;
            settings.AirControlPercent = 0.78f;
            settings.TurnSpeedDegrees = 980f;
            settings.JumpHeight = 2.45f;
            settings.Gravity = -48f;
            settings.JumpCutGravityMultiplier = 3.2f;
            settings.CoyoteTime = 0.13f;
            settings.JumpBufferTime = 0.15f;
            settings.GroundedStickForce = -7f;
            EditorUtility.SetDirty(settings);
        }

        private static void ApplyCameraFeelDefaults(CameraSettings settings)
        {
            // Tighter follow + slightly wider FOV for readable platforming spectacle.
            settings.Distance = 8.2f;
            settings.Height = 2.35f;
            settings.FieldOfView = 52f;
            settings.FollowSmoothTime = 0.065f;
            settings.OrbitSensitivity = 150f;
            settings.PitchSensitivity = 105f;
            settings.GamepadLookMultiplier = 1.4f;
            EditorUtility.SetDirty(settings);
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.74f, 0.95f);
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();

            var collision = rig.AddComponent<CameraCollision>();
            var collisionSo = new SerializedObject(collision);
            collisionSo.FindProperty("settings").objectReferenceValue = settings;
            collisionSo.FindProperty("followTarget").objectReferenceValue = follow;
            collisionSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            collisionSo.ApplyModifiedPropertiesWithoutUndo();

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

                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
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
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static T LoadOrCreateSettings<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
