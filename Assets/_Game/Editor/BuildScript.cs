using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Duck.EditorTools
{
    /// <summary>
    /// One-call Android build. From the editor: Duck/Build Android APK.
    /// From the command line (editor closed):
    ///   Unity.exe -batchmode -quit -projectPath PATH -buildTarget Android
    ///             -executeMethod Duck.EditorTools.BuildScript.BuildAndroid -logFile build.log
    /// Output: Builds/Android/DuckDefense.apk
    /// </summary>
    public static class BuildScript
    {
        public const string ApkPath = "Builds/Android/DuckDefense.apk";

        [MenuItem("Duck/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!File.Exists(SceneBuilder.ScenePath))
                SceneBuilder.BuildGameScene();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            UseAsciiGradleHome();

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary sum = report.summary;
            Debug.Log($"[Duck] Android build: {sum.result}, {sum.totalSize / (1024f * 1024f):0.0} MB, {sum.totalTime.TotalSeconds:0}s -> {Path.GetFullPath(ApkPath)}");

            if (Application.isBatchMode)
                EditorApplication.Exit(sum.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// The Android prefab tool fails to load from a path with non-ASCII letters (error CXX1429), and the default
        /// Gradle cache sits in the user profile. In that case the cache goes next to the project folder instead.
        /// </summary>
        static void UseAsciiGradleHome()
        {
            const string variable = "GRADLE_USER_HOME";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable))) return;
            if (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).All(c => c < 128)) return;
            // The working directory of the editor is the project folder.
            Environment.SetEnvironmentVariable(variable, Path.GetFullPath("../.gradle"));
        }
    }
}
