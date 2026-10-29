# AGENTS.md — "Overgrown" · 3-day hackathon MVP

Read this file fully before touching anything. Then read `TASKS.md` (`TASKS-MVP.md` for the MVP) and your task file in `tasks/`.

## 1. Game in one paragraph
Third-person 3D exploration of **floating islands** (one blocky white/black/gold player model, orbit camera). You cannot cross the gaps — you **grow plants** that become
**bridges, ladders and platforms**. Seeds are found on islands, carried in a **limited inventory**, and planted in soil patches. Seeds only sprout; a **watering can** (2 charges, refilled at waterfalls, lakes or Water Collector plants) makes them grow. Each run is built from a **seed number**: islands are assembled from modular kit pieces and plants are
picked from **procedural Blender (Geometry Nodes) variations**. Goal: reach the Heart Tree on the last island.
Style: fantasy, **white / green / blue**, soft, glowing, stylized.

**Run:** no lose condition and no timer pressure; falling just respawns you. The elapsed time is shown, and **best times** (time spent to finish) are saved per level/seed. There is no countdown or "challenge" mode (removed in v3.0).
**Levels:** MVP ships at least **1 curated level** (fixed seed) and a **level-select menu** with: curated levels
(1 required, 3 nice), **Random Level**, and **Enter Seed Code**. Any generated level can be re-created from its
**seed code** (copy/paste shareable).

**Controls:** WASD or the arrow keys move (**W / S and Up / Down also climb up / down** on ladders) · Mouse look · Space jump (while climbing it jumps off the vine) · **Q** pick up the item you look at, or drop the selected item · **Tab** cycle inventory slot · **E** use (plant the selected seed on a soil patch) · **F** water a sprout, or refill the can at a lake, waterfall or collector · Esc pause · R restart run (hold). Optional alias: 1–4 select a slot (no mouse-wheel selection). Key hints (Q / E / F / Tab) are shown on screen by default; the pause menu toggles them (`GameSettings.Hints`). Inventory: **4 slots**, no stacking. The **watering can** is separate from the 4 slots: **2 charges**, never dropped, refilled with F at a lake, waterfall or Water Collector.

## 2. Tech stack and environment (modelled on the sibling project *ClockWork*; T00 confirms versions)
- **Unity 6000.6.5f1**, Universal 3D / URP, new Input System, Cinemachine (built-in in 6.6, pin is redundant), TextMeshPro. C#.
  The Unity project is **`Game/`**. Repo root holds docs, tasks and Blender sources.
- **Blender 5.2.2 LTS** (confirmed via MCP, Python 3.13) on the user's Mac (metric, scale 1, FBX + glTF exporters on), Geometry Nodes. Sources live
  **outside Unity** in `Source/Blender/`. Never put `.blend` into `Game/Assets/`.
