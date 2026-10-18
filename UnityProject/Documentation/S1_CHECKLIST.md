# S1 foundation: preparation and acceptance

S1 preparation can be maintained without a cloud runner. **S1 is not accepted yet:** this environment has no usable Unity Editor/licence, and generated settings/package resolution are still pending. The diagnostic scene is a placeholder, not the simulator. No access to the user's computer is authorized.

| Requirement | Work present and checked here | Actual Unity work outstanding |
| --- | --- | --- |
| Preserve reference | Original restore/build, 270 original cases and 30 portable cases pass. MSBuild excludes all UnityProject sources. | Cross-runtime checks later; these .NET results do not validate Unity. |
| Pin toolchain | Editor 6000.3.26f1; URP 17.3.0, Input System 1.17.0, uGUI 2.0.0, Test Framework 1.4.5 requested. Provenance in TOOLCHAIN.md. | Install/licence exact Editor; resolve/verify packages. No real lockfile exists yet. |
| Track project/scene | Stable metadata, enabled diagnostic scene, camera/bootstrap, runtime/Editor/test asmdefs; static checks pass. | Actual import, YAML deserialization and compilation. |
| Configure rendering/settings | Configure creates real URP assets and applies all quality levels, Mono/.NET Standard, 1500x900 resizable window, Force Text and scene settings. ValidateConfiguration verifies those settings. | Run setup with real Editor; inspect/commit generated URP assets, ProjectSettings and UPM lockfile. Code is authored, uncompiled in Unity. |
| Repeatable setup | Receipt checks asset identities after two setup calls. Two additional NUnit tests cover settings/idempotence and preservation of an already loaded scene. | Run **9 EditMode + 1 PlayMode** methods, currently all unrun. |
| Validate without runner | `Tools/run_unity_validation.py --editor <path>` invokes real Editor, checks fresh receipts/XML and exports reviewed-source candidates. CLI negative preflight and 18 synthetic orchestration tests pass. | Execute it on an explicitly authorized licensed Editor environment; no runner needed. This task has not invoked Unity. |
| Preserve cloud tooling | `run_unity_cloud.py` delegates to common orchestration while retaining cloud/self-hosted guards and existing artifact paths. | Nothing to provision: user deferred runner setup. Keep opt-in job disabled. |
| Windows foundation | Existing Editor build entry point and optional `--build-windows`, with module/build-result/output checks and refusal to overwrite old player output. | Actual build and standalone launch; distribute full output folder. This is foundation-only. |
| Desktop evidence | Reports keep desktop/player execution/application parity/stage acceptance false. | Open scene, enter Play Mode, inspect camera/diagnostic, errors and resizable window. Full simulator/recording checks are later stages. |

Use the standalone command documented in [../README.md](../README.md). It requires Python 3.9+ and an already licensed Editor with package access; Windows builds additionally require the corresponding build module. It does not install or activate anything, discover another computer, register a runner, commit, push or mark a stage complete.

Do not run it alongside an open Editor or another batch process for the same project. The script checks UnityLockfile and takes a shared orchestration lock under ignored Artifacts. If a process is interrupted, inspect whether it is still running before manually clearing a stale orchestration lock; never delete another run's lock automatically.

Each run uses unique private logs/results under `Artifacts/UnityValidation/private-*` and an allowlisted source ZIP plus summary under `Artifacts/UnityValidation/reports/standalone-*`. Raw logs/XML can contain private machine/licensing information: review them locally on the authorized machine, preserve account access restrictions and share only reviewed summaries/source candidates. The standalone command does not upload files. Existing cloud mode retains its prior publication policy.

Passing batch checks does not commit generated sources or establish desktop appearance, player launch or full application parity. Review actual version, current-run counts, generated assets/settings/lock and errors, record actual results in MIGRATION_STATUS.md, then checkpoint under the existing push-approval rule. Do not invent generated files or report source/mocked tests as Unity evidence.
