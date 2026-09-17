# Practical Tower selection diagnostic: resource qualification

**Historical snapshot:** this document records its original scope. Current disposition is in the [execution review](Tower-Practical-Selection-Diagnostic-Execution-Review.md); the earlier launch restrictions below have been superseded for that one completed run.

17 September 2026. Target: the offline BalanceHarness. **A concrete conditional envelope is now specified: 1,380 seconds /512 MiB, with separate admission, search, confirmation and audit ceilings. Gameplay launch remains NoGo.** Retained evidence supports sizing the workload; it does not establish completion probability or qualify the current native runtime at this workload. These proposed caps are not an approved allowance or a runnable request.

This follows the [implementation review](Tower-Practical-Selection-Diagnostic-Implementation-Review.md). The [read-only reader](analysis/practical-selection-diagnostic-resources.py) produces the [resource appendix](Tower-Practical-Selection-Diagnostic-Resources.json) from authenticated files. It performs no benchmark, build, backend test, native reconstruction, candidate generation, seed derivation, entropy acquisition or combat. No earlier experiment allowance is reopened.

**Subsequent execution:** The [completed diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) completed all **4,640 fights** and both built-in audits in **248.562 seconds /152.36 MiB**, returning **NoSelectionMissDemonstrated**. The frozen primary won **712/1,000**, versus **632/1,000** and **628/1,000** for the anchors and **598/1,000** for the other challenger. This is encouraging fixed-team evidence, while adoption remains **Hold** and both anchors remain recommended. The scope is closed; **2,089** new reservations bring the permanent union to **486,374**, with no new Pending reservation. No further experiment is queued. The analysis, fixture/probe measurements and JSON below retain their original historical scopes.

## What is compatible, and what remains unmeasured

Both sealed native inventories still authenticate exactly **53 and 1,564 files**. The native run's 16 content hashes match the current gameplay data. Of its 166 pinned source files, 159 remain identical; seven harness integration/validation/allocation files differ. The diagnostic's unchanged-kernel parity and process checks remain covered by the previous 404-case verification.

File-byte compatibility does not revalidate current native settings or prepared inputs. The 484,285 exclusions are the authenticated retained snapshot; complete live-registry reconciliation remains required. This analysis does not replace native admission.

All five current producing assembly hashes differ from the retained native run's hashes. This does **not** establish that gameplay changed: rebuilds and artifact locations can change binary identity. It does mean the previous run is not exact producing-runtime evidence for the current build. The appendix records both identities and the current 26-file executable-copy inventory. No retained archive is rebound to the new executable.

The native run recorded 1,408 fights and enclosing phase times, but no per-fight wall times. The saved `durationTicks` and `durationSeconds` are simulation durations. They cannot be used as measurements of CPU or elapsed execution time. The literal diagnostic fixture establishes workflow behavior with 4,640 small fabricated reports and a one-value history; its 21.524-second completion record and 10,058,719 retained bytes do not measure native combat, full-history admission or native report processing.

The phase boundaries also changed. The new diagnostic copies content and the producing executable, prepares inventory/mechanics, and writes repeated history-bearing bindings during **admission**. The old native package's admission/run/audit phases included a different distribution of preparation, internal verification and separate external audits. Copying their ceilings or multiplying their total duration is insufficient.

## Native report and audit workload

The reader decompresses the existing reports without replaying them or deriving a new result:

| Retained stage | Reports | Compressed bytes | Largest compressed report | Expanded JSON bytes |
| --- | ---: | ---: | ---: | ---: |
| Discovery | 512 | 10,357,006 | 21,963 | 81,926,473 |
| Selection | 128 | 2,550,866 | 20,751 | 20,633,979 |
| Confirmation | 768 | 15,298,955 | 20,863 | 123,777,427 |

The implemented run's native state-machine audit reads 4,640 reports. Its separate direct-outcome audit reads the 4,000 confirmation reports again. That is **8,640 report deserializations plus two complete study-inventory passes**, before parent publication. Using the retained stage sizes gives a projected **1,391,908,649 expanded JSON bytes** across those reads. This is processed data volume, not simultaneous memory or retained disk usage. A later standalone public verification repeats work and requires separate accounting; it is not included in this count.

The largest observed battle lasted 1,184 ticks; the current engine input permits 6,000. The native total was 1,218,917 ticks. A hypothetical 4,640-fight run reaching every tick cap would contain 27,840,000 ticks, **22.84 times** that native total, or **6.93 times** its mean ticks per fight. Tick cost and event complexity vary, so this is neither a wall-time bound nor a forecast. It demonstrates why a twofold time margin does not guarantee completion.

## Admission and storage model

The model uses the complete **484,285-value** retained exclusion union, the same identifiers/cohort/gear, an indented source template and the current executable asset selection. Its serializer sizing exactly reproduces the retained Windows .NET definition file, including CRLF. It constructs only in-memory size projections; no definition, binding, panel or new values are emitted. Reserved-word sizes use maximum signed-int text width, without generating values.

| Admission component | Modeled bytes |
| --- | ---: |
| Indented source template | 7,005,735 |
| Root and study search bindings | 15,961,440 |
| Study definition | 7,006,698 |
| Historical ledger and 41 search-word positions | 6,919,080 |
| Content copy | 584,903 |
| Current producing executable copy | 19,111,561 |
| Boss profiles | 3,870,509 |
| Generation inputs/mechanics | 275,710 |
| Other admission metadata allowance | 2,097,152 |
| **Total** | **62,832,788 (59.92 MiB)** |

The historical 32-MiB admission allowance would not fit this model. The diagnostic fixture's 8-MiB admission limit is also specific to its fabricated data. The proposed 96-MiB phase cap leaves room for the largest modeled atomic binding write, 7,980,720 bytes, and additional metadata. The 2-MiB metadata allowance and representative identifiers are explicit assumptions, not measured upper bounds.

