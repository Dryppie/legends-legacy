# Character Profile Generator design

## Implementation status

The production-valid vertical slice, approved-catalog milestone, expanded evidence-backed portfolio, role-aware World Tower discovery, production-runtime finalist qualification, shadow-calibration consumer, and guarded Tower campaign workflow were implemented on 2026-08-26.

Implemented:

- Versioned application contracts for generation reports, teams, profiles, prepared previews, and equipment summaries.
- Explicit-Essence overloads in `CanonicalEquipmentBuildFactory`, including definition, duplicate, and source-family validation.
- Deterministic selection of distinct `Meta`, `Typical`, and `WeakButLegal` finalist teams from an immutable balance-audit report.
- Deterministic profile, team, character, and snapshot identities.
- Full canonical equipment and Essence materialization into detached `CharacterSnapshot` instances.
- Validation of every generated team through `CombatPreparationPipeline` using its selected `CombatContentType`.
- Prepared previews containing final attributes, abilities, tags, Essences, equipment, health, and Power Rating.
- Rejection of stale audit reports by comparing their content fingerprint with the current ability, status, summon, Essence, and equipment-balance fingerprint.
- Provenance for profile schema, generator, Power Rating algorithm, combat rules, equipment balance, canonical roster, source audit, source content, and selection seed.
- An Admin Dashboard workflow attached to completed balance audits, including content, quality, team-count, and seed controls, prepared profile inspection, and JSON export.
- Backend integration tests covering deterministic generation, family selection, explicit Essence preservation, stale-audit rejection, and production preparation.
- A versioned, source-controlled approved catalog at `LL/src/API/API.LL/Data/combat/combat-character-profiles.json`.
- A shared profile materializer used by generation and catalog revalidation, preventing those paths from drifting apart.
- Catalog validation for schema and engine versions, current combat-content fingerprint, global ID uniqueness, required control families, team references, slots, canonical equipment rungs, legal Essence loadouts, Power Rating, and the complete prepared combatant preview.
- Admin API endpoints for loading the committed catalog and validating an imported or staged candidate.
- An Admin Dashboard catalog workflow that imports either a catalog or a single generated profile set, stages or replaces matching scenarios, retires sets, displays validation issues, and exports only a valid normalized catalog.
- Backend tests proving valid catalog reconstruction, stale-content rejection, Power drift detection, prepared-combatant drift detection, and loading of the committed empty bootstrap catalog.
- Multi-seed finalist round robins in the Essence audit. The audit now retains per-seed combination scores and complete aggregated head-to-head matchup evidence instead of relying on the first seed or the capped battle-summary preview.
- Evidence-qualified selection using separate aggregate-team and direct-match minimum battle counts, Wilson 95% confidence limits, maximum cross-seed score spread, and maximum Jaccard Essence overlap.
- An expanded portfolio containing `Budget`, paired `Counter`/`Countered`, paired `EqualPowerAdversarial`, every canonical `RoleSpecialist.*`, and a production-prepared `NoEssence` control in addition to the three core families.
- Heterogeneous standard teams built from the canonical cooperative roster (`Guardian`, `Restorer`, `Striker`, and larger-party continuations), with role-specific Defensive, Sustain, Offense, Balanced, or Area equipment profiles.
- Role-aware five-character Essence discovery using the canonical `Guardian`, `Restorer`, `Striker`, `Striker`, and `Controller` slot sequence, each with its actual canonical discovery-equipment attributes and `Role.*` tag.
- Backward-compatible legacy balance simulations: non-role-aware requests retain the old normalized signatures, while role-aware signatures include role identity and cannot collide with legacy saved candidates.
- A bounded World Tower candidate qualifier that materializes every eligible finalist at the exact target equipment rung and executes ten deterministic samples on every target floor through production preparation, guardian scaling, stagger, cooldown, runtime, and playback behavior.
- Context-aware profile selection: Meta and Budget prioritize worst-floor and average Tower results, Typical targets the scenario success band, Weak-but-Legal selects the lowest qualified result, and PvP discovery score becomes a secondary signal when Tower evidence exists.
- Persisted party-level Tower evidence containing floor and scenario identity, sample outcomes, timeout and duration metrics, deterministic seed-manifest provenance, and production-runtime/cooldown assertions.
- Catalog admission rules that revalidate source uniqueness, portfolio mode, evidence thresholds, finite seed and overlap metrics, matchup adversaries with persisted direct samples, scores, and directional confidence intervals, Budget rarity, No-Essence legality, specialist roles, and canonical heterogeneous role composition.
- Admin Dashboard controls and evidence displays for portfolio mode, sample threshold, confidence width, seed spread, Essence overlap, synthetic controls, selection rationale, and audited adversaries.
- A diagnostic-only World Tower shadow runner that executes the unchanged canonical cohorts and approved profile teams through `WorldTowerCombatRuntimeFactory` and Tower playback, then reports both together without writing recommendations.
- Exact roster-size coverage gates: a 1v1 or 3v3 profile team is never silently cloned into a 5-, 10-, or 15-slot Tower encounter. Missing profile sizes are explicit report issues.
- Explicit, configurable population buckets: 25% Meta, 40% Typical, 20% Role Specialist, and 15% weak/budget/counter/adversarial resilience profiles. Available buckets are normalized; No-Essence remains a zero-weight diagnostic control.
- Per-floor selection of the approved profile set nearest the authored recommendation, weighted outcome summaries, canonical deltas, source-audit/content provenance, and fail-closed handling for invalid or stale catalogs.
- An Admin Dashboard endpoint and UI for running and exporting World Tower shadow reports. The report carries `RecommendationsChanged: false` by construction.
- Profile schema/generator version 7/13 includes explicit scenario identity and floor coverage, party composition and evidence, production Tower context qualification, separate matchup safeguards, directional adversary confidence, target-aware World Tower anchor selection, and bounded direct Tower candidate search. Team/profile identities include the exact content, roster size, equipment tier/rarity/quality, source-audit equipment profile, and Essence count, so adjacent progression contexts cannot collide.
- Catalog validation now requires the scenario contract, checks it against every team and profile, and uses its stable ID for duplicate detection instead of inferring a scenario from the first heterogeneous character.
- Essence balance audits and manual simulations now support teams of up to 15, covering every currently authored Tower roster size.
- `WorldTowerProductionCalibrationRunner` derives a requirements manifest from the same recommended-loadout curve used by canonical calibration; it is not a duplicated Dashboard matrix.
- The Admin Dashboard World Tower Portfolio Batch maps saved audits to each production-derived requirement, reuses an audit across qualities when appropriate, generates every selected Expanded portfolio, merges by scenario ID, revalidates the complete catalog candidate, and remains export/review-only.
- A diagnostic World Tower certification runner now uses one explicit, fixed seed manifest shared across canonical below/recommended/stronger cohorts, every profile team, campaign runs, and 100-sample anchor confirmation. Campaign identity is provenance rather than a hidden source of new random samples. Certification requires complete exact-size profile coverage, an exact selected-scenario match to the production-prepared recommended equipment and Essence context, configurable minimum samples, monotonic canonical outcomes, and at least one exact-context team with an inclusive 5%–20% estimated win rate, bounded timeout rate, and valid production-runtime evidence. Per-team confidence intervals, weighted population outcomes, and equal-context spread remain diagnostics.
- Expanded World Tower generation screens every eligible finalist with ten production samples and confirms apparent hits with 100. Generator 13 also builds a partial reserve from up to 500 additional legal parties directly in each Tower scenario, then evaluates coverage after the homogeneous core families have been selected. Direct anchors retain neutral PvP fields and cannot enter ordinary profile families. Every diagnostic anchor records overlap without being blocked by the ordinary portfolio-diversity limit. One candidate may cover multiple floors; otherwise the generator adds one distinct anchor per uncovered floor. These diagnostic-weight anchors supplement the existing ten families, producing at most twelve teams for the currently grouped scenarios. Generation fails explicitly when no legal confirmed final team exists.
- Certification artifacts include deterministic input, canonical, profile, catalog, and seed-manifest fingerprints plus preparation, rating, combat-rules, equipment, roster, generator, runtime, architecture, and build-configuration provenance. The Dashboard can run and export the machine-readable pass/fail evidence, but the runner cannot change recommendations.
- The Admin-only World Tower Audit Campaign derives production requirements, deduplicates quality-only contexts into distinct audit work, executes expensive audits sequentially in a background worker, persists campaign state and full reports under the user's local application-data directory, resumes interrupted work on Admin API restart, supports cancellation and retry of unfinished work, and automatically generates and validates the complete Expanded catalog.
- The Dashboard discovers persisted campaigns, polls active progress, displays audit/scenario coverage, and can stage or export the generated catalog and download the complete evidence bundle. Browser storage is not part of campaign durability.
- Historical schema-3 campaign `e283b6e9-0463-497e-8ff3-3536ba1fd1b7` completed five audits and 380,760 battles, then generated 13 exact-scenario profile sets and 130 teams with zero catalog-validation issues. It remains useful structural evidence but is stale under the role-aware and Tower-qualified contract.
- Historical schema-4 campaign `6339d840-f00a-4630-a869-d5ad862a3bd1` completed smoke and 100-sample certification but was correctly blocked by 29 certification findings. Its investigation exposed the identical-participant discovery discrepancy and lack of production-floor qualification that version 7 now corrects.
- Fingerprint-contract-3 campaign `e6b1d2a5-3f66-4b19-85ff-cde9e53af32e` completed five fresh role-aware audits, production qualification, 13 valid generator-7 profile sets, smoke, and 100-sample certification. Contract-3 re-evaluation `72d8c717-7b5e-4b02-8dc9-eb59d5785094` applied the fixed 5%–20% any-one-team profile band and blocked promotion with nine canonical findings plus ten floors without an in-band team. Generator 8 supersedes those catalog artifacts; see [`world-tower-contract-v3-campaign-review.md`](world-tower-contract-v3-campaign-review.md).
- Generator-8 campaign `08e3c381-ad73-41a1-8f07-7b099154a14d` reused all five compatible discovery audits, correctly rejected generator-7 catalog reuse, and failed catalog generation at floor 4 because no remaining diverse legal finalist had a 5%–20% production-qualification estimate. Later scenarios were not evaluated after the hard failure.
- Generator-13 campaign `66368b83-07c1-4a7a-baf6-487c65fc8492` reused the five audits, generated 13 valid sets, passed smoke, and passed fixed-seed 100-sample certification with zero issues. All 15 floors have at least one qualifying final team and all canonical confidence gates pass. The candidate is ready for human promotion review; the approved source catalog is unchanged.
- Fingerprint contract 3 invalidates profile reuse after changes to canonical role builds, exact Tower definitions, persisted guardians, native creature abilities, guardian Essence loot mappings, region scaling, qualification rules, or target materialization inputs.

