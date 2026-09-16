# Filler diversity comparison: readiness

15 September 2026. **PreparedAwaitingSeedAndTimeException**. Eight backend tests, exact 32-team proposal/trace parity, both control preparations and independent sealed-input checks passed. **Zero fights, fresh values and retries.** Complete live registry preflight remains pending and is required before allocation.

The [frozen protocol](Tower-Filler-Diversity-Comparison-Protocol.md) compares team coverage v1 with filler diversity v1 at the same search budget. Each evaluates 16 deterministic teams on four shared discovery seeds; the top two from each receive eight shared screening seeds. Each policy's winner and the two fixed controls then receive 32 confirmation seeds. Maximum **288 fights**; each finalist plays 44 total. Equipped ability order stays fixed. A generation label is not an independent structural restart. No policy tuning or gameplay changes are included.

This experiment tests whether the broader filler pool produces stronger teams. The preceding implementation broadened Essence coverage from 35/80 to 80/80 but reduced distinct used recipes from 96 to 81, and construction took 0.3592 versus 0.0823 seconds. Neither diversity nor these new zero-combat checks establish improved combat strength. Confirmation victories are primary; health and duration remain descriptive and cannot select winners. No default adoption, reliability or global optimality claim follows from this small comparison.

## Verification completed

The new isolated comparison model/runner uses exactly the sealed gameplay assemblies, content, settings, templates, equipment and timestamp for both policies and controls. Eight focused tests ran through `build/run-tests.ps1`, covering definition equality and costs, context drift, nominations, partial stages, tie-breaking, duplicate origins, paired intervals and durable interrupted attempts. The nomination test additionally matches every generated party and structural trace to the sealed team/filler fixtures. All evaluation scores are fabricated and combat entry is guarded. The implementation's prior 71 tests and ten old-policy parity fixtures are retained without rerunning them.

Native preparation reads the sealed prior ledger containing **482,731** reservations, materializes historical-label schema fixtures, prepares both controls and exports 66 interval cases. Completed preparations: 2. The separate Python audit checks the complete sealed union, identical input definitions, participants/statistics against the earlier captured controls, all intervals and test exports. This is a **sealed-input check, not live-registry readiness**. Live preflight and its independent audit must pass after approval, followed by another live check before any allocation. All 59 predecessor packages were verified unchanged.

| Preparation phase before publication | Seconds |
| --- | ---: |
| audit | 0.781 |
| build | 3.625 |
| check | 1.203 |
| freeze | 4.985 |
| request | 0.125 |
| test-build | 1.734 |
| tests | 3.188 |

Incoming diagnostic work was **2,320.929 seconds**; this preparation uses the existing 2,400-second ceiling with its own 60-second cap, including build/tests/preparation/audits/publication. The [completion receipt](../TestResults/balance/tower-filler-diversity-comparison-preparation-20260915/control/completion.json) records measured time remaining, output and a one-second closure allowance. The cumulative output cap remains **4 GiB**, with 192 MiB preparation, 128 MiB execution and 1 GiB study subcaps and a 1 MiB shared-test allowance.

## Approval boundary

The runnable package is prepared for the specific exception: **45 fresh seed values, at most 288 fights, 300 additional diagnostic seconds, zero retries/resumes/replays, unchanged cumulative 4 GiB output cap.** Additional time would make the cumulative ceiling 2,700 seconds; it does not alter any old experiment cap. Carry the measured current remainder forward. No authorization receipt, allocation or combat exists.

This request comes from the user's original zero-fresh-seed instruction and the current exhausted approvals/time limit. The previous comparable live check and audit took 62.953 seconds, and execution plus verification/publication took 149.671 seconds. The incoming 79.071 seconds could not cover the complete workflow. These observations guide the proposed allowance and are not a runtime promise. Authorization requires at least 270 seconds available; binding requires 170 seconds. A failed preflight, changed registry or reached deadline stops dependent work and preserves evidence without retry. Successful allocation would extend 482,731 reservations to **482,776**.

## Reproduction

These commands were run once, in order; sealed or failed directories must never be rerun. Source generation precedes manual review and freezing. Logs retain exact compiler/test/native commands and hashes.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-filler-diversity-comparison-preparation-20260915'
& $python -B "$work/assemble.py"
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" request
& $python -B "$work/workflow.py" check
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Tests: `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter FullyQualifiedName~BalanceHarnessFillerDiversityComparisonTests`. Native `prepare` is the zero-combat command behind `workflow.py check`.

**Unexecuted, gated commands:** `authorize.py` with the user's actual reply/context, then `execute.py check`, `audit-preflight`, `bind`, `run`, `verify`, `audit-execution`, and `finish.py`. Native verification rereads durable archives without combat. The independent execution audit reconstructs schedules, combat records, nominations, finalists, complete merged family, paired intervals and attempt/seed charges, and again checks exact proposal/trace parity. Detailed performance evidence is saved on success/failure. These live/execution checks have not run.

Changed files: new `BalanceHarnessFillerDiversityComparisonTests.cs`, isolated comparison model/runner/workflow/audit package, protocol/readiness review and six active Markdown handoffs. Root harness and unrelated dirty files are preserved. No full backend suite, gameplay/configuration changes, migrations or deployment. V19 retains all 253 required recipes and unused 512 original confirmation values; v19 Unresolved, reliability Fail 1/3, deep recovery 0/3, adoption Hold.
