# Combat System Consistency Analysis

## Overall assessment

All combat types ultimately use the same `FastCombatEngine`, so damage, abilities, statuses, threat, targeting, stagger processing, and tick resolution are fundamentally shared.

The inconsistency is before and after the engine:

- Combatants are reconstructed differently.
- Snapshot guarantees differ.
- Random seeds and retry behavior differ.
- Mode metadata does not represent the real content type.
- Battle outcomes and final-state DTOs are finalized differently.

This creates several real cases where identical character data would not produce identical combatants.

## P1 implementation status

The P1 recommendations in this review were implemented on 2026-08-26:

- Dungeon combat now reconstructs the player exclusively through the canonical snapshot builder before applying run-specific modifiers.
- Arena defense snapshots and both Tournament teams now use the same canonical snapshot builder, including weapon-slot and blueprint reconstruction.
- Region Boss snapshots are persisted when matchmaking closes and reused by worker resolution and retries. A legacy safeguard creates a missing snapshot when a run is first claimed.
- Tournament battle IDs and random seeds are deterministically derived from the tournament and match IDs.
- Character snapshots now retain `ImagePath` so reconstructed source entities preserve their presentation identity.

## P2 implementation status

The P2 recommendations in this review were implemented on 2026-08-26:

- Every encounter plan now requires an exhaustive `CombatContentType`, while `CombatMode` remains the broader engine-mechanics category. Runtime validation prevents mismatched combinations.
- Content identity now controls essence activity explicitly, so Arena and Tournament, Raid and World Tower, and Idle and Quest Training no longer collapse into the same preparation activity by accident.
- `CombatResult` retains an immutable `EngineOutcome` and a separately evaluated `ContentOutcome`; the legacy `Outcome` contract remains a content-outcome alias for API and stored-replay compatibility.
- Raid objective and survival rules now use a shared objective evaluator instead of overwriting the engine outcome.
- Every engine execution path now authors the final teams, including threat, party number, reinforcement waves, and dynamic waves. Result finalization passes those teams through without rebuilding them.

## P3 implementation status

The P3 recommendations in this review were implemented on 2026-08-26:

- Every production battle entry point now constructs live and snapshot combatants through one `ICombatPreparationPipeline`.
- The pipeline preserves request order, enforces stable runtime/source identity, requires explicit creature scaling context, maps the exact content type to its essence activity, performs one final preparation pass, and validates health after content-specific hooks.
- Content-specific differences remain explicit hooks around shared preparation. Dungeon and Raid scaling, World Tower bonuses and stagger, Region Boss wave scaling, and Quest Training's forced health therefore do not fork the base preparation algorithm.
- Engine execution settings now use one immutable `CombatRuleset`, including standard duration, overtime, stagger-adjacent encounter behavior, downed/revival, wave recovery, hostile fury, supplemental abilities, and event-log policy.
- The unused, partially unsupported generic loader/combatant/runtime factory chain was removed after all production paths had migrated to the replacement pipeline.

## Combat types reviewed

| Combat type | Character source | Special rules |
| --- | --- | --- |
| Idle | Live character; prepared templates cloned per encounter | Stable offline seed and cadence |
| Dungeon | Canonically reconstructed run snapshot | Run/enemy modifiers |
| Arena | Live attacker; canonically reconstructed defender snapshot or live fallback | Standard 6,000-tick limit |
| Tournament | Canonically reconstructed snapshots for both teams | Regulation plus overtime |
| Raid | Canonically rehydrated snapshots | Lanes, waves, final-boss stagger and overtime |
| World Tower | Canonically rehydrated snapshots | Party bonuses and guardian stagger |
| Region Boss | Canonically reconstructed matchmaking snapshots | Downed/revival, endless boss waves, recovery and fury |
| Quest training | Live entities | Idle loadout, forced 10-health enemy |

## Prioritized findings

### P1 — Dungeon snapshots did not actually freeze the combat build (implemented)

Before this implementation, dungeon runs carried a `CharacterSnapshot`, but combatant construction started from the current live character and only replaced `EquippedEssences`. Snapshot base attributes, level, and equipment were not restored.

See [`DungeonCombatResolutionSessionFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/Dungeon/DungeonCombatResolutionSessionFactory.cs), especially `BuildFriendlyTemplates`.

Former consequences:

- Equipment changes after starting a dungeon affect later rooms.
- Level or permanent-attribute changes can affect the active run.
- Essences remain frozen while gear and stats remain live, creating a hybrid build that never existed.
- Raid and World Tower do not behave this way because they use the canonical snapshot builder.

Implemented solution: the dungeon player is built through `ISnapshotCombatantBuilder`, then dungeon run attribute and ability modifiers are applied before final preparation.

### P1 — Arena and tournament snapshots could use the current weapon's behavior (implemented)

Both Colosseum implementations previously copied snapshot equipment into `CombatEntity.Equipment`, but did not reconstruct `MainHandEquipment` or `OffHandEquipment`. They also omitted `BlueprintId`.

- Arena reconstruction: [`ColosseumService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs)
- Tournament reconstruction: [`TournamentGroundsService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs)

The combat engine derives attack-speed multiplier, damage multiplier, range, and damage type from `MainHandEquipment`, including its blueprint, in [`CombatEngineExecutor.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs).

This meant a snapshot could use:

- Snapshotted equipment stats and set bonuses.
- Current live main-hand weapon behavior.
- Current live blueprint behavior.

If the current character had no weapon but the snapshot did, basic attacks fell back to default behavior.

Implemented solution: both bespoke reconstruction methods were removed in favor of [`SnapshotCombatantBuilder.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/SnapshotCombatantBuilder.cs), and the shared snapshot now includes display-image handling.

### P1 — Region Boss did not snapshot participants (implemented)

Region Boss previously stored only character IDs and power ratings, while the resolver reloaded live characters when the background worker resolved the run in [`RegionBossCombatResolver.cs`](../../LL/src/Infrastructure/Service/Services.LL/RegionBosses/RegionBossCombatResolver.cs).

Former consequences:

- Players can change equipment or essences between signup and resolution.
- A retry after a simulation failure can use a different build.
- The fixed random seed does not make the battle reproducible if the input combatants change.
- This differs from Raid, World Tower, Dungeon, Arena defense, and Tournament expectations.

Implemented solution: an activity-specific `CharacterSnapshot` is created at matchmaking lock and attached to each `RegionBossSignup`. Resolution exclusively uses those snapshots, with a first-claim safeguard for pre-migration runs.

### P1 — Tournament retries could reroll combat (implemented)

The tournament previously received a stable `matchId`, but created a new random `battleId` and used it as `EncounterId` in [`TournamentGroundsService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs).

When no explicit seed is provided, the engine derives its seed from `EncounterId` in [`CombatEngineExecutor.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs).

A worker retry before persistence could therefore resolve the same match differently.

Implemented solution: battle identity and seed are deterministically derived from a versioned identity string plus tournament ID and match ID.

### P2 — `CombatMode` cannot represent the actual combat types (implemented)

`CombatMode` contains only Idle, Dungeon, Raid, PvP, and RegionBoss because it describes broad engine mechanics rather than the exact content identity.

Before this implementation:

- World Tower declares itself `CombatMode.Raid` in [`WorldTowerCombatRuntimeFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerCombatRuntimeFactory.cs).
- Training declares itself `CombatMode.Idle` in [`QuestEncounterService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Quests/QuestEncounterService.cs).
- Arena and Tournament both declare `PvP`, despite using different essence activities and time rules.
- The removed legacy `CombatEncounterRuntimeFactory` mapped every PvP encounter to Arena, not Tournament.

Callers avoided some failures by bypassing that factory and passing activities manually, but the type system did not protect future callers.

Implemented solution: [`CombatEncounterPlan.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/CombatEncounterPlan.cs) now requires an exhaustive `CombatContentType`. Its mapping to broad engine mode and essence activity is centralized in [`CombatMode.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/CombatMode.cs), and [`CombatEncounterRuntime.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/Models/CombatEncounterRuntime.cs) rejects incompatible content/mode combinations.

### P2 — Outcome semantics are inconsistent (implemented)

