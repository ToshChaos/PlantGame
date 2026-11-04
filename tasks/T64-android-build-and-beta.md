# T64 — Android build, signing and beta distribution

**Owner:** A8 Tester/Release · **Status:** DOING (post-MVP, see `TASKS-ANDROID.md`) · **Depends on:** T62, T63

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Switch the project to the Android build target; Android SDK/NDK via Unity Hub, IL2CPP, ARM64 only, min API 26, target current Play requirement, App Bundle (.aab) plus APK for direct testing; Vulkan with GLES3 fallback; Input System only (`Both` off).
- Package id (e.g. `com.<studio>.overgrown`), version/bundle code scheme, icon + adaptive icon, splash, keystore (kept OUTSIDE the repo, document where), build script menu `PlantGame > Build Android` + a CLI entry for CI.
- Verify the stripped build keeps shaders / materials (`Resources/WorldKit`, `M_Keep_*`), `RuntimeMats` works, third-party assets listed in LICENSES.md (Play requires attributions for CC-BY ones).
- Beta distribution: Google Play internal / closed testing track, or Firebase App Distribution as the quick path; crash reporting (Unity Cloud Diagnostics or Firebase Crashlytics) and a simple feedback link in the pause menu. Add the privacy-policy stub Play requires (the game collects nothing unless analytics are added).
- Test matrix: one low-end, one mid-range, one tablet; document results in `docs/qa-checklist.md` (touch section).

## Acceptance
- A signed .aab installs from the test track on two physical devices, plays Level 1 start to finish, and the best-time save survives a reinstall-free update.

## Handoff
Done (groundwork): `Editor/AndroidBuild.cs`, menu `PlantGame > Configure Android / Build Android APK / Build Android AAB (Play)`, CLI `-executeMethod PlantGame.Editor.AndroidBuild.BuildApk`. Applies IL2CPP, ARM64, min API 26, target API auto, Vulkan + GLES3, package id `com.overgrown.game`, landscape only. Output: `Builds/Overgrown-android/` (git-ignored), log `Game/Logs/build-android.txt`.
Blocked: the Unity 6000.6.5f1 install has no Android Build Support module (only macOS and WebGL), so no APK has been built yet. Install it in Unity Hub (Android Build Support, OpenJDK, Android SDK & NDK Tools), then run the menu item.
Not done: release keystore (debug keystore until then; keep the real one outside the repo), icons check, LICENSES.md review, beta track, test matrix.

Update 2026-10-29: Android module installed, `Overgrown.apk` builds. Release signing via env vars, privacy policy, store listing and a Play checklist added (`docs/private/release-android.md`). Still open: keystore creation, AAB upload to internal testing, device matrix.

Update 2026-11-04: Medium managed stripping is on (`AndroidBuild.Configure`, same as iOS). APK 61.7 -> 54.2 MB with the shader strip -> 50.7 MB with Medium; details in `docs/task-log.md`. **This player has not been run: no device or emulator here.** Needs a person, on the first device install:
- App starts past the splash; title draws with the right materials and shaders (Vulkan, then a GLES3-only device if one is at hand).
- Level 1 and a generated level load; plants grow; rain and snow draw.
- Touch: tap-to-walk, marker taps, hotbar, double-tap drop, Jump and Pause buttons, beanstalk climb, glide.
- Generate level: Copy code and Paste work; the seed code keyboard works.
- `save.json` is written, a best time is saved and survives a restart. Music and Sound toggles work while paused.
- `adb logcat -s Unity` shows no `MissingMethodException`, `TypeLoadException` or `Unable to find` lines, which are what over-stripping looks like. A fix for one is a `link.xml`, not lowering the level.

Update 2026-11-04 (later): APK 50.7 -> 43.4 MB with IL2CPP `OptimizeSize`, R8 and no `libAppUITextMateLib.so`; details in `docs/task-log.md`. Still not run on a device. Add to the first install:
- `adb logcat` shows no `ClassNotFoundException`, `NoSuchMethodError` or `NoSuchFieldError`, which are what over-shrinking by R8 looks like. A fix for one is a keep rule, not turning R8 off.
- Frame time and load time in a level, against a build with `OptimizeSpeed` if either feels worse. `OptimizeSize` stays only if the phone agrees.
