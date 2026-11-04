# T71 — iOS platform behaviour

**Owner:** A0 Director · **Status:** DOING (code in; the rest needs a device) (see `TASKS-IOS.md`) · **Depends on:** T62, T69

## Left to do (on a device)
- **Audio session:** the game mixes with the player's music and obeys the silent switch, and that suits the game. After a call, Siri, an alarm or a headphone change, sound returns at the right volumes.
- **AGENTS.md audio rule on a rebuilt iOS player:** toggle Sound and Music off and on in the pause menu (`timeScale` 0) and hear silence at once.
- **Lifecycle:** lock, home, the app switcher, Control Center, Notification Center and a call or Siri each land in the pause menu or a sensible state, with the clock stopped and audio right.
- **Saves:** kill the app from the switcher right after a best time and after changing a setting; nothing is lost (`SaveService`, `PlayerPrefs.Save`). Saves survive an update (final check in T74).
- **Seed codes:** a code copied in the game, sent through another app and pasted back starts the same level. Paste shows the iOS permission prompt: allowing fills the field, declining breaks nothing. The soft keyboard in landscape does not hide the seed field and dismisses cleanly.

## Handoff
- `TitleMenu.NoQuit` hides Quit on iOS (title, pause, end panel). Test-scene buttons are hidden on all mobile platforms.
- `MobileLifecycle` pauses on focus loss as well as app pause, and frees unused assets on `Application.lowMemory` (at most once per 10 s). Orientation comes from Player Settings.
