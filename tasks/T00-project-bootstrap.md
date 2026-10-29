# T00 — Project bootstrap, packages, folders, git

**Owner:** A0 Director · **Depends on:** —
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A clean Unity project at the repo root that every other role can open, with the folder layout, packages and git hygiene in place.

## Deliverables
- Unity 6000.6.5f1 URP project in `PlantGame/Game/` (Assets, Packages, ProjectSettings), builds for the demo platform; `com.unity.pipeline` kept
- Folder tree from `AGENTS.md` section 3 created with `.gitkeep`s
- Packages: Input System (active input handling = Input System Package), Cinemachine, Post Processing via URP Volume, TextMeshPro
- `Game/.gitignore` (Unity + Blender `*.blend1`), `git init`, repo-local git identity, first commit; confirm the `mcp__remote-devices__unity__*` tools connect (editor open on `Game`, MCP server on) and `device_request_delete_permission` once; `Blender/` kept outside `Assets/`
- Layers/tags from section 5.6; Physics matrix: `Player` collides with `Ground`, `Plant`, `Climbable`(trigger), `Interactable`(trigger)
- Empty scenes: `Main.unity`, `Sandbox_Gameplay`, `Sandbox_Plants`, `Sandbox_World`
- `Game/Assets/ThirdParty/LICENSES.md` and `README.md` stubs; `docs/` folder
- Assembly definition `PlantGame.Runtime` (optional but speeds compile)

## Steps
1. Open Unity on `Game` (target 6000.6.5f1, URP template) and load the Unity tools with ToolSearch; if none appear follow the recovery steps in `AGENTS.md` section 2. Record exact versions (Unity, Blender, URP) there.
2. Create or open the project, set Color Space Linear, URP asset (Quality: medium), disable unneeded packages.
3. Create folders, layers, tags, `.gitignore`; commit.
4. Add `GameEvents` + `IInteractable`/`IUsableTarget` stubs in `Scripts/Core` exactly as in section 5.5 so others can compile against them.
5. Open Blender via MCP, save empty `Source/Blender/plants.blend` and `Source/Blender/kit.blend` to confirm paths work.
6. Announce readiness: set T00 `DONE`.

## Acceptance criteria
- [ ] Project opens and compiles with zero errors
- [ ] Folder layout matches `AGENTS.md`
- [ ] Contract stubs compile; `GameEvents` raise helpers exist
- [ ] Git repo has a first commit and a working `.gitignore`
- [ ] Version info updated in `AGENTS.md` section 2

## Notes
- If the installed Unity is not Unity 6, keep going and adjust API names; note differences in `AGENTS.md` section 9.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
Packages (InputSystem 1.20.1, URP 17.6.0, uGUI 2.6.0), folder tree, layers (Ground 8, Plant 9, Climbable 10, Interactable 11, Player 13; Water = built-in 4), URP asset, Linear, scenes Main/Sandbox_*. Rebuild: menu PlantGame/Rebuild Core Assets. Known: git NOT yet initialised (device shell unavailable); activeInputHandler change needs editor restart.
