"""Synthetic orchestration tests. No Unity executable or substitute engine is run."""

import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

from ci_results import EDITOR_VERSION, GENERATED_FILES, REQUIRED_CASES, ResultError
import run_unity_cloud
import run_unity_validation as validation


class StandaloneValidationTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.project = Path(directory.name) / "Unity project"
        (self.project / "ProjectSettings").mkdir(parents=True)
        self.version = self.project / "ProjectSettings/ProjectVersion.txt"
        self.version.write_text("m_EditorVersion: " + EDITOR_VERSION + "\n")
        self.editor = Path(directory.name) / "Editor file"
        self.editor.write_text("non-executable-content test fixture, never invoked")
        self.editor.chmod(0o700)
        self.output_patch = patch("sys.stdout", new=io.StringIO())
        self.error_patch = patch("sys.stderr", new=io.StringIO())
        self.output_patch.start()
        self.error_patch.start()
        self.addCleanup(self.output_patch.stop)
        self.addCleanup(self.error_patch.stop)

    def receipt(self, raw):
        data = dict(schemaVersion=1, editorVersion=EDITOR_VERSION, configurationPasses=2,
                    rendererGuid="a" * 32, pipelineGuid="b" * 32, sceneValidated=True,
                    monoBackend=True, dotNetStandard=True, pipelineAssigned=True,
                    packageLockPresent=True, applicationParity=False)
        (raw / "configuration-receipt.json").write_text(json.dumps(data))

    def synthetic_phase(self, editor, project, raw, phase, arguments, **kwargs):
        # Produces explicitly synthetic parser fixtures at a mocked process boundary.
        if phase == "configure":
            self.receipt(raw)
            for relative in GENERATED_FILES:
                path = project / relative
                if path == self.version:
                    continue
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("{}" if path.suffix == ".json" else "synthetic source fixture\n")
        elif phase in REQUIRED_CASES:
            names = REQUIRED_CASES[phase]
            root = ET.Element("test-run", total=str(len(names)), passed=str(len(names)),
                              failed="0", skipped="0", inconclusive="0", result="Passed")
            for name in names:
                ET.SubElement(root, "test-case", fullname=name, result="Passed")
            ET.ElementTree(root).write(raw / (phase + ".xml"))

    def invoke(self, phase=None, **kwargs):
        with patch.object(validation, "run_editor_phase", side_effect=phase or self.synthetic_phase) as call:
            code = validation.run_validation(self.editor, project=self.project, **kwargs)
        return code, call

    def summary(self, cloud=False):
        root = self.project / "Artifacts" / ("UnityCloud" if cloud else "UnityValidation")
        paths = list(root.glob("reports/*/validation-summary.json"))
        self.assertEqual(len(paths), 1)
        return json.loads(paths[0].read_text())

    def test_standalone_needs_no_runner_and_keeps_stage_and_visual_claims_false(self):
        with patch.dict(os.environ, {}, clear=True):
            code, call = self.invoke()
        self.assertEqual(code, 0)
        self.assertEqual([c.args[3] for c in call.call_args_list], ["configure", "EditMode", "PlayMode"])
        for case in call.call_args_list[1:]:
            self.assertNotIn("-quit", case.args[4])
        summary = self.summary()
        self.assertEqual(summary["outcome"], "Passed")
        self.assertEqual(summary["mode"], "standalone")
        for key in ("applicationParity", "stageAccepted", "desktopVisualChecked", "windowsPlayerBuilt", "windowsPlayerExecuted"):
            self.assertIs(summary[key], False)
        self.assertFalse((self.project / "Artifacts/.foundation-validation.lock").exists())

    def test_cloud_entry_still_rejects_an_unmarked_machine_before_editor_invocation(self):
        with patch.dict(os.environ, {}, clear=True):
            code, call = self.invoke(cloud=True)
        self.assertEqual(code, 1)
        call.assert_not_called()
        self.assertEqual(self.summary(cloud=True)["phase"], "preflight")

    def test_cloud_entry_still_requires_self_hosted_runner(self):
        with patch.object(validation.sys, "platform", "win32"), patch.dict(os.environ, {"UNITY_CLOUD_RUNNER": "true"}, clear=True):
            code, call = self.invoke(cloud=True)
        self.assertEqual(code, 1)
        call.assert_not_called()

    def test_designated_cloud_mode_retains_artifact_location(self):
        values = dict(UNITY_CLOUD_RUNNER="true", RUNNER_ENVIRONMENT="self-hosted", GITHUB_RUN_ID="123", GITHUB_RUN_ATTEMPT="2")
        with patch.object(validation.sys, "platform", "win32"), patch.dict(os.environ, values, clear=True):
            code, _ = self.invoke(cloud=True)
        self.assertEqual(code, 0)
        self.assertEqual(self.summary(cloud=True)["mode"], "cloud")
        self.assertTrue((self.project / "Artifacts/UnityCloud/reports/123-2/validation-summary.json").is_file())

    def test_missing_editor_cannot_produce_a_pass(self):
        self.editor.unlink()
        code, call = self.invoke()
        self.assertEqual(code, 1)
        call.assert_not_called()
        self.assertEqual(self.summary()["outcome"], "Failed")

    def test_project_pin_mismatch_stops_before_editor_invocation(self):
        self.version.write_text("m_EditorVersion: another-version\n")
        code, call = self.invoke()
        self.assertEqual(code, 1)
        call.assert_not_called()

    def test_running_editor_lock_is_preserved(self):
        lock = self.project / "Temp/UnityLockfile"
        lock.parent.mkdir()
        lock.write_text("existing Editor lock")
        code, call = self.invoke()
        self.assertEqual(code, 1)
        call.assert_not_called()
        self.assertEqual(lock.read_text(), "existing Editor lock")

    def test_other_validation_lock_is_preserved(self):
        lock = self.project / "Artifacts/.foundation-validation.lock"
        lock.parent.mkdir()
        lock.write_text("other run")
        code, call = self.invoke()
        self.assertEqual(code, 1)
        call.assert_not_called()
        self.assertEqual(lock.read_text(), "other run")

    def test_zero_process_exit_without_receipt_fails_and_releases_owned_lock(self):
        code, call = self.invoke(phase=lambda *a, **k: None)
        self.assertEqual(code, 1)
        self.assertEqual(call.call_count, 1)
        self.assertEqual(self.summary()["phase"], "configure")
        self.assertFalse((self.project / "Artifacts/.foundation-validation.lock").exists())

    def test_previous_result_xml_is_never_accepted(self):
        old = self.project / "Artifacts/UnityValidation/private-old"
        old.mkdir(parents=True)
        self.synthetic_phase(self.editor, self.project, old, "EditMode", [])

        def phase(editor, project, raw, name, args, **kwargs):
            if name == "configure":
                self.synthetic_phase(editor, project, raw, name, args)

        code, call = self.invoke(phase=phase)
        self.assertEqual(code, 1)
        self.assertEqual(call.call_count, 2)
        self.assertEqual(self.summary()["phase"], "EditMode")

    def test_editmode_failure_stops_before_playmode(self):
        def phase(editor, project, raw, name, args, **kwargs):
            self.synthetic_phase(editor, project, raw, name, args)
            if name == "EditMode":
                path = raw / "EditMode.xml"
                doc = ET.parse(path)
                doc.getroot().set("result", "Failed")
                doc.write(path)

        code, call = self.invoke(phase=phase)
        self.assertEqual(code, 1)
        self.assertEqual(call.call_count, 2)
        self.assertEqual(self.summary()["phase"], "EditMode")

    def test_existing_windows_player_is_never_overwritten(self):
        output = self.project / "Artifacts/WindowsFoundation"
        output.mkdir(parents=True)
        marker = output / "keep.txt"
        marker.write_text("existing build")
        code, call = self.invoke(build_windows=True)
        self.assertEqual(code, 1)
        call.assert_not_called()
        self.assertEqual(marker.read_text(), "existing build")

    def test_build_success_exit_without_executable_fails(self):
        code, _ = self.invoke(build_windows=True)
        self.assertEqual(code, 1)
        self.assertEqual(self.summary()["phase"], "build-windows")
        self.assertIs(self.summary()["windowsPlayerBuilt"], False)

    def test_launch_error_does_not_leak_arbitrary_error_details_to_summary(self):
        def fail(*args, **kwargs):
            raise OSError("private-fixture-detail")

        code, _ = self.invoke(phase=fail)
        self.assertEqual(code, 1)
        self.assertNotIn("private-fixture-detail", json.dumps(self.summary()))
        self.assertFalse((self.project / "Artifacts/.foundation-validation.lock").exists())

    def test_process_arguments_preserve_spaces_and_nonzero_exit_fails(self):
        raw = self.project / "raw"
        raw.mkdir()
        with patch.object(validation.subprocess, "run", return_value=SimpleNamespace(returncode=7)) as run:
            with self.assertRaisesRegex(ResultError, "phase failed"):
                validation.run_editor_phase(self.editor, self.project, raw, "EditMode", ["-runTests"])
        command = run.call_args.args[0]
        self.assertIsInstance(command, list)
        self.assertEqual(command[0], str(self.editor))
        self.assertEqual(command[command.index("-projectPath") + 1], str(self.project))
        self.assertNotIn("shell", run.call_args.kwargs)

    def test_process_timeout_is_a_failure(self):
        raw = self.project / "raw"
        raw.mkdir()
        with patch.object(validation.subprocess, "run", side_effect=subprocess.TimeoutExpired("fixture", 1)):
            with self.assertRaisesRegex(ResultError, "timed out"):
                validation.run_editor_phase(self.editor, self.project, raw, "PlayMode", [])

    def test_cli_requires_explicit_editor_without_discovering_other_computers(self):
        with patch.dict(os.environ, {}, clear=True), patch.object(validation.sys, "argv", ["run_unity_validation.py"]):
            with patch.object(validation, "run_validation") as run:
                with self.assertRaises(SystemExit) as exit_result:
                    validation.main()
        self.assertEqual(exit_result.exception.code, 2)
        run.assert_not_called()

    def test_cloud_wrapper_keeps_the_cloud_guard_and_build_flag(self):
        with patch.object(run_unity_cloud.sys, "argv", ["run_unity_cloud.py", "--build-windows"]):
            with patch.object(run_unity_cloud, "run_validation", return_value=1) as run:
                self.assertEqual(run_unity_cloud.main(), 1)
        self.assertEqual(run.call_args.kwargs, {"build_windows": True, "cloud": True})


if __name__ == "__main__":
    unittest.main()
