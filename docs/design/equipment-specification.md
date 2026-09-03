# Equipment & Forge: Design and Implementation Requirements

**Updated:** 3 September 2026

**Architecture:** content-earned equipment, deterministic ranks, and reusable Blueprint styles.

**Naming and cleanup update:** Equipment and Forge are the current player equipment path. Alpha data does not need preserving. Response/route/configuration aliases, cohort policies, conversion adapters and refunds are removed. Stable identifiers and physical names still used by current equipment remain; see the [storage notes](../engineering/equipment-naming-and-compatibility.md).

**Document status:** current implementation specification. The Shenic / Tier 1 and Meran / Tier 2 loops and Alpha cleanup are implemented in the working tree. See [Meran content and transition validation](equipment-region-two-progression.md) and the completed first [Meran PvE/economy assessment](equipment-meran-pve-balance-report.md). Complete dungeon runs, broader Essence/counter-build progression and authenticated acceptance remain open. Enabled defaults do not establish deployed or full-game readiness.

**Owner update, 2 September 2026:** gear-selling merchants are excluded from the current scope. Starter rewards, recoverable baseline quest equipment, and targeted content guarantees provide ordinary equipment access. Existing unrelated vendors are not removed by this decision. The [Region 1 progression proposal](equipment-region-one-progression.md) and [reproducible balance report](equipment-region-one-balance-report.md) begin Phase 1; their numerical proposals are not approved runtime balance.

**Implementation order:** build the complete loop with provisional configurable values, then balance content. Starter grants, Forge ranks/styles/learning/salvage, protected dungeon rewards, baseline and earned-target recovery, trade/guild equipment, ordinary drops/Scrap/sigil progress and current quest integration are implemented. Crafting/gathering, queued tempering, obsolete profession content and Alpha compatibility paths are removed. The five equipment capabilities default to enabled; there is no separate quest-integration flag. Later-region/tier coverage and whole-loop combat/economy acceptance remain. Raid redesign and further LiveOps work are deferred. See the [implementation status](equipment-implementation-status.md) and [cleanup record](equipment-post-alpha-cleanup.md) for exact scope, generated migrations and verification. The task has not applied game-database migrations or deployed changes.

This document defines the selected equipment model, the requirements for implementing it, and the systems that must be retired. It follows the [equipment design review](equipment-gathering-crafting-review.md), which remains the evidence and architecture-comparison record. Requirements below describe the intended complete behavior; the implementation-status document distinguishes completed backend support from pending integration and cutover work.

**The selected loop is: earn equipment through content → equip it → improve it deterministically → specialize it with reusable Blueprints → retain it through a meaningful progression band → replace it when the build or content warrants it.**

The architecture is decided. Drop rates, exact prices, and stat shares identified as **balance candidates** still require simulation and content authoring. Operational defaults below resolve details needed to make the architecture implementable; they should be reviewed as part of the implementation plan, rather than mistaken for previously shipped behavior.

## Part I — Game design

### 1. Design contract

Equipment progression has these defining rules:

1. Equipment is primarily earned from combat and content. Drops and rewards arrive as usable equipment.
2. Ordinary equipment access has a deterministic floor: starter rewards, recoverable baseline quest equipment, and targeted content guarantees.
3. Equipment archetypes, slots, weapon/offhand configurations, armor roles, and meaningful set interactions remain.
4. Tier represents a content progression band. It is not another random roll on a drop.
5. Tempering has ranks 0–5. Every ordinary combat item can reach rank 5 through predictable investment; rarity does not change that cap.
6. Tempering spends existing Cinders and Tempered Scrap, succeeds deterministically, and does not occupy the character's idle action.
7. A Blueprint teaches one reusable style across compatible equipment on the character. An item has at most one active style.
8. Quality, Potential, item rarity XP, randomized production stat variance, and destructive Tempering outcomes disappear from the new equipment model.
9. Gathering professions, gathering tools, routine equipment production, the Crafting profession, and recipe mastery are retired.
10. Essences continue to own the main ability/long-term build collection role. Equipment supplies the statistical and physical foundation, with a limited specialization layer.
11. Trading supports discoveries, Blueprint copies, and investment materials. Personal use binds equipment; the design does not preserve a finished-item manufacturing profession.
12. No repair chores, new profession ladders, ordinary Tier ascension, random affix casino, or additional equipment currency is introduced to replace the removed systems.

The initial implementation is for the existing game and authored regions. It must scale through data to later regions, but it does not include authoring eight additional regions, a new raid system, or unrelated Essence redesign.

### 2. What an equipment item means

| Concept           | New meaning                                                                          | Player-facing consequence                                                            |
| ----------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------ |
| Archetype         | Authored slot, armor/weapon role, behavior, compatible styles, and stat distribution | A mace, shield, staff, or cloth chest remains a deliberate build choice              |
| Tier              | Content/equipment progression band with a clear equip requirement                    | Players know when and where the next band becomes available                          |
| Rarity            | Authored identity category: Common, Rare, or Legendary                               | Describes ordinary, named, or aspirational identity; never a hidden power multiplier |
| Tempering rank    | Predictable investment, 0 through 5                                                  | Shows how much the item has been improved and its remaining investment path          |
| Native style      | The style an authored reward originally carries, if any                              | A named find can support its intended build immediately                              |
| Active style      | The one specialization currently applied                                             | Determines the style stat distribution and any corresponding set membership          |
| Provenance        | Original source, award type, and item identity                                       | Preserves a named item's story and determines economic rules                         |
| Binding/ownership | Unbound, personally bound, or guild-owned                                            | Determines whether it can be sold, transferred, donated, or borrowed                 |

Quality and Potential have no replacements. An ordinary item is not secretly defective because it rolled low capacity. Rank 0 is usable, including a native style if the source provides one.

Common, Rare, and Legendary are **equipment-specific categories**. The current shared rarity enum is used outside ordinary combat equipment; this decision does not authorize renumbering it or collapsing Essence/resource rarity.