The engine defines only kill-based Victory, Defeat, or Draw in [`FastCombatEngine.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs).

Before this implementation, different modes interpreted it differently:

- Raid Vanguard mutates the result to Victory when the guardian objective is completed even if other enemies remain.
- Raid Main Guard mutates it based on survival thresholds in [`RaidCombatResolver.cs`](../../LL/src/Infrastructure/Service/Services.LL/Raids/RaidCombatResolver.cs).
- Region Boss leaves the engine outcome intact but exposes a separate termination reason.
- World Tower treats anything other than engine Victory as failure.
- Arena and Tournament retain Draw.

These differences can be valid, but mutating the shared `CombatResult.Outcome` hides whether the value means “team extermination” or “content objective completed.”

Implemented solution: [`CombatResult.cs`](../../LL/src/Core/Domain/Models/Combat/CombatResult.cs) retains separate engine and content outcomes while keeping `Outcome` as the backward-compatible content-result alias. [`CombatObjectiveEvaluator.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatObjectiveEvaluator.cs) evaluates elimination, objective-completion, and survival policies, and Raid now uses it for Vanguard and Main Guard resolution.

### P2 — Final combat results are built through two different paths (implemented)

The engine's final-team builder included threat and party number in [`CombatEngineExecutor.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs).

The generic result factory overwrote those teams using `CombatSetupService.CreateSimpleCombatEntities`, which only preserved health, barrier, level, identity, and display fields in [`CombatEncounterResultFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatEncounterResultFactory.cs).

Consequently Raid and Region Boss results can retain party and threat data while Idle, Dungeon, Arena, Tournament, Training, and Tower lose it. The factory also only reads initial hostiles, making it unsafe for waves.

Implemented solution: every public execution shape in [`CombatEngineExecutor.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs) now populates authoritative post-combat teams. [`CombatEncounterResultFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatEncounterResultFactory.cs) passes them through without rebuilding or overwriting them.

### P3 — Preparation was duplicated despite an unfinished shared abstraction (implemented)

Before this implementation, there were separate preparation implementations for:

- Idle and Dungeon template factories.
- Arena and Tournament snapshot reconstruction.
- Raid.
- Region Boss.
- World Tower.
- Quest training.

A generic runtime factory and combatant factory also existed and were registered, but no production combat path consumed them. Some branches were explicitly unsupported.

This was the central maintenance pain point: adding a new equipment field, snapshot property, combat activity, or preparation rule required updating several independent paths.

Implemented solution: [`CombatPreparationPipeline.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatPreparationPipeline.cs) now owns the invariant path for live and immutable-snapshot sources. Each content service supplies only its explicit source policy and pre/post-preparation transforms. The pipeline is used by Idle, Dungeon, Arena, Tournament, Raid, World Tower, Region Boss, and Quest Training. The old unused `EncounterEntityLoader`, `CombatantFactory`, and `CombatEncounterRuntimeFactory` chain and its interfaces were removed.

## Implemented target design

The shared invariant preparation pipeline now performs:

1. Select the source policy: live entity or immutable snapshot.
2. Rehydrate the complete entity, including equipment slots, main/off-hand identity, blueprint, essences, level, base attributes, and display data.
3. Assign stable runtime IDs and party information.
4. Apply content scaling and temporary modifiers.
5. Resolve the explicit essence activity.
6. Calculate combat attributes exactly once.
7. Validate required fields and health.
8. Execute through one engine core and an explicit ruleset.
9. Evaluate the content objective separately.
10. Finalize one authoritative result without rebuilding teams.

Intentional engine differences are represented through the immutable `CombatRuleset`:

```text
CombatRuleset
  RandomSeed
  MaxTicks
  StartAbilitiesOnCooldown
  SupplementalAbilities
  BasicAttackIntervalTicks
  Overtime
  DownedAndRevival
  WaveRecovery
  HostileFury
  EventLogPolicy
```

Stagger remains encounter/combatant configuration, while Overtime, Fury, revival, recovery, supplemental abilities, duration, and event logging are explicit ruleset fields. Objective interpretation remains separate in the P2 objective evaluator. This preserves the modes' intended behavior without allowing base entity preparation or engine defaults to drift.

## Implemented sequence

