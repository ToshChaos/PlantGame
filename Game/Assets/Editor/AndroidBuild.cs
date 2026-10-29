using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantGame.Editor
{
    /// <summary>
    /// T62 / T64: Android settings and builds. Menu: PlantGame > Configure Android, Build Android APK / AAB.
    /// CLI: Unity -batchmode -executeMethod PlantGame.Editor.AndroidBuild.BuildApk. Output goes to the git-ignored Builds/ folder.
    /// Needs the "Android Build Support" module (with OpenJDK and Android SDK and NDK) installed through Unity Hub.
    /// Release signing: set the keystore in Project Settings > Player > Publishing Settings, keep the file OUTSIDE the repo.
    /// Without one the build is signed with the debug keystore, which is fine for sideloading.
    /// </summary>
    public static class AndroidBuild
    {
        const string OutDir = "../Builds/Overgrown-android";

        [MenuItem("PlantGame/Configure Android", false, 110)]
        public static void Configure()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.overgrown.game");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            // landscape only, both directions
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.renderOutsideSafeArea = false;   // notches stay clear of the HUD until T59 adds safe-area layout

            ApplySigning();
            Debug.Log("[PlantGame] Android player settings applied (IL2CPP, ARM64, API 26+, Vulkan + GLES3, landscape only).");
        }

        /// <summary>
        /// Release signing without keeping secrets in the repo: set OVERGROWN_KEYSTORE (path), OVERGROWN_KEYSTORE_PASS, OVERGROWN_KEY_ALIAS and
        /// OVERGROWN_KEY_PASS (defaults to the keystore password) in the environment. OVERGROWN_VERSION_CODE sets the Play version code
        /// (must grow with every upload). Without the variables the debug keystore is used. See docs/release-android.md.
        /// </summary>
        static void ApplySigning()
        {
            var vc = System.Environment.GetEnvironmentVariable("OVERGROWN_VERSION_CODE");
            if (int.TryParse(vc, out var code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;
            var path = System.Environment.GetEnvironmentVariable("OVERGROWN_KEYSTORE");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { PlayerSettings.Android.useCustomKeystore = false; return; }
            var pass = System.Environment.GetEnvironmentVariable("OVERGROWN_KEYSTORE_PASS") ?? "";
            var alias = System.Environment.GetEnvironmentVariable("OVERGROWN_KEY_ALIAS") ?? "overgrown";
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = System.Environment.GetEnvironmentVariable("OVERGROWN_KEY_PASS") ?? pass;
            Debug.Log("[PlantGame] Android release keystore in use (" + alias + ").");
        }

        [MenuItem("PlantGame/Build Android APK", false, 111)]
        public static void BuildApk() => Build(false);

        [MenuItem("PlantGame/Build Android AAB (Play)", false, 112)]
        public static void BuildAab() => Build(true);

        static void Build(bool bundle)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                const string msg = "Android Build Support is not installed for this Unity version. Install it in Unity Hub (Installs > Add modules: Android Build Support, OpenJDK, Android SDK & NDK Tools).";
                Debug.LogError("[PlantGame] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("Android build", msg, "OK");
                return;
            }
            Configure();
            Directory.CreateDirectory(OutDir);
            EditorUserBuildSettings.buildAppBundle = bundle;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(OutDir, bundle ? "Overgrown.aab" : "Overgrown.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var s = BuildPipeline.BuildPlayer(opts).summary;
            File.WriteAllText("Logs/build-android.txt", $"{s.result} size {s.totalSize / (1024 * 1024)} MB time {s.totalTime.TotalSeconds:0}s errors {s.totalErrors} warnings {s.totalWarnings} -> {s.outputPath}\n");
            bool made = File.Exists(opts.locationPathName);   // the report can say Succeeded while the Android post-processing threw (module just installed, editor not restarted): trust the file
            if (s.result == BuildResult.Succeeded && !made) Debug.LogError("[PlantGame] Build reported success but " + opts.locationPathName + " was not created. Restart Unity after installing the Android module, and check the console for 'Build target Android not supported'.");
            else if (s.result != BuildResult.Succeeded) Debug.LogError("[PlantGame] Android build failed: " + s.result);
            else Debug.Log("[PlantGame] Android build done: " + s.outputPath);
        }
    }
}
