# Unity migration status

Last updated: 2026-10-17 (Asia/Singapore).

## Read this first

- Repository: https://github.com/jhng83/CrowdCheckpointSimCsPRO
- Work branch: `unity-migration`.
- Reference/base: `8786c52f5be7aea683a9d14b69682bf07ed2f6e8` (`docs: expand agent guidance for simulation development`). GitHub `main` matched this SHA at checkpoint preparation.
- Latest checkpoint: **S0, the commit containing this revision**; identify it with `git log -1 --format='%H %s'` and verify subsequent history when resuming.
- **Publication state at checkpoint preparation: local only; push approval pending. No push is authorized.** A future task must inspect the remote and user instructions; this committed observation is not live remote state.
- **Unity port completeness: 0 implemented application features.** Only planning/resume documentation and ignore rules exist. `UnityProject/` is not yet an openable Unity project. No scenes, packages, settings, converted scripts, encoders or UI have been created.
- Read root `AGENTS.md`, `UnityProject/AGENTS.md` and `MIGRATION_PLAN.md` before resuming. The plan contains the feature/settings/metrics inventory, behaviour contracts, dependencies, stages and acceptance gates.

## Stage ledger

| Stage | State | Evidence / remaining work |
| --- | --- | --- |
| S0 - resumable checkpoint foundation | Complete | Branch created; handoff/plan/agent instructions and local ignore rules; original baseline validated. No Unity implementation. |
| S1 - verified toolchain and Unity scaffold | Not started | Exact editor, licence/network/module access, package pins, root compile exclusion and real minimal scene required. |
| S2 - compatible domain helpers and fixtures | Not started | Models, language/API adaptations, numeric/RNG/reference checks. |
| S3 - core movement/gates/accounting | Not started | Engine port and corresponding Unity/reference tests. |
| S4 - responder/Dasher parity | Not started | Lock, priority, fatigue, robot and human-only Dasher verification. |
| S5 - world/camera | Not started | Hall/funnel/gates/materials/projection/zoom. |
| S6 - entities/interaction | Not started | Humanoids/robots/gait/overlays/picking/placement. |
| S7 - setup UI | Not started | All mode-specific settings, validation and controls. |
| S8 - runtime/results UI | Not started | Run lifecycle, live dashboards, metrics and results. |
| S9 - persistence | Not started | JSON compatibility/migration, Files UI and path validation. |
| S10 - frame capture/encoder | Not started | Playable built-player full-UI MP4 and fallback AVI. |
| S11 - recording lifecycle | Not started | Pause/resize/reset/return/close/error finalization. |
| S12 - CI/Windows distribution | Not started | Clean-checkout pipeline, full player and Windows checks. |
| S13 - final parity/release documentation | Not started | Feature/visual/performance evidence and approved differences. |

## Completed work in S0

- Confirmed origin, clean original checkout and matching local/GitHub reference commit. Started from local `work`; created `unity-migration` without changing/pushing `main`.
- Recorded the full inventory and staged plan in `MIGRATION_PLAN.md`.
- Added root discovery guidance and `UnityProject/AGENTS.md` so a new task follows the same resume, validation, commit and push-approval rules without chat history.
- Added Unity-specific local ignores for generated caches/builds/local data and activation/secret artefacts. No generated Unity state or credentials is part of this checkpoint.
- Preserved all original application source, project files, dependencies and tests.

## Toolchain and decisions

| Item | Actual state / decision |
| --- | --- |
| Reference toolchain | .NET SDK 8.0.426, target net8.0 / C# 12. |
| Current machine | Debian 13 x86-64; .NET tools are activated in this workspace via `/workspace/.onboarding/activate.sh`. This helper is outside Git and must not be assumed in a fresh task. |
| Unity editor | Not installed/detected. Proposed Unity 6.3 LTS (`6000.3.x`); no exact patch selected or validated. |
| Unity packages | None installed/pinned. Proposed URP, uGUI/TextMeshPro, Input System, Test Framework and Unity-supported Newtonsoft JSON. |
| Player/runtime | Proposed Windows x86-64 Mono / .NET Standard 2.1; no build exists. |
| Recording dependency | Reference uses OpenCvSharp4 + runtime.win 4.13.0.20260627. Cached package includes .NET Standard 2.0/2.1 managed assemblies and win-x64 native DLLs; Unity compatibility is untested. |
| Core boundary | Port compatible C# source inside UnityProject, independent of UnityEngine; System.Numerics mathematics retained; Unity converts for display only. Original source/tests remain reference. |
| Movement | No Unity physics/NavMesh substitution; preserve implemented rules and execution/RNG order. |
| Known uncertainty | Dasher preferred holding point remains implemented (6.0, 0.65) m, clamped. Conflicting x=4 m/left-centre notes do not authorize a change. |
| Approved differences | None. Editor local-data paths, rendering/font differences, numerical/performance thresholds and any necessary behaviour deviation need explicit decisions/approval before divergence. |

