#if UNITY_EDITOR
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Commercial encounter density: concurrent ScrapMite packs per world + Arrival zones.
    /// Also wires WorldMusicDirector into each world scene.
    /// </summary>
    public static class CommercialEncounterBootstrap
    {
        private static readonly (string scene, int worldIndex, int miteCount, float radius)[] Jobs =
        {
            ("Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity", 1, 0, 0f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity", 2, 5, 5.5f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity", 3, 5, 6f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity", 4, 6, 6.5f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity", 5, 6, 6.5f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity", 6, 6, 7f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity", 7, 7, 7.5f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity", 8, 7, 7.5f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity", 9, 8, 8f),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity", 10, 8, 8.5f),
        };

        [MenuItem("Space & Crafts/Bootstrap Commercial Encounters + Music")]
        public static void Bootstrap()
        {
            // Arrival: denser mite pack in Glade + music.
            var arrival = EditorSceneManager.OpenScene(Jobs[0].scene, OpenSceneMode.Single);
            EnsureArrivalMitePack();
            WorldMusicDirector.Ensure(1);
            EditorSceneManager.MarkSceneDirty(arrival);
            EditorSceneManager.SaveScene(arrival);

            for (var i = 1; i < Jobs.Length; i++)
            {
                var job = Jobs[i];
                if (!System.IO.File.Exists(job.scene))
                {
                    Debug.LogWarning($"[Space & Crafts] Skip encounters — missing {job.scene}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(job.scene, OpenSceneMode.Single);
                PlaceMitePack(job.miteCount, job.radius, job.worldIndex);
                PlaceBossLaneMitePack(2 + job.worldIndex / 4, job.worldIndex);
                PlaceFloaterPack(2 + job.worldIndex / 5, job.worldIndex);
                PlaceRollerPack(1 + job.worldIndex / 6, job.worldIndex);
                WorldMusicDirector.Ensure(job.worldIndex);
                EnsureSplatPresentation();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Space & Crafts] Encounters+music: world {job.worldIndex:00} mites={job.miteCount}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Commercial encounters + music bootstrapped.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureArrivalMitePack()
        {
            var glade = GameObject.Find("Zone_Glade");
            if (glade == null)
            {
                return;
            }

            // Add 2 extra mites if only one exists under glade.
            var existing = glade.GetComponentsInChildren<ScrapMite>(true);
            var need = 3 - existing.Length;
            for (var i = 0; i < need; i++)
            {
                PlaceMite(glade.transform, new Vector3(-2f + i * 2f, 0.7f, 2.5f + i * 0.5f), i);
            }
        }

        private static void PlaceMitePack(int count, float radius, int worldIndex)
        {
            var packName = "ART_EncounterPack";
            var old = GameObject.Find(packName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var pack = new GameObject(packName);
            pack.transform.position = new Vector3(0f, 0.7f, 6f);
            for (var i = 0; i < count; i++)
            {
                var ang = (i / (float)count) * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(ang) * radius * 0.35f, 0f, Mathf.Sin(ang) * radius * 0.35f);
                PlaceMite(pack.transform, pos, i + worldIndex);
            }
        }

        private static void PlaceBossLaneMitePack(int count, int worldIndex)
        {
            const string packName = "ART_EncounterPack_BossLane";
            var old = GameObject.Find(packName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var pack = new GameObject(packName);
            pack.transform.position = new Vector3(0f, 0.7f, 14.5f);
            for (var i = 0; i < count; i++)
            {
                var x = (i - (count - 1) * 0.5f) * 2.2f;
                PlaceMite(pack.transform, new Vector3(x, 0f, (i % 2) * 1.2f), 100 + worldIndex * 10 + i);
            }
        }

        private static void PlaceFloaterPack(int count, int worldIndex)
        {
            const string packName = "ART_EncounterPack_Floaters";
            var old = GameObject.Find(packName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var pack = new GameObject(packName);
            pack.transform.position = new Vector3(0f, 1.4f, 9f);
            for (var i = 0; i < count; i++)
            {
                var floater = new GameObject($"ART_ScrapFloater_{worldIndex}_{i}");
                floater.transform.SetParent(pack.transform, false);
                floater.transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * 2.4f, 0.2f, (i % 2) * 1.5f);
                OrganicScrapFloaterBuilder.Build(floater.transform, ApplyMatRuntime);
                var ai = floater.GetComponent<ScrapFloater>() ?? floater.AddComponent<ScrapFloater>();
                ai.Configure(floater.transform.position, 1.4f + i * 0.2f, 1.0f + i * 0.15f);
            }
        }

        private static void PlaceRollerPack(int count, int worldIndex)
        {
            const string packName = "ART_EncounterPack_Rollers";
            var old = GameObject.Find(packName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var pack = new GameObject(packName);
            pack.transform.position = new Vector3(3.5f, 0.2f, 11f);
            for (var i = 0; i < count; i++)
            {
                var roller = new GameObject($"ART_ScrapRoller_{worldIndex}_{i}");
                roller.transform.SetParent(pack.transform, false);
                roller.transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * 2.6f, 0f, (i % 2) * -1.4f);
                OrganicScrapRollerBuilder.Build(roller.transform, ApplyMatRuntime);
                var ai = roller.GetComponent<ScrapRoller>() ?? roller.AddComponent<ScrapRoller>();
                ai.Configure(roller.transform.position);
            }
        }

        private static void PlaceMite(Transform parent, Vector3 localPos, int seed)
        {
            var mite = new GameObject($"ART_ScrapMite_{seed}");
            mite.transform.SetParent(parent, false);
            mite.transform.localPosition = localPos;
            OrganicScrapMiteBuilder.Build(mite.transform, ApplyMatRuntime);
            var a = new GameObject("PatrolA");
            a.transform.SetParent(mite.transform, false);
            a.transform.localPosition = new Vector3(-1.6f, 0f, 0f);
            var b = new GameObject("PatrolB");
            b.transform.SetParent(mite.transform, false);
            b.transform.localPosition = new Vector3(1.6f, 0f, 0f);
            var ai = mite.AddComponent<ScrapMite>();
            var so = new SerializedObject(ai);
            so.FindProperty("pointA").objectReferenceValue = a.transform;
            so.FindProperty("pointB").objectReferenceValue = b.transform;
            so.FindProperty("moveSpeed").floatValue = 2.2f + (seed % 3) * 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSplatPresentation()
        {
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                return;
            }

            var motor = splat.GetComponent<SpaceAndCrafts.Player.SplatMotor>();
            var visual = splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (splat.GetComponent<SpaceAndCrafts.Player.SplatSkeletonAnimator>() == null && motor != null)
            {
                var sk = splat.AddComponent<SpaceAndCrafts.Player.SplatSkeletonAnimator>();
                sk.Bind(motor, visual != null ? visual : splat.transform);
            }

            if (splat.GetComponent<CharacterVoicePlayer>() == null && motor != null)
            {
                var voice = splat.AddComponent<CharacterVoicePlayer>();
                var so = new SerializedObject(voice);
                so.FindProperty("motor").objectReferenceValue = motor;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplyMatRuntime(GameObject go, string assetName, Color color, float smoothness, float metallic)
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
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}
#endif
