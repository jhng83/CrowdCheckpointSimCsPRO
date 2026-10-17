# Pre-simulation operator controls

This build makes two operator parameters configurable before scenario confirmation.

- **Operator acquisition radius:** 4.0 m to 10.0 m, default 6.0 m. A free operator may acquire an Undetected pedestrian only when the pedestrian is at or inside the selected current physical distance. The selected radius is acquisition-only; an already-acquired target is not dropped for moving outside it.
- **Initial manual clearance:** 10.0 s to 30.0 s, default 10.0 s. This is the verification duration for the operator's first case.
- **Fatigue progression:** +2.5 s after each completed manual clearance, with no hard maximum. For example, selecting 30.0 s produces 30.0, 32.5, 35.0, 37.5 s, and so on.
- Both values are locked once the scenario is confirmed.

Existing 2.0 m cooperative stop, 0.5 m catch, 1.2 s stop/hold, SFM, orderly funnel queue, right-of-way/escort, outcome quota and health-bar behaviour are unchanged.
