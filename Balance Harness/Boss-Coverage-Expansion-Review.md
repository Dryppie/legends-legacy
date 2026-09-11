# Serevin and Serath coverage expansion

11 September 2026. This offline Balance Harness increment adds the fixed Nhalia validation to Tower Lab and expands direct boss screening to Serevin (floor 11) and Serath (floor 15). It follows the [boss-specific plan](Boss-Specific-Essence-Loadout-Plan.md), the [refinement study](Boss-Specific-Essence-Loadout-Refinement-Review.md), and the [fixed Nhalia validation](Nhalia-Fresh-Validation-Review.md).

## Progression correction after sealing

The user clarified that **floor 10 should require approximately five to six Essences per character and floor 11 should require at least seven**. The prior loadout plan already recorded floor 10 at around six. This campaign failed to carry that progression goal into its budget-selection rule.

Four was selected as a search experiment: every five-through-ten-slot cell had a 5/5 best control, while the four-slot cell had no clears. The rule sought room for search improvement rather than enforcing the intended progression budget. Its four-slot Serevin result is therefore **below-target diagnostic evidence**, not a recommended floor-11 budget or evidence that the desired progression is satisfied. The five-/six-slot screening wins and the four-slot specialist's later 10/20 result flag a possible difficulty mismatch under these modeled party/progression assumptions.

Future Serevin progression evaluation must use seven slots as its main cohort and retain lower-budget controls to measure that mismatch. A clear-rate acceptance criterion and fixed gear/level assumptions must be declared before balancing; restricting search to seven slots alone would hide the issue. This clarification updates the live documentation only. The sealed protocol, recipes, observations and original publication snapshot remain unchanged, preserving what was actually measured.

## Fixed validation integration

Tower Lab now shows the three Nhalia recipes' separate 100-seed target observations: the historical anchor's 0/100, previous discovery primary's 5/100 and previously exploratory recipe's 8/100. The view includes every paired outcome, exact complete 100-seed recipe export, nominal Wilson intervals, gained/lost comparisons and separately qualified recovery measures. These are the existing sealed observations, not new combats. The 130 distinct historical recipes, their discovery anchors and their earlier 20-/40-sample all-floor matrices remain unchanged.

The new portable fixture imports 486 source files verified against the fixed validation's sealed checksums. It retains the exact ten-character, seven-Essence parties and 18,687 excluded integers: the captured conservative prior union plus the 100 validation seeds. Its SHA-256 is `c4cdda4166b23b4c14e6ee9acbfde1200dc0b5453b07bd91e02aba12608c5971`. An explicit Git `eol=lf` attribute preserves the hash across Windows checkouts.

Every new schema-2 boss plan, including bosses without historical reference entries, includes those exclusions and declares the validation fixture's exact hash. Execution verifies both the source and copied fixture before any combat; reconstruction verifies the declared frozen copy again. Older archives omit this optional field and retain their original semantics. The validation view has its own API and never fabricates transfer observations from the target-only test.

## Predeclared expansion protocol

The new campaign package is [`TestResults/balance/tower-boss-expansion-20260911`](../TestResults/balance/tower-boss-expansion-20260911/). Its independent [protocol review](../TestResults/balance/tower-boss-expansion-20260911/protocol-review/protocol-review.md) checks the exact historical sources, full party projections, counts, selection rules and accounting before outcomes.

The finite control cohort retains every latest whole-party finalist projected through both actual saved ally contexts, the authored control, and ten explicitly reviewed historical recipe entries at matching Essence budgets. The ten entries cover the five published pilot recipes, three published refinement recipes, Nhalia's fixed historical anchor and the known `126553…` floor-11 counter. That counter is already represented by generalist projections and adds no unique recipe. Missing participants use their actual recorded destination-floor allies; no first-group repetition substitutes for them. Fixed identity, equipment, training and other non-Essence fields match the destination exactly.

