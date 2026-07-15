# Prophecy-Adjacent Currency Plan

Status: implemented on 2026-07-14; live telemetry integration remains a follow-up because the repository has no metrics/analytics substrate

Scope: Fate Echo, Sigil Fragments, and Ascension Stone Fragments

Related analysis: `docs/prophecies-system-analysis.md`, pain point #8

## Implementation Record

The core economy described by this plan is now implemented:

- A reroll replaces all three daily offers as one set. Fate Echo funds two paid set rerolls after the free reroll, at server-owned costs of 40 and 80.
- Daily reroll count, shown-definition history, Fate Echo spent, UTC period, and an optimistic concurrency version are stored in a dedicated per-character state row.
- Sigil Fragments assemble a selected accessible dungeon sigil for a server-owned cost of 10. Forge access ignores only the sigil being created; previous-difficulty and other entry requirements still apply.
- Fate Echo and Sigil Fragment balances, the next reroll price, and accessible forge options are visible on the Prophecies page. The Dungeons page links back to the forge.
- Active Prophecy, cache, and Guild sources no longer award Ascension Stone Fragments. The character balance, reward property, and database column are removed outright because there is no production data to preserve.
- There is no conversion version, balance converter, or historical reward-snapshot translator.
- Economy flags, prices, and limits live in `Data/prophecies/economy.json` and are startup-validated.
- Guild Sigil Fragment grants were retuned, Champion's Market labels now match their payouts, and dungeon sigil drop terminology no longer calls complete sigils “fragments.”

The later telemetry guardrail section remains the rollout specification for a future shared telemetry facility. No bespoke analytics stack was introduced solely for this feature.

## Executive Decision

The three balances should not all receive sinks merely because they already exist. Each should survive only if it creates a clear player decision that is not already handled by another resource.

| Currency | Decision | Intended identity |
| --- | --- | --- |
| Fate Echo | Keep and make the primary Prophecy agency currency | Spend to replace unwanted daily offers without buying completion or Favor. |
| Sigil Fragments | Keep, add a deterministic assembly sink, and retune sources | Spend to create a chosen, currently accessible dungeon sigil. |
| Ascension Stone Fragments | Retire and replace | Remove the currency; future rewards use the existing Monster Core/Soul Dust economy instead. |

This leaves Prophecies with one native spend currency, one dungeon bridge currency, and no redundant ascension currency.

## Why This Decision

The current implementation has three scalar fields on `Character`, exposes them through `CharacterDto`, grants them from Prophecies and parts of the Guild shop, and never subtracts them. The frontend displays them as reward lines but does not provide a durable balance surface or a route to spend them.

That creates four problems:

1. A reward without a use is not a reward decision; it is an unexplained number.
2. Three dead balances inflate cognitive load around an already currency-heavy game.
3. Sigil and ascension terminology overlaps functional inventory items that already unlock dungeons and ascend Essences.
4. Adding arbitrary shops for all three would preserve the data model while making the overall economy harder to understand.

The target design uses existing systems wherever they are already stronger than the unfinished currency concept.

## Goals

- Give every retained currency one memorable sentence explaining why it exists.
- Make each sink repeatable enough to prevent permanent balance accumulation.
- Keep the local schema and reward model free of compatibility-only state.
- Avoid turning Prophecies into a mandatory source of general combat power.
- Avoid letting currency spending buy Prophetic Favor, objective completion, or weekly milestone access.
- Use data-driven prices and limits so balance changes do not require service rewrites.
- Keep the implementation maintainable for a solo developer.

## Non-Goals

- Redesigning Cinders, Soulstones, Arena Glory, Guild Favor, Guild Honors, or Prophetic Favor.
- Rebalancing the complete dungeon-sigil drop economy.
- Reworking Essence ascension costs.
- Adding a general-purpose currency exchange.
- Making prophecy currencies tradable on the Marketplace.
- Solving claim idempotency or concurrency as part of this economy change.

## Current-State Audit

### Storage and presentation

The original design stored all three currencies as `long` properties directly on `Character`:

- `FateEcho`
- `SigilFragments`
- `AscensionStoneFragments`

The implemented design retains and displays Fate Echo and Sigil Fragments. Ascension Stone Fragments have been removed from the character model, DTOs, reward snapshots, and active content.

### Sources

