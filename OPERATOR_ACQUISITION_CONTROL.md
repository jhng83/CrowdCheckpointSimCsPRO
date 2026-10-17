# Configurable Operator Acquisition Guard

A free operator (`IdleAtPost`) may begin a new pursuit only when an **Undetected** pedestrian is currently within the user-selected acquisition radius.

Pre-simulation range: **4.0 m to 10.0 m**. Default: **6.0 m**.

This is an **acquisition-only** guardrail:

- Undetected pedestrian outside the selected radius: operator remains free/idle and does not activate.
- Undetected pedestrian at or inside the selected radius: a free operator may acquire the pedestrian.
- Busy operators do not acquire an additional target.
- After acquisition, moving outside the selected radius does not cancel the pursuit.
- The existing 2.0 m cooperative-stop distance is unchanged.
- The existing 0.5 m physical catch distance is unchanged.
- The 1.2 s hold, escort, crowd right-of-way, verification, fatigue, health bar and post-verification Cleared behaviour are unchanged.
- Crowd SFM and orderly-funnel queue logic are unchanged.

The acquisition test uses the pedestrian's **current physical position**, not a predicted interception position. Prediction remains part of target ranking only after a pedestrian is eligible for acquisition.
