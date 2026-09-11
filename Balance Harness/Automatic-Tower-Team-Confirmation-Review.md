# Staged Tower team confirmation

11 September 2026. Increment 3 of the [automatic discovery plan](Automatic-Tower-Team-Discovery-Plan.md) is implemented in the offline `LL/tools/BalanceHarness`. A single command now runs independent generation, selection validation, frozen fresh confirmation, paired reference comparisons and the [10–50% acceptance policy](Tower-Balance-Acceptance-Policy.md). The report separates execution completion, generated viability, confirmation-family balance, unresolved earlier findings and archive verification.

This is an implementation increment. Garran remains at Health **1.6764** / offense **1.6368**, and the supplied party's historical **291/1,000** result remains evidence for that specific party and budget. No new full discovery pilot, broader progression assessment or tuning campaign was run.

## Frozen stages and selection

The schema-3 definition now includes `stages.selectionPolicyVersion: "tower-staged-confirmation-v1"`. The existing generation policy and legacy boss-search schema-1/2 behavior are preserved. The study takes a detached copy of its definition, validates content/settings/execution and costs, and freezes the full inputs before its first fight. Each output directory is new; there is no append, automatic resume or sample-until-pass path.

| Boundary | Retained artifact | Rule |
| --- | --- | --- |
| Before discovery | Definition, scope, cost, seed ledger, generation inputs/mechanics, content and producing executable | Explicit stage schedules and caller-supplied historical exclusions remain fixed. References stay outside the generator boundary. |
| Before selection | `discovery.json`, `discovery-shortlist.json` | Every frozen shortlisted generated party receives the full reserved selection schedule. References cannot affect those measurements or ranking. |
| Before confirmation | `selection-results.json`, `finalists.json`, `confirmation-freeze.json`, exact exports | Select the generated primary/alternatives first, then register all declared references and deduplicate exact prepared recipes. Record the number of preceding trials and hashes of shortlist/selection. |
| After confirmation | `study.json`, `assessment.json`, `study.md` | Evaluate the fixed family with fresh outcomes, including missing/partial cells. Confirmation cannot replace finalists or alter the sample schedule. |
| Before replay audits | `replay-plan.json` | Select the first observed confirmation example of each outcome in fixed trial order, capped by the declared replay reserve. |

The primary uses the established strength objective: highest worst-context win rate, then lower guardian health, greater party survival, shorter winning duration and stable recipe ID. An above-50% primary remains selected. No search score targets 30% or caps strength at 50%.

Up to four alternatives may accompany the primary. They must fall within **ten percentage points** of its selection win rate and have both a different capability pattern and a different coarse observed behavior pattern from every selected party. Alternatives are considered by descending win rate and stable ID. An Essence-order change alone cannot supply capability diversity. The tool leaves unused places empty when it lacks qualifying alternatives.

Behavior bins are fixed by the policy version: health-deficit ratio in tenths; prevented damage, effective friendly recovery and guardian recovery in logarithmic magnitude bins; denied ticks and active-summon ticks in hundreds. These are observational distinctions, not claims of causal synergy. Full selection measurements remain available for inspection. Optional mechanism diagnostics are reserved but unused in this increment.

## Reference identity and acceptance

References preserve their full ordered character builds, equipment and actor-identity vectors. Their original source recipes remain in the definition. During this experiment, generated and reference scenarios use the common experiment ID and the context's new confirmation schedule; this removes an unrelated scenario-label difference while retaining the exact party. Prepared-recipe deduplication normalizes equipment ordering and equivalent implicit/explicit identity vectors. Distinct actual actor identities remain distinct cells.

If independent generation converges on a reference, one confirmation cell carries both generated and reference labels. It receives one seed schedule and contributes once to the uncertainty family and actual combat cost. References added, removed or reordered do not change generated proposals, discovery scores, selection scores or finalists. Real-combat tests exercise that boundary.

Acceptance uses the existing standalone evaluator: every included party must support the 50% ceiling, and at least one party per declared context must support 10% viability. The report shows wins, defeats/draws in JSON, planned/valid samples, raw above-ceiling flags, pointwise 95% Wilson intervals and approximate Bonferroni-adjusted intervals across the full frozen family. Missing cells never shrink that family. **10/10 is a failure, not acceptance.** Draws count as non-wins.

Generated viability separately asks whether independently selected generated parties support the 10% lower threshold in every context. A viable reference does not make an unsuccessful generator viable. A generated party above 50% can establish search viability while failing balance. Neither finding implies a global optimum or acceptance for an untested floor/budget.

All earlier discovery/selection above-ceiling findings remain in `study.json`. If an exact candidate/context remains unconfirmed, an otherwise passing confirmation family yields an overall **Inconclusive** result for unresolved coverage. Exact equality with a confirmed reference counts as coverage without promoting it into generated finalist selection. The report does not pool earlier outcomes with fresh confirmation, add candidates after inspection or resample to remove concerns.

Generated/reference differences use common paired seeds, with gained/lost wins and the existing approximate paired interval. Differences are displayed in **percentage points**. These descriptive comparisons do not replace absolute acceptance thresholds, and zero discordance retains uncertainty under the existing interval method.

## Accounting and reproducibility

