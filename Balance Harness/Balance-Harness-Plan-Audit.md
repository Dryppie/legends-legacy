# Balance harness plan audit — 8 September 2026

The harness follows the original idea: fixed scenarios, real combat execution, saved results, goal evaluation and baseline comparisons. At the time of this audit, it was an idle-progression measurement tool with several bespoke investigations, rather than the broad balance and benchmarking system originally outlined. Most scope reductions were documented decisions. The audit identified four implementation gaps, including a reproduced failure to read historical evidence.

This audit compares the original plan at Git commit `a34819196` with the 8 September working tree, including uncommitted harness and Combat Styles changes. It also uses the expanded plan and its linked policy, cohort, progression, entry, pressure, band, local-validation and handoff reviews. Documented user decisions are treated as intentional scope changes, not defects.

## Follow-up status — 9 September 2026

The findings, line references, test counts and recommendations below describe the original audit snapshot. Subsequent work is recorded separately:

- [Starter path acceptance](Starter-Path-Acceptance.md#archive-compatibility-and-preservation) records the archive-contract correction and successful evaluation of the real historical 14-file archive. The CI filter now includes `CombatStyleHarness`; that filter change does not establish independent Combat Styles parity.
- Both selected starter checkpoints now have accepted local baselines. The [combined regression](Starter-Regression-Review.md) reproduced their 52,000 saved trial identities on the retained 8 September build, with all four primary goals passing. Earlier Inconclusive evidence remains unchanged.
- The [verified local package](Starter-Baseline-Package.md) retains those baselines, their supporting evidence and exact executables. All 428,158 files restored, four saved suites passed their existing checks and eight replays matched on the original runtime. This addresses recovery for the selected retained references; it does not reconstruct missing older builds or guarantee reconstruction from dirty source. Off-device storage is deferred by user choice.

This documentation follow-up does not reassess the independent Combat Styles parity or early experiment-identity findings. Hosted verification of unpublished changes, a hosted gameplay regression gate, broader coverage and runtime benchmarking remain separate work. See the [main plan](Balance-Harness-Plan-With-Benchmarking.md#phase-2-integration-and-follow-up) for current priorities.

## Findings at audit time

### 1. P1 — Adding Combat Styles breaks historical archive evaluation and comparison

**Evidence:** [OfflineContent.cs:32](../LL/tools/BalanceHarness/OfflineContent.cs#L32) adds `combat-styles/combat-styles.v1.json` to the mandatory snapshot list. [RunBundle.cs:116](../LL/tools/BalanceHarness/RunBundle.cs#L116) requires every archived manifest to have exactly the current list. Both old and new manifests still use schema version 1. `SavedSuite.Read` calls this check before evaluation or comparison.

The existing starter discovery archive has the former 14 files. Running the current executable against it fails with `InvalidDataException: Unexpected content snapshot files.` and exit code 2. This is an archive-reading failure, not a replay rejection caused by different binaries, nor an actual balance failure.

Reproduced command:

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll evaluate --run TestResults/balance/blood-grove-starter-reference/discovery --goals LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json --output TestResults/balance/plan-audit-historical-evaluation-20260908
```

This contradicts historical comparisons in the original plan and the current plan's explicit promise that reading old measurements does not require executing their old assemblies. It also breaks the documented workflow for re-evaluating retained starter/pressure evidence.

**Recommended correction:** version the content-snapshot contract. Preserve an exact, safe allowlist for the legacy archive format and a separate contract for new style-enabled bundles. Keep checksum validation strict and replay identity checks separate. Add regression coverage using an actual legacy-format archive, rather than constructing all historical test evidence with the latest file list. Do not add current content to old evidence or rewrite its accepted hashes.

### 2. P2 — Replay fingerprints are not enough to recover the original executable

**Evidence:** [RunBundle.cs:8](../LL/tools/BalanceHarness/RunBundle.cs#L8) records runtime, operating system, architecture and five assembly hashes. Its manifest records input/content hashes. It does not record Git revision, dirty state, the relevant source patch/snapshot, SDK or explicit build configuration. Replay then requires the exact original executable identity at line 107.

The current plan's section 11 explicitly requires source revision and recoverable dirty-source provenance. The original plan also calls for game/version reproducibility. The README acknowledges that source patches and binaries are not archived, so this is a known implementation limitation rather than a hidden claim of portability.

In this heavily modified checkout, an assembly hash identifies what ran but does not let a later reviewer reconstruct it. Rebuilding after further edits can leave old runs readable in principle but no longer replayable, even after finding 1 is fixed. Relevant reports reference ignored local bundles, and the CI artifacts expire after seven days; neither provides durable executable retention.

**Recommended correction:** capture source revision, dirty status and a retained relevant source snapshot or patch including required untracked source files; record SDK/build configuration and a durable artifact reference. Define retention for accepted evidence and the matching runnable build. Exclude secrets and unrelated workspace data from capture.

### 3. P2 — Combat Styles bypasses the normal resolution path without matching parity coverage

**Evidence:** [OfflineContent.cs:140](../LL/tools/BalanceHarness/OfflineContent.cs#L140) constructs `CombatSetupService` without `ICombatStyleService`. `FreezeCombatStyle` separately constructs Channeled Essence options and style snapshots. [IdleBattleRunner.cs:31](../LL/tools/BalanceHarness/IdleBattleRunner.cs#L31) injects that snapshot after preparation and marks it captured.

The harness shares the compiler and `CombatStyleRules`, which is good. Nevertheless, [CombatStyleHarnessTests.cs:10](../LL/tests/EssenceSystem.Tests/CombatStyleHarnessTests.cs#L10) compares two runs through this same harness path; the other test covers validation/tampering. These tests do not compare against independently prepared normal idle combat using `CombatStyleService.ResolveAsync`. The broader `BalanceHarnessTests` parity matrix covers unstyled controls and equipment Fury, not the new Combat Styles selection path. Equipment Fury is a separate feature.

No gameplay divergence was demonstrated here. The finding is a missing prerequisite for trusting the new fixture as gameplay evidence: the plan requires parity across setup as well as execution. In addition, the workflow's `FullyQualifiedName~BalanceHarness` filter does not include the class named `CombatStyleHarnessTests`.

**Recommended correction:** extend independent normal-idle parity to Bastion and Conduit, Channeled Essence identity/order, representative refinements/upgrades, and progressed active abilities. Include that class in the harness CI filter. Keep style fixture results advisory until this is verified.

### 4. P2 — Earlier multi-run investigations do not freeze the whole experiment identity

**Evidence:** [build/investigate-blood-grove.ps1:114](../build/investigate-blood-grove.ps1#L114) invokes each progression variant against the live API content directory. It checks trial pairing and battle counts, but does not verify a common combat-content/settings identity across the twelve runs. Its economic files are copied separately.

[BloodGroveEntryExperiment.cs:144](../LL/tools/BalanceHarness/BloodGroveEntryExperiment.cs#L144) also snapshots each seed set from the live directory. Lines 154–155 compare content hashes and executable identity between sets, but omit the selected combat settings from `appsettings.json`. Those settings are intentionally outside the copied content-file list. A threat/cadence edit between sets can therefore pass this check, despite changing the experiment.

This is a static finding; I did not alter production settings during an experiment. Each individual suite captures its own evidence, but that alone cannot establish that differences across variants or confirmation sets are caused only by the declared intervention. The current plan says not to load changing content during a run. Later pressure/local/handoff protocols already demonstrate stronger source/settings checks.

**Recommended correction:** snapshot content and selected settings once per investigation and run every variant from that private snapshot, with verification against the declared plan. Retain the early investigations as historical protocols, but give them the same identity safeguards before using them for new causal comparisons.

## Intentional scope changes and unfinished commitments at audit time

### The regression machinery exists; the ongoing regression workflow is unfinished

The original flow ends with historical comparison and regression detection. `baseline accept`, `compare` and `evaluate` implement that machinery, with explicit acceptance reasons, immutable evidence hashes and separate invalid/fail/inconclusive results.

However, [smoke-balance.ps1:74](../build/smoke-balance.ps1#L74) creates both runs using the same current executable/content, then accepts a disposable repeatability reference. CI runs this for the original controls and First Hunt. This detects instability and broken tooling, but a deterministic balance regression relative to the preceding revision can still pass. No reviewed gameplay baseline manifest is tracked; the selected starter baseline remains pending, and its enforced policy is not a hosted gate.

This is accurately disclosed in the current plan and policy review. It should remain labelled an unfinished Phase 2 commitment, not a completed cross-revision balance gate. After archive compatibility and provenance are fixed, retain a reviewed compatible reference and add an advisory cross-revision comparison before deciding on enforcement.

### Runtime benchmarking remains absent

Only aggregate wall time is recorded by `SuiteBundle` and the smoke script. There is no stage timing, cold/warm separation, allocation/peak-memory measurement, controlled throughput comparison or runtime benchmark history. The existing database-backed idle measurement script is a different workflow and is correctly kept separate.

This is an unimplemented part of the original benchmarking plan. Current documentation acknowledges it, but the phased acceptance table would benefit from giving it an explicit deliverable. It need not block gameplay checks, and machine-dependent timings should remain separate from balance assertions.

### Investigation-specific code has grown ahead of general coverage

The public CLI now has entry, coarse pressure, fine pressure, local Blood Grove validation and Crystal Creek handoff commands. Their code pins area IDs, content versions, reward expectations, seed schedules and policies. These constraints make the individual protocols reviewable and prevent silently changing completed experiments. They also mean a new balancing question tends to require more custom C# or PowerShell orchestration.

The current plan explicitly acknowledges that these are bounded steps toward phases 4/5 before phase 3. The documented priority is one viable starter path. This is deliberate sequencing, not unauthorized scope expansion. For a solo developer, the next similar investigation is a sensible point to introduce a small declarative experiment contract over the existing suite runner, rather than adding another permanent area-specific command. Preserve the existing historical protocols.

### Broader content and analysis are deferred, not accidentally missing

Tower parties and `RequiredSlots`, alternative party compositions, boss/dungeon/PvP adapters, state carryover, general legal build generation, Essence contribution/synergy analysis, rankings and automatic tuning remain open. Current idle trials condition on fixed spawns and fresh encounters; they do not measure area-wide win rates or acquisition time. The reports generally state these limitations clearly.

The old `AbilityBalanceSimulator` does not fill that gap: [its engine options](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityBalanceSimulator.cs#L489) still omit `StartActiveAbilitiesOnCooldown`, leaving the engine's `false` default, while the idle harness explicitly uses `true`. The current plan already flags this distinction. Keep those simulator rankings separate from normal-gameplay evidence until preparation/rule parity is established.

## Coverage against the original plan at audit time

| Original sections | Status at audit time |
| --- | --- |
| 1 and 15 — Goals, constraints and assertions | Implemented for named idle cells. Primary/guardrail/diagnostic roles and draft/enforced policies are distinct. Only the selected starter has reviewed enforcement. |
| 2 and 9 — Scenarios and encounter profiles | Real fixed idle encounters with one to three enemies; broader content and mechanic coverage deferred. |
| 3 — Benchmark suite | Fixed control, First Hunt, starter, handoff and style fixtures exist. Runtime benchmarking remains open. |
| 4 and 5 — Essence simulator and scoring | Per-battle statistics exist; no general contribution, synergy or overall scoring layer. Legacy simulator is not gameplay-parity evidence. |
| 6 — Character profiles | Materialized legal equipment/Essence fixtures and progression variants exist. Entry/reference/optimized coverage and attainable budgets are incomplete. |
| 7 — Build generator | Hand-authored fixtures and bounded armor/style substitutions. No general random/archetype/optimized generator. |
| 8 — Party creator | Not implemented in this harness. |
| 10 and 21 — Real simulator and parity | Production idle preparation/executor reused; extensive unstyled/equipment-style parity tests. New Combat Styles parity gap remains. |
| 11 and 18 — Matrix and orchestration | Validated bounded matrices, paired seeds, sequential fresh battles, partial artifacts and reports. Experiment orchestration is increasingly bespoke. |
| 12 — Metrics | Clear rate, duration distributions, terminal health, raw participant/ability statistics and compact telemetry. General effective-healing/control attribution and mechanic metrics remain open. |
| 13 — Baselines/history | Explicit acceptance and paired comparison implemented. Historical file-set regression, provenance/retention and routine cross-revision integration need attention. |
| 14 — Outlier/dominance detection | Manual scorecard/paired-investigation review; no general dominance or mandatory-composition detection. |
| 16 and 17 — Parameters/search | Production content remains authoritative; bounded private regional-offense overlays and fixed candidate selection exist. No general optimizer or automatic production promotion. |
| 19 — Reporting | JSON/Markdown scorecards, comparisons, policy evaluations and experiment reports. Essence/party rankings and Tower curves deferred. |
| 20 — Reproducibility/versioning | Frozen inputs/content, stable seeds, deterministic IDs and binary fingerprints exist. Archive compatibility and recoverable source provenance are incomplete. |

The statistical discipline is largely aligned: fixed confirmation budgets, separate discovery/confirmation seeds in recent protocols, paired differences, Wilson intervals, explicit unavailable uncertainty, and no promotion of the Raven result to Pass. The widened 50–90% band is recorded as an explicit user decision with the old policy retained. It is not evidence of automatic target fitting. The result remains Inconclusive, and this audit does not recommend rerunning until it passes.

## Documentation corrections identified by the audit

- The current plan says “The shipped goals are all `Draft`” at line 330. The selected Blood Grove policy is now `Enforced`; narrow that sentence to the original cohorts.
- The README artifact table still says 14 catalog files. New bundles now contain 15. Document the new format together with the compatibility correction, rather than merely updating the count.
- Historical test counts and previously blocked builds belong to specific increments. This audit's current verification is below; it does not replace historical experimental evidence.

## Verification and review boundaries

- Ran `build/run-tests.ps1` with the plan's harness, preparation, equipment, telemetry, outcome and legacy-simulator filters, plus `FullyQualifiedName~CombatStyleHarness`: **125 passed, 0 failed, 0 skipped**. Release build: zero warnings/errors.
- The initial sandboxed build could not read the user NuGet configuration. Retrying the same repository runner with approved access succeeded; no verification command remains blocked by that issue.
- Ran both current smoke workflows with `-NoBuild`: **72 control battles plus 216 First Hunt battles**, all valid. Both comparisons had zero changed gameplay records, both detailed replays matched, and both draft evaluations were advisory/inconclusive as expected at three samples per cell.
- Independently attempted historical starter evaluation: **failed with exit 2**, reproducing finding 1. Failure evidence is retained in `TestResults/balance/plan-audit-historical-evaluation-20260908/failure.json`.
- Smoke evidence is retained in `TestResults/balance/plan-audit-controls-20260908` and `TestResults/balance/plan-audit-first-hunt-20260908`. The scripts created only their disposable workflow references; no reviewed gameplay baseline was promoted.
- Did not rerun full confirmation/search budgets, the full backend suite, frontend tests, hosted CI or human playtesting. No production code was changed, so those broader executions were not needed for this review. Historical experimental claims were reviewed as recorded evidence, not represented as newly reproduced results.

Only this audit document was added by the review, alongside ignored verification artifacts. Existing working-tree edits were preserved. No gameplay policy, configuration, migration, deployment or external environment was changed.

Original recommended order: repair historical archive reading; retain recoverable execution provenance; close Combat Styles parity and older experiment identity gaps; finish an advisory cross-revision baseline workflow. Then resume broader coverage and performance benchmarking according to the intended starter-path priority. The dated follow-up above records subsequent progress without rewriting this audit's evidence.
