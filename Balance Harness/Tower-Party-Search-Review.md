# Tower character robustness and joint parties — 10 September 2026

The new offline `tower-party-search` command implements the next bounded step after the [ally-transfer study](Tower-Loadout-Reliability-Review.md): select character loadouts using both ally contexts, then measure how independently searched characters work together. The previous results, accepted starter evidence and unrelated production changes remain separate.

Evidence is retained at `TestResults/balance/tower-party-search-20260910/`, including the producing executable, source snapshots, test logs, complete compressed combats and `run/party-search.md/json`. The contract is `tower-party-search-v1`; it does not reinterpret earlier pilot schemas or change their rankings.

## Frozen scope and selection

- **All 15 floors**, using production RequiredSlots. Four equipped Essences per participant, level 30, Uncommon Standard tier-1 rank-1 baseline gear, level-1 unascended/unevolved Essences, no styles, hypothetical ownership.
- Search first-cell absolute slots **1, 2, 3 and 4** independently: Guardian, Restorer and two Strikers. Roles are labels, not Essence restrictions. Controller and later cells remain fixed. The two Strikers can choose different loadouts.
- Character candidates run with original balanced allies and fixed candidate-05 allies, excluding the target. Equipment and runtime identity remain fixed. Every candidate receives both contexts before ranking.
- Rank by **minimum floor-1 win-count gain against each context's own control**, then minimum all-floor gain, lower mean guardian health, higher survival and stable ordered-loadout ID. Strong allies reaching a ceiling cannot cancel an entry regression in the other context. The existing `LoadoutFitness.EntryWins/Wins` fields encode those minimum gains in this separately versioned study. Per-floor clears and diagnostics remain available; no personal damage/healing score or Wilson bound enters ranking.
- Both methods receive the same control; the Restorer also receives historical A/B/C in order before exploration. Guided search uses its existing beam, random restarts and single/double/order mutations. The comparator uses uniform ordered distinct-definition sampling with family rejection. No claim that guided search is already better motivated this extension.
- Two generation seeds, **8563 and 9677**, with twelve candidates per method/character, two contexts and three discovery samples per floor. Each arm pays **1,080 actual combats**; all sixteen arms cost **17,280**.
- Each character shortlist retains its control, up to two structurally distinct generalists and a remaining positive-gain floor/context specialist when available. Specialist selection is exploratory; all-floor tradeoffs stay visible.
- Screen at most **24 joint parties**, including the control, historical Restorer C, the combined character winners from each method/seed, single-character generalist substitutions and deterministically shuffled combinations from the finite shortlist product. Combination seed **10871**. Each screened party gets two contexts, all fifteen floors and four samples: at most **2,880** combats. This combines complementary changes across multiple characters; it does not enumerate the catalog or all possible parties.
- Freeze up to **eight parties**, retaining the control and method-derived parties before filling generalist/specialist places. Confirm each at forty fresh samples per floor/context, at most **9,600** combats. Confirmation does not reselect candidates, drop failures or extend sampling after viewing results.

The predeclared upper bound is **29,760** combats; the hard cap is **30,000**. Deduplication or a smaller finite shortlist product may reduce party-stage counts. Character arms must reach equal candidate/combat budgets or the run is incomplete. Caching is isolated by arm/party/stage; a successful experiment requires zero cache subsidies.

Actual generated schedules from masters **202609105** (character discovery), **202609106** (joint screening) and **202609107** (confirmation) are disjoint and checked against **873 declared historical seeds**. Both contexts and method replications share paired combat schedules. This checks the recorded prior pilot/reliability ledger, not every seed ever used by any harness or test.

Joint parties replace all first-cell slots 1–4 in both contexts. Therefore their **floor-1 parties are identical** across contexts; larger-floor contexts differ only in later fixed cells. They cannot be pooled into an independent eighty-trial estimate. Likewise, multiple search seeds sharing the same combat schedule do not increase the clear-rate sample count. Method-derived joint parties are a useful descriptive comparison, but their interacting choices do not isolate the superiority of a party-search algorithm.

## Measured results

