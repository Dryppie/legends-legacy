# Whole-loadout placement comparison implementation

The separately versioned `tower-loadout-placement-comparison-v1` adapter is implemented. Its native [plan](Tower-Loadout-Placement-Comparison-Plan.json) exactly matches `plannedNativePlan` in the immutable [design](Tower-Loadout-Placement-Comparison-Design.json). Runtime qualification and scientific admission remain pending.

## Implemented behavior

The control retains the exact v5 allied-action affinity proposer and racing v7 with the captured affinity inventory. The candidate uses the existing v6 whole-loadout proposer and racing v8 with a null inventory. Both arms use the same benchmark-validation selector, benchmark, physical scope and ordered racing, nomination and validation panels. The mixed inventory rule is enforced specifically for this comparison; older pair validators remain strict.

The binding retains twelve roots, 109 search values per pair, 256 isolated held-out values per root, 4,380 selected values and a 21,888-fight ceiling. Both searches have 528 charged requests. All 24 outputs freeze before held-out measurement. All exposed unused values remain reserved by the existing owner, and incomplete work cannot publish a result. The new version requires resource envelope v2.

Success means only that a larger fresh evaluation is warranted: both equal-root mean gains must reach 0.02, at least three roots must differ, and at least three candidate roots must be novel relative to all references and gain at least 0.03 against the benchmark. Either mean at or below -0.02 abandons this configuration, regardless of novelty. Neither implementation nor a passing development pilot permits adoption.

Each study retains a complete `placement-catalogue-XX.json` per root. Native reconstruction regenerates this evidence. The Python auditor independently enumerates the 240 assignments, excludes identities and references, deduplicates recipes, checks all assignment and scenario bindings, and verifies the two disjoint nine/eight proposal waves and their provenance. The existing v8 racing archive schema is unchanged. Native reconstruction remains responsible for replaying the exact root-derived shuffle; the independent auditor checks catalogue membership, uniqueness and ordered draw provenance without importing the engine. Its native hash bridge explicitly supports the frozen ASCII scope/scenarios; affinity inventories retain their native binding.

## Changed files

| Area | Files and purpose |
| --- | --- |
| Native comparison | New `LL/tools/BalanceHarness/TowerLoadoutPlacementComparison.cs`; explicit dispatch in `TowerProposalComparison.cs`, `TowerBenchmarkValidationComparison.cs` and `Program.cs`. |
| Study and audit | `TowerProposalStudy.cs`, `TowerProposalStudyProtocol.cs` and `TowerProposalStudyArchive.cs`: version registration, complete catalogues, both-arm diagnostics, decision rule and v2 envelope. |
| Independent tools | `Balance Harness/analysis/audit-proposal-affinity-study.py` and `build/run-proposal-affinity-study.py`: exact new policy, catalogue, decision and resource contracts. |
| Verification | New `BalanceHarnessLoadoutPlacementComparisonTests.cs` and `build/test-loadout-placement-comparison.py`; shared literal fixtures in `BalanceHarnessProposalStudyTests.cs`, `ProposalStudyFixtureHost.cs`, `build/test-proposal-affinity-study.py` and `build/test-proposal-affinity-study-owned.py`. |
| Publication | Native plan, this report, two LF rules in `.gitattributes`, and line-three status updates in nine existing documents. Their historical bodies remain unchanged. |

## Verification and retained evidence

All **380 focused backend tests** passed: 30 new comparison tests and the established 350-test proposer/racing/comparison regression set, executed through `build/run-tests.ps1`. All **75 Python tests** passed: 10 new contract/archive tests, 42 legacy arithmetic/archive tests, five resource-envelope tests and 18 frozen-design tests. Both Windows-owned synthetic fixtures passed. Exact commands and logs are retained in the implementation package.

Verification receipts are in [the implementation package](../TestResults/loadout-placement-comparison-implementation-20260924/files.json), with before/after source snapshots. The [bounded native plan check](../TestResults/loadout-placement-comparison-native-plan-20260924/files.json) verifies exact design parity and reconstructs the saved generation export, including its old proposer arms. It completed in 7.938 seconds with zero combat, preparation or new values; all three owned process trees exited cleanly.

The new backend fixture checks the full twelve-root study under a combat-entry guard. Literal observations produce 18,816 reports, six differing outputs, six novel outputs and six pass/six fallback decisions in each arm. Both held-out mean differences are zero, giving `Inconclusive`. These outcomes are deliberately synthetic engineering evidence, not efficacy or runtime qualification measurements.

The independent adversarial tests reject missing/resealed catalogues, changed physical hashes, altered assignments, changed draw positions, extra candidate inventory, missing control inventory and altered proposal metadata. Separate Windows-owned success and failure fixtures exercise the real launch, reservation, audit, publication and verification boundaries while replacing entropy/content/combat with literal test inputs. The failure fixture retains its unmatched attempt and all 16,384 exposed synthetic values and publishes no result.

Final test counts, process receipts, source pins and publication checks are recorded in the implementation verification JSON and handoff. An exploratory broad harness run was interrupted after failures outside the comparison suites, including missing captured reference files, expected-count mismatches and timeout/file-lifetime failures. Those failures were not diagnosed or changed in this implementation, and this is not a passing full-suite result. Its partial log is retained. The initial sandbox build could not read the user's NuGet configuration; the approved retry built successfully with 44 warnings and no errors. The first independent archive check exposed a `scheduledOwners` field-name mismatch in the auditor; that was corrected before the final fixture runs.

The bounded native plan verification charges its full 180 seconds / 67,108,864 bytes once, without retry. Cumulative recorded charges become 54,019.661 seconds / 38,626,246,974 bytes; cumulative declared maxima become 142,920 seconds / 93,767,860,224 bytes. Ordinary synthetic tests are separate engineering work. No current history scan, prospective scientific allocation, campaign execution, runtime qualification or admission is claimed.

## Next gate

Perform the separately declared current-runtime qualification described in the frozen design, including producing assembly/PDB/source correspondence, guarded preparation coverage for every placement and saved controls, complete reconstruction, and retained metadata/audit costs. Preserve the 900-second / 1-GiB qualification limit, the v2 scientific partitions, inherited upward-only resource floors, factor-of-two margins and publication reserve. The inherited audit floor leaves only 36 seconds below the 1,800-second cap; admission must fail if the new forecast does not fit. Do not raise limits after observing costs or preview prospective roots.

Only a successful, separately pinned resource admission permits a fresh scientific run. This implementation changes no gameplay defaults, application configuration or database schema. No migration or deployment is required or performed.
