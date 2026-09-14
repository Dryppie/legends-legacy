# Captured-v19 correction screen — materialized, seed binding pending

Completed **14 September 2026** for the offline BalanceHarness. All **1,012 setting/recipe combinations** were materialized and verified using captured-v19 gameplay. **Zero fights, new seeds or combat retries** were used. All **481,219 reservations** remain excluded, including the 512 unused v19 values. The [frozen statistical design](Tower-Portfolio-Ceiling-Recalibration-Plan.md) and every historical experiment remain unchanged.

The preparation status is **MaterializedSeedBindingPending**. Four isolated content configurations and complete seed-free definition templates are saved. They are deliberately non-runnable: this task did not allocate the future 128-value schedule or create a combat study. The [execution handoff protocol](Tower-Portfolio-Ceiling-Execution-Protocol.md) binds the reviewed inputs and identifies the remaining execution integration/binding requirements. Adoption remains **Hold**, reliability **Fail 1/3**, and the existing ordinary/joint family assessment **Fail / Fail**.

## Implemented preparation and verification

`LL/tools/BalanceHarness/TowerCeilingScreenPreparation.cs` adds the fixed factor contract, whole-screen selection calculation, complete preparation and saved-output verification. `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCeilingPreparationTests.cs` adds 14 zero-combat contract cases. No existing harness, gameplay, test, catalog or content source was edited by this increment; unrelated dirty work was preserved.

The final preservation check detected a concurrent edit to `LL/docs/strongholds-game-design-review.md`. The file was left untouched; its before/after hashes are retained in `concurrent-checkout-changes.json`. This unrelated documentation change does not alter the captured inputs or test scope.

The isolated compiler retained all **120 captured source/resource files**, added the reviewed preparation source and a small preparation-only entry point, and referenced the retained gameplay DLLs. It exposes `prepare`, `check` and `math-fixture`, with no seed-allocation or combat command. Its build passed with **zero warnings/errors**. Application, Common, Domain and Services.LL DLLs remain byte-identical to the confirmed captured-v19 inputs. The ordinary checkout test build is separate and is not the source of gameplay evidence.

Preparation uses the first already-used confirmation seed, **1694558530**, solely to materialize input. No reservation was added. Each future definition template contains **253 exact recipes**, empty seed arrays, minimum sample count 128 and the complete unchanged 481,219-value exclusion set. The original scenario/actor identities, ordered Essences, party, equipment, roles and cohort remain preserved. A nonempty fresh schedule must be explicitly bound later; the ordinary evaluator rejects the current templates.

Only isolated floor-5 Health/Power scalars differ. The baseline is byte-identical across all 16 content files and settings. Candidate floor JSON passes complete semantic equality after restoring those two fields; all other content files retain their hashes. Materialized guardian values are shown below; floating-point conversion and integer starting health are retained as produced by the engine.

| Variant | Recipes | Starting health | Materialized maximum health | Materialized Power |
| --- | ---: | ---: | ---: | ---: |
| 1.00 | 253 | 19,258 | 19,258.996 | 872.25964 |
| 1.04 | 253 | 20,029 | 20,029.355 | 907.14984 |
| 1.08 | 253 | 20,799 | 20,799.715 | 942.04034 |
| 1.12 | 253 | 21,570 | 21,570.076 | 976.93066 |

All **253 baseline participant hashes** exactly match the original compact archive. Independent Python reconstruction also matches every baseline normalized input and exact recipe, after replacing its historical 512-seed array with the single already-reserved materialization probe. All **759 candidate snapshots** preserve every friendly participant and every guardian field outside combat Power, combat MaxHealth and starting health. Allowed scalar products are checked independently; proportional materialized floating-point values use relative tolerance 1e-5, with an absolute floor of .001. This tolerance does not permit changes to any unrelated field or to the exact decimal content inputs.

## Safeguards and tests

**31 tests passed: 14 new preparation/selection/accounting cases and 17 existing storage cases**, through `build/run-tests.ps1`. The final focused build retains six existing warnings; the new nullable warning was removed. The tests execute no combat engine calls. Synthetic journal events are fixture writes, not simulated fights or seed reservations.

Coverage includes exact scalar isolation, participant/Essence-order/identity mutation rejection, complete 1,012-cell selection, baseline exclusion from correction selection, pooled-rate and incomplete-family rejection, and cancellation before input/output access. Four synthetic factor writers share one durable `TowerRescreenAttempts` journal and `TowerStorageAccountant`; earlier charges and sealed bytes remain counted across factor boundaries. Other cases cover interrupted starts, missing completions, pending output, no implicit reopen/resume, oversize metadata and writer leases, same-length tampering, extra files, linked directories and mandatory final audits.

