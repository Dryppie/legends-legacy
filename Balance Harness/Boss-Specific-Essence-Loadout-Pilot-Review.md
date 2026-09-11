# Boss-specific Essence loadout pilot — 10 September 2026

The completed controlled pilot found a four-slot Kodoku specialist with **20/20 fresh wins versus the strongest retained control's 10/20**, plus alternatives with different transfer costs. Eydis produced one rare-clear five-slot party at 2/20; seven-slot Nhalia remained unbeaten. Morrowmaw's screening was saturated and its comparative search was skipped. These are bounded, descriptive findings with substantial tradeoffs.

The pilot uses the offline [boss-search implementation](Boss-Specific-Essence-Loadout-Implementation.md) under the [analysis plan](Boss-Specific-Essence-Loadout-Plan.md). The evidence package is `TestResults/balance/tower-boss-pilot-20260910/`. Production combat, content, accounts, databases and deployment are outside this experiment.

## Frozen protocol and controls

Before sampling, the package froze all 28 potential boss/budget definitions, content, executable assemblies, non-secret combat settings, catalogs, exact controls, historical seed exclusions and future stage schedules. The effective seed namespace is `boss-pilot-20260910-v1`; `2026091019` is a campaign label, not another seed input. The three generation seeds are −1190161071, −585433539 and 717738667. All methods share the same paired discovery schedule within a study. Confirmation and diagnostic seeds are disjoint from discovery, probes, historical trials and the other potential cells.

Controls come from every finalist in the latest sealed [whole-party study](Tower-Whole-Party-Review.md), projected onto the current target's mutable positions in both original authored ally contexts. Positions absent from a historical first-cell recipe retain that context's actual ally recipe. Identical projected parties are deduplicated. The current authored party is also retained. This preserves repeated, alternating and first-cell deployment evidence that a simple repeat of an older five-character fixture would omit. `control-provenance.json` records every source and projection.

The only additional harness change for this pilot expands explicit contracts from eight to 32 controls and from 40 to 64 finalists. Mandatory control/method/strategy allocation and the 100,000-fight per-study hard limit remain enforced. The pilot's selected cohorts use eight, 18 and 19 controls. Default CLI/dashboard search sizes are unchanged.

The screening stage evaluated every distinct control on five paired seeds for each of four bosses at all seven budgets: **1,980 actual fights across 396 control/cell evaluations**. Before choosing budgets, verification reconstructed every expected trial, input hash, recipe, seed, cache key and derived result. All expected observations were present and cache hits were zero.

The selection rule was declared before probes: prefer a budget whose best retained control wins 1–4/5, closest to six slots; otherwise choose the zero-clear budget with the smallest observed mean guardian health. Ties use distance from six, then lower budget. Skip search if every budget's best retained control wins 5/5. Probes are never resampled to obtain a preferred cell.

| Boss | Best control wins at 4 / 5 / 6 / 7 / 8 / 9 / 10 slots | Decision |
| --- | --- | --- |
| Morrowmaw, floor 3 | 5 / 5 / 5 / 5 / 5 / 5 / 5, each out of five | Skip comparative search: screening saturation |
| Eydis, floor 7 | 0 / 0 / 5 / 5 / 5 / 5 / 5 | Five slots: closest observed zero-clear progress |
| Kodoku, floor 8 | 2 / 5 / 5 / 5 / 5 / 5 / 5 | Four slots: mixed screening outcomes |
| Nhalia, floor 13 | 0 / 0 / 0 / 0 / 5 / 5 / 5 | Seven slots: closest observed zero-clear progress |

Morrowmaw's 5/5 is a result on five screening seeds. It does not establish certainty or a newly confirmed boss specialist. The skipped comparison and its full probe evidence remain in the package.

Each selected study compares uniform legal random exploration, the legacy structural mutation proposal, joint refinement and graph-seeded refinement. Every method/restart receives all controls plus **eight new candidates**, four paired discovery seeds and unchanged full Tower combat fidelity. Shared controls are charged again in every arm. Graph arms can also emit ordinary joint mutations, so a result from that arm does not necessarily come from an enabler/consumer proposal.

| Boss | Explicit budget | Mutable characters | Controls / candidates per arm | Finalist cap | Canonical floor/context cells per seed | Fight cap |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| Eydis | Five Essences; level 40; Uncommon Standard tier 1, rank 2 | 5 | 8 / 16 | 25 | 24 | 12,784 |
| Kodoku | Four Essences; level 30; Uncommon Standard tier 1, rank 1 | 10 | 18 / 26 | 35 | 17 | 13,164 |
| Nhalia | Seven Essences; level 60; Uncommon Fine tier 2, rank 3 | 10 | 19 / 27 | 36 | 17 | 13,552 |

