# T47 — Surface footsteps and vine-bridge sounds

**Owner:** A7 + A1 · **Depends on:** T18, T35 (T45 for snow)
**Status:** DONE

## Goal
- **Footsteps per surface:** each island's ground kind picks the footstep set: grass (soft rustle), mud (squelch), rock (hard tap), snow (crunch, with T45), plus wood/stone for ruin floors if cheap. Footsteps follow the surface actually under the player (raycast / island ground kind), also for landing thuds. Pitch/volume variation, no repeats back to back.
- **Vine bridge sound:** walking on a grown bridge plays creaking and rope/vine rustle (different from ground steps), a stronger creak on landing or running, and a soft sway/creak ambience when standing on it. Sources are 3D and fade on leaving the bridge. Optional: a short growth creak while the bridge grows.
- Sounds generated or logged per the licence rule (`docs/audio/make_audio.py`, `LICENSES.md`); respects SFX volume.

## Acceptance
- Walk across grass, mud, rock and a bridge and hear four distinct sounds; ladder and platform plants keep their current steps unless trivial to improve.
- No footsteps while airborne or climbing; no console errors.

## Handoff
Footsteps by island material (`M_Terrain` grass, `M_TerrainMud`, `M_TerrainRock`/`M_Stone`, `M_TerrainSnow`), `footstep_bridge` on grown vine bridges plus `bridge_creak` every 4th step, on landing and while standing still. Clips come from `docs/audio/make_audio.py` (`surfaces_extra`, own random). Scenes 12/16 rebuilt to pick them up. Not yet listened to in play.
