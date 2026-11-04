using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantGame.Editor
{
    /// <summary>
    /// T69: iOS settings and builds. Menu: PlantGame > Configure iOS, Build iOS (Device), Build iOS (Simulator).
    /// CLI: Unity -batchmode -executeMethod PlantGame.Editor.IosBuild.BuildDevice (or BuildSimulator). Output: git-ignored Builds/Overgrown-ios/
    /// (an Xcode project), log Logs/build-ios.txt. Needs the "iOS Build Support" module and a full Xcode.
    /// Environment, nothing in the repo: OVERGROWN_APPLE_TEAM (team id, automatic signing), OVERGROWN_BUILD_NUMBER (grows with every upload).
    /// </summary>
    public static class IosBuild
    {
        const string OutDir = "../Builds/Overgrown-ios";

        [MenuItem("PlantGame/Configure iOS", false, 120)]
        public static void Configure()
        {
            var ios = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(ios, "com.overgrown.game");
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(ios, 1);   // ARM64
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            // T70: a look drag or hotbar tap near the bottom edge must not trigger the home-indicator swipe at once (the first swipe is deferred, a second one leaves)
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.BottomEdge;

            // landscape only, both directions
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            var team = System.Environment.GetEnvironmentVariable("OVERGROWN_APPLE_TEAM");
            if (!string.IsNullOrEmpty(team))
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                PlayerSettings.iOS.appleDeveloperTeamID = team;
            }
            var bn = System.Environment.GetEnvironmentVariable("OVERGROWN_BUILD_NUMBER");
            if (!string.IsNullOrEmpty(bn)) PlayerSettings.iOS.buildNumber = bn;
            UnityEngine.Debug.Log("[PlantGame] iOS player settings applied (IL2CPP, ARM64, Metal, iPhone + iPad, landscape only, team " + (string.IsNullOrEmpty(team) ? "not set" : "set") + ").");
        }

        [MenuItem("PlantGame/Build iOS (Device)", false, 121)]
        public static void BuildDevice() => Build(false);

        [MenuItem("PlantGame/Build iOS (Simulator)", false, 122)]
        public static void BuildSimulator() => Build(true);

        /// <summary>
        /// T73: Info.plist after the Xcode export. ITSAppUsesNonExemptEncryption = false (the game uses no encryption beyond the exempt system kind and makes no
        /// network calls), so each TestFlight / App Store upload skips the export-compliance question. Uses plutil (the export only happens on a Mac).
        /// </summary>
        [PostProcessBuild(100)]
        public static void PatchInfoPlist(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plist = Path.Combine(path, "Info.plist");
            if (!File.Exists(plist)) { UnityEngine.Debug.LogError("[PlantGame] Info.plist not found in " + path); return; }
            var psi = new ProcessStartInfo("/usr/bin/plutil", $"-replace ITSAppUsesNonExemptEncryption -bool NO \"{plist}\"") { UseShellExecute = false, RedirectStandardError = true };
            using var proc = Process.Start(psi);
            proc.WaitForExit();
            if (proc.ExitCode != 0) UnityEngine.Debug.LogError("[PlantGame] plutil failed: " + proc.StandardError.ReadToEnd());
        }

        static void Build(bool simulator)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                const string msg = "iOS Build Support is not installed for this Unity version. Install it in Unity Hub (Installs > Add modules > iOS Build Support) and restart Unity.";
                UnityEngine.Debug.LogError("[PlantGame] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("iOS build", msg, "OK");
                return;
            }
            Configure();
            EditorUserBuildSettings.development = false;   // the editor's own flag leaked a Development Console into the Simulator build
            EditorUserBuildSettings.connectProfiler = false;
            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            var dir = OutDir + (simulator ? "-sim" : "");
            // A build into an existing folder appends and keeps the old player libraries (an earlier Development build leaked into later ones): start clean.
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/Main.unity" },
                locationPathName = dir,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            var s = BuildPipeline.BuildPlayer(opts).summary;
            File.WriteAllText("Logs/build-ios.txt", $"{s.result} {(simulator ? "simulator" : "device")} size {s.totalSize / (1024 * 1024)} MB time {s.totalTime.TotalSeconds:0}s errors {s.totalErrors} warnings {s.totalWarnings} -> {s.outputPath}\n");
            if (s.result != BuildResult.Succeeded || !File.Exists(Path.Combine(dir, "Unity-iPhone.xcodeproj/project.pbxproj")))
                UnityEngine.Debug.LogError("[PlantGame] iOS build failed: " + s.result);
            else UnityEngine.Debug.Log("[PlantGame] iOS Xcode project written: " + s.outputPath);
        }
    }
}