| Source | Fate Echo | Sigil Fragments | Ascension Stone Fragments |
| --- | ---: | ---: | ---: |
| Daily prophecy | 7 / 10 / 13 / 16 by difficulty | 3 for a Dungeon-category daily | 1 for a Dungeon-category daily |
| Greater Prophecy | 26 / 34 / 42 / 50 by difficulty | 10 / 12 / 14 / 16 for Dungeon category | 6 / 7 / 8 / 9 for Dungeon category |
| Weekly Revelation milestones | 10 + 20 + 35 | Cache rolls only | Cache rolls only |
| Prophecy caches | Common weighted reward | Weighted rewards of 2–5 | Weighted rewards of 1–2 |
| Guild `Echo Stipend` | 15 | 100 | — |
| Guild `Honor Reliquary` | 25 | — | 5 |
| Guild `Elder Cache` | — | 250 | — |

Important observations:

- A full seven-daily week currently produces roughly 220–310 Fate Echo before Guild rewards, depending on prophecy difficulty and cache rolls.
- Sigil Fragment source scales conflict. Prophecies award single digits or low double digits, while Guild offers award 100 or 250.
- Ascension Stone Fragments arrive in quantities that look like fractions of a future item, but no assembly rule or Ascension Stone item exists.
- Reward snapshots formerly included the unused fragment field. Because there is no production or shared environment to preserve, the field is removed without a compatibility reader.

### Existing adjacent systems

The game already has stronger destination resources:

- Dungeons consume concrete sigil inventory items such as Goblin, Catacomb, and Hive Sigils.
- Idle combat already drops those complete sigil items, targeted at approximately two drops per day.
- Essence ascension already consumes Lesser, Greater, and Primal Monster Cores.
- Essence leveling already consumes Soul Dust.
- Potential progression already consumes region-specific Potential Cores.

Creating a new Ascension Stone recipe tree would compete with Monster Cores. Sigil Fragments are different: they can provide deterministic choice alongside random complete-sigil drops.

### Misleading adjacent content

Two Champion's Market entries use fragment terminology without granting the named resources:

- `cache.sigil_fragments` grants Cinders.
- `cache.ascension_stone_shards` grants Soulstones.

These should be renamed at the display level or replaced with accurately modeled rewards. They should not be treated as evidence that the fragment currencies have working uses.

## Currency Design Rules

A retained currency must meet all of these rules:

1. **Clear loop:** the player knows where it comes from and where to spend it.
2. **Distinct purpose:** another existing currency or item does not already do the same job better.
3. **Meaningful choice:** spending changes an outcome, not merely converts into generic Cinders.
4. **Repeatable sink:** engaged players can continue spending it after finishing finite unlocks.
5. **No mandatory detour:** players should not need unrelated PvP or Guild participation to operate the sink.
6. **Visible balance:** every reward preview links mentally and visually to its spend surface.
7. **Atomic mutation:** spend and reward happen in one transaction and reject insufficient balances.

## Fate Echo: Keep as Prophecy Agency

### Product role

Fate Echo should mean: **“I can challenge the omen I was given.”**

The game now grants one free reroll of the complete unaccepted daily offer set per UTC day. Fate Echo should therefore fund **additional** agency beyond that baseline rather than charging players for their first correction.

### Implemented baseline: one free reroll

- The player rerolls while all three daily prophecies are still `Offered`.
- The server replaces all three definitions together, preserving each instance's slot.
- The action is unavailable after any daily has been accepted, completed, claimed, declined, or expired.
- Current definitions cannot remain in the replacement set; earlier shown definitions are suppressed while the authored pool permits it.
- Duplicate definitions, repeated categories, and recent history are suppressed where the authored pool allows it. Full level/feature/profession eligibility remains future work.
- Rerolls are persisted per daily period and character.
- Selection remains deterministic through a reroll-specific seed.
- Consumption is atomic across API replicas.

### Implemented Fate Echo sink: additional rerolls

After the free use, Fate Echo can buy up to two additional rerolls. Initial escalating costs:

| Reroll in the same UTC day | Cost |
| ---: | ---: |
| First | Free |
| Second | 40 Fate Echo |
| Third | 80 Fate Echo |

The three-reroll cap and paid prices are implemented in server-owned balance data.

Why escalating cost is preferable to a flat price:

- One correction is universally available.
- Repeated fishing for an optimal objective becomes expensive.
- The sink scales with engaged players without forcing casual players to spend.
- It can absorb the current weekly Fate Echo supply without adding a generic reward shop.

### Rules that must remain explicit

