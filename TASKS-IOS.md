# TASKS-IOS.md — iOS release backlog

Open work only. The touch layer (T57-T61) and the quality tiers (T63) are shared and are not redone here. Settled decisions and compliance facts: `docs/release-ios.md`. Controls design: `docs/mobile-controls.md`.
Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW`. When a task is done, remove its row.

| ID | Task | Owner | Depends on | What is left |
|----|------|-------|-----------|--------------|
| [T69](tasks/T69-ios-toolchain-and-build.md) | Build tooling | A8 | — | Install on a physical iPhone, signed archive |
| [T70](tasks/T70-ios-safe-area-and-ipad-layout.md) | Home indicator and iPad layout | A7 | T59, T69 | Other landscape direction, iPad Options, window resize, bottom-edge swipe on a device, Android `renderOutsideSafeArea` |
| [T71](tasks/T71-ios-platform-behaviour.md) | Platform behaviour | A0 | T62, T69 | Device checks: audio, lifecycle, saves, paste, keyboard |
| [T72](tasks/T72-ios-performance-and-memory.md) | Performance and memory | A6 | T63, T69 | Device profile: fps, peak memory, thermal, minimum device |
| [T73](tasks/T73-ios-identity-and-compliance.md) | Identity and compliance | A8 | T69 | Organizer validation, privacy report, licences review, host the policy |
| [T74](tasks/T74-ios-testflight-beta.md) | TestFlight beta | A8 | T70-T73 | All of it (needs a person) |
| [T75](tasks/T75-ios-app-store-release.md) | App Store release | A8 | T74 | All of it (needs a person) |

T70 and T71 both touch the title and pause menus, so not at the same time in one checkout. T74 and T75 come last.

## Needs a person
- Apple Developer Program membership (paid yearly; a free personal team is enough for the first device test, T69). Agreements, App Store Connect forms, signing in to Xcode.
- A physical iPhone, ideally also an iPad, for device passes.

## Open decisions (before the first upload)
| Decision | Recommendation | Why it matters |
|----------|----------------|----------------|
| Enrol as an individual or an organisation | Individual unless a company already exists | The seller name on the store is your legal name; an organisation needs a D-U-N-S number |
| Bundle id | `com.overgrown.game`, the same as Android (already set) | It cannot change after the first upload |
| App name | Check that "Overgrown" is free in App Store Connect; fallback "Overgrown: Floating Islands" | App Store names are unique |
