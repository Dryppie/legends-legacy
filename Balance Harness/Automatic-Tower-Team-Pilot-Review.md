# Independent Tower team pilots: floors 1 and 5

**Later calibration:** the [retained-build follow-up](Retained-Tower-Builds-Calibration-Review.md) now records applied tuning and automatic benchmark reuse. Measurements below remain historical evidence on their original captured content.

11 September 2026. Both fixed pilots of the [independent discovery workflow](Automatic-Tower-Team-Discovery-Plan.md) are **complete and reconstructed**. They found viable generated teams, and both declared floor budgets **fail the 50% balance ceiling**. The offline `LL/tools/BalanceHarness` completed **148,680 combats including four matching detailed replays**, without tuning bosses. Protocols and source audits were frozen before combat; results apply to the declared full Essence pool and hypothetical ownership.

## Frozen scope

| Pilot | Complete party | Fixed progression budget | Historical references | Fresh confirmation samples per party | Maximum combats |
| --- | ---: | --- | ---: | ---: | ---: |
| Floor 1, Garran | 5 independently generated characters | 4 Essences each; level 30, Uncommon Standard tier 1/rank 1 | 40 compatible historical parties plus the exact user party | 1,000 | 53,624 |
| Floor 5, Kharad | 10 independently generated characters | 5 Essences each; level 40, Uncommon Standard tier 1/rank 2 | 90 compatible historical parties | 972 | 99,964 |

Equipment follows the declared five-position pattern across every required slot. Essence choices are generated independently for every character. Both studies use the full **80-Essence pool across 77 source families**, hypothetical ownership, baseline attribute rolls, level-1 untrained Essences and no Combat Styles or contributions. There is no Common-only or acquisition-timing restriction. These gear/level assumptions are explicit experimental budgets; the approved checkpoints do not establish their acquisition timing.

Garran's local Health **1.6764** / offense **1.6368** remains unchanged. Both pilots use one frozen content/settings snapshot and the same verified executable. The user party is a floor-1 reference only; no reference recipe or result enters candidate generation, parent selection or generated-finalist selection.

Discovery retains the default two methods, three generation seeds per method, 128 candidates per arm and eight paired battle seeds: at most **6,144 combats**. Selection reserves 16 candidates × 64 fresh seeds. At most five generated finalists are frozen before confirmation. Diagnostics reserve 256 combats and replay verification reserves 200; unused reservations are not reallocated.

The original 1,000-sample floor-5 proposal would reserve 102,624 combats with all 90 references. Before combat, confirmation was reduced uniformly to **972**, the largest count fitting the unchanged 100,000-combat cap. Generation and selection allocations remain unchanged; compatible weak and high-clear references are both retained. The family size and uncertainty adjustment are fixed before confirmation. No extra samples, reselection, reference removal or tuning follows from observed results within these pilots.

## Historical inputs and implementation correction

The seed audit retained **85,996 conservative exclusions**. It combines the dashboard's 85,974 exclusions with an unbounded-depth metadata audit across 3,588 pruned directories and all live fixture seed fields, hashing 1,582 sources. Generation/master integers are conservatively excluded too. The audit includes the original user benchmark, offense-only and linked tuning, earlier campaigns and completed/cancelled implementation studies. Floor 5 also excludes floor 1's entire reserved schedule. Historical selection is independent of which references are included.

The reference audit examines eight final generalist/specialist archives: the first party search, five-slot progression, four-/five-slot whole-party search, four-slot Kodoku and five-slot Eydis pilot studies, five-slot Eydis refinement and four-slot Serevin expansion. Every compatible confirmation party is retained. It records 352 compatible historical observations, deduplicated by complete prepared identities/recipes, and 352 observations from incompatible equipment/level budgets. Historical context aliases remain in the audit; they are not independent samples. Source report, trial-ledger and recipe hashes match their archive inventories. This declared reference portfolio is not an exhaustive audit of every historical workflow or every legal build.

