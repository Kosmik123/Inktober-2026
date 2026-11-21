using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Inktober2026.Editor
{
    /// <summary>
    /// Builds the game from the command line (Unity batch mode), for example:
    /// Unity.exe -batchmode -nographics -quit -projectPath . -buildTarget WebGL -logFile build.log
    ///           -executeMethod Inktober2026.Editor.BuildScript.BuildWebGL -outputPath Builds/WebGL
    /// Exit code 0 on success, 1 on failure. Builds the scenes enabled in Build Settings.
    /// </summary>
    public static class BuildScript
    {
        private const string OutputPathArgument = "-outputPath";
        private const string DefaultWebGLPath = "Builds/WebGL";

        /// <summary>WebGL build. The output folder is taken from -outputPath (absolute, or relative to the project folder).</summary>
        public static void BuildWebGL()
        {
            Build(BuildTarget.WebGL, GetOutputPath(DefaultWebGLPath));
        }

        private static string GetOutputPath(string defaultPath)
        {
            var arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, OutputPathArgument);
            string path = index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : defaultPath;
            return Path.GetFullPath(path);
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("BuildScript: no enabled scenes in Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"BuildScript: building {target} to {outputPath} ({scenes.Length} scene(s)).");
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"BuildScript: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} error(s), {summary.totalTime}.");
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
