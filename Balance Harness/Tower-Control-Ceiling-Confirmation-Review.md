# Saved-control ceiling confirmation

Fresh confirmation of `team-1a924a7cfff12298633bee909cdea4ad` measured **479/1,000 (47.90%)**, with six-cell-adjusted interval **43.76–52.07%**. Its repeatable ceiling status is **Unresolved**. The fresh adjusted interval still crosses 50%; the underlying ceiling question remains unresolved. The fixed sample will not be extended.

The unchanged evaluator returns **Inconclusive** for the complete six-control confirmation family. The fresh six-control family cannot be accepted. Observed fresh ceiling breaches: **0**. The target's earlier **131/256 (51.17%)** observation and the v6 pilot's **Fail** remain sealed; these new results neither erase that observation nor pool its samples. Independent-search reliability remains **Fail (0/3 in v6)**, and near-optimality is unestablished.

## Scope and frozen decision

This is one new, fixed confirmation experiment selected after the [collective-provider pilot](Tower-Collective-Provider-Review.md). That pilot's adjusted target interval was 41.33–60.92%, so its observed ceiling breach did not establish an underlying rate above 50%. [Historical trigger evidence](../TestResults/balance/tower-control-ceiling-confirmation-20260913/historical-trigger.json) records the exact recipe, prior count, interval and source hash.

All six saved controls were frozen before fresh combat, including both earlier independently discovered v4 winners and the original gate anchor. There was no new discovery, recipe mutation, gear increase, family reduction, paired-superiority hypothesis or setting search. Each control received the same 1,000 fresh seeds, with no post-result replacements or sample extension. The earlier zero-win generated finalists were not new candidates in this control-only confirmation; all previous packages and their complete families remain preserved. This is not a new complete Tower-family assessment.

Target service: offline `LL/tools/BalanceHarness`. Kharad remains **Health 3.04881408 / Power 3.85370128**. The exact floor-5 budget remains ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, fixed gear, no Combat Styles or contributions. Saved recipes retain hypothetical ownership, including Rare Essences; practical acquisition remains unverified.

The [protocol](../TestResults/balance/tower-control-ceiling-confirmation-20260913/protocol.json), [experiment design](../TestResults/balance/tower-control-ceiling-confirmation-20260913/experiment-design.json) and [complete definition](../TestResults/balance/tower-control-ceiling-confirmation-20260913/definition.json) froze before any confirmation combat. Protocol SHA-256: `2fb4e3efa619f47c7abe5366af0f7b569f9662482be92d59c1167fe66df99cea`.

- The unchanged `bonferroni-wilson-95-v1` evaluator allocates `.05/6` to each of six rate intervals. Pointwise intervals remain descriptive. Approximate simultaneous coverage applies within this fixed experiment; it is not a lifetime repeated-study guarantee.
- For the fixed target, adjusted lower **>50%** supports an underlying breach; adjusted upper **≤50%** supports at-most-ceiling performance. An interval crossing 50% leaves the underlying question unresolved.
- Any fresh observed rate **>50%** separately fails acceptance, even if uncertainty crosses 50%. A scoped family Pass requires every adjusted upper ≤50% and at least one adjusted lower ≥10% in every intended cohort. Missing or invalid cells prevent acceptance.
- Historical observations remain visible, without pooling or retrospective reassessment. Search reliability and overall Tower balance cannot be inferred from this six-control sample.

The [ledger](../TestResults/balance/tower-control-ceiling-confirmation-20260913/seed-ledger.json) excludes **469,465** seeds: the union of every array in the immediately preceding ledger, including unused reservations. The 1,000 new seeds are unique and disjoint from that union. The same seeds across controls permit matched sampling; the diagnostic replays duplicate trials and add no statistical sample size.

| Phase | Frozen allocation | Fights |
| --- | --- | ---: |
| Fresh confirmation | 6 exact controls × 1,000 seeds | 6,000 |
| Historical parity | 4 fixed old detailed reports | 4 |
| New parity | First confirmation seed for each control in frozen order | 6 |
| **Total** | **Zero combat retries, no automatic resume** | **6,010** |

