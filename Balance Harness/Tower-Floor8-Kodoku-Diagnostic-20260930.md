# Floor 8: Kodoku replay diagnostic — 30 September 2026

**Latest floor-8 Miasma trial (30 September):** The [isolated Miasma trial](Tower-Floor8-Miasma-Trial-20260930.md) closed **incomplete after a resource stop**. One batch completed **5,664 fights / 32 fresh seeds** in **341.161s**; its doubled next-batch estimate (**682.322s**) exceeded the unchanged **672s** admission limit. **No acceptance verdict, confirmation or gameplay change.** The candidate is not yet assessed. All completed outcomes and prepared inputs were independently audited; **208 Python checks and 251 backend cases pass / four intentional skips**, plus the native study fixture. Exclusions: **923,932**. Next is the frozen, unallocated resource proposal: the **same candidate and 177 recipes**, a new **eight-by-16** contract, still 128 fresh seeds per phase and independent confirmation. The old 32 seeds stay excluded and contribute no acceptance outcomes. Floor 8 remains unresolved; no dungeon/acquisition work.

**Completed: 96 exact historical replays / 893,993 independently audited events.** All first casualties followed a physical basic attack on the same tick: 54 from Kodoku and 42 from Venomspawn. No new acceptance fight or seed, and no gameplay change. **Floor 8 remains unresolved.**

Target: the primary LL World Tower and offline Balance Harness. Continue the [closed limited-armor trial](Tower-Floor8-Limited-Armor-20260930.md). All 110 partial-armor variants won 0/128; full-party armor leaders won 37/128 and 28/128. This diagnostic explains those outcomes without adding acceptance evidence.

## Frozen protocol

Authenticate publication `76757296258d0fa4b16e4892965d20b8d1ad6dfa1470c1f87bb9957c9fa9e818`, proposal `6dd48ba6049092e87ed1dc9cb6a01230290ae548ca3243336b6a2e822caa47f2`, and complete first-batch source manifest `fc3bf5eded69b653f2f567249e7b7bea719ff311a48379b7454c671427bfbe56`. Preserve all 923,900 excluded seeds, 102 current catalog hashes and the qualified runtime.

Replay exactly the first 16 declared seeds of the first completed screen batch across six saved recipes: baseline, selected two-character armor and full-party armor for each original leading composition. Partial armor uses slots 3/7 for A and 3/8 for B. Preserve exact raw identities, ordered Essences, all ten party positions and expected progression gear. Retain and authenticate the entire 177-recipe/nine-composition source family. Recount the selected recipes' 192 saved outcomes before replay. No fresh seed, acceptance fight, retest, retry, extension or candidate application is included.

Implement a separate floor-eight diagnostic. Test exact selection, resource limits, unchanged raw builds, complete-report parity, recipient healing/regeneration/damage reconciliation, original-party versus summoned actor attribution, and time/event-order boundaries before execution. Reuse the authenticated 214 backend passes/four intentional skips and unchanged native runtime; no backend implementation changes are planned.

Freeze source, protocol, guard results, exact sample, native runtime, catalogs and ledgers in a declaration before the first replay. Use a fresh output directory. Bound the complete diagnostic to 96 replays, 840 seconds, 60 seconds per native replay, 16 MiB per log and 2 GiB overall. Before allocation, project from the authenticated 48-replay floor-seven survival diagnostic, scaling by replay count, twice the party size and a further factor of two. Require time and bytes below 80% of limits and free disk for the projection plus 2 GiB reserve. Native child processes use the established bounded Windows owner. Observe supervisor stdout only while active.

Require every detailed replay to equal its complete saved report after removing only `eventLog`. Reconcile each original character and Kodoku against saved recipient statistics. Separate original-party, friendly-summon, guardian and hostile-summon damage. Examine [0,15), [15,40), [40,end) seconds and complete battles. Partition event order immediately before the first original-party Death; killing damage belongs to the preceding partition. Record first casualty sources, Kodoku effect notifications and actual healing/regeneration. Notifications and descriptive gear comparisons do not establish exact status stacks or counterfactual causal effects.

