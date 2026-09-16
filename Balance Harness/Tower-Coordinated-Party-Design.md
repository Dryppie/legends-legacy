# One coordinated party hypothesis: earlier Seal breaks

**Design complete — 16 September 2026.** Use the established `040e` anchor and replace **Poisonous Rat with Web Weaver Spider in slot 3**, and **Cinder Beetle with Web Weaver Spider in slot 10**. Evaluate these two changes as one frozen complete party. The hypothesis is that additional natural basic attacks and their existing Poison effects can move some Seal breaks ahead of the second pulse, while retaining every existing healing and barrier ability. This is a source-grounded proposal, not a demonstrated improvement or an adopted team.

The [complete seedless recipe](../TestResults/balance/tower-coordinated-party-design-20260916/party-design.json) preserves the exact ten builds, equipment, levels, tiers, ranks, quality, identity settings and subgroup placement. It contains no combat seed schedule and is deliberately not an executable TowerScenario. The [static checks](../TestResults/balance/tower-coordinated-party-design-20260916/static-validation.json) establish the inspected composition constraints; native preparation and combat remain unrun.

## Why target Seal timing

This review reads only the **48 observed fights** from the [closed diagnostic](Tower-Mechanic-Diagnostic-Execution-Review.md). Its 24 unobserved duplicate parity executions are excluded. The saved reports and traces refer to unchanged recipes; none contains this proposed party. The [analysis artifact](../TestResults/balance/tower-coordinated-party-design-20260916/saved-evidence-analysis.json) retains the per-fight, per-pillar, per-Seal and per-character calculations.

| Mechanism | Saved evidence | Design implication |
|---|---|---|
| Earlier Seal removal | Each Seal supplied 963 barrier. All 249 starts emitted their first pulse at start +19 ticks. Of 220 broken Seals, 109 emitted one wave, 94 two, 15 three and 2 four. | The practical target is crossing the **second-wave boundary**, not merely increasing the already high break rate or promising zero pulses. |
| Pillar kills | Each pillar had 1,926 health. The 302 that reached timed expiry took a median **41.98%**, mean **41.13%**, maximum **72.85%** of their health as damage. None was killed. | Typical completed pillar lifetimes need roughly 2.38 times their observed damage to reach a kill. This is a descriptive ratio, not a forecast of required Essence slots. |
| Permanent Resonance lock | Five of 48 fights locked, usually immediately before combat ended. Eighteen of 23 losses never locked. | Preventing lock alone is too narrow an objective. Removing a Seal pulse can help before lock and still helps after it. |

Seal pulses are authored every 20 ticks, but the observed first application is at **+19**, and later waves at +39, +59 and so on because of engine scheduling. Evaluate recorded event order at the boundary; a break at the same tick must not automatically count as preceding the pulse. Across the panel there were 452 distinct pulse waves and 3,799 target application attempts. These are not damage totals or independent observations. Censored contributions and timed-out barriers remain separately labelled.

The 30 pillars removed when Kharad died and the 24 still active at combat end are excluded from the completed-lifetime damage comparison. Reports confirm zero pillar healing, regeneration and generated barrier, and agreement between damage taken and final health damage. Expiry sets health to zero: terminal health is not used as evidence of a kill. The 302 full-lifetime pillars are clustered inside fights and seed pairs; their count is not a statistical sample size of 302 independent battles.

Friendly characters dealt 1,038,653 recorded damage to Kharad and 245,550 to pillars across all 48 fights. The existing area attacks therefore reached the pillars, but the observed damage fell well short of kills. The proposal adds no area-damage package and sets no pillar-kill requirement. Seal barrier and pillar health are useful scale comparisons, not interchangeable damage costs: targeting, mitigation, overlapping windows and simultaneous area hits differ.

## The frozen complete party

`040e` is the original practical-pilot starting anchor, chosen for continuity and a clear two-change comparison. The other anchor's better point result in the 16-seed mechanic sample is not a selection rule. Both anchors tied at 161/256 in the earlier practical confirmation; that pilot did not demonstrate an improved challenger. Those older outcomes are not pooled into any future evaluation.

