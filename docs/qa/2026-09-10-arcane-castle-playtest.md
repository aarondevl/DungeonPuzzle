# Arcane Castle Vertical Slice — QA Record

**Status:** IN PROGRESS — everything automatable is done and passing; the human playtest is pending.

## Build under test

| Field | Value |
|---|---|
| Commit tested | `56dd543` (`test: cover the castle route and the Room_04 optional passage`) |
| Branch | `codex/arcane-castle-pending` |
| Unity version | 6000.5.0b10 (fc01675d12b6) |
| Target platform | StandaloneWindows64 (Player) |
| Date | 2026-09-11 / 2026-09-12 |

---

## Step 1 — Automated tests

| Suite | Total | Passed | Failed | Skipped | Duration |
|---|---|---|---|---|---|
| EditMode | 80 | 80 | 0 | 0 | 1.94 s |
| PlayMode | 6 | 6 | 0 | 0 | 10.82 s |

The PlayMode count grew as Task 10 coverage landed; a final re-run is owed once the manual
steps close (step 8).

Unity console after both runs: zero errors, zero compilation errors.

**Result: PASS.**

### Task 9 ambience evidence (automated)

Generated clips, read back from disk and from the import pipeline:

| Clip | Length | Channels | Rate | Force To Mono | Load Type | Format | Quality | Preload | Peak | Loop seam Δ |
|---|---|---|---|---|---|---|---|---|---|---|
| `Music/patio_hall_ambient` | 12.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | 0.4500 | 0.0232 |
| `Music/guard_wing_ambient` | 12.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | 0.4500 | 0.0494 |
| `Music/library_ambient` | 12.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | 0.4500 | 0.0846 |
| `Music/dungeons_ambient` | 12.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | 0.4500 | 0.0202 |
| `Music/tower_ambient` | 12.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | 0.4500 | 0.0374 |
| `Music/dungeon_ambient` (fallback) | 20.000 s | 1 | 44100 | true | Streaming | Vorbis | 0.55 | false | — | — |

Crossfade measured in Play Mode with `MasterVolume=1.0`, `MusicVolume=0.8`, `SfxVolume=1.0`
(expected steady-state music volume `0.6 × 0.8 = 0.48`):

| Transition target | Duration | Min combined volume | Max combined volume | Final volume |
|---|---|---|---|---|
| `Music/patio_hall_ambient` | 0.600 s | 0.4800 | 0.4800 | 0.4800 |
| `Music/guard_wing_ambient` | 0.601 s | 0.4800 | 0.4800 | 0.4800 |
| `Music/library_ambient` | 0.601 s | 0.4800 | 0.4800 | 0.4800 |
| `Music/dungeons_ambient` | 0.601 s | 0.4800 | 0.4800 | 0.4800 |
| `Music/tower_ambient` | 0.601 s | 0.4800 | 0.4800 | 0.4800 |

No silence gap and no volume spike: the combined volume of the two music sources stays flat at
`0.48` for the whole 0.6 s crossfade. Master/Music/SFX preferences were unchanged by the
transitions, and `AudioListener.volume` matched `MasterVolume` afterwards.

Alert loudness, verified in source:

- suspicion — `GuardBase.cs:48` and `GuardBase.cs:88` both call `SfxLibrary.Play("SFX/alert", 0.35f)`;
- detection — `GameManager.cs:144` calls `SfxLibrary.Play("SFX/detected", 0.45f)`;
- no continuous chase loop was added, and the `State == GuardState.Alerted` guards still prevent stacking.

---

## Bugs found and fixed during this QA pass

Automating steps 2-5 surfaced three defects. All three are fixed and covered by tests.

| Defect | Symptom | Fix |
|---|---|---|
| Guard sight exceeded its cone | `VisionCone` used `distance` as a world length for physics and a local length for mesh vertices. With the 0.7 root scale on the guard prefabs the visible cone reached 3.5 world units while the queries probed 5, so any wall in that band stretched detection out to it. Measured: 4.2 instead of 3.5. | `d02a3d0`, covered by `VisionConeRangeTests` |
| Console error on every death and win | `GameManager.OnSceneLoaded` resolved a `SpawnPoint` for every scene and logged an error when it found none; `MainMenu` and `GameOver` legitimately have none. | `db5035b` |
| Room_05 spawned the player inside a wall | The Torre spawn sat at (-6, -4); `Wall_Bottom` spans y -6.5 to -3.5 and the player has radius 0.4, so physics pushed the player out frame by frame (drift 0.087 rising to 0.674 and climbing). Other rooms drift at most 0.003. | `fd14785` |

