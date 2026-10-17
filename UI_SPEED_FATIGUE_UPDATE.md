# Professional UI, Movement Settings, and Fatigue Update

Baseline: the exact uploaded CrowdCheckpointSimCsPRO(6).zip, recovered through its matching file identifier.

## Setup workflow

The setup sidebar has five tabs: Hall / crowd, Checkpoint, Operators, Dasher, and Files. Sliders use a label above the full-width control, with units in the value. Longer settings scroll within their own area; the tab strip stays accessible, and each tab retains its own scroll position. Placement and readiness remain below it. Regular and bold system fonts improve readability, with a fallback when those fonts are unavailable. The setup preview reserves only a short instruction strip above the canvas, reclaiming the old empty 400-pixel area for a larger scene and easier operator placement.

The passenger hall floor is warm off-white (#F3F0E8). Its labels use dark text. Funnel flooring, physical walls, collision geometry, and colour meanings are retained.

## New and expanded settings

| Setting | Range | Default / scope |
| --- | --- | --- |
| Minimum passenger preferred speed | 0.20-1.65 m/s, no greater than maximum | 0.85 m/s; both modes |
| Maximum passenger preferred speed | Minimum-1.65 m/s | 1.55 m/s; both modes |
| Undetected interception speed | 0.50-3.50 m/s | 1.55 m/s; Open Funnel normal pursuit only |
| Initial manual clearance | 10-180 s | 10 s; Open Funnel |
| Red manual clearance | 10-180 s | 10 s; Multi-Lane Barricade, fixed duration |
| Clearance increase per completed case | 0-100% of initial time | 25%; Open Funnel |
| Speed decrease per physical normal interception | 0-50% of initial speed | 0% preserves baseline speed; Open Funnel |

Passenger bounds rescale the original three-band random distribution without additional RNG draws. They describe preferred free-flow motion; queues, yielding, congestion, or a cooperative stop can reduce actual speed to zero. The existing 1.65 m/s SFM velocity cap, acceleration limits, force parameters, and overlap correction are retained.

Manual duration = initial duration + completed cases × initial duration × increase percent / 100.

Normal pursuit speed = initial speed × max(0.5, 1 - physical catches × loss percent / 100).

Each operator tracks its own cases and catches. Pursuit loss starts after a physical catch and stops at exactly half the initial speed, including when a decrement would overshoot. Failed pursuits do not count. A recatch after pre-emption is another physical interception. Timing fatigue starts after a completed clearance and continues beyond the visible capacity-bar floor, as in the baseline. The 180-second bound applies to the initial manual setting; subsequent fatigue can make a case take longer.

The existing capacity bar still loses ten percentage points per completed manual clearance and stops at 50%. Timing and pursuit loss are independent settings; setting either to zero disables that effect.

## Preserved behaviour

- Orderly queue in the funnel; SFM after the checkpoint.
- Cooperative stop, catch, hold, escort, verification, right-of-way and pre-emption rules.
- Dasher at 2.50 m/s; Purple Dasher pursuit at 3.50 m/s; all existing Dasher states and timing options.
- Escort and return speeds.
- Mode 2 automatic attempts, quotas, direct Red dispatch and disabled fatigue.
- Save/load, seeds, operator placement, zoom, recording, run controls, live dashboard and final results.
- All baseline source files, existing tests, and pinned package versions.

Saving includes every new value. Older JSON files without the new fields keep original passenger speeds, normal pursuit speed and disabled pursuit loss. Their missing fatigue percentage is derived from the old 2.5-second step, preserving previous clearance durations even when initial time was not ten seconds.

## User test checklist

1. Start the app maximized. Check the setup tabs, readable values, and off-white hall.
2. Open Hall / crowd. Set both preferred speeds to 1.20 m/s. Confirm the queue remains orderly and room passengers still steer, yield and stop when needed.
3. Open Operators in Open Funnel. Set interception to 2.00 m/s and speed loss to 10%. Watch the operator panel after catches: 1.80, 1.60, 1.40, 1.20, then 1.00 m/s, with no further decrease.
4. Set initial clearance to 30 s and increase to 25%. Completed cases should lead to 30, 37.5, 45, 52.5 s for successive clearances. Set increase to zero and confirm the duration stays fixed.
5. Set initial clearance to 180 s. Confirm the slider accepts it and manual clearance waits the configured duration.
6. Select a Dasher after changing normal speeds and fatigue. Its original pursuit, two-catcher response and holding behaviour should remain intact.
7. Restart with the same seed. Catch counts, timing fatigue and speed loss should reset while the configured values remain.
8. Save a configuration in Files, return to setup, change the settings, then load it. Confirm all values and posts are restored. Also try an older saved configuration.
9. In Multi-Lane mode, set Red manual clearance to 180 s. Confirm fatigue remains disabled and automated lane settings still work.
10. Check recording, pause/resume, stop/results, restart, mode selection, zoom and operator removal.

## Validation limits

New xUnit cases cover seeded passenger bounds, catch-driven speed loss, the 50% floor, reset, percentage timing, legacy file migration, non-finite inputs, Mode 2 isolation and Dasher trajectory isolation. Existing persistence tests now round-trip all new fields, and manual-range tests include 180 seconds.

The execution environment has no .NET SDK, and its network could not fetch one. Compilation, xUnit execution and a native OpenGL visual run could not be performed here. Static validation and baseline source comparison are reported in VALIDATION.md. Run the commands in run_instructions.md on the target Windows PC before relying on the updated executable.
