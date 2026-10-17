# Short User Guide

## 1. Scenario setup

Choose a mode, then use the setup tabs. Operator placement and scenario confirmation remain below the tabs.

| Tab | Settings |
| --- | --- |
| Hall / crowd | Hall, funnel and exit geometry; density; minimum/maximum preferred passenger speed |
| Checkpoint | Detection percentages or automated barricade timing and outcomes |
| Operators | Acquisition radius, confirmation delay, normal interception speed, Purple clearance/fatigue, fixed robot on-site clearance |
| Dasher | Existing holding-point clearance duration; fixed Dasher and Purple pursuit speeds |
| Files | Random seed and configuration save/load |

- Adjust **Main hall length (20-30 m)**, **Main hall width (15-20 m)**, **Funnel width (5-20 m, capped by hall width)**, **Exit width (3-12 m, capped by hall width)**, **Target density**, and **Random seed** in the left panel.
- **Main hall length** changes the yellow-circled room distance from the exit wall to the checkpoint. It does not change the funnel population by itself.
- Applied population remains calculated from `fixed 10 m funnel length x funnel width x density`.
- The 2.5D camera reserves space for the maximum 30 m hall length and 20 m hall width plus the fixed 10 m funnel, so resizing the hall does not crop the simulation.
- Use **Save Current Config** to store the current setup values and operator-post locations in the app's `configs` folder. Choose a saved configuration from the list and use **Load Selected Config** to restore it.
- Under **Responder Placement**, choose **Place Purple operator** or **Place humanoid robot**. Hover over the hall for a matching ghost preview.
- Left-click to place the selected type; right-click removes the nearest responder post of either type. Use **Finish placement** to end placement.
- **Clear operators** removes Purple posts; **Clear robots** removes robot posts.
- Place at least one Purple operator or robot before **Confirm Scenario** becomes available. Robot-only setups are supported.
- Use the **Zoom** slider in the View section or the mouse wheel over the simulation.
- Purple and teal rings show the configured acquisition radius for humans and robots respectively.

## 2. Runtime

After confirmation, pedestrians advance in orderly queue lanes while inside the funnel. Once fully across the checkpoint they switch to emergent SFM movement through the main room.

After confirmation:

- Use the compact permanent top strip to monitor **Mode**, **State**, **Elapsed**, **Speed**, and **Seed**.
- Use the permanent bottom bar for Pause, Stop, restart, new-seed, and mode-selection controls.
- **Start** sits in the left sidebar below the simulation-speed controls and above **Full UI Recording**; while paused, the same control is labelled **Resume**.
- **Pause** freezes simulation time while preserving the current state.
- **Stop** freezes the final state and opens the run-results screen.
- **Restart same seed** resets crowd, metrics, quotas, fatigue, and time, then starts an immediately comparable repeat run.
- **Run with new seed** performs the same reset, selects a different seed, and starts a new sample immediately.
- **Return to mode selection** clears the completed run and operator posts, then returns to the opening mode choice.
- **Back to Setup** unlocks setup controls again.
- **Exit Application** closes the window.

## 3. Live outcomes and final results

In Open Funnel mode, the bold **UNDETECTED OUTCOMES** card is arranged as:

- **CURRENT 60s** - Caught, Escaped, and Catch rate for the current fixed 60-second simulation interval. The counters reset at each minute boundary.
- **ACCUMULATED** - Caught, Escaped, and Catch rate for the full run.

Catch rate is `Caught / (Caught + Escaped)`. Each traveller is resolved once, so a person already caught before an operator is pre-empted cannot later inflate both counters. The rate displays **N/A** until at least one Undetected case has been resolved.

Mode 2 reports Undetected catch rate as **N/A** because its Yellow travellers clear the second automated attempt and are not Purple intervention targets.

Stopping a run opens a scrollable result screen containing:

