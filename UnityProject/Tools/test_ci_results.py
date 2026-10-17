"""Synthetic result fixtures test CI gates; none are evidence of Unity execution."""

import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile

from ci_results import (
    EDITOR_VERSION, GENERATED_FILES, REQUIRED_CASES, ResultError,
    configuration_receipt, export_generated_sources, reference_results, unity_results,
)


class CloudResultGateTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.result = self.root / "results.xml"

    def unity_fixture(self, mode="EditMode", names=None, **changes):
        names = sorted(REQUIRED_CASES[mode]) if names is None else names
        values = dict(total=str(len(names)), passed=str(len(names)), failed="0", skipped="0", inconclusive="0", result="Passed")
        values.update({key: str(value) for key, value in changes.items()})
        root = ET.Element("test-run", values)
        suite = ET.SubElement(root, "test-suite")
        for name in names:
            ET.SubElement(suite, "test-case", fullname=name, result="Passed")
        ET.ElementTree(root).write(self.result, encoding="utf-8")
        return root

    def reference_fixture(self, total=270, **changes):
        namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
        root = ET.Element(namespace + "TestRun")
        summary = ET.SubElement(root, namespace + "ResultSummary", outcome="Completed")
        values = dict(total=str(total), executed=str(total), passed=str(total), failed="0", notExecuted="0")
        values.update({key: str(value) for key, value in changes.items()})
        ET.SubElement(summary, namespace + "Counters", values)
        results = ET.SubElement(root, namespace + "Results")
        for index in range(total):
            ET.SubElement(results, namespace + "UnitTestResult", testName="fixture" + str(index), outcome="Passed")
        ET.ElementTree(root).write(self.result, encoding="utf-8")

    def receipt_fixture(self, **changes):
        data = dict(schemaVersion=1, editorVersion=EDITOR_VERSION, configurationPasses=2,
                    rendererGuid="a" * 32, pipelineGuid="b" * 32, sceneValidated=True,
                    monoBackend=True, dotNetStandard=True, pipelineAssigned=True,
                    packageLockPresent=True, applicationParity=False)
        data.update(changes)
        path = self.root / "receipt.json"
        path.write_text(json.dumps(data), encoding="utf-8")
        return path

    def test_both_unity_suites_require_the_expected_cases(self):
        for mode in REQUIRED_CASES:
            self.unity_fixture(mode)
            self.assertEqual(unity_results(self.result, mode)["total"], len(REQUIRED_CASES[mode]))

    def test_missing_result_is_rejected(self):
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_zero_test_success_is_rejected(self):
        self.unity_fixture(names=[])
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_wrong_suite_with_passing_counts_is_rejected(self):
        self.unity_fixture(names=["Unrelated.One", "Unrelated.Two"])
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_failed_unity_run_is_rejected(self):
        self.unity_fixture(result="Failed", failed=1, passed=1)
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_skipped_unity_case_is_rejected(self):
        self.unity_fixture(skipped=1, passed=1)
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_fabricated_counts_without_cases_are_rejected(self):
        self.unity_fixture(total=9, passed=9)
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_case_failure_cannot_be_hidden_by_summary(self):
        root = self.unity_fixture()
        next(root.iter("test-case")).set("result", "Failed")
        ET.ElementTree(root).write(self.result, encoding="utf-8")
        with self.assertRaises(ResultError):
            unity_results(self.result, "EditMode")

    def test_full_reference_baseline_is_accepted(self):
        self.reference_fixture()
        self.assertEqual(reference_results(self.result)["passed"], 270)

    def test_missing_reference_cases_are_rejected(self):
        self.reference_fixture(total=269)
        with self.assertRaises(ResultError):
            reference_results(self.result)

    def test_skipped_reference_case_is_rejected(self):
        self.reference_fixture(passed=269, notExecuted=1)
        with self.assertRaises(ResultError):
            reference_results(self.result)

    def test_aborted_reference_run_cannot_hide_behind_passing_cases(self):
        self.reference_fixture()
        tree = ET.parse(self.result)
        tree.getroot().find("{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}ResultSummary").set("outcome", "Aborted")
        tree.write(self.result, encoding="utf-8")
        with self.assertRaises(ResultError):
            reference_results(self.result)

    def test_actual_receipt_is_required(self):
        with self.assertRaises(ResultError):
            configuration_receipt(self.root / "missing.json")

    def test_complete_receipt_is_accepted_without_arbitrary_data(self):
        path = self.receipt_fixture(unrelatedPrivateData="fixture-value")
        result = configuration_receipt(path)
        self.assertTrue(result["monoBackend"])
        self.assertNotIn("unrelatedPrivateData", result)

    def test_wrong_editor_in_receipt_is_rejected(self):
        with self.assertRaises(ResultError):
            configuration_receipt(self.receipt_fixture(editorVersion="6000.0.0f1"))

    def test_non_object_receipt_is_rejected(self):
        path = self.root / "receipt.json"
        path.write_text("[]", encoding="utf-8")
        with self.assertRaises(ResultError):
            configuration_receipt(path)

    def test_missing_actual_configuration_checks_are_rejected(self):
        with self.assertRaises(ResultError):
            configuration_receipt(self.receipt_fixture(packageLockPresent=False))

    def test_foundation_receipt_cannot_claim_application_parity(self):
        with self.assertRaises(ResultError):
            configuration_receipt(self.receipt_fixture(applicationParity=True))

    def export_fixture(self):
        project = self.root / "UnityProject"
        for relative in GENERATED_FILES:
            path = project / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("{}" if path.suffix == ".json" else "synthetic fixture only\n", encoding="utf-8")
        return project

    def test_source_archive_excludes_logs_caches_and_licences(self):
        project = self.export_fixture()
        for relative in ("Unity_lic.ulf", "Library/cache.bin", "Artifacts/editor.log"):
            path = project / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("excluded fixture", encoding="utf-8")
        archive = self.root / "sources.zip"
        export_generated_sources(project, archive)
        with zipfile.ZipFile(archive) as source:
            self.assertEqual(set(source.namelist()), set(GENERATED_FILES))

    def test_nonempty_credential_like_field_blocks_source_export(self):
        project = self.export_fixture()
        (project / "ProjectSettings/ProjectSettings.asset").write_text("serviceToken: synthetic-not-a-real-token\n")
        archive = self.root / "sources.zip"
        with self.assertRaises(ResultError):
            export_generated_sources(project, archive)
        self.assertFalse(archive.exists())

    def test_missing_generated_source_is_not_replaced_with_placeholder(self):
        project = self.export_fixture()
        (project / "Packages/packages-lock.json").unlink()
        with self.assertRaises(ResultError):
            export_generated_sources(project, self.root / "sources.zip")

    def test_nested_json_credential_blocks_source_export(self):
        project = self.export_fixture()
        (project / "Packages/manifest.json").write_text(json.dumps({"nested": {"serviceToken": "synthetic-not-real"}}))
        with self.assertRaises(ResultError):
            export_generated_sources(project, self.root / "sources.zip")

    def test_credential_url_blocks_source_export(self):
        project = self.export_fixture()
        (project / "Packages/manifest.json").write_text(json.dumps({"url": "https://example.invalid/?token=synthetic-not-real"}))
        with self.assertRaises(ResultError):
            export_generated_sources(project, self.root / "sources.zip")


if __name__ == "__main__":
    unittest.main()