The same archetype, tier, active style, and rank must produce the same combat stats under the same balance version, regardless of acquisition route. Provenance, display identity, binding, and salvage entitlement may differ. Set effects must also follow the active style rather than grant an extra source-specific power bonus.

#### Stat and set design

Use the existing slot budgets, stat units, caps, and weapon behavior model as the basis of the new evaluator. Remove the dependency on an item having been crafted to receive the correct normalized stats.

The initial **balance candidate** is:

- Unstyled item: allocate the full stat budget through the archetype profile.
- Styled item: allocate 85% through the archetype and 15% through the style.
- Each Tempering rank adds 4% of the tier/slot baseline budget, to a maximum of +20% at rank 5, allocated through that same distribution.
- A style reallocates budget; it does not append the existing Blueprint bonus budget on top.
- Existing set bonuses receive a separate explicit allowance in full-loadout balance. Their attributes and granted abilities are not free power outside the review.

These percentages describe stat budget, not guaranteed DPS or survivability gains. A constrained evaluator must preview the actual attribute changes. When caps are reached, use authored redistribution or reject an invalid definition; never charge for a supposedly improving rank that has no meaningful effect.

Two-handed versus one-handed/offhand choices must retain comparable combined hand budgets. A style change must not bypass hand restrictions, change a base's slot, or stack its old set with its new set.

### 3. Equipment acquisition and content rewards

#### Content ownership

| Source                                        | Equipment responsibility                                                                                                   |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Normal idle combat                            | Ordinary Scrap income and occasional plain regional equipment; existing Essence/access rewards remain                      |
| Strong regional encounters or authored elites | Better access to a small local equipment pool where supported by existing content                                          |
| Area/region bosses                            | Recognizable named items or a meaningful first-clear choice, with explicit repeat reward eligibility                       |
| Dungeons                                      | Main targeted source for named archetypes and Blueprint styles; retain their Essence/Core roles                            |
| World Tower                                   | Existing progression/Tokens, selected equipment/style milestones, and bounded optional investment rewards                  |
| Raids                                         | Aspirational identities/styles and deterministic purchases using existing raid trophies                                    |
| PvP                                           | Prestige and optional equivalent resource opportunities; no exclusive mandatory PvE equipment cap                          |
| Guilds                                        | Shared content, selective rewards, and equipment lending; no compulsory production quotas                                  |
| Quests                                        | Starter functionality and selected milestone rewards; teach targeting and investment                                       |
| Achievements                                  | Recognition, cosmetics, and occasional utility; no compulsory equipment-stat checklist                                     |
| Prophecies                                    | Supplementary progression/resources through ordinary play; no requirement to craft junk or repeatedly modify finished gear |
| Baseline quest rewards                        | Plain, personally bound, rank-0 equipment, with a recovery route after the relevant progression requirement                 |

No source gets every named item and every style. A source catalog must say what belongs there, why it belongs there, and which progression requirements apply. A scheduled boss, raid, Tower milestone, guild, or PvP activity cannot be the sole ingredient source for ordinary Tempering ranks.

Starter and baseline quest rewards must cover all basic combat slots and viable hand configurations. A starter player must be able to fight the content that awards better equipment without first obtaining that content's own rare rewards. Recovering basic equipment must not require winning a fight with missing gear. Baseline claims do not bypass the equip/access requirement for a later band. Gear-selling merchants are outside the current scope.

#### Sparse discoveries

Normal equipment drops should be uncommon enough that a return session contains a few useful decisions, rather than pages of disposal work. A **balance candidate** of 0.03% per eligible victorious idle encounter averages about 2.6 pieces over 24 hours at a ten-second cadence and perfect wins. This is only the idle contribution; dungeon, quest, raid, and other rewards must be counted in total arrivals.

Randomness determines when a recognizable item arrives. It does not also roll Tier, Quality, Potential, arbitrary affixes, and several independent stat magnitudes.

Named equipment may arrive with its native style and an authored rank, such as rank 1. Random and guaranteed versions of the same target use the same mechanical definition and awarded rank. Rarity alone never supplies additional ranks or a higher cap.

#### Targeted acquisition guarantees

The player chooses a specific eligible target from a source's visible pool. The target is an item identity/archetype/style reward, not a vague “weapon” category that can repeatedly award the wrong build.

The initial **balance candidate** for dungeon targets is a 20% matching-drop chance per qualifying completion, with the item guaranteed on completion 8 if it has not arrived. This gives an expected wait of approximately 4.16 completions and a maximum of eight. Content access items and run availability must be included when translating that into days.

Required behavior:

1. Progress belongs to the character and a specific source/difficulty/band protection pool. There is no new tradeable pity currency.
2. The player sees eligible targets, the random chance, the guarantee ceiling, and current progress before committing to the source.
3. Only a qualifying completion while a valid target is selected advances that target pool's record. Failed attempts, replayed events, and ineligible difficulties do not advance it.
4. Switching targets within the same protection pool preserves accumulated progress. Targets in one pool must share compatible eligibility and protection terms; a stronger reward belongs in another pool.
5. Switching sources preserves the old source's record. Easier or earlier-band clears cannot charge a later-band guarantee.
6. A qualifying matching drop or the guaranteed award resets the relevant record when the item is durably awarded. An unrelated item does not reset it. Purchases, trades, and baseline quest claims do not reset it.
7. At the guarantee boundary, award one matching target. Do not grant both a successful target roll and a second guarantee item for that same opportunity.
8. Guaranteed items bind to the character on award. Random discoveries may be traded until used.
9. Repeat guarantees remain available, but have no base salvage value and no refundable value for awarded ranks. Only actual subsequent paid investment can be partially recovered.
10. Progress never disappears because the player logged out, an inventory view was full, or a claim request was retried.

The target and eligible definition version for an in-progress run are frozen at the run's existing commitment boundary. Target changes affect subsequent eligible activity. A durable pending reward counts as secured; it must be claimable exactly once and retain the correct item definition. Claiming it later must not duplicate the award or reset a newer target record.

### 4. Blueprints and specialization

