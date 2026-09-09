# Shepherd

**Status: Full design proposal — not implemented. All numerical tuning is provisional.**

**Core mechanic:** Oathbond. **Identity:** Build mutual survival around one chosen companion.

Shepherd connects the character's defenses and recovery to one owned summon. The character protects a companion that can, in turn, carry part of the character's incoming pressure. Equipment still supplies attributes, and Essences still supply abilities, including the summon itself.

The style grants no summon, additional summon capacity, or manual combat action. Its decisions happen while building the character; the bond operates automatically during combat.

See the [Combat Styles overview](../README.md) for the shared structure. This proposal expands the [original Shepherd concept](../game-design.md#5-three-potential-combat-styles).

## Core mechanic: Oathbond — level 0

Choose one eligible equipped Essence and one summon type produced by that Essence as the **Companion source**. While the owner and the bound companion are alive, they can share qualifying incoming damage.

In the base form, **20% of a qualifying hit** is directed from its original recipient to their bonded partner. The original recipient retains the remaining amount. **Bond protection** reduces the redirected share as mastery level bonuses are earned.

The partner must begin the hit above **35% Health**. The amount diverted by one hit is capped at **10% of that partner's current Max Health**, measured before Bond protection and the partner's defenses. Any amount above this cap remains with the original recipient.

This threshold is an eligibility check, not a guaranteed minimum Health. Damage amplification or several successive hits can still defeat either combatant. The bond cannot intercept damage for a dead or absent partner.

### Damage calculation

Let **D** be qualifying damage after the original recipient's mitigation, Block and Guard, but before their Barrier. Let **M** be the partner's current Max Health, **S** the form's sharing fraction, and **L** the mastery level.

`Diverted damage = min(D × S, M × 0.10)`

`Original recipient's remaining damage = D − diverted damage`

`Partner's incoming share = diverted damage × (1 − 0.02 × L)`

The partner then applies the ordinary rules for redirected damage, including their applicable damage-taken modifiers, typed defense, damage reduction, Barrier and Health. Keep intermediate amounts precise and round at normal damage-resolution boundaries.

**Mastery level 0 example:** A hit reaches the owner with 200 damage after the owner's defenses. A living companion above 35% Health has 1,000 Max Health, so the cap is 100. Oathbond diverts 40, leaving **160 to the owner and 40 to the companion** before their respective Barrier pools and the companion's applicable defenses.

**Mastery level 10 example:** The same hit still diverts 40. Bond protection reduces the companion's incoming share to **32**, while the owner still receives 160. If the companion's applicable defenses reduce its share by a further 25%, it receives 24 before Barrier.

**Smaller companion example:** A partner with 200 Max Health can receive at most 20 diverted damage from that hit. The original recipient retains 180, and mastery level 10 reduces the partner's incoming share to 16 before its defenses.

## Companion selection and binding

- Save the owned Essence identity and the summon definition it produces. Selection follows that source, rather than an arbitrary combatant name or whichever summon has the highest attributes.
- An eligible source must be equipped and its prepared ability must be able to create the selected owned summon directly. An evolved Essence uses its currently prepared summon options.
- A source that produces several summon types presents those types as separate choices. Choose one type; other types remain ordinary summons.
- Summons created by another player, an enemy, or another summon cannot become this character's companion. An allied boss or other combatant without this ownership and source identity is also ineligible.
- If several eligible copies appear together, bind the first created in ordinary summon resolution order, with stable combatant identity breaking a tie. Keep that instance until it is gone; do not switch according to changing Health or attributes.
- A capped summoning cast retains the game's ordinary behavior. It does not replace the companion, refresh its resources, or count as a new bond when no new instance appears.
- If the companion is defeated, expires, is consumed, or is legitimately replaced by its ability, end that instance's bond immediately. After the current effect finishes, bind the oldest remaining living eligible copy; otherwise wait for the next eligible summon.
- An instance created by a later normal summon cast can establish the bond again. Shepherd does not resurrect it, shorten its summoning cooldown, extend its duration, or transfer the previous companion's resources.
- A newly bound companion receives only the effects explicitly listed here. It does not inherit the owner's Combat Style, mastery level bonuses, upgrades, equipment, or attributes as independent bonuses.

The owner must remain alive for binding, sharing, and every Shepherd benefit. Owner defeat immediately disables the bond and follows the game's ordinary owned-summon expiry rules. A later valid revival can bind an eligible living companion again, without resetting battle-limited effects or cooldowns.

If the saved source becomes unequipped or its evolution no longer produces the selected summon, the configuration needs a valid Companion source before a new Shepherd battle starts. Temporary absence during a valid battle is ordinary: the character continues fighting and waits for the source's normal summoning behavior.

## Qualifying damage and shared defenses

Oathbond considers direct enemy attacks, including enemy basic attacks and direct attacks by enemy summons, against either member of the pair. Each hit of a multi-hit attack is evaluated separately with the resources present before that hit.

Periodic damage, reflected damage, self-inflicted damage, resource expenditure, and an already redirected share do not start another Oathbond transfer. A missed or fully prevented attack has no amount to share.

The proposal uses the game's existing **Cover** and **redirected damage** vocabulary. Existing Cover has priority: when an active Cover applies to an incoming event, Shepherd does not add another redirect from that event, including when Cover's remaining budget is small. A recipient's Cover never forwards the received share.

The original attack makes its ordinary hit, critical, Block and Guard checks once. The redirected share keeps the enemy source and damage type, skips source damage amplification a second time, and receives the partner's applicable defenses. It does not roll another critical, dodge, or Block, consume another Guard or Vulnerable charge, or initiate another attack.

The share cannot trigger another Oathbond, Cover, on-hit copy, Lifesteal payout, or reflection loop. Ordinary Barrier absorption and depletion, Health loss, and death still resolve for the recipient. The attack's original on-hit effects remain attached to its original target.

These rules deliberately give the redirected recipient its existing mitigation stage, as current Cover does. Bond protection is an additional proposed reduction before that stage. It is not a second application of the owner's defenses and does not increase the sharing fraction.

There is one bond per owner and one recipient per diverted share. Another Shepherd cannot bind this owner's summon, and group effects cannot join several bonds into a protection chain. An attack hitting both owner and companion resolves two original hits normally; each may share once if its partner remains eligible.

Oathbond grants no healing. Redirecting damage never counts as Health restored, and its protective reductions do not create Barrier by themselves. The two combatants retain independent Health, Barrier, and ordinary Barrier caps.

## Mastery level bonuses

Every mastery level adds **2 percentage points of Bond protection**. It reduces what the partner receives from the diverted share, without changing the amount removed from the original recipient. Level 0 starts with no Bond protection; level 10 supplies 20%.

| Mastery level | Bond protection | Partner receives from a 40-point diverted share |
| ---: | ---: | ---: |
| 0 | 0% | 40 |
| 1 | 2% | 39.2 |
| 2 | 4% | 38.4 |
| 3 | 6% | 37.6 |
| 4 | 8% | 36.8 |
| 5 | 10% | 36 |
| 6 | 12% | 35.2 |
| 7 | 14% | 34.4 |
| 8 | 16% | 33.6 |
| 9 | 18% | 32.8 |
| 10 | 20% | 32 |

The table isolates mastery levels before the partner's defenses and any conditional upgrade. The formula is `diverted share × (1 − Bond protection)`. Bonuses add; they do not compound.

All forms use the same level scaling. It does not alter summon damage, the receiving-Health threshold, the per-hit sharing cap, healing amounts, or Shepherd's separately granted Barrier.

## Refinements — level 3

Choose exactly one of these three refinements, or retain the base form. Each changes who carries the pair's incoming pressure; the full core and existing safety boundaries still apply.

| Form | Behavior | Example before Bond protection |
| --- | --- | --- |
| **Base form** | Share 20% in either direction whenever the receiving partner is eligible. | A qualifying 200-point hit diverts 40. |
| **Stand Behind Me** | Share only damage aimed at the companion, directing **30%** to its owner. | A 200-point hit on the companion diverts 60 to the owner. |
| **Faithful Guardian** | Share only damage aimed at the owner, directing **30%** to the companion. | A 200-point hit on the owner diverts 60 to the companion. |
| **Balance the Burden** | Share **30%** only when the original recipient begins the hit at a strictly lower Health percentage than its partner. | At 45% owner Health and 80% companion Health, an owner-directed 200-point hit diverts 60. |

The examples assume the partner's per-hit cap is large enough. Each form uses the same 10% receiving-Max-Health cap and above-35%-Health eligibility check.

Stand Behind Me supports an owner built to carry pressure for a valuable companion. Faithful Guardian lets a sturdy companion support the owner's survival. Balance the Burden shifts the direction automatically as their Health changes; equal percentages leave that hit with its original recipient.

Evaluate percentages once before each hit, after any preceding hit has finished. Sharing does not continuously rebalance Health, move Barrier between combatants, or reconsider the direction halfway through the hit.

## Upgrades — slots at levels 5 and 8

All three upgrades become available at level 5. Equip one then, and up to two different upgrades after level 8. The base form and every refinement can use each upgrade.

### Steady Bond

Add **5 percentage points of Bond protection** when the receiving partner begins the hit with any Barrier. This adds to mastery level bonuses: at mastery level 10, an eligible redirected share receives 25% Bond protection.

For example, a diverted 40 becomes **30** before the partner's defenses. Barrier is checked before the share is applied; having enough Barrier to absorb the whole share is unnecessary.

### Shared Provisions

When the owner's normal Essence active cast directly restores the owner's Health, grant the living bound companion Barrier equal to **20% of the Health actually restored**. The total grant from one cast is capped at **3% of the owner's current Max Health**.

A cast restoring 100 Health grants 20 companion Barrier. With 1,000 owner Max Health, several healing components in the same cast can grant at most 30 in total. Count only healing received while that companion is bound, and share one cast budget across all its components and any replacement companion.

The owner's ordinary healing resolves once. Unused healing, periodic healing, Lifesteal, regeneration, triggered copies and outside allied healing do not contribute. The granted Barrier uses the companion's ordinary cap, cannot trigger healing or Barrier-gain reactions, and receives no further healing, Barrier-power, or mastery-level multiplier.

### Reunion

Establishing a bond grants the owner Barrier equal to **2% of the owner's current Max Health**. This can occur on the first binding and after a genuine companion change, at most once every **20 seconds** of battle time.

The cooldown belongs to the owner and continues through companion absence, death, replacement, and owner revival. A binding during the cooldown skips the grant; it does not queue one. Keeping an existing bond through another summon cast does not count as binding again. An eligible activation consumes its cooldown even if the owner's Barrier cap rejects the grant.

## Opening Technique — level 7

**Together at Dawn:** At battle start, grant the owner Barrier equal to **5% of the owner's Max Health**. Also grant the first eligible companion bound in that battle Barrier equal to **5% of that companion's Max Health**.

The owner portion activates automatically before the first combat action. If the companion is initially absent, its portion waits for the first valid binding while the owner is alive; it expires at battle end if never used. A companion present at the opening receives its portion then.

Each portion is granted at most once per battle. A replacement, later wave, or revival cannot repeat it. This technique works with every form, uses no upgrade slot, and has no extra choice or active button. It is independent of Reunion and mastery level bonuses.

## Upgrade Mastery — level 9

Optionally empower **one equipped upgrade**, retaining its ordinary effect and existing slot. Switching the empowered upgrade is a saved build choice. Removing it clears the mastery choice.

| Empowered upgrade | Additional behavior |
| --- | --- |
| **Steady Bond** | Its 5-point bonus also activates when the receiving partner begins the hit at or above **80% Health**. Barrier and Health together still grant the bonus only once. |
| **Shared Provisions** | Direct healing of the owner by the **bound companion's own active ability** also qualifies for the ordinary 20% Barrier grant to that companion. Use the same 3%-owner-Max-Health cap per originating cast. |
| **Reunion** | A successful Reunion activation also grants its bound companion Barrier equal to **2% of that companion's current Max Health**. Both grants share the owner's existing 20-second cooldown. |

Shared Provisions mastery still requires actual owner Health restoration. It does not copy that healing, heal the companion, or turn periodic, passive, triggered, or another summon's healing into qualifying recovery. Its Barrier output cannot start another provision grant.

Reunion mastery never creates a second cooldown or an independent companion activation. A binding skipped because of the cooldown also skips the companion grant. On an eligible activation, the two grants resolve independently, so a full owner Barrier pool does not prevent the companion's grant. All Shepherd Barrier grants follow each recipient's ordinary cap, discard overflow, and do not feed Barrier-gain loops.

## Progression and saving

| Mastery level | Reward |
| ---: | --- |
| 0 | Full Oathbond and Companion source selection; 0% Bond protection. |
| 1 | 2% Bond protection.  |
| 2 | 4% Bond protection.  |
| 3 | 6% Bond protection. Refinement choice. |
| 4 | 8% Bond protection.  |
| 5 | 10% Bond protection. First upgrade slot; all three upgrade choices available. |
| 6 | 12% Bond protection.  |
| 7 | 14% Bond protection. Together at Dawn. |
| 8 | 16% Bond protection. Second upgrade slot. |
| 9 | 18% Bond protection. Empower one equipped upgrade. |
| 10 | 20% Bond protection. Maximum Shepherd level. |

Shepherd would use the shared individual style-XP rules. Its level, source selection, refinement, upgrades and mastery are remembered independently. Equipping it applies one saved configuration to every battle; there is no separate selection for each activity.

Ordinary idle combat adopts saved changes at the next encounter. Committed dungeon runs, matches and boss battles retain their captured source identity and rules until their normal boundary. A multi-wave battle keeps binding history, opening use, and Reunion cooldown between its waves.

Encounter-start summons use the same source matching as summons created later. A boss encounter that removes a partner from combat or explicitly prevents that partner from receiving damage also prevents routing damage into that partner; the hit stays with its original recipient. Oathbond cannot use an absent or encounter-protected summon as a damage sink. Ordinary defenses still apply normally to an eligible partner. Group bosses can supply heavy pressure, but other players' summons and protective links never become part of this bond.

## Build directions

- **Companion keeper:** Stand Behind Me, owner mitigation and recovery, and Shared Provisions support a companion whose continued presence matters to the build.
- **Trusted guardian:** Faithful Guardian with a durable companion and Steady Bond supports an owner focused on another role while both continue taking ordinary damage.
- **Balanced pair:** Balance the Burden with recovery for both combatants shifts protection as pressure moves between them.
- **Returning companion:** Reunion and its mastery support a summon whose ordinary duration or encounter role leads to legitimate replacement; reliable long-lived companions can choose the other upgrades.

The desired payoff is a different relationship between the owner's build and one summon. Summon count and raw summon damage should not become the universal answer to Shepherd progression.

## Player-facing information

Show the selected source and summon type, the bound companion when one exists, the current form's sharing direction, and Bond protection at the current mastery level. A compact worked hit should separate the original recipient's remaining damage from the partner's incoming share.

Use the normal Health and Barrier displays for both combatants. Describe opening and upgrade grants in their cards. The page should make temporary companion absence understandable without requiring a manual battle action or adding a separate build system.

## Design playtests

- Compare the base form and all three refinements with a sturdy companion, a fragile companion, and a short-duration companion at mastery levels 0 and 10.
- Test threshold boundaries at 35% and 80% Health, equal Health percentages, very small summons, large boss hits, and attacks hitting both members.
- Verify one redirect per hit with Cover, periodic damage, reflection, multiple Shepherds, multiple summon copies, and chains of triggered effects present.
- Check that owner defeat, summon consumption, expiry, replacement and revival preserve deterministic binding and never refresh opening grants or cooldowns.
- Compare long fights and repeated short encounters: opening protection and legitimate Reunion activations should remain useful without making deliberate summon cycling dominant.
- Test Shared Provisions with multi-component heals, overhealing, healing suppression, companion healing, Barrier caps and reaction-heavy builds; record actual restoration and grants separately.
- Check that both mastery level gains and refinement choices improve a clear build purpose, while healing-heavy pairs cannot stall indefinitely through protection and capped recovery.

## Design references

The [existing combat engine](../../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) supplies the current Cover, redirected-damage, summon-cap, ownership and owner-expiry conventions used as reference. The [current Combat Styles rules](../../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs) and [shared progression](../../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs) establish the surrounding configuration structure.

Oathbond, its source binding, all Shepherd refinements and upgrades, Together at Dawn, and their exact interactions above are proposed rules. None is supplied by the current Bastion/Conduit implementation. Playtests should settle the sharing fractions, receiver threshold, per-hit cap, protection increments and grant cooldown before implementation.
