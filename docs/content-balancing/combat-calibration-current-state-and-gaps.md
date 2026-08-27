# Combat calibration: current state and missing work

## Purpose

This document records the current state of the shared combat system, Character Profile tooling, and recommended-power calibration work. It distinguishes implemented infrastructure from completed balance evidence so that an available tool is not mistaken for a certified player-facing recommendation.

The detailed design and historical analysis remain in:

- [`combat-system-analysis.md`](combat-system-analysis.md)
- [`character-profile-generator-design.md`](character-profile-generator-design.md)

## Executive summary

The combat execution architecture is now substantially unified. Production combat types prepare combatants through the shared `CombatPreparationPipeline`, execute immutable `CombatRuleset` settings, and retain both engine and content outcomes. Content-specific behavior such as World Tower stagger, Tournament overtime, Raid objectives, Region Boss waves, and Dungeon modifiers remains explicit around that shared foundation.

The Character Profile and World Tower calibration infrastructure is also implemented. It can discover strong and representative Essence combinations, create complete production-valid profiles, validate a versioned catalog, run those profiles through the real World Tower runtime, and produce statistical certification evidence.

The first schema-4 campaign, `6339d840-f00a-4630-a869-d5ad862a3bd1`, completed all five audits, catalog generation, smoke, and 100-sample certification on 2026-08-26. It retained 380,760 discovery battles and produced all 13 exact Tower scenarios, but certification correctly blocked promotion with 29 issues: ten canonical confidence findings, eight profile confidence findings, and eleven profile-spread findings. Several were genuine point-estimate or core-profile spread failures, while some were low-sample confidence ambiguity and three spread findings were caused only by diagnostic stress profiles.

That failed campaign exposed a more fundamental modeling discrepancy: Essence discovery assigned identical balanced attributes to every participant, while generated Tower profiles mapped those positions to different canonical roles and equipment. Profile selection also used PvP discovery scores without first testing finalists against the actual floor guardian, stagger setup, and production playback path. The implementation now corrects both issues. Discovery is role-aware, and a bounded production Tower qualification runs for eligible finalists before profile-family selection. These changes advance the profile schema/generator to 7/7 and fingerprint contract to 3, so all earlier catalogs and campaign evidence are historical rather than promotable. A fresh campaign is required to measure the corrected model.

Therefore:

- World Tower recommendations are currently calibrated estimates for the canonical unprepared roster, not fully certified population-level guarantees.
- Raid recommendations remain authored guidance; their static recommended power values are not production-simulation certified.
- Other content either has synthetic diagnostic coverage or no player-facing recommended-power contract.

## World Tower campaign snapshot

Status captured on 2026-08-26 after completing and independently checking the replacement campaign:

| Field | Current value |
| --- | --- |
| Campaign ID | `6339d840-f00a-4630-a869-d5ad862a3bd1` |
| Floor coverage | 1–15 |
| Status | `Completed` |
| Completed audits | 5 of 5; 380,760 battles preserved |
| Current audit | None |
| Campaign model | Five-member discovery, schema 4 campaign / pre-role-aware fingerprint contract |
| Candidate pool | 500 |
| Screening battles | 10,000 per seed |
| Seeds | 1,337; 2,027; 9,001 |
| Finalist matchup sampling | 34 fights × 3 seeds = 102 direct battles |
| Catalog generated | Yes; 13 profile sets and 130 teams |
| Catalog valid | Yes; zero validation issues |
| Profile schema / generator | Historical run: 6 / 6; current implementation: 7 / 7 |
| Exact roster distribution | Five 5-slot, six 10-slot, two 15-slot scenarios |
| Candidate smoke | Completed |
| 100-sample certification | Failed; 29 issues |
| Promotion ready | No |
| Campaign error | None; failure is represented by certification gates |

The earlier thirteen-audit campaign remains preserved separately as diagnostic and equipment-sensitivity evidence. Its 10/15-character simulations did not assign production party boundaries and must not be used as production profile evidence.

Campaign state is persisted outside the repository at:

`%LOCALAPPDATA%\LegendsLegacy\AdminDashboard\combat-audit-campaigns\6339d840f00a4630a869d5ad862a3bd1\campaign.json`

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
- Exact scenario identity includes content, roster size, equipment tier, rarity, quality, equipment profile, and Essence count.
- World Tower discovery is constrained to five-character parties, while target expedition size and equipment are supplied separately.
- Tower discovery assigns the canonical Guardian, Restorer, Striker, Striker, and Controller roles before simulation and gives each role its actual canonical discovery-equipment attributes and role tag.
- Legacy non-role-aware audit signatures remain stable; role-aware signatures include role identity so saved candidates cannot be confused across the two contracts.
- Before Tower profile selection, every eligible finalist is materialized at the exact scenario equipment rung and tested on every target floor with ten deterministic production-runtime samples.
- Qualification evidence records exact outcomes, timeout rate, duration, floor/team identity, seed-manifest provenance, cooldown policy, and production-runtime usage in each selected party.
- Meta/Budget prioritize worst-floor and average Tower performance; Typical targets the scenario success band; Weak-but-Legal uses the weakest Tower-qualified candidate. PvP discovery score is secondary when Tower evidence exists.
- Five-, ten-, and fifteen-slot generated expeditions carry one, two, and three explicit party records, and party numbers survive catalog reconstruction and runtime creation.
- Expanded World Tower scenarios generate ten deterministic expedition profiles, including Meta/Typical and role-specialist mixtures. Each party retains independent statistical evidence; composed expeditions do not claim unobserved team-level audit results.
- Catalog validation detects stale content, invalid loadouts, version drift, Power Rating drift, preparation drift, incomplete controls, and duplicate identities.
- Generated catalogs are review artifacts; the Admin Dashboard does not silently overwrite source-controlled approved content.

