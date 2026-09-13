# Attribute-defense coverage: implementation and bounded comparison

The opt-in **`independent-defense-v8`** policy completed its frozen comparison with unchanged v6. Search reliability is **Fail: 0/3 passing restarts, with 2 required**. Of the **6 new-arm finalists, 0 recorded a held-out win**. Across both methods, 2 of 12 finalists recorded a win. The six saved controls measured **20.31–52.344%**. Ordinary and joint-adjusted tested-family assessments are **Fail** and **Fail**, respectively.

This completes one isolated feature change and one bounded comparison. Defaults and Kharad are unchanged. V7's Fail, the **131/256 (51.17%)** and **136/256 (53.125%)** observed control breaches, and the separate **479/1,000 (47.90%)** Inconclusive confirmation with adjusted interval **43.76–52.07%** remain sealed. No historical samples or restart gates were pooled; secondaries never replaced the primaries chosen before validation. This study cannot establish complete Tower-family acceptance, practical acquisition or near-optimality.

## Implemented change

The [saved-evidence diagnosis](Tower-Completion-Diagnosis-Review.md) found five eligible providers with direct attribute mitigation missing from guided protection coverage. [TowerAttributeDefense.cs](../LL/tools/BalanceHarness/TowerAttributeDefense.cs) adds a content-derived classifier for supported friendly-targeted **Armor, Resistance and DamageReduction** effects. It augments the existing protection category without adding categories, count targets or learned weights. The frozen output contains **71 coverage entries, compared with 66 legacy entries**, including nine additional effect evidence keys and five new providers.

The classifier follows three engine value rules. `ModifyAttribute` accepts a positive constant or supported nonnegative attribute scaling, rejecting negative or unsupported event/condition/status/summon scaling. `ModifyAttributePercentOfInitial` uses its positive coefficient and ignores BaseValue, as the engine does. `SynchronizeAttributePerLivingNonSummonedAlly` requires a positive ally cap and a positive coefficient, or a positive base only when the coefficient is absent. Recipient attributes, rounding, conditions, chance, caps, duration and living allies can still prevent benefit; these limitations are saved with augmented features. Unsupported and nested routes remain ordinarily reachable rather than being inferred as reliable defense.

Direct abilities with no triggers inherit the engine's active/passive default trigger. Explicit triggers must select the effect. Conditional selected triggers are retained as hypotheses with ability, trigger and effect evidence; they are not treated as guaranteed uptime. The added providers are derived from all eligible content, without reading saved Essence IDs/counts, recipes, ancestry, historical fitness or held-out results.

[Generation policy selection](../LL/tools/BalanceHarness/TowerBossGeneration.cs) recognizes exactly `["collective-joint", "defense-joint"]`. [Generation mechanics](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) stores a separate optional `DefenseCoverage` collection. Only the new arm selects it; original Coverage serialization remains unchanged for v1–v7 and for the comparator. [Coverage validation](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) requires all old entries and limits additions to protection. The [provenance contract](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) restricts `fresh-defense` to the new arm with zero parents or reference ancestry.

Both arms retain v6's 50% core-insertion skip, all v6 refinements, group ordering, uniform route, ownership/family legality, filler weights, final ordering, ranking and beam/exploration. V7's unconditional insertion was not combined with this experiment. Coverage-aware substitutions use each arm's own coverage collection. Global inventory intent signals remain unchanged. The methods use separate deterministic RNG streams, so the comparison assesses search policies and is not an identical-parent causal ablation.

## Precombat verification

All **eight prior packages** and sealed reviews verified unchanged, as did content, both catalogs and all four gameplay assemblies. The new harness has a separately frozen execution identity. All **182 relevant regression tests passed**, covering eligibility/value semantics, conditions, negative/zero values, unsupported routes, floor/slot budgets, deterministic and uniform behavior, empty/missing features, scarce ownership, feature isolation, provenance and compact interrupted/resumed parity. The new tests also verify that unchanged coverage produces the same complete constructor choices.

