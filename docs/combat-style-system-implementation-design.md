# Combat Style System Implementation Design

## Purpose

Combat Styles are a build-shaping layer that determines how a character fights while Essences remain the primary source of abilities. The current implementation adds the foundation for one active Combat Style per character, persisted style progression, row/lane skill-tree investment, dungeon snapshotting, runtime combat resources, combat rule integration, Essence ability mutators, API endpoints, and an Angular management page.

The system intentionally preserves these design rules:

- Combat Styles do not grant active abilities.
- Combat Styles do not restrict Essence equipment.
- Only one Combat Style can be active.
- Combat Style resources are runtime-only and reset each encounter.
- Dungeon combat uses the style snapshot captured at dungeon entry.
- Idle combat, dungeon combat, PvP, and future shared-engine battles can carry Combat Style snapshots into encounter resolution.
- Summoner style improves summon-related Essence behavior but does not create summons by itself.

## Implemented Scope

### Backend Foundation

The implementation adds `PlayerCombatStyle` as character-owned persisted progression:

- `CharacterId`
- `StyleId`
- `Level`
- `Experience`
- `SelectedFocusId`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

Persistence is wired through `IDbContext`, `LLDbContext`, EF configuration, and the `AddCombatStyles` migration. The database model enforces:

- One row per character/style.
- One active style per character through a filtered PostgreSQL unique index.
- Level range from 1 to 50.
- Non-negative experience.

### Data-Driven Definitions

Combat Style definitions are JSON-backed game data via `ICombatStyleDefinitionProvider`, implemented by `JsonCombatStyleDefinitionProvider`.

The current styles are:

- Defensive
- Fighter
- Caster
- Summoner
- Swift
- Marksman
- Support
- Controller

Each definition includes:

- Style identity and description.
- Resource ID.
- Max level.
- Recommended tags.
- Recommended stats.
- Core mechanic text.
- Row/lane skill-tree nodes.
- Mutator metadata for eligible tree nodes.
- Structured node tooltip content.
- Rule metadata.

Definitions are centralized in `LL/src/API/API.LL/Data/combat-styles/*.json` so balance values, tree layout, node text, and future styles can be tuned without touching controllers or UI code.

### Service Layer

`ICombatStyleService` provides the backend feature surface:

- List all styles with character progress.
- Seed missing player style rows.
- Default Fighter as active style when no active style exists.
- Activate a style.
- Select a focus path.
- Rank up skill-tree nodes.
- Reset the skill tree.
- Validate focus unlock level.
- Generate an active style snapshot.
- Generate a build preview from equipped Essence tags.
- Grant Combat Style XP.

`ICombatStyleSwitchValidator` currently blocks switching during active dungeon runs.

### API

`CombatStylesController` exposes:

- `GET /api/v1/combat-styles`
- `POST /api/v1/combat-styles/{styleId}/activate`
- `POST /api/v1/combat-styles/{styleId}/focus/{focusId}/select`
- `GET /api/v1/combat-styles/build-preview`

The API uses the existing MediatR command/query pattern and `Response<T>` wrapper for mutations.

API boundary tests cover:

- Route metadata for all Combat Style endpoints.
- Overview query dispatch with the authenticated character ID.
- Activation command dispatch and successful mutation response.
- Activation failure response for active-dungeon switch blocking.
- Focus selection command dispatch and updated style response.
- Build preview query dispatch and response.

### Dungeon Snapshotting

`CombatStyleSnapshot` is stored on `DungeonRunState` when a run is created. This snapshot includes:

- Style ID and name.
- Style level and experience.
- Selected focus ID and name.

Dungeon combat orchestration passes the snapshotted style into the combat encounter plan. This prevents outside-run style changes from affecting an active dungeon run.

Idle combat fetches the character's active style once per idle processing batch and attaches it to every generated idle encounter plan. PvP combat fetches both attacker and defender active style snapshots; the attacker style applies to the friendly side and the defender style applies to the hostile side. The shared combat executor also has a character-only fallback that can resolve missing side snapshots from `Character` participants, so future battle modes that use the shared runtime get Combat Style support without creating style rows for creature participants.