A Blueprint item is consumed once to learn a style **for the character across every compatible archetype**. There is no per-recipe copy requirement, mastery, or style level. Baseline weapon/armor access never requires a rare Blueprint.

Learning a style gives the player the right to apply that style to compatible owned equipment. An item with a native style can already use that style without a learned Blueprint. It does not automatically teach the style for other items.

The Forge supports applying a learned compatible style, returning to an item's native style, or returning to its plain archetype distribution. Rank and paid investment remain intact. Restoring a native style on that original item does not require purchasing knowledge of a style it already carried. This is a clarified implementation default to prevent experimenting from permanently locking the player out of the item's original behavior.

Use one active style. Changing it replaces its budget allocation and set membership atomically. A named item's identity/provenance remains recognizable even if its active style changes. Style changes do not change rarity or Tier.

**Cost default:** rank improvements use Cinders and Scrap; style changes use a small Cinder fee only, with no catalyst or separate Scrap charge. The first application of each newly learned style is free once per character/style. Returning to native/plain follows the ordinary fee schedule. Applying the already-active style is a no-op and consumes nothing. The previous review discussed both Scrap/Cinder and Cinder-only style costs; this specification chooses the simpler Cinder-only rule.

Any real stat-changing style application, including a free one, establishes personal binding. This prevents a free application allowance from becoming a workaround for producing enhanced tradeable gear. Merely previewing or learning a style does not bind unrelated items.

Core styles have a clear first-clear/milestone or other bounded acquisition route. Optional prestige styles may use longer trophy goals but must not increase the ordinary rank or stat-budget cap. Tradeable duplicate books can be sold; bound duplicate reward entitlements need an authored replacement that cannot be repeatedly farmed for unintended value.

### 5. Deterministic Tempering

Tempering is the main equipment-investment action:

1. Select an owned item at rank 0–4, including an equipped item when current combat-state rules permit mutation.
2. Preview the next rank's exact stats, full-loadout/set consequences where relevant, price, and resulting binding.
3. Pay the displayed Cinders and Scrap.
4. Receive exactly the quoted improvement, or receive a clear stale/unavailable response with no charge.

There is no success roll, neutral outcome, item XP, reduced XP, lost Potential, destruction, Quality increase, or exclusive idle queue. Rank 5 is complete and offers no further paid rank attempt. Tempering cannot increase Tier or rarity.

For active idle combat, resolve already-earned rewards under the old equipment state before an immediate mutation affects future eligible encounters. Persistent dungeon/Tower/raid/PvP snapshots follow their existing lock rules; they must not change retroactively. If a particular active mode disallows the mutation, explain the lock and charge nothing. “No Tempering timer” does not mean rewriting an already-started battle.

The material cost curve must make early ranks accessible and final ranks a decision about keeping the item. Ordinary content must fund every ordinary rank without requiring raid/PvP/Tower/guild-only rewards. The first useful equipment upgrades should not require dismantling the only usable equipment the player owns.

Tempering no longer awards profession XP, attempts-based achievement progress, or independent Soulstone drops. Any desired preservation of the old overall reward rate belongs in the ordinary content economy; a resource-spending command must not become a currency faucet.

### 6. Economy, binding, and salvage

#### Materials and currency

Use existing **Cinders** and **Tempered Scrap**. Scrap is earned directly from eligible ordinary content and from legitimate surplus equipment. It becomes a shared equipment-investment material across bands; later content and later investment scale together.

Remove routine Metal/Wood/Hide equipment materials and equipment catalysts from current rewards. This includes old selection caches and shop stock. Do not remove Essence Dust, Monster Cores, dungeon access resources, Tower Tokens, raid trophies, Glory, or other unrelated resources merely because they are also called materials/currencies. Audit every retiring item ID for consumers first.

Later-band characters should not be sent back to starter areas for their best Scrap/hour. Tie reward eligibility and yields to content difficulty/progression using existing progression concepts, and model the result. Do not introduce a new material family for every region.

#### Ownership states

| State             | Permitted behavior                                                                                                |
| ----------------- | ----------------------------------------------------------------------------------------------------------------- |
| Unbound discovery | Can be listed, transferred, or donated before personal use; previews do not bind                                  |
| Personally bound  | Can be equipped, tempered, restyled, kept, or salvaged by that character; cannot be sold, transferred, or donated |
| Guild-owned       | Can be loaned under guild rules; cannot become personal saleable property through borrowing/return                |

First equip, rank improvement, or actual style change binds ordinary personal gear. Guaranteed/baseline quest gear is personally bound immediately. Unequipping, changing style, or logging out never unbinds it. Any existing stricter reward-binding rules remain enforceable.

Guild donation converts an eligible unbound discovery into guild property. Borrowing must preserve guild ownership instead of applying personal binding. Borrowers cannot salvage, personally temper/restyle, transfer, or list that item. It returns to the vault with the same guild identity. The initial scope does not add a new guild-funded Tempering service; donated items retain their authored rank/style and existing migrated value. This limitation must be visible before donation. Donated equipment cannot be permanently withdrawn. Disbanding retires available and borrowed guild property without returning personal gear or salvage; the confirmation must state this loss explicitly.

Keep the current Cinder marketplace and its fee/escrow mechanisms. Instance binding supplements the present item-base binding checks. It is not safe to decide eligibility solely from the generic item base when two copies can have different owners and award rules.

#### Salvage contract

For eligible personally owned equipment:

`Scrap returned = eligible authored base salvage + floor(actual paid rank Scrap × 0.50)`.

The 50% recovery rate is a **balance candidate** carried forward from the review. The rules below are structural:

- Refund only Scrap actually charged for paid rank improvements. Store that amount; do not infer it from current rank or current prices.
- Awarded/free ranks have no paid investment. Cinder costs and style-change fees are never refunded.
- Random discoveries can retain a modest authored base salvage value even after binding. Being bound does not by itself mean having zero base value.
- Guaranteed and baseline quest gear have zero base salvage value. They still recover the same fraction of subsequent paid rank investment as other gear.
- Exceptional current reward types require an explicit salvage policy. Their apparent rank is not proof of refundable expenditure.
- Salvage consumes the instance once. Equipped, listed, pending, borrowed, or otherwise unavailable gear cannot also be salvaged from an inventory request.
- Favorites/locks require an explicit user override before destructive bulk actions. Automatic salvage is optional and defaults off.