These are distinct progression/gear budgets, not an experiment isolating the effect of one additional Essence slot. Ordered Essences are level 1, unascended and unevolved, with hypothetical ownership and no Combat Styles. Identities, training, equipment and all nonmutable participants remain fixed. Every legal RequiredSlot is filled; summons, time limits and production targeting remain intact.

Every retained control, method/restart winner and strategy allocation freezes before **20 fresh confirmation seeds on every one of the 15 floors**. Identical resulting contexts share trial IDs and count once. Additional characters on transfer floors retain their declared authored builds. Four separately reserved seeds evaluate a predeclared diagnostic quartet, independently of selection.

The initial campaign cap was **63,196 fights**, covering all probes, the worst possible selected study for each boss and 160 replay/export repeats. Applying the frozen screening rule reduced that cap to **41,640**: 1,980 probes, at most 39,500 study fights and 160 reserved verification repeats. Full study allocation is 3,312 discovery, 36,140 confirmation and 48 diagnostic fights. No confirmation result triggers another search or a new seed batch.

## Reading the results

The complete finalist set remains frozen. Fresh-result maxima below describe that set; they do not select a second set for confirmation. `analysis/summary.json` compares every finalist against **every retained control** on each canonical floor/context, with exact paired gained/lost outcomes, seeds, trial IDs and recipe references. A gain against the authored baseline alone is insufficient to claim a new best result.

Wilson 95% intervals describe individual clear rates without multiplicity adjustment. Generation restarts and repeated method winners do not increase the independent confirmation sample size. These selected, known-boss measurements support observed repeatability under the declared budget, with no optimum, universal Essence ranking, method-superiority or unseen-boss generalization claim.

## Eydis: rare clears with substantial transfer costs

All eight retained controls scored **0/20** against Eydis. One new frozen finalist, `e568dfaabbb86d4f3e8e0156d7ce16cdbf590b366a63fc0f1ee200e0de81de68`, scored **2/20**, gaining two wins and losing none against every control. Its nominal Wilson interval is **2.8–30.1%**. The other 16 new finalists scored 0/20. This is a rare-clear exploratory alternative, not a dependable clear recipe.

Its mean guardian remaining health was **30.863%**, compared with **54.2955%** for the strongest retained progress result, `10851df77b81`. It was the frozen graph-arm winner for generation −1190161071, created by a `joint-double` proposal. This observation does not identify graph-pair synergy or establish the graph method's superiority.

The same party scored **8/20 on floor 8 with balanced later allies versus a retained control's 20/20**, a paired loss of 12 clears. With alternative later allies it scored **20/20** on that floor, demonstrating the importance of the fixed teammate context. On floor 11 with balanced later allies it scored **2/20 versus 13/20** for a retained control; with alternative later allies it scored 9/20 versus 18/20. All 17 new Eydis finalists had at least three non-target canonical cells below some retained control. These weaknesses are part of the result, not reasons to replace or hide the frozen finalists.

The two Eydis victory observations are `trial-006190` and `trial-006207`. The complete target scenario is archived as recipe `29d96386447e3e1b92f0998996f2ca11d2a81b7db4e6ed3ce9984cd479804861.json` in the Eydis study.

## Kodoku: stronger specialists and an alternative that changes one character

The strongest of 18 retained controls, `49d06852e534`, scored **10/20** against Kodoku; another scored 9/20 and the remaining 16 scored 0/20. Sixteen of the 17 new finalists exceeded the strongest control's target win count. The remaining new finalist tied 10/20, with five gained and five lost outcomes, illustrating why equal totals do not imply identical performance.

| New frozen party | Target wins | Nominal Wilson 95% | Gained / lost versus the strongest retained control | Observed tradeoff |
| --- | --- | --- | --- | --- |
| `b4b8971c6c8f` | **20/20** | 83.9–100% | **10 / 0** | Focused specialist; 0/20 on floors 3 and 6 where retained alternatives achieve 20/20 |
| `8db553132d9d` | **17/20** | 64.0–94.8% | **9 / 2** | Frozen representative for greater measured prevention; all floors 1–6 regress against at least one retained control |
| `33e64251b22b` | **14/20** | 48.1–85.5% | **5 / 1** | Changes only absolute character slot 5 from `49d06852e534`; retains 20/20 on floors 1, 2 and 5 |

