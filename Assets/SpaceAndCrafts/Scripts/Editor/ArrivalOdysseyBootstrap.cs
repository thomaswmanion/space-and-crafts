#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Patches RainbowWorld_Arrival with Odyssey-leaning systems:
    /// quests, craft variants, combat director, secret registry, open spurs.
    /// </summary>
    public static class ArrivalOdysseyBootstrap
    {
        private const string ArrivalScene = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";

        public static void TuneSecretNookBadgeReadabilityIter2152()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var nook = GameObject.Find("TEMP_ART_Zone_SecretNook");
            if (nook == null) throw new System.Exception("Secret Nook is missing");

            SetMaterialColor(nook.transform.Find("ART_NookWallBack"), new Color(0.18f, 0.27f, 0.43f));
            SetMaterialColor(nook.transform.Find("ART_NookWallL"), new Color(0.15f, 0.23f, 0.38f));
            SetMaterialColor(nook.transform.Find("ART_NookWallR"), new Color(0.15f, 0.23f, 0.38f));
            SetMaterialColor(nook.transform.Find("ART_NookPedestal"), new Color(0.2f, 0.23f, 0.39f));
            SetMaterialColor(nook.transform.Find("ART_Pickup_discovery_badge_secret_nook/TEMP_ART_BadgeRing"), new Color(0.86f, 0.49f, 0.14f));
            SetMaterialColor(nook.transform.Find("ART_Pickup_discovery_badge_secret_nook/TEMP_ART_BadgeStar"), new Color(0.9f, 0.34f, 0.06f));
            SetLight(nook.transform.Find("TEMP_ART_NookGlow"), 0.4f, 3.2f, new Color(0.28f, 0.78f, 1f));

            var lighting = nook.transform.Find("ART_ZoneLighting");
            SetLight(lighting != null ? lighting.Find("ART_ZonePoint") : null, 0.65f, 5f, new Color(0.3f, 0.64f, 1f));
            SetLight(lighting != null ? lighting.Find("ART_ZoneSpot") : null, 0.45f, 6f, new Color(0.62f, 0.52f, 1f));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log("SECRET_NOOK_BADGE_READABILITY_TUNED lights=0.4/0.65/0.45 palette=slate-amber");
            EditorApplication.Exit(0);
        }

        public static void WidenWorkshopPowerCrystalIter2157()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var crystal = GameObject.Find("TEMP_ART_Zone_JunkArchWorkshop/ART_Pickup_component_power_crystal");
            if (crystal == null) throw new System.Exception("Workshop power crystal is missing");
            crystal.transform.localScale = new Vector3(1.05f, 0.85f, 1.05f);
            SetMaterialColor(crystal.transform, new Color(0.32f, 0.78f, 0.98f));
            SetMaterialColor(crystal.transform.Find("TEMP_ART_CrystalShardA"), new Color(0.35f, 0.84f, 1f));
            SetMaterialColor(crystal.transform.Find("TEMP_ART_CrystalShardB"), new Color(0.25f, 0.63f, 0.88f));
            SetMaterialColor(crystal.transform.Find("TEMP_ART_CrystalGlow"), new Color(0.42f, 0.96f, 1f));
            var bounds = crystal.GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log($"WORKSHOP_POWER_CRYSTAL_TUNED localScale={crystal.transform.localScale:F2} bounds={bounds.size:F2} palette=cyan");
            EditorApplication.Exit(0);
        }

        public static void RefineResourcePickupSilhouettesIter2158()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var spring = GameObject.Find("TEMP_ART_Zone_PaintfallGlade/ART_Pickup_component_spring");
            var dish = GameObject.Find("TEMP_ART_Zone_JunkArchWorkshop/ART_Pickup_component_metal_dish");
            if (spring == null || dish == null)
                throw new System.Exception($"Resource pickups missing: spring={(spring != null)}, dish={(dish != null)}");

            spring.transform.localScale = new Vector3(1.15f, 1.3f, 1.15f);
            var genericSpringCore = spring.GetComponent<MeshRenderer>();
            if (genericSpringCore == null) throw new System.Exception("Spring generic core renderer is missing");
            genericSpringCore.enabled = false;
            var springCoil = spring.transform.Find("TEMP_ART_SpringCoil");
            var springCoilFilter = springCoil != null ? springCoil.GetComponent<MeshFilter>() : null;
            if (springCoilFilter == null) throw new System.Exception("Spring coil mesh is missing");
            springCoilFilter.sharedMesh = OrganicMeshFactory.SpringHelix(0.86f, 0.43f, 0.07f, 4, 28);
            var springCore = spring.transform.Find("TEMP_ART_SpringCore");
            var springCoreRenderer = springCore != null ? springCore.GetComponent<MeshRenderer>() : null;
            if (springCoreRenderer != null) springCoreRenderer.enabled = false;

            dish.transform.localScale = new Vector3(1.4f, 0.85f, 1.4f);
            SetPickupMesh(dish.transform, OrganicMeshFactory.DishBowl(0.55f, 0.28f, 12, 36));
            SetPickupMesh(dish.transform.Find("TEMP_ART_DishBowl"), OrganicMeshFactory.DishBowl(0.58f, 0.32f, 12, 40));
            SetPickupMesh(dish.transform.Find("TEMP_ART_DishRim"), OrganicMeshFactory.Torus(0.58f, 0.06f, 40, 12));
            SetPickupMesh(dish.transform.Find("TEMP_ART_DishHandle"), OrganicMeshFactory.HookCurve(0.42f, 0.24f, 0.05f, 20));

            var springBounds = GetRendererBounds(spring.transform);
            var dishBounds = GetRendererBounds(dish.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival resource pickup scene changes");
            Debug.Log($"RESOURCE_PICKUP_SILHOUETTES_TUNED springScale={spring.transform.localScale:F2} springBounds={springBounds.size:F2} dishScale={dish.transform.localScale:F2} dishBounds={dishBounds.size:F2}; spring is coil-only, dish bowl depth broadened.");
            EditorApplication.Exit(0);
        }

        private static void SetPickupMesh(Transform target, Mesh mesh)
        {
            var filter = target != null ? target.GetComponent<MeshFilter>() : null;
            if (filter == null) throw new System.Exception($"Pickup mesh target is missing: {target}");
            filter.sharedMesh = mesh;
            EditorUtility.SetDirty(filter);
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void SetMaterialColor(Transform target, Color color)
        {
            var renderer = target != null ? target.GetComponent<Renderer>() : null;
            var material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null) throw new System.Exception($"Missing material renderer: {target}");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
        }

        private static void SetLight(Transform target, float intensity, float range, Color color)
        {
            var light = target != null ? target.GetComponent<Light>() : null;
            if (light == null) throw new System.Exception($"Missing nook light: {target}");
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            EditorUtility.SetDirty(light);
        }

        public static void RepositionEastSpurRollerIter2151()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var roller = GameObject.Find("Zone_EastSpur/ART_ScrapRoller");
            if (roller == null) throw new System.Exception("East Spur Scrap Roller is missing");

            // Keep the optional encounter active, but move it off the entry line
            // so the player can read the Wonder and platform before engaging.
            roller.transform.localPosition = new Vector3(-3.2f, 0.6f, -1.3f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log($"EAST_SPUR_ROLLER_REPOSITIONED local={roller.transform.localPosition:F2} world={roller.transform.position:F2}");
            EditorApplication.Exit(0);
        }

        public static void RecomposeEastSpurOverlookIter2151()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var pillar = GameObject.Find("ART_EastOverlook/ART_OverlookPillar");
            if (pillar == null || pillar.transform.parent == null)
                throw new System.Exception("East Spur overlook pillar is missing");

            pillar.transform.parent.localPosition = new Vector3(0f, 0.76f, 3.8f);
            pillar.transform.localScale = new Vector3(0.6f, 1.65f, 0.6f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            var bounds = pillar.GetComponent<Renderer>().bounds;
            Debug.Log($"EAST_SPUR_OVERLOOK_RECOMPOSED local={pillar.transform.localPosition:F2} scale={pillar.transform.localScale:F2} bounds={bounds.size:F2}");
            EditorApplication.Exit(0);
        }

        [MenuItem("Space & Crafts/Bootstrap Arrival Odyssey Systems")]
        public static void Bootstrap()
        {
            if (!System.IO.File.Exists(ArrivalScene))
            {
                Debug.LogError($"[Space & Crafts] Missing Arrival scene: {ArrivalScene}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Arrival Odyssey systems bootstrapped (quests, craft variants, combat, secrets, spurs).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Patch currently-open Arrival scene (also called from graybox bootstrap).</summary>
        public static void ApplyToOpenScene()
        {
            var systems = GameObject.Find("ArrivalSystems");
            if (systems == null)
            {
                systems = new GameObject("ArrivalSystems");
            }

            var quests = systems.GetComponent<WorldQuestDirector>() ?? systems.AddComponent<WorldQuestDirector>();
            quests.Configure(BuildArrivalQuests());
            var board = systems.GetComponent<QuestBoardHud>() ?? systems.AddComponent<QuestBoardHud>();
            board.Bind(quests);
            _ = systems.GetComponent<EnemyCombatDirector>() ?? systems.AddComponent<EnemyCombatDirector>();
            _ = systems.GetComponent<SecretWonderRegistry>() ?? systems.AddComponent<SecretWonderRegistry>();
            _ = systems.GetComponent<CombatStartFlash>() ?? systems.AddComponent<CombatStartFlash>();
            ProgressSaveBridge.Ensure();
            WorldMusicDirector.Ensure(1);

            ConfigureCraftVariants();
            EnsureOpenSprawl();
            EnsureOpenSprawlMaterials();
            EnsureNamedSecrets();
            EnsureAmbientBackdrop();
            EnsureEasterEgg();
            EnsureSpitterPack();
        }

        private static void EnsureSpitterPack()
        {
            if (GameObject.Find("ART_EncounterPack_Spitters") != null)
            {
                return;
            }

            var pack = new GameObject("ART_EncounterPack_Spitters");
            var world = GameObject.Find("TEMP_ART_ArrivalWorld");
            if (world != null)
            {
                pack.transform.SetParent(world.transform, false);
            }

            pack.transform.position = new Vector3(-6f, 1.5f, 20f);
            for (var i = 0; i < 2; i++)
            {
                var go = OrganicMeshFactory.CreateMeshObject(
                    $"ART_ScrapSpitter_{i}",
                    OrganicMeshFactory.SoftBlob(3, 0.55f, 0.9f, 0.04f),
                    pack.transform,
                    new Vector3(i * 2.4f, 0f, 0f),
                    Vector3.one,
                    null);
                ApplyMat(go, "Mat_ScrapSpitter", new Color(0.85f, 0.35f, 0.55f), 0.5f, 0.2f);
                var mouth = OrganicMeshFactory.CreateMeshObject(
                    "ART_SpitterMouth",
                    OrganicMeshFactory.SoftBlob(2, 0.18f, 1f, 0.02f),
                    go.transform,
                    // Seat the fused mouth bead on the front surface. At 0.35 m
                    // it was buried in the 0.55 m body and produced a bright
                    // intersection ring during the shot wind-up.
                    new Vector3(0f, 0.15f, 0.65f),
                    Vector3.one,
                    null);
                ApplyMat(mouth, "Mat_ScrapSpitterMouth", new Color(1f, 0.85f, 0.3f), 0.6f, 0.1f);
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.6f;
                var ai = go.AddComponent<ScrapSpitter>();
                ai.Configure(go.transform.position);
            }
        }

        private static void EnsureEasterEgg()
        {
            if (GameObject.Find("ART_RainbowEasterEgg") != null)
            {
                return;
            }

            var shelf = GameObject.Find("Zone_ArrivalShelf") ?? GameObject.Find("Zone_Shelf");
            var parent = shelf != null
                ? shelf.transform
                : GameObject.Find("TEMP_ART_ArrivalWorld")?.transform;
            var go = OrganicMeshFactory.CreateMeshObject(
                "ART_RainbowEasterEgg",
                OrganicMeshFactory.FacetedStar(0.28f, 0.11f, 0.07f, 6),
                parent,
                new Vector3(3.5f, 1.1f, -1.5f),
                Vector3.one * 0.7f,
                null);
            ApplyMat(go, "Mat_RainbowEasterEgg", new Color(1f, 0.85f, 0.25f), 0.55f, 0.1f);
            go.AddComponent<RainbowEasterEgg>();
        }

        private static List<QuestDefinition> BuildArrivalQuests()
        {
            return new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    id = "arrival_main",
                    title = "Rainbow Kingdom: The Arrival",
                    isSideQuest = false,
                    steps = new List<QuestStep>
                    {
                        new QuestStep
                        {
                            kind = QuestStepKind.CollectItem,
                            label = "Find the spring",
                            targetId = "component_spring",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.CollectItem,
                            label = "Find the metal dish",
                            targetId = "component_metal_dish",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.CollectItem,
                            label = "Find the power crystal",
                            targetId = "component_power_crystal",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.BuildInvention,
                            label = "Assemble a Boing-O-Matic",
                            targetId = "boing_o_matic",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.UseInvention,
                            label = "Use your Boing-O-Matic to launch",
                            targetId = "boing_o_matic",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.ReachCheckpoint,
                            label = "Reach the high Rainbow path",
                            targetId = "Checkpoint_GulchFar",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.DefeatBoss,
                            label = "Boing Rattle's exposed relay",
                            requiredCount = 1
                        }
                    }
                },
                new QuestDefinition
                {
                    id = "arrival_wonder_seeker",
                    title = "Wonder Seeker",
                    isSideQuest = true,
                    steps = new List<QuestStep>
                    {
                        new QuestStep
                        {
                            kind = QuestStepKind.FindSecrets,
                            label = "Find two hidden rainbow wonders",
                            requiredCount = 2
                        }
                    }
                },
                new QuestDefinition
                {
                    id = "arrival_turbo_crafter",
                    title = "Turbo Crafter",
                    isSideQuest = true,
                    steps = new List<QuestStep>
                    {
                        new QuestStep
                        {
                            kind = QuestStepKind.CollectItem,
                            label = "Collect the Secret Nook badge too",
                            targetId = "discovery_badge_secret_nook",
                            requiredCount = 1
                        },
                        new QuestStep
                        {
                            kind = QuestStepKind.BuildInvention,
                            label = "Build Turbo Boing (badge recipe)",
                            targetId = "boing_o_matic_turbo",
                            requiredCount = 1
                        }
                    }
                }
            };
        }

        private static void ConfigureCraftVariants()
        {
            var site = Object.FindAnyObjectByType<BuildSite>();
            if (site == null)
            {
                return;
            }

            site.ConfigureRecipes(new List<BuildRecipe>
            {
                new BuildRecipe
                {
                    displayName = "Standard Boing",
                    inventionId = "boing_o_matic",
                    requiredCollectibleIds = new List<string>
                    {
                        "component_spring",
                        "component_metal_dish",
                        "component_power_crystal"
                    },
                    launchSpeedOverride = 26f,
                    launchToast = "BOING!",
                    accentTint = Color.white
                },
                new BuildRecipe
                {
                    displayName = "Turbo Boing",
                    inventionId = "boing_o_matic_turbo",
                    requiredCollectibleIds = new List<string>
                    {
                        "component_spring",
                        "component_metal_dish",
                        "component_power_crystal",
                        "discovery_badge_secret_nook"
                    },
                    launchSpeedOverride = 34f,
                    launchToast = "TURBO BOING!",
                    accentTint = new Color(1f, 0.55f, 0.2f, 1f)
                }
            });
        }

        private static void EnsureOpenSprawl()
        {
            var world = GameObject.Find("TEMP_ART_ArrivalWorld") ?? GameObject.Find("RainbowWorld_Arrival");
            var parent = world != null ? world.transform : null;

            // West spur — floater pack off the critical path.
            if (GameObject.Find("Zone_WestSpur") == null)
            {
                var spur = new GameObject("Zone_WestSpur");
                spur.transform.SetParent(parent, false);
                spur.transform.position = new Vector3(-14f, 0f, 12f);
                CreateGround(spur.transform, new Vector3(8f, 0.4f, 8f), new Color(0.45f, 0.7f, 0.55f));
                PlaceFloater(spur.transform, new Vector3(-1.5f, 1.2f, 1f));
                PlaceFloater(spur.transform, new Vector3(1.8f, 1.4f, -0.5f));
                PlaceBeacon(spur.transform, new Vector3(0f, 1.2f, 2.5f), "wonder_west_spur", "West spur wonder found — a quiet rainbow pocket!");
            }

            // East spur — roller + scenic overlook.
            if (GameObject.Find("Zone_EastSpur") == null)
            {
                var spur = new GameObject("Zone_EastSpur");
                spur.transform.SetParent(parent, false);
                spur.transform.position = new Vector3(16f, 0f, 18f);
                CreateGround(spur.transform, new Vector3(9f, 0.45f, 9f), new Color(0.55f, 0.5f, 0.75f));
                PlaceRoller(spur.transform, new Vector3(-3.2f, 0.6f, -1.3f));
                PlaceBeacon(spur.transform, new Vector3(2.5f, 1.3f, 2f), "wonder_east_spur", "East spur wonder found — curiosity paid off!");
                var vista = new GameObject("ART_EastOverlook");
                vista.transform.SetParent(spur.transform, false);
                vista.transform.localPosition = new Vector3(0f, 0.76f, 3.8f);
                OrganicMeshFactory.CreateMeshObject(
                    "ART_OverlookPillar",
                    OrganicMeshFactory.SoftBlob(3, 0.8f, 0.4f, 0.05f),
                    vista.transform,
                    Vector3.zero,
                    new Vector3(0.6f, 1.65f, 0.6f),
                    null);
                var overlook = vista.transform.Find("ART_OverlookPillar");
                if (overlook != null)
                {
                    ApplyMat(overlook.gameObject, "Mat_Arrival_OverlookPillar",
                        new Color(0.46f, 0.53f, 0.72f), 0.28f, 0.08f);
                }
            }
        }

        private static void EnsureOpenSprawlMaterials()
        {
            var wonderIds = new[] { "wonder_west_spur", "wonder_east_spur" };
            for (var i = 0; i < wonderIds.Length; i++)
            {
                var wonder = GameObject.Find($"ART_Wonder_{wonderIds[i]}");
                var renderer = wonder != null ? wonder.GetComponent<Renderer>() : null;
                if (renderer != null && renderer.sharedMaterial == null)
                {
                    ApplyMat(wonder, "Mat_Arrival_SecretWonder",
                        new Color(0.94f, 0.72f, 0.38f), 0.34f, 0.06f);
                }
            }

            var pillar = GameObject.Find("ART_OverlookPillar");
            var pillarRenderer = pillar != null ? pillar.GetComponent<Renderer>() : null;
            if (pillarRenderer != null && pillarRenderer.sharedMaterial == null)
            {
                ApplyMat(pillar, "Mat_Arrival_OverlookPillar",
                    new Color(0.46f, 0.53f, 0.72f), 0.28f, 0.08f);
            }
        }

        private static void EnsureNamedSecrets()
        {
            // Tag existing Secret Nook pedestal if present.
            var nook = GameObject.Find("Zone_SecretNook");
            if (nook == null)
            {
                return;
            }

            var existing = nook.GetComponentInChildren<SecretWonderBeacon>();
            if (existing == null)
            {
                PlaceBeacon(nook.transform, new Vector3(-1.5f, 1.2f, -1.2f), "wonder_secret_nook", "Secret Nook wonder found — your map missed this one!");
            }
            else
            {
                existing.Configure("wonder_secret_nook", "Secret Nook wonder found — your map missed this one!");
            }
        }

        private static void EnsureAmbientBackdrop()
        {
            if (GameObject.Find("ART_AmbientHorizonPulse") != null)
            {
                return;
            }

            var world = GameObject.Find("TEMP_ART_ArrivalWorld") ?? GameObject.Find("RainbowWorld_Arrival");
            var pulse = new GameObject("ART_AmbientHorizonPulse");
            if (world != null)
            {
                pulse.transform.SetParent(world.transform, false);
            }

            pulse.transform.position = new Vector3(0f, 18f, 40f);
            var mesh = OrganicMeshFactory.CreateMeshObject(
                "ART_HorizonRibbon",
                OrganicMeshFactory.SoftBlob(2, 2.5f, 0.2f, 0.02f),
                pulse.transform,
                Vector3.zero,
                new Vector3(48f, 2.5f, 6f),
                null);
            ApplyMat(mesh, "Mat_AmbientHorizonRibbon", new Color(0.55f, 0.35f, 0.85f, 0.45f), 0.35f, 0.05f);
            pulse.AddComponent<AmbientHorizonBob>();
        }

        private static void CreateGround(Transform parent, Vector3 scale, Color color)
        {
            scale.y = Mathf.Max(scale.y, 1.25f);
            var ground = OrganicMeshFactory.CreateMeshObject(
                "ART_SpurGround",
                OrganicMeshFactory.SoftBlob(2, 1.2f, 0.15f, 0.02f),
                parent,
                Vector3.zero,
                scale,
                null);
            var filter = ground.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                ground.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else
                {
                    mat.color = color;
                }

                renderer.sharedMaterial = mat;
            }
        }

        private static void PlaceFloater(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("ART_ScrapFloater");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            OrganicScrapFloaterBuilder.Build(go.transform, ApplyMat);
            var floater = go.GetComponent<ScrapFloater>() ?? go.AddComponent<ScrapFloater>();
            floater.Configure(go.transform.position, 1.6f, 1.1f);
        }

        private static void PlaceRoller(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("ART_ScrapRoller");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            OrganicScrapRollerBuilder.Build(go.transform, ApplyMat);
            var roller = go.GetComponent<ScrapRoller>() ?? go.AddComponent<ScrapRoller>();
            roller.Configure(go.transform.position);
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            const string matFolder = "Assets/SpaceAndCrafts/Materials/TempArt";
            if (!AssetDatabase.IsValidFolder(matFolder))
            {
                AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Materials", "TempArt");
            }

            var path = $"{matFolder}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = assetName
                };
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

        private static void PlaceBeacon(Transform parent, Vector3 localPos, string id, string toast)
        {
            var go = OrganicMeshFactory.CreateMeshObject(
                $"ART_Wonder_{id}",
                OrganicMeshFactory.FacetedStar(0.35f, 0.14f, 0.08f, 5),
                parent,
                localPos,
                Vector3.one * 0.85f,
                null);
            var beacon = go.AddComponent<SecretWonderBeacon>();
            beacon.Configure(id, toast);
            ApplyMat(go, "Mat_Arrival_SecretWonder",
                new Color(0.94f, 0.72f, 0.38f), 0.34f, 0.06f);
        }
    }
}
#endif
