# T11 — Growth system: SoilPatch, growth, colliders, climb

**Owner:** A2 Botanist · **Depends on:** T01, T09 (start with a placeholder) spike → 2
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Planting a seed makes a plant grow over a few seconds into something the player can cross or climb. This is the core mechanic; prove it with a placeholder on Day 1.

## Deliverables
- `PlantDefinition`, `SoilPatch`, `PlantInstance` per contract 5.5 in `Scripts/Plants`
- **Placeholder plants** (Day 1): a stretched cube/capsule with `M_PlantGrow` for each kind so the mechanic works before Blender art lands
- `PlantInstance`: drives `_Growth` 0→1 over `growSeconds` with `growCurve` via MaterialPropertyBlock; enables colliders progressively from `COL_*` proxies by index; enables `CLIMB_*` triggers (layer `Climbable`) when ladder is mature; exposes `IsGrown`
- Orientation/alignment: the plant is spawned at the patch `Socket_Soil` and rotated to face the nearest `Socket_Land` of another island (or the patch's forward)
- Variant choice: `variants[new System.Random(seed ^ patchId).Next(n)]` — deterministic
- Prefab builder editor script: turns imported FBX variants into prefabs with colliders/layers (coordinates with A3 / T09)
- **Water gating:** non-Collector plants stop in a `Sprout` state (`_Growth` ~0.08, no colliders) after planting and implement `IWaterable`; `Water()` starts growth. The Day-1 spike skips this (`needsWater` off); T27 switches it on.
- Events: `PlantStarted`, `PlantSprouted`, `PlantGrown` (SFX/VFX hook); safe to plant once per patch; invalid use gives feedback
- Player safety: if the player stands on a growing plant it must not eject them (colliders only enable when the geometry has reached them; platforms rise with the player on top allowed)
- `Sandbox_Plants.unity` with a patch, a gap, all three kinds

## Steps
1. Day 1: SoilPatch + PlantInstance with placeholder meshes; hook `UseItem` from T04.
2. Verify crossing the placeholder bridge using the player from T03.
3. Day 2: swap in real prefabs from T09; implement progressive colliders and climb triggers.
4. Add editor menu `PlantGame/Build Plant Prefabs` and document it.
5. Test determinism: same (seed, patch) → same variant after reload.

## Acceptance criteria
- [ ] Seed used on a soil patch grows the right plant kind over `growSeconds`
- [ ] Player can cross the bridge, climb the ladder, hop up the platform pads in the sandbox
- [ ] No collider exists where mesh hasn't grown yet; no player ejected while standing on a growing plant
- [ ] Same seed + patch picks the same variant every time
- [ ] `PlantStarted`/`PlantGrown` fire once; a watered sprout grows, an unwatered sprout waits indefinitely

## Notes
- Colliders: use the proxies from Blender, not mesh colliders (stable and cheap).
- Keep the shader contract: vertex colour R = `growth_t`.

## Fallback / cut plan
If progressive colliders are fiddly, enable all colliders at 60% growth.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
