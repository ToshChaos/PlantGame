# iOS release — TestFlight / App Store checklist

Tooling is in place (`Editor/IosBuild.cs`); the steps marked **person** need an account, a physical device or store forms.

## 1. Build
```bash
export OVERGROWN_APPLE_TEAM=XXXXXXXXXX      # person: your Apple team id (automatic signing)
export OVERGROWN_BUILD_NUMBER=2             # must grow with every upload
```
Unity (menu `PlantGame > Build iOS (Device)`, or batch `-executeMethod PlantGame.Editor.IosBuild.BuildDevice`) writes an Xcode project to `Builds/Overgrown-ios/`; log `Game/Logs/build-ios.txt`.
`Build iOS (Simulator)` writes `Builds/Overgrown-ios-sim/`. A post-build step sets `ITSAppUsesNonExemptEncryption = false` in `Info.plist`.

Simulator run from the command line:
```bash
cd Builds/Overgrown-ios-sim && xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -sdk iphonesimulator -configuration Release -derivedDataPath ../ios-sim-dd CODE_SIGNING_ALLOWED=NO build
```
Archive (person: needs signing): `xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release -destination generic/platform=iOS -archivePath ../Overgrown.xcarchive archive`, then Organizer, Validate, Distribute.

## 2. Compliance facts (checked 2026-11-04)
- **Network:** no `UnityWebRequest` / sockets in `Assets/Scripts`; analytics, ads, purchasing and hardware stats (`submitAnalytics`) are off. App Privacy answer for App Store Connect: **Data Not Collected**. Tracking: none.
- **Encryption:** none beyond the exempt system kind; the plist flag skips the export question.
- **Privacy manifest:** Unity's exported `UnityFramework` and `UnityRuntime.framework` ship `PrivacyInfo.xcprivacy` covering UserDefaults (`PlayerPrefs`), file timestamps, boot time and disk space. The game adds no further required-reason API. Verify in Xcode: Product > Archive > Generate Privacy Report (person).
- **Privacy policy:** `docs/privacy-policy.md`, store neutral. Fill in the contact email and host it once (person).
- **Quit button:** hidden on iOS (title, pause, end panel).
- **Audio session:** `muteOtherAudioSources` is off (Unity's Ambient category): the game mixes with the player's music and follows the silent switch.
- **Orientation:** landscape only; the first home-indicator swipe is deferred (`deferSystemGesturesMode` = bottom edge).
- **Launch screen:** Unity's default storyboard in the splash background colour; the Unity splash follows the licence in use.

## 3. Minimum device and sizes
Supported: iPhone and iPad, landscape only. Minimum iOS 15.0; iOS 17 and later is the preferred test target (test matrix: one device on 15 or 16 if available, the rest on 17+). First-run preset: Low under 3.5 GB RAM (iPhone 8 / X / XR), else Medium at 30 fps without postprocessing. Minimum device and IPA size: **to be measured on a device (T72)**; Simulator Xcode project export is 1.4 GB, which is not the IPA.

## 4. TestFlight (person)
- [ ] Upload the archive; add internal testers; install on an iPhone and an iPad.
- [ ] Device pass: touch controls, bottom-edge swipes, lock / home / call return into pause, kill from the switcher after a best time, seed code copy and paste, Sound and Music off/on in pause.
- [ ] Update over an older build keeps best times.
- [ ] 15 min thermal run, peak memory in Xcode on the largest level (T72).

## 5. App Store (person)
Listing text `docs/store-listing.md`, screenshots (6.9" iPhone landscape, 13" iPad), App Privacy: Data Not Collected, age rating questionnaire, review notes (no account needed).
