using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class BuildWebGL
    {
        private const string HomeScene = "Assets/Scenes/Home.unity";
        private const string WorldScene = "Assets/Scenes/World01_Test.unity";

        [MenuItem("Space & Crafts/Build WebGL")]
        public static void BuildDevelopment()
        {
            EnsureScenesExist();

            const string outputPath = "Builds/WebGL";
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { HomeScene, WorldScene },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"WebGL build failed: {report.summary.result} with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"WebGL build succeeded at {outputPath} ({report.summary.totalSize} bytes).");
        }

        private static void EnsureScenesExist()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool homeExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScene) != null;
            bool worldExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldScene) != null;
            if (!homeExists || !worldExists)
            {
                Debug.Log("One or more build scenes are missing. Generating Space & Crafts scenes before building.");
                CreatePrototypeScene.Create();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScene) == null)
            {
                throw new FileNotFoundException($"Unity scene generation did not create {HomeScene}.");
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldScene) == null)
            {
                throw new FileNotFoundException($"Unity scene generation did not create {WorldScene}.");
            }
        }
    }
}