- Fate Echo cannot purchase Prophetic Favor.
- Fate Echo cannot complete, skip, extend, or revive an accepted objective.
- Rerolling never changes the one-daily-acceptance rule.
- Each rerolled prophecy keeps the same instance ID, daily period, and slot identity while receiving fresh target, objective, progress, and reward snapshots.
- All three replaced definition IDs are retained on their instances for support history and repetition suppression.
- The command must be idempotent once progress-event idempotency is addressed, but this plan does not implement pain point #3.

### Optional later sink: targeted reroll

After normal reroll usage is measured, a higher-priced option may let the player request an eligible category such as Combat, Dungeon, Crafting, Gathering, or Essence.

Provisional price: 60 Fate Echo, counting as one of the future paid rerolls.

Do not implement this until eligibility filtering is reliable. A category picker that can return inaccessible content would make the paid action feel broken.

### Why Fate Echo should not be replaced with Cinders

Cinders already function as the broad trade and soft currency. Charging Cinders for Prophecy rerolls would couple offer quality to Marketplace wealth and make Prophecies another generic Cinder sink. Fate Echo keeps agency earned mostly inside the Prophecy loop.

## Sigil Fragments: Keep as Deterministic Dungeon Access

### Product role

Sigil Fragments should mean: **“Random sigil drops did not give me the dungeon I wanted, so I can assemble the right one.”**

That role complements complete sigil-item drops rather than duplicating them. Random idle drops provide volume; fragments provide selection.

### Implemented sink: contextual dungeon sigil assembly

The exchange belongs to Dungeons, where the player is already choosing a destination and reviewing its entry cost. It is intentionally not exposed from the Prophecies page.

- When a selected dungeon is missing its sigil, its Entry Cost panel offers an explicit assembly action.
- Spend Sigil Fragments to create that dungeon's sigil item.
- Only sigils for currently unlocked and accessible dungeons may be selected.
- The output uses the existing sigil item base; it does not create a second “crafted sigil” item family.
- Spend and inventory grant occur atomically.
- The response returns the new fragment balance and updated inventory quantity.
- Assembly is never automatic when entering a dungeon.

Cost: **10 Sigil Fragments for one chosen sigil**.

At the configured Prophecy income target, a complete week produces 24.05 fragments on average and therefore funds roughly 2.4 selected sigils. Compare this against:

- Median and high-percentile existing fragment balances.
- Actual complete-sigil drops per active character per week.
- Dungeon-entry consumption per active character per week.
- Marketplace price and volume for each sigil item.

### Source budget

A completed Prophecy week grants 15 fragments directly: 2 from each of five dailies and 5 from the weekly. Opening the Greater Prophecy and three Weekly Revelation caches adds 9.05 fragments in expected value, producing a total of 24.05. This baseline does not depend on receiving or choosing Dungeon-category Prophecies.

Guild offers are optional additional sources and are excluded from that weekly baseline. At the 10-fragment assembly price, the 25-fragment Echo Stipend and 50-fragment Elder Cache are worth 2.5 and 5 assembled sigils respectively, so their purchase rate and effect on sigil supply should be monitored separately.

### Marketplace decision

Sigil Fragments remain character-bound scalar currency and cannot be listed. The assembled sigil follows the existing sigil item's trade rules.

This conversion can inject tradeable sigils into the economy, so Marketplace volume and price must be monitored. If it causes excessive arbitrage, prefer a weekly forge limit or bound sigil variants; do not make the scalar fragments directly tradeable.

### Terminology cleanup

`SigilFragmentDropRateRelativeBps` currently modifies the chance of dropping complete sigil items, not the scalar `SigilFragments` balance. Rename its code/config/display terminology to `DungeonSigilDropRateRelativeBps` during an independent cleanup. Otherwise two unrelated meanings of “fragment drop rate” will coexist.

### Why Sigil Fragments should not be replaced immediately

Replacing them with random complete sigils would remove their only potentially valuable distinction: player-selected dungeon access. Replacing them with Fate Echo would make Fate Echo a broad exchange currency and weaken both identities.

If assembly engagement remains negligible after one full balance cycle, removal becomes the fallback: convert balances into accessible sigil items, change future rewards to actual sigil caches, and remove the scalar field.

## Ascension Stone Fragments: Retire and Replace

### Product role assessment

Ascension Stone Fragments do not pass the distinct-purpose test.