Independently recount all 96 completed logs, outcomes, resource receipts, event totals and unchanged bindings after execution. On failure preserve the owner and completed logs without repeating combat. On success document findings, limitations and the next separately scoped experiment. No gameplay, search, dungeon, acquisition, migration, configuration, database or deployment change is part of this diagnostic.

## Completed findings

**Completed: 96 exact historical replays / 893,993 independently audited events.** All first casualties followed a physical basic attack on the same tick: 54 from Kodoku and 42 from Venomspawn. No new acceptance fight or seed, and no gameplay change. **Floor 8 remains unresolved.**

This is the preselected 16-seed diagnostic sample, not a new win-rate estimate. The preceding acceptance result remains all 110 partial-armor variants at 0/128, with the two full-armor leaders at 37/128 and 28/128. No diagnostic count is pooled into it.

| Composition / gear | Median first casualty | Mean Kodoku Health remaining | Damage to Kodoku per second before / after first casualty |
| --- | ---: | ---: | ---: |
| A/baseline | 60.45s | 48.74% | 228.0 / 184.3 |
| A/limited-armor | 85.15s | 39.44% | 234.5 / 173.8 |
| A/full-armor | 101.95s | 14.53% | 238.8 / 188.1 |
| B/baseline | 55.45s | 50.11% | 228.6 / 182.2 |
| B/limited-armor | 67.60s | 38.98% | 233.3 / 188.9 |
| B/full-armor | 115.70s | 7.57% | 241.6 / 198.5 |

Guardian and Venomspawn basic attacks contribute 95.6–98.8% of incoming Health damage during seconds 15–40. Venomspawn alone contribute 46.6–51.0% in that window. Poison was not the immediate cause of any first casualty. Armor protects against both dominant attack sources, so changing guardian penetration alone would leave much of this pressure untouched.

The leading compositions deal broadly similar damage per second to Kodoku before the first casualty (228–242 across the six setups). Output falls to 174–198 afterward; this is a descriptive event-order comparison, not a controlled estimate of the effect of death. Full armor mainly preserves more time and surviving attackers. All logged guardian healing comes from Insect Jar overflow (mean 6,880–9,062 Health per battle across these setups); this healing was not removed or adjusted.

Withering Miasma first applies at 15 seconds in all 96 reports. Its authored effects independently reduce healing received by 80% and regeneration pulse progress by 80%. The engine also applies healing-received modifiers to regeneration amounts. Consequently, while both effects are active and before other modifiers, regeneration operates at approximately **20% pulse rate × 20% amount = 4% of its normal rate**. This is a code-derived nominal rate; actual healing also depends on pulse timing, other modifiers, rounding and missing Health. It does not prove a particular counterfactual win rate or exact logged status stacks.

The prepared equipment makes this worth isolating: baseline regeneration is higher than the full-armor alternative, but the compounded suppression largely prevents that tradeoff from helping.

| Composition / gear | Combined party MaxHealth | Combined regeneration attribute |
| --- | ---: | ---: |
| A/baseline | 26,572 | 994.40 |
| A/limited-armor | 27,520 | 935.94 |
| A/full-armor | 31,312 | 702.10 |
| B/baseline | 26,572 | 994.40 |
| B/limited-armor | 27,520 | 935.94 |
| B/full-armor | 31,312 | 702.10 |

## Next experiment

The frozen, **unallocated** proposal is `TestResults/tower-floor8-miasma-trial-proposal-20260930.json` (SHA `3ecdebb79cd88ff32b044c968a51607b17432700871f0f306aa36fba692e9081`). Remove only Miasma's `ModifyRegenerationRate` effect. Preserve its 80% healing-received reduction, which still reduces regeneration amounts by 80%; update the description to make that coverage explicit. Keep all guardian values, cooldowns/durations, other abilities, summon rules and equipment unchanged. Removing an effect avoids retaining a no-op zero-valued effect or changing the shared regeneration engine.

