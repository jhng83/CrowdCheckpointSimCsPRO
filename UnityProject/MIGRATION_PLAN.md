# Unity migration plan and parity contract

Repository: https://github.com/jhng83/CrowdCheckpointSimCsPRO

Reference commit: `8786c52f5be7aea683a9d14b69682bf07ed2f6e8`. Reference app: root C# 12/.NET 8 Silk.NET/OpenGL/ImGui application. Development branch: `unity-migration`; never merge or push to `main` during this work. This plan describes future work, not implemented Unity features. Read `MIGRATION_STATUS.md` for actual progress.

## Architecture and proposed toolchain

- Complete Unity project under `UnityProject/`, including wired scenes, UI, prefabs/assets, materials/fonts, settings, packages/lockfile, tests, build entry points and run instructions. Preserve the original application and tests as a reference.
- S1a requests Unity **6000.3.26f1**, whose version identity was verified in Unity's official reference-source tag. See `Documentation/TOOLCHAIN.md` for immutable provenance. This is NOT an installed/validated Editor: release API/download/registry access is blocked here. Verify the actual Editor/revision, support and modules before completing S1; do not invent an installer checksum or Editor revision.
- Proposed runtime profile: .NET Standard 2.1, Unity-supported C# syntax; initial Windows x86-64 Mono player. IL2CPP is a separately validated target, not an assumed requirement.
- Proposed presentation: URP orthographic 2.5D meshes/materials, uGUI/TextMeshPro and Input System. Preserve the reference projection, palette, shading, gait, markers, picking and window behaviour. Pool/batch visual objects without changing simulation order. No physics/NavMesh-based movement.
- Reuse core algorithms as a Unity-compatible source assembly with no UnityEngine dependency. Retain System.Numerics within simulation and convert only for presentation. Translate unsupported file-scoped namespaces, required/init/record-struct/collection syntax and runtime APIs with equivalent invariants/value semantics. Explicitly supply imports formerly implicit in the SDK.
- Audit APIs such as ThrowIfNull, generic Enum.IsDefined, Random.Shared and MathF.Tau. Test seeded RNG sequences and floating-point results against .NET 8; do not assume Unity's runtime gives identical trajectories. Preserve random-call order.
- Adapt camera, responsive-scale and humanoid-animation mathematics. Rebuild Silk.NET lifecycle, ImGui UI/drawing and framebuffer capture in Unity.
- Proposed persistence: Unity-supported Newtonsoft JSON package, explicit DTOs/converters maintaining the existing JSON contract. Proposed recording: compatibility spike using OpenCvSharp 4.13.0.20260627's existing .NET Standard assembly and Windows native runtime; Unity full-frame capture replaces GL readback. A runtime recorder is required, not an Editor-only substitute.
- `Packages/manifest.json` pins S1a version requests with provenance in `Documentation/TOOLCHAIN.md`; they are NOT installed/resolved dependencies. Complete actual package compatibility validation and commit the real UPM lockfile in S1b. Pin later native/managed dependencies and retain licences/notices.
- Windows builds use portable `configs` and `recordings` folders beside the executable; proposed Editor equivalents are ignored project-local folders. Any path, codec, appearance or behaviour difference needs an explicit decision and user approval before implementing a divergence.
- Root SDK compile globs include descendant scripts: before adding Unity C#, add an explicit `UnityProject/**/*.cs` compile exclusion while retaining the original target, dependencies and tests.

## Feature-parity inventory

All rows are requirements, not completion claims. Preserve default values, mode-specific visibility, validation, disabled states and feedback.

