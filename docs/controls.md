# Controls

Mirrors `AGENTS.md` section 5.8 (that file wins on conflict). Bindings live in
`Game/Assets/Scripts/Player/PlantGameInput.inputactions`, read through `PlayerInputReader`. Prompts must print the bound key from the action, never a hard-coded letter.

| Key | Action |
|-----|--------|
| W A S D | Move relative to the camera (third person, T31); the character turns to face where it moves. While **climbing**, **W** goes up and **S** goes down; A / D are ignored |
| Mouse | Look (cursor locks on Play; **Esc** releases it, a click grabs it back) |
| Left Shift | Run (6 m/s; walk is 4 m/s) |
| Space | Jump (1.6 m). While climbing: **jump off the vine** — normal jump plus ~2 m/s away from the surface, 0.3 s re-grab cooldown |
| Q | Pick up the item you look at; otherwise drop the selected item (T04) |
| Tab / Shift+Tab | Next / previous inventory slot, wraps around; empty slots can be selected (T05) |
| 1–4, mouse wheel | Optional slot aliases |
| E | Use: plant the selected seed on a soil patch; refill the watering can at a lake, waterfall or collector (T04, T11, T27) |
| F | Water a sprout within 3 m, costs one charge (T27) |
| Esc | Pause (T19; T03 only releases the cursor) |
| R (hold ~1 s) | Restart the run (`PlayerController.RestartRequested`, wired in T15) |

## Climbing
Stand in a `ClimbableVolume` and press **W** to start (below the top zone). At the top edge, standing on the ladder's `Land`, press **S** to start going down.
Reaching the top steps you onto `Land`; reaching the floor while holding S lets go.

## Movement numbers (`PlayerController`)
Walk 4, run 6 m/s · jump 1.6 m (gravity 20, 8 m/s up) · coyote 0.12 s · jump buffer 0.12 s · air control 14 m/s² ·
step offset 0.35 m · slope limit 45° · radius 0.35 m, height 1.8 m. A level gap of 5 m can be cleared with a running jump, 7 m cannot.