Remaining follow-up work:

- Direct consumption of approved profiles by Raid, Dungeon, and other production calibration runners. World Tower consumption now exists in shadow mode.
- Review and source-control promotion of the first fully passing generator-13 catalog candidate.
- Population of the bootstrap catalog using a completed, passing, and reviewed current-contract campaign. The committed catalog deliberately remains empty until that evidence exists.
- Approved World Tower portfolios for every required scenario. Until they are committed, the shadow report will deliberately show coverage gaps instead of extrapolating smaller or differently equipped teams.
- Optional tier- and equipment-context stability evidence. Current stability is measured across the finalist audit's configured random seeds; separate audits are still required to compare progression contexts.
- A release certification run after the approved catalog is populated. Pull-request evidence should use at least 100 samples per team; release evidence should raise this to 500–1,000.

The catalog deliberately uses source-control review instead of a database approval table. The Admin Dashboard never mutates repository content: it produces a normalized, fully revalidated `combat-character-profiles.json` artifact that is reviewed and committed. This keeps approvals immutable, diffable, reproducible in CI, and migration-free.

## Purpose

A Character Profile Generator should become the shared foundation for combat calibration. It should create complete, production-valid character builds that can be reused by World Tower, Raid, Dungeon, and future recommended-power simulations.

The generator should use strong Essence combinations discovered by the existing Admin Dashboard balance tooling, but strong combinations should be one profile category rather than the definition of a normal player. Calibration must also cover typical, specialized, imperfect, and countered builds so that recommendations remain useful for players who do not follow the current best-performing meta.

