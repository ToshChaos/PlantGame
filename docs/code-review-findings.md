# Code review findings (2026-11-12)

A pass over all code for optimisations, unused code and unnecessary comments. This file lists what was changed and, mainly,
what was left for a later session. Delete entries as they are dealt with.

## How the code was checked

- **Roslyn analyzers over all four assemblies** (Runtime, Editor, Tests, Editor.TestMenu), built outside Unity with `dotnet build`
  on the generated `.csproj` files. Rules: unused private members, fields that are written but never read, dead assignments, unused
  parameters, unneeded `using`s, redundant casts, zero-length and constant array allocations. Recipe at the end of this file.
- **A reference count of every declared name** (to catch unused public members, which the analyzers do not report).
- **Every `Update` / `LateUpdate` / `FixedUpdate` body was read** (40 methods), plus these files in full: `PlayerController`,
  `PlayerInteraction`, `InteractionSensor`, `CameraOcclusionFade`, `CapHops`, `PlantInstance`, `AudioManager`, `LoopEmitter`,
  `SeedAmbience`, `RuinSounds`, `GameSettings`, `GoalFlag`, `GameBootstrap`, `LeaflessTree`, `GoldenButterflies`, `RunConfig`,
  `TouchInput`, `WeatherState`, `QualityTier`, `WateringCan`, `FloatBob`, and the per-frame half of `PlayerHud`.
- **All 2036 comments were listed** and searched for TODOs, commented-out code (none found), task numbers and history notes.
  The short ones were read; the long ones were only pattern-searched.

**Not read line by line** (analyzers and pattern searches only): the world-building code (`Scenery`, `WorldGenerator`, `Overgrowth`,
`GoalBuilder`, `OrganicProps`, `ShapeMeshes`, `SeedMeshes`, `DebrisMeshes`, `LevelLayout`, `LevelPlan`, `IslandTerrain`, `SurfaceFx`,
`WeatherController`, `WorldOptimizer`, `WaterPlants`, `PalmLook`, `Waterfall`), the UI build code (`TitleMenu`, `PlayerHud.Build`,
`UiSprites`, `UiKit`), `TouchControls` and `InteractionMarkers` outside their update methods, every editor builder, the five shaders
and the Blender Python scripts. That code runs once per level or in the editor, so it matters for load time, not frame time.

## Done in this pass

- Analyzer findings: 15 unused `using`s, 5 never-read fields, 9 dead assignments, 7 unused parameters (`heads`, `k`, `isle`, `theme`,
  `fruit`, `n`, `t`), 6 redundant casts, 3 zero-length arrays, the always-false floating-gem branch in `Scenery`, and the unused
  `PlayerOutfitApplier.SetOutfit`.
- Per-frame work: `GameSettings` keeps its values instead of asking PlayerPrefs on every read; `AudioManager.OnTitle` no longer
  allocates a scene-name string every frame; `GameBootstrap.Current` no longer builds a new default config per read; the butterflies
  share one ground raycast a frame (was one each, 16); `PlayerInteraction` looks for a thirsty sprout once a frame (was twice) and
  builds the "Plant X" prompt once per seed; the camera's scenery test and the island-riding lookup are remembered per collider; the
  HUD prompt is assigned once a frame (with Hints off it used to be set and then cleared, rebuilding the text every frame); faded
  scenery materials are only written when their alpha changes.
- Comments: task numbers of finished tasks removed (the task files and the old git history are gone, so they pointed at nothing),
  "stub / will extend / was X / user request" notes rewritten to say what the code does now, and four comments that were wrong
  (jump height 4.8 m, Hints "on by default", Auto water "off by default", a doubled `<summary>` in `AudioManager`).
- Checked: all four assemblies compile, 50 / 50 edit-mode tests pass (including the golden-seed worlds), and a play-mode run of
  levels 1 and 4 in the editor (auto refill, planting, auto water, growth, prompts with Hints off and on, Sound toggle on a pond
  emitter). **Not checked:** a rebuilt Mac or phone player, touch controls, and whether the frame time actually changed (no profiler run).

## Left for later: optimisations

None of these is known to cost much; profile on a phone before spending time on them.

1. `InteractionSensor.TryResolve` does three `GetComponentInParent` calls per collider, and the sensor, `InteractionMarkers` and
   `PlayerInteraction` each resolve the same colliders every frame. A per-collider cache would remove that, but it must be dropped
   when a plant is added under a soil patch (the answer changes), which is why it was not done blind.
2. `PlayerInteraction.NearbyWaterable` runs its own, wider `OverlapSphere` every frame next to `OverlapNear`. One wider overlap
   could feed both.
3. `InteractionMarkers.LateUpdate` (187 lines): `Kit.Item(soil.acceptedItemId)` is looked up twice per soil; every pooled marker
   gets `SetActive` every frame; the beanstalk, bridge and mushroom-cap blocks repeat the same placement code and could share a helper.
