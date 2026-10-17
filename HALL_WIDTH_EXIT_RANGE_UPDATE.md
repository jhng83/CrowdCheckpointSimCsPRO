> Historical range report. Current ranges supersede those below: hall length 20–30 m, hall width 15–20 m, Open Funnel width 5–20 m (within hall width), barricade 15–20 lanes. See `WIDTH_AND_LANES_UPDATE.md`.

# Hall Width / Exit Width / Acquisition Range Update

This build adds the following pre-simulation configuration ranges:

- Main hall length: 10-15 m (existing)
- Main hall width: 10-15 m (new)
- Exit width: 3-12 m, dynamically limited so the exit cannot exceed the current hall width
- Purple/operator acquisition radius: 4-10 m

The existing `RoomHeight` property remains as a compatibility alias for `HallWidth`, so the
renderer and simulation continue to use the same physical coordinate system. Saved scenario
configuration files continue to persist the hall width through the existing `RoomHeight` field,
which preserves compatibility with previously saved configuration files.

If resizing the hall causes an existing operator post to fall outside the valid physical room, the
post is removed by the existing reconfiguration rule and the setup UI now explicitly reports how
many posts were removed so the user can reposition them before confirming the scenario.