1. Replace Dungeon, Arena, and Tournament reconstruction with the canonical snapshot builder.
2. Add Region Boss snapshots.
3. Make Tournament identity and seed deterministic.
4. Introduce exhaustive content identity and explicit essence-activity mapping.
5. Consolidate engine options into `CombatRuleset`.
6. Separate engine outcome from content outcome.
7. Consolidate result finalization.
8. Introduce the shared combat-preparation pipeline and migrate every production battle path.
9. Remove the unused or incomplete generic factories after the production paths use the replacement.

## Recommended contract tests

Cross-mode and pipeline contract tests should continue to prove:

- One snapshot produces identical attributes, abilities, set bonuses, and weapon behavior in Dungeon, Tournament, Raid, and Tower.
- Changing the live character after snapshot creation changes none of those battles.
- Re-running the same scheduled encounter produces an identical result.
- Every content type maps to exactly one essence activity.
- Enabling Stagger, Overtime, Fury, or revival changes only that mechanic.
- Final teams preserve party number, threat, and every spawned wave.

## Verification and repository impact

P3 adds focused pipeline contract coverage for mixed live/snapshot preparation, ordering, identity, exact activity mapping, hook ordering, health validation prerequisites, required creature scaling context, and duplicate-slot rejection. Existing mode-specific suites continue to cover the migrated battle entry points.

The full backend suite passes after the combined implementation: **1,614 tests, 0 failures, 0 skipped** in Release configuration. The build reports two existing xUnit analyzer warnings.

The combined P1–P3 implementation includes the P1 snapshot persistence migration `UnifyCombatSnapshots`. P3 itself adds no migration or configuration change and has no direct deployment action beyond deploying the updated application and applying the existing P1 migration through the normal release process.

## Recommended-power calibration audit

### Conclusion

The current tests are not yet sufficient to make an absolute player-facing guarantee that every recommended Power Rating predicts live combat accurately.

World Tower has a strong production-path calibration test: it uses the canonical equipment builder, immutable snapshots, `SnapshotCombatantBuilder`, `CombatPreparationPipeline`, `WorldTowerCombatRuntimeFactory`, `CombatSetupService`, and `CombatEngineExecutor`. That makes it the best reference implementation.

The remaining system has material gaps:

- Raid's displayed `RecommendedWingPower` values are authored numbers. No test runs below/at/above those exact ratings through all three production wings and the final assault.
- The broad Idle, Dungeon, Tower, and Raid encounter matrix uses the real low-level engine and authored content, but bypasses the production combatant-preparation and engine-executor paths.
- Power Rating intentionally excludes ability value and several equipment behaviors while still reporting `High` confidence.
- The default calibration sample sizes are too small to support precise win-rate claims.
- Several calibration runners report balance exceptions without making those exceptions fail CI.

Current confidence by content:

| Content | Recommendation coverage | Production-path fidelity | Confidence in displayed recommendation |
| --- | --- | --- | --- |
| World Tower | Direct below/recommended/stronger cohorts for all 15 floors | High for preparation and execution; narrower for population coverage | Medium |
| Raid | Authored wing values and formula tests; synthetic final-assault diagnostics | Low for static recommendations; high for the player-specific Battle Plan preview | Low |
| Idle | Progression-envelope encounter samples; no explicit displayed recommendation found | Medium-low | Not established |
| Dungeon | Representative room/boss progression samples; no explicit displayed recommendation found | Medium-low | Not established |
| Arena | No recommended-power calibration | Production combat itself is shared | Not applicable |
| Tournament | No recommended-power calibration | Production combat itself is shared | Not applicable |
| Region Boss | Power Rating is used for matchmaking, not a content recommendation | Matchmaking tests use fixed ratings | Rating-to-outcome relationship is not established |
| Quest Training | No recommended-power calibration | Production combat itself is shared | Not applicable |

### How player Power Rating is produced

