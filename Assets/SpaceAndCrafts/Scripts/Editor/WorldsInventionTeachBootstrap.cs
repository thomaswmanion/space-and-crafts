#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Wires ART_InventionTeachPad on Worlds 02–10 with InventionTeachPad + teach anchors.
    /// Idempotent via Find-by-name; does not touch Arrival.
    /// </summary>
    public static class WorldsInventionTeachBootstrap
    {
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        private static readonly (
            int worldIndex,
            string inventionId,
            InventionTeachPad.TraversalBeat beat,
            string toast,
            string anchorName,
            Vector3 anchorLocal)[] Jobs =
        {
            (2, "vine_spooler", InventionTeachPad.TraversalBeat.VineYank,
                "Pingline Spooler — ping, latch, and zip!", "ART_VineAnchor", new Vector3(0f, 4.5f, 7.5f)),
            (3, "junk_magnet_gauntlet", InventionTeachPad.TraversalBeat.MagnetPull,
                "Junk Magnet — pull the gap!", "ART_MagnetTarget", new Vector3(0f, 2.2f, 8f)),
            (4, "paint_bubble_raft", InventionTeachPad.TraversalBeat.BubbleFloat,
                "Paint Bubble — soft float!", "ART_BubbleLane", new Vector3(0f, 3.2f, 6.5f)),
            (5, "windup_bridge_bug", InventionTeachPad.TraversalBeat.BridgeBoost,
                "Bridge Bug — dash the span!", "ART_BridgeGap", new Vector3(0f, 1.2f, 9f)),
            (6, "heat_tape_torch", InventionTeachPad.TraversalBeat.TorchDash,
                "Heat-Tape — melt dash!", "ART_MeltLane", new Vector3(0f, 1.4f, 8.5f)),
            (7, "boom_dish_redirector", InventionTeachPad.TraversalBeat.BoomLaunch,
                "Boom Dish — launch up!", "ART_BoomCatch", new Vector3(0f, 5.5f, 5f)),
            (8, "vacuum_skimmer", InventionTeachPad.TraversalBeat.VacuumSpit,
                "Vacuum Skimmer — spit boost!", "ART_VacuumSpit", new Vector3(0f, 2f, 9f)),
            (9, "star_seed_planter", InventionTeachPad.TraversalBeat.StarFloat,
                "Star Seed — float + sprout!", "ART_StarSeedSpot", new Vector3(0f, 1.1f, 7f)),
            (10, "glass_sail_shield", InventionTeachPad.TraversalBeat.GlassBoost,
                "Glass Sail — beam boost!", "ART_GlassBeam", new Vector3(0f, 3.5f, 8f)),
        };

        [MenuItem("Space & Crafts/Bootstrap Worlds 02-10 Invention Teach Pads")]
        public static void Bootstrap()
        {
            var catalog = WorldCatalogBootstrap.SeedCatalog();
            var upgraded = 0;

            for (var i = 0; i < Jobs.Length; i++)
            {
                var job = Jobs[i];
                var def = catalog.GetByIndex(job.worldIndex);
                var scenePath = def != null && !string.IsNullOrEmpty(def.scenePath)
                    ? def.scenePath
                    : KnownScenePath(job.worldIndex);
                var displayName = def != null ? def.displayName : $"World {job.worldIndex:00}";
                var accent = def != null ? def.accentColor : Color.gray;

                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip invention teach — missing scene for world {job.worldIndex}: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                WireTeachPad(job.worldIndex, job.inventionId, job.beat, job.toast, job.anchorName, job.anchorLocal, accent);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                upgraded++;
                Debug.Log($"[Space & Crafts] Invention teach pad wired: {displayName} ({job.inventionId})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Worlds 02–10 invention teach pads complete ({upgraded} scenes).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void WireTeachPad(
            int worldIndex,
            string inventionId,
            InventionTeachPad.TraversalBeat beat,
            string toast,
            string anchorName,
            Vector3 anchorLocal,
            Color accent)
        {
            var artRoot = FindArtRoot(worldIndex) ?? new GameObject($"ART_World{worldIndex:00}");
            var teach = EnsureTeachPad(worldIndex, artRoot.transform, accent);
            var anchor = EnsureAnchor(artRoot.transform, teach.transform, anchorName, anchorLocal, beat, accent);

            var pad = teach.GetComponent<InventionTeachPad>() ?? teach.AddComponent<InventionTeachPad>();
            var so = new SerializedObject(pad);
            so.FindProperty("inventionId").stringValue = inventionId;
            so.FindProperty("beat").enumValueIndex = (int)beat;
            so.FindProperty("successToast").stringValue = toast;
            so.FindProperty("lockedToast").stringValue = "Craft the world invention first!";
            so.FindProperty("targetAnchor").objectReferenceValue = anchor;
            so.FindProperty("cooldown").floatValue = 1.25f;
            so.FindProperty("launchSpeed").floatValue = LaunchSpeedFor(beat);
            so.FindProperty("boostMultiplier").floatValue = 1.55f;
            so.FindProperty("boostDuration").floatValue = 1.6f;
            so.FindProperty("floatDuration").floatValue = 1.4f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsureTeachPad(int worldIndex, Transform artRoot, Color accent)
        {
            var teach = GameObject.Find("ART_InventionTeachPad")
                        ?? FindDeep(artRoot, "ART_InventionTeachPad");

            if (teach == null)
            {
                var site = GameObject.Find("ART_BuildSite")
                           ?? GameObject.Find("ART_VineBuildSite")
                           ?? FindDeep(artRoot, "ART_BuildSite")
                           ?? FindDeep(artRoot, "ART_VineBuildSite");

                teach = new GameObject("ART_InventionTeachPad");
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

            var col = teach.GetComponent<SphereCollider>();
            if (col == null)
            {
                col = teach.AddComponent<SphereCollider>();
            }

            col.isTrigger = true;
            col.radius = 1.15f;
            col.center = new Vector3(0f, 0.45f, 0f);
            return teach;
        }

        private static Transform EnsureAnchor(
            Transform artRoot,
            Transform teachPad,
            string anchorName,
            Vector3 localFromTeach,
            InventionTeachPad.TraversalBeat beat,
            Color accent)
        {
            var existing = GameObject.Find(anchorName) ?? FindDeep(artRoot, anchorName);
            if (existing != null)
            {
                return existing.transform;
            }

            var anchor = new GameObject(anchorName);
            anchor.transform.SetParent(artRoot, false);
            var world = teachPad.TransformPoint(localFromTeach);
            anchor.transform.position = world;

            switch (beat)
            {
                case InventionTeachPad.TraversalBeat.VineYank:
                    PlaceRing(anchor.transform, "ring", 0.55f, 0.07f, accent, new Color(0.3f, 0.7f, 0.35f));
                    break;
                case InventionTeachPad.TraversalBeat.MagnetPull:
                    PlaceRing(anchor.transform, "foil", 0.5f, 0.08f, accent, new Color(0.7f, 0.85f, 1f));
                    break;
                case InventionTeachPad.TraversalBeat.BubbleFloat:
                    PlaceBlob(anchor.transform, "bubble", 0.45f, accent, new Color(0.65f, 0.9f, 1f));
                    break;
                case InventionTeachPad.TraversalBeat.BridgeBoost:
                    PlaceBlob(anchor.transform, "gearMark", 0.35f, accent, new Color(0.85f, 0.7f, 0.35f));
                    break;
                case InventionTeachPad.TraversalBeat.TorchDash:
                    PlaceBlob(anchor.transform, "meltMark", 0.35f, accent, new Color(1f, 0.55f, 0.3f));
                    break;
                case InventionTeachPad.TraversalBeat.BoomLaunch:
                    PlaceRing(anchor.transform, "dishCatch", 0.65f, 0.06f, accent, new Color(0.75f, 0.45f, 0.95f));
                    break;
                case InventionTeachPad.TraversalBeat.VacuumSpit:
                    PlaceRing(anchor.transform, "spitRing", 0.5f, 0.06f, accent, new Color(0.4f, 0.8f, 0.85f));
                    break;
                case InventionTeachPad.TraversalBeat.StarFloat:
                    PlaceBlob(anchor.transform, "seedSpot", 0.4f, accent, new Color(0.7f, 0.65f, 1f));
                    break;
                default:
                    PlaceRing(anchor.transform, "beamRing", 0.55f, 0.06f, accent, new Color(1f, 0.85f, 0.45f));
                    break;
            }

            return anchor.transform;
        }

        private static void PlaceRing(Transform parent, string name, float major, float minor, Color accent, Color tint)
        {
            var ring = OrganicMeshFactory.CreateMeshObject(
                name,
                OrganicMeshFactory.Torus(major, minor, 36, 12),
                parent,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(ring, $"Mat_Teach_{parent.name}_{name}", Color.Lerp(accent, tint, 0.45f), 0.25f, 0.2f);
        }

        private static void PlaceBlob(Transform parent, string name, float radius, Color accent, Color tint)
        {
            var blob = OrganicMeshFactory.CreateMeshObject(
                name,
                OrganicMeshFactory.SoftBlob(2, radius, 0.7f, 0.04f),
                parent,
                Vector3.zero,
                Vector3.one,
                null);
            ApplyMat(blob, $"Mat_Teach_{parent.name}_{name}", Color.Lerp(accent, tint, 0.4f), 0.2f, 0.1f);
        }

        private static float LaunchSpeedFor(InventionTeachPad.TraversalBeat beat)
        {
            switch (beat)
            {
                case InventionTeachPad.TraversalBeat.VineYank: return 20f;
                case InventionTeachPad.TraversalBeat.MagnetPull: return 18f;
                case InventionTeachPad.TraversalBeat.BoomLaunch: return 22f;
                case InventionTeachPad.TraversalBeat.VacuumSpit: return 19f;
                case InventionTeachPad.TraversalBeat.TorchDash: return 17f;
                case InventionTeachPad.TraversalBeat.GlassBoost: return 16f;
                default: return 14f;
            }
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