### Combat Runtime Integration

Combat Style runtime state is encounter-local. The combat engine creates `CombatStyleRuntimeState` instances from encounter plan snapshots and never persists resources.

Runtime state tracks:

- Style ID.
- Style level.
- Focus ID.
- Ranked tree node IDs and ranks.
- The combat side the style belongs to.
- Resource values.
- Trigger counts.
- Pending empowerments.

Combat hooks currently integrated:

- Active ability resolution.
- Effect amount calculation.
- Damage dealt.
- Damage taken.
- Summon attribute creation.

The runtime is side-aware. A style state treats the non-summoned combatant on its own side as the player, so the same rules work for friendly-side PvE, hostile-side PvP defenders, and future combat modes that set or can infer the appropriate encounter-plan snapshots.

`ProcCoefficient` was added to ability effects and authored across the JSON ability/status catalog. Coefficients keep repeated, reactive, multi-target, and status-driven effects from counting as full-strength direct hits for Combat Style resource and empowerment math.

### Rule Engine and Trigger Caps

`CombatStyleRuleEngine` now acts as a facade over reusable rule-definition interpretation for the common combat hooks:

- Active ability resolution.
- Effect amount calculation.
- Damage dealt.
- Damage taken.
- Summon attribute calculation.

Generic rule execution currently supports:

- Effect amount modification.
- Incoming damage reduction.
- Style resource gain.
- Pending empowerment setup.
- Encounter-level trigger caps.
- Source-level trigger caps.
- Target-level trigger caps.
- Summon stat modification.

Rule selection gathers base style rules, ranked node rules, selected focus/build-identity rules, and resource-overflow rules. Style-specific behavior should live in JSON definitions plus reusable operations rather than style identity branches in the engine.

### Essence Ability Mutators

`CombatStyleAbilityMutatorResolver` applies selected tree mutators before ability compilation.

The resolver:

- Gathers ranked combat-style nodes with mutator definitions.
- Applies at most one mutator per mutator group.
- Checks ability metadata, delivery tags, effect tags, targeting, damage type, operation type, and conversion flags.
- Applies transforms such as tag additions, damage type changes, scaling changes, cooldown/resource multipliers, potency multipliers, and tradeoffs.
- Leaves ineligible abilities unchanged.

This keeps Combat Styles as a build-shaping layer over equipped Essence abilities instead of giving styles their own active ability list.

### Balance Simulation

`ICombatStyleBalanceSimulator` provides deterministic style-vs-style simulations for tuning. It builds a synthetic shared ability suite, creates base-style and focus-path candidates from static definitions, runs round-robin encounters through `FastCombatEngine`, and returns ranked style/focus results with battle summaries.

The Admin Dashboard diagnostics API exposes this through:

- `POST /api/v1/diagnostics/combat-style-balance-simulation`

The simulator is intended as a balance report generator, not as a final authority on tuning. It gives repeatable signals for win rate, duration, and focus spread so manual balancing can be guided by combat output instead of intuition alone.

### Implemented Combat Rules

Defensive:

- Reduces incoming player damage by 5%.
- Gains Guard when the player takes damage.
- At 100 Guard, applies Protective Shell.
- Protective Shell grants a max-health-based barrier.
- Protective Shell is capped at 2 triggers per encounter.
- Player barrier effects receive a bonus.
- Bulwark strengthens Protective Shell/barrier identity.
- Counterguard empowers the next active damage effect after taking damage.
- Commander reinforces owned summons and gains Guard when they are struck.
- Level 25 and 40 milestones scale Bulwark barrier/shell strength, Counterguard retaliation, and Commander summon support.

Fighter:

- Gains Momentum from player direct damage.
- At 100 Momentum, empowers the next active damage effect.
- Duelist empowers active single-target damage.
- Berserker empowers active damage while the player is at half health or lower.
- Vanguard reduces incoming direct damage and gains Momentum when hit.
- Level 25 and 40 milestones scale Duelist precision damage, Berserker wounded damage, and Vanguard durability/Momentum.

Caster:

