#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class Worlds06to10CraftLoopBootstrap
    {
        private const string CollectiblePath = "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap Worlds 06-10 Craft Loops")]
        public static void Bootstrap()
        {
            EnsureFolder(CollectiblePath);
            Wire("Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity", 6, "heat_tape_torch", "HeatTapeTorch",
                ("component_heat_tape", "Heat Tape"), ("component_torch_nozzle", "Torch Nozzle"), ("component_warm_cell", "Warm Cell"), typeof(HeatTapeTorch));
            Wire("Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity", 7, "boom_dish_redirector", "BoomDishRedirector",
                ("component_echo_dish", "Echo Dish"), ("component_resonator", "Resonator"), ("component_amp_crystal", "Amp Crystal"), typeof(BoomDishRedirector));
            Wire("Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity", 8, "vacuum_skimmer", "VacuumSkimmer",
                ("component_vac_intake", "Vac Intake"), ("component_pressure_tank", "Pressure Tank"), ("component_skim_fins", "Skim Fins"), typeof(VacuumSkimmer));
            Wire("Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity", 9, "star_seed_planter", "StarSeedPlanter",
                ("component_star_seeds", "Star Seeds"), ("component_planter_cup", "Planter Cup"), ("component_grow_lamp", "Grow Lamp"), typeof(StarSeedPlanter));
            Wire("Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity", 10, "glass_sail_shield", "GlassSailShield",
                ("component_stained_pane", "Stained Pane"), ("component_sail_frame", "Sail Frame"), ("component_prism_hub", "Prism Hub"), typeof(GlassSailShield));

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Worlds 06–10 craft loops wired.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Wire(string scenePath, int worldIndex, string inventionId, string objectName,
            (string id, string display) a, (string id, string display) b, (string id, string display) c, System.Type type)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var ca = Upsert(a.id, a.display);
            var cb = Upsert(b.id, b.display);
            var cc = Upsert(c.id, c.display);
            Place(ca, new Vector3(-3f, 1f, 2f), new Color(0.7f, 0.9f, 1f));
            Place(cb, new Vector3(3f, 1f, 3f), new Color(1f, 0.75f, 0.45f));
            Place(cc, new Vector3(0f, 1.2f, 8f), new Color(0.85f, 0.55f, 1f));

            var site = GameObject.Find("ART_BuildSite") ?? new GameObject("ART_BuildSite");
            site.transform.position = new Vector3(0f, 0.9f, 5f);
            var invention = GameObject.Find($"ART_{objectName}")
                ?? GameObject.Find($"ART_{inventionId}")
                ?? new GameObject($"ART_{objectName}");
            invention.name = $"ART_{objectName}";
            invention.transform.SetParent(site.transform, false);
            invention.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            invention.SetActive(true);
            OrganicInventionBuilders.BuildForWorld(worldIndex, invention.transform, ApplyMat);
            invention.SetActive(false);
            if (invention.GetComponent(type) == null)
            {
                invention.AddComponent(type);
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
            var def = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path) ?? ScriptableObject.CreateInstance<CollectibleDefinition>();
            if (AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path) == null)
            {
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("description").stringValue = $"{display} craft component.";
            so.FindProperty("category").enumValueIndex = (int)CollectibleCategory.Component;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void Place(CollectibleDefinition def, Vector3 pos, Color color)
        {
            if (GameObject.Find($"ART_Pickup_{def.Id}") != null)
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
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(mat);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
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
