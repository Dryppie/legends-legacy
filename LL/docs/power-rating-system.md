# Simulation-backed Power and dungeon readiness

## Why the old rating was replaced

The former Combat Rating added fixed weights for attributes, equipment, Essence attributes, and manually authored ability values. It double-counted effects already resolved by combat, could not represent cooldowns or targeting accurately, and required manual scores for every new ability and dungeon.

Power is now derived from deterministic runs through the production `FastCombatEngine`. Raw attribute sums are still used by normal attribute construction, but they are never treated as authoritative Power.

## Architecture

- `PowerBuildSnapshotFactory` loads the server-owned character, active equipment, and active Essence loadout. It prepares the same `CombatEntity` used by gameplay and computes a SHA-256 fingerprint from combat-relevant inputs.
- `CombatEngineExecutor.ExecuteSimulationAsync` uses the production compiler and resolver with an explicit seed and tick limit. It does not synchronize results back to source `CombatEntity` instances.
- `PowerAnalysisSimulationRunner` owns dedicated neutral benchmark combatants, canonical parties, fixed seeds, and isolated full-dungeon simulations. Active abilities begin on cooldown exactly as they do in production combat.
- `PowerRatingService` performs an exponential search followed by binary search for the highest successful benchmark intensity. The character overview and ordinary dungeon access checks request only Overall Power; the full component suite is calculated only for the explicit detailed rating/readiness paths. Overall and full results are cached separately by fingerprint and all material algorithm versions.
- `DungeonPowerAnalyzer` finds the minimum intensity at which each canonical party profile reaches the named 72% target. The balanced profile sets the general recommendation; specialized profiles provide the diagnostic range and confidence.
- `DungeonReadinessService` directly simulates the selected build through generated dungeon routes. The recommendation ratio is explanatory only; completion probability comes from dungeon simulations.

Overall Power comes only from the neutral mixed benchmark. Its enemies scale both health and incoming physical/magical pressure, and a successful result must retain at least 50% party health. This prevents a nearly-dead damage-race victory from being presented as dungeon-ready strength. Profile values are independent scenario results and must not be added together.

The player-facing character overview displays only Overall Power. Component benchmark results remain internal to the detailed rating and dungeon-readiness API paths.

## Benchmarks and scale

The benchmark suite contains mixed combat, single-target offense, area damage, physical durability, magical durability, and sustain/attrition scenarios. The area benchmark pins current-target attacks to an extremely durable anchor and scores only three secondary targets, so sequential basic or single-target attacks cannot earn Area Damage. Physical and magical durability use matched production-engine pressure abilities with the same scaling and cadence; basic attacks are disabled only for those isolated scenarios so the damage types are not contaminated by unequal physical pressure. Control utility is measured from the reduction in resolved hostile actions against a no-ability counterfactual; it is not awarded from tags.

Benchmark intensity is centralized in `PowerAnalysisSimulationRunner`. One successful intensity level maps to 10 displayed Power. The current search cap is intensity 4096. Three fixed seeds are used for character benchmarks and two of three must pass.

Dedicated benchmark and canonical abilities are supplied only to isolated simulation calls. They are not JSON game content, cannot be equipped, grant no rewards, and cannot appear in encounters.

## Parties and companions

The API and cache keys model a `DungeonPartySelection`, and the simulation runner evaluates lists of combatants together so healing, barriers, buffs, target distribution, and overlapping effects are naturally resolved as party behavior. A party rating is never the sum of individual ratings.

The current game domain has no companion ownership, equipment, selection, or dungeon participation model. Non-empty companion selections therefore return the explicit `Unsupported` state instead of simulating fabricated or unauthorized NPC data. When companions are introduced, the snapshot factory is the single integration point that must resolve authorized companion `CombatEntity` snapshots.

## Dungeon recommendations

Recommendations are derived from the real dungeon definition, generated room map, encounter selection, tier scaling, Vigor attrition, and Rest Sites. No dungeon JSON field stores Power.

Canonical calibration and player readiness sample route choices deterministically from their fixed seed suites. This covers the dungeon's easier and harder branches instead of silently measuring only the lowest-Vigor route.

The balanced profile supplies `Recommended Party Power`, matching the same all-around measurement used for player Overall Power. Offense, sustain, defensive, and area-focused profiles supply the minimum and maximum diagnostic range and expose matchup sensitivity. Final profile rates use 24 fixed seeds; the adaptive search uses a smaller fixed seed set. The dungeon content hash covers identity, tier, grade, room bounds, Rest Sites, room types, and encounter IDs.