- throughput and completed traveller count;
- average and maximum queue time, measured from queue entry/spawn until checkpoint processing completes;
- Undetected caught, escaped, unresolved, and resolved catch rate;
- operator utilisation, calculated from active operator-seconds divided by available operator-seconds;
- final average/lowest operator capacity and fatigue used in Open Funnel mode;
- average and maximum completed manual-clearance waiting time;
- Mode 2 per-lane processed count, throughput, and average/maximum queue time.

Manual-clearance waiting time starts when a traveller becomes eligible for Purple intervention and ends when clearance completes. Escaped and still-pending cases are not included in its average or maximum.

## 4. Visual meaning

- Grey/blue pedestrians: have not yet crossed the checkpoint.
- Cleared: detected and counted, or an Undetected person successfully verified by an operator.
- Undetected: counted but not detected; operators may intervene.

The Undetected target is adjustable from 0% to 4%; Uncounted/Red is fixed at 1%; Cleared is automatically the remainder from 99% down to 95%.
- Purple humanoids: operators.
- Silver wheeled units with teal sensor masts: robots.
- Blue floor markers: operator posts.
- Teal floor markers: robot posts.
- Bar above an operator: 100% initially, then -10 percentage points after each completed enrolment, with a 50% floor.

## 5. Operator sequence

A free operator is assigned to an eligible Undetected pedestrian. The pedestrian continues normally until the operator reaches approximately 2.0 m, then cooperatively stops. A catch is recorded only at approximately 0.5 m. The pair pauses for 1.2 seconds, the operator escorts the person to the post, verification occurs, and the person becomes Cleared and resumes toward the exit.

Wheeled robots use their placed positions as docks. Purple operators establish locks first, including during visual confirmation. Robots immediately pursue eligible unreserved passengers, retain the acquisition-radius and right-of-way guards, and ignore Dashers. After the securing hold, robots clear passengers on the spot using the independent fixed Robot manual clearance time (1–180 seconds), release them, then immediately intercept another nearby unreserved passenger, or return empty to their original docks if none is available. Robot clearance time has no fatigue progression. See WHEELED_ROBOT_UPDATE.md.

## Crowd yielding around Purple operators

A Purple operator has right-of-way whenever it is away from its own station. This includes normal Undetected pursuit/escort, Dasher response, and return-to-post movement. Nearby ordinary pedestrians slow, sidestep, or briefly stop when needed so the Purple route remains clear. During an escort, the Purple/target pair uses the wider priority formation.

## Operator detection/acquisition range

Before confirmation, set **Operator acquisition radius** from 4.0 m to 10.0 m (default 6.0 m). A free purple operator will not begin pursuing an **Undetected** pedestrian while that pedestrian is outside the selected radius. When an Undetected pedestrian enters the selected radius and that operator is free, the operator may activate and pursue it. Once pursuit has started, the target is not dropped merely for moving back outside the selected radius; the normal 2.0 m cooperative-stop and 0.5 m catch rules continue to apply.

In the **Operators** tab, set **Initial manual clearance time** from 10 to 180 seconds. **Clearance increase per completed case** adds a percentage of the initial duration each time; 0% disables the increase. For example, 30 seconds with 25% becomes 30.0, 37.5, 45.0, 52.5 seconds. The 3-minute limit applies to the initial setting; accumulated timing fatigue remains uncapped.

**Undetected interception speed** is adjustable from 0.50 to 3.50 m/s. **Speed decrease after each interception** subtracts 0-50% of the initial speed after each physical normal catch. It stops at exactly 50% of the initial speed. For example, 2.00 m/s with 10% loss progresses as 2.00, 1.80, 1.60, 1.40, 1.20, 1.00 m/s, then stays at 1.00. A decrement that crosses halfway is clamped to that floor. This affects normal pursuit only; escort, return and Dasher response retain their existing speeds.

