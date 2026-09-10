# Tower loadout reliability and ally transfer — 10 September 2026

This follow-up strengthens the [four-Essence pilot](Tower-Loadout-Pilot-Review.md) with more search seeds, more combat samples and a second fixed ally context. It keeps the existing guided algorithm and equal-cost uniform-random comparator, so increased replication and sampling can be assessed before adding search complexity. The new offline command is `tower-loadout-reliability`.

Evidence, the producing executable, source snapshots and logs are retained at `TestResults/balance/tower-loadout-reliability-20260910/`. The full all-floor reports are `run/pilot.md/json`; `run/reliability.json` adds method comparisons and transfers between ally contexts. Historical evidence remains separate.

**35,280 battles completed:** 17,280 discovery and 18,000 confirmation. All 116 Tower tests passed, 13 detailed replays matched, and an exported recipe's 40 results matched the compressed study reports after JSON decoding. The new archive occupies approximately 655 MB, with lossless report compression saving 87.74% against the same minified JSON payloads.

## Scope and frozen decisions

- One gear cohort: **Uncommon Standard rank 1, tier 1, level 30, baseline rolls**, with four level-1 unascended/unevolved Essences per participant. Concentrating on the weakest entry gear budget makes stronger sampling practical; the earlier four gear cohorts and 4–10-slot progression fixtures remain preserved.
- All **15 floors**, with legal production RequiredSlots. Larger parties retain repeated cells; only target **party slot 2** in the first cell changes during search.
- Two fixed ally contexts: the original balanced party, and the earlier candidate-05 Guardian/Striker substitutions. The target keeps its original recipe and runtime identity in both contexts. Allies and equipment do not adapt within a search.
- Four generation seeds: **4111, 5227, 6337 and 7451**. Each guided/random arm evaluates **12 candidates × 15 floors × 6 paired discovery samples = 1,080 actual combats**. Total discovery cost is **17,280** fights across the two contexts and four search seeds.
- The predeclared objective remains floor-1 wins, then all-floor wins, lower mean guardian health, higher survival and stable ID. Essence order remains part of the existing execution/search contract. Roles remain labels, not imposed Essence restrictions.
- Discovery and confirmation master seeds are **202609103** and **202609104**, respectively. Actual generated seeds must be distinct, disjoint between stages, and absent from the **183 declared historical exclusions**: previous pilot discovery/confirmation schedules and the original three order-probe seeds. The exclusions and both new schedules are saved before combat. This is a check against the declared historical ledger, not an account-wide seed-use registry.

Schema 2 freezes a **common finalist union** per gear budget before any confirmation fight: the target control, the two promising historical pilot loadouts, and every guided/random arm winner from either ally context. Identical ordered loadouts merge. Every finalist then runs every floor with **both** ally parties at **40 new samples per floor**. Thus a loadout discovered with one party cannot silently avoid testing with the other. Confirmation does not reselect winners.

The two predeclared historical loadouts are A (`be46b0ccba61…`: Glade Panther / Plague Ghoul / Nightshade Blossom / Illusion Fox) and B (`a639fc770419…`: Feral Ghoul / Ravenous Ghoul / Thornback Boar / Green Slime), in that order. They were fixed from historical evidence before this study, not selected from its confirmation results.

At most 19 unique finalists can enter the shared union, giving an upper bound of **40,080 battles** including discovery. The hard cap is **45,000**. Each method pays the same actual combat budget; exact cache entries remain isolated by arm so shared controls/candidates cannot subsidize one method. Generation-seed replications share paired combat schedules; neither those repeats nor the two ally contexts are independent extra clear-rate samples.

Deduplication retained **15 finalists**, each confirmed in both contexts: 15 × 2 × 15 floors × 40 seeds = **18,000 confirmation battles**. All 17,280 discovery references were distinct actual trial records; there were zero cache hits and zero overlaps with the declared historical seed exclusions.

