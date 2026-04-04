namespace Scripts.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Profile;
    using UnityEditor.Build.Reporting;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public static class BuildAutomation
    {
        private const string LinuxBuildProfilePath = "Assets/Settings/Build Profiles/Linux.asset";

        public static void BuildLinuxWithCurrentProfile()
        {
            var buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(LinuxBuildProfilePath);

            if (buildProfile == null)
            {
                throw new InvalidOperationException($"Missing build profile at '{LinuxBuildProfilePath}'.");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();

            var buildName = ReadBuildName();
            var buildDirectory = Path.Combine("build", "StandaloneLinux64");
            var locationPathName = Path.Combine(buildDirectory, $"{buildName}.x86_64");

            Directory.CreateDirectory(buildDirectory);

            var options = new BuildPlayerWithProfileOptions
            {
                buildProfile = buildProfile,
                locationPathName = locationPathName,
                options = BuildOptions.None,
                assetBundleManifestPath = string.Empty,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var result = report.summary.result;

            Debug.Log($"Build result: {result} | {report.summary.totalErrors} errors | {report.summary.outputPath}");

            if (result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Linux build failed with result '{result}'.");
            }
        }

        private static string ReadBuildName()
        {
            var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

            if (string.IsNullOrWhiteSpace(branch))
            {
                branch = Environment.GetEnvironmentVariable("BUILD_NAME");
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                return "BovineLabs";
            }

            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var sanitized = branch.Replace('/', '-').Replace(' ', '-');

            foreach (var invalid in invalidFileNameChars)
            {
                sanitized = sanitized.Replace(invalid.ToString(), string.Empty, StringComparison.Ordinal);
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "BovineLabs" : sanitized;
        }
    }
}
