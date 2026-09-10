# Four-Essence Tower character-search pilot — 10 September 2026

The offline `tower-loadout-search` command now generates and evaluates whole character loadouts, performs bounded adaptive mutations, compares against equal-cost random search and confirms a frozen shortlist on unused combat seeds. The recorded pilot completed **8,760 battles**: **5,760 discovery + 3,000 confirmation**, with **108 passing Tower tests**, **10 matching detailed pilot replays** and a successful exported-recipe round trip. This is the first scoped character-search pilot, not completion of the broader B–E plan or a certified best-build search.

Evidence, the producing executable, source snapshots and reports are retained at `TestResults/balance/tower-loadout-pilot-20260910/`. The main machine-readable and Markdown reports are `run/pilot.json` and `run/pilot.md`. All 3,192 checksum-listed files from the earlier foundation evidence remain unchanged. Accepted starter and previous Tower evidence were not overwritten.

## Scope and selection policy

The target is party slot 2, the original Restorer position, in the first five-character cell. Roles are labels, not enforced skill requirements: a successful candidate may replace healing with damage or other mechanics. All other participants stay fixed during character search; larger floors retain their original allies in later cells. Production RequiredSlots still determines whether the party has 5, 10 or 15 characters.

Every participant uses four level-1, unascended, unevolved Essences, level 30, Uncommon tier-1 equipment and baseline rolls. The four **Standard/Fine × rank 1/2** gear cohorts are separate experiments. All 15 floors are evaluated, while this is explicitly an **entry-budget** search: later-floor four-slot outcomes do not test the user's six-slot floor-10 progression expectation. Ownership of the declared 80-definition/77-family pool is hypothetical; acquisition is not modeled.

Discovery ranks candidates by **floor-1 victories first**, then total all-floor victories, lower mean remaining guardian health, higher mean party survival and stable candidate ID. This predeclared entry objective differs from the earlier progression-curve generalist search. Wilson 95% intervals describe confirmation uncertainty; they are not the ranking function. No starter 50–90% target, automatic acceptance or boss tuning applies.

## Search and cost

Each gear cohort runs guided and random search at generation seeds **1701 and 2903**. Each arm evaluates **12 distinct ordered candidates × 15 floors × 2 paired combat samples = 360 actual battles**. All arms include the same control. Search-seed restarts share the discovery combat schedule; they must not be pooled as additional independent combat samples.

Guided search uses a direct-mechanic whole-loadout hypothesis, random starts and restarts, a beam retaining up to three structurally different candidates, and single-Essence, paired-Essence and order mutations. Random search samples ordered distinct-definition tuples uniformly, rejecting duplicate source families. Mechanic metadata proposes a start; it does not score fitness or exclude unknown mechanics. The existing ordered-loadout policy remains unchanged.

Every proposal records its origin, parent, rejection/duplicate status and ordered IDs. This run evaluated **192 arm-specific candidates**, with three duplicate proposals skipped. Shared candidates across arms are deliberately re-executed so both search methods pay the same actual combat cost. Exact cache keys include the arm, algorithm/content/execution/settings and complete materialized combat input. The pilot used **zero cache hits**; cache correctness and invalidation are tested separately. Exhausting the proposal cap before completing an arm marks the run incomplete instead of presenting an unequal-cost comparison.

The global shortlist was saved before any confirmation combat. Each cohort retained the unchanged control, the historical candidate-05 party substitutions, and each arm winner. Duplicates merged, leaving **five finalists per cohort**, each receiving **15 floors × 10 unused-seed trials = 150 fights**. The predeclared upper bound was 9,360 battles; merging finalists reduced actual cost to 8,760. The hard cap was 10,000. Confirmation did not replace or reselect finalists.

Candidate 05 is a separate party comparator: Guardian Hobgoblin → Horned Wolf and both Strikers' Pixie → Dire Wolf, repeated across cells. It changes allies and never competes in the fixed-allies character ranking. Its substitutions were remeasured at the pilot's entry budgets with pinned identities; these are not a rerun of the old progression-curve party at its original budgets. All comparisons in this review use newly captured current content and new paired schedules; earlier reported win rates are not directly comparable.

## Results

Two loadouts shared across cohorts were:

- **A, `be46b0ccba61…`:** Glade Panther, Plague Ghoul, Nightshade Blossom, Illusion Fox, in that slot order. Found by random search at seed 1701.
- **B, `a639fc770419…`:** Feral Ghoul, Ravenous Ghoul, Thornback Boar, Green Slime, in that slot order. Found by both methods at seed 2903; retained once per cohort.

Floor-1 confirmation victories, each out of ten:

| Gear cohort | Original control | Loadout A | Loadout B | Guided seed-1701 finalist | Previous-05 party |
| --- | ---: | ---: | ---: | ---: | ---: |
| Standard rank 1 | 8 | 10 | 10 | 6 | 10 |
| Standard rank 2 | 9 | 10 | 10 | 10 | 10 |
| Fine rank 1 | 10 | 10 | 10 | 10 | 10 |
| Fine rank 2 | 10 | 10 | 10 | 9 | 10 |

Total victories across the same 15-floor confirmation matrix, each out of 150:

| Gear cohort | Original control | Loadout A | Loadout B | Guided seed-1701 finalist | Previous-05 party |
| --- | ---: | ---: | ---: | ---: | ---: |
| Standard rank 1 | 13 | 29 | 24 | 13 | 30 |
| Standard rank 2 | 27 | 30 | 30 | 30 | 32 |
| Fine rank 1 | 32 | 42 | 44 | 30 | 42 |
| Fine rank 2 | 37 | 52 | 54 | 41 | 48 |

