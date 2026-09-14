# Portfolio confirmation: implemented contract and captured-scope audit

Completed **14 September 2026** in the offline BalanceHarness. **The separate 253-recipe confirmation contract is implemented, 201 relevant tests pass, and every recipe validates against captured v19 gameplay.** This task executed **zero new diagnostic or confirmation fights** and allocated **zero new balance seeds**. No fresh confirmation package or schedule was prepared.

The [implementation verification protocol](Tower-Portfolio-Confirmation-Implementation-Protocol.md) and [audit amendment](Tower-Portfolio-Confirmation-Audit-Amendment.md) bind this engineering work. Evidence is retained in [the new implementation package](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/). The earlier performance diagnostics, v19 plan, reviews, campaign and seed ledger remain unchanged.

## Version decision

Use **captured v19 gameplay** to finish the original generation-reliability comparison. The new harness compiled directly against the retained v19 Application, Common, Domain and Services.LL assemblies. Its execution directory copied the full v19 runtime, replacing only the harness DLL/PDB. The [freeze receipt](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/freeze-receipt-v2.json) records exact gameplay hashes; the [machine definition](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/audit-definition-v2.json) binds source, executable, source manifests and comparison inputs before auditing.

The [successful audit](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/audit-v2/audit.json) verifies:

- Both complete sealed v19 inventories/hashes, its 86,016 alternating start/completion pairs, the capacity-stop receipt, independent selection receipt and full family export with exact origins.
- All **253 recipes**, **112 controls**, six original nomination groups and six screened nomination groups, including the already frozen screened primaries. The fixed anchor and stronger control remain unchanged.
- Identical captured gameplay DLLs, runtime/OS/architecture, content and combat settings, followed by production input materialization of the entire family with a no-combat guard.
- All **480,707 distinct reservations**, including the **512 unused v19 confirmation values**. No new allocation function was invoked on this real source.

The current checkout/test build differs in **all four gameplay DLLs**, as well as the harness. Its combat settings match, but **ten of the bound content files differ**: abilities, creature abilities, statuses, summons, essences, items, region combat balance, creature essence loot tables, creatures and regions. These are hashes of the captured input set; they are not a claim that all unrelated current-game files were audited. An updated-gameplay confirmation needs an explicit new scope/baseline and cannot automatically inherit the original reliability claim.

The [harness source comparison](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/harness-source-continuity.json) matches **101 of 109 existing source files** against the v19 producing/before-source snapshots. `OfflineContent`, `RunBundle`, `TowerBattleRunner`, `TowerBundle` and `TowerPreparedBattle` are byte-identical. The eight changed existing files concern CLI registration, the new evaluator envelope and the previously reviewed accounting/instrumentation work. Statistical calculations remain unchanged. New confirmation/diagnostic files are recorded separately in captured source snapshots.

## Contract and execution behavior

The new policy is **`tower-portfolio-confirmation-v1`**. It imports a sealed v19 `CapacityExceeded` result into a separate study. It never reruns generation or screening, changes primaries after confirmation results, drops ceiling-only recipes, pools prior trials, resumes v19 or increases its capacity.

| Requirement | Implemented behavior |
| --- | --- |
| Complete family | Exactly 253 canonical ordered recipes, preserving all origins, 112 controls and 141 new recipes. All original and screened nominations remain bound to their saved shortlist/selection and sealed export. |
| Future shared schedule | Exactly 512 unique fresh values, shared in order by every cell: **129,536 fights**. Allocation unions the complete source ledger with the latest supplied history and preserves all unused prior reservations. |
| Statistical rules | Unchanged ordinary 10–50% evaluator; joint alpha .025 over 253 rates and .025 over nine paired differences/18 discordance intervals. The same numerical 2/3 portfolio-primary gate compares viability, matching deep primary and fixed anchor. Stronger-control comparisons remain separate. No automatic promotion. |
| Legacy limits | Schema 1 retains its **100,000-fight** evaluator cap. V19 retains capacity **144**, maximum attempts **159,744**, and its original limits. New definition schema 2 is accepted only for this policy's exact 253 × 512 envelope, one cohort and 112/141 reference/generated roles. |
| Input validation | All recipes are materialized in **112 + 112 + 29** groups under the unchanged discovery reference limit. This groups validation work only; the statistical family remains one complete 253-cell definition. |
| Future resource contract | Preparation requires explicit seconds/bytes and a nonempty study plan. Supported bounds are 1–21,600 seconds and 1 MiB–8 GiB. This task chose no live study limits or master seed. |
| Durability and storage | Zero retries, durable start/completion charging, execute-once start marker, mandatory owned parent/child accounting, exact chunk/campaign verification and full lifecycle audits. Final inventory bytes are included before completion publication. |
| Reconstruction | The completed verifier checks producing identity, source bindings, deterministic allocation, the complete definition, start marker, attempt counts, every archived report, ordinary/joint quality, metrics and exact final inventory under a no-combat guard. |

New commands are `tower-portfolio-confirmation-audit`, `-prepare`, `-check`, `-run` and `-verify`. Audit copies bound historical inputs into an **audit bundle**, which has no new schedule or root study protocol. Preparation is separate and creates fresh reservations only after source/scope checks. A normal current-gameplay build pointed at old content fails the gameplay guard; future preparation must use the reviewed captured runtime or establish a different study scope explicitly.

## Verification, failures and measurements