## Results

**Stronger allies can hide a weak target loadout.** Every finalist, including the target control, won 40/40 floor-1 confirmation fights with candidate-05 allies. With original balanced allies, some finalists won only **18/40** or **19/40**, compared with the unchanged target's **25/40**. A character loadout should not be called strong merely because an already strong party carries it through that boss.

The earlier A and B loadouts both retained **40/40** floor-1 results with either ally context. A new candidate **C, `6a0959f5cab0…`**, found by random search at seed 7451, also won 40/40 in both and had the highest observed all-floor win totals among the frozen finalists. Its ordered Essences are:

1. `essence.nightshade_blossom`
2. `essence.spider_queen_royal_venom`
3. `essence.venomous_spiderling`
4. `essence.cinder_beetle`

| Fixed loadout | Original allies: floor 1 / 40 | Alternative allies: floor 1 / 40 | Original allies: all floors / 600 | Alternative allies: all floors / 600 |
| --- | ---: | ---: | ---: | ---: |
| Target control | 25 | 40 | 47 | 117 |
| Earlier A | 40 | 40 | 113 | 153 |
| Earlier B | 40 | 40 | 104 | 144 |
| New C | 40 | 40 | 118 | 164 |

These all-floor totals summarize the declared encounter matrix, not a pooled universal win probability. All 450 per-candidate/context/floor cells, including defeats, draws, regressions, paired differences and intervals, remain in the full report. Candidate C is the strongest observed on these totals, not proven better than A/B or optimal across the catalog. Forty out of forty has a pointwise Wilson 95% interval of approximately **91.2–100%**, while 25/40 has approximately **47.0–75.8%**. Do not merge both contexts into an artificial 80-sample independent estimate.

Confirmation outcomes of the guided/random discovery winners (G/R):

| Ally context | Search seed | Floor-1 wins G/R (each /40) | All-floor wins G/R (each /600) |
| --- | --- | --- | --- |
| Original | 4111 | 35 / 35 | 76 / 76 |
| Original | 5227 | 19 / 40 | 27 / 106 |
| Original | 6337 | 34 / 21 | 70 / 37 |
| Original | 7451 | 34 / 40 | 51 / 118 |
| Alternative | 4111 | 40 / 40 | 133 / 127 |
| Alternative | 5227 | 40 / 40 | 116 / 124 |
| Alternative | 6337 | 40 / 40 | 110 / 117 |
| Alternative | 7451 | 40 / 40 | 114 / 164 |

On confirmation all-floor totals, guided led in two comparisons, random led in five and one tied. Floor-1 comparisons were guided one, random two and five ties. Some guided mutations helped, but the study still establishes **no dependable guided-search advantage**. In particular, the alternative-context seed-6337 guided winner led discovery 18–17 on all-floor wins, then trailed its random/control counterpart 110–117 in confirmation. Keep the random baseline and fixed historical candidates; do not add search complexity on the assumption that the beam/mutation method is already better.

The next search-quality improvement should address the strongest-ally floor-1 ceiling and retain performance across contrasting contexts. Use original-context performance and per-context gains/regressions to avoid promoting builds carried by teammates. Any changed selection policy belongs in a fresh experiment with new confirmation seeds; this run's results and shortlist remain frozen.

## Implementation

`TowerLoadoutReliability.cs` defines the stronger default, materializes fixed ally contexts with preserved identities, freezes the common union and reports both method replication and ally transfer. `TowerLoadoutPilot.cs` adds a strict schema-2 contract and reconstructs discovery recipes/seeds/scores, frozen selection and every confirmation metric from the recorded fights. `Program.cs` exposes the new CLI command through the existing search/verify/replay workflow.

`TowerLoadoutArchive.cs` adds explicit `gzip-json-v1` report storage. It losslessly compresses the **complete** battle report; no ability contributions, terminal states or replay checks are discarded. Each trial still has its own input hash and recorded provenance. Recipes remain normal JSON for export through the existing Tower command. Schema-1 raw JSON archives remain readable, and historical replay still requires each run's original executable/runtime/platform.

