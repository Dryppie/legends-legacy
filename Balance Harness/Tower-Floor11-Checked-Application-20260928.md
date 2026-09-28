# Floor-11 checked local application — 28 September 2026

**Historical Rare-budget application, subsequently superseded locally.** The [carried-equipment application](Tower-Carried-Equipment-Checked-Application-20260928.md) now uses **Health 17.94375 / Power 23.0175**. The original application below set **Health 6.525 / Power 8.37** and retains its evidence unchanged. The current build prepared all **228** captured recipes, matched all **58,368** saved input hashes and reproduced all **12** representative battle reports exactly. The [independent audit](../TestResults/tower-floor11-application-owner-20260928/independent-audit.json) passed on its first run and confirms exactly two content fields changed.

## Content and design

The target is the primary game's local [World Tower content](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). The [completed independent confirmation](Tower-Floor11-Higher-Setting-Confirmation-20260928.md) supplies the setting:

| Floor-11 field | Previous | Applied | Factor |
| --- | ---: | ---: | ---: |
| `guardianScaling.health` | 2.90 | **6.525** | 2.25 |
| `guardianScaling.offense` (Power) | 3.72 | **8.37** | 2.25 |

Both values use the same factor, retaining the Health/Power ratio. The edit belongs to Serevin's floor-specific scaling. The shared creature definition remains identical to the confirmed content. A byte-local replacement preserves the file's existing mixed line endings; independent structural comparison requires exactly these two changed values and equality with the complete confirmed floor document. The other 28 captured content files must match byte for byte.

The repeating ten-floor equipment curve remains Rare/Standard/rank 2 for positions 1–3, Epic/Fine/rank 3 for 4–6, Unique/Exceptional/rank 4 for 7–9, and Legendary/Masterpiece/rank 5 for position 10. Character levels, Essence counts, ordered Essences, party positions, equipment and IDs stay attached to their captured recipes. The supported search remains `affinity-creation-with-benchmark-validation-v1`.

## Application verification

The [owner protocol](../TestResults/tower-floor11-application-owner-20260928/protocol.json) freezes the edit, executable/source hashes and deterministic replay selections before changing content or starting diagnostics. The original floor file is retained as [floor-before.json](../TestResults/tower-floor11-application-owner-20260928/floor-before.json). The owner requires the pinned, independently audited `Pass`; it verifies every consumed source artifact against its retained manifest.

The opt-in [native check](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11ApplicationTests.cs), enabled by `LL_FLOOR11_APPLICATION`, uses the **newly built current game assemblies**, with no historical DLL substitution. All five current assembly hashes differ from the captured confirmation build; [scope.json](../TestResults/tower-floor11-application-20260928/scope.json) records the actual execution identity. Compatibility must therefore be demonstrated by inputs and outcomes, rather than inferred from binary identity.

The fixture copies the current content, captures only sanitized effective Tower settings, and requires agreement with attribute rules 18, equipment balance 4, `healing-v1`, the captured threat rules and 10 ticks per checkpoint. It prepares all **228** recipes through production preparation with combat disabled, then reconstructs **all 58,368** saved input hashes with the exact 256-seed panel. Only after that succeeds may it execute the **12** preselected complete-report replays:

- First archived win and loss for each of the repeated and alternating Shadow Imp additions: four reports.
- First archived win for the repeated Gnoll Shaman and Lizardfolk Elementalist additions: two reports.
- First seed for both level-60 six-Essence controls and both level-50 resistance-and-health controls: four reports.
- First seed for the four-Essence anchor with resistance-and-health gear and the first retained original seven-Essence baseline: two reports.

These are deterministic integration checks using existing seeds. They add **zero fresh samples** and do not modify any statistical result or exclusion ledger. Complete saved reports are compared, including outcome, checkpoints, participants and statistics, at the original report detail level; no new event capture is enabled. Full input parity covers the entire fixed family, while battle parity covers these 12 representatives.

The [Python owner and auditor](analysis/check-floor11-application.py) runs the fixture through `build/run-tests.ps1`, limits diagnostics to 12 fights, 900 seconds internally, 960 seconds for the process owner and 512 MiB of output, and prohibits automatic retries/resume. Builds and ordinary backend regression simulations are separate from this diagnostic budget. The independent `verify` mode authenticates the output inventory, compares the two-field edit and all saved/current reports structurally, checks the full input-match journal, effective content/settings, runtime hashes and process drainage. It starts no fights.

The native check took **169.17 seconds**. The owner completed in **171.36 seconds** and drained all **eight processes**. The sealed output is **11,878,341 bytes**. All 12 attempts completed, with no retries or fresh samples. The auditor confirmed all 28 other content files remain identical to the confirmed study.

