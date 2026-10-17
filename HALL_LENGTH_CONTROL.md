# Main Hall Length: 20–30 Metres

Baseline: the attached `CrowdCheckpointSimCsPRO(6).zip`.

Before confirming a scenario, open **Hall / crowd → Main hall length**.

- Minimum: **20.0 m**.
- Maximum: **30.0 m**.
- Default: **20.0 m**.
- Applies to both Open Funnel and Multi-Lane Barricade modes.

Hall length is the distance from the exit wall (x = 0) to the checkpoint/funnel interface. Increasing it moves the checkpoint and its queue farther from the exit. It does not increase the separate 10 m funnel length or change its target population. Hall width is adjustable from 15 to 20 m, and barricade lane counts are 15–20 using the existing one-metre lane rule. Open Funnel width is selectable from 5 to 20 m, capped by hall width. See `WIDTH_AND_LANES_UPDATE.md`.

The UI slider, input clamping, configuration default/validation and camera already derive from the shared minimum/maximum constants. Only those two constants change executable behaviour. Camera framing therefore accommodates the maximum 30 m hall plus the fixed 10 m funnel using its existing scale/zoom logic.

The complete simulation engine, SFM, orderly queue movement, right-of-way, collision guards, target allocation, Purple/robot/Dasher behaviour, speeds, fatigue, timing and detection rules are unchanged. A longer travel distance can naturally change travel times and interception opportunities without changing those rules.

## Saved configurations

Save/load and clone retain values within 20–30 m. Existing saved JSON files explicitly using a shorter hall must have their `HallLength` changed to a value in this range before loading; the existing strict validation has been retained. Files without a hall length use the new 20 m default.

## Quick check on your PC

1. Run the project and select either checkpoint mode.
2. Open **Hall / crowd** and check that Main hall length starts at 20 m and stops at 30 m.
3. Try 20, 25 and 30 m. Check that the checkpoint/funnel moves while the separate funnel length stays fixed.
4. At 30 m, reset zoom to 100% and check the complete scene; try placing an operator near the checkpoint end of the hall.
5. Save and reload a 25 m or 30 m scenario, confirm it, and run it.

All application/test sources compiled with the .NET 8 compiler, and all 246 regression cases passed. New tests cover both modes at 20, 25 and 30 m, unchanged funnel population, far-end post placement, checkpoint flow, camera bounds, mouse unprojection and save/load/clone. See `VALIDATION.md` for execution details and remaining native desktop verification.

This archive contains updated source. Run `dotnet run` or rebuild/publish it to generate an executable containing the changes.
