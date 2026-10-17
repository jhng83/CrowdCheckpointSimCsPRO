# Ghost Operator Placement Preview

During pre-simulation setup, moving the pointer over a valid operator-post location now shows a light, semi-transparent purple humanoid before placement.

Behavior:
- Preview appears only while setup is unlocked.
- Preview uses the same world-to-ground cursor mapping as actual placement.
- Preview uses `ScenarioGeometry.IsValidOperatorPost`, so the ghost appears only where a left-click will be accepted.
- Left-click places the real operator at that position.
- Right-click removal behavior is unchanged.
- The preview participates in the same painter-depth ordering as walls and agents.

The lighter purple palette follows the Python reference preview, with transparency added in C# to make the pre-placement state visually distinct.