The default reservation remains **12,624 + 1,000 × C** combats for one context, where C is the declared reference count before any generated/reference convergence. All stages and replay attempts are subject to the frozen caps. Actual counts are reported separately by stage as attempted and completed; interrupted attempts consume their reservation. Fewer qualifying alternatives, exact convergence and unused diagnostics/replay slots reduce actual consumption without reallocating it.

The archive retains:

- Frozen content, settings, execution identity, definition, source mechanics, seed exclusions and schedules.
- All generated proposals and lineage, discovery/selection measurements, finalist reasons and fixed confirmation membership.
- Exact normal-Tower scenario exports, recipes, compressed combat reports and the ordered trial ledger.
- Confirmation evidence, absolute assessment, generated/reference comparisons and qualified earlier concerns.
- The producing harness and its runtime dependencies, dependency/runtime configuration files and hashes. The matching .NET runtime/platform remains a prerequisite.
- Detailed replay reports and a final file inventory covering the archive.

Replay audits compare prepared participants, combat summaries and Tower outcome/health/duration with the original reports. A replay mismatch or interrupted execution prevents an overall Pass even if the confirmation matrix is complete. Partial confirmation evidence remains readable and retains raw breaches, but interrupted/invalid studies cannot pass full semantic reconstruction.

Verification checks the file inventory, rebuilds mechanics/generation, consumes recorded trials in stage order, reconstructs exact production inputs/cache identities, and recomputes shortlist, selection, finalists, family, evidence, acceptance, comparisons, accounting and replay audit results. It detects a changed finalist even when the file inventory has been updated to match the changed file. It executes no new combat and reports integrity independently of balance. Use each archive's producing build; later code changes are not silently applied to historical evidence. This is local reproducibility, not a cryptographic timestamp or proof against a fabricated entire experiment.

## Commands

Use a complete definition authored with the current build and explicit historical exclusions. The definition factory/preflight and default budget are documented in the [contract guide](Automatic-Tower-Team-Discovery-Implementation.md).

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-study --definition discovery.json --content-root LL/src/API/API.LL --output TestResults/balance/team-study-new
dotnet TestResults/balance/team-study-new/executable/BalanceHarness.dll tower-boss-study-verify --run TestResults/balance/team-study-new
```

Study exit codes: **0 Pass, 1 Fail, 2 Invalid, 3 Inconclusive/incomplete search, 130 Cancelled**. Verification returns 0 for a fully reconstructed complete study even when its balance assessment fails; it does not convert that assessment to Pass. Attempt-exhausted discovery stops before selection/confirmation and verifies as incomplete (exit 3). Preparation failures retain an error artifact and no acceptance report.

The existing `tower-boss-discover` / `tower-boss-discovery-verify` commands remain discovery-only. Standalone `tower-balance-evaluate` and legacy boss search retain their semantics. An additional explicitly requested `tower-loadout-replay --run <archive> --battle <trial-id> --detailed` uses normal Tower replay outside this completed study's recorded budget.

## Verification and changed files

**All 286 relevant tests passed**, including 13 new study/policy cases. The final build completed with zero errors and five pre-existing warnings in unrelated tests. Logs, the TRX and source/document hashes are retained under `TestResults/balance/tower-staged-confirmation-20260911/`.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerDashboardBossTests|FullyQualifiedName~BalanceHarnessGoal'
```

New coverage includes strength-preserving selection, qualified alternatives, 200-sample selection and 1,000-sample acceptance fixtures, missing evidence, reference-only viability, paired differences, unresolved earlier breaches, exact recipe convergence, real combat with reference removal/reordering, retained-executable launch, CLI/export parity, resigned-checksum tampering, frozen-stage timing, interrupted confirmation and replay mismatch. Synthetic outcome oracles explicitly exercise 10/10 and victory/draw/defeat audit selection; they are not game-balance measurements. The bounded real studies use only six confirmation seeds per cell and cannot establish the planned pilot's balance.

| Area | Changed files |
| --- | --- |
| New offline workflow | `TowerBossStudyPolicy.cs`, `TowerBossStudy.cs`, `TowerBossStudyArchive.cs`, `TowerBossStudyMarkdown.cs`: selection, freezing, execution, reporting, executable retention and reconstruction. |
| Existing harness integration | `TowerBossDiscoveryContract.cs` freezes the selection-policy version; `TowerBossDiscoveryRun.cs` shares stage measurement internally; `Program.cs` exposes two commands; `BalanceHarness.csproj` grants test-only access to the internal state machine. |
| New tests | `BalanceHarnessTowerBossStudyPolicyTests.cs`, `BalanceHarnessTowerBossStudyTests.cs`. |
| Documentation | This review, the discovery plan/implementation/generation reviews, acceptance policy, boss-specific/general harness/loadout plans and harness README. |

All relevant verification commands completed. No browser check was needed because the UI is unchanged. No migration, production configuration, gameplay-content, dependency-package or deployment change is introduced. Existing unrelated working-tree edits, the supplied party fixture and sealed campaigns were preserved.

[Increment 4: Tower Lab integration](Automatic-Tower-Team-Lab-Review.md) is now implemented, including independent mode, full budget/pool/reference preview, cost/progress/cancel, separate generated/reference/acceptance results, exports and replay. The [fixed floor-1 and floor-5 pilots](Automatic-Tower-Team-Pilot-Review.md) are now complete, with viable generated teams and failed balance ceilings at both declared budgets. Wider floor 1–10 progression acceptance remains unestablished.