- No Ascension Stone item exists.
- No assembly sink exists.
- Essence ascension already has a complete, tiered Monster Core economy.
- Soul Dust already covers lower-level Essence investment.
- Adding an Ascension Stone would give players two different resource families for the same progression verb.

The field should therefore be retired rather than justified retroactively.

### Future reward replacement

Replace new Ascension Stone Fragment rewards with resources that already have working sinks:

| Current source | Replacement direction |
| --- | --- |
| Dungeon daily prophecy: 1 fragment | A small fixed Soul Dust reward, or a low deterministic progress value toward an existing Essence cache. Prefer Soul Dust for the MVP. |
| Dungeon Greater Prophecy: 6–9 fragments | One Lesser Monster Core at lower difficulties; consider two only after source/sink telemetry. |
| Greater/Perfect/Greater Prophecy cache rolls | Soul Dust on common rows and a complete Lesser Monster Core on rare rows. |
| Guild Honor Reliquary: 5 fragments | One Lesser Monster Core or a clearly named Essence-material cache. |

Do not introduce “Monster Core Fragments” as the replacement. That would rename the same redundant abstraction without simplifying the economy.

### Local schema removal

There is no production environment or player data requiring preservation. Retirement is therefore intentionally direct:

- Remove `AscensionStoneFragments` from `Character`, DTOs, and reward snapshots.
- Remove it from every active JSON source and fallback definition.
- Drop the database column in the feature migration.
- Do not add conversion versions, balance translators, or compatibility-only properties.
- Recreate disposable local data if an older snapshot cannot be deserialized.

## Target Economy Summary

| Player action | Resource earned | Resource spent | Outcome |
| --- | --- | --- | --- |
| Claim Prophecies and Revelation milestones | Fate Echo | — | Builds Prophecy agency. |
| First daily reroll | — | — | Receives a different set of three eligible offers. |
| Additional daily reroll | — | Fate Echo | Receives another different set of three eligible offers. |
| Complete Prophecies / selected Guild rewards | Sigil Fragments | — | Builds deterministic dungeon access. |
| Assemble an accessible dungeon sigil | — | Sigil Fragments | Receives one existing sigil item. |
| Claim selected Dungeon/Essence-oriented rewards | Soul Dust or Monster Cores | — | Feeds the existing Essence progression economy. |
| Level or ascend an Essence | — | Soul Dust / Monster Cores | Uses existing implemented sinks. |

## Server Design

### Likely repository impact

| Area | Current implementation point | Planned responsibility |
| --- | --- | --- |
| Character balances | `LL/src/Core/Domain/Models/Entities/Characters/Character.cs` | Retain Fate Echo and Sigil Fragments; remove the ascension balance. |
| Prophecy rewards and claims | `LL/src/Infrastructure/Service/Services.LL/Prophecies/ProphecyService.cs` | Apply the retained/replacement sources and expose reroll behavior through a focused service operation. |
| Prophecy contracts | `LL/src/Core/Application/Interfaces/Services/LL/Prophecies` and Prophecy DTOs | Add reroll/authoritative-balance responses and phase out the retired reward property. |
| Prophecy API | Prophecy controller and commands under `LL/src/API/API.LL` and `LL/src/Core/Application/UseCases/Prophecies` | Own authorized rerolls; it does not own dungeon sigil assembly. |
| Guild sources | `LL/src/API/API.LL/Data/guilds/guild-content.json` and `GuildContentProvider.cs` fallback content | Retune Sigil Fragment awards and replace Ascension Stone Fragment grants. |
| Dungeon access | Dungeon controller, command/service, definitions, access policy, and repository | Own selected-dungeon validation, atomic fragment spending, and sigil granting. |
| Essence destinations | `EssenceProgressionConstants.cs` and `EssenceSystemService.cs` | Reuse Monster Core and Soul Dust item IDs without changing ascension rules. |
| Character DTOs | Backend `CharacterDto` and Angular `characterDto.ts` | Return only the retained balances. |
| Prophecies UI | `prophecies-page.component.ts/.html` | Own Fate Echo display and daily rerolls; no assembly controls or fragment balance. |
| Dungeons UI | Region dungeon components | Show contextual assembly in the selected difficulty's Entry Cost panel and refresh authoritative dungeon state. |
| Persistence | EF configuration and migrations | Store reroll state and drop the retired character column. |

### Balance configuration

For the remaining economy work, create one server-owned configuration model for:

- Additional Fate Echo reroll costs and the total paid limit.
- Dungeon sigil assembly price.
- Guild source quantities.
- Feature flags for each rollout phase.

