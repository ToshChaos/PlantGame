# Overgrown

_Grow plants to cross the floating islands._

A third-person exploration game on floating islands. You cannot cross the gaps, so you grow plants:
vine **bridges**, beanstalk **ladders**, shelf-fungus **platforms**. Seeds are found on the islands, carried in a
4-slot inventory and planted in soil patches; a watering can (2 charges) makes sprouts grow. Every run is built from a
shareable **seed code**. Goal: reach the Heart Tree.

_3-day hackathon MVP. Stub README — T24 writes the real one (screenshots, credits, how to play)._

## Where things are
- `AGENTS.md` — contracts, roles, rules. Read first.
- `TASKS.md` — task board; details in `tasks/`.
- `Game/` — Unity 6 (URP) project. Open this folder in Unity Hub.
- `Source/Blender/` — `.blend` sources and the python scripts that rebuild/export assets.
- `docs/` — art bible, pipeline, controls, test seeds, QA checklist, task log.

## Quick start
1. Open `Game/` in Unity (version in `AGENTS.md` section 2).
2. Run menu **PlantGame > Build All** once (sets layers/tags/physics, builds materials, player prefab and sandbox scenes).
3. Open `Game/Assets/Scenes/Sandbox_Gameplay.unity` and press Play. Controls: `docs/controls.md`.

## Credits (third-party art)
Full list with licences: `Game/Assets/ThirdParty/LICENSES.md`.
- Kenney Nature Kit and Fantasy Town Kit (CC0), https://kenney.nl
- Quaternius Stylized Nature MegaKit (CC0), https://quaternius.com
- FreeStylized Ruins Modular Kit 01 (royalty free), https://freestylized.com

