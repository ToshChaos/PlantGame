# T43 — Ambience that matches its source, mysterious ruin sounds

**Owner:** A7 · **Depends on:** T17, T18, T36
**Status:** DONE

## Goal
- **Ambient sounds relate to what they represent:** waterfalls/ponds sound like water, wind in trees rustles leaves, seed trees and plants have organic sounds, rain levels have rain, and so on. Audit every `AudioSource`/ambience loop and replace generic ones. 3D spatial, distance-limited.
- **Mysterious sounds near ruins:** low drones, faint whispers/chimes, distant stone resonance, played on a slow random schedule when the player is near ruin pieces (some islands only, seeded).
- All sounds made or logged per the licence rule (`docs/audio/make_audio.py`, `LICENSES.md`).

## Acceptance
- A listening pass on a lake, a waterfall, a forest island and a ruin island, each sounding distinct and fitting.
- Respects the SFX/ambience volume settings; nothing plays on the title screen except music.

## Handoff
`docs/audio/make_audio.py world_ambience()` writes loops (`water_babble`, `waterfall_rush`, `leaf_rustle`) and ruin one-shots (drones, whispers, stone, chimes) to `Resources/Audio`. `Audio/WorldAudio.cs`: `LoopEmitter` (3D, distance limited, silent on the title, follows Sound) on ponds/streams, waterfalls and leafy islands; `RuinSounds` near the ruin of ~60 % of islands (slow random schedule, only within 22 m of the player). Seed trees keep their own loop; rain levels keep `amb_rain`. Compiles and the emitters spawn; NOT listened to yet (levels, mix and loudness unchecked).
