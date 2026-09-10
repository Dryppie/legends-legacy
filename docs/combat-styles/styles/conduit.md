# Conduit

**Status:** Implemented.

The first Essence in your battle loadout is your **Channeled Essence**. **Charge** is the resource your other Essences build for it. When your Channeled Essence casts, it spends all stored Charge to strengthen the damage, healing and Barrier it delivers directly.

The core mechanic, **Circuit**, is available at Combat Style level 0. See the [Combat Styles overview](../README.md) for shared selection, saving and XP rules.

## Circuit and Charge

In the base form, Conduit holds up to **3 Charge**. Each of your other equipped Essences adds **1 Charge** when its active ability casts, once between Channeled Essence casts.

1. One of your other Essences casts and adds 1 Charge.
2. A different Essence can add another Charge, up to the cap.
3. Your Channeled Essence casts and spends all stored Charge.
4. Each of your other Essences can now add Charge again.

Until the Channeled Essence casts, using the same Essence again adds no more Charge. Short Circuit is the refinement that changes this rule. Casts made at the Charge cap cannot save extra Charge for later. Basic attacks, passive abilities and summons' actions do not build or spend Charge.

Your Channeled Essence starts at **80% of its normal strength** and gains **a flat +20% for each Charge spent**. Flat bonuses add directly to its strength: 1 Charge brings it to 100%, and 2 Charge brings it to 120%. Before upgrade bonuses, the full calculation is:

**Channeled Essence strength = 80% + 20% flat × Charge spent + mastery-level bonus.**

The mastery-level bonus applies when at least 1 Charge is spent. A zero-Charge Channeled Essence uses 80% at every mastery level.

| Charge spent | Channeled Essence strength at mastery level 0 |
|---|---:|
| 0 | 80% |
| 1 | 100% |
| 2 | 120% |
| 3 | 140% |

For example, a direct heal that normally restores 200 Health restores up to 240 when your Channeled Essence spends 2 Charge at mastery level 0. Normal combat rules still apply, including healing modifiers and the target's missing Health.

## Choosing a Channeled Essence

Place the Essence you want to strengthen in the first occupied slot of your loadout. Its active ability must do at least one of these things directly when it casts:

- Direct damage.
- Direct healing or Health restoration.
- Direct Barrier generation or restoration.

Channeled Essence follows visible slot order, skipping empty slots. It comes from the Essence loadout assigned to that battle's activity, using the existing loadout fallback when none is assigned. Different loadouts can therefore have different Channeled Essences while Conduit remains your global Combat Style.

While Conduit is equipped, the Essence page marks the first occupied slot **Channeled**. **Channel Essence** swaps another eligible equipped Essence into that position, preserving the rest of the loadout and saving the change once. A failed save restores the saved arrangement. This sets which Essence Charge strengthens; abilities still cast using their normal cooldowns.

The game checks your Essence's current ability, including its evolution. If the first Essence is unsuitable, the page explains which type of ability is needed. Put a suitable Essence first before starting a new battle with Conduit. An empty loadout also needs an Essence before battle. The game does not skip an unsuitable Essence to channel a later slot. Already committed battles retain their captured Channeled Essence.

The Charge bonus changes those direct damage, healing and Barrier amounts. It leaves other parts of the ability unchanged, including damage or healing over time, secondary damage, and summons' own actions. Healing based on damage already dealt, such as Lifesteal, uses that damage normally and does not receive the bonus a second time. Triggered copies or automatically repeated casts do not build or spend Charge or receive another Channeled Essence bonus; the normal cast's own direct hits and targets use its bonus.

## Mastery level bonuses

Every mastery level adds **a flat +1%** to your Channeled Essence's direct damage, healing and Barrier when it spends at least 1 Charge. Apply the level bonus once to each of those amounts, regardless of how much Charge was spent.

Level 0 uses the base strength. With no Charge, your Channeled Essence receives no level bonus: it stays at 80%, or 60% with Deep Reservoir, through level 10.