## Validation evidence for this checkpoint

Commands run from the repository root on 2026-10-17, using the activated .NET SDK:

```sh
dotnet --version
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore
```

- SDK: **8.0.426**.
- Restore: **passed**.
- Build: **passed, four warnings, zero errors**: two existing CS9057 OpenCvSharp analyzer/compiler-version warnings, one xUnit2031 style warning and one xUnit2009 style warning. No warning was suppressed or fixed in this checkpoint.
- Standard xUnit/VSTest: **270 executed, 270 passed, 0 failed, 0 skipped**. No tests/assertions were added, removed or disabled in S0.
- Documentation/whitespace, staged-file scope and representative cache/activation ignore rules: validated before the S0 commit; inspect the checkpoint diff when resuming.
- Unity import/compilation, EditMode, PlayMode, Windows build, Windows native encoding and desktop UI/visual checks: **not performed in S0**. Original .NET test success does not validate Unity.

## Known issues and prerequisites

1. No Unity Editor or usable Unity licence was established here. Only licence-variable names/presence were inspected, not values. Obtain the editor/licence through supported secure settings; never put credentials or activation files in Git/chat.
2. Access to official Unity documentation endpoints previously returned proxy 403. No exact current editor/package version was verified. Future tasks must recheck access rather than treat this as a permanent denial or disable verification.
3. A Windows runner/machine with graphics and native encoder support is needed for required built-player/desktop/recording gates. None has been established.
4. The root SDK compile glob must exclude `UnityProject/**/*.cs` before Unity C# is added. This documentation-only checkpoint intentionally leaves the project file unchanged.
5. Existing tests cover initial seeded behaviour and recording path helpers, not full cross-runtime trajectories or actual video encoding. Those gaps remain migration work.
6. Local commits alone do not make this branch available to a new GitHub-based task. User-approved push and verified remote SHA are needed for that handoff.

## Exact next steps

1. **Stop at S0 and ask whether to push this specific local checkpoint to `origin/unity-migration`. Do not push or start S1 without resolving the current checkpoint approval boundary.** No merge or PR is requested.
2. If the user explicitly approves the push, check branch/cleanliness and local/remote history, push only the approved checkpoint to `unity-migration` (never force), then verify `git ls-remote origin refs/heads/unity-migration` matches the approved SHA. Report the result. The next status update must record that transition; a saved status entry alone is not push authorization.
3. When resuming implementation, inspect actual branch/history/files and user steering. Begin S1: verify an installed/licensed Unity editor and supported exact patch, Windows build modules and package access. Establish who can execute Windows desktop/encoding checks.
4. Pin verified editor/packages/backend, add only the root compile exclusion needed to protect the original app, and create a real minimal scene/settings/assets scaffold. Label the scene as foundation only, with all missing application features still not started.
5. Run actual S1 Unity import/compile/scene checks and original restore/build/tests. If editor/licence/access is unavailable, mark S1 blocked or implemented/unvalidated; do not call it complete. Record exact failing command, result and independent work performed.
6. Update this status with S1 files, tool/dependency pins, validation, known issues and next substep; inspect/stage intended files, commit locally on `unity-migration`, then stop for approval to push that new checkpoint.

## Interruption recovery

No S1 substep or uncommitted implementation is in progress at S0. On a later interruption, inspect `git status` and the latest stage/substep notes before running commands. Preserve unrelated files, validate existing work and continue the first unfinished substep rather than regenerating the project or replaying completed stages. Incomplete features and validation must remain explicitly labelled. Never infer that an unperformed check passed or that a previous checkpoint's push approval covers a new commit.