Do not hard-code the new prices across controller, service, and frontend code.

### Prophecy reroll state

The implemented free reroll currently persists:

- Character ID.
- Daily period start.
- A usage timestamp on the period's Steady offer.
- The definition IDs replaced across the rerolled set.

Paid escalating rerolls use a dedicated daily reroll state row for count, shown-definition history, and Fate Echo paid rather than stretching the single-use anchor fields.

### Commands and responses

Suggested application operations:

- `RerollDailyProphecies(characterId)`
- `AssembleDungeonSigil(characterId, dungeonId)`

Responses should return authoritative balances and changed state:

- Reroll: the authoritative updated overview, remaining Fate Echo, count, and next cost.
- Assembly: selected dungeon ID, granted sigil item ID, remaining fragments, and new inventory quantity.

Both operations require character ownership checks and transaction-scoped balance validation.

### Reward model transition

For the local retirement:

- Remove the retired property from reward snapshots and DTO responses.
- Add complete item rewards through the existing `Items` collection.
- Update prophecy cache tables, direct reward formulas, Guild JSON, and Guild fallback definitions together.
- Add startup validation that rejects active reward content referencing retired currency types.

The Guild system currently duplicates content in JSON and fallback code. Both representations must remain synchronized until that duplication is removed.

## Frontend Design

### Prophecies page

The Prophecies page shows the resource used by its own agency action:

- Current Fate Echo balance.
- The next paid reroll cost.

The daily section shows one set-level reroll action while all three offers remain eligible. Paid states additionally explain insufficient Fate Echo and the daily limit.

### Dungeons page

The selected dungeon's Entry Cost panel owns “Assemble Sigil”:

- Show owned complete sigils.
- Show fragment balance and assembly cost.
- Show the action only when that selected difficulty is missing its sigil.
- Disable assembly when non-sigil access requirements or fragment balance block it.
- Refresh the authoritative dungeon hub after a successful command so entry readiness updates immediately.

### Currency glossary

Tooltips should use one-sentence identities:

- **Fate Echo:** “Spend to replace unwanted daily Prophecy offers.”
- **Sigil Fragments:** “Assemble the sigil for an accessible dungeon of your choice.”
- Ascension Stone Fragments are removed and should not receive a tooltip for a retired system.

### Champion's Market cleanup

Rename the two misleading offer displays while preserving stable IDs, unless their reward schemas are expanded to grant the named resources:

- “Sigil Fragment Cache” should not grant only Cinders.
- “Ascension Stone Shard Cache” should not grant only Soulstones.

Keeping stable internal IDs avoids unnecessary purchase-history migration.

## Migration and Rollout

### Phase 0: establish local balance defaults

1. Estimate weekly source totals per character and source type from authored content.
2. Compare the sigil assembly price with complete-sigil drops and dungeon-entry consumption.
3. Stop adding new currency sources until the target model is implemented.
4. Keep all prices and limits in server-owned JSON so local playtesting can retune them.

Exit criterion: coherent starting earn/spend ranges suitable for local playtesting.

### Phase 1: make Fate Echo useful

1. Add reroll balance configuration and persisted daily reroll state.
2. Implement the transactional reroll command.
3. Exclude current, previously shown, duplicate, and inaccessible definitions.
4. Add Prophecies-page balance and reroll UI.
5. Add audit events and telemetry.
6. Run for at least one full weekly cycle before changing Fate Echo source quantities.

Exit criterion: successful rerolls, no negative balances, and measurable ongoing Fate Echo spend.

### Phase 2: launch dungeon sigil assembly

1. Add the transactional selected-sigil exchange.
2. Validate dungeon access using the existing dungeon access policy.
3. Add the contextual Dungeons Entry Cost action; keep Prophecies focused on Prophecy decisions.
4. Retune Guild fragment awards.
5. Correct the misleading sigil-drop bonus terminology.
6. Monitor sigil Marketplace effects.

Exit criterion: fragments have a repeatable spend rate and complete-sigil supply remains healthy.

### Phase 3: retire Ascension Stone Fragments

1. Replace every active source in Prophecy formulas, cache tables, Guild JSON, and Guild fallback content.
2. Remove the balance and reward fields from the domain model and DTOs.
3. Remove all service and UI handling for the retired currency.
4. Generate a migration that drops the character column directly.
5. Recreate disposable local data if necessary.

Exit criterion: no active source, model, DTO, service, or current schema exposes the currency.