---

## Step 2 — Fresh-save critical path

**Automated part: PASS. Timing: PENDING — needs a human session.**

`CastleRoutePlayModeTests.FreshSave_RunsPatioToTowerWithoutReturningToTheMenu` runs a fresh save
from Patio to Tower through all five rooms, checking each room is recorded as completed, that
travelling never costs a life, and that the tower's final exit produces a win.

**Scope limit:** the test places the player on each exit rather than playing the room, so it
verifies the route is *wired*, not that the puzzles are *solvable*, and it exercises no lever, key
or door. Its runtime says nothing about pacing.

The 20-30 minute acceptance criterion is about how long a human takes on a first run. That number
cannot be produced by automation and is still owed:

| Room | Time | Notes |
|---|---|---|
| Patio + Gran Salón (Room_01) | | |
| Ala de Guardias (Room_02, at least one thrown stone) | | |
| Biblioteca (Room_03, lever solution) | | |
| Calabozos (Room_04, no secret) | | |
| Torre (Room_05, win screen) | | |
| **Total** | | Accept when 20-30 min |

---

## Step 3 — Failure / recovery matrix

**Result: PASS (automated).**

`CastleFailureRecoveryPlayModeTests` covers all five rooms, 5/5 passing: the first two detections
reload that same room at its default spawn point, the third opens Game Over, and retrying restores
three lives while castle completion and secret state survive.

| Room | Detection 1 reloads at default spawn | Detection 2 reloads at default spawn | Detection 3 opens Game Over | Retry keeps progress |
|---|---|---|---|---|
| Room_01 | PASS | PASS | PASS | PASS |
| Room_02 | PASS | PASS | PASS | PASS |
| Room_03 | PASS | PASS | PASS | PASS |
| Room_04 | PASS | PASS | PASS | PASS |
| Room_05 | PASS | PASS | PASS | PASS |

**Scope limit:** guards and spike traps are disabled on load so only the test's own detections
count. What is under test is how `GameManager` reacts to a detection, not whether a guard can
produce one — that is covered by `VisionConeRangeTests` and the guard tests. This is not
equivalent to a human letting themselves be seen three times.

The test snapshots and restores the `PlayerPrefs` keys it touches, so a run does not disturb a real
save.

---

## Step 4 — Optional route and configuration failure checks

| Check | Result | Evidence |
|---|---|---|
| Room_04 completed with the passage | PASS | `Room04OptionalRoutePlayModeTests.Room04_CompletesThroughTheSecretPassage` — the secret is recorded and survives the room transition |
| Room_04 completed without the passage | PASS | `Room04OptionalRoutePlayModeTests.Room04_CompletesWithoutTheSecretPassage` — the room completes and the secret stays undiscovered |
| Exit set to `MissingScene`, no load and one clear error | PASS | `CastleTravelRules.CanStart` rejects an unloadable scene (`CastleTravelTests.CanStart_UsesSceneLoadabilityProbe`); `GameManager.TravelTo` logs exactly one `Castle travel rejected by door` error and returns `false` |
| Entry set to `MissingEntry`, default spawn used | PASS | `SpawnPoint.Resolve` falls back to the default point (`CastleTravelTests.Resolve_UsesDefaultWhenRequestedIdIsMissing`) |
| Both player colliders overlap an exit, one travel only | PASS (code review) | `ExitTrigger._used` latch set before dispatch and only cleared when `TravelTo` returns `false`; `GameManager.PlayerDetected` also early-returns while `_isTransitioning` |

The two in-editor serialized-value experiments named in the plan were replaced by the equivalent
automated checks so that no scene asset had to be temporarily edited and re-saved.

---

## Step 5 — Audiovisual and performance review

**Performance and memory: PASS. Aesthetic sign-off: PENDING.**

### Frame time

| Room | Average | Worst frame | Average FPS |
|---|---|---|---|
| Room_02 | 2.45 ms | 4.55 ms | 408 |
| Room_05 | 2.61 ms | 4.34 ms | 384 |

Even the worst frame lands near 220 FPS, comfortably past the 60 FPS bar. Measured in editor Play
Mode at 2560x1440 with guards live. Draw-call count could not be sampled: that counter is not
available in this configuration.

### Memory across room transitions

Two full passes through all five rooms:

| Pass | Room_01 | Room_02 | Room_03 | Room_04 | Room_05 |
|---|---|---|---|---|---|
| 0 (Unity total) | 895.6 MB | 903.9 MB | 896.4 MB | 905.6 MB | 896.9 MB |
| 1 (Unity total) | 896.5 MB | 904.2 MB | 896.7 MB | 904.4 MB | 896.5 MB |