[`PowerBuildSnapshotFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerBuildSnapshotFactory.cs) loads the live character, selects the default `EssenceCombatActivity.None` loadout, calculates an attribute-based rating, and separately prepares a `CombatEntity`. [`CombatRatingCalculator.cs`](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/CombatRatingCalculator.cs) values positive base, equipment, and explicitly supplied Essence attribute modifiers using equipment-budget cost weights.

Important limitations of the scalar rating are explicit in the implementation:

- Essence active and passive ability value is excluded.
- `ControlUtility` is always zero.
- Single-target and multi-target offense are currently identical.
- Main-hand recipe/blueprint attack interval, damage multiplier, attack type, damage type, and range behavior are not valued.
- Equipment-set attribute bonuses and granted abilities are not included by the rating calculation.
- Party synergy, threat distribution, stagger contribution, healing timing, barriers, summons, and matchup-specific resistance/penetration interactions cannot be represented accurately by one additive number.
- The rating uses the default Essence loadout, while Dungeon, Arena, Tournament, Raid, World Tower, and Region Boss snapshots select activity-specific loadouts.

The final point can make the number shown to a player describe a different build from the one that enters the battle. Production content creates activity-specific snapshots in [`CharacterSnapshotRepository.cs`](../../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Snapshots/CharacterSnapshotRepository.cs), while Power Rating always selects `None`.

Despite these omissions, [`PowerRatingService.cs`](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerRatingService.cs) returns `PowerRatingConfidence.High`. Its status text acknowledges that Essence abilities are absent, but the confidence level does not.

### Test layers and what they actually prove

#### Power Rating unit tests

[`PowerRatingCoreTests.cs`](../../LL/tests/EssenceSystem.Tests/PowerRatingCoreTests.cs) verifies deterministic weighting, cap handling, modifier semantics, equipment deduplication, and fingerprint stability. These are valuable algorithm-contract tests.

They do not currently prove:

- That `PowerBuildSnapshotFactory` selects the same activity loadout as a given content type.
- That the rating's projected attributes equal the final attributes produced by `CombatSetupService`.
- That two materially different builds with the same rating have comparable live win rates.
- That weapon behavior, equipment sets, or Essence abilities are reflected in the displayed number.

#### Synthetic progression and encounter calibration

[`PlayerProgressionSnapshotFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/PlayerProgressionSnapshotFactory.cs) generates deterministic, Essence-free attribute dictionaries from budget envelopes rather than actual equipment instances. [`EssenceCalibrationMatrixFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/EssenceCalibrationMatrixFactory.cs) adds selected Essence identities, and [`EncounterCalibrationRunner.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/EncounterCalibrationRunner.cs) constructs `RuntimeCombatant` objects directly.

Strengths:

- Uses current authored creatures, regions, dungeons, Tower floors, Raid definitions, abilities, statuses, and summons.
- Uses `CreatureScaler` and deterministic seeds.
- Exercises several gear envelopes, build families, Essence envelopes, party compositions, win rates, duration, survivability, support, summons, control, stagger, and confidence intervals.
- Provides useful broad regression and mechanic diagnostics.

Differences from production:

- It does not use `CharacterSnapshot`, `SnapshotCombatantBuilder`, `CombatPreparationPipeline`, `CombatSetupService`, `CombatEngineExecutor`, or the content resolver.
- Friendly Essence abilities are compiled directly, but the production loadout resolver's attribute modifiers and Essence tags are not applied.
- Hostiles receive authored native abilities, but not the creature Essence loadout that production preparation can add, including its modifiers and abilities.
- Projected equipment attributes have no real main-hand/off-hand item, recipe, blueprint, equipment set, or granted set ability. Basic attacks therefore use default behavior.
- Tower and Raid scaling logic is copied into the calibration factory, creating another drift point.
- Idle and Dungeon samples use much lower `MaxTicks` values than the production executor's 6,000-tick standard limit.
- Raid samples model an isolated, fully prepared final boss. They omit Rearguard waves, Vanguard objectives, Main Guard survival, surviving reinforcements, boss-variant selection, and the way those phases affect the final assault.
- Synthetic Raid victory means killing the boss, while production also exposes `Repelled`, `Wounded`, `Broken`, and `Slain` outcomes.

The comprehensive encounter test produces 1,680 aggregate rows and 5,040 seeded simulations, but each row has only three samples by default. More importantly, [`EncounterCalibrationTests.cs`](../../LL/tests/EssenceSystem.Tests/EncounterCalibrationTests.cs) does not require the complete assessed report to have zero exceptions. It verifies selected exceptions are absent and that diagnostics are well formed, so unrelated out-of-band results can coexist with a passing suite.

