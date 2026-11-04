# T71 — iOS platform behaviour

**Owner:** A0 Director · **Status:** TODO (see `TASKS-IOS.md`) · **Depends on:** T62, T69

## Work
- **Quit:** iOS apps should not close themselves and an in-game Quit can look like a crash to App Review. Hide the button on iOS in `TitleMenu` (home, both layouts) and `PlayerHud` (pause, end of run), and close the gap it leaves. Android keeps it.
- **Test scenes:** `TitleMenu` hides them with `platform != Android`; change to `Application.isMobilePlatform`.
- **Audio session:** `muteOtherAudioSources` is off, so the game should mix with the player's music and obey the silent switch; confirm that is what happens and that it suits the game. After a call, Siri, an alarm or a headphone change, sound returns at the right volumes.
- **AGENTS.md audio rule on a rebuilt iOS player:** toggle Sound and Music off and on in the pause menu (`timeScale` 0) and hear silence at once.
- **Lifecycle:** `MobileLifecycle` auto-pause on home, the app switcher, Control Center, Notification Center and a call. Decide which of these should pause. There is no Back button, so the Pause button is the only way in. Update the Android-only wording in its comments.
- **Saves:** kill the app from the switcher right after a best time and after changing a setting; nothing is lost (`SaveService`, `PlayerPrefs.Save`). Saves survive an update (final check in T74).
- **Seed codes:** Copy works from Generate and pause; Paste shows the iOS paste permission prompt: allowing fills the field, declining breaks nothing. The soft keyboard in landscape does not hide the seed field and dismisses cleanly.

## Acceptance
- No Quit button anywhere on iOS; layouts have no holes.
- On a device: lock, home, call (or Siri) and return each land in the pause menu or a sensible state, with the clock stopped and audio right.
- A seed code copied in the game, sent out through another app and pasted back starts the same level.

## Handoff
