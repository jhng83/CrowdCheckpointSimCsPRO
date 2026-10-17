# Humanoid Visual V2B - Procedural Motion Pass

Humanoid Visual V2B adds presentation-only procedural gait animation on top of the V2A professional humanoid geometry.

## Visual behaviour

- Walking agents use a restrained walking cadence with shorter stride and arm swing.
- Active Dashers use a faster running cadence, longer stride, higher knee lift and a subtle forward lean.
- Purple operators in `DasherPursuing` use the running gait while retaining the Purple role styling.
- Arms and legs move contralaterally: the arm opposite the forward leg swings forward.
- Legs are visually jointed at hip, knee and ankle.
- Arms are visually jointed at shoulder and elbow, with a higher bent-arm pose while running.
- A small vertical body bob is applied while moving and disappears immediately when stationary.
- Gait phase is deterministically offset by agent ID so crowds do not march in synchrony.
- Animation time comes from simulation time, so pausing the simulation also freezes the visual gait.

## Guardrail

This is a rendering-only change. It does not modify agent positions, velocities, SFM calculations, collision radii, checkpoint outcomes, operator acquisition, Dasher catch rules, right-of-way, manual enrolment, fatigue, or any other simulation state machine.

## Main implementation

- `Rendering/HumanoidAnimation.cs` - deterministic presentation-only gait pose calculation.
- `Rendering/WorldRenderer.cs` - jointed limb rendering, running lean, body bob and walk/run selection.
- `tests/CrowdCheckpointSim.Tests/HumanoidAnimationTests.cs` - stationary, contralateral gait, running lean and determinism checks.
