#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Produces a convenient windowed Windows build for a quick hands-on review.
    /// </summary>
    public static class BuildWindowsReviewPlayer
    {
        private const string OutputDirectory = "Builds/Windows";
        private const string OutputPath = OutputDirectory + "/SpaceAndCrafts.exe";

        [MenuItem("Space & Crafts/Build Windows Player (Windowed Review)")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputDirectory);

            // Use a normal, resizable window so the first run does not take over
            // the desktop or depend on an exclusive fullscreen mode.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            if (options.scenes.Length == 0)
            {
                Debug.LogError("[Space & Crafts] No enabled scenes are configured in Build Settings.");
                return;
            }

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Space & Crafts] Windows player built: {Path.GetFullPath(OutputPath)} " +
                          $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime}).");
            }
            else
            {
                Debug.LogError($"[Space & Crafts] Windows player build {summary.result}: " +
                               $"{summary.totalErrors} errors, {summary.totalWarnings} warnings.");
            }
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var enabled = new System.Collections.Generic.List<string>(scenes.Length);
            foreach (var scene in scenes)
            {
                if (scene.enabled && File.Exists(scene.path))
                {
                    enabled.Add(scene.path);
                }
                else if (scene.enabled)
                {
                    Debug.LogError("[Space & Crafts] Enabled build scene is missing: " + scene.path);
                }
            }

            return enabled.ToArray();
        }
    }
}
#endif
