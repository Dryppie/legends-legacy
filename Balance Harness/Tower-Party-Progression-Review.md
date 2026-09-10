# Tower loadout search at 5–10 slots — 10 September 2026

This increment extends the [four-slot joint-party search](Tower-Party-Search-Review.md) to explicit 4–10-slot budgets, records six separate progression studies, and connects whole-loadout search to Tower Lab's **Find loadouts** controls. The six-slot study runs first because it addresses the user's floor-10 goal. All studies retain all 15 floors and production RequiredSlots; no Tower acceptance band is introduced.

Producing executable, source snapshots, test logs, full compressed runs and verification artifacts are retained at `TestResults/balance/tower-party-progression-20260910/`. The preceding four-slot evidence is unchanged; its 35,121 checksum-listed files were checked individually.

## Predeclared budgets and experiment contract

| Slots | Level | Uncommon equipment: quality / tier / rank | Priority floor | Sampling | Maximum combats |
| --- | ---: | --- | ---: | --- | ---: |
| 6 | 50 | Standard / 2 / 2 | 10 | Thorough | 15,840 |
| 5 | 40 | Standard / 1 / 2 | 1 | Coverage | 5,040 |
| 7 | 60 | Fine / 2 / 3 | 10 | Coverage | 5,040 |
| 8 | 70 | Fine / 2 / 3 | 10 | Coverage | 5,040 |
| 9 | 80 | Fine / 2 / 4 | 10 | Coverage | 5,040 |
| 10 | 90 | Fine / 2 / 4 | 10 | Coverage | 5,040 |

The **41,040-combat batch cap** is fixed before execution. Each row is a separate gear/level budget from the existing progression curve. Gear remains provisional, especially for the six-slot anchor; the studies do not isolate the effect of adding a slot. Four-slot search remains available in the dashboard at level 30, Uncommon Standard tier-1 rank-1 gear. Ordinary benchmarks still support the other entry gear cohorts and the fixed-level controlled-slot catalog.

Every participant has the cohort's declared Essence count, with level-1 unascended/unevolved Essences, baseline equipment rolls, hypothetical ownership and no Combat Styles. Search changes only absolute first-cell slots 1–4: Guardian, Restorer and two separately searched Strikers. Controller and later-cell members stay authored. Each candidate runs with original and fixed candidate-05 allies, excluding the target. Joint contexts exclude all four changed targets and therefore differ only in later cells; five-character floors are identical across those contexts. Repeats cannot be pooled as independent samples.

The schema-2 definition freezes its complete budget, exact ordered starting candidates and extended reference party before combat. `tower-party-starts.json` preserves the historical four-slot shortlists and observed random-9677 party. Extensions append legal distinct families from the cohort's control, then the sorted pool if needed. This is a proposal mechanism, not a claim that an optimal higher-slot build includes the four-slot build. Every arm reserves room for fresh exploration; both methods receive the same control and historical starts and pay the same actual combat budget. Guided mutations can replace any position.

Coverage sampling uses one generation seed, eight candidates per method/character, one character sample per floor/context, twelve joint parties with two samples, and four frozen finalists with twenty confirmation samples. Thorough sampling uses two generation seeds, two character samples per floor/context, sixteen joint parties and six finalists with forty confirmation samples. All roles use matched discovery schedules. One generation seed in coverage studies is insufficient to infer algorithm superiority.

Rank by the minimum priority-floor win-count gain against each context's own control, then minimum all-floor gain, lower guardian health, higher survival and stable ID. The priority floor is 10 for the six-slot study and later cohorts; all 15 floors remain in discovery and confirmation. The historical `fitness.entryWins` field means priority-floor gain in schema 2. Wilson 95% is descriptive uncertainty, never the selection objective. Specialists remain exploratory, and confirmation never reselects or drops unsuccessful finalists.

Each cohort derives three distinct combat masters and generation/combination seeds from its fixed master `202609110 + slotCount`. Actual schedules are checked against **1,578 historical exclusions** plus all preceding cohorts' reserved schedules. `batch-plan.json` saves every definition before the first fight. This is a declared local experiment ledger, not a registry of all account-wide or concurrently running tests. Repeating the same batch is reproduction, not fresh confirmation.

## Completed progression measurements

The batch completed its full **41,040 combats**: 17,280 character discovery, 4,560 joint-party screening and 19,200 frozen-party confirmation. It screened 76 joint candidates and confirmed 26 parties across six separate budgets. All 15 floors are present for every stage and context.

