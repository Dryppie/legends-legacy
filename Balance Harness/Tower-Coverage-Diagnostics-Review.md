# Reusable coverage diagnostics: implementation review

Completed **13 September 2026** in the offline BalanceHarness. The [reporting plan](Tower-Coverage-Diagnostics-Plan.md) is implemented as `tower-coverage-diagnostics`, with a separate versioned output. **Zero new combats, zero party-constructor calls and zero new seeds.** The latest ledger remains **471,656** distinct seeds across every array. No v10 hypothesis was selected.

## Delivered behavior

The command verifies complete compact campaign manifests, retained producing executables, frozen content/settings and all compact records using the existing archive verifier. It supports explicit historical interpretation with identical game assemblies/runtime/platform and a different reporting harness; it never executes an old harness or reconstructs generation. The typed inventory and unchanged baseline/compatibility classifiers provide all eligible authored routes. Every discovery arm, finalist and control remains visible, including zero-win recipes.

The report separates authored selectors/triggers/conditions/values from equipped coverage and observed recipients. It matches effect IDs, runtime condition identities and ability-named stagger logs by owner, keeps ambiguity and unmatched application records visible, and reconciles restored healing, regeneration, incoming health damage and first deaths per recipient. Applied stagger, break/recovery events, threshold changes and denied-action statistics remain distinct. Nested status/summon/equipment effects are retained in the inventory but are not assigned marginal Essence credit.

The reader handles targetless ability-use events and the runtime spelling `Vulnerable` → `condition.vulnerability`. The latter adds matched observations that the earlier ad hoc decoder left unmatched; it changes neither saved outcomes nor category eligibility. The previous diagnosis/review remains sealed.

## Verified saved evidence

The [final report](../TestResults/balance/tower-coverage-diagnostics-20260913/report-final/report.json), [mechanics](../TestResults/balance/tower-coverage-diagnostics-20260913/report-final/mechanics.json), [cases](../TestResults/balance/tower-coverage-diagnostics-20260913/report-final/cases.json) and [replays](../TestResults/balance/tower-coverage-diagnostics-20260913/report-final/replays.json) cover **576 discovery cases / 4,608 records**, **18 validation cells / 4,608 records**, and exactly **four already-fixed detailed replays**. Only four trials have detail; all other missing detail remains explicit. All 594 equipped-coverage rows, original trial metrics, 71 baseline/69 compatible features and category reach counts reproduce the [preceding diagnosis](Tower-Coverage-Realization-Diagnosis-Review.md).

| Existing replay | First death tick | Restored healing | Regeneration | Control instances with application / equipped |
| --- | ---: | ---: | ---: | ---: |
| New primary, restart 1 | 429 | 1,263 | 5,193 | 6 / 8 |
| New primary, restart 2 | 429 | 2,692 | 5,291 | 6 / 6 |
| New primary, restart 3 | 429 | 1,352 | 3,514 | 5 / 5 |
| Fixed original anchor | 690 | 3,786 | 7,222 | 9 / 9 |

The first stagger breaks remain ticks **400/400/400/200**. The report reproduces the complete saved stagger sequence, changing thresholds and ten-second recovery/damage windows. All four direct-route ambiguity counts are zero; unmatched application counts remain **30/71/45/213**, including unsupported nested effects. Nonzero application counts do not establish efficacy. See the [independent reproduction checks](../TestResults/balance/tower-coverage-diagnostics-20260913/reproduction.json).

The scientific result is unchanged: v9 reliability **Fail 0/3**, all twelve generated finalists **0/256**, strongest control **126/256**, tested-family assessment **Inconclusive**. No confidence calculation, pooling, sample extension or interpretation of earlier ceiling breaches was changed. Kharad remains **Health 3.04881408 / Power 3.85370128** with the fixed level-40/tier-1/rank-2 gear and five untrained Essences per character. Practical acquisition and complete-family/near-optimal claims remain unestablished.

## Files and design

- [TowerCoverageDiagnostics.cs](../LL/tools/BalanceHarness/TowerCoverageDiagnostics.cs): bounded input contract, explicit historical compatibility, archive verification, full-case joins, provenance and separate immutable output.
- [TowerCoverageDiagnosticMechanics.cs](../LL/tools/BalanceHarness/TowerCoverageDiagnosticMechanics.cs): typed authored routes, recipient exposure and conservative log matching.
- [TowerCoverageDiagnosticReplay.cs](../LL/tools/BalanceHarness/TowerCoverageDiagnosticReplay.cs): existing-replay binding, per-recipient health/death reconciliation, elapsed windows and stagger/denial observations.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs): one separate CLI command; [TowerBulkCampaign.cs](../LL/tools/BalanceHarness/TowerBulkCampaign.cs): existing manifest verifier made internal for reuse, with no behavior or serialization change.
- [Fixture tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCoverageDiagnosticsTests.cs), harness README and active Markdown plans/handoff document the implementation and next boundary.

Generator policy/defaults, eligibility, sampling, ranking, refinement, all existing serialized contracts and gameplay sources are unchanged. The reporting classes have no callers in search construction or fitness. Controls and observations are consumed only by diagnostics. Source/execution hashes and all input receipts are retained in the new package.

## Verification and limits

`dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore --verbosity quiet` passed, with five pre-existing unrelated warnings. `build/run-tests.ps1 -NoBuild -Filter FullyQualifiedName~BalanceHarnessTowerCoverageDiagnosticsTests` passed **20/20** fixture tests. These tests execute no combat campaigns. The [TRX](../TestResults/balance/tower-coverage-diagnostics-20260913/tests-3.trx) is retained. The full backend suite was not rerun; the prior 196-test result remains historical evidence, not a new pass claim.

The final CLI report completed in **26.54 seconds**. A second new output reproduced every file byte for byte. Negative CLI checks rejected a modified trusted manifest hash, a replay case with no saved trial, and an existing output directory. [Report verification](../TestResults/balance/tower-coverage-diagnostics-20260913/report-verification.json) records deterministic files, bytes and rejection messages. Both complete outputs remain retained. A development report in `report/` predates the condition-name correction and is superseded by `report-final/`; development logs and inputs are retained separately. The initial test build had an anonymous-array fixture type error, and the first CLI attempt exposed targetless ability-use events; both were fixed and their logs retained. No required command remains blocked.

The [final receipt](../TestResults/balance/tower-coverage-diagnostics-20260913/final-verification.json) verifies all 13 preceding packages, sealed reviews, source changes, unchanged catalogs/content/game assemblies, current Markdown links and whitespace checks. No migrations, configuration/default/catalog changes, deployments or external-environment actions are included.

## Next boundary

Reporting is complete. Use its authored mechanic evidence to decide whether at most one independently defined search hypothesis deserves a separate plan. Any behavioral experiment needs its own comparator, resource limits, complete seed exclusions and frozen precombat protocol. The four saved replays do not establish a causal remedy, and this implementation allocates no next campaign, constructor tracing or additional replay generation. The [current handoff](Tower-Coverage-Replication-Plan.md) is authoritative.