This tests whether restoring the intended regeneration tradeoff helps limited armor without making full armor too reliable. It is **not an accepted fix**. Before allocation, add a separate strict candidate/aggregate/native contract and native tests for cadence, amount and expiry. Then retain all 177 recipes/nine actual compositions, four 32-seed batches per phase, the same 26–43/128 qualifying count range, at least two qualifying limited-equipment compositions, every control's ceiling, and independent confirmation only after a complete passing screen. Maximum 45,312 fresh fights/256 seed reservations; currently zero allocated. Complete live parity is required before any local application.

## Resource recovery and verification

The original owner closed `Failed` after **five successful replays and one truncated sixth log**. The sixth exceeded its 16 MiB cap; its process tree drained and all handles closed. All original records remain intact. The separately frozen recovery reused those five complete logs and ran only the remaining 91 sample members with a 64 MiB raw-log cap. The one truncated member received one new attempt: **97 total native attempts, 96 completed distinct replays, one preserved failure**. No successful replay was repeated.

Every new complete log was gzip-compressed, verified byte-for-byte and hashed before removing only its new temporary raw log. Both archives total **176,289,803 bytes**; largest complete raw log **19,474,232 bytes**. Successful native process time totals **196.670s**, with **2.375s** reported separately for the failed process observation. Recovery admission used twice the largest measured completed cost and included the preserved archive, transient log and summary reserves. The initial and recovery time allowances are separate; the failed initial run is not hidden inside the recovery allowance.

**29 fresh Python checks pass** (18 selection/event guards, five existing exact-report guards and six recovery guards). The unchanged runtime's **214 backend passes/four intentional skips** were authenticated and reused, not rerun or counted as fresh. Original backend execution used `build/run-tests.ps1`. All 96 logs independently match complete saved reports; every original recipient and Kodoku reconciles raw damage, mitigation, Health damage, healing, regeneration and deaths. Recounted all time windows, actor attribution, event-order partitions, log compression, process cleanup, attempt accounting, source hashes and the unchanged 923,900-seed exclusion union.

The first collector reached all 96 logs but read skips from the TRX aggregate `notExecuted` counter, which is zero in these receipts. The corrected collector verifies the individual results (214 Passed/four NotExecuted); its original source is retained. This reporting correction performed no combat.

Maintained changes: separate floor-eight diagnostic, 18 dedicated regression cases, this report and current Tower/harness status documentation. Six additional recovery guards and frozen owner/audit/publication artifacts are retained under TestResults. Existing strict diagnostics and production combat/search implementations remain intact. No migration, environment configuration, shared database action or deployment. No required verification remains blocked.

## Evidence

- Independent evidence: `TestResults/tower-floor8-kodoku-evidence-20260930.json`, SHA `211fff68564b1e2b1439ea53b0edc084d0ea50f8bf12683e3f9da79c6e98f8d3`.
- Initial owner manifest: `58a42e99282114cc7836d8398954fccb0be0b25ec783c51a1bb903a299047f36`; recovery manifest: `85e8a548708833695b2ad70810b14eea5a5f5e23d15aea8d8da19f6bab700657`.
- Read-only mechanism review: `TestResults/tower-floor8-kodoku-mechanism-review-20260930.json`, SHA `033846d66a31cd6ca81c75b26499866b885f33ab86ec2bd1ba8dd861d33a6f91`.
- Current publication: `TestResults/tower-floor8-kodoku-publication-check-20260930.json`.
- Commands: bundled Python with `-B -X utf8` for both maintained diagnostic test suites, the six recovery checks, frozen initial/recovery owners, independent collector and this publisher; `git diff --check` and local Markdown link validation.

Floors **8–10 and 12–15**, followed by the final current-version 1–15 sweep, remain. The accepted previous floors, repeating expected gear curve, Essence budgets and stronger-equipment carry-forward remain unchanged. No dungeon or acquisition work is included.
