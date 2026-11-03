# T64 — Android build, signing and beta distribution

**Owner:** A8 Tester/Release · **Status:** DOING (post-MVP, see `TASKS.md`) · **Depends on:** T62, T63

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

Update 2026-10-29: Android module installed, `Overgrown.apk` builds. Release signing via env vars, privacy policy, store listing and a Play checklist added (`docs/release-android.md`). Still open: keystore creation, AAB upload to internal testing, device matrix.
