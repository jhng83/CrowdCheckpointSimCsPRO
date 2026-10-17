# Multi-Lane Barricade Mode

This build provides a second checkpoint operating mode while retaining the original **Mode 1 - Open Funnel** behaviour unchanged.

## Mode selection

The user selects the checkpoint mode before scenario setup:

- **Mode 1 - Open Funnel**: existing dense orderly funnel queue, 95-99% Green / 0-4% Yellow / fixed 1% Red outcomes, Yellow response, fatigue, and Dasher behaviour.
- **Mode 2 - Multi-Lane Barricade**: fixed one-metre single-file lanes with independently timed gates and a two-attempt automated clearance workflow.

Returning to mode selection clears setup operator posts so posts from one layout are not silently carried into another mode.

## Mode 2 geometry

- Main hall width is restricted to whole metres from **15 m to 20 m**.
- Lane width is fixed at **1.0 m**.
- Lane/gate count is derived automatically from hall width: **15-20 lanes**.
- The funnel spans the full selected hall width.
- Translucent waist-height lane dividers sit on the one-metre lane boundaries.
- Each lane has its own visible barricade gate at the checkpoint threshold.

## Queue density

The existing 0.5-8.0 density control becomes a queue-occupancy control in Mode 2.

- Lower values create shorter, deliberately uneven queues between lanes.
- The maximum value of **8.0** fills every lane to the calculated gate-to-rear single-file capacity.
- Each lane remains single-file at all occupancies; density never creates side-by-side passengers inside a lane.
- Replacement travellers enter at the rear after a traveller leaves that lane, preserving continuous operation.

## Automated barricade clearance

A shared **Barricade clearance time** setting is available from **3-20 seconds** (default **5 seconds**). Every lane uses the same duration, but each gate operates independently.

Each ordinary traveller is assigned one Mode 2 checkpoint outcome using the configured random seed:

- **Green - 90% fixed**: clears the first automated attempt. After one clearance interval, the gate opens and the traveller enters the main hall as Green.
- **Yellow - 0-10% configurable**: fails the first attempt, immediately undergoes a second full clearance interval, then clears and enters the main hall as Yellow. Purple does not intercept Mode 2 Yellow travellers.
- **Red - derived as 10% minus Yellow**: fails both automated attempts. The traveller turns Red before the closed barricade and waits for Purple manual intervention.

Therefore the Mode 2 target percentages always satisfy:

`Green 90% + Yellow X% + Red (10-X)% = 100%`.

A Dasher selected while still waiting in a lane is excluded from this normal 90/Yellow/Red outcome allocation. The Dasher receives one normal gate-processing interval and activates only after crossing, preserving the existing Dasher incident logic.

## Mode 2 Red manual clearance

A Mode 2 Red traveller blocks its lane at the closed barricade until a Purple operator physically reaches and secures it. Because this is an explicit barricade failure, a free Purple operator is dispatched even if the Red traveller is outside the normal acquisition radius. Purple then escorts the traveller to its own post for **manual clearance**. The acquisition-radius setting still applies to Dasher response.

- Mode 2 manual clearance time is configurable from **10-30 seconds**.
- Mode 2 manual clearance has **no fatigue increment** and no fatigue-based timing change.
- After manual clearance completes, the Red traveller becomes Green and resumes normal SFM movement from the operator post toward the exit.
- The traveller does not return to the barricade for a third automated attempt.
- Mode 2 Yellow travellers are already successfully cleared and are not Purple targets.

Dasher response remains the highest-priority Purple task and otherwise follows the existing Dasher rules.

## Runtime metrics

The Mode 2 dashboard reports:

- First-attempt Green outcomes.
- Second-attempt Yellow outcomes.
- Manual-clearance-required Red outcomes.
- Red travellers awaiting Purple/manual clearance.
- Purple operators responding to Red.
- Completed manual clearances.
- Gates processing, open, or awaiting manual intervention.

Short-run observed percentages can vary around the configured probabilities because Mode 2 outcomes are sampled, while the configured random seed makes a repeated scenario deterministic.

## Runtime fast-forward

Both checkpoint modes provide **1x, 2x, 3x, and 4x** runtime speed controls. Fast-forward advances simulation time using additional fixed 60 Hz simulation steps; it does not individually multiply pedestrian velocities or timer values. Pausing remains a true pause, and changing the selected multiplier while paused does not resume the run.
