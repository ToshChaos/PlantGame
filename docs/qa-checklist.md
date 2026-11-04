# QA checklist (manual)

Automated first: **Window > General > Test Runner > EditMode** (inventory, seed codes, save service, world generation, golden seeds) and
**PlantGame > Smoke Test Levels** (plays each curated level for a few seconds, report in `Game/Logs/smoke.txt`).
Tick what you observed, note the build and date. Controls reference: `docs/controls.md`.

## Menus and flow
- [X] Title: Play, Random Level, Enter Seed Code, Options all respond; title world grows its plants
- [X] Level select shows ten levels with best times
- [X] Enter Seed Code: valid code builds the world; empty, one word, six words, a mistyped word each show a message; spaces / capitals / underscores accepted; code from another version warns
- [X] Copy seed code (pause menu, win panel) then paste it back: same world
- [X] Options: reset best times; key hints toggle; volume sliders
- [X] Pause (Esc): resume, restart, back to menu; time stops; pickup prompt hidden
- [ ] Restart: hold R restarts; spam R, Esc and Restart together does not break the run

## Controls and movement
- [X] WASD and arrows move relative to camera; Shift runs; Space jumps; mouse looks
- [X] Beanstalk: W / S and Up / Down climb; A / D ignored; Space jumps off; top steps onto the target island; no re-grab for 0.3 s
- [X] Fall into the void: respawn on the last island; watering can charges kept
- [ ] Alt-tab out and back: no stuck keys, timer behaves

## Inventory and items
- [X] Q picks up the focused item; 4 slots, no stacking; Q with a full inventory shows the "full" feedback and picks up nothing
- [X] Q with nothing focused drops the selected item; drop over the void: item is not lost forever (seed sources regrow)
- [X] Tab / Shift+Tab wrap around, empty slots selectable; slots 1-4 alias
- [X] After using a seed the selection moves to the next filled slot

## Planting and water
- [X] E on a soil patch plants the selected seed (sprout, no colliders); E on a used soil patch or with a non-seed item is rejected
- [X] Plant twice in the same patch: second is rejected, seed kept
- [X] F near a sprout: uses one charge and starts growth; F on the same sprout twice: second costs nothing
- [X] F with an empty can: dry feedback, nothing changes; F on a grown plant or anything else: costs nothing
- [X] F at a lake or waterfall: can fills to 2; F with a full can: nothing lost
- [X] Water Collector (Dew Seed): grows without water, +1 charge per 20 s, capacity 1; F with a full can leaves the charge in the plant; keeps filling while the player is on another island
- [X] Collector-only level (3): empty can at start, no water ponds anywhere else
- [X] Bridge, beanstalk, shelf platform: colliders appear as they grow; the player can cross / climb / step off
- [X] Frozen water and soil on snow levels: prompt says a Heat Seed is needed; E with a Heat Seed thaws
- [X] Parachute leaf: Q pick-up, jump, E in the air glides once

## Modes and scoring
- [X] No fail; elapsed time shown
- [X] Best time saved per level; a slower run does not overwrite; new best is announced
- [X] Goal kinds work: flag, fruit, gate, campfire; level 6 needs the Heart Seed brought home

## Audio and look
- [X] Music per level, menu tune, win music; footsteps per surface; water and wind ambience; no clipping or silence after scene changes
- [X] Weather themes (rain, snow, fog, sunset, sunrise, autumn) and wind gusts on levels 8 and 10

## Regression after generator changes
- [ ] Test Runner: `GoldenSeedsKeepTheirWorld` passes (else bump `SeedCode.GeneratorVersion` and re-record on purpose)
- [X] `PlantGame > 15 Validate Seeds`: 0 invalid
