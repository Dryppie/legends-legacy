# Combat calibration: current state and missing work

## Purpose

This document records the current state of the shared combat system, Character Profile tooling, and recommended-power calibration work. It distinguishes implemented infrastructure from completed balance evidence so that an available tool is not mistaken for a certified player-facing recommendation.

The detailed design and historical analysis remain in:

- [`combat-system-analysis.md`](combat-system-analysis.md)
- [`character-profile-generator-design.md`](character-profile-generator-design.md)

## Executive summary

The combat execution architecture is now substantially unified. Production combat types prepare combatants through the shared `CombatPreparationPipeline`, execute immutable `CombatRuleset` settings, and retain both engine and content outcomes. Content-specific behavior such as World Tower stagger, Tournament overtime, Raid objectives, Region Boss waves, and Dungeon modifiers remains explicit around that shared foundation.

The Character Profile and World Tower calibration infrastructure is also implemented. It can discover strong and representative Essence combinations, create complete production-valid profiles, validate a versioned catalog, run those profiles through the real World Tower runtime, and produce statistical certification evidence.

The first fingerprint-contract-3 campaign, `e6b1d2a5-3f66-4b19-85ff-cde9e53af32e`, completed all five fresh role-aware audits, production finalist qualification, catalog generation, smoke, and 100-sample certification on 2026-08-27. It retained 380,760 discovery battles and produced all 13 exact Tower scenarios under profile schema/generator 7. Its original certification-contract-1 report blocked promotion with 27 findings.

Certification contract 4 reflects the current product rule. Every selected exact-context legal team must have an estimated win rate strictly below 20%, regardless of family or population weight, and at least one selected team per floor must be strictly above 5% and below 20%. Exactly 5% does not satisfy the anchor requirement; exactly 20% fails the universal cap. Confidence intervals, the weighted population, and cross-team spread remain visible diagnostics but do not replace those point-estimate gates. Contract 3 used an obsolete inclusive any-one-team interpretation. See [`world-tower-contract-v3-campaign-review.md`](world-tower-contract-v3-campaign-review.md).

