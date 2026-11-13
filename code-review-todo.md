# Code review findings (2026-11-12)

Done on 2026-11-13: optimisations 1 to 6, legacy items 1 to 4 and 6, and the Level 1 layout warning (see `docs/task-log.md`,
"Code review follow-up"). What is left:

## Unused or legacy code that needs a decision

1. Many public fields are tunables read only inside their own file (about 100: `PlayerController`, `InteractionSensor`,
   `InteractionMarkers`, `AudioManager`, `LevelLayout` constants). They could be private with `[SerializeField]`. Left as they are:
   the play-mode tests and tuning sessions drive the game through `Unity_RunCommand` scripts, which can only reach public members
   (reflection is blocked there), so making them private would cost more than the tidier surface gives.
2. Deliberately kept: the legacy `LEAF-...` seed-code decoder, `SaveService.Entry.mode`, the `GameSettings.Bloom` name (the menus
   call it Postprocessing; the PlayerPrefs key keeps saved choices).
3. Analyzer false positives, no action: the "never instantiated" build callbacks (`DropUnusedAndroidLibraries`,
   `StripUnusedPackageShaders`), `[MenuItem]` methods and `RuntimeInitializeOnLoadMethod` `Boot` methods.

## To check:

- The Burst compiler server logs four errors on each domain reload in the editor (`Deferred Burst compiler server command failed`,
  thrown by `Process.Kill` in `RemoteCompilerServer.KillLingeringProcess`). Looked at on 2026-11-13: `Game/Library/burst.pid` was
  left over from 2026-11-04 and held pid 701, which now belongs to a system daemon (`/usr/libexec/tzd`), so the editor was trying to
  kill a process it does not own. Removing the file did **not** stop the errors (four more on each of the next two reloads, and the
  file was not written again), so the pid file is not the whole cause. Still Unity-internal; the game uses no Burst code. Not tried:
  restarting the editor without the file, deleting `Library/BurstCache`.