The 20/20 specialist is the frozen random-arm winner for generation 717738667. All twenty observations were victories; the pointwise interval and the selected search context still leave uncertainty about future trials. Its floors 1–6 results are 10, 6, 0, 0, 2 and 0 wins out of 20, compared with per-floor retained maxima of 20, 20, 20, 4, 20 and 20. These maxima can belong to different controls and are not presented as one hypothetical combined party.

The 14/20 alternative replaces character slot 5's fourth Essence, **Cinder Beetle → Pack Howler**, preserving Nightshade Blossom, Spider Queen Royal Venom and Venomous Spiderling in their original order. Every other character is identical to `49d06852e534`. It preserves more early-floor performance, though it still scores 0/20 on floor 3 versus a retained 20/20, 2/20 on floor 4 versus 4/20, and 18/20 on floor 6 versus 20/20. The full comparison retains its exact paired outcomes against each real control. Its unchanged teammates are part of the recipe; the result is not a standalone tier judgment on those Essences.

Frozen method/restart winners confirm at the following target counts, in generation-seed order −1190161071, −585433539, 717738667:

| Proposal method | Fresh Kodoku wins, each out of 20 |
| --- | --- |
| Random | 10 / 10 / 20 |
| Legacy structural mutation | 15 / 10 / 10 |
| Joint refinement | 11 / 10 / 14 |
| Graph-seeded refinement | 11 / 13 / 10 |

Shared controls can be the winner in several arms. These are repeated recipe comparisons on the same seed schedule, not 60 independent observations per method. This bounded study found useful boss-specific alternatives, including a uniformly generated complete party; it establishes no reliable advantage for the more elaborate proposal methods.

## Nhalia: no successful seven-slot recipe found

All **19 retained controls and 17 new finalists scored 0/20** against Nhalia. Every frozen method/restart winner also scored 0/20. The pointwise interval for each 0/20 result is 0–16.1%; these outcomes do not establish that the budget is impossible to clear. They establish that this bounded comparison did not find a successful recipe.

The strongest observed progress party, `a4eaec8b4a9a`, left **35.378%** guardian health on average versus the best retained progress result's **49.943%**. It still scored 0/20. Progress differences and exact parties remain in the archive as unsuccessful exploratory results. No finalist is promoted as a healing-feedback counter, and the search is not repeated after seeing confirmation. Eight-slot screening saturation belongs to a different level/gear budget and cannot be used to label a seven-slot failure successful.

## Reserved mechanics diagnostics

The deterministic graph diagnostic chose Blood Harpy / Bloodfang Wolf for all three studies. Harpy replaces Transparent Slime at character 1, Essence position 1; Wolf replaces Blue Slime at character 2, position 1. The quartet compares the reference, either replacement alone and both replacements on four separately reserved paired seeds.

All four variants scored **0/4** in each of the three studies. The clear-rate interaction contrast is zero throughout. No successful diagnostic synergy or boss counter was established.

The frozen baseline already contains other Bleed sources—Cinder Beetles and Goblins—so Wolf alone already has potential enablers. Harpy supplies additional Bleed/damage while removing a protection Essence; Wolf also removes one source of group recovery/barriers. Harpy's lowest-health-enemy target and Wolf's current-target condition can diverge when Kodoku has adds. Their cooldowns and the timing of existing Bleed applications matter.

Kodoku's arithmetic progress interaction was positive while the combination still left more guardian health than the reference. Lower summon presence accompanied earlier defeat and fewer recorded summon deaths, so it does not demonstrate improved add clearing. For Nhalia, aggregate party healing cannot be multiplied by the authored Undertow coefficient to infer avoided boss healing: restoration paths, rounding, caps and timing matter. `analysis/diagnostic-mechanic-audit.md` preserves the exact frozen definitions, results and attribution limits.

## Exact recipe library

[Five complete reviewed recipes](Boss-Pilot-Recipes-20260910/README.md) are preserved alongside this report: the three Kodoku alternatives, its strongest retained reference, and the rare-clear Eydis party. These copies contain the full fixed party and original seed schedule, not only a list of ingredients. The package exports **all 96 frozen finalists**, including unsuccessful Nhalia and Eydis alternatives, under `recipe-library/` with an index linking outcomes, source IDs and hashes. The selection and every all-floor recipe remain in the sealed studies.

## Completion and verification

The campaign completed **41,568 of its 41,640 allowed actual fights**, leaving 72 unused:

