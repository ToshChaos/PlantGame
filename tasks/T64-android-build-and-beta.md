# T64 — Android build, signing and beta distribution

**Owner:** A8 Tester/Release · **Status:** TODO (post-MVP, see `TODO.md`) · **Depends on:** T62, T63

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
_(fill in when done)_
