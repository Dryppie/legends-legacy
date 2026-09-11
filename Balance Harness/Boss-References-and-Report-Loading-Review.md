# Boss references, fixed validation and report loading

11 September 2026. This follow-up completes the three actions after the [refinement review](Boss-Specific-Essence-Loadout-Refinement-Review.md): fixed fresh-seed Nhalia validation, portable refinement references in Tower Lab, and measured improvements to completed-report loading. Implementation is confined to the offline Balance Harness, its tests and documentation.

## Fixed Nhalia validation

The unchanged historical anchor, previous primary and previous exploratory recipe were tested on the same **100 unused seeds** under the original captured executable, content, settings and complete seven-slot parties. Recipes, schedules, inputs and replay policy were frozen before combat; no search or reselection was performed.

| Fixed recipe | Fresh target victories | Historical refinement victories |
| --- | ---: | ---: |
| Historical anchor | 0/100 | 0/40 |
| Previous primary | 5/100 | 1/40 |
| Previous exploratory | 8/100 | 6/40 |

The exploratory recipe gained eight winning seeds and lost five relative to the previous primary; they shared no winning seed. These remain unreliable builds. The separate observations do not establish method superiority or fix the recorded transfer weaknesses. The experiment used **305 of 312 reserved maximum combats**, including five predeclared-rule detailed parity replays. Freshness means disjoint from the available local exclusion sources, not an account-wide guarantee.

The [complete validation review](Nhalia-Fresh-Validation-Review.md) contains paired outcomes, uncertainty, recovery measurements, exact recipes and reproduction commands. Its independently verified package contains 517 sealed files, with manifest SHA-256 `d94ad2683d159101987cf5f6e5338a58620fbf929ad34f38ca759be7bc4d103f`.

## Portable historical references

Tower Lab retains **130 distinct recipes**: 42 for Eydis at five slots, 35 for Kodoku at four slots and 53 for Nhalia at seven slots. The original pilot fixture remains byte-identical. Every old recipe survives, and all 61 earlier Eydis/Nhalia observations retain their complete 20-sample evidence separately from the later 40-sample observations.

The new catalog includes complete target parties, ordered Essences, identities, gear, all-floor matrices, frozen transfer contexts, strategy origin, anchors and source hashes. Import checked 205 source files against the sealed refinement package and reproduced the fixture byte-for-byte. Its SHA-256 is `c7c9c56117a92a4be9a3de39896316c60b4300735e8bb62f43dee3f2f48ee065`.

The historical anchors remain the discovery-frozen refinement primaries: Eydis 40/40 and Nhalia 1/40. The later-noticed Nhalia 6/40 alternative does not replace its primary. Kodoku keeps its pilot anchor and provenance. Existing studies load their original frozen catalog and preserve their historical controls and report semantics.

Open **Inspect retained recipes and historical results** beneath a matching boss plan. The initial request loads a compact selector; choosing a recipe loads its full party and observations. Earlier observations appear separately. Rows that alias the same effective context explicitly identify shared observations and are not pooled. Exact JSON export retains original seeds; it reproduces historical inputs and adds no fresh evidence. Transfer rows depend on their recorded allies, not just the exported target party.

The catalog contains 16,760 excluded integers from the pilot/refinement records. The fixed validation's additional seed ledger remains in its separate sealed package. Any future campaign must also exclude that ledger before claiming fresh samples; no new search was launched in this follow-up.

## Verified report loading

Read-only profiling used the sealed Eydis refinement archive with 53,184 recorded combats and 42 finalists. Both versions had a warm operating-system file cache; the first read used a new dashboard service with no cached result. These are observations on this machine, not latency guarantees.

| Operation | Producing build | Updated build |
| --- | ---: | ---: |
| Native reconstruction | 164.409 s | 120.060 s |
| First dashboard report | 192.819 s | 116.554 s |
| Reopen the same report | — | 2.078 s |
| Export recipe after report load | — | 1.994 s |
| Export report after report load | — | 2.014 s |

The first dashboard read is **39.6% faster** and still takes about two minutes for this archive. Reopening the same result is about 93 times faster than the former uncached read. Switching archives replaces the single cached entry, so returning to the previous archive requires full reconstruction again.