## Testing Strategy

### Fate Echo

- Reroll succeeds only when all three slots have eligible replacements.
- Balance is deducted exactly once.
- Escalating prices use the persisted daily count.
- Reroll rejects the set after a daily is accepted, completed, declined, or expired.
- Every current definition is replaced, with no duplicates in the new set.
- An incomplete replacement set fails without charging or partially changing offers.
- Concurrent rerolls cannot overspend or create duplicate active offers.
- Daily rollover resets cost without erasing history.

### Sigil Fragments

- Forge grants only an accessible dungeon's sigil.
- Insufficient balance and inaccessible dungeon attempts do not mutate state.
- Spend and inventory grant are atomic.
- Concurrent forge requests cannot make the balance negative.
- Response quantities match persisted inventory.
- Marketplace eligibility follows the existing sigil item rules.

### Ascension retirement

- Character and reward models contain no retired field.
- New snapshots contain no retired reward.
- Guild purchases no longer grant the retired balance.
- Active JSON content contains no retired reward key.
- The feature migration drops the retired database column without conversion bookkeeping.

### Content validation

- Active reward definitions cannot reference retired currency types.
- Guild JSON and fallback definitions produce equivalent active offers.
- Reroll costs and forge costs are positive and limits are valid.

## Telemetry and Balance Guardrails

Track weekly per-character distributions, not only totals:

- Fate Echo earned, spent, and closing balance.
- Rerolls by price tier, slot, category, and eventual accepted objective.
- Percentage of players who never spend Fate Echo.
- Sigil Fragments earned, forged, and closing balance.
- Chosen sigil distribution and dungeon entries after forging.
- Complete sigil drops, Marketplace listings, sale prices, and unsold volume.
- Monster Cores granted through replacement rewards versus normal gameplay.
- Essence ascensions before and after fragment retirement.

Initial guardrails:

- No currency balance may become negative.
- At least half of weekly Fate Echo earned by engaged Prophecy users should have a plausible voluntary sink; this does not mean every player must spend half.
- Sigil assembly output should remain a minority of total complete-sigil supply unless deterministic access is intentionally promoted.
- Replacement Monster Core grants should not exceed ordinary Monster Core acquisition without an explicit balance decision.
- No retired currency source may remain active after Phase 3.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Players reroll until only the easiest objective remains | Escalating costs, daily limit, slot preservation, duplicate/history exclusion. |
| Rerolls offer inaccessible objectives | Do not launch paid targeted selection before eligibility enforcement is reliable. |
| Existing Fate Echo stockpiles trivialize reroll costs | Audit balances first; use escalating costs and tune from percentiles rather than averages. |
| Guild Sigil Fragment rewards flood complete sigils | Retune 100/250 awards, monitor Marketplace supply, add a forge limit only if needed. |
| Disposable local snapshots contain the removed field | Recreate local data; do not carry compatibility code into the product model. |
| Currency UI becomes another large feature page | Keep spending contextual: rerolls on offers, assembly beside dungeons. |
| Generic exchanges blur system identities again | Do not add open-ended Cinder, Soulstone, Glory, or Fate Echo conversion tables. |

## Acceptance Criteria

Implementation status against the acceptance criteria:

- [x] Fate Echo has a visible, transactional daily-offer reroll sink.
- [x] Sigil Fragments can create a selected accessible dungeon sigil.
- [x] Prices and limits are server-owned data.
- [x] Both retained balances are visible next to their spend actions.
- [x] Ascension Stone Fragments have no active source.
- [x] Ascension Stone Fragment model, reward, DTO, and database fields are removed outright.
- [x] No conversion or historical-snapshot compatibility layer is retained.
- [x] Champion's Market labels describe their actual rewards.
- [x] Automated coverage includes ownership, insufficient funds, concurrency, and UTC rollover.
- [ ] Telemetry can compare weekly earn and spend rates by source and sink. This waits on a shared telemetry facility; the reroll state already retains per-period count and Fate Echo spend for future export.

## Final Recommendation

Keep **Fate Echo** because it can make Prophecy selection more forgiving and expressive. Keep **Sigil Fragments** only because deterministic dungeon selection is a distinct and useful counterweight to random full-sigil drops. Remove **Ascension Stone Fragments** because Monster Cores and Soul Dust already provide the complete Essence progression language.

This is a smaller and clearer economy than either extreme: it does not discard every thematic currency, and it does not invent three shops to defend three unfinished fields.