The baseline comparison in that test compares the generated artifact to itself. It proves deterministic serialization, not regression against a previously approved balance baseline.

#### World Tower production calibration

[`WorldTowerProductionCalibrationRunner.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/WorldTowerProductionCalibrationRunner.cs) and [`WorldTowerProductionCalibrationTests.cs`](../../LL/tests/EssenceSystem.Tests/WorldTowerProductionCalibrationTests.cs) are substantially closer to live behavior:

- Canonical builds use real recipes, item bases, deterministic stat rolls, tempering, real weapon identities, and real Essence definitions.
- Builds are converted to full `CharacterSnapshot` objects.
- Snapshot rehydration, content activity, set resolution, creature Essence preparation, Tower scaling, stagger, and engine execution all use production services.
- The test covers below-recommended, recommended, and stronger cohorts for every currently authored floor.
- The test fails when cohort win rates leave their defined bands.

The new [`WorldTowerProfileShadowCalibrationRunner.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/WorldTowerProfileShadowCalibrationRunner.cs) now runs approved, evidence-backed character profiles beside those canonical cohorts through the same runtime and playback executor. It selects the exact approved equipment scenario for each floor, records per-team and population-weighted results, and compares them with both the closest-power canonical cohort and the canonical recommended cohort. Profile scenario IDs explicitly distinguish roster size, equipment rung and quality, source audit profile, and Essence count. The required scenario manifest is derived from the canonical runner's own loadout curve.

World Tower Essence discovery uses reusable five-character parties with the canonical Guardian, Restorer, Striker, Striker, and Controller roles and role-specific discovery attributes. Before profile families are selected, every eligible finalist is materialized at the exact target equipment rung and receives ten deterministic battles on every target floor through the production guardian, scaling, stagger, cooldown, runtime, and playback paths. Profile materialization then creates exact 5/10/15-character expeditions with explicit party assignments. The pass remains intentionally non-authoritative: invalid catalogs fail closed, missing or malformed qualification coverage is reported, and no runner can update floor definitions or player-facing recommendations.

Remaining limitations:

- The legacy smoke calibration still uses ten implicitly seeded samples per floor/cohort. At a measured 50% win rate, ten Bernoulli samples have a very wide 95% interval of roughly 24%–76%; a one-battle change moves the estimate by ten percentage points. The new certification path defaults to 100 samples and supports 1,000.
- Legacy shadow and smoke runs preserve their historical encounter-derived seeds. Certification instead injects one explicit, versioned seed manifest into every below/recommended/stronger cohort and profile team, providing common random numbers without changing ordinary smoke-test baselines.
- The authoritative canonical pass still uses one deterministic canonical roster and one deterministic roll per progression rung. The shadow pass samples approved Meta, Typical, specialist, weak, budget, counter, adversarial, and No-Essence profiles, but it cannot cover a floor until an exact-size approved portfolio exists.
- Calibration and finalist qualification load the same persisted guardian source used by production. Fingerprint contract 3 includes exact Tower definitions, guardian combat fields, native creature abilities, guardian Essence loot mappings, and region scaling so these changes invalidate stale profile reuse.
- Preparation contribution bonuses are fixed at zero. This is a reasonable unprepared baseline, but the recommendation is not labelled with that assumption.
- Early floors require at least 80% wins at recommendation while later floors require 40%–70%. The player-facing meaning of “recommended” therefore changes by floor.
- Legacy smoke tests still use point estimates. The certification runner uses Wilson confidence bounds, common seeds, monotonic canonical checks, profile-spread limits, timeout limits, and exact scenario coverage.
- Certification records rating, rules, preparation, equipment, roster, generator, runtime, build, seed, catalog, and content fingerprints. A headless CI/release policy that requires a current approved artifact is still missing.

During this audit, the same World Tower calibration passed in Release but one below-recommended cohort produced 30% wins against a 20% ceiling in an existing Debug build. Release is the repository's required test configuration and passed repeatedly, but this demonstrates that ten-sample boundary assertions are sensitive enough that build/runtime provenance should be recorded and cross-configuration determinism should be investigated.