| Slots | All-floor trials per context | Control wins: original / alternative | Searched wins: original | Searched wins: alternative | Floors with no finalist wins in either context |
| --- | ---: | --- | --- | --- | --- |
| 5 | 300 | 134 / 138 | 156–173 | 162–175 | 7, 10, 12, 13, 14, 15 |
| 6 | 600 | 400 / 400 | 402–412 | 403–423 | 12, 13, 14, 15 |
| 7 | 300 | 209 / 215 | 216–220 | 220 | 12, 13, 14, 15 |
| 8 | 300 | 213 / 221 | 218–267 | 226–283 | None |
| 9 | 300 | 222 / 258 | 238–270 | 261–277 | None |
| 10 | 300 | 263 / 292 | 286–298 | 297–300 | None |

Ranges include every frozen searched finalist; they do not reselect a winner using confirmation. All-floor totals describe the fixed encounter matrix, not the success rate of a persistent climb. “None” means each floor had at least one win among finalists, not that all parties cleared every floor. Per-party/per-floor outcomes, exact recipes, Wilson and paired intervals remain in each `party-search.md/json` and the aggregate `results-summary.json`.

Five-slot finalists all won 20/20 on floor 1 and 0/20 on floor 10. Every six-/seven-/eight-/nine-/ten-slot finalist cleared floor 10 in every confirmation trial. These observations also change level, gear and entire loadouts. They do not prove that a slot unlock alone causes a difficulty transition or that five slots cannot clear floor 10 under another budget.

At eight slots, random-method finalist `19af2f270039` recorded 267/300 original-context wins and 283/300 alternative-context wins, including 2/20 and 4/20 on floor 15. At ten slots, random-method finalist `bf5e3631705d` recorded **298/300 and 300/300**, with **18/20 and 20/20 on floor 15**, versus control's 4/20 and 15/20. Its original-context losses were both on floor 15. Coverage studies use only one generation seed and twenty confirmation samples per cell; these observations establish neither method superiority nor optimality. Two nine-slot and one ten-slot confirmation draws remain draws in totals and reports.

## Six-slot anchor result

All six frozen parties, including the unchanged control, won **40/40 on floor 10 in each context**. Each individual cell's Wilson 95% interval is 91.2–100%; the two contexts cannot be pooled. The control cleared floors 1–10 in every confirmation trial and none of floors 11–15, giving 400/600 wins per context.

The strongest observed six-slot all-floor finalist, `10121f7f9ffd` (`method-guided-42121407`), retained those floor-1–10 clears and added **12/40 floor-11 wins with original allies and 23/40 with alternative later-cell allies**: 412/600 and 423/600 overall. The other searched parties also retained all floor-10 clears. No six-slot finalist cleared floors 12–15. These are results for frozen finalists; confirmation did not reselect the party or expand the candidate pool.

That party's first-cell Guardian and both Strikers use Alpha Wolf / Thornback Boar / Grave Wisp / Crystal Wisp / Spider Queen Royal Venom / Skeleton, in that order. Its Restorer uses Blue Slime / Spider Queen Royal Venom / Nightshade Blossom / Cinder Beetle / Venomous Spiderling / Forest Spirit. Exact definition IDs, unchanged teammates and gear are retained in the exported recipes and full reports. Role labels are search/reporting hints, not class restrictions.

The floor-10 objective is saturated under this budget, including the authored control. This supports feasibility at six slots with the provisional level/gear assumptions, but does not establish that these Essences are necessary, optimal, available to a real account or reliably sufficient at weaker gear. Floor-11 gains are exploratory secondary findings. A smaller numerical win-rate target or a narrower equipment budget would need its own predeclared study.

## Implementation and verification

`TowerPartyProgression.cs` adds explicit budgets, legal deterministic extensions, versioned contexts, seed reservations and the six-cohort batch command. `TowerPartySelection.cs` and `TowerPartySearch.cs` accept schema 2 while preserving absent-field serialization and old schema-1 behavior. Legacy study verification keeps its original objective and Markdown; new runs report their priority floor and exact budget.

`TowerDashboardLoadouts.cs`, the existing dashboard service/server and HTML/JavaScript add cost preview, fresh suggested seeds, job start/cancel, search-run discovery, verified report projection, exact finalists, recipe download and replay. The UI keeps loadout-search budgets separate from benchmark selections. The unchanged benchmark/earlier authored-search APIs remain available. Local session/origin protections and path validation also apply to the new endpoints. The dashboard excludes discoverable saved loadout schedules when preparing a new search; it does not claim a complete registry of external concurrent jobs.

Search results are semantically reconstructed before being presented as verified. The table contains confirmation cells; total trial count also includes character/party discovery. The replay menu offers the first saved seed per finalist/context/floor, while JSON contains every trial ID. Exports validate the saved recipe against its recorded input hash. Complete historical archives can be viewed with current code; replay requires the producing executable/runtime. Cancellation preserves partial trials and marks the report incomplete. There is no resume or multiworker scheduling.

