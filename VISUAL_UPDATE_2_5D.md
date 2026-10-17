# Visual Update - Python-Style 2.5D Humanoids

This revision replaces the temporary top-down circle presentation with a Python-style 2.5D isometric view.

## Camera

The C# camera uses the same reference viewing direction as the Python prototype:

- eye: approximately `(24.0, -13.5, 16.0)`
- target: approximately `(8.55, 5.0, 0.5)`
- up axis: `+Z`
- orthographic/isometric presentation

The renderer auto-fits the scene to the available VS Code-launched application window so the narrower C# sidebar does not clip the funnel.

## Humanoids

Pedestrians and operators are assembled from simple rectangular prisms for:

- left/right legs
- torso
- left/right arms
- head

This deliberately mirrors the low-poly geometric appearance of the Python OpenGL implementation without introducing an external 3D asset pipeline.

## Operator bar

The operator bar follows the Python rule exactly:

`100% -> 90% -> 80% -> 70% -> 60% -> 50%`

Additional completed enrolments remain at 50%. Verification-time fatigue remains separate from the bar: the user selects an initial 10-30 s clearance time during setup, then each completed clearance adds +2.5 s with no hard timing cap.
