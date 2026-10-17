# Crowd Checkpoint Simulation - C# / .NET / OpenGL

A standalone C# conversion of the supplied Python crowd-checkpoint prototype, runnable from VS Code with:

```powershell
dotnet run
```

The hall-length option now supports **20.0–30.0 m**, with **20.0 m** as the default, in both checkpoint modes. See `HALL_LENGTH_CONTROL.md`.

Hall width is now **15–20 m**, Open Funnel width is **5–20 m** (capped by the selected hall width), and Multi-Lane Barricade supports **15–20 one-metre lanes**. See `WIDTH_AND_LANES_UPDATE.md`.

The previous updates add grouped setup tabs, a warm off-white passenger-hall floor, passenger preferred-speed bounds, configurable normal Purple interception speed, and separate timing/speed fatigue controls. See `UI_SPEED_FATIGUE_UPDATE.md` for ranges, defaults, and a short user test checklist. All baseline source features and pinned dependencies are retained.

Wheeled robots can be placed alongside Purple operators. They skip visual confirmation, take only unreserved normal targets and ignore Dashers. Robots clear passengers at the interception point using a separate fixed 1–180 second timer, then immediately intercept another nearby unreserved passenger, or return empty to their original docks if none is available. See `WHEELED_ROBOT_UPDATE.md` for setup, compatibility and validation details.

## Architecture