The API startup worker loads current recommendations from `DungeonPowerRecommendationCacheEntries` into the in-memory store. A row is reusable only when its dungeon/tier/content hash and algorithm, combat-rules, benchmark-definition, and recommendation-seed versions all match. Missing or stale rows are calibrated, persisted immediately, and published to memory. The normal dungeon list only reads the in-memory store, so opening a dungeon card never waits for a simulation.

`DungeonPowerCalibration:Enabled` controls whether missing or stale recommendations may be calculated. Database loading always occurs. The base configuration enables calculation; `appsettings.Development.json` disables it so local startup remains fast. Set the Development value to `true`, or override it with `DungeonPowerCalibration__Enabled=true`, when local recalibration is intentional. When calculation is disabled and no current row exists, that dungeon is reported as unavailable rather than remaining in a perpetual calibrating state.

Combat Vigor tolls use the party's missing health percentage when combat ends. Damage that was healed during combat does not increase the toll, while a downed member naturally contributes zero remaining health. Real runs and recommendation simulations share the same `DungeonVigorService` implementation.

The requirement profile inspects encounter group sizes and the actual combat ability catalog to identify physical, magical, area, control, boss, and attrition pressure.

## Readiness probabilities

Player previews run in deterministic batches from 8 up to 24 samples. Sampling stops early only when the 95% Wilson interval lies entirely inside one readiness band:

- Very Unlikely: below 15%
- Risky: 15% to below 40%
- Uncertain: 40% to below 60%
- Favored: 60% to below 80%
- Comfortable: 80% or higher

If the confidence interval crosses bands, the response is `Uncertain`/`LowConfidence`. This detailed endpoint is retained for diagnostics and telemetry; the normal player dungeon card uses only the cached general recommendation and does not display completion probability.

## Caching and invalidation

Character and readiness caches are process-local, bounded (2,048 build/readiness entries), and disposable. Dungeon recommendations have a 512-entry process cache plus a durable database projection used to warm the in-memory recommendation store after restarts.

The build fingerprint changes with character level, attributes, equipped item identity and modifiers, tier, quality, rarity, Potential, tempering, masterpiece state, affinity tags, special modifiers, active Essence selection, Essence level, Potential tier, ascension, and evolution. Currency, cosmetics, and other non-combat values are excluded.

Dungeon cache keys additionally contain dungeon identity/tier, dungeon content hash, combat-rules version, benchmark-definition version, algorithm version, and seed-set version. Lazy fingerprint lookup means equipment, Essence, and progression changes invalidate naturally without tightly coupled recalculation calls.

## Versioning

`PowerRatingAlgorithm` is the single version source. Increment `Version` for broad Power semantics changes, `CombatRulesVersion` for combat, creature, or ability buffs/nerfs, `BenchmarkDefinitionVersion` for benchmark changes, and the relevant seed version for seed-set changes. Any increment invalidates persisted dungeon recommendations at the next startup. Never compare snapshots with different algorithm versions.

## Side-effect safety

Simulation constructs detached combatants and calls `ExecuteSimulationAsync`. That path:

- has explicit deterministic randomness and cancellation checks;
- does not synchronize health or barrier back to source entities;
- does not call reward writers, repositories, the unit of work, outbox, achievements, PvP, quests, or SignalR;
- does not spend entry items, Sigils, attempts, or Vigor from a persisted run.

Full-dungeon analysis uses `DungeonRunFactory.CreateForSimulation`; all run state is in memory.

## Telemetry and calibration

When a player requests readiness, a short-lived server buffer retains the versioned prediction. If that player starts the same dungeon, the prediction is copied into the dungeon run's existing JSON state. Completion/failure records actual outcome, furthest room, Rest Site reach, duration, and failure reason alongside the hashed build fingerprint and prediction bounds.

Calibration remains global and versioned. Player outcomes never silently modify that player's rating.

## Developer tooling

The Admin Dashboard API endpoint `GET /api/v1/diagnostics/dungeon-power` analyzes every dungeon and returns recommendation range, requirement profile, canonical completion rates, estimated duration, content hash, algorithm version, errors, and balance warnings. Warnings include tier inversions, large canonical-profile variance, and extreme completion rates.

The existing isolated dungeon simulator remains useful for detailed per-room clear-rate diagnostics, but no longer calculates or displays the removed static score.

## Adding combat mechanics

New mechanics must first be supported by the production combat compiler/resolver. Power analysis then receives them automatically from combatant snapshots and ability definitions. Do not add a separate scoring formula. If a mechanic cannot run safely in simulation, return `Unsupported` with diagnostic context until production and simulation share a supported resolution path.

## Database and deployment

Apply the `PersistDungeonPowerRecommendations` EF Core migration before deploying the API. It adds the `DungeonPowerRecommendationCacheEntries` table; recommendation payloads are JSONB while cache identity/version fields remain queryable columns. The application never applies the migration automatically.
