# Known Behavioural Differences / Clarifications

This C# version follows the latest clarified implementation intent: **orderly queue movement in the funnel, then emergent SFM movement in the main room**.

1. **The movement model is hybrid: queue in the funnel, SFM after the checkpoint.**
   - The Python reference already contains orderly front-to-back lane/queue logic inside the funnel.
   - The C# implementation now preserves that behaviour: pedestrians keep a queue lane, maintain safe longitudinal spacing, accelerate/decelerate smoothly, and replenish from the rear by lane.
   - Once a pedestrian fully crosses the checkpoint, the pedestrian switches to urgency-influenced Social Force Model movement in the main room.
   - This is an intentional clarification from the latest implementation direction even though the written handover text broadly says fixed lanes should not be the primary crowd-motion solution.

2. **Verified Yellow pedestrians are not deleted.**
   - The Python prototype removes the pedestrian object when verification completes.
   - The handover says the pedestrian changes Yellow -> Green and resumes movement to the exit.
   - The C# implementation follows the handover for this behaviour.

3. **Rendering is standalone .NET/OpenGL instead of Unity.**
   - This project uses Silk.NET OpenGL/windowing and Dear ImGui because the requested delivery target is `dotnet run`, not a Unity project.
   - The observable setup controls, runtime controls, states, and metrics are retained.

4. **Presentation now intentionally follows the Python 2.5D OpenGL style.**
   - The C# renderer uses a fixed isometric/orthographic view based on the Python camera direction.
   - Pedestrians and operators are geometric humanoids built from 3D-looking prism body parts instead of top-down circles.
   - The simulation physics remain planar (X/Y); Z height exists for rendering only.

5. **Operator bar rule is explicitly preserved from the Python reference.**
   - Every operator begins at 100%.
   - The bar drops by 10 percentage points after each completed manual enrolment/verification.
   - The bar is capped at 50% remaining.
   - Initial manual-clearance duration is now selectable from 10 s to 30 s before the scenario is confirmed. Verification fatigue independently adds +2.5 s per completed case from that selected starting value, with no hard timing cap.

5. **Escort right-of-way is explicitly pair-aware in the C# implementation.**
   - During escort, the operator and caught Yellow pedestrian are treated as one wider moving priority formation.
   - Non-target pedestrians receive proactive sidestep/braking behaviour around the pair to avoid escort deadlock in dense crowds.

6. **Main hall length is configurable while the funnel length remains fixed.**
   - `Main hall length` means the distance from the exit wall to the checkpoint/funnel interface.
   - It is configurable from 10.0 m to 15.0 m during setup.
   - The orderly-queue funnel remains 10.0 m long, so changing main hall length alone does not change target queue population.

7. **Checkpoint Green/Yellow target is configurable before the run.**
   - The original handover baseline was 95% Green / 4% Yellow / 1% Red.
   - The active product model uses three checkpoint outcomes: Red/Uncounted is fixed at 1%, Yellow/Undetected is configurable from 0% to 4%, and Green/Cleared is the remainder from 99% to 95%.
   - The default target is 95% Green / 4% Yellow / 1% Red.
