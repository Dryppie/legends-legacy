# Attribute-defense failure: saved-evidence diagnosis

V8's failure is not explained by universally inactive defense features. The three fixed new-primary replays contain **12 equipped instances of the added effect routes**, of which **11 have positive same-owner application logs**. All nine newly classified effect definitions target **Self**. In the three replays, positive added defenses reach **3, 1 and 3 of ten characters**, respectively; activation and protection of the whole party are different questions.

The full saved validation schedules still show early losses: the new primaries' median first deaths are **35.9, 49.9 and 42.9 seconds**, versus **70.9 seconds** for the original anchor. The diagnosis does not establish which change would prevent those losses. It identifies a smaller, independently checkable mismatch: **two of 16 protection providers offer only Poison- or Burn-specific reduction**, while the frozen Kharad hostile ability/status/summon closure supplies Physical and Magical damage. The [proposed compatibility experiment](Tower-Protection-Compatibility-Plan.md) isolates that applicability rule without assigning learned weights or removing either Essence from ordinary search.

This work used **zero new battles, zero constructor calls and zero new seeds**. All **576 saved search parties / 4,608 discovery records**, **18 validation cells / 4,608 validation records**, and the **four already allocated detailed replays** were read. V8 remains **Fail, 0/3 restarts**, all six new finalists remain **0/256**, and its **134/256 (52.34375%)** control breach remains a tested-family Fail. Earlier **131/256**, **136/256** and the separate **479/1,000 Inconclusive** confirmation retain their original scopes. No samples, confidence intervals or restart gates were combined or updated.

## Scope and integrity

Target: offline `LL/tools/BalanceHarness` evidence and documentation. The [retrospective protocol](../TestResults/balance/tower-defense-diagnosis-20260913/protocol.json) declares the already-known v8 findings, zero battle/seed/constructor/retry budgets, a **120-second analysis cap** and **64 MiB output cap**. This is exploratory analysis of observed outcomes, not a blinded or confirmatory experiment.

Preparation verified all **nine preceding evidence packages**, **68 sealed reviews**, **2,229 C# source files**, current content, both retained catalogs and frozen execution assemblies. Active documentation had legitimately changed after the v8 seal; its old documentation hashes remain a historical snapshot. No sealed receipt was rewritten. The [diagnostic seed ledger](../TestResults/balance/tower-defense-diagnosis-20260913/seed-ledger.json) is an identical copy of v8's: **471,387 distinct seeds across every array**, including unused and constructor-only reservations. The generator was never invoked, and saved controls supplied no generation inputs or weights.

## Saved search and validation results

[Search structure](../TestResults/balance/tower-defense-diagnosis-20260913/search-structure.json) recomputes every arm's discovery ranking, ancestry and parent-relative changes. New v8 best-fresh parties left **68.42–72.02% guardian health**, versus **57.77–66.55%** for v6. Refinement improved v8 remaining guardian health by **4.23, 6.72 and 21.00 percentage points**, but its final discovery leaders still left **67.01, 61.70 and 51.01%**. These are adaptive search observations, not causal method comparisons or an additional reliability test. Frozen primaries were retained.

Each row below uses its original 256-trial validation cell. First death is conditional on an initial-character death; no-death counts are retained separately in the [cell metrics](../TestResults/balance/tower-defense-diagnosis-20260913/saved-cell-metrics.json). The [trial metrics](../TestResults/balance/tower-defense-diagnosis-20260913/saved-trial-metrics.json) preserve all records, including secondary finalists and all six controls.

| Role / generation seed | Saved recipe | Wins | Median first death | Mean guardian HP remaining | Mean duration |
| --- | --- | ---: | ---: | ---: | ---: |
| V6 primary / 815430299 | `team-0a4daedbc2fc693876bf7903b01425bd` | 0/256 | 42.9s | 49.81% | 71.71s |
| V8 primary / 815430299 | `team-049a9eb42c06b723b32207441f2b5a99` | 0/256 | 35.9s | 65.25% | 66.56s |
| V6 primary / -1881549266 | `team-59cc566d4950fa8afe0d73419f0b6aa9` | 0/256 | 42.9s | 57.89% | 78.74s |
| V8 primary / -1881549266 | `team-61fa75cd9ef4f884a242aa7234349e2b` | 0/256 | 49.9s | 63.39% | 82.99s |
| V6 primary / -1694738814 | `team-510b04e5685c5e59dcf8a7455dc9aade` | 1/256 | 56.0s | 42.80% | 95.17s |
| V8 primary / -1694738814 | `team-0183fd26d2331b9f7808cc3a924e6da0` | 0/256 | 42.9s | 53.46% | 86.00s |
| Fixed anchor | `team-1abe76ca1891d97a91d484f0a3662048` | 81/256 | 70.9s | 14.47% | 108.71s |

The other nonzero generated finalist was v6's secondary at **6/256**. No v8 secondary replaces its preselected primary. Healing, regeneration, barriers and attacks are retained in the cell metrics; their totals depend on battle duration and survival and cannot independently identify an optimal defense or damage composition.

## Activation, recipients and early deaths

The four predeclared detailed replays use the same previously allocated seed, **1710924649**, and all four are defeats. Their complete summaries reconcile with their corresponding saved compact trials. Matching v6-primary detailed replays were not allocated in v8; this diagnosis compares v6's full saved summaries without creating additional traces.