- Gains Arcane Charge from active Essence ability use.
- At 5 Arcane Charge, empowers the next active effect.
- Arcanist amplifies active Magic/Spell effects.
- Spellblade focus adds Power-scaling magical bonus damage to Melee-tagged active damage.
- Occultist amplifies Curse/DoT active effects and builds extra Arcane Charge from Curse/DoT active abilities.
- Level 25 and 40 milestones scale Arcanist amplification/Arcane Charge, Spellblade conversion, and Occultist amplification.

Summoner:

- Gains Command from Summon-tagged active abilities.
- At 100 Command, empowers the next Summon-tagged effect.
- Improves summon attributes.
- Does not create summons by itself.
- Horde leans owned summons toward power.
- Champion leans owned summons toward health.
- Ritualist further amplifies Curse/Holy summon effects.
- Level 25 and 40 milestones scale Horde summon power, Champion summon health, and Ritualist Curse/Holy summon amplification.

Swift:

- Gains Flow from active Essence ability use.
- At 100 Flow, empowers the next active effect.
- Active Essence effects receive a small baseline bonus.
- Flurry amplifies active Melee and Ranged damage.
- Evasion reduces incoming direct damage.
- Tempo builds extra Flow and strengthens Flow empowerment.
- Level 25 and 40 milestones scale Flurry pressure, Evasion mitigation, and Tempo empowerment.

Marksman:

- Gains Aim from ranged damage dealt by the player.
- At 100 Aim, empowers the next ranged damage effect.
- Ranged damage receives a baseline bonus.
- Sniper amplifies ranged single-target damage.
- Volley amplifies ranged multi-target damage.
- Trapper improves ranged status/debuff setup and Aim generation.
- Level 25 and 40 milestones scale Sniper precision, Volley spread pressure, and Trapper setup value.

Support:

- Gains Resolve from active healing and barrier effects.
- At 100 Resolve, empowers the next heal or barrier effect.
- Healing and barrier effects receive a baseline bonus.
- Healer amplifies healing effects.
- Warden amplifies barrier effects and adds modest incoming direct damage protection.
- Chaplain amplifies Holy healing, barriers, and damage.
- Level 25 and 40 milestones scale Healer restoration, Warden protection, and Chaplain Holy hybrid support.

Controller:

- Gains Control from active status and debuff effects.
- At 100 Control, empowers the next status, debuff, Curse/DoT, or damage setup effect.
- Status and debuff effects receive a baseline bonus.
- Hexer amplifies Curse and DoT effects.
- Tactician amplifies Debuff and Physical setup effects.
- Frostbinder amplifies Control/Freeze/Stun effects and related follow-up damage.
- Level 25 and 40 milestones scale Hexer attrition, Tactician setup pressure, and Frostbinder hard-control identity.

### Style XP Rewards

Combat Style XP now follows existing combat XP timing:

- Idle combat grants Combat Style XP immediately alongside character combat XP.
- Dungeon combat grants Combat Style XP when claimable dungeon XP is claimed.
- Dungeon withdrawals use the same secured-loot claim path, so style XP follows secured dungeon XP rather than unsecured pending rewards.
- XP is granted only to the currently active style and still respects the level 50 cap.

### Build Preview

Build preview is display-only and grants no stats. It uses equipped Essence tags from the active loadout.

The current preview includes:

- Active style.
- Selected focus.
- Build identity name.
- Top equipped Essence tags.
- Recommended stats.
- Notes about current synergies.

### Frontend

The Angular implementation adds:

- Combat Style API service.
- Signal-based Combat Style state service.
- Combat Styles page under Character.
- Sidebar navigation entry.
- Current dungeon card display for snapshotted style/focus.

The Combat Styles page displays:

- Active style and selected build identity/focus signal.
- Style level and XP progress.
- Resource ID.
- Core mechanic.
- Build identity.
- Top Essence tags.
- Recommended tags and stats.
- A one-style-at-a-time management view.
- A unified row/lane skill-tree map for redesigned combat-style trees.
- Legacy branch rendering as a fallback for older tree data.
- Selected node details with exact effects, mutator groups, affected ability categories, changes, tradeoffs, and non-affected cases.
- Activate, rank-up node, and reset tree actions.
- Backend validation errors, including active dungeon switching failures.

