# T54 — Island dressing: grass, decals, shiny bits, overgrown ruins, pond plants

**Owner:** A5 + A6 · **Depends on:** T39, T35, T36
**Status:** REVIEW

## Goal
More life on every island: lots more grass on grassy islands (a little on mud and rock, rock about 15% less than first tried), mud and stone decals on bare ground, moss, low shrubs, sticks and pebbles everywhere; marble chips and brick chunks round ruins, a few glinting gems, shiny debris hovering outside the rim; some ruins heavily vine-covered; plants on ponds and some murky (green, undrinkable) ponds; no random spheres where rocks, gems or bricks fit.

## Acceptance
- Shots `docs/shots/t54_*.png`; `15 Validate Seeds` still 0 invalid; no new console errors.
- Murky ponds never offer a refill (no `WaterSource`, no trigger).

## Handoff
`World/Overgrowth.cs` (`Clutter`, vine-heavy ruins), `World/Scenery.cs` (`ShinyBits`, `MeshPrim`; spheres replaced by `DebrisMeshes` rocks), `World/GemSparkle.cs`, `World/WaterPlants.cs`, `WorldGenerator` (`MurkyMat`, `Isle.murky`, `pondDiscs`). Murky ponds change the carved hollows, so item positions on those islands can differ from earlier builds. 11 unused Quaternius textures (22 MB) deleted. Not measured: frame time (T23). Not viewed in a render: a real-water pond close-up, a vine-heavy ruin.
