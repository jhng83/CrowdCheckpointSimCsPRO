# Unity migration status

Last updated: 2026-10-18 (Asia/Singapore).

## Read this first

- Repository: https://github.com/jhng83/CrowdCheckpointSimCsPRO
- Branch: `unity-migration`. Preserve the original app and tests; never push/merge to `main` for this migration.
- Reference: `8786c52f5be7aea683a9d14b69682bf07ed2f6e8`. GitHub `main` remained at that SHA when this task resumed.
- **S0 publication verified:** remote `unity-migration` matched `9c22095aca99d6c5f61015efce7d24a5adfa0a52`, which the user approved and pushed. Do not ask to push S0 again.
- **S1a publication verified:** remote `unity-migration` matched approved checkpoint `8b47e9c0505f8add22a3a03e00e93c2bbcbf74e6` at the earlier S1-CI resume. Do not ask to push S1a again.
- Latest local checkpoint: **S1c, the commit containing this revision**; latest pushed checkpoint before this work is S2a `3acf45819a6fe807c0ece3bbbdb525bafdd71450`. Identify it with `git log -1 --format='%H %s'` and reconcile later history on resume.
- **S1-CI publication previously verified:** remote `unity-migration` matched approved `10b229dd0c939ea2bc0aae4d7b3591676b9ed07f`. **S2a publication verified:** remote unity-migration matches approved `3acf45819a6fe807c0ece3bbbdb525bafdd71450`. **S1c publication state at preparation: local only; stop for approval to push this specific new checkpoint.** Prior approvals do not authorize it.
- **Overall S1: source/preparation implemented; actual Editor work blocked; NOT complete.** Foundation, standalone validation and optional cloud-CI orchestration are authored. A cloud runner is not required for acceptance. Local static/result-gate checks pass; no Unity import/package resolution/compilation, Unity tests, visual check or Windows build has run. GitHub job results after S1-CI publication were not verified. A skipped CI Unity job must never count as validation.
- **Application feature parity: 0 completed migrated application features.** S2a helpers are implemented and checked on .NET, not connected to the placeholder scene. No crowd movement, responder state machines, application controls, metrics UI, persistence or recording has been ported. The exact-version project scaffold is incomplete until its licensed Editor/UPM validation and generated source settings are committed.
- Read root `AGENTS.md`, `UnityProject/AGENTS.md`, `MIGRATION_PLAN.md`, `README.md`, `Documentation/TOOLCHAIN.md`, **`Documentation/S1_CHECKLIST.md`** and `Documentation/CLOUD_CI.md`. The latter records cloud setup, licence blockers and researched costs. No chat history is needed.
- **Latest user constraints:** complete feasible S1 work without setting up a cloud runner; preserve completed migration work. Keep actual Unity checks explicit and S1 incomplete until they pass; do not access the user's computer or provision services. The user reports no confirmed existing CI licence. No subscriptions, machines, paid jobs or personal-computer connections were created.

## Stage ledger