Generator-13 campaign `66368b83-07c1-4a7a-baf6-487c65fc8492` is historical evidence only. Generator 23 applies contract 4 during selection, creates a distinct scenario for every floor, and combines exact-floor-qualified discovery finalists with a bounded direct-search reserve. Campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` passed with 15 sets, 75 teams, and zero certification issues. Every team was below 20%, every floor had at least one strict anchor, and the largest estimate was 19%. The source-controlled approved catalog remains unchanged pending explicit review and promotion.

Therefore:

- World Tower recommendations are currently calibrated estimates for the canonical unprepared roster, not fully certified population-level guarantees.
- Raid recommendations remain authored guidance; their static recommended power values are not production-simulation certified.
- Other content either has synthetic diagnostic coverage or no player-facing recommended-power contract.

## World Tower campaign snapshot

Status captured on 2026-08-27 after completing the first generator-23 contract-4 campaign:

| Field | Current value |
| --- | --- |
| Current evidence campaign | `c27ad5ef-8483-4b30-b413-62e92f0443a1` |
| First fingerprint-contract-3 campaign | `e6b1d2a5-3f66-4b19-85ff-cde9e53af32e`; historical generator-7 failure |
| Contract-2 re-evaluation | `db118f5e-e968-4113-be25-3f17e6cdc53d` |
| Contract-3 re-evaluation | `72d8c717-7b5e-4b02-8dc9-eb59d5785094` |
| Generator-8 anchor campaign | `08e3c381-ad73-41a1-8f07-7b099154a14d`; failed at floor 4 |
| Generator-13 historical campaign | `66368b83-07c1-4a7a-baf6-487c65fc8492`; obsolete contract-3 pass, contract-4 fail on all floors |
| Floor coverage | 1–15 |
| Status | `Completed` |
| Completed audits | 5 of 5; 380,760 battles preserved |
| Current audit | None |
| Campaign model | Five-member canonical-role discovery, schema 4 campaign / fingerprint contract 3 |
| Candidate pool | 500 |
| Screening battles | 10,000 per seed |
| Seeds | 1,337; 2,027; 9,001 |
| Finalist matchup sampling | 34 fights × 3 seeds = 102 direct battles |
| Catalog generated | Yes; 15 profile sets and 75 teams |
| Catalog valid | Yes; zero validation issues |
| Historical campaign profile schema / generator | 7 / 7 |
| Current implementation profile schema / generator | 7 / 23 |
| Exact roster distribution | Six 5-slot, seven 10-slot, and two 15-slot floor scenarios |
| Candidate smoke | Passed |
| Generator-23 certification | Passed; 15 of 15 floors, zero issues, maximum team estimate 19% |
| Original contract-1 certification | Failed; 27 issues |
| Contract-2 re-evaluation | Failed; 15 issues (11 canonical, 4 floors without a qualifying team) |
| Contract-3 re-evaluation | Failed; 19 issues (9 canonical, 10 floors without a qualifying team) |
| Promotion ready | Yes as a candidate; source-control review is still required |
| Campaign error | None |

The earlier thirteen-audit campaign remains preserved separately as diagnostic and equipment-sensitivity evidence. Its 10/15-character simulations did not assign production party boundaries and must not be used as production profile evidence.

Campaign state is persisted outside the repository at:

`%LOCALAPPDATA%\LegendsLegacy\AdminDashboard\combat-audit-campaigns\c27ad5ef84834b30b41362e92f0443a1\campaign.json`

The earlier schema-2 and schema-3 campaigns remain preserved as historical diagnostic evidence. They must not be retried or promoted under the current role-aware discovery and Tower-qualification contract.

## What is implemented

### Shared production combat preparation and execution

- Production combat entry points use `ICombatPreparationPipeline` for live and snapshot combatants.
- Snapshot reconstruction preserves the combat build instead of mixing historical and current character state.
- Content identity is explicit through `CombatContentType`; Essence activity is selected from the real content type.
- Engine mechanics are centralized in immutable `CombatRuleset` values.
- Final teams come from engine execution rather than being inconsistently reconstructed afterward.
- `CombatResult` retains both the engine outcome and the content-specific evaluated outcome.
- Deterministic seeds and persisted snapshots support reproducible Tournament and Region Boss retries.
- Content-specific hooks preserve necessary differences including stagger, overtime, lane objectives, waves, recovery, fury, and forced training health.

### Character Profile generation

- Balance-audit finalists can produce deterministic, immutable combat profiles.
- Profiles use real equipment, Essences, canonical roles, snapshots, and production preparation.
- Expanded portfolios include Meta, Typical, Weak-but-Legal, Budget, Counter/Countered, Equal-Power Adversarial, Role Specialist, and No-Essence control families.
- Evidence gates include minimum battles, Wilson confidence width, cross-seed stability, direct matchup evidence, and Essence-overlap limits.
- Exact Tower scenario identity includes floor number, content, roster size, equipment tier, rarity, quality, equipment profile, and Essence count.
- World Tower discovery is constrained to five-character parties, while target expedition size and equipment are supplied separately.
- Tower discovery assigns the canonical Guardian, Restorer, Striker, Striker, and Controller roles before simulation and gives each role its actual canonical discovery-equipment attributes and role tag.
- Legacy non-role-aware audit signatures remain stable; role-aware signatures include role identity so saved candidates cannot be confused across the two contracts.
- Before Tower profile selection, every eligible finalist is materialized at the exact floor equipment rung and qualified through deterministic production-runtime samples. Selected teams carry 100-sample certification-manifest evidence.
- Generator 23 combines legal qualified finalists with a bounded reserve of up to 500 additional parties, reserves strict anchor coverage first, and fills the remaining four CalibrationTeam slots only from teams below the cap. All teams, including the No-Essence control, remain subject to the absolute below-20% gate.
- Qualification evidence records exact outcomes, timeout rate, duration, floor/team identity, seed-manifest provenance, cooldown policy, and production-runtime usage in each selected party.
- Five-, ten-, and fifteen-slot generated expeditions carry one, two, and three explicit party records, and party numbers survive catalog reconstruction and runtime creation.
- Expanded World Tower scenarios generate five deterministic expedition profiles: four exact-floor CalibrationTeams and one No-Essence control. Historical PvP family labels remain available to other profile modes but are not used to justify Tower legality.
- Catalog validation detects stale content, invalid loadouts, version drift, Power Rating drift, preparation drift, incomplete controls, and duplicate identities.
- Generated catalogs are review artifacts; the Admin Dashboard does not silently overwrite source-controlled approved content.

### World Tower calibration and certification

- The production calibration runner creates below-recommended, recommended, and stronger canonical cohorts for every authored floor.
- Canonical teams use real equipment construction, snapshots, production preparation, World Tower scaling, stagger, and the production runtime/executor path.
- The profile shadow runner compares approved profile populations with canonical cohorts without changing recommendations.
- Exact roster-size and scenario matching fail closed; smaller or differently equipped teams are not extrapolated.
- Certification uses one fixed common deterministic seed manifest for canonical cohorts, profile teams, anchor confirmation, and repeated campaigns; campaign IDs do not alter the measured cohort.
- Certification checks minimum samples, Wilson 95% intervals and monotonicity for canonical cohorts, an absolute strict-below-20% point-estimate cap for every selected profile team, at least one strict `>5%` and `<20%` anchor per floor, timeout rates, production-runtime evidence, and exact scenario coverage. Profile confidence width, weighting, and spread are diagnostic.
- Certification artifacts include deterministic fingerprints and versions for the catalog, inputs, seed manifest, preparation, rating, combat rules, equipment, roster, generator, runtime, architecture, and build configuration.
- The Admin-only audit campaign can run the five prerequisite party audits sequentially, persist evidence, recover after interruption, materialize every exact floor scenario, generate the Expanded catalog, and validate it.
- Current campaigns fingerprint complete Essence definitions, every canonical role discovery/materialization build, exact Tower definitions, guardian combat definitions, native abilities, Essence loot mappings, region scaling, and the qualification contract. They reject stale retry/resume attempts, reuse only compatible artifacts, and automatically run candidate smoke plus certification.
- Candidate calibration validates an in-memory campaign catalog and records candidate identity without reading or writing the approved catalog.

## Current approved-data state

The approved catalog at [`LL/src/API/API.LL/Data/combat/combat-character-profiles.json`](../../LL/src/API/API.LL/Data/combat/combat-character-profiles.json) currently contains:

```json
{
  "schemaVersion": 1,
  "catalogVersion": 1,
  "profileSets": []
}
```

This empty catalog is deliberate. It prevents unfinished or unreviewed audit results from becoming official calibration inputs. Approved-catalog World Tower runs therefore still report coverage gaps; the latest in-memory generator-13 candidate is rejected by corrected contract 4 and is not pending promotion.

## Confidence by content type

| Content | Current calibration state | Current confidence | Main missing proof |
| --- | --- | --- | --- |
| World Tower | High-fidelity canonical production calibration plus a passing 100-sample candidate certification | Medium-high for the candidate | Human promotion into the approved catalog, CI enforcement, and 500–1,000-sample release certification |
| Raid | Authored recommendations and a high-fidelity roster-specific Battle Plan preview | Low for static recommendations | Production calibration of every wing objective and full Raid outcome |
| Dungeon | Representative synthetic progression diagnostics | Not established for a displayed recommendation | Production-path recommendation runner and explicit success contract |
| Idle | Representative synthetic progression diagnostics | Not established for a displayed recommendation | Production-path recommendation runner and explicit success contract |
| Arena | Shared production combat; no recommendation calibration | Not applicable | Only needed if a recommended rating is introduced |
| Tournament | Shared production combat with overtime; no recommendation calibration | Not applicable | Only needed if a recommended rating is introduced |
| Region Boss | Rating-based matchmaking rather than a content recommendation | Not established as rating-to-outcome proof | Outcome calibration by rating band and build distribution |
| Quest Training | Shared production combat; no recommendation calibration | Not applicable | Only needed if a recommendation is introduced |

## What is still missing

### 1. Review and promote the World Tower candidate

Generator-23 campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` is the zero-issue corrected-contract candidate. Review its 15 floor-specific sets, 75 selected teams, exact-context evidence, and machine-readable certification report. If accepted, export the normalized catalog and commit it through the normal source-control review path. The Admin workflow does not modify the approved catalog automatically.