Application manifest: `a499d45ac176b3a9e13118b6cc1cabd28943685641ce7b78af0e7e99aba5cb2d`.
Result SHA-256: `aa4479956d4c69896984b2cd32f6a339234eb830d386593cfa65d678a1127830`.
Source confirmation manifest: `41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378`.

The source archives, current build and application outputs remain local under ignored `TestResults`; they are required for historical reproduction and are absent from a clean checkout. The application command requires the exact unapplied baseline and new output directories, so it intentionally refuses to reapply or resume after this completed operation. Further read-only audits require a new receipt path.

## Changed files and verification

Changed application scope:

- `LL/src/API/API.LL/Data/world-tower/tower-floors.json`: the two floor-11 scaling values.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11ApplicationTests.cs`: opt-in current-build input/preparation/report parity with bounded diagnostic accounting.
- `Balance Harness/analysis/check-floor11-application.py`: guarded local application, frozen representative replay selection, process ownership and independent audit.
- This report, `Tower-Floor11-Higher-Setting-Confirmation-20260928.md` and `LL/tools/BalanceHarness/AFFINITY-SEARCH.md`: applied status, verification evidence and next balance target. Historical failed/confirmed experiments retain their original results.

The focused regression suite passed **182 tests before and 182 after application**. It covers production Tower preparation/playback, Tower services, full-report replay integrity, prepared-runtime reuse, the equipment curve, progression upgrades and Serevin's abilities. Two opt-in study fixtures were skipped in those ordinary runs; the new application fixture was then explicitly enabled and passed separately. The historical progression-upgrade study was not rerun. The full initial build reported **45 existing warnings and zero errors**; the final incremental fixture build reported **16 existing warnings and zero errors**.

Python syntax and CLI checks, documentation links, new-file whitespace and `git -c core.safecrlf=false diff --check` passed. No required verification command remains blocked or unrun. The [post-application regression log](../TestResults/tower-floor11-application-regressions-20260928.log) and [native application log](../TestResults/tower-floor11-application-owner-20260928/application.log) retain the results.

Commands executed from the repository root (Python denotes the bundled runtime):

```powershell
$filter = 'FullyQualifiedName~WorldTower|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~Serevin|FullyQualifiedName~BalanceHarnessFloor11ApplicationTests'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor11-application-build-20260928' -Filter $filter
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor11-application-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor11ApplicationTests'
# Historical execution record: apply intentionally refuses to rerun on the now-applied baseline.
python -B -X utf8 'Balance Harness/analysis/check-floor11-application.py' apply --package 'TestResults/tower-floor11-application-owner-20260928' --output 'TestResults/tower-floor11-application-20260928' --artifacts 'TestResults/tower-floor11-application-build-20260928'
python -B -X utf8 'Balance Harness/analysis/check-floor11-application.py' verify --package 'TestResults/tower-floor11-application-owner-20260928' --manifest-pin 'a499d45ac176b3a9e13118b6cc1cabd28943685641ce7b78af0e7e99aba5cb2d' --receipt 'TestResults/tower-floor11-application-owner-20260928/independent-audit.json'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-floor11-application-build-20260928' -Filter $filter
git -c core.safecrlf=false diff --check
```

## Meaning and remaining work

The original confirmation remains the strength evidence: the repeated Shadow Imp upgrade won **47/256 (18.36%)**, with an approximate simultaneous adjusted interval of **11.10–28.82%**. All 30 four-/six-Essence controls won zero, with adjusted upper bounds of 5.06%. The previous factor-2.0625 failure remains preserved. Only one intended team demonstrates the 10% viability threshold; broad build diversity, unsearched combinations and practical acquisition remain unresolved.

The successful application verification closes this floor-11 calibration cycle at its reference budget. Floor 10 has subsequently completed calibration, [fresh confirmation](Tower-Floor10-Family-Confirmation-20260928.md) and [checked local application](Tower-Floor10-Checked-Application-20260928.md) of Health **12.71** / Power **7.13** at Legendary/Masterpiece/rank 5. That separate application changes no floor-11 value. The subsequent [carried-equipment screen](Tower-Carried-Equipment-Screen-20260928.md) completed **14,592 historical fights** and passed its independent audit: all **228 combinations won 32/32**, including every six-Essence transition control. This exposes a separate progression-balance gap. Next, calibrate with retained Legendary equipment and matching upgrades of the successful floor-10 parties, preserving the requested curve and supported search. The Rare-reference confirmation remains valid only for its declared budget.

This is a local content change. There are no migrations, dependencies, appsettings changes, shared-database operations, deployments or service restarts. The Tower provider loads its definitions into a singleton; an already running service needs a normal restart/content reload before using the edited file. Existing unrelated equipment-migration work is preserved.
