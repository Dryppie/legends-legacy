# Combat Styles

Game design proposal · 8 September 2026

**Scope:** Two developed Combat Styles, Bastion and Conduit, followed by three exploratory concepts. All Combat Style mechanics described here are proposals. Numerical values are starting points for playtesting, not validated balance targets.

**Progression decision:** Each character levels each Combat Style individually. Combat Style levels and experience are not shared. The level cap, milestone schedule, rank bonuses, and upgrade values below are provisional expressions of that decision.

## 1. The third character-building pillar

Combat Styles give a character a combat philosophy: a rule that changes how the player combines equipment and Essences.

| Pillar    | Player question                   | Contribution to a build                                               |
| --------- | --------------------------------- | --------------------------------------------------------------------- |
| Equipment | What are my strengths?            | Attributes and the balance between offense, defense, and recovery.    |
| Essences  | What tools do I have?             | Active abilities, passive abilities, and their individual identities. |
| Combat Style      | How do those tools work together? | A defining interaction, an opportunity, and a meaningful limitation.  |

The promise is that two characters with the same equipment and Essences can pursue different strategies by choosing different Combat Styles. A recovery-heavy loadout could turn healing into protection through Bastion, or concentrate its other abilities into a powerful healing cast through Conduit.

The design succeeds when selecting a Combat Style makes the player reconsider at least one equipment choice and one Essence choice.

### Design principles

- **Identity immediately.** Selecting a Combat Style activates its defining mechanic immediately. The interesting behavior is not an eventual reward at the end of a progression tree.
- **Open combinations.** Combat Styles do not impose weapon, armor, element, or Essence-family restrictions. Some combinations will naturally benefit more than others.
- **A cost that changes decisions.** A Combat Style's limitation should influence preparation, timing, or survival. Each Combat Style explains that limitation as prominently as its benefit.
- **Preparation suits idle combat.** The player chooses a build before combat. Every Combat Style operates automatically during combat and while the player is offline.
- **Essences retain ownership of abilities.** Combat Styles transform or connect existing tools. Selecting a Combat Style does not grant an additional active ability.
- **Choices remain choices.** A player cannot eventually purchase every refinement and activate them together.
- **Mastery belongs to the Combat Style.** Playing an equipped Combat Style develops that Combat Style. Its full defining mechanic is available from level 1, and later bonuses preserve its central tradeoff.
- **Readable causes and outcomes.** Players can see what the Combat Style contributed and what its tradeoff cost them.

Combat Styles form a single third pillar. This design supersedes the earlier eight-style and talent-web concepts; those historical proposals do not add another selection or progression system alongside this feature.

## 2. The player's choices

### Build structure

A character equips **one Combat Style**, initially in its **base form**. At Combat Style level 3, the player can choose **one of its three refinements** or retain the base form. Refinements alter the defining mechanic and are mutually exclusive alternatives, not ranks. The base form remains a valid finished choice and benefits from levels and upgrades normally.

Further levels grant modest automatic **core ranks** and up to **two upgrade slots**. Core ranks strengthen the Combat Style's mechanic; upgrades provide smaller, situational adjustments. Neither grants another refinement or an additional active ability.

Some Combat Styles need one additional choice specific to their mechanic. Conduit, for example, asks the player to designate a Focus Essence. This is a choice among already equipped Essences and does not add an Essence slot.

Players can also leave the Combat Style slot empty while learning or comparing builds. An empty slot applies no Combat Style benefit or penalty.

### Introduction

Introduce Combat Styles when the player has unlocked a third Essence slot and acquired at least three different Essences. At that point there is a real loadout to reshape, while the player is still discovering build identities.

The introductory quest presents both developed Combat Styles in a practice encounter with temporary example loadouts. It demonstrates a benefit and a failure case for each: Bastion preparing Barrier and recovering slowly after a breach; Conduit producing both a charged cast and an undercharged cast. Practice does not award or consume the example equipment or Essences.

Completing the introduction unlocks both Combat Styles at **Combat Style level 1 with zero Combat Style XP**. Their core mechanics are immediately usable; refinements and upgrade slots follow each Combat Style's own level milestones. The practice encounter is guided learning, with no required victory or preferred answer, and awards no Combat Style XP. A player can finish it without being forced to adopt a Combat Style.

Progression combines individual Combat Style mastery with discovering combinations, obtaining better-suited equipment and Essences, and learning which configuration suits an encounter. Future Combat Styles would expand available strategies rather than replace early Combat Styles with stronger versions.

Conduit can function with the introductory three-slot loadout, but its base form needs a Focus Essence and three different contributors to reach maximum Charge. That higher ceiling becomes available naturally as the player gains another slot and a suitable Essence.

### Individual Combat Style levels and experience

Each unlocked Combat Style has its own permanent level, XP progress, and earned milestones. The initial proposed level range is **1–10**. A character can therefore have a level-10 Bastion and a level-1 Conduit. Selecting Conduit uses its own level; returning to Bastion restores access to everything already earned there.

