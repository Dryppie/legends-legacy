# Competitive Tower build search

11–12 September 2026. Target: the offline `LL/tools/BalanceHarness`, its local Tower Lab and retained build library. The user requires benchmarks strong enough to represent the best players in server-wide Tower progression. Existing portfolio balance passes do not establish that search-quality requirement.

Current finding: expanded searches found teams winning up to **1,000/1,000** against the previous Kharad setting. Search quality remains **Fail** under the predeclared recovery, reliability and plateau rules. A separate 2,438-party calibration is completing confirmation and archive verification; its candidate scaling remains unapplied until the complete global assessment passes.

## Implemented tooling

- Cumulative historical exclusions now allow **1,000,000 seeds**, consistently across independent study contracts, standalone confirmation and Tower Lab's history union. Local study imports allow **32 MiB** in both the browser and HTTP handler. The **100,000-combat cap per schema-3 study or standalone confirmation definition**, 96-reference limit, candidate limits and retained record's per-study seed limit are unchanged. The separate, explicitly bounded multi-partition calibration below expands campaign-level confirmation coverage; it uses one statistical correction across its whole family.
- The explicitly versioned **`retained-teams-v1`** policy executes `improve-supplied` definitions. `retained-local` scans a seeded permutation of single substitutions and pairwise Essence-order swaps. `retained-joint` also uses double substitutions, coordinated cross-character changes, whole-character replacement and recombination. Every fourth refinement attempt explores a fresh constructive party.
- Every distinct supplied start is measured inside each arm's candidate budget. Parent selection uses current discovery outcomes. No historical fitness or confirmation outcomes enter the optimizer. Exact source IDs and ancestry propagate through descendants; fresh independent proposals remain identifiable.
- Starts must match the declared fixed character identities, gear, ordered Essences, families and owned-copy rules. This avoids silently changing the saved team's combat identity before assessing an improvement.
- Independent generation keeps its original input boundary and policy. New improvement studies use the same frozen shortlist, selection, confirmation, accounting, exact export and archive-reconstruction machinery. Legacy reserved improve definitions require explicit conversion before execution.
- Tower Lab accepts complete improvement definitions through its existing import flow and labels supplied search parents and retained-search results. The default form still generates independent teams. Compatible controls and CLI finalists can persist for future runs.

## Commands

Build the harness, then create a fresh independent preview from current content and the local retained library:

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-team-plan --floor 5 --slots 5 --seed 294711 --output TestResults/balance/new-kharad-plan --runs-root TestResults/balance --catalogs-root LL/tools/BalanceHarness/Fixtures --content-root LL/src/API/API.LL
```

Convert a fresh definition using explicit comma-separated reference IDs from its `references` list. Preparation preserves its schedules, so independently executed comparison studies must declare whether schedules are intentionally paired or allocate separate fresh schedules. Never convert a completed study and call its old seeds fresh.

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-improvement-prepare --definition <fresh-definition.json> --references <reference-id-1,reference-id-2> --output <new-preparation-directory> --content-root LL/src/API/API.LL
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-boss-study --definition <prepared-definition.json> --output <new-study-directory> --runs-root TestResults/balance --content-root LL/src/API/API.LL
dotnet <study-directory>/executable/BalanceHarness.dll tower-boss-study-verify --run <study-directory>
```

The optional study `--runs-root` persists completed search finalists, including useful builds from a failed balance study. Interrupted studies do not become controls. Historical results still require fresh measurements on future boss content.

## Frozen Kharad comparison

The reusable [comparison script](../LL/tools/BalanceHarness/Scripts/compare-tower-team-search.py) writes a protocol before combat and executes distinct `prepare`, `search`, `freeze-audit`, `audit` and `report` phases. The current [frozen protocol](../TestResults/balance/tower-competitive-kharad-20260911/campaign/protocol.json) holds Kharad's content fixed, uses ten complete characters with five Essences each, and retains all eight compatible controls. Gear, levels, baseline attributes, neutral identities and untrained Essences remain fixed; the full pool and hypothetical ownership are explicit experimental assumptions.