- **Two session setups — check which tools you have (ToolSearch) before starting:**
  - **Desktop app on the Mac (Code tab):** Bash, Read/Write/Edit and git run directly on the Mac in the repo. Blender:
    `mcp__Blender__*` (`execute_blender_code`, `render_viewport_to_path`, ...). Unity: `mcp__unity-mcp__Unity_RunCommand`
    (compile + run a C# `CommandScript` in the editor: menus/builders, play mode, input, captures), `Unity_GetConsoleLogs`,
    `Unity_Camera_Capture`. No `editor_focus`/`wait_for`; see the play-mode recipe below.
  - **Cloud session with remote devices:** Blender via `mcp__remote-devices__Blender__*`, Unity via
    `mcp__remote-devices__unity__*` (scene, GameObject, script, asset, package, build, test, console, screenshot, `eval`,
    `editor_focus`, `wait_for`), shell via `device_bash` (see the next bullets).
  All of these are deferred tools: load them with ToolSearch. They need the Unity Editor **open on `Game`** and the Unity MCP server enabled
  (Claude desktop app: Settings, Developer, "unity" on) plus the `com.unity.pipeline` package (keep it).
  **If no unity tools appear:** check the editor is open, run `RefreshMcpTools`; compile errors put the editor in Safe
  Mode and block the connection — fix the C# and restart Unity. Prefer the tools over hand-editing `.unity`/`.prefab`/`.asset`.
- (Cloud setup only) The sandbox shell (`device_bash`) runs in a Linux VM: it **cannot run Unity, Blender or other Mac programs** and cannot
  reach the Mac's `localhost`. The repo is mounted at `$HOME/mnt/PlantGame`; edit files there with `device_bash`
  (Read/Write/Edit act on the cloud, not the Mac). Plain python3 there is fine for helper scripts (e.g. SFX synthesis).
- Git works from the sandbox (cloud) or directly (desktop). Cloud only: deleting files (git lock files) needs `device_request_delete_permission` once per session.
  Set a repo-local git identity in T00. Never commit `Library/ Temp/ Logs/ UserSettings/ *.csproj *.slnx`; commit `.meta`.
- Blender -> Unity: export to `Source/Blender/exports/` with `export_fbx.py`, copy with `sync_to_unity.py` into
  `Game/Assets/Art/Models/<Category>/`. Pinned settings (T02, `docs/pipeline.md`): `axis_forward='Z'`, `axis_up='Y'`,
  `FBX_SCALE_UNITS`, `bake_space_transform=True` — verified 1:1, no X mirror, for single objects. **Parented hierarchies**
  (empties with child meshes) break with `bake_space_transform`: export them with `overrides=CHAR_FBX_OVERRIDES` and let
  Unity bake the axis conversion (T31). Naming `Type_Name_Variant`.
- Generate scenes/prefabs with **editor builder scripts** (menu `PlantGame > Build ...`, in `Game/Assets/Editor/`) so
  they can be rebuilt. Same for Blender: every asset has a reproducible script in `Source/Blender/scripts/`
  (`exec(open(path).read()); build_all()`).

### Play-mode testing recipe (learned the hard way in ClockWork)
- The editor only ticks frames while focused. Remote-devices tools: call `editor_focus` right before every `wait_for` /
  capture, then `wait_for` a higher `UnityEngine.Time.frameCount` (the focus click counts as a left click and can fire Use).
  Desktop `Unity_RunCommand`: after entering play mode set `Application.runInBackground = true` — frames then tick while
  unfocused (each RunCommand call is roughly one or more frames apart; check `Time.frameCount`).
- Fake input with `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W, Key.LeftShift))`; queue an empty
  `KeyboardState()` to release. Capture the game camera by rendering it into a RenderTexture and writing a PNG to `docs/shots/`.
- Drive state with `eval` (teleport the player, call `Interact`/`UseItem`/`Plant` directly), then `wait_for` a state
  (`PlantInstance.IsGrown`, `RunSession` state). Use `Time.timeScale` 3–12 to skip waits.
- Do **not** edit scripts in play mode (domain reload wipes non-serialized state). After script or package changes the
  domain reloads: calls fail with "Network error" for a few seconds — wait ~10 s and retry. `Game/Logs/Editor.log` is the
  fallback when the console tool lags. The third-person camera is a child of the player, so teleports move it too.
- Docs habit: long per-task notes go in `docs/task-log.md`; keep `TASKS.md` and the task files short. `docs/shots/` is gitignored (local screenshots only; describe what you saw in the task log).
- Audio options (Music / Sound toggles and sliders) must work **while the game is paused** (`timeScale` 0), in a **standalone build**, not only in the editor. Rules: every audio source follows `GameSettings.Sound` / `Music` through `AudioSource.mute` as well as `volume` (a NaN or infinite volume from boost / weather maths is silently ignored by Unity and leaves the old volume playing); apply the setting in the `GameSettings.Changed` handler as well as in `Update` (never rely on a frame tick or on game time); use `SoundGain` / `MusicGain`, not the raw slider values; keep audio logic free of `Time.time` / `timeScale`. A new audio source or emitter is not done until you have toggled Sound and Music off and on in the pause menu of a rebuilt Mac player and heard silence at once (the editor is not enough: a stale build or a hot-reloaded script gives false results, so rebuild first).
- Runtime materials: create them through `PlantGame.Core.RuntimeMats` (never a bare `Shader.Find`). A player build only carries shaders and keyword combos used by a material asset; the `M_Keep_*` materials (`MaterialBuilder`, listed in `Resources/WorldKit`) cover transparent Lit and Particles/Unlit. Add a keeper when code turns on a new keyword combo.

## 3. Repository layout (contract — do not invent new top-level folders)
```
PlantGame/                         (git root)
  AGENTS.md  CLAUDE.md  README.md
  TASKS-MVP.md                     MVP board (complete)
  TASKS.md                         board after the MVP: later tasks + open work
  tasks/                           one file per task (T00..T67)
  docs/                            art-bible, pipeline, controls, mobile-controls, test-seeds, qa-checklist, bugs, task-log, shots/ (local, git-ignored)
  Builds/                          standalone builds (git-ignored)
  Source/Blender/
    plants.blend                   Geometry Nodes live here
    characters.blend               the player model (T31, script char_player.py)
    scripts/                       python run through the Blender tools — commit these
    exports/                       raw exports before they are copied into the Unity project (git-ignored)
  Source/ThirdParty-raw/           downloaded third-party packs (git-ignored; only used models are copied into Game/)
  Game/                            Unity project root
    Assets/
      Art/{Models,Textures,Materials,Shaders,VFX}
      Audio/{Music,SFX,Mixers}
      Prefabs/{Plants,Island,Items,UI,Player,Environment}
      Scenes/                      Title, Main, Sandbox
      ScriptableObjects/{Items,World}
      Scripts/{Core,Player,Inventory,Plants,World,Audio,UI}    namespace PlantGame.<Folder>
      Editor/                      builder menus, importers
      Tests/
      ThirdParty/LICENSES.md       log every sourced asset: source, author, licence
```

## 4. Agent roster
Roles, not necessarily separate sessions — one session may wear several hats, but **respect the ownership column**.

| ID | Role | Owns (may edit freely) | Tools | Tasks |
|----|------|------------------------|-------|-------|
| A0 | **Director** (integrator/producer) | `Main.unity`, `TASKS.md`, `AGENTS.md`, project settings, git merges | Unity, git | T00 T15 T21 T24 |
| A1 | **Gameplay** | `Scripts/Player`, `Scripts/Inventory`, `Scripts/Core` (interaction), Player/Items prefabs, `Scripts/Core/Modes`, `Scripts/Core/Save` | Unity | T03 T04 T05 T12 T25 T31 |
| A2 | **Botanist** | `Scripts/Plants`, plant ScriptableObjects, Plants prefabs, grow shader hookup, watering can | Unity | T11 T27 |
| A3 | **Plant Smith** | `Source/Blender/plants.blend`, `characters.blend`, `Source/Blender/scripts/plants_*`, `char_*`, `Art/Models/Plants`, `Art/Models/Characters` | Blender | T06 T07 T08 T28 T09 T31 |
| A4 | **Stonemason** | imported props `Art/Models/{Kenney,Quaternius,Ruins}` (the Blender island kit, T10, was CUT) | Unity | T10 T29 |
| A5 | **Cartographer** | `Scripts/World`, `ScriptableObjects/World`, Island prefabs | Unity | T13 T14 |
| A6 | **Tech Artist** | `Art/Shaders`, `Art/Materials`, `Art/VFX`, skybox, post-processing, water shaders | Unity (+Blender for cloud cards) | T01 T16 T20 T30 |
| A7 | **Sound & UI** | `Scripts/Audio`, `Scripts/UI`, `Audio/*`, `Prefabs/UI` | Unity (+ python for SFX) | T17 T18 T19 T26 |
| A8 | **Tester/Release** | `Tests/`, `docs/test-seeds.md`, builds | Unity | T22 T23 |
| A0+A3 | Pipeline (shared) | `Source/Blender/scripts/export_*`, import presets | Blender+Unity | T02 |

### Working rules
1. **One owner per file.** Need a change in someone else's area? Add a short request under "Requests" in *your* task
   file and tell A0, or make a tiny, clearly-commented edit and note it in the task's Handoff section.
2. **Scenes are conflict magnets.** Only A0 edits `Main.unity`. Everyone else builds **prefabs** and tests in their
   own `Sandbox_<role>.unity`. A0 drops finished prefabs into Main.
3. **Contracts first** (section 5). If a contract must change, edit this file, bump the note in section 9 and tell A0.
4. **Vertical slice over breadth.** Ugly-but-playable beats pretty-but-broken. Hold the cut line (section 8).
5. **Verify with your eyes.** Blender agents: screenshot/render the result. Unity agents: enter Play mode, check
   Console is clean, test the acceptance criteria. Don't tick a box you didn't observe.
6. **Keep tasks small.** If a task drags, stop, post status in the task file, and ask A0 whether to cut.
7. **Small commits**, message `T<nn>: what changed`. **Always commit when a task (or a self-contained chunk of it) is done** — don't wait to be asked. While one person works on the project, commit **everything** Unity changed too (ProjectSettings, scenes, regenerated materials/prefabs); only the ignored `Library/ Temp/ Logs/ UserSettings/` stay out. Branch `task/T<nn>-slug` if using branches; merge by A0.
8. **No unlicensed assets.** Every third-party file gets a line in `Game/Assets/ThirdParty/LICENSES.md` (name, author, license, URL).

## 5. Contracts

### 5.1 Scale, axes, units
1 Unity unit = 1 m. Blender: Z-up, apply scale; FBX settings are pinned in T02 (`docs/pipeline.md`).
Kit grid = **4 m**. Player: height 1.8 m, radius 0.35 m, walk 4 m/s, run 6 m/s, jump height **3.6 m**
(v1.6, was 4.8 in v1.5), max horizontal jump about **7.2 m** (run). **Open:** the 7 m gap rule below predates the higher jump;
T14 must keep plant-required gaps above the jump reach (or the jump is retuned). So any gap >= **7 m** needs a plant (world gen uses 7–14 m).

### 5.2 Plant kinds and reach envelopes (A3, A2, A5 all rely on this)
| Kind | Seed item | Behaviour | Reach (from anchor) | Grow time |
|------|-----------|-----------|---------------------|-----------|
| Bridge | Vine Seed | arcs outward, walkable top, width >= 1.2 m; tilts up or down to meet the next island | 8–14 m horizontal, up to +/-3.4 m vertical (v2.7; was +/-1.5) | 6 s |
| Ladder | Beanstalk Seed | climbs up, climbable (trigger volume), **no top platform**: the climber steps off at `Land` straight onto the target island, whose rim lies about 1.2 m in front of the stalk top | 0–2 m horizontal, 8.5–12.3 m vertical (generator uses variant 3: 11 m) | 5 s |
| Platform | Shelf Fungus / Leaf-pad Seed | spiral of pads, each >= 1.5 m wide, step rise <= 1.2 m | 3–6 m horizontal, 2–4 m vertical | 4 s |
| Collector | Dew Seed | **Water Collector** (pitcher bloom, 1.8-2.7 m): needs **no water to grow**, then collects dew and makes **1 charge every 20 s** | none (stays on its soil patch) | 4 s |

### 5.2b Water rules (all numbers live in `ScriptableObjects/World/WaterSettings`)
- **Watering can:** separate from the 4 slots, never dropped. `MaxCharges = 2`, **starts empty** (v1.7: fill it at the start island's water), charges persist through respawn. A can with water is shown in the player's hand.
- **Sprout -> grow:** planting a Bridge / Ladder / Platform seed (E) only creates a **sprout** (`_Growth` ~0.08, no colliders).
  **F** on a sprout within 3 m uses **1 charge** and starts growth. F on anything else costs nothing; F with 0 charges
  gives a "dry" feedback event. Collectors skip the sprout state (`needsWater = false`).
- **Refill (F):** `WaterSource` (Lake, Waterfall) is unlimited and fills the can instantly; `WaterCollector` hands over its stored
  charges, up to what the can can hold.
- **Water Collector:** once grown, +1 stored charge every **20 s**, capacity **1** (`WaterCollector.secondsPerCharge` / `capacity`, v1.8; was 15 s / 2), fill level visible in its bowl (`Reservoir`).
  It occupies a soil patch and a Dew Seed (inventory slot while carried).
- **Collector-only levels (v1.8):** `WaterMode.CollectorOnly` levels have **no WaterSource at all**; the first island has a grown single-use Water Collector (`WaterCollector.oneShot`: one charge, never refills, enough for the first plant) and the second island has the soil patch plus a Dew Seed; the Water Collector grown there is the only refilling water. Random / code levels in the rain use this mode for about half of their seeds (`WorldTheme.ResolveWater`, no seed-code change). Plan for waiting 20 s per charge (and the walk back to the first island). `Sandbox_PlantTest` is built this way; Level 1 uses `WaterMode.Sources`.
- **World gen water rule (T14):** the Start island always has a water source (the can starts empty); other islands may have none (Level 1: water on 4 of 7 islands); along the main path there are never more than
  2 water-needing links between two water access points (source, or a ready collector); islands without a source may
  carry a Dew Seed supply. The validator simulates charges, so every level is beatable with at most one backtrack.

### 5.3 Plant prefab contract (Blender export -> Unity)
Each exported mesh variant `Plant_<Kind>_<NN>` (NN = 01..06, = Blender seed) must contain:
- Vertex colour attribute **`growth_t`** (stored in vertex colour R, 0 at the base -> 1 at the tip). The shared grow
  shader (`M_PlantGrow`, T01/T11) reveals/scales geometry where `growth_t <= _Growth`.
- Empties exported alongside: `Anchor` (base, at soil), `Tip` (end at full growth), optional `Land` (where the
  player steps off).
- Collision proxies: child objects `COL_<NN>_<idx>` (simple boxes/capsules, ordered base->tip). Unity converts these
  to colliders enabled progressively by growth. Ladders also export `CLIMB_<idx>` trigger boxes.
- Pivot at `Anchor`; plant grows along its local +Z (bridge), +Y (ladder) or spiral (platform).
- **Collector variants** (`Plant_Collector_NN`) additionally export: child mesh `Reservoir` (the water volume; Unity scales/clips it
  0..1 for the fill level), empties `WaterMin` / `WaterMax`, and one interaction trigger `INTERACT_NN` (layer `Interactable`). No
  `COL_`/`CLIMB_` needed beyond a small body collider. `growth_t` still runs 0->1 base to rim.
- Tri budget: <= 10k tris per variant (v2.8; was 4k). One material slot (`M_PlantGrow` instance via vertex colours for look).

### 5.4 Island kit contract (A4 -> A5)
- Pieces snap to a 4 m grid, pivot at bottom centre. Names `Kit_<Category>_<Name>_<NN>`.
- Categories: `Water` (`Waterfall` edge piece that spills off the island, `Lake` recessed pond tile with shore, both walkable shallows), `Terrain` (flat, edge, corner, inner-corner, slope, underside rock), `Ruin` (arch, pillar, wall,
  broken wall, stairs, altar), `Veg` (tree, bush, flowers, grass tuft, mushrooms), `Prop` (rock, crystal, soil patch,
  seed pedestal, Heart Tree).
- Every island prefab exposes empties: `Socket_Water` (lake / waterfall position, optional per island), `Socket_Soil` (planting spots, with facing = growth direction),
  `Socket_Land` (where a plant from another island must land), `Socket_Item` (item spawn spots), `Spawn`.
- Terrain pieces include a simple convex collider; props have none unless named `COL_*`.

### 5.5 C# contracts (A1/A2/A5/A7 code against these exact names)
```csharp
namespace PlantGame.Core {
  public enum InteractAction { Pickup, Use }   // Pickup = Q, Use = E
  public interface IInteractable {            // anything Q or E can act on
    InteractAction Action { get; }            // WorldItem = Pickup; WaterSource, WaterCollector = Use
    string Prompt { get; }                    // "Pick up Vine Seed"
    bool CanInteract(GameObject actor);
    void Interact(GameObject actor);
  }
  public interface IUsableTarget {            // anything E-with-selected-item can act on (soil patch, ...)
    bool CanUseItem(ItemDefinition item, GameObject actor);
    void UseItem(ItemDefinition item, GameObject actor);
  }
  public static class GameEvents {            // static C# events, no hard references between systems
    public static event System.Action<ItemDefinition> ItemPickedUp, ItemDropped, ItemUsed;
    public static event System.Action<int> WorldGenerated;         // seed
    public static event System.Action<ItemDefinition> InventoryFull; public static event System.Action<string> HintRequested; public static event System.Action UseRejected;
    public static event System.Action GameWon, PlayerRespawned, PlayerJumped, PlayerLanded;
    public static event System.Action RunStarted; public static event System.Action<float, bool> RunFinished; // seconds, newBest
    public static event System.Action Watered, CanRefilled, CanEmptyTried, CollectorReady;
    // Raise<...> helpers live in GameEvents too.
  }
}
namespace PlantGame.Inventory {
  [CreateAssetMenu] public class ItemDefinition : ScriptableObject {
    public string id; public string displayName; public Sprite icon;
    public GameObject worldPrefab; public ItemKind kind;     // Seed, Key, Misc
  }
  public class Inventory : MonoBehaviour {                   // 4 slots, no stacking
    public const int Slots = 4;
    public bool TryAdd(ItemDefinition i); public ItemDefinition Remove(int slot);
    public int Selected { get; } public ItemDefinition Get(int slot);
    public event System.Action Changed;
  }
}
namespace PlantGame.Plants {
  public enum PlantKind { Bridge, Ladder, Platform, Collector, Glider }   // seeds map to a kind by item id (World.SeedItems)
  public class PlantInstance : MonoBehaviour { public PlantKind kind; public float growSeconds; }   // one grown / growing plant
  // Soil patches are PlantGame.Core.SandboxTarget (IUsableTarget + IWaterable); baked variants come from Resources/WorldKit.
}
namespace PlantGame.Plants {
  public interface IWaterable { bool NeedsWater { get; } void Water(GameObject actor); }   // sprouts
  public class WateringCan : MonoBehaviour {               // on the Player, not an inventory item
    public const int MaxCharges = 2; public int Charges { get; }
    public bool TryConsume(); public int Refill(int amount);   // returns charges actually added
  }
  public class WaterSource : MonoBehaviour, IInteractable { public WaterKind kind; }  // Lake, Waterfall: unlimited
  public class WaterCollector : MonoBehaviour, IInteractable { public int Stored { get; } } // +1/15 s, cap 2
}
namespace PlantGame.Core {
  [CreateAssetMenu] public class LevelDefinition : ScriptableObject {   // curated levels, in ScriptableObjects/World
    public string id; public string title; public string description;
    public int seed; public int islandCount;               // 4–8
    public PlantKind[] allowedKinds;                       // tutorial = Bridge only, later levels add Ladder/Platform
  }
  public struct RunConfig {                                // what the menu hands to the game scene
    public int seed; public int islandCount;
    public PlantKind[] allowedKinds; public string levelId; // levelId null for random/code levels
  }
  public static class SeedCode {                           // shareable, human-typable, versioned
    // "LEAF-7K3Q-2M" = base32(seed:32b | islandCount:4b | kindsMask:4b | genVersion:4b) + checksum, groups of 4
    public static string Encode(RunConfig c);  public static bool TryDecode(string code, out RunConfig c);
    public const int GeneratorVersion = 1;                 // bump when world-gen output changes; old codes then warn
  }
  public class RunSession : MonoBehaviour {                // owns timer + state; events via GameEvents
    public RunConfig Config; public float Elapsed;
    public void StartRun(RunConfig c); public void Win(); public void Restart();
  }
  public static class SaveService {                        // JSON in Application.persistentDataPath/save.json
    public static bool TrySubmitTime(string key, float seconds); // returns true if new best
    public static float? BestTime(string key); public static void SetLastSeedCode(string c);
    // key = levelId for curated levels, or the SeedCode string for random/code levels
  }
}
namespace PlantGame.World {
  public class WorldGenerator : MonoBehaviour {
    public int Seed; public void Generate(int seed);         // deterministic: same seed -> same world
  }
}
```
Determinism rule: **all** randomness in generation/variant choice uses `System.Random(seed ^ salt)` or a seeded
`Unity.Mathematics.Random`. Never `UnityEngine.Random` in world gen.

### 5.6 Layers & tags
Layers: `Ground`, `Plant`, `Climbable`, `Interactable`, `Water`, `Player`. Tags: `Player`, `Respawn`.
Kill plane: y < island-bottom - 30 m -> respawn at the last visited island `Spawn`.

### 5.7 Art direction (details in `docs/art-bible.md`, T01)
Palette: cloud white `#F4F8F6`, mist blue `#BFD9EE`, sky blue `#6FA8DC`, deep blue `#2C5E8F`, leaf green `#5DB46B`,
moss `#2F7D4F`, glow cyan `#7CF2E6` (magic/accent only). Water: translucent sky blue with white foam and a glow-cyan edge. Soft rim light, gentle bloom, low-poly stylized shading,
no textures required — vertex colours + gradients. Music: calm, airy, harp/pad. SFX: soft, chimey, organic.

### 5.8 Input resolution (T03 / T04 implement exactly this; `docs/controls.md` mirrors it)
| Key | Resolution |
|-----|-----------|
| W A S D / arrow keys | Move relative to the camera. In the Climbing state **W** goes up and **S** goes down; A / D are ignored. |
| Left Shift | Hold to run (6 m/s; walk 4 m/s). |
| Space | Jump. While Climbing it **jumps off the vine**: normal jump velocity plus a small push (about 2 m/s) away from the climb surface, air control kept, 0.3 s re-grab cooldown, `PlayerJumped` raised. |
| Q | Focused `IInteractable` with `Action == Pickup` and a free slot -> pick up. Otherwise drop the selected item (if any). |
| Tab / Shift+Tab | Cycle the selected slot forward / backward with wraparound (empty slots can be selected). |
| E | With a selected item and a focused `IUsableTarget` -> `UseItem` (plant a seed). Otherwise a focused `IInteractable` with `Action == Use` (the goal flag; water sources are F only). A selected Heat Seed next to frozen water melts it (v2.5; was F). |
| F | Water: focused `IWaterable` within 3 m costs one charge (5.2b). At a focused `WaterSource` / `WaterCollector` it refills the can instead (v1.9; E no longer refills). |
| Esc / hold R | Pause / restart the run. |
Climbing starts when the player is inside a `Climbable` volume and presses **W** (or **S** at the top edge to go down); reaching
the top steps the player onto the ladder's `Land`. Prompts print the key from the bound action, never a hard-coded letter.

## 6. Definition of done (every task)
- [ ] Acceptance criteria in the task file are met and observed.
- [ ] Unity: compiles, no red Console errors/warnings you introduced, Play-mode smoke test passed.
- [ ] Blender: file saved, scripts committed to `Source/Blender/scripts`, exports re-importable, screenshot in `docs/shots/`.
- [ ] Contract names used exactly as in section 5.
- [ ] Task file updated: status line, Handoff notes (what exists, how to use it, known issues).
- [ ] Row in `TASKS.md` updated by the owner.

## 7. Handoff protocol
End each task by filling the **Handoff** section of its task file: files created, prefab/scene names, public API,
how to test in 30 seconds, known issues. Then set status to `REVIEW`; A0 sets `DONE` after checking in Main.
Statuses: `TODO` -> `DOING` -> `REVIEW` -> `DONE` (or `CUT`, `BLOCKED(reason)`).

## 8. Scope & cut line
_Final (2026-10-25): the MVP below shipped complete (`TASKS-MVP.md`); later work is in `TASKS.md`. The cut order is kept for reference._

**MVP (must ship):** T27 (watering can), T29 (lake + waterfall), T30 (water look), T31 (player model + third-person camera), T00–T05, T06–T09, T10, T11–T15, T16–T19, T21–T26 (T26 minimum: title -> level 1 / random / seed code).
**Cut order if behind (cut from the top first):** T20 growth VFX polish -> second music track / ambience layers -> Water Collector plant (T28; generator then puts a water source on every island) ->
Platform plant kind (keep Bridge + Ladder) -> ruin variety -> PlantGallery showcase scene (replace with a video).
**Never cut:** watering can with 2 charges + refill at a lake or waterfall, seeded generation with copyable seed codes, best-time saving, at least 2 plant kinds with real Blender variants, pickup/drop/use/limited
inventory, music + SFX + sky, a win condition, a working build.
**Stretch:** parachute leaf (T32, user request: plant leaf, one-time glide), retrieve plant (Use on grown plant -> seed back), glow-shard collectibles, day/night, gamepad, water mechanic.

## 9. Change log of contracts
- v3.3 (2026-10-28): `GameSettings.AutoWater` (off by default, title Options left column after Music / Sound, FPS counter moved to the right column; also in the pause menu): `PlayerInteraction` waters a thirsty sprout within reach, else refills at water within reach, as if F was pressed (0.6 s between automatic actions).
- v3.2 (2026-10-28, cleanup): `GameEvents.FocusChanged` and `CanChanged` removed (raised, never subscribed: the HUD reads `PlayerInteraction.CurrentPrompt` and `WateringCan.Charges`); `PlayerInteraction.waterRange` removed (watering uses `InteractionSensor.range`); `RunConfig` / `WorldTheme` / `LevelTheme` / `GoalKind` / `WaterMode` live in `Core/RunConfig.cs` (was `GameMode.cs`); cached `Layers.InteractMask` / `SolidMask`.
- v3.1 (2026-10-27): **Player model reworked**: rounded explorer-mage of the ruined magic civilisation (`char_player.py`: hat with leaf sprig, glowing-glyph cloak, satchel, vine on the left arm, boots). Joint names unchanged. Mesh suffix is now a material ROLE (`_Skin _Hair _Cloth _Cloak _Gear _Trim _Glow _Leaf _Dark`), mapped by a `PlayerOutfit` asset (`ScriptableObjects/Player/Outfit_Explorer`, `PlayerOutfitApplier` on `Model`) so reskins are a new outfit asset; materials `M_Player<Role>` replace `M_PlayerWhite/Black/Gold`. The watering can is part of the model (`Hold_Can` / `Can_Tilt` / `CanWater_Glow` under `Joint_ArmR`); `WateringCanVisual` only shows, tips and fills it.
- v3.0 (2026-10-27): **Challenge mode removed** (no value): `GameMode`, `RunConfig.mode` / `timeLimit`, `LevelDefinition.challengeTimeLimitSeconds` / `parTimeSeconds`, `RunSession.Fail` / `Remaining` / `AddTime`, `RunState.Failed`, `GameEvents.GameFailed` / `TimeAdded`, `TimeFruit` and the mode toggle on the title screen are gone; `RunStarted` has no argument; `SaveService` best times are per key only (old Challenge records are ignored).
- v2.9 (2026-10-26, cleanup): `PlantDefinition` (never created) removed, `PlantKind` lives in `Plants/PlantKind.cs`, `ItemDefinition.plant` removed; `GameEvents.PlantStarted` / `PlantGrown` (never raised) and `GlideStarted` / `GlideEnded` (no listeners) removed; `kit.blend` deleted (T10 cut); raw Blender exports and built `.app` folders are git-ignored; post-MVP tasks moved to `TODO.md`.
- v2.8 (day 3, T67): plant variant tri budget 10k (was 4k); platform plant is a mushroom patch (per-cap stalks from the ground, no central stem; decorative baby mushroom has `padi = -1`); bridge sprout = two small vines (`spr` flag, bridge growth_t starts at 0.085); parachute is a big leaf (`LeafMesh.CreateHeld`, `Scenery.LeafPlant`).
- v2.7 (day 3): vine bridges tilt up or down by the height difference to the next island (`LevelLayout.MaxBridgeRise` 3.4 m, was 1.5); the plant is stretched along its slope so the horizontal reach stays the same.
- v2.6 (day 3): beanstalk climbing: Up / Down arrows climb like W / S; the player climbs on the start-island side of the stalk (`ClimbableVolume.playerBehind`, volume spans both sides, grab only from the player's side), so the camera has open air behind the player; frozen water shows a cold glow and the prompt "Frozen water: it needs a Heat Seed" (F says so too); after a use (plant, thaw, glide) the selection moves to the next filled slot (`Inventory.SelectNextFilled`).
- v2.5 (day 3): game renamed **Overgrown** ("Grow plants to cross the floating islands."); `SeedCode.GeneratorVersion` 3: two vine bridges from one island leave at least 50 degrees apart and bridges elsewhere keep 7 m between them, one or two decoy soil patches (`Soil_*_decoy`, no route needs them) on levels with more than 3 islands; swamp ponds (`Isle.murky`, not a `WaterSource`); the Heat Seed melts ice with E; scenery between the camera and the player fades (`CameraOcclusionFade`); shrubs have no colliders; Sandbox_PlantTest, Sandbox_Plants and Sandbox_World are gone, Sandbox holds one of every item.
- v2.4 (day 3): `RunConfig.spiral` (islands curl round, +50 degrees per link; Windy Summit), snow-capped goal island and altitude-scaled wind ambience on windy spirals; Challenge-only time fruits (`RunSession.AddTime`, `GameEvents.TimeAdded`): ceil(islands / 3) per level, 30 s per island in total; best times are kept per level and per mode.
- v2.3 (day 3): arrow keys move like WASD; the player turns slower (300 deg/s); turning the player more than 15 degrees away from the camera while moving forward swings the camera round to follow the facing (the mouse still looks around freely and overrides it). Themes `Snow` and `Fog` plus a per-level `wind` flag (`RunConfig.wind`, `WorldTheme.ResolveWind`); ten curated levels (T50).
- v2.2 (day 3): the beanstalk ladder has no platform at the top and is taller (8.5-12.3 m, generator 11 m); `LevelLayout.LadderPadReach` 1.2 m, `LadderEdgeInset` 1.6 m; `Land` sits 1.9 m in front of the stalk top (T51).
- v2.1 (day 3): islands keep 8.5 m from every island but their parent; bridge gaps 8.6-11.6 m (plant stretched to fit, `SandboxTarget.bridgeReach`), platform gaps 4.5 m; up to 15 degrees of island tilt; same `GeneratorVersion` 2 (not yet released).
- v2.0 (day 3): `SeedCode.GeneratorVersion` 2: optional side islands (`LevelPlan.mainLinks`, `goalIsland`, `RunConfig.sideIslands`), alternative soil patches per gap, overlap-free island placement, per-island ground (`GroundKind`). The game scene spawns more than one `Soil_*` per gap now.
- v1.9 (day 3): Refilling the watering can at a lake, waterfall or Water Collector moved from E to F (E no longer refills); the HUD advertises F. Seeds spin in a random direction at a random speed (0.2 to 60 deg/s). Parachute leaf: pointed yellow blade (`LeafMesh`), open leaf is a long sideways yellow blade.
- v1 (day 0): initial contracts.
- v1.1 (day 0): added game modes, LevelDefinition, RunConfig, SeedCode, RunSession, SaveService; tasks T25, T26.
- v1.2 (day 1): water — `WateringCan`, `IWaterable`, `WaterSource`, `WaterCollector`, `PlantKind.Collector`, `needsWater`, sprout state, `Socket_Water`, layer `Water`, seed code `kindsMask` 4 bits; tasks T27–T30.
- v1.3 (day 1): final controls — Q pickup/drop, Tab cycle, E use, F water, W/S climb; `InteractAction` added to `IInteractable`; section 5.8.
- v1.5 (day 1): jump height 1.6 -> 4.8 m, mouse look 2x (user request); `GameEvents.InventoryFull`, `FocusChanged`, `UseRejected` (T04/T05).
- v1.8 (day 3): Water Collector delivered (T28): 1 charge per 20 s, capacity 1; `WaterMode { Sources, CollectorOnly }` per level (builder flag now, `LevelDefinition.waterMode` with T14); water materials/shader `PlantGame/OvergrowthWater` (ponds/streams, light/mid/dark).
- v1.7 (day 3): the watering can starts empty; water only on some islands (start island always); water comes as ponds or streams; HUD charges are droplets; the can is visible in hand while it holds water.
- v1.6 (day 3): jump height 4.8 -> 3.6 m (user request); running reach ~7.2 m, so 7 m gaps still need a plant (level 1 gaps are ~8+ m).
- v1.4 (day 1): third-person orbit camera + one player model `Char_Player_01` (T31); `PlayerController.EyeTransform` is now the camera.
  Parented hierarchies export with `CHAR_FBX_OVERRIDES` (no `bake_space_transform`), Unity bakes the axis conversion for `Art/Models/Characters`.
