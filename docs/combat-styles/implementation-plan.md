# Combat Styles implementation plan

8 September 2026. Gameplay authority: [Combat Styles game design](game-design.md). Delivery evidence: [implementation status](implementation-status.md).

## Scope

The primary `LL` game service owns this feature: Domain, Application, Infrastructure, `API.LL`, and the Angular game client in `LL/src/Presentation/ll`. Chat, the admin application, and infrastructure-as-code are outside its scope.

Implement Bastion and Conduit with separate levels, one defining mechanic, three mutually exclusive refinements, and three upgrades each. Reaper, Shepherd, and Gambler remain design concepts.

The selected Combat Style is **global for every battle type**. There is no Combat Style activity loadout, practice introduction, unlock quest, or combined saved-build system. Existing equipment and Essence loadouts retain their independent behavior.

## Rules

- A character has one optional global selection. Leaving it empty disables style benefits and penalties for every new battle.
- Both styles are available at level 0 without an introduction, character-level gate, or Essence-count gate.
- Each style retains its own XP, level, refinement, upgrades, and mastered upgrade. Conduit's Channeled Essence comes from the first occupied slot of the Essence loadout used for each battle. Switching never copies progression between styles.
- Level 0 provides the full core mechanic. Every mastery level adds 1% of the base converted Barrier for Bastion or a flat +1% to charged Channeled Essence effects for Conduit; level 10 retains +10% of base converted Barrier and a +10% flat increase to charged Channeled Essence strength. Flat bonuses add directly to the displayed percentage; a multiplier such as ×1.2 scales the amount it applies to. Zero-Charge Channeled Essence output is unchanged. Refinements unlock at level 3, upgrade slots at levels 5 and 8, Opening Technique at level 7, and Upgrade Mastery at level 9. There is no separate Core Rank system.
- Only the style captured for an eligible rewarded encounter receives XP. Noncombat quest XP does not train styles.
- Selection changes are free but obey existing encounter/committed-run boundaries. They cannot rewrite completed combat, heal, reset cooldowns, or transfer Charge or Barrier between encounters.
- Styles modify existing abilities without changing shared ability definitions or adding equipment attributes or Essence slots.

## Domain and persistence

| Model | Responsibility |
| --- | --- |
| `CombatStyleDefinition` and versioned catalog | Authoritative descriptions, choices, tuning, and XP schedule. |
| `CharacterCombatStyle` | Per-character, per-style level, XP, and remembered choices. Absence of a row represents the available level-0 base form. |
| `CharacterCombatStyleSelection` | One row keyed only by `CharacterId`, containing the optional style ID, refinement, upgrades, and mastered upgrade. No activity field or override precedence. Legacy stored Channeled Essence columns are ignored for current choices. |
| `CombatStyleSnapshot` | Immutable identity, content version, resolved tuning, mastery level, choices, and Channeled Essence captured for combat. |

Read-only overview and preview operations must not create progression rows. Selection and XP commands persist required progress inside the established character transaction. Repository caches follow tracking generations so clearing tracked entities cannot cause updates to stale objects.

The selection repository exposes one nullable global selection. Combat setup must use that same row for idle combat, dungeons, raids, World Tower, arena, tournaments, and region bosses. There is no fallback to an old per-activity style.

## Conduit Channeled Essence

Conduit automatically uses the first occupied slot in the Essence loadout selected for each battle. Select by `SlotIndex`, skipping only empty slots. Honor the existing per-activity Essence loadout assignment and archive-order fallback. For supplied captured Essences, use their already ordered slot sequence. The global Combat Style editor neither loads nor validates an arbitrary default Essence loadout; saving and numeric previews are independent of Channeled Essence eligibility.

A shared Channeled Essence resolver checks the same prepared current ability, including evolution, used in combat. Return `IsChanneledEssenceEligible` on owned Essence DTOs so the Essence page can identify unsuitable first slots without duplicating combat rules in the client. While Conduit is equipped, mark the first occupied slot with a Channeled Essence badge. Offer **Channel Essence** on another eligible equipped Essence: swap it with the first occupied slot, preserve every other Essence, save once through the existing loadout flow, and restore the saved arrangement on failure.