The **Hall / crowd** tab sets passenger preferred-speed bounds within 0.20-1.65 m/s. Defaults remain 0.85-1.55 m/s. These are free-flow preferences; the existing queue and SFM can slow or stop passengers. Minimum cannot exceed maximum. Dasher speed remains 2.50 m/s and Purple Dasher pursuit remains 3.50 m/s.

## Dasher incident
During setup, **Dasher clearance time** can be set from 10 to 30 seconds (default 10 s); this is the time a secured Cyan/Dasher remains at the holding point before being removed. During runtime, click **Select Dasher** in the Dasher Incident section. Selection mode is explicit: eligible passengers still inside the funnel glow cyan on hover, clicking one marks it as the Dasher, and **Esc** or **Cancel Dasher Selection** exits the mode. Clicking a passenger who has already entered the main hall explains why that passenger is not eligible. Only one Dasher can be marked/active at a time. The marked passenger stays in the orderly queue until crossing the checkpoint, then becomes a cyan Dasher and runs toward the exit. Free Purple operators within the configured acquisition radius may pursue the same Dasher simultaneously. See `DASHER_INCIDENT.md` for the detailed catch/escort sequence.


## Dasher priority pre-emption update
Dasher response has priority over normal Purple work. If the active Dasher is inside an operator's configured acquisition radius, Purple may drop Yellow pursuit, catch/stop, escort, verification, or return-to-post activity and pursue the Dasher. A Yellow whose verification already started completes the remaining timer autonomously and becomes Cleared; a Yellow interrupted before verification is released as Undetected and may be acquired again later. Purple keeps right-of-way whenever away from its station.

## Mode 2 - Multi-Lane Barricade checkpoint

Select this mode on the opening mode-selection screen before configuring the scenario. Hall width is selected in whole metres from 15 m to 20 m. Because each lane is fixed at exactly 1.0 m, hall width directly determines the number of checkpoint lanes and gates (15-20).

The Mode 2 density control changes queue occupancy while keeping every lane single-file. Lower values create different queue lengths from lane to lane. At the maximum value of 8.00, every lane is filled from the gate toward the rear of the 10 m queue area using close orderly spacing.

Each lane has a translucent waist-height divider and an independent gate. **Barricade clearance time** is adjustable from **3 to 20 seconds** (default 5 s). A normal traveller waits one complete interval for each automated attempt. The gate opens only for a successfully cleared traveller and closes again after that traveller enters the main hall.

Mode 2 uses a separate outcome model: **Green is fixed at 90%**, **Yellow is adjustable from 0% to 10%**, and **Red is automatically 10% minus Yellow**. Green clears on the first attempt. Yellow uses two complete clearance intervals, clears on the second attempt, passes the gate as Yellow, and is not targeted by Purple. Red fails both attempts, turns Red before the barricade, and waits at the closed gate for Purple.

Mode 2 **manual clearance time** is adjustable from **10 to 180 seconds (3 minutes)**. A Red barricade failure automatically requests an available Purple operator even when the Red traveller is outside the normal acquisition radius; the radius setting still applies to Dasher response. Purple secures the Red traveller, escorts it to the Purple post, performs the configured fixed manual-clearance time, and then the traveller becomes Green and resumes toward the exit. Mode 2 does not apply the Mode 1 fatigue timing increase or fatigue-capacity reduction.

A user-selected Cyan/Dasher remains outside the normal Green/Yellow/Red percentages and retains the existing Dasher pursuit/catch/holding behaviour.

## Runtime simulation speed

During a confirmed run, choose **1x, 2x, 3x, or 4x** under Simulation Speed. This fast-forwards simulation time as a whole, including pedestrian movement and simulation timers, while retaining the fixed-step simulation logic. Selecting a speed while paused does not resume the simulation.

## Display scaling

The application opens maximized and scales fonts, panels, controls, dashboard height, and spacing uniformly from its 1500 x 900 reference layout. On a screen with a different aspect ratio, the limiting dimension controls the scale so the interface keeps the same proportions without horizontal or vertical stretching.