The skill-tree UI follows the shared frontend design system:

- Shared `--ll-*` colors, radii, shadows, and display font tokens.
- Texture-backed dark panels.
- Compact bordered node controls.
- Restrained gold accents for selected, ranked, and available states.
- Subtle tree connectors that sit behind the content.

## Partially Implemented Areas

### Rule Definition Engine

The runtime now interprets reusable rule definitions for baseline effect modifiers, damage reductions, resource gains, pending empowerments, and trigger caps.

Remaining partial coverage:

- Some focus and milestone behavior is still implemented directly in C# because it depends on selected focus, style level bands, or multi-step style-resource behavior.
- `AddBonusDamageFromStatOperation` is still handled by bespoke focus logic. The current operation metadata does not yet express focus ownership cleanly enough to run it generically without over-applying Spellblade-style bonuses.
- Generic rule-trigger logging is not yet exposed for debugging or admin analysis.

### Trigger Caps

Generic rule definitions support encounter, source, and target trigger caps through runtime trigger counters. Protective Shell and future capped rules can use the same trigger-counting path.

Remaining partial coverage:

- Trigger counts are runtime-only and not surfaced in combat logs yet.
- Cap behavior is covered by focused unit tests, but broader balance validation still depends on simulation output.

### Focus Paths

All current styles now have level 10, level 25, and level 40 focus behavior.

The current milestone implementation is intentionally numeric/scaling-focused rather than adding new ability-like behavior at each tier. Later tuning can add more expressive milestone mechanics if simulations show the identities need sharper differentiation.

### Proc Coefficient Balance Depth

The JSON ability/status catalog now has explicit `ProcCoefficient` values on all authored effects. The current pass uses broad balance buckets:

- Full single-target active hits generally remain `1.0`.
- AoE and two-target effects are reduced.
- Status riders, buffs, debuffs, summons, and resource effects are reduced.
- DoT ticks and reactive damage are reduced further.

Future tuning should validate these coefficients through the combat style balance simulator and per-build balance reports rather than treating the first pass as final.

### Balance Simulator Interpretation

The balance simulator now exists and is available through the Admin Dashboard diagnostics API. What remains is the tuning workflow around it:

- Define acceptable win-rate and matchup-duration bands.
- Run reports after each major style or ability tuning pass.
- Convert outlier reports into concrete rule, coefficient, or focus-path adjustments.
- Add scenario-specific suites when real encounter telemetry shows gaps in the synthetic ability suite.

### Dungeon UI Integration

Active dungeon state displays the snapshotted style/focus. Dungeon access/reward preview cards do not yet include active style, build identity, or dungeon-specific style recommendations.

## Not Yet Implemented

### Full Combat Rule Coverage

Not yet implemented:

- Summon action Command generation.
- Owned summon action tracking.
- Deeper encounter AI usage of Support/Controller identity beyond effect math and build preview.
- Full simulation-driven proc-coefficient balancing decisions.

### Additional Switching Validators

The MVP blocks active dungeon runs only. Later validators should include:

- Active PvP matches.
- Active raids.
- Currently resolving combat sessions, if represented separately.

### Frontend Visual QA

Angular build verification passes. Browser-level route smoke checking was performed against a temporary local Angular dev server, but the isolated browser session did not have signed-in game state, so final in-game visual approval still needs a signed-in session.

## Runtime Flow

### Style Activation

1. Client calls activate endpoint.
2. Service validates style exists.
3. Switch validator checks whether style switching is allowed.
4. Missing style rows are created if needed.
5. Existing active style is deactivated.
6. Requested style becomes active.
7. Changes are saved.

### Focus Selection

1. Client calls select focus endpoint.
2. Service validates style exists.
3. Service validates focus exists and belongs to the style.
4. Service checks player style level against focus unlock level.
5. Selected focus is persisted.

### Dungeon Entry

