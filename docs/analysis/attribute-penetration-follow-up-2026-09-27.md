# Flat penetration follow-up — 27 September 2026

Subsequent [penetration price review](penetration-price-review-2026-09-27.md): 62,944 study fights support **4 budget per point as the next candidate price**, retaining this formula and cap. The review did not apply that price; production source still specifies 1.5.

The user selected **subtraction of percentage points**, specifically 60% mitigation becoming 20% with 40 penetration. This is implemented in the unreleased version 18 candidate. Version 17 remains the default live selection.

## Rule and boundaries

For the relevant damage channel:

```text
netRating = max(0, rating) * (1 - clamp(Corrosion, 0, 50) / 100)
defenseMitigation = 0.8 * netRating / (netRating + 165)
hitMitigation = max(0, defenseMitigation - clamp(penetration, 0, 40) / 100)
```

Armor Penetration opposes Armor; Magic Penetration opposes Resistance. The 40-point cap includes ability penetration bonuses. Corrosion operates on rating before the curve; penetration operates on the resulting mitigation. Block and general Damage Reduction retain their separate damage steps. Penetration cannot produce negative mitigation or extra damage against an undefended target.

Examples: 60% mitigation minus 40 points leaves 20%; 25% minus 40 leaves zero. With rating 495, 50 Corrosion produces 48% mitigation, then 40 penetration leaves 8%. A 100-damage hit against 60% typed mitigation, 40 penetration and 25% general Damage Reduction deals 60 damage before any other effects.

Raw over-cap contributions remain available so temporary buff removal is reversible. Current equipment allocation caps each penetration stat at 40 and redirects excess allocation into the existing core overflow. Character projections and comparison cap waste use the selected character rules version. Bootstrap metadata supplies the corresponding cap and description to the frontend.

Version 17 retains its rating-based penetration, 60% cap, frozen reinforcement and authored base-modifier behavior. No additional migration, configuration key, activation or database write was required. The candidate equipment price remains 1.5 budget per penetration point; this change does not certify that price.

## Changed files

- Domain: `AttributeRules.cs`, `AttributeCatalog.cs` and `AttributeCalculator.cs` implement the formula and versioned effective caps.
- Equipment: `EquipmentStatBudgetCatalog.cs`, `EquipmentEvaluator.cs`, `EquipmentData.cs` and `EquipmentInstance.cs` apply current allocation caps while preserving historical item paths.
- Application/services: `GetGameBootstrapQuery.cs` selects the live metadata version; `CombatRatingCalculator.cs` explicitly retains its historical diagnostic contract.
- Tests: new `PenetrationRulesTests.cs`, updated `AttributeRedesignTests.cs` and `AttributeCombatSystemTests.cs` cover the new semantics and old compatibility.
- Records: implementation, rollout and previous study reports link this amendment and identify the earlier formula used by their evidence.

## Verification

The Release build passed with no errors and existing warnings. Backend execution used the required wrapper:

```powershell
./build/run-tests.ps1 -Configuration Release -Filter 'FullyQualifiedName~PenetrationRulesTests|FullyQualifiedName~AttributeRedesignTests|FullyQualifiedName~AttributeCombatSystemTests|FullyQualifiedName~EquipmentComparisonProjectorTests|FullyQualifiedName~EquipmentMigrationTests|FullyQualifiedName~EquipmentBlueprintTests|FullyQualifiedName~ArmorBalanceTests|FullyQualifiedName~CombatRating'
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~Equipment|FullyQualifiedName~Attribute|FullyQualifiedName~GameBootstrap|FullyQualifiedName~Snapshot|FullyQualifiedName~CombatPreparation|FullyQualifiedName~CombatRating|FullyQualifiedName~CanonicalEquipmentBuild'
```

Results: **124 focused tests passed**, then **466 related regressions passed with one expected skip** for the opt-in PostgreSQL rehearsal. These selections overlap; their counts are not distinct-test totals. TRX files are retained at `TestResults/penetration-focused-20260927.trx` and `TestResults/penetration-regressions-20260927.trx`. Logs are in TEMP as `ll-penetration-tests-20260927.log` and `ll-penetration-regressions-20260927.log`.

Coverage includes both damage channels through the actual engine, ability bonuses, cap/floor/Corrosion ordering, independent general damage reduction, versioned metadata and projections, comparison waste, reversible over-cap buffs, historical frozen reinforcement, and every catalog definition at maximum tier/rank/quality/roll with budget preservation and a 40-point item cap.

No verification command remains blocked. The full backend/Angular suites and disposable PostgreSQL rehearsal were not repeated for this adjustment. Earlier broad verification and the copied-player rehearsal remain dated evidence as described in the implementation record. `git diff --check` passed.

## Bounded balance comparison

Four existing equal-budget PvP fixtures were selected before running: the matched damage-over-time caster with precision/haste alternatives, and the no-relic basic maul with precision/speed alternatives. Both references use their default penetration specialization. The request fixes 8 exploration seeds (100001–100008) and 32 held-out confirmation seeds (101001–101032), both starting sides and 640 fights per executable. The no-relic, zero-Essence physical build is a deliberately simplified diagnostic.

The current build ran 640 fights; the retained earlier executable then ran the same recipes and seeds for another 640. Content hashes were identical. This compares the earlier candidate with the revised formula **and** cap/overflow behavior, not a formula-only causal intervention. Both executions use nominal rules version 18 because the candidate has not been activated; their manifest assembly hashes distinguish them.

Each number below is the alternative's confirmation win-rate difference relative to its default penetration reference within that executable. Positive favors the alternative; negative favors the penetration reference. Mirrors share a seed and are not independent observations.

| Alternative | Earlier rating-based penetration | Revised flat penetration |
|---|---:|---:|
| Caster precision | +31.25 pp | -23.44 pp |
| Caster haste | +28.12 pp | -40.62 pp |
| Basic maul precision | -37.50 pp | -50.00 pp |
| Basic maul speed | +50.00 pp | -40.62 pp |

This is a substantial balance change: the default penetration build wins every selected allocation comparison under the revised rules, including reversals for caster precision/haste and physical speed. Four fixtures cannot establish a universal ranking or a suitable new price. Keep activation pending balance acceptance; do not carry forward the earlier screen's price recommendation without new evaluation. No automatic stat-price retuning occurred.

Evidence is retained in ignored local artifacts:

- Current request: `TestResults/attribute-penetration-smoke-request-20260927.json`; results: `TestResults/attribute-penetration-smoke-20260927`.
- Earlier request: `TestResults/attribute-penetration-before-smoke-request-20260927.json`; results: `TestResults/attribute-penetration-before-smoke-20260927`; executable: `TestResults/attribute-executable-20260927/BalanceHarness.dll`.
- Both outputs include frozen content/builds, request/execution hashes, raw trials, estimates, rankings and detailed first-seed replays. Current trials SHA-256: `f2356f7de33173ba4b0f686dffcfd1cde2276cb92ce4394efa56417f5bc80c4b`; earlier trials SHA-256: `bdcf6f7260352deb042685e98d18540d0a9556d6b6ac3b6e335b59d0024fa8b1`.

Commands used after the Release build:

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll attribute-allocation-study TestResults/attribute-penetration-smoke-request-20260927.json
dotnet TestResults/attribute-executable-20260927/BalanceHarness.dll attribute-allocation-study TestResults/attribute-penetration-before-smoke-request-20260927.json
```

Reruns require new output directory names; existing evidence must not be overwritten. The original 25 September and earlier 27 September studies remain preserved with their original formula and executable.
