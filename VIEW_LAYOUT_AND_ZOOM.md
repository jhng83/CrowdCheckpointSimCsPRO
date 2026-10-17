# View layout and zoom update

- Pre-simulation slider/input controls use a shorter fixed item width so long labels remain readable.
- The world viewport begins 150 px lower, leaving a clean upper-right reserve for future UI components.
- Mouse-wheel zoom works whenever the pointer is over the simulation viewport, during setup and runtime.
- Zoom is clamped to 60%-180% of the fitted view.
- Zoom is presentation-only: simulation coordinates, orderly queue logic, SFM, operator acquisition distances, catch/escort guardrails, timing and metrics are unchanged.


## Scene lower-offset refinement

- Setup guidance remains pinned near the top-left of the world panel.
- The rendered simulation scene now starts after a 400 px reserved band (150 px previous reserve + 250 px additional offset).
- Mouse-wheel zoom remains available in pre-sim and runtime and does not change simulation coordinates or physics.
