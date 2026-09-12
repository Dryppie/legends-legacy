# Balance Harness: idle balance workflow and Tower benchmarks

**Competitive search update — 12 September 2026:** [stronger Kharad searches](../../../Balance%20Harness/Tower-Competitive-Build-Search-Review.md) confirmed new teams at **948/1,000** and **1,000/1,000**, breaching the previous balance result. The tools now support explicit improvement of saved teams, larger history imports, paired search-quality comparisons and complete-portfolio calibration. See the review for the latest measurements and remaining competitive-search limits.

Verified strong discoveries also ship in the [versioned retained catalog](Fixtures/tower-retained-builds.json), with [readable complete recipes](../../../Balance%20Harness/Competitive-Kharad-Recipes-20260912/README.md). Fresh checkouts can use them as compatible controls; future studies still measure them on fresh seeds. Historical results never become current fitness or automatic balance acceptance.

**Progression audit and calibration — 11 September 2026:** [floors 2–5 review](../../../Balance%20Harness/Tower-Progression-Floors-2-to-5-Review.md) records independent four-Essence searches on floors 2–4, their separately confirmed linked Health/Power calibration against every known breach, and Kharad's fresh **20.9%** strongest-control check. Compatible controls and calibrated top builds persist for future searches. Floors 6–11 and practical acquisition coverage remain open.

The earlier progression batch retained **4 / 50 / 52 / 50 / 8 compatible controls for floors 1–5** and excluded **97,838 seeds**. These are dated snapshots. The [competitive search review](../../../Balance%20Harness/Tower-Competitive-Build-Search-Review.md) records the subsequent builds and evidence. Cumulative history now supports **1,000,000 exclusions**, with **32 MiB imports**. Independent generation starts from scratch; explicitly labeled `improve-supplied` studies can refine compatible saved teams. Floors 6–11 and practical acquisition coverage remain open.

**Fresh search and Kharad follow-up — 11 September 2026:** [new independent searches](../../../Balance%20Harness/Post-Calibration-Tower-Team-Search-Review.md) confirmed Garran's strongest saved team at **34.8%** and found a stronger Kharad team at **59%**, triggering a separate calibration. The [expanded-portfolio follow-up](../../../Balance%20Harness/Kharad-Expanded-Portfolio-Calibration-Review.md) applied another **8% to both Kharad Health and Power**; its strongest of 122 parties confirmed at **25.25%**, and the full family passes. At that stage, main-dashboard searches retained **4 floor-1 / 6 floor-5 controls**. Broader progression, practical Essence access and further independent ceiling searches remain open.

**First retained-build calibration — 11 September 2026:** compatible saved builds now enter new Tower Lab searches automatically as fresh benchmark controls; completed future studies retain their generated finalists. The [retained-build calibration](../../../Balance%20Harness/Retained-Tower-Builds-Calibration-Review.md) applied linked Health/Power factors of **1.06 to Garran** and **1.56 to Kharad** relative to their pre-campaign inputs. The strongest of 57/106 retained parties confirmed at **34% / 24.25%**, respectively, and both frozen families pass the 10–50% policy. This uses the declared full Essence pool including Rare Essences; this historical result is followed by the fresh searches and expanded calibration above.

**Historical independent-team pilots — 11 September 2026:** floor 1’s generated primary won **859/1,000 (85.9%)**, while the supplied reference won **325/1,000 (32.5%)**. Five floor-5 generated finalists each won **972/972**. Both tested cohorts fail the 50% ceiling. The studies use the full 80-Essence pool with hypothetical ownership, including Rare Essences; acquisition-constrained budgets remain to be defined. See the [pilot review and exact builds](../../../Balance%20Harness/Automatic-Tower-Team-Pilot-Review.md). All 296 relevant backend tests passed; saved studies reconstruct and expose separate search/balance findings. No bosses were retuned.

**Independent Tower Lab teams — 11 September 2026:** the dashboard now defaults to **Discover teams from scratch**, with floor/gear/pool/ownership controls, explicit references, seed-history import and an exact review-before-run workflow. Results distinguish generated viability, the frozen family, overall balance and evidence integrity, with cancel/export/replay support. See the [Tower Lab review](../../../Balance%20Harness/Automatic-Tower-Team-Lab-Review.md) for commands, verification and limits. The fixed pilots, fresh independent searches and expanded Kharad calibration are complete; broader progression coverage and further independent ceiling searches remain next.

Legacy boss-specific search is available through **Historical reference refinement** and documented in [its implementation guide](../../../Balance%20Harness/Boss-Specific-Essence-Loadout-Implementation.md). For the default independent workflow, use the [automatic discovery guide](../../../Balance%20Harness/Automatic-Tower-Team-Discovery-Implementation.md). Historical generalist and boss-search reports retain their original semantics.

**Independent team discovery — 11 September 2026:** `tower-boss-study --definition <schema-3-json> --output <new-directory> [--content-root <API.LL-directory>]` now runs independent generation, selection validation, frozen fresh confirmation, paired reference comparisons and scoped 10–50% acceptance. `tower-boss-study-verify --run <directory>` reconstructs all stages from saved trials without new combat. The archive retains producing assemblies/dependencies, content, recipes and detailed replay audits. The [confirmation review](../../../Balance%20Harness/Automatic-Tower-Team-Confirmation-Review.md) documents commands, the frozen selection policy, outlier handling, actual costs and verification. Tower Lab integration is implemented; the fixed pilots are complete; legacy boss-search behavior is preserved.

`tower-boss-discovery-prepare --definition <schema-3-json> --output <new-directory> [--content-root <API.LL-directory>]` still provides preparation and full-plan cost without combat. `tower-boss-discover` with the same options executes discovery only (at most 6,144 default discovery fights per context); `tower-boss-discovery-verify --run <directory>` reconstructs those discovery-only archives. `tower-balance-evaluate --definition <frozen-confirmation-json> --sources <cell-run-map-json> --output <new-directory>` separately evaluates verified normal-Tower confirmation archives. Discovery-only completion does not invoke acceptance. See the [contract and evaluator guide](../../../Balance%20Harness/Automatic-Tower-Team-Discovery-Implementation.md).

**Earlier user-party floor-1 tuning, 11 September 2026:** Garran was reset to original Health **1.27** / offense **1.24**, then both were scaled by **1.32**. That experiment used Health **1.6764** / offense **1.6368**; the later retained-build calibration supersedes those inputs. The unchanged user party confirmed at **291/1,000 wins (29.1%; pointwise 95% interval 26.37–31.99%)**, meeting the 10–50% band for that party/budget. The [linked-tuning review](../../../Balance%20Harness/Tower-Floor-1-Linked-Tuning-Review.md) records the bounded search, comparison with two HP-heavy alternatives, 1,000 exact local-content parity matches and 70 unchanged paired reports across floors 2–15. The earlier [offense-only 394/1,000 result](../../../Balance%20Harness/Tower-Floor-1-Tuning-Review.md) and original 100% benchmark remain historical. That calibration changed only two floor-1 catalog fields and deployed nothing. Independent generation was implemented afterward; a reusable tuning control and broader build-ceiling validation remain open.

**User-authored floor-1 test, 11 September 2026:** the [exact five-character scenario](Fixtures/tower-floor-1-user-party.json) preserves the user's twenty ordered Essence selections with the existing level-30, four-slot Uncommon Standard tier-1/rank-1 entry equipment assumptions. It won **1,000/1,000** fixed-seed battles, with a 99.62–100% pointwise 95% interval: an upper-bound breach of the Tower target. The [benchmark review](../../../Balance%20Harness/Tower-Floor-1-User-Party-Review.md) records all equipment, results, 106 passing tests, matching detailed replay and reproduction commands. This standalone fixture is supplied explicitly through `tower --scenario`; it is not installed as a default party for other floors. Automatic team generation remains the preferred broader workflow.

**Tower balance target approved 11 September 2026:** at each floor's intended progression budget, no evaluated legal party may exceed **50%** wins and at least one must reach **10%**. A 10/10 result breaches the observed ceiling; weak builds do not all need 10%, and averaging must not hide stronger builds. See the [Tower acceptance policy](../../../Balance%20Harness/Tower-Balance-Acceptance-Policy.md) for uncertainty, budget scope and the implemented standalone evaluator. Independent staged studies apply acceptance after fresh confirmation in the CLI and Tower Lab. `Complete`, archive verification and retained rankings remain separate from a balance pass. Historical statements below about an undecided Tower band describe their original studies and are superseded by this decision; the separate starter 50–90% policies remain unchanged.

The [controlled boss pilot](../../../Balance%20Harness/Boss-Specific-Essence-Loadout-Pilot-Review.md) screened all 4–10-slot budgets for four bosses, then confirmed 96 frozen parties across all 15 floors in three selected studies. Kodoku's strongest new four-slot specialist won 20/20 versus the strongest retained control's 10/20, with substantial negative transfer. Eydis produced one rare-clear five-slot alternative at 2/20; all seven-slot Nhalia finalists remained at 0/20. Morrowmaw's screening was saturated and its comparison was skipped. [Five complete reviewed recipes](../../../Balance%20Harness/Boss-Pilot-Recipes-20260910/README.md) preserve several observed tradeoffs; these findings do not establish optimality or method superiority.

The subsequent [boss refinement](../../../Balance%20Harness/Boss-Specific-Essence-Loadout-Refinement-Review.md) adds all 96 pilot finalists as portable Tower Lab references, anchored local proposals, boss-specific replacement diagnostics and separate recovery observations. Eydis's discovery-frozen five-slot primary cleared 40/40 fresh trials versus its anchor's 2/40, with substantial floor-11 losses. Nhalia's seven-slot primary cleared 1/40; an exploratory 6/40 finalist remains a hypothesis for fresh testing. [Three exact recipes and their all-floor results](../../../Balance%20Harness/Boss-Refinement-Recipes-20260910/README.md) accompany the completed 103,777-fight campaign, including 105 replay/export checks. Historical samples remain separate, and schema-1 archives retain their original semantics.

The [fixed Nhalia validation](../../../Balance%20Harness/Nhalia-Fresh-Validation-Review.md) tested three unchanged recipes on 100 unused paired seeds: the historical anchor cleared 0/100, the previous primary 5/100 and the former 6/40 candidate 8/100. These remain unreliable builds; samples are separate from the earlier study. The completed validation used 305/312 fights including five verified replays. Tower Lab now retains all **130 distinct pilot/refinement recipes**, exposes separate historical observations and exact exports, and reuses completed report reconstruction only after freshly checking every archive file hash.

The [reference and report-loading review](../../../Balance%20Harness/Boss-References-and-Report-Loading-Review.md) records the implementation and verification. The 53,184-fight Eydis report's first read fell from 193 to 117 seconds, with subsequent reads and exports around two seconds; full first-read reconstruction remains required.

The [coverage expansion review](../../../Balance%20Harness/Boss-Coverage-Expansion-Review.md) adds a separate fixed Nhalia validation view with all 100 paired outcomes, exact complete recipe exports and qualified recovery measurements. Every new schema-2 boss plan automatically excludes the fixed-validation seeds as well as the recorded historical union, including plans for bosses without reference entries. The plan declares the portable fixture's exact hash; execution and reconstruction check its frozen copy. Historical matrices remain separate. This increment passed 261 relevant backend tests and desktop/mobile browser checks.

The completed Serevin/Serath expansion used **28,662/28,682 fights**, including 88 exact export/replay checks. Serevin's four-slot discovery primary cleared 10/20 fresh target trials against its anchor's 1/20, but lost all 20 floor-8 trials where a retained control won all 20. Serath's nine-slot primary remained a retained generalist control at 7/20, matching its anchor; the exploratory 12/20 observation does not replace it. [Four exact primary/anchor exports](../../../Balance%20Harness/Boss-Expansion-Recipes-20260911/README.md) accompany 76 frozen finalists, 1,216 canonical confirmation cells and 25,536 paired comparisons against all retained controls. The expansion's newly used seed ledgers must also be excluded from future fresh experiments; these new recipes are in saved reports and the campaign library, separate from the 130-entry historical portable catalog.