Every access still enumerates the complete inventory and hashes every archived file. A completed result and compact battle previews are held as serialized JSON, so callers cannot mutate the trusted result. A changed inventory invalidates reuse and requires full semantic reconstruction. No timestamps or persistent trust files bypass verification. Incomplete and cancelled results are not cached.

Native reconstruction reuses a prepared party input only across consecutive seeds of the exact same scenario. Each trial still checks its full reconstructed input hash, arm-specific cache key, stage, seed and identity. Compact battle previews avoid a second inventory pass and repeated report decompression. The browser displays elapsed loading time and cancels a superseded request when another saved run is selected.

The native report hash is identical to the producing build. All dashboard evidence, repeated-read results and exported recipe/report bytes match. Only the expected historical `replayCompatible` flag differs, because exact replay still requires the original executable. Profiling executed no combat and wrote nothing into the source archive. Drivers, measurements and the comparison verifier are in [`TestResults/balance/boss-next-20260911-performance`](../TestResults/balance/boss-next-20260911-performance/).

## Changed files and verification

| Files | Purpose |
| --- | --- |
| `LL/tools/BalanceHarness/TowerBossReferences.cs`, `Fixtures/tower-boss-references-refinement.json`, `Scripts/import-boss-refinement-references.py` | Portable catalog, independent prior observations, frozen contexts, provenance and reproducible import |
| `TowerDashboardBossReferences.cs`, `TowerDashboardServer.cs`, `Dashboard/index.html`, `Dashboard/dashboard.js`, `Dashboard/dashboard.css` | Historical recipe list/details/export and cancellable loading feedback |
| `TowerLoadoutArchive.cs`, `TowerBossSearch.cs`, `TowerDashboardBosses.cs`, `TowerDashboardLoadouts.cs` | Fresh inventory verification, exact input-template reuse, compact previews and one completed-result cache |
| `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBossReferenceTests.cs`, `BalanceHarnessTowerBossRefinementTests.cs`, `BalanceHarnessTowerBossSearchTests.cs`, `BalanceHarnessTowerDashboardBossTests.cs` | Catalog compatibility, evidence isolation, multiple-seed reconstruction, UI API and cache-integrity regressions |
| Harness README, implementation guide, this review and `Nhalia-Fresh-Validation-Review.md` | Usage, measurements, limitations and sealed validation results |

The required repository test command passed **245 tests, zero failed or skipped**:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower|FullyQualifiedName~CompactCombatTelemetryTests'
```

Coverage includes same-length/same-timestamp file tampering, added and missing files, forged evidence plus an updated manifest, cancellation, caller-mutated cached objects, exact exports and historical catalog compatibility. A subsequent build after simplifying one test assertion passed with zero errors and five existing warnings in unrelated tests. The initial sandbox NuGet configuration read failure was resolved by the authorized retry; no verification command remains blocked.

Headless Edge/Chromium browser checks passed for all three historical budgets, primary versus exploratory labels, separate 20-/40-sample matrices, context aliases, exact exports, superseded-request cancellation and completed-report filters. The real saved Eydis study preserved its original 25 controls and 42 finalists. Reopened results were identical, with no JavaScript errors. Screenshots were inspected at 390 and 1,440 pixels; neither viewport had document overflow. The local dashboard was stopped after verification. Browser timings were collected during concurrent compatibility work and are not the isolated performance measurements above.

The current Release executable also reconstructed the schema-1 pilot Eydis archive (**12,784 recorded combats**) and schema-2 refinement Nhalia archive (**50,488 recorded combats**). Both passed frozen-selection and all-floor parity with zero new fights; package/study manifests remained unchanged. Together with the Eydis refinement profile, these checks cover both historical boss-search formats. Exact historical replay continues to use the captured producing build.

`node --check` and scoped `git diff --check` passed. The reference import was repeated byte-for-byte, and the fixed validation's 517-file seal passed again. The shell's plain `python` command was unavailable; the bundled Python runtime ran these checks successfully. Test logs/TRX, source and executable snapshots, browser evidence, compatibility commands/hashes and linked performance evidence are captured in [`TestResults/balance/boss-follow-up-20260911`](../TestResults/balance/boss-follow-up-20260911/).

There are no migrations, production configuration changes, database actions or deployment requirements. Production combat/content and unrelated working-tree changes are preserved. The fixed validation uses its captured producing build; the new harness does not reinterpret historical replays as compatible with a different executable.
