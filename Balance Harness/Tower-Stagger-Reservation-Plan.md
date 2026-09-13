# Stagger reservation: completed opt-in pilot

Status: **pilot complete; reliability Fail 0/3**. The [pilot review](Tower-Stagger-Reservation-Review.md) records 9,224 fights, both campaign reconstructions and eight matching fixed replay checks. The [precombat plan snapshot](../TestResults/balance/tower-stagger-reservation-20260913/implementation-plan.md) preserves the implemented rule and original pilot specification. The [implementation review](Tower-Stagger-Reservation-Implementation-Review.md) retains its separate 243-test and exact-comparator evidence.

## Hypothesis and isolated change

Test whether requesting enough copies of a chosen recurring-control provider to supply **one first stagger threshold in nominal authored power** helps independent search find stronger complete parties. Nominal power sums one offered application per requested carrier before chance, predicates, targeting, immunity and contribution capping. It does not predict when applications occur, whether they are accepted, or whether the party wins.

Implemented opt-in policy: `independent-stagger-reservation-v10`, with exactly `["compatible-defense-joint", "stagger-reservation-joint"]`. The comparator preserves v9's `compatible-defense-joint`. The new arm uses the same v9 construction and mutation machinery, changing only the requested count for a supported recurring-control provider during guided fresh construction:

1. Derive `T` with the existing `BossStaggerDefinition.CalculateThreshold(requiredPartySize, 0)`. Use the declared full friendly party size, matching World Tower preparation. Do not substitute a replay's damage window, accumulated stagger or surviving-party count.
2. After the unchanged uniform provider selection, derive positive integer `P` from its single supported direct control effect's authored `StaggerPower`.
3. Set `minimum = ceil(T / P)` using safe integer arithmetic. If `1 <= minimum <= requiredPartySize`, replace `random.Next(requiredPartySize + 1)` with `random.Next(minimum, requiredPartySize + 1)` for that nomination. Preserve one count draw and uniform sampling within the new inclusive range.
4. Otherwise retain the original zero-through-party-size count range. Do not remove the provider, clamp an unattainable minimum to the party size, resample a provider, add another random draw or repair a failed placement.

Provider selection, category membership/order, every other category's count distribution, shuffled positions, family/ownership checks, existing-copy handling, core insertion, fillers and final ordering retain their existing algorithms. Keep the one-in-eight whole-party uniform route, empty-pool fallback and v9's 50% core-insertion skip. Preserve every mutation, including collective provider substitution and placement, with the same operator schedule, parent selection, beam/exploration, ranking and discovery objective. The minimum is a requested fresh-construction bias, not a condition imposed on legal parties, finalists or mutations.

## Supported authored route and fallback contract

The metadata builder consumes the independent generation input, frozen typed inventory and target boss definition. For each eligible recurring-control provider, support exactly one distinct direct `ApplyCondition` Stun/Freeze effect route with positive authored `StaggerPower`, positive authored chance, a selected/default trigger and no unknown direct effect/ability definition. Its evidence keys must resolve through the existing typed inventory. Multiple direct routes, nested/delegated routes, missing definitions and unsupported values use the unchanged count range; no speculative power sum is introduced.

The target must have enabled stagger that permits a first break. Disabled/absent stagger, a nonpositive maximum-break limit, an unsupported route or a minimum above the party size uses the unchanged count range. Invalid authored content continues to fail existing validation rather than becoming a silent fallback. Do not use elapsed time, expected proc chance, damage shares, effect duration, healing values, probability margins or threshold growth to adjust the count. The production compiler passes through positive authored stagger power, and Essence progression preserves that field; the existing untrained budget remains fixed.

Keep selected/default triggers, target selectors, effect/trigger predicates, use limits and both chance gates visible in the metadata. Predicates do **not** change provider membership in this experiment. In particular, an effect requiring a low-health target remains eligible and may not contribute early. Any later eligibility hypothesis would require a separate comparison.