At combat preparation, require the first Essence to have direct damage, healing, or Barrier on its active cast. Reject an empty or unsuitable first slot with a clear instruction to place a suitable Essence first; never skip to a later eligible Essence. Capture that Essence's owned identity and definition in the immutable battle snapshot. Existing committed snapshots retain their original Channeled Essence, even if it was selected under the old policy. Slot position does not introduce a casting sequence.

Remove manual Channeled Essence fields from current API selection/entry contracts and remove the Channeled Essence options list and dropdown. Ignore stale client or persisted Channeled Essence IDs, clear legacy choice columns on normal authorized saves, and leave historical migrations and captured snapshot fields intact. No schema migration is needed. Catalog version `combat-styles.v5` introduced the first-slot policy; version `combat-styles.v6` names the mechanic Channeled Essence. All numeric tuning is unchanged.

Use **Creature Focus** for the Creatures-tab feature and **Channeled Essence** for Conduit throughout current UI, API contracts, services, tests, and guides. The loadout badge reads **Channeled** and its action reads **Channel Essence**. Keep historical database column names, durable quest/event identities, and immutable battle JSON members as explicit compatibility boundaries. Current Conduit DTOs expose `channeled*` fields independently of historical snapshot serialization. Old captured content versions retain their original combat-log text for deterministic replay; new captures use the new name.

## Application and API

Keep the authenticated, character-scoped CQRS operations for:

- Reading the global overview.
- Previewing a proposed global selection.
- Saving a global selection.
- Awarding captured style XP through existing reward transactions.

Selection requests contain `CombatStyleId`, `RefinementId`, `UpgradeIds`, optional `MasteredUpgradeId`, and optional remembered-choice restoration. A non-null mastery requires the style's level to be at least 9 and the upgrade to be equipped. Overview and request contracts have no manual Channeled Essence, activity, or introduction fields. Entries expose progression and choices without an unlock entitlement.

Definitions expose Opening Technique metadata, mastery descriptions on upgrades, and separately resolved milestone tuning. Capture this tuning and the chosen mastery in `CombatStyleSnapshot` so later catalog changes cannot alter committed battles. Fields absent from old snapshot JSON default to zero/false/null; old battles receive no new opening or mastery effects.

Style definitions contain descriptions and tuning without a separate `Tradeoff` display field. Remove Cost UI, negative-only preview notes, and corresponding help/tour callouts. Numerical descriptions and actual combat tuning remain unchanged by this presentation cleanup.

Remove all practice, introduction-completion, and combined preset commands, queries, DTOs, mapping profiles, controllers, services, registrations, and synchronization scopes. The retained `combat-styles` scope invalidates the page and character overview after relevant selection or progression changes.

## Combat and rewards

Live combat preparation and character snapshot creation share global style resolution. The activity supplied to preparation remains relevant only to the game's independent equipment/Essence selection and battle rules.

Existing committed snapshots remain immutable, including legacy snapshots with no style. Their replays and pending rewards must continue to describe the combat that actually occurred. Newly captured battles use the current global selection; pending idle work settles under its previous selection before a change is saved.

Preserve the existing engine mechanics:

- Bastion conversion of all combat healing received before missing-Health/cap limits, recipient-owned rules, reaction exclusions, all refinements, and conditional upgrades. Preserve existing healing target selection, priorities and authored cast conditions; Barrier does not influence targeting.
- Conduit normal-cast provenance, contributor tracking, charge curves, Channeled Essence validation, and direct-component restrictions.
- Encounter-local state, continuous-wave behavior, deterministic checkpoints, and internal balance diagnostics.

At the beginning of each encounter, level-7 Bastion gains Barrier equal to 5% maximum Health and level-7 Conduit gains 1 Charge, with normal caps. Apply openings once before the first action, after checkpoint/stat tracking is initialized. New waves in a continuous battle do not reapply an existing combatant's opening, and summons do not inherit one. Barrier grants must not emit recursive Barrier-gain reactions.

