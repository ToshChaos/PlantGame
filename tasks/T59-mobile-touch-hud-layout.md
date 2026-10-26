# T59 — Touch HUD: layout, safe area, scaling, left-handed mode

**Owner:** A7 Sound & UI · **Status:** TODO (post-MVP, see `TODO.md`) · **Depends on:** T57

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Goal
Build the on-screen layout from `docs/mobile-controls.md` and make the existing HUD usable on a phone.

## Work
- Touch overlay prefab (Canvas, Scale With Screen Size, reference 1920x1080, match height) with stick zone, look zone, Jump, Context, Water, Pause; built by an editor builder (`PlantGame > Build Touch Controls`), not by hand.
- Safe area handling for notches / gesture bars on the overlay and the existing HUD; HUD elements (hotbar, timer, hints, prompts) get minimum 48 dp targets where tappable; key hints (Q/E/F/Tab) are replaced by button icons on touch, text bound to the active scheme.
- Options: layout scale, button opacity, left-handed mirror, stick type (floating / fixed), look sensitivity, invert Y, hints. Saved in `GameSettings`.
- Show/hide with the active device (T57); hide the overlay in menus, win and fail panels; keep it out of screenshots only if a capture flag is set.

## Acceptance
- On a 20:9 phone and a 4:3 tablet simulator every control is on screen, reachable by thumbs, nothing overlaps the hotbar, timer or goal hint; left-handed mode mirrors it correctly.
- Resizing / rotating between the two landscapes does not break anchors.

## Handoff
_(fill in when done)_
