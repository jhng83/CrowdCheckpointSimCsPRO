# Python-to-C# Reference Mapping

| Python reference | C# location | Notes |
|---|---|---|
| `config.py / SimulationConfig` | `Core/SimulationConfig.cs` | Physical dimensions, density, seed, queue rows/lanes, agent radius, computed funnel/exit bounds and population. |
| `geometry.py / build_queue_lane_y_positions` | `Core/CrowdSimulation.cs / BuildQueueLaneYPositions` | Preserves the lateral orderly queue-lane layout. |
| `geometry.py / build_initial_queue_positions` | `Core/CrowdSimulation.cs / BuildInitialCrowd` | Preserves the front-to-back queue lattice and centred partial rows. |
| `geometry.py / build_wall_segments` | `Core/ScenarioGeometry.cs` | Room/funnel/exit wall segments and geometry helpers. |
| `queue_simulation.py / Pedestrian` | `Core/Pedestrian.cs` | ID, queue lane, position/velocity, queue speed/phase, walking preference, checkpoint state, urgency and exit target. |
| `queue_simulation.py / _update_queue_lane` | `Core/CrowdSimulation.cs / UpdateQueueLane` | Orderly lane movement inside the funnel with safe spacing and bounded acceleration/deceleration. |
| `queue_simulation.py / _spawn_pending_pedestrians` | `Core/CrowdSimulation.cs / SpawnPendingPedestrians` | Rear replenishment remains lane-specific and waits for physical space. |
| `queue_simulation.py / _assign_detection_status` | `Core/DetectionQuotaTracker.cs` | Uses the cumulative current-60-second quota algorithm with configurable Yellow (0-4%), fixed Red (1%), and derived Green (99-95%). |
| Python released-pedestrian movement | `Core/CrowdSimulation.cs / ComputeRoomSfmAcceleration` | After checkpoint release, movement uses urgency-influenced SFM in the main room. |
| `queue_simulation.py / CheckpointOperator` | `Core/CheckpointOperator.cs` | Home/post, pursuit/stop/escort/verify/return state, target, catch count, fatigue count. |
| Python operator target selection/update | `Core/CrowdSimulation.cs` | Predictive Yellow target assignment, 2.0 m cooperative stop, 0.5 m catch, 1.2 s stop, escort, fatigue verification, return. |
| Python operator-yield functions | `Core/CrowdSimulation.cs` | Local SFM-compatible operator repulsion and corridor side-yield forces in the main room. |
| Python metrics counters | `Core/SimulationMetrics.cs` | Current 60-second and whole-run flow/detection/operator metrics. |
| `viewer.py`, `viewer_components.py / build_mvp_matrix` | `Rendering/IsometricCamera.cs` | Ports the Python-style fixed isometric camera direction and screen-to-ground operator placement. |
| `viewer_components.py / build_agent_vertices` | `Rendering/WorldRenderer.cs / DrawHumanoid` | Ports geometric humanoid legs, torso, arms and head with checkpoint-status colours. |
| `viewer_components.py / build_operator_vertices` | `Rendering/WorldRenderer.cs / DrawOperator` | Ports purple geometric humanoid operators. |
| `viewer_components.py / operator_fatigue_bar_fraction` | `Core/OperatorTiming.cs / HealthFraction` | 100% start, -10% per completed manual verification, minimum 50%. |
| `viewer.py`, `viewer_components.py` | `App/`, `UI/`, `Rendering/` | Standalone Silk.NET window + OpenGL + Dear ImGui interface. |