| Cell | Methods × restarts × candidates | Discovery combats | Selection | Maximum search confirmation | Replay reserve |
| --- | --- | ---: | ---: | ---: | ---: |
| Small independent | 2 × 3 × 128 | 12,288 | 2,048 | 13,000 | 200 |
| Small retained | 2 × 3 × 128 | 12,288 | 2,048 | 13,000 | 200 |
| Large independent | 2 × 3 × 512 | 49,152 | 2,048 | 13,000 | 200 |
| Large retained | 2 × 3 × 512 | 49,152 | 2,048 | 13,000 | 200 |

The four cells intentionally share 16 discovery, 128 selection and 1,000 search-confirmation seeds to support paired comparisons. Repeated recipes across arms are charged but do not create independent samples. A separate unused 1,000-seed audit schedule is excluded from every search.

The audit family is the exact deduplicated union of all four 16-party shortlists and every original control, at most **72,000 additional combats**. It is frozen before audit combat. Search-confirmation outcomes never select audit members. Selection measurements freeze one primary per cell and one nominee per method/restart from that arm's shortlisted recipes.

Total reservation: **183,872 search/replay combats + 72,000 audit combats = 255,872**. Unused stage allocations are not reassigned.

## Search-quality decision

The predeclared meaningful gap is **five percentage points**. This is an experimental search-quality margin, separate from the approved 10–50% boss win-rate target. Approximate simultaneous paired intervals use Bonferroni-Wilson bounds for both discordant probabilities across every unordered pair in the frozen audit family.

1. **Recovery:** both large-cell frozen primaries must support a gap of at most five points against every original control.
2. **Reliability:** separately for `constructive-joint` and `retained-joint`, at least two of the three large-cell restart nominees must support a gap of at most five points against every audited recipe.
3. **Plateau:** for each mode, the upper supported gain of the large-cell primary over the small-cell primary must be at most five points.

All criteria must pass for a bounded search-quality finding. A supported larger gap fails; unresolved interval width is inconclusive. A pass would still be evidence about this search allocation and budget, not proof of a global optimum or practical acquisition. Earlier above-ceiling candidates outside the audit remain unresolved for overall balance acceptance. New stronger builds must remain visible and may require a separately frozen calibration.

## Results and verification

All four searches completed and reconstructed with their captured producing executable. They performed **177,084 actual combats**, including 12 detailed matching replays, within the 183,872 search reservation.

| Search cell | Frozen generated primary | Generated viability | Scoped overall balance |
| --- | ---: | --- | --- |
| Small independent | 22/1,000 (2.2%) | Fail | Pass, supplied by retained controls |
| Small retained | 948/1,000 (94.8%) | Pass | Fail |
| Large independent | 1,000/1,000 (100%) | Pass | Fail |
| Large retained | 987/1,000 (98.7%) | Pass | Fail |

The strongest original control won **224/1,000 (22.4%)** on their common confirmation schedule. Two large independent finalists won 1,000/1,000; another won 999/1,000. These observations supersede the earlier Kharad portfolio's acceptance for the expanded set. They demonstrate a substantial search gap, not confidence that no stronger build exists. [Three complete primary recipes](Competitive-Kharad-Recipes-20260912/README.md) preserve every character, ordered Essence, identity and equipment field.

The original 70-party audit completed 70,000 fights but is **Invalid**: its generated scenarios exported an unknown `notes` field instead of the required `assumptions` field. The strict confirmation reader rejected the family. Preserve that [failed audit record](../TestResults/balance/tower-competitive-kharad-20260911/campaign/audit-invalid.json); its outcomes are not valid audit evidence. The helper now validates the complete contract before combat.

