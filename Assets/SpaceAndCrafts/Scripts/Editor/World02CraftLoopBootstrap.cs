#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Upgrades an already-bootstrapped World02 scene with collect→craft Pingline Spooler loop pieces.
    /// </summary>
    public static class World02CraftLoopBootstrap
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string CollectiblePath = "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap World 02 Craft Loop")]
        public static void Bootstrap()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            EnsureFolder(CollectiblePath);

            var reel = UpsertCollectible("component_vine_reel", "Winding Spool", "A smooth reel core for a line that reaches across the cave.", CollectibleCategory.Component);
            var hook = UpsertCollectible("component_grapple_hook", "Tuning-Fork Hook", "A rounded fork that catches the cave's resonant anchor rings.", CollectibleCategory.Component);
            var sap = UpsertCollectible("component_sap_battery", "Ping Crystal", "A bright crystal that gives the spooler its clear two-note signal.", CollectibleCategory.Component);

            var world = GameObject.Find("ART_World02_CorkscrewCanopy") ?? new GameObject("ART_World02_CorkscrewCanopy");
            PlacePickup(world.transform, reel, new Vector3(-3f, 1f, 2f), new Color(0.47f, 0.62f, 0.93f));
            PlacePickup(world.transform, hook, new Vector3(3f, 1f, 3f), new Color(0.85f, 0.75f, 0.35f));
            PlacePickup(world.transform, sap, new Vector3(0f, 1.2f, 8f), new Color(1f, 0.65f, 0.2f));

            var siteGo = GameObject.Find("ART_VineBuildSite");
            if (siteGo == null)
            {
                siteGo = new GameObject("ART_VineBuildSite");
                siteGo.transform.position = new Vector3(0f, 0.9f, 5f);
                var mesh = OrganicMeshFactory.CreateMeshObject("ART_BuildPlinth", OrganicMeshFactory.SoftBlob(2, 0.7f, 0.35f, 0.02f), siteGo.transform, Vector3.zero, Vector3.one, null);
                ApplyMat(mesh, "Mat_VineBuildPlinth", new Color(0.45f, 0.35f, 0.25f), 0.4f, 0.1f);
            }

            var invention = GameObject.Find("ART_VineSpooler");
            if (invention == null)
            {
                invention = new GameObject("ART_VineSpooler");
                invention.transform.SetParent(siteGo.transform, false);
                invention.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                invention.SetActive(false);
            }

            var inventionWasActive = invention.activeSelf;
            invention.SetActive(true);
            OrganicInventionBuilders.BuildPinglineSpooler(invention.transform, ApplyMat);
            invention.SetActive(inventionWasActive);

            var build = siteGo.GetComponent<BuildSite>();
            if (build == null)
            {
                build = siteGo.AddComponent<BuildSite>();
            }

            var so = new SerializedObject(build);
            so.FindProperty("inventionId").stringValue = "vine_spooler";
            var req = so.FindProperty("requiredCollectibleIds");
            req.arraySize = 3;
            req.GetArrayElementAtIndex(0).stringValue = "component_vine_reel";
            req.GetArrayElementAtIndex(1).stringValue = "component_grapple_hook";
            req.GetArrayElementAtIndex(2).stringValue = "component_sap_battery";
            so.FindProperty("assembledInventionRoot").objectReferenceValue = invention;
            so.FindProperty("assemblyDuration").floatValue = 2.2f;
            var splat = GameObject.Find("Splat_TEMP");
            if (splat != null)
            {
                so.FindProperty("inventory").objectReferenceValue = splat.GetComponent<InventoryComponent>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (invention.GetComponent<VineSpooler>() == null)
            {
                var vine = invention.AddComponent<VineSpooler>();
                if (splat != null)
                {
                    vine.AttachOwner(splat.transform);
                }
            }

            if (invention.GetComponent<SphereCollider>() == null)
            {
                var col = invention.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.2f;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[Space & Crafts] World02 craft loop wired (spool/fork/ping crystal → Pingline Spooler).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static CollectibleDefinition UpsertCollectible(string id, string display, string desc, CollectibleCategory category)
        {
            EnsureFolder(CollectiblePath);
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
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void PlacePickup(Transform parent, CollectibleDefinition def, Vector3 pos, Color color)
        {
            var go = new GameObject($"ART_Pickup_{def.name}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var mesh = OrganicMeshFactory.CreateMeshObject("mesh", OrganicMeshFactory.SoftBlob(2, 0.28f, 1f, 0.04f), go.transform, Vector3.zero, Vector3.one, null);
            ApplyMat(mesh, $"Mat_Pickup_{def.name}", color, 0.4f, 0.1f);
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