Example: a named item drops at rank 1, then costs 10 + 20 Scrap for ranks 2–3. Its paid basis is 30, so 15 Scrap is recoverable at 50%. No cost is invented for rank 1. A guaranteed copy returns the same 15 paid-investment Scrap but no base salvage. This preserves an unlucky player's investment without making repeat guarantees into a Scrap faucet.

### 7. Item lifetime and Essence interaction

Use a mixed lifespan centered on equipment retained for a meaningful progression band:

| Equipment role                     | Desired investment/lifetime                                                                |
| ---------------------------------- | ------------------------------------------------------------------------------------------ |
| Starter/plain gap filler           | Immediately useful; rank 0–1; replace after a few sessions when appropriate                |
| Chosen ordinary/named item         | Rank 1–3 normally, 4–5 selectively; a substantial band, provisionally 1–2 weeks            |
| Exceptional late-band/endgame item | Rank 5 and a chosen style; several weeks, provisionally 4–8 where content supports it      |
| Previous-band favorite             | Useful into the next band's opening or in an alternate build; not permanently best in slot |

Tune rank costs, tier growth, and content speed together. Test approximately 25% band budget growth against a maximum +20% rank investment before choosing final values. The present normalized-rating model must be evaluated in real loadouts; a budget ratio alone does not establish combat strength.

Do not implement routine Tier upgrades or an equipment ascension ladder. Content owns the next-band acquisition, while partial salvage recovery reduces the cost of leaving an invested item behind.

Essences retain active/passive ability identity, leveling, Ascension, and collection. Equipment must remain important to damage, defenses, and physical configuration, with styles/set interactions supporting those abilities. Monster Cores must not become equipment-upgrade ingredients. New ordinary equipment must not add a second full ability bar, proc collection, or Ascension economy.

### 8. Player experience

The player needs three practical views:

- **Find equipment:** source-owned reward pools, eligible target selection, current progress, access requirements, and next-band goals.
- **Inspect equipment:** actual stats, archetype/Tier/rank/style, native identity, set effects, binding, comparison, and salvage preview.
- **Forge:** next-rank improvement and style selection, with exact prices and results.

Use the existing game's compact dark-fantasy interface. Remove profession XP, Quality odds, Potential bars, rarity XP, queue controls, and recipe ingredient panels. Keep readable base names, item art, useful tooltips, favorite protection, hand/armor information, and full-loadout comparison.

Return summaries highlight discoveries, secured targets, newly useful items, and Blueprint unlocks. Aggregate routine Scrap. Do not impose per-login loot caps or require frequent logins to preserve progress. Dungeon pending rewards and large offline batches must still produce understandable, recoverable results.

The tutorial becomes: acquire/equip a usable starter → select a content goal → receive a reward → make a first deterministic improvement → learn/apply a style when relevant. It must not tell the player to craft a weapon, equip a gathering tool, or spend Potential.

### 9. Balance candidates and authoring decisions

These are starting values for validation, **not final configured requirements**:

| Parameter                    | Candidate                                                      | Required validation                                                                                            |
| ---------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Idle equipment rate          | 0.03% per eligible victory                                     | Total item arrivals across all activities and offline durations                                                |
| Targeted dungeon drop        | 20% matching chance; guarantee by clear 8                      | Access-item supply, runs/day, time to desired slot/style                                                       |
| Stat allocation              | 85% archetype / 15% style                                      | Every style useful; no universally dominant distribution                                                       |
| Tempering budget             | +4% baseline per rank; +20% at rank 5                          | Stat caps, weapon parity, set effects, PvE/PvP build outcomes                                                  |
| Band growth                  | About 25% baseline                                             | Previous-band usefulness and eventual replacement incentives                                                   |
| Incremental Scrap rank costs | 5 / 10 / 20 / 40 / 80 at a reference band                      | Costs across a whole loadout and simultaneous Essence goals                                                    |
| Ordinary Scrap supply        | About 20–30/day at that reference band                         | Full rank-0→5 material cost of 155 takes roughly 5–8 days for one item; a whole set must be modeled separately |
| Paid Scrap recovery          | 50%, rounded down                                              | Replacement affordability and absence of salvage/craft/purchase loops                                          |
| Style-change cost            | Small Cinder fee; one free first application per learned style | Affordable experimentation, no no-op charges                                                                   |
| Cinder rank prices           | Band-scaled, to be authored                                    | Current net income, existing sinks, baseline access, market fees                                               |

Before enabling the new model, author complete tables for every released archetype, style, set threshold, tier/equip gate, reward source, target pool, baseline quest grant, rank price, Scrap yield, salvage entitlement, and migration mapping. A blank field must fail content validation; it must not silently generate fallback gear or an impossible progression gate.

## Part II — Implementation requirements

### 10. Scope and repository boundaries

The target service is the primary game under `LL/`. Implementation spans Core Domain/Application, Services.LL, Persistence.LL, API.LL content/contracts, and the Angular game. Worker/runtime consumers and admin/live-operations surfaces require compatibility audits where they expose the affected state. LL-Chat and infrastructure-as-code do not acquire new gameplay responsibilities.

Follow repository boundaries:

- Put item/stat/eligibility/economic invariants in Core where appropriate. Core must not depend on Infrastructure, API, or Presentation.
- Use existing CQRS/MediatR conventions: mutations implement `ICommand<T>`, reads implement `IQuery<T>`.
- Services orchestrate domain behavior through repository interfaces. Do not add EF/DbContext access or return DTOs from services; map in Application.
- Keep controllers thin and Angular components focused on presentation.
- Preserve npm/package-lock ownership of the game frontend and keep npm caches outside the checkout.
- Reuse existing transaction, outbox, pending-reward, state-synchronization, and content-definition patterns rather than introducing parallel frameworks.

