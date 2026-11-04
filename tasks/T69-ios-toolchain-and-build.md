# T69 — iOS toolchain and build script

**Owner:** A8 Tester/Release · **Status:** TODO (see `TASKS-IOS.md`) · **Depends on:** —

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