The intended progression is **floor 10 at approximately five to six Essences per character, then floor 11 at at least seven**. The completed four-slot Serevin experiment selected search headroom and omitted that progression constraint. Its results are below-target diagnostic evidence, not a recommended floor-11 budget. Future progression evaluation must center on seven slots and retain lower-budget controls to expose difficulty mismatches. The [coverage review's correction](../../../Balance%20Harness/Boss-Coverage-Expansion-Review.md#progression-correction-after-sealing) preserves the original sealed evidence while recording this requirement.

An offline .NET console tool for measuring progression difficulty and explaining the effect of combat code or content changes. It runs real idle and Tower combat through production preparation and execution. The original control suite has 12 cells (1,200 battles); the separate First Hunt cohort has 36 cells (3,600 battles), including two-enemy encounters. It saves replayable battles, produces Markdown/JSON scorecards, compares accepted idle references and evaluates versioned idle goals. The first Tower slice has one fixed floor, one five-character party and 20 declared seeds; its results are descriptive.

The current workflow is:

1. Run the reference suite and review its assumptions, completeness and scorecard.
2. Accept a complete run as a comparison baseline with a written reason.
3. Run a candidate with matching fixtures, sample count and seeds after a code/content change.
4. Compare the runs and evaluate the candidate against its matching goal policy.
5. Inspect failed or inconclusive checks and replay selected battles before deciding on a gameplay or policy change.

The original cohort goals remain draft. The selected Blood Grove starter and the primary level-10 Crystal Creek reward build have reviewed, enforced 50–90% clear-rate policies when explicitly evaluated. Duration does not gate either selected starter. An accepted baseline records a reference state; approving the desired player experience is a separate design decision. See the [development plan](../../../Balance%20Harness/Balance-Harness-Plan-With-Benchmarking.md) and [starter acceptance review](../../../Balance%20Harness/Starter-Path-Acceptance.md).

The dated workflow sections below retain the scope, limitations and proposed next work of their original increments. Use the independent discovery status above for the active workflow and next pilots; sealed evidence and its captured documentation remain unchanged.

## Whole-party loadout search — 10 September 2026

For boss-specific builds, see the [boss mechanics and alternative search analysis](../../../Balance%20Harness/Boss-Specific-Essence-Loadout-Plan.md) and its implementation guide above. It covers every released floor, distinguishes AoE/add clearing from group protection, and compares mechanic-guided proposals, joint search, strategy archives and other approaches. The separate boss objective is implemented; the schema-3 workflow below retains its existing generalist objective.

Tower Lab's **Find loadouts** now defaults to **Whole party · all five roles**. It searches Guardian, Restorer, both Strikers and Controller, then compares first-group-only, repeated-group and alternating-group deployments across all 15 floors. The earlier four-character scope remains available. Select 4–10 Essences per character; each cohort retains its fixed level/gear budget. The preview states the actual search-seed count, confirmation samples and maximum fights.

Schema 3 preserves every finalist from the preceding cohort as a control, uses the same historical starts and actual character-combat budget for guided/random exploration, screens explicit deployment variants and freezes finalists before new-seed confirmation. Repeated groups reuse the same five selected role recipes; alternating groups combine guided/random groups from the same search seed. This evaluates a proposed combination, not assumed synergy. `deployment-comparisons.json` maps every method/seed/policy to its party ID, discovery score, confirmation status and paired gained/lost counts against the matching first-cell variant. Markdown and the dashboard also identify the deployment comparisons.

```powershell
# Six-slot study first, then 4/5/7/8/9/10; predeclared total maximum 72,480 fights.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-whole-party --output TestResults/balance/tower-whole-party-new

# Same verifier, detailed replay and normal-Tower recipe exports as earlier party studies.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-party-verify --run TestResults/balance/tower-whole-party-new/slots-6
```

Six-slot thorough sampling uses three generation seeds per method, eight candidates per character/arm, one character sample per floor/context, 32 joint candidates with one sample, and sixteen finalists with twenty confirmation samples: at most **17,760 fights**. Other batch cohorts use two search seeds, 24 joint candidates and twelve finalists with ten confirmation samples: **9,120 fights each**. Six-slot coverage retains six old controls instead of four, so it uses fourteen finalists and **9,720 fights**. Every retained control is confirmed even if discovery looks weak. The best-discovery deployment family keeps its complete first/repeat/alternating trio for matched confirmation. Other method/seed families retain their best discovery deployment; extras come from the ranking and specialist policy.

The seven budgets are unchanged from the progression table below. Full-party deployments replace both authored contexts identically; their duplicate results must not be pooled as independent samples or claimed as contrasting-ally robustness. Schema 3 gives each context an explicit evaluation identity so the declared actual fight counts stay accurate. Schema-1/2 archives retain their old selection/serialization behavior. The current fixture declares 4,653 historical exclusions; each batch adds all earlier cohorts' reserved schedules before combat. Dashboard exclusions cover discoverable local jobs, not every external concurrent process.

The seven-cohort batch completed **72,480 fights**, screening 176 joint candidates and confirming 88 frozen parties including all 30 retained controls. **161 Tower tests passed** through `build/run-tests.ps1`; every archive reconstructed, **58 detailed replays matched**, and **80 exported normal-Tower fights were identical**. A separate rendered-browser study completed 9,120 fights and passed cancellation/results/download/export/replay checks. All seven batch cohorts are available in the new dashboard on port 5095.

Six-slot deployment improved a matched floor-11 result from 16/20 original-context wins to 20/20. The best new eight-slot finalist reached 9/10 on floor 15. Four-slot search did not beat its retained best; five-slot results were mixed, and seven-/ten-slot best totals tied retained results. Every new ten-slot finalist cleared all floors in 10/10 per context. These are descriptive findings at separate fixed budgets, not automatic promotions or guarantees.

Evidence, the producing executable, source snapshots and final reports are saved in `TestResults/balance/tower-whole-party-20260910/`. Concurrent work changed only the unused Combat Styles catalog between content snapshots; 900 additional normal-Tower comparisons across all saved catalog versions matched exactly. Use a frozen content root for future batches alongside content edits. See the [whole-party review](../../../Balance%20Harness/Tower-Whole-Party-Review.md) for all results and scope details. The search does not independently optimize every later character, change composition, model account ownership/training or certify optimality. Next work should deepen joint refinement from strong retained four-/five-slot parties with more discovery samples and fresh confirmation. No production tuning or automatic Tower target is introduced; Phase 2 remains deferred.

The [next implementation checklist](../../../Balance%20Harness/Essence-Loadout-Search-Plan.md#next-implementation-joint-refinement-of-retained-parties) describes the planned four-/five-slot joint-refinement experiment and its verification requirements. Controller discovery and the three deployment policies are already implemented. The [whole-party review](../../../Balance%20Harness/Tower-Whole-Party-Review.md) records the completed package's 111,331-file seal and manifest checksum. Keep its executable, content and documentation snapshots unchanged; use new output directories for subsequent experiments.

## Find loadouts: 4–10 slots and progression studies — 10 September 2026

Tower Lab exposes **Find loadouts**, with a 4–10-slot selector, fixed progression-budget preview, sampling choice, experiment seed, progress and cancellation. The earlier four-character scope covers all 15 floors, searches the first-cell Guardian/Restorer/two Strikers separately, screens joint parties and confirms frozen finalists on a disjoint seed schedule. Saved results show exact ordered builds, per-floor outcomes and Wilson intervals; JSON/Markdown include the full search and paired comparisons. The representative fight selector supports verified replay and **Export selected fight’s party recipe**. The earlier authored four-substitution search remains available in a disclosure.

| Slots | Level | Uncommon gear: quality / tier / rank | Priority floor |
| --- | ---: | --- | ---: |
| 4 | 30 | Standard / 1 / 1 | 1 |
| 5 | 40 | Standard / 1 / 2 | 1 |
| 6 | 50 | Standard / 2 / 2 | 10 |
| 7 | 60 | Fine / 2 / 3 | 10 |
| 8 | 70 | Fine / 2 / 3 | 10 |
| 9 | 80 | Fine / 2 / 4 | 10 |
| 10 | 90 | Fine / 2 / 4 | 10 |

These are **separate progression cohorts** using the existing curve's provisional gear assumptions. Comparing their outcomes does not isolate the effect of an additional slot, because level and gear also change. All Essences remain level 1, unascended/unevolved, with hypothetical ownership and no Combat Styles. Other quality/rank comparisons and the level-90 controlled-slot fixture remain available through ordinary benchmarks. Floor-10 priority is an exploratory search objective, not an automatic balance acceptance threshold.

Coverage sampling costs at most **5,040 fights**: one generation seed, eight candidates per method/character, one discovery sample per context/floor, twelve joint parties with two samples, and four finalists with twenty confirmation samples. Thorough sampling costs at most **15,840 fights**: two generation seeds, two character samples, sixteen joint parties and six finalists with forty confirmation samples. Both guided/random arms pay the same actual combat budgets and receive the same historical starts. Reports keep small-sample method comparisons descriptive.

`Fixtures/tower-party-starts.json` preserves the previous four-slot character shortlists and observed random-9677 party as starting hypotheses. Extensions retain their order, then add distinct families from the cohort's authored control, followed by the sorted pool when needed. Both methods also explore fresh whole builds. The schema-2 definition stores exact extensions and the reference party before combat; extension is not a claim that a stronger build contains its four-slot predecessor.

```powershell
# Six slots first, then 5/7/8/9/10 as separate frozen studies; total cap 41,040.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-party-progression --output TestResults/balance/tower-progression-new

# Verify any complete cohort; recipes and replay use the existing commands.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-party-verify --run TestResults/balance/tower-progression-new/slots-6

# Run an explicitly edited schema-2 budget/seed definition as one job.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-party-search --definition my-search.json --output TestResults/balance/my-new-search
```

The batch predeclares all six definitions and excludes 1,578 prior pilot/reliability/party-study seeds plus every preceding cohort's reserved schedules. The six-slot cohort uses thorough sampling; the other five use coverage sampling. Repeating this fixed batch is reproduction, not fresh evidence. For a new scientific experiment, supply unused master seeds and extend the declared exclusions. Dashboard jobs generate a fresh suggested seed and reject collisions against discoverable saved loadout-run schedules; that is a local results-folder ledger, not a registry of every external or concurrently running tool.

Historical schema-1 archives retain their original serialization, objective and verifier behavior. Schema 2 ranks minimum priority-floor gains over each context's own control, then minimum all-floor gains, guardian health, survival and stable ID. `fitness.entryWins` carries the priority-floor gain in schema 2. The unchanged Controller and later cells remain limitations. Joint contexts are identical on five-character floors; context repeats and method restarts are not independent samples. Full semantic verification runs before presenting complete search results; downloading/verifying a large archive can take time. Cancellation retains partial trials but does not make an incomplete report valid confirmation evidence. No resume, account inventory writes, production tuning, migrations, deployment or Phase 2 integration is added.

The [progression search review](../../../Balance%20Harness/Tower-Party-Progression-Review.md) records the measurements and retained evidence at `TestResults/balance/tower-party-progression-20260910/`. The six-slot control and all five searched finalists won **40/40 floor-10 confirmation fights per context** at level 50 with Uncommon Standard tier-2 rank-2 gear. Search improved some floor-11 results; six-slot floors 12–15 remained unbeaten. This supports the six-slot anchor under the provisional budget, without guaranteeing a clear or establishing optimal loadouts. **144 Tower tests passed**, and the rendered dashboard completed a separate **5,040-fight** run with cancellation, verified results, report/recipe download and replay checks. A second browser check displayed the six-slot CLI study's 180 confirmation cells, six exact parties and 180 replay choices without page or visible errors. The previous 29,760-fight four-slot archive also verifies with the current reader; its 35,121 checksum-listed files are unchanged.

The progression extension completed **41,040 fights** across separate 5–10-slot studies. All archives reconstructed successfully; **44 detailed replays matched**, and **140 exported normal-Tower fights** reproduced exactly. The studies and separate browser run use identical content/settings/executable fingerprints and 2,730 reserved combat seeds with no cross-study overlap. At ten slots, one frozen searched party won 298/300 with original allies and 300/300 with alternative allies, including 18/20 and 20/20 on floor 15. The progression review reports every cohort and the limits of those small confirmation samples. Controller/later-cell search and stronger method replication are the next bounded extension.

## Character robustness and joint party search — 10 September 2026

`tower-party-search` searches the first-cell Guardian, Restorer and both Strikers separately, then measures joint combinations. It evaluates **all 15 floors** using production RequiredSlots and unchanged production combat. The bounded default uses four Essences, level 30, Uncommon Standard tier-1 rank-1 baseline gear and level-1 unascended/unevolved Essences. Ownership is hypothetical; the Controller and later cells stay fixed. Existing 4–10-slot and other gear catalogs remain available as separate benchmarks.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-party-search --output TestResults/balance/tower-party-new-run
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-party-verify --run TestResults/balance/tower-party-new-run
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-loadout-replay --run TestResults/balance/tower-party-new-run --battle trial-000001 --detailed
```

Optional `--definition`, `--content-root` and `--catalogs-root` support bounded reruns. Save a copy of the producing executable for historical replay. Reusing the default seeds after inspecting this study is a deterministic reproduction, not new confirmation evidence; supply fresh three-stage seeds and extend `excludedCombatSeeds` for a new experiment. All actual generated seeds are checked for overlap before combat.

The default caps the study at **29,760 combats**: 17,280 character-discovery fights (four characters × two search seeds × two methods × twelve candidates × two contexts × fifteen floors × three samples); up to 2,880 joint-screen fights; up to 9,600 confirmation fights. The hard cap is 30,000. Character methods pay equal actual combat budgets. Both get identical historical Restorer A/B/C starts and control; the remaining random arm proposals sample uniform legal ordered loadouts. Guided proposals add the existing beam, substitutions and order mutations. Other roles start from their own control plus random exploration.

Selection maximizes the **minimum floor-1 win-count gain over each context's own control**, then minimum all-floor gain, lower guardian health, higher survival and stable ID. The versioned JSON `fitness.entryWins` and `fitness.wins` carry those minimum gains in this command. Per-context, per-floor clears remain alongside them. Wilson 95% describes uncertainty; it never ranks builds or applies the starter's target band. A diverse shortlist retains controls, up to two generalists and a positive-gain floor/context specialist where available.

Joint screening includes historical C, combinations of each method's per-character winners, single-character alternatives and deterministic sampling from the finite shortlist product. Strikers can differ. Joint contexts replace slots 1–4 together, so floor-1 parties are identical; larger floors test fixed alternative later-cell allies. Those repeated context outcomes and search restarts cannot be pooled as independent samples. Method-derived parties are retained for confirmation, but interacting character choices mean their comparison is not an isolated test of a party-search algorithm.

`shortlists.json`, `party-candidates.json` and `selection.json` freeze before their next stage. `party-search.md/json` retain exact builds and all-floor confirmation, including failures, draws, paired gains/losses and nominal intervals. Full compressed battle reports, content, catalog, execution identity, proposals, trial/input hashes and three seed schedules support independent review. `tower-party-verify` reconstructs deterministic proposals, all scores, both selections and confirmation from saved fights; detailed replay additionally reruns production combat. Any JSON in `recipes/` exports directly through the normal `tower --scenario <file>` command with matching content/settings. Cancellation retains completed trials and marks the report incomplete; resume and multiworker execution are not implemented.

The [recorded joint-party study](../../../Balance%20Harness/Tower-Party-Search-Review.md) completed **29,760 battles**. Its seven searched finalists each won 40/40 on floor 1 versus the control's 26/40. The strongest observed all-floor party recorded 195/600 wins with original allies and 199/600 with alternative later cells, versus 46/600 for each control. It came from random character search seed 9677; guided search has no established advantage. All-floor totals describe this matrix, not a universal clear probability or optimality claim. Exact builds, failures and tradeoffs are retained in the review and `TestResults/balance/tower-party-search-20260910/run/party-search.md/json`. Verification reconstructed the complete study with the corrected implementation, matched fourteen detailed replays and reproduced forty exported normal-Tower fights. The 125-test Tower suite and final seven-test party-search suite passed through `build/run-tests.ps1`.

This command is CLI-only. Connecting **Find loadouts** to the dashboard and measuring all roles at 5–10 slots remain later work. Four-slot higher-floor coverage does not validate the six-slot floor-10 progression goal. No production tuning, migrations, deployment, baseline promotion or Phase 2 integration is included.

## Local Tower dashboard — 9 September 2026

Start the dashboard from the repository root, then open the printed local URL (normally `http://127.0.0.1:5087`):

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-dashboard
```

Choose a profile catalog, floors, parties, trial count and seed, then click **Run Tower Benchmarks**. All **15 currently released floors** are selected by default; each party fills the floor's actual 5, 10 or 15 slots. The button generates legal profiles from current content and runs the existing production-backed benchmark. Selecting a saved reference restores its floor/party selection and seed schedule for comparison, including the narrower selection of historical references. The page shows progress, cancellation, verified results and deltas, Markdown/JSON downloads, and verified detailed replay with a searchable, paginated combat log. Tower measurements have no automatic target band.

Every operation creates a fresh `dashboard-<UTC>-<id>/` folder under `TestResults/balance`; benchmarks save `catalog.json`, `job.json` and the full `run/` bundle. Replay creates a separate operation folder containing `replay.json`, leaving source evidence unchanged. Cancel preserves completed trials and labels the benchmark incomplete. One operation runs at a time; there is no queue, resume or background service installation. Stop with Ctrl+C.

Options: `--port` (default 5087), `--runs-root`, `--catalogs-root` and `--content-root`. The default catalog folder is the built tool's `Fixtures/`; provide a custom folder for editable benchmark JSON catalogs. Reload the page after catalog changes. New runs capture fresh balance content; code changes require rebuilding and restarting the dashboard. The tool now needs the **ASP.NET Core 10 shared runtime** as well as .NET 10; UI assets are embedded, with no frontend package installation. It binds only to IPv4 loopback and uses same-origin/session checks. It adds no production endpoint or database configuration.

Saved-run discovery examines at most 2,000 directories, three levels below the configured results root, skipping links and large battle/content archives. Point `--runs-root` at the appropriate parent for deeper evidence. Selecting/downloading a run verifies its evidence; cached comparison results are recomputed only when the reference is available under that root. Measurements can be reviewed across builds, but replay requires the original executable/runtime/platform. Retain binaries yourself. Only the current completed replay is downloadable in the page; earlier replay JSON remains on disk. This UI does not edit recipes, approve targets or simulate acquisition/full Tower journeys. The separate bounded Essence search below investigates authored build alternatives.

The [dashboard review](../../../Balance%20Harness/Tower-Dashboard-Review.md) records **2,238 passing backend tests**, four dashboard HTTP integration tests, browser-driven repeat/comparison/replay and partial cancellation. Evidence is separate in `TestResults/balance/tower-dashboard-20260909/`. Phase 2 integration remains deferred, and accepted starter/earlier Tower evidence is preserved.

## All-floor progression and party compositions — 9 September 2026

Select `tower-curve-v1` for an explicit provisional progression from four to ten Essences across all 15 released floors. Three compositions use the same per-floor budget: balanced, an extra healer (`support`), and an extra damage dealer (`pressure`). Tower Lab groups the preview by floor budget. Catalog schema 2 requires `floorCellProfiles` for every selected floor; schema 1 catalogs and saved hashes remain supported unchanged.

The [curve review](../../../Balance%20Harness/Tower-Curve-Review.md) records the budget table, **900 all-floor trials**, **900 reserved-seed confirmation trials**, and **14 matching replays**. At floor 10, the balanced six-slot party won 100/100, support won 52 and drew 48, and pressure lost 100/100. The floor-10 budget is provisionally Uncommon Standard tier 2 rank 2. Floors 4, 7, 12 and 13 had no discovery wins across the tested compositions; these are investigation points under assumed budgets, not automatic tuning failures.

**79 Tower tests passed**, including the previously blocked Uncommon checks and independent normal-Tower parity for the new curve. A historical 600-battle archive still verifies with unchanged comparison results. No verification command is blocked. Evidence is retained in `TestResults/balance/tower-curve-20260909/`; discovery and confirmation reports appear in Saved results. The user's floor-1 gear budget and floor-10 six-slot target remain distinct from all provisional interpolation/gear decisions. No boss, target, baseline, migration or deployment is changed.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-curve.json --output TestResults/balance/tower-curve-new
```

## Bounded Essence build search — 9 September 2026

### Search reliability and ally transfer — 10 September 2026

`tower-loadout-reliability` runs the stronger schema-2 study. Its default focuses on **Uncommon Standard rank-1, tier-1 gear, level 30 and four Essences**. It retains all **15 floors** and the first-cell Restorer target, testing both original balanced allies and the fixed candidate-05 ally substitutions. The target's original recipe/identity remains identical between contexts; the second context changes only allies. Gear and allies never adapt within a search.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-reliability --output TestResults/balance/tower-loadout-reliability-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-verify --run TestResults/balance/tower-loadout-reliability-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-replay --run TestResults/balance/tower-loadout-reliability-new --battle trial-000001 --detailed
```

Four generation seeds × guided/random × two ally contexts × 12 candidates × 15 floors × six discovery samples cost **17,280 actual discovery battles**. Each arm pays the same 1,080-battle budget. The search algorithm and entry-first ranking stay unchanged so this study investigates replication and sampling precision. Search-seed repetitions share combat schedules; they are method replications, not extra independent clear-rate samples.

Before any confirmation, schema 2 freezes a **common union** per gear cohort: the unchanged target, two promising loadouts explicitly carried from the earlier pilot, and all arm winners from both contexts. Every finalist is then tested on every floor in **both** contexts, with **40 fresh samples per floor**. Deduplication reduces the at-most-19-finalist union. The default upper bound is **40,080 battles**, within a 45,000 hard cap. Current discovery and confirmation seeds are checked against each other and the 183 declared historical seeds from the previous pilot schedule/foundation probes. The exclusions are saved in the definition and seed ledger.

`pilot.md/json` retain all-floor results and paired comparisons against each context's own control. `reliability.json` and the extra Markdown tables compare guided/random arm winners on discovery and confirmation, and show every finalist's transfer between ally contexts. Finalists remain fixed even when confirmation disappoints; there is no automatic method-superiority claim, optimality claim or Tower target. Wilson intervals continue to describe uncertainty only.

Schema-2 battle files use **lossless compressed JSON** (`battles/trial-*.json.gz`), explicitly recorded as `reportStorage: gzip-json-v1` in the scope. This preserves the complete report while reducing repeated JSON storage. Recipes remain ordinary JSON for the existing Tower command. The verifier checks archive checksums, discovery recipes/seeds/fitness, frozen selection, every confirmation recipe/schedule/metric and derived method/transfer reports. Replay verifies the original materialized input and production combat result. Reading schema-1 uncompressed archives remains supported; historical replay still requires each run's retained executable/runtime/platform.

The existing schema-1 pilot default and prior evidence are preserved. An edited schema-2 definition may choose other supported gear cohorts, up to eight search seeds and twenty discovery samples per floor, with explicit total-cost validation (maximum hard cap 100,000); the measured default deliberately concentrates on one gear budget. Other roles, 5–10-slot studies, joint party search, multiplicity-controlled claims and dashboard integration remain separate work. See the [reliability review](../../../Balance%20Harness/Tower-Loadout-Reliability-Review.md) for results and verification.

The recorded study completed **35,280 battles**, with **116 passing Tower tests**, **13 matching detailed replays** and forty exported results matching after decoding. Fifteen finalists were confirmed in both ally contexts. All won 40/40 on floor 1 with strong allies, while some fell to 18–19/40 with original allies; the original-context control won 25/40. Historical A/B and a new random-search finalist won 40/40 in both. Guided search still showed no dependable advantage. Full report compression saved **87.74%** against equivalent minified JSON, reducing the complete archive to approximately **655 MB**. Evidence/build: `TestResults/balance/tower-loadout-reliability-20260910/`.

### Four-Essence character pilot — 10 September 2026

`tower-loadout-search` adds bounded whole-loadout search through production Tower preparation, combat and outcome interpretation. The default changes only the Restorer in the first five-member cell, keeping all allies and character/equipment identities fixed. Every character has four level-1 unascended/unevolved Essences, level 30, Uncommon tier-1 gear and baseline rolls. Standard/Fine × rank 1/2 are four separate cohorts. Every candidate runs **all 15 floors**; this entry-budget coverage does not replace the existing six-slot floor-10 or 4–10-slot progression cohorts.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-search --output TestResults/balance/tower-loadout-pilot-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-verify --run TestResults/balance/tower-loadout-pilot-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-replay --run TestResults/balance/tower-loadout-pilot-new --battle trial-000001 --detailed
```

The default is **12 evaluated candidates × 2 methods × 2 search seeds × 4 gear cohorts × 15 floors × 2 discovery samples = 5,760 discovery battles**, plus at most **3,600 confirmation battles**, for **9,360 total**. The hard cap is 10,000. Duplicate finalists reduce confirmation cost. Both methods include the same control and receive equal actual combat budgets; cache reuse is isolated by arm so it cannot subsidize one method. Search seeds share discovery combat seeds and must not be pooled as independent observations.

Guided search uses a direct-mechanic whole-loadout start, random starts/restarts, a diverse three-member beam and single/double/order mutations. Random search samples ordered distinct-definition tuples uniformly, rejecting duplicate source families. Unknown mechanics stay eligible. Ranking is predeclared: **floor-1 wins first**, all-floor wins next, lower mean remaining guardian health, higher survival, stable ID. This entry objective differs from the earlier progression-curve search. The pilot does not use Wilson bounds as a selection score.

`selection.json` freezes the control, the historical candidate-05 substitutions (remeasured with pinned identities), and each arm's winner before any unused-seed confirmation. Candidate 05 changes allies and is a separate party comparator, not a competitor in the character search. Duplicate finalists merge; no reselection follows confirmation. All finalists receive all-floor results, Wilson 95% intervals and paired gains/losses. These are descriptive estimates with no simultaneous-confidence or optimality claim.

The command saves strict `definition.json`, fixed `contexts.json`, `scope.json`, captured `content/`, mechanics Markdown/JSON, `searches/` proposal and evaluation logs, `seed-ledger.json`, `selection.json`, complete exported `recipes/`, per-fight `battles/`, `trials.jsonl`, `pilot.md/json` and `files.json` checksums. Verify reconstructs discovery fitness and frozen selection from saved fights. Replay checks the archive and regenerates the input against captured content, requires the original executable/runtime/platform, then compares the detailed combat result and Tower outcome. Keep the executable that produced a run; rebuilding after code changes intentionally invalidates replay. Exported recipes also work with the existing `tower --scenario` command against current content.

An edited saved definition can be supplied with `--definition`; it accepts target party slot 1–5, optional allowed Essence pool, separate gear cohorts, search/combat seeds and bounded sample/candidate/cost controls. Unsupported fields fail. All floors and four-Essence entry budgets remain fixed in this pilot. Cancellation retains completed trials and proposal logs, marks the report incomplete and never recommends a partial search; there is no resume. One worker is supported. The existing dashboard button still uses the earlier authored search.

The [pilot review](../../../Balance%20Harness/Tower-Loadout-Pilot-Review.md) records **8,760 completed battles**, **108 passing Tower tests**, **11 matching detailed replays** and ten exported-recipe fights matching the original pilot byte for byte. At Standard rank 1 on floor 1, two selected alternatives won 10/10 versus the control's 8/10, while another guided finalist regressed to 6/10. Guided search did not establish an advantage over equal-cost random search; ten confirmation samples per floor remain exploratory. Evidence/build: `TestResults/balance/tower-loadout-pilot-20260910/`. Remaining work includes additional roles and ally contexts, 5–10-slot search jobs, joint party optimization, specialist retention, a matched-cost authored-search comparison and dashboard controls. No boss tuning, numerical target, migration or deployment is introduced; Phase 2 stays deferred.

### Loadout foundation — 10 September 2026

The separate `tower-loadout-prepare` command implements legal candidate preparation and a slot-order audit for the broader search plan. It accepts a strict versioned definition with complete Tower contexts, target slots, allowed pools, zero-based pins, declared ownership assumptions and bounded proposal/audit schedules. Without `--definition`, it generates 60 progression/role contexts and 105 controlled slot-count contexts from the existing catalogs, covering all 15 floors and counts 4–10. Only the target character changes; the opt-in reference `identityEssenceIds` field retains the original character, equipment and slot-instance identities across Essence changes. Omitting that field preserves old reference identities and serialized hashes.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-prepare --output TestResults/balance/tower-loadout-foundation-new
```

The [foundation review](../../../Balance%20Harness/Tower-Loadout-Foundation-Review.md) records **652 accepted/prepared proposals**, eight family-rule rejections and an inventory of all **80 definitions/77 families**. The command saves the resolved definition/settings, source/execution fingerprints, mechanics Markdown/JSON, accepted scenario recipes, candidate rejection reasons, order-audit battle bundles and checksums. Generated `definition.json` can be edited and supplied with `--definition`; unknown fields and unsupported progression policies fail. `--content-root` and `--catalogs-root` select the inputs. It is sequential/cancellable, requires a new output directory, and offers no resume or optimizer yet.

**Order matters:** 12/18 paired probes changed gameplay, including one floor-1 defeat becoming a victory when the Restorer's Essence order was reversed. Keep ordered loadouts; do not sort them into sets for search deduplication. **99 Tower tests and all 2,514 backend tests passed**, 14 detailed replays matched, and a historical 600-battle archive still verifies with zero changed comparison records. Retained evidence/build: `TestResults/balance/tower-loadout-foundation-20260910/`.

These foundation proposals are legality probes, not ranked recommendations. Mechanics signals describe direct authored effects, with full structured definitions retained; effective power and indirect/runtime interactions remain unassessed. Ownership is assumed within a declared pool, and training stays level 1/unascended/unevolved. The separate pilot above adds order mutations, an equal-budget random-search comparison and four-slot optimization. Joint party search remains open. The existing dashboard button below is unchanged. No boss tuning, numerical target, migration or deployment is introduced; Phase 2 stays deferred.

### Existing bounded quality search

For expansion from four authored substitutions to whole character/party loadout search, see the [Essence loadout search plan](../../../Balance%20Harness/Essence-Loadout-Search-Plan.md). The offline foundation and character pilot above are implemented. Party synergy and dedicated retest/reoptimization workflows remain planned; the current dashboard button does not invoke the new pilot.

Click **Search Essence builds** in Tower Lab. This button has its own fixed plan, independent of the benchmark form: all 15 floors, the balanced composition and the curve's 4–10-slot progression. It generates all 16 combinations of four authored substitutions, tests eight discovery trials per floor/candidate, freezes a shortlist, and confirms it with 30 trials per floor on a disjoint seed set. The maximum budget is 4,620 battles. No manual character editing is needed when equipment or Essence definitions change; new runs materialize current content. Code changes still require rebuilding/restarting the dashboard.

The four hypotheses replace Guardian Hobgoblin with Horned Wolf, Restorer Goblin Shaman with Pack Howler, Striker Pixie with Dire Wolf, and Controller Giant Bat with Wood Nymph. Gear, level, Essence count/progression, roles and RequiredSlots remain fixed within each floor. `candidate-00` retains the original Essence choices; the other IDs encode combinations in that order. The report names every substitution and explains its intended synergy. Ownership/access remains an assumption; these are legal alternatives, not an exhaustive catalog search or certified optimal builds.

Discovery ranks by total wins, lower remaining guardian health, higher party survival, then stable ID. Selection retains the control and two noncontrol generalists, plus up to three distinct floor winners ordered by win gain over control and floor number. `selection.json` is saved before any confirmation fight. Every selected candidate runs every floor in confirmation, exposing tradeoffs instead of merging the best observed result on each floor into a fictional generalist. Draws count as nonwins. Paired gained/lost wins and nominal intervals are descriptive, with no multiple-comparison guarantee or automatic promotion.

**Selection clarification — 10 September:** actual party combat results select the tested loadouts. Wilson 95% clear-rate intervals describe uncertainty; they are not an Essence quality score or the discovery ranking criterion. Paired intervals describe changes against the control. Individual damage/healing statistics and proposed synergies help explain candidates but do not identify the best Essences on their own. The current button searches only the four predefined substitutions above; broader legal whole-loadout exploration and joint party optimization remain planned in the [selection principle](../../../Balance%20Harness/Essence-Loadout-Search-Plan.md#selection-principle-clarified--10-september-2026). Recommendations are best-found choices for stated budgets/allies/bosses, not certified optimal builds.

Each new search folder contains frozen content/settings, `search-config.json`, `candidates.json`, `selection.json`, `finalists.json`, full `discovery/` and `confirmation/` benchmark bundles, `search-status.json`, and `search-report.md/json`. Both stages appear in Saved results; their **Build search report** and **Search evidence JSON** links regenerate the report from verified evidence. Reports include per-floor comparisons and per-role damage, healing, damage taken, attention, stagger and conditional first-death timing. Detailed replay retains ability statistics and events; contribution totals are not causal attribution. Cancellation preserves partial evidence and cannot produce a completed search report.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-search --output TestResults/balance/tower-search-new
./build/run-tests.ps1 -Configuration TowerSearchFinalVerification -Filter 'FullyQualifiedName~BalanceHarnessTower'
```

`--catalogs-root` selects a directory containing `tower-essence-search.json` and its base catalog; `--content-root` selects current API content. The config allows one to four distinct role substitutions, discovery samples 1–20, confirmation samples 1–100, and separate master seeds; the benchmark's overall execution limits still apply. To run another experiment, use a new output directory. For verified replay use the retained executable's existing `replay --run <search/confirmation> --battle floor-1.candidate-00/tower.0001 --detailed` command against either stage. See the [search review](../../../Balance%20Harness/Tower-Essence-Search-Review.md) for measured results and limitations. **86 Tower tests passed**; Phase 2 remains deferred and boss tuning remains paused.

The full browser run completed **4,620 battles** (1,920 discovery, 2,700 confirmation), with **17 distinct matching replays**. Candidate 05 (Guardian Horned Wolf plus Striker Dire Wolf substitutions) led confirmation at **330/450 wins**, versus **218/450** for the unchanged control. At floors 1/2/12 it won **30/26/13 out of 30**, versus control **22/6/0**; all finalists won 30/30 at floor 10. Floors 4 and 7 remain unresolved with zero observed wins, and floor 13 has only rare clears. These findings support further build investigation before tuning. They do not establish globally optimal builds or replace the existing reference catalogs. Retained evidence is under `TestResults/balance/tower-search-20260909/`, with both full stage bundles in the browser operation identified in the review.

## Intended Uncommon Tower entry budget — 9 September 2026

The user specifies floor 1 at **four Essences per character, Uncommon equipment, Standard/Fine quality and rank 1–2**. Select `tower-entry-uncommon-v1` to run all four quality/rank combinations on all 15 floors. Level 30, tier 1 and the fixed Essence selections remain modeling assumptions. Earlier Common-gear catalogs and evidence are preserved.

The [Uncommon entry review](../../../Balance%20Harness/Tower-Uncommon-Entry-Review.md) records 1,500 all-floor discovery battles and 1,000 reserved-seed confirmation battles. Floor-1 confirmation wins were **50/100 Standard rank 1**, **89/100 Standard rank 2**, and **100/100 Fine at both ranks**. Floor 1 is demonstrably beatable by these builds within the intended budget; no boss edit or numerical target was applied. The separate six-slot floor-10 checkpoint won 100/100.

Nine replays matched using the retained measurement build, and 75 paired trials across all floors matched a newly compiled harness. The subsequent curve increment resolved the temporary compilation block: all 70 existing Tower tests passed, including the Uncommon materialization and independent normal-Tower parity checks, followed by 79 passing tests with curve coverage. Earlier failed attempts remain recorded in the review.

Matching executables, frozen content, protocol, logs, reports and replay evidence are retained in `TestResults/balance/tower-entry-uncommon-20260909/`. The new catalog and saved discovery/confirmation reports are available in Tower Lab. Mixed quality sets, broader compositions, acquisition timing and numerical clear-rate policy remain open.

## Four-slot Tower entry reinforcement — 9 September 2026

Select `tower-entry-ranks-v1` in Tower Lab to compare level-30 four-Essence parties at equipment ranks 0–3 across every released floor. The existing six-slot level-50 checkpoint is included. Recipes vary only reinforcement rank within the four-slot cohort; gear acquisition remains an assumption.

The [entry review](../../../Balance%20Harness/Tower-Entry-Ranks-Review.md) records **1,500 all-floor discovery battles** and **600 reserved-seed confirmation battles**. Floor-1 discovery wins at ranks 0/1/2/3 were **0/0/3/5 out of 20**. Confirmation found **0/100** wins at rank 0 and **10/100** at rank 2; the six-slot floor-10 checkpoint won **100/100**. Reinforcement helps, but does not establish an approachable four-slot entry build. Support survival and legal loadout alternatives are the next investigation; no boss or numerical target was changed.

**66 Tower tests passed** through `build/run-tests.ps1 -Configuration TowerEntryVerification`, including independent normal Tower parity. Eight replays matched. The separate build configuration avoided another process's locked Release test DLL. Evidence and matching executable are retained in `TestResults/balance/tower-entry-ranks-20260909/`; discovery and confirmation appear in Saved results. Existing catalogs/evidence remain unchanged.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-entry-ranks.json --output TestResults/balance/tower-entry-ranks-new
```

## User Tower progression anchors — 9 September 2026

The stated design anchors are **floor 1 starting at about four equipped Essences per character** and **floor 10 being doable at about six**. These describe Essence progression, not expedition party size. Floor 1 still requires five characters and floor 10 requires fifteen. No exact clear-rate band, gear budget or mapping for the other floors has been approved.

Select **tower-anchors-v1** to check these at the first slot-unlock levels. `entry-4` uses level 30, four Essences and Standard tier-1 rank-0 gear; `floor-10-6` uses level 50, six Essences and the existing Standard tier-2 rank-3 mid budget. Both have seven items, baseline rolls, the same role composition, level-1 unascended Essences and no styles/contributions. The levels and gear are explicit modeling assumptions; only the two floor/slot anchors came from the user. Both parties still run all 15 floors for context (600 trials by default), but only their named checkpoints are the intended anchors.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-anchors.json --output TestResults/balance/tower-anchors-new
```

The [anchor review](../../../Balance%20Harness/Tower-Anchors-Review.md) records **62 passing Tower tests**, a complete 600-trial run and two matching replays. At the named checkpoints, the assumed entry party won 0/20 on floor 1 and the assumed mid party won 20/20 on floor 10. Floor 1 remains a gap to investigate for this budget; no numeric policy or baseline was approved. The level-90 slot-range catalog remains useful for isolating loadout changes, but it does not establish viability at slot unlock. Earlier catalogs/evidence remain preserved, and no gameplay tuning or automatic balance gate is introduced.

## Parties with four through ten Essences — 9 September 2026

Select **tower-essence-slots-v1** in **Profile catalog** for parties with **4, 5, 6, 7, 8, 9 and 10 equipped Essences per character**. All seven are selected by default and run against all 15 released floors: **105 combinations / 2,100 trials** at 20 samples per combination. Party size still follows the floor's RequiredSlots (5, 10 or 15 characters); Essence count is separate from party size.

Every build stays at level 90 with seven Standard tier-2 rank-3 items, baseline rolls and no styles. All ten Essence slots are legally unlocked, with unused slots left empty in the smaller loadouts. Each count extends the same pinned role lists by one Essence per character. The Guardian/Restorer/two Strikers/Controller composition, equipment and character level stay fixed. Essences are level 1, unascended and unevolved; ownership/access remain conditional assumptions. This compares added loadouts, not different character-level unlock milestones or an individual Essence in isolation.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-essence-slots.json --output TestResults/balance/tower-essence-slots-new
```

The [slot-range review](../../../Balance%20Harness/Tower-Essence-Slots-Review.md) records **59 passing Tower tests**, a complete 2,100-trial measurement, a repeated 105-trial smoke and seven retained matching replays. The five-to-six-Essence drop observed on floor 11 also matched independent normal Tower execution; additional Essences do not guarantee better performance. Existing reference, progression and factor catalogs are preserved. Seeds remain paired within each floor, saved runs support verified replay, and Tower results have no automatic target band. `--samples 1` produces a 105-battle smoke; the full selection permits at most 95 samples per combination under the existing 10,000-trial cap.

## Tower factor presets — 9 September 2026

Select **tower-factors-v1** to investigate the floor-11/12 progression transitions. Eight parties cover every released floor with 20 paired seeds: **120 combinations / 2,400 trials**. The parent relationships below isolate one declared build factor; all gear is Standard tier 2 with baseline rolls, fixed equipment definitions and no styles. Essences remain level 1, unascended and unevolved.

| Preset | Level | Rank | Essences | Compare with / change |
| --- | --- | --- | --- | --- |
| control | 50 | 3 | 6 | Mid-budget control |
| level-70 | 70 | 3 | 6 | control / character level |
| level-90 | 90 | 3 | 6 | control / character level |
| rank-4 | 50 | 4 | 6 | control / equipment rank |
| rank-5 | 50 | 5 | 6 | control / equipment rank |
| essences-8 | 90 | 3 | 8 | level-90 / added Essence loadout |
| essences-10 | 90 | 3 | 10 | level-90 / added Essence loadout |
| late | 90 | 5 | 10 | essences-10 / equipment rank |

Essence extensions use the same pinned role lists as progression presets. Higher Essence counts are tested at level 90 to respect slot unlocks; they are not illegal level-50 builds. Adding several Essences tests the combined loadout, not any one ability or composition. These are conditional ownership budgets, not player acquisition claims. Reference comparisons match the same catalog across runs; use the table above to interpret differences between parties within a run. The existing reference and progression catalogs are preserved.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-factors.json --output TestResults/balance/tower-factors-new
```

The [factor review](../../../Balance%20Harness/Tower-Factors-Review.md) records **55 passing Tower tests**, two matching 2,400-trial runs and ten verified replays. On floor 11, level 90 with six/eight/ten Essences won 4/20, 10/20 and 20/20 respectively; rank 4/5 at level 50 won 0/20. Every preset lost on floors 12–15. These are descriptive observations from one fixed schedule. No automatic target, optimization or boss tuning is applied. `--samples 1` is a 120-battle smoke; complete preset selections permit at most 83 samples per cell under the unchanged 10,000-battle cap.

## Tower progression presets — 9 September 2026

Select **tower-progression-v1** in the dashboard's **Profile catalog**, then run. Its early, mid and late parties all cover floors 1–15, producing **45 combinations × 20 trials = 900 battles**. The existing `tower-reference-v1` catalog remains available and unchanged. A compact budget summary appears in each party preview; expand it for individual equipment, Essence selections and assumptions.

| Preset | Character level | Equipment per character | Essences per character |
| --- | --- | --- | --- |
| early | 20 | Seven Standard tier-1 rank-0 items | 3 |
| mid | 50 | Seven Standard tier-2 rank-3 items | 6 |
| late | 90 | Seven Standard tier-2 rank-5 items | 10 |

All use baseline equipment rolls, no styles, and level-1 unascended/unevolved Essences. The same Guardian/Restorer/two Strikers/Controller cell repeats to fill RequiredSlots. Equipment choices stay fixed across presets, and Essence lists extend the existing canonical role suggestions, explicitly pinned in [tower-progression.json](Fixtures/tower-progression.json). Character progression, reinforcement and added Essence abilities vary together; this measures whole builds, not the isolated effect of one upgrade. Tier 2 is legal at level 50; rank 5 is the equipment maximum. These labels are conditional benchmark budgets, not approved player populations or a claim that every item is available at that stage. Acquisition costs/time, ascension and a continuous climb remain outside the simulation.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-progression.json --output TestResults/balance/tower-progression-new
# After balancing changes, repeat with the same catalog/seeds and add --reference <previous-run>.
```

New runs resolve current equipment and Essence content automatically. Each preset on a floor shares the same seed schedule. Use a progression run as the reference to compare matching presets across balance changes; the earlier starter/mixed catalog has different cell IDs and is not a matching progression reference. See the [progression review](../../../Balance%20Harness/Tower-Progression-Review.md) for measured results, verification and remaining limitations. No Tower target band is applied.

## Automatic Tower benchmarks — 9 September 2026

`tower-benchmark` generates the declared profiles from **current equipment and Essence content**, fills the selected floors' `RequiredSlots`, runs deterministic battles and writes Markdown/JSON reports. You do not edit character stats when balancing items or abilities. Change a profile recipe only when changing the intended equipment, Essence selection or progression budget.

```powershell
# Generate profiles and save a reference measurement.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --output TestResults/balance/tower-reference-new

# After a balance change, regenerate and compare matching parties/seeds in one command.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --output TestResults/balance/tower-candidate-new --reference TestResults/balance/tower-reference-new

# Replay a trial, or compare two already saved benchmarks.
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/tower-candidate-new --battle floor-5.mixed/tower.0001 --detailed
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower-compare --reference TestResults/balance/tower-reference-new --run TestResults/balance/tower-candidate-new --output TestResults/balance/tower-comparison-new
```

The [catalog](Fixtures/tower-benchmark.json) selects **all released floors, 1–15**. Floors 1–4 and 6–7 require five participants; floors 5, 8–9 and 11–14 require ten; floors 10 and 15 require fifteen. Each floor runs two parties: five copies of the existing conditional level-10 starter build, and a level-20 mixed cell with Guardian, Restorer, two Strikers and Controller. The mixed budget is seven items filling eight equipment slots, Standard tier 1, rank 0, baseline rolls, and three level-1 unascended Essences per character. Its equipment/Essence choices follow existing canonical role suggestions, explicitly pinned in the catalog. Ownership/access remain assumptions; these roles are conventions, not player classes or approved Tower audience budgets. Each ordered cell repeats to fill larger floors with distinct participants and production PartyNumber assignment. There are no contributions or styles.

The default is **30 cells × 20 seeds = 600 battles**. Optional `--catalog`, `--seed` (default 1337), `--samples` and `--content-root` select inputs. `--samples 1` runs a 30-battle smoke. Limits are 20 floors, 20 parties, 100 profiles, 1–1,000 samples per cell and 10,000 total battles. Each cell has five profile references. All generated scenarios are validated before combat; floor/party/catalog reordering preserves scenarios and seeds. Seeds derive from `tower-benchmark-seeds-v1`, master seed, floor number and zero-based trial index, sharing seeds across parties on the same floor. Changing sample counts, recipes or schedules makes affected comparison cells incompatible.

`benchmark-input.json` freezes the catalog, selected settings and expanded scenarios; `benchmark-manifest.json` fingerprints the captured content and execution. `scenarios/` exposes generated recipes; each `cells/<cell-id>/` is a complete existing Tower bundle with individual replay support. `benchmark.md` / `benchmark.json` report clear intervals, original-party survival, duration and guardian health by cell. `--reference` adds `comparison/comparison.md` and `.json`, showing candidate-minus-reference changes and content/settings/build differences. It also reports shared-win duration separately. A saved reference is a measurement, not automatic balance acceptance. No pooled win rate or starter target applies.

Comparisons verify saved content, inputs, trial hashes and report consistency without requiring the old executable. They reject missing/corrupt evidence, and label changed recipes/schedules/rules or incomplete runs rather than presenting them as comparable. Uncertainty uses the existing paired clear-rate method; paired mean intervals remain unavailable below 30 pairs or with zero variance. Changed content coefficients and assemblies are allowed and disclosed. Detailed replay still requires the exact original executable/runtime/platform. Existing outputs are preserved; cancellation saves partial cells and an incomplete benchmark. There is no automatic resume, sample extension, reference selection or baseline promotion.

The [benchmark review](../../../Balance%20Harness/Tower-Benchmark-Review.md) records two complete 120-battle runs with **zero changed gameplay records**, six detailed replays and **2,234 passing backend tests**, including 33 Tower tests. Temporary-content tests prove that equipment and Essence coefficient edits alter fresh results while recipes remain unchanged. Independent production parity now includes the mixed party on all three floors, including ten-character preparation and playback.

The local dashboard above calls this command workflow's underlying runner. Broad build search, scouting variants, approved progression budgets/targets, hosted integration and full Tower journeys remain future work. Phase 2 integration stays deferred. The earlier first-slice evidence and accepted starter archives are unchanged.

The [all-floor review](../../../Balance%20Harness/Tower-All-Floors-Review.md) covers the expansion from three floors to all 15: **49 Tower tests passed**, including independent normal-path parity on every floor, complete-party preparation, all-floor HTTP comparison and floor-15 replay. Two complete 600-battle runs matched all 600 pairs, and **15 detailed replays matched**, one per floor. Every cell had 0/20 wins, reported descriptively. The explicit floor list is checked against the production released-floor provider so future releases cannot silently miss benchmark coverage. Existing profile IDs, recipes and per-floor seeds are unchanged. Each floor starts fresh; this is coverage of every boss, not a persistent climb with acquisition, damage carryover or approved progression builds. Retained earlier measurements remain historical evidence at their original scope.

## First Tower slice — 9 September 2026

[tower-floor-1.json](Fixtures/tower-floor-1.json) fixes floor 1, **The Waking Step / Garran, the Gatekeeper**, with its actual `RequiredSlots = 5`. Five distinct level-10 characters each have Shortsword, Heavy Breastplate, Amulet and level-1 Goblin Warrior + Goblin Essences. Equipment is Standard tier 1, rank 0, with baseline rolls and no styles. This homogeneous party is a legal conditional starter-reward build; it is not an approved representative Tower cohort or an optimized role composition. Ownership and access are assumed, not simulated.

Slots 1–5 retain distinct character/equipment identities, ordered by `PartySlot`, all in `PartyNumber = 1`. The uncleared floor has no contributions or scouting bonuses. Combat starts at full prepared health with production resource defaults, active abilities on cooldown, a 30-tick base attack interval and a 6,000-tick cap. Guardian scaling, native abilities, stagger and party targeting come from production Tower preparation. No starter win-rate policy is applied.

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower --output TestResults/balance/tower-floor-1-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/tower-floor-1-new --battle tower.0001 --detailed
```

`tower` also accepts `--scenario <json>` and `--content-root <API.LL-directory>`. The scenario must declare 1–1,000 distinct integer seeds and exactly fill the selected released floor's slots with unique legal builds. The checked-in schedule is `1337, 17, -12345, 0, 918101–918116`. There is no sample extension/resume command. Use a new output directory for each run. Invalid recipes, modified materialized inputs or rules, cancellation and missing results cannot produce complete evidence.

Tower bundles use a separate version-1 envelope, leaving historical idle manifests and content allowlists unchanged:

| File | Purpose |
| --- | --- |
| `tower-input.json` | Entire predeclared schedule, resolved floor/guardian, full party equipment/Essence data, assumptions, rules and selected settings, saved before combat |
| `tower-manifest.json` | Input hash, 16 allowlisted content hashes (current idle files plus Tower floors), runtime/platform and assembly hashes |
| `battles/tower.NNNN.json` | Prepared participants, engine/content outcomes, terminal state, statistics/telemetry, Tower success, guardian health and display duration |
| `scorecard.md` / `scorecard.json` | Completeness, wins/defeats/draws/timeouts, descriptive 95% Wilson clear interval, trial rows; JSON includes separate win/non-win durations |
| `tower-results.json` | Saved result hashes checked before replay |
| `failure.json` | Visible invalid/cancelled execution diagnostics when needed |

Normal trials call `WorldTowerCombatRuntimeFactory` and `ExecuteTowerPlaybackAsync`; detailed replay calls the simulation executor with equivalent explicit rules and verifies the saved preparation, full gameplay summary and Tower outcome. `SnapshotCombatantBuilder.BuildFromItemBases` is the shared production rehydration seam; offline lookup supplies catalog item bases without a database. The ordinary result factory supplies the content outcome; success requires Victory, as in `WorldTowerService`. Each trial gets a fresh executor. Replay requires a saved completed trial and the original assemblies/runtime/platform, even for a partial cancelled bundle; it never rewrites the original report.

The [first-slice review](../../../Balance%20Harness/Tower-First-Slice-Review.md) records **20 valid trials: 0 wins, 20 defeats, no draws/timeouts**, clear rate **0% [0–16.11%]**, and **four matching detailed replays**. Mean defeat duration was **55.475 seconds**; mean guardian health remaining was **91.412%**. This demonstrates trustworthy execution, not an accepted Tower balance target. Local evidence is in `TestResults/balance/tower-floor-1-20260909/`, including a retained `executable/`, `run/` and `replays/`. Replay it with the retained build:

```powershell
dotnet TestResults/balance/tower-floor-1-20260909/executable/BalanceHarness.dll replay --run TestResults/balance/tower-floor-1-20260909/run --battle tower.0001 --detailed
```

`BalanceHarnessTowerTests` independently persists/reloads real character snapshots through the normal database-backed builder, Tower factory and playback executor for four seeds, comparing preparation, party/stagger metadata, terminal state/statistics, duration and outcome interpretation. It also checks repeatability, detailed/compact equivalence, recipe order, illegal parties/floors/loadouts, input/rule changes, damaged evidence, output preservation and partial cancellation. **All 20 Tower tests and all 2,157 backend tests passed** through `./build/run-tests.ps1`; see the review for retained verification details and the resolved sandbox access limitations.

Limits: only this floor/party and the uncleared/no-contribution state are measured. Multi-party floors, alternative compositions, scouting states, full Tower progression, victory/draw examples for this adapter, acquisition budgets, rewards/rally persistence, baseline/comparison/goal integration and performance benchmarking remain unverified or deferred. Environment overrides are not applied. The command fingerprints but does not automatically retain binaries/runtime; keep the matching build for replay. Phase 2 integration remains deferred; starter fixtures, policies, accepted evidence, production content and deployment settings are unchanged by this increment.

## Run the suite

From the repository root:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --output TestResults/balance/idle-reference-001 --seed 1337
```

For a quick 36-battle smoke run, use a new output directory and add `--samples 3`. `--samples` overrides the per-cell sample count, not the total. `suite` also accepts `--suite <json>` and `--content-root <API.LL-directory>`. Its default fixture is [idle-reference.json](Fixtures/idle-reference.json). Preflight validates every cell before executing; local limits are 10,000 samples per cell, 1,000 cells, and 100,000 total battles.

| Checkpoint | Builds | Fixed encounters | Acquisition assumptions |
| --- | --- | --- | --- |
| Level 1, Lumo Ruins | Mace / shortsword | Goblin / Goblin Warrior | First Weapon complete; one starter weapon, Goblin essence |
| Level 5, Blood Grove entry | Mace + medium chest / dagger + light chest | Raven / Blood Zombie | Trial of Lumo complete; one acquired chest, Goblin essence |
| Level 10, Crystal Creek entry | Greatsword + medium armor / staff + light armor | Blue Slime / Frost Imp | Blood in the Grove complete; four equipment items, two unlocked essence slots; Goblin + Goblin Warrior / Goblin + Vampire Bat |

All gear is tier 1, rank 0, standard quality, with baseline attribute rolls and no active styles. All selected essences are level 1, unascended and unevolved. Two-handed weapons occupy both hands. These are explicit ownership hypotheses; drop rates, acquisition time, quest completion, and essence training are not simulated. Builds within a stage use equal equipment counts, not a claimed equal power budget. The fixture retains the full assumptions in each run. “Challenge” is an encounter-selection hypothesis, not a guaranteed difficulty ordering.

The [8 September policy review](../../../Balance%20Harness/Idle-Policy-Review.md) found that Goblin is not one of the current First Hunt choices, later equipment ownership is unverified, and Blood Grove/Crystal Creek normally spawn two enemies. Keep this fixture as a fixed control; it does not certify an immediate post-tutorial build or ordinary area difficulty. The separate First Hunt cohort below addresses those selections while leaving training and exact reward outcomes as explicit assumptions. All numerical goals remain draft.

Seeds are derived by `StableRandom.Seed` from `idle-suite-seeds-v1`, the master seed, stage ID, encounter ID and zero-based trial index. Alternative builds share the same encounter seeds. Reordering cells or changing combat coefficients preserves the schedule. Changing stable stage/encounter IDs changes it. Results across builds are paired observations and must not be pooled as independent samples.

The scorecard reports wins/losses/draws, valid sample sizes, a 95% Wilson clear-rate interval, separate win/non-win durations, final player health and tick-limit draws. JSON includes mean, median and interpolated p90 distributions; p90 is unavailable below ten observations. Missing samples remain unavailable rather than becoming zero. Invalid, cancelled and unexecuted battles are counted separately and cannot produce a complete suite. No balance pass/fail policy is applied.

Select a battle ID from `scorecard.md` or `battles.jsonl` to reproduce it with an event log:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/idle-reference-001 --battle starter.mace.ordinary.0001 --detailed > TestResults/balance/starter-replay.json
```

## First Hunt cohort and enemy groups

[idle-first-hunt.json](Fixtures/idle-first-hunt.json) crosses Goblin Warrior, Hollow Stag and Skeleton with mace/wand choices at levels 1, 5 and 10. It uses one enemy at level 1 and fixed duplicate/mixed pairs at levels 5 and 10. Equipment follows quest-reward counts: one weapon, then one Armor Chest item, then one Jewelry Chest item. Medium Mail and Amulet are fixed possible box outcomes, not guaranteed selections. All Essences remain untrained at level 1; level 10 adds a Goblin selected through the earlier Lumo Token. See the [cohort review and measured results](../../../Balance%20Harness/First-Hunt-Cohort.md).

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json --output TestResults/balance/first-hunt-001 --seed 1337
```

The suite has 36 cells, 3,600 battles by default, or 108 with `--samples 3`. Use [idle-first-hunt-goals.json](Fixtures/idle-first-hunt-goals.json) with `evaluate --goals`; it expands six draft goals into 180 checks. These entry goals have no upper clear-rate ceiling. A baseline must come from this same cohort and use matching sample counts/seeds. Neither its targets nor baselines are interchangeable with the original controls.

Schema 2 scenarios and encounter definitions retain `creatureId` for the first enemy and may append one or two `additionalCreatureIds`, in order. The input archives matching `additionalCreatures` snapshots. Every occurrence receives an independent combat slot/state, including duplicate species. All members must belong to the selected area and the count must be possible there. Changes to count, order or IDs make a cell non-comparable; changed frozen companion coefficients are reported as resolved-input changes. Schema 1 remains single-enemy and its hashes stay unchanged because unused extension fields are omitted. Replay retains its original binary/runtime checks.

## Investigate training and reinforcement

The [Blood Grove review](../../../Balance%20Harness/Blood-Grove-Progression-Review.md) checks actual progression costs and reports a fixed 14,400-battle experiment. All six starter builds still lost both pairings at ranks 0, 1 and 5 on both seed sets. Unascended Essence training from level 1 to 10 changed no combat summaries; ability growth occurs at Ascension. These are conditional progression probes, not approved player budgets or balance targets.

```powershell
./build/investigate-blood-grove.ps1 -OutputDirectory TestResults/balance/blood-grove-001
```

The script writes its fixed plan before execution, generates six recipes from the Blood Grove stage, runs 100 samples per cell on seeds 1337 and 7331, and retains per-cell evidence plus detailed replays. Add `-NoBuild` after building or `-SamplesPerCell 2` for a small workflow smoke. It requires a new output directory and does not run goal evaluation or promote a gameplay baseline.

Schema-2 stages and single scenarios optionally accept `"essenceLevels": { "essence.goblin_warrior": 10 }`. Keys must name selected Essences (across the stage's builds for a stage map); omitted selections stay level 1. Values must be unascended levels 1–10. Ascension/evolution recipes are outside this extension. Training maps participate in fixture/scenario identity and are checked against frozen snapshots. Absent maps are omitted from JSON, preserving both existing cohorts' hashes. Different progression recipes remain incompatible with ordinary regression comparison, even when their combat outcomes match.

## Selected Blood Grove starter reference

[idle-blood-grove-starter.json](Fixtures/idle-blood-grove-starter.json) pins the user-selected Goblin Warrior + Sword + Heavy Chest build for Blood Grove. Sword maps to the one-handed Shortsword and Heavy Chest to Heavy Breastplate. Character level is 5; the one Essence stays level 1, unascended and unevolved. Both items are common, Standard, tier 1, rank 0 with baseline rolls and no Fury. The exact chest is a fixed possible reward outcome. This two-cell suite preserves the earlier cohorts and uses the existing command:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter.json --seed 1337 --output TestResults/balance/blood-grove-starter-001
```

The [starter reference review](../../../Balance%20Harness/Blood-Grove-Starter-Reference.md) records the recipe, fixed 400-battle discovery/confirmation measurement and two selected replays. Both encounters originally produced 0/100 wins on both seed sets. The current user-approved target is a **50–90% band for each encounter, retaining the 70% aim**. [idle-blood-grove-starter-goals.json](Fixtures/idle-blood-grove-starter-goals.json) carries reviewed policy v2 and pins the unchanged recipe. The [band revision](../../../Balance%20Harness/Blood-Grove-Band-Review.md) records saved-run re-evaluations; [v1](Fixtures/idle-blood-grove-starter-goals-v1.json) preserves the historical 65–75% policy. The [local candidate](../../../Balance%20Harness/Blood-Grove-Local-Validation.md) now changes Blood Grove only and confirms at 89.30% / 76.73% over 3,000 trials per encounter. That original result remains Inconclusive; the later [acceptance confirmation](../../../Balance%20Harness/Blood-Grove-Acceptance-Confirmation.md) passes both checks and has a separate accepted local baseline.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --goals LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json --run TestResults/balance/blood-grove-starter-reference/confirmation --output TestResults/balance/blood-grove-starter-evaluation-001
```

This absolute-only policy needs no baseline argument. It enforces the band when explicitly selected: the original losing reference returns two failed checks and exit 1; overlapping confidence intervals return 3. At least 100 trials are required, and 70/100 now passes the wider band. The saved fine candidate's 74.1% mixed-pair result passes, while 88.5% against two Ravens remains inconclusive because its interval reaches 90.33%. Declare any further confirmation budget and fresh seeds in advance without extending completed experiments. CI continues to smoke-test the two existing draft cohorts, while harness tests cover this selected recipe and policy. Do not pass the enforced policy to the advisory smoke script.

### Validate the Blood Grove-only candidate

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- validate-blood-grove --output TestResults/balance/blood-grove-local-001
```

The fixed [local validation protocol](../../../Balance%20Harness/Blood-Grove-Local-Validation.md) checks version-12 content: Blood Grove offense 2.421, unchanged regional bonus 2.3 and an explicit 87% Crystal Creek offense transition ceiling. The ceiling changes validation only; the other 13 areas retain their scaling. The command captures candidate/original content (the original copy removes the two local override entries), pins the current policy/recipe and verifies actual seeds against earlier schedules before combat.

Default `--samples 100` declares 3,000 confirmation trials per encounter and 100 per control cell: **21,600 battles** across both content versions and both complete control suites on fresh master seed 518091. Smaller workflow checks use seed 518092; `--samples 1` runs 216 battles. It requires 32 non-Blood-Grove cells to retain gameplay exactly and saves four outcome-independent detailed replays. Use a new output directory. Completion returns 0 even if the nested policy fails or is inconclusive; baseline acceptance is never automatic.

The completed local reference passed all integrity/scope checks, with 89.30% Raven clears (Inconclusive, interval 88.14–90.36%) and 76.73% mixed clears (Pass, interval 75.19–78.21%). No viable baseline was accepted and no samples were appended. All 2,066 backend tests passed. The regional candidate is implemented locally; nothing was deployed. Keep the completed evidence fixed while reviewing gameplay and the larger transition to unchanged Crystal Creek.

### Blood Grove acceptance confirmation

The later [fixed acceptance protocol](../../../Balance%20Harness/Blood-Grove-Acceptance-Confirmation.md) uses the existing `suite` command, unchanged content and policy, and 10,000 trials per encounter on fresh seed 918091. All **20,000 battles** completed: **89.13% Ravens [88.50–89.73%]** and **76.21% mixed [75.37–77.03%]**, both Pass. Both detailed replays match and all **2,112 backend tests pass**. The actual schedule is disjoint from all 223 retained suite schedules; the earlier Inconclusive result was not extended, pooled or granted an exception.

`TestResults/balance/baselines/blood-grove-starter-v1.json` now explicitly accepts that new two-cell confirmation. Keep it with `blood-grove-acceptance-reference` and `retained-builds/blood-grove-acceptance-v1`; the script and frozen protocol are retained with the evidence. The separate Creek baseline stays tied to its earlier executable. No gameplay code, content, policy or CLI implementation changed for this confirmation. The subsequent combined check below verifies both references on one newer build.

### Combined starter regression

[build/check-starter-balance.ps1](../../../build/check-starter-balance.ps1) repeats both accepted starter schedules against one captured local executable/content snapshot. Build and test first; the script uses the compiled Release output and does not rebuild or establish that its source is current:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild
./build/check-starter-balance.ps1 -OutputDirectory TestResults/balance/starter-regression-001
```

Run from the repository root with PowerShell 7 and the supported .NET runtime. Both accepted baseline manifests and complete referenced run directories must exist locally. Defaults use `TestResults/balance/baselines/{blood-grove,crystal-creek}-starter-v1.json`, `LL/src/API/API.LL` for content and the harness's Release `net10.0` output. Override `-BloodGroveBaseline`, `-CrystalCreekBaseline`, `-ContentRoot` or `-HarnessDirectory` for explicitly retained paths. Do not edit an accepted manifest to turn a candidate into a passing reference.

The check captures the compiled executable/dependencies, fifteen combat files, eighteen allowlisted ThreatAndTanking properties and encounter cadence. It copies the saved fixture definitions as exact raw JSON, preserving timestamp offsets and numeric spelling. It validates both accepted archives against the pinned approved policies before any candidate combat, then records the frozen input hashes and plan. Unknown threat settings stop capture for an allowlist review; unrelated application configuration is not copied.

Blood Grove uses seed **918091**, **10,000 trials × two cells**. Creek uses seed **818092**, **2,000 trials × sixteen cells**. The total is **52,000 paired regression battles**, followed by four fixed trial-index-0 detailed replays. These repeated trial identities are not additional independent samples. Both level-5 cells and only the two level-10 Amulet/Goblin Creek cells enforce the existing **50–90% win band**. The remaining fourteen Creek cells are diagnostic; duration has no target.

Retain the entire new output directory: `plan.json`, captured executable/content/fixtures, accepted-baseline evaluations, each checkpoint's candidate/comparison/evaluation/replays, and aggregate `results.json`/`summary.md`. Existing output directories are rejected. Frozen-file changes or incompatible/incomplete evidence stop the check; an in-progress technical failure retains `failure.json`. A completed aggregate policy Pass exits **0**, Fail **1**, and Inconclusive **3**; technical errors exit nonzero. Changed gameplay records/outcomes and post-capture source/settings drift are reported separately from the policy outcome. The script does not promote baselines, add samples, tune content or enable a hosted workflow gate.

The first [completed combined review](../../../Balance%20Harness/Starter-Regression-Review.md) passed all 52,000 fights across eighteen cells with **zero changed gameplay records or outcomes**, four matching replays and **2,119 passing backend tests**. Win rates remain 89.13% / 76.21% in Blood Grove and 74.40% / 55.45% in Creek, with all four intervals inside the band. Evidence is under `TestResults/balance/starter-regression-reference-corrected`, with the tested build under `retained-builds/starter-regression-v1`. The retained first attempt exposed a fixture timestamp-copy error and stopped as Incompatible; the corrected raw-copy script reused its frozen build/content in a new output without pooling the failed attempt's fights. Both accepted baseline manifests remain unchanged. The portable package below now retains these directories; off-device storage is deferred by user choice.

## Package and recover accepted starter evidence

[export-starter-balance.ps1](../../../build/export-starter-balance.ps1), [restore-starter-balance.ps1](../../../build/restore-starter-balance.ps1) and [verify-starter-balance-package.ps1](../../../build/verify-starter-balance-package.ps1) preserve and recover the accepted references independently of the checkout. The [versioned contract](../../../build/starter-balance-package-v1.json) pins reviewed input hashes and includes the complete supporting experiments, failed regression attempt, original baselines and exact retained executables. PowerShell 7.4 or later is required; the archive helper uses its existing .NET libraries.

```powershell
./build/test-balance-evidence.ps1
./build/export-starter-balance.ps1 -ArchivePath TestResults/balance/exports/starter-baselines-001.zip
```

Choose a new output path. Export preserves file bytes and relative baseline links, rechecks every source file before sealing, and emits the ZIP checksum and receipt. Restore requires the expected ZIP hash and rejects existing output, unsafe paths, missing/unexpected files and checksum mismatches. Verification output stays outside the immutable package. See the [complete recovery guide](../../../build/STARTER_BALANCE_PACKAGE.md) for commands, `-IntegrityOnly`, and selecting an isolated original runtime with `-DotnetPath`.

The [9 September recovery review](../../../Balance%20Harness/Starter-Baseline-Package.md) verifies **428,158 payload files** in the **1.59 GB** `TestResults/balance/exports/starter-baselines-v1-corrected.zip`. All four accepted/regression suites pass their existing checks, all eight fixed replays match, and all fifteen archive/command checks pass. The corrected ZIP fixes relative-path handling in four packaging scripts; every game-evidence file remains unchanged. The system had moved to .NET 10.0.12, so full recovery used a separate verified 10.0.11 runtime. No system configuration or accepted archive was changed. These are recovery checks, with no added independent samples or baseline promotion.

The user chose **local storage for now**. Keep the corrected ZIP, checksum/receipt, recovery tools and separate runtime ZIP together under `TestResults/balance/exports`. Off-device storage and hosted integration remain separate work. Historical recovery does not certify later runtime or game changes.

## Level-10 starter handoff to Crystal Creek

[idle-crystal-creek-starter.json](Fixtures/idle-crystal-creek-starter.json) measures the selected Goblin Warrior/Shortsword/Heavy Breastplate at level 10 in both Blood Grove and Crystal Creek. Four fixed preparation steps retain the original equipment, add an Amulet, add Goblin in the newly unlocked second slot, then apply affordable Fury to the sword. The Amulet and second-Essence choices follow the existing cohort convention; chest outcomes are conditional and no random drop or reinforcement budget is assumed. See the [acquisition audit, comparison and playtest checklist](../../../Balance%20Harness/Crystal-Creek-Starter-Handoff.md).

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-handoff --output TestResults/balance/crystal-creek-handoff-001
```

The command captures economic sources and a fixed plan before running all 16 cells on seeds 618091/618092 at 500 trials per cell: **16,000 battles**. `--samples 1` runs 32 workflow battles on separate development seeds 618093/618094. It verifies complete saved evidence, records adjacent preparation steps as paired comparisons within each encounter, and replays eight predeclared confirmation battles. Across-area comparisons are descriptive because species/scenarios differ; ordinary regression compatibility stays unchanged. Both seed sets remain separate, with no outcome-based selection or sample extension. Use a new output directory and retain the complete bundle.

`quest-rewards` is the primary conditional checkpoint. The original investigation was advisory. The subsequent user decision approved a separate 50–90% win-rate policy for its two Crystal Creek cells, with no duration target. The other fourteen cells remain diagnostics. The old Blood Grove reference is not rerun or pooled, and no baseline is promoted. No production content change is part of this investigation.

The completed handoff run records 99.4% / 100% confirmation clears in Crystal Creek with the primary Amulet + Goblin reward build, versus 0% / 0% with the original two-item, single-Essence build at level 10. Both primary encounters fail the subsequently approved upper bound. All eight original replays matched; the acceptance increment passes 108 relevant tests. See the [acceptance review](../../../Balance%20Harness/Starter-Path-Acceptance.md) for local onboarding observations, target evaluation and hosted CI evidence. Evaluate the unchanged saved confirmation independently:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --goals LL/tools/BalanceHarness/Fixtures/idle-crystal-creek-starter-goals.json --run TestResults/balance/crystal-creek-handoff-reference/confirmation --output TestResults/balance/crystal-creek-evaluation-new
```

This absolute-only policy needs no baseline and returns exit 1 for the saved above-band results. It is not an input to the advisory smoke script.

### Bounded Crystal Creek creature-pressure investigation

The [fixed tuning protocol](../../../Balance%20Harness/Crystal-Creek-Tuning-Review.md) tests a 7 × 7 grid of Blue Slime opening Barrier and Frost Imp Ice Needle coefficients, with the existing 16-cell checkpoint and 50–90% policy unchanged. Separate creature ability variants preserve the original player Essence abilities. Regional scaling, equipment and rewards remain fixed; all content changes are confined to new experiment directories.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-creek-pressure --output TestResults/balance/crystal-creek-pressure-new
```

Default `--samples 100` declares 120,000 battles: 78,400 across all discovery candidates, 32,000 for original/selected confirmation, and 9,600 across both original control cohorts. `--samples 1` uses separate development seeds for 1,200 workflow battles; it cannot certify the unchanged policy sample minimums. Selection minimizes the two primary cells' worst distance from 70%, then mean distance and coefficient change, before any confirmation outcomes are read. The other fourteen handoff cells remain diagnostics. No duration target is added.

The command freezes sources/settings/execution and validates all candidate copies before discovery. It rejects incomplete runs, changed inputs, incompatible comparisons and changed non-Creek controls. Four fixed confirmation replays verify both primary encounters for original and selected content. A valid completed investigation returns 0 even if its nested policy fails; inspect `evaluation/candidate/evaluation.md` and `results.json`. Invalid/cancelled investigations retain failure/partial evidence and return 2/130. No production change or passing baseline is automatically promoted.

The completed coarse run selected Barrier 0.28 / Ice Needle 2.4. Confirmation reached **91.6% [89.72–93.16%]** against two Slimes and **50.3% [47.21–53.39%]** against Slime + Imp: both Inconclusive. All controls and replays passed, and no candidate was applied to the game. A finer investigation needs its own frozen protocol and fresh seeds; the completed 120,000-battle run remains closed.

### Fine Crystal Creek confirmation and local content

The separately declared [fine protocol and review](../../../Balance%20Harness/Crystal-Creek-Fine-Tuning-Review.md) adds `investigate-creek-pressure-fine`: 42 candidates crossing Barrier 0.35–0.50 in 0.03 steps with Ice Needle 1.7–2.3 in 0.1 steps. Default `--samples 100` runs **275,200 battles**: 300 discovery trials per handoff cell, 2,000 original/selected confirmation trials, and 100 trials per control cell. Full seeds are 818091/818092; development uses 818093/818095. `--samples 1` runs 2,752 workflow battles and cannot pass the policy sample minimum. The unchanged selection rule freezes the candidate before fresh confirmation.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-creek-pressure-fine --content-root TestResults/balance/crystal-creek-pressure-fine-reference/source-content --output TestResults/balance/crystal-creek-pressure-fine-new --samples 1
```

Both closed pressure protocols now reject an already tuned API content root; use a retained original `source-content` copy for workflow checks. The current generator removes the creature variants' `owningEssenceId` so they cannot become extra player Essence slots or simulator abilities. The fine experiment's original executable/source and evidence predate that metadata correction and remain retained for exact replay.

The full fine run selected **Barrier 0.44 / Ice Needle 1.7** and confirmed **74.40% [72.44–76.26%]** against two Slimes and **55.45% [53.26–57.62%]** against Slime + Imp: both **Pass**. All 32 external controls, eight Blood Grove return cells and four replays matched. A separate compatibility repetition removes the two ownership fields and verifies **36,800 identical gameplay records plus two matching replays**, with the same seeds and no added statistical observations. Those corrected definitions/mappings are now local API content; original player abilities, regional scaling, rewards and settings remain fixed. The ability behavior manifest covers both new variants.

The scoped local regression reference uses the corrected 16-cell confirmation, enforcing only the two primary Creek cells. Blood Grove's later acceptance confirmation has its own passing reference. The separate [combined regression](#combined-starter-regression) verifies both on the retained 8 September build. Keep both ignored baselines with their evidence and matching executables in the verified local package; they are not published or covered by the earlier hosted run. The full natural spawn distribution, other jewelry outcomes and other activity modes remain unmeasured.

## Investigate attainable Blood Grove equipment

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- investigate-entry --output TestResults/balance/blood-grove-entry-001
```

This fixed experiment crosses six First Hunt builds with all nine possible Armor Chest items and plain/Fury weapons: 216 cells, 100 samples each, on discovery seed 1337 and confirmation seed 940031 (43,200 battles). `--samples 1` runs the full matrix with a small sample budget. Each build retains one weapon and one armor item; Fury spends one guaranteed Blueprint and 100 of the 500 starting Cinders. The Lumo Token is retained.

The command derives the production chest candidate list, records the resource ledger and stopping rule before execution, verifies both archived suites, reports paired substitutions, and saves 24 predeclared detailed replays. It rejects reused output directories, incomplete runs, changed content/execution between seed sets and mismatched paired schedules. It does not change regression-comparison compatibility, promote a baseline or approve goals. No new fixture schema is needed; existing style/slot support supplies the builds.

The [entry review](../../../Balance%20Harness/Blood-Grove-Entry-Review.md) records zero wins in every tested cell on both seed sets, survival differences and selected replay diagnostics. Existing fixtures/goals remain unchanged. This is a local investigation; CI continues to run correctness tests and the two small cohort smoke workflows.

## Investigate regional offense pressure

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-pressure --content-root TestResults/balance/blood-grove-local-reference/original-content --output TestResults/balance/blood-grove-pressure-001
```

This historical bounded experiment varies only the gated Shenic profile's `offenseCurve.postTutorialBonus`, while keeping the selected starter recipe and original 65–75% policy fixed. Both pressure commands explicitly use `idle-blood-grove-starter-goals-v1.json` to preserve their declared protocol; evaluate their saved runs with the current goals file separately. The coarse grid preflights 2.3 down to 0 in steps of 0.1; production validation rejects zero with the existing floor, leaving 23 legal values. Each content copy contains only allowlisted data and selected nonsecret combat settings. No production content is edited.

These historical commands now reject content with area overrides, because a fixed Blood Grove multiplier would hide the regional parameter's effect. Supply the retained `original-content` directory from a local validation as above; another checkout must capture its own validation evidence first. Re-execution on a newer engine produces new evidence, not the archived result.

The plan declares 100 discovery trials per encounter on seed 1337, then freezes the closest candidate before original/candidate confirmation at 1,000 trials per encounter on reserved seed 318091. Both full existing control suites run at 100 samples per cell for original/candidate content. Total: 18,200 battles and four preselected detailed replays. Selection minimizes worst distance from 70%, then mean distance, then the size of the change. `--samples 1` runs a 182-battle workflow check with separate confirmation seed 318092. No grid refinement, reselection or sample extension occurs within a run.

The command returns 0 when the complete investigation is valid, even if the nested enforced goal evaluation fails; inspect `results.json`, `summary.md` and `evaluation/candidate`. Invalid/incomplete execution returns 2; cancellation returns 130 with partial evidence. Experimental baseline manifests identify original-content controls, without approving a viable gameplay baseline.

The [pressure review](../../../Balance%20Harness/Blood-Grove-Pressure-Review.md) records the completed run: bonus 0.2 was selected, but confirmation reached 88.4% Raven-pair and 78.6% mixed-pair clears, both above the working band. All 16 Lumo control cells stayed identical; six early Shenic areas changed, including regeneration through the existing scaling rule. The candidate was not promoted. The separate finer experiment below has also completed.

### Fine follow-up

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-pressure-fine --content-root TestResults/balance/blood-grove-local-reference/original-content --output TestResults/balance/blood-grove-pressure-fine-001
```

The separate [fine experiment](../../../Balance%20Harness/Blood-Grove-Fine-Pressure-Review.md) tests 0.30 through 0.20 in steps of 0.01, with 500 discovery trials per encounter on master seed 418091. The unchanged selection rule freezes one candidate before original/candidate confirmation at 1,000 trials per encounter on reserved seed 418092. Both complete control suites run at 100 trials per cell, giving **24,600 battles** and four preselected replays. Preflight checks derived starter seeds against earlier investigations and development checks. Content copies, integrity checks and exit-code semantics are shared with the coarse experiment; its original grid and budget are unchanged.

For this command, `--samples` is the base/control count; discovery uses five times that count and confirmation ten times. `--samples 1` runs 246 battles on separate discovery/confirmation seeds 418093/418094, without consuming the full run's reserved sets. Schema-2 plans record the experiment ID, discovery count and excluded master seeds. Use the default 100 for the declared full experiment; smaller runs only check the workflow.

The fine run selected 0.21: its original-policy confirmation reached 88.5% Raven-pair clears (Fail) and 74.1% mixed-pair clears (Inconclusive because its interval overlaps 75%). All 16 Lumo cells stayed identical, and the same six areas changed. Discovery's sharp jump between 0.22 and 0.21 aligns with Raven basic-attack/Piercing Peck rounding thresholds. Under the revised 50–90% policy, these results become Inconclusive and Pass respectively. Both experiments remain completed historical evidence; the later Blood Grove-only implementation and fresh validation are recorded above.

## Accept a baseline and compare changes

After reviewing a complete run, explicitly record it as a reference with a written reason:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- baseline accept --run TestResults/balance/idle-reference-001 --output TestResults/balance/baselines/idle-v1.json --reason 'Reviewed reference builds and encounter assumptions; advisory comparison reference.'
```

The manifest records the reason, acceptance time, metrics version, accepted scorecard, relative run location and a SHA-256 fingerprint of the evidence. It never overwrites an existing manifest. Acceptance requires a complete run: the reader verifies content/input hashes, recomputes the scorecard from individual observations, and checks each completed observation against its saved battle. The fingerprint pins the input, manifest, JSON/Markdown scorecards, battle index and all battle records; archived content is pinned through its verified manifest hashes. Checksums detect altered evidence but do not authenticate its author or prove gameplay parity.

Retain the original run directory with its baseline manifest. The small manifest can be kept in source control if desired; full bundles remain ignored artifacts. Moving the manifest and its referenced run together preserves the relative link. This command accepts a comparison reference; it does not approve difficulty targets or make an incomplete run acceptable.

Generate a candidate run with the same fixtures and seeds, then compare it:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --output TestResults/balance/idle-candidate-001 --seed 1337
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- compare --baseline TestResults/balance/baselines/idle-v1.json --run TestResults/balance/idle-candidate-001 --output TestResults/balance/idle-comparison-001
```

Comparison reads saved evidence without executing combat or requiring the historical binaries. Replay continues to require the original binary/runtime/platform identity. A new executable or content hash is an expected comparison input and is listed prominently in the report.

The comparison writes `comparison.json` and `comparison.md` in a new output directory. It shows:

- Per-cell baseline/candidate clear rates, gained/lost wins on the same seeds, and clear-rate change in **percentage points**.
- Winning duration change for seeds won in **both** runs, alongside each run's winning median and eligible pair count. Newly won/lost fights affect clear rate rather than being mistaken for faster/slower wins.
- Mean remaining-health change over all paired attempts, including defeats.
- Outcome and gameplay-record change counts. Preparation, terminal state and statistics can change even when headline metrics do not.
- Changed content/assembly hashes and resolved inputs, plus up to three changed-battle examples per cell with saved-record links and replay instructions.

Compatibility is checked per stable cell ID. Changed scenario assumptions, loadout recipes/selections, essence progression, rules/cadence, tick units or trial schedules are non-comparable. Added/removed cells are listed. Reordering cells or trials is supported; partial seed overlap is not used. Derived character coefficients and encounter data can change under the same recipe and are reported explicitly. Runtime/platform differences exclude all matched cells so environmental changes are not mistaken for a controlled content experiment. Valid unchanged cells still compare when other cells are non-comparable or incomplete.

All changes are candidate minus baseline. Clear-rate uncertainty combines two 97.5% Wilson intervals for gained/lost win probabilities using a Bonferroni adjustment, giving a conservative approximate 95% interval for the paired difference. Zero changed wins does not imply zero sampling uncertainty. Duration/health mean intervals use an exploratory normal approximation with at least 30 eligible pairs and nonzero observed variance; otherwise the interval is unavailable with a reason. These intervals are not corrected across cells/metrics. No result is labeled balanced, improved or regressed automatically: easier content may be undesirable, and targets/practical thresholds are still design decisions.

Exit code **0** means a complete advisory comparison, regardless of the measured direction. **2** means an incomplete/non-comparable comparison or invalid evidence; well-formed partial runs retain a report, while corrupt/missing evidence writes `failure.json`. Cancellation returns **130**. There is no automatic promotion, default baseline replacement, resume or CI balance gate.

## Evaluate balance goals

The [default goals file](Fixtures/idle-goals.json) defines six **draft proposals**, expanded into 60 checks across all 12 cells. It proposes 90% minimum clears for ordinary enemies, a 60–90% challenge clear-rate band, a 60-second mean winning duration limit, and tolerances for baseline movement. These are proposed experience goals, not values established by the observed results. The default and First Hunt policies remain `Draft`; the separately selected Blood Grove and Crystal Creek starter policies have reviewed 50–90% enforced bands with no duration target. No balance gate is enabled by default or added to hosted CI.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --run TestResults/balance/idle-candidate-001 --baseline TestResults/balance/baselines/idle-v1.json --output TestResults/balance/idle-evaluation-001
```

Use `--goals <json>` for another policy. `--baseline` is required whenever the selected goals include change metrics; it can be omitted for an absolute-only policy. The evaluator reads verified saved evidence, recomputes any requested baseline comparison, and writes a new directory containing `goals.json`, `evaluation.json`, `evaluation.md`, and the comparison JSON/Markdown when supplied. It preserves the input archives and baseline. The report pins the exact goals hash, evaluator/metrics versions, fixture contract and run fingerprints.

Supported metrics and required units:

| Metric | Unit | Eligible observations |
| --- | --- | --- |
| `ClearRate` | `percent` | Wins / all valid attempts; 95% Wilson interval |
| `WinDurationMean` | `seconds` | Duration of victories only |
| `RemainingHealthMean` | `percent` | Final health fraction × 100, including defeats |
| `ClearRateChange` | `percentage points` | Paired gained/lost wins against the baseline |
| `SharedWinDurationChange` | `seconds` | Duration difference only for seeds won in both runs |
| `RemainingHealthChange` | `percentage points` | Health difference over all paired attempts |

Each goal declares its exact cells, metric/unit, primary/guardrail/diagnostic role, draft/enforced status, minimum eligible sample count, rationale and at least one inclusive `minimum`/`maximum` bound. Each `requiredCells` entry needs a primary goal. Optional `diagnosticCells` declare comparison-only cells without numerical targets: they must be distinct, disjoint from required cells and together cover the exact saved suite. The new Crystal Creek starter policy uses this to enforce its two primary cells while retaining fourteen preparation/return diagnostics. Unknown fields, invalid bounds/units, duplicate goals/cells, undeclared extra cells or missing coverage are rejected. The declaration permits up to 1,000 goals and 100,000 expanded checks.

New run manifests use schema 2 with the exact 15-file catalog allowlist. Historical schema 1 accepts the original 14-file set and the transitional 15-file Combat Styles set; each archived checksum is verified without loading current content. Other sets/versions are rejected, and historical replay still requires the original execution identity.

The `fixtureHash` pins normalized suite recipes, assumptions, encounter selections and starting conditions. Sample count and enumeration order are excluded so a smoke sample is still the same cohort. Code/content coefficient changes can be evaluated under the same recipes. Added/removed cells or a changed cohort are invalid until reviewed; the report provides the actual fixture hash to support an intentional policy update. Changing that hash is a review decision, not an automatic fix.

A three-sample smoke run checks execution, but does not meet the default goals' sample minimums. Its paired comparison also needs a baseline with the same three trials per cell: comparing it with a 100-sample reference is incompatible, even though both have the same fixture hash. Use smoke runs for workflow verification and a predeclared reference sample budget for policy review.

| Check result | Meaning |
| --- | --- |
| `Pass` | The whole interval is inside the inclusive bounds and the sample minimum is met |
| `Fail` | The whole interval is outside a disallowed boundary |
| `Inconclusive` | An interval overlaps a boundary, sample count is too small, or uncertainty is unavailable |
| `Invalid` | Required cells/evidence are missing, simulations are incomplete, or the comparison/cohort is incompatible |

Duration/health means share the comparison's normal-approximation policy: at least 30 nonconstant eligible samples are needed for an interval. A stricter per-goal `minimumSamples` still applies. No victories/shared victories give an inconclusive conditional-duration result. Constant observed differences are not assumed to establish zero population uncertainty. Intervals are evaluated per check without a correction across goals/cells; review this limitation before enforcing many goals. Duration must always be interpreted beside clear rate.

The report separates **assessment** (all findings, including drafts) from **enforcement** (reviewed primary/guardrail checks). A draft failure or inconclusive result remains advisory. To enforce a goal after review, explicitly change that goal to `Enforced` and provide a nonempty `reviewReason`; diagnostics cannot be enforced. Mixed policies show both draft and enforced counts, and an enforcement pass applies only to the latter. The CLI never edits the policy or promotes a baseline.

Evaluation exit codes:

| Code | Meaning |
| --- | --- |
| `0` | Valid advisory evaluation, or all enforced checks pass |
| `1` | At least one enforced check fails |
| `2` | Invalid configuration/evidence/coverage, even when all goals are draft |
| `3` | No enforced failure, but at least one enforced check is inconclusive |
| `130` | Evaluation cancelled |

Invalid takes precedence over fail, which takes precedence over inconclusive. Malformed/corrupt input writes `failure.json`; valid archives with policy/cohort/evidence issues retain an `evaluation.json`/Markdown report marked invalid. Current `suite`/`compare` exit behavior is unchanged. The CI smoke workflow below verifies advisory execution; reviewed gameplay enforcement remains a separate decision.

## Verify the workflow locally and in CI

From the repository root:

```powershell
./build/smoke-balance.ps1 -OutputDirectory TestResults/balance/smoke-001
```

Add `-NoBuild` after building the tool or backend test project. The script runs the reference fixture twice with three samples per cell, creates a disposable same-revision repeatability manifest, checks a complete comparison with zero changed evidence/gameplay, replays a battle with detailed logging, and evaluates all 60 draft checks. It requires a new output directory and writes `summary.md` plus both complete bundles, comparison/evaluation reports and `replay.json`. It does not replace a reviewed baseline or compare with another Git revision.

`-SamplesPerCell 100` runs the same workflow at reference size; `-Seed` and `-Configuration` are also supported. Choose another cohort with `-SuitePath` and its matching `-GoalsPath`. The default control smoke has 72 total battles and 60 inconclusive checks; the First Hunt smoke has 216 total battles and 180 inconclusive checks. Goal sample minimums are preserved. Group cohorts preferentially replay a multi-enemy cell. Draft failures or inconclusive findings do not fail the workflow; execution, integrity, repeatability and replay failures do. The script refuses a policy containing enforced goals so a later promotion cannot silently turn this small advisory sample into a gameplay gate.

[The GitHub Actions workflow](../../../.github/workflows/balance-harness.yml) runs the harness tests through `build/run-tests.ps1` and then smoke workflows for both cohorts on relevant backend/tool pull requests or manual dispatch. It publishes job summaries and retains both cohorts' evidence/test results for seven days, including partial artifacts on failure. The job timeout is 15 minutes, with three minutes for each smoke step. The first hosted execution passed on published main; current local changes require verification after publication. Gameplay targets and branch-protection requirements are not enabled by this change.

## Run a single fight

From the repository root:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- run --output TestResults/balance/starter-001 --seed 1337
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/starter-001
```

The single-fight default remains the level-1 mace/Goblin control against a Goblin in Lumo Ruins. Its `tutorial-starter` profile name is historical; Goblin is not a current First Hunt reward choice. Each run requires a new output directory. Add `--detailed` to `run` or `replay` to capture the event log; suites capture compact telemetry and enable detail on replay. Replay writes JSON to stdout and its completion message to stderr. Complete suites and completed single fights (including defeats/draws) return exit code 0. Invalid inputs, execution errors, incomplete suites or replay mismatches return 2; cancellation returns 130.

`run` also accepts `--content-root <API.LL-directory>` and `--scenario <json>`. The default content root is discovered relative to the built tool. A scenario may use `tutorial-starter` or an explicit `build` matching its `characterProfile` ID. Build recipes use `EquipmentReferenceBuildDefinition`; the shared factory enforces equipment types, hand rules, tier eligibility, essence slots and distinct monster families. The fixed creature must belong to the area and the character must meet its level requirement.

## Saved artifacts

Single-fight and suite runs contain:

| Artifact | Contents |
| --- | --- |
| `suite-input.json` | Suite recipe, assumptions, materialized cell inputs, explicit rules/settings, versioned seed schedule and every battle ID/seed |
| `input.json` (single fight) | Scenario assumptions, materialized character attributes, frozen equipment descriptors, essence progression, authored creature/area, seed, explicit rules and combat settings |
| `manifest.json` | Input/content checksums, hashes of the tool and game assemblies, .NET runtime and platform |
| `content/Data/` | One shared snapshot of 15 allowlisted catalog files in manifest schema 2; historical schema-1 bundles retain their exact 14- or 15-file set |
| `battles/<battle-id>.json` / single-fight `result.json` | Prepared participants, engine/content outcome, ticks and seconds, terminal state, per-entity/ability statistics, compact telemetry, optional event log |
| `battles.jsonl` | Compact outcome/error index, flushed as each battle completes |
| `scorecard.json`, `scorecard.md` | Suite completeness, per-cell metrics, assumptions and replay examples |
| `failure.json` | Preflight, cancellation or bundle failure details when execution cannot complete normally |

Later workflow commands create separate artifacts:

| Command | Artifacts |
| --- | --- |
| `baseline accept` | The requested manifest JSON, pointing to the retained suite bundle and pinning its accepted evidence |
| `compare` | `comparison.json` and `comparison.md` with compatibility, paired changes, uncertainty and battle examples |
| `evaluate` | Frozen `goals.json`, `evaluation.json` and `evaluation.md`; also `comparison.json`/`comparison.md` when a baseline is supplied |

These commands require a new output file or directory and preserve their source bundles. Corrupt or missing comparison/evaluation evidence produces `failure.json` in the new output directory; an invalid report is never a passing balance result.

`TestResults/` is already ignored by Git. Only the required non-secret combat settings are selected from `appsettings.json`; the settings file itself, account data, connection strings, and credentials are not copied. Environment-specific setting overrides are not applied.

Replay uses the saved materialized inputs and archived content, without consulting the current API content root. It rejects changed inputs/content or a different runtime/platform/game/tool binary identity. Preserve the original checkout/build if a run must remain replayable; the bundle fingerprints executable code but does not archive source patches or binaries. Debug and Release builds are different replay identities.

If a saved result exists, replay compares preparation and gameplay summaries, allowing event logging to differ. Failures after the input and manifest have been saved can be re-executed; failures before that point contain diagnostics but not a complete replay bundle. Suite cancellation during execution writes a partial scorecard and preserves completed battles. Replaying a failed/unexecuted trial does not rewrite its original status or scorecard. There is no resume command.

## Gameplay parity

The harness uses `CombatPreparationPipeline`, `CombatSetupService`, the authored region scaling provider, `CombatEngineExecutor.ExecuteSimulationAsync`, and the ordinary encounter result factory. Normal idle rules are explicit: 6,000 maximum ticks, 30-tick base attack interval, active abilities starting on cooldown, and compact telemetry enabled. The engine supplies the tick rate.

Small production seams make the file-only composition possible:

- `EssenceCombatLoadoutFactory` contains the existing selected-loadout calculation; `EssenceSystemService` delegates to it.
- `CombatPreparationPipeline` accepts live-only construction without a persisted-snapshot builder. Snapshot requests still require that dependency.
- `SnapshotCombatantBuilder.BuildFromItemBases` shares the existing rehydration body between normal database-backed snapshots and the offline Tower catalog lookup.
- `EquipmentReferenceBuildFactory` optionally permits incomplete equipment, preserving full-loadout validation for existing callers. Both recipes and frozen inputs use its slot validation; materialized two-handed weapons share a single instance across both hands.

`BalanceHarnessTests` rebuilds equivalent sources independently, then uses `IdleCombatResolutionSessionFactory` and the normal executor path to compare preparation, outcomes, duration, terminal state and statistics for multiple seeds and progression builds, including armor and two-handed/multiple-essence cases. It also checks repeated/concurrent execution, compact versus detailed logging, replay integrity, invalid inputs, and sensitivity to incorrect opening cooldowns. `BalanceHarnessSuiteTests` covers matrix expansion, paired seeds, legal partial gear, statistical calculations, victory/defeat replay and partial cancellation artifacts. `BalanceHarnessComparisonTests` checks baseline immutability, archived-build comparison, known paired statistics, win-duration selection, incompatible/partial runs, corrupted evidence, and an actual enemy-offense change applied only to temporary content.

`BalanceHarnessGoalTests` covers inclusive and one-sided bounds, zero-win numerical endpoints, small samples, fixture/policy validation, draft versus reviewed enforcement, distinct exit codes, missing/incompatible baseline evidence and immutable evaluation artifacts.

`BalanceHarnessFirstHuntTests` covers the authored starter choices, legal quest-reward budgets, 36-cell/180-check contract, independent duplicate enemies, group validation, archived group replay/comparison/evaluation and unchanged legacy hashes. Production parity cases also cover duplicate and mixed enemy groups with the actual First Hunt Essences.

`BalanceHarnessProgressionTests` verifies training recipes and frozen progression. `BalanceHarnessEntryTests` verifies the armor/style matrix, production Forge-quote equivalence, reward budgets, complete experiment/replay evidence, paired schedules, output preservation and cancellation. Two additional `BalanceHarnessTests` parity cases cover the selected Goblin Warrior/Shortsword/Heavy Breastplate reference against both Blood Grove pairs. `BalanceHarnessGoalTests` pins the current v2 policy and checks uncertainty inside and outside the 50–90% band. `BalanceHarnessPressureTests` covers legal overlays, source/settings isolation, selection before confirmation, both complete coarse/fine workflows, historical v1 policy retention, fine-grid budgets and seed separation, distinct bonus labels and cancellation. The preceding fine-sweep increment passed 85 tests across eight classes. Verification of the revised policy tests was blocked by concurrent combat-style compilation errors, as recorded in the band revision review above.

Run relevant backend verification through the repository script:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentHandRuleTests'
```

Shared combat/preparation changes also warrant the full `./build/run-tests.ps1` suite.

## Scope

Each trial resolves a fresh single fight. The suite conditions on specific spawns; it does not estimate an area's overall win rate. It excludes spawn-distribution sampling, offline time progression, rewards, account persistence, and multi-encounter carryover. Runs execute sequentially with a fresh executor and mutable combat state per battle. Elapsed wall time is recorded but is not a controlled performance benchmark.

Explicit baseline acceptance, paired comparison, goal evaluation and advisory CI for both original cohorts are available. The first hosted workflow passed on published `main`; current local changes require their own hosted check after publication. The First Hunt cohort improves starter/reward/encounter coverage. Blood Grove and Crystal Creek have scoped passing local references, a repeatable combined regression check on the retained 8 September build, and a verified portable local recovery package. Off-device storage is deferred by user choice. Hosted integration, broader progression coverage, additional content adapters and rankings remain open; other numerical goals remain draft. A completed suite establishes reproducible measurements, not balance acceptance.

No database, running API, hosted workers, migrations, deployment, or production configuration changes are required. The tool adds a `Microsoft.Extensions.Configuration` dependency matching the existing backend's 10.0.5 version.

## Combat Styles

An idle scenario may include `combatStyle` with `id`, individual `level` (0–10), optional `refinementId`, `upgradeIds`, and `masteredUpgradeId`. Conduit channels the first Essence in the build's ordered `essenceIds`; put the desired Channeled Essence first. A legacy `focusEssenceDefinitionId` may only assert that same first Essence and cannot override it. A suite stage may set `combatStyles`, a map from its build IDs to those same selections. Omitting these fields preserves the no-style control. Existing equipment `activeStyleId` / `useNativeStyle` fields retain their equipment meaning.

Selections use the shared content validator and actual progressed active abilities for Channeled Essence eligibility. Frozen character inputs contain the versioned effective Combat Style and deterministic owned Channeled Essence identity. Battle summaries retain `combatStyles` even with detailed logs disabled. Replays reject changed configuration or tuning that disagrees with the saved scenario/content.

Current code uses `Channeled*` names. The immutable recipe, snapshot, tuning, and diagnostic JSON keep their historical `focus*` members and property order so existing reports retain their identity. Content version `combat-styles.v6` uses Channeled Essence in new combat logs; captured versions v1–v5 retain their original log wording for replay. This terminology change does not alter tuning or the first-slot rule.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/combat-styles.json --samples 3 --seed 1337 --output TestResults/balance/combat-styles-new-run
./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyleHarness|FullyQualifiedName~CombatStyleEngine'
```

The [initial Combat Styles evidence](../../../docs/combat-styles/verification.md) records the fixture's scope, results, undercharged losses and provisional individual progression timing. It does not accept final balance targets.
