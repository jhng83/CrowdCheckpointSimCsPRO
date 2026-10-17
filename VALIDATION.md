# Validation: Hall Width, Funnel Width and Lane Ranges (PRO14)

Baseline: `CrowdCheckpointSimCsPRO13_HallLength_20_30m.zip`.

## Executed

- All application and test source files compiled with the .NET 8 Roslyn compiler, .NET 8 reference assemblies and baseline dependency DLLs. No compilation errors; direct assembly references produced the same framework-identity unification warnings as before.
- All **270 regression cases passed**, zero failures/skips. Actual xUnit Fact/Theory/InlineData methods and assertions were executed through a reflection harness on the .NET 8 runtime. This was not an invocation of standard `dotnet test`.
- Twenty-four new width cases cover Open Funnel area/population and placement, invalid widths, every lane count from 15 to 20, one-metre centres and full single-file occupancy, automated release at both lane-count bounds, maximum camera bounds/unprojection and save/load/clone.
- Existing fixtures were moved into the new supported ranges; resize tests still remove only out-of-bound posts. The Dasher exclusion fixture is centred on the wider hall's exit approach, preserving the same relative setup. The global 12 m exit-width limit is now tested as fitting within the minimum 15 m hall width.
- Source preservation checks confirm the entire crowd engine, SFM, queues, geometry, collision/yield rules, operator/robot allocation, Dasher rules, speeds, fatigue, timing and outcome accounting are unchanged from PRO13. Production changes are range constants/validation, UI range wiring and explanatory text, with camera/renderer comments updated. Camera calculations already derive their bounds from shared limits.
- Project files parse and pinned NuGet dependencies are unchanged. All baseline source and test files are retained.

## Still to verify on the target PC

Standard restore / MSBuild / `dotnet test`, native window appearance, live input and recording were not run here. Follow `run_instructions.md` and the checklist in `WIDTH_AND_LANES_UPDATE.md`. This is updated source; rebuild/publish to obtain an executable containing the changes.

---

# Earlier validation reports

The reports below describe previous versions. The PRO14 report above is current.

# Validation: Hall Length 20–30 Metres

## Executed checks

- All application and test source files compiled with the .NET 8 Roslyn compiler, .NET 8 reference assemblies and baseline dependency DLLs. There were no compilation errors; direct dependency references produced the same framework-identity unification warnings as before.
- All **246 regression cases passed**, with zero failures/skips. Actual xUnit Fact/Theory/InlineData methods and xUnit assertions were executed through a reflection harness on the .NET 8 runtime. This was not a standard `dotnet test` invocation.
- Twelve additional cases cover geometry, queue population, far-end post placement and checkpoint crossing in both modes at 20/25/30 m; camera framing and floor unprojection; and save/load/clone across the range.
- Existing hall-length assertions/fixtures were updated from the old range to valid 20–30 m values. All other existing test coverage is retained, including the 44 robot cases.
- A comparison against the attached baseline confirms that executable production-code changes consist only of `MinHallLength = 20.0f` and `MaxHallLength = 30.0f`. The camera comment was updated. The entire crowd simulation engine, geometry, UI controls, persistence implementation, robot allocation, Dasher logic, SFM, orderly queues, collision and yielding code are otherwise identical.
- Both project XML files parse and all package versions are unchanged.

## Remaining verification

Standard NuGet restore / MSBuild / `dotnet test`, native window rendering, live input and recording were not run for this update. Use the commands in `run_instructions.md` and the short visual checklist in `HALL_LENGTH_CONTROL.md` on your PC. The archive contains updated source rather than a rebuilt native executable.

---

# Earlier validation reports

The reports below describe earlier versions. The hall-range validation above is the current result.

# Validation: Consecutive Robot Interceptions (PRO12)

## Executed

