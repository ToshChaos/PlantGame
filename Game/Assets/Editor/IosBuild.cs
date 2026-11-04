using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
            Debug.Log("[PlantGame] iOS player settings applied (IL2CPP, ARM64, Metal, iPhone + iPad, landscape only, team " + (string.IsNullOrEmpty(team) ? "not set" : "set") + ").");
        }

        [MenuItem("PlantGame/Build iOS (Device)", false, 121)]
        public static void BuildDevice() => Build(false);

        [MenuItem("PlantGame/Build iOS (Simulator)", false, 122)]
        public static void BuildSimulator() => Build(true);

        static void Build(bool simulator)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                const string msg = "iOS Build Support is not installed for this Unity version. Install it in Unity Hub (Installs > Add modules > iOS Build Support) and restart Unity.";
                Debug.LogError("[PlantGame] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("iOS build", msg, "OK");
                return;
            }
            Configure();
            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            var dir = OutDir + (simulator ? "-sim" : "");
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
                Debug.LogError("[PlantGame] iOS build failed: " + s.result);
            else Debug.Log("[PlantGame] iOS Xcode project written: " + s.outputPath);
        }
    }
}