The existing canonical builds currently use hard-coded Essence lists in [`CanonicalEquipmentBuildFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/CanonicalEquipmentBuildFactory.cs). Replacing those lists, for calibration purposes, with versioned and simulation-informed profiles would materially improve the realism and breadth of the balance tests.

## Profile contents

Each generated profile should completely describe a battle-ready character:

- Character level and combat role.
- Equipment tier, rarity, quality, recipe, blueprint, set, and deterministic rolls.
- Essence combination, levels, ascension tiers, and evolution states.
- Intended activity or content type.
- Resulting Power Rating.
- Prepared attributes, abilities, tags, weapon behavior, and equipment-set effects.
- Source balance-audit identifier and content hash.
- Generator and profile versions.
- Selection category and the reason the profile was included.

Profiles should be detached calibration objects rather than persisted fake player characters. They should materialize as real `CharacterSnapshot` instances so that every calibration runner can feed them directly into `CombatPreparationPipeline`.

## Prefer combination results over individual rankings

The primary input should be `AbilityBalanceAuditReport.Finalists` or the simulator's `RankedCombinations`, rather than a list composed from the individually highest-scoring Essences.

Individual Essence rankings can obscure:

- Essence synergies.
- Duplicate or redundant functionality.
- Role coverage.
- Healing, control, barrier, summon, or stagger interactions.
- Combinations that are powerful only with a particular partner.
- Essences whose apparent strength comes from frequently appearing in already-strong teams.

The audit's finalist results and replacement validation are particularly valuable for distinguishing genuinely strong Essences from correlated passengers. Individual adjusted scores remain useful as supporting evidence and as a diversity constraint, but should not be the sole selection rule.

## Recommended profile portfolio

The generator should create several profile families at every relevant progression level.

| Profile family | Purpose |
| --- | --- |
| Meta | Strong, well-supported combinations from current audit results |
| Typical | Combinations near the median result |
| Budget | Legal, accessible combinations without rare optimal synergies |
| Weak-but-legal | Reveals whether recommendations punish reasonable imperfect builds |
| Role specialist | Guardian, Restorer, Striker, Controller, Area Specialist, or Defensive Hybrid |
| Countered | A generally strong build placed into an unfavorable matchup |
| Counter build | A build specifically suited to the target content |
| Equal-power adversarial | Materially different builds with approximately equal Power Rating |
| No-Essence baseline | Separates equipment contribution from Essence contribution |

An initial configurable population weighting for recommended-power calibration could be:

- 25% current-meta profiles.
- 40% typical profiles.
- 20% role-specialist profiles.
- 15% weak, countered, or adversarial profiles.

These weights are a starting policy, not a permanent balance constant. They should be explicit in the calibration configuration and recorded in result provenance.

## Limitations of the existing Essence simulator

[`AbilityBalanceSimulator.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityBalanceSimulator.cs) is useful for finding promising Essence combinations, but it should not be the final validator of generated profiles.