| Essences per character | Serevin controls | Serevin study cap | Serath controls | Serath study cap |
| --- | ---: | ---: | ---: | ---: |
| 4 | 21 | 14,316 | 26 | 14,536 |
| 5 | 22 | 14,704 | 22 | 13,144 |
| 6 | 23 | 15,092 | 23 | 13,492 |
| 7 | 22 | 14,704 | 25 | 14,188 |
| 8 | 17 | 12,764 | 17 | 11,404 |
| 9 | 21 | 14,316 | 21 | 12,796 |
| 10 | 17 | 12,764 | 17 | 11,404 |

Screening uses five paired seeds per distinct control at each of the 14 boss/budget cells: **1,470 fights**. For each boss, prefer a budget whose best control clears 1–4/5, choosing closest to six slots and then lower slots. If none qualify, choose a zero-clear budget with the lowest best-control guardian health, then distance to six and lower slots. Skip a boss if every budget's best control clears 5/5. Reconstruct the entire probe matrix before applying this rule; never resample to obtain a preferred cell.

The selected budget's anchor is fixed by probe wins descending, guardian health ascending, survival descending and stable party identity. Every method then evaluates all retained controls plus eight new complete parties, across three generation seeds and four paired discovery seeds. All controls, method/restart winners and strategy allocations freeze before 20 fresh confirmation seeds on every released floor and effective ally context. The unchanged progression presets, complete required parties, ordered legal Essences, content and full combat fidelity are preserved.

The existing graph arm falls back to joint search when no supported mechanic replacement starts exist. Both arms retain their declared cost, and matching proposals/outcomes must be verified; these duplicated comparisons provide no independent graph-method evidence. Serevin and Serath have no supported replacement diagnostic in this version. Each study reserves four nominal diagnostic fights, executes zero of them and cannot spend that reserve elsewhere. The campaign measures complete-party performance without claiming a validated dispel/stagger or health-distribution mechanism.

The maximum allocation is **31,198 combats**: 1,470 probes, at most 29,628 for one selected study per boss, and a separately reserved 100-fight verification allowance. Verification uses each selected study's discovery primary and probe anchor, deduplicated within the study. Their complete 20-seed target exports cost at most 80 ordinary Tower fights. The first scheduled trial and first observed Victory/Defeat/Draw per recipe, deduplicated, cost at most 12 detailed replays. The prescribed verification maximum is therefore 92; unused allowance cannot extend the experiment.

Content, executable, minimal combat settings, all potential schedules, exact controls, historical exclusion sources and code are frozen before probes. Each job reserves its full maximum before execution. Failure/cancellation preserves outputs and the full conservative charge, with no retry, resume or adaptive second study. Replays repeat existing observations and add no independent samples. Seed freshness covers available local recorded sources; it is not an account-wide guarantee.

Final calibration and an independent Python reconstruction matched all 294 control entries and all 14 study caps. The 4,340 possible combat seeds are mutually distinct and disjoint from 74,753 conservative prior integers gathered from 1,657 local source files. The calibration's exclusion vector includes those prior integers plus the 70 probe seeds; each candidate definition also excludes every other possible cell's discovery, confirmation and nominal diagnostic schedule. The separate portable validation fixture is a subset of this broader campaign history.

Initialization regenerated the entire reviewed draft exactly before freezing it. The final draft hash is `358e4a3bb30b5abab8e6b81d64917ec703bc6c27d94762da7ea634095f63659b`; its execution/source receipt hash is `fd914cd1de8ebf09868380fe45971f6dea5ea3a7c0f21024cc962fbbc93e52fb`. The frozen source snapshot contains 1,901 files plus its manifest, alongside 31 executable files and both compiled campaign/verification drivers. The verifier's reservation-order checks passed 13 synthetic cases and its first-observed-outcome selection checked all 729 six-trial outcome vectors before any campaign fight.

## Completed screening and frozen comparison

All **1,470/1,470** screening fights completed with zero cache hits. Native reconstruction reproduced every recipe, input hash, trial, outcome row and control identity before applying the frozen selection rules.

| Essences | Serevin best control clears | Serath best control clears |
| --- | ---: | ---: |
| 4 | 0/5 | 0/5 |
| 5 | 5/5 | 0/5 |
| 6 | 5/5 | 0/5 |
| 7 | 5/5 | 0/5 |
| 8 | 5/5 | 5/5 |
| 9 | 5/5 | 3/5 |
| 10 | 5/5 | 5/5 |