For later stages, charge every future report at a multiple of the largest native compressed report, 21,963 bytes. Add 4 MiB for search metadata, 8 MiB plus the reservation-growth allowance for confirmation, 4 MiB for audit/publication, one largest atomic temporary file, and the 4-MiB closeout reserve:

| Report-size scenario | Modeled output plus temporary/closeout | Confirmation growth | Fits proposed 256-MiB confirmation cap? |
| --- | ---: | ---: | --- |
| Every report at observed maximum | 184.76 MiB | 91.82 MiB | Yes, conditionally |
| Every report at twice observed maximum | 281.94 MiB | 175.60 MiB | Yes, conditionally |
| Every report at four times observed maximum | 476.32 MiB | 343.16 MiB | **No** |

Even the fourfold scenario fits the proposed 512-MiB total while exceeding a phase cap. Phase limits cannot be transferred. These are sensitivity scenarios, not observations of future reports or a disk high-water guarantee. Full-size native metadata, serialization and sampled storage behavior still need measurement on the current path.

## Conditional prospective envelope

| Phase | Explicit time-sizing proxy | Proposed seconds | Proposed output-growth cap |
| --- | ---: | ---: | ---: |
| Admission | Old admission plus the entire old run: 119.562 s | 180 | 96 MiB |
| Search | Twice old run time ×640/1,408: 90.525 s | 120 | 64 MiB |
| Confirmation | Twice old run time ×4,000/1,408 plus old admission: 585.768 s | 660 | 256 MiB |
| Audit/publication | Twice old separate audit time ×4,640/1,408: 309.358 s | 360 | 16 MiB |
| **Phase sum** | | **1,320** | **432 MiB** |
| **Proposed cumulative maximum** | | **1,380** | **512 MiB** |

These time proxies deliberately charge old overhead along with fight-equivalent work; they are not fitted estimates. The factor two is a declared stress assumption, not a confidence bound. The rounded limits sit above those proxies and the twice-maximum report-size scenario. The old audit's Python/C# composition differs from the new two-auditor path, so its time proxy is particularly uncertain.

After the mandatory two-second /4-MiB closeout reserve, the proposal leaves **58 seconds and 76 MiB** beyond its phase sums. Prior costs and launcher setup must fit the remaining allowance; that space is not transferable phase capacity. A concrete request must freeze its actual prior costs and pass the existing sum constraints. If they no longer fit, stop admission and revise the proposal explicitly; do not silently reset costs or increase the limit.

This envelope covers the diagnostic run's own native reconstruction, direct-outcome audit and publication. It does not authorize a calibration, retry, external audit command, larger sample, replacement panel or second search. Any operational failure is terminal, preserves exclusions and yields incomplete evidence. The roughly 81.09% conditional detection bound excludes operational failure and therefore is not an unconditional success guarantee under these caps.

## Original probe proposal and its limits

The proposed observation was a **bounded zero-combat resource probe on the current producing runtime**, using authenticated retained inputs and full-sized reports. A proposed probe ceiling is **180 seconds /256 MiB**, one attempt with no retry, including its preparation and closeout. The onefold saved-report storage scenario fits that proposed ceiling; these were prospective sizing assumptions. The first allowance was granted for one attempt and failed. The later, separately authorized corrected probe completed as linked above; both allowances are now closed.

The probe should measure complete live-history admission without allocation; native content/executable copying and full-history metadata writes; 4,640 saved-report occurrences through native input preparation/serialization; the two audit read workloads; and per-phase elapsed time, sampled storage high-water and final elapsed time including sealing. It must label reused reports as resource-test occurrences, retain a compact source-pinned receipt, and refuse combat, entropy, new candidate generation or publication as a valid diagnostic. Place probe output outside the allocation registry and use no authoritative reservation-ledger filenames.

A passing probe would qualify those non-combat costs within its observed environment. It still could not establish future combat cost, completion probability or the existence of a stronger missed nominee. Gameplay admission would then require compatible combat-time evidence **or explicit acceptance of the candidate envelope's capped operational-failure risk**, together with a frozen native request, runtime, content, history and prior costs. This review neither invents that acceptance nor queues a gameplay calibration. If the narrow selection question is not worth that capped cost, stop the proposal.

## Verification and changed files

The reader verifies both exact sealed inventories and all 1,617 hashes, the fixture receipt against the passing final TRX, all 16 current content pins, the full exclusion union, retained stage/report counts and identities, the source tick cap, exact representative serializer sizing, current dependency-copy selection, and the proposed time/byte sum constraints. A second invocation reproduces the JSON exactly. Source pins are rechecked before output. Markdown links, whitespace and preservation of unrelated work are checked separately.

```powershell
python -B "Balance Harness/analysis/practical-selection-diagnostic-resources.py"
```

Added this review, its read-only reader and JSON appendix. Updated the current pointers in the implementation review, assessment, readiness review, README and practical guide. Historical design/contract calculations and sealed receipts retain their original results. The first reader check caught Windows CRLF sizing rather than matching the retained definition; sizing was corrected and now requires exact agreement before producing any projection. No required analysis verification remains blocked. Backend builds/tests and benchmarks were not run in this source/evidence-only increment.

Preservation checks found only the five intended Markdown updates among 222 baseline files; the other **217 files remained byte-identical**. All local links and whitespace checks passed.

There are **no migrations, application configuration changes, gameplay or harness implementation changes, or deployments**. The 484,285 exclusions, both anchor recommendations, V19's 512 unused values /253 required recipes /Unresolved, adoption Hold and all closed experiment scopes remain unchanged.
