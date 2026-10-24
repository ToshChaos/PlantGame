# Seeds: curated levels and showcase picks (T21)

Frozen list. Generator changes must not alter these worlds (re-run **PlantGame > 15 Validate Seeds** and the level audit after any change to `World/`).
Estimated minimum = 30 s walking per gap + grow time + 20 s per Dew wait + 40 s setup (a lower bound, not a measured time).

| Level | Title | Seed | Islands | Gaps (B bridge, L ladder, P platform, G glide) | Challenge limit | Par | Est. min |
|---|---|---|---|---|---|---|---|
| 1 | First Steps | 1101 | 3 | BB | 240 s (was 150) | 120 s | 112 s |
| 2 | Beanstalk Hill | 2207 | 4 (+1 side) | BLB | 240 s | 150 s | 147 s |
| 3 | Water from the Sky | 3357 | 4 | BPL, collector only | 600 s | 420 s | 205 s |
| 4 | Skyfall | 4413 | 5 (+1 side) | BLGB | 420 s | 300 s | 177 s |
| 5 | Stepping Stones | 20261 | 7 | hand plan | 300 s | 240 s | 252 s |
| 6 | There and Back | 6619 | 3 | BL, round trip | 360 s | 240 s | 111 s |
| 7 | Snowed In | 7719 | 5 | BLPB, snow | 420 s | 300 s | 181 s |
| 8 | Windy Summit | 8843 | 6 | BLLBL, spiral + wind | 480 s | 340 s | 217 s |
| 9 | Into the Mist | 9967 | 5 | BPBL, fog | 480 s | 340 s | 181 s |
| 10 | Storm Season | 10303 | 8 | BLBPLBP | 720 s | 520 s | 286 s |

Audit (2026-10-24, headless planner + validator): levels 1-4 and 6-10 validate with 0 issues and no re-roll. Level 5 reports 4 issues because the
validator checks the generated-chain rules and level 5 is a hand-built puzzle (seeds deliberately sit far from their gaps); it is intended.
Limits are 1.4-2x the lower bound, except level 5 (300 s limit against a 252 s bound: tight on purpose, par 240 s). None of this was played through by a person in this pass; confirm par times when playing.

## Showcase seeds (for the manual demo)
Chosen by scan of 3000 candidates: 8 islands, valid with no re-roll, three bridges, three ladders and three platforms each, all kinds allowed, `WaterMode.Sources`.
Enter the code under **Enter Seed Code** (island count and kinds are inside the code).

| Pick | Seed | Seed code | Water islands |
|---|---|---|---|
| A | 14032468 | `zephyr-lazy-earthy-badger-quick` | 6 |
| B | 15386617 | `lynx-spring-vine-badger-sparky` | 7 |
| C | 4299 | `amber-gentle-swallow-badger-snowy` | 7 |

Not yet looked at in play: pick the best two on the day of the demo.
