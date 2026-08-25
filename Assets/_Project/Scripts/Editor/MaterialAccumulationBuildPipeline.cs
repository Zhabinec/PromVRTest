using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PromVR.MaterialAccumulation.Editor
{
    public static class MaterialAccumulationBuildPipeline
    {
        private const string BuildFolder = "Builds/Windows";
        private const string PlayerPath = BuildFolder + "/MaterialAccumulation.exe";
        private const string DemoScenePath = "Assets/_Project/Scenes/MaterialAccumulationDemo.unity";

        [MenuItem("Tools/Material Accumulation/Build Windows Development")]
        public static void BuildWindowsDevelopmentPlayer()
        {
            BuildWindowsPlayer(BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        [MenuItem("Tools/Material Accumulation/Build Windows Release")]
        public static void BuildWindowsReleasePlayer()
        {
            BuildWindowsPlayer(BuildOptions.CleanBuildCache);
        }

        private static void BuildWindowsPlayer(BuildOptions options)
        {
            MaterialAccumulationDemoBuilder.BuildDemoScene();
            Directory.CreateDirectory(BuildFolder);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { DemoScenePath },
                locationPathName = PlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Windows build failed with result {report.summary.result} and " +
                    $"{report.summary.totalErrors} errors.");
            }

            float sizeMegabytes = report.summary.totalSize / (1024f * 1024f);
            Debug.Log(
                $"Windows player built at {PlayerPath} " +
                $"({sizeMegabytes:F1} MB, {report.summary.totalTime.TotalSeconds:F1} s).");
        }
    }
}
