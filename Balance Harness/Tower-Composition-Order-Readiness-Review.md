# Composition and order diagnostic readiness

15 September 2026. **PreparedAwaiting64ValueException. Zero fights, fresh seed derivations or reservations.** The offline BalanceHarness package is verified under the [frozen protocol](Tower-Composition-Order-Diagnostic-Protocol.md).

## What this prepares

The [saved-trajectory diagnosis](Tower-Deep-Search-Trajectory-Review.md) found missing exact loadouts but did not establish whether composition or ability order explains the search gap. This executable diagnostic compares the strongest saved control (`team-040e60d3dbc5c127321653c47ed3a9d3`, historical 63/256) with the best new challenger (`team-63357a2e7176e667b6bcf0a7cf380a1e`, historical 7/256). Historical outcomes select the two sources only and are not pooled into new estimates.

| Composition | Original order | Ascending Essence ID | Descending Essence ID |
| --- | ---: | ---: | ---: |
| Saved control | 64 future fights | 64 future fights | 64 future fights |
| New challenger | 64 future fights | 64 future fights | 64 future fights |

Exactly **six distinct recipes, 384 fights on 64 shared fresh values** after explicit authorization. The captured fixture has **ten character slots across two five-player parties**, each character with five Essence slots. Sort only the five Essence IDs within each owner; membership, placement, equipment, attributes, neutral identities and preparation context stay fixed. All cases use a common scenario ID, preventing factor-dependent encounter identities. These normalized scenarios are new controlled fixtures, not historical exact replays. Ascending/descending are reproducible ordering policies, not rankings of Essence strength.

Five fixed paired contrasts compare control minus challenger under each ordering and ascending minus descending within each composition. The original-order gap includes ordering differences; common-policy contrasts concern whole compositions. Nothing attributes an effect specifically to Bark Golem or another ingredient. Adjusted Wilson rate and paired discordance bounds split alpha .025/.025. With 64 trials, only large effects can be resolved. Intervals spanning zero remain unresolved; equal outcomes do not prove ordering invariance. This is sensitivity evidence, not optimizer acceptance, recovery, an interaction test or balance Pass.

## Measured checks

| Check | Observed result |
| --- | --- |
| Captured-gameplay harness compilation | Passed, 3.594 seconds |
| Focused test assembly compilation | Passed, 1.532 seconds |
| `build/run-tests.ps1` | **13/13 passed**, 2.062 seconds wrapper time |
| Frozen native six-roster preparation and registry check | Passed, 51.313 seconds |
| Independent matrix, roster, reservation and numerical audit | Passed, 23.609 seconds |
| Diagnostic charge so far | **74.922 / 1,800 seconds** |
| Preparation output before report publication | **55.47 MiB / 1 GiB** |

All **130 rate-interval fixtures and 25 paired fixtures** match the independent implementation within **1.01e-10**. The six exported rosters have matching canonical hashes and exactly the prescribed Essence order. All **482,222 reservations** remain excluded, including the original unused512 and unused32.

Two initial build restores failed because the sandbox could not read the user-profile NuGet.Config. Both failed receipts and logs remain in `control/`; an explicit repository-local RestoreConfigFile alone did not fix that. Reusing already restored SDK/package metadata and compiling with `--no-restore` succeeded. The bootstrap records those metadata copies for reproduction. No compilation source error or diagnostic retry was hidden. Builds and unit tests are measured separately from the 30-minute diagnostic allowance. The focused suite uses synthetic fixtures and enters no combat; it does not certify the entire dirty checkout.

The four captured gameplay assembly hashes and content match the sealed deep study. New code is confined to the offline diagnostic helper and its tests; an isolated captured adapter supplies check/bind/run/verify commands. The default CLI, search policy and gameplay content are unchanged. The existing compact campaign retains durable Start/Complete charging, prepared execution, owned storage accounting, cancellation and full archive verification. There are no migrations, deployed configuration changes or deployment steps.

## Reproduction and execution gate

Evidence: [preparation package](../TestResults/balance/tower-composition-order-preparation-20260915/control/completion.json), [protocol](Tower-Composition-Order-Diagnostic-Protocol.md), [six exact recipes](../TestResults/balance/tower-composition-order-preparation-20260915/check/plan.json), [native check](../TestResults/balance/tower-composition-order-preparation-20260915/check/result.json), [independent audit](../TestResults/balance/tower-composition-order-preparation-20260915/control/independent-check.json), [test results](../TestResults/balance/tower-composition-order-preparation-20260915/control/tests.trx), [file seal](../TestResults/balance/tower-composition-order-preparation-20260915/preparation-files.json). Never rerun the sealed directory. Preparation commands below document the once-only sequence; bootstrap and build outputs belong to an unused preparation workspace with the captured prerequisites.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-composition-order-preparation-20260915'
& $py -B "$w/bootstrap.py"
& $py -B "$w/workflow.py" build-cached
& $py -B "$w/workflow.py" test-build
& $py -B "$w/workflow.py" tests
& $py -B "$w/workflow.py" freeze
& $py -B "$w/workflow.py" check
& $py -B "$w/audit.py" preparation
& $py -B "$w/finish.py" preparation
```

Only after an explicit new **64-value exception** to the original zero-fresh-seed restriction, and successful preparation, the frozen execution sequence is:

```powershell
& $py -B "$w/workflow.py" bind --approved-64
& $py -B "$w/workflow.py" run
& $py -B "$w/workflow.py" verify
& $py -B "$w/audit.py" execution
& $py -B "$w/finish.py" execution
```

**None of these execution commands ran during preparation.** Binding reserves exactly 64 values using the existing durable allocator; expected union 482,286. The earlier 331-value exception covered the closed deep study only. This package does not supply a new exception. The combat cap is 384 attempts, zero retries/resumes/replays, 900 seconds native combat and 1,800 seconds total diagnostic work including the preparation charge above. New output is capped at 4 GiB (preparation/control below 1 GiB; study below 3 GiB). User decision wait is excluded. A failure or cap stop preserves evidence and ends the scope.

Historical portfolio reliability remains **Fail 1/3**, deep recovery **0/3**, adoption **Hold**. Sealed v19 remains **Unresolved**, retaining all 253 recipes and no internal confirmation. This work establishes no measured search or gameplay improvement.