| Slot | Complete Essence composition | Change |
|---|---|---|
| 1 | Bark Golem; Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling | None |
| 2 | Elder Treant; Flame Harpy; Illusion Fox; Pack Howler; Spider Queen — Royal Venom | None |
| 3 | Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling; **Web Weaver Spider** | Replaces Poisonous Rat |
| 4 | Elder Treant — Thornstorm; Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling | None |
| 5 | Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling; Wood Nymph | None |
| 6 | Enchanted Fairy; Hobgoblin — Brutal Charge; Pack Howler; Venomous Spiderling; Web Weaver Spider | None |
| 7 | Elder Treant — Thornstorm; Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling | None |
| 8 | Bark Golem; Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling | None |
| 9 | Bark Golem; Enchanted Fairy; Pack Howler; Spider Queen — Royal Venom; Venomous Spiderling | None |
| 10 | Enchanted Fairy; Pack Howler; Ravenous Ghoul; Venomous Spiderling; **Web Weaver Spider** | Replaces Cinder Beetle |

Slots 1–5 and 6–10 remain their existing subgroups. The JSON records exact variant IDs and ordinal Essence-ID order; the table is a readable inventory. No ability permutation, cooldown offset, combat-start timing or equipment tuning is proposed. Replacing an Essence necessarily changes the contents of the canonical ability list, but its ordering rule remains outcome-independent.

## Why these two changes belong together

The [pinned ability definitions](../TestResults/balance/tower-coordinated-party-design-20260916/selected-content.json) establish the following mechanism:

- Venomous Spiderling already supplies Slow and applies Poison from basic attacks against Slowed enemies. Web Walker grants its owner Haste while an enemy is Slowed. Weaver's Grasp deals 80% Power physical damage plus another 80% against a Slowed target, with a base 90-tick cooldown.
- Slot 3 also has Royal Venom, which adds Poison during its eight-second basic-attack buff. Slot 10 retains Ravenous Ghoul's healing. Both retain Pack Howler. This changes two owners across the two subgroups while preserving the existing support structure.
- Haste contributes **+0.25 to the natural basic-attack rate multiplier** in `FastCombatEngine.GetBasicAttackRate` (around line 3643). It does not shorten active cooldowns or multiply Howler's number of forced attacks. With Slow, caps, controls and downtime, neither a 25% total damage increase nor permanent Haste follows from this definition.
- Howler's `PerformBasicAttack` uses the ordinary basic-attack path (around lines 686–724 and 2022–2025), including on-basic-attack effects, but blocked characters do not attack. The subgroup filters remain in `AreAbilityAllies` and `FillFilteredTargets` (around lines 4814 and 4956). These existing forced attacks are retained; the proposed incremental benefit principally comes from natural attacks and the replacement active abilities.

Slot 6 already has Web Weaver, and its saved reports record Web Walker activations. This supports feasibility of the component in this encounter, not a causal estimate of adding it elsewhere. Slow is a timed condition that can be refreshed or absent; its presence and a new owner's Haste uptime were not directly traced. Poison's periodic damage goes through the ordinary damage/barrier path (`FastCombatEngine`, around lines 2160–2288 and 3061), so it can contribute to a Seal break. A Poison application does not imply immediate damage before the next pulse.

`CurrentTarget` uses attention selection; it does not guarantee Kharad while pillars are alive. `RandomEnemy` can also choose a pillar. This is why the proposed mechanism predicts an earlier-break tendency, not a fixed burst delivered to the boss on command. Aggregate ability damage cannot establish how much landed inside a particular Seal window, and no such temporal attribution is made here.

## Costs and reasons it might fail

In the 16 observed `040e` fights, slot 3's removed Toxic Bite accounted for **7,799** damage, while its retained Basic Attack, Royal Venom and Toxic Opportunity accounted for 13,765, 14,141 and 4,890 respectively. Slot 10 loses **4,481** Burning Mandibles damage and **63** Molten Shell damage; it retains 11,572 Basic Attack and 4,344 Toxic Opportunity damage. These are whole-fight totals, including non-boss targets, not predictions of the replacement's value. Both removed abilities may have helped break Seals already.

The proposal also loses Poisonous Rat's Poison resistance and chance to apply Toxic Blood, plus Cinder Beetle's Burn/Bleed applications and retaliation. It keeps all Royal Cocoons, Ancient Essence healing, Wood Nymph protection, Ravenous Ghoul healing, Barkskin, Hobgoblin protection, Howlers and Fairy control. Preserving their definitions does not guarantee unchanged healing delivered, survival or aggro: altered damage and targeting change the fight.