**144 Tower tests passed** through `build/run-tests.ps1 -Configuration TowerProgressionFinal -Filter 'FullyQualifiedName~BalanceHarnessTower'`. They include all seven slot budgets on all floors, fixed identities and legal extensions, seed/cost rejection, floor-10 scoring, complete six-/ten-slot archive reconstruction and detailed replay, the new HTTP preview/start/cancel/export boundary, and six independent persisted normal-Tower parity cases covering slots 6 and 10 on floors 1/10/15. The initial test-only inaccessible-settings-helper error was corrected; the final build had five existing test warnings and no errors. Shared production combat/preparation code did not change, so the complete backend suite was not repeated.

The dashboard JavaScript passed Node's syntax check. An isolated Chrome headless browser with a temporary profile verified every 4–10-slot option and cost preview, cancelled a partial search, then completed a separate **5,040-battle four-slot search** through the rendered controls. It checked verified saved results, all four exact frozen recipes, recipe export, JSON report download and a matching detailed replay; no page errors occurred. The complete browser archive is also copied into this increment's evidence package. Windows computer-use inspection had timed out waiting for app approval; the isolated browser completed the required UI coverage. The initial browser test expected US number formatting despite the Danish locale; the final test fixes its locale explicitly. No required verification remains blocked by those environment issues.

The current reader also reconstructed all **29,760 fights** in the preceding schema-1 joint-party archive without differences. Replay uses each study's producing executable; the new package retains its exact `TowerProgressionFinal` build. The original dashboard on port 5093 was left running, and the updated dashboard uses port 5094.

All **41,040 archived trials** passed full semantic reconstruction of candidates, scores, shortlists, joint screening, frozen selection and confirmation. **44 detailed replays matched**, covering character discovery, party screening, controls, floors 1/10/15, nine-/ten-slot draws and reimported exports. **140 exported floor-10 fights** (forty for six slots; twenty for each other cohort) reproduced exactly after JSON decoding through the ordinary `tower` command, with zero changed results. Each exported bundle also replayed successfully. The browser replay is additional to those 44.

The aggregate audit found **2,730 distinct reserved combat seeds** across the six cohorts and browser study, including 345 for the latter, with zero cross-study/stage overlaps. Each cohort also passed its historical-exclusion check. All six scopes and the browser scope have the same content/settings/executable fingerprint, so concurrent content drift did not affect these comparisons. Joint contexts and method restarts intentionally reuse within-study paired schedules and are not additional independent samples.

A second rendered-browser check loaded the six-slot CLI archive and displayed **180 confirmation cells, six exact frozen parties and 180 replay choices**, without visible or page errors. JavaScript syntax and scoped Git whitespace checks passed. No required verification command remains blocked. The six study archives occupy **1,079,440,346 bytes** across 52,948 files, before adding retained binaries, replay logs, exports and browser evidence. Runtime/size observations are not controlled performance benchmarks.

`verify-cohort.ps1` and `summarize.ps1` in the retained package reproduce the audit steps. The latter requires complete outputs and checks stage/cohort/browser seed separation and shared scope identity. `checksums.json` seals the evidence and documentation snapshots; `seal-policy.json` excludes the live dashboard log, seal process log and the manifest itself. Saved reports are immutable inputs, and no prior evidence was replaced.

## Remaining limits

The search budget remains small relative to the catalog, and quality is conditional on the declared party, equipment and encounter mix. Strong joint results do not independently confirm each character in every ally context. Higher-slot studies add more variables through level/gear and must not be pooled into an isolated slot-count claim. Ownership/acquisition/training constraints, Combat Styles, Controller/later-cell optimization, evolving ally contexts, composition search, unseen-encounter generalization and rigorous multiplicity-adjusted superiority claims remain open.

The next bounded search increment proposed by this progression study was to include the Controller and measure whether extending the searched roles into later cells improves whole-party results at these same fixed budgets. That increment is now completed in the [whole-party review](Tower-Whole-Party-Review.md), preserving the current parties as controls, reserving fresh confirmation seeds and increasing search-seed replication. The limitations above describe this earlier progression study. The [current next step](Essence-Loadout-Search-Plan.md#next-implementation-joint-refinement-of-retained-parties) is deeper joint refinement of retained four-/five-slot parties. Floor-10 feasibility is already supported at the provisional six-slot budget; these findings do not call for automatic boss tuning. A controlled weaker-gear study remains a separate way to test how much of that feasibility comes from the assumed equipment.

No production bosses, gear, Essences or rules were tuned. Accepted starter policies/evidence and unrelated working-tree changes were preserved. There are no migrations, shared-database writes or deployments. Phase 2 integration remains deferred.
