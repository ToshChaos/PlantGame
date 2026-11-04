# TASKS-IOS.md — iOS release backlog

iOS board, the sibling of `TASKS-ANDROID.md`. The touch layer (T57-T61) and the quality tiers (T63) are shared and are not redone here; these tasks cover what iOS adds.
Controls design: `docs/mobile-controls.md`. Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT`.

| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T69](tasks/T69-ios-toolchain-and-build.md) | Xcode, Unity iOS module, `IosBuild` (device + Simulator), signing and build number from env | A8 | — | DOING (simulator + xcodebuild done; device, archive, Mac/Android re-check open) |
| [T70](tasks/T70-ios-safe-area-and-ipad-layout.md) | Home-indicator gestures, iPad 4:3 and resizable windows (iPhone safe area checked fine) | A7 | T59, T69 | DOING (gesture defer + resize watcher in; iPad 13" landscape checked; resize, other direction, device open) |
| [T71](tasks/T71-ios-platform-behaviour.md) | No Quit buttons, audio session and interruptions, lifecycle, keyboard and paste | A0 | T62, T69 | DOING (Quit, test scenes, focus pause done; audio, saves, paste, device open) |
| [T72](tasks/T72-ios-performance-and-memory.md) | iOS first-run preset, memory limit, thermal, device profile | A6 | T63, T69 | DOING (preset rule done; memory, thermal, size need a device) |
| [T73](tasks/T73-ios-identity-and-compliance.md) | Opaque icon, launch screen, encryption flag, privacy manifest, hardware stats off | A8 | T69 | DOING (icon, plist flag, hardware stats, network check, privacy manifest done; Organizer validation, licences review open) |
| [T74](tasks/T74-ios-testflight-beta.md) | Archive and upload, `docs/release-ios.md`, TestFlight, device matrix, update keeps saves | A8 | T70-T73 | TODO (`docs/release-ios.md` drafted; the rest needs a person) |
| [T75](tasks/T75-ios-app-store-release.md) | App Store listing, screenshots, App Review, release | A8 | T74 | TODO |

Order: T69 unblocks everything. T70-T73 can then run side by side (T70 and T71 both touch the title and pause menus, so not at the same time in one checkout). T74 and T75 come last.

## Needs a person
- Apple Developer Program membership (paid yearly; a free personal team is enough for the first device test, T69). Agreements, App Store Connect forms, signing in to Xcode.
- A physical iPhone, ideally also an iPad, for device passes.

## Decisions before the first upload
| Decision | Recommendation | Why it matters |
|----------|----------------|----------------|
| Enrol as an individual or an organisation | Individual unless a company already exists | The seller name on the store is your legal name; an organisation needs a D-U-N-S number |
| Bundle id | `com.overgrown.game`, the same as Android | It cannot change after the first upload |
| App name | Check that "Overgrown" is free in App Store Connect; fallback "Overgrown: Floating Islands" | App Store names are unique |
| iPhone only or iPhone + iPad | **Decided: iPhone + iPad** (`targetDevice` is already 2) | iPad needs T70's 4:3 and window work and 13" screenshots |
| Minimum iOS | **Decided: 15.0, preferred 17+** (keep 15.0 unless Unity 6.6 raises it) | Older OS means older, weaker devices in T72's matrix |

## Decisions (2026-11-04)
| Decision | Choice | Why |
|----------|--------|-----|
| Home-indicator swipe | `deferSystemGesturesMode = BottomEdge`; `hideHomeButton` stays off | A look drag near the bottom no longer leaves the game; the first swipe is swallowed, a second one leaves |
| Resize / rotation | `ScreenWatcher` (touch platforms): re-fit CanvasScalers and render lines on any size change, reload the title scene when the phone / tablet layout choice flips; HUD only needs the scaler | Menus were laid out once; iPadOS 26 ignores `UIRequiresFullScreen` |
| Devices and minimum iOS | iPhone and iPad both supported; minimum iOS 15.0 (the build setting stays), iOS 17 and later is the preferred test target | Agreed 2026-11-04. iOS 17 needs an iPhone XS or newer, so the Low preset rule (under 3.5 GB: iPhone 8 / X / XR) only matters on 15 and 16 |
| Orientation | Landscape only on all iPhones and iPads (no portrait) | The UI is authored for landscape |
| Quit | Hidden on iPhone and iPad (`TitleMenu.NoQuit`); test-scene buttons hidden on all mobile platforms | App Review; test scenes are not for players |
| Audio session | Keep `muteOtherAudioSources` off (Ambient: mixes with the player's music, obeys the silent switch) | Not yet heard on a device |
| Encryption | `ITSAppUsesNonExemptEncryption = false` via a post-build `plutil` step in `IosBuild` (no `UnityEditor.iOS.Xcode` reference, so other platforms compile unchanged) | Skips the export question on every upload |
| Privacy | App Privacy answer: Data Not Collected, no tracking; privacy policy made store neutral; Unity's frameworks carry the required-reason manifests | No network code, analytics off |

## Open findings
- The iOS Simulator player logs `ArgumentException: 'InputUpdateType.None' is not a valid update mask` at startup. Stack: `EnhancedTouch.Finger` created for a touchscreen that the Input System discovers natively at launch, while EnhancedTouch's update mask is still empty (Input System 1.20.1; nothing else in our code touches EnhancedTouch but `TouchControls`). Touch input works anyway (tap-to-walk, jump, menus checked on the iPhone 18 Pro Simulator). Not fixed: try a newer Input System, or poll `Touchscreen.current` instead of EnhancedTouch; check on a device first.
- The Simulator export reports `Build type 'Development'` in the player log even with the editor's development flag off and a clean output folder (`IosBuild` now deletes the folder first). The on-screen console is hidden at startup (`MobileLifecycle`). Check the device build and its size before release.