| Stage | Actual fights |
| --- | ---: |
| Screening probes | 1,980 |
| Equal-cost discovery | 3,312 |
| Fresh all-floor confirmation | 36,140 |
| Separately reserved diagnostics | 48 |
| Normal-Tower exported-recipe repeats | 60 |
| Detailed replay repeats | 28 |
| **Total** | **41,568** |

The three studies retained **96 finalists: 45 controls and 51 new alternatives**. All completed their declared allocation with zero cache hits. Eydis, Kodoku and Nhalia took 282.6, 272.1 and 300.9 seconds respectively in sequential execution; their archives were approximately 266.4, 273.2 and 288.4 MiB. The probes took 43.3 seconds. These are local runtime/storage observations, not method-performance benchmarks.

Verification completed:

- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerDashboardBoss'` passed **39 tests**, including the expanded control/finalist contract and full-fidelity draw/replay case. Build had zero errors; five existing warnings were outside the changed feature.
- The offline orchestration and verification drivers built with zero warnings/errors against the captured local assemblies.
- Probe reconstruction verified all 28 cells and 396 complete control evaluations before budget selection.
- `TowerBossSearch.VerifyAsync` reconstructed all three full studies from their saved trials, including proposals, fitness, controls, selection, strategy labels, diagnostics and all-floor results. The reconstruction ran no new combat.
- Three normal-Tower exports repeated each study's frozen overall discovery winner on its full 20-seed target schedule. **All 60 complete reports matched** their archived confirmation reports.
- **All 28 detailed replays matched** preparation, combat summaries, telemetry and Tower outcomes. They include both rare Eydis target victories, target victories for each reviewed Kodoku recipe, all three diagnostic quartets on the first reserved seed, and negative-transfer examples. An actual confirmation draw, Eydis-study `trial-000835`, reproduced through detailed execution; the Nhalia and Kodoku confirmation sets contained no draw to replay.
- All five repository recipe copies matched their archived input files byte for byte. The read-only analyzer checked hashes, exact paired seed order, alias equivalence, complete retained-control coverage, equal discovery cost and every finalist/control comparison.
- Scoped `git diff --check` passed. The final preservation audit and package seal are recorded in `verification/preservation.json`, `checksums.json` and `seal.json`.

Each replay/export job reserved its entire fight cost before execution in an append-only ledger protected by a single-writer lock. All 31 jobs completed: 88 reserved and 88 actual fights. No failed-job retry consumed an unrecorded budget. Those repeats verify parity and do not increase the independent confirmation sample size.

The first local driver initialization failed before any combat because its generated minimal settings used the wrong JSON property casing; its files and error are preserved under `initialization-failure-1/`. The corrected initialization froze the protocol before probes. Sandboxed builds initially could not access the user's NuGet configuration; builds and the required test script subsequently passed with local access. No verification command remains blocked.

To independently reconstruct a sealed study without running combat or changing its files, use the captured executable from the repository root:

```powershell
dotnet TestResults/balance/tower-boss-pilot-20260910/executable/BalanceHarness.dll tower-boss-verify `
  --run TestResults/balance/tower-boss-pilot-20260910/studies/floor-8-slots-4
```

The one-off orchestration source, producing executable, content, definitions, stage ledgers, full trials, source/test evidence, recipe library, analysis, verification policy and execution ledger are retained in the package. Its `audit-seal.py --verify` command checks the final inventory and hashes. Reproduction requires the recorded assemblies, runtime and platform; use new output directories for additional execution.

## Scope and next decision

Changed repository files are the boss contract and its boundary test, this review, five exact recipe JSON files with their README, and the implementation/BalanceHarness documentation. Experiment drivers and the full evidence package remain under ignored `TestResults/`; no experiment-specific behavior was added to production combat.

This pilot supports retaining several Kodoku specialists and a modest character-slot-5 substitution with explicit teammate dependencies. Eydis's rare clear and Nhalia's negative result remain valuable limits of the current budget and search. The repeated Bleed diagnostic offers narrow hypothesis coverage; it does not substitute for targeted add-control, prevention or healing-route experiments. Further comparison should be a separately frozen, bounded study driven by these gaps. Broad search expansion, cooperative coevolution, surrogate proposals and the four-/five-slot generalist refinement are not silently added to this campaign.

No migration, production configuration change, database operation, balance tuning, deployment or Phase 2 integration is required. The earlier whole-party package and accepted starter evidence remain historical and unchanged; unrelated working-tree changes are preserved.
