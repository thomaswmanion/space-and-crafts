#if UNITY_EDITOR
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class Worlds02to10BossExamBootstrap
    {
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        private static readonly (string scene, string inventionId, string toast, int worldIndex)[] Jobs =
        {
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity", "vine_spooler", "The cave chimes answer — a safe passage opens!", 2),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity", "junk_magnet_gauntlet", "Marsh core sunk — gateway pulses!", 3),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity", "paint_bubble_raft", "Bazaar winds calm — rainbow opens!", 4),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity", "windup_bridge_bug", "Gears settle — crater rainbow wakes!", 5),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity", "heat_tape_torch", "Filament thawed — icy rainbow gleams!", 6),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity", "boom_dish_redirector", "Studio stills — sonic rainbow sings!", 7),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity", "vacuum_skimmer", "Tide yard clear — salt rainbow rises!", 8),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity", "star_seed_planter", "Nursery blooms — star rainbow unfolds!", 9),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity", "glass_sail_shield", "Cathedral bright — final rainbow opens!", 10),
        };

        [MenuItem("Space & Crafts/Bootstrap Worlds 02-10 Boss Exams")]
        public static void Bootstrap()
        {
            for (var i = 0; i < Jobs.Length; i++)
            {
                var job = Jobs[i];
                var scene = EditorSceneManager.OpenScene(job.scene);
                var boss = GameObject.Find("ART_WorldBossExam");
                if (boss == null)
                {
                    boss = new GameObject("ART_WorldBossExam");
                    boss.transform.position = new Vector3(0f, 1.5f, 16f);
                    var col = boss.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    col.radius = 1.6f;
                }

                RebuildBossBody(boss.transform, job.worldIndex, out var body, out var dangerRing);
                var exam = boss.GetComponent<WorldBossExam>() ?? boss.AddComponent<WorldBossExam>();
                var so = new SerializedObject(exam);
                so.FindProperty("requiredInventionId").stringValue = job.inventionId;
                so.FindProperty("hitsToClear").intValue = 3;
                so.FindProperty("clearToast").stringValue = job.toast;
                so.FindProperty("introToast").stringValue = $"World exam ahead — {job.inventionId.Replace('_', ' ')}!";
                so.FindProperty("bodyRoot").objectReferenceValue = body;
                so.FindProperty("dangerRing").objectReferenceValue = dangerRing;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Space & Crafts] Boss exam placed for {job.inventionId}");
            }

            Debug.Log("[Space & Crafts] Worlds 02–10 boss exams bootstrapped.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void RebuildBossBody(Transform bossRoot, int worldIndex, out Transform body, out Transform dangerRing)
        {
            for (var i = bossRoot.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bossRoot.GetChild(i).gameObject);
            }

            var bodyGo = new GameObject("ART_BossBody");
            bodyGo.transform.SetParent(bossRoot, false);
            body = bodyGo.transform;

            // Distinct SoftBlob + CrystalFacet + SpringHelix compositions per world.
            switch (worldIndex)
            {
                case 2:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.0f, 1.15f, 0.07f), Vector3.zero, Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.35f, 0.75f, 0.4f), 0.4f, 0.1f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(1.6f, 0.55f, 0.06f, 6, 28), new Vector3(0f, 0.2f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.25f, 0.55f, 0.3f), 0.45f, 0.05f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(0.9f, 0.22f, 6), new Vector3(0f, 1.1f, 0.2f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(1f, 0.7f, 0.25f), 0.2f, 0.05f);
                    break;
                case 3:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.15f, 0.7f, 0.06f), Vector3.zero, new Vector3(1.3f, 0.85f, 1.1f),
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.4f, 0.65f, 0.95f), 0.3f, 0.45f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(0.9f, 0.7f, 0.05f, 4, 24), new Vector3(0f, 0.6f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.7f, 0.85f, 1f), 0.2f, 0.65f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(0.7f, 0.28f, 5), new Vector3(0f, -0.2f, 0.4f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.85f, 0.35f, 0.35f), 0.25f, 0.55f);
                    break;
                case 4:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.25f, 0.95f, 0.09f), Vector3.zero, Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Body", new Color(1f, 0.5f, 0.75f), 0.15f, 0.05f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(0.55f, 0.18f, 7), new Vector3(-0.55f, 0.7f, 0.2f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_CrystalA", new Color(0.55f, 0.85f, 1f), 0.15f, 0.05f);
                    Place(body, "crystalB", OrganicMeshFactory.CrystalFacet(0.45f, 0.15f, 6), new Vector3(0.55f, 0.55f, 0.15f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_CrystalB", new Color(0.95f, 0.85f, 0.35f), 0.15f, 0.05f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(0.6f, 0.9f, 0.04f, 3, 20), new Vector3(0f, -0.35f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.95f, 0.7f, 0.4f), 0.3f, 0.15f);
                    break;
                case 5:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(3, 0.85f, 0.75f, 0.04f), Vector3.zero, new Vector3(1.4f, 0.8f, 1.1f),
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.9f, 0.65f, 0.3f), 0.35f, 0.4f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(1.2f, 0.4f, 0.055f, 7, 28), new Vector3(0f, 0.5f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.75f, 0.75f, 0.8f), 0.25f, 0.7f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(1.1f, 0.2f, 8), new Vector3(0.5f, 0.3f, 0.3f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.95f, 0.8f, 0.4f), 0.3f, 0.5f);
                    break;
                case 6:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.05f, 0.6f, 0.05f), Vector3.zero, new Vector3(0.9f, 1.4f, 0.9f),
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.7f, 0.9f, 1f), 0.15f, 0.2f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(2.0f, 0.35f, 0.04f, 8, 28), new Vector3(0f, 0.1f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(1f, 0.55f, 0.3f), 0.25f, 0.15f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(1.3f, 0.16f, 6), new Vector3(0f, 1.2f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.85f, 0.95f, 1f), 0.12f, 0.1f);
                    break;
                case 7:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 0.95f, 0.85f, 0.06f), new Vector3(0f, 0.2f, -0.2f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.75f, 0.45f, 0.95f), 0.25f, 0.3f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(0.8f, 0.65f, 0.05f, 5, 24), new Vector3(0f, 0.7f, 0.3f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.55f, 0.35f, 0.8f), 0.3f, 0.4f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(1.0f, 0.3f, 7), new Vector3(0f, 0.4f, 0.7f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.95f, 0.55f, 1f), 0.15f, 0.05f);
                    break;
                case 8:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.2f, 0.55f, 0.05f), Vector3.zero, new Vector3(1.6f, 0.7f, 1.1f),
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.3f, 0.7f, 0.8f), 0.35f, 0.25f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(1.0f, 0.5f, 0.05f, 4, 24), new Vector3(0f, 0.45f, -0.3f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.45f, 0.55f, 0.6f), 0.3f, 0.5f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(0.8f, 0.22f, 5), new Vector3(0.6f, 0.2f, 0.4f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.6f, 0.9f, 0.85f), 0.2f, 0.35f);
                    break;
                case 9:
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 0.9f, 1.05f, 0.08f), Vector3.zero, Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.5f, 0.4f, 0.95f), 0.2f, 0.1f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(1.4f, 0.45f, 0.045f, 5, 24), new Vector3(0f, 0.6f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.7f, 0.6f, 1f), 0.25f, 0.15f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(0.75f, 0.2f, 8), new Vector3(0f, 1.25f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(0.75f, 0.95f, 1f), 0.12f, 0.08f);
                    Place(body, "seed", OrganicMeshFactory.SoftBlob(3, 0.25f, 1f, 0.04f), new Vector3(0.55f, 0.4f, 0.4f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Seed", new Color(0.9f, 0.85f, 1f), 0.15f, 0.05f);
                    break;
                default: // 10
                    Place(body, "body", OrganicMeshFactory.SoftBlob(4, 1.1f, 0.9f, 0.05f), Vector3.zero, Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Body", new Color(0.95f, 0.8f, 0.45f), 0.3f, 0.35f);
                    Place(body, "helix", OrganicMeshFactory.SpringHelix(1.5f, 0.6f, 0.05f, 3, 22), new Vector3(0f, 0.3f, 0f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Helix", new Color(0.85f, 0.7f, 0.35f), 0.3f, 0.5f);
                    Place(body, "crystal", OrganicMeshFactory.CrystalFacet(1.2f, 0.28f, 8), new Vector3(0f, 0.9f, 0.25f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_Crystal", new Color(1f, 0.4f, 0.45f), 0.12f, 0.05f);
                    Place(body, "crystalB", OrganicMeshFactory.CrystalFacet(0.7f, 0.18f, 6), new Vector3(-0.5f, 0.5f, 0.35f), Vector3.one,
                        $"Mat_Boss_W{worldIndex}_CrystalB", new Color(0.4f, 0.7f, 1f), 0.12f, 0.05f);
                    break;
            }

            var telegraph = new GameObject("ART_BossTelegraph");
            telegraph.transform.SetParent(bossRoot, false);
            telegraph.transform.localPosition = new Vector3(0f, -1.35f, 0f);
            telegraph.SetActive(false);
            dangerRing = telegraph.transform;

            Place(dangerRing, "ring", OrganicMeshFactory.Torus(0.5f, 0.045f, 56, 14), Vector3.zero, Vector3.one,
                $"Mat_Boss_W{worldIndex}_DangerRing", new Color(1f, 0.3f, 0.2f, 0.7f), 0.2f, 0.05f);
            Place(dangerRing, "impact", OrganicMeshFactory.SoftBlob(3, 0.45f, 0.12f, 0.02f), new Vector3(0f, -0.02f, 0f), new Vector3(1f, 0.35f, 1f),
                $"Mat_Boss_W{worldIndex}_Impact", new Color(1f, 0.5f, 0.2f, 0.45f), 0.25f, 0.1f);
        }

        private static void Place(
            Transform parent,
            string name,
            Mesh mesh,
            Vector3 localPos,
            Vector3 localScale,
            string matName,
            Color color,
            float smoothness,
            float metallic)
        {
            var go = OrganicMeshFactory.CreateMeshObject(name, mesh, parent, localPos, localScale, null);
            ApplyMat(go, matName, color, smoothness, metallic);
        }

        private static void ApplyMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            if (!AssetDatabase.IsValidFolder(MatPath))
            {
                AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Materials", "TempArt");
            }

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

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            EditorUtility.SetDirty(mat);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
            }
        }
    }
}
#endif