| Mastery level | Charged Channeled Essence bonus | 1 Charge | 2 Charge | 3 Charge |
| ---: | ---: | ---: | ---: | ---: |
| 0 | +0% flat increase | 100% | 120% | 140% |
| 1 | +1% flat increase | 101% | 121% | 141% |
| 2 | +2% flat increase | 102% | 122% | 142% |
| 3 | +3% flat increase | 103% | 123% | 143% |
| 4 | +4% flat increase | 104% | 124% | 144% |
| 5 | +5% flat increase | 105% | 125% | 145% |
| 6 | +6% flat increase | 106% | 126% | 146% |
| 7 | +7% flat increase | 107% | 127% | 147% |
| 8 | +8% flat increase | 108% | 128% | 148% |
| 9 | +9% flat increase | 109% | 129% | 149% |
| 10 | +10% flat increase | 110% | 130% | 150% |

These percentages describe the base form before upgrades. At level 1, a direct heal normally worth 200 becomes 202 with 1 Charge. At level 10, it becomes **220 with 1 Charge**, **260 with 2 Charge**, or **300 with 3 Charge**, before other healing rules.

Every refinement gains the same +1% flat increase per mastery level. Its starting strength, bonus per Charge and Charge cap determine the rest. Levels add to the total: level 10 adds a flat +10%, without multiplying the previous level's result.

## Refinements — level 3

Choose one refinement or keep the base form. Refinements change how you build or spend Charge. Your mastery level bonuses, Opening Technique and equipped upgrades still apply.

| Form | Charge cap | How other Essences add Charge | Channeled Essence strength before mastery levels and upgrades |
|---|---:|---|---|
| Base form | 3 | Each Essence adds Charge once between Channeled Essence casts | 80% + 20% flat per Charge |
| Short Circuit | 2 | The same Essence can add Charge again before the Channeled Essence casts | 80% + 20% flat per Charge |
| Deep Reservoir | 4 | Each Essence adds Charge once between Channeled Essence casts | 60% + 25% flat per Charge |
| Relay | 3 | Each Essence adds Charge once between Channeled Essence casts | 80% + 15% flat per Charge |

### Short Circuit

Your other Essences build 1 Charge every time they cast, even if the same Essence casts again. Store up to 2 Charge.

This applies to normal active casts. The Channeled Essence still starts at 80% strength and gains a flat +20% per Charge.

At mastery level 10, spending 2 Charge gives **130%** before upgrades. For Full Circuit, maximum Charge with this refinement means 2.

### Deep Reservoir

Store up to 4 Charge, with each of your other Essences building 1 Charge between Channeled Essence casts. Your Channeled Essence's immediate damage, healing and Barrier start at 60% of normal strength and gain a +25% flat increase per Charge spent.

Building all 4 Charge through Essence casts takes four different Essences besides the Channeled Essence. Primed Circuit supplies 1 starting Charge separately.

At mastery level 10, your Channeled Essence reaches **95%, 120%, 145% and 170%** strength with 1–4 Charge respectively, before upgrades. With no Charge, it uses 60%.

### Relay

After your Channeled Essence spends 2 or more Charge, regain 1 Charge for its next cast. Its immediate damage, healing and Barrier start at 80% of normal strength and gain a +15% flat increase per Charge spent.

Hold up to 3 Charge. Each other Essence can still add Charge only once between Channeled Essence casts. The returned Charge arrives **after the Channeled Essence finishes casting**, even if it misses, and adds to any new Charge up to the cap. Spending only 1 Charge gives no return.

At mastery level 10, your Channeled Essence reaches **105%, 120% and 135%** strength with 1–3 Charge respectively, before upgrades. Each of your other Essences can add Charge again once the Channeled Essence casts.

## Upgrades and Upgrade Mastery

Unlock your first slot at Mastery 5 and a second at Mastery 8.

All three upgrades become available with the first slot. Equip up to two different upgrades.

At Mastery 9, choose one of your equipped upgrades to gain its additional mastery effect.

It keeps its existing slot. Save the choice alongside the rest of the Combat Style configuration; removing that upgrade clears its mastery selection.