Implement all six mastery enhancements described in the game design with one equipped selection. Sample zero-Barrier/Health/Charge conditions before the affected cast or recovery, apply each bonus once, and retain Channeled Essence component restrictions and Shelter recipient caps. Use the same milestone tuning for previews and runtime. The offline harness accepts `MasteredUpgradeId`, freezes both additions, and rejects invalid or tampered recipes.

Idle style XP uses each recipient's outcome-eligible unbonused combat XP share. It advances between encounters, including offline batches. Dungeon rewards retain captured style identity and eligible base XP through pending, secured, and claimed rewards. Existing durable claim/settlement boundaries prevent duplicate training.

Modes that award no ordinary eligible combat XP do not invent a separate style reward. They still apply the same global style mechanics.

## Reprisal replaces Counterweight

Content version `combat-styles.v4` introduced the current Bastion refinement ID `reprisal`; versions v5 and v6 retain it. Retain Fortification's normal conversion and all existing progression, upgrade, Opening Technique, and Upgrade Mastery rules.

Store 25% of actual enemy damage absorbed by the character's own Barrier, capped at 10% of Max Health. Barrier may come from any source; another combatant's absorption does not contribute. Reserve existing stored damage at the next normally cast damaging Essence's cast start; its first direct enemy attack attempt consumes it once, including a miss or dodge. New absorption during the cast remains banked for a later cast. Return an unused reservation if no eligible attack occurs, subject to the cap, and recheck the cap before release if Max Health has fallen. The contribution uses the attempt's damage type and target mitigation without further outgoing amplification, remains noncritical, and does not generate Lifesteal or extra damage-derived reactions. Reprisal never spends Barrier.

Keep stored damage in the combatant's encounter-local runtime state. Hostile direct, periodic, and reflected damage can contribute; self-inflicted and allied damage cannot. The reservation counts toward the shared cap, only whole bonus damage is consumed, and fractions remain stored. Continuous waves and revival within the same battle retain the bank; each new battle starts empty. No extra active ability, button, equipment attribute, or persistent progression field is needed. Existing damage and Barrier reporting remains the player-facing combat summary.

Normalize former `counterweight` references to `reprisal` when resolving current global selections, remembered choices, previews, and new saves or captures. A read may expose the normalized choice without writing a progression row. Historical committed snapshots retain their explicit Counterweight ID and captured spending behavior; compatibility must not silently replace their mechanics or alter replay hashes. This transition requires no schema migration or XP reset.

The existing refinement cards render catalog metadata, and the current preview-fact list renders the Reprisal rule and example. Show the authored percentages as **25% of absorbed damage stored · cap 10% Max Health**, with **200 absorbed → 50 bonus damage** assuming 1,000 Max Health. Preserve the shared page layout, draft/save flow, and live XP updates.

Verify ownership and damage boundaries, storage below/at/above the cap, first-attempt release, misses, prevention, multi-hit and multi-target casts, passive/repeated-cast exclusion, seeded replay, and continuous waves. Cover current-selection aliases separately from frozen legacy behavior.

## Frontend

Preserve the approved layout and existing game design system:

- Shared header with mastery level, XP, current level bonus, slots, Save/Discard, supplied Combat Styles icon, and shared navigation tabs aligned with the panels.
- Core mechanic beside Milestones on wide content; stacked panels when the available space is narrow.
- Refinement cards and upgrade slots/options below.
- Shared panels, cards, subtle borders, neutral copy, and accent selections used by the other character pages.

Conduit's mechanic panel explains that the first Essence in the battle loadout is the Channeled Essence. The separate dropdown is removed; loadout arrangement and its Channeled badge live on the Essence page. Keep preview loading/errors, unsaved changes, and the help guide; the manual Page tour and Refresh buttons are removed. Level/XP invalidations refresh live, including updates arriving during an in-flight request, while preserving draft choices. The milestone timeline identifies Opening Technique at 7 and Upgrade Mastery at 9. Show the technique's current unlock state and one mastery selector tied to the equipped upgrades; removing its upgrade clears the draft mastery. The overview widget describes the globally selected style and provides a direct link to the editor.

