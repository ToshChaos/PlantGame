# Android release — Google Play checklist

Tooling is in place (`Editor/AndroidBuild.cs`); the steps below need a person (accounts, a device, store forms).

## 1. Release keystore (once, keep it OUTSIDE the repo; back it up — losing it means no more updates)
```bash
keytool -genkeypair -v -keystore ~/keystores/overgrown-release.jks -alias overgrown -keyalg RSA -keysize 2048 -validity 10000
```
Prefer **Play App Signing** (Play holds the app key; this is your upload key).

## 2. Build the bundle
```bash
export OVERGROWN_KEYSTORE=~/keystores/overgrown-release.jks OVERGROWN_KEYSTORE_PASS=... OVERGROWN_KEY_ALIAS=overgrown
export OVERGROWN_VERSION_CODE=2   # must grow with every upload
```
Start Unity from that shell, or batch mode: `Unity -batchmode -quit -projectPath Game -executeMethod PlantGame.Editor.AndroidBuild.BuildAab`.
Output: `Builds/Overgrown-android/Overgrown.aab`. Version name: `bundleVersion` in Player Settings (now 0.1.0).

## 3. Play Console (developer account: one-time 25 USD)
- Create app, package `com.overgrown.game` (change it in `AndroidBuild.Configure` if you want your own domain — before the first upload, it cannot change later).
- Store listing text: `docs/store-listing.md`. Privacy policy URL: host `docs/privacy-policy.md`.
- Data safety form: no data collected or shared. Content rating questionnaire: no violence, no user content (expect PEGI 3 / Everyone). Target audience: 13+ or all ages, not a children's-program app.
- Upload the .aab to **Internal testing** first; add testers; install on two phones (one low-end, one tablet), play Level 1, update the build and check the best time survives.
- New personal accounts must run a closed test with 12 testers for 14 days before production access.

## 4. Before promoting to production
- [ ] Device pass: touch controls, safe area, Back button, home/return auto-pause (`docs/qa-checklist.md`, touch section)
- [ ] Profile on a mid-range phone (T63): 30 fps minimum; the "Battery saver (30 fps)" option exists in Options
- [ ] `ThirdParty/LICENSES.md` reviewed (CC0/Kenney/Quaternius entries need no attribution; check the Ruins kit licence allows redistribution)
- [ ] Screenshots (phone landscape, 2 min), 512x512 icon, 1024x500 feature graphic
