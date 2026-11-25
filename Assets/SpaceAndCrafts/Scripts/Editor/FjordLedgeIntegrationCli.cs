using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class FjordLedgeIntegrationCli
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Commissioned/Environments/MeshySpenddown/FjordKit_Iter1842/Fjord_Cliff_Ledge_Module_Iter1842.fbx";
        private const string TextureFolder = "Assets/SpaceAndCrafts/Art/Commissioned/Environments/MeshySpenddown/FjordKit_Iter1842/Fjord_Cliff_Ledge_Module_Iter1842_textures/";
        private const string MaterialPath = "Assets/SpaceAndCrafts/Art/Commissioned/Environments/MeshySpenddown/FjordKit_Iter1842/Mat_FjordCliffLedge_Iter1842.mat";

        public static void Inspect()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new System.Exception("Could not load " + ModelPath);
            var probe = Object.Instantiate(model);
            probe.hideFlags = HideFlags.HideAndDontSave;
            var renderers = probe.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Debug.Log($"FJORD_MODEL local-root={probe.transform.position} bounds-center={bounds.center} size={bounds.size} renderers={renderers.Length} materials={string.Join(",", renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct())}");
            foreach (var name in new[] { "Splat_TEMP", "ART_BuildSite", "ART_Checkpoint_GulchFar", "ART_FjordLandform", "ART_ShelfFloor" })
            {
                var t = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.name==name);
                Debug.Log(t == null ? $"FJORD_SCENE {name}=missing" : $"FJORD_SCENE {name}={t.position} scale={t.lossyScale}");
            }
            Object.DestroyImmediate(probe);
            EditorApplication.Exit(0);
        }

        public static void InspectOptionalRoutes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var names = new[]
            {
                "Zone_WestSpur", "Zone_EastSpur", "TEMP_ART_Zone_SecretNook",
                "ART_Pickup_discovery_badge_secret_nook", "ART_Wonder_wonder_east_spur",
                "ART_Wonder_wonder_west_spur", "ART_CuriositySpur"
            };
            foreach (var name in names)
            {
                var t = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(x => x.name == name);
                if (t == null)
                {
                    Debug.Log($"OPTIONAL_ROUTE {name}=missing");
                    continue;
                }
                var renderer = t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>(true);
                var collider = t.GetComponent<Collider>() ?? t.GetComponentInChildren<Collider>(true);
                Debug.Log($"OPTIONAL_ROUTE {name} world={t.position:F2} local={t.localPosition:F2} scale={t.lossyScale:F2} parent={(t.parent != null ? t.parent.name : "root")} bounds={(renderer != null ? renderer.bounds.ToString() : "no renderer")} collider={(collider != null ? collider.GetType().Name + ":" + collider.bounds.ToString() : "none")} active={t.gameObject.activeInHierarchy}");
            }
            var nook = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "TEMP_ART_Zone_SecretNook");
            if (nook != null)
            {
                foreach (var child in nook.GetComponentsInChildren<Transform>(true))
                {
                    var renderer = child.GetComponent<Renderer>();
                    var collider = child.GetComponent<Collider>();
                    Debug.Log($"OPTIONAL_NOOK_CHILD {child.name} world={child.position:F2} scale={child.lossyScale:F2} renderer={(renderer != null ? renderer.bounds.ToString() : "none")} collider={(collider != null ? collider.GetType().Name + ":" + collider.bounds.ToString() : "none")}");
                }
            }
            EditorApplication.Exit(0);
        }

        public static void Integrate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new System.Exception("Could not load " + ModelPath);
            var material = EnsureMaterial();
            const string groupName = "ART_FjordLedgeModules_Iter2148";
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == groupName);
            if (existing != null) Object.DestroyImmediate(existing);
            var group = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(group, "Integrate commissioned fjord ledges");

            // The centerline route spans x=0 and is about 6.3 m wide. These larger
            // modules remain outside its collider after the Iteration 2149 player-
            // view capture showed the first composition reading as small piles.
            var placements = new[]
            {
                new Vector3(-5.45f, -0.85f, 1.6f),
                new Vector3( 5.45f, -0.85f, 5.7f),
                new Vector3(-5.45f, -0.85f, 9.8f),
                new Vector3( 5.45f, -0.85f, 13.9f),
                new Vector3(-5.45f, -0.85f, 18.0f),
                new Vector3( 5.45f, -0.85f, 22.1f),
            };
            // Model prefab children carry a 0.01 unit conversion; use the larger
            // effective root scales measured in the rendered scene.
            var scales = new[] { 330f, 335f, 340f, 345f, 340f, 335f };
            var yaws = new[] { 18f, 164f, 344f, 191f, 25f, 155f };
            for (var i = 0; i < placements.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                if (instance == null) throw new System.Exception("Prefab instantiation failed at module " + i);
                instance.name = $"ART_FjordCliffLedge_{i + 1:00}";
                Undo.RegisterCreatedObjectUndo(instance, "Place fjord ledge module");
                instance.transform.SetParent(group.transform, false);
                instance.transform.SetPositionAndRotation(placements[i], Quaternion.Euler(0f, yaws[i], 0f));
                instance.transform.localScale = Vector3.one * scales[i];
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }
                Debug.Log($"FJORD_PLACED {instance.name} pos={instance.transform.position} scale={scales[i]} colliders={instance.GetComponentsInChildren<Collider>(true).Length}");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log("FJORD_INTEGRATION_SAVED " + ScenePath);
            EditorApplication.Exit(0);
        }

        public static void RecomposeNearBanksIter2150()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var group = scene.GetRootGameObjects()
                .FirstOrDefault(g => g.name == "ART_FjordLedgeModules_Iter2148");
            if (group == null) throw new System.Exception("The Iteration 2148 fjord ledge group is missing");

            // Preserve the route collider and its z rhythm. Move the scenic meshes
            // 0.35 m inward and enlarge them; measured mesh bounds leave at least
            // about 0.35 m beyond the 3.15 m half-width route collider.
            var placements = new[]
            {
                new Vector3(-5.45f, -0.85f, 1.6f),
                new Vector3( 5.45f, -0.85f, 5.7f),
                new Vector3(-5.45f, -0.85f, 9.8f),
                new Vector3( 5.45f, -0.85f, 13.9f),
                new Vector3(-5.45f, -0.85f, 18.0f),
                new Vector3( 5.45f, -0.85f, 22.1f),
            };
            var scales = new[] { 330f, 335f, 340f, 345f, 340f, 335f };
            for (var i = 0; i < placements.Length; i++)
            {
                var instance = group.transform.Find($"ART_FjordCliffLedge_{i + 1:00}");
                if (instance == null) throw new System.Exception($"Missing ledge module {i + 1:00}");
                instance.SetPositionAndRotation(placements[i], instance.rotation);
                instance.localScale = Vector3.one * scales[i];
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }
                var bounds = instance.GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.bounds)
                    .Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Debug.Log($"FJORD_RECOMPOSED {instance.name} pos={instance.position:F2} scale={scales[i]} bounds={bounds.size:F2} colliders={instance.GetComponentsInChildren<Collider>(true).Count(c => c.enabled)}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log("FJORD_NEARBANK_RECOMPOSITION_SAVED " + ScenePath);
            EditorApplication.Exit(0);
        }

        public static void ExtendNearBanksIter2153()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var group = scene.GetRootGameObjects()
                .FirstOrDefault(g => g.name == "ART_FjordLedgeModules_Iter2148");
            if (group == null) throw new System.Exception("The fjord ledge group is missing");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new System.Exception("Could not load " + ModelPath);

            var baseMaterial = EnsureMaterial();
            const string coolMaterialPath = "Assets/SpaceAndCrafts/Art/Commissioned/Environments/MeshySpenddown/FjordKit_Iter1842/Mat_FjordCliffLedge_Cool_Iter2153.mat";
            var coolMaterial = AssetDatabase.LoadAssetAtPath<Material>(coolMaterialPath);
            if (coolMaterial == null)
            {
                coolMaterial = new Material(baseMaterial) { name = "Mat_FjordCliffLedge_Cool_Iter2153" };
                AssetDatabase.CreateAsset(coolMaterial, coolMaterialPath);
            }
            coolMaterial.SetColor("_BaseColor", new Color(0.84f, 0.93f, 1f, 1f));
            EditorUtility.SetDirty(coolMaterial);
            AssetDatabase.SaveAssets();

            // Fill the alternating gaps along both shoulders. These scenic meshes
            // remain outside the active ridge collider and have no enabled colliders.
            var positions = new[]
            {
                new Vector3(-5.45f, -0.90f, 5.7f),
                new Vector3(-5.45f, -0.82f, 13.9f),
                new Vector3(-5.45f, -0.93f, 22.1f),
                new Vector3( 5.45f, -0.91f, 1.6f),
                new Vector3( 5.45f, -0.84f, 9.8f),
                new Vector3( 5.45f, -0.89f, 18.0f),
            };
            var scales = new[] { 330f, 340f, 330f, 330f, 340f, 330f };
            var yaws = new[] { 208f, 30f, 170f, 340f, 12f, 195f };
            for (var i = 0; i < positions.Length; i++)
            {
                var name = $"ART_FjordBankInfiller_Iter2153_{i + 1:00}";
                var existing = group.transform.Find(name);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                if (instance == null) throw new System.Exception("Prefab instantiation failed at infill module " + i);
                instance.name = name;
                instance.transform.SetParent(group.transform, false);
                instance.transform.SetPositionAndRotation(positions[i], Quaternion.Euler(0f, yaws[i], 0f));
                instance.transform.localScale = Vector3.one * scales[i];
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = coolMaterial;
                }
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }
                var bounds = instance.GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.bounds)
                    .Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Debug.Log($"FJORD_BANK_INFILL {name} pos={instance.transform.position:F2} scale={scales[i]} yaw={yaws[i]} bounds={bounds.size:F2} enabledColliders={instance.GetComponentsInChildren<Collider>(true).Count(c => c.enabled)} material={coolMaterial.name}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log("FJORD_BANK_INFILL_SAVED " + ScenePath);
            EditorApplication.Exit(0);
        }

        public static void RefineNearBanksIter2154()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var group = scene.GetRootGameObjects()
                .FirstOrDefault(g => g.name == "ART_FjordLedgeModules_Iter2148");
            if (group == null) throw new System.Exception("The fjord ledge group is missing");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/SpaceAndCrafts/Art/Commissioned/Environments/MeshySpenddown/FjordKit_Iter1842/Mat_FjordCliffLedge_Cool_Iter2153.mat");
            if (material == null) throw new System.Exception("The cool fjord material is missing");
            material.SetColor("_BaseColor", new Color(0.94f, 0.98f, 1f, 1f));
            EditorUtility.SetDirty(material);

            // Break the nearly level crown into three low and three raised seats;
            // the shoreline silhouette now steps at different points on each bank.
            var heights = new[] { -1.18f, -0.55f, -1.08f, -0.56f, -1.14f, -0.62f };
            for (var i = 0; i < heights.Length; i++)
            {
                var target = group.transform.Find($"ART_FjordBankInfiller_Iter2153_{i + 1:00}");
                if (target == null) throw new System.Exception($"Missing fjord bank infill {i + 1:00}");
                var position = target.position;
                position.y = heights[i];
                target.position = position;
                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }
                var bounds = target.GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.bounds)
                    .Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Debug.Log($"FJORD_BANK_REFINED {target.name} pos={target.position:F2} bounds={bounds.size:F2} enabledColliders={target.GetComponentsInChildren<Collider>(true).Count(c => c.enabled)}");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Failed to save Arrival scene");
            Debug.Log("FJORD_BANK_REFINEMENT_SAVED tint=(0.94,0.98,1.0) staggeredHeights=6");
            EditorApplication.Exit(0);
        }

        private static Material EnsureMaterial()
        {
            var normalPath = TextureFolder + "normal.png";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter
                && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.sRGBTexture = false;
                normalImporter.SaveAndReimport();
            }
            foreach (var linearMap in new[] { TextureFolder + "metallic.png", TextureFolder + "roughness.png" })
            {
                if (AssetImporter.GetAtPath(linearMap) is TextureImporter importer && importer.sRGBTexture)
                {
                    importer.sRGBTexture = false;
                    importer.SaveAndReimport();
                }
            }
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "base_color.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "metallic.png");
            if (albedo == null || normal == null || metallic == null)
                throw new System.Exception("Fjord ledge texture import is incomplete");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new System.Exception("URP/Lit shader is missing");
                material = new Material(shader) { name = "Mat_FjordCliffLedge_Iter1842" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", new Color(0.92f, 0.97f, 1f, 1f));
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.7f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", metallic);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
