# Overgrown

_Grow plants to cross the floating islands._

A third-person exploration game on floating islands. You cannot cross the gaps, so you grow plants:
vine **bridges**, beanstalk **ladders**, shelf-fungus **platforms**. Seeds are found on the islands, carried in a
4-slot inventory and planted in soil patches; a watering can (2 charges) makes sprouts grow. Every run is built from a
shareable **seed code**. Goal: reach the Heart Tree.

Built in three days for a hackathon. The demo is showcased manually (no recorded video or gallery scene).

## How to play
| Key | Action |
|-----|--------|
| W A S D / arrows | Move (W / S or Up / Down also climb up / down a beanstalk) |
| Mouse | Look |
| Shift | Run |
| Space | Jump (jumps off a vine while climbing) |
| Q | Pick up the item you look at, or drop the selected one |
| Tab / Shift+Tab | Cycle the inventory slot |
| E | Use: plant the selected seed on a soil patch (a Heat Seed melts frozen water) |
| F | Water a sprout, or refill the can at a lake, waterfall or Water Collector |
| Esc / hold R | Pause / restart the run |

Full table: `docs/controls.md`. Key hints are on by default and can be switched off in the pause menu.

**Loop:** pick up seeds, plant them in soil patches (E), water the sprouts (F), cross what grows. The can starts empty:
fill it at the start island's water. Dew Seeds grow a Water Collector (one charge every 20 s) for levels without lakes.

## Modes and levels
- **Exploration:** no lose condition; falling just respawns you.
- **Challenge:** a countdown per level; reach the Heart Tree before it hits zero. Time fruits add seconds.
- Best times are saved per level and per mode.
- Ten curated levels (sunny, autumn, rain, snow, wind, fog, sunset, sunrise), plus **Random Level** and **Enter Seed Code**.

### Seed codes
Every generated level has a shareable code of five dash-joined words, shown on the win and pause screens. On the
title screen choose **Enter Seed Code**, paste or type it, and the same world is built (same seed, same islands, same plants).

## Tech overview
- **Unity 6 (URP)**, C#, new Input System, Cinemachine. Project in `Game/`.
- **Blender 5.2 Geometry Nodes** build the plants. Each plant kind has six seeded variants, baked with a `growth_t`
  vertex colour and collision proxies, exported to FBX (`Source/Blender/scripts/`).
- A shared grow shader reveals the mesh as the plant grows; colliders switch on progressively.
- A deterministic world generator (`System.Random(seed ^ salt)`) lays out islands, gaps, plants and water and
  validates that every level is beatable.
- Everything is rebuildable from editor menus (**PlantGame > Build ...**) and Blender scripts; see `docs/pipeline.md`.

## Where things are
- `AGENTS.md` — contracts, roles, rules. Read first.
- `TASKS.md` — task board; details in `tasks/`.
- `Game/` — Unity project. Open this folder in Unity Hub (version in `AGENTS.md` section 2).
- `Source/Blender/` — `.blend` sources and the python scripts that rebuild/export assets.
- `docs/` — art bible, pipeline, controls, test seeds, QA checklist, task log.

## Quick start
1. Open `Game/` in Unity.
2. Run menu **PlantGame > Build All** once (layers, tags, physics, materials, player prefab, scenes).
3. Open the Title scene and press Play, or open `Game/Assets/Scenes/Sandbox_Gameplay.unity` for one of every item.

## Credits (third-party art)
Full list with licences: `Game/Assets/ThirdParty/LICENSES.md`.
- Kenney Nature Kit and Fantasy Town Kit (CC0), https://kenney.nl
- Quaternius Stylized Nature MegaKit (CC0), https://quaternius.com
- FreeStylized Ruins Modular Kit 01 (royalty free), https://freestylized.com
