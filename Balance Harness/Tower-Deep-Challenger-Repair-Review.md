# Deep policy repair passed; control adapter preparation stopped

15 September 2026. Target: offline BalanceHarness. **Generation repair verified; overall preparation StoppedNoRetry and not ready for binding or combat.** This separately authorized [repair](Tower-Deep-Challenger-Repair-Protocol.md) preserves the [earlier failed preparation](Tower-Deep-Challenger-Preparation-Review.md). Zero fights, fresh seed candidates or reservations.

## Completed repair

Added the new policy to five existing gates: coordinated loadout acceptance in `TowerLoadoutComposition.cs`, required mechanic cores and creation of cores/coverage in `TowerBossPartyGenerator.cs`, and required coverage validation in `TowerPartyCoverage.cs`. Historical policy branches, operators, RNG streams and budgets are unchanged.

Five added test cases actually execute new-policy generation, require all four loadout operations, compare the complete generated deep arm with the old portfolio component, reject missing cores/coverage and cancel at fixture evaluations 384 and 400 while retaining partial attempts. This replaces the prior coverage gap where the new-policy tests only relabeled existing generated arms.

**91/91 tests passed** through `build/run-tests.ps1` in **39.90 seconds**, first invocation in this repair. The isolated captured-gameplay build passed in **2.98 seconds**. All tests used synthetic measurement callbacks, with no combat engine calls.

The once-only native diagnostic then recreated archived mechanics/coverage from captured content and reproduced **all 4,608 evaluations and 4,750 proposals** across three deep arms. Entire saved arms match, including every ordered recipe, operator, parent, measurement, attempted proposal and stop reason. The emitted `replayed-arms.json` and `replay-attempt.json` retain this successful result. A read-only inspection of those saved outputs also found exact object equality with the archived arms and mechanics.

## Remaining adapter defect

The overall diagnostic failed after **9.44 seconds** when preparing the first control. The adapter first replaces the saved scenario's seed list with an empty list for its future non-runnable template, then passes that same scenario to `TowerBattleRunner.CreateInput`. That method correctly requires a nonempty declared list containing the chosen preparation seed.

```text
Tower requires schema 1, distinct declared seeds and an uncleared floor without contributions.
```

This is a separate defect in the preparation adapter, not another failure of the repaired generation path. The input validation was not weakened. No `PrepareAsync` or combat call was reached for either control. The two control participant hashes, new +10% mechanics comparison, live registry union and confidence-output checks therefore remain **unverified by this package**. The full independent readiness audit could not run because its successful-native-result prerequisite is absent.

The next repair must use a temporary scenario declaring the chosen **already reserved historical** seed when preparing each control, while retaining an empty seed list only in the exported non-runnable template. It must test that preparation boundary before freezing its diagnostic. Finish both control preparations and the remaining checks in a separately frozen zero-combat scope; do not rerun this package or regenerate the already proven arms merely to obtain another success receipt. Reuse the sealed producing/runtime artifacts by reference to avoid another full copy and retain the shared limits.

## Measurements, limits and preservation

| Item | Measurement / status |
| --- | --- |
| Current native diagnostic | 9.437 seconds, exit 1 at control input validation |
| Both native diagnostics combined | 13.203 / 1,800 seconds |
| Current package before publication | 0.412 GiB |
| Both preparations before publication | 0.778 / 1 GiB |
| Carried setup charge before publication | 61.71 / 180 minutes |
| Fights / fresh values / retries in this scope | 0 / 0 / 0 |

The setup charge carries the previous 3,144.243291 seconds forward and adds this repair's elapsed work; the intervening user wait is excluded. Final bytes and charges are in `control/preparation-completion.json`, with the complete new package sealed by `preparation-files.json`. The failed predecessor seal remains unchanged. Source/executable/protocol pins and all failure logs are retained. This is not a newly measured performance speedup or balance finding.

The last verified reservation union remains **481,891**, including unused original512 and unused32. Neither preparation invoked allocation or created a reservation ledger. The 989-team focused result remains **Pass within that set**; 42,890 other retained teams remain outside it. Historical portfolio reliability remains **Fail 1/3**, adoption **Hold**; sealed v19 remains **Unresolved, 253 recipes retained, no internal confirmation**.

## Exact verification commands

These commands were executed once in this repair and are retained for inspection; do not repeat them against the closed package:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-deep-challenger-repair-20260915'
& "$work/development-tests.ps1" -Attempt 1
& $py -B "$work/bootstrap.py"
& $py -B "$work/workflow.py" build
& $py -B "$work/workflow.py" freeze
& $py -B "$work/workflow.py" check
```

The test wrapper invokes `build/run-tests.ps1` with `FullyQualifiedName~BalanceHarnessTowerDeepChallengerTests|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerCompleteFamilyTests`. Build and tests completed with local NuGet configuration access. The independent readiness audit, reservation and combat commands were **not run** following the diagnostic failure. No new allocation exception is requested while readiness is incomplete.

## Changed files and implications

Changed the three guard files and added five cases to `BalanceHarnessTowerDeepChallengerTests.cs`. Added this repair protocol/review, captured adapter/evidence scripts and seven active Markdown handoffs. The earlier deep policy/controller and two contract changes remain part of the dirty checkout; this repair does not change their allocations. No default CLI behavior, live gameplay, production configuration, migration or deployment changed. Unrelated dirty backend/UI work was preserved with a pre-edit inventory and recorded concurrent differences.
