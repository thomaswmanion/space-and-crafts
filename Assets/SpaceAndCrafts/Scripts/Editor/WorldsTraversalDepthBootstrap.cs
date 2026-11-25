#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Adds organic traversal platforms, secret spur, and invention teach pad to Worlds 02–10.
    /// Idempotent via Find-by-name; does not recreate Arrival.
    /// </summary>
    public static class WorldsTraversalDepthBootstrap
    {
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        private static readonly (int index, string fantasy)[] FallbackJobs =
        {
            (2, "vine discs"),
            (3, "foil pads"),
            (4, "bubble discs"),
            (5, "gear plates"),
            (6, "ice shelves"),
            (7, "dish plates"),
            (8, "hull decks"),
            (9, "star pads"),
            (10, "glass panes"),
        };

        [MenuItem("Space & Crafts/Bootstrap Worlds 02-10 Traversal Depth")]
        public static void Bootstrap()
        {
            var catalog = WorldCatalogBootstrap.SeedCatalog();
            var upgraded = 0;

            for (var worldIndex = 2; worldIndex <= 10; worldIndex++)
            {
                var def = catalog.GetByIndex(worldIndex);
                string scenePath;
                string displayName;
                Color accent;
                if (def != null && !string.IsNullOrEmpty(def.scenePath))
                {
                    scenePath = def.scenePath;
                    displayName = def.displayName;
                    accent = def.accentColor;
                }
                else
                {
                    scenePath = KnownScenePath(worldIndex);
                    displayName = $"World {worldIndex:00}";
                    accent = Color.gray;
                }

                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip traversal depth — missing scene for world {worldIndex}: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                UpgradeTraversal(worldIndex, accent);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                upgraded++;
                Debug.Log($"[Space & Crafts] Traversal depth added: {displayName} ({FantasyLabel(worldIndex)})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Worlds 02–10 traversal depth complete ({upgraded} scenes).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static void CapWorld08HullDecksIter449()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var updated = 0;
            var foundPads = 0;
            var artRoot = FindArtRoot(8);
            var pads = artRoot != null ? artRoot.GetComponentsInChildren<Transform>(true) : new Transform[0];
            for (var i = 0; i < pads.Length; i++)
            {
                var pad = pads[i] != null && pads[i].name.StartsWith("ART_TraversalPad_", System.StringComparison.Ordinal)
                    ? pads[i].gameObject : null;
                if (pad == null) continue;
                foundPads++;
                var deck = pad != null ? pad.transform.Find("deck") : null;
                var filter = deck != null ? deck.GetComponent<MeshFilter>() : null;
                if (filter == null)
                {
                    Debug.LogWarning($"[Space & Crafts] World 08 traversal pad {i} has no serialized hull deck mesh.");
                    continue;
                }

                filter.sharedMesh = OrganicMeshFactory.RibbedCylinder(
                    0.22f, 0.7f, 10, 0.04f, 36, 8, capEnds: true);
                EditorUtility.SetDirty(filter);
                updated++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Added top and bottom faces to {updated} World 08 hull decks.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated == foundPads ? 0 : 2);
            }
        }

        private static void UpgradeTraversal(int worldIndex, Color accent)
        {
            var artRoot = FindArtRoot(worldIndex) ?? new GameObject($"ART_World{worldIndex:00}");
            var routeRoot = FindOrCreateChild(artRoot.transform, "ART_TraversalRoute");

            var padCount = PlatformCount(worldIndex);
            for (var i = 0; i < padCount; i++)
            {
                var padName = $"ART_TraversalPad_{i}";
                if (GameObject.Find(padName) != null || FindDeep(routeRoot, padName) != null)
                {
                    continue;
                }

                var t = padCount <= 1 ? 0.5f : i / (float)(padCount - 1);
                var z = Mathf.Lerp(0f, 14f, t);
                var x = SideOffset(worldIndex, i);
                var y = 0.55f + (i % 3) * 0.35f;
                BuildPlatform(worldIndex, routeRoot, padName, new Vector3(x, y, z), accent, i);
            }

            EnsureSecretSpur(worldIndex, routeRoot, accent);
            EnsureInventionTeachPad(worldIndex, artRoot.transform, accent);
            EnsureCommercialZones(worldIndex, artRoot.transform, accent);
            RefreshTraversalPadColliders(worldIndex, artRoot.transform);
            if (worldIndex == 2)
            {
                RefreshWorld02MainVineDiscs(artRoot.transform);
            }
        }

        /// <summary>
        /// Arrival-style zone sprawl: Approach / Mid / BossLane platforms beyond the main route.
        /// New names only — never renames existing ART_TraversalPad_*.
        /// </summary>
        private static void EnsureCommercialZones(int worldIndex, Transform artRoot, Color accent)
        {
            var zonesRoot = FindOrCreateChild(artRoot, "ART_CommercialZones");
            EnsureZone(worldIndex, zonesRoot, "ART_Zone_Approach", new Vector3(-7.5f, 0f, 4f), accent, 0, 3);
            EnsureZone(worldIndex, zonesRoot, "ART_Zone_Mid", new Vector3(8f, 0f, 9f), accent, 1, 3);
            EnsureZone(worldIndex, zonesRoot, "ART_Zone_BossLane", new Vector3(0f, 0f, 18f), accent, 2, 2);
        }

        private static void EnsureZone(
            int worldIndex,
            Transform zonesRoot,
            string zoneName,
            Vector3 zoneLocalPos,
            Color accent,
            int zoneIndex,
            int padCount)
        {
            var zone = FindOrCreateChild(zonesRoot, zoneName);
            zone.localPosition = zoneLocalPos;

            // Walkable slab floor for the zone (idempotent).
            var floorName = $"{zoneName}_Floor";
            if (FindDeep(zone, floorName) == null)
            {
                var floor = new GameObject(floorName);
                floor.transform.SetParent(zone, false);
                floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                floor.transform.localScale = new Vector3(6.5f, 0.35f, 5.5f);
                var filter = floor.AddComponent<MeshFilter>();
                filter.sharedMesh = OrganicSlabMesh.Create(0.12f, 5, 0.02f);
                floor.AddComponent<MeshRenderer>();
                ApplyMat(floor, $"Mat_ZoneFloor_W{worldIndex}_{zoneIndex}", Color.Lerp(accent, new Color(0.35f, 0.32f, 0.4f), 0.45f), 0.35f, 0.08f);
                var floorCol = floor.AddComponent<BoxCollider>();
                floorCol.size = Vector3.one;
                floorCol.center = Vector3.zero;
            }

            for (var i = 0; i < padCount; i++)
            {
                var padName = $"ART_TraversalPad_Z{zoneIndex}_{i}";
                if (FindDeep(zone, padName) != null || GameObject.Find(padName) != null)
                {
                    continue;
                }

                var x = (i - (padCount - 1) * 0.5f) * 1.9f;
                var y = 0.45f + (i % 2) * 0.4f;
                var z = -1.2f + i * 1.1f;
                BuildPlatform(worldIndex, zone, padName, new Vector3(x, y, z), accent, zoneIndex * 10 + i);
            }
        }

        private static int PlatformCount(int worldIndex)
        {
            // 4–6 pads; stagger by world so routes feel distinct.
            return 4 + ((worldIndex - 2) % 3);
        }

        private static float SideOffset(int worldIndex, int index)
        {
            var lane = (index % 3) - 1;
            var widen = 1.4f + (worldIndex % 3) * 0.25f;
            return lane * widen + (worldIndex % 2 == 0 ? 0.2f : -0.2f);
        }

        private static void BuildPlatform(int worldIndex, Transform parent, string name, Vector3 localPos, Color accent, int index)
        {
            var pad = new GameObject(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = localPos;

            switch (worldIndex)
            {
                case 2: BuildVineDisc(pad.transform, accent, index); break;
                case 3: BuildFoilPad(pad.transform, accent, index); break;
                case 4: BuildBubbleDisc(pad.transform, accent, index); break;
                case 5: BuildGearPlate(pad.transform, accent, index); break;
                case 6: BuildIceShelf(pad.transform, accent, index); break;
                case 7: BuildDishPlate(pad.transform, accent, index); break;
                case 8: BuildHullDeck(pad.transform, accent, index); break;
                case 9: BuildStarPad(pad.transform, accent, index); break;
                case 10: BuildGlassPane(pad.transform, accent, index); break;
                default: BuildVineDisc(pad.transform, accent, index); break;
            }

            ApplyWalkableCollider(worldIndex, pad);
        }

        private static void RefreshTraversalPadColliders(int worldIndex, Transform artRoot)
        {
            var children = artRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child != null && child.name.StartsWith("ART_TraversalPad_", System.StringComparison.Ordinal))
                {
                    ApplyWalkableCollider(worldIndex, child.gameObject);
                }
            }
        }

        public static void WidenWorld02CanopyPadSupportIter952()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var pads = Object.FindObjectsByType<Transform>();
            var updated = 0;
            for (var i = 0; i < pads.Length; i++)
            {
                var pad = pads[i];
                if (pad == null || !pad.name.StartsWith("ART_TraversalPad_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var collider = pad.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = pad.gameObject.AddComponent<BoxCollider>();
                }

                collider.size = new Vector3(2.65f, 0.32f, 2.65f);
                EditorUtility.SetDirty(collider);
                updated++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Iteration 952 widened {updated} World 02 traversal support decks to 2.65 m, preserving their 0.32 m thickness; rendered vine discs are about 2.75 m across.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 2);
            }
        }

        public static void WidenWorld02MainVinePadsIter953()
        {
            const string scenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var updated = RefreshWorld02MainVineDiscs(null);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Iteration 953 scaled {updated} main World 02 vine discs to a 3.43 m rendered footprint and 3.35 m walkable top; secondary pads retain their existing size.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated == 4 ? 0 : 2);
            }
        }

        private static int RefreshWorld02MainVineDiscs(Transform artRoot)
        {
            var updated = 0;
            for (var i = 0; i < 4; i++)
            {
                var pad = artRoot != null
                    ? FindDeep(artRoot, $"ART_TraversalPad_{i}")
                    : GameObject.Find($"ART_TraversalPad_{i}");
                if (pad == null)
                {
                    continue;
                }

                var disc = pad.transform.Find("disc");
                if (disc != null)
                {
                    disc.localScale = new Vector3(2f, disc.localScale.y, 2f);
                    EditorUtility.SetDirty(disc);
                }

                ApplyWalkableCollider(2, pad);
                updated++;
            }

            return updated;
        }

        private static void ApplyWalkableCollider(int worldIndex, GameObject pad)
        {
            var col = pad.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = pad.AddComponent<BoxCollider>();
            }

            const float colliderDepth = 0.32f;
            // World 02's soft vine discs render roughly 2.75 m across. Keep the
            // walkable deck close to that visible rim so Splat's capsule can run
            // over the artwork without its radius leaving a much smaller hidden top.
            // Widen only these discs; the other worlds use different pad shapes.
            var isWorld02MainPad = worldIndex == 2
                && pad.name.Length == "ART_TraversalPad_0".Length
                && pad.name.StartsWith("ART_TraversalPad_", System.StringComparison.Ordinal)
                && pad.name["ART_TraversalPad_".Length] >= '0'
                && pad.name["ART_TraversalPad_".Length] <= '3';
            var colliderWidth = isWorld02MainPad ? 3.35f : worldIndex == 2 ? 2.65f : 1.8f;
            var walkableTop = WalkableSurfaceTop(worldIndex);
            col.size = new Vector3(colliderWidth, colliderDepth, colliderWidth);
            col.center = new Vector3(0f, walkableTop - colliderDepth * 0.5f, 0f);
        }

        private static float WalkableSurfaceTop(int worldIndex)
        {
            // Keep the invisible flat walking deck aligned with the central body,
            // not the old shared +0.225 m height that left several small meshes
            // visibly below Splat's feet. Values follow the mesh transforms below.
            switch (worldIndex)
            {
                case 2: return 0.0832f; // measured disc top; keep the walkable collider on visible skin
                case 3: return -0.034f; // foil bowl's central floor
                case 4: return 0.144f; // bubble disc
                case 5: return 0.175f; // gear hub
                case 6: return 0.0636f; // measured ice shelf surface top
                case 7: return -0.039f; // dish's central floor
                case 8: return 0.077f; // hull deck
                case 9: return 0.0667f; // measured star pad body top
                case 10: return 0.0454f; // measured glass pane walking surface top
                default: return 0.087f;
            }
        }

        private static void BuildVineDisc(Transform root, Color accent, int index)
        {
            var discMesh = OrganicMeshFactory.SoftBlob(2, 0.85f, 0.22f, 0.04f);
            OrganicMeshFactory.SetPlanarTopDownUVs(discMesh, index * 31f);
            var disc = OrganicMeshFactory.CreateMeshObject(
                "disc",
                discMesh,
                root,
                Vector3.zero,
                new Vector3(1.6f, 0.45f, 1.6f),
                null);
            ApplyMat(disc, "Mat_Trav_VineDisc", new Color(0.72f, 0.82f, 0.72f, 1f), 0.3f, 0.02f);

            var rib = OrganicMeshFactory.CreateMeshObject(
                "stem",
                OrganicMeshFactory.RibbedCylinder(0.55f, 0.12f, 6, 0.03f, 28, 10),
                root,
                new Vector3(0f, -0.35f, 0f),
                Vector3.one,
                null);
            ApplyMat(rib, "Mat_Trav_VineStem", Color.Lerp(accent, new Color(0.35f, 0.45f, 0.2f), 0.4f), 0.45f, 0.05f);
            _ = index;
        }

        private static void BuildFoilPad(Transform root, Color accent, int index)
        {
            var pad = OrganicMeshFactory.CreateMeshObject(
                "foil",
                OrganicMeshFactory.DishBowl(0.75f, 0.12f, 8, 32),
                root,
                Vector3.zero,
                new Vector3(1.5f, 0.8f, 1.5f),
                null);
            ApplyMat(pad, "Mat_Trav_FoilPad", Color.Lerp(accent, new Color(0.75f, 0.85f, 1f), 0.35f), 0.25f, 0.7f);

            var ring = OrganicMeshFactory.CreateMeshObject(
                "rim",
                OrganicMeshFactory.Torus(0.7f, 0.06f, 36, 12),
                root,
                new Vector3(0f, 0.08f, 0f),
                Vector3.one,
                null);
            ApplyMat(ring, "Mat_Trav_FoilRim", Color.Lerp(accent, Color.white, 0.25f), 0.2f, 0.75f);
            _ = index;
        }

        private static void BuildBubbleDisc(Transform root, Color accent, int index)
        {
            var bubble = OrganicMeshFactory.CreateMeshObject(
                "bubble",
                OrganicMeshFactory.SoftBlob(3, 0.7f, 0.35f, 0.06f),
                root,
                Vector3.zero,
                new Vector3(1.5f, 0.55f, 1.5f),
                null);
            var tint = Color.Lerp(accent, new Color(0.6f, 0.9f, 1f), (index % 3) * 0.12f);
            ApplyMat(bubble, $"Mat_Trav_BubbleDisc_{index % 3}", tint, 0.12f, 0.05f);

            var ring = OrganicMeshFactory.CreateMeshObject(
                "soap",
                OrganicMeshFactory.Torus(0.55f, 0.05f, 32, 10),
                root,
                new Vector3(0f, 0.12f, 0f),
                Vector3.one,
                null);
            ApplyMat(ring, "Mat_Trav_BubbleRing", Color.Lerp(accent, Color.white, 0.4f), 0.1f, 0.05f);
        }

        private static void BuildGearPlate(Transform root, Color accent, int index)
        {
            var gear = OrganicMeshFactory.CreateMeshObject(
                "gear",
                OrganicMeshFactory.GearRing(0.65f, 0.08f, 10 + index % 3, 0.1f, 56, 12),
                root,
                Vector3.zero,
                new Vector3(1.2f, 0.9f, 1.2f),
                null);
            ApplyMat(gear, "Mat_Trav_GearPlate", Color.Lerp(accent, new Color(0.7f, 0.7f, 0.75f), 0.35f), 0.3f, 0.65f);

            var hub = OrganicMeshFactory.CreateMeshObject(
                "hub",
                OrganicMeshFactory.RibbedCylinder(0.25f, 0.22f, 8, 0.02f, 24, 8),
                root,
                new Vector3(0f, 0.05f, 0f),
                Vector3.one,
                null);
            ApplyMat(hub, "Mat_Trav_GearHub", Color.Lerp(accent, new Color(0.85f, 0.55f, 0.25f), 0.3f), 0.35f, 0.5f);
        }

        private static void BuildIceShelf(Transform root, Color accent, int index)
        {
            var shelf = OrganicMeshFactory.CreateMeshObject(
                "shelf",
                OrganicMeshFactory.SoftBlob(2, 0.9f, 0.18f, 0.03f),
                root,
                Vector3.zero,
                new Vector3(1.8f, 0.4f, 1.3f),
                null);
            ApplyMat(shelf, "Mat_Trav_IceShelf", Color.Lerp(accent, Color.white, 0.35f), 0.15f, 0.15f);

            var crystal = OrganicMeshFactory.CreateMeshObject(
                "shard",
                OrganicMeshFactory.CrystalFacet(0.45f, 0.12f, 5 + index % 2),
                root,
                // Keep the landing center clear so a player's feet do not
                // overlap a decorative crystal shard on every traversal shelf.
                new Vector3(0.55f, 0.2f, -0.5f),
                Vector3.one,
                null);
            ApplyMat(crystal, "Mat_Trav_IceShard", Color.Lerp(accent, new Color(0.85f, 0.95f, 1f), 0.4f), 0.12f, 0.1f);
        }

        private static void BuildDishPlate(Transform root, Color accent, int index)
        {
            var dish = OrganicMeshFactory.CreateMeshObject(
                "dish",
                OrganicMeshFactory.DishBowl(0.8f, 0.16f, 10, 36),
                root,
                Vector3.zero,
                new Vector3(1.4f, 0.7f, 1.4f),
                null);
            ApplyMat(dish, "Mat_Trav_DishPlate", Color.Lerp(accent, new Color(0.7f, 0.45f, 0.9f), 0.25f), 0.3f, 0.35f);

            var arch = OrganicMeshFactory.CreateMeshObject(
                "lip",
                OrganicMeshFactory.ArchGateway(1.1f, 0.55f, 0.12f, 20),
                root,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(1f, 0.6f, 1f),
                null);
            ApplyMat(arch, "Mat_Trav_DishLip", Color.Lerp(accent, new Color(0.55f, 0.35f, 0.8f), 0.3f), 0.25f, 0.4f);
            _ = index;
        }

        private static void BuildHullDeck(Transform root, Color accent, int index)
        {
            var deck = OrganicMeshFactory.CreateMeshObject(
                "deck",
                OrganicMeshFactory.RibbedCylinder(0.22f, 0.7f, 10, 0.04f, 36, 8, capEnds: true),
                root,
                Vector3.zero,
                new Vector3(1.3f, 0.7f, 1.1f),
                null);
            ApplyMat(deck, "Mat_Trav_HullDeck", Color.Lerp(accent, new Color(0.35f, 0.5f, 0.55f), 0.35f), 0.35f, 0.45f);

            var rail = OrganicMeshFactory.CreateMeshObject(
                "rail",
                OrganicMeshFactory.Torus(0.65f, 0.05f, 28, 10),
                root,
                new Vector3(0f, 0.2f, 0f),
                new Vector3(1f, 0.4f, 1f),
                null);
            ApplyMat(rail, "Mat_Trav_HullRail", Color.Lerp(accent, new Color(0.85f, 0.75f, 0.4f), 0.25f), 0.3f, 0.55f);
            _ = index;
        }

        private static void BuildStarPad(Transform root, Color accent, int index)
        {
            var pad = OrganicMeshFactory.CreateMeshObject(
                "pad",
                OrganicMeshFactory.SoftBlob(2, 0.75f, 0.2f, 0.04f),
                root,
                Vector3.zero,
                new Vector3(1.5f, 0.45f, 1.5f),
                null);
            ApplyMat(pad, "Mat_Trav_StarPad", Color.Lerp(accent, new Color(0.45f, 0.35f, 0.9f), 0.3f), 0.25f, 0.2f);

            var star = OrganicMeshFactory.CreateMeshObject(
                "star",
                OrganicMeshFactory.FacetedStar(0.45f, 0.18f, 0.1f, 5 + index % 2),
                root,
                new Vector3(0f, 0.18f, 0f),
                Vector3.one,
                null);
            ApplyMat(star, "Mat_Trav_StarGlyph", Color.Lerp(accent, new Color(1f, 0.9f, 0.55f), 0.4f), 0.2f, 0.15f);
        }

        private static void BuildGlassPane(Transform root, Color accent, int index)
        {
            var pane = OrganicMeshFactory.CreateMeshObject(
                "pane",
                OrganicMeshFactory.RibbonPlane(1.4f, 1.1f, 0.08f, 18, 8, 0.2f),
                root,
                Vector3.zero,
                new Vector3(1f, 0.35f, 1f),
                null);
            ApplyMat(pane, "Mat_Trav_GlassPane", Color.Lerp(accent, new Color(0.7f, 0.9f, 1f), 0.35f), 0.1f, 0.2f);

            var frame = OrganicMeshFactory.CreateMeshObject(
                "frame",
                OrganicMeshFactory.ArchGateway(1.5f, 0.7f, 0.1f, 22),
                root,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(1f, 0.55f, 1f),
                null);
            ApplyMat(frame, "Mat_Trav_GlassFrame", Color.Lerp(accent, new Color(0.95f, 0.75f, 0.35f), 0.3f), 0.25f, 0.55f);
            _ = index;
        }

        private static void EnsureSecretSpur(int worldIndex, Transform routeRoot, Color accent)
        {
            if (GameObject.Find("ART_SecretSpur") != null || FindDeep(routeRoot, "ART_SecretSpur") != null)
            {
                return;
            }

            var spur = new GameObject("ART_SecretSpur");
            spur.transform.SetParent(routeRoot, false);
            var side = worldIndex % 2 == 0 ? -5.5f : 5.5f;
            spur.transform.localPosition = new Vector3(side, 1.1f, 8.5f);

            var platform = OrganicMeshFactory.CreateMeshObject(
                "spurPad",
                OrganicMeshFactory.SoftBlob(2, 0.7f, 0.25f, 0.04f),
                spur.transform,
                Vector3.zero,
                new Vector3(1.3f, 0.5f, 1.3f),
                null);
            ApplyMat(platform, $"Mat_Trav_SecretSpur_W{worldIndex}", Color.Lerp(accent, new Color(0.9f, 0.55f, 1f), 0.35f), 0.35f, 0.15f);

            var glow = OrganicMeshFactory.CreateMeshObject(
                "glow",
                OrganicMeshFactory.SoftBlob(3, 0.35f, 0.9f, 0.08f),
                spur.transform,
                new Vector3(0f, 0.55f, 0f),
                Vector3.one,
                null);
            ApplyMat(glow, $"Mat_Trav_SecretGlow_W{worldIndex}", Color.Lerp(accent, new Color(1f, 0.85f, 0.4f), 0.45f), 0.15f, 0.05f);

            var beacon = glow.AddComponent<SpaceAndCrafts.Gameplay.SecretWonderBeacon>();
            var beaconSo = new SerializedObject(beacon);
            beaconSo.FindProperty("wonderToast").stringValue =
                $"World {worldIndex:00} secret shimmer — curiosity pays off!";
            beaconSo.ApplyModifiedPropertiesWithoutUndo();

            var col = spur.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 0.4f, 1.4f);
            col.center = new Vector3(0f, 0.05f, 0f);

            var momentGo = new GameObject("ART_KingdomMoment");
            momentGo.transform.SetParent(spur.transform, false);
            momentGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var momentCol = momentGo.AddComponent<BoxCollider>();
            momentCol.isTrigger = true;
            momentCol.size = new Vector3(2.2f, 2f, 2.2f);
            var moment = momentGo.AddComponent<SpaceAndCrafts.Gameplay.KingdomMomentTrigger>();
            var momentSo = new SerializedObject(moment);
            momentSo.FindProperty("momentId").stringValue = $"w{worldIndex:00}_secret_vista";
            momentSo.FindProperty("toast").stringValue = $"World {worldIndex:00} vista — take it in!";
            momentSo.FindProperty("lookTarget").objectReferenceValue = glow.transform;
            momentSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureInventionTeachPad(int worldIndex, Transform artRoot, Color accent)
        {
            if (GameObject.Find("ART_InventionTeachPad") != null)
            {
                return;
            }

            var site = GameObject.Find("ART_BuildSite")
                       ?? GameObject.Find("ART_VineBuildSite")
                       ?? FindDeep(artRoot, "ART_BuildSite")
                       ?? FindDeep(artRoot, "ART_VineBuildSite");

            var teach = new GameObject("ART_InventionTeachPad");
            if (site != null)
            {
                teach.transform.SetParent(site.transform, false);
                teach.transform.localPosition = new Vector3(1.6f, 0.15f, -0.4f);
            }
            else
            {
                teach.transform.SetParent(artRoot, false);
                teach.transform.localPosition = new Vector3(1.8f, 0.9f, 4.2f);
            }

            var blob = OrganicMeshFactory.CreateMeshObject(
                "ghostBlob",
                OrganicMeshFactory.SoftBlob(2, 0.55f, 0.4f, 0.03f),
                teach.transform,
                Vector3.zero,
                new Vector3(1.1f, 0.55f, 1.1f),
                null);
            ApplyMat(blob, $"Mat_Trav_TeachPad_W{worldIndex}", Color.Lerp(accent, Color.white, 0.45f), 0.2f, 0.1f);

            var torus = OrganicMeshFactory.CreateMeshObject(
                "ghostRing",
                OrganicMeshFactory.Torus(0.48f, 0.06f, 36, 12),
                teach.transform,
                new Vector3(0f, 0.2f, 0f),
                Vector3.one,
                null);
            ApplyMat(torus, $"Mat_Trav_TeachRing_W{worldIndex}", Color.Lerp(accent, new Color(1f, 0.9f, 0.5f), 0.35f), 0.25f, 0.2f);
        }

        private static string FantasyLabel(int worldIndex)
        {
            for (var i = 0; i < FallbackJobs.Length; i++)
            {
                if (FallbackJobs[i].index == worldIndex)
                {
                    return FallbackJobs[i].fantasy;
                }
            }

            return "organic pads";
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            var existing = FindDeep(parent, name);
            if (existing != null)
            {
                return existing.transform;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        private static GameObject FindArtRoot(int worldIndex)
        {
            var candidates = new[]
            {
                $"ART_World{worldIndex:00}",
                "ART_World02_CorkscrewCanopy",
                "ART_corkscrew_canopy",
                "ART_magnet_marsh",
                "ART_bubble_bazaar",
                "ART_clockwork_crater",
                "ART_frosted_filament",
                "ART_echo_atelier",
                "ART_tide_tinker_yard",
                "ART_nebula_nursery",
                "ART_scrap_cathedral"
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var go = GameObject.Find(candidates[i]);
                if (go != null)
                {
                    return go;
                }
            }

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name.StartsWith("ART_World") || roots[i].name.StartsWith("ART_corkscrew") ||
                    roots[i].name.StartsWith("ART_magnet") || roots[i].name.StartsWith("ART_bubble") ||
                    roots[i].name.StartsWith("ART_clockwork") || roots[i].name.StartsWith("ART_frosted") ||
                    roots[i].name.StartsWith("ART_echo") || roots[i].name.StartsWith("ART_tide") ||
                    roots[i].name.StartsWith("ART_nebula") || roots[i].name.StartsWith("ART_scrap"))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root.gameObject;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string KnownScenePath(int worldIndex)
        {
            switch (worldIndex)
            {
                case 2: return "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
                case 3: return "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";
                case 4: return "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity";
                case 5: return "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity";
                case 6: return "Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity";
                case 7: return "Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity";
                case 8: return "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity";
                case 9: return "Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity";
                case 10: return "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity";
                default: return null;
            }
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
