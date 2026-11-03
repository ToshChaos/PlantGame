# T39 — Overgrown feel: leaf litter, moss, vines

**Owner:** A5 + A6 · **Depends on:** T35, T36, T38
**Status:** DONE

## Goal
The game is called Overgrown: islands should look reclaimed by plants.
- **Leaves on the ground:** scattered leaf litter decals/meshes, tinted like the trees on that island (autumn levels: reds and oranges).
- Moss patches on rocks and ruins, ivy/vines hanging from ruin walls and island rims, ferns, tall grass and flower clumps filling the empty ground.
- Ruins get visibly swallowed by roots/vines. Keep the vertex-colour / simple-material look; watch the tri budget and batching (T23).

## Acceptance
- Side by side with a pre-T39 screenshot an island reads as lush and overgrown, on every theme (Sunny / Rain / Autumn).
- Seeded and deterministic (`System.Random`), no overlap with soil patches, items, goals or walking lanes.
- No noticeable frame-time regression.

## Handoff
`World/Overgrowth.cs` (called from `Scenery.Decorate`, own random): leaf litter rings under trees + loose patches (theme colours), moss patches (thick around ruins), hanging vines on the rim and ruin walls; merged into a few meshes per island under an "Overgrowth" child that the terrain Lift skips. Ferns/tall grass scatter added in Scenery. Also cosmetic `World/FallingBits.cs`: crumbling rim chips on ~50 % of mud / 80 % of rock islands, and rare leaves shed by leaf trees within 6 m of the rim. Screenshot `docs/shots/T39_overgrown.png` (level 2). Not checked: other themes, frame time, falling bits seen in motion.