Caps: 600 seconds for the execute invocation, 1 GiB for the complete package and 512 MiB for the confirmation campaign. An inconclusive result is allowed; there is no optional extension to obtain a pass.

## Fresh results

| Control | Wins | Observed rate | Six-cell-adjusted interval | Observed >50% |
| --- | ---: | ---: | ---: | --- |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 281/1,000 | 28.10% | 24.51–31.99% | No |
| `team-693ffa8ec0b654154a06722aba06a968` | 224/1,000 | 22.40% | 19.12–26.06% | No |
| `team-1abe76ca1891d97a91d484f0a3662048` | 308/1,000 | 30.80% | 27.09–34.77% | No |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 282/1,000 | 28.20% | 24.61–32.09% | No |
| `team-38248d838d1db9634fd82536c177df0a` | 338/1,000 | 33.80% | 29.98–37.85% | No |
| `team-1a924a7cfff12298633bee909cdea4ad` (target) | 479/1,000 | 47.90% | 43.76–52.07% | No |

All raw trial outcomes, draws, pointwise intervals and adjusted intervals are retained. [Independent analysis](../TestResults/balance/tower-control-ceiling-confirmation-20260913/analysis.json) agrees with the [native evaluator](../TestResults/balance/tower-control-ceiling-confirmation-20260913/confirmation/assessment.json) and preserves the target's separate repeatable-ceiling classification. Recipes are compared exactly with the frozen historical controls, including complete characters, equipment, Essence order and starting state.

The original 131/256 observation is selection evidence for this new study; its samples do not appear in these rates. A narrower confirmation interval does not change the earlier experiment's recorded result. Likewise, a six-control result cannot establish that no stronger legal party exists or accept the server-wide progression goal.

## Verification, resources and reuse

All **6,010 fights** started and completed, with no retries or lost attempts. Measured phases totaled **145.11 seconds**; the complete execute invocation took **147.40 seconds**. The package retains approximately **202 MiB**, with exact pre-receipt bytes in [final verification](../TestResults/balance/tower-control-ceiling-confirmation-20260913/final-verification.json).

The complete compact campaign reconstructed without combat. All four old detailed reports matched exactly, and all six new detailed replays matched their compact summaries at the first fresh seed. Independent checks verified manifests, compressed records, every trial's seed/order/recipe, both attempt journals, all 6,000 outcomes, frozen definitions and native confidence bounds.

All **88 relevant backend tests passed** through `build/run-tests.ps1 -NoBuild -Configuration Release`, filtering `BalanceHarnessTowerBalanceEvaluatorTests`, `BalanceHarnessTowerBulkTests` and `BalanceHarnessTowerCompactTests`; [test receipt](../TestResults/balance/tower-control-ceiling-confirmation-20260913/regression-tests.trx). The test assembly and every harness/gameplay source and execution assembly remained unchanged from the verified v6 state. The offline driver restored using an explicit source-free NuGet configuration with scoped approved access, then built with `dotnet build --configuration Release --no-restore` with zero warnings/errors. All required commands completed; no command remains blocked.

This turn adds the local experiment driver/evidence and this review, and updates the active discovery, balancing, acceptance and replication handoffs plus the harness README. It changes no harness/gameplay source, content, catalog, default, migration, configuration, database or deployment. All four preceding experiment packages and sealed reviews are unchanged.

All six seed-free recipes, fresh measurements and historical source references are saved in [validated-builds.json](../TestResults/balance/tower-control-ceiling-confirmation-20260913/validated-builds.json). Reuse these recipes directly without rediscovery or catalog promotion. Any future experiment must exclude every array of this [latest ledger](../TestResults/balance/tower-control-ceiling-confirmation-20260913/seed-ledger.json). No further search campaign, sample extension, calibration or floor batch is started by this confirmation.