#### Raid recommendation and Battle Plan tests

Raid's static `RecommendedWingPower` values live in [`raid-bosses.json`](../../LL/src/API/API.LL/Data/raids/raid-bosses.json). [`RaidDefinitions.cs`](../../LL/src/Core/Domain/Models/Raids/RaidDefinitions.cs) derives +level recommendations from a fixed growth formula, and [`RaidSystemTests.cs`](../../LL/tests/EssenceSystem.Tests/RaidSystemTests.cs) verifies that arithmetic.

No current test uses an authored recommended wing value to construct corresponding player builds and then measures the production Rearguard, Vanguard, or Main Guard result. The synthetic encounter matrix tests final assaults, but does not consume `RecommendedWingPower` and cannot validate the three wing-specific numbers.

The player-specific Battle Plan preview is different: [`RaidCombatResolver.cs`](../../LL/src/Infrastructure/Service/Services.LL/Raids/RaidCombatResolver.cs) runs ten samples through the same complete four-stage production resolver used by the real raid, with alternate deterministic seeds and immutable signup snapshots. That preview is a good relative forecast for the exact signed-up roster, but it does not establish that the static recommendation itself is correct and ten samples still provide limited confidence.

#### Supporting ability, Essence, and stagger tests

Ability balance, Essence progression, and [`StaggerCalibrationTests.cs`](../../LL/tests/EssenceSystem.Tests/StaggerCalibrationTests.cs) provide useful mechanic-isolation coverage. They intentionally use synthetic combatants or isolated state machines and should not be treated as proof of a content Power Rating recommendation.

Stagger calibration also returns exceptions without asserting that the complete exception collection is empty. It validates determinism and metric ranges, not universal adherence to the authored target bands.

### Prioritized findings

#### P0 — The displayed rating can describe a different Essence loadout than the battle

Power Rating uses `EssenceCombatActivity.None`; scheduled content snapshots use the exact content activity. A player can therefore satisfy a recommendation with one set of attribute modifiers while entering combat with another set of modifiers and abilities.

Recommended solution: make rating creation require `CombatContentType` or `EssenceCombatActivity`, and calculate the displayed readiness rating from the same immutable snapshot that will be used for that encounter. If a global rating remains, label it `Base Power` and show a separate content-specific readiness rating.

#### P0 — Raid wing recommendations are not simulation-validated

The values are authored and their +level growth is arithmetically tested, but no production combat test establishes their outcome meaning.

Recommended solution: add a `RaidProductionCalibrationRunner` that creates canonical immutable rosters below, at, and above each authored wing recommendation; runs Rearguard, Vanguard, Main Guard, and the full final assault through `RaidCombatResolver`; and enforces lane-specific objective bands plus final Raid outcome bands for every regular tier and representative +levels.

#### P0 — The general calibration matrix does not run the production setup

Direct construction of `RuntimeCombatant` omits multiple production inputs and duplicates content scaling.

Recommended solution: retain the synthetic runner for fast mechanic exploration, but rename it clearly as a synthetic diagnostic. Add a production calibration layer whose only way to create combatants is `CharacterSnapshot`/`CombatPreparationPipeline` and whose only way to execute is the relevant production runtime factory/resolver and `CombatEngineExecutor`.

#### P0 — Reported balance exceptions do not necessarily fail tests

A green suite does not mean the full assessed encounter or stagger matrices are within their target bands.

Recommended solution: define an explicit approved-exception allowlist with owner, reason, expiry, and content fingerprint. Fail CI for every unapproved exception. Compare reports against a committed approved baseline rather than comparing an artifact to itself.

#### P1 — Power Rating omits high-impact mechanics but reports high confidence

Equal attribute budgets do not imply equal combat effectiveness when abilities, weapons, sets, control, healing, summons, stagger, and matchup damage types differ.

Recommended solution: either incorporate empirically fitted ability/weapon/set contributions into a versioned content-specific rating, or downgrade the confidence and present multidimensional readiness such as offense, durability, sustain, control, and role fit. `ControlUtility` and single/multi-target offense should not be published as meaningful dimensions until they are actually calculated.