| Stage | State | Evidence / remaining work |
| --- | --- | --- |
| S0 - resumable foundation | Complete; published | Checkpoint 9c22095 verified on GitHub; original handoff retained. |
| S1a - authored foundation and isolation | Published; Unity unvalidated | Exact version requests/provenance, tracked scene and metadata, assemblies, setup/build commands and unrun Unity tests. This is a partial S1 checkpoint, not an accepted Unity stage. |
| S1-CI - cloud validation preparation | Published; Unity unrun, hosted results unverified | Disabled-by-default Unity workflow, real-Editor receipt/result gates, safe source handoff, cloud runner/licence/cost instructions. No cloud service is provisioned. |
| S1c - runner-independent preparation | Implemented / locally checked; Unity unvalidated | Standalone command, retained cloud API/guards, full configuration validation, two additional unrun Unity tests and S1 checklist. No runner set up. |
| S1b - finish S1 with actual Unity | Blocked / not run | Licensed Editor/package access needed, then actual settings/lock, compilation and Unity checks; runner is optional. Validation deferred; source stages may proceed without accepting S1 (latest user instruction). |
| S2 - domain helpers and fixtures | S2a implemented / Unity unvalidated; stage incomplete | Four portable helper sources; .NET Standard 2.1/C# 9 build and 30 .NET checks pass. Five new Unity tests unrun. Config/models/math/RNG remain. |
| S3 - movement/gates/accounting | Not started | Core port and corresponding Unity/reference tests. |
| S4 - responder/Dasher parity | Not started | Ownership, priorities, fatigue, robot and human-only Dasher verification. |
| S5 - world/camera | Not started | Hall/funnel/gates/materials/projection/zoom. |
| S6 - entities/interaction | Not started | Humanoids/robots/gait/overlays/picking/placement. |
| S7 - setup UI | Not started | Every setting/control, validation and mode-specific visibility. |
| S8 - runtime/results UI | Not started | Lifecycle, dashboards, metrics and results. |
| S9 - persistence | Not started | JSON schema/legacy cross-loading, Files UI and path policy. |
| S10 - frame capture/encoder | Not started | Built-player full-UI MP4 and fallback AVI. |
| S11 - recording lifecycle | Not started | Pause/resize/reset/return/close/error finalization. |
| S12 - CI/Windows distribution | Not started | Clean-checkout jobs, complete player and Windows functional checks. |
| S13 - parity/release documentation | Not started | Full feature/visual/performance evidence and approved differences. |

## Completed and authored work in S1a

- Reconciled actual checkout/remote against the published S0 checkpoint; preserved its files and original simulation source/tests.
- Added only `Compile Remove="UnityProject/**/*.cs"` to the root project. Inspected actual MSBuild Compile items: Unity files excluded, original core retained. The original .NET application remains buildable.
- Requested Editor 6000.3.26f1, URP 17.3.0, Input System 1.17.0, uGUI 2.0.0 and Test Framework 1.4.5; documented evidence and its limits. No package resolution is claimed and no lockfile was fabricated.
- Authored `Assets/Scenes/MigrationFoundation.unity` with an orthographic camera and serialized FoundationBootstrap reference, scene build settings, Force Text editor settings, four assembly definitions and 15 unique asset/folder GUID metadata files.
- Authored FoundationBootstrap's explicit temporary diagnostic. It is not a simulation, final camera implementation, uGUI interface or completed application feature.
- Authored Editor setup that creates URP assets, applies quality/player settings and configures the scene; an Editor-only Windows development-build entry point checks failure. These commands have NOT run in Unity. Full generated settings/URP assets do not yet exist.
- Authored two EditMode wiring tests and one PlayMode smoke test. **All three are unrun**, not passing or skipped.
- Added a static checker plus six Python positive/negative fixture tests for missing/duplicate metadata, camera references, build-scene enablement and version pins. These are NOT Unity tests.
- Added first-import/configure/test/build commands in `README.md`, provenance in `Documentation/TOOLCHAIN.md`, and Python cache ignores. No credentials/caches are intended for the checkpoint.

## Completed and authored work in S1-CI

- Preserved all S1a authored files/GUIDs and original application/tests; did not start S2 or access any personal computer.
- Added `.github/workflows/unity-cloud-validation.yml`: automatic reference/static checks only on relevant approved migration-branch pushes, with an opt-in dedicated Windows **cloud VM** job. Unity job remains skipped unless `UNITY_CLOUD_CI_ENABLED=true` and the configured runner exists. No GitHub variables/settings were changed and no workflow was dispatched.
- Pinned official Actions to checked SHA revisions, read-only contents permission, serialized execution, bounded timeouts, short artifact retention, no PR/fork triggers, no auto-commit/push. Added actionlint's custom cloud-runner label configuration.
- Authored `CloudFoundationValidation.cs` and its new .meta (16 total asset/folder GUIDs now). It will call actual Editor setup twice, validate retained asset identities/scene/settings/real lockfile and write a receipt. It has NOT compiled or run in Unity.
- Added `run_unity_cloud.py` for pre-activated, designated Windows cloud runners. It invokes actual Unity, preserves exit codes, uses fresh result directories, never installs/activates a licence, and optionally builds the Windows foundation. No placeholder/mock engine is used.
- Added strict original-TRX/Unity-NUnit/Editor-receipt gates, sanitized summaries and allowlisted generated-source export in `ci_results.py`. Rejects zero/missing/wrong/failed/skipped runs, incomplete receipts and detected credential-like export fields. Raw activation/editor logs stay private to the cloud runner. Source artifacts still require review before commit.
- Added 23 CI-gate fixture tests; together with six preserved static-validator tests, **29 Python tests pass**. Fixtures are synthetic parser/security tests, not Unity run evidence.
- Documented cloud-only setup and alternatives (GameCI, Unity Build Automation), licence uncertainty, current GitHub prices and unverified VM/Unity quotes in `Documentation/CLOUD_CI.md`. User has no confirmed paid CI entitlement, so activation/provisioning is not assumed or performed.

