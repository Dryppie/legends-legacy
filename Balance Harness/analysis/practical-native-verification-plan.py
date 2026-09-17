"""Read-only evidence/cost inspection; emits a proposal, never allocates or launches.

Run from any directory. Stdout is JSON; source files are only read. The pinned
manifests establish consistency with this inspected snapshot, not native audit.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PILOT = ROOT / "TestResults/balance/tower-incumbent-practical-pilot-20260916"
SUBGROUP = ROOT / "TestResults/balance/tower-subgroup-comparison-20260916"
RUNTIME = ROOT / "TestResults/practical-search-build/bin/BalanceHarness/release"
MIB = 1024 * 1024


def digest(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def require(condition: bool, message: str):
    if not condition:
        raise ValueError(message)


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def inspect() -> dict:
    pins = {}

    def checked(root: Path, name: str, manifest: dict):
        path = root / name
        actual = digest(path)
        require(actual == manifest[name], f"Changed retained input: {path}")
        pins[relative(path)] = actual
        return read(path)

    manifests = []
    for root, expected in [
        (PILOT, "687187963760b347593a60efd0f4573eb988755d42725ce13b47cdb02dad79de"),
        (SUBGROUP, "f30d8e85e90e65ad5fee8062b2f01aee422fd96dd533eaba5643c8b1cec4d119"),
    ]:
        path = root / "files.json"
        require(digest(path) == expected, f"Changed inspected manifest: {path}")
        pins[relative(path)] = expected
        manifests.append(read(path))

    pilot_files, subgroup_files = manifests
    completion = checked(PILOT, "completion.json", pilot_files)
    native = checked(PILOT, "native-verification.json", pilot_files)
    audit = checked(PILOT, "independent-audit.json", pilot_files)
    definition = checked(PILOT, "study/definition.json", pilot_files)
    scope = checked(PILOT, "study/scope.json", pilot_files)
    cost = checked(PILOT, "study/cost.json", pilot_files)
    study_files = checked(PILOT, "study/files.json", pilot_files)
    latest = checked(SUBGROUP, "completion.json", subgroup_files)
    latest_audit = checked(SUBGROUP, "independent-audit.json", subgroup_files)
    history_files = checked(SUBGROUP, "live-history-files.json", subgroup_files)
    recovery_pins = checked(SUBGROUP, "recovery-pins.json", subgroup_files)
    checked(SUBGROUP, "history-input.json", subgroup_files)
    for path, expected in recovery_pins.items():
        path = Path(path)
        require(digest(path) == expected, f"Changed recorded recovery source: {path}")
        pins[relative(path)] = expected

    require(native["verificationStatus"] == audit["status"] == "Passed", "Missing recorded pilot verification")
    require(native["newFights"] == 0 and completion["combatRetries"] == 0, "Changed pilot accounting")
    require(completion["newFights"] == completion["completedFights"] == cost["total"] == 1408, "Changed pilot cost")
    require(completion["newSeeds"] == 297 and completion["pilot"] == "Closed", "Changed pilot closure")
    require(latest_audit["totalExclusions"] == 483988 and latest["priorChargesPreserved"], "Changed historical baseline")
    require(definition["generation"]["candidatesPerArm"] == 64
            and definition["generation"]["maximumAttemptsPerArm"] == 256
            and definition["stages"]["shortlist"] == 4, "Changed reference workload")

    content = ROOT / "LL/src/API/API.LL/Data"
    content_matches = {name: digest(content / name) == expected for name, expected in definition["contentHashes"].items()}
    require(all(content_matches.values()), "Content drift requires revising this proposal")
    assemblies = {
        name: {"retainedSha256": expected, "currentSha256": digest(RUNTIME / (name + ".dll"))}
        for name, expected in scope["execution"]["assemblyHashes"].items()
    }
    # Match RetainExecutable's dependency selection without loading any assembly.
    deps = read(RUNTIME / "BalanceHarness.deps.json")
    runtime_files = {"BalanceHarness.dll", "BalanceHarness.deps.json", "BalanceHarness.runtimeconfig.json"}
    for library in deps["targets"][deps["runtimeTarget"]["name"]].values():
        runtime_files.update(Path(name).name for name in library.get("runtime", {}) if not name.endswith("/_._"))
        runtime_files.update(
            str(Path(asset["locale"]) / Path(name).name)
            for name, asset in library.get("resources", {}).items()
            if (RUNTIME / asset["locale"] / Path(name).name).is_file()
        )
    runtime_files.update(str(p.relative_to(RUNTIME)) for p in (RUNTIME / "runtimes").rglob("*") if p.is_file())
    runtime_bytes = sum((RUNTIME / name).stat().st_size for name in runtime_files)
    runtime_pins = {name.replace("\\", "/"): digest(RUNTIME / name) for name in sorted(runtime_files)}

    # File sizes are observations, not a hash audit of every saved battle.
    study_bytes = sum((PILOT / "study" / name).stat().st_size for name in study_files)
    study_bytes += (PILOT / "study/files.json").stat().st_size
    retired_runtime_bytes = sum((PILOT / "study" / name).stat().st_size for name in study_files if name.startswith("executable/"))
    wrapper_names = ["source-definition.json", "definition.json", "history-input.json", "seed-ledger.json"]
    # Reserve four whole definition-sized copies rather than assuming registration
    # metadata is free. This is a proxy, not the future serialized byte count.
    definition_bytes = (PILOT / "study/definition.json").stat().st_size
    archive_proxy = study_bytes - retired_runtime_bytes + runtime_bytes
    study_and_wrapper_proxy = archive_proxy + len(wrapper_names) * definition_bytes

    stages = {"constructionValues": 1, "discoveryValues": 8, "selectionValues": 32, "confirmationValues": 256}
    fights = {"discovery": 64 * 8, "selection": 4 * 32, "confirmationMaximum": 3 * 256}
    require(sum(stages.values()) == 297 and sum(fights.values()) == 1408, "Plan arithmetic")
    phases = {
        "admissionAndAllocationCheck": {"maximumSeconds": 120, "maximumBytes": 32 * MIB},
        "publicAllocationRunIncludingNativeVerification": {"maximumSeconds": 360, "maximumBytes": 208 * MIB},
        "separateAuditAndCloseout": {"maximumSeconds": 120, "maximumBytes": 16 * MIB},
    }
    return {
        "version": "tower-practical-native-verification-proposal-v1",
        "status": "ProposedNotAuthorizedOrAllocated",
        "nativeLaunchReady": False,
        "blockingPrerequisite": "Explicit scope for native admission, complete live-history reconciliation and the bounded real workflow; handoff implementation has passed synthetic verification only",
        "nextWork": "Decide the proposed native scope before creating its concrete master, request and unscheduled template; preserve the quality-experiment no-go",
        "ownedHandoff": {
            "requestVersion": "tower-practical-allocated-search-v1",
            "checkCommand": "tower-practical-search-allocation-check",
            "runCommand": "tower-practical-search-allocate-run",
            "status": "ImplementedAndSyntheticallyVerified",
            "allocationPhase": "publicAllocationRunIncludingNativeVerification",
            "partialPendingRecovery": "Unsupported; unresolved allocated-search Pending blocks later allocation",
        },
        "purpose": "One representative native integration and resource-feasibility observation; no search-policy comparison",
        "sourcePins": pins,
        "retainedEvidence": {
            "verification": "Selected metadata matches pinned manifests; recorded native/audit results read, not rerun",
            "pilotBindingAndRunSeconds": completion["bindingAndRunSeconds"],
            "pilotVerificationSeconds": completion["verificationSeconds"],
            "pilotTotalChargedSeconds": completion["chargedDiagnosticSeconds"],
            "pilotTotalChargedBytes": completion["chargedOutputBytes"],
            "pilotStudyFiles": len(study_files), "pilotStudyBytesIncludingManifest": study_bytes,
            "latestKnownHistoryFiles": len(history_files), "historicalExclusions": latest_audit["totalExclusions"],
            "closedHistoricalRemainingBytes": latest["remainingOutputBytes"],
            "closedHistoricalRemainingSeconds": latest["remainingDiagnosticSeconds"],
        },
        "staticCompatibility": {
            "contentFilesMatching": sum(content_matches.values()), "contentFiles": len(content_matches),
            "assemblies": assemblies, "currentRuntimeFiles": runtime_pins,
            "nativeAdmissionPerformed": False, "completeLiveRegistryScanned": False,
            "settingsSemanticallyCompared": False,
        },
        "storagePlanning": {
            "currentRuntimeBytes": runtime_bytes, "retiredRuntimeBytes": retired_runtime_bytes,
            "definitionBytes": definition_bytes, "wrapperProxyFiles": wrapper_names,
            "studyAndWrapperProxyBytes": study_and_wrapper_proxy,
            "runBytesAboveProxy": phases["publicAllocationRunIncludingNativeVerification"]["maximumBytes"] - study_and_wrapper_proxy,
            "limitation": "One retained archive plus current dependency sizes and four definition-sized wrapper copies; not a measured new-workflow peak or guarantee",
        },
        "proposal": {
            "policy": "retained-composition-incumbents-v1", "constructionRestarts": 1,
            "evaluatedParties": 64, "maximumProposals": 256, "nominees": 4, "finalists": 1,
            "stages": stages, "freshValuesMaximum": sum(stages.values()),
            "fights": fights, "attemptedFightsMaximum": sum(fights.values()),
            "retries": 0, "replays": 0, "resume": False, "sampleExtension": False,
            "phases": phases, "maximumSeconds": sum(p["maximumSeconds"] for p in phases.values()),
            "maximumBytes": sum(p["maximumBytes"] for p in phases.values()),
            "historicalBudgetTransfer": False, "masterChosen": False, "runnableDefinitionCreated": False,
        },
    }


if __name__ == "__main__":
    print(json.dumps(inspect(), indent=2, sort_keys=True, allow_nan=False))