- **Only the equipped Combat Style earns Combat Style XP.** Unequipped Combat Styles gain no passive XP, and an empty Combat Style slot earns none. There is no shared mastery pool, transferable XP, inherited level, or catch-up multiplier in this version.
- **Use ordinary eligible combat rewards.** Grant 1 Combat Style XP per point of the character's base combat XP reward, before character or Essence XP bonuses. Use the character's own reward share in group combat. Noncombat quest rewards and practice battles do not grant Combat Style XP.
- **Reward participation through the ordinary combat reward rules.** Combat Style XP does not depend on healing performed, Barrier generated, Charge spent, ability count, or whether the Combat Style's mechanic activated. A completed encounter that earns no base combat XP also earns no Combat Style XP. Reaching the character-level cap does not suppress otherwise eligible base combat XP for Combat Style progression.
- **Online and offline combat use the same rules.** Experience belongs to the Combat Style used for the rewarded encounter, even if the player has selected another Combat Style before collecting its rewards.
- **Earned progress is permanent.** Defeat, switching, changing a refinement, and replacing upgrades do not remove XP or levels.
- **Level 10 ends that Combat Style's XP progression.** Excess XP is discarded rather than banked for another Combat Style or another progression layer. A large reward can cross several levels, awarding every milestone reached.

Both developed Combat Styles use the same XP requirement schedule initially, while keeping separate XP totals. Exact level thresholds and time to reach level 10 remain tuning questions. The first three levels should be accessible during a short period of ordinary progression-relevant combat, so a newly tried Combat Style reaches its first specialization promptly.

### Level milestones, core ranks, and upgrade slots

| Combat Style level | Newly earned benefit | Total core rank | Available upgrade slots |
| ---: | --- | ---: | ---: |
| 1 | Full core mechanic and base form. | 0 | 0 |
| 2 | First core rank bonus. | 1 | 0 |
| 3 | All three refinements become available; choose at most one. | 1 | 0 |
| 4 | Second core rank bonus. | 2 | 0 |
| 5 | First upgrade slot and access to the Combat Style's three upgrade choices. | 2 | 1 |
| 6 | Third core rank bonus. | 3 | 1 |
| 7 | Continue toward the second upgrade slot; no additional combat bonus. | 3 | 1 |
| 8 | Fourth core rank bonus and second upgrade slot. | 4 | 2 |
| 9 | Continue toward the final core rank; no additional combat bonus. | 4 | 2 |
| 10 | Fifth core rank bonus; Combat Style level cap reached. | 5 | 2 |

Core ranks are automatic bonuses to the Combat Style's defining mechanic. They are not another level track, do not require spending points, and do not grant general attributes such as Power or Max Health. Their exact effects are defined under each developed Combat Style.

Upgrade slots each hold one choice from that Combat Style's upgrade menu. Each upgrade is a single fixed effect: no duplicate selections, separate upgrade levels, or additional upgrade ranks. All three choices become available at level 5, but only two can ever be equipped together. Slots may remain empty. Choices can be replaced freely at the same boundaries as other build changes.

Refinements remain the major behavioral choice. Upgrades cannot purchase another refinement, remove a Combat Style's core limitation, or increase the number of equipped Essences. There are no consumable Combat Style items, rank-point purchases, or additional upgrade currency in this proposal.

Unless explicitly labeled as ranked or upgraded, the combat formulas, refinement values, and worked examples below show **unranked values without upgrades**. They isolate the underlying mechanics; they do not imply that refinements are selectable at level 1. Actual previews include the core ranks already earned and the selected upgrades.

### Changing and saving a Combat Style

- Switching Combat Styles, available refinements, upgrades, or a Focus Essence is free between encounters and has no cooldown. The selected Combat Style always uses its own earned level and milestones.
- A Combat Style and its effective core rank, refinement, and upgrades are fixed for a committed activity: a single battle, an entire dungeon run, or a PvP match. The player reviews the choice before entering.
- During ordinary idle combat, a changed configuration takes effect at the next encounter. Previously completed combat keeps the configuration under which it occurred.
- A saved build remembers its equipment, Essences, Combat Style, refinement, upgrades, and any style-specific choice together. Loading a build uses that Combat Style's current earned level and cannot roll back, duplicate, or transfer XP. Locked choices cannot be activated through a saved build.
- Each Combat Style remembers its last selected refinement, upgrades, and Focus where applicable. Switching back restores those choices when they are still valid.
- If a saved Focus Essence is no longer equipped or eligible, the player chooses a replacement before starting combat with Conduit. The game identifies the missing choice; it does not silently select another Essence.
- Combat Style resources and encounter-generated protection follow encounter boundaries. Changing a Combat Style never creates healing, resets ability cooldowns, or transfers stored resources into another encounter.

Levels and milestones are earned when their XP is awarded. Newly earned core ranks take effect at the next boundary where the build may change; earning a level during a dungeon run does not alter that run's combat bonuses. A newly unlocked refinement or upgrade slot waits for the player's choice. Idle and offline combat continue with the existing selections and never pause or select an upgrade automatically.

## 3. Combat Style: Bastion

> **"Recovery becomes preparation. Build the protection you will need before the next blow lands."**

### Fantasy and identity

The Bastion is a fighter who turns recovery into a defensive reserve. Healing remains valuable even at full Health, but Health lost after that reserve breaks is difficult to restore quickly.

Its central question is: **How much recovery can I turn into protection without giving up the damage needed to finish the fight?**

Bastion supports a durable solo fighter, a protector accompanying summons or allies, and a fighter who spends protection to create offensive pressure. It does not require a shield or heavy armor.

