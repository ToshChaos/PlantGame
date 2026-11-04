# TASKS-IOS.md — iOS release backlog

iOS board, the sibling of `TASKS-ANDROID.md`. The touch layer (T57-T61) and the quality tiers (T63) are shared and are not redone here; these tasks cover what iOS adds.
Controls design: `docs/mobile-controls.md`. Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT`.

| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T69](tasks/T69-ios-toolchain-and-build.md) | Xcode, Unity iOS module, `IosBuild` (device + Simulator), signing and build number from env | A8 | — | TODO |
| [T70](tasks/T70-ios-safe-area-and-ipad-layout.md) | Home-indicator gestures, iPad 4:3 and resizable windows (iPhone safe area checked fine) | A7 | T59, T69 | TODO |
| [T71](tasks/T71-ios-platform-behaviour.md) | No Quit buttons, audio session and interruptions, lifecycle, keyboard and paste | A0 | T62, T69 | TODO |
| [T72](tasks/T72-ios-performance-and-memory.md) | iOS first-run preset, memory limit, thermal, device profile | A6 | T63, T69 | TODO |
| [T73](tasks/T73-ios-identity-and-compliance.md) | Opaque icon, launch screen, encryption flag, privacy manifest, hardware stats off | A8 | T69 | TODO |
| [T74](tasks/T74-ios-testflight-beta.md) | Archive and upload, `docs/release-ios.md`, TestFlight, device matrix, update keeps saves | A8 | T70-T73 | TODO |
| [T75](tasks/T75-ios-app-store-release.md) | App Store listing, screenshots, App Review, release | A8 | T74 | TODO |

Order: T69 unblocks everything. T70-T73 can then run side by side (T70 and T71 both touch the title and pause menus, so not at the same time in one checkout). T74 and T75 come last.

## Needs a person
- Install Xcode (26 or later, from the App Store) and select it: `sudo xcode-select -s /Applications/Xcode.app`. Add **iOS Build Support** to Unity 6000.6.5f1 in Unity Hub.
- Apple Developer Program membership (paid yearly; a free personal team is enough for the first device test, T69). Agreements, App Store Connect forms, signing in to Xcode.
- A physical iPhone, ideally also an iPad, for device passes.

## Decisions before the first upload
| Decision | Recommendation | Why it matters |
|----------|----------------|----------------|
| Enrol as an individual or an organisation | Individual unless a company already exists | The seller name on the store is your legal name; an organisation needs a D-U-N-S number |
| Bundle id | `com.overgrown.game`, the same as Android | It cannot change after the first upload |
| App name | Check that "Overgrown" is free in App Store Connect; fallback "Overgrown: Floating Islands" | App Store names are unique |
| iPhone only or iPhone + iPad | iPhone + iPad (`targetDevice` is already 2) | iPad needs T70's 4:3 and window work and 13" screenshots |
| Minimum iOS | Keep the current 15.0 unless Unity 6.6 raises it | Older OS means older, weaker devices in T72's matrix |

## Found while planning (2026-11-04)
- **No safe-area code exists, and none was needed on iPhone (checked 2026-11-04 on the Simulator).** `safeArea` appears nowhere in `Assets/Scripts`; Android hides the problem with `renderOutsideSafeArea = false`, the only mention in code. See T70.
- **Quit buttons on phones** in three places (home, pause, end of run). See T71.
- **The first-run preset rule is Android-tuned:** under 5 GB gives Low, so a 4 GB iPhone with a strong GPU starts on Low. See T72.
- **`T_AppIcon.png` has an alpha channel**; App Store Connect rejects icons with one. See T73.
- **`submitAnalytics: 1`** in Player Settings looks at odds with the privacy policy's "no analytics, no network access". It also affects Android. See T73.