## Toolchain, dependencies and design decisions

| Item | Actual status |
| --- | --- |
| Reference SDK | .NET 8.0.426 / net8.0 / C# 12; restored and executed. |
| Cloud machine | Debian 13 x86-64. Existing `/workspace/.onboarding/activate.sh` supplies .NET here; it is outside Git and must not be assumed on another task/machine. |
| Editor version | **Requested 6000.3.26f1**, source-tag identity verified at official tag SHA c57f9f00d22b3794130a9bfa76dd7c8b32e4f2f1. No installed Editor, licence/revision/installer checksum or successful import is established. |
| Package requests | URP 17.3.0; Input System 1.17.0; uGUI 2.0.0 (TMP source included); Test Framework 1.4.5; required built-in modules 1.0.0. Metadata provenance is recorded; compatibility/resolution is unverified. |
| Lockfile/settings | Actual UPM lockfile, generated URP assets and full Unity-generated player/graphics/quality settings are pending licensed S1b. Do not invent them or mark them complete. |
| Player settings | Editor Configure method requests Windows x86-64 Mono, .NET Standard and 1500x900 resizable windowed foundation. Method is unexecuted. Final maximized-window parity is later work, not claimed. |
| Packages deferred | Newtonsoft JSON, OpenCvSharp, application fonts/input activation and final rendering assets are not imported/implemented by this checkpoint. |
| Core boundary | Assets/Simulation ports enums/timing/acquisition/quota only. Its asmdef has noEngineReferences=true and no assembly dependencies; System.Numerics retained. .NET Standard 2.1/C# 9 compilation passes. No full engine, physics/NavMesh or RNG change. |
| CI availability | Runner setup deferred by user. Preserved opt-in Unity job is not enabled by this work; no VM/licence/service provisioned. Hosted reference job results remain unverified. User computer must not be accessed. |
| CI tools checked here | Official actionlint 1.7.11 release binary downloaded outside Git and SHA256 verified; workflow lint passes. Python result/static checks execute locally; no Unity execution is implied. |
| Behaviour differences | None approved or implemented. Temporary diagnostic UI/window/camera is clearly foundation-only. Keep implemented Dasher preferred holding point (6.0, 0.65) m with clamping pending intent clarification. |

## Historical S1a validation evidence (2026-10-18)

Baseline on pushed S0: original restore/build passed with four existing warnings and zero errors; standard VSTest executed **270 passed / 0 failed / 0 skipped**.

After the root project exclusion and authored Unity files, commands run from repository root with .NET activated:

```sh
python3 UnityProject/Tools/validate_foundation.py
python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v
python3 UnityProject/Tools/validate_foundation.py --check-dotnet
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore
git diff --check
```

- Static scene/metadata/assembly/package-request checks: **passed**, 15 asset GUIDs / 4 assemblies. This does not deserialize the scene in Unity or compile C#.
- Python validator fixture suite: **6 executed, 6 passed, 0 failed**. Tests demonstrate malformed handoff assets/settings are rejected; no Unity API stubs or substitute engine were used.
- Actual MSBuild Compile-item isolation: **passed**; root Core remains compiled and UnityProject scripts are excluded.
- Original restore: **passed**.
- Original post-change incremental build: **passed, one existing CS9057 warning, zero errors**. Initial baseline build also emitted the other existing compiler/xUnit style warnings; no warnings were suppressed/fixed.
- Original standard VSTest: **270 executed, 270 passed, 0 failed, 0 skipped**. Original tests/assertions unchanged.
- Unity package resolution/import/C# compilation, two EditMode tests, one PlayMode test, setup/build entry points, desktop visual/input checks and Windows build/encoding: **NOT PERFORMED**. Headless/static/.NET results are not evidence of those capabilities.

