# Unity migration foundation (S1, not validated in Unity)

This is an **incomplete foundation**, not the ported simulator. The tracked scene has an orthographic camera and a wired diagnostic component. Its temporary OnGUI message is a **placeholder**, not the planned uGUI interface. No crowd simulation, setup controls, metrics, persistence or recording has been implemented.

Start with [MIGRATION_STATUS.md](MIGRATION_STATUS.md) and [MIGRATION_PLAN.md](MIGRATION_PLAN.md). Use branch `unity-migration`; preserve the root .NET reference app/tests. The foundation is authored but **Unity import/compilation, tests, visuals and builds have not run**.

Cloud-only CI setup, runner/licence requirements and dated cost research are in [Documentation/CLOUD_CI.md](Documentation/CLOUD_CI.md). The prepared workflow runs reference checks on migration-branch pushes and **skips Unity by default** until a licensed cloud Windows runner is explicitly enabled. No access to the user's computer is required. This is prepared automation, not evidence of any completed Unity run.

## Toolchain requests and prerequisites

- Exact requested Editor: **6000.3.26f1**. The version identity was verified in Unity's official reference-source tag, not by installing/running the Editor. This does not establish current download availability or compatibility.
- Requested package versions and metadata provenance: [Documentation/TOOLCHAIN.md](Documentation/TOOLCHAIN.md).
- A licensed matching Editor, package-registry access, and Windows build support are required for their respective commands. Do not save activation files, credentials or licence values in the repository.
- `Packages/packages-lock.json` is deliberately absent: UPM has not resolved dependencies. Do not invent a lockfile. Generate, inspect and commit it with the real Editor when available.

## First licensed Editor run

1. Install the pinned Editor through Unity Hub with suitable Windows build support and normal licence activation. Open **this folder**, not the repository root. Do not upgrade it silently.
2. Let UPM resolve the requested packages and wait for compilation. Record all failures and any necessary version changes; do not disable verification.
3. Run **Tools > Crowd Checkpoint > Migration > Configure S1 Foundation**. This creates actual URP renderer/pipeline assets under `Assets/Settings/`, assigns graphics/quality settings, selects Windows Mono/.NET Standard and configures the scene. It refuses to overwrite an unrelated asset. This command is required because no Editor has generated those assets/settings yet.
4. Open `Assets/Scenes/MigrationFoundation.unity` and enter Play Mode. Expect only an orthographic background and the explicit S1 diagnostic message. Do not expect a simulator or functional application controls.
5. Run EditMode and PlayMode tests in Test Runner. There are **two authored EditMode tests and one PlayMode test**, all currently unrun.
6. Review/commit Editor-generated source settings, `.meta` files and the real package lockfile in the next checkpoint; never commit `Library`, `Temp`, `UserSettings`, logs or licence files. Update the migration status with actual results. S1 stays incomplete until its gates pass.

## Batch entry points (not executed yet)

From the repository root in PowerShell, set `$UnityEditor` to the installed Editor executable and create the ignored output folder:

```powershell
$UnityEditor = "C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe"
$Project = Join-Path $PWD "UnityProject"
New-Item -ItemType Directory -Force "$Project/Artifacts" | Out-Null
& $UnityEditor -batchmode -quit -projectPath $Project -executeMethod CrowdCheckpoint.Unity.Editor.FoundationProjectSetup.Configure -logFile "$Project/Artifacts/configure.log"
& $UnityEditor -batchmode -projectPath $Project -runTests -testPlatform EditMode -testResults "$Project/Artifacts/editmode.xml" -logFile "$Project/Artifacts/editmode.log"
& $UnityEditor -batchmode -projectPath $Project -runTests -testPlatform PlayMode -testResults "$Project/Artifacts/playmode.xml" -logFile "$Project/Artifacts/playmode.log"
& $UnityEditor -batchmode -quit -projectPath $Project -executeMethod CrowdCheckpoint.Unity.Editor.FoundationProjectSetup.BuildWindows -logFile "$Project/Artifacts/build-windows.log"
```

Run commands individually and check `$LASTEXITCODE`, logs and current result XML before continuing. A zero-test run is not success. Do not run multiple Editor processes on the same project. The build entry point checks the build result and exits unsuccessfully on failure. Its output is `UnityProject/Artifacts/WindowsFoundation/`; distribute the **whole folder**, not only the exe. This is a development foundation player, not the completed release distribution.

## Checks available without Unity

```sh
python3 UnityProject/Tools/validate_foundation.py
python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v
# With .NET 8 activated:
python3 UnityProject/Tools/validate_foundation.py --check-dotnet
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore
```

The Python checker verifies static JSON/GUID/scene references and actual MSBuild compile isolation when requested. It does **not** validate Unity YAML deserialization, package resolution, C# compilation, rendering or runtime behaviour. See the status for measured results and exact next steps.
