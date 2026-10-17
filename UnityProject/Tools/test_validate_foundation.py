"""Negative fixture checks for the static handoff validator, not Unity tests."""

from pathlib import Path
import shutil
import tempfile
import unittest

from validate_foundation import ValidationError, validate_project


class FoundationValidationTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.project = Path(self.directory.name) / "UnityProject"
        source = Path(__file__).resolve().parents[1]
        for folder in ["Assets", "Packages", "ProjectSettings"]:
            shutil.copytree(source / folder, self.project / folder)

    def change(self, relative, old, new):
        path = self.project / relative
        text = path.read_text()
        self.assertIn(old, text)
        path.write_text(text.replace(old, new))

    def test_intact_fixture_passes_static_checks(self):
        assets, assemblies = validate_project(self.project)
        self.assertGreater(assets, 0)
        self.assertEqual(assemblies, 4)

    def test_missing_script_meta_is_rejected(self):
        (self.project / "Assets/Foundation/FoundationBootstrap.cs.meta").unlink()
        with self.assertRaisesRegex(ValidationError, "Missing .meta"):
            validate_project(self.project)

    def test_duplicate_asset_guid_is_rejected(self):
        source = self.project / "Assets/Foundation/FoundationBootstrap.cs.meta"
        target = self.project / "Assets/Editor/FoundationProjectSetup.cs.meta"
        target.write_text(source.read_text())
        with self.assertRaisesRegex(ValidationError, "Duplicate asset GUID"):
            validate_project(self.project)

    def test_dangling_camera_reference_is_rejected(self):
        self.change("Assets/Scenes/MigrationFoundation.unity", "foundationCamera: {fileID: 1002}",
                    "foundationCamera: {fileID: 9999}")
        with self.assertRaisesRegex(ValidationError, "camera reference"):
            validate_project(self.project)

    def test_disabled_build_scene_is_rejected(self):
        self.change("ProjectSettings/EditorBuildSettings.asset", "- enabled: 1", "- enabled: 0")
        with self.assertRaisesRegex(ValidationError, "enabled in build settings"):
            validate_project(self.project)

    def test_changed_editor_pin_is_rejected(self):
        self.change("ProjectSettings/ProjectVersion.txt", "6000.3.26f1", "6000.0.0f1")
        with self.assertRaisesRegex(ValidationError, "Editor version"):
            validate_project(self.project)


if __name__ == "__main__":
    unittest.main()