## Historical S1-CI validation evidence (2026-10-18)

- Resumed at actual pushed S1a `8b47e9c`; original restore/build passed with **four existing warnings, zero errors**; 270 standard original tests passed, none skipped.
- Re-ran original tests with a fresh current-run TRX and exercised the new actual-result gate: **270 executed, 270 passed, 0 failed, 0 skipped**, per-case results agree with counters and completed summary. No original source/test/project edits in this checkpoint.
- Foundation checker: **passed**, now **16 asset GUIDs / 4 assemblies**; MSBuild isolation remains verified. The only new Unity asset is the Editor receipt helper/.meta.
- `python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v`: **29 executed, 29 passed, 0 failed**. Synthetic XML/receipt/export fixtures are not actual Unity test results.
- `python3 UnityProject/Tools/ci_results.py reference UnityProject/Artifacts/Reference/reference.trx`: **passed** on the current real .NET TRX.
- `actionlint` **1.7.11** with `.github/actionlint.yaml`: workflow syntax/action expressions **passed**. This does not execute an Actions runner or prove licence/package access. Local tool install is outside Git; official release SHA256 was checked.
- Negative cloud preflight: this Linux environment was correctly **rejected** with exit 1 before any Editor invocation. This expected rejection validates the guard, not Unity. Its generated diagnostic is ignored under Artifacts.
- Whitespace/staged-scope/cache/source-reference checks are performed before the checkpoint commit.
- **Unrun:** GitHub hosted reference job, configured cloud runner job, new Editor helper compilation/execution, all three Unity smoke tests, Windows player build/run, desktop visuals and encoding. No service/resource was provisioned, no activation or credential values were requested/printed, no subscription was purchased and no personal computer was contacted.

Reproduce available checks from repository root, with .NET 8 activated where needed:

```sh
python3 UnityProject/Tools/validate_foundation.py --check-dotnet
python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore --results-directory UnityProject/Artifacts/Reference --logger 'trx;LogFileName=reference.trx'
python3 UnityProject/Tools/ci_results.py reference UnityProject/Artifacts/Reference/reference.trx
# With official actionlint 1.7.11 available:
actionlint -shellcheck= .github/workflows/unity-cloud-validation.yml
```

## Historical S2a implementation and actual validation

