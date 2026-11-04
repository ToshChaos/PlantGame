# T73 — App identity and compliance

**Owner:** A8 Tester/Release · **Status:** DOING (see `TASKS-IOS.md`) · **Depends on:** T69

## Left to do
- Validate a signed archive in Xcode Organizer: no icon, plist or privacy errors. Generate Xcode's privacy report from it.
- See the icon on the Home Screen, in Settings and in App Store Connect.
- Review `ThirdParty/LICENSES.md`, same as Android (raw Ruins kit files are not redistributed; a built game is fine).
- Fill in the contact email in `docs/privacy-policy.md` and host it once for both stores.
- Check the Unity splash setting against the licence in use.

## Handoff
- Icon: `AppIconBuilder.BuildIos` writes `T_AppIcon_iOS.png` (1024, RGB, no alpha, uncompressed) into all 19 iOS slots.
- `IosBuild.PatchInfoPlist` sets `ITSAppUsesNonExemptEncryption = false`. Compliance facts and the App Privacy answer are in `docs/private/release-ios.md`.