**29,760 battles completed:** 17,280 character discovery, 2,880 joint screening and 9,600 confirmation. All four character shortlists retained four alternatives. All 24 joint candidates were screened and all eight frozen finalists completed both contexts on every floor. There were no cache subsidies or omitted floors.

Every searched finalist won **40/40 on floor 1**, versus **26/40** for the control. Forty out of forty has a nominal pointwise Wilson 95% interval of approximately 91.2–100%; the duplicate joint floor-1 contexts do not double that sample size. All-floor results below are totals over the declared 15-floor matrix, each out of 600 per context, not a universal clear probability.

| Frozen party | ID prefix | Floor 1 / 40 | Original allies: all / 600 | Alternative later cells: all / 600 |
| --- | --- | ---: | ---: | ---: |
| Control | `450182e03961` | 26 | 46 | 46 |
| Guided character winners, seed 8563 | `973c04f32ae3` | 40 | 125 | 131 |
| Random character winners, seed 8563 | `2cb96583cc8c` | 40 | 134 | 142 |
| Guided character winners, seed 9677 | `b56615f81dae` | 40 | 153 | 185 |
| Random character winners, seed 9677 | `6fcdb5325570` | 40 | **195** | **199** |
| First discovery generalist combination | `1da0e3e694dd` | 40 | 173 | 181 |
| Second generalist combination | `384b238ad47b` | 40 | 169 | 191 |
| Retained specialist | `01f7dde76c42` | 40 | 151 | 163 |

The first discovery generalist did not retain the largest all-floor total on fresh seeds. The party assembled from seed-9677 random character winners was strongest on the observed all-floor totals in both contexts. Random-derived parties also exceeded guided-derived parties in both seed comparisons and both contexts, but two correlated method replications do not establish statistical algorithm superiority. Retain the random benchmark and frozen alternatives.

The strongest observed party uses these **ordered** loadouts; equipment/attributes still follow each participant's original role recipe:

| First-cell slot | Ordered Essences |
| --- | --- |
| 1 — Guardian | Poisonous Rat / Nightshade Blossom / Alpha Wolf / Elder Treant Thornstorm |
| 2 — Restorer | Nightshade Blossom / Spider Queen Royal Venom / Venomous Spiderling / Cinder Beetle (historical C) |
| 3 — Striker | Poisonous Rat / Nightshade Blossom / Alpha Wolf / Elder Treant Thornstorm |
| 4 — Striker | Poisonous Rat / Nightshade Blossom / Alpha Wolf / Elder Treant Thornstorm |

The two Strikers were searched separately and were allowed to differ; this particular confirmed party selected the same vector for both. Other finalists retain asymmetric choices. No post-hoc role restrictions or forced difference were applied. Full definition IDs, controls and every finalist's recipes are in `run/selection.json` and `run/party-search.md/json`.

For that party with original allies, floor wins were **40/40 at floors 1, 2, 3 and 6; 34/40 at floor 5; 1/40 at floor 11; and 0/40 at every other floor**. Alternative later cells changed floor 5 to 39/40 and floor 11 to 0/40. Neither context cleared floor 10 at this four-slot budget. These results strengthen the entry-build evidence; they do not complete the later progression targets or establish that the chosen Essences are optimal. The first generalist combination recorded one draw in each context, retained alongside defeats and victories.

## Implementation and verification

`TowerPartySelection.cs` owns the explicit bounded contract, minimum-gain scoring, diverse/specialist retention, deterministic finite combination proposal, frozen final selection and human-readable report. `TowerPartySearch.cs` runs the three stages through `TowerLoadoutArchive` and the existing production Tower adapter. It saves each shortlist before using the next stage's schedule. `LoadoutSearch.cs` adds an opt-in shared-start mode; its default behavior and historical serialization remain unchanged. `Program.cs` exposes `tower-party-search` and `tower-party-verify`; existing `tower-loadout-replay` supports the new archive.