The accounting/trace/attempt implementations exercised by those tests are byte-identical to the captured source used by the preparation build. This establishes their fixture behavior; **no four-factor combat campaign or end-to-end combat interruption was run**. Execution integration must preserve the same global owner/ledger and be checked before any future run. Preparation's own saved archive passed its exact file-set/hash verifier.

The ordinary Wilson helper has an unchanged **1,000-cell limit**. Whole-screen intervals instead reuse the existing staged arithmetic with alpha .025 over 506 cells, which gives exactly the required per-cell allocation **.025/506 = .05/1,012**, including both tails. Reporting and selection retain the actual **1,012-cell** screen family. No evaluator or experiment cap was increased. Independent `statistics.NormalDist` reconstruction checked all **129 possible win counts (0–128)**; the maximum difference was **3.111 × 10⁻¹¹**, below the frozen 1e-8 tolerance.

## Measured work and retained evidence

| Operation | Seconds | Result |
| --- | ---: | --- |
| Native materialization, including source verification | 14.6544 | 1,012 cells; zero fights/seeds |
| Complete preparation command | 15.1971 | Exit 0 |
| Native saved preparation check | 0.2874 | Exit 0 |
| Native interval fixture | 0.0646 | Exit 0 |
| Enclosing native-command driver | 15.7158 | Three commands complete |
| Independent baseline/variant/interval audit | 7.7004 | Pass |

The [diagnostic protocol](../TestResults/balance/tower-ceiling-screen-preparation-20260914/diagnostic-protocol.json) froze exact executable, source, request, content hashes and commands before materialization. Limits were zero combat/new seeds/retries, 1,800 seconds and 4 GiB of new diagnostic output; the native preparation has its own 900-second/1-GiB cap. Before final documentation/sealing, the full work package occupied about **745 MiB**, including isolated builds, four content copies and all prepared snapshots. Final file counts, bytes, hashes, historical preservation and document snapshots are in [final verification](../TestResults/balance/tower-ceiling-screen-preparation-20260914/final-verification.json). These are preparation timings, not a combat throughput improvement.

The detailed [native receipt](../TestResults/balance/tower-ceiling-screen-preparation-20260914/materialized/preparation.json) persists the existing performance trace and every prepared input/participant hash. The [independent verification](../TestResults/balance/tower-ceiling-screen-preparation-20260914/independent-verification.json) records each of the 253 source matches, all variant checks and the interval error bound.

## Preserved failures and commands

The initial sandboxed test build could not read the user NuGet configuration; the required wrapper subsequently ran with approved access. The first test execution passed 29 and failed two selection cases because the ordinary interval helper rejects 1,012 cells. The corrected whole-screen implementation passed all 31. Final tests were repeated after removing the new nullable warning; no native materialization was repeated.

The Python content-copy audit initially compared locale-decoded original JSON with UTF-8 candidate text; a floor-10 description's en dash caused the equality failure. The copied floor-5 values were correct. Both failed scripts and the encoding diagnosis are preserved; explicit UTF-8 comparison passed. The first independent audit expected a full input object where the historical archive stores a compact input plus separate recipe. Its log/script are retained; the corrected audit checked both pieces and passed. These were engineering/verifier corrections, not combat retries. No failed evidence was deleted, no study cap was extended, and no required command remains blocked.

Completed provenance commands; do not rerun them into this sealed work directory:

```powershell
$w = 'TestResults/balance/tower-ceiling-screen-preparation-20260914'
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTowerCeilingPreparationTests|FullyQualifiedName~BalanceHarnessTowerStorageTests' -ArtifactsPath "$w/build/tests"
dotnet build "$w/compiler/CapturedPreparation.csproj" --configuration Release --output "$w/executable"
& $py "$w/run-diagnostics.py" freeze
& $py "$w/run-diagnostics.py" run
& $py "$w/verify-independent-compact.py"
git -c core.safecrlf=false diff --check
```

The scripts retain corrected setup commands and exact native arguments. Reproduction requires a new isolated sibling work package, the same source bindings and an explicitly frozen zero-combat scope; existing output is refused. The executable protocol is still **seed-binding pending**, not permission to launch the 129,536-fight screen. The **43,879-entry broader inventory** was not materialized in this task and still exceeds the existing staged contract's capacity. No current-gameplay or full-family acceptance follows. There are no migrations, live configuration changes, catalog promotion or deployments.
