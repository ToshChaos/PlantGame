# T03 — Player controller: WASD, look, jump, climb, camera

**Owner:** A1 Gameplay · **Depends on:** T00
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A responsive, forgiving character you can run, jump and climb with — the base for everything else.

## Deliverables
- `Scripts/Player/PlayerController.cs` (CharacterController or kinematic rigidbody — pick one and stay with it)
- `PlayerInput` via the Input System: Move, Look, Jump, PickupDrop(Q), CycleSlot(Tab / Shift+Tab), Use(E), Water(F), Pause, RestartHold(R); optional Slot1–4 and Scroll aliases (bindings per `AGENTS.md` 5.8)
- Third-person (Cinemachine FreeLook/orbit) or first-person camera — pick third-person if a stylized capsule/character exists, else first-person
- Movement numbers from section 5.1 (walk 4, run 6, jump 1.6 m, coyote time 0.12 s, jump buffer 0.12 s, air control)
- **Climb state**: inside a `Climbable` volume, **W** starts climbing (**S** starts a descent from the top edge); W / S move up / down, A / D are ignored, Space **jumps off the vine** (full jump velocity + about 2 m/s push away from the climb surface, air control kept, 0.3 s re-grab cooldown, raises `PlayerJumped`), reaching the top steps onto `Land`
- Fall detection: below kill plane → raise `PlayerRespawned` (respawn point provided by T15; use a placeholder `Respawn` tag)
- `docs/controls.md` (key table), `Prefabs/Player/Player.prefab` and `Sandbox_Gameplay.unity` with ground, steps, a ladder trigger

## Steps
1. Implement input actions asset with the bindings in `AGENTS.md` section 1.
2. Implement ground movement, slopes (<= 45 degrees), step offset 0.35 m, jump with coyote/buffer.
3. Implement climb state; emit `PlayerJumped` / `PlayerLanded` events (for SFX).
4. Hook camera, cursor lock, Esc to release.
5. Tune in sandbox with gap tests at 5 m (jumpable), 7 m (not jumpable), 1.6 m ledge (jumpable).

## Acceptance criteria
- [ ] Can move, look, jump; gap of 5 m is clearable and 7 m is not
- [ ] Can climb a test ladder and step off at the top without jitter
- [ ] Space while climbing launches the player off the ladder like a normal jump, and the player cannot re-attach for 0.3 s (no instant re-grab or stutter)
- [ ] No sliding off flat ground, no sticking on walls
- [ ] Events `PlayerJumped`/`PlayerLanded` fire once per jump
- [ ] Prefab works when dropped into any scene (self-contained, no scene refs)

## Notes
- Keep it a tight state machine: Grounded, Airborne, Climbing.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
PlayerController/PlayerInputReader/ClimbableVolume, Player prefab, Sandbox_Gameplay (menu PlantGame/5). Verified numerically via Tick(dt): walk 4, run 6, jump, coyote/buffer, ladder up/down. Known: 5 m gap lands at far edge (marginal); jump apex 1.67 m.
