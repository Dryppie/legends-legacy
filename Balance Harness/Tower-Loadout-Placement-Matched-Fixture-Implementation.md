# Matched placement fixtures and selected-team audit repair

**Status: matched fixture implementation verified; the first cost attempt failed its independent audit and remains permanently failed. No current resource forecast, runtime qualification or scientific admission is available.**

The [frozen matched-fixture plan](Tower-Loadout-Placement-Matched-Fixture-Plan.json) was executed once. Both packages passed exact context, settings, runtime/PDB, synthetic allocation, evaluator and twelve-root common-v5 plan checks before either owner started. The baseline completed 17,280 literal requests and native reconstruction. The independent Python audit rejected a valid physically deduplicated selected team. The runner retained the entire failed attempt and stopped before starting the placement measurement.

The [sealed failed package](../TestResults/loadout-placement-matched-owned-20260924/files.json) has SHA-256 `7dd1df4ab8ea9d24c53d50cac4137bdb59351a56110e9642cd791474e3f02605`. Its original runner, auditor, test executable, declaration, packages, logs, native result and failed receipt remain unchanged. There was one baseline attempt, zero placement attempts and zero timing retries. The production harness remains `a2bd39956ffdc56c352336019960494a5c501a4afff61f4488e8b45635cceff9`; its producing PDB and dependency manifest also match the authenticated source fixture.

## Implementation and the audit defect

`ProposalStudyFixtureHost.cs` now has two explicit, test-only matched profiles. They derive v4/v5 and v5/v6 plans from the same authenticated ten-actor, five-slot placement context. Both use reference draws, generated-party victories, the frozen odd/even validation rule and held-out victories. Production combat entry still throws. A preparation receipt binds the common physical inputs, synthetic values and all twelve v5 plans.

`build/test-proposal-affinity-study-owned.py` separates preparation from execution, preserving its existing default fixture assertions. The new `build/test-loadout-placement-matched-owned.py` prepares and compares both packages before measurement, runs the existing owner and auditors, retains failures, and implements the frozen upward-only cost formula. Final closeout audit/publication costs remain binding. Changing the output path cannot create a replacement attempt under this registration. Externally pinned verification can authenticate the retained failed package without inventing a completion or forecast.

On odd roots, v4 and v5 chose identical physical teams whose `PartyChoice.source` fields were respectively `tower-proposal-policy-v4` and `tower-proposal-policy-v5`. Native code groups by physical recipe and retains the first representative in control, candidate, benchmark order. Python previously required the retained representative to equal every role's complete metadata record. That rejected valid differing provenance.

The corrected independent auditor reconstructs the physical groups, requires their complete sorted role membership, and authenticates the exact first representative, including its provenance. It also computes selected IDs, identity and novelty from the physical groups, matching the native contract. It still rejects changed builds, changed representative metadata, missing or duplicated roles, split physical groups and incomplete studies. No production algorithm or native evidence format changed.

## Verification

- Backend wrapper: `build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessMatchedPlacementFixtureTests' -ArtifactsPath '.artifacts/loadout-placement-comparison-20260924'`: **6 passed**.
- `build/test-loadout-placement-matched.py`: **11 passed**.
- `build/test-proposal-selected-members.py` with the retained failed baseline and the externally pinned completed placement archive: **9 passed**.
- `build/test-proposal-affinity-study.py ArithmeticTests`: **25 passed**.
- `build/test-proposal-resource-envelope.py`: **5 passed**.
- Existing owned failure fixture: passed; one charged synthetic attempt, all 16,384 synthetic values retained, no audit/publication after the injected native failure.
- All twelve failed-baseline v5 archives are **byte identical** to the authenticated original placement fixture's v5 archives, including proposals, pruning, panels, selections, recipes and battle rows. No normalization was necessary. This is retained functional evidence; the new paired placement measurement did not run.
- The corrected auditor still verifies the original completed 18,816-report placement archive. A test-only, in-memory isolation of the failed baseline's terminal guard reconstructs its 17,280 saved rows and exactly matches native reconstruction. The public audit rejects that failed archive both before and after the diagnostic. No source evidence or failed receipt is edited, and no completion is published.

There are **50 passing Python tests** and **6 passing backend tests**. Logs, before snapshots, retained common-v5 proof and the failed-package verification are in the [implementation evidence](../TestResults/loadout-placement-matched-fixture-implementation-20260924).

The initial backend build was denied access to the existing user NuGet configuration; the approved wrapper invocation succeeded. An initial regression invocation supplied an incorrect closeout pin and was correctly rejected; the corrected complete invocation passed all nine tests. The full checkout's whitespace check reports pre-existing trailing spaces in `LL/docs/strongholds-mechanical-progression-revision.md`; that unrelated file was not changed. No required test command remains blocked.

## Resource accounting and next gate

The declaration charged the complete engineering pair allowance at the start: **24,420 seconds and 13,019,119,616 bytes**, including the unchanged two owned-v2 limits and separately declared preparation, post-publication verification and analysis support. The unused placement allowance is not reclaimed. The failed pair elapsed 196.406 seconds before sealing. Its native receipt records 146.6040994 measured seconds and 1,078,739,935 observed bytes; these are incomplete-attempt evidence, **not** completed baseline phase costs. The separate injected-failure regression is an ordinary functional test, not another cost sample.

The frozen formula requires the first matched baseline attempt to complete. It did not. Consequently there is no raw matched cost ratio, no repaired resource forecast and no basis to start the separate 900-second/1-GiB qualification. The candidate package is prepared but unexecuted. The old placement measurements, inherited floors, margin two, publication reserve, resource limits and all historical pins remain unchanged.

Next is a separately preregistered recovery protocol that explicitly handles this failed first-baseline evidence and the corrected auditor. It must retain the failed attempt and all charged allowances, prohibit choosing a faster replacement timing, and leave this frozen experiment's failed status intact. A valid matched result and guarded current-runtime qualification are still required before scientific admission. This turn does not amend the frozen no-retry contract.

No real combat, production entropy, new scientific reservation, live-history rescan, migration, application configuration change, deployment or external-environment operation occurred.
