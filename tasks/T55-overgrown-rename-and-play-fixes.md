# T55 — Rename to Overgrown, camera fade, soil patches, level generation fixes, test scene cleanup

**Owner:** A0 + A1 + A5 · **Depends on:** T14, T26, T45, T54
**Status:** REVIEW

## Goal
- Game renamed **Overgrown**; description "Grow plants to cross the floating islands." (title screen, README, AGENTS, CLAUDE, art bible, product name).
- Scenery between the camera and the player turns see-through (`CameraOcclusionFade`); trees, rocks and ruins no longer pull the camera in.
- Shrubs have no colliders; rocks, ruins, logs and trees still block.
- Soil patches sit lower in the ground and are tinted per plant kind (bridge tan, beanstalk dark green, mushroom violet, collector blue).
- Heat Seed melts frozen water with E (not F).
- Title world: half the floating debris, only the bigger pieces.
- Movement course holds one of every item; Plant test island chain, Plant gallery and World sandbox removed.
- Two vine bridges never lie close together (>= 50 degrees apart from one island, >= 7 m apart elsewhere); one or two decoy soil patches (`Soil_*_decoy`) on levels with more than 3 islands.
- Beanstalk: Up / Down arrows climb (`PlayerInputReader.ClimbAxis`); the player climbs on the start-island side of the stalk (`ClimbableVolume.playerBehind`, box spans both sides, grab only from the player's side, prefabs rebuilt with menu 8) so the camera sees open air, not the target island's cliff.
- Frozen water: cold glow on the ice, prompt "Frozen water: it needs a Heat Seed", F says the same.
- Inventory: after planting, thawing or gliding the selection moves to the next filled slot.
- Project naming: product / company name Overgrown, identifier `com.overgrown.game`, version 0.1.0 (the editor window title still shows the project folder `Game`: renaming that folder would break the layout contract).
- Vine bridges tilt up or down to the height of the next island (`LevelLayout.MaxBridgeRise` 3.4 m); measured on seeds 1-3: tip height equals the layout rise, horizontal reach unchanged.

## Acceptance
- `SeedCode.GeneratorVersion` 3 (layouts changed); `15 Validate Seeds` 0 invalid; decoys present on 5-island seeds.
- Compiles; scenes listed in Build Settings: Title, Main, Sandbox_Gameplay.

## Handoff
`Player/CameraOcclusionFade.cs` (candidates = renderers under `Scenery` / `SideDecor`, swaps to transparent clones, 10 Hz check), `PlayerController.UpdateCamera`, `LevelLayout.BridgeApart`, `WorldGenerator` decoys, `SoilPatchMesh.KindMat`, `PlayerInteraction.Use` (thaw), `MainSceneBuilder.TrimTitleDebris` (also applied to `Title.unity`), `ProjectSetup.ApplyLightingPreset` (moved from the removed `SandboxPlantsBuilder`). Not verified (also: a real beanstalk climb from the new side, camera on it, landing at the top, climbing down from the top): the camera fade in a real play session, that bridges now separate on every seed, soil look in a render, Heat Seed on ice by hand.