The final required wrapper run passed **201 tests, 0 failed, 0 skipped**, including **39 new contract cases**. [Log](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/tests-v2.log) and [TRX](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/tests-v2.trx) are retained. Coverage includes full family/cohort retention, old cap preservation, bounded materialization groups, exclusion union and deterministic allocation on synthetic data, tampered recipes/origins/nominations, missing/reordered/pooled evidence, all four 2/3 gate outcomes, ceiling-only failures, explicit resource bounds, captured gameplay mismatch, unsafe frozen paths, cancellation and prevention of a second start. Existing compact/accounting, prior 94-recipe confirmation and evaluator tests also pass.

Two implementation issues were found and fixed with evidence preserved:

1. Initial tests returned **151 passed / 20 failed** because the shared legacy evaluator correctly rejected 129,536 fights. The exact schema-2 envelope resolved this; the intermediate 200-test suite passed. The legacy cap was not raised.
2. The first real-source audit returned **exit 2 in 13.873 s**, before combat, seed allocation or audit-bundle creation. Registering all 253 recipes as discovery references exceeded the unchanged limit of 112. The amendment grouped input validation and added its regression case. Original source/runtime/comparison snapshots and failure log remain untouched; corrected verification used new `*-v2` paths.

The corrected audit returned **exit 0 in 13.542 s**, or **13.595 s** including its post-run frozen-input recheck. Its internal trace snapshot ends at **12.272 s**, before copying/writing the audit bundle. The command timer includes that work. Retained audit output is **62,519,860 bytes / 16 files**. These are validation timings, not combat throughput measurements.

Independent [preservation checks](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/sealed-preservation.json) took **41.100 s** and matched **103,193 v19 campaign files**, **892 v19 work files**, **104,971 original performance evidence files**, and **103,386 parity-closure evidence files**. All seed reservations remain exact. No sealed package was changed.

The pre-sealing [resource snapshot](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/resource-summary.json) records approximately **877 MB** of new builds/source/audit evidence, below this task's 1-GiB bound. Combined new evidence across this and both performance tasks remains below 4 GiB. [Final verification](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/final-verification.json) records final bounds, source/document hashes and the evidence index. Logical file bytes follow the existing infrastructure; allocation/metadata overhead was not separately measured. Cumulative performance diagnostics remain **76 fights**, and recorded diagnostic/audit workload remains below 1,800 seconds. Compilation/correctness-test time is excluded from that workload limit.

## Reproducible commands and scope

Executed from the repository root:

```powershell
$w = 'TestResults/balance/tower-portfolio-confirmation-implementation-20260914'

./build/run-tests.ps1 -Configuration Release -ArtifactsPath "$w/build/tests-v2" -Filter 'FullyQualifiedName~BalanceHarnessTowerPortfolioConfirmationTests|FullyQualifiedName~BalanceHarnessTowerAllocationConfirmationTests|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerFeedbackBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerStorageTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'

dotnet build "$w/compiler-v2/CapturedBalanceHarness.csproj" -c Release --artifacts-path "$w/build/captured-v2"
& "$w/freeze-audit-v2.ps1"
& "$w/audit-frozen-v2.ps1"
& "$w/verify-preservation.ps1"
& "$w/finalize.ps1"
```

The audit script runs the captured `execution-v2/BalanceHarness.dll` with exact arguments stored in `audit-definition-v2.json`, after checking frozen hashes. The original scripts/definitions/logs retain the earlier invocation. These are provenance commands; sealed evidence must not be overwritten. A new invocation needs a new evidence directory and input snapshot. No `-prepare`, `-run` or completed-study `-verify` command was executed in this task.

The initial sandbox build could not read the existing user NuGet configuration; tool-level access resolved it without changing configuration. Captured-gameplay builds passed with zero warnings. The final broader checkout build emitted 34 warnings outside the changed files. No required command remains blocked.

Changed code: `TowerPortfolioConfirmation.cs` for the new family/schedule/quality contract; `TowerPortfolioConfirmationArchive.cs` for source binding, scope checks and audit; `TowerPortfolioConfirmationRun.cs` for prepare/check/run/reconstruction; CLI registration in `Program.cs`; the narrow schema-2 envelope in `TowerBalanceEvaluator.cs`; and the new confirmation tests. Active README and discovery/replication/acceptance handoffs point here. The checkout began clean at **8f2f139af60bbe524a722ce2570df1b495e38fcc**. One unrelated document appeared concurrently and was preserved; the [concurrent-change receipt](../TestResults/balance/tower-portfolio-confirmation-implementation-20260914/concurrent-checkout-changes.json) separates it from this task.

## Remaining boundary

The contract and real-source import/materialization checks are verified. **Real preparation, execution and completed-study reconstruction remain unrun**, because preparation would allocate fresh values and execution would start the separately prohibited confirmation workload. Synthetic test results are not balance evidence. Future execution must first freeze its actual plan, latest exclusions, master seed/shared schedule, captured executable and explicit time/storage limits, then pass prepared verification.

V19 remains **VerifiedCapacityExceeded**, all **253 recipes retained**, confirmation **NotRun**, reliability **Unresolved**, adoption **Hold**, ordinary/joint **NotRun / NotRun**. No gameplay/content, Kharad, old-cap, migration, production configuration, deployment or catalog/default promotion changed. The next action is the [separate study freeze](Tower-Coverage-Replication-Plan.md#next-scoped-work), not a discovery rerun or automatic confirmation launch.
