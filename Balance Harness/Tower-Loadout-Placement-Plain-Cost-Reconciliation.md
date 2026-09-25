# Whole-loadout placement: complete workload, costs, and scientific execution

Current status: **The twelve-root scientific comparison completed 16,000 actual fights. Its frozen decision is Inconclusive: whole-loadout placement did not demonstrate an improvement. Native and independent Python audits, publication, and final post-publication reconstruction all passed.**

The comparison tests moving complete five-Essence loadouts among fixed actors within each subgroup. It does not treat Essence list order as a search dimension. Actors, equipment, stats, captured gameplay/content, validation rules, search budgets, and promotion criteria remain unchanged.

The placement search selected the fixed benchmark at all twelve roots. The allied-action control selected the benchmark at eleven roots and a challenger at root 10. All 24 searches froze before held-out evaluation. Deduplicating identical physical teams within each root reduced the held-out workload to 3,328 fights; the search workload remained the full 12,672 fights. There were no retries, replacement roots, or changes to the decision rules.

| Scientific endpoint | Observed result |
| --- | ---: |
| Placement minus control, equal-root mean | -0.326 percentage points |
| Descriptive 95% root interval | -1.042 to +0.391 percentage points |
| Placement minus fixed benchmark | Exactly 0 (identical selected teams) |
| Roots with different selected teams | 1 of 12 |
| Selected novel / promising novel roots | 0 / 0 |
| Placement challengers passing validation | 0 of 12 |
| Control challengers passing validation | 1 of 12 |

At root 10, control won 209 of 256 held-out fights and placement/benchmark won 199. The other eleven method contrasts are exactly zero because both methods returned the same physical team. The frozen criteria require at least +2 percentage points against both control and benchmark, at least three differing roots, and at least three promising novel roots. This run does not qualify for a larger evaluation on those criteria. It also does not cross the -2-point abandonment threshold. The descriptive interval is not a powered efficacy claim, and no production default is promoted.

An exploratory diagnosis of the already-frozen validation observations helps identify the next search problem. Across 720 paired validation samples per arm, placement nominees gained 103 wins and lost 118 relative to the benchmark: a mean difference of -2.083 points. Four roots had positive raw differences, six negative, and two zero. Control nominees gained 116 and lost 133 (-2.361 points). These figures describe the nominated candidates before the fallback gate, not the final held-out endpoint. They do not prove why nomination failed, but they provide no evidence that the validation threshold is the only obstacle.

Both arms accepted 17 proposals per root. Across roots, placement explored 139 distinct proposed teams versus 26 for control, so the placement operator did expand the proposals explored. Only six placement challengers and five control challengers came from that root's generated proposals; the rest were existing reference teams. The six generated placement challengers gained 42 wins and lost 40 in 360 validation samples (+0.556 points); the six reference challengers gained 61 and lost 78 (-4.722 points). These post-hoc subgroups are diagnostic only and cannot qualify a method or reverse the frozen decision.

The next algorithm work should examine how the 16-sample nomination stage chooses a challenger, particularly when existing references displace generated candidates, then improve the proposal/selection path under a separately frozen comparison. Reuse these archives for diagnosis without retuning the completed pilot or reusing its seeds for confirmation. Keep Essence lists canonical and preserve the validation gate while assessing candidate quality. A broader blind run of unchanged placement is not supported by this result.

The published scientific archive is `TestResults/balance/tower-loadout-placement-pilot-01-20260925`, with closeout SHA-256 `082bd59ef6e5f862903295fb58b372d65754d7b729db318ac0cc9b6e11cea506`. Its native work took 1,115.406 seconds and audit/publication through closeout took 217.375 seconds, totaling 1,332.781 seconds and 1,865,250,303 retained bytes. These observations are below the original limits. One 16,384-word entropy draw assigned 4,380 fresh values; one historical collision left 16,383 values reserved, including unused values. All reservations remain consumed.

