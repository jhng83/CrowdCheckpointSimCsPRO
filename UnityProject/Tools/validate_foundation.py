#!/usr/bin/env python3
"""Static S1 consistency checks, never a substitute for actual Unity validation."""

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


EDITOR_VERSION = "6000.3.26f1"
SCENE = "Assets/Scenes/MigrationFoundation.unity"
BOOTSTRAP = "Assets/Foundation/FoundationBootstrap.cs"
REQUESTS = {
    "com.unity.inputsystem": "1.17.0",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.test-framework": "1.4.5",
    "com.unity.ugui": "2.0.0",
}


class ValidationError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise ValidationError(message)


def read(path):
    require(path.is_file(), "Missing file: " + str(path))
    return path.read_text(encoding="utf-8")


def meta_guid(path):
    match = re.search(r"^guid: ([0-9a-f]{32})$", read(path), re.MULTILINE)
    require(match is not None, "Missing/invalid GUID: " + str(path))
    return match.group(1)


def validate_project(project, check_dotnet=False):
    project = Path(project).resolve()
    version = read(project / "ProjectSettings/ProjectVersion.txt")
    require("m_EditorVersion: " + EDITOR_VERSION in version.splitlines(), "Unexpected Editor version")
    try:
        manifest = json.loads(read(project / "Packages/manifest.json"))
    except json.JSONDecodeError as error:
        raise ValidationError("Invalid package manifest JSON: " + str(error)) from error
    dependencies = manifest.get("dependencies", {})
    for package, expected in REQUESTS.items():
        require(dependencies.get(package) == expected, "Unexpected package request: " + package)

    assets = project / "Assets"
    guids = {}
    for meta in sorted(assets.rglob("*.meta")):
        asset = Path(str(meta)[:-5])
        require(asset.exists(), "Orphan .meta file: " + str(meta))
        guid = meta_guid(meta)
        require(guid not in guids, "Duplicate asset GUID: " + guid)
        guids[guid] = asset
    for asset in sorted(assets.rglob("*")):
        if asset.name.endswith(".meta"):
            continue
        require(Path(str(asset) + ".meta").is_file(), "Missing .meta file: " + str(asset))

    local_assemblies = {}
    for path in sorted(assets.rglob("*.asmdef")):
        try:
            definition = json.loads(read(path))
        except json.JSONDecodeError as error:
            raise ValidationError("Invalid asmdef JSON: " + str(path)) from error
        name = definition.get("name")
        require(name and name not in local_assemblies, "Missing/duplicate assembly name: " + str(path))
        local_assemblies[name] = definition
    require(len(local_assemblies) >= 4, "Foundation/runtime/editor/test assemblies are missing")
    for definition in local_assemblies.values():
        for reference in definition.get("references", []):
            require(reference in local_assemblies or reference.startswith("Unity."),
                    "Unresolved local assembly reference: " + reference)
    require(local_assemblies["CrowdCheckpoint.Unity.Editor"].get("includePlatforms") == ["Editor"],
            "Editor assembly must remain Editor-only")

    scene = read(project / SCENE)
    bootstrap_guid = meta_guid(project / (BOOTSTRAP + ".meta"))
    require("guid: " + bootstrap_guid in scene, "Scene has a dangling bootstrap script reference")
    require("foundationCamera: {fileID: 1002}" in scene, "Bootstrap camera reference is missing")
    require("--- !u!20 &1002" in scene and "orthographic: 1" in scene, "Scene lacks the expected orthographic camera")
    object_ids = re.findall(r"^--- !u!\d+ &(-?\d+)$", scene, re.MULTILINE)
    require(len(object_ids) == len(set(object_ids)), "Duplicate serialized scene object IDs")
    for reference in re.findall(r"\{fileID: (-?\d+)\}", scene):
        require(reference == "0" or reference in object_ids, "Dangling scene object reference: " + reference)

    build_settings = read(project / "ProjectSettings/EditorBuildSettings.asset")
    require("- enabled: 1\n    path: " + SCENE in build_settings, "Foundation scene must be enabled in build settings")
    require("guid: " + meta_guid(project / (SCENE + ".meta")) in build_settings,
            "Build-settings scene GUID does not match its .meta")
    setup = read(project / "Assets/Editor/FoundationProjectSetup.cs")
    require('ExpectedEditorVersion = "' + EDITOR_VERSION + '"' in setup, "Setup Editor pin differs from project pin")

    if check_dotnet:
        repository = project.parent
        result = subprocess.run(
            ["dotnet", "msbuild", "CrowdCheckpointSim.csproj", "-getItem:Compile"],
            cwd=repository, capture_output=True, text=True, check=True)
        items = json.loads(result.stdout)["Items"]["Compile"]
        compiled = [item["Identity"].replace("\\", "/") for item in items]
        require(compiled and any(path.startswith("Core/") for path in compiled), "Reference core is not compiled")
        require(not any("UnityProject/" in path for path in compiled), "Original project compiles Unity sources")
    return len(guids), len(local_assemblies)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-dotnet", action="store_true", help="Inspect actual .NET 8 MSBuild compile items")
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1]
    try:
        asset_count, assemblies = validate_project(project, args.check_dotnet)
    except (ValidationError, OSError, subprocess.CalledProcessError, json.JSONDecodeError) as error:
        print("Static foundation validation FAILED: " + str(error), file=sys.stderr)
        return 1
    print(f"Static foundation checks passed: {asset_count} asset GUIDs, {assemblies} assemblies.")
    if args.check_dotnet:
        print("Actual MSBuild compile items exclude UnityProject and retain the original core.")
    print("Unity package resolution, import, C# compilation, rendering and runtime are NOT validated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
