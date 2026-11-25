#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Adds collect→craft loops for Worlds 03–05 using their unique invention components.
    /// </summary>
    public static class Worlds03to05CraftLoopBootstrap
    {
        private const string CollectiblePath = "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap Worlds 03-05 Craft Loops")]
        public static void Bootstrap()
        {
            EnsureFolder(CollectiblePath);
            WireWorld(
                "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity",
                3,
                "junk_magnet_gauntlet",
                "JunkMagnetGauntlet",
                ("component_magnet_coil", "Magnet Coil"),
                ("component_scrap_glove", "Scrap Glove"),
                ("component_iron_core", "Iron Core"),
                typeof(JunkMagnetGauntlet));
            WireWorld(
                "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity",
                4,
                "paint_bubble_raft",
                "PaintBubbleRaft",
                ("component_soap_film", "Soap Film"),
                ("component_paint_bladder", "Paint Bladder"),
                ("component_balloon_ring", "Balloon Ring"),
                typeof(PaintBubbleRaft));
            WireWorld(
                "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity",
                5,
                "windup_bridge_bug",
                "WindUpBridgeBug",
                ("component_mainspring", "Mainspring"),
                ("component_bug_chassis", "Bug Chassis"),
                ("component_key_crank", "Key Crank"),
                typeof(WindUpBridgeBug));

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Worlds 03–05 craft loops wired.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void WireWorld(
            string scenePath,
            int worldIndex,
            string inventionId,
            string inventionObjectName,
            (string id, string display) a,
            (string id, string display) b,
            (string id, string display) c,
            System.Type inventionType)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var ca = Upsert(a.id, a.display);
            var cb = Upsert(b.id, b.display);
            var cc = Upsert(c.id, c.display);

            PlacePickup(ca, new Vector3(-3f, 1f, 2f), new Color(0.6f, 0.8f, 1f));
            PlacePickup(cb, new Vector3(3f, 1f, 3f), new Color(0.95f, 0.7f, 0.4f));
            PlacePickup(cc, new Vector3(0f, 1.2f, 8f), new Color(0.9f, 0.5f, 0.85f));

            var site = GameObject.Find("ART_BuildSite") ?? new GameObject("ART_BuildSite");
            site.transform.position = new Vector3(0f, 0.9f, 5f);
            if (site.GetComponentInChildren<MeshFilter>() == null)
            {
                var plinth = OrganicMeshFactory.CreateMeshObject("plinth", OrganicMeshFactory.SoftBlob(2, 0.7f, 0.35f, 0.02f), site.transform, Vector3.zero, Vector3.one, null);
                ApplyMat(plinth, $"Mat_{inventionId}_Plinth", new Color(0.5f, 0.4f, 0.3f), 0.4f, 0.1f);
            }

            var invention = GameObject.Find($"ART_{inventionObjectName}")
                ?? GameObject.Find($"ART_{inventionId}");
            if (invention == null)
            {
                invention = new GameObject($"ART_{inventionObjectName}");
                invention.transform.SetParent(site.transform, false);
                invention.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                invention.SetActive(false);
            }

            invention.name = $"ART_{inventionObjectName}";
            var inventionWasActive = invention.activeSelf;
            invention.SetActive(true);
            OrganicInventionBuilders.BuildForWorld(worldIndex, invention.transform, ApplyMat);
            invention.SetActive(inventionWasActive);

            if (invention.GetComponent(inventionType) == null)
            {
                invention.AddComponent(inventionType);
            }

            var build = site.GetComponent<BuildSite>() ?? site.AddComponent<BuildSite>();
            var so = new SerializedObject(build);
            so.FindProperty("inventionId").stringValue = inventionId;
            var req = so.FindProperty("requiredCollectibleIds");
            req.arraySize = 3;
            req.GetArrayElementAtIndex(0).stringValue = a.id;
            req.GetArrayElementAtIndex(1).stringValue = b.id;
            req.GetArrayElementAtIndex(2).stringValue = c.id;
            so.FindProperty("assembledInventionRoot").objectReferenceValue = invention;
            so.FindProperty("assemblyDuration").floatValue = 2.2f;
            var splat = GameObject.Find("Splat_TEMP");
            if (splat != null)
            {
                so.FindProperty("inventory").objectReferenceValue = splat.GetComponent<InventoryComponent>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Space & Crafts] Craft loop wired: {inventionId}");
        }

        private static CollectibleDefinition Upsert(string id, string display)
        {
            var path = $"{CollectiblePath}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CollectibleDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("description").stringValue = $"{display} component for adventure craft.";
            so.FindProperty("category").enumValueIndex = (int)CollectibleCategory.Component;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void PlacePickup(CollectibleDefinition def, Vector3 pos, Color color)
        {
            var existing = GameObject.Find($"ART_Pickup_{def.Id}");
            if (existing != null)
            {
                return;
            }

            var go = new GameObject($"ART_Pickup_{def.Id}");
            go.transform.position = pos;
            var mesh = OrganicMeshFactory.CreateMeshObject("mesh", OrganicMeshFactory.SoftBlob(2, 0.28f, 1f, 0.04f), go.transform, Vector3.zero, Vector3.one, null);
            ApplyMat(mesh, $"Mat_Pickup_{def.Id}", color, 0.4f, 0.1f);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;
            var pickup = go.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<PickupMotion>();
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

            EditorUtility.SetDirty(mat);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }

            _ = smoothness;
            _ = metallic;
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
