# T60 — Touch hotbar, pause and menus

**Owner:** A7 Sound & UI · **Status:** REVIEW (post-MVP, see `TODO.md`) · **Depends on:** T59

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Hotbar slots are tappable: tap selects (replaces Tab), tap the selected slot again drops; selection still moves to the next filled slot after a use. Bigger slots on touch (min 48 dp); keep the four-slot layout top right.
- Pause button top-left and Android Back = Esc (pause / resume, back out of sub-panels). Restart lives in the pause menu (no hold-R on touch).
- Title menu, level select, Options, win / fail panels: tap targets >= 48 dp, scrollable lists with touch drag, no hover-only states, no keyboard-only paths. "Input seed" opens the soft keyboard (TMP_InputField) with the seed words; "copy seed code" works via `GUIUtility.systemCopyBuffer`; paste button next to the field because long-press paste is awkward.
- Menus must not depend on the cursor lock state.

## Acceptance
- Whole flow title -> level select -> play -> pause -> restart -> win -> back to menu works with touch only, including entering a seed code.

## Handoff
Done: hotbar slots are buttons (`PlayerHud.OnSlotTapped`: tap selects, tap the selected slot drops), 130 px slots on touch; the temporary Next-item button is gone; the seed field asks for an ASCII soft keyboard (the Paste button and `systemCopyBuffer` copy already existed); Esc / Android Back backs out of title sub-panels (`TitleMenu.Update`). The pause menu and title screens were already big uGUI buttons on a 1920x1080 canvas, so they were left as they are.
Verified in the editor: slot taps change the selection. Not verified: drop by tapping, real-device soft keyboard, that Android Back arrives as Escape, scrolling lists (none exist), win / fail flow by touch.
