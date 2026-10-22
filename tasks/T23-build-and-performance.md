# T23 — Performance pass + standalone build

**Owner:** A8 Tester/Release · **Depends on:** T22 · **Estimate:** 2h · **Day:** 3
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A shippable build that runs smoothly.

## Deliverables
- Profile typical and worst-case (8 islands) scenes; target 60 fps on the dev machine; fix top offenders (batching, shadow distance, particle counts, collider counts)
- Standalone build (macOS/Windows per demo machine) in `Builds/` (git-ignored) with a one-line run note in README
- Build smoke test: title → level 1 → win → back; random level; paste seed code; save file persists best time

## Steps
1. Use Unity Profiler/Frame Debugger via editor tools where possible.
2. Strip unused assets; check texture/audio import settings.

## Acceptance criteria
- [ ] Build launches and is playable start to finish
- [ ] Frame time stable; no console errors in the built player log
- [ ] Best times persist between launches

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
