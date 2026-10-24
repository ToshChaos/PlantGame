# Bug list

Severity: **blocker** (cannot finish or crashes), **major** (wrong behaviour, a workaround exists), **minor**, **polish**.
Retest column: date and build when a fix was checked.

| ID | Sev | Area | Description | Owner | Status | Retest |
|----|-----|------|-------------|-------|--------|--------|
| B1 | minor | Editor | `PlayFromTitle` resets the editor start scene after every domain reload, and Main bounces to Title unless `PlayerHud.SkipMenuOnce()` was called; tooling that plays Main must handle both (`LevelSmokeTest` does) | A0 | known, documented | n/a |

No blocker or major bugs found in the automated pass: 37 of 37 EditMode tests pass and all ten curated levels (plus level 1 in Challenge) load in play mode with a player, a run and no console errors. A full by-hand playthrough has not been done: the checklist is the way to find the rest.