The requirement IDs below are stable references for later implementation tasks and acceptance checks. “Must” describes required target behavior; paths are current anchors rather than mandatory future filenames.

### 11. Equipment model and canonical stat generation

| ID        | Requirement                                                                                                                                                                                                                          |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| ME-EQP-01 | Introduce an acquisition-independent equipment definition/instance model representing archetype, identity, Tier, native/active style, rank, balance version, provenance, ownership, and salvage entitlement.                         |
| ME-EQP-02 | Route all new normal equipment through one deterministic evaluator, including drops, guarantees, baseline claims, quest rewards, raid/vendor grants, and administrative creation. Do not retain separate crafted/direct-grant power rules. |
| ME-EQP-03 | Preserve stat units, caps, hand rules, armor behavior, quantization, and set semantics; evaluate comparisons and power ratings from the same resulting model used by combat.                                                         |
| ME-EQP-04 | Store actual paid rank investment and distinguish awarded rank value. Repricing, style changes, and later balance versions cannot invent refundable costs.                                                                  |
| ME-EQP-05 | Define equipment rarity mapping without renumbering shared persisted rarity values used by other item types. Remove mechanical rarity boosts from equipment progression gear only.                                                                 |
| ME-EQP-06 | Version historical state and snapshots so old battles remain interpretable. A missing recipe ID on new gear must not select the old direct-grant modifier path.                                                                      |

Required persisted facts, regardless of exact schema:

| Record                  | Required information                                                                                                                                              |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Equipment definition    | Stable ID, archetype/slot/behavior, authored rarity/identity, default style, allowed tier/rank ranges, source constraints                                         |
| Equipment instance      | Stable instance ID, definition/archetype, Tier, native/active style, rank, owner/binding, balance version, provenance, paid investment basis, base salvage policy |
| Learned style           | Character/style identity with uniqueness; first-application entitlement and consumed status                                                                       |
| Protected target record | Character, source/difficulty/band protection key, active target, progress, applicable definition terms, concurrency/version information                           |
| Award receipt           | Unique resolution/award identity, frozen equipment descriptor, pending/claimed state, protection consequence                                                      |
| Economy history         | Actual currencies/materials charged/returned, cause, item identity, operation identity, migration distinction                                                     |

Reuse existing inventory/ledger/outbox entities where they already cover these facts; this table is not a demand for six new parallel subsystems.

Current anchors: [EquipmentInstance](../../LL/src/Core/Domain/Models/Items/Equipments/EquipmentInstance.cs), [InventoryItemFactory](../../LL/src/Infrastructure/Service/Services.LL/Inventories/InventoryItemFactory.cs), [ItemStatRollService](../../LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/ItemStatRollService.cs), [EquipmentCraftingDesign](../../LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentCraftingDesign.cs), [EquipmentTierBudgetCurve](../../LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentTierBudgetCurve.cs), [EquipmentSnapshot](../../LL/src/Core/Domain/Models/Snapshots/EquipmentSnapshot.cs).

### 12. Content rewards and guaranteed acquisition

| ID         | Requirement                                                                                                                                                                                                                                                                      |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ME-LOOT-01 | Extend the item reward contract so a normal equipment award carries sufficient definition/Tier/style/rank/provenance/binding information. The current item ID/quantity/source tuple is insufficient by itself unless the referenced definition fully resolves the missing facts. |
| ME-LOOT-02 | Add authored source pools and eligibility rules, including clear distinction between equipment Tier, dungeon difficulty, source progression, and rarity. Validate every reference and every released-slot acquisition path.                                                      |
| ME-LOOT-03 | Implement source-local target progress and matching/guaranteed awards exactly as section 3 defines, including target switches, unrelated drops, threshold handling, and no double award.                                                                                         |
| ME-LOOT-04 | Make reward/progress persistence retry-safe: one qualifying completion produces at most one contribution and one intended award. A durable pending reward retains its original descriptor and cannot be rerolled by delayed claim.                                               |
| ME-LOOT-05 | Preserve online/offline equivalent outcomes for the same eligible activity and existing offline limits. Resolve batches efficiently without dropping awards or creating per-login incentives.                                                                                    |
| ME-LOOT-06 | Add bound baseline quest acquisition and recovery; validate that access does not depend on the item being awarded by the gated content, and recovery does not require combat with missing gear.                                                                                  |
| ME-LOOT-07 | Replace relevant tool/material/catalyst rewards and vendors with the new authored equipment/resource contract, preserving unrelated Essence, dungeon-access, and activity rewards.                                                                                               |

Reward generation, protection progress, pending delivery, and claim behavior must be designed together. An item stored only as a generic item ID and reconstructed at claim time could lose its Tier/style/rank or change after a content update.

Current anchors: [RewardEntryDefinition](../../LL/src/Core/Domain/Models/Rewards/RewardEntryDefinition.cs), [ItemRewardResult](../../LL/src/Core/Domain/Models/Rewards/ItemRewardResult.cs), [reward-tables.json](../../LL/src/API/API.LL/Data/rewards/reward-tables.json), [IdleCombatRewardCalculator](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/IdleCombatRewardCalculator.cs), [DungeonCompletionRewardApplier](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonCompletionRewardApplier.cs), [DungeonPendingRewardWriter](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonPendingRewardWriter.cs), [DungeonRunRewardClaimer](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonRunRewardClaimer.cs).

### 13. Forge operations, transactions, and APIs

