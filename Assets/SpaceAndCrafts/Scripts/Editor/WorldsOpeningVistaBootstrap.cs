#if UNITY_EDITOR
using SpaceAndCrafts.Cameras;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Wires WorldOpeningVista into worlds 02–10 (landmark / boss tease look).
    /// </summary>
    public static class WorldsOpeningVistaBootstrap
    {
        private static readonly (string scene, string toast)[] Jobs =
        {
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity", "Beeping Caves Kingdom — ping a line to a safe ledge!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity", "Magnet Marsh — pull junk, don't sink!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity", "Bubble Bazaar — float the stalls!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity", "Clockwork Crater — wind the bridges!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity", "Frosted Filament — melt a path!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity", "Echo Atelier — redirect the boom!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity", "Tide Tinker Yard — skim the spray!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity", "Nebula Nursery — plant star seeds!"),
            ("Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity", "Scrap Cathedral — raise the glass sail!"),
        };

        [MenuItem("Space & Crafts/Bootstrap Worlds 02-10 Opening Vistas")]
        public static void Bootstrap()
        {
            for (var i = 0; i < Jobs.Length; i++)
            {
                var job = Jobs[i];
                if (!System.IO.File.Exists(job.scene))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(job.scene, OpenSceneMode.Single);
                var systems = GameObject.Find("SYS_WorldSystems") ?? GameObject.Find("Systems");
                if (systems == null)
                {
                    systems = new GameObject("SYS_WorldSystems");
                }

                var vista = systems.GetComponent<WorldOpeningVista>() ?? systems.AddComponent<WorldOpeningVista>();
                var rig = Object.FindAnyObjectByType<PlatformingCameraRig>();
                var landmark = GameObject.Find("ART_WorldLandmark")
                               ?? GameObject.Find("ART_Landmark")
                               ?? GameObject.Find("ART_WorldBossExam");
                // Prefer any transform with Landmark in name.
                if (landmark == null)
                {
                    var all = Object.FindObjectsByType<Transform>();
                    for (var t = 0; t < all.Length; t++)
                    {
                        if (all[t].name.IndexOf("Landmark", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            landmark = all[t].gameObject;
                            break;
                        }
                    }
                }

                vista.Bind(rig, landmark != null ? landmark.transform : null, job.toast);
                var so = new SerializedObject(vista);
                so.FindProperty("cameraRig").objectReferenceValue = rig;
                so.FindProperty("lookTarget").objectReferenceValue = landmark != null ? landmark.transform : null;
                so.FindProperty("toast").stringValue = job.toast;
                so.FindProperty("duration").floatValue = 2.0f;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Space & Crafts] Opening vista wired: {job.scene}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Worlds 02–10 opening vistas complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif
