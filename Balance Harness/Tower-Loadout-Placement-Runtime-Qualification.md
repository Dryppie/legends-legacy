# Whole-loadout placement: captured-runtime qualification

Current status: **The corrected plain runtime is qualified. Resource reconciliation and scientific admission remain pending.** All 4,196 preparations matched between the captured and current harnesses. No combat, production entropy, scientific reservation, or efficacy comparison occurred.

The successful package is [loadout-placement-runtime-qualification-compatible-helper-20260925](../TestResults/loadout-placement-runtime-qualification-compatible-helper-20260925), with manifest SHA-256 `42da1105a3a207a760f0a53bc244faa2fa6eeca539829c5a995a58ff17fd4ac7`. Its independently pinned declaration was written before execution. The attempt finished in **60.907 seconds**, retaining **668,564,858 bytes**, within its unchanged 900-second / 1-GiB allowance. These preparation and replay measurements are not a combat or full-study cost forecast.

The first attempt exposed a real binary compatibility failure. Earlier content accounting work had added `Services.LL.Content.ContentJsonReader` and changed provider constructor signatures. The current harness compiled against those signatures, while the frozen gameplay assembly lacked the type. Tests using freshly built gameplay dependencies did not expose the mismatch. The captured baseline prepared all 2,098 cases, but the candidate stopped before preparation.

`TowerContentProviders` now selects the loaded gameplay assembly's supported constructor signatures. It keeps a cache of constructor/loader metadata, passes the accounting reader to current providers, and invokes the original constructors for captured providers. The only method referencing the new reader is isolated from legacy JIT compilation with `NoInlining`. Provider exceptions are unwrapped with their original exception identity and stack. No gameplay binary, gameplay value, search policy, or selection threshold was changed for qualification.

Captured providers cannot produce the new content-read counters. Attempting to activate those counters now fails explicitly before provider file IO. The successful qualification checks that rejection. This supports the plain execution path; it does not establish complete accounting coverage or open either compressed launch guard. Reflection dispatch adds work whose cost still belongs in the forthcoming current-runtime reconciliation.

Coverage used two already historical values for each recipe instance:

| Coverage | Recipe instances |
| --- | ---: |
| Existing first-wave, preservation, and all twelve allied-action previews | 568 |
| Every distinct legal placement recipe | 238 |
| All three fixed references | 3 |
| Placement preview teams at all twelve saved roots | 240 |
| Total | 1,049 |

Each runtime performed 2,098 materializations and preparations. The resulting input and prepared-participant hashes match row for row. The native catalogue independently reconstructed all 238 placement scenarios before preparation. All twelve complete mixed v4/v5/v6 preview exports were reproduced, along with the legacy four-arm, preserving two-arm, and twelve allied-action exports. The comparison plan was rebound only to the qualified harness execution identity; every other physical-context field and every non-scope plan field remained identical.

The producing portable PDB matches the harness's CodeView identity, and all **248 compiled source documents** match their checksums. Production retention preserved all **27 runtime files**, including the producing symbols. The qualified harness SHA-256 is `445e62c459e850408e13942ff762a398c1865d3b202f8855d3878089b7da4913`; execution identity is `089ab1059b0dbda4dbfb2487065e5c06e21ed81773784ec2dc6613fab3e2a46d`. All non-harness execution hashes and captured content remain unchanged. Combat-entry guards were active throughout.

All attempts remain sealed:

| Attempt | Result | Recorded duration | Retained bytes |
| --- | --- | ---: | ---: |
| v1: original runtime | Candidate could not load the new reader type; baseline completed 2,098 preparations | 36.250 s before sealing | 643,280,827 |
| v2: compatible runtime | Qualification helper assigned PowerShell's reserved `$Error` variable; neither preparation process started | 21.110 s before sealing | 642,515,631 |
| v3: corrected helper | Full runtime qualification passed | 60.907 s including sealing | 668,564,858 |

Each correction was separately declared before its attempt; neither failed package nor declaration was overwritten. The helper regression now executes its actual exception-handler text against a wrapped provider exception. The full declared qualification charge is **2,700 seconds and 3 GiB**, including both failures. All prior charges, the failed matched pair, its native denominator ceilings, the original 1,806-second assessment, and the publication-inventory amendment remain preserved. Failed-attempt timings exclude their sealing tails and must not be represented as complete measured totals. A registration preflight also caught a transcribed manifest-pin character before any declaration or attempt directory existed; the corrected pin was checked against the preceding sealed handoff.

Verification of the final implementation passed **128 backend tests and 21 Python tests**. The backend tests include a full literal placement archive and 16 new provider cases checking identical values, exact current-reader counters, exception types/messages, and partial-work counters across all eight providers. An earlier 137-test backend run passed before the compatibility failure was discovered; it is retained separately and is not additional coverage of the final change.

Commands used:

```text
build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-runtime-compatible-20260925 -Filter '(FullyQualifiedName~BalanceHarnessContentAccountingTests|FullyQualifiedName~BalanceHarnessLoadoutPlacementTests|FullyQualifiedName~BalanceHarnessLoadoutPlacementComparisonTests|FullyQualifiedName~BalanceHarnessAlliedActionPreservationTests)&FullyQualifiedName!~Full_native'
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-runtime-qualification.py"
python -B -X utf8 "Balance Harness/analysis/qualify-loadout-placement-runtime.py" register
python -B -X utf8 "Balance Harness/analysis/qualify-loadout-placement-runtime.py" run --pin 0f8683dac31494b6b113f5aa8d28f9badd71a7b2127b206a3ab2ed0dacc33605
```

The qualification commands describe completed, sealed attempts and cannot be rerun over those outputs. The first build was blocked by sandbox access to NuGet.Config; an approved execution resolved that restriction. A subsequent compile found a missing namespace import, which was fixed. The final build has zero errors and 17 existing warnings. No required verification command remains blocked.

Changed implementation files are the new `LL/tools/BalanceHarness/TowerContentProviders.cs`; its call sites in `OfflineContent`, `TowerBattleRunner`, `TowerBenchmark`, `TowerBossDiscoveryContract`, `TowerBossInventory`, and `EssenceMechanicsInventory`; and `BalanceHarnessContentAccountingTests.cs`. Three analysis files implement the bounded qualification, native replay, and Python checks. Three prospective declarations and this report retain the decisions and outcomes. Nine existing status documents change only on line 3. There are no migrations, application configuration changes, or deployments. Captured gameplay DLLs must remain frozen when packaging the qualified harness.

The next bounded step is **complete plain-format maximum-workload coverage and the separately registered matched cost pair**, retaining both audits and publication costs. The former 18,816-report fixture does not cover the 21,888-report maximum. Reconcile all changed costs without treating missing measurements as zero, keep the inherited floors and failed native denominator ceilings, and preserve the source-specific single-inventory-pass amendment. Only a passing current forecast and fresh admission can release the frozen 12-root, 21,888-fight comparison. This qualification establishes runtime compatibility, not stronger teams or improved search effectiveness.
