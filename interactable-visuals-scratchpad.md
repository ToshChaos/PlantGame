# Interactable visuals scratchpad (seed sources, seeds, grown plants)

Working notes for the next session that works on the look of the things the player picks up, plants and grows. Started 2026-11-16.
The first pass added motion and glow in code only: no Blender rebake, no prefab or scene rebuild, no layout or collider change.

## State of the first pass

Checked in the editor's play mode on 2026-11-16 (High tier; Medium for the sources): a sunny random level (seed 3, 7 islands), level 5
(bamboo, beans, puffballs, leaf; bridge, beanstalk, mushrooms grown), level 3 in the rain (dew patch, both collectors) and level 7 in the
snow (pumpkins). Console clean, EditMode tests 51 / 51. What that covered and what it did not is in `docs/task-log.md`.

| Thing | What was added | Where | Numbers to tune |
|---|---|---|---|
| Seed pickup | Lifts off the ground once at rest, bobs, leans, spins; glow breathes | `Inventory/ItemBob.cs` | `hoverGap` 0.1 m, `bobHeight` 0.08 m, `bobSeconds` 2.6, `tiltDegrees` 6, `glowRange` 0.8-1.9 |
| Seed pickup | Motes in the seed's colour rise from it (High tier only) | `ItemBob.BuildVisual` | rate 3 / s, size 0.12 m, max 10 |
| Seed source | Pollen in the seed's colour over every source (all tiers, half rate on Medium / Low) | `WorldGenerator.SeedSource` | rate 6 / s, size 0.2 m, box 2.6 m wide, 3 m tall for groves and poles |
| Seed source | Fruit and leaves sway, puffballs breathe, pumpkin faces flicker (High tier only) | `World/PatchLife.cs` | `sway` 5 degrees, `breathe` 3 %, `flicker` 30 %, `activeDistance` 32 m |
| Seed source | Dewdrops on the succulents glint | `Scenery.SucculentPatch` (`GemSparkle`) | `baseLevel` 0.7, `flare` 1.5 |
| Seed source | A puff where a regrown seed appears | `SeedTree.SeedRegrown`, `GrowthVfx.OnSeedRegrown` | 14 dots |
| Seed source | The moss on the puffball log lies along the log (it stood upright through it) | `Scenery.ShelfPatch` | |
| Sprout | Bounces twice every 2.8 s while it waits for water | `PlantInstance.Update` | `SproutEvery`, `SproutBounce` 16 % |
| Grown plant | Leaves and flowers sway; flower centres pulse | `Art/Shaders/PlantGrow.shader` | `_Sway` 0.022 m, `_GlowPulse` 0.7 (shader defaults, no material stores them) |
| Grown plant | A few fireflies stay round it (High tier only) | `GrowthVfx.Fireflies` | rate 3-6 / s by size, size 0.2 m, max 30 |

The looping motes (seeds, pollen, fireflies) come from one builder, `Core.Motes.Ambient`, and one material, `Motes.Soft`: an
alpha-blended dot with a solid core. The first two tries (the additive glow dot `GrowthVfx` uses for its bursts, then a soft alpha
dot at 0.1 m) were close to invisible against the sky; 0.2 m with a core reads from across a bridge without turning into snow.

## How to check it (10 minutes)

1. Play mode, then a level: `GameBootstrap.PendingRun = level.ToConfig(); PlayerHud.SkipMenuOnce(); SceneManager.LoadScene("Main");`
   and `Application.runInBackground = true`. Level 5 has four source kinds and three plant kinds in the sun, level 3 the dew patch
   and the collectors, level 7 the pumpkins. Do not edit scripts while playing: stop, edit, wait for the reload, play again.
2. Grow everything at once: for each `SoilPatch` whose name has no `alt` / `decoy`, `UseItem(Kit.Item(acceptedItemId), player)`
   (a frozen one first takes `Kit.Item("heat_seed")`), then `Water(player)`; plants are grown about 8 s later.
3. Captures: a temporary `Camera` copied from `Camera.main` with `UniversalAdditionalCameraData.renderPostProcessing`, rendered into a
   1280 x 720 RenderTexture, PNG to `docs/shots/` (git-ignored). Sources from 6 m, seeds from 1.6 m, a plant from 1.3 times its bounds.
   A fixed offset often lands inside a tree: try eight directions and keep the one with the longest free ray.
4. Motion does not show in one frame: read `Visual.localPosition`, a plant's `localScale`, a `Pumpkin0` property block or a
   `Puffball0` scale in three calls a second apart.
5. Tune particles live (sizes, rates and the dot texture on `Motes.Soft` can be set from `Unity_RunCommand` while playing), then
   copy the numbers into the code.
6. Medium: `GameSettings.Quality = 1`, load the level again, and **set it back to 2 before leaving play mode** (it is saved, and it
   rewrites `QualitySettings.asset`).

## Not looked at yet

- **The motion as motion.** Stills and sampled values say the bob, the bounce, the sway, the breathing and the flicker run; nobody
  has watched them at speed. The first things to judge by eye: is 16 % too much for the sprout, does 2 cm of leaf sway show at all
  from the third-person camera, do the flower centres pulse too fast.