### Core mechanic: Fortification

**Your self-generated healing restores 25% of its normal amount as Health and grants the remaining 75% as Barrier.**

For example, a self-heal worth 200 normally instead restores up to 50 Health and grants up to 150 Barrier. At full Health, the 50 Health portion is lost but the Barrier can still be gained.

The advantage is the ability to save recovery for later damage. The cost is much slower repair of actual Health loss. The unranked conversion does not increase the combined amount of Health and Barrier produced; later core ranks and selected upgrades can add a modest bonus without restoring ordinary self-healing.

### What counts as self-generated healing

Fortification applies to healing received by the Bastion from their own active and passive abilities, Health Regeneration, and Lifesteal. Healing supplied to the Bastion by their own summons also qualifies: a summon in the same build cannot bypass the Combat Style's recovery tradeoff.

Healing received from another player or that player's summons remains ordinary healing. Healing the Bastion gives to another character or summon also remains ordinary healing.

A summon does not itself inherit Fortification. Healing received by the summon remains ordinary healing; only healing supplied by it to its Bastion owner is converted.

Health costs, revives, effects that set or exchange current Health, and recovery outside combat are not healing conversions. Fortification cannot revive a dead character or prevent death after a lethal hit has already resolved.

### Healing, Barrier, and existing conditions

1. Determine the healing amount using its normal modifiers, including applicable Healing Power, Wound, Recovery, and regeneration modifiers.
2. Divide that amount into the Health and Barrier portions **before** limiting it to missing Health.
3. Restore only the Health that is actually missing. Add only the Barrier that fits within the shared Barrier cap.

Both unused Health restoration and Barrier overflow are lost. Overflow from one portion does not move into the other portion.

Converted Barrier follows ordinary Barrier rules: it absorbs damage before Health is lost, has no duration, and shares the cap of **2.5 times current Max Health** with every other Barrier source. Ordinary mitigation and Guard apply before damage consumes Barrier. If Max Health falls, the ordinary Barrier cap falls with it.

Wound reduces the source healing before conversion. For example, a 200-point heal reduced to 140 by Wound produces 35 Health and 105 Barrier. A naturally granted 150 Barrier remains a direct Barrier grant and is unaffected by Wound; Fortification does not convert or amplify it.

Only the amount of Health actually restored counts for effects requiring Health restoration. Creating converted Barrier does not additionally count as healing. Conversion is a single transformation of the original recovery; its outputs are not converted again. Neither a fully wasted heal nor rejected Barrier overflow grants a secondary benefit.

Converted Barrier participates in ordinary damage absorption and subsequent Barrier-absorption or Barrier-break reactions. It does not activate effects triggered solely by gaining Barrier. This applies to all Fortification protection, including both recipients of Shelter. It prevents a Barrier-gain reaction from creating healing that immediately recreates the same reaction through Fortification. A later incoming attack can still produce a normal absorption or break reaction. Naturally granted Barrier keeps its ordinary reactions.

### Automatic ability use

A self-healing ability remains useful when Health is full and Barrier has room. Automatic combat must recognize that opportunity, while preserving the ability's actual target restrictions and cast conditions. Fortification does not override an Essence explicitly limited to low-Health use.

The player should not need a Battle Plan system to make the central mechanic function. Equally, a full Health bar and a full Barrier pool are not a reason to cast a recovery-only ability that would accomplish nothing.

### Refinements: choose one, or remain in the base form

| Refinement        | Change to Fortification                                                                                                                                                                                                                                              | New decision and tradeoff                                                                               |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Rebuild**       | A self-heal that begins at or below 35% Health restores its entire normal amount as Health and grants no converted Barrier. Above that threshold, use the normal 25% / 75% split.                                                                                    | Gain an emergency recovery route, but stop preparing Barrier during that recovery.                      |
| **Counterweight** | When activating an Essence with an immediate direct enemy-damage component while holding at least 20% of Max Health as Barrier, spend Barrier equal to 10% of Max Health. Add the amount spent as damage to the cast's first direct attack attempt against an enemy. | Spend a defensive reserve to accelerate the fight, accepting a weaker position against the next attack. |
| **Shelter**       | Divide Fortification's Barrier portion equally between yourself and the other living ally with the lowest Health percentage. The Health portion still restores only your Health.                                                                                     | Protect another combatant by giving up half of your own newly generated protection.                     |

**Rebuild details.** Evaluate the threshold once, at the beginning of each healing event. One large heal can carry the character above 35% Health without being divided midway. At exactly 35%, Rebuild applies. Any amount above full Health is lost rather than converted into Barrier. Incoming healing modifiers apply normally.

The base form suits builds that depend on uninterrupted Barrier generation and later absorption or break reactions, including while Health is low. Rebuild interrupts that supply in exchange for repairing Health directly. That difference must be useful in practice for the base form to remain a worthwhile choice.