Preflight exposed a 64-reference contract limit. `TowerBossDiscoveryContract.cs` now accepts up to **96 references**, retaining the 100,000-combat safeguard and existing generation/selection policies. A regression test checks the 90-reference/972-sample case, the 96-reference boundary, rejection at 97 references and over-budget rejection. All **296 relevant backend tests passed** through `build/run-tests.ps1` after a successful Release build. The first build encountered the running dashboard's Windows assembly lock; stopping that local server and rebuilding resolved it, and the dashboard was restarted on port 5619. No verification remains blocked.

The original definitions/protocol are preserved. A second pre-combat execution record changes only the producing executable hash after this correction; budgets, references and all schedules remain identical. Both revised definitions pass the CLI preparation command against the frozen snapshot. The floor-5 definition exceeds the dashboard's 2 MB import limit, so these pilots use the CLI; their saved reports remain available in Tower Lab.

## Floor-1 results

The pilot completed **49,170 combats** within its 53,624 reservation: 6,144 discovery, 1,024 selection, 42,000 confirmation and two matching detailed replays. All stages reconstructed from the retained archive without new combat. No diagnostic fights were used; unused allocations remain unused.

The independently generated primary confirmed at **859/1,000 wins (85.9%)**, with zero draws. Its pointwise 95% Wilson interval is **83.61–87.92%**; the interval adjusted for the 42-cell frozen family is **81.96–89.09%**. Generated viability passes, while both the frozen family and overall floor balance fail the 50% ceiling.

This primary includes the **Rare Treant Guardian Essence**; its other selected Essence definitions are Common. It satisfies the declared pool and slot/gear budget, but this pilot does not establish that a player entering floor 1 can obtain that exact collection. Define practical Essence access before applying these findings to an acquisition-constrained entry cohort. Do not silently reinterpret this full-pool result as a Common-only test.

The exact user party confirmed at **325/1,000 (32.5%)** on the same fresh paired schedule. The generated primary gained 579 wins and lost 45 relative to it: **+53.4 percentage points**, with an approximate paired interval of **48.16–58.11 points**. All 40 other historical references remained below 50%; their best was 8/1,000. Their historical high-clear observations used earlier content and are not pooled with these fresh measurements.

[The complete generated build and exact JSON export](../TestResults/balance/tower-independent-pilots-20260911/published/floor-1-generated-builds.md) preserve all five characters, ordered Essences, identities and gear. The generated team contains no Illusion Fox and was not descended from the user reference. Its selection score was 57/64; it remained primary throughout confirmation. Only one finalist met the predefined capability/behavior diversity requirements. Other strong shortlisted teams remain visible as earlier findings, not newly confirmed alternatives.

All 384 random candidates recorded zero discovery wins. The constructive/joint method found 77 candidates with at least one win among its 384 evaluations. These measurements share the fixed discovery schedule and three generation restarts; they support this pilot's result, not a universal method-superiority claim.

Saved combat statistics describe a distributed recovery team. Character 4 recorded the most healing, averaging 3,056 per fight, while character 3 recorded the most damage, averaging 1,827. The primary's victories averaged 222.94 engine seconds, compared with 149.87 for the user party's victories. These are conditional observations, not proof of a causal interaction or a numerical duration target. No substitution experiment was run; full per-character observations are retained separately.

## Floor-5 results

All five generated finalists confirmed at **972/972 wins each (100%)**, with zero draws. Their pointwise 95% Wilson lower bound is **99.61%**; the lower bound adjusted for the full 95-cell family is **98.78%**. All 90 historical references also exceeded 50%, ranging from **947/972 (97.43%)** to **972/972 (100%)**. Generated viability passes, and both the frozen family and overall floor balance fail.

The pilot measured **99,510 combats** within the 99,964 reservation: 6,144 discovery, 1,024 selection, 92,340 confirmation and two matching detailed replays. Every confirmation cell received its full 972 samples. There were no confirmation draws, missing samples or diagnostic fights; 454 reserved combats remain unused.

