# T26 — Title + level select, Random Level, seed codes (copy/enter)

**Owner:** A7 Sound & UI · **Depends on:** T14, T25
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The menu that sets up every run: choose a level and a mode, or generate and share random worlds.

## Deliverables
- `Title.unity`: logo, calm animated sky (reuse T16), music (T17), buttons Play / Settings / Quit
- Level select screen: **cards** for curated levels (title, description, islands, kinds, best time per mode), a **Random Level** card, and **Enter Seed Code** field (paste support, live validation with friendly errors: invalid, checksum, old generator version warning)
- Mode toggle: Exploration / Challenge (Challenge shows time limit)
- Random Level: generates a fresh seed via system time, shows its seed code before starting and on the win panel so it can be shared; 'Replay this seed' button
- Difficulty selector for random levels: islands 4–8 (Easy/Normal/Hard) mapping to `RunConfig`
- Implement `SeedCode.Encode/TryDecode` per contract (base32, checksum, groups of 4, case-insensitive, ignores dashes/spaces) — coordinate with A5 on `GeneratorVersion`
- Writes the `RunConfig` into `GameBootstrap.PendingRun` then loads `Main`

## Steps
1. Implement and unit-test SeedCode first (round-trip 10,000 random configs; fuzz bad input).
2. Build screens with the UI framework used by T19.
3. Test: generate random level → copy code → quit → enter code → identical world (compare the seed supply debug string and hash).

## Acceptance criteria
- [ ] Level 1 → start in both modes works from the title
- [ ] Random Level shows a seed code; entering the same code reproduces the exact world
- [ ] Invalid codes show helpful errors and never crash
- [ ] Best times appear on cards and update after a run

## Notes
- Minimum viable (if short on time): Title with Play Level 1 / Random / Enter Code, no cards or difficulty selector.

## Fallback / cut plan
Cut: curated levels 2–3 cards, difficulty selector, animated background.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Handoff (day 3)
- `Title.unity` (menu 16) + `UI/TitleMenu`: Home (mode, Play, Select level, Generate level, Input seed, Options, Quit); level cards with per-level best times and test-scene buttons; generate screen shows the seed (five dash-joined words) with copy; input screen normalises spaces to dashes and validates live. Behind it a generated showcase world whose plants grow.
- Known: Play on the home screen starts level 1.
