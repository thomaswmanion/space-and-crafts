#if UNITY_EDITOR
using SpaceAndCrafts.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Batchmode: run without -quit; exits the Editor with 0/1 when smoke finishes.
    /// </summary>
    public static class ArrivalPlayModeSmokeMenu
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string SessionKey = "SpaceAndCrafts.ArrivalPlayModeSmoke";
        private const string PassKey = "SpaceAndCrafts.ArrivalPlayModeSmoke.Pass";
        private const string DoneKey = "SpaceAndCrafts.ArrivalPlayModeSmoke.Done";
        private const string ReportKey = "SpaceAndCrafts.ArrivalPlayModeSmoke.Report";

        [MenuItem("Space & Crafts/Run Arrival PlayMode Smoke")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ArrivalSmoke] Already in play mode.");
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
            ArrivalPlayModeSmokeBridge.Finished -= OnSmokeFinished;
            ArrivalPlayModeSmokeBridge.Finished += OnSmokeFinished;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SessionKey, false))
            {
                var host = new GameObject("~ArrivalPlayModeSmoke");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<ArrivalPlayModeSmokeRunner>();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(DoneKey, false))
            {
                var pass = SessionState.GetBool(PassKey, false);
                var report = SessionState.GetString(ReportKey, string.Empty);
                SessionState.SetBool(DoneKey, false);
                SessionState.SetBool(SessionKey, false);
                if (!Application.isBatchMode)
                {
                    if (pass)
                    {
                        Debug.Log("[ArrivalSmoke] Interactive run finished; Unity remains open.\n" + report);
                    }
                    else
                    {
                        Debug.LogError("[ArrivalSmoke] Interactive run failed; Unity remains open.\n" + report);
                    }
                }
                else if (pass)
                {
                    Debug.Log("[ArrivalSmoke] Batch exit 0\n" + report);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[ArrivalSmoke] Batch exit 1\n" + report);
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
