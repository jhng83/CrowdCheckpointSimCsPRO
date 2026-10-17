# Wheeled Robots: On-site Clearance

The wheeled implementation builds on PRO10; PRO12 adds consecutive nearby interceptions after clearance. Existing robot posts now represent wheeled robots, with the same docking coordinates and responder IDs.

## Setup

1. Choose a checkpoint mode and open **Operators**.
2. In **Wheeled robot: on-site clearance**, set **Robot manual clearance time** from **1 to 180 seconds**. The default is **10 seconds**.
3. Choose **Place wheeled robot**, then click valid positions in the hall. Each placed position is that robot's original dock.
4. Place Purple operators as needed, finish placement, confirm and Start.

The robot clearance timer is separate from Purple manual clearance. Every robot case takes the selected duration; completed cases never increase or decrease that duration. There is no robot clearance-fatigue setting. The existing shared normal interception speed and speed-loss controls are retained in Open Funnel mode. Purple clearance fatigue, visual search and Dasher controls remain available.

## Robot sequence

1. Available Purple operators reserve targets first. A robot immediately pursues an eligible remaining target without visual confirmation. Existing human and robot locks, acquisition guards and autonomous-clearance exclusions still apply.
2. The robot uses the existing cooperative stop, catch distance and 1.2-second securing hold.
3. The passenger and robot remain at the interception point for the configured manual-clearance duration. There is no escort to the dock.
4. The passenger becomes Cleared and resumes normal movement. Clearance and catch statistics use the existing accounting paths.
5. Before starting a return trip, the robot checks for another eligible, unlocked passenger within the configured acquisition radius measured from its current position. Purple operators reserve first. If a target is available, the robot immediately intercepts and clears that passenger on site, repeating this check after every clearance.
6. If no eligible nearby target is available, it returns empty to its original dock using existing return movement and right-of-way. The dock never moves. After the check chooses a return trip, ordinary return behaviour continues until docking.

Robots ignore Dashers in both idle assignment and busy-task pre-emption. They cannot chase, catch, hold or join the Dasher team. Dasher response still requires two human Purple catchers and retains its existing speeds, holding and clearance rules. A robot-only scenario therefore has no Dasher catchers.

In Multi-Lane Barricade mode, robots retain Red-alarm response. They clear Red at the closed gate, then open that same gate so the passenger crosses normally. The original recorded Red outcome is retained; the passenger is not reclassified as an automated Green result. Second-pass Yellow is still excluded from manual intervention. After a Mode 2 clearance, the same nearby-target check can acquire another unreserved Red alarm within acquisition range. From the dock, the original Red-alarm dispatch rules remain in force. Human Mode 2 escort behaviour is unchanged.

## Display and compatibility

Robots have four visible wheels, a silver chassis and a teal sensor mast. Placement previews, docking marks and target lines use teal. Status rows show Docked, Intercepting, Passenger secured, Clearing on site, Checking nearby targets and Returning to dock. During clearance, progress bars show the remaining fixed timer rather than human fatigue.

Results include all responders in team utilisation, while capacity/fatigue averages use Purple operators only. The fixed robot clearance duration is reported separately.

Scenario saves retain the robot timer and typed docking posts. New saves write `WheeledRobot`; old `HumanoidRobot` entries and numeric robot identities load as wheeled robots at the same coordinates. Files without a responder kind still load as Purple operators. Files without a robot timer use the 10-second default. Restart retains types/docks/settings and resets workload.

## Verification

Regression tests cover target ownership, immediate pursuit, fixed clearance away from the dock, consecutive nearby interceptions, fixed timing across cases, range from the current position, no-candidate return, human-first reservations, unique reservations between completed robots, robot exclusion from idle/busy Dasher response, human-only Dasher teams, Mode 2 gate release, shared yielding, timer validation, reset and legacy save migration.

All application/test sources compiled and 234 regression cases passed, including 44 robot cases, using the .NET 8 compiler/runtime and actual xUnit methods/assertions via a reflection harness. Standard SDK CLI commands and native visual verification could not be run here. See `VALIDATION.md`. Run the commands in `run_instructions.md` on your PC, then check:

- Set robot clearance to 15 seconds and Purple clearance to 60 seconds with fatigue enabled. Watch two robot cases each take 15 seconds at their interception points.
- Keep another unreserved Undetected passenger near the robot as clearance finishes. Check it intercepts directly without travelling toward its dock, and uses the same fixed timer for that next passenger.
- Let all eligible nearby passengers clear or leave acquisition range. Check the robot returns empty to its original dock.
- Reserve a nearby target with Purple and check the robot leaves that reservation alone.
- Activate a nearby Dasher while a robot is docked, intercepting, clearing and returning. Check that only Purple operators respond.
- Check the wheeled preview/body, live progress and fixed-time status on your display.
- Save/reload and restart a mixed team. Check docking positions and robot time are retained.
- In Mode 2, check on-site Red clearance releases the same gate and keeps the recorded Red outcome.

The download contains updated source. Rebuild or publish to produce a new executable.
