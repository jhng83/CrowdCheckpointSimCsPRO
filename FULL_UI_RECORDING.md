# Full UI Recording

The runtime sidebar now includes **Start Recording** and **Stop Recording** controls.

## What is captured

Recording captures the complete OpenGL client framebuffer after Dear ImGui has rendered the frame:

- left runtime sidebar;
- four-column top dashboard;
- simulation viewport;
- humanoids and status colours;
- Dasher/Purple activity;
- labels, status text, and recording indicator.

The operating-system title bar and desktop outside the application are not captured.

## Output

- Target frame rate: 30 fps.
- Preferred output: MP4 using `mp4v`.
- Fallback: Motion-JPEG AVI if MP4 cannot be opened by the available backend.
- `dotnet run`: recordings are created under `<project>\\recordings` when launched from the project root.
- Published Windows executable: recordings are created under `<exe folder>\\recordings`.

Example:

```text
recordings/
  CrowdSim_2026-08-16_180405_123.mp4
```

## Safety / lifecycle

An active recording is finalized when the user:

- presses Stop Recording;
- resets the run;
- returns to Setup;
- exits the application;
- closes the application window.

Recording runs downstream of the simulation update and does not change SFM, queue movement,
operator logic, Dasher logic, detection outcomes, or the fixed simulation timestep.

## Windows publish

The project references `OpenCvSharp4` plus the Windows x64 native runtime directly. This avoids
requiring the WPF extension package for this non-WPF application. Publish and distribute the whole
output folder so the native runtime files remain beside the application.