Scarce ownership, occupied families and shared-category copies can prevent requested placements. Preserve the current `Add` behavior: a copy already present on a character satisfies that placement without consuming another copy; failed placements remain visible. Do not interpret requested counts as additional occupied slots or achieved total capacity. The current hypothetical-ownership cohort remains the experiment's scope.

## Content audit supporting the proposal

Kharad's initial threshold is **250** for ten participants. These values cover every current eligible recurring-control provider and are audit output, not an ID allowlist or saved-team recipe:

| Authored provider ability | Stagger power per accepted application | Derived minimum | New requested-count range | Activation caveat |
| --- | ---: | ---: | --- | --- |
| Fae's Charm | 40 | 7 | 7–10 | Interval trigger starts at tick 200; authored chance 80%, separate runtime control gate 80%. |
| Feral Pounce | 50 | 5 | 5–10 | Active targeting/cast order; authored chance 100%, separate runtime control gate 80%. |
| Drag Beneath | 35 | 8 | 8–10 | Active targeting/cast order; authored chance 100%, separate runtime control gate 80%. |
| Brutal Charge | 25 | 10 | 10 | Control requires target health below 30%; authored chance 100%, separate runtime control gate 80%. |

Under the current four-provider selection and zero-through-ten count sampler, **14 of 44 equally likely provider/count choices (31.818%)** have at least 250 nominal power from one application per requested carrier. This is conditional on a guided recurring-control nomination, not a party success rate. Other categories/fillers can add providers, different providers can combine, and repeated casts can cross the threshold with fewer carriers. The new distribution raises mean **requested** control count from **5 to 8.75** in this content audit; actual slot use depends on overlapping coverage and placement legality.

The [authored audit](../TestResults/balance/tower-search-hypothesis-assessment-20260913/authored-assessment.json) preserves complete trigger/predicate caveats. Counts come from the threshold and authored powers only. No saved reference recipe, copy count, identity, ancestry, fitness, held-out result or fitted first-break deadline supplies a feature or weight.

## Why this remains an experiment

More control reservations may displace damage, recovery or useful cores. Stochastic gates, target selection, casts, deaths and conditional effects may prevent nominal power from arriving. A broken/recovering target rejects further contributions; later thresholds grow, and only four breaks are possible for Kharad. The unchanged mutation portfolio may also reduce or substitute the initial capacity. None of these effects is modeled as a new fitness score.

The four fixed historical replays show different first-break timing, but they do not establish that earlier breaks cause wins. Their first-death/recovery alignment is not a justified timing cutoff. Search continues to maximize its existing combat objective, including discovering parties above 50%; actual fresh validation and the unchanged reliability gate decide this comparison.

## Implementation contract and completed verification

- Keep changes in offline BalanceHarness and its tests. Introduce only the opt-in policy/method/provenance support, an optional source-derived reservation metadata field and the isolated count branch. Omit new fields for old schemas/policies and preserve their serialization. Leave factory/Tower Lab defaults, global categories, catalogs and gameplay untouched.
- Verify all sealed packages/reviews in the latest receipt, including this assessment, plus current source/content/execution and both catalogs before editing. Use a separate implementation/study directory and retain producing hashes. Do not modify the assessment snapshot or old results.
- Add deterministic fixtures for threshold scaling/rounding and positive-power count bounds; all four current providers including the conditional one; disabled/exhausted/absent stagger; zero/multiple/unknown routes; an unattainable threshold; ownership/family limits; overlaps; uniform/fallback routes; immutable parents; metadata order and reference isolation. Verify old serialization and exact comparator parity. Run backend tests through `build/run-tests.ps1`.
- Keep the new metadata out of fitness/ranking. Validate that the comparator never enters the new count branch and that other categories keep their count algorithm. Record provider evidence, threshold, power, derived minimum, requested/satisfied placements and fallback reason for the new arm without inventing nomination identities in old traces.
- Before a production-like constructor probe or any campaign, freeze its limits and allocation separately. The completed [implementation protocol](../TestResults/balance/tower-stagger-reservation-implementation-20260913/protocol.json) allocated only retrospective parity and synthetic constructor checks; the later combat pilot used its own separately frozen protocol. Fixed fixture examples are tests, not new scientific evidence. Do not allocate tracing seeds merely to populate diagnostics.

