# Bastion

**Status:** Implemented. **Core mechanic:** Fortification. **Identity:** Turn recovery into protection prepared for incoming damage.

Bastion makes all healing received during combat a source of both Health and Barrier. Its refinements support direct recovery, answering absorbed damage with Reprisal, or protecting an ally. The style operates automatically and does not require a particular weapon or armor type.

See the [Combat Styles overview](../README.md) for shared selection rules, individual XP, and the full milestone schedule.

## Core mechanic: Fortification

All healing received during combat restores **25% of its normal amount as Health** and converts **75% into Barrier** before mastery level and upgrade bonuses.

A 200-point heal received at mastery level 0 allocates **50 Health + 150 Barrier**. Health restoration is limited to missing Health; Barrier follows the normal shared cap of **250% of Max Health**. Healing modifiers apply before the split, so the example assumes 200 healing after those modifiers.

Fortification applies to healing from any source: active and passive abilities, Health Regeneration, Lifesteal, other characters, and any character's summons. The receiving Bastion's mastery level, refinement, upgrades, and Upgrade Mastery determine the result. Healing supplied by a Bastion to an ally follows the recipient's own rules; a non-Bastion receives ordinary healing. Summons do not independently inherit Fortification.

Healing target selection and priorities remain unchanged and do not account for Barrier. Fortification changes the result of a heal that reaches a Bastion; it does not redirect healing or override an ability's cast conditions. Existing usefulness checks for Bastion's self-recovery remain in place. Recovery outside combat, revives, Health costs, and effects that set or exchange Health retain their existing rules.

Barrier can be generated while Health is full. Converted Barrier absorbs damage under the ordinary Barrier rules and participates in absorption and break reactions. Its creation does not trigger another healing conversion or a Barrier-gain reaction.

## Mastery level bonuses

Every mastery level adds **1% of the base converted Barrier portion**. Level 0 has no level bonus; level 1 adds 1%, and level 10 adds **10%**. The ordinary Health allocation stays at 25%.

| Mastery level | Barrier bonus | Result from a 200-point heal |
| ---: | ---: | --- |
| 0 | +0% | 50 Health + 150 Barrier |
| 1 | +1% | 50 Health + 151.5 Barrier |
| 2 | +2% | 50 Health + 153 Barrier |
| 3 | +3% | 50 Health + 154.5 Barrier |
| 4 | +4% | 50 Health + 156 Barrier |
| 5 | +5% | 50 Health + 157.5 Barrier |
| 6 | +6% | 50 Health + 159 Barrier |
| 7 | +7% | 50 Health + 160.5 Barrier |
| 8 | +8% | 50 Health + 162 Barrier |
| 9 | +9% | 50 Health + 163.5 Barrier |
| 10 | +10% | 50 Health + 165 Barrier |

The table shows the base form without upgrades, before missing-Health and Barrier-cap limits. The formula is:

`Converted Barrier = healing × 0.75 × (1 + 0.01 × mastery level)`

Bonuses add against the same base amount; they do not compound from the previous level. For a 200-point heal, each new level adds 1.5 converted Barrier. Shelter distributes the result after level and upgrade bonuses. Rebuild uses its direct Health restoration when its condition applies. Direct Barrier grants, including Entrenched, keep their own amounts.

## Refinements — level 3

Choose one refinement, or keep the base form. Mastery level bonuses, upgrade slots, and later milestones work with either choice.

| Choice | Effect |
| --- | --- |
| **Base form** | Use Fortification's 25% Health / 75% Barrier allocation at every Health level. |
| **Rebuild** | Healing received at or below **35% Health** restores its full normal amount as Health. Above that threshold, use the normal Fortification split. |
| **Reprisal** | Store **25% of actual enemy damage absorbed by your Barrier** as bonus damage, up to **10% of Max Health**. Your next normally cast damaging Essence uses the stored amount on its first direct enemy attack attempt. |
| **Shelter** | Split converted Barrier equally with the ally with the lowest health percentage; retain it all when alone. |

