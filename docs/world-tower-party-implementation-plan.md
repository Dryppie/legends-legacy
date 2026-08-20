# World Tower Expedition Parties Implementation Plan

> Implementation status: completed in the current working tree. Migration `20260819225156_AddWorldTowerParties` has been generated but not applied.

## Objective

Introduce parties within World Tower Expeditions while preserving the existing combat-team model.

- A party contains at most five players.
- A floor with five slots has one party.
- A floor with ten slots has two parties.
- Party count is calculated as `ceil(requiredSlots / 5)` so the rule also covers the existing three- and fifteen-slot floors.
- Players may only use abilities on allies in their own party.
- Enemy abilities using `AbilityTargetSelector.AllEnemies` continue to hit every player, regardless of party.
- Basic attacks, threat, victory conditions, and enemy targeting continue to work normally across the full encounter.

## Recommended Product Behavior

- New participants enter the Bench with no party slot.
- The Expedition leader can rearrange party membership before combat starts.
- The leader can click a benched participant and then a party/open slot, or drag the participant onto an open slot.
- The layout exposes exact numbered slots, with slots 1-5 in Party 1, 6-10 in Party 2, and so on.
- Party assignments are locked when the attempt starts.
- Summons inherit their owner's party.
- Party membership is visible to all Expedition members.
- Existing combat modes remain unchanged because party identity is optional and only populated by party-aware encounters.

Leader-controlled rearrangement is recommended because otherwise acceptance order would determine strategically important healing and support groups.

## 1. Add Party Rules and Persisted Assignments

Add a small domain rule, such as `WorldTowerPartyRules`, containing:

- `MaximumPartySize = 5`
- `GetPartyCount(requiredSlots)`
- Validation for party numbers and capacities

Add nullable `PartySlot` to `TowerRallyParticipant` in `LL/src/Core/Domain/Models/WorldTower/WorldTowerModels.cs`. A null slot means the participant is benched; party number is derived from the slot.

Assignments should follow these rules:

- The Expedition leader starts in Party 1.
- Accepted applications and development-roster participants enter the Bench.
- Roster and combat ordering use `PartySlot`, then `JoinedAt`, then participant ID for deterministic results.

Before an Expedition starts, validate that:

- Every participant occupies one unique valid slot and no participant remains benched.
- A party can contain no more than its five exact slots.
- Each slot is between 1 and `RequiredSlots`.
- Every Expedition slot is filled, preserving the existing start requirement.

## 2. Add Party Management to the Tower API

Use one atomic operation to save the complete party layout. Individual move commands cannot safely reorganize two full five-player parties without temporarily exceeding capacity.

Suggested endpoint:

```text
PUT /world-tower/rallies/{rallyId}/parties
```

The request should contain every current participant and the desired nullable party slot. Validate that:

- Only the Expedition leader can update the layout.
- The rally status is `Recruiting` or `Ready`.
- Every current participant appears exactly once.
- The request contains no unknown or duplicate characters.
- Every assigned slot is valid and unique; null is accepted as the Bench.

Use the existing World Tower floor command lock and publish the existing rally-updated realtime event with an event name such as `PartiesUpdated`.

Expected command and API changes:

- `LL/src/Core/Application/UseCases/WorldTower/WorldTowerRequests.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/WorldTower/IWorldTowerService.cs`
- `LL/src/API/API.LL/Controllers/V1/WorldTowerController.cs`
- `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`

## 3. Extend the API Contract

Add the following fields:

- `partyCount` and `maximumPartySize` to `TowerRallyDto`
- `partySlot` and derived `partyNumber` to `TowerRallyParticipantDto`
- `canManageParties` to `TowerRallyDto`
- Party number to new battle-report participant records if reports should retain the combat grouping

Applications do not need a party number until they are accepted.

Update the matching TypeScript contracts and API call in:

- `LL/src/Presentation/ll/src/app/core/services/api/world-tower/world-tower.service.ts`

## 4. Carry Party Identity Into Combat

Extend `CombatParticipantSlot` with an optional party number in:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/CombatParticipantSlot.cs`

Then:

1. Pass each persisted party number when `WorldTowerService.ResolveCombatAsync` creates friendly participant slots.
2. Transfer the value from the participant slot into `RuntimeCombatant` in `CombatEngineExecutor`.
3. Store the optional party number on `RuntimeCombatant`.
4. Make summoned combatants inherit the summoner's party number.
5. Leave participant party numbers unset in existing non-Tower combat modes.

Expected combat-runtime changes:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`

## 5. Enforce Party-Scoped Allied Effects

Introduce a centralized helper equivalent to:

```text
CanAffectAsAlly(source, candidate):
  candidate is on the same team as source
  and either source has no party scope
      or candidate has the same party number
```

Use it for all explicitly allied selectors:

- `AllAllies`
- `TwoAllies`
- `RandomAlly`
- `LowestHealthAlly`
- `HighestMaxHealthAlly`
- `SummonedAllies`
- `NonSummonedAllies`

Also apply a final target guard after selector resolution. This prevents indirect selectors such as `EventSource`, `EventTarget`, or `EveryoneButSelf` from affecting a same-team member in another party.

Party scope should also apply wherever an ally contributes to ability behavior, including:

- `EventSourceIsAlly`
- `LivingNonSummonedAllyDamagePercent`
- Future ally-count and ally-event mechanics

Do not change enemy matching. In particular, the following rule remains team-based:

```text
AllEnemies => candidate.Team != source.Team
```

