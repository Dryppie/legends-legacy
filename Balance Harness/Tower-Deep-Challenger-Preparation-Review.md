# Deep challenger preparation: failed readiness check

15 September 2026. Target: offline BalanceHarness. **StoppedNoRetry; not ready for seed binding or combat. Zero fights, fresh seed candidates or reservations.** The existing 989-team focused Pass, historical portfolio reliability **Fail 1/3**, adoption **Hold**, and sealed v19 **Unresolved / 253 retained / no internal confirmation** remain unchanged.

## Implemented scope and measured verification

The [proposed study](Tower-Deep-Challenger-Study-Protocol.md) exposes the existing 1,536-candidate deep component on three independent starts against the fixed captured-v19 +10% boss. It adds a controller with discovery, three 32-team screens, complete required-family selection, confirmation, durable attempts, owned storage, cancellation, performance recording and reconstruction. Both existing viable controls are included in the design. No search feedback is allowed from those controls.

| Command/check | Measured result |
| --- | --- |
| `build/run-tests.ps1` via the saved development wrapper | **86 passed, 0 failed**, 40.98 seconds; synthetic fixtures, zero engine calls |
| Isolated captured-gameplay adapter build | **Passed**, 2.45 seconds |
| Once-only native saved-trajectory check | **Failed**, exit 1 after **3.77 seconds** |
| Independent saved-output audit | **Not run**; native prerequisite failed |
| Fresh reservation, combat, post-run verification | **Not run** |

The 86 passing cases cover selection, limits, overflow, inference, existing portfolio and complete-family contracts. They did not establish execution of the new generation policy. The new test fixture relabels existing portfolio-generated arms. This left a generation-registration defect undetected until the saved-trajectory diagnostic.

The exact diagnostic and producing inputs were frozen before execution under the [preparation protocol](Tower-Deep-Challenger-Preparation-Protocol.md). Request SHA256: `52a6cb8f27003735d3c57fe7cf78c8f82df3c4b6038fd377d77320d7d782c20c`. The diagnostic error was:

```text
Deep replay incomplete: InvalidDataException: Coordinated loadouts require legal generated parents and ordered modules.
```

## Diagnosed defect and limits of this result

The new policy was registered in the generation and discovery contracts but omitted from `TowerBossPartyGenerator.CoordinateLoadouts` in `TowerLoadoutComposition.cs:38`. Its policy guard therefore rejects the deep search's coordinated-loadout operation regardless of whether its parent/modules are otherwise valid.

Static inspection also found omitted policy registration in `TowerBossPartyGenerator.cs` for required mechanic cores and content-derived core/coverage creation (lines 60, 107, 108), and in `TowerPartyCoverage.cs:77` for required coverage validation. Fixing only the first exception would not establish the required unchanged generation behavior. These omissions remain unfixed in this closed preparation.

The native check did not publish a successful replay or reach control preparation, the reservation-registry union check or the confidence-output checks. **No claim of 4,608-evaluation trajectory parity, control preparation parity or successful executable readiness is made.** The full predecessor manifest check precedes replay in the executed code; no predecessor output was written. The last verified reservation union remains 481,891, including unused original512 and unused32. No allocator ran in this preparation.

The diagnostic consumed only **3.77 / 1,800 seconds**, zero fights and zero fresh values. New output before this report was **0.366 GiB**, below the preparation 1-GiB ceiling and total 4-GiB envelope. Charged setup before publication was **52.40 minutes** including the conservative initial 15-minute charge. This is development/preparation cost, not combat throughput. There is no newly measured performance improvement or balance finding.

## Preserved evidence and exact commands

Evidence lives in `TestResults/balance/tower-deep-challenger-preparation-20260915`: captured source/gameplay executable, frozen protocols/scripts, request pins, 86 test results, build log, failed check log and failure receipt, static diagnosis and this review. The closing `preparation-files.json` seals the entire package. The original failure and producing files are preserved unchanged.

Executed commands, retained for inspection and reproduction in a separately scoped package only:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-deep-challenger-preparation-20260915'
& "$work/development-tests.ps1" -Attempt 1
& $py -B "$work/workflow.py" build
& $py -B "$work/workflow.py" freeze
& $py -B "$work/workflow.py" check
```

The test wrapper calls `build/run-tests.ps1` with `FullyQualifiedName~BalanceHarnessTowerDeepChallengerTests|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerCompleteFamilyTests`. The exact process arguments and durations are saved in `control`. Do not repeat these commands against this sealed package. Build/test invocations required local NuGet-configuration read access; they completed successfully.

## Next bounded repair

Correct all new-policy gates while preserving historical policy behavior, then add a synthetic test that actually runs new-policy generation through coordinated loadouts and tests content-derived mechanics/coverage. A separately frozen zero-combat verification package must prove archived deep trajectories and both control preparations before any new allocation request. Do not modify or retry this package. Its scripts reject downstream binding/run when the failed check receipt exists.

The tentative combat design remains at most 108,544 fights, three hours and 4 GiB with zero retries. It would require a specific exception for 331 new values (3 roots + 8 discovery + 64 screening + 256 confirmation) under the earlier user restriction. **That exception is not requested now because executable readiness failed.** No fresh values are derived, old reservations reassigned, or combat launched to work around this result.

## Changed files and implications

Added `TowerDeepChallenger.cs`, `TowerDeepChallengerRun.cs` and `BalanceHarnessTowerDeepChallengerTests.cs`; changed `TowerBossGeneration.cs` and `TowerBossDiscoveryContract.cs` to recognize the explicit policy. Added study/preparation protocols, this failed-readiness review, producing/verification artifacts and seven active Markdown handoffs. These implementation files remain **incomplete and unverified for real generation**; no CLI default was changed. The additional guard files were inspected only.

Unrelated dirty backend/UI work was preserved. The broad checkout inventory followed the first owned generation-policy edit, and its clean Git base is separately retained; no false original working-file hash is claimed. Concurrent unrelated differences are recorded, not reverted. No migration, production configuration change, gameplay-content edit or deployment. The independent audit and all execution commands could not proceed because the frozen native check failed.
