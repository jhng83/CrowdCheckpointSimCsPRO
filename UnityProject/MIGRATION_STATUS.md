# Unity migration status

Last updated: 2026-10-18 (Asia/Singapore).

## Read this first

- Repository: https://github.com/jhng83/CrowdCheckpointSimCsPRO
- Branch: `unity-migration`. Preserve the original app and tests; never push/merge to `main` for this migration.
- Reference: `8786c52f5be7aea683a9d14b69682bf07ed2f6e8`. GitHub `main` remained at that SHA when this task resumed.
- **S0 publication verified:** remote `unity-migration` matched `9c22095aca99d6c5f61015efce7d24a5adfa0a52`, which the user approved and pushed. Do not ask to push S0 again.
- **S1a publication verified:** remote `unity-migration` matched approved checkpoint `8b47e9c0505f8add22a3a03e00e93c2bbcbf74e6` when this task resumed. Do not ask to push S1a again.
- Latest local checkpoint: **S1-CI, the commit containing this revision**; identify it with `git log -1 --format='%H %s'` and reconcile later history on resume.
- **Publication state at preparation: S1-CI is local only; approval to push this new checkpoint is pending. Prior checkpoint approvals do not authorize this push.** Inspect the actual remote/user instructions when resuming.
- **Overall S1: implemented / unvalidated, externally blocked; NOT complete.** Foundation and cloud-CI orchestration are authored. Local static/result-gate checks pass; no GitHub job, cloud VM, Unity import/package resolution/compilation, Unity tests, visual check or Windows build has run. A skipped CI Unity job must never count as validation.
- **Application feature parity: 0 completed migrated features.** The OnGUI diagnostic is explicitly a placeholder. No crowd movement, responders, application controls, metrics, persistence or recording has been ported. The exact-version project scaffold is incomplete until its licensed Editor/UPM validation and generated source settings are committed.
- Read root `AGENTS.md`, `UnityProject/AGENTS.md`, `MIGRATION_PLAN.md`, `README.md`, `Documentation/TOOLCHAIN.md` and **`Documentation/CLOUD_CI.md`**. The latter records cloud setup, licence blockers and researched costs. No chat history is needed.
- **Latest user constraints:** continue feasible cloud work, keep S1 incomplete until actual Unity validation, investigate cloud CI setup/costs, and do not access the user's computer. The user reports no confirmed existing CI licence. No subscriptions, machines, paid jobs or personal-computer connections were created.

## Stage ledger

| Stage | State | Evidence / remaining work |
| --- | --- | --- |
| S0 - resumable foundation | Complete; published | Checkpoint 9c22095 verified on GitHub; original handoff retained. |
| S1a - authored foundation and isolation | Published; Unity unvalidated | Exact version requests/provenance, tracked scene and metadata, assemblies, setup/build commands and unrun Unity tests. This is a partial S1 checkpoint, not an accepted Unity stage. |
| S1-CI - cloud validation preparation | Prepared / locally checked; jobs unrun | Disabled-by-default Unity workflow, real-Editor receipt/result gates, safe source handoff, cloud runner/licence/cost instructions. No cloud service is provisioned. |
| S1b - finish S1 with actual Unity | Blocked / not run | Supported licence + cloud Windows runner/package access needed, then actual settings/lock, compilation and Unity checks. Finish S1 before S2. |
| S2 - domain helpers and fixtures | Not started | Language/API adaptation, math/RNG/reference checks. |
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
| Core boundary | No core port yet. Keep compatible simulation C# independent of UnityEngine and retain System.Numerics. No physics/NavMesh movement or changed RNG/update order. |
| CI availability | Prepared only. No registered/selected cloud VM, confirmed CI licence, GitHub setting change, hosted job run or paid service. User computer must not be accessed. |
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

## Current S1-CI validation evidence (2026-10-18)

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

## External blockers and uncertain checks