| Area | Required parity |
| --- | --- |
| Mode selection | Open Funnel / Multi-Lane Barricade; change mode; selection/reset rules; separate mode-specific controls. |
| Setup UI | Hall/crowd, Checkpoint, Operators, Dasher, Files tabs; scrolling and derived values. |
| Placement | Purple/robot tools, valid-position ghost, acquisition ring, left-click add, right-click nearest removal, finish placement, clear each kind, bounds/spacing rejection, at least one responder before confirmation. |
| Run controls | Confirm, Start, Resume, Pause, Stop, same-seed restart, different-seed run, Back to Setup, Exit, return to mode selection; preserve each action's settings/posts/metrics/recording effects. |
| Status/results navigation | Permanent mode/state/elapsed/speed/seed strip and bottom controls; results show/hide; view stopped scene; status messages and button enablement. |
| View | Fixed isometric framing reserved for maximum geometry; 60-180% zoom, viewport wheel zoom, reset 100%, acquisition rings, clipping and projected-body picking. |
| Dasher UI | Explicit select/cancel/Escape, eligible cyan hover/banner, rejection reasons, one-incident lock and Mode 2 eligibility. |
| Visuals/window | Navy/slate theme, semantic colours, floor/walls/exit labels, gates/dividers, posts/docks, humanoids and wheeled robots, procedural gait, target/selection indicators, capacity/progress bars, previews; maximized/resizable Windows client and proportional 1500x900 layout, fonts and scrolling. |
| Persistence | Name/save, select/load, refresh and sorted list; overwrite/sanitization/errors; complete settings and typed posts; invalid-post feedback and legacy migration. |
| Recording | Start/stop/finalizing, full client frame including UI, indicator/duration, paths/errors, codec fallback, resize and lifecycle finalization. |

| Setting | Existing range/meaning | Default |
| --- | --- | --- |
| Hall length / width | 20-30 m / 15-20 m | 20 / 15 m |
| Open Funnel width / fixed length | 5-20 m capped by hall / fixed 10 m | 5 / 10 m |
| Barricade width/lanes | 15-20 whole one-metre lanes, full hall span | 15 |
| Exit width | 3-12 m, fits hall | 3 m |
| Density | UI 0.5-8; Mode 1 area density / Mode 2 occupancy | 6.6 |
| Preferred passenger speeds | Both 0.20-1.65 m/s, minimum <= maximum | 0.85 / 1.55 |
| Mode 1 outcomes | Undetected 0-4%, Uncounted fixed 1%, Cleared remainder | 4 / 1 / 95% |
| Mode 2 outcomes | First-pass Green fixed 90%, second-pass Yellow 0-10%, Red remainder | 90 / 5 / 5% |
| Mode 2 per-attempt gate time | 3-20 s | 5 s |
| Acquisition radius | 4-10 m | 6 m |
| Purple visual confirmation | Mode 1 normal targets, 1-5 s | 2 s |
| Normal interception speed | Mode 1, 0.50-3.50 m/s | 1.55 |
| Purple initial clearance | Mode 1, 10-180 s | 10 s |
| Clearance fatigue | Add 0-100% of initial time per completed case | 25% |
| Catch-driven speed loss | 0-50% of initial speed per physical catch; 50% speed floor | 0% |
| Mode 2 Purple clearance | Fixed selected 10-180 s; no fatigue | 10 s |
| Robot clearance | Fixed independent 1-180 s | 10 s |
| Dasher holding clearance | 10-30 s | 10 s |
| Random seed | Editable integer and same/new-seed restart semantics | 42 |

Preserve persisted/non-slider agent/operator radii, queue-wall clearance, responder coordinates/types and aliases. Do not narrow valid loaded/API configurations to UI slider ranges. Default Open Funnel population is 330 in 30 rows/11 lanes.

Metrics: fixed-current-minute and run crossing/outcome counts; per-category counts/percentages; crossing versus outcome throughput; caught/escaped/unresolved and resolved-case catch rate; overall queue average/maximum/sample count; time-weighted all-responder utilisation; Purple average/lowest capacity/fatigue; manual wait average/maximum/completion samples and total verified; per-lane processed count/throughput/average/maximum queue time; gate states/counts; Red awaiting/assigned; responder identity/state/ROW, catches/cases, speed, clearance and robot progress. Preserve zero-sample/N/A formatting, double-count exclusions and mode-specific meaning.

Persistence: maintain format version 1, property casing/default rules, schema/enum forms, filenames/sanitization/path restrictions, sort/overwrite behaviour, error handling and invalid-post skipping. Preserve RoomHeight alias, numeric/legacy HumanoidRobot identities, missing-kind Purple default, missing robot-time default and missing-fatigue +2.5-second migration. Cross-load original and Unity files. Save setup, not a live runtime checkpoint.

Recording: 30 fps real time, MP4/mp4v then MJPEG AVI fallback, complete client/UI (not OS title bar), even dimensions, timestamp filename, resize to initial recording dimensions, duplication/catch-up policy and cleanup on failure. Finalize on explicit stop, reset/restart, setup/mode return, exit and window close. Simulation pause does not implicitly stop recording. Verify encoders in a built Windows player.

