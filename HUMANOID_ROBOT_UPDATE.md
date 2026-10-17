# Historical PRO10 Behaviour

This document describes the previous humanoid implementation. It is superseded by `WHEELED_ROBOT_UPDATE.md`: robots are now wheeled, ignore Dashers, clear on site with an independent fixed timer, and return empty to their docks.

# Humanoid Robot Responders

Baseline: CrowdCheckpointSimCsPRO9_ProfessionalUI_SpeedFatigue.zip.

## Placement

1. Choose a checkpoint mode and configure the scenario.
2. Under Responder Placement, choose Place Purple operator or Place humanoid robot.
3. Hover over the hall for a preview, then left-click to place. Both types use the same boundary and post-spacing checks.
4. Right-click removes the nearest post of either type. Clear operators and Clear robots act separately.
5. Finish placement, confirm, then Start. A robot alone satisfies the responder readiness requirement.

Robots have silver bodies, square head housings and teal sensor visors. Teal posts/rings distinguish them from Purple operators. The status list labels each robot and operator with a unique ID.

## Target ownership and response

Both types reuse the existing responder state machine.

Normal assignment runs in two passes. Available Purple operators reserve targets first. Robots then choose from the remaining candidates, while all existing human and robot locks are excluded. A Purple visual-confirmation lock counts immediately, before the Purple starts moving. Several robots cannot reserve the same normal passenger.

A robot starts in Pursuing directly and does not wait for visual search. In Open Funnel mode, it still obeys the configured acquisition radius for new targets. Once pursuing, it retains the target outside that radius under the same rules as Purple.

Robots use the existing 2.0 m cooperative stop, 0.5 m physical catch, 1.2 s hold, escort back to their own posts, configured manual-clearance timer, and Green release. They keep the same right-of-way and passenger-yield interactions. Interrupted autonomous verification remains excluded from acquisition.

## Shared settings and guardrails

- Purple and robots share normal interception speed, manual-clearance duration, configured timing fatigue and pursuit-speed fatigue. Each responder accumulates its own workload/catches.
- Escort and return-to-post speeds remain unchanged.
- Dasher priority, pursuit speed, two-responder catch/hold/escort and clearance rules are retained. Robots may join the existing Dasher response just like Purple operators; the normal-target ownership restriction does not prohibit the existing shared Dasher team.
- In Multi-Lane mode, both types respond to Red failures using the existing alarm dispatch and fixed manual duration. Second-pass Yellow remains outside normal intervention, and fatigue stays disabled.
- Team utilisation, capacity and manual-clearance totals include both types. Individual status rows identify the type.
- The orderly funnel queue, room SFM, boundaries, force constants, collision separation, normal outcome accounting and existing controls are retained.

## Saving and restart

Saved posts now carry Kind = PurpleOperator or HumanoidRobot. Coordinates and the format version remain compatible with previous configuration files. A missing Kind loads as a Purple operator. The existing coordinate-based Save API is retained, and the application uses the typed overload for mixed teams.

Restart resets each responder's state and accumulated workload while retaining its post/type. Hall resizing keeps valid posts of both types and removes posts outside the new boundary. Returning to mode selection clears both types.

## User test checklist

1. Place one Purple operator and one robot. Check different previews, bodies, markers, status names and counts.
2. Set visual confirmation to 5 seconds. Check that an eligible robot begins moving immediately while Purple retains its confirmation phase.
3. Let Purple lock one Undetected passenger. Check that a nearby robot chooses another unlocked passenger, or waits if none exists.
4. Place several robots. Check that their normal target lines do not converge on one reserved passenger.
5. Watch a robot complete stop, catch, hold, escort, manual clearance and release. Check the clearance timer and fatigue settings match the configured values.
6. Watch ordinary passengers yield around a robot away from its post, including escort and return.
7. Save and reload a mixed team. Restart the same seed. Check responder types/posts are retained and workload resets.
8. Clear only robots, then only operators. Check that the other type remains. Test right-click removal near each type.
9. Try a robot-only scenario and a mixed Dasher response. Check the existing two-responder Dasher rules.
10. In Multi-Lane mode, check Red dispatch/clearance and continued exclusion of second-pass Yellow.

## Validation status

Static structure checks and comparison of 23 protected core method bodies passed. Regression tests were added for the behaviours above. The execution environment has no .NET SDK, so compilation, xUnit execution and OpenGL visual verification could not be run here. Run the commands in run_instructions.md on the target PC.
