# Second-Seal mechanism review: one-tick mismatch, no frozen replacement

**16 September 2026 — MechanismLocatedNoRecipeJustified /Closed.** The new attribution data expose a real timing mismatch: every recorded second-Seal contribution from direct **Fae's Corrosion and Venom Web arrived at tick 360**, immediately after its second pulse at tick 359. Their authored 120- and 90-tick cooldowns explain why that boundary is a plausible structural weakness. This is much more specific than “increase whole-fight damage.” It still does not justify choosing a new complete party.

**Keep both incumbents and do not launch another comparison from this review.** Ice Harpy's Chilling Gust is the clearest source-grounded lead among the alternatives inspected: its nominal repeat schedule can land inside all three early Seal windows, and its area target includes Kharad. However, a Fairy replacement also removes Corrosion and control, while the number and placement of substitutions needed to close the measured gap are unsupported. Freezing a small arbitrary swap would repeat the weakness of the failed Web Weaver proposal. Freezing a broad replacement would introduce a large unquantified defensive and mitigation tradeoff. No recipe, ownership pattern, winner or new experiment is selected here.

## What the saved evidence adds

Only the **eight diagnostic fights per anchor** from the [completed attribution scope](Tower-Barrier-Attribution-Readiness-Recovery-Execution-Review.md) enter the following arithmetic. The parity panel and all earlier studies remain separate. The analysis reads JSON; it does not replay combat, shift events, recalculate damage or simulate a hypothetical party. A fight is the observation unit.

| Anchor, eight fights each | Mean barrier at tick 359 | Mean Fae direct consumption at tick 360 | Mean Venom Web direct consumption at tick 360 | Gap minus those two recorded amounts |
|---|---:|---:|---:|---:|
| 040e | 360 | 178.625 | 76.875 | 104.5 |
| 49f6 | 284.375 | 156.75 | 72.875 | 54.75 |

The subtracted quantity remained positive in **8/8 fights for each anchor**. These two packets therefore are smaller than the observed gap even when considered together. This is a comparison of recorded magnitudes, **not a counterfactual outcome**: their observed consumption can be clipped by a barrier breaking, and earlier damage would alter mitigation, subsequent actions, targets and overflow. It neither proves nor disproves that a legal change could prevent a pulse. It does rule out presenting those two late totals as an already sufficient, validated repair.

The [per-fight evidence](../TestResults/balance/tower-second-seal-mechanism-design-20260916/saved-timing-evidence.json) retains exact event positions, actor IDs, canonical effect IDs, tick, consumed amount, original pulse remainder and end tick. All selected events were checked against the already reconciled second-Seal activation/application identity. No unknown effect ID or display label was used to identify these two sources.

## Why the cooldown explanation is credible—and limited

The [pinned definitions](../TestResults/balance/tower-second-seal-mechanism-design-20260916/selected-content.json) give Fae's Corrosion a **120-tick** cooldown and Venom Web a **90-tick** cooldown. Their third/fourth nominal repeats coincide at 360. The captured scenario starts active abilities on cooldown. [AbilityRuntime](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs) initializes and restarts their counters through [CalculateCooldownTicks](../LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs), which applies the Cooldown attribute and rounds upward. [FastCombatEngine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) uses ready abilities and restarts their cooldowns in `UseReadyActiveAbilities` (around lines 635–669); blocking can delay a ready cast. Actual recorded tick-360 hits supply the evidence that this alignment occurred here.

The [nominal screen](../TestResults/balance/tower-second-seal-mechanism-design-20260916/nominal-cooldown-screen.json) is only integer period arithmetic from authored cooldowns, with no combat execution. It deliberately excludes casts exactly on a Seal's start or pulse tick, because within-tick order matters. It cannot establish actual new-party cast times, cooldown state, targeting, damage or survival. Ability order, equipment, combat-start settings and manual cooldown offsets remain fixed and outcome-independent.

Inspection of all **85 captured base Essences** found no direct Cooldown attribute bonus, cooldown-resource effect or reset effect in their selected base active/passive abilities; the captured status definitions also contain no Cooldown field. This finding is limited to the unascended/unevolved setup. The engine supports cooldown mechanics in general, but there is no identified legal Essence-only switch here that simply advances these two existing packets by one tick. Equipment or order tuning would change the current design constraints.

## Alternatives assessed against the actual deadline

| Direction | Concrete source mechanism | Cost or limitation | Decision |
|---|---|---|---|
| Retime the existing Fairy/Spiderling actives | Their nominal repeat occurs at 360, one tick late. | No identified base-Essence cooldown control in this pool; observed packet magnitudes still leave an arithmetic remainder. | No unchanged-party repair established. |
| More Web Weaver copies | Haste raises natural basic-attack rate; its own active has a 90-tick cooldown. | The active shares the 360 alignment. Haste does not shorten active cooldowns. The prior complete-party comparison failed its mechanism and outcome gates. | Do not repeat the closed variant. |
| Fairy-to-Ice Harpy substitutions | Chilling Gust: **1.4 ×Power magical damage to all enemies**, cooldown 170. Nominal ticks **170/340/510** precede second-pulse boundaries **199/359/519**. Chill can also slow natural attacks. | Replacing Fairy loses its Corrosion application and Fae's Charm. Source fit gives no justified slot/copy count or net damage/survival estimate. | Best timing lead among those inspected; insufficient for a frozen complete recipe. |
| Crystal Wisp burst | Cooldown 110 gives nominal tick **330**, within the second-Seal window; magical coefficient is random, 0.5–2.5. | RandomEnemy can hit a pillar. Nominal 220 and 550 miss the first/third second-pulse deadlines. | Weak fit to the required three-Seal constraint. |
| Add a damage amplifier | Scout applies Exposed; Hobgoblin applies Vulnerable. | Exposed adds **10 percentage points of critical chance**, not 10% guaranteed damage. Vulnerable is a consumable charge multiplying one qualifying direct hit by **1.25**, not a persistent party-wide multiplier. | No source-grounded claim that a small addition closes the measured gap. |