### 2. Archive release-scale World Tower certification

After the catalog is reviewed and committed:

- Preserve the passing 100-sample campaign report as the pull-request-scale evidence.
- Require every floor to have exact scenario coverage and a passing result.
- Require every selected team per floor to have a point estimate strictly below 20%, and at least one team per floor to be strictly above 5% and below 20%, while satisfying the sample-count, timeout, and production-runtime contract; preserve confidence intervals and population summaries as diagnostics.
- Run 500–1,000 samples for release certification.
- Archive the machine-readable certification artifact with the build/content fingerprints that produced it.

The existing ten-sample smoke test should remain a fast regression detector, but it is not sufficient evidence for a probability claim.

### 3. Add a headless CI and release gate

Certification currently exists as an Admin diagnostic workflow. A green build does not yet prove that an approved certification artifact exists for the current content.

The missing gate should:

- Load only the source-controlled approved catalog.
- Fail on stale or incomplete profile coverage.
- Run the deterministic 100-sample certification in the required build configuration.
- Fail on any certification issue or unapproved calibration exception.
- Compare fingerprints with the approved baseline so content/rules changes invalidate stale evidence.
- Support a separate release-scale 500–1,000-sample job.
- Preserve the complete report as a build artifact.

### 4. Make displayed Power Rating activity-specific

The displayed Power Rating can still be calculated without the exact content Essence activity used by the battle. This can make the number describe a different effective loadout from the one entering combat.

The rating path should require `CombatContentType` or `EssenceCombatActivity` and calculate readiness from the same immutable snapshot used for the encounter. If a global rating remains, it should be labelled as base power and shown separately from content-specific readiness.

