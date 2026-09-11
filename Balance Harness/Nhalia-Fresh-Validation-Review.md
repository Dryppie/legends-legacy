# Nhalia fixed fresh validation — 11 September 2026

The three fixed seven-Essence recipes remain unreliable against Nhalia. On **100 unused paired seeds**, the historical anchor cleared **0/100**, the previous discovery primary **5/100**, and the previously exploratory recipe **8/100**. The exploratory recipe gained eight victories and lost five against the previous primary; their winning seeds did not overlap. This validates rare clears for both newer recipes, without establishing a reliable build or a superior search method.

This experiment contained no search. The complete three recipes and all 100 seeds were frozen before any new outcome. Its producing executable and production content are exact copies from the sealed [refinement campaign](Boss-Specific-Essence-Loadout-Refinement-Review.md). Only the combat seed schedule changed.

## Fixed scope

- Floor 13, Nhalia; ten characters with seven ordered Essences each.
- Existing level-60, Uncommon Fine, tier-2/rank-3 gear, character identities, training, preparation and ally composition.
- Level-1 unascended/unevolved Essences, hypothetical ownership, and the existing disabled-style assumptions.
- Exactly 100 paired target seeds for each recipe. No additional candidate, adaptive sampling, early stopping or transfer-floor rerun.

The recipes were fixed in this order:

| Label | Exact prior recipe identity | Prior refinement confirmation |
| --- | --- | ---: |
| Historical anchor | `a4eaec8b4a9ab78088b9a6e5a672bfdf0a00bab29264d2a3694b2b1f22aa2a18` | 0/40 |
| Previous discovery primary | `7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e` | 1/40 |
| Previous exploratory finalist | `813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1` | 6/40 |

The previous exploratory finalist became a fixed hypothesis before this validation. Its new result is not a maximum selected from new confirmation outcomes. The other historical 6/40 ordering was not included, and no fourth recipe was introduced. Earlier observations remain separate historical samples and are not pooled with the new 100-trial results.

The effective seed namespace is `nhalia-fixed-validation-20260911-v1`. Initialization read 1,340 available historical seed ledgers, trial ledgers and saved Tower input files beneath `TestResults/balance`, excluding this new package. The conservative union contained 18,587 excluded integers; all 100 new seeds were distinct and absent from that union. Historical seed-ledger numeric fields contribute conservative exclusion values, so this count does not mean 18,587 independently observed fights. The record covers the available local sources and makes no account-wide claim.

Both prior package manifest hashes and complete file inventories were checked: 107,878 refinement files and 44,807 pilot files. Every imported producing input and every consumed seed source belonging to those sealed packages matched its existing checksum. Initialization did not rehash unrelated historical battle files and did not change either old package.

## Fresh results

| Fixed recipe | Wins | Defeats | Draws | Pointwise Wilson 95% | Mean guardian health remaining | Mean party survival |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Historical anchor | 0/100 | 100 | 0 | 0.00–3.70% | 34.99% | 0.00% |
| Previous primary | 5/100 | 95 | 0 | 2.15–11.18% | 28.69% | 2.00% |
| Previous exploratory | 8/100 | 92 | 0 | 4.11–15.00% | 28.93% | 4.40% |

There were no tick-limit terminations, invalid trials, cancellations or unrun trials. Mean victorious duration was 93.98 seconds for the previous primary and 82.83 seconds for the previous exploratory recipe. The anchor had no victories, so a victorious-duration mean is unavailable.

| Comparison on the same 100 seeds | Gained victories | Lost victories | Net victories | Both recipes won |
| --- | ---: | ---: | ---: | ---: |
| Previous primary versus anchor | 5 | 0 | +5 | 0 |
| Previous exploratory versus anchor | 8 | 0 | +8 | 0 |
| Previous exploratory versus previous primary | 8 | 5 | +3 | 0 |

Wilson intervals are nominal pointwise binomial intervals without multiplicity adjustment. Paired gained/lost counts describe the recorded contrasts. Five and eight clears still leave 95 and 92 failures respectively; these observations do not justify a reliable-build claim. The recipes' disjoint winning seeds also make the three-win marginal difference an incomplete description of their paired behavior.

The experiment did not reevaluate other floors. The earlier floor-14 loss against a retained control remains relevant; this target-only validation does not establish all-floor suitability.

## Observed recovery

Values below are means per new target fight. The complete totals and all 300 individual outcomes are retained in the package analysis.

