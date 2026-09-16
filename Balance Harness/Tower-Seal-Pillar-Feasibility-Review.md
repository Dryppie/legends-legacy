# Seal and pillar handling: source feasibility review

**Prioritize measuring Seal barrier breaks and pulse exposure before selecting another team change. Breaking Seal’s barrier removes its linked damage effect immediately, including after Resonance has locked.** The legal pool contains abilities that can reach both the boss and pillars without changing canonical ability order. The available evidence does not establish that any replacement can supply enough timely damage or protection to improve confirmation results.

This completes the zero-combat source review recommended by the [saved-loss diagnosis](Tower-Practical-Loss-Diagnosis.md). It concerns the offline BalanceHarness and the existing game combat engine. No production implementation, candidate generation, combat, replay or new values were performed.

## Correction to the earlier expiry interpretation

**The previous diagnosis’s 5,454 “expired” pillars are non-kill endings, including possible owner-death cleanup; they are not 5,454 proven timed expiries that fed Resonance.** The recorded zero kills across 5,716 pillars remains valid, as do the 262 pillars still active at combat end. The old analysis package remains immutable; this review supersedes the interpretation of its expiry and resolved-group labels.

The engine’s `ExpireOwnedSummons` sets surviving summons’ health to zero and emits `SummonExpired` when their owner dies. Timed expiry emits the same event type. The compact statistics retain that shared counter and end tick but do not retain the reason string. Similarly, the earlier derived `twinGroupsResolved` field means both members have end timestamps; it does not prove an `OnSummonGroupResolved` gameplay trigger occurred. A victory can end pillars through Kharad’s death without granting any Resonance.

This distinction is established in [FastCombatEngine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs), particularly `TickSummons`, `ExpireOwnedSummons` and `LogSummonExpired` (lines 4652, 5276 and 5290), and [CombatStatsAggregator](../LL/src/Infrastructure/Service/Services.LL/Combat/Stats/CombatStatsAggregator.cs), lines 160 and 302. No aggregate expiry count should be converted into a Resonance-stack history.

## What actually happens

| Boundary | Engine behavior | Practical implication |
| --- | --- | --- |
| Seal contribution depleted by damage | Removes active effects matching the contribution’s activation and linked effect IDs, then publishes `OnBarrierContributionBroken`. | Breaking this barrier stops its remaining linked pulses; waiting for the ten-second duration is unnecessary. |
| Seal contribution times out | Removes the same linked effect and publishes `OnBarrierExpired`. | Both routes end the pulses, but only the break route receives the favorable Resonance trigger. |
| Pillar-group deadline | Counts living members, expires survivors, and publishes `OnSummonGroupResolved` with that count only while the owner lives. | Two survivors add two stacks; one survivor adds one; zero survivors permits a one-stack reduction. The group reward is evaluated at its deadline, not immediately when the second pillar dies. |
| Resonance reaches five | `RuntimeStatus` remembers the locked maximum and rejects negative stack changes; status removal also respects the lock. | Later Seal breaks still stop pulses, but cannot recover Resonance stacks after lock. |
| Kharad dies | Remaining owned pillars receive expiry/cleanup events. | A pillar end or zero final pillar health alone is not evidence of a kill or a successful mechanic response. |

The contribution-break path is in [FastCombatEngine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs), lines 2239–2253; timed barrier handling is at 4571–4613; linked-effect removal is at 4635–4649. Stack reduction and removal dispatch are at 3722 and 4053. The permanent lock is enforced by [AbilityRuntime](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs), lines 345–355. These are source findings, not newly tested combat outcomes.

The captured [Kharad abilities](../TestResults/balance/tower-incumbent-practical-pilot-20260916/content/Data/combat/abilities.json) connect the Seal break to −1 Resonance, Seal timeout to +1, and surviving pillars to their survivor count. The captured [status](../TestResults/balance/tower-incumbent-practical-pilot-20260916/content/Data/combat/statuses.json) sets a five-stack maximum and lock. The [summons](../TestResults/balance/tower-incumbent-practical-pilot-20260916/content/Data/combat/summons.json) have 120-tick duration and maximum health equal to 10% of their owner’s; Seal starts with a 5%-maximum-health barrier. These percentages alone do not calculate required team damage after mitigation, targeting and survival.

## Reachability within the fixed legal pool

The definition admits all examples below. These are mechanic-relevant building blocks, not recommended replacements or measured improvements.