Rebuild checks Health before each healing event. Its full Health allocation applies to the whole event, even if the heal takes Health above the threshold. Ordinary Rebuild healing produces no converted Barrier; Measured Recovery mastery can turn its excess Health allocation into Barrier.

Reprisal counts hostile damage absorbed by your own Barrier, regardless of who granted it, including enemy periodic and reflected damage. Allied players' and summons' Barrier absorption belongs to them and does not fill your stored amount. Self-inflicted or allied damage, damage reaching Health, and damage prevented before Barrier absorption do not contribute. Barrier is never spent to activate Reprisal.

`Stored damage = min(10% × Max Health, previous stored damage + 25% × enemy damage absorbed)`

With 1,000 Max Health, the stored-damage cap is **100**. Absorbing 200 enemy damage stores **50 bonus damage**; absorbing a further 300 before using it raises the stored amount to the 100-point cap. The next qualifying Essence adds that amount once, before its applicable damage mitigation. Multiple hits and targets do not repeat the bonus.

The Essence reserves existing stored damage at cast start, then uses its whole-number portion on the first direct enemy attack attempt, including a miss or dodge; fractions remain stored. Damage absorbed during that cast is stored for a later cast. Reserved and unreserved amounts share the same cap. If the cast produces no eligible attack attempt, its reserved amount returns to storage, subject to that cap. A reduction in Max Health also reduces the cap applied when the bonus is used.

Reprisal's contribution uses that attack's damage type and normal target mitigation, without further outgoing damage amplification or critical scaling. It creates no Lifesteal or extra damage-derived hit reactions; the ordinary attack retains its own behavior. Basic attacks, passive or periodic effects, summon attacks, and automatically repeated casts do not release the stored amount.

Stored Reprisal starts empty in each new battle. Continuous waves and revival within the same battle retain it in that encounter's runtime state.

Shelter can protect an allied player or summon. Each recipient's own Barrier cap applies separately.

## Upgrades — slots at levels 5 and 8

Unlock your first slot at Mastery 5 and a second at Mastery 8.

All three upgrades become available with the first slot. Equip up to two different upgrades.

| Upgrade | Ordinary effect | 200-point heal example |
| --- | --- | --- |
| **Prepared Wall** | When you receive healing at 80% Health or higher, Fortification grants extra Barrier equal to 7.5% of the heal. | +15 Barrier when eligible. |
| **Hold the Breach** | When you receive healing while having no Barrier, Fortification grants extra Barrier equal to 7.5% of the heal. | +15 Barrier when eligible. |
| **Measured Recovery** | Fortification’s normal healing split restores 30% as Health, up from 25%. | 60 allocated Health; the ordinary Barrier allocation stays the same. |

Barrier bonuses apply when Fortification converts healing.

Health and Barrier conditions are checked before the healing event changes either resource. Each conditional Barrier bonus equals 10% of Fortification's level-0 Barrier portion: `75% × 10% = 7.5%` of the heal. Mastery level bonuses and both conditional Barrier bonuses add against that same level-0 Barrier portion. Measured Recovery's change from 25% to 30% is a 20% increase to the divided Health allocation.

The core preview makes each bonus's calculation explicit:

| Preview fact | Value for a 200-point heal | Meaning |
| --- | --- | --- |
| Prepared Wall | `+7.5% flat increase · +15 Barrier` | Add a flat +7.5% to the share of the heal allocated as Barrier. |
| Hold the Breach | `+7.5% flat increase · +15 Barrier` | Add the same amount when its condition qualifies. |
| Measured Recovery | `+20% Health recovery (×1.2) · 60 Health` | Multiply the divided Health allocation by 1.2: `25% × 1.2 = 30%` of the heal. |

Prepared Wall's ordinary preview condition reads, “Gain extra Barrier when you receive healing at 80% Health or higher.” Hold the Breach reads, “Gain extra Barrier when you receive healing with no Barrier.” Prepared Wall mastery extends its condition to also include having no Barrier.