This is required before claiming that a displayed recommendation and the simulated battle measure exactly the same build.

### 5. Implement production-path Raid calibration

Static `RecommendedWingPower` values are currently formula-authored and arithmetically tested, but not outcome-validated.

A `RaidProductionCalibrationRunner` should:

- Build immutable canonical and approved-profile rosters below, at, and above every wing recommendation.
- Execute Rearguard, Vanguard, Main Guard, and the full final assault through `RaidCombatResolver`.
- Validate lane-specific objectives and final `Repelled`, `Wounded`, `Broken`, and `Slain` outcomes.
- Cover regular tiers and representative +levels.
- Use common seeds, confidence bounds, monotonicity, diversity gates, and complete provenance.

### 6. Extend approved profiles to other recommendation runners

World Tower is currently the only content with a profile shadow consumer. Raid, Dungeon, Idle, and future recommendation systems should materialize approved profiles through the same profile materializer and preparation pipeline instead of maintaining separate calibration-only entity construction.

The existing synthetic encounter matrix remains useful for broad mechanics exploration, but it should be clearly labelled diagnostic and must not be treated as production recommendation proof.

### 7. Make all calibration exceptions enforceable

Some synthetic encounter and stagger tests produce exception collections without requiring every exception to be absent. Their current baseline comparison can demonstrate deterministic serialization without proving conformance to an approved balance baseline.

CI needs an explicit exception allowlist containing:

- Stable exception identity.
- Owner.
- Reason.
- Expiry or review date.
- Relevant content fingerprint.

Every unapproved or expired exception should fail CI.

### 8. Remove remaining source-of-truth drift

World Tower calibration and finalist qualification now load the same persisted guardian definition used by production. The guarded materialization fingerprint also includes exact Tower definitions, guardian combat fields, native abilities, Essence loot mappings, and the region-scaling catalog, so those changes force requalification instead of reusing stale profile selections.

The remaining work is to apply the same source-input fingerprint discipline to future Raid, Dungeon, Idle, and other production calibration runners.

### 9. Define one explicit meaning for each recommendation

The meaning of “recommended” is not uniform. Early World Tower floors target high success rates while later floors target a near-even result, and Raid recommendations do not currently encode a target outcome.

Every recommendation should publish:

- The exact content objective.
- The represented population/build policy.
- Whether the rating is per character, average party rating, or total party rating.
- Assumed preparation contribution.
- Expected success interval.
- Applicable content and profile versions.

### 10. Close the loop with production telemetry

Offline simulation cannot fully represent live build choices or player behavior. Privacy-safe aggregate telemetry should compare real outcomes with predicted confidence intervals by content version, rating band, party composition, preparation level, timeout rate, and objective result.

Telemetry should trigger review when reality diverges from certification. It should validate recommendations, not silently rewrite them.

## Required definition of done

A player-facing recommended power value should be considered release-grade only when all of the following are true:

1. The displayed readiness rating is calculated from the exact activity-specific immutable snapshot used in combat.
2. Calibration uses the production preparation pipeline, runtime/resolver, ruleset, engine executor, and objective evaluator.
3. Below, recommended, and stronger cohorts exist for every authored recommendation.
4. The recommendation has a documented outcome contract.
5. Approved profiles represent strong, typical, specialist, imperfect, and adversarial legal builds.
6. Exact scenario coverage, canonical confidence and monotonicity, the universal strict-below-20% profile cap, and the at-least-one strict `>5%` and `<20%` anchor gate pass; all profile confidence intervals, population outcomes, and spread are retained diagnostically.
7. Pull-request certification uses at least 100 samples and release certification uses 500–1,000.
8. Complete version and content provenance is recorded.
9. Unapproved calibration exceptions fail CI.
10. Production telemetry remains compatible with the certified prediction.

## Recommended execution order

1. Review and explicitly promote the contract-4-compliant generator-23 floor-specific profile catalog.
2. Add the headless CI certification gate.
3. Run and archive 500–1,000-sample release certification.
4. Complete displayed readiness Power Rating parity with the exact activity snapshot.
5. Implement the production-path Raid calibration runner.
6. Extend profile-backed production calibration to Dungeon, Idle, and other applicable content.
7. Enforce approved exception baselines and source-data parity.
8. Add privacy-safe production outcome validation.

## Current conclusion

The shared combat architecture and World Tower certification machinery are in place. What is missing is primarily approved evidence, automated enforcement, activity-specific rating parity, cross-content production calibration, and real-world validation.

Generator-13 evidence does not pass the corrected World Tower contract. Generator 23 now enforces the absolute cap during floor-specific selection, and campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` passes it. Cross-team spread and profile confidence width remain diagnostic. The next actions are catalog review and promotion, CI enforcement, and release-scale evidence.
