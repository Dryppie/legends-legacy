"""Once-only owner for the explicitly approved 17 September native verification.

No optimizer, derivation, gameplay, recovery or retry implementation lives here.
The production public commands own those behaviors. Every invoked process tree is
assigned, while suspended, to the existing kill-on-close Windows Job Object.
"""
from __future__ import annotations

import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time
import traceback

ROOT = next(p for p in Path(__file__).resolve().parents if (p / "LL/tools/BalanceHarness").is_dir())
SOURCES = Path(__file__).resolve().parent
PACKAGE = ROOT / "TestResults/practical-native-verification-20260917"
REGISTRY = ROOT / "TestResults/balance"
RUN = REGISTRY / "tower-practical-native-verification-20260917"
BUILD = ROOT / "TestResults/practical-search-build/bin/BalanceHarness/release"
PLAN = ROOT / "Balance Harness/Tower-Practical-Native-Verification-Plan.json"
PILOT = REGISTRY / "tower-incumbent-practical-pilot-20260916"
SUBGROUP = REGISTRY / "tower-subgroup-comparison-20260916"
OWNED = REGISTRY / "tower-barrier-attribution-readiness-recovery-20260916/bounded_output_process.py"
CONTENT = ROOT / "LL/src/API/API.LL"
MIB = 1024 * 1024
MASTER = 2026091701
DOMAIN = "tower-practical-native-verification-v1"


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def sha(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def save(path, value):
    with Path(path).open("xb") as stream:
        stream.write(json.dumps(value, sort_keys=True, separators=(",", ":"), allow_nan=False).encode())
        stream.flush()
        os.fsync(stream.fileno())


def size(path):
    return sum(p.stat().st_size for p in path.rglob("*") if p.is_file()) if path.exists() else 0


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def ledger(value):
    found = set()
    if isinstance(value, list):
        for child in value:
            if type(child) is int:
                require(-(2**31) <= child < 2**31, "Invalid historical value")
                found.add(child)
            else:
                found.update(ledger(child))
    elif isinstance(value, dict):
        require(value.get("reservationState", "Complete") == "Complete", "Unresolved historical reservation")
        for child in value.values():
            found.update(ledger(child))
    return found


def main():
    require(not PACKAGE.exists() and not RUN.exists(), "Existing claim: no retry or second root")
    started = time.monotonic()
    PACKAGE.mkdir()
    phase = "admission"
    phase_started = started
    phase_seconds, phase_bytes, phase_base = 120, 32 * MIB, 0
    peak = 0
    measurements = {}
    last_sample, last_progress = -float("inf"), -float("inf")
    sampled = 0

    def guard(force=False):
        nonlocal peak, last_sample, sampled, last_progress
        now = time.monotonic()
        require(now - started < 600 and now - phase_started < phase_seconds - 1, "Enclosing phase/time limit")
        if force or now - last_sample >= .25:
            sampled = size(PACKAGE) + size(RUN)
            last_sample = now
            peak = max(peak, sampled - phase_base)
        require(sampled < 256 * MIB - 65536 and peak < phase_bytes - 65536, "Enclosing phase/output limit")
        if phase == "run" and now - last_progress >= 20:
            attempts = RUN / "attempts.jsonl"
            rows = attempts.read_text().splitlines() if attempts.exists() else []
            complete = sum('"Completed"' in row for row in rows)
            print(f"Native run: {complete} completed attempts; {now-phase_started:.1f}s; {peak/MIB:.1f} MiB phase peak", flush=True)
            last_progress = now

    def bounded(label, command):
        guard(True)
        result = owned.run(command, ROOT, PACKAGE / f"{label}.log",
                           min(started + 600, phase_started + phase_seconds) - 2,
                           cleanup_seconds=1, guard=guard)
        save(PACKAGE / f"{label}-process.json", result)
        guard(True)
        require(result["exitCode"] == 0 and not result["timedOut"] and result["activeProcesses"] == 0,
                f"{label} failed; scope stopped with no retry")
        return result

    def close_phase():
        guard(True)
        receipt = dict(status="Passed", measuredSeconds=time.monotonic()-phase_started,
                       observedPeakBytes=peak, chargedSeconds=phase_seconds, chargedBytes=phase_bytes,
                       accounting="Full phase allowance forfeited; no transfer, refund or retry")
        save(PACKAGE / f"{phase}-phase.json", receipt)
        guard(True)
        measurements[phase] = receipt

    try:
        save(PACKAGE / "authorization.json", dict(
            version="tower-practical-native-verification-execution-v1", status="AuthorizedOnce",
            basis="User said Please proceed after the explicit 297-value/1408-fight/600-second/256-MiB scope question",
            master=MASTER, domain=DOMAIN, freshValues=297, maximumFights=1408,
            maximumSeconds=600, maximumBytes=256*MIB, retries=0, replays=0, resume=False,
            historicalBudgetTransfer=False, qualityExperimentAuthorized=False,
            phaseAllowances={"admission": [120,32*MIB], "run": [360,208*MIB], "audit": [120,16*MIB]}))
        plan = read(PLAN)
        save(PACKAGE / "frozen-plan.json", plan)
        pins = {str(ROOT / p): h for p,h in plan["sourcePins"].items()}
        for path, expected in pins.items():
            require(sha(path) == expected, f"Changed retained pin: {path}")
            guard()
        # Authenticate the reused owner against its retained manifest.
        owner_manifest = read(OWNED.parent / "files.json")
        require(owner_manifest.get(OWNED.name) == sha(OWNED), "Changed retained process owner")
        for source in [*SOURCES.glob("*.py"), SOURCES / "native-context.ps1", OWNED]:
            shutil.copyfile(source, PACKAGE / source.name)
        owned = load("practical_owned_process", PACKAGE / OWNED.name)
        save(PACKAGE / "orchestration-files.json", {p.name:sha(p) for p in PACKAGE.iterdir() if p.suffix in (".py", ".ps1")})
        runtime = PACKAGE / "runtime"
        for name, expected in plan["staticCompatibility"]["currentRuntimeFiles"].items():
            require(sha(BUILD / name) == expected, f"Changed verified runtime: {name}")
            target = runtime / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(BUILD / name, target)
            require(sha(target) == expected, "Runtime copy mismatch")
            guard()
        save(PACKAGE / "source-files.json", {
            str(p.relative_to(ROOT)).replace("\\", "/"):sha(p)
            for p in sorted((ROOT / "LL/tools/BalanceHarness").glob("*.cs"))})
        print("Capturing native producing identity and combat settings; no allocation or combat.", flush=True)
        bounded("native-context", ["pwsh", "-NoProfile", "-File", PACKAGE / "native-context.ps1",
                                  "-Runtime", runtime, "-ContentRoot", CONTENT, "-Output", PACKAGE / "native-context.json"])
        context = read(PACKAGE / "native-context.json")
        template = read(PILOT / "study/definition.json")
        require(context["settingsHash"] == template["settingsHash"], "Native settings drift")
        require(context["execution"]["assemblyHashes"] == {
            name: item["currentSha256"] for name,item in plan["staticCompatibility"]["assemblies"].items()}, "Producing identity drift")
        for name, expected in template["contentHashes"].items():
            require(sha(CONTENT / "Data" / name) == expected, "Content drift")
        pins[str(CONTENT / "appsettings.json")] = sha(CONTENT / "appsettings.json")
        required = read(SUBGROUP / "live-history-files.json")
        last_ledger = SUBGROUP / "history-input.json"
        required[str(last_ledger)] = sha(last_ledger)
        contract_path = REGISTRY / "tower-subgroup-launch-readiness-20260916/launcher-contract.json"
        historical_path = Path(read(contract_path)["historicalLedger"])
        require(str(historical_path) in required, "Authoritative historical ledger missing from pinned registry")
        for path, expected in required.items():
            require(sha(path) == expected, f"Changed historical input: {path}")
            guard()
        history = ledger(read(historical_path)) | ledger(read(last_ledger))
        require(len(history) == 483988, "Changed complete baseline")
        recovery_request = REGISTRY / "tower-fresh-first-comparison-study-20260916/request.json"
        recoveries = read(recovery_request)["pendingHistoryRecoveries"]
        recovery_hashes = {path:sha(path) for path in recoveries.values()}
        template["id"] = DOMAIN
        template["executionHash"] = context["executionHash"]
        template["excludedCombatSeeds"] = sorted(history)
        template["generation"]["seeds"] = []
        for schedule in template["stages"]["schedules"].values():
            schedule.update(discovery=[],selection=[],confirmation=[],diagnostics=[],feedback=None)
        require(all(not r["scenario"]["seeds"] for r in template["references"]), "Scheduled historical reference")
        save(PACKAGE / "template.json", template)
        request = dict(version="tower-practical-allocated-search-v1", contentRoot=str(CONTENT),
                       definitionPath=str(PACKAGE / "template.json"), definitionHash=sha(PACKAGE / "template.json"),
                       registryRoot=str(REGISTRY), outputRoot=str(RUN), requiredHistory=required,
                       maximumSeconds=480, maximumBytes=240*MIB, priorSeconds=120, priorBytes=32*MIB,
                       pendingHistoryRecoveries=recoveries, recoveryReceiptHashes=recovery_hashes,
                       allocation=dict(master=MASTER,domain=DOMAIN,discoverySamples=8,selectionSamples=32,confirmationSamples=256))
        save(PACKAGE / "request.json", request)
        pins[str(contract_path)] = sha(contract_path)
        pins.update(required)
        save(PACKAGE / "retained-input-pins.json", pins)
        print("Checking native recipes, exact cost and the complete recovery-aware history registry.", flush=True)
        bounded("allocation-check", ["dotnet", runtime / "BalanceHarness.dll", "tower-practical-search-allocation-check", PACKAGE / "request.json"])
        checked = read(PACKAGE / "allocation-check.log")
        require(checked["status"] == "ReadyNoReservation" and checked["historicalValues"] == 483988
                and checked["declaredValues"] == 297 and checked["newValues"] == checked["fights"] == 0
                and checked["cost"]["total"] == 1408, "Unexpected native admission")
        close_phase()

        phase = "run"
        phase_started, phase_seconds, phase_bytes = time.monotonic(), 360, 208*MIB
        phase_base, peak = size(PACKAGE), 0
        print("Admission passed. Launching the public allocated workflow once.", flush=True)
        bounded("public-run", ["dotnet", runtime / "BalanceHarness.dll", "tower-practical-search-allocate-run", PACKAGE / "request.json"])
        result = read(RUN / "result.json")
        require(result["integrityStatus"] == "Verified" and result["executionStatus"] == "Complete", "Incomplete native publication")
        close_phase()

        phase = "audit"
        phase_started, phase_seconds, phase_bytes = time.monotonic(), 120, 16*MIB
        phase_base, peak = size(PACKAGE)+size(RUN), 0
        print("Native publication completed. Running the separate producing-runtime and saved-JSON audits.", flush=True)
        bounded("public-audit", ["dotnet", RUN / "study/executable/BalanceHarness.dll", "tower-practical-search-verify", RUN])
        bounded("independent-audit", [sys.executable, "-B", PACKAGE / "audit.py", str(PACKAGE), str(RUN)])
        independent = read(PACKAGE / "independent-audit.json")
        require(independent["status"] == "Passed", "Independent audit failed")
        close_phase()
        save(PACKAGE / "completion.json", dict(
            status="NativeWorkflowVerified", scope="Closed", measuredSeconds=time.monotonic()-started,
            chargedSeconds=600, chargedBytes=256*MIB, retainedBytes=size(PACKAGE)+size(RUN),
            phaseMeasurements=measurements, newValues=297, completedFights=independent["completedFights"],
            strengthDecision=result["strengthDecision"], balanceAssessment=result["balanceAssessment"],
            totalExclusions=484285, historicalExclusions=483988, retries=0, replays=0,
            searchReliability="Unresolved", adoption="Hold", qualityExperimentAuthorized=False))
        save(PACKAGE / "files.json", {p.relative_to(PACKAGE).as_posix():sha(p) for p in PACKAGE.rglob("*") if p.is_file()})
        guard(True)
        print(json.dumps(read(PACKAGE / "completion.json"), indent=2), flush=True)
    except BaseException:
        failure = dict(status="StoppedNoRetry", scope="Closed", phase=phase,
                       elapsedSeconds=time.monotonic()-started, error=traceback.format_exc(),
                       chargedPhaseSeconds=phase_seconds, chargedPhaseBytes=phase_bytes,
                       completedPhases=measurements, retainedBytes=size(PACKAGE)+size(RUN),
                       reservationsPreserved=True, retries=0, noResume=True)
        save(PACKAGE / "failure.json", failure)
        print(json.dumps(failure, indent=2), flush=True)
        raise


if __name__ == "__main__":
    main()