- Confirmed pushed HEAD/remote migration branch `10b229dd0c939ea2bc0aae4d7b3591676b9ed07f`, clean checkout, and unchanged remote main `8786c52f5be7aea683a9d14b69682bf07ed2f6e8` before editing. Original source/project/tests are untouched in this checkpoint.
- Adapted four exact-reference source files under `Assets/Simulation`: SimulationEnums, OperatorTiming, OperatorAcquisition and DetectionQuotaTracker. Only block namespaces, explicit System imports and the equivalent NaN/Infinity radius guard differ. Preserve math order and current quota rules.
- Added `CrowdCheckpoint.Simulation.asmdef` with `noEngineReferences=true` and no assembly dependencies. The scene remains the original diagnostic placeholder; helpers are not a completed simulator.
- Added SDK validation projects under `Validation/`: the exact production helper sources compile against **.NET Standard 2.1 / C# 9**, explicit imports, warnings as errors. Reference tests run on **.NET 8.0.426**, not Unity Mono. No Unity packages/dependencies/version pins changed.
- **30 .NET parity/regression cases passed / 0 failed / 0 skipped**. Four fractions each compare 1,000 quota decisions/counters with resets/reconfiguration; timing floats are compared bit-for-bit; enums/constants, radius boundaries/non-finite input and validation behavior are checked. See `Validation/README.md` for mappings to the original suite.
- Five framework-independent expectation methods run in that .NET suite and are wired into five new NUnit EditMode methods. Total authored Unity tests: **7 EditMode + 1 PlayMode; all UNRUN in Unity**. The CI result gate now requires the new methods; a synthetic regression rejects a foundation-only run.
- Foundation/static isolation checker passed: **24 asset/folder GUIDs, 5 assemblies**. Original MSBuild still excludes every UnityProject C# source and retains Core. Two negative fixture cases enforce the no-engine/dependency boundary.
- Python suite: **32 passed / 0 failed** (8 foundation + 24 result-gate fixtures); none are Unity evidence.
- Original solution restore/build passed: **3 pre-existing warnings** in the incremental solution build (CS9057, xUnit2031, xUnit2009), **0 errors**. Standard original VSTest: **270 passed / 0 failed / 0 skipped**, fresh `s2a-reference.trx` verified by the strict result gate.
- Initial portable-test build also emitted two existing/transitive CS9057 warnings from the original OpenCvSharp analyzer. A forced rebuild of portable production sources passed with 0 warnings / 0 errors, with warnings treated as errors. No warning suppression or reference assertion changes were added.
- The existing ordinary GitHub reference job now includes portable compilation/tests. The optional Unity cloud job stays opt-in; no settings/runner/service were enabled or dispatched. Workflow lint with actionlint 1.7.11 passed locally; actual GitHub job results are unverified.
- **Not performed:** actual Unity import/package resolution/compilation, any Unity EditMode/PlayMode test, scene playback, desktop UI/input/visual checks, Windows build/player run, video encoding, cross-runtime RNG/long-run trajectory verification. S1/S2 remain incomplete. No user computer was accessed.

Commands run from repository root with `.NET` activated via `/workspace/.onboarding/activate.sh`:

```sh
dotnet restore UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --disable-parallel
dotnet test UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --no-restore --logger 'trx;LogFileName=s2a-domain-parity-final.trx' --results-directory UnityProject/Artifacts/DomainParity
dotnet build UnityProject/Validation/Domain/Domain.csproj --no-restore --no-incremental
python3 UnityProject/Tools/validate_foundation.py --check-dotnet
python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore --results-directory UnityProject/Artifacts/Reference --logger 'trx;LogFileName=s2a-reference.trx'
python3 UnityProject/Tools/ci_results.py reference UnityProject/Artifacts/Reference/s2a-reference.trx
actionlint -shellcheck= .github/workflows/unity-cloud-validation.yml
git diff --check
```

## Current S1c work and actual validation (2026-10-18, Asia/Singapore)

