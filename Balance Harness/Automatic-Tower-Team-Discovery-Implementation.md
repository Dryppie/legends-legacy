# Automatic Tower team discovery: contracts, generation and acceptance

**Competitive search update — 12 September 2026:** [stronger Kharad searches](Tower-Competitive-Build-Search-Review.md) confirmed new teams at **948/1,000** and **1,000/1,000** on the previously passing content. Its earlier scoped Pass is superseded for the expanded build portfolio. Retained-build improvement and the history-capacity extension are implemented; the fresh quality audit and separate linked calibration are documented in the new review. Near-optimality remains unestablished.

**Progression audit and calibration — 11 September 2026:** [floors 2–5 review](Tower-Progression-Floors-2-to-5-Review.md) records independent four-Essence searches on floors 2–4, their separately confirmed linked Health/Power calibration against every known breach, and Kharad's fresh **20.9%** strongest-control check. Compatible controls and calibrated top builds persist for future searches. Floors 6–11 and practical acquisition coverage remain open.

**Fresh search and Kharad follow-up — 11 September 2026:** [new independent searches](Post-Calibration-Tower-Team-Search-Review.md) confirmed Garran's strongest saved team at **34.8%** and found a stronger Kharad team at **59%**, triggering a separate calibration. The [expanded-portfolio follow-up](Kharad-Expanded-Portfolio-Calibration-Review.md) applied another **8% to both Kharad Health and Power**; its strongest of 122 parties confirmed at **25.25%**, and the full family passes. At that stage, main-dashboard searches retained **4 floor-1 / 6 floor-5 controls**. Broader progression, practical Essence access and further independent ceiling searches remain open.