**Counterweight details.** Spend once per normal Essence activation, before the first direct attack attempt. Basic attacks, passive effects, periodic damage, reflected damage, summon attacks, and automatically repeated copies of a cast do not spend Barrier. The bonus uses that first attempt's damage type and applicable damage mitigation, but cannot critically strike, generate Lifesteal, or create extra on-hit effects. The original attack keeps its normal behavior. A miss loses the expenditure; later hits do not receive the bonus. Multiple hits or targets do not multiply the bonus; use the first enemy attack attempt in the ability's normal resolution order. If no direct attack ultimately occurs, the spent Barrier is still lost. Spending Barrier is not damage absorption and cannot activate effects that require absorbed damage. If the expenditure empties the pool, it does not activate a Barrier-break reaction. Counterweight can spend Barrier from any source and does not create a separate Barrier pool.

**Shelter details.** Your own summons and allied players are eligible recipients; enemies and yourself are not. Compare current Health percentages before distributing Barrier, using normal party order for a tie. Choose one recipient for the entire healing event. If there is no other living ally, keep the whole Barrier portion yourself. Each recipient's own cap applies separately; rejected protection is lost rather than redirected. Shared Barrier is a Barrier grant, never a new heal or a second Fortification conversion.

### Bastion core ranks and upgrades

Each core rank grants **2% more converted Barrier**, relative to the Barrier portion Fortification would otherwise produce. Five ranks give a total **10% bonus to that portion**. The ordinary Health portion remains 25% of the original healing amount.

At level 10, with five core ranks and no upgrades, a 200-point self-heal produces **50 Health and 165 Barrier** instead of 50 Health and 150 Barrier. Wound and other healing modifiers still apply before conversion. Natural Barrier grants, the shared Barrier cap, and Counterweight's spending threshold and damage conversion are unchanged.

Rebuild receives no Barrier bonus when it replaces conversion with full Health restoration. Shelter divides the converted Barrier after the rank bonus, then applies each recipient's cap separately.

| Upgrade | Effect | Build preference |
| --- | --- | --- |
| **Prepared Wall** | Gain an additional 10% of Fortification's unranked Barrier portion when beginning the healing event at or above 80% Health. | Prepare protection while relatively healthy. |
| **Hold the Breach** | Gain an additional 10% of Fortification's unranked Barrier portion when beginning the healing event with zero Barrier. | Reestablish protection after the reserve is exhausted. |
| **Measured Recovery** | Increase only Fortification's divided Health portion by 20%, changing it from 25% to 30% of the original healing amount. The Barrier portion is unchanged. | Recover somewhat more Health while retaining the conversion tradeoff. |

Evaluate upgrade conditions once, before that healing event restores Health or grants Barrier. Rank bonuses and the two conditional Barrier bonuses add relative to the same unranked Barrier portion: at five ranks, both conditions together produce `75% × (1 + 10% + 10% + 10%)` of the original healing amount as Barrier. A 200-point heal in that case produces up to 50 Health and 195 Barrier.

These upgrades affect only Fortification output. With Shelter, apply the bonuses once before dividing the Barrier. With Rebuild below its threshold, the event uses full ordinary healing instead, so none of these upgrades increases that event's output. Measured Recovery does not amplify outside healing, Lifesteal before conversion, or the full heal granted by Rebuild. All overflow and reaction restrictions still apply.

### Worked encounter: the base form

Assume the Bastion has 1,000 Max Health, begins at full Health with no Barrier, and uses a self-heal worth 200. Incoming damage below is already reduced by defenses.

| Moment             | Outcome                                            | Health | Barrier |
| ------------------ | -------------------------------------------------- | -----: | ------: |
| Encounter begins   | No protection is banked from earlier battles.      |  1,000 |       0 |
| Self-heal for 200  | The 50 Health portion is unused; gain 150 Barrier. |  1,000 |     150 |
| Receive 100 damage | Barrier absorbs the entire hit.                    |  1,000 |      50 |
| Receive 250 damage | Lose the remaining 50 Barrier and then 200 Health. |    800 |       0 |
| Self-heal for 200  | Restore 50 Health and gain 150 Barrier.            |    850 |     150 |

The last row expresses the Combat Style's limitation: the character has regained protection, but has not repaired most of the Health loss.

For comparison, with Rebuild, a character at 300 of 1,000 Health receiving the same 200-point self-heal reaches 500 Health and gains no converted Barrier. With Counterweight, a character holding 200 Barrier spends 100 on a qualifying cast, retains 100, and adds 100 damage before mitigation to its first direct hit.

### Example builds

These are patterns of Essence functions, not newly granted abilities or claims that a particular named Essence has every listed effect.

**Stoneheart — Rebuild.** Combine regular self-healing, one larger recovery ability, mitigation, and enough offense to make steady progress. Favor Max Health, appropriate Armor or Resistance, and recovery output on equipment. Regular healing builds protection above the threshold; the larger heal can repair Health after a serious breach. The opportunity cost is dedicating slots and attributes to survival instead of faster clears.

**Siegebreaker — Counterweight.** Combine direct damaging actives, a source of Lifesteal, recurring recovery, and a direct Barrier source. Balance offensive attributes with Max Health and mitigation. Recovery prepares Barrier, then direct Essence casts spend it to increase pressure. Faster casting also means more frequent defensive spending, so Cooldown investment can make the character less safe. The bonus damage itself does not generate Lifesteal; ordinary eligible damage still can.

**Hearthkeeper — Shelter.** Combine personal recovery with an Essence that summons a combatant, or enter group encounters with allies. Recovery supports two combatants at once, while the Bastion retains enough offense or protection to survive its smaller personal share. A vulnerable summon may receive repeated protection at the cost of protecting another ally; selecting fragile summons is therefore a meaningful commitment.

