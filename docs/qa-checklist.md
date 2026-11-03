# QA checklist (manual)

Automated first: **Window > General > Test Runner > EditMode** (inventory, seed codes, save service, world generation, golden seeds) and
**PlantGame > Smoke Test Levels** (plays each curated level for a few seconds, report in `Game/Logs/smoke.txt`).
Tick what you observed, note the build and date. Controls reference: `docs/controls.md`.

## Menus and flow
- [ ] Title: Play, Random Level, Enter Seed Code, Options all respond; title world grows its plants
- [ ] Level select shows ten levels with best times
- [ ] Enter Seed Code: valid code builds the world; empty, one word, six words, a mistyped word each show a message; spaces / capitals / underscores accepted; code from another version warns
- [ ] Copy seed code (pause menu, win panel) then paste it back: same world
- [ ] Options: reset best times; key hints toggle; volume sliders
- [ ] Pause (Esc): resume, restart, back to menu; time stops; pickup prompt hidden
- [ ] Restart: hold R restarts; spam R, Esc and Restart together does not break the run

## Controls and movement
- [ ] WASD and arrows move relative to camera; Shift runs; Space jumps; mouse looks
- [ ] Beanstalk: W / S and Up / Down climb; A / D ignored; Space jumps off; top steps onto the target island; no re-grab for 0.3 s
- [ ] Fall into the void: respawn on the last island; watering can charges kept
- [ ] Alt-tab out and back: no stuck keys, timer behaves

## Inventory and items
- [ ] Q picks up the focused item; 4 slots, no stacking; Q with a full inventory shows the "full" feedback and picks up nothing
- [ ] Q with nothing focused drops the selected item; drop over the void: item is not lost forever (seed sources regrow)
- [ ] Tab / Shift+Tab wrap around, empty slots selectable; slots 1-4 alias
- [ ] After using a seed the selection moves to the next filled slot

## Planting and water
- [ ] E on a soil patch plants the selected seed (sprout, no colliders); E on a used soil patch or with a non-seed item is rejected
- [ ] Plant twice in the same patch: second is rejected, seed kept
- [ ] F near a sprout: uses one charge and starts growth; F on the same sprout twice: second costs nothing
- [ ] F with an empty can: dry feedback, nothing changes; F on a grown plant or anything else: costs nothing
- [ ] F at a lake or waterfall: can fills to 2; F with a full can: nothing lost
- [ ] Water Collector (Dew Seed): grows without water, +1 charge per 20 s, capacity 1; F with a full can leaves the charge in the plant; keeps filling while the player is on another island
- [ ] Collector-only level (3): empty can at start, no water anywhere else
- [ ] Bridge, beanstalk, shelf platform: colliders appear as they grow; the player can cross / climb / step off
- [ ] Frozen water and soil on snow levels: prompt says a Heat Seed is needed; E with a Heat Seed thaws
- [ ] Parachute leaf: Q pick-up, jump, E in the air glides once

## Modes and scoring
- [ ] No countdown, no fail; elapsed time shown
- [ ] Best time saved per level; a slower run does not overwrite; new best is announced
- [ ] Goal kinds work: flag, fruit, gate, campfire; level 6 needs the Heart Seed brought home

## Audio and look
- [ ] Music per level, menu tune, win music; footsteps per surface; water and wind ambience; no clipping or silence after scene changes
- [ ] Weather themes (rain, snow, fog, sunset, sunrise, autumn) and wind gusts on levels 8 and 10

## Regression after generator changes
- [ ] Test Runner: `GoldenSeedsKeepTheirWorld` passes (else bump `SeedCode.GeneratorVersion` and re-record on purpose)
- [ ] `PlantGame > 15 Validate Seeds`: 0 invalid