| Fixed replay | Positive added-effect instances / equipped | Recipients with a positive added defense | First death | Seal share of incoming final-health-damage telemetry |
| --- | ---: | --- | ---: | ---: |
| V8 primary 1 | 3/3 | Slots 2 and 5: Resistance; slot 7: Armor | 42.0s, slot 1 | 59.04% |
| V8 primary 2 | 4/4 | Slot 10: four conditional DamageReduction increments | 42.0s, slot 1 | 61.43% |
| V8 primary 3 | 4/5 | Slots 1, 3 and 8: Armor | 42.9s, slot 1 | 71.48% |
| Fixed anchor | 6/6 | Slots 1 and 2: Armor; slot 1: conditional DamageReduction | 59.6s, slot 8 | 73.63% |

[Effect observations](../TestResults/balance/tower-defense-diagnosis-20260913/defense-activation.json) retain exact effect IDs, owners, recipients, event indexes, timestamps, magnitudes and removals:

- Primary 1's Resistance effects applied at combat start. Slot 2 logged +10, expired at 49.9s, reapplied and expired again at 51.9s; slot 5 logged +14 and expired at 55.5s. Both recipients survived the party's first death at 42.0s. Their presence does not establish protection of slot 1 or explain the defeat.
- Primary 2's four Hollow Core increments each logged +1 DamageReduction on slot 10, first at **49.9s**, then 67.9, 71.9 and 83.9s. None activated before the party's first death at 42.0s. These are separate threshold effects on one recipient, not four independent defensive samples.
- Primary 3 logged Armor additions on three recipients, including the first character to die. Its equipped Layered Mud Armor route had no positive effect observation. Missing log evidence is not declared inactivity. The new Armor routes do not mitigate Magical damage through the engine's typed-defense path.

The [engine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) maps Armor to Physical/Bleed and Resistance to Magical/Burn/Poison/Shadow before generic damage reduction and barriers. Both Physical and Magical damage are substantial in these traces; there is no basis for discarding Armor or selecting a new weight from the dominant observed pulse. Positive logs establish application, not marginal damage prevented. Synchronized deltas are rounded; exact attribute trajectories and uptime are not inferred. Prevention totals combine all defenses, and final-health-damage telemetry can include overkill. These four traces cannot determine deaths a defense would prevent or causal search-policy superiority.

## Protection applicability gap

The [static semantics audit](../TestResults/balance/tower-defense-diagnosis-20260913/defense-semantics.json) resolves all nine added effect definitions and all **16 protection providers** from frozen content. The hostile closure includes Kharad's four native abilities, two inert pillars and Resonance status. Its damage effects are Physical Crushing Verdict and Magical Seal; the pillars cannot attack and have no abilities. Natural Physical attacks add no new channel. Neither Poison nor Burn is supplied by this authored closure, and neither appears as incoming damage in the four fixed replays.

Two providers are categorized as protection solely through typed reductions: Poisonous Rat reduces Poison damage and Smolder Rat reduces Burn damage. Their other abilities may still be useful. The proposed rule therefore concerns their nomination as protection for this encounter, not their legal availability or overall quality.

| Generation seed | V6 fresh parties containing either provider | V8 fresh parties containing either provider |
| ---: | ---: | ---: |
| 815430299 | 29/42 | 25/42 |
| -1881549266 | 34/42 | 26/43 |
| -1694738814 | 30/42 | 28/42 |

[Exposure findings](../TestResults/balance/tower-defense-diagnosis-20260913/defense-exposure.json) retain all 576 evaluated parties and all 18 validation recipes. In total **79/127 fresh v8 parties** contain either provider; this is a structural count, not a pooled combat rate. **None of the three selected v8 primaries contains either provider**, so this mismatch does not explain their direct failure. Presence also does not establish that the protection reservation inserted that Essence; ordinary fill, other categories and uniform generation can supply it.

Across all six arms, the median fresh character counts for added routes are **Armor 1, Resistance 0 and DamageReduction 0**. Counts alone do not justify minimum copy quotas, learned weights, control templates or extra placement frequency. The bounded proposal addresses only explicitly incompatible protection evidence and retains unknown or broadly applicable routes.

## Next action and verification

The [proposed next experiment](Tower-Protection-Compatibility-Plan.md) compares unchanged v8 with one conservative encounter-compatibility filter. It has not been implemented or frozen for combat. First verify a complete content-derived threat description and unchanged comparator behavior; ambiguous damage routes must retain existing eligibility. If those prerequisites cannot be met within the isolated scope, retain the diagnosis without running a pilot. Do not infer that removing two nominations will meet the reliability gate.

`python -B analyze-records.py` processed all 9,216 records in **4.48 seconds**; `python -B analyze-defense.py` completed its static/exposure/replay analysis in **0.24 seconds**. The [independent final verifier](../TestResults/balance/tower-defense-diagnosis-20260913/verify-final.py) checks raw records, frozen rankings, detailed-summary parity, effect observations, source/package hashes, unchanged seeds, links and `git diff --check`; the [receipt](../TestResults/balance/tower-defense-diagnosis-20260913/final-verification.json) seals the package. Backend tests were not rerun because runtime source and assemblies did not change; v8's **182 passing tests** remain the prior result. No required verification command remains blocked.

Changed files are this new review, the compatibility plan, active handoff/discovery/loadout/policy documentation and README, plus local diagnostic scripts and derived evidence. All preceding reviews and evidence packages remain sealed. Kharad stays **Health 3.04881408 / Power 3.85370128**, with the same gear, five untrained Essences per character and hypothetical ownership. There are no gameplay changes, migrations, configuration changes, database changes or deployments. Retuning, default/catalog promotion and floors 6–11 remain deferred; near-optimality and practical acquisition remain unestablished.