**First retained-build calibration — 11 September 2026:** compatible saved builds now enter new Tower Lab searches automatically as fresh benchmark controls; completed future studies retain their generated finalists. The [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied linked Health/Power factors of **1.06 to Garran** and **1.56 to Kharad** relative to their pre-campaign inputs. The strongest of 57/106 retained parties confirmed at **34% / 24.25%**, respectively, and both frozen families pass the 10–50% policy. This uses the declared full Essence pool including Rare Essences; this historical result is followed by the fresh searches and expanded calibration above.

**Original independent-team pilot results — 11 September 2026:** increments 1–5 of [Automatic Tower team discovery](Automatic-Tower-Team-Discovery-Plan.md) are complete. The [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) found a floor-1 generated primary at **859/1,000 wins (85.9%)** and five floor-5 generated finalists at **972/972 each**. Independent search found viable builds; both declared cohorts fail the 50% balance ceiling. These pilots use the full 80-Essence pool with hypothetical ownership, including Rare Essences. The retained-build follow-up above now calibrates these declared budgets; practical Essence access and the wider progression curve remain separate coverage. Wider progression balance remains unestablished; no bosses were tuned by these pilots.

The original implementation used Garran at Health **1.6764** / offense **1.6368** and changed no gameplay content. The later [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied the current Health **1.776984** / offense **1.735008**. His earlier **291/1,000** user-party result remains historical evidence for its original setting and budget.

## Contract and isolation

[`TowerBossDiscoveryContract.cs`](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) introduces a separate `TowerBossDiscoveryDefinition`, schema 3. Legacy boss-search schema-1/2 readers, execution, cost formulas and report reconstruction retain their existing semantics. Other harness workflows also use a schema number 3; their definitions are different contracts and are not interchangeable.

The new definition freezes:

- Target floor, actual production `RequiredSlots`, character/gear budget, starting time and budget purpose.
- One to four distinct equipment contexts, each with every character explicitly represented. Templates have empty Essence vectors and stable neutral character IDs; the factory does not load historical recipes or repeat the user's example across floors.
- Legal Essence IDs and source families, with optional owned-copy limits. Within-character family restrictions apply; repeated Essences across different characters remain legal. An omitted owned-copy entry means zero copies when an owned inventory is supplied; `null` means hypothetical ownership.
- Generation methods, restart seeds, bounded attempts, target-strength objective, stage allocations, all explicit combat schedules, exclusions and a hard cap.
- Separate benchmark references and supplied starts, reference evidence hashes, complete content hashes, settings hash and execution hash.

`TowerBossDiscovery.Create(...)` builds an independent definition from a content root, explicit budget/equipment contexts, master seed and caller-supplied seed exclusions. It reads current production Essence families and floor size, validates actual equipment through production materialization, and creates schedules before inspecting references. It performs no combat. Custom definitions can then restrict the pool or ownership and must be validated again.

`GenerationInputs(...)` returns a detached input object containing the budget, allowed pool, versioned generation policy, discovery schedules, shortlist capacity, equipment selections and source hashes. Reference recipes, source labels, reference fitness, actor identity vectors and selection/confirmation schedules are absent. Increment 2 verifies unchanged inputs, proposals, scores and discovery shortlists when references are added, removed or reordered, including three matching production-combat discovery runs.

Every generated character position can receive any legal ordered Essence selection. Preparation holds actor identities fixed behind the generation boundary. Equipment labels impose no Essence roles. Reordering a candidate's Essences remains a distinct tested loadout.

Independent mode requires zero supplied starts. `improve-supplied` requires explicitly named starts matching registered references. The provenance validator checks parent existence, operator arity, generation seed/method and propagation of reference ancestry through mutation and recombination; missing, cyclic or relabeled ancestry is rejected. The explicitly versioned `retained-teams-v1` generator now implements this separate workflow; legacy reserved definitions require explicit conversion. See the [competitive search review](Tower-Competitive-Build-Search-Review.md) for operators, CLI commands and verification.

Exact reference recipes are deduplicated after normalizing equipment order and equivalent explicit/default identity vectors. Distinct actual actor identities remain distinct recipes. References must match the target floor, full party and context's fixed equipment budget; the user's floor-1 party is accepted only through explicit registration.

## Progression and costs

`budgetPurpose` defaults to `intended-progression`. Known checkpoints require four slots at floor 1, five at floor 5, six at floor 10 and at least seven at floor 11. A legal alternative budget requires `diagnostic` at those checkpoints. This is a reporting/experiment guard, not a gameplay slot gate: lower-budget parties remain legal combat probes and their successes remain visible. Intermediate transitions and gear are explicit modeling assumptions, not newly approved progression targets.

Default target-only cost with one equipment context:

| Stage | Allocation | Maximum combats |
| --- | --- | ---: |
| Discovery | 2 methods × 3 generation seeds × 128 candidates × 8 paired combat seeds | 6,144 |
| Selection | 16 shortlisted generated parties × 64 seeds | 1,024 |
| Generated confirmation | Up to 5 frozen parties × 1,000 seeds | 5,000 |
| Reference confirmation | Each deduplicated reference × 1,000 seeds in its declared context | 1,000 × C |
| Diagnostics | Up to 8 candidates × 32 seeds | 256 |
| Replay reserve | Explicit allowance | 200 |
| Total | C is the actual reference count | **12,624 + 1,000 × C** |

Each extra equipment context adds its full discovery, selection, generated-confirmation and diagnostic costs. Reference cost follows its declared context; the replay reserve is charged once. Every plan must fit its explicit cap, at most 100,000 combats. Definitions support 128 candidates and 1,000 confirmation samples without inheriting legacy 100-candidate/100-confirmation or all-floor multiplication limits.

The contract supports up to **96 references** after the [fixed-pilot preflight correction](Automatic-Tower-Team-Pilot-Review.md). The combat cap still applies: a large family may require a smaller, explicitly predeclared confirmation schedule. The floor-5 pilot retains all 90 compatible historical references and uses 972 samples per confirmation cell, reserving 99,964 combats. This decision precedes all outcomes. Complete definitions exceeding Tower Lab's 2 MB import limit can be prepared and executed through the CLI.

Combat schedules are disjoint between stages/contexts and exclude the declared historical union. Methods and restarts share the same context/stage schedule, so restarts do not multiply independent confirmation samples. Direct factory/CLI callers must supply the complete exclusions independently of reference selection, including original user-party, offense-only and linked Garran tuning ledgers. Tower Lab now imports built-in history and recognized ledgers beneath its configured results root, records their sources/hashes in the preview, and rechecks history before starting. Its bounded scan does not cover every external or deeply nested campaign: review the [scanner limits](Automatic-Tower-Team-Lab-Review.md) and add missing exclusions explicitly. Neither workflow reserves seeds against concurrent external jobs.

## Available commands

Build the harness, then validate a previously authored schema-3 definition without running a search:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-discovery-prepare --definition discovery.json --content-root LL/src/API/API.LL --output TestResults/balance/discovery-preparation-new
```

The new output contains `definition.json`, `cost.json` and, in independent mode, `generation-inputs.json`. Existing outputs are rejected. This command requires a complete definition and performs preflight only. To execute independent discovery, use `tower-boss-discover --definition <schema-3-json> --output <new-directory> [--content-root <API.LL-directory>]`; reconstruct it with `tower-boss-discovery-verify --run <directory>`. See the [generation guide](Automatic-Tower-Team-Generation-Review.md) for costs, artifacts and limits. A definition's execution hash must match the producing build.

For standalone acceptance, author a `TowerBalanceDefinition` before confirmation. It has schema 1, an ID, `intervalPolicy: "bonferroni-wilson-95-v1"`, frozen content/settings/execution hashes, required cohorts, cells, exclusions and a maximum battle count. Each cohort identifies its floor/budget, party size, context, `equipmentBudgetHash` and `purpose`. Each cell declares a unique ID, cohort ID, `generated`/`reference` role, exact normal `TowerScenario` including the complete seed schedule, and minimum samples. Cohort equipment hashes use `TowerBossDiscovery.EquipmentBudgetHash(...)`.

Run each declared scenario through the existing `tower --scenario` command and create a source mapping, for example:

```json
[
  { "cellId": "generated-primary", "runDirectory": "runs/generated-primary" },
  { "cellId": "user-reference", "runDirectory": "runs/user-reference" }
]
```

Relative run paths resolve against the mapping file's directory. Cell IDs must match the frozen definition exactly. Then:

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-balance-evaluate --definition confirmation.json --sources sources.json --output TestResults/balance/tower-assessment-new
```

[`TowerBalanceRuns.cs`](../LL/tools/BalanceHarness/TowerBalanceRuns.cs) reads saved normal-Tower archives, verifies their recorded files/reports and reconstructs preparation against their retained content. It does not accept summary win counts or execute more battles. Output includes the frozen definition, source mapping, verified trial evidence, `assessment.json` and `assessment.md`. Exit codes are **0 Pass, 1 Fail, 2 Invalid, 3 Inconclusive**.

The standalone evaluator's source archives must remain available; its assessment output is not a self-contained combat archive. Integrity checks establish recipe/content/seed consistency, not that a human-authored protocol predates its results. Retrospective evaluation remains retrospective. For integrated execution, `tower-boss-study --definition <schema-3-json> --output <new-directory> [--content-root <API.LL-directory>]` freezes the shortlist before selection and the generated finalists/reference family before confirmation, then runs fresh confirmation and acceptance once. `tower-boss-study-verify --run <directory>` reconstructs all stages without new combat. This study archive retains producing assemblies/dependencies, content, settings and detailed replay audits. See the [confirmation review](Automatic-Tower-Team-Confirmation-Review.md) for the explicit selection policy, source deduplication, accounting and limits.

## Acceptance behavior

[`TowerBalanceEvaluator.cs`](../LL/tools/BalanceHarness/TowerBalanceEvaluator.cs) assesses each declared floor/budget/equipment-context cohort separately. Every included party must support an upper win-rate bound of at most 50%; at least one must support a lower bound of at least 10%. A zero-clear control can coexist with a viable party. A high-clear outlier cannot be hidden by averaging weaker parties.

The report shows pointwise 95% Wilson intervals and approximate Bonferroni-adjusted Wilson intervals using the fixed number of declared recipe/context cells. Missing cells do not shrink that family. Draws count as non-wins. Any observed rate above 50%, including 10/10, blocks acceptance; 1/10 and 5/10 are inconclusive. If every party's adjusted upper bound is below 10%, the cohort fails; if viability remains uncertain it is inconclusive. Missing, duplicate, replaced, corrupt or incomplete evidence is invalid, while any raw above-ceiling observation remains visible.

An aggregate pass covers only the declared cohort set. Every declared context must meet its own viability requirement. All requested progression floors must be declared to support a progression-wide conclusion; the evaluator does not invent omitted floor coverage. Diagnostic findings remain explicitly labeled and cannot establish intended progression. Keep diagnostic experiments separate from the intended-progression acceptance family. Bounded search provides no guarantee about unsearched parties.

Search ranking remains a strength objective, without a 50% cap or a target of 30%. Balance assessment, search quality, completed execution and archive integrity remain separate conclusions. Independent Tower Lab studies invoke this evaluator after fresh confirmation; legacy searches retain their prior semantics; the separate starter 50–90% evaluator is unchanged.

## Verification and next increment

Backend verification uses `build/run-tests.ps1`. Increment 1 passed 261 relevant regression tests, followed by all 45 contract/evaluator tests after its final source-path fix; those logs remain in `TestResults/balance/tower-discovery-contracts-20260911/`. Increment 2 passed 273 relevant tests in `TestResults/balance/tower-independent-generation-20260911/`, and increment 3 passed 286 in `TestResults/balance/tower-staged-confirmation-20260911/`. These are separate historical verification runs.

**Historical increment-4 verification: 295 relevant tests passed**, plus desktop/mobile browser checks for preview, execution, cancellation, results, exports and replay. Its build completed with zero errors and five existing warnings in unrelated tests. Evidence is retained in `TestResults/balance/tower-team-lab-20260911/`; the [Tower Lab review](Automatic-Tower-Team-Lab-Review.md) records the commands and scope. The bounded browser studies verify implementation behavior and do not establish floor balance.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~BalanceHarnessGoal'
```

Tests cover progression checkpoints and diagnostic budgets, full-party production preparation through floor 15, reference isolation, provenance, owned-copy/family legality, stage costs, strict serialization, uncertainty boundaries, independent interval fixtures, verified archive evaluation and legacy boss-search reconstruction. Two older test assumptions were corrected during regression: hexadecimal prefixes now use ordinal comparison, and historical-reference content checks account for later local tuning instead of assuming the old content hashes still match.

Independent generation, staged confirmation, Tower Lab integration and the [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) are complete. The pilots establish viable generated builds but fail both tested balance cohorts. The reference-cap correction passed 296 relevant tests; see the pilot review for exact evidence and limits. The retained-build campaign above now calibrates the full-pool budgets requested by the user; practical acquisition limits remain separate coverage. No migration, production configuration change, deployment or new dependency is introduced.

The latest [progression batch](Tower-Progression-Floors-2-to-5-Review.md) passed **25 relevant retention, Tower Lab and World Tower tests both before and after local application**, plus 140 exact current-content reports, 60 unaffected-floor pairs and 32 detailed replays. These targeted checks used the captured compiled engine and do not claim a rebuild or full-suite check of concurrent source changes. Both evidence packages are sealed.

The [competitive search increment](Tower-Competitive-Build-Search-Review.md) now allows **1,000,000 cumulative historical exclusions** and **32 MiB imports** in both the browser and server, while retaining the separate 100,000-combat study/confirmation cap. It adds retained-build improvement, equal-budget search comparisons, fresh quality audits and complete-portfolio calibration across explicitly bounded verification partitions. Floor-6–11 coverage remains open; stronger search evidence takes priority before competitive acceptance.

## Increment 1 changed files

| Area | Files and purpose |
| --- | --- |
| Offline tool | `TowerBossDiscoveryContract.cs`, `TowerBalanceEvaluator.cs`, `TowerBalanceRuns.cs` and `Program.cs`: new contract, standalone verified assessment and two commands. |
| New verification | `BalanceHarnessTowerBossDiscoveryContractTests.cs` and `BalanceHarnessTowerBalanceEvaluatorTests.cs`: 45 contract, numerical, production-preparation and CLI/archive cases. |
| Existing verification | `BalanceHarnessTowerBossDiagnosticsTests.cs` and `BalanceHarnessTowerBossValidationReferenceTests.cs`: ordinal hash-prefix assertions and accurate historical-content expectations after tuning. |
| Documentation | This guide, `Automatic-Tower-Team-Discovery-Plan.md`, `Tower-Balance-Acceptance-Policy.md`, `Boss-Specific-Essence-Loadout-Plan.md`, `Balance-Harness-Plan-With-Benchmarking.md`, `Essence-Loadout-Search-Plan.md` and the harness `README.md`: current completion status, commands, limits and next increment. |

Other existing working-tree changes, sealed experiment packages and the user-authored party fixture were preserved.