### Strengths, weaknesses, and counterplay

- Strong when damage arrives in separated bursts and recovery can prepare for the next one.
- Rewards mitigation because reduced incoming damage makes each Barrier point last longer.
- Makes recovery valuable during periods at full Health.
- Vulnerable to dangerous openings before protection is established, sustained damage above replenishment, and healing suppression.
- Gives up rapid Health repair in its base form. Allied healing is a deliberate team synergy, not a reason to assume that limitation disappears in solo balance.
- Can overinvest in survival and fail damage checks or encounter time limits.
- Does not automatically synergize with taking Health damage. For example, existing Thorns depends on qualifying damage reaching Health; absorbing that damage with Barrier reduces those opportunities.

### What the player should see

The Combat Style card shows the conversion and its cost together: **"200 self-healing becomes 50 Health + 150 Barrier. Actual Health recovers more slowly."**

That is the unranked illustration. The actual card shows Bastion's own level, XP to the next level, core rank, selected upgrades, and the resulting conversion. Conditional upgrade bonuses are identified as conditional rather than always included in the displayed amount. At level 10 without upgrades, the same illustration reads 50 Health + 165 Barrier.

Affected ability previews show their Health and Barrier outputs. The combat summary separates Health restored, converted Barrier granted, converted Barrier absorbed, and protection lost to overflow. Counterweight additionally shows Barrier spent and damage contributed; Shelter shows who received protection. Values describing wasted healing distinguish a full Health bar from a full Barrier pool.

## 4. Combat Style: Conduit

> **"Every part of the build feeds one decisive expression of power."**

### Fantasy and identity

The Conduit connects several Essences into a casting cycle. The player designates one equipped Essence as their **Focus Essence**. Other Essence actives build **Charge**, which the Focus Essence consumes when it casts.

Its central question is: **Which ability deserves the payoff, and can the rest of my loadout prepare it reliably?**

This develops the earlier idea of varied casts preparing a stronger repeat into one explicitly chosen payoff. A designated Focus makes the player's intention clear in automatic combat; the next incidental repeated ability cannot accidentally take the payoff instead.

Conduit can support a damaging centerpiece, a large heal, or a Barrier ability. A loadout must still function between its stronger Focus casts.

### Core mechanic: Circuit

Choose one eligible Focus Essence before combat. Begin each encounter with **0 Charge**, with a maximum of **3**.

- Each other equipped Essence's normal active cast grants **1 Charge**.
- Each different contributor can grant Charge only once between Focus casts. Repeatedly casting the same contributor does not grant more Charge during that cycle.
- Casting the Focus Essence spends all available Charge and begins a new cycle. Every contributor can then contribute again.
- Charge lasts until spent or the encounter ends. There is no time-based decay.

The Focus Essence's eligible effect amounts are multiplied by **80% + 20 percentage points per Charge spent**.

| Charge spent | Focus effect amount | Example: an otherwise 200-point effect |
| -----------: | ------------------: | -------------------------------------: |
|            0 |                 80% |                                    160 |
|            1 |                100% |                                    200 |
|            2 |                120% |                                    240 |
|            3 |                140% |                                    280 |

The cost is an underpowered Focus cast when the build has not prepared enough Charge. Contributors retain their ordinary effects; there is no blanket penalty on the rest of the loadout.

### Focus eligibility and effect boundaries

A Focus Essence must have at least one immediate direct damage, direct healing, or direct Barrier component in its active ability. The game identifies eligible Essences and highlights exactly which components will change.

The Charge multiplier affects those components of that one normal cast, across their ordinary hits and targets. It is applied once to each eligible amount. It does not create extra casts, hits, targets, or opportunities to trigger effects.

Condition stack counts, condition durations, damage-over-time ticks, control effects, summon count, summon attributes, cooldowns, and costs remain unchanged. A status-only or summon-only active can contribute Charge but cannot be selected as the Focus. A mixed ability can be the Focus when it contains an eligible component; its other components remain unchanged.

Damage-derived recovery keeps its normal relationship to actual damage. For example, Lifesteal from an empowered hit may heal more because the hit dealt more eligible Health damage; that recovery is not multiplied a second time as an additional Focus effect. The same rule applies to any healing calculated from damage already modified by the Focus.

Passive abilities, basic attacks, summon actions, and triggered or repeated copies of active abilities neither generate nor spend Charge and receive no additional Focus multiplier. Effects that explicitly copy an already resolved amount use that amount once, without applying another Conduit bonus.

### Casting and cycle rules

Charge is earned when an ordinary active cast occurs, once per cast rather than once per effect, hit, or target. An attempted action that never becomes a cast contributes nothing. A cast can contribute even if an enemy subsequently resists or avoids its effect.

The Focus checks and spends its Charge when its cast begins. The resulting multiplier applies to the whole cast and is not recalculated if Charge changes during it. A miss or an ineffective heal still spends the Charge; an action prevented before it begins does not.

At maximum Charge, further contributors cannot create overflow Charge or reserve it for a later cycle. A Focus cast, including a cast at zero Charge, ends the current cycle and clears contributor participation for the next one.

