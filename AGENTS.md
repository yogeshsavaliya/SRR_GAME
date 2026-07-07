# AGENTS.md

## Cursor Cloud specific instructions

This repository is a **Unity 6 game project** (`SRR_GAME` / `ARROW`). It is a
standalone, client-side game — there is **no backend, database, or web service**.
The single "service" is the Unity Editor itself, which compiles the C# scripts,
restores packages, runs the game, and produces player builds. The required editor
version is pinned in `ProjectSettings/ProjectVersion.txt` (`6000.0.33f1`).

### Where things live
- Unity Editor (Linux, baked into the VM snapshot): `/opt/unity/6000.0.33f1/Editor/Unity`
- Project root (the `-projectPath`): `/workspace`
- Package manifest / lock: `Packages/manifest.json`, `Packages/packages-lock.json`
  (UPM packages are restored **automatically** the first time the editor opens the
  project — there is no separate install step).
- Only scene / runtime entry point: `Assets/Scenes/SampleScene.unity`

### Licensing (required before ANYTHING else works)
The editor will not compile, run, test, or build without an activated license.
Activation needs a Unity account and is **not** baked into the snapshot, so it must
be (re)applied per pod. Provide credentials via secrets, then activate in batchmode:

```bash
# Personal license: UNITY_SERIAL can be omitted. Pro/Plus: include UNITY_SERIAL.
/opt/unity/6000.0.33f1/Editor/Unity -batchmode -nographics -quit -logFile - \
  ${UNITY_SERIAL:+-serial "$UNITY_SERIAL"} \
  -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD"
```

Alternative (manual, no stored password): generate `Unity_v6000.0.33f1.alf` with
`-createManualActivationFile`, upload it at https://license.unity3d.com/manual to
obtain a `.ulf`, store the `.ulf` contents in a `UNITY_LICENSE` secret, then
`-manualLicenseFile <file>.ulf`. Note the `.ulf` is machine-bound and may need
re-generating if the pod's machine id changes; credential-based activation is more
robust across fresh pods.

Verify success: the batchmode log should end without `No ULF license found` and the
license store `~/.config/unity3d/Unity/licenses/` should contain a license file.

### Common commands (run after activation)
All editor invocations are headless (`-batchmode -nographics`). Use `-logFile -` to
stream the log to stdout; the editor's real exit code / errors are in that log, not
just the shell exit status.

- Compile + import the project (proves the C# compiles and packages resolve):
  ```bash
  /opt/unity/6000.0.33f1/Editor/Unity -batchmode -nographics -quit \
    -projectPath /workspace -logFile - -accept-apiupdate
  ```
- Run automated tests (none exist yet; command still works and reports 0 tests):
  ```bash
  /opt/unity/6000.0.33f1/Editor/Unity -batchmode -nographics -runTests \
    -projectPath /workspace -testPlatform EditMode \
    -testResults /tmp/results.xml -logFile -
  ```
- Building/running an actual player requires a C# build method invoked via
  `-executeMethod` (none exists in the repo yet). The Linux editor tarball includes
  Linux standalone build support. A built player is graphical, so launch it under a
  virtual display (`xvfb-run`) since Cloud pods are headless.

### The game (Arrows Puzzle Escape)
- Gameplay code lives in `Assets/Scripts/`. `Core/` is **pure C#** with no
  `UnityEngine` dependency (board model, move resolution, hearts, solver, level
  generator). `Game/` is the Unity view layer (procedural sprites, tap input,
  animations, UI, level flow).
- `SampleScene` contains an **"Arrows Game"** GameObject with the `GameController`
  component, so pressing **Play** runs the full game. The board/UI are built
  procedurally at runtime, so the Scene/Game view looks empty in edit mode until
  you press Play — this is expected. `GameBootstrap`
  (`[RuntimeInitializeOnLoadMethod]`) is a safety net that spawns a controller
  only if a scene has none. No prefabs or art assets are required.
- **Test the logic without a Unity license:** `bash tools/run-logic-tests.sh`
  compiles `Core/` + `tools/LogicTests` with the editor's bundled Mono compiler
  and runs mechanic/hearts/solver tests (all handcrafted + generated levels are
  asserted solvable). `... gen` regenerates level layouts.
- **Type-check the whole project without a license:** compile
  `Assets/Scripts/**/*.cs` with `mcs -noconfig -nostdlib` referencing
  `Data/NetStandard/ref/2.1.0/netstandard.dll` and the
  `Data/Managed/UnityEngine/UnityEngine.*Module.dll` set.

### Non-obvious caveats
- Only **one** Unity Editor may hold the project lock at a time. If a batchmode run
  is interrupted, remove a stale `Temp/UnityLockfile` under the project before retrying.
- `-nographics` is required (no GPU); graphics-dependent PlayMode tests may behave
  differently than on a workstation.
- The editor frequently returns shell exit code `0` even on errors — always grep the
  `-logFile` output for `error`/`Aborting` to detect failures.
