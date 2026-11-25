#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Gives a freshly opened project a visible scene to play instead of an empty Game view.
    /// It preserves real or dirty recovered scenes, but replaces Unity's empty/default backup.
    /// </summary>
    [InitializeOnLoad]
    public static class GameStartSceneBootstrap
    {
        private const string StartScenePath = "Assets/SpaceAndCrafts/Scenes/Menus/HomeMenu.unity";
        private static bool startSceneOpenQueued;

        static GameStartSceneBootstrap()
        {
            QueueOpenStartSceneWhenEditorIsEmpty();
            // Unity can restore Temp/__Backupscenes after InitializeOnLoad has run.
            // Catch that later restore too; otherwise the editor can stay on an
            // empty camera/light backup and Play Mode has no game content to show.
            EditorSceneManager.sceneOpened += OnEditorSceneOpened;
            EditorApplication.playModeStateChanged += FocusGameViewWhenPlayStarts;
        }

        private static void OnEditorSceneOpened(Scene scene, OpenSceneMode _)
        {
            if (IsProjectRecoveryScene(scene) || IsEmptyUnityRecoveryBackup(scene))
            {
                QueueOpenStartSceneWhenEditorIsEmpty();
            }
        }

        private static void QueueOpenStartSceneWhenEditorIsEmpty()
        {
            if (startSceneOpenQueued)
            {
                return;
            }

            startSceneOpenQueued = true;
            EditorApplication.delayCall += () =>
            {
                startSceneOpenQueued = false;
                OpenStartSceneWhenEditorIsEmpty();
            };
        }

        private static void FocusGameViewWhenPlayStarts(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && !Application.isBatchMode)
            {
                // Unity can leave a new/untitled default scene active after the
                // project has already loaded. The one-time startup fallback will
                // not catch that case, so pressing the toolbar Play button would
                // run an empty scene. Route only the stock camera/light scene to
                // the real home menu before Play Mode starts.
                EnsurePlayableSceneBeforePlay();
                return;
            }

            // Unity may restore the previously selected editor tab while the Play
            // transition is in progress. Focusing Game during ExitingEditMode can
            // therefore be undone before the first frame is drawn.
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += FocusGameView;
            }
        }

        private static void EnsurePlayableSceneBeforePlay()
        {
            var activeScene = SceneManager.GetActiveScene();
            var isDefaultUntitledScene = activeScene.IsValid() &&
                                         string.IsNullOrEmpty(activeScene.path) &&
                                         IsDefaultCameraAndLightOnly(activeScene);
            var isEmptyRecoveryScene = IsEmptyUnityRecoveryBackup(activeScene);
            // An untouched Unity default scene is only a camera/light recovery
            // stub, so replacing it cannot discard game content. Asking Unity to
            // save it can leave Play Mode on that empty scene if the prompt is
            // cancelled or dismissed. Keep the save guard for every authored
            // scene, but route default-only scenes directly to Home Menu.
            if ((!isDefaultUntitledScene && !isEmptyRecoveryScene) && !CanReplaceActiveScene())
            {
                return;
            }

            if (LoadStartScene())
            {
                Debug.Log("[Space & Crafts] Replaced Unity's empty default scene with Home Menu before Play Mode.");
            }
        }

        private static void FocusGameView()
        {
            // The toolbar Play button does not change the selected dock tab. If the
            // editor is showing Scene, Console, or Project, Play can look like a
            // blank launch even though the game is running normally. Focus only
            // after Unity has fully entered Play Mode so the selection persists.
            if (!TryFocusGameView())
            {
                Debug.LogWarning("[Space & Crafts] Play Mode started, but Unity could not focus the Game view. Select the Game tab to see the running game.");
            }
        }

        private static bool TryFocusGameView()
        {
            // The menu command can report success while only selecting the dock
            // tab. Resolve and focus the actual window as well, so Play does not
            // look blank when Unity restores a different tab after the transition.
            var menuOpenedGameView = EditorApplication.ExecuteMenuItem("Window/General/Game");
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                return menuOpenedGameView;
            }

            var gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView == null)
            {
                return menuOpenedGameView;
            }

            gameView.Show();
            gameView.Focus();
            return true;
        }

        [MenuItem("Space & Crafts/Open Game Start Scene")]
        public static void OpenGameStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Space & Crafts] Exit Play Mode before opening the game start scene.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            var isEmptyRecoveryScene = activeScene.IsValid() && IsEmptyUnityRecoveryBackup(activeScene);
            if (!isEmptyRecoveryScene && !CanReplaceActiveScene())
            {
                return;
            }

            LoadStartScene();
        }

        [MenuItem("Space & Crafts/Play Game (Home Menu) %#r")]
        public static void PlayGameFromHomeMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            var isEmptyRecoveryScene = activeScene.IsValid() && IsEmptyUnityRecoveryBackup(activeScene);
            if ((!isEmptyRecoveryScene && !CanReplaceActiveScene()) || !LoadStartScene())
            {
                return;
            }

            // Unity keeps the previously selected dock tab during Play Mode. If that is
            // Scene/Console/Project, users can reasonably think the game did not start.
            // Bring the actual player view forward before entering Play Mode.
            if (!TryFocusGameView())
            {
                Debug.LogWarning("[Space & Crafts] Could not focus the Game view automatically. Select the Game tab to see the running game.");
            }

            // Scene loading is synchronous; Play Mode starts on the next editor update.
            EditorApplication.isPlaying = true;
        }

        private static bool CanReplaceActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            return !activeScene.IsValid() || !activeScene.isDirty ||
                   EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static void OpenStartSceneWhenEditorIsEmpty()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenStartSceneWhenEditorIsEmpty;
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            var isProjectRecoveryScene = IsProjectRecoveryScene(activeScene);
            // Unity's untitled default scene already contains a camera and light. Those
            // roots used to make the fallback mistake that scene for a real game scene.
            if (activeScene.IsValid() && activeScene.path.Length > 0 &&
                !isProjectRecoveryScene && !IsEmptyUnityRecoveryBackup(activeScene))
            {
                return;
            }

            // Do not discard work in an untitled scene if the user has edited it.
            var isEmptyRecoveryScene = activeScene.IsValid() && IsEmptyUnityRecoveryBackup(activeScene);
            if (!isEmptyRecoveryScene && !CanReplaceActiveScene())
            {
                return;
            }

            if (activeScene.IsValid() && activeScene.path.Length > 0)
            {
                Debug.Log(isProjectRecoveryScene
                    ? "[Space & Crafts] Unity opened a recovered scene from Assets/_Recovery; keeping that file intact and opening Home Menu for a playable start."
                    : "[Space & Crafts] Unity restored an empty default-only backup scene; opening Home Menu so Play Mode has visible game content.");
            }

            LoadStartScene();
        }

        private static bool IsProjectRecoveryScene(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                return false;
            }

            var path = scene.path.Replace('\\', '/');
            return path.StartsWith("Assets/_Recovery/", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEmptyUnityRecoveryBackup(Scene scene)
        {
            var path = scene.path.Replace('\\', '/');
            if (!path.StartsWith("Temp/__Backupscenes/", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Unity may restore a temp backup instead of the previously open scene after
            // a build/editor restart. Only treat a truly default-only backup as empty:
            // any authored root, renderer, UI, or custom component means preserve it.
            // A dirty default-only backup still needs recovery, but CanReplaceActiveScene()
            // will ask Unity whether the user wants to save or discard it before loading
            // HomeMenu. Rejecting it here made a dirty camera/light-only backup bypass the
            // fallback entirely and could leave the Game view empty when Play was pressed.
            var roots = scene.GetRootGameObjects();
            return IsDefaultCameraAndLightOnly(roots);
        }

        private static bool IsDefaultCameraAndLightOnly(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            return IsDefaultCameraAndLightOnly(scene.GetRootGameObjects());
        }

        private static bool IsDefaultCameraAndLightOnly(GameObject[] roots)
        {
            if (roots.Length == 0)
            {
                return true;
            }

            if (roots.Length > 2)
            {
                return false;
            }

            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null || (root.name != "Main Camera" && root.name != "Directional Light"))
                {
                    return false;
                }

                var components = root.GetComponentsInChildren<Component>(true);
                for (var j = 0; j < components.Length; j++)
                {
                    var component = components[j];
                    if (component == null || component is Transform || component is Camera ||
                        component is Light || component is AudioListener ||
                        component.GetType().Name == "FlareLayer" ||
                        component.GetType().Name == "UniversalAdditionalCameraData" ||
                        component.GetType().Name == "UniversalAdditionalLightData")
                    {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool LoadStartScene()
        {
            if (!System.IO.File.Exists(StartScenePath))
            {
                Debug.LogError("[Space & Crafts] Game start scene is missing: " + StartScenePath);
                return false;
            }

            var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[Space & Crafts] Could not load the game start scene: " + StartScenePath);
                return false;
            }

            Debug.Log("[Space & Crafts] Home Menu loaded. Press Play to run the game; use Space & Crafts > Play Arrival (Visual) to jump straight into World 1.");
            return true;
        }
    }
}
#endif
