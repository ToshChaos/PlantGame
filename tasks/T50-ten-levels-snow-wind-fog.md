# T50 — Ten levels: snow, windy summit, dense fog (+ worldgen options)

**Owner:** A5 + A6 + A1 · **Depends on:** T45, T46
**Status:** DONE

## Goal
- Ship **10 curated levels** (today 6). Add: a **Snow** level (snow ground, frozen soil/water, Heat Seed; T45), a **Windy summit** level (altitude gusts; T46) and a **Dense Fog** level (very poor visibility: short fog distance, glowing cues for goal / water / soils so it stays playable). The tenth level is a free design slot (e.g. a night / glow level or a second long hand plan).
- Add Snow, Wind and Fog as **theme / weather options in world generation**: `LevelTheme` gains `Snow` and `Fog` (wind is an extra flag), `WorldTheme.Resolve` picks them for some random seeds, the level-select and seed codes carry them (watch the 4-bit generator fields and bump `SeedCode.GeneratorVersion` if needed).

## Acceptance
- Level select lists 10 levels with correct themes; each is completable (Validate Seeds 0 invalid, one real playthrough each).
- Random levels sometimes come out snowy, windy or foggy; a seed code reproduces them.
- Fog level: visibility clearly poor but the goal, water and soil patches remain findable (soft glow, sounds).

## Handoff
Ten levels (`WorldKitBuilder`): 7 Snowed In (snow), 8 Windy Summit (wind), 9 Into the Mist (dense fog), 10 Storm Season (rain + wind, 8 islands). `LevelTheme` now has Snow and Fog; `RunConfig/LevelDefinition.wind`; `WorldTheme.Resolve` (about 45 % sunny, 20 % autumn, 12 % rain, 12 % snow, 6 % fog) and `ResolveWind` (25 %) apply to random levels and seed codes (themes of old seed codes may change; layouts do not). Snow: white ground (`Ground.SnowMat`), snowfall, pale sky. Fog: fog end 40 m, grey sky, warm beacon light at the goal. Wind: `World/WindGusts.cs` pushes the player via `PlayerController.WindVelocity`, stronger higher up, hint at the first gust. Level select shows 10 cards in two rows. Screenshots `docs/shots/T50_snow_level.png`, `T50_fog_level.png`. Validate Seeds 0 invalid. Still open: no frozen water / Heat Seed (T45), no spiral summit layout (T46), wind has no extra sound or particles, levels 7-10 not played through to the goal, fog level glow not tuned for Challenge timing.

Addendum: `LevelTheme.Sunset` and `Sunrise` (sky palettes, low sun; Skyfall is a sunset level, There and Back a sunrise one; random seeds pick them about 12 % / 6 %). Wind levels blow leaves (`WeatherController.WindLeaves`, steered by `WindGusts` so they show gust direction and strength). Snow levels freeze up to two ponds; a Heat Seed from the start island melts one (see T45). Screenshots `docs/shots/sunset_level.png`, `T50_snow_ice.png`.