Serevin selects **four Essences per character**, the only zero-clear cell with no mixed cell available. Its frozen probe anchor is `d77927fe9757ae80ec5b9fc4f745761a1b09de52a90e4b3383284159b72acf49`, leaving mean guardian health 32.152% in screening. Serath selects **nine Essences per character**, the only mixed cell. Its anchor is `87cc6205ce50e6ccb511d8dd843df925686ef7891595f850a7b630a7ec9746b5`, clearing 3/5 with mean guardian health 8.892%.

Each study retains 21 controls. The complete target parties have ten participants for Serevin and fifteen for Serath. Serevin confirms 17 canonical floor/context cells per finalist; Serath confirms 15. Their nominal study caps are 14,316 and 12,796, reducing the selected campaign maximum to **28,682** including probes and verification. The unused pre-screening allowance cannot fund another study.

These five-sample screens select an informative experiment; they do not establish a minimum viable budget. Different budgets retain different complete parties and seed schedules, so Serath's 5/5 at eight slots and 3/5 at nine do not imply that adding an Essence causes worse performance.

## Serevin: useful four-slot progress with substantial transfer losses

The four-slot comparison completed **14,312/14,316** fights: 1,392 discovery and 12,920 confirmation, with zero cache hits and zero diagnostic fights. All 38 frozen finalists received 20 fresh samples on each of 17 canonical floor/context cells. The unused four-fight diagnostic reserve remains unspent.

The discovery-frozen primary `1c18bf10a62ca6feea953e0d5d9e1de2599dba50450f18ed6bea71daaf355b7a` cleared **10/20** fresh Serevin trials. The probe-frozen anchor cleared **1/20**, also the strongest observed target result among the 21 retained controls. The primary gained nine winning seeds and lost none against the anchor; one seed was a shared win. Its mean remaining guardian health was 10.7185%, versus the anchor's 31.898%. Another new finalist, `186f47fb02b71fb648f7a9b539165d7e7a32da37a05f0fe0779da9a9c7b3c74e`, also cleared 10/20; it does not replace the discovery-frozen primary.

This is meaningful observed progress at four Essences per character, with ten target losses still present. The primary also cleared **0/20 on floor 8**, where retained control `b4b8971c6c8f794ec2fa5c61981c39398777d8935dc2d5e70a784ac993641eef` cleared **20/20**. Shared context aliases are the same observations, not additional samples. Keep this as a boss-specific experimental recipe, not a reliable all-floor build or an established optimum.

Its nominal pointwise Wilson 95% interval is 29.9–70.1%; the anchor's is 0.9–23.6%. These descriptive intervals have no multiplicity correction. Restarts, repeated winners and duplicated graph/joint runs do not increase the 20-sample confirmation size.

The archived native `boss-search.md` uses a legacy generic “authored controls” heading when a custom cohort has no portable `ReferenceSetId`. That heading is inaccurate for these projected historical controls. The frozen definitions, `control-provenance.json`, current dashboard and new analysis/review retain the correct cohort. The original Markdown bytes are preserved because native archive reconstruction checks that exact rendering; changing it requires a compatible report-format increment.

## Serath: the nine-slot primary did not improve the fresh clear rate

The nine-slot comparison completed **12,792/12,796** fights: 1,392 discovery and 11,400 confirmation. All 38 frozen finalists received 20 fresh samples on all 15 canonical floors. There were zero cache hits and zero diagnostic fights.

The discovery-frozen primary `e72a8f4a8c4a24ae2f7e9b1298572cbe7f5c25dcfd2fbfc5045024bfccc85ca5` is itself a retained generalist control. Every one of the twelve method/restart arms selected that same discovery winner. It and probe anchor `87cc6205ce50e6ccb511d8dd843df925686ef7891595f850a7b630a7ec9746b5` each cleared **7/20** fresh target trials. They shared three wins; the primary gained four seeds and lost four. Its mean remaining guardian health was 7.3015%, versus the anchor's 5.404%. The strongest observed retained control cleared **9/20**. The comparison did not establish a new-method improvement.

