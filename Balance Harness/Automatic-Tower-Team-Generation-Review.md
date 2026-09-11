# Independent Tower team generation

11 September 2026. This review records increment 2 of the [automatic discovery plan](Automatic-Tower-Team-Discovery-Plan.md): independent complete-party generation and discovery reconstruction in the offline `LL/tools/BalanceHarness`. The subsequent [increment 3 review](Automatic-Tower-Team-Confirmation-Review.md) now documents implemented selection validation, fresh confirmation and acceptance. [Tower Lab integration](Automatic-Tower-Team-Lab-Review.md) is now implemented; the [fixed floor-1/floor-5 pilots](Automatic-Tower-Team-Pilot-Review.md) are now complete.

Garran remains at Health **1.6764** / offense **1.6368**. His historical **29.1%** result still describes the supplied party at its recorded budget. This coding increment does not claim a newly confirmed viable generated team or broader Tower balance.

## Implemented generation policy

`independent-teams-v1` is explicitly frozen in the schema-3 generation contract and execution scope. The generator receives detached legal budgets, equipment contexts, the allowed pool, discovery schedules, shortlist capacity and source-derived mechanics. It cannot access benchmark vectors, imported fitness, supplied starts, actor identity vectors or confirmation outcomes. The normal preparation adapter owns identities and keeps them fixed across candidate changes.

Every production `RequiredSlots` position is generated separately; no five-character team is automatically repeated on larger floors. Source-family rules apply within each character. Cross-character repetition, including repeated support Essences, remains legal unless explicit owned-copy limits prevent it. Equipment and character budgets stay fixed; gear labels do not assign Essence roles.

| Method | Construction and refinement |
| --- | --- |
| `random` | Fresh uniform legal ordered parties. Integer completion counts weight families by their number of available Essence variants; chosen Essences are shuffled into a uniform order. With owned limits, complete-party rejection preserves the conditional legal distribution. |
| `constructive-joint` | The first quarter of its evaluated candidates are independently constructed complete parties: 32 of the default 128. Generic damage, sustain, protection, denial, pressure and boss-relevant add-handling capabilities influence randomized choices across every character. Subsequent evaluations refine generated parents, with fresh construction every fourth refinement attempt. |

The constructive method cycles single substitution, double substitution, Essence ordering, coordinated cross-character substitution, whole-character replacement and recombination. Structural enabler/consumer proposals retain their originating mechanism and compatibility label. Same-owner hypotheses cannot be placed across characters. Pair substitutions replace an existing member of the relevant family when necessary, avoiding artificial duplicate-family violations.

Capability weights are proposal heuristics, not Essence scores. The saved mechanics inventory retains effect targets, subject/trigger conditions, stacking, timing, upkeep and resource details. Exact recipient eligibility, effective triggers and competing costs are resolved by real whole-party combat; unverified cross-character links remain labeled hypotheses. This increment does not claim a complete analytical model of resource feasibility or causal synergy.

Parent selection retains the strongest four measured recipes and up to four exploration recipes with different capability patterns or ordered loadouts. The primary objective always comes first: **worst-context target win rate**, then lower guardian health, greater party survival and shorter winning duration, with a stable recipe-ID tie-break. There is no 30% target or 50% cap on search strength. An above-ceiling generated team remains visible.

Every attempt records its operator, generation seed/method, generated parents, exact ordered recipe when available, interaction hypothesis and evaluated/duplicate/illegal outcome. Duplicate or illegal proposals consume attempts and execute no combat. Every distinct accepted candidate receives the full paired schedule. Methods and restarts each pay for their own battles; shared seeds and repeated recipes across arms do not become independent samples.

The attempt cap is real. Tight owned inventories can make uniform full-party rejection inefficient; the tool reports `Incomplete`/`ProposalBudgetExhausted` if it cannot fill an arm. It does not switch distributions, silently raise the cap or claim equal-budget method comparison. Cancelled or invalid measurements retain partial evidence and cannot pass as complete discovery.

## Commands and artifacts