#### P1 — Sample sizes and cohort comparisons are statistically weak

Three samples per general matrix row and ten samples per World Tower cohort are suitable for smoke detection, not trustworthy probability estimates.

Recommended solution:

1. Use the same seed set for below/recommended/stronger cohorts.
2. Gate on Wilson confidence bounds or a sequential probability test rather than raw point estimates.
3. Use at least 100 deterministic seeds for pull-request gating and 500–1,000 for release calibration of player-facing recommendations.
4. Require stochastic monotonicity: stronger cohorts must not perform worse than recommended cohorts, and recommended cohorts must outperform below-recommended cohorts within a declared tolerance.

#### P1 — Canonical cohorts are too narrow

One deterministic roster can validate that roster, not the player population represented by the same average rating.

Recommended solution: sample multiple legal equipment rolls, weapon families, blueprints, equipment sets, Essence loadouts and ascension tiers, role compositions, and intra-party power distributions. Include adversarial equal-rating builds so the maximum win-rate spread at a given rating becomes a release gate.

#### P1 — Calibration provenance is incomplete outside the World Tower certification path

World Tower certification now records detailed algorithm, runtime, catalog, build, content, and seed provenance. The synthetic general matrix and future Raid, Dungeon, Idle, and other production recommendation runners do not yet demonstrate the same complete approval identity.

Recommended solution: reuse or extend the World Tower deterministic provenance contract for every recommendation runner, containing at least:

- Power Rating and combat-rules versions.
- Preparation-pipeline/schema version.
- Ability, Essence, creature, region, dungeon, Raid, Tower, crafting, equipment-set, and threat/tanking content hashes.
- Runtime/framework and build configuration.
- Canonical roster/build generator versions and random seed manifest.

Changing any component should invalidate the previous recommendation baseline.

#### P2 — The meaning of “recommended” is inconsistent

World Tower currently targets high success on early floors and near-even success on later floors. Raid wing recommendations have no encoded target outcome.

Recommended solution: publish one explicit contract per recommendation, for example: “A legal balanced roster at this content-specific average rating has a 70%–85% chance to satisfy the content objective with no preparation bonus.” Use content objectives, not only extermination, and expose assumptions in the UI.

#### P2 — Production telemetry is not closing the calibration loop

Offline canonical builds cannot capture real player behavior and build distributions completely.

Recommended solution: record privacy-safe aggregates keyed by content/version/rating band—attempts, objective success, timeout, duration, party composition, and preparation level. Compare observed confidence intervals with calibration predictions and require review when they diverge. Telemetry should validate recommendations, not silently rewrite balance.

### Recommended acceptance standard

A player-facing recommendation should be considered trustworthy only when all of the following are true:

1. The displayed rating is calculated from the exact activity-specific immutable combat snapshot.
2. Calibration uses the production preparation pipeline, content resolver, engine executor, ruleset, and objective evaluator.
3. Every authored recommendation has below/at/above cohorts with an explicit success contract.
4. All unapproved calibration exceptions fail CI.
5. Confidence-based sample thresholds and monotonic cohort checks pass.
6. Multiple legal builds and party compositions at the same rating stay within an approved outcome spread.
7. The report carries complete version/content provenance.
8. Live aggregate outcomes remain within the predicted confidence band.

The diagnostic certification runner now automates conditions 2, 3, 5, 6, and most of 7 for World Tower. It fails closed on missing profile coverage or seed provenance and exports a machine-readable artifact. Conditions 1, 4, and 8 still require their respective production snapshot, CI policy, and telemetry integrations before recommendations should become authoritative.

The Admin-only audit campaign closes the operational gap before certification: it derives the exact Tower scenario portfolio, reuses audits only where role-aware discovery inputs are identical, requalifies profiles when Tower/guardian/materialization inputs change, persists full evidence outside browser storage and source control, resumes after process interruption, and produces a validated catalog candidate. Human source-control review remains mandatory before that artifact becomes the approved catalog.

Until those conditions are met, World Tower recommendations should be described as calibrated estimates for the canonical unprepared roster, and Raid recommendations should be described as authored guidance supplemented by the roster-specific Battle Plan preview—not guaranteed indicators of success.
