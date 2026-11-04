# T69 — iOS toolchain and build script

**Owner:** A8 Tester/Release · **Status:** DOING (simulator and xcodebuild done; device build, archive, Mac/Android re-check open) (see `TASKS-IOS.md`) · **Depends on:** —

## State on 2026-11-04
Not installed: Xcode (only Command Line Tools are active) and Unity's iOS module (`PlaybackEngines` holds Android, Mac, WebGL). No signing identities. Player Settings have no iPhone bundle id; `iPhoneSdkVersion` is the device SDK; automatic signing is off.

## Work
- Person: install Xcode 26 or later (Apple has required the iOS 26 SDK for uploads since 2026-04-28), select it, add iOS Build Support in Unity Hub, restart Unity.
- `Editor/IosBuild.cs`, modelled on `AndroidBuild.cs`. Menu `PlantGame > Configure iOS / Build iOS (Device) / Build iOS (Simulator)`; CLI through `-executeMethod`. Output `Builds/Overgrown-ios/` (git-ignored), log `Game/Logs/build-ios.txt`.
- Settings: bundle id, IL2CPP (the only iOS backend), ARM64, Metal, iPhone + iPad, landscape only, input system only, the same explicit scene list as Android (Title, Main).
- Secrets and numbers from env, nothing in the repo: `OVERGROWN_APPLE_TEAM` (team id, automatic signing), `OVERGROWN_BUILD_NUMBER` (must grow with every upload). Version string stays `bundleVersion`.
- Keep `UnityEditor.iOS.*` code inside `#if UNITY_IOS`, so the Mac and Android builds still compile. Confirm Mac and Android builds still work after the target switch, and commit what the switch changes in `ProjectSettings`.
- Check the pre-release packages (`com.unity.ai.assistant`, `com.unity.ai.inference`) do not break or bloat the player. Use the pinned Unity version's UIScene support if the Xcode 26 SDK asks for it.

## Acceptance
- A Simulator build launches through `mcp__Claude_Code_iOS_Simulator__control`: title -> Level 1 runs with no pink materials and a clean log (`RuntimeMats`, `M_Keep_*` survive stripping).
- A device build installs on a physical iPhone from Xcode (a free personal team is enough; its builds expire after 7 days) and reaches Level 1.
- Xcode project and archive also build from the command line (`xcodebuild`), ready for T74.

## Handoff

Done 2026-11-04 (A8):
- Xcode 27.2 and Unity's iOS module are installed. `Game/Assets/Editor/IosBuild.cs` (menu `PlantGame > Configure iOS / Build iOS (Device) / Build iOS (Simulator)`, CLI `PlantGame.Editor.IosBuild.BuildDevice` / `BuildSimulator`) writes the Xcode project to `Builds/Overgrown-ios[-sim]/`, log `Game/Logs/build-ios.txt`. No `UnityEditor.iOS.*` code, so no `#if UNITY_IOS` needed yet.
- Simulator build: Unity export OK (0 errors, 364 warnings, 128 s, 1.4 GB project), then `xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -sdk iphonesimulator -configuration Release -derivedDataPath ../ios-sim-dd CODE_SIGNING_ALLOWED=NO build` -> BUILD SUCCEEDED. Launched on iPhone 18 Pro (iOS 27.2): title, then Level 1, no pink materials, no Unity errors in the app log. Touch controls (Menu, Jump) show.
- Seen for T70: the Dynamic Island covers the Menu button's left side and the title screen ignores the safe area; app icons are empty (T73).
- Warning from Configure: "empty graphics device types list for platform iOSSupport" (harmless, Metal is the default).
- Open: device build on a physical iPhone (needs `OVERGROWN_APPLE_TEAM`), `xcodebuild archive`, re-check Mac and Android builds after the target switch, pre-release packages' effect on size (project export is 1.4 GB, check the real .app size). `iPhoneSdkVersion` in ProjectSettings flips between device (988) and simulator (989) with the last build.

2026-11-04 (later): Simulator export rebuilt after the T70-T73 changes (0 errors), `xcodebuild` BUILD SUCCEEDED, plist flag verified in the export, runs on the iPad Pro 13" Simulator (Level 1 HUD fine). `IosBuild` now forces the development flag off (a Development Console had leaked in), defers the bottom-edge gesture. Still open: device build, `xcodebuild archive` (needs signing), Mac and Android re-check, real `.app` size.