A [separately frozen replacement](../TestResults/balance/tower-competitive-kharad-audit-20260912/protocol.json) kept the same 70 exact parties, selection-frozen primaries/restart nominees and numerical quality rules, corrected that descriptive field, and completed another 70,000 combats on an entirely new 1,000-seed schedule. No original audit outcome selected its members. Strict archive assessment completed successfully; the [search-quality report](../TestResults/balance/tower-competitive-kharad-audit-20260912/quality-report.json) is **Fail**.

| Frozen primary | Replacement audit wins |
| --- | ---: |
| Small independent | 28/1,000 (2.8%) |
| Small retained | 956/1,000 (95.6%) |
| Large independent | 999/1,000 (99.9%) |
| Large retained | 987/1,000 (98.7%) |

Both large primaries pass recovery against the original controls. Retained restart reliability passes; independent restart reliability fails. The independent budget increase gains **97.1 percentage points**, with simultaneous paired interval **91.81–98.69 points**. The retained increase gains **3.1 points**, with interval **−1.66–7.74 points**, so its plateau remains unresolved at the predeclared five-point margin. These results support continued independent exploration alongside saved-team improvement. They do not establish near-optimal builds.

## Separate full-portfolio calibration

The [calibration protocol](../TestResults/balance/tower-competitive-kharad-calibration-20260912/protocol.json) freezes **1,548 exact parties**: every current earlier breach, shortlist and finalist; all eight original controls; and all 122 recipes from the prior Kharad calibration, deduplicated by complete combat recipe. It holds character and equipment budgets fixed. Source study confirmation outcomes may inform this new experiment; its own confirmation schedule is unused and separate.

The linked coarse sweep tests 70 current shortlisted/control teams on 16 new paired seeds at eight multipliers. A fine sweep tests all generated finalists and additional strong coarse candidates, up to 32 parties, on 64 new paired seeds at eleven settings. The selection target is 25%, with an eligible 18–35% discovery maximum, to leave room for uncertainty. This selection preference does not change the final 10–50% rule.

The frozen setting is **1.20 × both Health and Power**, relative to Health 2.106 / offense 2.661984. Its candidate values are **Health 2.5272 / offense 3.194381**. The strongest fine-sweep observation was **22/64 (34.375%)**. All 387,000 confirmation fights and their strict archive verification completed. The [global assessment](../TestResults/balance/tower-competitive-kharad-calibration-20260912/assessment-parallel.json) is **Fail**: its strongest team won **162/250 (64.8%)**, with adjusted interval **51.66–76.02%**. These candidate values remain unapplied.

Every party receives **250 fresh paired confirmation seeds**, totaling **387,000 confirmation combats**. Five normal-archive verification partitions contain at most 350 parties and 87,500 fights each. The existing C# verifier checks exact preparation, trial identities, content, settings and producing assemblies. Their individual viability labels do not determine the overall outcome: the separate campaign assessor uses one approximate Bonferroni-Wilson family of 1,548, requires every adjusted upper bound to be at most 50%, and requires at least one adjusted lower bound to reach 10%. Missing, extra, duplicate or invalid evidence blocks assessment. Six numerical/completeness tests verify this aggregation.

The explicit campaign reservation is **419,488 combats**: 8,960 coarse, 22,528 fine, 387,000 confirmation and 1,000 reserved application/replay checks. This is a larger campaign than one standalone confirmation definition; partitioning does not reduce the statistical family or silently raise the existing per-definition limits. Application requires a global Pass, followed by exact local-content parity.

The reusable [calibration script](../LL/tools/BalanceHarness/Scripts/calibrate-tower-search-portfolio.py) implements this first floor-5 campaign through `freeze`, `discover` and `confirm`. It records every input, seed schedule, selected factor and partition before dependent combat. Local NTFS compression preserves the JSON bytes and hashes while reducing the large normal-archive footprint; no evidence is deleted.

The [verification handoff](../TestResults/balance/tower-competitive-kharad-calibration-20260912/verification-handoff.json) preserves the original sequential driver's state. That driver was stopped only after all 387,000 fights completed, while it performed redundant read-only verification. Five separate normal C# archive verifiers completed all partitions; `assessment-parallel.json` is authoritative. No fight was cancelled, replaced or sampled again. The captured version-1 parallel verifier remains in this campaign; the current reusable version adds support for the explicitly expanded follow-up policy.

