# Discovery comparison gate: separate bounded verification

16 September 2026. Offline harness and verification tooling only. The user requested proceeding after the sealed prerequisite timeout. Preserve that failed scope unchanged; this is a new scope, not its retry or resume. Incoming [receipt](../TestResults/balance/tower-discovery-comparison-gate-20260916/completion.json): **2,864.784 diagnostic seconds used / 135.216 remaining**. Cap this scope at **110 diagnostic seconds / 128 MiB**, inside the unchanged cumulative **3,000-second / 4 GiB** limits. Zero fights, runtime preparations, fresh values, retries, resumes or replays. Preserve all **482,821 reservations**, fixed ability order and adoption Hold.

## Preservation change

The previous serial preservation scan exhausted its frozen 20-second deadline before returning a complete result. That timeout does not establish corruption or a specific I/O cause. Leave all legacy assertions and sealed scripts intact. Add `build/tower_evidence_verification.py`, which runs the existing preservation functions with bounded four-worker content hashing. Manifest reads prehash their member files in batches of at most 32; each old content-hash assertion still compares the actual digest against its expected value. Keep every seal, exact inventory, historical-ledger and package-count assertion.

Use a fresh cache for each complete before/after verification pass, keyed by resolved path. Each file is read and hashed in that pass; cache hits require unchanged device, inode, size and modification/change timestamps. Reject a change while hashing or before a cache hit. No hash is trusted merely because two files have the same expected digest, and no hashes carry between passes. Reject manifest path escapes. Stop at deadlines, record metrics and retain evidence on failure. This is bounded read concurrency, not parallel agents or a weaker preservation rule.

## Frozen diagnostics

Use `TestResults/balance/tower-discovery-comparison-gate-verification-20260916`. Before execution, retain the pre-edit checkout snapshot and pin the protocol, wrapper, scripts, existing gate/test source and input dependencies. The gate and its ten tests must match the previous failed package's source snapshots; no search or gate behavior changes are planned.

1. During freeze, run **eight zero-combat Python fixtures**: valid repeated reads; tampered content; missing member; extra member; changed manifest seal; path escape; expired deadline; and same-pass file change. Only the valid case passes. Then verify all **70 predecessor packages**, including the failed gate scope, with unchanged legacy assertions. Record bytes/files actually hashed, same-pass hits, package counts and elapsed time. Freeze has a new explicit **35-second** ceiling within this new scope; the old failed 20-second limit stays unchanged.
2. Compile the captured harness and tests without restore using the retained compiler dependencies. Run exactly the previously planned **105 backend tests** once through `build/run-tests.ps1 -NoBuild -ArtifactsPath`: the prior 95 plus ten gate tests. No full gameplay build or full backend suite.
3. Process the same four saved fixture pairs once: baseline `team.json` paired with `refinement`, `repeat`, `reordered`, and `reverse`. Do not regenerate or reevaluate teams. The three complete pairs must return the unchanged two nominees per arm; reverse must durably record 16 proposals, 14 evaluations and two `missing-team-roles` charges, stop the entire comparison and return zero nominees. The synthetic report envelopes are not combat evidence. No later-stage calls exist in this diagnostic.
4. Run the independent Python receipt/hash/count/ranking/ledger audit. At publication, run a fresh full 70-package preservation pass, then update Markdown and publish the measured receipt/manifest. No cross-pass cache. Report full-pass timings honestly; the old 20-second partial timeout is not a comparable complete baseline or a speedup denominator.

Phases before publication share at most **75 seconds** (individual ceilings: freeze 35, build 20, test-build 15, tests 15, saved fixtures 10, audit 10). Normal execution/publication stops within **100 seconds**, reserving the final ten seconds of the 110-second scope for failure closure if needed. Publication includes a fresh preservation pass and one closure second. Subprocess deadlines and per-phase byte checks remain enforced. Charge a conservative 1 MiB shared-test-artifact allowance. Source editing is excluded as in preceding scopes; compilation, fixtures, tests, audits and publication are charged.

Any failure or limit stops dependent phases and forbids retries. A failure-only closure may use at most 10 seconds inside the same scope to seal its receipts and update the handoff, without repeating failed checks. Never overwrite old evidence or publish a verification success if a prerequisite failed.

## Run-once commands

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-comparison-gate-verification-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The gate is a nomination boundary for already verified discovery archives, not a replacement for archive verification or durable combat-attempt charging. Full comparison-driver integration and any combat remain outside this scope. No default adoption, gameplay/content/configuration change, Kharad tuning, ability-order tuning, large confirmation or deployment. V19 remains Unresolved; reliability Fail 1/3, deep recovery 0/3 and adoption Hold stay unchanged. Earlier fresh-seed approvals are exhausted.