As a result, a Guardian AoE continues to hit Party 1, Party 2, and Party 3.

## 6. Update the Rally UI

Update the Tower rally page to render separate party sections, for example:

```text
Party 1 - 5/5
Party 2 - 4/5
```

For Expedition leaders before combat:

- Display new participants in a Bench above the party sections.
- Allow click-then-place and native drag-and-drop into exact open slots.
- Save every completed move as a complete atomic layout.
- Provide `Distribute all`, `Auto-balance parties`, and `Reset parties` leader actions.
- Disable or explain invalid layouts.
- Keep leadership transfer independent from party assignment.

For other users:

- Display party membership read-only.
- Group open slots under the appropriate party.

Expected frontend changes:

- `LL/src/Presentation/ll/src/app/features/game/world/tower/rally/tower-rally.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/world/tower/rally/tower-rally.component.html`
- `LL/src/Presentation/ll/src/app/features/game/world/tower/tower-page.scss`
- `LL/src/Presentation/ll/src/app/core/services/api/world-tower/world-tower.service.ts`

Battle reports should preferably retain and display party labels. Grouping the live combat playback can be deferred: adding party metadata to the compact playback bundle would require playback-schema compatibility work and is not required to implement the combat rule.

## 7. Add the EF Core Migration

Update the World Tower participant configuration and generate an EF Core migration.

Expected persistence changes:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/WorldTower/WorldTowerConfigurations.cs`
- A new migration under `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations`
- `LLDbContextModelSnapshot.cs`

Compatibility behavior:

- Keep `PartySlot` nullable because null is the intentional Bench state.
- Give the creator slot 1 and bench every later participant until the leader allocates them.
- Leave existing participants benched after migration so a leader must confirm their strategic layout.
- Add a check constraint allowing either `null` or a positive party slot.
- Add an index on `(TowerRallyId, PartySlot)`; uniqueness remains a domain validation so full-party rebalancing can swap occupied slots in one atomic save.

The migration may be generated in this repository but must not be applied to a shared or production database.

## 8. Keep World Tower Balance Analysis Representative

Party-limited healing, barriers, and buffs will materially change ten- and fifteen-player encounters.

Update `WorldTowerBalanceAnalyzer` so simulated participants receive the same five-player grouping as production combat. Otherwise balance reports would continue measuring the obsolete all-roster support behavior.

Expected balance changes:

- `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerBalanceAnalyzer.cs`
- Supporting simulation-runner changes that allow optional friendly party assignments
- `LL/tests/EssenceSystem.Tests/WorldTowerBalanceAnalyzerTests.cs`

After implementation, reassess:

- Warden and Sovereign win rates
- Average survivors
- Guardian scaling
- Recommended Power Ratings

This feature may require balance-data adjustments even though no authored Guardian ability needs to change.

## Test Coverage

### Backend

Add tests for:

- Party counts for three, five, ten, and fifteen slots.
- New-participant bench placement and explicit leader allocation.
- Leader-only, pre-start party editing.
- Rejection of missing, duplicate, unknown, out-of-range, or over-capacity layouts.
- Start rejection for invalid persisted assignments.
- Every allied selector remaining within the source's party.
- Summons inheriting party scope.
- Ally-event conditions and ally-count scaling being party-local.
- Enemy `AllEnemies` effects hitting players in every party.
- Unscoped non-Tower combat retaining existing team-wide behavior.
- World Tower balance simulations using production-equivalent parties.
- Controller dispatch for the new endpoint.

Primary test files:

- `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`
- `LL/tests/EssenceSystem.Tests/WorldTowerServiceTests.cs`
- `LL/tests/EssenceSystem.Tests/WorldTowerControllerTests.cs`
- `LL/tests/EssenceSystem.Tests/WorldTowerTests.cs`
- `LL/tests/EssenceSystem.Tests/WorldTowerBalanceAnalyzerTests.cs`

### Frontend

Add tests for:

- One group for a three- or five-slot floor.
- Two groups for a ten-slot floor.
- Three groups for a fifteen-slot floor.
- Leader editing and save behavior.
- Read-only grouping for non-leaders.
- Realtime refresh after party changes.

Add or extend a rally component specification alongside the rally component and update the World Tower API service specification.

## Verification

Run the following after implementation:

1. Backend correctness suite:

   ```powershell
   ./build/run-tests.ps1
   ```

2. Angular tests from `LL/src/Presentation/ll`, using an npm cache beneath `%TEMP%`:

   ```powershell
   npm run test:ci
   ```

3. Angular development build:

   ```powershell
   npm run build:development
   ```

4. A targeted World Tower balance run for the ten- and fifteen-slot floors.

## Deployment Implications

- An EF Core migration is required.
- The migration must be deployed before code that writes party assignments.
- Existing rows migrate with a null `PartySlot`; unfinished rallies require leader allocation before they can start.
- No infrastructure-as-code changes are required.
- No external service deployment should be performed from this repository.

## Acceptance Criteria

The feature is complete when:

- Every new Tower Expedition exposes the correct number of parties.
- No party can contain more than five players.
- Leaders can arrange parties before starting an attempt.
- Party assignments cannot change after combat starts.
- A player's allied abilities cannot affect members of another party.
- Summoned allies obey their owner's party scope.
- Guardian and other enemy `AllEnemies` abilities affect all player parties.
- Non-Tower combat behavior remains unchanged.
- Production and balance-simulation party behavior match.
- Backend tests, frontend tests, and the frontend build pass.