The simulator now has two explicit modes. General/manual balance requests retain the legacy normalized `Balance` participant model. World Tower campaign discovery enables canonical roles and gives all five slots their real role-specific projected discovery attributes and `Role.*` tags.

Both modes still construct lightweight `RuntimeCombatant` objects directly and primarily evaluate symmetrical PvP performance. Discovery therefore still does not by itself reproduce real equipment instances, weapon behavior, equipment sets, granted set abilities, a Tower guardian, stagger, or the complete production Essence preparation path. For World Tower, those limitations are now bounded: audit finalists nominate candidates, then `WorldTowerProfileCandidateQualifier` materializes and executes them against the exact production floor before any core profile family is selected.

A combination that dominates a symmetrical 3v3 environment may not be the best Raid healer, Tower guardian, stagger contributor, or PvE area-damage profile. Balance-audit results should therefore nominate profile candidates, while production-path simulations decide whether those candidates are suitable for calibration.

## Intended data flow

```text
Essence balance audit
        |
        v
Candidate combinations
        |
        v
Role-aware target-content qualification
        |
        v
Character Profile Generator
        |
        v
Complete CharacterSnapshot
        |
        v
CombatPreparationPipeline
        |
        v
Content-specific production simulation
        |
        v
Approved calibration profile
```

## Admin Dashboard workflow

The Admin Dashboard exposes Character Profile generation, catalog validation, campaign execution, evidence export, and source-controlled promotion support across the following stages.

### 1. Import candidates

Use an Essence balance audit as the source and filter combinations by:

- Minimum battle count.
- Confidence interval.
- Cross-seed stability.
- Tier stability.
- Team-size stability.
- Equipment-profile stability.
- Combination score and individual adjusted scores.
- Maximum permitted overlap with profiles already selected.

Manual audit history may still exist in browser local storage, but official World Tower campaigns persist immutable audit reports and campaign evidence under the Admin service's local application-data directory. Promotable profiles must reference those server-generated artifacts or an equivalent immutable imported report; browser-local state alone is insufficient provenance.

### 2. Generate a portfolio

The administrator selects:

- Team size.
- Equipment progression rung.
- Essence slot count.
- Intended content type.
- Desired profile families.
- Number of profiles.
- Meta, typical, specialist, and weak profile distribution.
- Deterministic random seed.

For World Tower, the generator first production-qualifies eligible combinations on the exact target floors, then selects diverse sources and assigns them to role-appropriate canonical equipment builds.

Selection should optimize robustness and diversity rather than raw win rate alone. A useful candidate must be sufficiently tested, stable across requested contexts, legal under current loadout rules, and meaningfully different from profiles already included.

### 3. Validate through production preparation

Every candidate profile should be materialized and rehydrated through the complete production preparation pipeline.

The preview should show:

