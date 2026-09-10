# Combat Styles

Game design and rationale · updated 9 September 2026

**Scope:** Two implemented Combat Styles, Bastion and Conduit, followed by three possible styles. The individual guides describe their current implementation or proposed design status. Progression below uses level 0–10 and an automatic core bonus at every level; numerical balance remains subject to playtesting.

**Progression decision:** Each character levels each Combat Style individually. Combat Style levels and experience are not shared. The level cap, milestone schedule, mastery level bonuses, and upgrade values below are provisional expressions of that decision.

**Related document:** [Implementation plan](implementation-plan.md).

## 1. The third character-building pillar

Combat Styles give a character a combat philosophy: a rule that changes how the player combines equipment and Essences.

| Pillar | Player question | Contribution to a build |
| --- | --- | --- |
| Equipment | What are my strengths? | Attributes and the balance between offense, defense, and recovery. |
| Essences | What tools do I have? | Active abilities, passive abilities, and their individual identities. |
| Combat Style | How do those tools work together? | A defining interaction, an opportunity, and a meaningful limitation. |

The promise is that two characters with the same equipment and Essences can pursue different strategies by choosing different Combat Styles. A recovery-heavy loadout could turn healing into protection through Bastion, or concentrate its other abilities into a powerful healing cast through Conduit.

The design succeeds when selecting a Combat Style makes the player reconsider at least one equipment choice and one Essence choice.

### Design principles

- **Identity immediately.** Selecting a Combat Style activates its defining mechanic immediately. The interesting behavior is not an eventual reward at the end of a progression tree.
- **Open combinations.** Combat Styles do not impose weapon, armor, element, or Essence-family restrictions. Some combinations will naturally benefit more than others.
- **Mechanics that change decisions.** Each Combat Style should influence preparation, timing, or survival. Its page explains the mechanic and resulting values without a separate Cost section or negative callouts.
- **Preparation suits idle combat.** The player chooses a build before combat. Every Combat Style operates automatically during combat and while the player is offline.
- **Essences retain ownership of abilities.** Combat Styles transform or connect existing tools. Selecting a Combat Style does not grant an additional active ability.
- **Choices remain choices.** A player cannot eventually purchase every refinement and activate them together.
- **Mastery belongs to the Combat Style.** Playing an equipped Combat Style develops that Combat Style. Its full defining mechanic is available from level 0, and later bonuses preserve its central tradeoff.
- **Readable causes and outcomes.** Players can see how a Combat Style changes their abilities through its description and numerical preview.

Combat Styles form a single third pillar. This design supersedes the earlier eight-style and talent-web concepts; those historical proposals do not add another selection or progression system alongside this feature.

## 2. The player's choices

### Build structure

A character equips **one global Combat Style** for all battles, initially in its **base form**. There are no per-activity Combat Style selections. At Combat Style level 3, the player can choose **one of its three refinements** or retain the base form. Refinements alter the defining mechanic and are mutually exclusive alternatives, not ranks. The base form remains a valid finished choice and benefits from levels and upgrades normally.

Every further level grants a modest automatic **mastery level bonus** and up to **two upgrade slots**. Level 7 grants an automatic **Opening Technique**, and level 9 unlocks **Upgrade Mastery** for one equipped upgrade. These benefits do not grant another refinement, active ability, or upgrade slot.

With Conduit, the first Essence in the loadout used for battle is your Channeled Essence: the ability your other Essences will strengthen. Empty slots are skipped. Arrange your Essences to choose the Channeled Essence; this does not add a separate selection or an Essence slot.

Players can also leave the Combat Style slot empty while learning or comparing builds. An empty slot applies no Combat Style benefit or penalty.

### Availability

Bastion and Conduit are available from **Combat Style level 0 with zero Combat Style XP**. There is no introductory quest, practice encounter, completion requirement, Essence-count requirement, or character-level gate. Their core mechanics are immediately usable; refinements and upgrade slots follow each Combat Style's own level milestones.

Selecting a style remains optional. The page explains its mechanic, numerical effects, and progression through the description, live preview, milestone grid, and help guide. The editor has no Cost section or separate tradeoff text.

Progression combines individual Combat Style mastery with discovering combinations, obtaining better-suited equipment and Essences, and learning which configuration suits an encounter. Future Combat Styles would expand available strategies rather than replace early Combat Styles with stronger versions.

Conduit requires an equipped Channeled Essence with direct damage, healing or Barrier. In the base form, three different other Essences can each add Charge before the Channeled Essence casts. Reaching the Charge cap without starting Charge therefore takes four equipped Essences in total, including the Channeled Essence. The style remains available with fewer Essences.

### Individual Combat Style levels and experience

Each unlocked Combat Style has its own permanent level, XP progress, and earned milestones. The mastery level range is **0–10**. A character can therefore have a level-10 Bastion and a level-0 Conduit. Selecting Conduit uses its own level; returning to Bastion restores access to everything already earned there.

- **Only the equipped Combat Style earns Combat Style XP.** Unequipped Combat Styles gain no passive XP, and an empty Combat Style slot earns none. There is no shared mastery pool, transferable XP, inherited level, or catch-up multiplier in this version.
- **Use ordinary eligible combat rewards.** Grant 1 Combat Style XP per point of the character's base combat XP reward, before character or Essence XP bonuses. Use the character's own reward share in group combat. Noncombat quest rewards do not grant Combat Style XP.
- **Reward participation through the ordinary combat reward rules.** Combat Style XP does not depend on healing performed, Barrier generated, Charge spent, ability count, or whether the Combat Style's mechanic activated. A completed encounter that earns no base combat XP also earns no Combat Style XP. Reaching the character-level cap does not suppress otherwise eligible base combat XP for Combat Style progression.
- **Online and offline combat use the same rules.** Experience belongs to the Combat Style used for the rewarded encounter, even if the player has selected another Combat Style before collecting its rewards.
- **Earned progress is permanent.** Defeat, switching, changing a refinement, and replacing upgrades do not remove XP or levels.
- **Level 10 ends that Combat Style's XP progression.** Excess XP is discarded rather than banked for another Combat Style or another progression layer. A large reward can cross several levels, awarding every milestone reached.

