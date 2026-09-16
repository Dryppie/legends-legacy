# Discovery comparison gate: bounded closure protocol

16 September 2026. User requested proceeding after the second sealed preservation timeout. This is a separate scope; neither failed scope is resumed or retried. Incoming [receipt](../TestResults/balance/tower-discovery-comparison-gate-verification-20260916/completion.json): **2,901.113 diagnostic seconds used / 98.887 remaining**. Limit this scope to **90 diagnostic seconds / 128 MiB** within the unchanged cumulative **3,000-second / 4 GiB** caps. Zero fights, runtime preparations, fresh balance values, retries or replays. Preserve **482,821 reservations**, fixed ability order and adoption Hold.

## Verification schedule

The preceding partial pass hashed 4,140,761,904 bytes before reaching its 35-second limit. Repeating a full history scan both before and after code tests would consume most of the remaining allowance. This scope checks the exact inputs against trusted sealed manifests before using them, then performs **one complete 71-package content-preservation audit before publishing any verified result**. Every existing seal, inventory, content-hash, package-count and ledger assertion remains required. A missing or failed final audit prevents verified completion, even if tests pass. Old caps and failed packages remain unchanged.

The gate, its ten tests and `build/tower_evidence_verification.py` remain byte-identical to their saved source. Reuse the preceding eight passing preservation fixtures by verifying their sealed receipt and unchanged wrapper; do not rerun them. The wrapper uses at most four hash workers, 32 queued entries and a fresh within-pass cache. No previous partial hash cache is reused. No search, ranking, gameplay or comparison design changes are planned.

## Exact scope and diagnostics

Use `TestResults/balance/tower-discovery-comparison-gate-closure-20260916`. Freeze protocol/scripts and retain the dirty-checkout snapshot before execution. Verify the trusted manifest hashes for the refined generator, captured compiler dependency package and latest failed scope. Verify every actually consumed captured fixture/source/dependency/test artifact against those manifests. Verify the current ledger against the preceding verified freeze and the current harness/test sources against captured sources, including the uncompiled gate snapshots. Preserve unmodified unrelated work.

Run once, sequentially:

1. **Freeze:** targeted input verification and capture only; no full historical content scan. Save exact source, binary dependency, fixture and protocol hashes before compilation. The previous eight preservation fixtures must be proven unchanged.
2. **Build and test-build:** isolated captured harness/test compilation without restore or ordinary gameplay build.
3. **Tests:** exactly **105 backend tests** through `build/run-tests.ps1 -NoBuild -ArtifactsPath`, covering the preceding 95 tests plus ten gate tests. No combat.
4. **Saved fixtures:** baseline `team.json` paired once with each of `refinement`, `repeat`, `reordered` and `reverse`. No new generation or evaluation. Three complete pairs must retain two nominees per arm; the 14/16 reverse pair must retain two `missing-team-roles` charges and stop before any nominations or later stages. Persist receipts and performance measurements.
5. **Audit:** independently check the four saved pairs' hashes, counts, schedules, statuses, unchanged ranking, zero later stages and reservation union.
6. **Preserve:** full fresh verification of all **71 predecessor packages**, including both failed gate scopes, using the unchanged legacy assertions and bounded hash wrapper. Record full-pass file/byte counts and timings. The previous partial timeouts are not comparable full baselines; claim no speedup ratio.
7. **Publish:** verify all success receipts and input pins, update active Markdown, check unrelated-file preservation and local links/whitespace, then seal this scope with measured time/output and exact remaining allowance.

Freeze/build/test/fixture/audit phases share at most **30 seconds** (individual ceilings: freeze 8, build 12, test-build 8, tests 8, saved fixtures 6, audit 6). Full preservation gets at most **55 seconds**, also bounded so normal work stops by **85 seconds**. Reserve the last five seconds of the 90-second scope for success or failure closure, including one closure second. Enforce subprocess deadlines and phase-boundary output checks; charge 1 MiB for shared test artifacts. Source editing is excluded; compilation, tests, fixtures, audits and publication are charged. No verified success without the full preservation result.

Any failure stops dependent phases. The failure-only closure seals receipts and updates status without repeating checks or raising caps. Unrun commands remain explicit. This protocol authorizes no fresh seeds or combat comparison.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-comparison-gate-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/publish.py"
```

The gate still consumes already verified discovery archives; it does not replace combat-attempt charging or archive verification. Full comparison-driver integration and any combat remain outside this scope. V19 stays Unresolved, reliability Fail 1/3, deep recovery 0/3, adoption Hold. Retain all 253 recipes and the original unused 512 confirmation values. No Kharad/ability-order tuning, default adoption, gameplay/configuration change, migration, large confirmation or deployment.
