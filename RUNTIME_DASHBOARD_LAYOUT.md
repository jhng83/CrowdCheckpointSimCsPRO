# Runtime Dashboard Layout

This build moves the runtime-only information out of the left portrait sidebar and into the reserved upper world region as a four-column landscape dashboard.

Columns:
1. Live Metrics
2. Detection Outcomes
3. Operator Outcomes
4. Operator Status / Fatigue

The operator column is a fixed-height scrollable region so the layout remains stable for any practical operator count. Health/work-capacity bars are intentionally compact rather than full-column width.

The redundant Scenario Status sidebar block has been removed to reduce vertical scrolling; live operational information remains in the top dashboard and the compact status message area.

The simulation scene remains below the 400 px reserved upper region. Mouse-wheel zoom and all simulation/SFM/operator logic are unchanged.