The strongest observed finalist, `b689313702d3…`, cleared **12/20**, an exploratory result that does not replace the discovery-selected primary. The primary had no canonical transfer cell with fewer wins than a retained control in this sample, but that does not rescue its weak target result or establish unseen-seed reliability. Its nominal Wilson interval is 18.1–56.7%; the exploratory maximum's is 38.7–78.1%, without multiplicity adjustment. Exact recipes preserve all fifteen participants and nine ordered Essences per character.

## Completed accounting, analysis and exact exports

| Stage | Actual fights | Reserved maximum |
| --- | ---: | ---: |
| Complete budget screening | 1,470 | 1,470 |
| Serevin four-slot study | 14,312 | 14,316 |
| Serath nine-slot study | 12,792 | 12,796 |
| Ordinary Tower exports and detailed replays | 88 | 100 |
| **Selected campaign total** | **28,662** | **28,682** |

The two studies executed 2,784 discovery and 24,320 confirmation fights. The 20 unused reserved fights comprise eight unsupported diagnostic fights and twelve unused verification fights; none funded additional work. The original pre-screening maximum was 31,198. There were no failed, cancelled, resumed or repeated campaign jobs.

Native verification reconstructed both studies and preserved all 76 frozen finalist target recipes. The separate [analysis](../TestResults/balance/tower-boss-expansion-20260911/analysis/summary.md) stores **1,216 canonical confirmation cells** and **25,536 finalist-versus-control paired comparisons**, including every target and transfer cell. A read-only rerun reproduced all analysis outputs byte-for-byte. Every graph/joint generation pair had identical proposals, ordered parties and measurements; both executions remain charged. Serevin's successful primary was selected by legacy, joint and graph at the third generation seed, so it cannot establish superiority of one method. Serath's twelve arms all retained the same existing-control winner.

The four discovery-primary/probe-anchor exports each reproduced all 20 saved target trials through ordinary Tower: **80 exact input/report matches**. One observed victory and one defeat per recipe produced **eight matching detailed replays**, normalizing only the added event log. No draw occurred in these four target vectors, and no example was fabricated. A final read-only verification checked all twelve completed jobs and their output hashes without more fights.

[Four published recipe files](Boss-Expansion-Recipes-20260911/README.md) retain their original complete parties, ordering, progression and seed vectors. Their Git attribute preserves the exact exported bytes. All 76 finalists remain in the [complete campaign library](../TestResults/balance/tower-boss-expansion-20260911/recipe-library/index.json), separate from the unchanged 130-entry historical portable catalog. Future fresh studies must additionally exclude this campaign's used and reserved seed schedules; its unused combat allowance cannot be reused.

The package's [seal receipt](../TestResults/balance/tower-boss-expansion-20260911/seal.json) identifies its final checksum manifest. Its audit covers all new archive bytes and inventories, producing code/content, original source seals and relevant imported/seed-source hashes, exact verification outputs, regenerated analysis and publication snapshots. Unrelated historical battle files are not rehashed. The captured publication includes the version of this review at sealing and the four recipe exports; later live-documentation corrections do not alter that snapshot. Checksum verification needs no original workspace.

## Implementation verification