- **Core/** - engine-independent deterministic crowd simulation.
- **Rendering/** - Python-style 2.5D isometric projection with geometric humanoids, rendered through Dear ImGui on the OpenGL backend.
- **UI/** - responsive setup/runtime controls, permanent run status, result reporting, config save/load, zoom controls, explicit Dasher selection, and metrics.
- **Configuration/** - JSON scenario config persistence for setup values and operator-post locations.
- **Ghost operator preview** - during setup, a light semi-transparent purple humanoid follows the cursor only over valid placement positions before left-click placement.
- **App/** - Silk.NET native window/OpenGL/ImGui lifecycle.
- **tests/** - xUnit coverage for baseline population, deterministic seeding, orderly queue behaviour, outcome quotas, fatigue timing, result metrics, and responsive scaling.

## Crowd algorithm - hybrid queue + SFM

The simulation deliberately uses two movement regimes.

### 1. Funnel: orderly queue model

Pedestrians inside the funnel do **not** use SFM as their primary movement algorithm.

- The initial crowd is arranged into front-to-back rows and lateral queue lanes, based on the Python reference.
- Every pedestrian remains assigned to a queue lane while inside the funnel.
- The front person in each lane advances when space is available ahead.
- Following pedestrians preserve physical spacing to the person in front.
- Acceleration and deceleration are bounded so the queue advances smoothly.
- Small deterministic timing and lateral variations prevent the queue from looking mechanically rigid.
- A replacement pedestrian is spawned at the rear of the same lane only when enough space becomes available.

### 2. Main room: urgency-influenced Social Force Model

After the complete pedestrian body crosses the checkpoint, the pedestrian switches to SFM movement:

1. Desired-motion force drives the pedestrian toward an individual exit target.
2. Urgency modifies preferred response/speed in the main room.
3. Pedestrian-pedestrian repulsion and body-overlap correction preserve separation.
4. Wall repulsion preserves physical boundaries while leaving the exit open.
5. A spatial hash limits neighbour calculations to local agents.
6. Operator right-of-way produces local sidestep/slow-down behaviour around an active operator corridor.
7. A fixed 60 Hz simulation step decouples the dynamics from rendering frame rate.

No Unity, NavMesh or A* pathfinding is used.

## Checkpoint outcomes

A cumulative 60-second quota tracker reproduces the reference rule as closely as whole-person counts allow:

- Cleared: derived automatically, 95% to 99%
- Undetected: user-adjustable in pre-simulation setup, 0% to 4%
- Uncounted: fixed at 1%

The default split is 95% Cleared / 4% Undetected / 1% Uncounted.

## Operator logic

Operators only target Undetected pedestrians. Intervention uses the required state machine:

`Idle -> Pursuing -> Cooperative stop at 2.0m -> Catch at 0.5m -> 1.2s Stop -> Escort -> Verify -> Idle`

Initial manual clearance is selectable from 10 to 180 seconds (3 minutes). The configurable fatigue increase adds a percentage of that initial duration after each completed case: at 30 seconds and 25%, subsequent durations are 37.5, 45.0, 52.5 seconds. Timing remains uncapped during a run. The existing operator capacity bar still drops by ten percentage points per completed case to a 50% floor. Normal Undetected interception speed is also configurable, with optional loss per catch that stops at 50% of its initial speed. After verification, the passenger becomes Cleared and resumes SFM movement to the exit.

See `run_instructions.md`, `USER_GUIDE.md`, and `KNOWN_DIFFERENCES.md` for handover details.


## 2.5D OpenGL presentation

The visual layer now follows the supplied Python OpenGL reference more closely:

- fixed isometric/orthographic viewing direction based on the Python camera;
- 3D floor planes and 1.25 m wall prisms;
- pedestrians built from separate prism legs, torso, arms and head;
- operators rendered as purple geometric humanoids;
- outcome status colours are applied to the whole humanoid;
- operator health bars are rendered above each operator in world space.

The simulation coordinates remain 2D for crowd dynamics; height is presentation-only, which makes this a 2.5D simulation rather than a full 3D physics model.

## Escort right-of-way

During an Undetected-person escort, the operator and pedestrian are treated as a two-person priority formation. Nearby non-target pedestrians proactively slow and sidestep around the wider escort corridor; direct blockers can briefly stop when no safe lateral gap is available. See `ESCORT_RIGHT_OF_WAY_FIX.md`.


## Configurable main hall dimensions and exit width

The **main hall length** can be adjusted before confirmation from **20.0 m to 30.0 m** (default **20.0 m**). The **main hall width** is **15.0–20.0 m** (default **15.0 m**). The orderly-queue funnel length remains fixed at **10.0 m**; its width is **5.0–20.0 m**, capped by the selected hall width. Exit width is selectable from **3.0 m to 12.0 m** and is dynamically capped so it can never exceed the current hall width. Changing main-hall dimensions does **not** alter queue population by itself; population continues to come from funnel area and target density. The 2.5D camera reserves framing for the maximum 30 m hall length and 20 m hall width plus the fixed 10 m funnel.

## Configurable operator acquisition guard

The operator acquisition radius is selectable in pre-simulation setup from **4.0 m to 10.0 m**, with **6.0 m** retained as the default. A free purple operator activates for an **Undetected** pedestrian only when that pedestrian is currently at or inside the selected radius. The radius gates new target acquisition only; after acquisition, the existing pursuit, 2.0 m cooperative stop, 0.5 m catch, 1.2 s hold, escort, verification, fatigue, health-bar and crowd right-of-way logic remains unchanged.

## Dasher incident
Runtime supports one user-selected Dasher incident at a time. Use the **Select Dasher** button to enter an explicit selection mode; eligible funnel passengers glow cyan on hover, **Esc** cancels the mode, and ineligible clicks return a reason. A selected passenger activates only after crossing the checkpoint. Pre-simulation setup also provides a **Dasher clearance time** control from 10 to 30 seconds (default 10 s), defining how long the secured Dasher remains at the holding point before removal. See `DASHER_INCIDENT.md` for the full multi-operator pursuit, single-catch, two-catcher apprehension, holding-point and right-of-way guardrails.

## Dasher selection usability

Dasher selection is now a deliberate interaction mode rather than an always-active canvas click. The Dasher panel provides **Select Dasher** / **Cancel Dasher Selection (Esc)**, the viewport displays an active-mode banner, eligible funnel passengers use a cyan hover glow, and clicking a main-hall passenger explains that only funnel passengers are eligible. Only one Dasher incident may be marked/active at a time.


## Purple priority and right-of-way update
Dasher response is the highest-priority Purple task. When an active Dasher enters an operator's configured acquisition radius, that operator may abandon normal Yellow pursuit, stop/catch, escort, verification, or return-to-post movement and respond to the Dasher. A Yellow already in verification preserves only the remaining timer and completes autonomously; earlier-stage Yellow cases are released as Undetected. Every Purple operator away from its own post has right-of-way, so ordinary passengers yield until the Purple returns to station. The exact-two-catcher Dasher team and all other simulation guardrails remain unchanged.

## Professional UI theme

The current product-style visual pass uses a dark navy/slate operations-console theme with blue interaction accents and reserved semantic colours for Cleared, Undetected, Purple operators and the Dasher. See `PROFESSIONAL_UI_THEME.md` for the palette and UX rationale. The change is visual only and does not alter simulation rules.

## Humanoid Visual V2A

The professional UI build now uses a more human-like 2.5D procedural silhouette with neutral trousers/shoes, status-coloured upper clothing, segmented limbs, neck/pelvis shaping and a softened multi-part head. This is a rendering-only update; simulation behaviour is unchanged. See `HUMANOID_VISUAL_V2A.md`.


## Humanoid Visual V2B

The professional 2.5D humanoids now include presentation-only procedural walk/run motion with jointed arms and legs, opposite arm/leg swing, restrained body bob and a distinct running posture for Dashers and Purple Dasher pursuit. See `HUMANOID_VISUAL_V2B.md`. Simulation physics and behavioural guardrails are unchanged.

## Full UI recording

Runtime controls include Start Recording / Stop Recording. Recording captures the complete rendered
application client area, including the sidebar, dashboard and simulation viewport, and saves the
result under a `recordings` folder. The preferred output is MP4 at 30 fps, with Motion-JPEG AVI as a
fallback if the available OpenCV video backend cannot open MP4. Active recordings are finalized on
Stop, Reset, Back to Setup, application Exit, or normal window close. See `FULL_UI_RECORDING.md`.

## Configuration save/load and view controls

Setup configurations can be saved as readable JSON files under the app-local `configs` folder. A saved file contains geometry, density, outcomes, passenger speeds, acquisition radius, operator interception speed, clearance and speed fatigue settings, random seed, and operator posts. Loading restores those values in setup mode. Older files without fatigue percentages preserve their original fixed 2.5-second increment. The View section retains its 60-180% zoom and acquisition-range rings.

## Checkpoint operating modes

The application starts with a mode-selection step. **Mode 1 - Open Funnel** preserves the established orderly-funnel behaviour. **Mode 2 - Multi-Lane Barricade** uses 15-20 fixed 1.0 m single-file lanes derived from the selected 15-20 m whole-metre hall width, translucent waist-height dividers, and independently timed gates. At maximum Mode 2 density, every lane is filled from the gate toward the rear of the queue area. Barricade clearance is configurable from 3-20 seconds per attempt. Mode 2 uses a fixed 90% first-attempt Green target, a user-selectable 0-10% second-attempt Yellow target, and a derived Red target of `10% - Yellow%`; Red travellers fail both attempts and are escorted by Purple for a fixed 10-180 second manual clearance with no fatigue. Runtime provides 1x-4x simulation-time fast-forward in both modes. See `MULTI_LANE_BARRICADE_MODE.md` and `MODE2_RETRY_MANUAL_AND_SPEED_UPDATE.md` for details.

## Run workflow and result reporting

A compact permanent top strip keeps the selected mode, run state, elapsed simulation time, speed, and random seed visible throughout mode selection, setup, runtime, and result review. The context-sensitive **Start/Resume** button sits in the runtime sidebar immediately after the simulation-speed controls and above the Full UI Recording section. A permanent bottom bar contains **Pause**, **Stop**, **Restart same seed**, **Run with new seed**, and **Return to mode selection**. Stop freezes the completed model state and opens the result screen; restart actions reset all run metrics and start immediately.

The result screen reports throughput, overall average/maximum queue time, resolved Undetected catch rate, time-weighted operator utilisation, final operator fatigue/capacity, manual-clearance waiting time, and per-lane Mode 2 performance. The live Open Funnel dashboard uses a compact bold **UNDETECTED OUTCOMES** card with Current 60s and Accumulated caught, escaped, and catch-rate values.

## Responsive display scaling

The native window opens maximized. UI dimensions and font presentation scale uniformly from the 1500 x 900 reference layout using the limiting display dimension, preserving the laptop layout proportions on larger, ultrawide, and 4K displays instead of leaving controls and wording at their original pixel size. The OpenGL viewport is explicitly synchronized with the actual framebuffer at startup, so the first maximized frame fills the client area without requiring a minimise/maximise cycle. Compact live-dashboard cards reclaim unused vertical space for the simulation.