All 16 shortlisted teams won 64/64 selection battles. The five frozen outputs satisfy the declared capability/behavior diversity rules; confirmation did not select a replacement primary. During discovery, 380 of 384 random candidates and 375 of 384 constructive/joint candidates recorded at least one win. The saturated target provides little evidence that either method is superior. The primary/alternatives' mean victory durations were **62.46, 63.07, 71.40, 79.25 and 89.97 engine seconds**, respectively; duration remains descriptive.

[All five exact ten-character builds](../TestResults/balance/tower-independent-pilots-20260911/published/floor-5-generated-builds.md) include complete ordered Essences, identities, equipment and original confirmation schedules. Each party uses 50 Essence selections, including five to seven Rare selections. The same practical-availability limitation as floor 1 applies. These results do not establish that five slots are necessary, that a Common-only cohort performs similarly, or that the rest of the Tower is balanced.

## Evidence and remaining work

Evidence is retained under [`TestResults/balance/tower-independent-pilots-20260911/`](../TestResults/balance/tower-independent-pilots-20260911/): seed/reference audits, original and revised protocols, exact definitions, preflight records, frozen content, source snapshot, build/test evidence and per-floor study/reconstruction logs. Study archives retain complete recipes, trials, producing executables, confirmation families and replay audits.

Both CLI reconstructions passed without new combat. Tower Lab independently reconstructed both complete reports and retains replay compatibility. First full dashboard reads took approximately 105 seconds for floor 1 and 447 seconds for floor 5; subsequent views reuse the immutable report cache while checking saved-file integrity. These read times are an operational cost of the large evidence sets, not additional battles.

Relevant commands from the repository root:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~BalanceHarnessGoal'
dotnet TestResults/balance/tower-independent-pilots-20260911/floor-1-run/executable/BalanceHarness.dll tower-boss-study-verify --run TestResults/balance/tower-independent-pilots-20260911/floor-1-run
dotnet TestResults/balance/tower-independent-pilots-20260911/floor-5-run/executable/BalanceHarness.dll tower-boss-study-verify --run TestResults/balance/tower-independent-pilots-20260911/floor-5-run
```

The only harness behavior change in this increment is the 64→96 reference-cap correction in `TowerBossDiscoveryContract.cs`, with its boundary/cost regression in `BalanceHarnessTowerBossDiscoveryContractTests.cs`. The remaining deliverables are audited pilot definitions, evidence, six exact generated recipes, readable build reports and updated planning/README status. This work preserved older sealed campaigns, the user fixture and boss scaling.

**Concurrent workspace change:** after freezing the pilots, other ongoing work added `signet` to the live `items/items.json` catalog (156→157 entries). No existing item definition was modified or removed. Both pilots and their reconstructions use the identical retained pre-addition snapshot; the boss catalog, user fixture and harness source still match the captured versions. The item addition is preserved and recorded in `concurrent-items-change.json`; it was not part of this pilot and is not silently incorporated into its content fingerprint.

Desktop and mobile browser checks passed for both full pilot reports, their generated/reference views and exact primary exports, with no JavaScript errors or page-wide horizontal overflow. All six generated JSON exports match the frozen confirmation recipes. An independent numerical check reproduces every pointwise and adjusted Wilson interval. The final documentation audit covers **16 Markdown files and 317 local links**. No required verification remains blocked.

Assess generated viability separately from each floor's [10–50% balance policy](Tower-Balance-Acceptance-Policy.md). A viable reference cannot make independent search successful; a generated winner above 50% remains useful discovery evidence and a balance breach. No pooled average may hide a strong team, and a bounded negative search cannot prove that no viable build exists.

Next define practical Essence availability and the remaining intended level/gear budgets. Use the retained strong teams as controls for a separately frozen linked Health/Power calibration, with fresh confirmation and any declared lower-budget comparisons. The wider floor-1–11 progression audit still requires separate coverage. This increment introduces no boss tuning, new dependency, migration, production configuration change, shared-database mutation or deployment.
