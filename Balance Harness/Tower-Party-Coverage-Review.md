# Tower whole-party coverage and placement pilot

The new opt-in `independent-coverage-v4` policy found two independently generated Kharad teams with **89/256 wins (34.77%)** and **99/256 wins (38.67%)** on fresh held-out seeds. The four saved controls measured **14.45–32.03%** on the same schedule. Both new builds came from one restart. The frozen reliability gate therefore remains **Fail: 1/3 passing restarts, with 2 required**. The stronger secondary's validation result does not replace the primary selected before validation.

This completes implementation and the first bounded comparison, following the [saved-team substitution experiment](Tower-Saved-Team-Ablation-Review.md). It is progress toward finding competitive builds, not proof of near-optimality or a new complete Tower-family acceptance. Both builds, all other finalists and their evidence are saved for reuse. Boss settings, shared catalogs and default generation policies were not changed.

## Implementation and independent boundary

The policy requires exactly `mechanics-joint` followed by `coverage-joint`. The first arm preserves v3's random stream, operators and mechanics. The new arm uses [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), integrated through generation, party construction and provenance validation. Its optional `coverage` archive field is omitted for older policies, preserving their serialization. Compatible mechanic cores remain available in both v3 and v4.

The extractor reads direct authored ability effects from the eligible production inventory and retains evidence node keys. Current content supplies **66 Essence/category entries**:

| Category | Entries | Structural basis |
| --- | ---: | --- |
| Recurring enemy control | 4 | Enemy-targeted Stun/Freeze on an active ability or a repeatable trigger |
| Enemy pressure | 29 | Authored enemy conditions such as Poison, Corrosion, Weaken or Slow |
| Attack enabling | 5 | Friendly basic-attack execution, Haste or Empower |
| Protection | 11 | Friendly barriers, cover, damage reduction or protective conditions |
| Recovery | 17 | Friendly healing, regeneration increases or recovery conditions |

These categories propose candidates; they are not estimated uptime, magnitude or success probabilities. Zero-chance effects and ambiguous event targets are excluded. Control that is only triggered by combat start or death/kill events is excluded from recurring control. Delegated status/summon effects are outside this first coverage extractor; the broader inventory and existing 48 mechanic-core hypotheses remain unchanged. Effects can belong to multiple categories. No Essence IDs, copy counts or saved team templates are hardcoded into the generator.

Fresh coverage construction shuffles category order, selects a provider for each category and samples a reservation count from zero through the full party size. It distributes those reservations across shuffled character positions, respecting family uniqueness, space and optional owned copies. Where space permits it adds compatible cores, then fills remaining slots and shuffles their Essence order. The intent trace records requested/satisfied reservations; these are not final category totals, because later fills can add coverage. A one-in-eight uniform route retains access to every legal ordered team, including uncategorized mechanics; an empty feature pool also uses explicit uniform fallback.

The new `coverage-count` mutation replaces a chosen Essence family or position across a sampled group of characters. The `placement` mutation exchanges existing Essences between characters, preserving the exact global multiset and enforcing family legality. Half of placement proposals take the first legal random swap; the other half sample at most 32 possibilities and prefer completion of existing compatible cores. This is a proposal heuristic, not a substitute for combat fitness. Parents are copied, and illegal or duplicate proposals remain recorded with their rejection reason.

`fresh-coverage` requires zero parents; `coverage-count` and `placement` require one generated parent and are restricted to the independent v4 coverage arm. Saved recipes, reference fitness, actor identities and held-out schedules remain outside the generation input. Ordinary and compact discovery both support the policy; factory and Tower Lab defaults remain v1. Production-combat tests verify that adding, removing or reordering references does not alter generation results.

## Frozen comparison and results

The [protocol](../TestResults/balance/tower-party-coverage-20260912/protocol.json) fixes Kharad at **3.04881408 Health / 3.85370128 Power** and preserves the floor-5 budget: ten level-40, tier-1, rank-2, Standard-quality characters, five level-1 unascended/unevolved Essences each, fixed gear, no combat styles or prior contributions. The full eligible pool includes Rare Essences under hypothetical ownership. Practical acquisition remains unverified.

Three fresh generation restarts evaluate both methods: **96 complete parties per arm on eight shared discovery seeds**, totaling **576 evaluated parties and 4,608 search fights**. There were 595 proposals: 576 evaluated, 13 duplicates and six family violations. The ledger excludes **468,389 historical/reserved seeds** and separates generation, discovery, held-out validation and unused selection/confirmation schedules.

Before any held-out fight, the top two recipes from each arm and all four saved controls were frozen as 16 distinct cells. Each received **256 fresh shared validation seeds**, totaling **4,096 fights**. The discovery-selected primary determines the gate; secondaries remain exploratory. Four historical detailed parity replays and four predeclared first-validation-seed replays complete the **8,712-fight** allocation. Test simulations are separate from this pilot accounting.

| Restart | v3 primary / secondary | v4 primary / secondary | Passing primary? |
| --- | --- | --- | --- |
| 1604426392 | 0 / 0 wins | 0 / 0 wins | No |
| −2121454998 | 0 / 0 wins | 0 / 0 wins | No |
| −1218588547 | 0 / 0 wins | **89 / 99 wins** | **Yes** |

Every table entry has 256 validation trials. Saved controls scored 37, 72, 77 and 82 wins. No validation cell had an observed rate above 50%. The winning primary had 5/8 discovery wins, but that small discovery estimate was not treated as balance acceptance; it was measured afresh at 89/256.

The joint nominal alpha .05 is split .025 across all 16 rate intervals and .025 across six predeclared paired comparisons. Each paired difference subtracts two Wilson discordance intervals using component alpha `.025/(6×2)`. Wilson coverage is approximate. The winning primary's adjusted rate interval is **26.09–44.59%**; the secondary's is **29.64–48.55%**. A zero-win finalist has an adjusted upper bound of **3.76%**, not proof of a zero true rate.

