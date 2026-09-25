# Supported affinity search implementation — 25 September 2026

The target is the offline `LL/tools/BalanceHarness` application. The supported profile is original affinity creation plus benchmark validation. The experimental comparison changes only challenger eligibility. Neither change establishes that the search beats the confirmed benchmark.

## Changed components

- `TowerAffinitySearch.cs` provides the supported plan factory, validation, native execution adapter, concise result and `tower-affinity-search-check` / `tower-affinity-search-verify` commands. `Program.cs` routes those commands.
- `TowerAffinityNominationComparison.cs` defines the explicit comparison. `TowerBenchmarkValidation.cs` and `TowerBatchRacing.cs` exclude all three references only for experimental racing v9. Existing versions retain their behaviour.
- The proposal policy, comparison, study and protocol files recognize the new version, enforce the shared seed layout and require the existing resource envelope. Both arms must have identical first 408 observations; shared validation observations must also agree.
- The independent Python auditor reconstructs the new selection rule and enforces the frozen advancement criteria. The existing owned launcher accepts the explicit comparison version.
- Native tests, the separate literal fixture host, Python regression tests, qualification/admission helpers and the [usage guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) cover operation and verification.

## Verification

- 92 targeted backend tests passed for the supported profile, benchmark validation, native reconstruction and comparison contracts.
- Four captured-context maximum-workload tests passed, covering placement compatibility and generated-only nomination. Backend tests ran through `build/run-tests.ps1`.
- 31 Python tests passed: 25 existing arithmetic/owner checks and six nomination/fixture regressions.
- All 2,098 captured combat preparations reproduced exactly. Gameplay dependencies were retained byte-for-byte; only the harness DLL and producing symbols changed.
- A 21,888-report synthetic study covered all 24 searches, 36 distinct held-out members, both independent audits and production publication. Read-only final verification then passed. This is engineering evidence, not evidence of better teams.
- Both supported CLI commands passed against the qualified synthetic archive. The verification command returned `ChallengerNeedsConfirmation` for the fixture's deliberately passing challenger.

The first fixture wrapper failed when its outer storage monitor used the wrong root for a disappearing native lease. The corrected attempt completed all reports, both audits and publication, but the outer monitor then mishandled a released registry lease during extra verification. Both records remain preserved. The published archive was verified directly without regenerating reports. Regression tests now cover both lease boundaries. Their full declared fixture charges remain recorded. No scientific seeds were used by these fixtures.

A build initially lacked assets in a new artifact directory; the existing restored test build succeeded. The qualified fixture restore initially encountered an inaccessible user NuGet configuration; an isolated process-local configuration resolved it. One test invocation supplied nomination inputs to the historical placement case; the complete suite passed with the intended captured source. No verification command remains blocked.

Key evidence:

- Runtime qualification: `TestResults/affinity-nomination-runtime-20260925/files.json`, SHA-256 `7716aff915e9c807e47a885d2c8ce568ed3bbca681cf736d48143b8c02eec45d`.
- Maximum-workload final verification: `TestResults/affinity-nomination-maximum-verification-20260925/files.json`, SHA-256 `23d475110ad55ab6c580245008d0241a910bf3d4007e9958fc2c232d9c45f744`.
- Fresh admission: `TestResults/affinity-nomination-admission-20260925/files.json`, SHA-256 `e9250de72dc581f3b1845ad7f1df9307490de870ed78b98af0df721ade239a5c`.

The [prospective design](Tower-Affinity-Search-Consolidation.md) fixes the single scientific attempt and stopping rule. The supported API still runs inside the existing admitted archive owner; this change adds no standalone unowned launch path or game UI integration. There are no database migrations, deployment changes or gameplay configuration changes.
