#!/usr/bin/env python3
"""Run actual Unity on a designated Windows CLOUD runner; no installation or activation."""

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import sys
import uuid

from ci_results import ResultError, configuration_receipt, export_generated_sources, unity_results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-windows", action="store_true", help="Also build the foundation player (extra time/storage)")
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1]
    output = project / "Artifacts/UnityCloud"
    run_key = os.environ.get("GITHUB_RUN_ID", "local-" + uuid.uuid4().hex) + "-" + os.environ.get("GITHUB_RUN_ATTEMPT", "1")
    if not run_key.replace("-", "").isalnum():
        raise ResultError("Invalid run identity")
    reports = output / "reports" / run_key
    raw = output / ("private-" + uuid.uuid4().hex)
    reports.mkdir(parents=True, exist_ok=False)
    raw.mkdir(parents=True)
    summary = {
        "startedUtc": datetime.now(timezone.utc).isoformat(),
        "outcome": "Failed",
        "applicationParity": False,
        "desktopVisualChecked": False,
        "windowsPlayerExecuted": False,
        "phase": "preflight",
    }

    def editor_phase(phase, arguments, timeout=1200):
        summary["phase"] = phase
        # Logs stay private to the dedicated cloud runner; never print or upload activation output.
        with (raw / (phase + ".stdout.log")).open("wb") as stdout, (raw / (phase + ".stderr.log")).open("wb") as stderr:
            command = [str(editor), "-batchmode", "-projectPath", str(project), "-logFile", str(raw / (phase + ".editor.log"))] + arguments
            print("Running actual Unity phase:", phase, flush=True)
            try:
                result = subprocess.run(command, cwd=project, stdout=stdout, stderr=stderr, timeout=timeout)
            except subprocess.TimeoutExpired as error:
                raise ResultError("Unity phase timed out; inspect private runner logs: " + phase) from error
        if result.returncode != 0:
            raise ResultError("Unity phase failed; inspect private runner logs: " + phase)

    try:
        if sys.platform != "win32" or os.environ.get("UNITY_CLOUD_RUNNER") != "true":
            raise ResultError("A designated Windows cloud runner with UNITY_CLOUD_RUNNER=true is required; no personal computer access")
        if os.environ.get("RUNNER_ENVIRONMENT") != "self-hosted":
            raise ResultError("This entry point requires the configured dedicated cloud VM runner")
        editor = Path(os.environ.get("UNITY_EDITOR_PATH") or r"C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe")
        if not editor.is_file():
            raise ResultError("Pinned Unity Editor is not installed at the configured cloud-runner path")
        # A UUID result directory prevents acceptance of a previous run's receipts/XML.
        editor_phase("configure", ["-quit", "-executeMethod", "CrowdCheckpoint.Unity.Editor.CloudFoundationValidation.ConfigureAndReport",
                                   "-migrationResultsPath", str(raw)])
        summary["configuration"] = configuration_receipt(raw / "configuration-receipt.json")
        summary["generatedSources"] = export_generated_sources(project, reports / "generated-foundation-settings.zip")
        summary["tests"] = []
        for mode in ("EditMode", "PlayMode"):
            results = raw / (mode + ".xml")
            editor_phase(mode, ["-runTests", "-testPlatform", mode, "-testResults", str(results)])
            summary["tests"].append(unity_results(results, mode))
        if args.build_windows:
            if (project / "Artifacts/WindowsFoundation").exists():
                raise ResultError("Use a clean CI checkout for the optional player build; existing outputs were preserved")
            editor_phase("build-windows", ["-quit", "-executeMethod", "CrowdCheckpoint.Unity.Editor.FoundationProjectSetup.BuildWindows"], timeout=1800)
            if not (project / "Artifacts/WindowsFoundation/CrowdCheckpointFoundation.exe").is_file():
                raise ResultError("Build returned success without the Windows executable")
            summary["windowsPlayerBuilt"] = True
        summary["outcome"] = "Passed"
        summary["phase"] = "finished"
        print("Foundation cloud checks passed; desktop visuals and application parity remain unvalidated.")
        return 0
    except (ResultError, OSError) as error:
        summary["failure"] = str(error)
        print("Cloud validation FAILED: " + str(error), file=sys.stderr)
        return 1
    finally:
        summary["finishedUtc"] = datetime.now(timezone.utc).isoformat()
        (reports / "validation-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    sys.exit(main())