Conduit uses ordinary automated casting and cooldowns. It does not delay the Focus until fully charged, reorder abilities, or let the player manually spend Charge. When several abilities are ready together, their normal cast order determines which contribute first. The build preview shows that order so the player can assess the opening cycle. This visibility does not introduce a new editable priority system.

### Refinements: choose one, or remain in the base form

| Refinement         | Exact rule change                                                                                                                                                                                            | New decision and tradeoff                                                                                         |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| **Short Circuit**  | Every normal active cast by another Essence can grant Charge, even if that contributor already contributed in the cycle. Maximum Charge falls to 2; keep the normal 80% + 20 points per Charge formula.      | A frequently casting partner can prepare the Focus, but its maximum effect amount falls to 120%.                  |
| **Deep Reservoir** | Maximum Charge rises to 4, with four different contributors required. Replace the Focus formula with 60% + 25 points per Charge.                                                                             | Earn a 160% ceiling through a broader, slower setup, while underprepared Focus casts become substantially weaker. |
| **Relay**          | Keep a maximum of 3 and the distinct-contributor rule. Use 80% + 15 points per Charge. After a Focus cast spends at least 2 Charge, return 1 Charge to the new cycle when that cast resolves, up to the cap. | Accept a lower peak in exchange for a head start on later cycles.                                                 |

**Short Circuit details.** The same contributor can grant Charge on successive normal casts, but one cast still grants at most one Charge regardless of its hits or triggered copies. The Focus never charges itself.

**Deep Reservoir details.** Effect amounts at 0, 1, 2, 3, and 4 Charge are 60%, 85%, 110%, 135%, and 160%. Reaching the maximum requires at least five equipped Essences in total, including the Focus. Preview this requirement before the player selects the refinement.

**Relay details.** Effect amounts at 0, 1, 2, and 3 Charge are 80%, 95%, 110%, and 125%. The returned Charge is added to any Charge already earned in the new cycle, up to the cap of 3; it does not replace Charge earned while the Focus cast was resolving. All contributor eligibility resets normally. Spending only that single returned Charge provides no further return, so an idle Focus cannot sustain its own cycle. A miss still returns the Charge if at least two Charge were spent. The return does not carry past the end of the encounter.

### Conduit core ranks and upgrades

Each core rank adds **2 percentage points** to the Focus effect multiplier when the cast spends **at least 1 Charge**. Five ranks add 10 points once to the cast's eligible effect amounts, regardless of how many Charge were spent. Apply this after the selected refinement's formula. A zero-Charge cast receives no rank bonus: it still produces only 80% of its ordinary effect amount, or 60% with Deep Reservoir.

At level 10 without upgrades, the base form's effect amounts at 0, 1, 2, and 3 Charge are **80%, 110%, 130%, and 150%**. Fully charged ceilings become 130% for Short Circuit, 170% for Deep Reservoir, and 135% for Relay. Charge caps, contributor requirements, and Relay's return are unchanged.

| Upgrade | Effect | Build preference |
| --- | --- | --- |
| **Full Circuit** | Add 5 percentage points to eligible Focus effect amounts when spending the selected refinement's maximum Charge, or 3 in the base form. | Reliably complete preparation before the payoff. |
| **Partial Flow** | Add 5 percentage points to eligible Focus effect amounts when spending exactly 1 Charge. | Support useful smaller payoffs when the full circuit is rarely ready. |
| **Emergency Channel** | When beginning a Focus cast at or below 35% Health and spending at least 1 Charge, add 5 percentage points to its eligible direct healing and Barrier components targeting yourself. Direct damage components receive no bonus from this upgrade. | Improve personal recovery under pressure without strengthening an uncharged emergency cast. |

Upgrade bonuses add to the multiplier after the refinement formula and core rank bonus. They are percentage-point additions, not successive multipliers. Full Circuit and Partial Flow cannot apply together with any of the developed Charge caps. Emergency Channel can combine with either, but only for the eligible healing and Barrier components that target the Conduit themselves; healing or protecting an ally does not receive its bonus.

For example, a level-10 base-form Conduit spending 3 Charge uses 150% before upgrades. Full Circuit raises that to 155%. If Emergency Channel also qualifies, direct self-healing and self-Barrier components use 160%, while direct damage and effects on other targets remain at 155%. No upgrade changes a zero-Charge cast, grants starting Charge, alters cast order, or creates extra casts.

### Worked sequence: the base form

Assume four equipped Essences: contributors A, B, and C, plus Focus F. F has a direct effect worth 200 before its Conduit multiplier. This is an illustrative sequence of normal casts, not a manual rotation added by the Combat Style.

| Cast                                | Circuit outcome                       | Charge after cast | F's effect amount |
| ----------------------------------- | ------------------------------------- | ----------------: | ----------------: |
| A                                   | A contributes to this cycle.          |                 1 |                 — |
| B                                   | B contributes to this cycle.          |                 2 |                 — |
| A again                             | A has already contributed.            |                 2 |                 — |
| C                                   | C contributes; Charge is full.        |                 3 |                 — |
| F                                   | Spend 3; begin a new cycle.           |                 0 |               280 |
| F again, before another contributor | Spend 0; begin another cycle.         |                 0 |               160 |
| B                                   | B can contribute again in this cycle. |                 1 |                 — |
| F                                   | Spend 1.                              |                 0 |               200 |

