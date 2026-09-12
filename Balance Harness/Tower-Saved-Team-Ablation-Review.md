# Tower saved-team substitution review

The controlled Kharad experiment is complete. The saved team won **75/256 fights (29.30%)**. Every one of the eight declared recipe changes reduced its win rate, with all eight paired comparisons supporting a loss after the frozen multiple-comparison adjustment. Removing half its Enchanted Fairies produced **0/256 wins**; redistributing the same total Essence inventory produced **20/256 (7.81%)**. This establishes sensitivity to coverage and placement in this particular team. It does not establish the best possible recipe or improve independent search reliability by itself.

The experiment used **2,332 fights** and **38.49 seconds across measured execution and reconstruction phases**. The corrected package retains about **330 MiB**, plus **39 MiB** for a preserved setup failure that occurred before any fight. There were no retries, additional samples or catalog promotions. The [mechanics-core pilot's search-quality Fail](Tower-Mechanics-Core-Review.md) and the [earlier complete-family balance assessment](Tower-Staged-Confirmation-Review.md) retain their original scope.

## Frozen question and budget

The question was why the saved strong recipe succeeds when recent independently generated finalists do not. We tested declared substitutions in `team-1abe76ca1891d97a91d484f0a3662048`, keeping Kharad at **3.04881408 Health / 3.85370128 Power**. This is explicitly reference-derived diagnosis: all nine cells are registered as references, the cohort purpose is diagnostic, and none is an independent generated parent or fitness input.

The floor-5 budget remains **10 characters with five Essences each**, level 40, tier 1, rank 2 and Standard quality. Gear, actor identities, ordered positions outside declared changes, level-1 unascended/unevolved Essences, and current production content stay fixed. No combat styles or prior contributions are used. The allowed pool includes Rare Essences under hypothetical ownership; practical acquisition is still unverified. All selected Essences have empty attribute bonuses. These are legal whole-Essence substitutions, not synthetic disabled-effect simulations.

The [frozen protocol](../TestResults/balance/tower-saved-team-ablation-20260912-v2/protocol.json) declares 256 fresh shared paired seeds, excluding **468,133** historical or reserved seeds. Nine cases make **2,304 new trials**. The first three seeds of every case were fixed for detailed replay before outcomes existed, giving 27 repeats plus one historical detailed parity replay: **28 diagnostics**. Neither old results nor diagnostic repeats enter the new rate estimates.

Limits were 2,332 total fights, 300 seconds of cooperative global execution cancellation, a 512-MiB package and a 256-MiB compact campaign. Execution used `prepared-v1`, durable attempt accounting and 32-record chunks, with zero retry reserve and no automatic extension or resume. All limits were met.

## Results and exact changes

Party slots below are one-based. Complete ordered recipes and the exact changed Essence indexes are retained in [ablation-variants.json](../TestResults/balance/tower-saved-team-ablation-20260912-v2/ablation-variants.json). Every row uses the same 256 seeds. Differences are variant minus baseline, in percentage points.

| Case | Change | Wins | Win rate | Adjusted paired difference interval |
| --- | --- | ---: | ---: | ---: |
| Baseline | Saved recipe unchanged | 75 | 29.30% | — |
| Half Fairy → Wind Harpy | Slots 2, 4, 6, 8; eight Fairies become four | 0 | 0.00% | −39.04 to −17.30 pp |
| All Fairy → Wind Harpy | Slots 2–8 and 10; eight become zero | 0 | 0.00% | −39.04 to −17.30 pp |
| All Fairy → Illusion Fox | Same eight positions; alternative replacement | 0 | 0.00% | −39.04 to −17.30 pp |
| Pack Howler → Horned Wolf | Slots 4, 8, 10 | 23 | 8.98% | −32.93 to −6.14 pp |
| Royal Venom → Webbed Domain | Spider Queen in slots 2, 4, 9 | 2 | 0.78% | −38.60 to −16.24 pp |
| Venomous Spiderling → Cave Bat | Slots 1, 3, 6, 9 | 9 | 3.52% | −37.12 to −12.46 pp |
| Wood Nymph → Forest Spirit | Slots 1, 2 | 20 | 7.81% | −34.34 to −6.98 pp |
| Same-inventory redistribution | Fifth-Essence swaps 4↔5, 6↔7, 9↔10 | 20 | 7.81% | −33.81 to −7.51 pp |

All eight upper bounds are below zero. Five comparisons support losses of at least ten percentage points: the three Fairy substitutions, Royal Venom replacement and Venomous Spiderling replacement. The ninth declared comparison, zero versus four Fairies using Wind Harpy, is **unresolved**: both have zero wins and its adjusted difference interval is **−3.84 to +3.84 pp**. Zero observed wins does not mean the true win rate is exactly zero.

The joint nominal alpha of .05 is split equally between nine individual rate intervals and nine paired comparisons. Individual rates use Wilson intervals with alpha `.025/9`. Each paired comparison uses gained/lost discordance counts and subtracts two Wilson intervals, each with alpha `.025/(9×2)`. These have approximate Wilson coverage. The baseline's adjusted rate interval is **21.60–38.39%**; each zero-win case has an upper bound of **3.38%**. All counts, rate intervals and paired discordances are in [analysis.json](../TestResults/balance/tower-saved-team-ablation-20260912-v2/analysis.json).

The normal evaluator reports Pass for this **nine-cell diagnostic family**, and no cell has an observed rate above 50%. This does not replace the earlier 2,918-party confirmation or establish a new complete Tower-family Pass. A local sensitivity experiment does not test undiscovered stronger teams.

## What this explains

Fairy coverage is a strong lead for the next generation model. Relative to the baseline, the half-Fairy substitution raises mean remaining guardian health from **14.71% to 64.79%**, lowers mean guardian stagger from **54.11 to 23.68 ticks**, and advances median first character death from **70.9 to 55.9 seconds**. With no Fairies, mean stagger is zero and median first death is 37.9 seconds for both replacements. These are descriptive observations alongside the supported recipe-level loss; the experiment does not separate Fairy's individual effects or prove eight copies are universally required.

The Royal Venom comparison preserves the Spider Queen family, rarity and Royal Cocoon passive while changing its active ability. Its severe loss makes the active package worth preserving in later hypotheses, but does not isolate damage, status application or downstream interactions. The Spiderling replacement retains access to Slow while changing other effects; that result likewise concerns the complete substitution.

Recovery volume alone is an inadequate objective. Replacing Wood Nymph with Forest Spirit increases mean effective healing received from **3,794 to 4,282**, while mean barrier damage absorbed falls from **2,447 to 1,639**, median first death advances to **56.9 seconds**, and wins fall to **7.81%**. More healing received can coexist with a weaker team. These totals cover only the ten initial, non-summoned characters, avoiding owner/summon double counting. Healing is actual restored health; regeneration is recorded separately. Battle duration varies, so these totals are not standardized rates or causal estimates of deaths prevented.

Placement also matters: the redistribution keeps the exact global Essence multiset, moving Web Weavers into slots 5, 7 and 10 and moving their Frost Imp, Hobgoblin Brutal Charge and Pack Howler counterparts into slots 4, 6 and 9. Its supported loss shows that the inventory alone is insufficient to describe strength. Because all three swaps move both partners, it does not identify a single causal swap or prove the Web Weaver location alone explains the result.

First-death medians use compact statistics from **all 256 fights per case**, conditional on an initial character dying. The baseline has two no-death battles and the redistribution has one; those are reported separately as censored, not assigned artificial death times. All other cases have a death in every battle. Detailed events from the 27 fixed replays agree with these statistics. Timing, recovery, guardian progress and event-source breakdowns are descriptive; no additional confirmatory comparisons were selected after observing them.

## Execution, correction and verification

The original diagnostic driver attempted its historical parity replay using an archived scenario whose declared `Seeds` list was empty. Input validation rejected it **before any battle started**. The [original failure package](../TestResults/balance/tower-saved-team-ablation-20260912/final-verification.json) is sealed and preserved. A [correction receipt](../TestResults/balance/tower-saved-team-ablation-20260912-v2/correction.json) records the fix: declare that existing historical replay seed before creating its input. A separate corrected protocol was frozen before combat. Experimental recipes, paired schedules, comparisons and resource caps are unchanged; the failed setup is not concealed or counted as completed combat.

The corrected phases measured 0.35 seconds for historical parity, 32.43 seconds for the paired campaign, 1.70 seconds for its first reconstruction, 1.40 seconds for one verified read of replay originals, about 0.91 seconds for all 27 detailed replays, and 1.70 seconds for final reconstruction. The total is **38.4943209 seconds**. Preparation, builds, regression tests, independent analysis and documentation are outside this timing. This is not a controlled performance comparison or a projection for a 600,000-fight workload. One verified read of the immutable originals avoids repeating a full campaign scan for every detailed replay.

Independent analysis checks exact recipe changes, seed exclusion and order, complete manifests, the 2,304 durable attempt records, zero lost/retry attempts, two complete reconstructions, the historical full detailed parity hash and all 27 full non-event replay hashes. All **81 harness sources**, the producing runtime assemblies, allowlisted content and both retained-build catalogs remain unchanged. Historical proof packages and protected evidence are checked again in the [final verification receipt](../TestResults/balance/tower-saved-team-ablation-20260912-v2/final-verification.json).

The targeted backend verification passed **85 tests**, with no failures or skips:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerBulkTests'
```

Both diagnostic driver builds passed with zero warnings or errors. Their local restores used an empty-source NuGet configuration; sandbox escalation to read the user NuGet configuration was approved. No command remains blocked. Tests used the unchanged previously built runtime; this turn did not rebuild or modify runtime/test sources. `git diff --check` also passed.

## Saved output and next increment

[validated-variants.json](../TestResults/balance/tower-saved-team-ablation-20260912-v2/validated-variants.json) saves all nine seed-free recipes, measurements and source/content/execution provenance. They can be inspected or reused without rerunning discovery or this completed experiment. Compact reports, fixed detailed replays, scripts, inputs and receipts remain in the corrected package. The original zero-fight package is retained for audit. Neither package changes the shared retained-build catalogs or independent generation inputs.

The next bounded increment should teach independent generation to propose **whole-party coverage and placement** from production content: vary the quantity and distribution of recurring enemy control, enabled attack pressure, protection and recovery, and search placement with inventory-preserving swaps. Existing compatible mechanic cores can contribute, but assembling pairs alone has not recovered the saved leaders. Keep all legal uniform alternatives reachable. Do not encode the saved recipe, Fairy count or specific Essence IDs as an independent construction template.

Implement and verify that proposal model separately, then freeze a small comparison with fresh held-out schedules and saved leaders kept behind the reference boundary. Maintain the same gear/untrained-Essence budget, resource caps and independent reliability gate. Do not expand either failed pilot, promote v2/v3, tune Kharad again or launch the next floor batch on the strength of this diagnostic result. Independent search quality still needs to pass before server-wide progression can be called competitively balanced.

This increment changes this review, the six active planning/policy documents, the harness README and ignored local experiment artifacts. There are **no gameplay, runtime configuration, default-policy, migration or deployment changes**.
