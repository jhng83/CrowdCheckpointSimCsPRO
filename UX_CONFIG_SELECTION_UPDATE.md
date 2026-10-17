# Configuration / Interaction UX Update

This update adds user-facing workflow improvements without changing the crowd, operator, or Dasher behaviour rules.

## Added

- Save current setup to JSON under `configs/`, including operator-post locations.
- Load a saved setup from an in-app saved-configuration picker.
- Visible 60-180% Zoom slider synchronized with existing mouse-wheel zoom.
- Explicit **Select Dasher** mode near Dasher status information.
- Cyan hover glow only for eligible funnel passengers while selection mode is active.
- **Esc** and an on-screen cancel button leave Dasher selection mode.
- Clicking an ineligible main-hall passenger returns a specific explanation instead of silently failing.
- Setup readiness card when no operator exists; the card is informational only and the primary **Activate Operator Placement** control remains in the Operator Placement section.
- Explicit operator-placement mode to prevent accidental world edits.
- Low-alpha Purple acquisition-radius discs/rings around each operator.

## Behaviour guardrail

All additions are UI, persistence, or presentation features. SFM constants, queue motion, detection quotas, operator timing, acquisition logic, Dasher pursuit/catch logic, right-of-way rules, and holding-point coordinates are unchanged.

## UI cleanup

- Removed the redundant **Scenario Status** sidebar block to reduce scrolling and duplicate information.
- Removed the duplicate operator-placement action from the warning card; the warning now communicates the requirement while the primary placement action remains above it.
