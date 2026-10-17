# Project guidance

This is a C# 12 / .NET 8 desktop checkpoint crowd simulator using Silk.NET, OpenGL and Dear ImGui.

For Unity migration work, first read `UnityProject/AGENTS.md`, `UnityProject/MIGRATION_STATUS.md` and `UnityProject/MIGRATION_PLAN.md`. Work only on `unity-migration`; preserve the original application and tests. Update the status and commit each verified stage locally, then stop for explicit approval before pushing that checkpoint. Never push or merge to `main` as part of migration work.

## Source map

- `Core/`: deterministic simulation, queues/gates, SFM, responders, geometry, settings and metrics; coordinated by `CrowdSimulation.cs`.
- `App/` and `Program.cs`: native window, input, OpenGL/ImGui lifecycle and entry point.
- `Rendering/` and `Visual/`: isometric camera, world/humanoid rendering, animation and palette.
- `UI/`: scenario setup, responder placement, runtime controls, scaling, dashboards and results.
- `Configuration/`: JSON scenario persistence and legacy responder/settings migration.
- `Recording/`: framebuffer recording, output paths and encoder lifecycle.
- `tests/CrowdCheckpointSim.Tests/`: xUnit regression tests for simulation, persistence and presentation helpers.

## Commands

Run from the repository root with the .NET 8 SDK. These restore/build/test commands have been executed successfully with SDK 8.0.426:

```sh
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore
dotnet run --project CrowdCheckpointSim.csproj --no-build --no-restore
```

Cloud shells: first `source /workspace/.onboarding/activate.sh`; desktop helper: `bash /workspace/.onboarding/launch.sh`. Desktop execution requires an OpenGL-capable display. Recording currently has a Windows-only OpenCvSharp native dependency and fails on Linux.

## Simulation rules to preserve

- Preserve deterministic 60 Hz stepping; 1x–4x advances all simulation timers. Funnel queues retain lanes, spacing, bounded acceleration and space-gated rear replenishment; never use SFM there. Ordinary passengers switch to urgency-influenced SFM after fully crossing. Preserve neighbour/wall forces, movement limits, geometry and overlap correction protecting queues/controlled formations.
- Non-idle responders outside their post radius have right-of-way, including robots. Ordinary passengers sidestep/brake around corridors and escort pairs. Normal pursuit targets do not evade their pursuer before cooperative stop. Preserve Dasher yielding and body separation.
- Normal target locks are exclusive across humans/robots, including visual confirmation; Purple reserves first. Exclude autonomous verification and existing reservations. Mode 1 acquires only hall Undetected passengers within radius; continued pursuit is not range-limited. Preserve its quota accounting: configurable 0–4% Undetected, fixed 1% Uncounted, remainder Cleared.
- Purple Mode 1 sequence: visual confirmation → pursuit → cooperative stop at 2 m → catch at 0.5 m → 1.2 s hold → escort to post → verification → idle. Verified passengers become Cleared and resume movement, not deletion. Preserve additive percentage timing fatigue, catch-driven pursuit loss with a 50% speed floor, and the capacity bar's 50% floor; Dasher speeds remain separate.
- Mode 2: 90% first-pass Green, configurable 0–10% second-pass Yellow, remaining Red. Each attempt takes the full gate timer. Yellow is not intercepted; Red waits for manual intervention. Initial Red dispatch ignores the acquisition radius; Purple catches at 0.6 m and escorts to its post. Mode 2 normal pursuit speed is fixed and fatigue is disabled.
- Robots skip confirmation, retain stop/catch/hold, and clear on site using a separate fixed 1–180 s timer. After clearance, check once for an unlocked target within range of the current position, after Purple reservations; otherwise return empty to the original dock. Mode 2 robots reopen the cleared Red's gate without changing its recorded outcome. Robots never respond to Dashers or count as catchers.
- One funnel passenger may be marked as Dasher; activation occurs at crossing, outside normal quotas. Multiple Purple responders within range may pursue. First catch stops the pair; exactly two physical catchers secure the team and release extras. Preserve the 1 s group stop, holding escort, configured 10–30 s clearance, removal and return to original posts. Escape occurs only while Dashing.
- Dasher response preempts normal Purple work within acquisition range. Started verification finishes its remaining time autonomously; earlier Mode 1 Yellow resumes Undetected movement, while interrupted Mode 2 Red remains stopped for reacquisition.

## Documentation conflicts

Resolve historical notes against executable code and regression tests. Flag uncertain intent; preserve current behaviour unless explicitly asked to change it.

- `KNOWN_DIFFERENCES.md` has stale ranges/fatigue: current hall length is 20–30 m, human initial clearance is 10–180 s in both modes, and Mode 1 timing fatigue is configurable percentage growth. Only legacy saves retain the fixed +2.5 s increment. See `SimulationConfig`, `OperatorTiming` and migration tests.
- Earlier Dasher notes allowing extra catchers or a fixed 10 s wait are superseded. **Uncertain holding location:** `DasherRules` uses preferred `(6.0, 0.65)` m, clamped for wall clearance; its comment and `DASHER_HOLDING_POINT_X_MARK_UPDATE.md` say x=4 m, and older notes say left-centre. The test follows the constants, so intent remains unresolved. Preserve the implemented point pending clarification.

## Change and validation discipline

- Preserve existing settings/defaults/ranges, public functions, controls, seeds, save/load compatibility, responder identities/posts, recording, run/restart behaviour and result accounting unless explicitly asked to change them.
- Add relevant regression tests for logic changes, covering the changed behaviour and affected invariants/modes, including locks, timers, reset and legacy persistence where applicable. Keep rendering changes independent of core dynamics.
- Report actual commands and pass/fail/skip counts, warnings, failures and limitations. Distinguish standard `dotnet test` from historical reflection harness results. Do not infer UI correctness from unit tests: state which desktop startup/rendering/input/recording checks were performed and explicitly identify those not performed.
- Do not commit `bin/`, `obj/`, `publish/`, recordings or generated local settings. Preserve unrelated user changes.