1. Dungeon run creation requests active Combat Style snapshot.
2. Snapshot is stored in `DungeonRunState`.
3. Future room resolution reads the run snapshot, not current player style.

### Combat Resolution

1. Combat orchestration places available style snapshots on `CombatEncounterPlan`.
2. Dungeon plans use the stored dungeon-entry snapshot.
3. Idle plans use the current active style snapshot for the idle processing batch.
4. PvP plans use the attacker's active style as the friendly style and the defender's active style as the hostile style.
5. Combat engine creates side-aware runtime style states from those snapshots.
6. Runtime state initializes style resources at zero.
7. Combat hooks mutate calculation values and resource state for the matching side.
8. Resources and pending empowerments disappear when the encounter ends.

## Key Files

Backend:

- `LL/src/Core/Domain/Models/CombatStyles/`
- `LL/src/Core/Application/Interfaces/Services/LL/CombatStyles/`
- `LL/src/Core/Application/UseCases/CombatStyles/`
- `LL/src/Core/Application/UseCases/_AdminDashboard/Diagnostics/Queries/RunCombatStyleBalanceSimulation/`
- `LL/src/API/API.LL/Data/combat-styles/`
- `LL/src/Infrastructure/Service/Services.LL/CombatStyles/`
- `LL/src/API/API.LL/Controllers/V1/CombatStylesController.cs`
- `LL/src/API/API.AdminDashboard/Controllers/V1/DiagnosticsController.cs`

Persistence:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/CombatStyles/PlayerCombatStyleConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260627110011_AddCombatStyles.cs`

Combat:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityCompiler.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs`
- `LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs`

Dungeon:

- `LL/src/Core/Domain/Models/Dungeons/Runs/DungeonRunState.cs`
- `LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunFactory.cs`
- `LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs`

Frontend:

- `LL/src/Presentation/ll/src/app/core/services/api/combat-styles/`
- `LL/src/Presentation/ll/src/app/features/game/character/combat-styles/`
- `LL/src/Presentation/ll/src/app/shared/models/combat-style.ts`

Tests:

- `LL/tests/EssenceSystem.Tests/CombatStyleSystemTests.cs`
- `LL/tests/EssenceSystem.Tests/CombatStylesControllerTests.cs`
- `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`
- `LL/tests/EssenceSystem.Tests/DungeonRogueliteStateTests.cs`
- `LL/tests/EssenceSystem.Tests/IdleCombatPlannerTests.cs`

## Verification

Completed:

- `dotnet build LL\LegendsLegacy.sln`
- `dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj`
- `dotnet ef migrations add AddCombatStyles --project LL\src\Infrastructure\Persistence\Persistence.LL --startup-project LL\src\API\API.LL`
- Angular build through direct system npm CLI invocation after `npm ci`.
- `LL\src\Presentation\ll\node_modules\.bin\ng.cmd build --configuration development`

Notes:

- The normal `npm` shim points to a missing user-level npm installation, so frontend verification used the system npm CLI through `node`.
- The Angular build emits an existing initial bundle budget warning.
- `npm ci` reported existing dependency audit findings; dependencies were not upgraded as part of this feature.
- The latest frontend verification used the local Angular CLI binary directly because the user-level `npm` shim is still broken.

## Migration and Deployment Notes

The migration creates the `PlayerCombatStyles` table. It has not been applied to any database.

Deployment implications:

- Database migration must be applied before enabling the feature in an environment.
- Existing characters will receive missing style progress rows lazily when Combat Styles are first loaded or when an active snapshot is requested.
- If static style definitions are renamed or removed later, existing persisted `StyleId` and `SelectedFocusId` values need a fallback/migration strategy.

## Future Work

Recommended next steps:

1. Use the Admin Dashboard balance simulator to establish acceptable win-rate and duration bands, then tune outliers.
2. Add dungeon preview build identity display.
3. Add generic rule-trigger logging so capped rules and resource gains can be inspected from combat reports.
4. Add browser-level visual QA for the Combat Styles page.
5. Add richer Support and Controller encounter-facing mechanics once healing, barriers, and status-control telemetry are available.
