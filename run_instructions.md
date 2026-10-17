# Run Instructions

## Prerequisites

- Windows 10/11, Linux, or macOS with an OpenGL-capable graphics driver.
- .NET 8 SDK.
- VS Code with the C# Dev Kit or C# extension.

## Open in VS Code

1. Extract the project folder.
2. Open **CrowdCheckpointSimCsPRO** as the VS Code folder.
3. Open the integrated terminal in the project root.

## Restore, build, test, run

```powershell
dotnet restore
dotnet build
dotnet test .\tests\CrowdCheckpointSim.Tests\CrowdCheckpointSim.Tests.csproj
dotnet run
```

The main project is deliberately at the repository root, so `dotnet run` is sufficient.
The application opens maximized, synchronizes rendering to the actual framebuffer on its first frame, and applies proportional UI scaling automatically. Normal window resizing remains available through the operating-system window controls. The compact status strip stays at the top. **Start/Resume** is in the left sidebar below the simulation-speed controls and above Full UI Recording, while the remaining run controls stay in the bottom bar.

## New setup controls

1. Choose a checkpoint mode.
2. Use **Hall / crowd** for geometry, density, and passenger preferred-speed bounds. **Main hall length** now ranges from **20 to 30 m** (default **20 m**) in both modes. **Hall width** is **15–20 m**; **Open Funnel width** is **5–20 m**, capped by the hall width. In **Multi-Lane Barricade**, the width/lane-count slider selects **15–20 lanes**.
3. Use **Checkpoint** for outcomes and automated gate timing.
4. Use **Operators** for acquisition, shared normal interception speed, Purple manual clearance/fatigue, and the separate fixed **Robot manual clearance time** (1–180 seconds).
5. Use **Dasher** for the existing holding-point duration.
6. Use **Files** for the seed and configuration save/load.
7. Under **Responder Placement**, choose **Place Purple operator** or **Place wheeled robot**, then click valid hall positions. Place at least one responder, confirm, then use **Start**.

Robots skip visual confirmation, take only unlocked normal targets and ignore Dashers. They clear passengers on the spot using their fixed timer, then intercept another nearby unlocked passenger, or return to their original docks if none is available. See `WHEELED_ROBOT_UPDATE.md` for the robot checklist.

See `UI_SPEED_FATIGUE_UPDATE.md` for the verification checklist. This download contains updated source; rebuild or publish it to generate an executable containing these changes.

## VS Code tasks

Use **Terminal -> Run Task** and choose:

- `build`
- `test`
- `run`

You can also press **F5** after the first successful build using the supplied `.vscode/launch.json`.

## Release executable

Framework-dependent Windows build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish\win-x64
```

Self-contained Windows build (larger, no separately installed .NET runtime required on the target PC):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o publish\win-x64-self-contained
```

## Full UI recording

During runtime, use **Start Recording** and **Stop Recording** in the left sidebar. Videos are saved
under `recordings`.

For a self-contained Windows build:

```powershell
dotnet publish CrowdCheckpointSim.csproj -c Release -r win-x64 --self-contained true -o .\publish
```

Distribute the complete `publish` folder, not only the `.exe`, because OpenCvSharp uses native
Windows runtime files for video encoding. In a published build, the `recordings` folder is created
beside `CrowdCheckpointSim.exe`.

## Hall-length range update

See `HALL_LENGTH_CONTROL.md`. Existing saved JSON configurations with `HallLength` below 20 must be updated to a value between 20 and 30 before loading; the existing range-validation behaviour is retained. To reuse one, open its JSON in the `configs` folder, change only `HallLength` (for example to `20.0`), save and load it again. Files that omit hall length use the new 20 m default.

## Width and lane-count update

See `WIDTH_AND_LANES_UPDATE.md`. Defaults are 15 m hall width, 5 m Open Funnel width and 15 barricade lanes. To use a 20 m Open Funnel, first select a 20 m hall width. Existing saved scenarios must use `HallWidth` (or legacy `RoomHeight`) within 15–20 and `FunnelWidth` within 5–20 and no wider than the hall. In barricade mode those widths must match and be whole metres. The existing save/load validation remains strict.