4. `GemSparkle` writes a property block to every renderer every frame, on screen or not. `FlagWave` already skips invisible parts.
5. `FlagWave` recalculates normals for every visible part every frame.
6. `WateringCanVisual.Update` calls `SetActive` on the fill object every frame without checking for a change.
7. Every `GameSettings` setter calls `PlayerPrefs.Save()`, so dragging a volume slider writes the prefs file on each step. Saving
   on slider release or when the menu closes would do (`MobileLifecycle` already saves on pause).
8. `CameraOcclusionFade.Refresh` walks every renderer in the scene and reads transform names; it runs once per world, so it is a
   load-time cost only.
9. The 29 "constant array argument" analyzer hits (CA1861) are all in mesh builders that run once and cache their result. No action.

## Left for later: unused or legacy code that needs a decision

1. `SeedTree.dropOnStart` is never set to false and `sandboxSeed` is the real per-source seed. Both names are leftovers; renaming
   needs `FormerlySerializedAs` because scenes and prefabs store them.
2. The `Scroll` action is still in `PlantGameInput.inputactions`; no code reads it any more.
3. `SandboxTarget` is the soil patch of every level, not a sandbox object. A rename touches the contract in AGENTS.md 5.5.
4. `RunSession.startOnAwake` and its "Sandbox defaults" only serve the Sandbox scene.
5. Many public fields are tunables read only inside their own file (about 100: `PlayerController`, `InteractionSensor`,
   `InteractionMarkers`, `AudioManager`, `LevelLayout` constants). They could be private with `[SerializeField]`; not worth the churn
   unless the public surface matters.
6. `Palette.Hex` and `Overgrowth.H` ignore the result of `ColorUtility.TryParseHtmlString` (a mistyped hex gives black, silently).
7. Deliberately kept: the legacy `LEAF-...` seed-code decoder, `SaveService.Entry.mode`, the `GameSettings.Bloom` name (the menus
   call it Postprocessing; the PlayerPrefs key keeps saved choices).
8. Analyzer false positives, no action: the "never instantiated" build callbacks (`DropUnusedAndroidLibraries`,
   `StripUnusedPackageShaders`), `[MenuItem]` methods and `RuntimeInitializeOnLoadMethod` `Boot` methods.

## Left for later: comments

1. Task tags of the **open** tasks were kept on purpose (T57, T59, T61 to T64, T68 to T75). Remove them when a task is closed.
2. The shaders and the Blender scripts still carry tags of finished tasks (T01, T02, T07, T16, T30, T31, T40, T49, T51, T67),
   including the material-inspector labels in `PlantGrow.shader` ("T67 bark / fibre texture"). Not touched: editing a shader
   reimports it and every material that uses it.
3. The long comments (over about 50 characters, roughly 1400 of them) were not read one by one.

## Seen on the way, not part of this pass

- Level 1 logs `[PlantGame] No valid layout for seed 1101, using the last attempt.` on every load (13 times in the previous editor
  log too, so it predates this pass). The level plays, but its layout fails the validator.
- The Burst compiler server logs four errors on each domain reload in the editor (`Deferred Burst compiler server command failed`).
  Unity-internal; the game uses no Burst code.

## Recipe: analyzer build outside Unity

Unity must have generated the `.csproj` files (open the project once). Then, from `Game/`, with a scratch folder `$S`:

1. `$S/analysis.globalconfig`: first line `is_global = true`, then one line `dotnet_diagnostic.<ID>.severity = warning` for each of
   IDE0051, IDE0052, IDE0059, IDE0060, IDE0005, IDE0044, IDE0004, CA1823, CA1806, CA1825, CA1861, CA1805, CA1812.
2. `$S/extra.targets`: `<Project><ItemGroup><EditorConfigFiles Include="$S/analysis.globalconfig" /></ItemGroup></Project>`
   (the generated projects ignore `GlobalAnalyzerConfigFiles`).
3. For each of `PlantGame.Runtime`, `PlantGame.Editor`, `PlantGame.Tests.EditMode`, `PlantGame.Editor.TestMenu`, in that order:

```bash
dotnet build $p.csproj -nologo -v q --no-dependencies --no-incremental -p:BaseIntermediateOutputPath=$S/obj/$p/ -p:IntermediateOutputPath=$S/obj/$p/ -p:OutputPath=$S/bin/ -p:EnforceCodeStyleInBuild=true -p:EnableNETAnalyzers=true -p:AnalysisLevel=latest -p:GenerateDocumentationFile=true -p:NoWarn="USG0001%3BCS1591%3BCS1573%3BCS1572%3BCS1587%3BCS1570%3BCS1574" -p:CustomAfterMicrosoftCommonTargets=$S/extra.targets
```

   The output paths keep everything out of `Game/Temp`, so the open editor is not disturbed. `GenerateDocumentationFile` is what
   makes IDE0005 (unneeded `using`) report. Ignore IDE0051 on Unity messages (`Awake`, `Update`, ...). The build takes a few seconds
   and also works as a compile check when the editor is busy.