These totals summarize a heterogeneous encounter matrix, not a universal character score or pooled confidence estimate. Every individual floor, draw count, paired gain/loss and interval is retained in `run/pilot.md/json`, including floors with zero clears and any regressions. Loadout A at Standard rank 1 gained 17 wins and lost one across the matrix; B gained 13 and lost two. More total wins do not imply that every floor improved.

The guided seed-1701 finalist was Grave Hound / Lumo Wisp / Alpha Wolf / Poisonous Rat at Standard rank 1 and Fine rank 2, the original Restorer with Lumo Wisp replaced by Illusion Fox at Standard rank 2, and Goblin Archer / Thornback Boar / Wandering Ghost / Plague Ghoul at Fine rank 1. The weak confirmation results remain reported. This illustrates why discovery's two samples per floor cannot establish build quality alone.

Guided search won no more discovery combats than random search in any of the eight cohort/search-seed comparisons; random search led in two, and six tied on wins. Both methods cleared floor 1 twice with their discovery winners. This pilot does **not** establish a search-efficiency advantage for the guided method. Keep the random baseline and investigate candidate generation/selection with more independent search seeds before adding algorithm complexity.

Ten confirmation trials per floor are exploratory. A 10/10 result has a pointwise Wilson 95% interval of approximately **72–100%**, and the paired difference from an 8/10 control remains uncertain. No loadout is promoted as statistically superior or globally optimal, and no confirmation evidence should be reused as an untouched test set after it informs another search.

## Implementation and verification

- `LoadoutSearch.cs`: bounded ordered candidate search, diversity, mutations, uniform random comparator and deterministic score ordering.
- `TowerLoadoutPilot.cs`: strict pilot contract, four fixed gear cohorts, all-floor contexts, paired seed schedules, identity-preserving changes, frozen selection, confirmation and Markdown/JSON reporting.
- `TowerLoadoutArchive.cs`: shared frozen content, exact input cache, per-fight archives, integrity verification and detailed replay using the production runner.
- `Program.cs`: `tower-loadout-search`, `tower-loadout-verify` and `tower-loadout-replay` CLI commands. Existing commands and dashboard behavior are preserved.
- `BalanceHarnessTowerLoadoutSearchTests.cs` and additions to `BalanceHarnessTowerTests.cs`: a completely enumerated small legal space with a two-Essence fitness valley, mutation/diversity behavior, determinism, cancellation/exhausted budgets, fixed identities/allies, cache separation, archive integrity, held-out schedules, and independent persisted normal-Tower parity on floors 1/10/15.

Verification commands:

```powershell
./build/run-tests.ps1 -Configuration TowerLoadoutPilotFinal -Filter 'FullyQualifiedName~BalanceHarnessTower'
dotnet TestResults/balance/tower-loadout-pilot-20260910/executable/BalanceHarness.dll tower-loadout-search --output TestResults/balance/tower-loadout-pilot-20260910/run --content-root LL/src/API/API.LL
dotnet TestResults/balance/tower-loadout-pilot-20260910/executable/BalanceHarness.dll tower-loadout-verify --run TestResults/balance/tower-loadout-pilot-20260910/run
dotnet TestResults/balance/tower-loadout-pilot-20260910/executable/BalanceHarness.dll tower-loadout-replay --run TestResults/balance/tower-loadout-pilot-20260910/run --battle trial-006211 --detailed
```

**All 108 Tower tests passed.** The earlier 105-test run also passed before the added normal-path parity cases. The first sandboxed build could not read the local NuGet configuration; the authorized test-script retry succeeded. No command remains blocked. Existing unrelated build warnings remain; shared production code was not changed, so this increment did not repeat the full backend suite.

`tower-loadout-verify` verified all **8,760** trial files and reconstructed discovery fitness and the frozen shortlist. Ten detailed pilot replays matched, covering all four gear cohorts plus victory and defeat outcomes and floor 15. The selected Standard rank-1 loadout-A recipe also ran through the existing `tower` command using the captured content/settings: all **ten result files were byte-for-byte identical** to the pilot. Its detailed normal Tower-bundle replay also matched, giving **11 detailed replays** in total. Those ten round-trip fights repeat existing seeds and are verification, not new independent quality samples. The first export smoke used an incomplete hand-built settings file; adding the required idle-cadence setting in the disposable export content fixed it. The failed smoke is retained separately and does not enter the pilot report.

Approximately 245 seconds elapsed between writing the captured scope and the completed report. The run archive contains 11,219 files totaling approximately **2.24 GB** (decimal); retaining full battle summaries is a material storage cost before expanding to more roles/cohorts. This is a local runtime/storage observation, not a controlled performance benchmark. Archives preserve the producing executable, input hashes, content, settings, completed trials and proposal logs. Cancellation marks partial work incomplete; it does not support resume. Exported recipes regenerate against current content through the existing Tower command; historical replay requires the retained executable/runtime/platform.

## Remaining work

The next investigation should increase discovery precision and compare candidate-generation strategies against random search across more independent search seeds. Use new confirmation seeds after any revised search. Then expand to other characters, at least two fixed ally contexts, all 4–10-slot cohorts and joint party choices. A dedicated floor-specialist archive, matched-cost comparison against the earlier 16-party authored search, owned/trained budgets, configurable scheduling/workers, resume and the dashboard **Find loadouts** workflow remain open. The current dashboard still runs the earlier authored search; this pilot is available through the CLI.

The harness, tests and documentation changed. No production combat rules, boss values, migrations, application configuration or deployments changed. Phase 2 integration remains deferred, and unrelated working-tree changes were preserved.
