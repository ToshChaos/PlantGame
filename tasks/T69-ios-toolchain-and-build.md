# T69 — iOS toolchain and build script

**Owner:** A8 Tester/Release · **Status:** DOING (see `TASKS-IOS.md`) · **Depends on:** —

## Left to do
- Device build on a physical iPhone from Xcode (a free personal team is enough; its builds expire after 7 days), reaching Level 1. Needs `OVERGROWN_APPLE_TEAM`.
- Signed `xcodebuild archive`, ready for T74.

## Handoff
- `Game/Assets/Editor/IosBuild.cs`: menu `PlantGame > Configure iOS / Build iOS (Device) / Build iOS (Simulator)`, CLI `PlantGame.Editor.IosBuild.BuildDevice` / `BuildSimulator`. Writes the Xcode project to `Builds/Overgrown-ios[-sim]/`, log `Game/Logs/build-ios.txt`. Commands: `docs/release-ios.md`.
- Team id and build number come from the environment. `iPhoneSdkVersion` in ProjectSettings flips between device (988) and simulator (989) with the last build; commit it as device.
- From a `Unity_RunCommand`, call `BuildSimulator()` directly: a `delayCall` is lost if a script reload follows. After editing build scripts, refresh and wait for the reload first, or the old code runs.
- Checked 2026-11-04: both exports and both unsigned `xcodebuild` runs succeed. The Simulator build plays on iPhone 18 Pro and iPad Pro 13" (no pink materials, clean log).