The semantic verifier executes the same deterministic scheduling/selection workflow using saved battle results, checks every trial's stage, recipe, seed, input hash and arm cache identity, reconstructs proposals and frozen selections, and compares JSON/Markdown reports. It also rematerializes saved contexts from the frozen catalog/content. This establishes reconstruction consistency. Independent persisted normal-Tower parity and detailed production replays provide separate combat checks; rerunning the selection code alone is not independent gameplay parity.

`BalanceHarnessTowerPartySearchTests.cs` covers the strong-ally ceiling/regression case, specialist retention, equal historical starts and random exploration, budget/seed rejection, asymmetric Strikers, fixed identities/equipment, complete three-stage reconstruction, resealed false-score rejection, cancellation and replay. Three new cases in `BalanceHarnessTowerTests.cs` persist asymmetric joint parties and compare the normal database-backed Tower preparation/playback/outcome path on floors 1, 10 and 15.

**125 Tower tests passed** through `build/run-tests.ps1 -Configuration TowerPartyFinal -Filter 'FullyQualifiedName~BalanceHarnessTower'`. After improving only Markdown interval formatting, all six new party-search tests passed again through the same script. A subsequent review fixed an edge case where a method party deduplicating against historical C could lose mandatory confirmation eligibility: retention now uses recipe identity rather than its first source label. All **seven** party-search tests passed under `TowerPartyVerified`, including the new regression. The full builds had 33 existing warnings; the incremental report-only build had none. No shared production combat/preparation code changed in this increment, so the complete backend suite was not repeated.

The measurement's original `TowerPartyFinal` executable and source remain in `executable/` and `source/`; the corrected implementation is retained in `verification-executable/` and `verified-source/`. Detailed replays use the original executable. The corrected verifier reconstructed **all 29,760 trials, proposals, shortlists, final selection and JSON/Markdown results exactly**. The deduplication edge case did not alter this experiment's frozen selection.

**Fourteen detailed replays matched**, including character/party/confirmation stages, both joint contexts, later floors, victory, defeat and a recorded draw. Exporting the first non-control finalist's floor-1 recipe through normal `tower` execution reproduced all **forty results without changes after JSON decoding**; its exported bundle also replayed. All **17,280 character-search references** point to distinct actual trials, with zero overlap against the 873 excluded historical seeds. The complete run contains **34,878 files / 633,750,974 bytes** (approximately 634 MB); this is an artifact-size observation, not a controlled performance claim.

The previous reliability package's **38,072 checksum-listed files remain unchanged**. Producing/verification executables, exact source versions, reports, exported smoke bundle, verification scripts/logs and documentation snapshots are retained separately under the evidence root and sealed with `checksums.json`. No required verification remains blocked. Historical starter evidence and unrelated working-tree changes were not edited.

## Remaining limits

This is the first scoped C slice, not completion of the broader B–E plan. The command is CLI-only; the dashboard's Find loadouts integration remains future work. Four-slot results at later floors are coverage evidence, not validation of the six-slot floor-10 progression goal. Existing 4–10-slot fixtures remain available, but this search does not yet optimize counts 5–10, Controller, later-cell members, other gear cohorts or party composition.

The candidate/search budget is small relative to the catalog. Individual shortlists may miss complementary builds that are weak in both fixed character contexts but strong together. Method winner parties can include a role winner outside its retained shortlist. Fresh confirmation evaluates joint parties; it does not separately confirm every character alternative in both original character-search contexts. Joint success therefore cannot establish that each chosen character is independently strong. Specialist gains are selected from many exploratory cells; intervals remain nominal and no multiplicity-adjusted superiority claim is made. Later work should revisit characters in improved fixed ally contexts with a new seed ledger before making broader recommendations.

Owned inventory, training/acquisition cost, Combat Styles, multiworker scheduling, resume, unseen-boss generalization and equal-cost comparison against the old authored search remain open. Every stored recipe can be exported through the normal `tower --scenario` command with matching content/settings. Reproducing this study with its existing seeds is replay/reproduction, not fresh confirmation. A new search after balance changes must record new final seeds and distinguish retesting saved builds from finding replacements.

There are no migrations, production configuration/tuning changes, deployments or accepted baseline changes. Phase 2 integration remains deferred.