## Challenger searches and persistence

Four verified source studies added **14 finalist recipes** to the main local retained library, increasing compatible Kharad controls from **8 to 22**. The staged merge was validated through a new CLI preview and applied under the library lock with a source-hash guard. Historical outcomes never become fitness on later runs. At this stage the portable fixture was unchanged.

Two [selected-setting challenger studies](../TestResults/balance/tower-competitive-kharad-challengers-20260912/protocol.json) are frozen while the separate calibration confirmation runs. Both use the exact selected content, 22 controls, three restarts per method, 512 candidates per arm, 20 discovery seeds, 128 selection seeds and 1,000 confirmation seeds. Their combined reservation is **181,376 combats**. Independent and retained-derived searches intentionally share paired stage schedules; those seeds are disjoint from all preceding experiments. Their results cannot change the ongoing calibration's frozen membership or factor. If that exact content is applied, these measurements serve as its challenger check; otherwise they remain evidence about a candidate setting. Newly stronger teams must remain visible and block final acceptance when they breach the ceiling.

Both challengers completed and reconstructed. They used **176,980 actual combats**, including four matching detailed replays. The independent study's generated finalists won **1, 6, 0 and 0 out of 1,000**; generated viability is Fail. Retained controls provide its scoped balance Pass, with a maximum **299/1,000 (29.9%)**. The retained study is Fail, with generated finalists at **985/1,000 (98.5%)** and **973/1,000 (97.3%)**, plus **874 distinct earlier-breach party IDs** (890 stage-specific records). The 1.20 setting therefore fails both the complete prior portfolio and the fresh retained challenger. Neither result supports applying it.

A second validated, guarded catalog merge retains these six challenger finalists and ten strongest complete-portfolio recipes, including the 64.8% team outside the original shortlist. This adds **16 records** and increases compatible Kharad controls from **22 to 38**. All older controls remain. See the [import receipt](../TestResults/balance/tower-competitive-retention-expanded-20260912/import-result.json); retention records evidence of useful completed searches even when balance fails.

The same 16 verified records now also extend the [versioned fixture](../LL/tools/BalanceHarness/Fixtures/tower-retained-builds.json), increasing fresh-checkout floor-5 controls from **5 to 21**, while preserving floor 1's two controls and all original records. The staged candidate passed a C# preview before a source-hash-guarded atomic replacement; the [publication receipt](../TestResults/balance/tower-competitive-published-initial-20260912/import-result.json) records its exact bytes. Both strong retained challengers also have [readable complete exports](Competitive-Kharad-Recipes-20260912/README.md). This publication makes proven discoveries available without requiring the main local results folder; it does not declare their associated boss settings balanced.

## Expanded follow-up calibration

The [separate follow-up protocol](../TestResults/balance/tower-competitive-kharad-refinement-20260912/protocol.json) preserves all 1,548 prior recipes and adds every challenger shortlist, earlier breach and confirmed recipe, yielding **2,438 distinct complete parties**. No earlier breach is dropped to fit a limit. Before any new combat, it selects all original/challenger finalists plus the strongest verified prior confirmation teams, totaling 48 screening teams.

Seven linked factors span 1.00–1.50 relative to the failed 1.20 setting, on 16 paired discovery seeds. An eleven-point finer sweep uses the same screening teams on 96 additional paired seeds. Its predeclared selection preference is nearest 25%, restricted to an 18–32% observed maximum. The selected factor then freezes before **609,500 confirmation fights**: every one of the 2,438 recipes receives 250 entirely new paired seeds.

