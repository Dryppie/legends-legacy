# Attribute-defense coverage: completed bounded experiment

Status: **complete** — see the [implementation and results](Tower-Attribute-Defense-Review.md). The [original plan](../TestResults/balance/tower-attribute-defense-20260913/implementation-plan.md) and [executable protocol](../TestResults/balance/tower-attribute-defense-20260913/protocol.json) remain sealed. The specification and precombat requirements below describe completed work; the [current handoff](Tower-Coverage-Replication-Plan.md) records the next-work boundary. The [zero-combat completion diagnosis](Tower-Completion-Diagnosis-Review.md) identifies a content-extraction gap, not a proven winning search policy.

## Implemented change

V8 added an opt-in `independent-defense-v8` policy with exactly `["collective-joint", "defense-joint"]`. Use unchanged v6 as the comparator and as the new arm's construction/refinement base. V7 did not meet its advancement gate; its unconditional core insertion is not combined with this experiment. Both arms retain v6's 50% insertion skip.

Only the new arm's protection coverage adds supported positive, friendly-targeted attribute-mitigation effects. Extend the existing protection category; do not add a new category or increase its placement frequency. The initial supported attributes are Armor, Resistance and DamageReduction, derived from their runtime meaning. Existing protection entries remain present. Supported operations are ModifyAttribute, ModifyAttributePercentOfInitial and SynchronizeAttributePerLivingNonSummonedAlly, with verified positive-value and activation checks and explicit limitations. These are the operation families found by an audit of all 80 eligible Essences, not an allowlist of Essence IDs.

The audit identified nine direct effect keys across six Essences; five previously lacked protection coverage and intent. The verified classifier extended only new-arm protection coverage from 66 to 71 entries and records activation/value limitations. Global inventory intent signals remain unchanged by design. Unsupported routes remain unclassified and ordinarily reachable; classification does not establish effective uptime or strength.

Preserve existing group order, provider/count sampling, ownership/family legality, uniform route, core insertion, filler weights, final ordering, beam/exploration, mutation frequencies, ranking and fight allocations. Newly classified providers must be available wherever the new arm already consumes protection coverage, including coverage-aware substitutions. Do not change global inventory intent signals or filler weighting in this experiment; that would add a second behavioral change. Classify all three mitigation attributes even though the observed pulse is Magical: selecting a winning damage-type weight from held-out telemetry is outside this hypothesis.

Keep original Coverage serialization unchanged for v1–v7 and for the v8 comparator. Isolate the augmented feature collection to the new arm. Follow the existing typed inventory and coverage patterns; do not use the retained-control replacement policy, which consumes an anchor and rejects bosses with adds. No reference ID, count, recipe, ancestry, discovery score or held-out measurement may enter independent generation or choose the new features.

## Precombat verification requirements — completed

1. Verify all seven prior packages plus the new diagnosis, sealed reviews, current content/execution and both catalogs. Preserve the dirty working tree. Use a new experiment directory.
2. Audit the supported operation/value combinations against the engine. Cover positive and negative values, zero chance, enemy targets, conditional routes, zero/negative scaling, nested or unsupported routes, family conflicts and scarce ownership. Do not treat a listed conditional defense as guaranteed uptime. Record limitations in the generated features.
3. Verify complete legal deterministic construction across existing floor/slot budgets, uniform reachability, immutable inputs, missing/empty-feature behavior and provenance. Restrict the new policy and method order explicitly.
4. Reconstruct all six saved v6 arms without combat and verify exact v6 comparator hashes under the new policy. Existing policies, default v1 and v7 must retain their feature/input/recipe behavior. Removing or reversing all six saved references must produce identical independent inputs.
5. Run relevant backend tests through `build/run-tests.ps1`. If constructor tracing is needed, predeclare at most 128 new seeds, 256 calls, 60 seconds and zero fights, with exact production/trace equality. Reserve those seeds before use and exclude them from every combat schedule.
6. Freeze the executable protocol only after implementation and verification. Stop for a compatibility, legality or boundary failure; do not consume the battle budget while repairing it.

## Frozen comparison

Use Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. Keep ten level-40, tier-1, rank-2 Standard characters; five level-1 unascended/unevolved Essences each; exact existing fixed gear; no styles or contributions. Full eligible pool including Rare remains hypothetical ownership, with practical acquisition unverified.

Generate three fresh restarts for each method, with 96 evaluated parties and eight shared discovery seeds per arm, at most 2,048 proposals per arm. Freeze discovery rank one as primary and rank two as exploratory, then validate all distinct finalists plus all six saved controls on exactly 256 fresh seeds. Preserve duplicate source associations. Controls remain outside generation.

| Phase | Maximum fights |
| --- | ---: |
| Discovery: 2 × 3 × 96 × 8 | 4,608 |
| Validation: at most 18 × 256 | 4,608 |
| Fixed diagnostics: 4 old parity replays; first validation seed for 3 new primaries and original anchor | 8 |
| **Total** | **9,224** |

