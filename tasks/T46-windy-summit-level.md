# T46 — "It's windy at the top": spiral ascent level and wind

**Owner:** A5 + A1 + A7 · **Depends on:** T14, T45 (snow summit; can start with plain snow ground from T35/T45 stubbed), T43
**Status:** DONE

## Goal
- **Curated level:** islands climb in a spiral pattern, each higher than the last, the goal on the topmost island, which is covered in snow (T45).
- **Wind:** strength grows with altitude. While airborne (jump, fall, glide) the player is pushed in **random directions** (gusts: direction and strength change every 1-3 s, seeded per run, smoothly blended). No push when grounded or climbing. Parachute leaf glides are affected too. Tunable in a ScriptableObject (`WindSettings`: base strength, per-metre gain, max, gust period).
- **Ambient wind noise:** looping wind whose volume/pitch/filter rises with altitude, plus gust swells synced to the gusts; respects the ambience volume setting. Optional light visuals (streaking leaves/snow particles, sway of trees).
- **Random levels:** some seeds enable the wind effect (a generator flag, shown in the level info); gentler strength, since they are not built as ascents. Optionally a spiral layout mode for random levels. Bump `SeedCode.GeneratorVersion` / kinds-mask bits if the output changes.
- Falling off still respawns at the last visited island.

## Acceptance
- Jumping near the top visibly drifts the player; low islands feel almost calm.
- The spiral level is beatable (validator: wind never moves a required jump out of reach; bridges and ladders stay safe, since wind only affects airborne control).
- Validate Seeds 0 invalid; wind sound audible and distinguishable between low and high.
- AGENTS.md contracts, controls and change log updated.

## Handoff
Spiral layout: `RunConfig.spiral` / `LevelDefinition.spiral` / `LevelPlan.spiral` (heading +50 degrees per link); level 8 uses it (rebuilt with menu 14). Wind gusts existed from T50. Snow-capped goal island (`Ground.Top(kind, snowCapped)`, fewer trees there) and wind ambience scaled by altitude and gusts (`AudioManager.AmbienceBoost` / `AmbiencePitch`, driven by `WindGusts`). Open: spiral for random levels (optional).