## Completed pilot allocation and result

The [frozen protocol](../TestResults/balance/tower-stagger-reservation-20260913/protocol.json) allocated the maximum below. The completed pilot used 9,224 fights in 249.74 execute seconds, with zero retries. It produced reliability Fail 0/3 and family assessments Inconclusive / Inconclusive. These are closed-study limits, not permission to repeat or extend the sample.

| Component | Frozen maximum battles |
| --- | ---: |
| Two methods × three paired restarts × 96 evaluated parties × eight shared discovery seeds | 4,608 |
| Up to twelve generated finalists plus six controls × 256 shared validation seeds | 4,608 |
| Four fixed historical detailed parity checks plus three new primaries and the original anchor on the first validation seed | 8 |
| Total | **9,224** |

Retain the 2,048-proposal limit per arm, no sample extensions, and explicit zero-retry allocation. Exact duplicate validation recipes retain all source associations and leave unused reservations unused. Freeze discovery rank one as primary and rank two as exploratory before validation; do not replace primaries afterward. The original six controls remain outside independent generation; retain the fixed reliability anchor `team-1abe76ca1891d97a91d484f0a3662048` for confirmation only.

Use the unchanged gate: at least two of three new primaries must have adjusted rate lower bound ≥10%, paired lower improvement over the same-restart comparator >0, and paired lower difference against the fixed anchor ≥−10 percentage points. Joint nominal alpha .05 remains split .025 over the full rate family and .025 over six paired comparisons, with component .025/12 for discordance bounds. The ordinary evaluator remains separately .05/family. Keep approximate Wilson limits and repeated-study caveats explicit.

Freeze at most 600 seconds for the measured campaign/reconstruction/replay phases, 1 GiB retained package storage and 512 MiB per campaign. Record build, verification, analysis and documentation timing separately. Any different envelope needs a separately documented protocol before execution. Exclude the union of **every array** in the latest [472,194-seed ledger](../TestResults/balance/tower-loadout-diversity-20260913/seed-ledger.json), including unused and constructor-only reservations, then retain every new reservation even if unused.

All zero-win recipes, rejected proposals, accounting, detailed checks and outcomes must be saved regardless of the gate. Every observed >50% remains a ceiling breach; each intended cohort needs a supported ≥10% viable party. Do not pool closed studies or promote a default/catalog/boss change after this pilot. No near-optimality or complete Tower-family acceptance follows from this bounded comparison.

Kharad remains **Health 3.04881408 / Power 3.85370128**. Keep ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, exact fixed gear, no styles/contributions and all 80 eligible Essences under hypothetical ownership. There are no migrations, configuration changes, deployments, acquisition claims or floor expansion in this proposal.

## Next boundary

The count-only pilot and its [separately frozen zero-combat diagnosis](Tower-Stagger-Reservation-Diagnosis-Review.md) are closed. The diagnosis found that all six new-arm finalists retain nominal Fairy capacity; it selected no remedy. The later [source-grounded assessment](Tower-Loadout-Diversity-Assessment-Review.md) selects a separate [elite-loadout diversity proposal](Tower-Loadout-Diversity-Plan.md) for implementation planning. The [v11 implementation](Tower-Loadout-Diversity-Implementation-Review.md), its [separately frozen pilot](Tower-Loadout-Diversity-Review.md) and its [completed zero-combat v11 diagnosis](Tower-Loadout-Diversity-Diagnosis-Review.md) are complete; no further combat campaign is allocated. Preserve all failed recipes and controls outside generation. No repeat campaign, eligibility change, timing margin, boss tuning, catalog/default promotion or floor expansion follows from these results.
