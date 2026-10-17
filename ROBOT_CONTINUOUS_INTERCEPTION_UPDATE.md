# Consecutive Robot Interceptions (PRO12)

No extra setting is needed. After each on-site clearance, a wheeled robot checks for another eligible, unlocked passenger within the existing acquisition radius, measured from the robot's current location.

- If a target is available, it intercepts directly from there and uses the same fixed clearance timer.
- Purple operators reserve targets first. Neither existing Purple/robot locks nor autonomous clearance can be taken over.
- Several robots cannot reserve the same passenger. Existing target scoring chooses among eligible candidates.
- If none is available, the robot returns empty to its original dock. Its dock remains unchanged across consecutive cases.
- Robots continue to ignore Dashers. In Mode 2, consecutive response applies to nearby unreserved Red alarms; second-pass Yellow remains excluded.

The decision runs on the next fixed simulation step, before any return movement. Human response, SFM, orderly queues, right-of-way, collision handling, fixed clearance timing and existing setup options are retained.

## Quick user test

1. Place a wheeled robot and set a short robot manual-clearance time.
2. Let it intercept an Undetected passenger away from its dock.
3. When clearance finishes, check that another eligible nearby passenger is intercepted directly.
4. Check that Purple-locked passengers are left alone and each robot clearance takes the same configured time.
5. With no eligible nearby passenger, check that it returns to its original dock.

Full source compilation succeeded and all 234 regression cases passed, including 44 robot cases. Tests used actual xUnit methods and assertions through the .NET 8 reflection harness described in `VALIDATION.md`. Native desktop visual verification remains pending. Build/run instructions are in `run_instructions.md`.