| ID          | Requirement                                                                                                                                                                                                                            |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ME-FORGE-01 | Add read operations for next-rank preview, style options/preview, learned styles, and salvage preview, returning actual effects, prices, ownership consequences, eligibility, and stale-state/version information.                     |
| ME-FORGE-02 | Add mutations for purchasing a rank and applying/restoring/removing a style. Validate ownership, availability, compatibility, rank bounds, current item version, and price terms on the server.                                        |
| ME-FORGE-03 | Learning a style consumes one eligible Blueprint item and creates one character-wide compatible unlock atomically. Duplicate/retried learns do not consume extra copies.                                                               |
| ME-FORGE-04 | Item mutation, currency/material deduction, paid-investment accounting, binding, and event publication must form one durable outcome. A failed or conflicting request cannot partly charge the player.                                 |
| ME-FORGE-05 | Retries and double submissions must not buy two ranks, consume a free-style entitlement twice, or replay a refund. Prevent races with equip, market listing, transfer, donation, and salvage through established concurrency patterns. |
| ME-FORGE-06 | Reconcile earned idle progression before changing equipped stats; retain existing snapshot locks for already-started encounters. Update future combat state without inventing a new Tempering timer.                                   |
| ME-FORGE-07 | Remove profession/attempt/Soulstone side effects from the new Forge operations. Emit events for the actual paid rank/style/unlock outcome, not legacy attempt counts.                                                                  |

Read-only previews never spend, bind, grant progress, or mutate saved gear to the current stat model. An expired preview or changed balance version returns a fresh quote without charging; the client cannot supply its own trusted price, rank, refund basis, or stats.

Add target/source reads and target-selection mutations alongside the Forge operations using the established feature boundaries. Existing production/queue endpoints must be retired or explicitly reject obsolete requests during transition; hiding buttons is insufficient. Exact route names belong to implementation, but the operation contract above is required.

Current anchors: [ForgeController](../../LL/src/API/API.LL/Controllers/V1/ForgeController.cs), [EquipmentController](../../LL/src/API/API.LL/Controllers/V1/EquipmentController.cs), [ForgeService](../../LL/src/Infrastructure/Service/Services.LL/Items/ForgeService.cs), [CharacterActionService](../../LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs), [EquipmentSlotRepository](../../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentSlotRepository.cs).

### 14. Economy, ownership, and integration

| ID        | Requirement                                                                                                                                                                                                                      |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ME-ECO-01 | Apply instance ownership/binding rules consistently in equip, Forge, sale/buyout, transfers, guild donation/borrowing/return, and salvage. Keep stricter base-level restrictions where applicable.                               |
| ME-ECO-02 | Compute salvage from authored entitlement plus recorded paid investment, consume the instance once, and credit Scrap once. Batch validation must not leave a partially charged or partially destroyed selection on failure.      |
| ME-ECO-03 | Preserve current market fee and escrow accounting. Bound instances cannot enter or complete new trades, and obsolete outstanding orders need a defined settlement/cancellation rule.                                             |
| ME-ECO-04 | Prevent loops involving baseline reclaims, repeat guarantees, free ranks, style changes, refunds, and guild loans. Paid recovery can return a fraction of spent Scrap, never mint paid history from rank alone.                  |
| ME-INT-01 | Replace crafting/tool tutorial gates and any quest, achievement, Prophecy, guild-order, event-quest, title, ranking, or bonus dependency on retired mechanics. Preserve earned history and fairly handle in-progress objectives. |
| ME-INT-02 | Replace raid/other entry rules tied to old Epic Blueprint armor with explicit new progression/readiness rules; review reference power ratings and encounter balance together.                                                    |
| ME-INT-03 | Remove obsolete Soulstone bonuses and catalyst-cache content; author current raid trophy, Champion Market and guild rewards directly so no reward requires a retired consumer.                                             |
| ME-INT-04 | Keep existing Essence acquisition/leveling/Ascension mechanics independent from equipment materials and validate complete builds, including set-granted effects.                                                                 |
| ME-INT-05 | Publish and consume coherent inventory/equipment/currency/style/target updates through existing outbox and realtime/state-sync patterns. All affected views must converge after a mutation or reconnect.                         |

Current anchors: [InventoryRepository](../../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs), [MarketPlaceService](../../LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs), [GuildVaultService](../../LL/src/Infrastructure/Service/Services.LL/Guilds/GuildVaultService.cs), [raid-bosses.json](../../LL/src/API/API.LL/Data/raids/raid-bosses.json), [soulstone-upgrades.json](../../LL/src/API/API.LL/Data/progression/soulstone-upgrades.json), [CatalystSelectionCrateCatalog](../../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/CatalystSelectionCrateCatalog.cs), [RealtimeInventoryGameEventOutboxConsumer](../../LL/src/Infrastructure/Service/Services.LL/Outbox/RealtimeInventoryGameEventOutboxConsumer.cs).

### 15. UI, content tooling, and observability

| ID        | Requirement                                                                                                                                                                                                                                |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| ME-UI-01  | Build the source/target, item inspection/comparison, and Forge flows described in section 8 using existing game components and styling. Support mobile, loading, empty, locked, unavailable, conflict, and insufficient-resource states.   |
| ME-UI-02  | Replace old item/profession fields in inventory, equipment, market, vault, reward summaries, tooltips, filters, help guides, tutorial tours, routes, and shared client models. Do not leave “crafted” as the test for valid new equipment. |
| ME-UI-03  | Show binding and exact cost before a use/investment action; protect favorites, new identities, and selected targets in bulk disposal. Any automation rule must be explicit and opt-in.                                                     |
| ME-OPS-01 | Validate archetype/style/source definitions and inspect new item/protection/investment state in existing admin/live-operations tooling where supported. Support corrections must preserve audit and ownership invariants.                  |
| ME-OPS-02 | Record acquisition source, random versus guarantee award, target wait, rank/style purchases, currencies spent, salvage returns, and migration version in existing telemetry/ledger facilities.                                             |
| ME-OPS-03 | Measure time to usable equipment, total arrivals per return session, item retention, target completion, material income/spending, and binding/trade failures. Do not confuse observed results with the balance candidates in section 9.    |

Operational information belongs in admin tools and logs. Player screens show goals, items, prices, and consequences, not schema versions, transaction IDs, or migration plumbing.