Both implemented Combat Styles use the same XP requirements while keeping separate progression. The [shared overview](README.md#current-xp-requirements) records the current level-0-to-10 schedule. Encounter rewards, victories, defeat retention and group shares determine advancement speed; there is no shared XP or catch-up bonus.

### Mastery level bonuses and upgrade slots

| Mastery level | Automatic level bonus | Additional unlock | Upgrade slots |
| ---: | --- | --- | ---: |
| 0 | Base mechanic; no level bonus | Full core and base form | 0 |
| 1 | +1% of Bastion's base converted Barrier / +1% flat increase to Conduit's charged Channeled Essence | — | 0 |
| 2 | +2% / +2% flat increase | — | 0 |
| 3 | +3% / +3% flat increase | Refinement selection | 0 |
| 4 | +4% / +4% flat increase | — | 0 |
| 5 | +5% / +5% flat increase | Upgrade slot 1 | 1 |
| 6 | +6% / +6% flat increase | — | 1 |
| 7 | +7% / +7% flat increase | Opening Technique | 1 |
| 8 | +8% / +8% flat increase | Upgrade slot 2 | 2 |
| 9 | +9% / +9% flat increase | Upgrade Mastery | 2 |
| 10 | +10% / +10% flat increase | Maximum mastery level | 2 |

Every mastery level improves the defining mechanic automatically. This replaces the former five Core Ranks earned at even levels. There is one level track, no rank counter or point spending, and no general attribute grant. The full mechanic is usable at level 0; level 1 supplies the first scaling bonus. Each level's bonus adds to the base amount rather than compounding.

Choose the bonuses that suit your Combat Style. Unlock your first slot at Mastery 5 and a second at Mastery 8.

Upgrade slots each hold one choice from that Combat Style's upgrade menu. There are no duplicate selections or separate upgrade levels. All three choices become available with the first slot, but only two can ever be equipped together. Slots may remain empty. At level 9, one equipped upgrade may additionally receive its authored mastery enhancement. Choices can be replaced freely at the same boundaries as other build changes.

### Level 7: Opening Technique

The technique activates automatically once at the start of each new battle, using the level captured for that battle. It works with the base form and every refinement and needs no additional selection.

| Style | Technique | Starting benefit |
| --- | --- | --- |
| Bastion | Entrenched | Gain Barrier equal to 5% of maximum Health, subject to the ordinary Barrier cap. |
| Conduit | Primed Circuit | Begin with 1 Charge, up to the refinement's cap. Each other Essence can still add Charge normally. |

Opening resources are freshly granted for each encounter. They are not transferred between encounters or granted again when another wave appears in a continuous battle. Summons do not independently inherit these techniques. Opening Barrier does not trigger Barrier-gain reactions or the healing conversion.

### Level 9: Upgrade Mastery

At Mastery 9, choose one of your equipped upgrades to gain its additional mastery effect.

Mastery augments that upgrade's ordinary effect and occupies no additional slot. Choosing another mastery replaces the previous one; leaving mastery unselected is allowed. Removing the mastered upgrade clears the mastery choice. Each Combat Style remembers its own selection, and Save/Discard applies to mastery alongside other build choices.

| Style | Upgrade | Mastery enhancement |
| --- | --- | --- |
| Bastion | Prepared Wall | You also gain this bonus when you have no Barrier. |
| Bastion | Hold the Breach | That heal also restores 20% more Health. |
| Bastion | Measured Recovery | Any overhealing is converted into Barrier. |
| Conduit | Full Circuit | This bonus now works whenever you spend 2 or more Charge. |
| Conduit | Partial Flow | This bonus now works with either 1 or 2 Charge. |
| Conduit | Emergency Channel | You gain this bonus at any Health, as long as your Channeled Essence spends at least 1 Charge. |

Prepared Wall grants its bonus once when either or both conditions are met. Hold the Breach multiplies the allocated Health portion after ordinary Measured Recovery, when both upgrades are equipped, and also applies to Rebuild's full Health heal. Measured Recovery converts 100% of the allocated Health healing above missing Health into additional Barrier after Health restoration; ordinary Barrier caps and Shelter distribution apply. Conduit's mastered upgrades keep the same bonuses and change when those bonuses apply.

Mastery does not automatically select an upgrade or alter the Channeled Essence. Numerical values are initial tuning and require balance playtesting. Existing committed battles keep their captured opening and mastery rules; newly earned milestones apply at the next permitted capture boundary.

Refinements remain the major behavioral choice. Upgrades cannot purchase another refinement, remove a Combat Style's core limitation, or increase the number of equipped Essences. There are no consumable Combat Style items, progression-point purchases, or additional upgrade currency in this proposal.

Unless explicitly labeled with a mastery level or upgrade, the combat formulas, refinement values, and worked examples below show **level-0 values without upgrades**. They isolate the underlying mechanics; they do not imply that refinements are selectable at level 0. Actual previews include the mastery level already earned and the selected upgrades.

### Changing and saving a Combat Style

- Switching Combat Styles, available refinements, upgrades, or the Essence arrangement that determines Channeled Essence is free between encounters and has no cooldown. The selected Combat Style always uses its own earned level and milestones.
- A Combat Style and its effective mastery level, refinement, and upgrades are fixed for a committed activity: a single battle, an entire dungeon run, or a PvP match. The player reviews the choice before entering.
- During ordinary idle combat, a changed configuration takes effect at the next encounter. Previously completed combat keeps the configuration under which it occurred.
- Saving updates one global Combat Style configuration for every battle type. Combat Styles do not provide combined saved builds or activity overrides. Existing equipment and Essence loadouts remain separate systems.
- Each Combat Style remembers its last selected refinement, upgrades, and mastered upgrade. Switching back restores those choices when they are still valid. Conduit's Channeled Essence always comes from the first occupied slot of the loadout used for that battle.
- If the first Essence has no direct damage, healing or Barrier, place a suitable Essence first before starting combat with Conduit. An empty loadout also needs an Essence. The game explains the requirement and never silently skips an unsuitable first Essence. Each activity can use its own Essence loadout and Channeled Essence while the Combat Style stays global.
- Combat Style resources and encounter-generated protection follow encounter boundaries. Changing a Combat Style never creates healing, resets ability cooldowns, or transfers stored resources into another encounter.

Levels and milestones are earned when their XP is awarded. Newly earned mastery level bonuses and Opening Techniques take effect at the next boundary where the build may change; earning a level during a dungeon run does not alter that run's combat bonuses. Newly unlocked refinements, upgrade slots, and Upgrade Mastery wait for the player's choice. Idle and offline combat continue with the existing selections and never pause or select an upgrade or mastery automatically.

## 3. Combat Style: Bastion

> **"Recovery becomes preparation. Build the protection you will need before the next blow lands."**

### Fantasy and identity

The Bastion is a fighter who turns recovery into a defensive reserve. Healing remains valuable even at full Health, but Health lost after that reserve breaks is difficult to restore quickly.

Its central question is: **How much recovery can I turn into protection without giving up the damage needed to finish the fight?**

Bastion supports a durable solo fighter, a protector accompanying summons or allies, and a fighter who spends protection to create offensive pressure. It does not require a shield or heavy armor.

### Core mechanic: Fortification

**All healing you receive during combat restores 25% of its normal amount as Health and grants the remaining 75% as Barrier.**

For example, receiving a heal worth 200 normally instead restores up to 50 Health and grants up to 150 Barrier. At full Health, the 50 Health portion is unused but the Barrier can still be gained. Measured Recovery mastery can also convert that unused Health allocation into Barrier.

Fortification saves recovery for later damage. The level-0 conversion retains the combined amount of Health and Barrier produced; later mastery level bonuses and selected upgrades can add a bonus to the allocated portions.

### Healing sources

Fortification applies to all healing received by the Bastion during combat, including active and passive abilities, Health Regeneration, Lifesteal, healing from other characters, and healing from any character's summons. The recipient's Bastion configuration determines the split and all level, refinement, upgrade, and mastery effects.

Healing supplied by a Bastion follows the recipient's rules. A non-Bastion ally receives ordinary healing; another Bastion converts that incoming healing using their own configuration.

A summon does not itself inherit Fortification. Healing received by the summon remains ordinary healing; healing supplied by it to any Bastion is converted.

Health costs, revives, effects that set or exchange current Health, and recovery outside combat are not healing conversions. Fortification cannot revive a dead character or prevent death after a lethal hit has already resolved.

### Healing, Barrier, and existing conditions

1. Determine the healing amount using its normal modifiers, including applicable Healing Power, Wound, Recovery, and regeneration modifiers.
2. Divide that amount into the Health and Barrier portions **before** limiting it to missing Health.
3. Restore only the Health that is actually missing. Add only the Barrier that fits within the shared Barrier cap.

Unused Health restoration and Barrier overflow are normally lost. Measured Recovery mastery converts unused Health allocation into additional Barrier before Shelter sharing and the recipients' Barrier caps.

Converted Barrier follows ordinary Barrier rules: it absorbs damage before Health is lost, has no duration, and shares the cap of **2.5 times current Max Health** with every other Barrier source. Ordinary mitigation and Guard apply before damage consumes Barrier. If Max Health falls, the ordinary Barrier cap falls with it.

Wound reduces the source healing before conversion. For example, a 200-point heal reduced to 140 by Wound produces 35 Health and 105 Barrier. A naturally granted 150 Barrier remains a direct Barrier grant and is unaffected by Wound; Fortification does not convert or amplify it.

Only the amount of Health actually restored counts for effects requiring Health restoration. Creating converted Barrier does not additionally count as healing. Conversion is a single transformation of the original recovery; its outputs are not converted again. Neither a fully wasted heal nor rejected Barrier overflow grants a secondary benefit.

Converted Barrier participates in ordinary damage absorption and subsequent Barrier-absorption or Barrier-break reactions. It does not activate effects triggered solely by gaining Barrier. This applies to all Fortification protection, including both recipients of Shelter. It prevents a Barrier-gain reaction from creating healing that immediately recreates the same reaction through Fortification. A later incoming attack can still produce a normal absorption or break reaction. Naturally granted Barrier keeps its ordinary reactions.

### Healing targets and automatic ability use

Healing target selection and prioritization remain unchanged and do not account for Barrier. A lowest-Health selector still chooses by Health, even when that recipient has substantial Barrier. Fortification transforms healing after its recipient has been selected; it does not redirect a heal or make an external healer seek Barrier capacity.

Ability restrictions and authored cast conditions remain unchanged. Existing usefulness checks for Bastion's own recovery and healing from its own summons remain in place: self-recovery may provide protection at full Health, and Shelter can still protect an ally when the owner's pools are full. Extending conversion to other healing sources does not extend those checks to their healing decisions.

### Refinements: choose one, or remain in the base form

| Refinement        | Change to Fortification                                                                                                                                                                                                                                              | New decision and tradeoff                                                                               |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Rebuild**       | Healing received at or below 35% Health restores its entire normal amount as Health and grants no converted Barrier. Above that threshold, use the normal 25% / 75% split.                                                                                    | Gain an emergency recovery route, but stop preparing Barrier during that recovery.                      |
| **Reprisal** | Store 25% of actual enemy damage absorbed by your Barrier as bonus damage, capped at 10% of Max Health. The next normally cast damaging Essence uses the stored amount once on its first direct enemy attack attempt. Barrier is not spent. | Prepare protection that also answers incoming enemy damage through an existing damaging Essence. |
| **Shelter**       | Split converted Barrier equally with the ally with the lowest health percentage; retain it all when alone.                                                                                     | Protect another combatant by giving up half of your own newly generated protection.                     |

**Rebuild details.** Evaluate the threshold once, at the beginning of each healing event. One large heal can carry the character above 35% Health without being divided midway. At exactly 35%, Rebuild applies. Any amount above full Health is normally lost; Measured Recovery mastery converts that excess Health allocation into Barrier. Incoming healing modifiers apply normally.

The base form suits builds that depend on uninterrupted Barrier generation and later absorption or break reactions, including while Health is low. Rebuild interrupts that supply in exchange for repairing Health directly. That difference must be useful in practice for the base form to remain a worthwhile choice.

**Reprisal details.** Keep Fortification's normal healing conversion. Actual enemy damage absorbed by the character's own Barrier adds 25% of that amount to stored bonus damage, up to 10% of Max Health. Any source may supply that Barrier, including allies, but absorption by other characters or summons does not build the owner's Reprisal. Hostile periodic and reflected damage can contribute; self-inflicted and allied damage cannot. Count the actual amount absorbed after normal damage prevention and mitigation; damage reaching Health does not contribute. Reprisal does not spend Barrier or trigger a Barrier-break reaction.

Reserve existing stored damage at the start of the next normally cast Essence with a direct enemy attack. Its first eligible direct enemy attack attempt consumes the whole-number portion once, including a miss or dodge; fractions remain stored. Damage absorbed during the cast remains stored for a later cast. The reserved and unreserved amounts share the cap. If no eligible attack attempt occurs, return the reserved amount to storage subject to that cap. Recheck the cap before release if Max Health has fallen.

Basic attacks, passive effects, periodic damage, reflected damage, summon attacks, and automatically repeated copies do not release Reprisal. Multiple hits or targets do not multiply the bonus. The first direct enemy attack attempt receives a noncritical contribution using its damage type and normal target mitigation, without further outgoing damage amplification. The bonus does not generate Lifesteal or additional damage-derived hit reactions; the original attack keeps its normal behavior.

With 1,000 Max Health, Reprisal stores up to 100 bonus damage. Absorbing 200 enemy damage stores 50; another 300 absorbed before release raises the stored amount to the 100-point cap. The attack uses the stored damage without consuming any of the character's remaining Barrier. Mastery level bonuses improve Fortification's converted Barrier; Reprisal's 25% storage rate and 10% Max Health cap remain constant.

Reprisal starts empty in a new battle and remains encounter-local. Continuous waves and revival within that battle retain the stored amount in runtime state.

**Shelter details.** Your own summons and allied players are eligible recipients; enemies and yourself are not. Compare current Health percentages before distributing Barrier, using normal party order for a tie. Choose one recipient for the entire healing event. If there is no other living ally, keep the whole Barrier portion yourself. Each recipient's own cap applies separately; rejected protection is lost rather than redirected. Shared Barrier is a Barrier grant, never a new heal or a second Fortification conversion.

### Bastion mastery levels and upgrades

Each mastery level grants **1% more converted Barrier**, relative to the base Barrier portion Fortification would otherwise produce. Level 1 grants 1%; level 10 gives a total **10% bonus to that portion**. The ordinary Health portion remains 25% of the original healing amount.

At level 10, with its +10% level bonus and no upgrades, a 200-point heal received produces **50 Health and 165 Barrier** instead of 50 Health and 150 Barrier. Wound and other healing modifiers still apply before conversion. Natural Barrier grants, the shared Barrier cap, and Reprisal's storage rate and damage cap are unchanged.

Rebuild receives no Barrier bonus when it replaces conversion with full Health restoration. Shelter divides the converted Barrier after the mastery level bonus, then applies each recipient's cap separately.

| Upgrade | Effect | Build preference |
| --- | --- | --- |
| **Prepared Wall** | When you receive healing at 80% Health or higher, Fortification grants extra Barrier equal to 7.5% of the heal. | Prepare protection while relatively healthy. |
| **Hold the Breach** | When you receive healing with no Barrier, Fortification grants extra Barrier equal to 7.5% of the heal. | Reestablish protection after the reserve is exhausted. |
| **Measured Recovery** | Fortification’s normal healing split restores 30% as Health, up from 25%. | Recover somewhat more Health while retaining the conversion tradeoff. |

Barrier bonuses apply when Fortification converts healing, before any sharing.

Evaluate upgrade conditions once, before that healing event restores Health or grants Barrier. Each conditional Barrier bonus equals 10% of Fortification's level-0 Barrier portion: `75% × 10% = 7.5%` of the heal. Mastery level bonuses and the two conditional Barrier bonuses add relative to that same level-0 Barrier portion: at level 10, both conditions together produce `75% × (1 + 10% + 10% + 10%)` of the original healing amount as Barrier. A 200-point heal in that case produces up to 50 Health and 195 Barrier. Measured Recovery increases the divided Health allocation by 20%, from 25% to 30% of the heal, while leaving the ordinary Barrier allocation unchanged.

These upgrades affect Fortification output from any healing source. With Shelter, apply the bonuses once before dividing the Barrier. Rebuild at or below its threshold uses the full Health allocation, so ordinary Measured Recovery does not amplify that event; its bonus affects only the divided Health portion. Hold the Breach mastery can improve Rebuild's Health allocation, and Measured Recovery mastery can convert Rebuild's excess healing into Barrier. Healing and Lifesteal amounts are not multiplied again before conversion. All Barrier caps and reaction restrictions still apply.

The core preview distinguishes flat increases from multiplicative Health recovery. Flat bonuses add directly to the displayed percentage, while ×1.2 scales the amount it applies to. These comparisons isolate the stated bonuses; actual previews also include other selected bonuses and earned mastery levels where applicable.

| Preview bonus | Display for a 200-point heal | Calculation |
| --- | --- | --- |
| Prepared Wall or Hold the Breach | `+7.5% flat increase · +15 Barrier` | Add to the share of the heal converted into Barrier: `75% + 7.5% flat = 82.5%`, before mastery level bonuses. |
| Measured Recovery | `+20% Health recovery (×1.2) · 60 Health` | Multiply the divided Health allocation: `25% × 1.2 = 30%` of the heal. |
| Hold the Breach mastery with Measured Recovery | `+20% Health recovery (×1.2) · 72 Health` | Apply the second multiplier after Measured Recovery: `25% × 1.2 × 1.2 = 36%` of the heal. |

Prepared Wall's ordinary condition reads, “Gain extra Barrier when you receive healing at 80% Health or higher.” Hold the Breach reads, “Gain extra Barrier when you receive healing with no Barrier.” Prepared Wall mastery also qualifies with no Barrier. The Barrier additions scale with the heal and apply only when Fortification converts healing. Hold the Breach mastery also multiplies Rebuild's full Health allocation, giving 240 Health from a 200-point heal. Displayed Health totals are allocated amounts before missing-Health limits and are not additional amounts to add to the ordinary preview again.

### Worked encounter: the base form

Assume the Bastion has 1,000 Max Health, begins at full Health with no Barrier, and receives healing worth 200. Incoming damage below is already reduced by defenses.

| Moment             | Outcome                                            | Health | Barrier |
| ------------------ | -------------------------------------------------- | -----: | ------: |
| Encounter begins   | No protection is banked from earlier battles.      |  1,000 |       0 |
| Receive 200 healing | The 50 Health portion is unused; gain 150 Barrier. |  1,000 |     150 |
| Receive 100 damage | Barrier absorbs the entire hit.                    |  1,000 |      50 |
| Receive 250 damage | Lose the remaining 50 Barrier and then 200 Health. |    800 |       0 |
| Receive 200 healing | Restore 50 Health and gain 150 Barrier.            |    850 |     150 |

The last row expresses the Combat Style's limitation: the character has regained protection, but has not repaired most of the Health loss.

For comparison, with Rebuild, a character at 300 of 1,000 Health receiving the same 200-point heal reaches 500 Health and gains no converted Barrier. With Reprisal, a character whose Barrier absorbs 200 enemy damage stores 50 bonus damage for the next qualifying Essence's first direct enemy attack attempt. Releasing that bonus spends no Barrier.

### Example builds

These are patterns of Essence functions, not newly granted abilities or claims that a particular named Essence has every listed effect.

**Stoneheart — Rebuild.** Combine regular self-healing, one larger recovery ability, mitigation, and enough offense to make steady progress. Favor Max Health, appropriate Armor or Resistance, and recovery output on equipment. Regular healing builds protection above the threshold; the larger heal can repair Health after a serious breach. The opportunity cost is dedicating slots and attributes to survival instead of faster clears.

**Siegebreaker — Reprisal.** Combine direct damaging actives, recurring recovery, and a direct or allied Barrier source. Recovery prepares protection, enemy damage absorbed by that protection stores Reprisal, and a normally cast damaging Essence releases it. Entrenched can prepare the first absorption at battle start. Equipment still supplies the build's attributes; Reprisal adds an automatic interaction without another active ability or an attribute bonus. Ordinary eligible damage can generate Lifesteal; Reprisal's added contribution does not.

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

The Combat Style card shows the conversion: **"200 healing received becomes 50 Health + 150 Barrier."**

That is the level-0 illustration. The actual card shows Bastion's own level, XP to the next level, current level bonus, selected upgrades, and the resulting conversion. Conditional upgrade bonuses are identified as conditional rather than always included in the displayed amount. At level 10 without upgrades, the same illustration reads 50 Health + 165 Barrier.

Affected ability previews show their Health and Barrier outputs. Selecting Reprisal also shows its authored storage rate and cap, plus **"200 absorbed → 50 bonus damage"** for a character with 1,000 Max Health. This is a rule example, not a new combat resource control. The combat summary uses the ordinary damage, healing, and Barrier statistics without a separate Combat Style panel. Detailed style conversion, overflow, Reprisal, and Shelter accounting is reserved for internal balance analysis.

## 4. Combat Style: Conduit

> **"Every part of the build feeds one decisive expression of power."**

### Fantasy and identity

The first Essence in your battle loadout is your **Channeled Essence**. **Charge** is the resource your other Essences build for it. When the Channeled Essence casts, it spends all stored Charge to strengthen the damage, healing and Barrier it delivers directly.

Its central question is: **Which ability deserves the payoff, and can the rest of my loadout prepare it reliably?**

The player chooses which Essence receives the bonus by placing it first in the loadout. Combat remains automatic: the other Essences build Charge through their normal casts, and only the Channeled Essence spends it.

The Channeled Essence can be a damaging ability, a large heal, or a Barrier ability. The rest of the build still needs to work between Channeled Essence casts.

### Core mechanic: Circuit

The first occupied slot of the battle's Essence loadout supplies the Channeled Essence. In the base form, hold up to **3 Charge**. Each new battle starts with **0 Charge**, or **1 Charge** once Primed Circuit unlocks at mastery level 7.

- Each other equipped Essence's normal active cast grants **1 Charge**.
- Each of those Essences can add Charge only once before the Channeled Essence casts. Casting the same Essence again adds no more Charge; Short Circuit is the refinement that changes this rule.
- Casting the Channeled Essence spends all stored Charge. Each of the other Essences can then add Charge again.
- Charge lasts until spent or the encounter ends. There is no time-based decay.

Your Channeled Essence starts at **80% of its normal strength**. Each Charge adds **a flat +20%** to its direct damage, healing and Barrier: 1 Charge brings it to 100%, and 2 Charge brings it to 120%. Mastery levels and upgrades add their bonuses afterward.

| Charge spent | Channeled Essence strength | Example: a direct heal normally worth 200 |
| -----------: | ------------------: | -------------------------------------: |
|            0 |                 80% |                                    160 |
|            1 |                100% |                                    200 |
|            2 |                120% |                                    240 |
|            3 |                140% |                                    280 |

With no Charge, the Channeled Essence uses 80% of its normal strength. Your other Essences keep their normal damage, healing and other effects while building Charge.

### Choosing a Channeled Essence and what Charge changes

A Channeled Essence must be an equipped Essence whose active ability deals direct damage, heals directly, or grants Barrier directly. The game checks its current ability, including its evolution. The Essence page identifies the first occupied slot with a Channeled Essence badge while Conduit is equipped and explains when that Essence is unsuitable. The Channel Essence action swaps an eligible equipped Essence into the first occupied position in one save without removing any other Essence.

Charge strengthens those direct damage, healing and Barrier amounts across the normal cast's hits and targets. Each amount receives the bonus once. The cast keeps its normal number of hits and targets and its normal chances to trigger other effects.

Charge does not change condition stacks or durations, damage or healing over time, control effects, summon count or attributes, cooldowns, or resource costs. An Essence that only applies conditions or summons allies can build Charge, but cannot be the Channeled Essence. If an ability combines those effects with direct damage, healing or Barrier, it can be the Channeled Essence; only those direct amounts change.

Healing based on damage dealt, such as Lifesteal, still uses the actual Health damage. A stronger hit can therefore heal more through Lifesteal, but that healing does not receive the Channeled Essence bonus again. The same rule applies to any healing calculated from damage the Channeled Essence already strengthened.

Passive abilities, basic attacks, summons' actions, and triggered copies or automatically repeated casts do not build or spend Charge or receive another Channeled Essence bonus. Secondary damage is also unchanged. An effect that copies damage or healing already calculated uses that amount once, without adding another Conduit bonus.

### When Charge is gained and spent

Charge is gained when another Essence's active ability begins a normal cast. One cast adds at most 1 Charge, however many hits, targets or effects it has. An action stopped before it becomes a cast adds nothing. Once the cast starts, it can add Charge even if its target later resists or dodges it.

The Channeled Essence spends Charge when its cast begins. That amount determines the bonus for the whole cast, even if new Charge arrives before it finishes. A miss or a heal that restores no Health still spends Charge. An action stopped before it becomes a cast does not.

Once Charge is full, further casts cannot save extra Charge for later. After every Channeled Essence cast, including one with no Charge, each of the other Essences can add Charge again.

Essences cast automatically using their normal cooldowns. Slot position determines the Channeled Essence, not a casting sequence. The Combat Styles page explains the first-Essence rule; there is no separate Channeled Essence dropdown. Channeled Essence is derived from each battle's actual loadout when that battle is prepared and stays captured for its normal lifetime.

### Refinements: choose one, or remain in the base form

| Refinement | What changes | Build preference |
| --- | --- | --- |
| **Short Circuit** | Your other Essences build 1 Charge every time they cast, even if the same Essence casts again. Store up to 2 Charge. | One frequently casting Essence can fill Charge. The Channeled Essence keeps its 80% + 20% flat per Charge formula, reaching 120% before mastery levels and upgrades. |
| **Deep Reservoir** | Store up to 4 Charge, with each of your other Essences building 1 Charge between Channeled Essence casts. Your Channeled Essence's immediate damage, healing and Barrier start at 60% of normal strength and gain a +25% flat increase per Charge spent. | Four different other Essences can build toward 160% strength before mastery levels and upgrades. |
| **Relay** | After your Channeled Essence spends 2 or more Charge, regain 1 Charge for its next cast. Its immediate damage, healing and Barrier start at 80% of normal strength and gain a +15% flat increase per Charge spent. | Start building for the next Channeled Essence cast with 1 Charge already stored. Keep a cap of 3 and one Charge from each other Essence between Channeled Essence casts. |

**Short Circuit details.** The same other Essence can add Charge on successive normal casts. Each cast still adds at most 1 Charge, regardless of its hits or triggered copies. The Channeled Essence never adds Charge to itself.

**Deep Reservoir details.** Channeled Essence strength at 0, 1, 2, 3, and 4 Charge is 60%, 85%, 110%, 135%, and 160%. Building all 4 Charge from other Essence casts requires at least five equipped Essences in total, including the Channeled Essence. Show this requirement before the player selects the refinement; Primed Circuit supplies 1 starting Charge separately.

**Relay details.** Channeled Essence strength at 0, 1, 2, and 3 Charge is 80%, 95%, 110%, and 125%. The returned Charge adds to any new Charge gained while the Channeled Essence was casting, up to the cap of 3. Each other Essence can add Charge again after the Channeled Essence begins its cast. Spending just the 1 returned Charge gives no further return, so the Channeled Essence still needs other Essences to build Charge. A miss still returns 1 Charge if at least 2 were spent. Stored Charge does not carry into a new battle.

### Conduit mastery levels and upgrades

Each mastery level adds **a flat +1%** to the Channeled Essence's direct damage, healing and Barrier when it spends **at least 1 Charge**. Level 1 gives a +1% flat increase and level 10 gives a +10% flat increase, applied once regardless of how much Charge was spent. Add this after the selected refinement's formula. With no Charge, the Channeled Essence receives no level bonus: it stays at 80% strength, or 60% with Deep Reservoir.

At level 10 without upgrades, the base form's strength at 0, 1, 2, and 3 Charge is **80%, 110%, 130%, and 150%**. At maximum Charge, Short Circuit reaches 130%, Deep Reservoir 170%, and Relay 135%. Levels do not change how Essences build Charge, how much you can hold, or when Relay returns it.

| Upgrade | Effect | Build preference |
| --- | --- | --- |
| **Full Circuit** | Spending maximum Charge adds a flat +5% to your Channeled Essence’s immediate damage, healing and Barrier. | Reliably complete preparation before the payoff. |
| **Partial Flow** | Spending exactly 1 Charge adds a flat +5% to your Channeled Essence’s immediate damage, healing and Barrier. | Support useful smaller payoffs when the full circuit is rarely ready. |
| **Emergency Channel** | When your Channeled Essence spends Charge at 35% Health or lower, add a flat +5% to the healing and Barrier it gives you immediately. | Improve personal recovery under pressure while spending at least 1 Charge. |

Maximum Charge means the selected refinement's cap, or 3 in the base form. Add upgrade bonuses after the form's strength and mastery level bonus. Full Circuit and Partial Flow normally require different amounts of Charge; mastering an upgrade can let both apply to the same cast. Emergency Channel checks your Health when the Channeled Essence starts casting. It can combine with either upgrade, but improves only direct healing and Barrier on you. It adds nothing to damage or healing and Barrier given to an ally.

The core preview shows each result as a percentage **of normal strength**, and shows the **Charge limit** alongside a plain explanation of how your other Essences build Charge. Upgrade bonuses are labeled `+5% flat increase`. The displayed results for each Charge amount already include Full Circuit and Partial Flow when their conditions are met. Emergency Channel is shown separately because it applies only to direct healing and Barrier on you. Flat bonuses add directly: `150% + 5% flat = 155%`. A multiplier such as ×1.2 instead scales the amount it applies to.

For example, a level-10 base-form Conduit spending 3 Charge uses 150% strength before upgrades. Full Circuit raises that to 155%. If Emergency Channel also applies, direct healing and Barrier on you use 160%, while damage and effects on other targets stay at 155%. These upgrades strengthen charged Channeled Essence casts without granting starting Charge, changing cooldowns, or making the ability cast again.

### Worked sequence: the base form

Assume four equipped Essences: A, B, C, and your Channeled Essence F. F has a direct heal normally worth 200. These are examples of automatic casts, not a manual casting order added by the style.

| Cast                                | What happens                          | Charge after cast | F's healing before other rules |
| ----------------------------------- | ------------------------------------- | ----------------: | ----------------: |
| A                                   | A adds 1 Charge.                      |                 1 |                 — |
| B                                   | B adds 1 Charge.                      |                 2 |                 — |
| A again                             | A already added Charge since the last Channeled Essence cast. |      2 |                 — |
| C                                   | C adds 1 Charge; Charge is full.       |                 3 |                 — |
| F                                   | Spend 3; other Essences can add Charge again. |          0 |               280 |
| F again, before another Essence     | Spend 0.                              |                 0 |               160 |
| B                                   | B can add Charge again.               |                 1 |                 — |
| F                                   | Spend 1.                              |                 0 |               200 |

The second Channeled Essence cast shows why the other Essences' cooldowns matter. If the Channeled Essence casts before they can build Charge, it uses less of its normal strength.

### Example builds

**Storm Engine — base form.** Choose a strong direct damage ability as the Channeled Essence. Equip three other Essences that provide useful damage, defense or conditions and cast often enough to build Charge. Favor Power and the relevant offensive attributes while keeping enough defense to survive. Consider all four abilities' cooldowns together: making the Channeled Essence cast faster helps only if the other Essences can keep building Charge for it.

**Pulsekeeper — Short Circuit.** Choose a direct healing or Barrier ability as the Channeled Essence, with one other Essence that casts frequently. That Essence can build both Charge through repeated normal casts, leaving room for other useful abilities. This suits a smaller set of Essences, with a lower maximum Channeled Essence strength. A Channeled Essence that casts early still spends whatever Charge is available.

**Grand Convergence — Deep Reservoir.** Choose a large direct damage Channeled Essence and four different Essences that remain useful while building Charge. Build enough defense to survive until the Channeled Essence is ready. Longer encounters give those Essences time to fill all 4 Charge; a build that needs the Channeled Essence immediately may get less from this refinement. Each supporting Essence should earn its place through its own ability as well as the Charge it adds.

### Strengths, weaknesses, and counterplay

- Rewards choosing one important ability and other Essences whose roles and cooldowns work well with it.
- Lets utility Essences build Charge while continuing to perform their usual role.
- Supports both offensive and recovery-focused play through the chosen Channeled Essence.
- Can suffer in very short encounters or when the Channeled Essence becomes ready before other abilities have generated Charge.
- Control effects can interrupt the casts that build Charge. Silence prevents active casts from building or spending Charge while it lasts.
- Changes only the Channeled Essence's direct damage, healing and Barrier. The preview must explain which parts of an ability stay unchanged.
- Choosing Essences only for Charge can leave the character without enough damage or protection between Channeled Essence casts.
- Becomes a poor choice if it requires an unrelated manual casting system to be enjoyable. Its build decisions must work with ordinary automatic combat.

### What the player should see

The Combat Style card shows the chosen Channeled Essence, maximum Charge, how strong the Channeled Essence is at each Charge amount, and which parts of its ability receive the bonus.

It also shows Conduit's own level, XP to the next level, level bonus, upgrade slots and selected upgrades. The strength shown at each Charge amount includes earned mastery level bonuses. Upgrade descriptions state how much Charge or Health their bonuses require. Switching from a higher-level Bastion does not raise Conduit's level or give it Bastion's bonuses.

During combat, display Charge beside the Channeled Essence icon. Show which other Essences have already added Charge since the last Channeled Essence cast. The cast log states how much Charge the Channeled Essence spent and its resulting strength, including casts below normal strength.

The post-combat summary has no separate Combat Style panel or Charge breakdown. The Combat Styles page shows numerical strength for each Charge amount, without negative callouts. Internal balance analysis can compare how much damage, healing and Barrier each Charge amount adds or removes.

## 5. Three potential Combat Styles

The table retains the original concept seeds. [Reaper](styles/reaper.md), [Shepherd](styles/shepherd.md) and [Gambler](styles/gambler.md) now have developed design proposals with provisional mechanics, per-level bonuses, upgrades and mastery. They remain unimplemented. Each proposal starts at level 0 and follows the shared mastery-level and unlock schedule.

| Potential Combat Style | Fantasy and possible core mechanic                                                                                                                                                                                               | Build decision                                                                                                                       | Main design question                                                                                                                                                                                                 |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Reaper**     | Turn your own lingering damage into an earlier kill. Qualifying direct Essence attacks could consume some remaining damage from your own Bleed, Burn, or Poison on their target and deliver part of it immediately.              | Balance applying lasting pressure against harvesting it early; choose how many direct attacks belong in the loadout.                 | Can the timing tradeoff remain useful in automatic combat without making damage-over-time universally stronger? Other players' conditions must remain theirs to use.                                                 |
| **Shepherd**   | Bind your survival to one chosen summon. A bond could share protection or incoming damage between the character and that summon, while concentrating selected summon-related benefits on it. The Combat Style itself supplies no summon. | Choose a reliable companion and build mutual survival, accepting dependence on keeping that companion present.                       | Does the bond change how the owner builds and survives, rather than simply make the strongest summon stronger? A broken bond must be survivable enough to allow recovery.                                            |
| **Gambler**    | Trade dependable output for occasional exceptional moments. A controlled Fortune cycle could deliver weaker ordinary casts and intermittent amplified casts, with a visible limit on how long the weak period can last.          | Decide whether the build can survive uneven output and exploit a peak; damage and emergency recovery tolerate that risk differently. | Is the experience interesting beyond average damage calculations? Randomly failed survival checks must feel understandable, and repeatedly resetting encounters must not let players choose only favorable outcomes. |

## 6. Shared combat and activity expectations

### Encounters and persistent activities

The same Combat Style rules apply in idle combat, dungeons, bosses, and PvP. Each new encounter starts with 0 Charge, or 1 Charge once Primed Circuit unlocks at mastery level 7. Barrier created by a previous encounter is cleared according to ordinary encounter rules. A new wave inside the same continuing battle does not reset Combat Style state or grant starting Charge again; a new dungeon room treated as a new encounter does.

Combat Styles do not change an activity's normal starting Health, recovery between encounters, ability readiness, victory conditions, or defeat consequences. A player cannot heal by toggling a Combat Style, bank Charge on a harmless encounter, or carry an old Barrier reserve into a new one.

An equipped Combat Style, its level, effective mastery level, refinement, and upgrades are visible when players inspect a build. Conduit's chosen Channeled Essence is also visible. Opponents should be able to understand the strategy and progression strength they are fighting. PvP uses the equipped Combat Style's earned progression; this proposal does not add automatic style-level normalization.

### Group play and summons

Each character owns their own Combat Style decision. Allies do not share Charge, and summoned combatants do not independently equip or inherit a Combat Style. Specific owner-summon interactions exist only where the chosen Combat Style explicitly describes them.

Bastion converts outside healing through the same Fortification rules as its own recovery, and Shelter can support another player's character. These are team-building opportunities. Assess sustained group survival as well as solo encounters, using unchanged Health-based healing targets.

### Balance intent

The two Combat Styles should excel in different circumstances, with useful reasons to retain their base forms and choose each refinement. Neither should be the default answer for every player, encounter, or loadout.

Compare Combat Style strength at equal levels and with the same number of unlocked upgrade slots before attributing an advantage to the Combat Style's identity. Separately assess the cost of switching from a mastered Combat Style to an untrained one. Individual progression intentionally gives the practiced Combat Style an advantage, but a new Combat Style must remain useful through its full level-0 mechanic. If switching feels excessively punishing, first adjust the early XP thresholds or the size of mastery level bonuses; do not silently introduce shared levels or automatic catch-up.

Evaluate the whole build: damage, survival, consistency, time to clear, wasted output, and the value of the Essence slots used to enable the Combat Style. A high peak or a large Barrier number is not sufficient evidence of a strong build.

Test Conduit with a few early Essences as well as builds that can fill Charge. As more Essence slots become available, the Charge cap and the need to choose useful abilities must keep Channeled Essence strength from growing without limit. Bastion should have useful self-recovery without requiring a rare or specialized Essence combination.

## 7. Design playtests and unresolved tuning

The identities and choice structure above are the proposed design. The following playtests determine whether their numerical expressions need to change.

| Playtest                                                     | What it should establish                                                                                  |
| ------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| Same equipment and Essences, switch only the Combat Style            | Does the character's behavior change visibly, and can the player explain why?                             |
| Rebuild the loadout around each Combat Style afterward               | Does understanding the Combat Style lead to meaningful equipment and Essence changes?                             |
| Equal Combat Style levels and upgrade-slot budgets | Does each Combat Style remain competitive when progression advantages are controlled? |
| Switch from level-10 Bastion to level-0 Conduit and return | Is Conduit's core immediately useful, does its first specialization arrive promptly, and is Bastion's progress preserved? |
| Earn levels during offline combat and a committed dungeon run | Are XP ownership and milestone rewards clear, with new combat bonuses applied only at the permitted boundary and no automatic build choices? |
| Level-0 values versus level 10 and two upgrades | Do bonuses feel rewarding while Bastion still repairs Health slowly and Conduit still suffers from uncharged casts? |
| Three-slot introductory builds and later wider builds        | Can both Combat Styles work when unlocked, while later slots add options without overwhelming earlier builds?     |
| Short encounters, long encounters, and dangerous openings    | Are the different preparation costs meaningful without making one Combat Style unusable in ordinary play?         |
| Bastion under repeated burst damage                          | Is 25% Health restoration enough to recover between breaches, or does the base form become too punishing? |
| Bastion with Rebuild, Reprisal, and Shelter             | Does each refinement change the build's purpose? Is the base form still attractive?                       |
| Reprisal with self-generated and allied Barrier, rapid multi-hit Essences, and enemy damage over time | Does storage follow actual owned Barrier absorption, remain capped, and release only once per qualifying activation? |
| Bastion with allies, summons, and strong healing suppression | Does the source distinction remain understandable, and can organized recovery create excessive stalling?  |
| Conduit under ordinary automated ability activation          | Can the player predict preparation and payoff without editing a script or controlling casts manually?     |
| Conduit with uneven cooldowns and mixed-effect Essences      | Are Channeled Essence eligibility, weak casts, and unmodified effects clear enough to support informed choices?       |
| Conduit with each refinement                                 | Are there useful loadouts for the small circuit, long preparation, and repeat-cycle approaches?           |
| Builds with no Combat Style and builds with the wrong Combat Style           | Are Combat Styles a rewarding source of strategy while their opportunity costs remain real and visible?           |
| PvP and encounters with time limits                          | Do durable combinations remain beatable, and do burst combinations allow understandable counterplay?      |

The first tuning questions are Bastion's 25% / 75% split, Rebuild's threshold, Reprisal's storage rate and damage cap, and Conduit's zero-Charge penalty and Charge curves. Progression tuning covers the level-10 cap, XP thresholds, the benefit from every mastery level, time to the first refinement, per-level bonus magnitudes, and the three upgrade choices for each developed Combat Style. Adjust these while preserving the central decisions: **Bastion trades immediate repair for preparation; Conduit trades reliable Channeled Essence output for a stronger prepared cast.**

## Design references

Existing terms follow the [combat lexicon](../combat-lexicon/README.md), especially [Barrier](../combat-lexicon/conditions/barrier.md), [Guard](../combat-lexicon/conditions/guard.md), [Wound](../combat-lexicon/conditions/wound.md), [Recovery](../combat-lexicon/conditions/recovery.md), [Regeneration](../combat-lexicon/conditions/regeneration.md), [Lifesteal](../combat-lexicon/conditions/lifesteal.md), and [damage categories](../combat-lexicon/damage-categories.md). These references establish the existing vocabulary; they do not indicate that Combat Styles are currently implemented.
