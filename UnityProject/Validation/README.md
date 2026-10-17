# Validation without an installed Unity Editor

These SDK projects compile the **same** source files imported by the Unity project; they do not supply Unity API stubs. `Domain/Domain.csproj` targets .NET Standard 2.1 / C# 9, disables implicit imports and treats warnings as errors. It has no package dependencies. This detects unsupported syntax/APIs in the portable helpers, but does not establish Unity assembly resolution or runtime behavior.

`DomainParity.Tests` executes that assembly on .NET 8 alongside the unchanged original application, isolated with an assembly alias. It reuses the reference suite's pinned xUnit/Test SDK packages. It also links the framework-independent fixtures used by the authored Unity EditMode tests. Passing here does **not** mean those Unity tests ran.

From the repository root, with .NET 8 activated:

```sh
dotnet restore UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --disable-parallel
dotnet test UnityProject/Validation/DomainParity.Tests/DomainParity.Tests.csproj --no-restore --results-directory UnityProject/Artifacts/DomainParity --logger 'trx;LogFileName=domain-parity.trx'
```

Current scope: 30 executed .NET cases, including five shared expectations. The Unity side has five corresponding NUnit methods; all are unrun in Unity.

| Original coverage / implementation | Portable checks |
| --- | --- |
| `OperatorTimingTests`, `MovementAndFatigueSettingsTests` / `OperatorTiming` | Legacy +2.5 s and additive percentage fatigue, catch-speed/capacity floors, negative clamps; bitwise reference comparison over six parameter combinations; all timing constants compared. |
| `OperatorAcquisitionTests` / `OperatorAcquisition` | Inclusive radius, 3-4-5 boundary, zero radius, invalid radii/coordinates; nine direct reference cases. |
| `DetectionQuotaTrackerTests` / `DetectionQuotaTracker` | 95/4/1 and 99/0/1 totals, invalid ranges, reset/configuration debt; all 1,000 decisions and counters compared at four fractions, with mid-sequence reset/reconfiguration. |
| `SimulationEnums` | Every migrated enum name/value compared to the original assembly. |

Only namespace/import compatibility and the equivalent explicit `IsNaN`/`IsInfinity` radius guard were adapted. Mathematical operation order is retained. No full simulation, RNG adapter, responder state machine, Unity UI or recording is implemented by this checkpoint.

Uncertain intended behavior: `DetectionQuotaTracker.Configure` currently accepts NaN fractions because its range comparisons do not reject them. A reference comparison preserves this acceptance; no new validation is silently imposed. Ordinary finite defaults/ranges remain covered. Decide separately whether the original and port should reject non-finite values before changing that contract.

Next source checkpoint: port configuration and domain models with compatible property/value semantics and reference fixtures; then geometry/math/RNG infrastructure. Keep S1 and S2 incomplete until their actual Unity gates pass. Long-run cross-runtime determinism remains unverified. Outputs under `bin`, `obj` and `Artifacts` are ignored and must remain outside commits.
