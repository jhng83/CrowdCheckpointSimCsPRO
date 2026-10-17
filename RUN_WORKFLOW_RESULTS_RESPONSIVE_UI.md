# Run Workflow, Results, and Responsive UI Update

## Permanent status and controls

The compact top status strip is visible in mode selection, setup, runtime, stopped-state review, and the results screen. It reports the selected checkpoint mode, workflow state, elapsed simulation time, active speed multiplier, and random seed. **Start/Resume** is in the runtime sidebar below the simulation-speed controls and above the Full UI Recording section. The other five primary controls remain available in a separate permanent bottom bar.

The control semantics are:

- **Start / Resume** begins a ready run or continues a paused run.
- **Pause** freezes simulation advancement without changing the current state.
- **Stop** freezes the final scene, stops active recording, and opens final results.
- **Restart same seed** resets the model and metrics, retaining the configuration and seed, and starts immediately.
- **Run with new seed** retains the configuration, chooses a different seed, resets the model and metrics, and starts immediately.
- **Return to mode selection** stops recording, clears the run and operator posts, and returns to the opening mode choice.

## Results definitions

- **Throughput** is completed checkpoint crossings per simulation minute in Open Funnel mode and resolved checkpoint outcomes per simulation minute in Multi-Lane Barricade mode.
- **Queue time** runs from pedestrian spawn/queue entry until checkpoint processing completes. Dasher incidents remain outside normal checkpoint statistics.
- **Undetected catch rate** is `caught / (caught + escaped)` in Open Funnel mode. Each traveller resolves only once; pending cases do not change the rate and are displayed separately. Mode 2 reports this metric as not applicable because second-attempt Yellow travellers are not Purple intervention targets.
- **Operator utilisation** is time weighted: non-idle operator-seconds divided by total available operator-seconds.
- **Fatigue** is reported as final average capacity remaining, lowest capacity remaining, and average capacity used. Mode 2 correctly reports fatigue as disabled.
- **Manual-clearance waiting time** runs from eligibility for Purple intervention until successful clearance. Escaped and incomplete cases are excluded.
- **Lane performance** reports resolved outcomes, outcomes per minute, and average/maximum queue time for each Mode 2 lane.

## Responsive layout

The 1500 x 900 design is the reference coordinate system. Fonts, style spacing, sidebars, dashboard cards, controls, and reserved viewport areas scale uniformly using the smaller of the width and height ratios. This retains the reference proportions on larger or differently shaped displays while allowing extra space to remain available to the simulation viewport. The native window opens maximized, and its OpenGL viewport is synchronized to the actual framebuffer before the first frame is cleared and drawn. This prevents the requested 1500 x 900 startup viewport from remaining stuck inside a larger maximized client area.

The live dashboard cards use a compact 230-pixel reference height instead of 300 pixels. Their data remains intact, including the scrollable operator-status list, while the released space expands the simulation area.

## Outcome wording

Open Funnel mode now presents a bold **UNDETECTED OUTCOMES** card with **CURRENT 60s** and **ACCUMULATED** groups. Each group contains Caught, Escaped, and Catch rate.

Multi-Lane Barricade setup now explains the implemented outcome model precisely: 90% of all travellers clear on attempt one; the remaining 10% receive attempt two; the selected Yellow percentage clears on attempt two; and the balance fails both attempts and becomes Red.
