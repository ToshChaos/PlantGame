# T31 — Player model (blocky, white/black/gold) + third-person camera

**Owner:** A1 Gameplay (controller, prefab) + A3 (Blender model) · **Depends on:** T02, T03 · **Estimate:** 2h · **Day:** 1
**Status:** REVIEW

> Read `AGENTS.md` first. MVP has exactly **one** player model.

## Goal
A simple, readable "Roblox-style" blocky character the camera orbits behind, in **white / black / gold**.

## Deliverables
- `Source/Blender/scripts/char_player.py` (`build_all()`): blocky character `Char_Player_01` in `Source/Blender/characters.blend`,
  parts as separate meshes under joint empties (`Joint_Head`, `Joint_ArmL/R`, `Joint_LegL/R`) so Unity can swing them; pivot at the feet,
  facing Unity +Z; one colour per mesh, suffix `_W` white, `_K` black, `_G` gold.
- FBX in `Game/Assets/Art/Models/Characters/`, materials `M_PlayerWhite`, `M_PlayerBlack`, `M_PlayerGold`.
- `Scripts/Player/PlayerAnimator.cs`: procedural walk/run/jump/climb limb swing (no rig, no Animator).
- Third-person orbit camera in `PlayerController`: mouse orbits, WASD relative to the camera, model turns to the move direction,
  camera pulls in against geometry.
- Player prefab rebuilt by `PlantGame > 4 Build Player Prefab`.

## Acceptance criteria
- [ ] Model reads at a glance: white body, black legs/gloves/face, gold trim; ~1.8 m tall, fits the 0.35 m capsule
- [ ] Mouse orbits the camera; W walks away from the camera; the character faces where it moves
- [ ] Limbs swing while walking, faster when running; pose changes in the air and on ladders
- [ ] Camera does not clip into ground or walls behind the player
- [ ] Console clean, existing T03 behaviour (jump, climb, respawn) unchanged

## Requests (to/from other roles)
- T04 (interaction): the focus ray should come from the camera (`PlayerController.ViewTransform`), not the head.

## Handoff
- **Blender:** `Source/Blender/scripts/char_player.py` → `build_all()` rebuilds `Source/Blender/characters.blend`, collection
  `Char_Player_01` (27 objects, 2.3k tris, 1.78 m + 0.15 m gold sprout), exports `exports/Char_Player_01.fbx` with `CHAR_FBX_OVERRIDES`.
  Render: `docs/shots/T31_player_blender.png`. `sync_to_unity.py` now maps `Char_*` → `Art/Models/Characters/`.
- **Pipeline fix:** `export_collection(..., overrides=)` added; hierarchies need it (see `docs/pipeline.md`, "Parented hierarchies").
- **Unity:** materials `M_PlayerWhite/Black/Gold` (`Palette.PlayerWhite/PlayerBlack/Gold`), `PlayerPrefabBuilder` adds a `Model`
  child (unpacked FBX, material by mesh suffix) and `PlayerAnimator`; camera is `CameraPivot/Camera` at 4.5 m, FOV 60.
- **API:** `PlayerController.model`, `cameraTransform`, `Yaw`, `Velocity`, `pitchMin/Max`, `cameraDistance`; `EyeTransform` = camera.
  Camera spring arm spherecasts `Layers.WalkableMask`.
- **30-second test:** open `Sandbox_Gameplay`, Play: mouse orbits, WASD moves relative to the camera, the character turns and swings
  its limbs, arms spread in the air, camera pulls in near walls. Shot: `docs/shots/T31_player_walk.png`.
- **Verified:** W+Shift runs 6 m/s along camera forward; D moves +X with the model facing 90°; airborne pose; console clean.
- **Known issues:** climb pose and camera pull-in were not eyeballed in play (code paths only); no head-look; the camera can
  still clip through thin Plant colliders it is inside of.