All **six historical v6 arms reconstructed exactly without combat**. A separate compatibility probe placed v6's three collective arms under v8 and matched their complete hashes. Its other three arms used **288 synthetic zero-win evaluations** solely to exercise the new policy; they were not battles or fresh search evidence. The probe used excluded historical generation seeds, added no seeds and completed within 60 seconds. Historical lookup scores remained confined to compatibility checks. [V6 reconstruction](../TestResults/balance/tower-attribute-defense-20260913/v6-comparator-reconstruction.json) and [v8 isolation](../TestResults/balance/tower-attribute-defense-20260913/v8-comparator-verification.json) preserve the receipts.

Removing or reversing all six references produced [identical independent generation inputs](../TestResults/balance/tower-attribute-defense-20260913/generation-boundary-verification.json). Independent preflight matched all nine additional direct effects against the prior all-content audit and verified that non-protection entries and old protection evidence remained intact. The **48 mechanic cores and 66 original coverage entries** are unchanged. No additional constructor tracing was performed. See [preflight](../TestResults/balance/tower-attribute-defense-20260913/precombat-verification.json).

## Frozen protocol and budget

Target: offline BalanceHarness, Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. Budget: ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, exact fixed gear, no styles or contributions. Full eligible pool including Rare under hypothetical ownership; practical acquisition remains unverified.

The [protocol](../TestResults/balance/tower-attribute-defense-20260913/protocol.json), [design](../TestResults/balance/tower-attribute-defense-20260913/experiment-design.json) and copied [implementation plan](../TestResults/balance/tower-attribute-defense-20260913/implementation-plan.md) froze before pilot combat. Protocol SHA-256: `7ca999be2ff28304c20c5a71383825c9cd059e8763c65f00304f45d87447a8f5`.

| Phase | Allocation | Maximum fights |
| --- | --- | ---: |
| Discovery | 2 methods × 3 restarts × 96 parties × 8 shared fresh seeds | 4,608 |
| Validation | Top 2 per arm plus all 6 controls; at most 18 recipes × 256 fresh seeds | 4,608 |
| Diagnostics | 4 old parity replays + first validation seed for 3 new primaries and original anchor | 8 |
| **Total** | **Zero combat retries; no automatic resume** | **9,224** |

Each arm permits at most 2,048 proposals. Execution is capped at 600 seconds, the package at 1 GiB and each campaign at 512 MiB. The [ledger](../TestResults/balance/tower-attribute-defense-20260913/seed-ledger.json) excludes **471,118 prior seeds**, including every unused reservation and constructor-only seed. Fresh generation, discovery, validation and unused stage reservations are mutually disjoint. Pairing across arms/restarts does not enlarge the independent sample per recipe.

The [complete validation family](../TestResults/balance/tower-attribute-defense-20260913/validation-selection.json) froze before validation. Rank one remains primary and rank two exploratory. At least two of three new primaries must satisfy adjusted rate lower ≥10%, paired lower improvement over the same-restart v6 primary >0, and paired lower difference from original anchor `team-1abe76ca1891d97a91d484f0a3662048` ≥−10 percentage points. Neither the other five controls nor secondaries may replace these comparisons.

Joint nominal alpha .05 splits .025 over all final rate cells and .025 over six paired comparisons; discordance components use `.025/12`. Wilson coverage is approximate within this fixed study, with no lifetime repeated-study guarantee, historical pooling, optional extension or held-out reranking. The ordinary evaluator separately uses `.05/18`. Any observed rate >50% remains a ceiling breach; every intended-budget cohort needs supported ≥10% viability. Search reliability and family acceptance remain separate.

## Results

Held-out wins out of 256, primary / secondary:

| Generation seed | Unchanged v6 wins | Attribute-defense wins | New primary gate |
| ---: | ---: | ---: | --- |
| 815430299 | 0 / 0 | 0 / 0 | Fail |
| -1881549266 | 0 / 0 | 0 / 0 | Fail |
| -1694738814 | 1 / 6 | 0 / 0 | Fail |

