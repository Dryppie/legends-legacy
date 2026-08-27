# P0 Balance Infrastructure Cleanup

This cleanup establishes the repository boundary for the replacement automated balance and content calibration system.

## Deleted

- The `BalanceCalibration` command-line tool and its encounter-calibration script.
- Authored encounter, Essence progression, and Stagger calibration runners and report renderers.
- World Tower production calibration, profile shadow calibration, certification, and audit-campaign orchestration.
- Generated combat-character profile services, profile catalogs, materialization, qualification, and committed profile data.
- Calibration-only progression and Essence-loadout manifests.
- Admin API endpoints and dashboard controls for generated profiles and World Tower calibration campaigns.
- Tests and reports dedicated only to the retired calibration architecture.

## Preserved

- The Admin Dashboard Essence Simulator, including 1v1/3v3 and larger team simulations, repeated seeded battles, Essence win-rate results, the multi-seed Essence audit view, API endpoints, exports, and dashboard presentation.
- The production combat engine, combat preparation and snapshots, Essence definitions and behavior, equipment/stat calculations, Combat Rating calculation, and PvE/PvP combat paths.
- Canonical equipment/role construction used by the preserved Essence Simulator.
- Ability catalog diagnostics and coverage checks.
- The idle-combat performance benchmark, which measures production catch-up performance and is not a balance-calibration architecture.
- Historical EF Core migrations, including the migration that removed earlier balance cache tables, so database migration history remains valid.

## Refactored

- Admin diagnostics dependencies now stop at the preserved Essence Simulator/audit services and no longer resolve retired profile or World Tower calibration services.
- The Essence audit content fingerprint now fingerprints only the content required by the preserved Essence analysis; profile-discovery and materialization fingerprints were removed.
- The unified content-scaling document now treats the retired calibration implementation as historical and points future offline analysis to the replacement plan.

## Remaining

- `AbilityBalanceSimulator`, `AbilityBalanceAuditService`, their contracts, tests, and dashboard UI remain intentionally because they implement the explicitly protected Admin Dashboard Essence Simulator and its legitimate Essence-analysis workflow.
- `CanonicalEquipmentBuildFactory` and `CanonicalCooperativeRosterCatalog` remain because the protected simulator uses them to build production-faithful role/equipment snapshots. They are reusable low-level construction infrastructure, not orchestration for the retired calibration system.
- Production classes whose names contain `Balance` (for example reward or region balance providers) remain because they load live gameplay configuration rather than run automated balance analysis.
- The idle-combat benchmark remains because it is a reproducible performance-regression harness for production combat, not content difficulty or Combat Rating calibration.

## Operational Confirmation

The Admin Dashboard Essence Simulator remains wired end-to-end through:

1. `DiagnosticsController` simulation and audit endpoints.
2. `IAbilityBalanceSimulator` / `AbilityBalanceSimulator` and `IAbilityBalanceAuditService` / `AbilityBalanceAuditService`.
3. Production ability, Essence, equipment, stat, snapshot, Combat Rating, and combat-engine services.
4. The dashboard diagnostics service, balance tab, result tables, saved combinations, and JSON/CSV exports.

No compatibility adapter or legacy runner remains between the preserved admin feature and production combat infrastructure.