This new campaign explicitly reserves **666,564 combats**: 5,376 coarse, 50,688 fine, 609,500 confirmation and 1,000 application/replay checks. The new versioned campaign assessor permits at most **3,000 recipes and 750,000 total combats**; neither the earlier campaign's bound nor the C# per-definition limits changes retrospectively. The same 10–50% rule and one correction across the entire 2,438-party family determine acceptance. Combat execution and read-only archive verification are separate phases, allowing completed partitions to verify while remaining fights continue.

The [refinement driver](../LL/tools/BalanceHarness/Scripts/refine-tower-search-calibration.py), [parallel verifier](../LL/tools/BalanceHarness/Scripts/verify-tower-calibration-partitions.py), [global assessor](../LL/tools/BalanceHarness/Scripts/tower-calibration-assessment.py), [local application checks](../LL/tools/BalanceHarness/Scripts/finish-tower-search-calibration.py) and [retention staging tool](../LL/tools/BalanceHarness/Scripts/stage-tower-competitive-retention.py) preserve frozen inputs, source hashes and separate outputs. Application requires a verified global Pass. Its reserved checks compare the published original primaries and up to ten strongest calibrated teams against their exact confirmation reports, plus before/after checks on all unaffected floors.

All 56,064 discovery fights completed. The coarse bracket was **1.38–1.44**; the fine sweep selected **1.392**, with a screening maximum of **24/96 (25%)**. Its frozen candidate values are **Health 2.931552 / offense 3.705482**. Complete-portfolio confirmation and overlapping normal-archive verification are running; the candidate remains unapplied.

The current rebuilt assemblies differ from the retained producing build. A [portable-PDB source checksum comparison](../TestResults/balance/tower-competitive-kharad-refinement-20260912/source-checksum-comparison.json) compares 1,415 document paths across five first-party assemblies: differences are confined to three tournament files and two harness display files. Tower preparation and combat source checksums match. Local application nevertheless requires twenty exact current-build comparisons for each selected parity team against the frozen confirmation reports, then twenty further comparisons on the applied local content. Both sets and the unaffected-floor checks fit the existing 1,000-combat parity reserve. These are reproducibility checks, not new acceptance samples.

An early current-build diagnostic selected the first frozen portfolio entry by ID, without consulting its outcomes. All twenty repeated-seed reports matched exactly. These twenty fights are charged to the same parity reserve; they do not extend confirmation or replace the later checks on the strongest verified teams.

The optional LZX compression pass stopped on a native Windows disk-space error while the drive still reported more than 200 GiB free. Its 2,463 recorded successful operations recovered **22.03 GiB** with identical before/after SHA-256 hashes. Full original C# archive verification confirmed that the affected archive and four additional archives covering in-flight work/the initial sample remain intact: **1,250 historical fights checked, none rerun**. The [final integrity record](../TestResults/balance/tower-competitive-storage-20260912/final-integrity.json) closes this storage incident. Compression remains partial and stopped; no evidence was deleted, and the separate confirmation continues.

A separate read-only verification probe used normal filesystem access and the unchanged first partition. It was stopped before completion because it established no clear speed benefit and added competing archive reads. It produced no assessment and is not acceptance evidence. The [stop receipt](../TestResults/balance/tower-competitive-kharad-refinement-20260912/expedited-probe-stopped.json) records that the original verification and all combat runs continued.

Subsequent profiling found millions of small reads in normal archive validation. `HarnessJson` now uses 128 KiB buffers for JSON text and SHA-256 file reads, preserving UTF-8 defaults, BOM detection and all validation logic. The [equivalence check](../TestResults/balance/tower-competitive-buffer-verification-20260912/result.json) produced identical complete evidence and assessments for five previously verified archives, covering 1,250 historical fights without combat. A further [timing comparison](../TestResults/balance/tower-competitive-buffer-verification-20260912/timing.json) measured 14.656 seconds with the original reader and 7.235 seconds with buffering; this single sequential comparison remains sensitive to cache order and competing work.