| Saved control | Wins | Observed rate | Joint-adjusted interval |
| --- | ---: | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 134/256 | 52.344% | 42.47–62.04% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 81/256 | 31.641% | 23.21–41.49% |
| `team-38248d838d1db9634fd82536c177df0a` | 81/256 | 31.641% | 23.21–41.49% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 62/256 | 24.219% | 16.76–33.66% |
| `team-693ffa8ec0b654154a06722aba06a968` | 52/256 | 20.312% | 13.49–29.42% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 59/256 | 23.047% | 15.77–32.40% |

The strongest control's **134/256 (52.344%)** is an observed ceiling breach and makes this tested family Fail. Its adjusted interval is **42.47–62.04%**; uncertainty does not erase an observed breach. Its interval crosses 50%, so this does not establish an underlying win rate above 50%; the observed-policy failure still stands. Observed validation ceiling breaches: **1**; discovery observations above 50%: **0**. [Analysis](../TestResults/balance/tower-attribute-defense-20260913/analysis.json) retains every cell, all six paired intervals and each restart's three gate components. No observation is dropped to obtain acceptance. The `coverageCharacterCounts` field uses the unchanged 66-entry classification for comparison; added-provider exposure is reported separately below.

Search evaluated **576 parties from 590 proposals**. Added-provider exposure includes the retained uniform route and ordinary sampling in either method; it measures recipe structure, not defense strength.

| Seed | Method | Fresh parties | Median fresh characters carrying added providers | Evaluated parties with discovery wins |
| ---: | --- | ---: | ---: | ---: |
| 815430299 | collective-joint | 42 | 1.0 | 0 |
| 815430299 | defense-joint | 42 | 2.0 | 0 |
| -1881549266 | collective-joint | 42 | 1.0 | 0 |
| -1881549266 | defense-joint | 43 | 2 | 0 |
| -1694738814 | collective-joint | 42 | 1.0 | 4 |
| -1694738814 | defense-joint | 42 | 1.0 | 0 |

[Defense findings](../TestResults/balance/tower-attribute-defense-20260913/defense-findings.json) retain per-arm histograms and all verified collective substitutions. Of 18 accepted new-arm collective substitutions, 1 involved an added provider and 0 improved discovery ranking over its parent. The fixed [detailed replay diagnostics](../TestResults/balance/tower-attribute-defense-20260913/replay-diagnostics.json) and [finalist recipes and ancestry](../TestResults/balance/tower-attribute-defense-20260913/trace-findings.json) are available for later diagnosis without new combat. Adaptive substitutions and small descriptive counts cannot establish causal operator superiority.

## Verification and handoff

All **9,224 fights** started and completed with zero retries or lost attempts. Measured phases totaled **247.20 seconds**; the execute invocation took **250.83 seconds**. Approximately **342 MiB** is retained, with exact pre-receipt bytes in [final verification](../TestResults/balance/tower-attribute-defense-20260913/final-verification.json). Both full campaigns reconstructed without combat and all eight detailed parity checks passed. Independent manifests, journals, schedules, recipes, selection, accounting and native interval checks passed.

Backend builds succeeded with five existing unrelated warnings and zero errors after correcting nullable annotations in the new tests before freezing. The local driver restored with its source-free NuGet configuration and approved scoped access, then built with zero warnings/errors. Backend tests ran through `build/run-tests.ps1 -NoBuild -Configuration Release` with the twelve relevant Tower class filters; the saved [receipt](../TestResults/balance/tower-attribute-defense-20260913/regression-tests.trx) records **182/182 passing**. No required command remains blocked. `git diff --check` passed.

Changed files: four existing harness sources listed above plus new `TowerAttributeDefense.cs`; two existing discovery/bulk test files plus new `BalanceHarnessTowerAttributeDefenseTests.cs`; this review, active plans/policy/handoffs and README. Gameplay source/content, gear, catalogs and defaults are unchanged. There are no migrations, configuration changes, database changes or deployments.

All **18 seed-free recipes** and measurements are saved in [validated-builds.json](../TestResults/balance/tower-attribute-defense-20260913/validated-builds.json), reusable without rediscovery or catalog promotion. This experiment is closed; do not extend its sample or automatically launch another variant. Future protocols must exclude every array in its ledger and retain all six controls outside generation, along with all historical ceiling findings. Boss retuning, default promotion and new floors remain deferred.
