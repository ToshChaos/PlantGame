# T74 — TestFlight beta

**Owner:** A8 Tester/Release · **Status:** TODO (`docs/release-ios.md` drafted) (see `TASKS-IOS.md`) · **Depends on:** T70, T71, T72, T73

## Work
- `docs/release-ios.md`, the sibling of `docs/release-android.md`: account, signing, build commands, upload, TestFlight, pre-release checklist. Add an iOS section to the git-ignored `docs/private/publishing.md` (team id, build-number log).
- Person: join the Apple Developer Program, register the bundle id, create the app record in App Store Connect (decisions in `TASKS-IOS.md`).
- Archive, export and upload from the command line (`xcodebuild archive`, `-exportArchive`, upload through Xcode or Transporter). Build number from `OVERGROWN_BUILD_NUMBER`.
- TestFlight: internal testers first (no review). External testers only if wanted; the first external build goes through Beta App Review. A simple feedback route for testers (TestFlight's built-in feedback is enough).
- Crash reports: use the ones TestFlight and Xcode Organizer already give, so no SDK and no privacy change.
- Device matrix: the weakest supported iPhone, a current Dynamic Island iPhone and an iPad. Add an iOS block to `docs/qa-checklist.md` (touch controls, safe area, audio, lifecycle, seed codes).

## Acceptance
- A signed build installs from TestFlight on two physical devices and plays Level 1 start to finish.
- A later build installed over the first keeps best times and settings.
- The matrix results are in `docs/qa-checklist.md`, with no open blocker.

## Handoff