Keep a 600-second execution cap, 1 GiB package cap, 512 MiB per campaign, zero combat retries and no automatic resume or optional extension. These limits were frozen in the linked executable protocol and the experiment completed within them. Preparation excluded the then-current 471,118-seed union from **every array** of the [completion-diagnosis ledger](../TestResults/balance/tower-completion-diagnosis-20260913/seed-ledger.json), including unused reservations and constructor-only seeds. The diagnosis itself added no seeds; the completed v8 reservation brought its then-current union to **471,387**.

Require at least two of three new primaries to satisfy all unchanged reliability components: adjusted win-rate lower bound ≥10%; paired lower improvement over the same-restart v6 primary >0; and paired lower difference from original anchor `team-1abe76ca1891d97a91d484f0a3662048` ≥−10 percentage points. No secondary or other control may replace either anchor after validation.

Joint nominal alpha .05 splits .025 across all final rate cells and .025 across six paired comparisons, using component alpha .025/12 for discordance bounds. Keep approximate Wilson coverage and the absence of a lifetime repeated-study guarantee explicit. The ordinary evaluator separately uses .05 divided by the complete family. No historical sample pooling or held-out reranking.

## Decision and handoff

The hypothesis tested whether recognizing direct attribute mitigation in guided protection placement would improve search reliability. It failed the predeclared gate. Classification alone does not establish activation, sustained damage or effective recipients, and this result does not isolate the cause of failure. All outcomes and reusable recipes remain saved. Do not increase budgets or add a second feature change after observing results.

Search advancement and 10–50% family acceptance remain separate. Retain the separate observed **131/256**, **136/256** and **134/256** control ceiling breaches and the separate **479/1,000** Inconclusive confirmation. Any new observed rate above 50% remains a breach. Each intended-budget cohort needs supported ≥10% viability; no small pilot establishes complete Tower-family acceptance or near-optimality.

No boss retuning, default/catalog promotion, equipment improvement, migration, configuration change, deployment or next-floor batch follows from this plan. The isolated change is implemented and this comparison is closed. Any subsequent experiment needs its own bounded hypothesis and verified protocol before combat.

## Completed result

**Attribute-defense pilot — 13 September 2026:** [opt-in `independent-defense-v8`](Tower-Attribute-Defense-Review.md) completed 9,224 fights in 247.20 seconds of measured phases with 182 passing tests. Reliability is **Fail (0/3; 2 required)**. All six new defense-arm finalists won **0/256**; the two nonzero finalists were unchanged-v6 comparators at **1/256** and **6/256**, while the other four comparator finalists won zero. Six controls measured 20.31–52.344%; the 18-cell assessment is **Fail** (joint-adjusted **Fail**). Only new-arm protection coverage grew from 66 to 71 entries. V6 compatibility, all eight prior packages and all earlier ceiling findings are preserved; no pooled acceptance, retuning, catalog/default promotion or new floors.

The strongest control's **134/256 (52.34375%)** is an observed ceiling breach. Its joint-adjusted interval **42.47–62.04%** crosses 50%, so an underlying rate above 50% remains unestablished. The [final receipt](../TestResults/balance/tower-attribute-defense-20260913/final-verification.json) closes the experiment. Its ledger is historical. The later [defense diagnosis](Tower-Defense-Diagnosis-Review.md) and [`independent-compatible-defense-v9` comparison](Tower-Protection-Compatibility-Review.md) are now complete. Future preparation must exclude every array of the latest ledger (**472,194 distinct seeds**) in the [current handoff](Tower-Coverage-Replication-Plan.md#seed-exclusions-and-evidence-preservation). The later [coverage-realization diagnosis](Tower-Coverage-Realization-Diagnosis-Review.md) is complete; the [completed reusable report](Tower-Coverage-Diagnostics-Review.md) adds visibility. The later [stagger-reservation assessment](Tower-Search-Hypothesis-Assessment-Review.md) selected the count-only proposal. Its [v10 implementation](Tower-Stagger-Reservation-Implementation-Review.md) and parity checks were followed by the [completed v10 pilot](Tower-Stagger-Reservation-Review.md). The [zero-combat v10 diagnosis](Tower-Stagger-Reservation-Diagnosis-Review.md) is also complete. The later [elite-loadout assessment](Tower-Loadout-Diversity-Assessment-Review.md) selects a [separate opt-in proposal](Tower-Loadout-Diversity-Plan.md) for implementation planning. The [v11 implementation](Tower-Loadout-Diversity-Implementation-Review.md), its [separately frozen pilot](Tower-Loadout-Diversity-Review.md) and its [completed zero-combat v11 diagnosis](Tower-Loadout-Diversity-Diagnosis-Review.md) are complete; no further combat campaign is allocated. No new combat allocation is selected.