The [verification handoff](../TestResults/balance/tower-competitive-kharad-refinement-20260912/buffered-verification-handoff.json) stopped only the original read-only controller and four archive readers. No partition assessment had been emitted, and no combat was cancelled or replaced. The [buffered verification driver](../LL/tools/BalanceHarness/Scripts/verify-tower-calibration-buffered.py) restarts the same seven frozen partitions with four workers and the separately captured, proven reader. It preserves the original driver, plans and logs and still writes the authoritative complete `assessment-parallel.json`. The [source checksum comparison](../TestResults/balance/tower-competitive-buffer-verification-20260912/source-checksum-comparison.json) records the reader change, generated assembly-version metadata, and the already identified tournament/display differences; Tower preparation, combat and evaluator source checksums match the producing executable.

The retention staging helper also accepts `--catalog <existing-catalog>` so the same verified leaders can extend either the local library or the versioned portable fixture. It only stages and validates candidate bytes. The final merge must still check the recorded source hash and preserve existing records; local library replacement uses its existing exclusive lock.

## Remaining competitive evidence

Search quality remains **Fail** under the original predeclared rule, regardless of whether the expanded portfolio calibration passes. The next search increment should improve independent restart reliability and test whether larger allocations produce little additional gain. Repeated support combinations across characters are a useful candidate proposal family to investigate from mechanics; that is an implementation hypothesis, not a demonstrated cause of the observed wins or a reason to hardcode any published team.

After tuning, use a separately frozen challenger study on the applied setting. Its members and fresh schedule must not be selected from that study's confirmation outcomes. Then revisit floors 1–4 using independent and retained searches, before continuing floors 6–9 at five Essences, floor 10 at six and floor 11 at seven. Practical top-player access to Essences, copies, training and equipment still needs an explicit budget. The current full-pool experiments do not establish that acquisition model.

## Verification

The reviewed Release build succeeded with **zero warnings or errors**, followed by **191 relevant backend tests** through the required wrapper. Coverage includes supplied-parent propagation, independent-mode isolation, owned-copy and fixed-identity rejection, legal mutations, cancellation, exact CLI/discovery/study reconstruction, persistence, dashboard import/run/results, a history exceeding the former 100,000-entry limit, imports larger than 2 MiB, and unchanged combat caps. JavaScript syntax and whitespace checks passed. **Twelve Python tests** passed separately: nine campaign-assessor tests cover the larger explicit policy, identical arithmetic on overlapping family sizes, complete 2,400-recipe coverage and invalid-cap rejection; three quality-report tests reject invalid strict assessments, duplicate teams and mismatched outcomes. The completed replacement audit also passed this additional read-only validation. Its frozen original driver and quality report remain unchanged; future campaigns use the hardened reporting function.

The later buffered-reader build also succeeds with zero warnings/errors. A broader concurrent test run recorded 386 passes and three dashboard timing failures before its test host was stopped to reduce contention with the large campaign. Preserve `TestResults/tower-competitive-buffer-tests-aborted.trx` and its log. Final verification must rerun after campaign load falls; this interrupted run is not an overall test pass. New encoding/hash tests cover multi-buffer UTF-8, UTF-8 BOM and UTF-16 files, including detection of changed file bytes.

The [real-browser verification](../TestResults/balance/tower-competitive-ui-20260912/ui-verification.json) imported a 5.7 MB definition, displayed eight supplied search parents, completed **102 of 112 reserved combats**, reconstructed its result, and matched an additional selected-fight replay. Its small statistical result was correctly Inconclusive and was not promoted into the competitive build library. The visible results and console check passed. A remaining static heading was generalized to “Team study results” afterward; the final display build also passed with zero warnings or errors. Frozen producing executables retain their original display text, while current source includes the corrected text and import limits.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~BalanceHarnessTowerRetainedBuildTests|FullyQualifiedName~WorldTowerTests'
```

The initial wrapper build could not read the sandbox-protected NuGet configuration; building with already restored packages avoided restore. A generated Persistence build-folder permission issue required an approved build retry. Final builds and test execution succeeded. No package dependencies, production configuration, migrations, database changes or deployment are introduced by this tooling increment.
