#!/usr/bin/env python3
"""Strict result gates and allowlisted source export; never creates validation evidence."""

import argparse
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

EDITOR_VERSION = "6000.3.26f1"
REQUIRED_CASES = {
    "EditMode": {
        "CrowdCheckpoint.Unity.Tests.FoundationSceneTests.SerializedSceneHasOneBootstrapAndItsOrthographicCamera",
        "CrowdCheckpoint.Unity.Tests.FoundationSceneTests.FoundationSceneIsEnabledInBuildSettings",
        "CrowdCheckpoint.Unity.Tests.FoundationConfigurationTests.ConfigureRetainsAssetIdentitiesAndAppliesAllFoundationSettings",
        "CrowdCheckpoint.Unity.Tests.FoundationConfigurationTests.ValidationPreservesAnAlreadyLoadedFoundationScene",
        "CrowdCheckpoint.Unity.Tests.DomainHelperTests.TimingPreservesLegacyAndPercentageFatigue",
        "CrowdCheckpoint.Unity.Tests.DomainHelperTests.CatchFatigueAndCapacityKeepTheirFloors",
        "CrowdCheckpoint.Unity.Tests.DomainHelperTests.AcquisitionIncludesBoundaryAndRejectsInvalidRadii",
        "CrowdCheckpoint.Unity.Tests.DomainHelperTests.QuotaMaintainsCountsAndResetsOnConfiguration",
        "CrowdCheckpoint.Unity.Tests.DomainHelperTests.InvalidQuotasRemainRejected",
    },
    "PlayMode": {
        "CrowdCheckpoint.Unity.Tests.FoundationSmokeTests.TrackedSceneLoadsAndRunsWithItsSerializedCamera",
    },
}
GENERATED_FILES = (
    "Assets/Settings.meta",
    "Assets/Settings/FoundationRenderer.asset", "Assets/Settings/FoundationRenderer.asset.meta",
    "Assets/Settings/FoundationPipeline.asset", "Assets/Settings/FoundationPipeline.asset.meta",
    "Packages/manifest.json", "Packages/packages-lock.json",
    "ProjectSettings/ProjectVersion.txt", "ProjectSettings/ProjectSettings.asset",
    "ProjectSettings/GraphicsSettings.asset", "ProjectSettings/QualitySettings.asset",
    "ProjectSettings/EditorBuildSettings.asset", "ProjectSettings/EditorSettings.asset",
)


class ResultError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise ResultError(message)


def count(attributes, key):
    try:
        value = int(attributes[key])
    except (KeyError, TypeError, ValueError) as error:
        raise ResultError("Missing or invalid result count: " + key) from error
    require(value >= 0, "Negative result count: " + key)
    return value


def read_xml(path):
    try:
        return ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        raise ResultError("Missing or malformed test result XML") from error


def reference_results(path):
    root = read_xml(path)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    summary = root.find("t:ResultSummary", ns)
    require(summary is not None and summary.get("outcome") in ("Completed", "Passed"), "Reference run did not complete successfully")
    counters = root.find("t:ResultSummary/t:Counters", ns)
    require(counters is not None, "Reference result counters missing")
    values = {key: count(counters.attrib, key) for key in ("total", "executed", "passed", "failed", "notExecuted")}
    require(values["executed"] >= 270, "Reference suite executed fewer than the 270 baseline cases")
    require(values["total"] == values["executed"] == values["passed"], "Reference cases were skipped or did not pass")
    require(values["failed"] == values["notExecuted"] == 0, "Reference failures/skips detected")
    for key in ("error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable", "disconnected", "inProgress", "pending"):
        require(count({key: counters.get(key, "0")}, key) == 0, "Reference run has an incomplete/error outcome")
    cases = root.findall("t:Results/t:UnitTestResult", ns)
    require(len(cases) == values["executed"] and all(case.get("outcome") == "Passed" for case in cases),
            "Reference per-case results disagree with counters")
    return {"suite": "original-dotnet", **values}


