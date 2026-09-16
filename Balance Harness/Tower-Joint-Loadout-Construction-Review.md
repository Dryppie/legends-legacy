# Joint loadout construction: preparation failure preserved

15 September 2026. **PreparationFailurePreserved; implementation unverified.** The standalone joint constructor and 11 backend tests are written, but verification stopped during preparation on a Windows text-encoding error. No build, test or captured construction ran. Zero fights, fresh values, retries or replays. All **482,641 reservations** remain unchanged.

## Implemented source

[`TowerJointLoadoutConstructor.cs`](../LL/tools/BalanceHarness/TowerJointLoadoutConstructor.cs) adds a standalone bounded constructor that jointly fills missing authored categories around a supplied mechanic core. It branches on the most constrained missing category, considers multi-role providers, preserves family/slot/remaining-copy constraints, canonicalizes Essence IDs and retains per-category evidence witnesses. State or result limits explicitly mark incomplete search. It accepts no combat outcomes, reference recipes, random seeds or ability-order choices.

“Complete” means structural coverage of required authored categories within at most five slots. Spare-slot filler and placement across a party are outside the API. Category evidence does not establish targeting compatibility, useful uptime or combat strength. No existing search policy calls the new constructor.

[`BalanceHarnessJointLoadoutTests.cs`](../LL/tests/EssenceSystem.Tests/BalanceHarnessJointLoadoutTests.cs) contains 11 intended tests, including 448 role/copy/slot cases compared with exhaustive enumeration of a four-provider fixture. These tests are **written but unexecuted**. No constructibility, parity or stronger-build conclusion follows yet.

## Failed boundary and evidence

The once-only `workflow.py freeze` command failed after **3.641 seconds** while reading a UTF-8 Python file through `Path.read_text()` without an explicit encoding. Windows selected `cp1252`, which cannot decode one byte in the report script's UTF-8 punctuation. The failure occurred before `freeze.json` or captured `input.json` was created. It was an analysis-workflow encoding failure, not a reported constructor, compiler or test failure.

The [failure receipt](../TestResults/balance/tower-joint-loadout-construction-20260915/control/freeze-failure.json) retains the exact traceback. The failed workflow and source snapshots remain unchanged. The [proposed encoding correction](../TestResults/balance/tower-joint-loadout-construction-20260915/unexecuted-encoding-correction.patch) makes the two workflow text reads explicitly UTF-8; it is **unapplied and unexecuted** and is provided for a separately frozen follow-up, not a retry of this package.

Following the [protocol](Tower-Joint-Loadout-Construction-Protocol.md), all dependent phases stopped: isolated adapter build, test build, tests through `build/run-tests.ps1`, 48-core captured construction and independent audit are **NotRun**. The previous sealed backend/parity results still describe the previous source; they do not verify this new constructor.

## Preservation, costs and commands

All **17,769 indexed files across 44 predecessor packages** were checked unchanged during closure. The package retains source hashes, the failed scripts, proposed correction, dirty-checkout status and this report. No global reservation scan, battle preparation or allocation was launched.

Prior diagnostic time: **1749.694 seconds**; prior output: **2,119,946,598 bytes**. The [completion receipt](../TestResults/balance/tower-joint-loadout-construction-20260915/control/completion.json) carries the failed freeze plus measured closure and one conservative closure second within the unchanged cumulative 1,800 seconds / 4 GiB. The planned engineering builds consumed zero runtime because neither was launched.

Executed once:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-loadout-construction-20260915'
& $python -B "$work/workflow.py" freeze
# Failed; all dependent commands stopped.
& $python -B "$work/close_freeze_failure.py"
```

Do not rerun into this sealed directory. The next required step is a separately frozen corrected preparation using explicit UTF-8 reads, followed by the unexecuted build/test/construction checks within the actual remaining budget. No policy integration or combat comparison is ready.

Changed files: the standalone constructor, its 11 intended tests, this protocol/review, six active Markdown handoffs and the separate failed diagnostic package. Existing harness source and unrelated dirty work were preserved. No gameplay, configuration, migrations or deployment changes. The full backend suite was not run; the required focused verification remains unresolved.

V19 retains all 253 recipes and unused 512 confirmation values. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved** and adoption **Hold** remain unchanged. No fresh seeds, fight batch, Kharad tuning, ability-order optimization, 129,536-fight confirmation or cap increase.