Do not show a committed-battle warning in the editor or character overview. Overview and preview responses have no committed-activity/style display fields and perform no activity/snapshot lookups for such a banner. Existing server-side mutation checks and frozen battle snapshots still enforce their normal boundaries.

The combat summary page has no Combat Style display. Remove its component, client model/state, API response fields, and playback-frame/checkpoint copies across all battle modes. Ordinary combat statistics and logs remain available. Final engine diagnostics used by the offline Balance Harness remain internal; they are not included in player-facing result or replay contracts.

## Schema transition

Preserve historical migrations because an environment may already contain the previous schema. Add a forward migration that:

1. Retains former default (`Activity = None`) style selections as the global selections.
2. Deletes former per-activity style override rows before removing `Activity` and changing the selection primary key to `CharacterId`. Do not promote an arbitrary activity override if a default was never selected.
3. Preserves earned `CharacterCombatStyles` progression and frozen snapshot JSON.
4. Removes combined preset tables and practice/introduction columns.
5. Removes the preset-only explicit Essence default flag/index; the independent Essence loadout resolver returns to its established activity assignment and archive-order fallback.

Generate and inspect migration SQL without applying it to any database. Removed preset/override data cannot be reconstructed by a downgrade; the downgrade can restore schema shape only. API, worker, client, and schema contracts require a coordinated later release.

For Upgrade Mastery, add a separate forward migration with nullable `MasteredUpgradeId` columns on the global selection and per-style remembered choices. Existing progression and selection rows remain intact and initially have no mastery choice. Opening Technique is derived from the captured level and catalog tuning and requires no stored unlock flag.

## Continuous mastery-level scaling

Use catalog version `combat-styles.v6` for new captures, retaining the continuous mastery scaling introduced in v3. Author the per-level Barrier bonus for Bastion as 0.01 and the per-level charged Channeled Essence bonus for Conduit as 0.01. Apply the captured mastery level using the resolved tuning for the selected refinement, then add qualifying upgrade and Upgrade Mastery bonuses. Return level-based bonuses in previews and remove Core Rank counters, pips and unlock labels from current UI and API contracts.

Historical committed snapshots retain their original rank-scaled tuning through the compatibility fallback; replay must not silently retune them. New snapshots resolve level scaling from versioned content. This change requires no schema migration or earned-XP reset.

Verify every level from 0 through 10, especially formerly unchanged odd levels; preserve level-10 output, zero-Charge amounts and milestone gates. Historical fixture pairs at levels 6/7 and 8/9 now include the extra per-level bonus as well as their opening or mastery difference.

## Verification

Run backend tests through `build/run-tests.ps1`. Verify:

- Both styles preview and select at level 0 without any unlock state; reads do not persist training.
- One global style/refinement/upgrades selection applies across every battle activity; Conduit derives its Channeled Essence from each battle's first occupied Essence slot. An empty selection disables styles globally.
- Unsorted stored slots and empty gaps resolve in visible slot order. Different activity assignments and captured loadouts can supply different Channeled Essence identities. An ineligible first Essence is not skipped, and stale client or stored Channeled Essence IDs cannot override the first slot.
- Existing XP, remembered choices, milestone restrictions, Channeled Essence eligibility, and tracking-generation behavior remain correct.
- Live preparation, snapshots, PvP participants, idle rewards, dungeon claims, and legacy replays preserve their boundaries.
- Practice and combined-preset code, endpoints, registrations, DTOs, and current schema entities are absent.
- The new migration retains only the intended former default row, preserves progression/snapshots, and matches the final EF model.
- Level 6/7 opening gates, level 8/9 mastery gates, all six empowered effects, no double procs or repeated wave openings, legacy snapshot defaults, and mastery save/restore/clear behavior.

Run focused frontend tests, generated state-scope checks, and the development build with npm. Cover Channeled badges, unsuitable first slots, empty and holey loadouts, atomic Channel Essence swaps, loadout switching, and failed-save rollback. Inspect the global editor and Essence loadout controls when a compatible local backend/schema is available. Report separately when a live migration or authenticated integration check has not been performed.