The separate final production reconstruction reproduced `result.json` exactly in 148.078 recorded seconds before sealing. It stayed inside the original audit and overall deadlines and added no allowance, entropy, or fights. Its package is `TestResults/loadout-placement-pilot-verification-20260925`, manifest `1b5c48f2ce99059d81502bac079cf4719df4d659b752f971e43dd9c56a076874`.

The new full fixture covers **21,888 reports**: 12,672 search reports and 9,216 held-out reports for three distinct physical teams at every root. All 24 searches freeze before held-out evaluation. It uses the qualified harness, captured content, real materialized input hashes, production native reconstruction, the independent Python auditor, publication checks, and post-publication native verification. Only combat outcomes and fixture allocation bytes are fabricated. These reports establish workload coverage and integrity, not stronger teams.

| Complete maximum fixture phase | Seconds | Retained bytes |
| --- | ---: | ---: |
| Native generation and retention | 279.063 | 1,728,876,294 |
| Audit and publication, through closeout | 186.281 | 3,031,577 |
| Additional post-publication verification | 144.656 | Included in supporting package |

The maximum fixture's sealed manifest is `52520d5bb7e77714aef52f16be9cf93a31d7c208b9524263372c2e58b400db1a`, under `TestResults/loadout-placement-plain-maximum-history-20260925`. Its total recorded duration before sealing is 621.453 seconds. Sealing tails are not presented as measured zero.

An earlier attempt stopped in production input validation before allocation or any report. The captured context's exclusion list omitted some values declared elsewhere in the context. The correction unions all declared generation, reference, root, and legacy values into the fixture history, rebinds the plan, and performs production inspection before launch. Physical inputs and outcome rules are unchanged. The failed package remains sealed at `TestResults/loadout-placement-plain-maximum-20260925`, manifest `78d4af9bfe4d4b151eb2d21f69f7eeef1733de7ad8250296f667e46ca1fcc7f3`. Each attempt retains its separately registered full 12,300-second / 6,576,668,672-byte charge. Neither was overwritten or treated as a favorable timing replacement.

The separate matched pair keeps the original physical fixture, literal evaluator, and values. Both packages were compared before either owner ran. Their shared v5 archives are byte-identical at all twelve roots. Both complete their native and independent audits, publication, and final verification. This pair intentionally preserves the historical synthetic input boundary; the separate maximum fixture supplies real-content and production-audit coverage.

| Matched fixture | Reports | Native seconds | Audit/publication seconds |
| --- | ---: | ---: | ---: |
| v4/v5 baseline | 17,280 | 148.766 | 59.750 |
| v5/v6 placement | 18,816 | 157.657 | 68.390 |

The pair is sealed at `TestResults/loadout-placement-qualified-pair-20260925`, manifest `ad286a0b8937f19d07e23418580d8d78234ca7e139ca6cb7ff8b0b6825c04841`. Its complete declared allowance is 24,420 seconds / 13,019,119,616 bytes, including failure coverage. The original failed 20260924 pair remains unchanged. Its native denominator ceilings remain 146.6040994 seconds and 1,078,739,935 bytes. Faster observations cannot reduce the original placement numerators or inherited floors.

The source-specific publication amendment still removes exactly one redundant protected-input inventory pass. The model retains the whole worker, both independent audit terms, storage-driven inventory scaling, factor two, the 120-second publication reserve, the original failed assessment, and every historical charge. The measured maximum workload supplies additional absolute floors, including final verification, and never supplies a matched denominator.

| Forecast partition | Rounded estimate | Unchanged limit |
| --- | ---: | ---: |
| Native seconds | 8,020 | 9,000 |
| Audit/publication seconds | 1,764 | 1,800 |
| Native bytes | 5,348,673,227 | 5,905,580,032 |
| Audit/publication bytes | 34,989,702 | 536,870,912 |