The second Focus cast shows why the Combat Style is more than an automatic upgrade. Selecting a fast Focus with slow contributors can reduce the value of the build's most important ability.

### Example builds

**Storm Engine — base form.** Select a strong direct damage active as the Focus. Use three different contributors that provide useful damage, defense, or conditions while casting often enough to prepare it. Favor Power and appropriate offensive attributes without sacrificing survival. Cooldown investment matters through the resulting cadence of the whole loadout; it is not automatically valuable solely because the Focus casts more often.

**Pulsekeeper — Short Circuit.** Select a direct healing or Barrier active as the Focus and use one frequently casting contributor alongside other useful tools. Repeated casts from that contributor can prepare the Focus without requiring a wide range of contributors. This supports a smaller, more specialized kit, with a lower maximum payoff. If incoming pressure requires the Focus to cast early, undercharging remains a real cost.

**Grand Convergence — Deep Reservoir.** Select a large direct damage Focus and four distinct contributors that remain useful while preparing it. Build enough survival to reach the payoff. This favors encounters that last long enough for the full cycle and punishes a loadout that needs its uncharged Focus to win immediately. Adding four weak contributors merely to reach the maximum should be worse than building a smaller coherent loadout.

### Strengths, weaknesses, and counterplay

- Rewards a clear centerpiece supported by abilities with complementary roles and cadence.
- Gives utility-focused Essences another contribution to a build without replacing their normal functions.
- Supports both offensive and recovery-focused play through the chosen Focus.
- Can suffer in very short encounters or when the initial cast order produces an uncharged Focus.
- Loses value when control effects disrupt the expected sequence. Silence prevents active casts from generating or spending Charge while it lasts.
- Does not increase the potency of every effect in a complex Essence. The preview must make unaffected components unmistakable.
- Can overinvest in feeding the Focus, leaving the character vulnerable or ineffective between its casts.
- Becomes a poor choice if it requires an unrelated manual casting system to be enjoyable. Its build decisions must work with ordinary automatic combat.

### What the player should see

The Combat Style card shows the selected Focus, maximum Charge, the current refinement's full effect curve, and the eligible components of the Focus ability.

It also shows Conduit's own level, XP to the next level, core rank, available upgrade slots, and selected upgrades. The effect curve includes earned ranks, and conditional upgrades show the exact Charge and Health requirements for their additional bonus. Switching from a higher-level Bastion does not imply a higher-level Conduit or reuse Bastion's rank bonuses.

During combat, display Charge beside the Focus icon. Indicate which contributors have already charged the current cycle. The cast log identifies the Charge spent and the resulting effect multiplier, including casts below normal strength.

The post-combat summary shows Focus casts at each Charge level and their average multiplier. Report the loss from undercharged casts as well as the additional output from charged casts. A summary that counts only the stronger casts would conceal the Combat Style's central cost.

## 5. Three potential Combat Styles

These are concept seeds only. Their final mechanics, rank effects, upgrade menus, and balance are intentionally undecided. If developed, they follow the individual style-level model above and begin at their own level 1 when unlocked. They are not part of the two developed Combat Style designs above.

| Potential Combat Style | Fantasy and possible core mechanic                                                                                                                                                                                               | Build decision                                                                                                                       | Main design question                                                                                                                                                                                                 |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Reaper**     | Turn your own lingering damage into an earlier kill. Qualifying direct Essence attacks could consume some remaining damage from your own Bleed, Burn, or Poison on their target and deliver part of it immediately.              | Balance applying lasting pressure against harvesting it early; choose how many direct attacks belong in the loadout.                 | Can the timing tradeoff remain useful in automatic combat without making damage-over-time universally stronger? Other players' conditions must remain theirs to use.                                                 |
| **Shepherd**   | Bind your survival to one chosen summon. A bond could share protection or incoming damage between the character and that summon, while concentrating selected summon-related benefits on it. The Combat Style itself supplies no summon. | Choose a reliable companion and build mutual survival, accepting dependence on keeping that companion present.                       | Does the bond change how the owner builds and survives, rather than simply make the strongest summon stronger? A broken bond must be survivable enough to allow recovery.                                            |
| **Gambler**    | Trade dependable output for occasional exceptional moments. A controlled Fortune cycle could deliver weaker ordinary casts and intermittent amplified casts, with a visible limit on how long the weak period can last.          | Decide whether the build can survive uneven output and exploit a peak; damage and emergency recovery tolerate that risk differently. | Is the experience interesting beyond average damage calculations? Randomly failed survival checks must feel understandable, and repeatedly resetting encounters must not let players choose only favorable outcomes. |

## 6. Shared combat and activity expectations

### Encounters and persistent activities

The same Combat Style rules apply in idle combat, dungeons, bosses, and PvP. Charge begins empty in every new encounter. Barrier created by a previous encounter is cleared according to ordinary encounter rules. A new wave inside the same continuing battle does not reset Combat Style state; a new dungeon room treated as a new encounter does.

Combat Styles do not change an activity's normal starting Health, recovery between encounters, ability readiness, victory conditions, or defeat consequences. A player cannot heal by toggling a Combat Style, bank Charge on a harmless encounter, or carry an old Barrier reserve into a new one.

