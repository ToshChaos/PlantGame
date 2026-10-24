using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>T23: PlantGame > Build Standalone (macOS). Title + Main only; output goes to the git-ignored Builds/ folder at the repo root.</summary>
    public static class StandaloneBuild
    {
        public const string OutDir = "../Builds/Overgrown-mac", AppName = "Overgrown.app";

        [MenuItem("PlantGame/Build Standalone (macOS)")]
        public static void BuildMac()
        {
            Directory.CreateDirectory(OutDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(OutDir, AppName),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            File.WriteAllText("Logs/build.txt", $"{s.result} size {s.totalSize / (1024 * 1024)} MB time {s.totalTime.TotalSeconds:0}s errors {s.totalErrors} warnings {s.totalWarnings} -> {s.outputPath}\n");
            if (s.result != BuildResult.Succeeded) Debug.LogError("[PlantGame] Build failed: " + s.result);
        }
    }
}