Mono heap stayed flat at 803.5 MB throughout. Pass 1 reproduces pass 0 to within a megabyte, so
there is **no growth accumulating across transitions**. Absolute figures are editor-inflated and
are not build numbers.

### Visual review

Reads well in all five rooms: the green player silhouette separates clearly from every floor, the
puzzle objects (keys, chests, plates, levers) stand out, the HUD is legible, and the per-room
accent colours land as intended — cyan in Patio, red in Ala de Guardias, purple in Biblioteca,
green in Calabozos, orange in Torre.

Three findings, each filed as an issue rather than fixed inside this task:

| Finding | Issue |
|---|---|
| Untextured flat-grey wall blocks in Room_02, Room_03, Room_04 and especially Room_05 | aarondevl/DungeonPuzzle-Solo#1 |
| Normal-state vision cone has poor contrast over the light olive floor of Room_05 and Room_02 | aarondevl/DungeonPuzzle-Solo#2 |
| Room_04 spawns the player about one tile from a patrolling guard | aarondevl/DungeonPuzzle-Solo#3 |

A minor observation not worth an issue: roughly the top 15% and bottom 20% of the frame is black
outside the room in every scene. That may be deliberate with a fixed camera, but it is a lot of
unused screen.

**Scope limits on the captures:** they were taken at 2560x1440, not the 1920x1080 the plan
specifies — that is the Game view size. Guards were disabled and the player teleported, so guard
positions are spawn positions, not real patrol states. They support judging art and readability,
not gameplay situations.

### Screenshots

| Artifact | Path |
|---|---|
| Room_01 spawn / puzzle | `Builds/qa-screenshots/room01-spawn.png`, `room01-puzzle.png` |
| Room_02 spawn / puzzle | `Builds/qa-screenshots/room02-spawn.png`, `room02-puzzle.png` |
| Room_03 spawn / puzzle | `Builds/qa-screenshots/room03-spawn.png`, `room03-puzzle.png` |
| Room_04 spawn / puzzle | `Builds/qa-screenshots/room04-spawn.png`, `room04-puzzle.png` |
| Room_05 spawn / puzzle | `Builds/qa-screenshots/room05-spawn.png`, `room05-puzzle.png` |

(`Builds/` is git-ignored; these are delivery evidence, not source control content.)

Profiler snapshots were taken as live counter samples rather than `.snap` captures, since the
acceptance criteria are frame time and memory growth, both of which the counters answer directly.

---

## Step 6 — Delivery build

**Result: PARTIAL — build produced and verified; the launch/relaunch check is manual.**

| Field | Value |
|---|---|
| Output | `Builds/ArcaneCastleVerticalSlice/DungeonPuzzle.exe` |
| Platform | StandaloneWindows64, non-development |
| Duration | 112.93 s |
| Total size | 130.13 MB |
| Errors | 0 |
| Warnings | 15 |

All 15 warnings are pre-existing `CS0618` API deprecations plus two URP build-processor notices;
none originate from the vertical-slice work. The affected sites are `DemoOverlay.cs`,
`HUDManager.cs:22`, `GameManager.cs:242`, `VisionCone.cs:64`, `MainMenuUI.cs:203`, and the
untouched listener lookup at `AudioMaster.cs:67`.

The player data folder contains `level0`–`level7`, matching the eight scenes enabled in
`EditorBuildSettings` (MainMenu, Room_01–Room_05, GameOver, Room_Demo).

Still pending (manual): launch the executable, run MainMenu → Room_01 → Room_02, close, relaunch,
and verify settings and progress load correctly.

---

## Accepted limitations

None accepted yet. The three visual findings from step 5 are filed as open issues
(aarondevl/DungeonPuzzle-Solo#1, #2, #3) rather than accepted; whether any of them is a
non-blocking cosmetic issue is a call to make after the human playtest.

---

## What is still owed

| Item | Owner |
|---|---|
| Step 2 first-run timing, total and per room, without editor teleportation | human playtest |
| Step 5 final aesthetic sign-off, and a verdict on issues #1-#3 | human playtest |
| Step 6 launch check: run the exe, MainMenu to Room_01 to Room_02, close, relaunch, confirm settings and progress load | human, outside Unity |
| Step 8 final `git diff --check` plus a full EditMode and PlayMode re-run, then commit this record | after the above |

The build in step 6 predates the fixes listed above. It needs rebuilding from the final commit
before the launch check means anything.