1. No Unity/Hub executable was found on PATH or in the inspected installation locations; relevant editor-path/licence variables were absent. A filename search of accessible `/opt`, `/usr/local`, `/workspace` and `/home/agent` found no Unity/Hub binary or `Unity_lic.ulf`; `/opt/containerd` was unreadable, so this is not a claim of exhaustive machine access. No usable Editor/licence was established. Do not infer licence availability or request credential values in chat.
2. Read-only checks returned proxy **403 Tunnel connection failed** for:
   - `services.api.unity.com/unity/editor/release/v1/releases?limit=1&version=6000.3`
   - `docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html`
   - `packages.unity.com/com.unity.render-pipelines.universal`
   - `download.unity3d.com/` (HEAD)
   - `public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage` (HEAD)
   Official raw GitHub metadata and Git operations worked. No verification was disabled and no proxy/network policy was broadened. Recheck after a meaningful access change.
3. The earlier cloud-first check found no Editor/licence/access. Latest instruction is cloud CI investigation with **no personal-computer access**. The user reports **"Not sure / no existing CI licence"**. The prepared cloud-VM workflow requires supported activation on that cloud machine, not collection of a licence from the user's computer. No runner/provider/region/entitlement or billing configuration is established. See CLOUD_CI.md before enabling or purchasing anything.
4. Version requests are based on source metadata, not verified installations or resolved compatibility. Necessary changes require recorded evidence and revalidation. Source-tag SHA is not an Editor build revision/installer checksum.
5. The manually authored scene, Editor APIs, NUnit assemblies and configuration are unvalidated in Unity. The configuration command must generate real settings/URP assets, followed by review and commit, before S1 can be complete.
6. Original long-run cross-runtime determinism, full UI and video encoding remain future validation. Headless cloud test success alone will not establish desktop visual/encoding parity; suitable cloud Windows graphics/encoding validation remains required.
7. Standard hosted GitHub prices were verified in official source: Linux $0.006/min, Windows $0.010/min for metered use; public standard execution is free, private use depends on remaining allowance. The prepared Unity job uses a separate cloud VM, whose provider/region/disk/licence costs are not quoted. Unity plan/Build Automation pricing endpoints were blocked. Do not promise free Unity CI or create paid resources without explicit approval.
8. Pushing this workflow starts **reference-checks** on the migration branch (allowance/metering may apply); Unity is **off by default**. A workflow file on a branch is not proof an Actions job ran. Actual job results must be inspected after an approved push.

## Exact next steps

1. **Commit S1-CI locally and stop for approval to push that exact checkpoint to origin/unity-migration.** No push/PR/merge is authorized by earlier approvals. Tell the user that an approved push will start baseline GitHub checks, while Unity stays disabled unless separately configured/enabled.
2. After explicit push approval, compare histories, push only the approved commit without force and verify the remote SHA. Inspect the hosted reference job's actual result; do not assume a successful Git push means CI passed.
3. Do not access the user's computer or start/buy a machine/subscription. Review `Documentation/CLOUD_CI.md` with the user: confirm a supported cloud Unity licence/entitlement and obtain any necessary provider/region/budget approval before provisioning. Current choice is a **prepared recommendation**, not a deployed runner.
4. Establish a dedicated Windows CLOUD VM, pinned licensed Editor, package access, GitHub runner labels/marker and protected environment as documented. Leave `UNITY_CLOUD_CI_ENABLED` absent/false until this is ready and costs approved. GitHub Actions variables/settings and licence activation must use secure supported flows, never values in chat/Git.
5. Resume **S1b**, not S2: on a separately approved push/run, execute actual Configure-twice/receipt, EditMode and PlayMode gates; optionally build Windows. Verify exact version, fresh result files, actual counts and source artifacts. Failure logs remain private on the cloud runner; sanitise any information brought back.
6. Retrieve and review actual generated URP assets, player/graphics/quality settings and UPM lockfile using authorised access. Preserve existing source/GUIDs and inspect exported files for credentials; no automatic application/commit of artifacts is permitted.
7. Fix real Unity issues and rerun affected checks. Keep S1 incomplete until actual import/compile/scene gates pass; record unperformed desktop/Windows checks separately. Update status with job/checkpoint/version/counts/artifacts, commit locally and stop for approval to push that next checkpoint.

## Interruption recovery

S1-CI contains repository automation/research and local result-gate tests only. No Editor processes or cloud jobs were started. On resume verify branch, local/remote SHA and pending push approval; preserve S1a scene/GUIDs and the CI work. Inspect actual configuration before repeating setup. Do not regenerate the scaffold, enable a paid job by assumption, copy licence state from a personal computer, advance S2 or treat fixture/static/hosted-reference tests as Unity validation.