| Fixed recipe | Friendly reported healing | Friendly effective regeneration | Guardian reported healing | Guardian effective regeneration |
| --- | ---: | ---: | ---: | ---: |
| Historical anchor | 6,225.05 | 8,081.04 | 2,884.83 | 11.46 |
| Previous primary | 7,032.72 | 8,632.39 | 3,157.20 | 12.46 |
| Previous exploratory | 7,283.87 | 8,182.05 | 3,115.10 | 12.41 |

`HealingDone` is a qualified engine-reported measure whose direct, periodic and lifesteal paths do not uniformly measure effective restoration. Regeneration is recorded separately as effective restoration. These aggregates cover initial participants and omit summons. They are observations, not isolated causal effects or deaths prevented; this fixed validation contains no new mechanism intervention.

## Accounting and verification

The predeclared maximum was **300 normal-Tower validation fights plus at most 12 detailed replays**, for a total cap of **312**. Every normal run reserved its entire 100-fight allocation before execution. Every detailed replay reserved one fight. A failed or cancelled job would retain its full reservation, and no retry or automatic resume was permitted.

The replay rule was fixed before validation: per recipe, replay the first scheduled trial and the first observed Victory, Defeat and Draw, deduplicating the same trial within that recipe. The actual outcomes produced five unique replays: each recipe's first-seed defeat and the first observed victory for each newer recipe. No draw was observed, so none was fabricated or sampled separately.

| Stage | Actual fights |
| --- | ---: |
| Historical anchor normal-Tower run | 100 |
| Previous primary normal-Tower run | 100 |
| Previous exploratory normal-Tower run | 100 |
| Detailed parity replays | 5 |
| **Total / maximum** | **305 / 312** |

Seven fights remained unused. All 300 saved outcomes and scorecards were reconstructed, and every materialized input was checked against its frozen complete recipe, content, settings and execution identity. All five detailed replays matched the complete original report after accounting for the added event log. A separate read-only verification run repeated those checks without executing combat. Replays add no independent statistical observations.

An independent second reader checked the three unchanged complete recipes, all outcomes and seed vectors, exclusion disjointness, frozen input/output hashes, all 16 reservation/completion events, paired contrasts and all five full detailed-report comparisons. The package audit also checked the prior seal inventories and relevant historical-source hashes. Both reviews passed without combat.

The wrapper built with zero warnings and zero errors against copies of the sealed DLLs, using no live project reference or external NuGet package. The first build attempt encountered the known sandbox restriction on NuGet's user configuration; the authorized retry succeeded with broader read access. Python syntax, paired-outcome and Wilson helper checks passed. No production code changed, so a backend suite was not rerun for this experiment; the relevant verification was performed against the unchanged producing executable. No verification command remains blocked.

## Artifacts and reproduction

The new package is [`TestResults/balance/nhalia-validation-20260911`](../TestResults/balance/nhalia-validation-20260911/). It contains:

- `protocol.json`, `seed-ledger.json`, exclusion-source hashes, the immutable `inputs.json`, and the exact three complete recipes.
- The copied producing executable and content, execution/settings identity, wrapper source, and all 300 inputs materialized before the first fight.
- Three complete ordinary Tower runs, their raw reports, append-only execution reservations and completion hashes.
- The predeclared-rule replay plan, five detailed logs, reconstruction receipts and final accounting.
- [`analysis/summary.json`](../TestResults/balance/nhalia-validation-20260911/analysis/summary.json), [`analysis/summary.md`](../TestResults/balance/nhalia-validation-20260911/analysis/summary.md), and [`analysis/outcomes.tsv`](../TestResults/balance/nhalia-validation-20260911/analysis/outcomes.tsv), including all exact seeds and outcomes.

From the repository root, repeat the native read-only validation without new fights:

```powershell
dotnet TestResults/balance/nhalia-validation-20260911/orchestration/runner/Validation.dll verify TestResults/balance/nhalia-validation-20260911 .
```

With Python 3 available, check the final package checksum seal without combat:

```powershell
python TestResults/balance/nhalia-validation-20260911/audit-seal.py --verify
```

Do not run `init`, `run`, `plan-replays` or `replay` against completed evidence. Repeating these seeds is reproduction, not fresh validation. Any new experiment requires a new directory and a predeclared schedule excluding this campaign and earlier available records.

This review and the local experiment package are the only additions. There are no production combat/content edits, migrations, production configuration changes, database actions or deployment implications. The final checksum seal captures the independently reviewed evidence and a copy of this review.