- All application and test source files compiled with the .NET 8 Roslyn compiler against .NET 8 reference assemblies and the dependency DLLs supplied with the baseline.
- All **234 regression cases passed**, including **44 robot cases**, with zero failures/skips. The actual xUnit test methods, InlineData cases and xUnit assertions were executed through a small reflection harness on the .NET 8 runtime. This was not an invocation of `dotnet test`.
- All 97 PRO11 archive entries are retained. Existing nonrobot tests and package pins are unchanged; both project XML files parse.
- 24 protected core methods match PRO11 exactly, including fixed stepping, SFM, orderly queues, gate processing, crossing/exit accounting, yield/overlap correction, normal movement, autonomous human clearance and Dasher incident progression.
- Changes are limited to robot availability after clearance, a one-time acquisition check before returning, a current-position radius guard for that check, reset of its transient flag, and UI wording. Humans reserve first; shared target ownership and scoring are reused.
- New regression cases verify consecutive clearances without a dock trip, Purple locks across all intervention states, idle Purple priority, range measured from the robot, autonomous-clearance exclusion, unique next targets across robots, Dasher exclusion, Mode 2 nearby Red alarms and restart.
- Fixed robot clearance, original docking coordinates, human response, Dasher rules and shared movement guardrails remain intact.
- Normal humanoid rendering was restored exactly from PRO9 and is separate from the new wheeled robot renderer.

The SDK command-line frontend could not start in this environment. Direct compiler/runtime execution provided compilation and functional regression coverage. Existing dependency DLL references produced CS1701 framework-identity unification warnings; there were no compilation errors.

## Still to verify on the target PC

A standard NuGet restore / MSBuild / `dotnet test` run, native window startup, live input, recording and the rendered wheeled model were not exercised here. Run the commands in `run_instructions.md` and the visual checklist in `WHEELED_ROBOT_UPDATE.md`.

The archive contains updated source, not a rebuilt native executable.

---

# Historical validation reports

The following sections describe earlier PRO10/PRO9 checks and their limitations at the time. The executed PRO12 checks above supersede those limitations for source compilation and functional test coverage.

# Validation

## Humanoid robot update

Static comparison against PRO9 confirmed that 23 protected core method bodies are unchanged, including the entire operator update state machine, SFM, queue movement, right-of-way, overlap correction, passenger sampling and Dasher handling. All prior source files, tests and package pins are retained. The new responder type changes setup identity/persistence and normal-target assignment, then uses that shared state machine.

RobotResponderTests adds functional cases for immediate pursuit, Purple-first allocation, locked targets across intervention stages, unique robot reservations, radius/autonomous-clearance exclusions, full clearance, matching pedestrian-yield dynamics, mixed Dasher teams, Mode 2 Red handling, typed placement/reset and old/new saved configurations.

Compilation, xUnit execution and native UI rendering remain unverified here because the .NET SDK is unavailable. The previous PRO9 static report follows.

## Executed checks

- All 47 existing C# source/project/solution entries were retained.
- All 45 current C# files passed the structural delimiter check. Both project XML files parse.
- 21 core method bodies match the uploaded baseline exactly: fixed stepping, room SFM, Dasher forces, speed limits, queue movement, spawning, automated gate processing, overlap correction, right-of-way, pre-emption, autonomous clearance and Dasher states.
- The operator state-machine source differs only where normal pursuit now receives the operator's configured current interception speed.
- DasherRules, geometry, quota tracking, enums, pedestrian state, spatial hash, acquisition guards, recording and application window lifecycle are unchanged.
- NuGet package names and pinned versions are unchanged.
- New fields are wired through config defaults/validation/clone, setup editing and persistence.

These are static source checks. They do not prove runtime behaviour or replace compilation.

## Added runtime tests

MovementAndFatigueSettingsTests covers configurable percentage timing, pursuit loss and its half-speed floor, seeded passenger sampling, reset, physical catch counting, legacy files, invalid input, Mode 2 fatigue isolation and Dasher trajectory isolation. Existing save/load and manual-time range tests were extended.

## Not executed

No .NET SDK is installed in this environment. Fetching it was unsuccessful. Compilation, xUnit execution and a native OpenGL UI run were therefore not possible.

On the target PC, run:

```powershell
dotnet restore
dotnet build
dotnet test .\tests\CrowdCheckpointSim.Tests\CrowdCheckpointSim.Tests.csproj
dotnet run
```

Then use the checklist in UI_SPEED_FATIGUE_UPDATE.md. The archive contains source, not an updated precompiled executable.