The final broad repository suite passed **261 tests, zero failures or skips**:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower|FullyQualifiedName~CompactCombatTelemetryTests'
```

The first run passed 260 tests and found a missing session-token setup in one new HTTP test. Adding the normal authorized test session corrected that test; the full rerun passed. Builds had zero errors and five existing warnings in unrelated tests. A final harness build includes a wording-only dashboard resource correction for custom declared controls.

Headless Edge/Chromium checked all three validation recipes, all 100 outcomes per recipe, exact exports, paired +8/−5 results, unchanged historical counts/matrices, automatic seed exclusions for floors 11 and 15, and desktop/mobile layouts at 1,440/390 pixels. There were no JavaScript errors or document overflow. Screenshots were visually inspected and the local server was stopped. `node --check` and scoped `git diff --check` passed. The fixed-validation import reproduced byte-for-byte.

The final executable reconstructed the original schema-1 pilot Eydis archive (12,784 recorded combats) and schema-2 refinement Nhalia archive (50,488 recorded combats), preserving their saved manifests and report semantics without new fights. Its SHA-256 is `88473bf865d6b4776b8b5cdf9ef943eab30d34067cdd40697c08a54dc79b3cf5`. The final harness-only build had zero warnings and errors; the broad test build's five unrelated warnings are recorded above.

The completed Serevin and Serath dashboard reports also passed real-browser checks: all 38 alternatives per study, 21 controls, 17 new recipes, the frozen anchor, all 15 floors, qualified recovery, unsupported diagnostics, explicit graph/joint fallback, replay compatibility and four exact 20-seed exports. There were no JavaScript errors or failed HTTP responses. Desktop/mobile views at 1,440/390 pixels had no document overflow; wide tables retain their own horizontal scroll. Representative screenshots were visually inspected. First report reads took 36.823 and 34.606 seconds after the source-hash check, descriptive timings rather than a controlled benchmark. The server stopped afterward, and all 28,435 protected source/ledger files retained their hashes. These checks executed zero combats.

No required verification command remains blocked. Initial NuGet access and the new test's session setup were resolved before the final successful builds and test run. The final Markdown updates use the completed receipts; no combat or backend rebuild is needed for those documentation edits.

| Changed files | Purpose |
| --- | --- |
| `LL/tools/BalanceHarness/TowerBossValidationReferences.cs`, `Fixtures/tower-boss-validation-references.json`, `Scripts/import-boss-validation-references.py` | Separate sealed fixed-validation observations, source checks, paired summaries and exclusions |
| `TowerBossSearchContract.cs`, `TowerBossSearch.cs` | Optional declared validation hash, exclusions on every new boss plan, frozen preflight/reconstruction checks |
| `TowerDashboardBossValidations.cs`, `TowerDashboardServer.cs`, `Dashboard/index.html`, `Dashboard/dashboard.js`, `Dashboard/dashboard.css` | Dedicated fixed-validation view/API/export and explicit graph fallback wording |
| Validation reference/integration tests and `BalanceHarnessTowerDashboardBossTests.cs` | Source tampering, historical compatibility, seed exclusion and complete UI API verification |
| `.gitattributes` | LF checkout rule for the exact-hash validation fixture and exact-byte preservation for the four published recipe JSON exports |
| Campaign orchestration, protocol/analysis/verification helpers and this review | Frozen, bounded measurement and auditable outputs |

The work adds no migration, production configuration change, database action or deployment requirement. The Git line-ending attribute is the only repository configuration addition. Production combat/content and unrelated working-tree changes are preserved.

## Inspect the preserved campaign

From the repository root, open the local dashboard against the campaign's captured executable, catalogs and content:

```powershell
dotnet 'TestResults/balance/tower-boss-expansion-20260911/executable/BalanceHarness.dll' tower-dashboard --runs-root 'TestResults/balance/tower-boss-expansion-20260911/studies' --catalogs-root 'TestResults/balance/tower-boss-expansion-20260911/catalogs' --content-root 'TestResults/balance/tower-boss-expansion-20260911/frozen-root' --port 5203
```

Saved report reads and recipe downloads execute no fights. The campaign's completed verification can be checked without replaying its fights:

```powershell
dotnet 'TestResults/balance/tower-boss-expansion-20260911/verification-driver/bin/Release/net10.0/ExpansionAudit.dll' verify 'TestResults/balance/tower-boss-expansion-20260911'
python 'TestResults/balance/tower-boss-expansion-20260911/analysis/analyze.py' --verify-existing
python 'TestResults/balance/tower-boss-expansion-20260911/audit-seal.py' --verify
```

Use the available Python executable for the last two commands; this session used the bundled Python runtime. Native reconstruction requires the captured .NET runtime/platform identity. The final package checksum verification is portable and does not require the original workspace. Do not rerun the campaign or parity execution modes: their existing reservations and outputs intentionally forbid retries and resumption.