These are conservative planning estimates, not measured combat durations or guarantees. The owned launcher enforces the actual limits and retains incomplete work if a limit is reached.

Fresh admission checks all **799,177 historical values across 264 ledger files**. The larger live history increases context bytes by a factor of **1.082131316790383**. Only history exclusions and the resulting plan scope binding change. The declared metadata rule scales the entire measured maximum workload, including final verification, by this growth factor; it retains all qualified historical floors and reapplies storage-dependent inventory scaling before checking every partition. No timing is resampled and no prior floor is reduced. The resulting rounded estimates remain those shown above.

Admission is sealed at `TestResults/loadout-placement-admission-20260925`, manifest `27b674db52474753b5670f4fa3e1492fbeb86359dd11fd9acaf94f3e7f3657ac`. Production native inspection and a final independent live-history recheck passed without combat, entropy, or reservation. The admission retains its separate 900-second / 1-GiB allowance. The qualified harness remains `445e62c459e850408e13942ff762a398c1865d3b202f8855d3878089b7da4913`, with all 27 captured runtime files unchanged.

The scientific execution declaration is `Tower-Loadout-Placement-Pilot-01-Declaration.json`, SHA-256 `8874dd2a558f1d0878984dcf93fd218e1fc257b1b30e159b262100eace0e810b`. It authorizes one owned execution at `TestResults/balance/tower-loadout-placement-pilot-01-20260925`: twelve roots, 528 search fights per arm/root, 256 held-out samples per distinct team, at most 21,888 fights, one 16,384-word entropy batch with 4,380 assigned values, and no retry or replacement roots. It keeps the all-24-output barrier and the frozen validation, novelty, improvement, and abandonment rules. No production default is promoted automatically.

Final implementation verification passed **9 backend tests and 11 Python tests**. The backend suite covers the maximum fixture on both its self-contained test context and the captured context, full history binding, and preserved matched-fixture rules. The qualified-only host build has zero warnings and errors; the normal backend build reports existing warnings. Initial sandbox access to NuGet.Config was resolved through approved execution; no verification command remains blocked. The admission PowerShell helper also passed parser validation and actual native execution.

Verification commands completed:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore --artifacts-path .artifacts/loadout-plain-maximum-20260925
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-plain-maximum-20260925 -Filter 'FullyQualifiedName~BalanceHarnessPlainMaximumFixtureTests|FullyQualifiedName~BalanceHarnessMatchedPlacementFixtureTests'
python -B -X utf8 'Balance Harness/analysis/test-loadout-placement-qualified-costs.py'
python -B -X utf8 'Balance Harness/analysis/test-loadout-placement-qualified-admission.py'
python -B -X utf8 'Balance Harness/analysis/verify-loadout-placement-pilot.py' --closeout-pin 082bd59ef6e5f862903295fb58b372d65754d7b729db318ac0cc9b6e11cea506
```

Python commands used the bundled Python interpreter. The final verification command is a completed single-use execution, not an instruction to overwrite its sealed package. The final handoff is `TestResults/loadout-placement-pilot-01-execution-handoff-20260925.json`; it binds source hashes, the diagnostic rows, test logs, all earlier attempts, the scientific closeout, and cumulative accounting.

Changed implementation files are `LL/tests/BalanceHarness.ProcessFixture/PlainMaximumFixtureHost.cs`, its project file, `BalanceHarnessPlainMaximumFixtureTests.cs`, and `build/test-proposal-affinity-study-owned.py`; the new maximum and matched-pair runners; the qualified cost model and its tests; the admission runner, binding helper, and admission tests; and the final verification runner. The conditional test build references the already qualified runtime instead of rebuilding gameplay assemblies. Declarations and sealed receipts preserve every attempt. Existing status documents change only on line 3. There are no migrations, application configuration changes, deployments, or gameplay-default changes. Both compressed-evidence guards remain closed; complete process accounting coverage is not claimed.
