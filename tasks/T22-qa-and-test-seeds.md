# T22 — QA pass, regression seeds, bug list

**Owner:** A8 Tester/Release · **Depends on:** T15
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Break the game on purpose; keep a regression list so the seed-code promise holds.

## Deliverables
- `docs/test-seeds.md`: golden seed codes with expected island hash/layout summary
- EditMode/PlayMode tests in `Tests/`: inventory rules, SeedCode encode/decode round trip (including bad input and checksum), generator determinism, generator validator on 100 seeds, SaveService best-time logic
- Manual test script `docs/qa-checklist.md`: controls, edge cases (full inventory, plant twice, drop in void, restart spam, alt-tab; water: F with an empty can, on a grown plant, on a sprout twice, refill at full, collector overfill, collector while the player is away), both modes, menu flows
- Bug list `docs/bugs.md` with severity; file Requests to owners; retest fixes

## Steps
1. Run all tests; fix or report.
2. Playtest 10 random seeds start to finish; record anything unfun or unsolvable.
3. Verify golden codes still produce the same worlds after each change.

## Acceptance criteria
- [ ] All automated tests pass
- [ ] No blocker/major bugs open at feature freeze
- [ ] Golden seed codes reproduce identical worlds

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Added test cases (day 3)
- Collector-only level (`Sandbox_PlantTest`): no water anywhere, empty can at start, plant the Dew Seed, wait 20 s, E hands over exactly 1 charge; E again before the next charge does nothing; a full can leaves the charge in the plant; it keeps producing while the player is on another island.
- Water look: ponds and streams light / mid / dark, one interact marker per feature, drinking from the middle of a stream.
