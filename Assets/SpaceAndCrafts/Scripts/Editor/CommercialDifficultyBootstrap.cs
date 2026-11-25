#if UNITY_EDITOR
using SpaceAndCrafts.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Escalating craft→boss difficulty across worlds (was flat 3 parts / 3 hits).
    /// Arrival stays tutorial-easy; later worlds demand more hits / denser exams.
    /// </summary>
    public static class CommercialDifficultyBootstrap
    {
        [MenuItem("Space & Crafts/Bootstrap Commercial Difficulty Curve")]
        public static void Bootstrap()
        {
            SetArrivalBossHits(3);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity", 3);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity", 3);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity", 4);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity", 4);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity", 4);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity", 5);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity", 5);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity", 5);
            SetWorldBossHits("Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity", 6);

            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Commercial difficulty curve applied (boss hits escalate 3→6).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void SetArrivalBossHits(int hits)
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity",
                OpenSceneMode.Single);
            var boss = Object.FindAnyObjectByType<ScrapStormSentinel>();
            if (boss != null)
            {
                var so = new SerializedObject(boss);
                so.FindProperty("hitsToDefeat").intValue = hits;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void SetWorldBossHits(string scenePath, int hits)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var exam = Object.FindAnyObjectByType<WorldBossExam>();
            if (exam == null)
            {
                return;
            }

            var so = new SerializedObject(exam);
            so.FindProperty("hitsToClear").intValue = hits;
            // Slightly longer telegraph on harder worlds.
            if (so.FindProperty("telegraphDuration") != null)
            {
                so.FindProperty("telegraphDuration").floatValue = Mathf.Lerp(0.85f, 1.25f, (hits - 3) / 3f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Space & Crafts] Boss hits={hits}: {scenePath}");
        }
    }
}
#endif