| Legal Essence | Relevant captured behavior | Limitation |
| --- | --- | --- |
| Bark Golem | Timber Slam hits `AllEnemies`, 2.5×Power physical damage, base cooldown 240 ticks. | Already present in all three measured teams; its presence did not produce a pillar kill. |
| Elder Treant — `thornstorm` variant | Thornstorm hits `AllEnemies` with 0.7×Power magical plus 0.7×Power physical damage, base cooldown 100 ticks. | Also already present. Additional copies have opportunity costs and unmeasured timing effects. |
| Giant Bat | Echoing Screech hits all enemies; Resonant Cry adds area damage every fourth basic attack. | Reaching a pillar is feasible, but sufficient damage within its lifetime is unproven. |
| Pixie | Pixie Burst hits all enemies, 0.8×Power magical damage, base cooldown 120 ticks. | A cooldown equal to the pillar lifetime is not a guarantee of an appropriately timed cast. |
| Blue Slime | Sweet Water heals allies over time; Protective Slime grants an innate ally barrier. | `AllAllies` respects the caster’s subgroup, so one character does not protect both five-character subgroups. Replacing damage with support can prolong exposure. |

`AllEnemies` includes living hostile summons; there is no general prohibition on these abilities damaging pillars. See target filling in [FastCombatEngine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs), lines 4880–4888 and 4945–4954. Ally filtering at 4778–4783 is subgroup-aware. The catalogue stores canonical IDs and full selected ability definitions in the [review evidence](../TestResults/balance/tower-seal-pillar-feasibility-20260916/ability-examples.json).

The measured teams already contain area damage, so “add an area ability” is too weak a hypothesis. Actual cast windows, damage lost to boss mitigation/barrier, pressure on both pillars, and survival of the relevant characters matter. Active abilities start on cooldown in the harness. Preserve the fixed ordinal order and existing target-selection rules; neither becomes a tuning variable in this proposal. Pool membership is not an inventory check or validation of an as-yet-unspecified new complete party.

## Minimum next implementation to consider

Add an **opt-in compact diagnostic observation path**, before changing search construction. It should report each Seal contribution’s start, break/timeout and pulse count; each pillar group’s scheduled resolution, killed members, timed expiries and owner-death cleanup; and Resonance’s first lock time and stack exposure. Generic contribution/status/summon identifiers should drive collection rather than hard-coded balance adjustments for Kharad.

Keep the normal combat outcome, random draws, targeting, ordering and existing archive format unchanged when observation is off. Prefer a separate versioned diagnostic artifact or explicit opt-in schema rather than adding default serialized fields that could invalidate reconstruction of old reports. Reuse existing event boundaries; do not infer missing trigger counts from final summary totals. This needs design and regression verification before any implementation is claimed complete.

Minimum zero-combat checks should cover independent barrier activations, pulse cancellation on break, timeout versus break classification, two/one/zero surviving pillars at the scheduled deadline, owner-death cleanup without a timed-resolution claim, and the persistent five-stack lock. Include compatibility checks that existing default evidence remains byte-stable. Synthetic event fixtures establish observation semantics only; they do not establish a winning composition. Later backend verification must use `build/run-tests.ps1` under a separately costed scope.

Only after useful observations and a concrete complete-party hypothesis exist should a new bounded combat diagnostic be considered. It must preserve fixed ordering, both original anchors, the closed pilot’s thresholds/result and independent fresh evaluation. This review authorizes no such run and identifies no proven replacement team.

## Verification and accounting

Changed files are this review, its small source/evidence/accounting package, and current README/handoff notices. Static verification checks legal-pool membership of the five examples, exact captured ability references, source/content hashes, preserved sealed packages and unrelated dirty files, links and Markdown whitespace. An initial read-only catalogue query treated the pool’s records as strings and failed; its corrected query used each record’s `id`. That inspection failure and its cost are retained in the review receipt. No source file was edited and no harness command ran.

The scope is capped at **3.5 diagnostic seconds /1 MiB**, charged to the remaining engineering balance; no component transfer or overall increase is assumed. The [completion receipt](../TestResults/balance/tower-seal-pillar-feasibility-20260916/completion.json) carries prior accounting forward. Backend builds/tests were intentionally not run for this source/documentation review. There are no gameplay, migration, persistent configuration or deployment changes. All **483,640** exclusions remain preserved, the pilot **Closed**, adoption **Hold** and V19 reliability **Unresolved**.