Two additions may be insufficient to move a 963-point barrier across the next boundary. Extra attacks can land after a pulse, hit pillars, or fail to compensate for the removed damage. Stun/Cocoon downtime can erase Haste's benefit. Removing Toxic Blood may be especially costly despite its low proc chance. The design deliberately freezes these opportunity costs instead of assuming every additional synergy is free.

This is one hypothesis assembled from the full party's existing interactions, not a new search policy or a claim that two independent substitutions are each improvements. A successful package would still not identify each substitution's individual causal contribution. No ablation, alternate order, runner-up recipe or adaptive replacement is queued.

## Legality and readiness

Static verification checks ten slots, five distinct allowed Essences and five distinct source families per character, the unchanged 85-Essence pool, ordinal ordering, and exact equality of every non-Essence build field to the pinned anchor. Duplicate copies across characters are allowed under this study's existing `ownedCopies: null` assumption; this is not an inventory ownership claim. The rules follow [TowerBossPartyGenerator.Invalid](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) and [TowerCompositionSearch](../LL/tools/BalanceHarness/TowerCompositionSearch.cs). All pre-existing active/passive Heal and GrantBarrier definitions on their owners are retained.

Native admission, effective-stat materialization and gameplay execution are not established by the Python checks. A later readiness step must validate the frozen recipe against the same captured content/settings and the existing native legality/materialization path without treating static screening as that result. Any backend tests must use `build/run-tests.ps1`.

## Smallest useful later evaluation

The next step is a bounded readiness and comparison protocol for **this one frozen party**, not another composition search. A reasonable diagnostic proposal is 64 fresh paired seed values across this party and both unchanged anchors, or **192 fights**. This paragraph allocates no values and authorizes no executions; protocol-specific time/output ceilings and native readiness must be established prospectively within the carried resource balances.

Freeze a primary mechanism endpoint before execution: fewer second-or-later Seal waves per fight versus `040e`, considered jointly with encounter duration, Seal opportunities, censoring and survival. Report first-wave offsets, break durations, timeouts, active-at-end contributions, boss damage and wins as companions. A shorter losing fight must not pass by producing fewer waves. Specify a conservative treatment of early deaths and missing Seal opportunities in that protocol; do not select the treatment after seeing outcomes. Pair comparisons by seed and use the fight, not a pulse or a pillar, as the independent unit.

Require a positive, uncertainty-qualified paired mechanic improvement and no observed win-rate decline against either anchor before considering a separate practical confirmation. A 64-seed diagnostic cannot reliably establish a small win-rate improvement. Practical adoption still requires independent evidence under the existing policy; the previous pilot's five-percentage-point and adjusted paired-comparison requirements are not relaxed by a good mechanic result. These are proposed gates for a later protocol, not a statistical claim from this design.

Stop after the single fixed diagnostic if the timing signal is absent, survival worsens, or the apparent benefit is explained by shorter losses. Do not tune slots or reorder abilities on those seeds, extend the sample opportunistically, or present an inconclusive result as a successful team. If the resource envelope cannot support meaningful native readiness and a bounded evaluation, close the proposal rather than drawing from old reservations.

## Verification and preservation

The saved-evidence analysis and publication scripts ran successfully. They checked the 48 observed reports/traces, prior source pins, all 192 history-ledger files, the complete sealed diagnostic manifest, static composition invariants, unchanged unrelated dirty files, local links and Git whitespace. See [preservation](../TestResults/balance/tower-coordinated-party-design-20260916/preservation.json) and the [current resource receipt](../TestResults/balance/tower-coordinated-party-design-20260916/completion.json).

The scope charges a conservative **15 engineering/diagnostic seconds** from the carried balances, with a **2 MiB output ceiling**, no transfer and no increased cap. The receipt carries actual conservatively accounted output bytes, including documentation snapshots and publication allowance. Engineering time remaining is **4.557880138483597 seconds**; that small remainder is a real constraint on later readiness.

Changed files are this review, the new design/evidence package, and current-status notices in the BalanceHarness README and five existing handoff documents. No production code, game content, migrations, persistent configuration or deployment changes were made. No build, backend test, new combat, replay, seed generation or reservation was run; those commands were intentionally outside this source-only scope, not failed checks. All **483,656** exclusions, V19's 253 required recipes and its 512 unused confirmation values are preserved. Pilot/diagnostic remain **Closed**, adoption **Hold**, and V19 reliability **Unresolved**.