Current anchors: [Forge UI](../../LL/src/Presentation/ll/src/app/features/game/character/forge/forge.component.html), [player guide catalog](../../LL/src/Presentation/ll/src/app/shared/help/guide-catalog.ts), [API.AdminDashboard item controller](../../LL/src/API/API.AdminDashboard/Controllers/V1/ItemController.cs), [live-operations client models](../../LL/src/Presentation/liveops/src/app/liveops.models.ts). Further LiveOps work remains deferred.

### 16. Acceptance and verification requirements

| Check                          | Required demonstration                                                                                                                                                                           | Requirements covered                                |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------- |
| AC-01: Baseline access         | A new self-found character can equip a viable set/hand configuration without a profession, market purchase, or lucky named drop; no quest/source requires its own inaccessible reward            | ME-LOOT-02, ME-LOOT-06, ME-INT-01, ME-INT-02        |
| AC-02: One item model          | Random, guaranteed, baseline, quest, vendor, and administrative examples with identical mechanical fields produce the same evaluated stats; no old crafted/direct-grant double budget                  | ME-EQP-01, ME-EQP-02, ME-EQP-03, ME-EQP-06          |
| AC-03: Target ceiling          | An all-miss sequence awards the selected target at the configured threshold; a matching drop resets it, an unrelated drop does not; ineligible clears cannot advance it                          | ME-LOOT-02, ME-LOOT-03                              |
| AC-04: Retry/pending safety    | Replayed completion and claim requests yield one award, one progress change, and the originally frozen descriptor; a content update or delayed claim cannot reroll it                            | ME-LOOT-01, ME-LOOT-04, ME-LOOT-05                  |
| AC-05: Switching targets       | Same-pool switching preserves earned progress; cross-pool switching preserves each separate record; a running encounter cannot be retargeted to exploit a different reward                       | ME-LOOT-03, ME-LOOT-04                              |
| AC-06: Deterministic purchase  | Ranks 0–5, insufficient funds, stale quotes, double clicks, and concurrent mutations have correct outcomes; no partial charge, rank 6, free binding bypass, or attempt rewards                   | ME-FORGE-01 through ME-FORGE-07                     |
| AC-07: Style rights            | One Blueprint unlock works across compatible bases; duplicate learning and free-use retries consume nothing extra; native/plain restoration preserves rank and replaces old set effects          | ME-EQP-03, ME-FORGE-03, ME-FORGE-04, ME-FORGE-05    |
| AC-08: Combat boundaries       | Idle rewards before a change use old gear; future eligible encounters use new gear; existing dungeon/Tower/raid/PvP snapshots remain unchanged                                                   | ME-EQP-06, ME-FORGE-06                              |
| AC-09: Binding everywhere      | Personal use prevents sale/transfer/donation; borrowing preserves guild ownership; race conditions cannot make gear both sold and equipped or both borrowed and salvaged                         | ME-ECO-01, ME-ECO-03, ME-FORGE-05                   |
| AC-10: Refund accounting       | A rank-1 drop upgraded for 30 paid Scrap refunds only 15 at the candidate rate, plus eligible base return; guarantee/baseline examples have zero base return; changed prices cannot mint refunds | ME-EQP-04, ME-ECO-02, ME-ECO-04                     |
| AC-11: Whole-build balance     | Representative armor/weapon/Essence/set combinations validate rank value, hand parity, new-band replacement, and the absence of mandatory raid/PvP/guild ingredients                             | ME-EQP-03, ME-INT-02, ME-INT-04                     |
| AC-12: Offline/UX burden       | Short sessions and the supported full offline window preserve outcomes, useful summaries, comparison accuracy, target visibility, and locked/favorite protection                                 | ME-LOOT-05, ME-UI-01, ME-UI-02, ME-UI-03, ME-INT-05 |
| AC-13: No retired dependencies | Content/reference scans and walkthroughs find no active crafting/tool gates, obsolete rewards, Quality/Potential UI, or dead profession objectives                                               | ME-LOOT-07, ME-INT-01, ME-INT-03, ME-UI-02          |
| AC-14: Cleanup integrity | Fresh schema creation and cleanup migrations succeed; obsolete quests/versions and their objectives are deleted, current quest progress is retained, and normal availability can recreate eligible current quests without legacy fallbacks. No Alpha conversion or compensation is required. | ME-MIG-01 through ME-MIG-05 |

Implement meaningful tests around these state transitions and invariants. Do not retain tests merely asserting the old random Tempering/production behavior. Reuse the existing equipment, reward, inventory/transfer, queue, quest, and snapshot fixtures where helpful; classify each old test as retained, rewritten, or retired.

Planned verification for the implementation task:

| Area                   | Command/workflow                                                                                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Backend behavior       | Run [build/run-tests.ps1](../../build/run-tests.ps1) from the repository root; use this required wrapper rather than a parallel ad hoc test entry point |
| Angular behavior       | Run `npm run test:ci` from the game frontend when its test prerequisites are available                                                                  |
| Angular integration    | Run `npm run build:development` from the game frontend after contract/UI integration                                                                    |
| Diff/static validation | Run `git diff --check` and content-reference/schema validation for modified catalogs                                                                    |
| Balance                | Simulate target wait distributions, total item/Scrap arrivals, full-loadout cost/lifetimes, progression bands, and representative combat builds         |
| Cleanup migrations | Generate SQL and verify deletion/cascade/current-data behavior with isolated fixtures; do not apply migrations to shared/production databases |

The test/build commands above are requirements for future code work, not claims that they ran for this Markdown-only task. Missing prerequisites must be reported in that implementation task. Package-manager caches stay outside the repository, including on Windows.

## Part III — Removal and transition

### 17. What must be removed

Alpha data does not need preserving. Remove obsolete gameplay paths, content and storage without building conversion, compensation or cohort support. The [cleanup record](equipment-post-alpha-cleanup.md) distinguishes completed removals from shared helpers that still serve current tools.

