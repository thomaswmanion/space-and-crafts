#if UNITY_EDITOR
using SpaceAndCrafts.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Batchmode: run without -quit; exits the Editor with 0/1 when World02 smoke finishes.
    /// </summary>
    public static class World02PlayModeSmokeMenu
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string SessionKey = "SpaceAndCrafts.World02PlayModeSmoke";
        private const string PassKey = "SpaceAndCrafts.World02PlayModeSmoke.Pass";
        private const string DoneKey = "SpaceAndCrafts.World02PlayModeSmoke.Done";
        private const string ReportKey = "SpaceAndCrafts.World02PlayModeSmoke.Report";

        [MenuItem("Space & Crafts/Run World 02 PlayMode Smoke")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[World02Smoke] Already in play mode.");
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[World02Smoke] Missing scene: {ScenePath}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            SessionState.SetBool(SessionKey, true);
            SessionState.SetBool(DoneKey, false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void HookPlayMode()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            World02PlayModeSmokeBridge.Finished -= OnSmokeFinished;
            World02PlayModeSmokeBridge.Finished += OnSmokeFinished;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SessionKey, false))
            {
                var host = new GameObject("~World02PlayModeSmoke");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<World02PlayModeSmokeRunner>();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(DoneKey, false))
            {
                var pass = SessionState.GetBool(PassKey, false);
                var report = SessionState.GetString(ReportKey, string.Empty);
                SessionState.SetBool(DoneKey, false);
                SessionState.SetBool(SessionKey, false);
                if (pass)
                {
                    Debug.Log("[World02Smoke] Batch exit 0\n" + report);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[World02Smoke] Batch exit 1\n" + report);
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void OnSmokeFinished(bool pass, string report)
        {
            SessionState.SetBool(PassKey, pass);
            SessionState.SetString(ReportKey, report ?? string.Empty);
            SessionState.SetBool(DoneKey, true);
            SessionState.SetBool(SessionKey, false);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
#endif
