# Hybrid Queue + SFM Change

This revision changes the movement architecture from SFM-everywhere to the intended hybrid behaviour.

## Funnel

- Orderly front-to-back queue lanes.
- Lane count adapts to the configured population/funnel width using the Python reference approach.
- Safe longitudinal body spacing.
- Bounded queue acceleration/deceleration.
- Small deterministic per-person timing and lateral variation.
- Lane-specific rear replenishment only when physical space is available.
- No pedestrian SFM force integration while a pedestrian remains in `PedestrianPhase.Funnel`.

## Checkpoint transition

When the complete body crosses the checkpoint threshold:

1. `PedestrianPhase.Funnel` changes to `PedestrianPhase.Room`.
2. The configured Green/Yellow/Red checkpoint outcome is assigned; Yellow is 0-4%, Red is fixed at 1%, and Green is the remainder (99-95%).
3. A replacement is queued for the same funnel lane.
4. On the next simulation step the released pedestrian is governed by the main-room SFM.

## Main room

- Urgency-influenced desired motion.
- Pedestrian-pedestrian local repulsion.
- Wall repulsion.
- Operator right-of-way/yielding.
- Existing Yellow intervention, escort and verification state machine retained.

The earlier ImGui `BeginChild` API fix is also retained in this revision.