Use a current complete schema-3 definition, created through `TowerBossDiscovery.Create(...)` or authored against the [contract guide](Automatic-Tower-Team-Discovery-Implementation.md). Recreate definitions after an executable/content change. Supply historical seed exclusions independently of reference selection, including the linked Garran tuning ledger.

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-discover --definition discovery.json --content-root LL/src/API/API.LL --output TestResults/balance/independent-discovery-new
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-discovery-verify --run TestResults/balance/independent-discovery-new
```

The first command executes **discovery only**, at most **6,144 combats** for the default two methods, three generation seeds, 128 candidates and eight combat seeds in one equipment context. It does not execute the contract's reserved selection, confirmation, references, diagnostics or replay allowance. Adding benchmark references does not change discovery cost or candidate selection.

The archive contains the full definition, content/settings/execution scope, cost, detached generation inputs, complete mechanics inventory, generation mechanics, proposals/evaluations, `discovery.json`, `discovery.md`, normal Tower recipes, compressed battle reports, trial ledger, discovery-shortlist exports and file inventory. The shortlist reserves each method/restart's best recipe, retains the primary and adds bounded exploration/ranked candidates. It is **not the confirmation finalist set**.

`tower-boss-discovery-verify` checks every saved file, regenerates the mechanics and proposals, reconstructs production input identities and the exact trial schedule, recomputes scores and compares the shortlist and report. It executes no new combat. Attempt-exhausted runs can be reconstructed; cancellation/invalid runs retain partial inventory evidence but are not presented as fully reconstructed searches. Altering report rankings and updating their file hashes still fails semantic reconstruction.

Exit codes for discovery are **0 Complete, 3 Incomplete, 130 Cancelled, 2 Invalid**. Complete describes execution, not balance acceptance. Verification returns 0 for a reconstructed complete run and 3 for a reconstructed attempt-exhausted run. Existing directories are preserved. `improve-supplied` is still a validated contract/provenance mode; this independent runner rejects it before output or combat. Its execution remains an optional later extension.

Existing `tower-loadout-replay --run <directory> --battle <trial-id> --detailed` works with these archives. `shortlist/` exports are normal Tower scenarios containing the already-used discovery schedule. Replaying them reproduces discovery; it is not fresh confirmation. Keep the producing Release executable directory with any retained experiment: the archive records execution identity but does not copy the binaries automatically.

## Verification

The new kernel tests compare the legal sampler with an exhaustive **216 ordered-loadout** oracle, including nonuniform family sizes. A 24-recipe complete search space recovers the exhaustive best and correctly reports that a requested 30-candidate arm cannot complete. A separate two-position interaction oracle has no benefit from either single ingredient, but benefits from the coordinated change; same-owner restrictions are also checked.

Reference-isolation tests compare the entire generated proposal/evaluation/shortlist result with references absent, present and reordered, including registration of the independently generated winner itself. A production integration test repeats the same **32-combat** discovery with no references and with two references in both orders. It compares all generated results and prepared-input hashes, reconstructs all three archives, checks every refinement operator, performs a detailed replay and reproduces an exported recipe through normal Tower combat. Additional coverage verifies owned copies, fresh full parties at floors 5/11/15, worst-context ranking, malformed evidence, cancellation, output preservation and semantic tamper rejection.

Backend checks run through `build/run-tests.ps1`:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerDashboardBossTests|FullyQualifiedName~BalanceHarnessGoal'
```

**All 273 relevant tests passed**, including 12 new generation/runner cases and the existing contract, acceptance and legacy regression coverage. Build and test logs are retained under `TestResults/balance/tower-independent-generation-20260911/`, including `regression-tests.trx`. The final build completed with zero errors and five pre-existing warnings in unrelated tests. No verification command remains blocked. These are implementation tests, not the full discovery/confirmation pilot. The synthetic objectives are test oracles and do not report game-balance results. No browser run was needed because this increment changes no UI.

## Changed files and next increment

New tool files are `TowerBossPartyGenerator.cs`, `TowerBossGeneration.cs` and `TowerBossDiscoveryRun.cs`. `TowerBossDiscoveryContract.cs` now includes the explicit generation-policy version and discovery shortlist capacity. `Program.cs` exposes discovery and verification. `TowerBossSearch.Observe` is shared internally without changing legacy behavior. Two new test files cover generation and the archive/CLI workflow; the plans, acceptance notes, implementation guide and harness README describe current completion and limits.

The next step recorded at increment 2 was staged selection validation and confirmation; it is now [implemented in increment 3](Automatic-Tower-Team-Confirmation-Review.md). Tower Lab integration is now implemented. The [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) are now complete; practical Essence access and a separate tuning protocol are the next decisions. Historical discovery-only artifacts retain their original semantics and require their original producing build for reconstruction.

No game-content, migration, production-configuration, dependency or deployment change is introduced. Legacy boss-search schema-1/2 behavior and sealed experiments remain unchanged.