| Upgrade | Ordinary effect | Empowered effect at level 9 |
|---|---|---|
| Full Circuit | Spending maximum Charge adds a flat +5% to your Channeled Essence’s immediate damage, healing and Barrier. | This bonus now works whenever you spend 2 or more Charge. |
| Partial Flow | Spending exactly 1 Charge adds a flat +5% to your Channeled Essence’s immediate damage, healing and Barrier. | This bonus now works with either 1 or 2 Charge. |
| Emergency Channel | When your Channeled Essence spends Charge at 35% Health or lower, add a flat +5% to the healing and Barrier it gives you immediately. | You gain this bonus at any Health, as long as your Channeled Essence spends at least 1 Charge. |

Maximum Charge means the cap for your selected form. Full Circuit and Partial Flow strengthen your Channeled Essence's direct damage, healing and Barrier. Emergency Channel improves only its direct healing and Barrier on you, using your Health when the Channeled Essence begins casting.

The core preview shows each result as a percentage **of normal strength**, and shows your **Charge limit** with the rule for building it. Upgrade bonuses are labeled `+5% flat increase`. Flat bonuses add directly: if your Channeled Essence is at 150% strength, one bonus raises it to `150% + 5% flat = 155%`. A multiplier such as `×1.2` instead scales the amount it applies to. The displayed results for each Charge amount already include Full Circuit and Partial Flow when their conditions are met. Emergency Channel is shown separately because it applies only to direct healing and Barrier on you.

Bonuses add together when their conditions are met. For example, **Short Circuit at mastery level 10**, spending 2 Charge with **Full Circuit** and **mastered Partial Flow**, reaches **140%**: 120% from the form and Charge, a flat +10% from mastery levels, and a flat +5% from each upgrade.

## Opening Technique — level 7

**Primed Circuit** begins each battle with **1 Charge**. It activates automatically once at battle start and works with the base form and every refinement.

This starting Charge does not count as Charge added by any Essence. Each of your other Essences can still add Charge, up to your form's cap. Before level 7, each battle begins with 0 Charge. A new wave in the same continuing battle does not reset Charge or grant this starting Charge again.

## Milestones

| Mastery level | Reward |
| ---: | --- |
| 0 | Your first Essence becomes your Channeled Essence; start building Charge. |
| 1 | +1% flat increase to charged Channeled Essence effects. |
| 2 | +2% flat increase to charged Channeled Essence effects. |
| 3 | +3% flat increase to charged Channeled Essence effects. Refinement choice. |
| 4 | +4% flat increase to charged Channeled Essence effects. |
| 5 | +5% flat increase to charged Channeled Essence effects. First upgrade slot. |
| 6 | +6% flat increase to charged Channeled Essence effects. |
| 7 | +7% flat increase to charged Channeled Essence effects. Primed Circuit: begin each battle with 1 Charge. |
| 8 | +8% flat increase to charged Channeled Essence effects. Second upgrade slot. |
| 9 | +9% flat increase to charged Channeled Essence effects. Upgrade Mastery: empower one equipped upgrade. |
| 10 | +10% flat increase to charged Channeled Essence effects. Maximum Combat Style level. |

See the [shared progression and XP rules](../README.md) for the XP required at each level.

## Implementation sources

This document describes the current `combat-styles.v6` content, checked on 2026-09-10. Version 6 names this mechanic Channeled Essence; it still comes from the first occupied battle-loadout slot. Charge, mastery scaling, refinements, and upgrade values are unchanged.

- [Combat Styles catalog](../../../LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json): authored Conduit settings, refinements, upgrades and milestone effects.
- [Combat Style progression](../../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs): levels, mastery level bonuses and unlock levels.
- [Combat Style rules](../../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs): Channeled Essence validation and configuration rules.
- [Combat Style service](../../../LL/src/Infrastructure/Service/Services.LL/CombatStyles/CombatStyleService.cs): derives Channeled Essence at battle preparation and previews style bonuses independently of loadouts.
- [Combat Styles combat engine](../../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.CombatStyles.cs): how Charge is built and spent, which effects change, and how mastery levels, opening and upgrades work.
