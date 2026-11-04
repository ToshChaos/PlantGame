# T73 — App identity and compliance

**Owner:** A8 Tester/Release · **Status:** DOING (see `TASKS-IOS.md`) · **Depends on:** T69

## Work
- **Icon:** `T_AppIcon.png` has an alpha channel, which App Store Connect rejects. `AppIconBuilder` writes an opaque iOS version from the Android background and O layers (the system rounds the corners) and assigns it to all iOS sizes, 1024 x 1024 included.
- **Launch screen:** a plain storyboard or colour matching the Title background instead of the default; no stretched image in either landscape direction. Check the Unity splash setting against the licence in use.
- **Info.plist (post-process, `#if UNITY_IOS`):** `ITSAppUsesNonExemptEncryption = false` so each upload skips the export question. Check the game uses no encryption beyond the exempt system kind first.
- **Hardware stats (DONE 2026-11-04):** `submitAnalytics` is now 0 (UnityConnect analytics, ads, purchasing and insights were already off). Original note: `submitAnalytics: 1` in Player Settings appears to be Unity's hardware statistics report. The privacy policy says "no analytics, no network access". Confirm what it does and turn it off, for Android too.
- **Network check:** confirm the player makes no network calls (no use of `UnityWebRequest`, no traffic while playing on a device), so "Data Not Collected" is true.
- **Privacy manifest:** generate the Xcode privacy report from an archive; every required-reason API (`PlayerPrefs` uses `UserDefaults`, file timestamps) has an approved reason from Unity's manifest or ours, or the upload is refused.
- **Licences:** `ThirdParty/LICENSES.md` review, same as Android (raw Ruins kit files are not redistributed; a built game is fine).
- **Privacy policy:** `docs/privacy-policy.md` says Google Play and has a contact placeholder. Make it store-neutral; host it once for both stores.

## Acceptance
- Validation in Xcode Organizer passes for a signed archive with no icon, plist or privacy errors.
- The icon shows correctly on the Home Screen, in Settings and in App Store Connect.
- The privacy policy matches what was found, and the App Privacy answer for T75 is written down.

## Handoff
- 2026-11-04: icon done. `AppIconBuilder.BuildIos` writes `T_AppIcon_iOS.png` (1024, RGB, no alpha, full bleed, O at 72%) into every iOS slot (19 filled: Application, Settings, Notification, Spotlight, Marketing). Not yet checked in a signed archive / Organizer.
- iPhone builds have no Quit button (`TitleMenu.NoQuit`: title, pause, end panel). `QualityTier.DetectPreset` iOS rule: Low only under 3.5 GB, else Medium, 30 fps, postprocessing off (T72 may refine with device data). `submitAnalytics: 0`.