A passing primary must have an adjusted rate lower bound of at least 10%, a paired lower difference above zero against its same-restart v3 primary, and a paired lower difference of at least −10 percentage points against predeclared control `team-1abe76ca1891d97a91d484f0a3662048`. For the winning restart, the paired intervals are **+22.73 to +44.32 pp** against v3 and **−9.78 to +22.59 pp** against that control. It clears the latter threshold narrowly; the result does not establish superiority over the saved control. The other two primaries fail every gate component. No sample is extended, pooled with history or selected again using validation outcomes.

The evaluator's Pass applies only to these 16 validation cells. The earlier [2,918-party staged confirmation](Tower-Staged-Confirmation-Review.md) keeps its original family/settings/evidence scope, and independent search reliability remains Fail.

## What the saved ancestry shows

The successful secondary is `team-1a924a7cfff12298633bee909cdea4ad`; the primary is `team-38248d838d1db9634fd82536c177df0a`. Their complete named recipes and ancestry are in [trace-findings.json](../TestResults/balance/tower-party-coverage-20260912/trace-findings.json), with reusable seed-free recipes and measurements in [validated-builds.json](../TestResults/balance/tower-party-coverage-20260912/validated-builds.json).

Both descend from one fresh coverage proposal, followed by mechanic-core replacements, a coverage mutation and several core-guided placement swaps. The primary adds a cross-character mutation to the secondary's lineage. This establishes independent provenance; it is not an operator ablation and cannot attribute the gain to one mutation.

The secondary contains ten Pack Howlers, ten Royal Venoms, eight Venomous Spiderlings, seven Enchanted Fairies and five Giant Worms, plus ten other Essence positions. It differs from the previous saved template. Both successful builds measured **zero barrier absorption** across validation. The primary's median first initial-character death was **53.25 seconds**, while it still won 34.77%. Conversely, the first restart's zero-win secondary used nine Fairies and nine Royal Venoms. Repeating a strong-looking ingredient, maximizing healing or imposing a fixed protection quota is therefore not established as a sufficient search rule.

Timing, recovery and coverage counts use all 256 compact reports per cell, with no-death battles separately recorded. The four fixed detailed replays agree with their compact summaries. These measurements are descriptive; the policy comparison does not isolate individual ability effects or prove that another successful defensive composition is unnecessary.

## Resources, verification and retained output

Measured execution and reconstruction phases totaled **219.65 seconds (3 minutes 40 seconds)**: discovery 138.86 s, discovery reconstruction 13.06 s, validation 54.98 s, validation reconstruction 2.62 s, and about 10.13 s for the eight parity/replay phases. Preparation, builds, tests, analysis and documentation are outside that total. The package retains about **339 MiB**, within the frozen ten-minute/1-GiB global limits and 512-MiB per-campaign limit. There were no interrupted/lost attempts, retries or budget extensions. This is not a controlled speed comparison with previous workloads.

The [analysis](../TestResults/balance/tower-party-coverage-20260912/analysis.json) independently checks manifests and exact file inventories, durable journals, recipes, source/content hashes, schedule order/exclusions, ranking-based selection, reference-free ancestry and all adjusted intervals. Both campaigns reconstruct without fights. All four historical detailed reports match, and the four new detailed replays match their compact results and summaries. The [final receipt](../TestResults/balance/tower-party-coverage-20260912/final-verification.json) also checks the protected historical packages, current content and both catalogs. Gameplay assembly hashes remain unchanged; the harness assembly changes to implement v4.

**144 relevant tests passed**, with zero failures or skips, through `build/run-tests.ps1`. They cover source-derived categories, recurring-control exclusions, legality at 4/5/7/10 slots, scarce ownership, immutable parents, exact inventory preservation, reproducibility, v3 comparison parity, provenance, reference isolation, compact resume and replay/reconstruction. Verification commands:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerCoverageTests|FullyQualifiedName~BalanceHarnessTowerMechanicCoreTests|FullyQualifiedName~BalanceHarnessTowerBossGenerationTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
git -c core.safecrlf=false diff --check
```

The initial wrapper build could not read the user NuGet configuration inside the sandbox. Building with existing restored dependencies succeeded, with five pre-existing warnings in unrelated test files and no errors; tests then ran through the wrapper with `-NoBuild`. The local diagnostic driver restore received sandbox approval and its build passed with zero warnings/errors. Nothing remains blocked.

Changed runtime files are `TowerPartyCoverage.cs`, `TowerBossPartyGenerator.cs`, `TowerBossGeneration.cs` and `TowerBossDiscoveryContract.cs`. Tests add `BalanceHarnessTowerCoverageTests.cs` and extend the discovery-run and bulk tests. This review, six planning/policy documents and the harness README record the implementation and outcome; ignored experiment files retain inputs, exact recipes, ancestry, reports, producing binaries and receipts. No gameplay configuration, migrations, shared catalog entries or deployment changed.

## Next bounded step

Replicate the **unchanged v4 policy** under a separately frozen set of new generation and combat seeds, using the two newly saved builds alongside the existing leaders as reference controls. Freeze the allocation and reliability rule before execution, retain every result, and keep controls outside independent parents/fitness. The current sample is closed; the new experiment must not append trials or reclassify its 1/3 outcome. Do not select the successful restart seed or encode its recipe into generation.

The immediate question is whether this policy can recover competitive strength repeatedly. Defer boss retuning, default promotion and the next floor batch until the independent reliability evidence supports them. The saved builds can already be loaded without repeating discovery; a new content or boss setting still needs fresh measurement against the strongest available controls.
