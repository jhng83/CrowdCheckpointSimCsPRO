# Mode 2 Retry, Manual Clearance, and Fast-Forward Update

This update changes only the Multi-Lane Barricade workflow and adds a runtime simulation-time multiplier shared by both modes.

## Mode 2 outcome workflow

- First automated attempt: fixed 90% Green target.
- Second automated attempt: user-selectable 0-10% Yellow target.
- Two-attempt failure: Red target is automatically `10% - Yellow%`.
- Each automated attempt consumes the complete configured 3-20 second barricade clearance duration.
- Yellow passes after attempt two and is not intercepted by Purple.
- Red remains before the closed barricade until Purple secures it.

## Mode 2 manual clearance

A Red barricade failure automatically dispatches an available Purple operator; this Mode 2 response does not depend on the normal acquisition radius. Purple escorts Red to its post for a fixed user-selected 10-30 second manual clearance. Mode 2 does not use the Mode 1 fatigue increment or fatigue capacity reduction. On completion the traveller becomes Green and continues toward the exit. The configured acquisition radius still governs Dasher response.

## Simulation speed

Runtime controls provide 1x, 2x, 3x, and 4x. The multiplier advances the existing fixed 60 Hz simulation more times per real-time frame slice, so movement, gate timers, Purple work, Dasher timing, and other simulation-time processes remain synchronized.