## Behaviour and conflict decisions

- Preserve fixed 60 Hz stepping, 1x-4x semantics, delta clamp/catch-up limit and update order. Rendering/recording must not change model state; do not substitute Time.timeScale or Unity physics.
- Funnel: orderly lanes, spacing, bounded acceleration and space-gated replenishment; no SFM. Released hall passengers: same urgency/congestion SFM, spatial hash, forces, caps, walls/exits and overlap correction.
- Preserve formation/queue-protecting collisions, responder corridors/yield/braking and Dasher yielding. Non-idle responders away from post have ROW, including robots through the shared implementation. Normal pursuit targets do not evade before cooperative stop.
- Preserve exclusive normal locks including visual confirmation, human-first reservation, autonomous-clearance exclusion and acquisition-radius boundaries. Radius gates acquisition, not continued pursuit.
- Purple Mode 1: confirmation -> pursuit -> 2 m cooperative stop -> 0.5 m catch -> 1.2 s hold -> post escort -> verification -> idle. Cleared passengers resume flow. Preserve additive timing fatigue, catch-based speed loss and capacity's 50% floor. Separate Dasher/escort/return speeds.
- Mode 2 uses sampled probabilities, not exact finite-run quotas. Full-duration attempts, automatic Yellow release, Red waiting, initial alarm dispatch outside radius and human 0.6 m catch/post escort remain. Normal movement/timing fatigue is disabled.
- Robots skip confirmation, use stop/catch/hold, clear on site with fixed timing, then check once within range from current position after Purple reservations. Otherwise return empty to original dock. Mode 2 gate reopening retains recorded Red. No robot Dasher response/catchers.
- Dasher: one funnel selection, crossing activation and quota exclusion; 2.5 m/s dash / 3.5 m/s Purple pursuit; first catch stationary, exactly two human catchers, extras released, 1 s group stop, holding escort, configured clearance/removal and return. Escape only while Dashing.
- Purple Dasher priority preempts normal work within range. Started verification completes remaining time autonomously; earlier Mode 1 Yellow resumes Undetected flow; interrupted Mode 2 Red stays stopped.
- Resolve stale KNOWN_DIFFERENCES ranges/fatigue and older Dasher speed/catcher/hold notes against code/tests. Current 60s is a fixed minute bucket, not a rolling window. Current layout supersedes older 150/400-pixel gap notes.
- Uncertain holding-point intent: code uses preferred (6.0, 0.65) m with wall clamping, comment/update says x=4 m, older notes say left-centre. The test follows constants. Preserve the implemented location; no location change is approved.
- The original test suite does not establish cross-runtime long-run determinism, full UI behaviour or native video encoding. Add meaningful coverage; do not present existing green tests as evidence of those capabilities.

## Small stages and verification gates

Each row is a separate local checkpoint and push-approval stop. Split further if a row becomes too large; never hide incomplete work behind a completion label.

| Stage | Deliverable | Required gate |
| --- | --- | --- |
| S0 | Branch, self-contained plan/status/instructions, cache/secret ignores, reference validation | Clean original restore/build; 270 existing cases executed; docs/diff/ignore checks; no Unity feature claims. |
| S1 | Verified editor/package/backend pins, root compile isolation, real Unity settings and minimal wired scene | Clean Unity import/compile and scene smoke; original build/tests still pass. Minimal scene is explicitly not feature parity. |
| S2 | Unity-compatible domain models/helpers, math/RNG adapters and fixture infrastructure | Relevant original cases mapped; Unity EditMode/helper/RNG fixtures pass; no unapproved sequence changes. |
| S3 | Core engine movement, queues, gates and accounting | Original suite plus Unity movement/gate/metric cases and reference state comparisons pass. |
| S4 | Purple/robot/Dasher parity verification and fixes | Ownership, pre-emption, fatigue, on-site/dock, both-mode and human-only catcher regressions pass. |
| S5 | Hall/funnel/gates/materials and orthographic camera | Projection/geometry/zoom tests and rendered reference views; state unaffected by rendering. |
| S6 | Entities/gait, overlays, picking and responder placement | PlayMode picking/preview/placement tests and visual checks; bounds/rings/identities retained. |
| S7 | Mode selection and every setup setting/control | Mode-specific UI, ranges/defaults/validation and configuration application tests pass. |
| S8 | Runtime lifecycle, dashboards and full results | PlayMode action/state/metrics tests; stopped-scene/results and restart invariants pass. |
| S9 | JSON persistence and Files UI | Cross-load/round-trip/legacy/invalid-file/post tests pass; original schema remains usable. |
| S10 | Full client-frame capture and runtime encoder | Windows player creates playable UI-inclusive MP4 and forced fallback AVI; dimensions/fps checked. |
| S11 | Recording lifecycle/resize/failure completion | Real Windows pause/stop/restart/setup/mode/close/error cases finalize correctly. |
| S12 | Reproducible CI and full Windows distribution | Clean-checkout original + Unity jobs pass; complete player launches independently of Editor. |
| S13 | Final parity, desktop/performance validation and run/build docs | All requirements checked; differences approved; remaining limitations accurately documented. |