def unity_results(path, mode):
    require(mode in REQUIRED_CASES, "Unknown Unity test mode")
    root = read_xml(path)
    require(root.tag == "test-run", "Expected NUnit test-run results")
    values = {key: count(root.attrib, key) for key in ("total", "passed", "failed", "skipped", "inconclusive")}
    require(root.get("result") == "Passed", "Unity test run did not pass")
    require(values["total"] >= len(REQUIRED_CASES[mode]), "Unity run executed too few cases")
    require(values["failed"] == values["skipped"] == values["inconclusive"] == 0,
            "Unity run contains failed, skipped or inconclusive cases")
    require(values["passed"] == values["total"], "Unity passing counts disagree")
    cases = list(root.iter("test-case"))
    require(len(cases) == values["total"], "Unity per-case results disagree with counters")
    require(all(case.get("result") == "Passed" for case in cases), "Unity case did not pass")
    passed = {case.get("fullname") for case in cases}
    require(REQUIRED_CASES[mode].issubset(passed), "Required foundation cases are absent or the wrong suite ran")
    # Deliberately exclude assertion messages, user data and arbitrary case arguments from CI artifacts.
    return {"suite": mode, **values, "requiredCasesPassed": sorted(REQUIRED_CASES[mode])}


def configuration_receipt(path):
    try:
        data = json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ResultError("Missing or malformed actual-Editor receipt") from error
    require(isinstance(data, dict), "Actual-Editor receipt must be an object")
    require(type(data.get("schemaVersion")) is int and data["schemaVersion"] == 1, "Unsupported receipt schema")
    require(data.get("editorVersion") == EDITOR_VERSION, "Actual Editor version differs from the pin")
    require(type(data.get("configurationPasses")) is int and data["configurationPasses"] == 2,
            "Repeated configuration was not verified")
    required_true = ("sceneValidated", "monoBackend", "dotNetStandard", "pipelineAssigned", "packageLockPresent")
    require(all(data.get(field) is True for field in required_true), "Actual Editor settings/scene/lock checks incomplete")
    require(data.get("applicationParity") is False, "Foundation receipt must not claim application parity")
    for field in ("rendererGuid", "pipelineGuid"):
        require(isinstance(data.get(field), str) and re.fullmatch(r"[0-9a-f]{32}", data[field]), "Invalid asset identity receipt")
    fields = ("schemaVersion", "editorVersion", "configurationPasses", "rendererGuid", "pipelineGuid", "applicationParity") + required_true
    return {field: data[field] for field in fields}


def export_generated_sources(project, destination):
    project = Path(project).resolve()
    sources = []
    for relative in GENERATED_FILES:
        path = project / relative
        require(path.is_file() and path.resolve().is_relative_to(project), "Generated source missing or outside project: " + relative)
        data = path.read_bytes()
        text = data.decode("utf-8-sig")
        # Refuse nonempty credential-like fields; never include arbitrary directories or licence files.
        for line in text.splitlines():
            match = re.match(r'''\s*["']?[\w.-]*(?:password|secret|token|licensekey|serialkey|keystorepass|certificatepass)["']?\s*:\s*(.*?)\s*[,}]?\s*$''', line, re.IGNORECASE)
            if match:
                require(match.group(1) in ("", '""', "''", "null"), "Credential-like field found in generated source: " + relative)
        if path.suffix == ".json":
            try:
                document = json.loads(text)
            except json.JSONDecodeError as error:
                raise ResultError("Malformed generated JSON source: " + relative) from error

            def inspect_json(value):
                if isinstance(value, dict):
                    for key, item in value.items():
                        if re.search(r"(?:password|secret|token|licensekey|serialkey)$", key, re.IGNORECASE):
                            require(item is None or item == "", "Credential-like field found in generated source: " + relative)
                        inspect_json(item)
                elif isinstance(value, list):
                    for item in value:
                        inspect_json(item)

            inspect_json(document)
        require(not re.search(r"https?://[^\s/]+@", text), "Credential-bearing URL found in generated source: " + relative)
        require(not re.search(r"[?&](?:access_token|token|apikey|api_key|password|secret)=[^\s\"'&]+", text, re.IGNORECASE),
                "Credential-bearing URL found in generated source: " + relative)
        sources.append((relative, data))
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(destination, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for relative, data in sources:
            archive.writestr(relative, data)
    return [relative for relative, _ in sources]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("suite", choices=["reference", "EditMode", "PlayMode"])
    parser.add_argument("results", type=Path)
    args = parser.parse_args()
    try:
        result = reference_results(args.results) if args.suite == "reference" else unity_results(args.results, args.suite)
    except ResultError as error:
        print("Result validation FAILED: " + str(error), file=sys.stderr)
        return 1
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
