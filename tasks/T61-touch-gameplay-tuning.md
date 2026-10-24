# T61 — Touch gameplay tuning: camera, climbing, jumps, glide

**Owner:** A1 Gameplay · **Status:** POST-MVP · **Depends on:** T57, T58

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Camera: touch look needs its own speed curve; auto-follow (swing the camera behind the player when moving forward, v2.3) probably wants to be stronger on touch since the look thumb is often busy. Verify `CameraOcclusionFade` and the third-person distance on a 6 inch screen (the player may need a slightly larger FOV or a closer camera).
- Climbing: stick Y climbs, context shows Grab/Let go; jump off the vine from the Jump button; no accidental grabbing when only walking past a stalk.
- Jump precision: bridges are 1.2 m wide with gaps that need a plant; check the stick has enough resolution near the walk speed, add input smoothing / snap-to-8-directions option for bridges if it is hard to stay on. Consider assisted ledge/bridge edge friction only if testing shows falls are frequent (respawn already exists).
- Parachute leaf (E mid-air): context button swaps to Glide while airborne with the leaf selected; wind gusts on Windy Summit must not make the stick unusable.
- Challenge mode readability: the timer and time-fruit pickup text sized for a phone.

## Acceptance
- Two testers who have not played the game finish Level 1 on a phone (Exploration) without verbal help; notes on falls and mis-grabs are in `docs/task-log.md`.

## Handoff
_(fill in when done)_
