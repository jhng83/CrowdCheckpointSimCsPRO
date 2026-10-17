# Unity migration status

Last updated: 2026-10-18 (Asia/Singapore).

## Read this first

- Repository: https://github.com/jhng83/CrowdCheckpointSimCsPRO
- Branch: `unity-migration`. Preserve the original app and tests; never push/merge to `main` for this migration.
- Reference: `8786c52f5be7aea683a9d14b69682bf07ed2f6e8`. GitHub `main` remained at that SHA when this task resumed.
- **S0 publication verified:** remote `unity-migration` matched `9c22095aca99d6c5f61015efce7d24a5adfa0a52`, which the user approved and pushed. Do not ask to push S0 again.
- Latest local checkpoint: **S1a, the commit containing this revision**; use `git log -1 --format='%H %s'` and inspect later history on resume.
- **Publication state at preparation: S1a is local only; approval to push this new checkpoint is pending. S0 approval does not authorize S1a.** This is a historical observation; inspect the remote/user instructions when resuming.
- **Overall S1: implemented / unvalidated, externally blocked; NOT complete.** Root compile isolation and static integrity checks pass. A minimal scene, diagnostic script, setup/build commands and three Unity tests are authored. Unity import, package resolution, C# compilation, scene runtime/visuals and Windows build are unperformed.
- **Application feature parity: 0 completed migrated features.** The OnGUI diagnostic is explicitly a placeholder. No crowd movement, responders, application controls, metrics, persistence or recording has been ported. The exact-version project scaffold is incomplete until its licensed Editor/UPM validation and generated source settings are committed.
- Read root `AGENTS.md`, `UnityProject/AGENTS.md`, `MIGRATION_PLAN.md`, `README.md` and `Documentation/TOOLCHAIN.md`. These files are the self-contained handoff; no chat history is needed.

## Stage ledger

| Stage | State | Evidence / remaining work |
| --- | --- | --- |
| S0 - resumable foundation | Complete; published | Checkpoint 9c22095 verified on GitHub; original handoff retained. |
| S1a - authored foundation and isolation | Static/.NET checks passed; Unity unvalidated | Exact version requests/provenance, tracked scene and metadata, assemblies, setup/build commands and unrun Unity tests. This is a partial S1 checkpoint, not an accepted Unity stage. |
| S1b - finish S1 with actual Unity | Blocked / not run | Licensed Editor/package access, real lockfile/URP assets/project settings, compilation, smoke tests and visual evidence needed. Finish S1 before S2. |
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
| Behaviour differences | None approved or implemented. Temporary diagnostic UI/window/camera is clearly foundation-only. Keep implemented Dasher preferred holding point (6.0, 0.65) m with clamping pending intent clarification. |

## Validation evidence (2026-10-18)

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

## External blockers and uncertain checks

1. No Unity/Hub executable was found on PATH or in the inspected installation locations; relevant editor-path/licence variables were absent. A filename search of accessible `/opt`, `/usr/local`, `/workspace` and `/home/agent` found no Unity/Hub binary or `Unity_lic.ulf`; `/opt/containerd` was unreadable, so this is not a claim of exhaustive machine access. No usable Editor/licence was established. Do not infer licence availability or request credential values in chat.
2. Read-only checks returned proxy **403 Tunnel connection failed** for:
   - `services.api.unity.com/unity/editor/release/v1/releases?limit=1&version=6000.3`
   - `docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html`
   - `packages.unity.com/com.unity.render-pipelines.universal`
   - `download.unity3d.com/` (HEAD)
   - `public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage` (HEAD)
   Official raw GitHub metadata and Git operations worked. No verification was disabled and no proxy/network policy was broadened. Recheck after a meaningful access change.
3. The user selected **"Check this cloud environment first"**. That check was performed: no Editor/Hub or usable activation was established, and the installer/registry requests were blocked as listed above. No Windows PC/runner has been selected or provided. Cloud S1b requires permitted Unity downloads/package access and supported licence activation; otherwise a licensed external machine can finish the same tracked foundation.
4. Version requests are based on source metadata, not verified installations or resolved compatibility. Necessary changes require recorded evidence and revalidation. Source-tag SHA is not an Editor build revision/installer checksum.
5. The manually authored scene, Editor APIs, NUnit assemblies and configuration are unvalidated in Unity. The configuration command must generate real settings/URP assets, followed by review and commit, before S1 can be complete.
6. Original long-run cross-runtime determinism, full UI and video encoding remain future validation. Windows graphics/native encoding access is required before final parity acceptance.

## Exact next steps

1. **Stop after committing S1a locally; ask for approval to push that exact new checkpoint to `origin/unity-migration`. Do not push without approval, merge to main, open a PR or advance to S2.**
2. If explicitly approved, compare local/remote histories, push only the approved commit without force, verify the remote SHA and report it. Record this transition in the next status update; a historical status entry is not authorization.
3. Resume **S1b**, preserving all authored files/GUIDs and completed S0 work. Obtain a licensed Editor 6000.3.26f1 and package access on the user-selected machine. Read `README.md` and the pinned metadata provenance; recheck actual editor/support/modules.
4. Open UnityProject in that Editor; allow UPM/import/compilation. Do not bypass signatures/TLS. Record actual failures. Fix this foundation rather than regenerating it; update version requests only with evidence.
5. Run **Tools > Crowd Checkpoint > Migration > Configure S1 Foundation** (or its README batch entry point). It creates actual URP assets and player/graphics/quality settings. Inspect generated source assets/.meta/settings and the real packages-lock.json; keep caches/licence data ignored.
6. Run the two EditMode tests and one PlayMode test, inspect real result counts, and enter the scene to observe its explicit foundation diagnostic. These are not full simulator checks. Run setup twice to verify idempotence and preservation of existing assets. Exercise the Windows foundation build when the required module/machine is available.
7. Run the Python checks and original .NET restore/build/test again if settings/source/project integration changes. Record current commands/results, generated settings/dependency versions, known issues and next steps. Only mark S1 complete once its Unity import/compile/scene gates actually pass.
8. Commit S1b locally and stop for that checkpoint's push approval. S2 begins only after S1 validation and the checkpoint boundary are resolved.

## Interruption recovery

Current stopping point is authored S1a with no Editor processes started and no core migration begun. On resume inspect branch, diff, latest commit and remote before touching files. Preserve the scene's script/camera GUID references and existing metadata. Do not recreate the scaffold, skip S1b validation or substitute green .NET/static tests for Unity results. Source remains reviewable and all placeholders/unperformed checks must remain explicit.
