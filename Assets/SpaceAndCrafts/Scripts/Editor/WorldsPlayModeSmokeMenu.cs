#if UNITY_EDITOR
using System.Collections.Generic;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Batchable PlayMode load+component smoke for Worlds 02–10.
    /// Lighter than ArrivalPlayModeSmoke: opens each world, enters playmode briefly, and asserts
    /// Splat + craft/boss basics (not a full collect/craft/boss loop).
    /// Batchmode: run without -quit; exits the Editor with 0/1 when smoke finishes.
    /// Optional CLI: -worldSmoke N (single world 2–10).
    /// </summary>
    public static class WorldsPlayModeSmokeMenu
    {
        private const string CatalogPath = "Assets/SpaceAndCrafts/ScriptableObjects/Worlds/WorldCatalog.asset";
        private const string SessionKey = "SpaceAndCrafts.WorldsPlayModeSmoke";
        private const string PassKey = "SpaceAndCrafts.WorldsPlayModeSmoke.Pass";
        private const string DoneKey = "SpaceAndCrafts.WorldsPlayModeSmoke.Done";
        private const string ReportKey = "SpaceAndCrafts.WorldsPlayModeSmoke.Report";
        private const string QueueKey = "SpaceAndCrafts.WorldsPlayModeSmoke.Queue";
        private const string IndexKey = "SpaceAndCrafts.WorldsPlayModeSmoke.Index";
        private const string AllPassKey = "SpaceAndCrafts.WorldsPlayModeSmoke.AllPass";
        private const string AwaitingNextKey = "SpaceAndCrafts.WorldsPlayModeSmoke.AwaitingNext";

        private static readonly string[] KnownScenePaths =
        {
            "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World04_BubbleBazaar.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World05_ClockworkCrater.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World06_FrostedFilament.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World07_EchoAtelier.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World08_TideTinkerYard.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World09_NebulaNursery.unity",
            "Assets/SpaceAndCrafts/Scenes/Worlds/World10_ScrapCathedral.unity",
        };

        [MenuItem("Space & Crafts/Run Worlds 02-10 PlayMode Smoke")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[WorldSmoke] Already in play mode.");
                return;
            }

            var queue = ResolveQueueFromArgs();
            if (queue.Count == 0)
            {
                Debug.LogError("[WorldSmoke] No worlds to smoke.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            SessionState.SetString(QueueKey, string.Join(",", queue));
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetBool(AllPassKey, true);
            SessionState.SetString(ReportKey, string.Empty);
            SessionState.SetBool(DoneKey, false);
            SessionState.SetBool(AwaitingNextKey, false);
            SessionState.SetBool(PassKey, false);
            SessionState.SetBool(SessionKey, true);

            Debug.Log($"[WorldSmoke] Starting queue: {string.Join(",", queue)}");
            StartWorldAtQueueIndex(0);
        }

        [InitializeOnLoadMethod]
        private static void HookPlayMode()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            WorldsPlayModeSmokeBridge.Finished -= OnSmokeFinished;
            WorldsPlayModeSmokeBridge.Finished += OnSmokeFinished;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SessionKey, false))
            {
                var queue = ParseQueue();
                var qi = SessionState.GetInt(IndexKey, 0);
                var worldIndex = qi >= 0 && qi < queue.Count ? queue[qi] : 2;
                var host = new GameObject("~WorldsPlayModeSmoke");
                Object.DontDestroyOnLoad(host);
                var runner = host.AddComponent<WorldsPlayModeSmokeRunner>();
                runner.WorldIndex = worldIndex;
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            if (SessionState.GetBool(AwaitingNextKey, false))
            {
                SessionState.SetBool(AwaitingNextKey, false);
                var next = SessionState.GetInt(IndexKey, 0) + 1;
                SessionState.SetInt(IndexKey, next);
                var queue = ParseQueue();
                if (next < queue.Count)
                {
                    StartWorldAtQueueIndex(next);
                    return;
                }

                FinalizeBatch();
                return;
            }

            if (SessionState.GetBool(DoneKey, false))
            {
                FinalizeBatch();
            }
        }

        private static void OnSmokeFinished(bool pass, int worldIndex, string report)
        {
            var aggregate = SessionState.GetString(ReportKey, string.Empty);
            var line = pass
                ? $"[WorldSmoke] PASS World {worldIndex:00}"
                : $"[WorldSmoke] FAIL World {worldIndex:00}";
            aggregate += line + "\n" + (report ?? string.Empty) + "\n";
            SessionState.SetString(ReportKey, aggregate);

            if (!pass)
            {
                SessionState.SetBool(AllPassKey, false);
            }

            SessionState.SetBool(PassKey, pass);
            SessionState.SetBool(SessionKey, false);

            var queue = ParseQueue();
            var qi = SessionState.GetInt(IndexKey, 0);
            var hasMore = qi + 1 < queue.Count;
            if (hasMore)
            {
                SessionState.SetBool(AwaitingNextKey, true);
            }
            else
            {
                SessionState.SetBool(DoneKey, true);
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void StartWorldAtQueueIndex(int queueIndex)
        {
            var queue = ParseQueue();
            if (queueIndex < 0 || queueIndex >= queue.Count)
            {
                FinalizeBatch();
                return;
            }

            var worldIndex = queue[queueIndex];
            var scenePath = ResolveScenePath(worldIndex);
            if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[WorldSmoke] FAIL World {worldIndex:00}: missing scene ({scenePath ?? "null"})");
                var aggregate = SessionState.GetString(ReportKey, string.Empty);
                aggregate += $"[WorldSmoke] FAIL World {worldIndex:00}\nFAIL: missing scene\n";
                SessionState.SetString(ReportKey, aggregate);
                SessionState.SetBool(AllPassKey, false);

                var next = queueIndex + 1;
                SessionState.SetInt(IndexKey, next);
                if (next < queue.Count)
                {
                    StartWorldAtQueueIndex(next);
                }
                else
                {
                    FinalizeBatch();
                }

                return;
            }

            Debug.Log($"[WorldSmoke] Opening World {worldIndex:00}: {scenePath}");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void FinalizeBatch()
        {
            var allPass = SessionState.GetBool(AllPassKey, false);
            var report = SessionState.GetString(ReportKey, string.Empty);
            SessionState.SetBool(DoneKey, false);
            SessionState.SetBool(SessionKey, false);
            SessionState.SetBool(AwaitingNextKey, false);

            if (allPass)
            {
                Debug.Log("[WorldSmoke] ALL PASSED\n" + report);
                if (Application.isBatchMode)
                {
                    Debug.Log("[WorldSmoke] Batch exit 0");
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                Debug.LogError("[WorldSmoke] FAILED\n" + report);
                if (Application.isBatchMode)
                {
                    Debug.LogError("[WorldSmoke] Batch exit 1");
                    EditorApplication.Exit(1);
                }
            }
        }

        private static List<int> ResolveQueueFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == "-worldSmoke" && i + 1 < args.Length && int.TryParse(args[i + 1], out var n))
                {
                    return ClampSingle(n);
                }

                if (arg.StartsWith("-worldSmoke=") && int.TryParse(arg.Substring("-worldSmoke=".Length), out n))
                {
                    return ClampSingle(n);
                }
            }

            var all = new List<int>(9);
            for (var w = 2; w <= 10; w++)
            {
                all.Add(w);
            }

            return all;
        }

        private static List<int> ClampSingle(int n)
        {
            if (n < 2 || n > 10)
            {
                Debug.LogError($"[WorldSmoke] -worldSmoke expects 2–10, got {n}");
                return new List<int>();
            }

            return new List<int> { n };
        }

        private static List<int> ParseQueue()
        {
            var raw = SessionState.GetString(QueueKey, string.Empty);
            var list = new List<int>();
            if (string.IsNullOrEmpty(raw))
            {
                return list;
            }

            var parts = raw.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out var n))
                {
                    list.Add(n);
                }
            }

            return list;
        }

        private static string ResolveScenePath(int worldIndex)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);
            if (catalog != null)
            {
                var def = catalog.GetByIndex(worldIndex);
                if (def != null && !string.IsNullOrEmpty(def.scenePath))
                {
                    return def.scenePath;
                }
            }

            var i = worldIndex - 1;
            return i >= 0 && i < KnownScenePaths.Length ? KnownScenePaths[i] : null;
        }
    }
}
#endif