An equipped Combat Style, its level, effective core rank, refinement, and upgrades are visible when players inspect a build. Conduit's chosen Focus is also visible. Opponents should be able to understand the strategy and progression strength they are fighting. PvP uses the equipped Combat Style's earned progression; this proposal does not add automatic style-level normalization.

### Group play and summons

Each character owns their own Combat Style decision. Allies do not share Charge, and summoned combatants do not independently equip or inherit a Combat Style. Specific owner-summon interactions exist only where the chosen Combat Style explicitly describes them.

Bastion deliberately benefits from outside healing, and Shelter can support another player's character. These are team-building opportunities. They must be considered when assessing sustained group survival, rather than balancing Bastion solely around solo encounters.

### Balance intent

The two Combat Styles should excel in different circumstances, with useful reasons to retain their base forms and choose each refinement. Neither should be the default answer for every player, encounter, or loadout.

Compare Combat Style strength at equal levels and with the same number of unlocked upgrade slots before attributing an advantage to the Combat Style's identity. Separately assess the cost of switching from a mastered Combat Style to an untrained one. Individual progression intentionally gives the practiced Combat Style an advantage, but a new Combat Style must remain useful through its full level-1 mechanic. If switching feels excessively punishing, first adjust the early XP thresholds or the size of rank bonuses; do not silently introduce shared levels or automatic catch-up.

Evaluate the whole build: damage, survival, consistency, time to clear, wasted output, and the value of the Essence slots used to enable the Combat Style. A high peak or a large Barrier number is not sufficient evidence of a strong build.

At introduction, Conduit must be useful with three slots. At higher slot counts, its Charge cap and contributor opportunity costs must keep extra slots from producing unlimited scaling. Bastion must have useful self-recovery at the same introduction point without requiring a rare or specialized Essence combination.

## 7. Design playtests and unresolved tuning

The identities and choice structure above are the proposed design. The following playtests determine whether their numerical expressions need to change.

| Playtest                                                     | What it should establish                                                                                  |
| ------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| Same equipment and Essences, switch only the Combat Style            | Does the character's behavior change visibly, and can the player explain why?                             |
| Rebuild the loadout around each Combat Style afterward               | Does understanding the Combat Style lead to meaningful equipment and Essence changes?                             |
| Equal Combat Style levels and upgrade-slot budgets | Does each Combat Style remain competitive when progression advantages are controlled? |
| Switch from level-10 Bastion to level-1 Conduit and return | Is Conduit's core immediately useful, does its first specialization arrive promptly, and is Bastion's progress preserved? |
| Earn levels during offline combat and a committed dungeon run | Are XP ownership and milestone rewards clear, with new combat bonuses applied only at the permitted boundary and no automatic build choices? |
| Rank-zero values versus five core ranks and two upgrades | Do bonuses feel rewarding while Bastion still repairs Health slowly and Conduit still suffers from uncharged casts? |
| Three-slot introductory builds and later wider builds        | Can both Combat Styles work when unlocked, while later slots add options without overwhelming earlier builds?     |
| Short encounters, long encounters, and dangerous openings    | Are the different preparation costs meaningful without making one Combat Style unusable in ordinary play?         |
| Bastion under repeated burst damage                          | Is 25% Health restoration enough to recover between breaches, or does the base form become too punishing? |
| Bastion with Rebuild, Counterweight, and Shelter             | Does each refinement change the build's purpose? Is the base form still attractive?                       |
| Bastion with allies, summons, and strong healing suppression | Does the source distinction remain understandable, and can organized recovery create excessive stalling?  |
| Conduit under ordinary automated cast order                  | Can the player predict preparation and payoff without editing a script or controlling casts manually?     |
| Conduit with uneven cooldowns and mixed-effect Essences      | Are Focus eligibility, weak casts, and unmodified effects clear enough to support informed choices?       |
| Conduit with each refinement                                 | Are there useful loadouts for the small circuit, long preparation, and repeat-cycle approaches?           |
| Builds with no Combat Style and builds with the wrong Combat Style           | Are Combat Styles a rewarding source of strategy while their opportunity costs remain real and visible?           |
| PvP and encounters with time limits                          | Do durable combinations remain beatable, and do burst combinations allow understandable counterplay?      |

The first tuning questions are Bastion's 25% / 75% split, Rebuild's threshold, Counterweight's spending and damage amounts, and Conduit's zero-Charge penalty and Charge curves. Progression tuning covers the level-10 cap, XP thresholds, levels without a new combat benefit, time to the first refinement, core rank magnitudes, and the three upgrade choices for each developed Combat Style. Adjust these while preserving the central decisions: **Bastion trades immediate repair for preparation; Conduit trades reliable Focus output for a stronger prepared cast.**

## Design references

Existing terms follow the [combat lexicon](../combat-lexicon/README.md), especially [Barrier](../combat-lexicon/conditions/barrier.md), [Guard](../combat-lexicon/conditions/guard.md), [Wound](../combat-lexicon/conditions/wound.md), [Recovery](../combat-lexicon/conditions/recovery.md), [Regeneration](../combat-lexicon/conditions/regeneration.md), [Lifesteal](../combat-lexicon/conditions/lifesteal.md), and [damage categories](../combat-lexicon/damage-categories.md). These references establish the existing vocabulary; they do not indicate that Combat Styles are currently implemented.
