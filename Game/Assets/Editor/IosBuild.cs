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
            // Only when needed: setting these logs "empty graphics device types list" even with Metal already set.
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS) || apis.Length != 1 || apis[0] != GraphicsDeviceType.Metal)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            }
            // T72: size and startup. APP_UI_EDITOR_ONLY compiles com.unity.dt.app-ui (editor tooling) out of the player, as on Android and Mac; without it the player
            // creates AppUI's manager object at launch. Medium stripping removes the other unused package and engine code (no reflection in Assets/Scripts).
            var defines = PlayerSettings.GetScriptingDefineSymbols(ios);
            if (!defines.Contains("APP_UI_EDITOR_ONLY")) PlayerSettings.SetScriptingDefineSymbols(ios, string.IsNullOrEmpty(defines) ? "APP_UI_EDITOR_ONLY" : defines + ";APP_UI_EDITOR_ONLY");
            PlayerSettings.SetManagedStrippingLevel(ios, ManagedStrippingLevel.Medium);
            // Battery and honest numbers: nothing reads the accelerometer (it polled at 60 Hz), and Metal API validation slows every run started from Xcode, which would skew T72's profile.
            PlayerSettings.accelerometerFrequency = 0;
            PlayerSettings.enableMetalAPIValidation = false;
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

        /// <summary>
        /// T72: quiets warnings in Unity's own generated Xcode project, so that a warning in the log means something. None of them can be fixed from the game:
        /// - IL2CPP hard-codes an iOS 11 compile target for GameAssembly (the app itself is 15.0), and libc++ warns once per source file (500 times);
        /// - the IL2CPP script phase has no outputs because it must run on every build, so it says so (alwaysOutOfDate);
        /// - Unity's trampoline (Classes/) uses its own deprecated members.
        /// </summary>
        [PostProcessBuild(101)]
        public static void PatchXcodeProject(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var file = Path.Combine(path, "Unity-iPhone.xcodeproj/project.pbxproj");
            if (!File.Exists(file)) { UnityEngine.Debug.LogError("[PlantGame] project.pbxproj not found in " + path); return; }
            var text = File.ReadAllText(file);
            var patched = text
                .Replace("--compile-cpp\\n", "--compile-cpp\\n    --compiler-flags=-Wno-#warnings\\n")
                .Replace("isa = PBXShellScriptBuildPhase;", "isa = PBXShellScriptBuildPhase;\n\t\t\talwaysOutOfDate = 1;")
                .Replace("IPHONEOS_DEPLOYMENT_TARGET = ", "GCC_WARN_ABOUT_DEPRECATED_FUNCTIONS = NO;\n\t\t\t\tIPHONEOS_DEPLOYMENT_TARGET = ");
            if (patched == text) UnityEngine.Debug.LogWarning("[PlantGame] project.pbxproj had nothing to patch: did Unity change its template?");
            File.WriteAllText(file, patched);
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