| ID | Retired behavior | Required replacement or end state |
| --- | --- | --- |
| RM-01 | Mining, Woodcutting and Skinning progression | Current combat/content rewards; no profession XP or level gate. |
| RM-02 | Gathering tools, affixes and Tool progression | Combat equipment and source-owned rewards; no tool acquisition objective. |
| RM-03 | Overworld/dungeon gathering nodes and processors | Explicit current area/dungeon rewards. |
| RM-04 | Routine equipment manufacture and batch production | Drops, source guarantees and starter/recovery grants. |
| RM-05 | Crafting profession progression | No replacement profession or Alpha compensation. |
| RM-06 | Recipe mastery and Quality-odds progression | Deterministic equipment evaluation and reusable styles. |
| RM-07 | Recipe-specific Blueprint learning | One style unlock across compatible archetypes. |
| RM-08 | Quality and random production-budget variance | Canonical archetype/tier/style/rank stats. |
| RM-09 | Potential, random/critical tempering and item XP | Deterministic ranks and actual paid-investment accounting. |
| RM-10 | Rarity as equipment XP/stat progression | Authored identity rarity without a hidden power multiplier. |
| RM-11 | Tempering queues and auto-return-to-combat coupling | Immediate Forge operations and independent combat scheduling. |
| RM-12 | Masterpiece/LevelingItem progression | No current creation path or objective depending on those flags. |
| RM-13 | Routine Metal/Wood/Hide materials and catalysts | Scrap/Cinders and content-earned styles. |
| RM-14 | Flat salvage return for every item | Award entitlement plus actual paid Scrap investment. |
| RM-15 | Profession quests/tasks, rankings and bonuses | Current combat/Essence/dungeon objectives; delete obsolete definitions and progress. |
| RM-16 | Production/queue routes, APIs, DTOs and help | Equipment and Forge contracts with no old-client aliases. |
| RM-17 | Tests whose intended behavior is retired gameplay | Tests of the current loop; retain shared numerical tests only for active consumers. |

#### Current removal/rework anchors

- [Cleanup record](equipment-post-alpha-cleanup.md): removed services, content, UI and persistence.
- [Quest flow](../../LEGENDSLEGACY_QUEST_FLOW.md): the 29 current regular quests and expired example event.
- [Profession/queue schema cleanup](../../LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903115622_RemoveAlphaProfessionsAndTemperingQueues.cs).
- [Saved quest-progress cleanup](../../LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903121634_RemoveRetiredAlphaQuestProgress.cs).

#### Things explicitly retained

- Canonical equipment bases, combat slots, hand/armor behavior, styles/sets and shared stat-budget/constraint machinery.
- Forge tempering as deterministic ranks, Blueprints as reusable styles, and the tempered_scrap item identity.
- Current operation receipts, ownership/concurrency checks, frozen awards and combat snapshots, and baseline recovery.
- Character progression, Essences, dungeon access/mastery, guilds, markets and unrelated currencies/rewards.
- Recipe/tempering simulations still consumed by CanonicalEquipmentBuildFactory and offline balance tools. These are active numerical dependencies, not player crafting APIs or Alpha compatibility paths.
- Historical EF migrations and stable identifiers still used by the current schema. Do not renumber unrelated shared enum values as part of this cleanup.

### 18. Migration requirements

The following requirements replace the original Alpha holdings-conversion plan. No inventory mapping, queue settlement, profession compensation, Soulstone refund or alternate quest cohort is required.

| ID | Requirement |
| --- | --- |
| ME-MIG-01 | Remove obsolete storage through explicit EF migrations while retaining a runnable fresh-schema chain. |
| ME-MIG-02 | Delete regular quest progress absent from the frozen post-Alpha ID/version catalog; cascade-delete its objectives and retain current pairs. |
| ME-MIG-03 | Verify generated SQL, an empty database, stale/valid quest fixtures, cascade deletion and repeat execution. Report PostgreSQL acceptance separately from SQLite or source checks. |
| ME-MIG-04 | Use the current API/player contracts together. The API applies pending migrations at startup; the task must not apply them to shared/production databases. |
| ME-MIG-05 | Record migration IDs, verification limits and irreversible data deletion. Do not invent recovery promises for discarded Alpha data. |

Two cleanup migrations follow the existing equipment schema migrations: RemoveAlphaProfessionsAndTemperingQueues and RemoveRetiredAlphaQuestProgress. The latter fixes strict quest lookup failing on deleted crafting/gathering IDs or removed definition versions. The task generated these changes without applying them to the game database. Rebuild and restart the local API to run pending migrations, then check the quest journal. No new configuration setting is needed.

### 19. Implementation sequence

| Area | Current state / next action |
| --- | --- |
| Canonical model, acquisition, Forge and ownership | Implemented for Shenic / Tier 1 and Meran / Tier 2 with provisional values. |
| Quest/reward/UI integration and Alpha cleanup | Implemented; local database startup and authenticated acceptance remain to be checked. |
| Meran / Tier 2 | Regional ordinary pools, dungeon targets/styles, recovery and explicit tier prices implemented. Future regions require authored content. |
| Integrated balance | Validate complete equipment/Essence builds, hand configurations, acquisition cadence, Cinder/Scrap costs and replacement timing. Update retained offline calibration consumers where needed. |
| Raid redesign and further LiveOps | Deferred by the owner. |

Alpha conversion rehearsal and compatibility rollout are removed from the sequence. The five capabilities default to enabled, but local correctness checks do not certify deployed state or complete-game balance. Gear-selling merchants remain excluded.

### 20. Definition of complete

- Players acquire usable gear and pursue targets with bounded bad luck, without gathering or production.
- Rank/style investment is predictable and correctly reflected in combat, comparisons, snapshots, inventory and economic history.
- Equipment/Essence combinations and full-loadout investment fit the supported progression bands and play patterns.
- Trade, binding, guild borrowing, baseline recovery and deterministic awards obey one ownership/economic contract.
- No active objective, reward, route or gate requires a removed system. Shared numerical helpers have named active consumers.
- Cleanup migrations and relevant backend/frontend/content checks pass, with database and authenticated acceptance limits reported separately.

Markdown-only updates are checked for requirement/removal/acceptance ID consistency, local links, content accuracy and whitespace. Application verification is recorded separately in the [status ledger](equipment-implementation-status.md).