The Ice Harpy lead is about an authored ability with a different natural schedule, not searching cooldown offsets or ability permutations. The saved nominal table is not a candidate ranking across the pool, and there is no claim that these are all viable combinations.

## The opportunity cost that blocks a defensible party choice

Fairy's Corrosion applies six stacks. `ApplyTypedDefense` (around lines 2736–2746) reduces the target's Armor or Resistance according to its current Corrosion stacks, capped at 50. That affects several damage types, including magical, poison and burn. Corrosion stacks last 120 ticks in the inspected implementation. Removing Fairy therefore can reduce the useful damage of **other owners**, even when Fairy's own direct hit arrives late. Its contribution is not limited to the direct amounts in the table.

Fae's Charm supplies an 80%-chance Stun application attempt to each enemy on a 200-tick interval, with stagger power 40. Replacing several copies changes control/stagger attempts. Retaining the same healing/barrier definitions elsewhere does not establish unchanged healing delivered or third-Seal survival. Ice Harpy's Chill affects natural attack rate; it is not equivalent to those control attempts. Its passive's small secondary-damage coefficient must be read from the definition, not assumed to be a large burst bonus.

The captured trace filters observe Seal, Resonance and pillars. They do **not** provide a complete Corrosion/control/cast-readiness timeline from which to reconstruct these indirect contributions. Ordinary totals and nominal schedules cannot fill that gap. We should not manufacture a marginal-damage score for each removed Fairy or pick slots from the largest observed post-deadline amounts. The latter would also adapt the recipe to this small descriptive panel.

Third-Seal evidence reinforces the constraint: the previous scope recorded two natural third-Seal timeouts for 49f6 and six later unfinished barriers overall. Those accounts remain explicitly labeled. This review does not pool historical wins, relabel the earlier Web Weaver failure, interpret co-occurring deaths as causal, or use a shorter losing fight as evidence of fewer harmful waves.

## Stopping decision and any future reopening

This analysis answers the next design question as far as current evidence supports it: **the deadline mismatch is located, but no complete replacement is justified under the frozen constraints**. Close this branch of the investigation here. The telemetry implementation remains useful and its finite-panel validation remains passed; no additional implementation or instrumentation expansion is queued merely to keep the experiment sequence moving.

A future proposal would need to state one complete legal recipe and explain both its before-deadline contribution and the party-wide costs it removes. It must preserve the fixed ordering rule, name independent mechanism and outcome gates, retain a first-three-Seal/survival constraint, and specify a bounded independent evaluation with no tuning on these 16 fights. An Ice Harpy-containing design is a possible research lead, not permission to substitute arbitrary copies or spend unused numerical capacity. If that whole-party argument cannot be made, retain the incumbents rather than conduct another minor variant. No fresh-seed approval is requested by this review.

## Verification, preservation and resources

The source/saved-evidence script and publication checks passed. Checks include the complete sealed attribution manifest, source/content pins, all **196 previously recorded history files**, exact **483,732**-value ledger union, ten characters/five distinct allowed Essences/five distinct source families per character in each unchanged anchor, event-to-Seal identity, unrelated dirty-file preservation, original handoff text, local links, whitespace and resource arithmetic. See [static validation](../TestResults/balance/tower-second-seal-mechanism-design-20260916/static-validation.json), [preservation](../TestResults/balance/tower-second-seal-mechanism-design-20260916/preservation.json) and [completion](../TestResults/balance/tower-second-seal-mechanism-design-20260916/completion.json). Known-file hashing is not described as a new live registry scan.

This scope charges **30 engineering/diagnostic seconds** conservatively and has a **4 MiB output ceiling**, from the existing balances with no transfer or cap increase. It leaves **29.525880 engineering seconds**; the receipt reports exact conservative bytes. Prior run/audit consumption is carried unchanged. No old allowance is reopened.

Changed files are this focused review, its analysis/source-pin/evidence/accounting package, and current notices in the BalanceHarness README and five handoff documents. No builds, backend tests, native preparation, live scans, combat, replay, derivation or reservation were run; they were outside this source-only scope, not failed verification. No production code, gameplay content, migration, persistent configuration or deployment changed. All **483,732 exclusions** remain, including V19's **512 unused confirmation values**; V19 retains **253 required recipes /Unresolved**. Adoption stays **Hold**, prior experiments and this design scope **Closed**.
