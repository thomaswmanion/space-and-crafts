#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Scaffolds Worlds 03–10 scenes with organic mesh floors, invention pedestal, and Splat spawn.
    /// Systems loops still incomplete; visuals start above primitive stacks.
    /// </summary>
    public static class Worlds03to10ScaffoldBootstrap
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
        private const string DataPath = "Assets/SpaceAndCrafts/ScriptableObjects";
        private const string MatPath = "Assets/SpaceAndCrafts/Materials/TempArt";

        [MenuItem("Space & Crafts/Bootstrap Worlds 03-10 Scaffolds")]
        public static void Bootstrap()
        {
            var catalog = WorldCatalogBootstrap.SeedCatalog();
            EnsureFolder("Assets/SpaceAndCrafts/Scenes/Worlds");

            // Snapshot paths/meta before NewScene unloads SO refs from memory.
            var jobs = new System.Collections.Generic.List<(int index, string id, string display, string inventionId, string inventionName, string scenePath, Color accent)>();
            for (var i = 3; i <= 10; i++)
            {
                var def = catalog.GetByIndex(i);
                if (def == null)
                {
                    continue;
                }

                jobs.Add((def.worldIndex, def.worldId, def.displayName, def.inventionId, def.inventionDisplayName, def.scenePath, def.accentColor));
            }

            for (var j = 0; j < jobs.Count; j++)
            {
                var job = jobs[j];
                BuildWorldScene(job.index, job.id, job.display, job.inventionId, job.inventionName, job.scenePath, job.accent);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Worlds 03–10 scaffolds built ({jobs.Count} scenes).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void BuildWorldScene(
            int worldIndex,
            string worldId,
            string displayName,
            string inventionId,
            string inventionDisplayName,
            string scenePath,
            Color accentColor)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject($"ART_{worldId}");
            var floor = OrganicMeshFactory.CreateMeshObject(
                "ART_WorldFloor",
                OrganicMeshFactory.SoftBlob(2, 1f, 0.18f, 0.03f),
                root.transform,
                Vector3.zero,
                new Vector3(12f, 1f, 12f),
                null);
            ApplyMat(floor, $"Mat_{worldId}_Floor", accentColor, 0.45f, 0.08f);
            floor.AddComponent<BoxCollider>().size = Vector3.one;

            var landmark = new GameObject("ART_WorldLandmark");
            landmark.transform.SetParent(root.transform, false);
            landmark.transform.localPosition = new Vector3(0f, 0.2f, 10f);
            OrganicLandmarkBuilders.BuildForWorld(worldIndex, landmark.transform, accentColor, ApplyMat);

            var invention = new GameObject(InventionObjectName(worldIndex, inventionId));
            invention.transform.SetParent(root.transform, false);
            invention.transform.localPosition = new Vector3(0f, 1f, 4f);
            OrganicInventionBuilders.BuildForWorld(worldIndex, invention.transform, ApplyMat);

            var systems = new GameObject("WorldSystems");
            systems.AddComponent<LevelSession>();
            systems.AddComponent<LevelFlowController>();
            systems.AddComponent<GameplayToastHud>();
            systems.AddComponent<AudioSource>();
            systems.AddComponent<TempSfxPlayer>();

            var splatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject splat;
            if (splatPrefab != null)
            {
                splat = (GameObject)PrefabUtility.InstantiatePrefab(splatPrefab);
                PrefabUtility.UnpackPrefabInstance(splat, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                splat = new GameObject("Splat_TEMP");
                OrganicSplatBuilder.BuildVisualRoot(splat.transform, ApplyMat);
                splat.AddComponent<CharacterController>();
                splat.AddComponent<SplatInputReader>();
                splat.AddComponent<SplatMotor>();
                splat.AddComponent<InventoryComponent>();
            }

            splat.name = "Splat_TEMP";
            splat.transform.position = new Vector3(0f, 1.2f, -2f);
            if (splat.GetComponent<PlayerRespawn>() == null)
            {
                splat.AddComponent<PlayerRespawn>();
            }

            var motorSettings = AssetDatabase.LoadAssetAtPath<SplatMotorSettings>($"{DataPath}/SplatMotorSettings.asset");
            var motor = splat.GetComponent<SplatMotor>();
            if (motor != null && motorSettings != null)
            {
                var so = new SerializedObject(motor);
                so.FindProperty("settings").objectReferenceValue = motorSettings;
                so.FindProperty("input").objectReferenceValue = splat.GetComponent<SplatInputReader>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var banner = new GameObject("TEMP_ART_BANNER");
            banner.transform.position = new Vector3(0f, 3f, -4f);
            var fantasy = OrganicLandmarkBuilders.FantasyLabel(worldIndex);
            banner.AddComponent<TempArtBanner>().SetLabel(
                $"TEMP ART — {displayName} organic scaffold ({fantasy} + {inventionDisplayName}); replaceable by commissioned finals");

            WorldsLightingMoodBootstrap.ApplyLightingMood(worldIndex);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Space & Crafts] Scaffolded {displayName} (world {worldIndex})");
        }

        private static string InventionObjectName(int worldIndex, string inventionId)
        {
            switch (worldIndex)
            {
                case 3: return "ART_JunkMagnetGauntlet";
                case 4: return "ART_PaintBubbleRaft";
                case 5: return "ART_WindUpBridgeBug";
                case 6: return "ART_HeatTapeTorch";
                case 7: return "ART_BoomDishRedirector";
                case 8: return "ART_VacuumSkimmer";
                case 9: return "ART_StarSeedPlanter";
                case 10: return "ART_GlassSailShield";
                default: return $"ART_{inventionId}";
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
