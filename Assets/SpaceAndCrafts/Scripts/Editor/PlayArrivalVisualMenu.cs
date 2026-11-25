#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Opens Arrival and enters Play Mode so you can see the current look.
    /// </summary>
    public static class PlayArrivalVisualMenu
    {
        private const string ArrivalScene = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string WorkshopScene = "Assets/SpaceAndCrafts/Scenes/Menus/WorkshopHub.unity";
        private const string HomeScene = "Assets/SpaceAndCrafts/Scenes/Menus/HomeMenu.unity";

        [MenuItem("Space & Crafts/Play Arrival (Visual)")]
        public static void PlayArrival()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!System.IO.File.Exists(ArrivalScene))
                {
                    Debug.LogError($"[Space & Crafts] Missing scene: {ArrivalScene}");
                    return;
                }

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            }

            EnterPlayModeWithGameView();
        }

        [MenuItem("Space & Crafts/Play Workshop Hub (Visual)")]
        public static void PlayWorkshop()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!System.IO.File.Exists(WorkshopScene))
                {
                    Debug.LogError($"[Space & Crafts] Missing scene: {WorkshopScene}");
                    return;
                }

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(WorkshopScene, OpenSceneMode.Single);
            }

            EnterPlayModeWithGameView();
        }

        [MenuItem("Space & Crafts/Play Home Menu (Visual)")]
        public static void PlayHome()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!System.IO.File.Exists(HomeScene))
                {
                    Debug.LogError($"[Space & Crafts] Missing scene: {HomeScene}");
                    return;
                }

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(HomeScene, OpenSceneMode.Single);
            }

            EnterPlayModeWithGameView();
        }

        private static void EnterPlayModeWithGameView()
        {
            // Switching scenes does not change Unity's selected dock tab. Bring the
            // rendered player view forward so the one-click play commands are visible.
            if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
            {
                Debug.LogWarning("[Space & Crafts] Could not focus the Game view automatically. Select the Game tab to see the running game.");
            }

            EditorApplication.isPlaying = true;
        }

        /// <summary>CLI: Unity.exe -projectPath ... -executeMethod SpaceAndCrafts.EditorTools.PlayArrivalVisualMenu.PlayArrivalFromCli</summary>
        public static void PlayArrivalFromCli()
        {
            if (!System.IO.File.Exists(ArrivalScene))
            {
                Debug.LogError($"[Space & Crafts] Missing scene: {ArrivalScene}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
            Debug.Log("[Space & Crafts] Arrival opened — entering Play Mode.");
        }
    }
}
#endif
