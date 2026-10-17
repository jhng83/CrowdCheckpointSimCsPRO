# Dasher Incident

This build adds one optional user-triggered Dasher incident while preserving the normal queue, SFM,
Undetected intervention, escort, verification, fatigue and outcome logic.

## Selection
- Runtime only: left-click a visible passenger still inside the funnel.
- Only one passenger can be marked at a time.
- The selected passenger remains in the orderly queue and does not become a Dasher until crossing
  the checkpoint into the main hall.
- The marked/active Dasher is rendered cyan with a DASHER marker.

## Behaviour
- On checkpoint crossing, the selected passenger becomes a separate Dasher incident and is excluded
  from the normal Cleared/Undetected quota so the configured outcome percentages stay intact.
- Active Dasher running speed: 3.0 m/s toward the exit.
- Purple operator Dasher-pursuit speed: 3.5 m/s, giving a 0.5 m/s closing-speed advantage.
- Ordinary passengers yield to the Dasher using a priority corridor similar to operator right-of-way.
- All currently free Purple operators inside the configured acquisition radius may respond to the same
  Dasher simultaneously. Operators already busy with normal Undetected cases do not switch targets.
- Dasher catch distance: 0.5 m.
- With exactly one physical catcher, the Dasher and catcher continue toward the exit at 1.25 m/s while
  other free eligible Purple operators may still pursue.
- With two or more physical catchers, the catchers + Dasher stop for 1.0 s, then move as a group to the
  left-centre holding point at 1.10 m/s.
- At the holding point, the Dasher is held for 10.0 s, disappears, and the involved Purple operators
  return to their own stations.
- If the Dasher reaches the exit before two operators catch it, the Dasher escapes, disappears, and
  any responders return to post.
- Operators that were pursuing but had not physically caught the Dasher are released when the second
  catch occurs; only actual catchers form the escort group.

## Guardrails retained
The normal Undetected state machine, 2 m cooperative stop, 0.5 m normal catch, manual verification,
fatigue timing, operator health/work-capacity, configurable acquisition radius, orderly funnel queue,
main-room SFM, wall collision, runtime dashboard, and mouse-wheel view zoom remain in place.


## Dasher priority pre-emption update
The Dasher is the highest-priority Purple target while it remains in the acquisition phase. A Purple inside its configured acquisition radius may abandon Yellow pursuit, stop/catch, escort, verification, or return-to-post movement to respond. Yellow verification already in progress completes its remaining timer autonomously; pre-verification Yellow cases are released as Undetected. Purple has right-of-way whenever away from its own station. The two-catcher lock still limits the secured apprehension/holding escort group to exactly two Purple operators.
