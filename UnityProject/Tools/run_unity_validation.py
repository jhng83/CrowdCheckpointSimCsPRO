#!/usr/bin/env python3
"""Run an installed, licensed Unity Editor without a cloud runner; no installation or activation."""

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import sys
import uuid

from ci_results import EDITOR_VERSION, ResultError, configuration_receipt, export_generated_sources, unity_results


def preflight(project, editor, build_windows=False, cloud=False):
    if cloud:
        if sys.platform != "win32" or os.environ.get("UNITY_CLOUD_RUNNER") != "true":
            raise ResultError("A designated Windows cloud runner with UNITY_CLOUD_RUNNER=true is required")
        if os.environ.get("RUNNER_ENVIRONMENT") != "self-hosted":
            raise ResultError("Cloud entry point requires the configured dedicated cloud VM runner")
    if not editor.is_file():
        raise ResultError("An installed, licensed pinned Unity Editor must be supplied with --editor or UNITY_EDITOR_PATH")
    if sys.platform != "win32" and not os.access(editor, os.X_OK):
        raise ResultError("The supplied Editor file is not executable")
    version = (project / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
    if "m_EditorVersion: " + EDITOR_VERSION not in version.splitlines():
        raise ResultError("Project Editor request differs from the validation pin")
    if (project / "Temp/UnityLockfile").exists():
        raise ResultError("Close the Editor for this project before batch validation; UnityLockfile exists")
    if build_windows and (project / "Artifacts/WindowsFoundation").exists():
        raise ResultError("Existing Windows build was preserved; move it aside before a fresh build")


def run_editor_phase(editor, project, raw, phase, arguments, timeout=1200):
    # No shell and no echoed command/output: Editor/activation logs remain private.
    with (raw / (phase + ".stdout.log")).open("wb") as stdout, (raw / (phase + ".stderr.log")).open("wb") as stderr:
        command = [str(editor), "-batchmode", "-projectPath", str(project),
                   "-logFile", str(raw / (phase + ".editor.log"))] + arguments
        print("Running actual Unity phase:", phase, flush=True)
        try:
            result = subprocess.run(command, cwd=project, stdout=stdout, stderr=stderr, timeout=timeout)
        except subprocess.TimeoutExpired as error:
            raise ResultError("Unity phase timed out; inspect private logs: " + phase) from error
    if result.returncode != 0:
        raise ResultError("Unity phase failed; inspect private logs: " + phase)


def run_validation(editor, project=None, build_windows=False, cloud=False):
    project = Path(project or Path(__file__).resolve().parents[1]).resolve()
    editor = Path(editor).resolve()
    output = project / "Artifacts" / ("UnityCloud" if cloud else "UnityValidation")
    run_key = "standalone-" + uuid.uuid4().hex
    if cloud:
        run_key = os.environ.get("GITHUB_RUN_ID", "local-" + uuid.uuid4().hex) + "-" + os.environ.get("GITHUB_RUN_ATTEMPT", "1")
    if not run_key.replace("-", "").isalnum():
        raise ResultError("Invalid run identity")
    reports = output / "reports" / run_key
    raw = output / ("private-" + uuid.uuid4().hex)
    reports.mkdir(parents=True, exist_ok=False)
    raw.mkdir(parents=True, mode=0o700)
    summary = {
        "startedUtc": datetime.now(timezone.utc).isoformat(),
        "outcome": "Failed",
        "mode": "cloud" if cloud else "standalone",
        "applicationParity": False,
        "stageAccepted": False,
        "desktopVisualChecked": False,
        "windowsPlayerBuilt": False,
        "windowsPlayerExecuted": False,
        "phase": "preflight",
    }
    # Cross-entry-point lock: standalone and cloud runs must not share a project concurrently.
    lock = project / "Artifacts/.foundation-validation.lock"
    lock_owned = False
    try:
        preflight(project, editor, build_windows, cloud)
        try:
            with lock.open("x", encoding="utf-8") as stream:
                stream.write(str(os.getpid()) + "\n")
            lock_owned = True
        except FileExistsError as error:
            raise ResultError("Another foundation validation owns the project lock; existing lock preserved") from error

        summary["phase"] = "configure"
        run_editor_phase(editor, project, raw, "configure", [
            "-quit", "-executeMethod", "CrowdCheckpoint.Unity.Editor.FoundationValidation.ConfigureAndReport",
            "-migrationResultsPath", str(raw)])
        summary["configuration"] = configuration_receipt(raw / "configuration-receipt.json")
        summary["generatedSources"] = export_generated_sources(project, reports / "generated-foundation-settings.zip")
        summary["tests"] = []
        for mode in ("EditMode", "PlayMode"):
            summary["phase"] = mode
            results = raw / (mode + ".xml")
            run_editor_phase(editor, project, raw, mode, ["-runTests", "-testPlatform", mode, "-testResults", str(results)])
            summary["tests"].append(unity_results(results, mode))
        if build_windows:
            summary["phase"] = "build-windows"
            if (project / "Artifacts/WindowsFoundation").exists():
                raise ResultError("Existing Windows outputs were preserved; a fresh build is required")
            run_editor_phase(editor, project, raw, "build-windows", [
                "-quit", "-executeMethod", "CrowdCheckpoint.Unity.Editor.FoundationProjectSetup.BuildWindows"], timeout=1800)
            if not (project / "Artifacts/WindowsFoundation/CrowdCheckpointFoundation.exe").is_file():
                raise ResultError("Build returned success without the Windows executable")
            summary["windowsPlayerBuilt"] = True
        summary["outcome"] = "Passed"
        summary["phase"] = "finished"
        print("Foundation batch checks passed. Review generated sources; desktop visuals and stage acceptance remain unverified.")
        return 0
    except (ResultError, OSError) as error:
        message = str(error) if isinstance(error, ResultError) else "Filesystem or process launch failed; inspect private logs locally"
        summary["failure"] = message
        print("Unity validation FAILED: " + message, file=sys.stderr)
        return 1
    finally:
        if lock_owned:
            lock.unlink()
        summary["finishedUtc"] = datetime.now(timezone.utc).isoformat()
        (reports / "validation-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
        print("Validation summary:", reports / "validation-summary.json")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor", type=Path, default=os.environ.get("UNITY_EDITOR_PATH"), help="Explicit path to the already licensed pinned Editor")
    parser.add_argument("--build-windows", action="store_true", help="Also build the foundation player with installed Windows build support")
    args = parser.parse_args()
    if args.editor is None:
        parser.error("Supply --editor or UNITY_EDITOR_PATH; this tool does not discover or connect to another computer")
    return run_validation(args.editor, build_windows=args.build_windows)


if __name__ == "__main__":
    sys.exit(main())
