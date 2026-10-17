# 2.5D Wall Depth-Sorting Fix

The physical wall geometry in `ScenarioGeometry.BuildWalls` was already correct and leaves the funnel/checkpoint opening and room exit opening clear.

The visual problem came from the ImGui painter-based 2.5D renderer treating each long wall as a single depth-sorted object. A wall was therefore sorted using only its midpoint, which could make an entire long wall draw over pedestrians/operators that should be in front of part of that wall.

`WorldRenderer` now:

- splits each physical wall into short render-only chunks (0.35 m maximum length),
- depth-sorts each wall chunk independently,
- sorts pedestrians and operators from their ground contact point (`z = 0`) rather than their body-centre height,
- leaves the physical wall list unchanged for SFM/collision behaviour.

This better approximates the depth-buffered Python OpenGL reference while retaining the existing Dear ImGui rendering pipeline.