`BalanceHarnessTowerReliabilityTests.cs` verifies total-cost bounds, seed exclusions, fixed target/gear/identity across contexts, shared winner transfer, compressed semantic round trips, detailed replay, cancellation with retained partial trials and rejection of falsified confirmation metrics even when the outer file checksum is updated. Additional independent normal-Tower tests persist/reload actual character snapshots and compare preparation, playback and outcome for the alternative allies on floors 1, 10 and 15.

## Verification

```powershell
./build/run-tests.ps1 -Configuration TowerReliabilityFinal -Filter 'FullyQualifiedName~BalanceHarnessTower'
dotnet TestResults/balance/tower-loadout-reliability-20260910/executable/BalanceHarness.dll tower-loadout-reliability --output TestResults/balance/tower-loadout-reliability-20260910/run --content-root LL/src/API/API.LL
dotnet TestResults/balance/tower-loadout-reliability-20260910/executable/BalanceHarness.dll tower-loadout-verify --run TestResults/balance/tower-loadout-reliability-20260910/run
```

**All 116 Tower tests passed** through the required backend runner; the preceding 113-test run also passed before the three additional normal-Tower parity cases. The new reader successfully verified the original 8,760-battle pilot. All **11,384 checksum-listed files** from that retained pilot remain unchanged. Test execution used authorized access to the local NuGet configuration/cache. Existing unrelated warnings remain; no production code changed, so the full backend suite was not repeated for this increment.

The new archive verifier checked all **35,280** saved trials, reconstructed discovery recipes/schedules/fitness and the common frozen selection, and reconstructed every confirmation cell and the method/transfer report. **Twelve detailed study replays matched**, covering both ally contexts, controls, historical A/B, the new C finalist, victories, defeats and floor 15. The exported original-context A recipe executed through the existing `tower` command for all forty confirmation seeds; all forty complete results matched the compressed originals after JSON decoding, and a detailed normal-bundle replay matched. That gives **13 detailed replays** including the export. The forty export fights repeat existing seeds and are verification only. No command remains blocked.

Storage measurements: **587,433,744 bytes** of compressed battle reports versus **4,791,192,487 bytes** of the same minified JSON payloads before gzip (**87.74% reduction**). The complete run contains **37,890 files / 654,828,792 bytes**, approximately **655 MB** in decimal units. This is a much smaller archive than the preceding 2.24-GB pilot despite roughly four times as many fights; the comparison is a local storage observation across different experiments, not a controlled performance claim. Approximately **975 seconds** elapsed between capturing the scope and writing the report. File-count growth and single-worker execution remain scaling limits.

## Interpretation and remaining limits

Clear-rate Wilson 95% intervals and paired differences remain descriptive; they neither select Essences nor certify a loadout or search method as superior. Method tables keep each search-seed result separate. Ally-transfer tables show raw wins alongside each context's own control; a raw cross-context gain also includes stronger allies. All regressions and zero-clear floors remain visible. Forty confirmation trials provide more precision than the earlier ten, but do not imply simultaneous confidence across all loadouts/floors.

This study covers one target, two authored ally contexts and one gear budget. It does not establish general-purpose builds across all possible allies, search all roles/5–10 slots, jointly optimize party members or validate the six-slot floor-10 progression budget. A matched-cost comparison against the earlier authored search, specialist archives, account ownership/training constraints, multiworker scheduling, resume, broad multiplicity-controlled claims and dashboard integration remain open. The existing dashboard still invokes the earlier authored search.

The harness implementation, tests and documentation changed. No production combat rules, boss coefficients, application configuration, migrations or deployments changed. Earlier starter/Tower evidence and unrelated working-tree changes were preserved; Phase 2 integration stays deferred.
