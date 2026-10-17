# Configurable Checkpoint Outcome Percentages

Pre-simulation setup allows the **Undetected (Yellow)** target percentage to be adjusted from **0.0% to 4.0%**.

- **Uncounted (Red)** is fixed at **1.0%**.
- **Cleared (Green)** is derived automatically as `99.0% - Undetected%`.
- Therefore Cleared ranges from **99.0%** when Undetected is 0.0% to **95.0%** when Undetected is 4.0%.
- The default split is **95% Cleared / 4% Undetected / 1% Uncounted**.
- The configured split is enforced deterministically by the cumulative quota tracker within each 60-second interval.
- Cyan Dasher incidents remain a separate incident class and do not consume the normal checkpoint outcome quota.