Isolating one conditional Barrier bonus from mastery level bonuses, the conversion becomes `75% + 7.5% flat = 82.5%` of the heal. Flat bonuses add directly to that share. A multiplier such as `×1.2` instead scales the Health amount it applies to. The extra Barrier still scales with the incoming heal, and the preview's Health totals are allocated amounts before missing-Health limits.

For example, at mastery level 10 with Prepared Wall and Hold the Breach equipped, a 200-point heal beginning at or above 80% Health with zero Barrier allocates **50 Health + 195 Barrier**: `150 × (1 + 0.10 + 0.10 + 0.10)`.

With Shelter, those bonuses apply before sharing. During Rebuild's full Health restoration, ordinary Barrier bonuses have no converted portion to increase, and ordinary Measured Recovery does not change that full Health allocation.

## Opening Technique — level 7

**Entrenched:** Begin each battle with Barrier equal to **5% of Max Health**, subject to the normal Barrier cap.

With 1,000 Max Health, Entrenched grants up to **50 starting Barrier**. It activates automatically once per battle in the base form and every refinement, without occupying an upgrade slot. It is a starting grant, separate from healing conversion and its mastery level bonuses. A new wave within the same continuing battle does not grant it again.

## Upgrade Mastery — level 9

At Mastery 9, choose one of your equipped upgrades to gain its additional mastery effect.

Its ordinary effect stays active, and mastery uses the same slot.

| Empowered upgrade | Additional benefit |
| --- | --- |
| **Prepared Wall** | You also gain this bonus when you have no Barrier. |
| **Hold the Breach** | That heal also restores 20% more Health. |
| **Measured Recovery** | Any overhealing is converted into Barrier. |

Prepared Wall grants its bonus once when either or both conditions are met. Hold the Breach increases the allocated Health portion after ordinary Measured Recovery and also works with Rebuild's full Health restoration. Measured Recovery converts 100% of the allocated Health healing above missing Health into additional Barrier, following normal Barrier caps and Shelter sharing.

Hold the Breach mastery uses the same multiplicative preview label, `+20% Health recovery (×1.2)`. With Measured Recovery equipped, its 200-point-heal result reads `+20% Health recovery (×1.2) · 72 Health`: `25% × 1.2 × 1.2 = 36%` of the heal. With Rebuild's full Health restoration, the corresponding total is 240 Health. These are final allocated Health totals for those conditions, rather than extra Health added to the ordinary preview a second time.

Examples for a 200-point heal:

- **Hold the Breach mastery + Measured Recovery:** With zero Barrier in the base form, allocate `50 × 1.20 × 1.20 = 72 Health`, alongside the eligible Barrier output.
- **Hold the Breach mastery + Rebuild:** With zero Barrier and at or below 35% Health, allocate `200 × 1.20 = 240 Health`.
- **Measured Recovery mastery:** At mastery level 10 in the base form, with full Health and enough Barrier capacity, convert the entire 60-point Health allocation into Barrier for **225 Barrier** total: `165 + 60`.

## Build directions

- **Prepared protection:** Retain the base form and combine regular self-recovery or allied healing with Prepared Wall to maintain a Barrier reserve.
- **Direct recovery:** Choose Rebuild to direct healing into Health at its threshold; Hold the Breach mastery adds another recovery interaction when Barrier is empty.
- **Offensive protection:** Choose Reprisal to turn enemy damage absorbed by your Barrier into bonus damage for direct-damage Essences. Self-generated or allied Barrier and Entrenched can prepare this interaction.
- **Ally protection:** Choose Shelter to share recovery-generated Barrier with an ally or summon while retaining your Health restoration.

## Sources

Values follow the [current Combat Styles catalog](../../../LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json), [progression rules](../../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs), and [combat implementation](../../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.CombatStyles.cs).
