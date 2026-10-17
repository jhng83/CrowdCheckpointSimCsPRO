# Hall Width, Funnel Width and Barricade Lane Count (PRO14)

This version uses PRO13 as its baseline. The hall length remains 20–30 m.

| Setting | Range | Default |
| --- | --- | --- |
| Hall width | 15–20 m | 15 m |
| Open Funnel width | 5–20 m, capped by hall width | 5 m |
| Barricade lane count | 15–20 whole lanes | 15 lanes |

In **Hall / crowd**, select the hall width before selecting a wider Open Funnel. To use a 20 m funnel, select a 20 m hall width. Reducing the hall width clamps the funnel to fit inside it. This preserves the existing geometry guardrail.

In **Multi-Lane Barricade**, the **Main hall width / lane count** slider selects 15–20 lanes. Each lane remains exactly 1 m wide; hall and checkpoint widths equal the lane count in metres. The gates still operate independently, and the queues remain single-file with the existing occupancy and replacement rules.

The camera automatically frames the maximum 30 m hall length, 20 m width and separate fixed 10 m funnel length. Normal zoom and placement remain available.

The full crowd simulation engine is unchanged: orderly queue movement inside the funnel, SFM in the hall, collision/yield guards, right-of-way, Purple/robot target locks, continuous robot interception, fixed robot clearance, fatigue and Dasher rules all retain their existing implementation. A wider funnel naturally changes population at the selected density under the same area-based calculation; wider geometry can also change paths and outcomes.

## Saved configurations

Save/load/clone retain the new dimensions. Strict validation is retained. Before loading an older scenario, update its `HallWidth` (or legacy `RoomHeight`) to 15–20 and `FunnelWidth` to 5–20, no wider than the hall. Barricade configurations require both widths to match in whole metres. Missing hall width uses the new 15 m default; missing funnel width uses 5 m.

## Quick user check

1. Start Open Funnel setup. Check hall width defaults to 15 m and the slider ends at 20 m.
2. Check funnel width starts at 5 m. Set the hall width to 20 m, then set the funnel to 20 m.
3. Reduce the hall width to 15 m and check the funnel fits the new width. Confirm and run a scenario.
4. Return to mode selection, choose Multi-Lane Barricade, and try 15, 17 and 20 lanes. Check one gate and one single-file queue per lane.
5. At hall length 30 m, width 20 m and 100% zoom, check the complete scene and try placement near the far corner.
6. Save/reload a wide scenario and restart the same seed. Check dimensions and responder types/positions are retained.

All application/test source files compiled and all 270 regression cases passed using the .NET 8 compiler/runtime and actual xUnit methods/assertions through the harness described in `VALIDATION.md`. Native desktop visual verification remains pending. Run instructions are in `run_instructions.md`.
