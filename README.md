# Overgrown

_Grow plants to cross the floating islands._

A third-person exploration game on floating islands. You cannot jump the gaps, so you grow your way across: vine
**bridges**, beanstalk **ladders**, mushroom-patch **platforms**. Seeds come from seed plants on the islands, ride in a
4-slot inventory and are planted in soil patches; a watering can (2 charges) makes the sprouts grow. Every level is built
from a shareable **seed code**. Reach the goal on the last island.

Built in three days for a hackathon. **Status: the MVP is complete** (every task on `TASKS-MVP.md` is done). Later work,
including the Android and iOS ports, is in `TASKS-ANDROID.md`, `TASKS-IOS.md` and `tasks/`.

## How to play
| Key | Action |
|-----|--------|
| W A S D / arrows | Move (W / S or Up / Down also climb a beanstalk) |
| Mouse | Look |
| Shift | Run |
| Space | Jump (jumps off a beanstalk while climbing) |
| Q | Pick up the item you look at, or drop the selected one |
| Tab / Shift+Tab, 1-4 | Change the inventory slot |
| E | Use: plant the selected seed on a soil patch; a Heat Seed thaws frozen soil or water; the leaf glides mid-jump |
| F | Water a sprout, or refill the can at a pond, waterfall or Water Collector |
| Esc / hold R | Pause / restart the run |

Full table: `docs/controls.md`. Key hints are on by default and can be switched off in Options or the pause menu.

**Loop:** pick up seeds, plant them (E), water the sprouts (F), cross what grows. The can starts empty, so fill it at the
start island's water first. Dew Seeds grow a Water Collector (one charge every 20 s) for levels without ponds.

## Modes and levels
- No lose condition; falling just respawns you. The elapsed time is shown.
- Best times are saved per level.
- **Ten curated levels:** First Steps, Beanstalk Hill (autumn), Water from the Sky (rain, collector only), Skyfall (leaf
  glide), Stepping Stones, There and Back (sunrise), Snowed In (snow, Heat Seeds), Windy Summit (spiral, gusts), Into the
  Mist (fog), Storm Season (rain and gusts).
- **Generate level** builds a random world, or rebuilds any world from a typed or pasted seed code.

### Seed codes
Every generated level has a code of five dash-joined words, shown on the pause and win screens. Type or paste it into
the seed field of **Generate level** and the same world is built: same islands, plants, seeds and water.

## Features
- Plants from **Blender Geometry Nodes**: six seeded variants per kind (bridge, beanstalk, mushroom patch, Water Collector),
  baked with a `growth_t` vertex colour and collision proxies. A shared grow shader reveals each plant as it grows and
  its colliders switch on step by step.
- **Deterministic world generator** (`System.Random(seed ^ salt)`): island graph, gaps a jump cannot clear, seed supply,
  water access and side islands, then a validator that simulates the watering can so every level is beatable.
- Themes with their own sky and weather (sunny, rain, autumn, snow, fog, sunset, sunrise), wind gusts, four goal types
  (flag, golden fruit, gate, campfire).
- 8-bit music per level, ambience that matches its source, surface footsteps, synthesized SFX.

## Getting started
1. Open `Game/` in Unity Hub with **Unity 6000.6.5f1** (URP).
2. Open `Assets/Scenes/Title.unity` and press Play. `Sandbox.unity` is a movement course with one of every item.

Every generated asset is committed. After changing a builder, rerun its **PlantGame** menu entry: **Rebuild Core Assets**
(project settings, materials, items, player, sandbox), **Plants > ... Prefabs**, **Scenes > World Kit** (the runtime asset
list), **Scenes > Main**, **Scenes > Title**, **Assets > Materials / Water Materials**.

### Build
**PlantGame > Platforms > Build macOS Standalone** writes `Builds/Overgrown-mac/Overgrown.app` (git-ignored), then:
```bash
open Builds/Overgrown-mac/Overgrown.app
```

### Testing and QA
Nothing here is part of a player build (the test assemblies are editor-only). Run in this order before a release:

| Layer | How to run | What it covers | Time |
|-------|-----------|----------------|------|
| 1. Unit / regression tests | **PlantGame > Test > Run Unit Tests** (summary and failures in the Console; **Open Test Runner** for the full tree) | inventory, seed codes, save service, watering can, level themes, world generation (determinism, 100 seeds beatable, golden seeds), the ten curated levels | ~3 s |
| 2. Seed validation | **PlantGame > Test > Validate Seeds** | 10,000 seed-code round trips and 200 generated levels through the validator | ~5 s |
| 3. Level smoke test | **PlantGame > Test > Smoke Test Levels** | plays each curated level in play mode, logs frame time and draw calls to `Game/Logs/smoke.txt` | ~1 min |
| 4. Manual pass | `docs/qa-checklist.md` (menus, controls, plants, water, audio, saves, mobile) in a rebuilt player | everything a script cannot judge | ~30 min |

Helpers: **Test > Game View: Phone 2340x1080** (reference phone), **Test > Touch Controls (editor test)** (touch UI in the editor),
**Test > Play From Title** (Play starts at the Title scene). Controls reference: `docs/controls.md`; frozen seeds: `docs/test-seeds.md`.

- **Golden seeds** (`GoldenSeedsKeepTheirWorld`): if one fails, a generator change altered existing worlds and shared seed codes.
  Bump `SeedCode.GeneratorVersion` if that was intended, then re-record the values in `WorldGenerationTests.cs`.
- **Adding a test:** put an NUnit class in `Game/Assets/Tests/EditMode/` (assembly `PlantGame.Tests.EditMode`); it appears in both menus at once.
- **Audio changes** need the extra check in `AGENTS.md` section 2 (toggle Sound / Music in a rebuilt Mac player).

## Repository
- `AGENTS.md`: contracts (scale, plant envelopes, C# API, controls), roles and rules. Read it before changing code.
- `TASKS-MVP.md`: the finished MVP board; `TASKS-ANDROID.md` / `TASKS-IOS.md`: open mobile tasks. Each open task has a file in `tasks/`.
- `Game/`: the Unity project. Scenes, prefabs and materials are rebuilt by the **PlantGame** editor menus.
- `Source/Blender/`: `plants.blend`, `characters.blend` and the Python scripts that build and export them
  (`docs/pipeline.md`). Raw exports and downloaded packs are git-ignored.
- `docs/`: art bible, pipeline, controls, mobile design, QA checklist, test seeds, bug list, task log.

## Credits (third-party art)
Full list with licences: `Game/Assets/ThirdParty/LICENSES.md`.
- Quaternius Stylized Nature MegaKit (CC0), https://quaternius.com
- FreeStylized Ruins Modular Kit 01 (royalty free), https://freestylized.com