- Verified sanitized repository URL, clean branch `unity-migration`, pushed/current HEAD `3acf45819a6fe807c0ece3bbbdb525bafdd71450` and unchanged remote main `8786c52f5be7aea683a9d14b69682bf07ed2f6e8` before editing. Preserved all S2a simulation helpers/tests and existing asset GUIDs. Original app/project/tests and package requests are unchanged.
- Rechecked PATH for Unity/UnityHub, common installation directories and presence of editor/licence/cloud-runner environment bindings without printing values. No usable Editor/licence was found. No runner, VM, subscription, activation or personal-computer connection was created.
- Added `Tools/run_unity_validation.py`: standalone command takes an explicit already licensed Editor path (or UNITY_EDITOR_PATH), requires no GitHub/cloud marker, uses unique result directories, private logs, strict real receipt/NUnit gates and allowlisted source export. Optional Windows build refuses old output. Checks existing UnityLockfile and takes a shared orchestration lock to avoid concurrent batch runs.
- Refactored `Tools/run_unity_cloud.py` to delegate to the common command while retaining designated Windows/cloud/self-hosted guards and Artifacts/UnityCloud paths. No workflow/GitHub variables were enabled or dispatched. No automatic stage acceptance, commit, push or upload was added to standalone mode.
- Moved receipt implementation to `Assets/Editor/FoundationValidation.cs`; preserved old CloudFoundationValidation class/method and its metadata as a forwarding public API. Both approved ignored result roots are accepted; case comparison follows the host OS. The real Editor still enforces its exact pinned version.
- Added FoundationProjectSetup.ValidateConfiguration for actual URP/global/all-quality settings, player backend/API/window settings, editor settings and enabled scene. Receipt calls it after setup twice. Added two NUnit methods for idempotence/settings and keeping an already loaded foundation scene open. **All new/old Unity code remains uncompiled/unrun here.**
- Total authored Unity tests now **9 EditMode + 1 PlayMode**, all unrun in Unity. Updated strict required-case names; no assertion disabled. Added 18 synthetic orchestration tests. These mock only the process boundary and exercise preflight, phase ordering, missing/stale/failed results, lock preservation/release, build-output guards, errors/timeouts, cloud compatibility and truthful reporting; they are **not engine/runtime evidence**.
- Baseline/current unchanged original solution restore/build passed with **4 existing warnings** (CS9057 twice, xUnit2031, xUnit2009), **0 errors**. Actual standard VSTest **270 passed / 0 failed / 0 skipped**, fresh s1c-reference.trx accepted by strict parser. Portable suite **30 passed / 0 failed / 0 skipped** with one existing transitive CS9057 warning; preserved S2a sources/assertions unchanged.
- Post-change foundation/MSBuild-isolation checks passed: **26 asset/folder GUIDs, 5 assemblies**. Root Core retained and every UnityProject C# file excluded. Python final run **50 passed / 0 failed** (8 foundation + 24 result-gate + 18 orchestration fixtures). Initial expanded run was 49 pass / 1 fail because a fixture's hard-coded count 9 now matched the actual required count; corrected the malformed-count fixture to use required-count + 1 and reran all 50 successfully.
- Real CLI negative checks: standalone with an explicitly nonexistent Editor returned **1** at preflight with no tests/configuration evidence; cloud wrapper on this unmarked Linux machine returned **1** before Editor invocation. These expected failures prove refusal behavior, not successful Unity checks. Logs/summaries remain ignored under Artifacts.
- Local actionlint 1.7.11 passed on the unchanged workflow. CLI help passed. Whitespace/source-scope/generated-file checks are performed before commit. No dependency pins changed; Python 3.9+ standard library suffices. URP Create API was reconfirmed in the same pinned official Graphics source; no installed Unity compatibility is inferred.
- **Unperformed:** Editor installation/activation/import/UPM resolve/C# compilation, real lockfile and generated source settings/URP assets, all 10 Unity test methods, scene playback, desktop input/rendering/window checks, Windows build/player launch, recording/encoding, actual GitHub jobs and cross-runtime determinism. S1 acceptance stays incomplete. Cloud runner setup is deliberately excluded.
- Handoff/run instructions are in README and Documentation/S1_CHECKLIST.md. Standalone output: Artifacts/UnityValidation/private-* and reports/standalone-*; no automatic upload. Raw logs/XML remain private and ignored. Review allowlisted generated-source candidates before committing them.

Commands executed from repository root with .NET activated:

```sh
python3 UnityProject/Tools/validate_foundation.py --check-dotnet
python3 -m unittest discover -s UnityProject/Tools -p 'test_*.py' -v
dotnet restore CrowdCheckpointSim.sln --disable-parallel
dotnet build CrowdCheckpointSim.sln --no-restore -m:2
dotnet test tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj --no-build --no-restore --results-directory UnityProject/Artifacts/Reference --logger 'trx;LogFileName=s1c-reference.trx'
dotnet restore UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --disable-parallel
dotnet test UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --no-restore --results-directory UnityProject/Artifacts/DomainParity --logger 'trx;LogFileName=s1c-parity.trx'
python3 UnityProject/Tools/ci_results.py reference UnityProject/Artifacts/Reference/s1c-reference.trx
python3 UnityProject/Tools/run_unity_validation.py --help
# Expected exit 1, no Editor invocation:
python3 UnityProject/Tools/run_unity_validation.py --editor /workspace/.onboarding/no-installed-Unity-Editor
python3 UnityProject/Tools/run_unity_cloud.py
actionlint -shellcheck= .github/workflows/unity-cloud-validation.yml
git diff --check
```

