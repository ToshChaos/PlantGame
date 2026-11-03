# T48 — Concave mushroom platforms

**Owner:** A3 (+ A2 for colliders) · **Depends on:** T08, T09, T11
**Status:** DONE

## Goal
The shelf-fungus platform pads are too flat. Make each pad **concave**: a shallow dish with a raised, slightly curled rim and a lower centre, so it reads as a mushroom cap seen from above and feels like a bowl to stand in. Keep the underside gills/stalk readable.

## Notes
- Edit the Geometry Nodes group (`plants_*` scripts in `Source/Blender/scripts/`), re-bake the 6 variants, re-export and sync (T09 pipeline). Keep the `growth_t` vertex colour, `Anchor`/`Tip`/`Land` empties and the tri budget (<= 4k).
- Keep the contract: pads >= 1.5 m wide, step rise <= 1.2 m. Dish depth about 0.15-0.3 m, so jumping between pads is not harder.
- `COL_<NN>_<idx>` proxies must follow the new shape (a flat box top is fine if the dish is shallow, otherwise add a low rim or use a convex-decomposed collider) so the player does not float above the centre or catch on the rim.

## Acceptance
- Screenshot of the pads before/after in `docs/shots/`; the player walks across a spiral of pads without snagging or hovering.
- Platform plants in the curated levels still grow, show the sprout state and pass Validate Seeds.

## Handoff
`plants_platform.py`: caps are 16-sided dishes (`DISH` 0.22 m, rim at the pad point's height, so rise / reach numbers are unchanged), white spots follow the dish, collider boxes sit halfway down the dish. Re-baked, exported, synced; `PlatformPrefabBuilder` rerun. Shot: `docs/shots/T48_concave_pads.png`. Not yet walked in play.
