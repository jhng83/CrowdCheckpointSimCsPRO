# Project guidance

This is a C#/.NET 8 OpenGL checkpoint crowd simulator.

Build:
dotnet restore
dotnet build

Test:
dotnet test ./tests/CrowdCheckpointSim.Tests/CrowdCheckpointSim.Tests.csproj

Run:
dotnet run

Preserve these rules unless explicitly asked to change them:
- Orderly queue movement inside the funnel; no SFM inside the funnel.
- SFM movement in the passenger hall.
- Existing collision, right-of-way and exclusive target-lock rules.
- Purple operators handle Dashers; wheeled robots ignore Dashers.
- Robots clear passengers on site using a fixed timer.
- After clearance, robots intercept nearby unlocked targets or return to their original docks.
- Preserve existing controls, settings, recording and results.

Do not commit bin, obj, publish or recordings.
Do not claim desktop visual verification unless actually performed.