## External blockers and uncertain behavior

- No usable installed Unity Editor/licence was established at prior checkpoints. Unity installer/release/documentation/package endpoints returned proxy 403; official Git/raw sources were reachable. No access/licensing change was reported; do not repeat blocked provisioning merely to continue source work. Version pins and package compatibility remain unverified in Unity.
- Real UPM lockfile, generated URP assets/player/graphics/quality settings and Editor scene deserialization remain pending S1b. Do not invent generated settings or accept S1 based on .NET/static success.
- **Cloud runner setup is deferred by the user.** Preserve prior research/tooling without provisioning, purchasing, enabling Unity CI or asking for runner/licence credentials. Do not access the user's computer. An actual licensed Editor is required eventually for Unity acceptance, but its future location is not selected.
- **New intent uncertainty:** original DetectionQuotaTracker.Configure accepts NaN yellow/red fractions because comparison guards do not reject NaN. The port preserves that acceptance and tests it against the reference. Ordinary finite ranges/defaults remain unchanged. Obtain an explicit behavior decision before tightening both implementations; not a blocker for finite configured scenarios.
- Existing Dasher holding-location conflict remains unresolved: preserve implemented preferred (6.0, 0.65) m with wall clamping, not the stale x=4 comment/doc. No behavior difference is approved.
- .NET Standard compilation and .NET-hosted parity do not establish Unity Mono math/RNG/long-run determinism or UI/recording parity. Full application source migration and actual Unity/Windows checks are still substantial remaining work.

## Exact next steps

1. Identify S1c's local checkpoint with git log and **stop for approval to push that exact commit**. S2a is already approved/published; do not ask to push it again. Push only this approved migration checkpoint without force and verify the remote SHA after approval. Keep main unchanged; no PR.
2. **S1 preparation is authored and locally checked; actual S1b needs an explicitly authorized licensed Editor environment, not a cloud runner.** Do not provision a runner or access the user's computer. Do not invent generated lock/settings or mark S1 accepted based on Python/.NET tests.
3. If an authorized Editor environment is available, follow README/Documentation/S1_CHECKLIST.md: close this project's Editor, run the standalone command with an explicit pinned Editor path, inspect actual Configure-twice receipt, fresh **9 EditMode + 1 PlayMode** results, errors, real UPM lock and generated source settings. Verify settings/asset GUIDs; review candidates before committing. Run desktop scene/window and optional Windows build/player checks separately and report them accurately. Fix real failures and rerun affected gates.
4. If no Editor is available, S1's remaining import/generation/compile/test work stays blocked. Under the existing source-migration direction, the next source stage is **S2b configuration/domain models** after this checkpoint is published/resumed. Read original SimulationConfig, Pedestrian, CheckpointOperator, ResponderPost, ScenarioGeometry and corresponding tests; preserve defaults/ranges, clone/validation, init/required invariants, value equality and typed post aliases. Split into another small checkpoint; do not start the full engine/UI at once.
5. Extend Assets/Simulation and the exact-source .NET Standard validation project with direct original comparisons and shared Unity expectations. Preserve all 270 original cases, S2a helpers and S1c tooling. S2c later covers geometry/math/RNG fixtures; preserve random-call order and investigate divergences.
6. Each small source checkpoint updates this status, commits locally and stops for its own push approval. Keep S1/S2 incomplete until their actual Unity gates pass. Source implementation, mock/static/.NET checks and actual Unity evidence remain separate.

## Interruption recovery

Latest work is S1c runner-independent preparation plus synthetic orchestration checks. No Editor, runner or cloud job was started. Verify local/remote history and pending push approval before further work. Preserve S1a scene/GUIDs, optional CI tooling, S2a helpers/tests and new standalone command. Read Documentation/S1_CHECKLIST.md for S1 gates; use README's standalone command only when an authorized licensed Editor exists. Outputs/locks under Artifacts and bin/obj remain ignored. Without an Editor, resume S2b source work after publication; do not repeat runner setup or mark S1 complete.