- Power Rating.
- Final prepared attributes.
- Active and passive abilities.
- Essence attribute modifiers and tags.
- Weapon behavior.
- Equipment-set effects and granted abilities.
- Stagger, control, healing, barrier, and summon capabilities.
- Preparation or validation errors.
- Any discrepancy between the inputs valued by Power Rating and the final prepared combatant.

A profile must not be approved if it cannot be prepared exactly like a live character.

### 4. Approve and version

Approved profiles should be immutable and carry:

- Profile identifier and version.
- Source audit identifier and content hash.
- Combat-rules and rating-algorithm versions.
- Complete character loadout.
- Intended role, profile family, and content type.
- Source combination score, confidence interval, and sample count.
- Generator configuration and random seed.
- Approval timestamp.

Changes to abilities, Essences, equipment balance, combat rules, or profile-generation rules should mark affected profiles as stale. The system should offer regeneration but must not silently replace approved profiles, because doing so would make historical recommendation results irreproducible.

## Proposed domain and service boundaries

### `CombatCharacterProfile`

An immutable, versioned profile definition containing:

- Stable identity and display name.
- Profile family and intended cooperative role.
- Activity or content scope.
- Character level and progression rung.
- Explicit equipment specification.
- Explicit Essence specification.
- Selection evidence and provenance.

### `ICombatCharacterProfileGenerator`

Consumes an immutable balance-audit report and a generation request, then returns candidate profiles plus selection diagnostics. It does not persist characters. For World Tower requests with exact floor coverage, it delegates bounded production battles to `IWorldTowerProfileCandidateQualifier` before selection; the generic generator does not embed Tower runtime logic itself.

### `ICombatCharacterProfileMaterializer`

Converts an approved or candidate profile into a complete detached `CharacterSnapshot`. Materialization should use the same canonical equipment creation and Essence definitions as production calibration.

### Production validation service

Accepts a materialized profile and prepares it through `CombatPreparationPipeline`. It returns a review representation of the final `CombatEntity`, including all inputs needed to identify preparation discrepancies.

### Calibration consumers

World Tower, Raid, Dungeon, and other recommendation runners should consume approved profile definitions through the materializer. They should not recreate Essence lists or equipment independently.

## Selection safeguards

The generator should enforce the following safeguards:

1. Do not approve a profile solely because it has the highest observed point-estimate win rate.
2. Require minimum sample and confidence thresholds.
3. Prefer results stable across multiple seeds and relevant equipment settings.
4. Prevent illegal duplicate-source Essence loadouts.
5. Preserve role coverage across the generated portfolio.
6. Limit near-duplicate profiles using an explicit similarity threshold.
7. Include typical and weak-but-legal controls alongside meta profiles.
8. Record the exact audit and content versions used for selection.
9. Revalidate every profile through production preparation.
10. Keep approved profiles immutable until an explicit replacement is reviewed.

## Historical first implementation

The following vertical slice is complete and retained as design history:

The first implementation slice should be:

1. Add the `CombatCharacterProfile` model and its provenance records.
2. Extend `CanonicalEquipmentBuildFactory` with a build method that accepts explicit Essence IDs while retaining all current loadout validation.
3. Add a profile materializer that creates a complete, detached `CharacterSnapshot`.
4. Add a generator that consumes `AbilityBalanceAuditReport.Finalists` and creates meta, typical, and weak-but-legal candidates.
5. Add a production-preparation preview endpoint to the Admin Dashboard API.
6. Add a Character Profiles tab with generation, inspection, approval, staleness status, and JSON import/export.
7. Add tests proving that a generated snapshot and the equivalent live snapshot produce identical prepared combatants.

This slice establishes a reusable profile layer before recommendation calculations change. The approved profiles can subsequently drive production-path World Tower, Raid, Dungeon, and other calibration runners consistently.

## Acceptance criteria for the first version

The first version is complete when:

1. A balance-audit finalist can be converted into a legal, complete character profile.
2. The same generation request, audit artifact, content versions, and seed produce byte-for-byte equivalent profile definitions.
3. Every generated profile records its source audit and content provenance.
4. Profiles cover at least meta, typical, and weak-but-legal families.
5. Explicit Essence combinations are validated against the same source-family restrictions as live loadouts.
6. A profile materializes into a complete `CharacterSnapshot` with real equipment and Essence identities.
7. The materialized profile passes through `CombatPreparationPipeline` without a calibration-only preparation branch.
8. Tests prove parity between the generated snapshot and equivalent production snapshot preparation.
9. Stale profiles are detected after relevant content or rules change.
10. Approved profiles can be exported and reproduced without relying on browser local storage.