- The regrow puff (a seed regrows after 60 s), the focus highlight on a hovering pickup (it also tints the pickup's motes), a seed
  dropped with Q on a slope or against a wall.
- Low tier, a rebuilt Mac player, a phone. Sunset / sunrise / fog themes, and the title screen (its showcase plants and seed sources
  go through the same code).
- Frame time. Rough count on level 5, High: about 20 pickups x 10 motes, 4-8 sources x 32, up to 8 plants x 30, all one material.

## Things to know before changing it

- **Shader sway moves every leaf vertex by its own phase**, so a leaf's root drifts up to about 2 cm from its stem. If it reads as
  detached, lower `_Sway` or scale the offset by distance from the stem (needs a baked attribute: vertex colour B is free).
  Parts that sway: ids 0.5-0.88 (leaves, yellow flowers) and above 0.97 (blossoms). Decks, vines, caps and bowls never move.
  The beanstalk's rungs are leaf parts, so they sway too (the climb volume does not care; they looked fine in the captures).
- **Hover height** comes from the mesh bounds and the solid sphere collider. The parachute leaf already floats 0.25 m in its mesh,
  so it only bobs. A seed resting against a wall or half inside a rock lifts straight up regardless.
- **`FocusHighlight` and `ItemBob` both write the pickup's property block.** `ItemBob` stands back while the highlight is enabled.
- **`PatchLife` finds pieces by name** (`Fruit*`, `Bean`, `Leaf`, `Puffball*`, `Pumpkin*`, direct children of the source). A new patch
  builder must keep those names or get its own rule. It is not added on Medium / Low, where `WorldOptimizer` merges the pieces.
- **Fireflies on a stretched bridge**: the emitter is a child of the scaled plant, its box is divided by `lossyScale`. A strongly
  tilted bridge may get a slightly skewed box.
- Level-authored plants (`GrowInstantly`: the single-use collector) raise no `Grown` event, so they get no fireflies.
- Motes in the seed's own colour are quiet on a like-coloured background (green bamboo pollen in front of the grove, white puffball
  pollen against cloud). They show against the ground and the sky.

## Ideas not done

- **Soil patches** (the planting spots) were left alone: a ring of small stones or a faint glow in the wanted seed's colour, a
  dust puff when the bed sinks (`SoilPatch.Vanish`).
- **Pickup**: the seed vanishes in one frame. A short fly-to-hand (scale down along an arc to the player, 0.2 s) would read better
  than the burst alone. `WorldItem.Interact` destroys the object at once, so it needs a detached copy of the `Visual` child.
- **Seed regrowth**: a bud that swells on the source for the last seconds before a seed drops (`SeedTree._regrowTimer` is private).
- **Water Collector**: droplets falling into the bowl while it fills, a glint on the water when a charge is ready
  (`GameEvents.CollectorReady` carries no position: add one or let `WaterCollector` own the effect).
- **Mushroom caps**: a squash and spring when the player lands on one (per-cap transform does not exist: the bake is one mesh, so
  this needs a shader offset driven by a per-plant "hit" position, or separate cap objects from Blender).
- **Bridge**: flowers that open as the growth front passes (needs a per-flower growth offset in the bake), fruit or lanterns
  hanging under the deck.
- **Beanstalk**: slow whole-stalk sway that grows with height (the climb volume and `Land` would have to follow, or the sway must
  stop while the player climbs).
- **Prefab instead of runtime split**: `ItemBuilder` could build the pickups with the `Visual` child (and `ItemBob.visual`
  serialized) so `ItemBob.BuildVisual` only wires things up. Needs PlantGame > Assets > Items and a look at every user of
  `worldPrefab.GetComponentInChildren<MeshFilter>()` (`WorldGenerator.SeedSource`, `PlayerHud`).
- **Blender side** (A3): more silhouette variety per seed (`SeedMeshes` is code, the plants are bakes), a second bloom colour per
  bridge variant through `_FlowerColor` on a per-plant property block (no rebake needed).

## Unity access (2026-11-16)

This session's tool list had no `mcp__unity-mcp__*` tools (the server is in the desktop app's own config, and the editor showed
"claude-ai" connected, but the Code session did not get the tools). What worked: a small stdio MCP client talking to the relay
(`~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp`), calling `Unity_RunCommand` and
`Unity_GetConsoleLogs`. The editor lists that client separately (it showed up as `cli`, process `python3`) under
Project Settings > AI > Unity MCP, and answers "Connection revoked" until the user allows that entry there: ask, do not work round it.
The approvals are kept in `Game/Library/AI.MCP/connections-v2.asset` (readable, which tells you the entry's name and status).
The Unity CLI (`unity status`) finds no editor: `com.unity.pipeline` is not in `Packages/manifest.json`.

Without any connection: the open editor recompiles when it notices file changes, and `Game/Logs/Editor.log` plus
`Game/Logs/shadercompiler-*.log` show the result. After a script edit the relay answers "Unity not detected" for about 20 s.
