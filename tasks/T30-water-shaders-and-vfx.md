# T30 — Water shaders + splash/drip VFX

**Owner:** A6 Tech Artist · **Depends on:** T01, T29
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Make water look like water in the art style, and give the interaction feedback.

## Deliverables
- `M_Water` (lake): translucent sky blue, depth tint, gentle ripple normals, white foam at the edges, glow-cyan sparkle
- `M_Waterfall`: scrolling streak + foam shader along the UV flow, bright at the lip, fading as it falls
- Particles: waterfall mist/spray at the lip and below, lake ripples, refill burst (droplets fly to the can), pour stream when watering, `VFX_CollectorDrip` and 'ready' sparkle, sprout 'thirsty' pulse (a subtle cyan ring, so players learn that sprouts need water)
- Prefab variants that attach to T29 pieces and to the player's can

## Steps
1. Build on top of the T01 materials and palette; no textures required.
2. Hook the VFX through `GameEvents` (`Watered`, `CanRefilled`, `CollectorReady`).
3. Test in the sandbox and at distance.

## Acceptance criteria
- [ ] Water reads at both close range and 30 m; no hard edges in the sky
- [ ] Pour, refill and thirsty-sprout cues are understandable without text
- [ ] VFX cost < 0.5 ms

## Fallback / cut plan
Cut: thirsty-sprout pulse, then ripples.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Progress (water pass, day 3)
- Done: `PlantGame/OvergrowthWater` shader (ripples, depth, glints, cyan rim) with light / mid / dark materials on every pond, stream and the collector reservoir; droplet HUD.
- Open: waterfall shader (`M_Waterfall`), foam at edges, splash / drip / pour VFX, refill burst, collector ready sparkle, thirsty-sprout pulse.