### World Tower calibration and certification

- The production calibration runner creates below-recommended, recommended, and stronger canonical cohorts for every authored floor.
- Canonical teams use real equipment construction, snapshots, production preparation, World Tower scaling, stagger, and the production runtime/executor path.
- The profile shadow runner compares approved profile populations with canonical cohorts without changing recommendations.
- Exact roster-size and scenario matching fail closed; smaller or differently equipped teams are not extrapolated.
- Certification uses common deterministic seeds for canonical and profile cohorts.
- Certification checks minimum samples, Wilson 95% intervals, canonical monotonicity, profile outcome spread, timeout rates, and exact scenario coverage.
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

This empty catalog is deliberate. It prevents unfinished or unreviewed audit results from becoming official calibration inputs. It also means that profile-based World Tower coverage and certification cannot currently pass.

## Confidence by content type

| Content | Current calibration state | Current confidence | Main missing proof |
| --- | --- | --- | --- |
| World Tower | High-fidelity canonical production calibration plus implemented profile shadow/certification tooling | Medium | Approved profiles and passing 100/500–1,000-sample certification |
| Raid | Authored recommendations and a high-fidelity roster-specific Battle Plan preview | Low for static recommendations | Production calibration of every wing objective and full Raid outcome |
| Dungeon | Representative synthetic progression diagnostics | Not established for a displayed recommendation | Production-path recommendation runner and explicit success contract |
| Idle | Representative synthetic progression diagnostics | Not established for a displayed recommendation | Production-path recommendation runner and explicit success contract |
| Arena | Shared production combat; no recommendation calibration | Not applicable | Only needed if a recommended rating is introduced |
| Tournament | Shared production combat with overtime; no recommendation calibration | Not applicable | Only needed if a recommended rating is introduced |
| Region Boss | Rating-based matchmaking rather than a content recommendation | Not established as rating-to-outcome proof | Outcome calibration by rating band and build distribution |
| Quest Training | Shared production combat; no recommendation calibration | Not applicable | Only needed if a recommendation is introduced |

## What is still missing

### 1. Complete and approve the World Tower profile evidence

This is the immediate blocker.

1. Run a fresh one-click campaign under fingerprint contract 3 so all five audits use canonical roles.
2. Confirm every non-control selected party contains complete production Tower qualification for all scenario floors.
3. Review all 13 exact-floor profile sets, especially Meta/Typical/Weak ordering, direct matchup evidence, Budget, Counter/Countered, and adversarial families.
4. Export the normalized catalog and complete evidence bundle only after certification passes.
5. Commit the reviewed catalog to `combat-character-profiles.json` through normal source-control review.

Until this is done, the profile shadow and certification runners must continue to fail closed on missing coverage.

### 2. Run and approve World Tower certification

After the catalog is committed:

- Run the certification suite with at least 100 samples per canonical cohort and profile team for pull-request evidence.
- Require every floor to have exact scenario coverage and a passing result.
- Resolve confidence-band, monotonicity, profile-spread, and timeout failures rather than weakening thresholds without documented balance reasoning.
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
6. Exact scenario coverage, confidence intervals, monotonicity, profile spread, and timeout gates pass.
7. Pull-request certification uses at least 100 samples and release certification uses 500–1,000.
8. Complete version and content provenance is recorded.
9. Unapproved calibration exceptions fail CI.
10. Production telemetry remains compatible with the certified prediction.

## Recommended execution order

1. Run the new role-aware, Tower-qualified World Tower campaign.
2. Inspect the new certification report and separate statistical ambiguity from actual balance failures.
3. Resolve actual canonical/profile failures and rerun; increase certification samples when the point estimate is acceptable but the interval is inconclusive.
4. Review and commit the generated World Tower profile catalog only after it passes.
5. Add the headless CI certification gate.
6. Run and archive 500–1,000-sample release certification.
7. Make displayed readiness Power Rating activity-specific.
8. Implement the production-path Raid calibration runner.
9. Extend profile-backed production calibration to Dungeon, Idle, and other applicable content.
10. Enforce approved exception baselines and source-data parity.
11. Add privacy-safe production outcome validation.

## Current conclusion

The shared combat architecture and World Tower certification machinery are in place. What is missing is primarily approved evidence, automated enforcement, activity-specific rating parity, cross-content production calibration, and real-world validation.

The next decision should be based on a fresh fingerprint-contract-v3 campaign using role-aware discovery and production Tower qualification—not on the older completed campaign or merely on the fact that the tooling exists.