S1 is split for interruption recovery: **S1a** authors the foundation/version requests and verifies static integrity/.NET isolation; **S1b** resolves packages with the licensed Editor, generates real URP/project settings and lockfile, compiles and runs the smoke checks. **Latest user direction permits source stages to proceed without a cloud runner while S1 remains incomplete.** Later source checkpoints may be implemented / Unity-unvalidated; never imply that the deferred gates passed. The diagnostic OnGUI label remains a temporary placeholder.

**S1-CI** prepared cloud-runner orchestration and strict evidence gates while S1b remains blocked. Cloud runner setup is now deferred; preserve the work without enabling/provisioning it. See `Documentation/CLOUD_CI.md` for historical investigation. Do not access a personal computer, buy a licence or treat local fixtures as Unity validation.

Split S2 into reviewable source checkpoints: **S2a** enums, operator timing/acquisition and quota helpers, .NET Standard 2.1 / C# 9 compilation, direct original comparisons and shared Unity fixtures; **S2b** configuration/domain models; **S2c** geometry/math/RNG fixtures and compatibility decisions. Scope may split further. All S2 Unity gates remain outstanding until actual Editor execution. See `Validation/README.md` for S2a's mapping and NaN-input uncertainty; preserve the original contract pending an explicit behavior decision.

## Validation and completion criteria

- Keep all 270 original cases; implement equivalent Unity coverage without weakening assertions. Use separate test-process/reference fixtures where matching namespaces would conflict. Cover camera/scaling/gait/path helpers as well as core logic.
- Golden scenarios compare IDs, seeded traits, lanes, positions/velocities, locks, state transitions, gates, timers and metrics across both modes, boundary settings, human/robot/mixed teams, minute rollovers, speed/lag cases, resets, dense crowds and Dasher interruptions.
- Proposed acceptance: matching discrete decisions/accounting; <=1 mm continuous-state difference at recorded checkpoints and timer events within one tick. These are proposed thresholds, not permission to mask changed decisions or silently accept behaviour differences. Investigate divergence and obtain approval for a necessary difference.
- Require fresh checkout import/build, valid scenes/assets without manual reconstruction, Windows player independent of Editor, and decodable full-UI recording including fallback/lifecycle tests.
- Proposed performance targets on an agreed Windows reference machine: 60 fps default 330-passenger case, >=30 fps agreed maximum-density cases at 1500x900; report recording overhead and sustained ten-minute errors/resource growth. Hardware and thresholds still require agreement.
- Original .NET CI: restore/build/test. Unity CI: import/compile, EditMode/PlayMode result XML, batch Windows build and logs/artifacts; actual graphics/input/native-video checks on Windows. Pin CI actions if used, verify exit codes and nonzero test counts, and keep licence values in secure settings only.
- A licensed exact editor, Windows build modules, package-network access and a Windows graphics/encoding validation machine are prerequisites for their respective gates. Never claim unperformed Unity/desktop/Windows checks. A blocker may leave a stage incomplete; document it and useful independent work.

## Change approval and publication

No simulation behaviour differences have been approved. Preserve implemented defaults while uncertainties remain documented. Ask before implementing a necessary difference. Commit each verified stage locally on `unity-migration`, update status, then stop for approval to push that particular checkpoint. Keep `main` untouched; no automatic pushes, merges or pull requests. A fresh remote-based task can resume only after the checkpoint has been pushed with approval.
