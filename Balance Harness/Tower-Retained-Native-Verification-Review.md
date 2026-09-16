# Corrected standalone native verification passed

The corrected native preparation test passed in full, and the independent artifact audit passed. Both historical teams are admitted through the existing standalone `tower-retained-composition-prepare` branch against matching current gameplay/content. The test's expected cost now matches the unchanged 256-value confirmation panel. **The native verification gap is closed; no further test correction or rerun is pending in this scope.**

This verifies preparation and compatibility. It does not establish current combat strength or authorize execution of the admission-only schedules. The block-search proposal remains closed, adoption Hold and V19 reliability Unresolved.

## Change and result

The isolated test source is byte-identical to the [correction prepared in the prior package](../TestResults/balance/tower-retained-native-admission-20260916/RetainedNativeAdmissionTests.corrected.cs.txt). Its only assertion change from the failed test is the expected cost tuple: `(1536, 768, 1280, 512, 0, 0, 4096)`. This corrects the fixture's assumption of 1,000 confirmation values to the retained panel's actual 256. Search rules, input schedules, recipes, production code and acceptance criteria did not change.

The isolated assembly uses the already extracted settings writer, with no conflicting helper entry point. Restore and build passed on the first attempt, with zero warnings/errors. **One native test passed, zero failed**, on its first invocation in this new scope. The test runs the real production `Program.Main` preparation branch under a combat guard, and completes every saved-output assertion and the native admission receipt. The previous build failure and failed test remain sealed and are not reclassified as passes.

The two admitted references remain `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`: ten level-40 characters, five Essences each, tier 1/rank 2, floor 5, fixed equipment, neutral identities and ordinal order. The pool remains 85 Essences in 82 families. No historical score becomes a current performance measurement, and unlimited-copy admission does not establish ownership in a player inventory.

The [independent audit](../TestResults/balance/tower-retained-native-verification-20260916/audit.json) confirms:

- The source definition and all four prepared artifacts are byte-for-byte identical to the earlier successful CLI output: definition, cost, generation inputs and improvement starts.
- The standalone executable, current content/settings, both reference recipes and their ancestry match the pinned inputs. Current source pins, including all 1,337 gameplay files, still match the reused compiled dependencies.
- Cost reconstructs to **4,096**: 1,536 discovery, 768 selection, 1,280 generated confirmation and 512 reference confirmation. This is an unexecuted fixture allocation.
- All 328 historical stage values, generation values and exclusions remain unchanged. All **483,046 reservations** remain intact.

The prior **13-case standalone parity result** remains valid and unchanged; it was not rerun because this scope changed only the isolated native test's expected cost. No stronger-team or whole-run performance claim follows from these checks.

## Verification commands and evidence

The [new package](../TestResults/balance/tower-retained-native-verification-20260916) contains the protocol, exact corrected source, project, pins, compiler logs, TRX, fresh native admission receipt, prepared outputs, independent audit and preservation manifest. It reuses the verified standalone harness DLL and matching gameplay dependencies. All required commands completed; none remain blocked.

Backend verification used the required repository entry point:

```powershell
./build/run-tests.ps1 -NoBuild `
  -ArtifactsPath TestResults/balance/tower-retained-native-verification-20260916/tests `
  -Filter 'FullyQualifiedName=EssenceSystem.Tests.RetainedNativeAdmissionTests.Standalone_cli_admits_both_current_anchors_without_combat'
```

The exact offline restore/build invocations are in `control/restore-command.json` and `control/build-command.json`. `workflow.py audit` independently checks the produced artifacts; sealing checks predecessor manifests, unrelated dirty files and scoped `git diff --check`. No broad backend suite, gameplay rebuild, candidate search, combat or fresh-seed allocation was required or executed.

## Files and resource accounting

Changes are confined to the new verification package, this review, the harness README and the five current strategy/plan/implementation/acceptance handoff documents. No active production or test-project source changed. Historical reviews and failed packages remain intact.

The user approved transferring **15 seconds /16 MiB** from unused run allowance to engineering. Component limits are now engineering **675 seconds /272 MiB**, run **2625 seconds /720 MiB**, and unchanged audit **300 seconds /32 MiB**. Overall limits remain **7,980 seconds /5,804,916,736 bytes**. This verification is capped at **19 seconds /24 MiB** within the resulting balances. The [completion receipt](../TestResults/balance/tower-retained-native-verification-20260916/completion.json) records measured charges, conservative storage accounting and remaining allowance; the transfer is not an overall budget increase.

No fights, new balance values, gameplay content changes, migrations, persistent configuration changes or deployments occurred. The closed comparison's 891 values and up to 6,912 fights remain unused. Both admission packages retain historical schedules and are unsuitable for launch as a fresh search. Any later practical search requires a distinct prospective plan and applicable resource/seed authorization; this verification schedules none.
