# Sustained independent Tower search: implementation and interrupted comparison

13 September 2026. Target: the offline `LL/tools/BalanceHarness`. Implements the [approved strategic direction](Tower-Search-Strategy-Reset.md). **Search improved, but the full comparison did not finish:** the deeper v4 baseline passed the development screen in two restarts; the 30-minute execution cap interrupted formal validation. The new behavior archive has not earned promotion. No boss retuning or floor expansion follows.

## What actually changed

The optional `independent-depth-behavior-v12` policy supports three explicit arms: unchanged v4 at 96 candidates, unchanged v4 at 384, and v4 construction/operators with a measured-behavior parent archive at 384. Three restarts evaluate **2,592 complete parties**, representing **2,519 distinct generated party IDs**, on the same eight discovery seeds. A larger v4 budget also changes its initial-construction allocation; this is a budget comparison, not a literal continuation of the small arm.

The archive preserves the best original-ranked party in each of at most 32 fixed behavior cells and selects uniformly among occupied cells. Four equal initial-party health-deficit bins cross eight mean total hostile-denial bins, whose inclusive upper bounds are 0, 30, 100, 300, 1,000, 3,000, 10,000 and infinity ticks. Dead original party members contribute full deficit. Denial includes hostile summons and duration exposure; these descriptors are not causal guardian uptime, new fitness rewards or balance targets. Final observed occupied-cell counts for the archive were 5, 6 and 4. That observation does not establish why it underperformed.

The reusable prepare/run/verify commands capture the original producing executable, content, combat settings, seed ledger, generation inputs and fixed control portfolio. Discovery freezes two finalists per arm, then screens all 24 finalists/controls on 64 fresh seeds. Validation is allowed only when the declared screen passes and uses all 24 cells on 256 separate seeds. Rank-one primaries cannot be replaced. Generation never receives reference recipes, IDs, counts, ancestry or held-out outcomes. Existing policy defaults and historical reconstruction paths remain unchanged.

Implementation files: [archive](../LL/tools/BalanceHarness/TowerBehaviorArchive.cs), [benchmark execution and verification](../LL/tools/BalanceHarness/TowerSearchBenchmark.cs), [selection and statistical rules](../LL/tools/BalanceHarness/TowerSearchBenchmarkResults.cs), scoped generation/contract/coverage changes, [CLI documentation](../LL/tools/BalanceHarness/README.md#sustained-independent-search-benchmark), and [regression tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerSearchBenchmarkTests.cs).

## Measured search result

Restarts are listed in their frozen execution order. All listed validation counts are individual completed cells inside an **unfinished family**; there is no complete-family reliability or balance assessment.

| Arm | Restart | Candidates with discovery wins / evaluated | Best discovery score | Primary screen wins / 64 | Retained primary validation |
| --- | ---: | ---: | ---: | ---: | --- |
| A: small v4 | 1233403840 | 0/96 | 0/8 | 0 | 0/256 |
| B: deep v4 | 1233403840 | 108/384 | 4/8 | 16 | 43/256 (16.80%) |
| C: behavior archive | 1233403840 | 17/384 | 3/8 | 9 | Not reached |
| A: small v4 | -7800881 | 0/96 | 0/8 | 0 | Not reached |
| B: deep v4 | -7800881 | 0/384 | 0/8 | 0 | Not reached |
| C: behavior archive | -7800881 | 0/384 | 0/8 | 0 | Not reached |
| A: small v4 | -226472186 | 0/96 | 0/8 | 0 | Not reached |
| B: deep v4 | -226472186 | 45/384 | 4/8 | 18 | 37/256 (14.45%) |
| C: behavior archive | -226472186 | 0/384 | 0/8 | 0 | Not reached |

The two successful deep-arm searches first found a discovery win at candidate **160** and **154**. Small-arm searches never recorded a discovery win. B's screening primaries scored 25%, 0% and 28.13%; the fixed anchor scored 31.25% (20/64). B therefore met the predeclared engineering gate in two restarts. C met it in none. This is evidence that a larger search allocation can uncover useful parties; it is not a formal reliability pass or proof that more budget alone solves reference competitiveness.

The fixed anchor subsequently recorded **95/256 (37.11%)** in its completed validation cell, versus B's two promising primaries at 43/256 and 37/256. Their observed anchor gaps are **−20.31 and −22.66 percentage points**, both already worse than the allowed −10-point non-inferiority margin. Those two primaries cannot satisfy that component of the frozen combined rule. The strongest saved control recorded 30/64 in screening and 111/256 in its completed validation cell. Its earlier **131/256 (51.17%)** ceiling breach remains unresolved; separate studies are never pooled or used to erase a breach.

All **24 seed-free recipes**, including every weak finalist and all six controls, are exported individually with exact equipment and ordered Essence IDs. Read the [build list](../TestResults/balance/tower-search-benchmark-verification-20260913/saved-builds.md), [measurements and recipes](../TestResults/balance/tower-search-benchmark-verification-20260913/saved-builds.json), [frozen family](../TestResults/balance/tower-search-benchmark-20260913/selected-family.json) and [complete derived findings](../TestResults/balance/tower-search-benchmark-verification-20260913/findings.json). The two promising B primaries are `team-182c7f825c1a871eee7d9faebb006c29` and `team-1f1583e7afa685170cd762c21da304da`. This export does not promote them to a catalog.

## Execution limit and technical interruptions

The original [protocol](../TestResults/balance/tower-search-benchmark-20260913/protocol.json), SHA-256 `b9f181fbfd28bdced167de04b2191a26b060b09ff65c7dffc86a3609c950eb38`, reserved at most **28,424 fights, 1,800 execute seconds, 2 GiB and zero combat retries**. Its full 24-cell reliability family allocates alpha .025 to rates and .025 across 12 paired comparisons, with 24 discordance components. These rules were not changed after outcomes were seen.

| Phase | Started | Completed | Durably retained outcomes |
| --- | ---: | ---: | ---: |
| Historical detailed parity | 4 | 4 | Four comparisons passed against captured historical reports |
| Discovery | 20,736 | 20,736 | 20,736 |
| Screen | 1,536 | 1,536 | 1,536 |
| Fixed screen detailed replays | 4 | 4 | 4 |
| Validation | 2,703 | 2,702 | 2,688 |
| Total | **24,983** | **24,982** | Validation is incomplete |

Windows first denied a directory rename while publishing eight fully completed discovery records. Their receipt hash, seeds and attempt journal matched. Original interruption evidence was preserved; the eight records were published and verified with **zero additional fights**, and the original captured search deterministically reconstructed its prefix before continuing. An explicit [recovery amendment](../TestResults/balance/tower-search-benchmark-20260913/recovery-amendment.json) charged the complete original elapsed interval, recovery and reconstruction against the same remaining time allowance.

The recovery runner then omitted `BalanceHarness.deps.json`, causing screen setup to fail **before any screen fight**. This was a recovery-runner packaging error. The zero-attempt setup was preserved, the unchanged captured dependency manifests were supplied, and a [second execution amendment](../TestResults/balance/tower-search-benchmark-20260913/finish-amendment.json) retained all limits, recipes, seed order and statistical decisions. Neither recovery changed gameplay or repeated a fight.

The remaining clock expired during validation. Cancellation unwound at **1,800.019 charged seconds**. The last 14 completed results had not reached a committed chunk, and one final battle was cancelled. All attempts remain charged. The validation prefix contains 84 intact chunks: ten complete 256-trial cells plus 128 trials of the next cell. No complete benchmark summary or quality report was manufactured. The original package remains an interrupted run, with its failures intact; [partial-results.json](../TestResults/balance/tower-search-benchmark-verification-20260913/partial-results.json) reports its actual status separately. Retained original output is approximately **951.58 MiB**; recovery, source and verification evidence are retained separately.

## Verification and preservation

- **121 distinct tests passed** through `build/run-tests.ps1`: a 120-test regression run and a 13-test focused run, with 12 overlapping tests. The focused run includes a real compact-combat fixture, deterministic zero-combat reconstruction and tamper rejection. Tests cover archive bounds/ties, unchanged v4 behavior, independent metadata/provenance, asymmetric cost accounting, cancellation/partial nomination, seed-history arrays, frozen-primary screening and adjusted quality calculations.
- The ordinary test invocation could not restore through the sandbox-restricted user NuGet configuration. A cached Release build with `--no-restore` succeeded, then `build/run-tests.ps1 -NoBuild -Filter ...` ran the required suites. The retained recovery and verification drivers built against captured assemblies with explicit cleared package sources. No build/test command remains blocked through that verified path.
- The producing harness reconstructed **all 20,736 discovery trials, all 1,536 screen trials, the selected family, both stage definitions, the screen decision and four fixed detailed replay comparisons**. Its normal compact verifier also checked all **2,688 retained validation records**, frozen recipes/seeds and the charged attempt prefix. The verifier enforced zero combat and took **68.33 seconds**, separate from the exhausted execution clock. The full `tower-search-benchmark-verify` command is inapplicable because the benchmark is incomplete; this is a scoped partial-evidence verification.
- All **1,832 recorded gameplay C# sources**, 16 gameplay content files and two saved catalogs match the previous baseline. Rebuilding changed assembly commit metadata from the earlier commit to `d0f1a8a55756d737ad849a7403b75f3326b894ec`; four historical detailed combat parity checks passed. The experiment consistently used its captured new producing identity. Source snapshots, test receipts, original failures, recovery drivers and [final verification receipt](../TestResults/balance/tower-search-benchmark-verification-20260913/final-verification.json) preserve the audit trail.

The authoritative [seed ledger](../TestResults/balance/tower-search-benchmark-20260913/seed-ledger.json) now excludes **472,525 distinct reservations**: 472,194 historical plus three generation, eight discovery, 64 screen and 256 validation seeds. Exclude every array, including unused reservations. No migration, gameplay configuration change, database change, deployment or infrastructure change occurred. Kharad remains **Health 3.04881408 / Power 3.85370128** with fixed gear and untrained/unevolved Essences.

## Next boundary

Finish the **already selected validation family** before making another search-policy or balance decision. This is a concrete execution continuation, not another provider heuristic, new discovery sweep or opportunity to replace disappointing teams. There are **3,456 missing durable validation outcomes** on the original ordered seeds. Fifteen of those are already charged attempts (14 lost uncommitted results plus one cancellation); completing them would require an explicitly amended retry/resource allowance and **28,439 cumulative attempts**, 15 above the original cap. The current zero-retry package cannot simply resume or have its ledgers reset.

The [prepared continuation proposal](../TestResults/balance/tower-search-benchmark-verification-20260913/continuation-proposal.json) lists every missing cell/seed/trial index and distinguishes previously charged attempts. It proposes 3,456 additional attempts and 180 additional execute seconds in a separate package while retaining the interrupted package unchanged. Reconstruct the full original 24-cell family from both retained sources; keep the original primaries, anchor, comparisons and multiplicity allocation. This review does not execute or allocate that continuation, reserve fresh seeds, or declare validation complete.

The present evidence already weakens the idea that a larger v4 budget alone restores anchor competitiveness: both promising B primaries are substantially below the anchor. Complete validation will close the full comparison and distinguish supported viability from reference recovery. Then choose the next substantive search change from that result. Boss calibration, the strongest-control challenge, floor expansion and practical acquisition remain separate prerequisites; the new behavior archive is not a default.
