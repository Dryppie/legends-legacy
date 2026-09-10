# Controller and whole-party deployment search — 10 September 2026

This increment extends the [progression search](Tower-Party-Progression-Review.md) to all five first-cell roles and explicit deployment choices for later groups. It stays in the offline BalanceHarness and local Tower Lab. Production Tower preparation, combat and outcome interpretation remain unchanged. Phase 2 stays deferred.

## Predeclared experiment

All 15 floors and legal RequiredSlots are retained. The seven 4–10-slot cohorts use the exact existing level, Uncommon quality/tier/rank budgets: 4 = level 30 Standard/1/1; 5 = level 40 Standard/1/2; 6 = level 50 Standard/2/2; 7 = level 60 Fine/2/3; 8 = level 70 Fine/2/3; 9 = level 80 Fine/2/4; 10 = level 90 Fine/2/4. These are separate provisional budgets, not an isolated slot-count experiment. Essences remain level 1, unascended/unevolved, with hypothetical ownership and no Combat Styles.

`tower-whole-party-controls.json` retains every finalist from each preceding progression cohort, with the preceding browser study supplying the four-slot controls. Source report hashes are recorded. The six-slot cohort has six controls; every other cohort has four. Their four searched recipes stay unchanged and the authored Controller is added explicitly. Every retained party is confirmed again on the new schedule, regardless of discovery performance. The fixture excludes 4,653 recorded historical combat seeds, including the previous completed and cancelled dashboard jobs. Earlier evidence is preserved.

Each of Guardian, Restorer, Striker 1, Striker 2 and Controller receives eight candidates per method/seed, tested with both fixed ally contexts on every floor. Both methods get the same control and legal historical starts, with room for fresh exploration. The Controller starts include a reordered control and previous Restorer/Striker hypotheses; role labels do not impose class restrictions. Runtime identities, equipment and levels stay fixed across alternatives.

For each method and generation seed, form a five-role group from that arm's best character candidates. Compare three explicit policies:

- **First cell:** change the first five characters, retaining authored later allies.
- **Repeat:** use that five-role group's recipes in every required group.
- **Alternating:** alternate that group with the other method's group from the same generation seed. Guardian/Restorer/two Strikers/Controller positions stay unchanged; this tests a proposed combination, not assumed synergy.

The joint pool also includes every old finalist, single-character alternatives and bounded combinations of the new shortlists. Schema 3 retains the existing minimum priority-floor gain objective, followed by minimum all-floor gain, lower guardian health, higher survival and stable ID. Priority remains floor 1 for four/five slots and floor 10 for six–ten. Wilson 95% is descriptive uncertainty only.

Before confirmation, freeze every old control, the best discovery deployment for each method/seed, and the complete three-policy family with the strongest discovery result. Fill remaining places by ranking with a reserved specialist when available. This preserves a matched deployment comparison even when one variant performs poorly. Deduplicated recipes retain their first source label; `deployment-comparisons.json` maps every method/seed/policy to the actual party ID, confirmation status and paired gained/lost counts against its first-cell variant. There is no confirmation-driven reselection.

| Study | Search seeds per method | Character samples per floor/context | Joint candidates / samples | Finalists / confirmation samples | Maximum fights |
| --- | ---: | ---: | --- | --- | ---: |
| Six slots first | 3 | 1 | 32 / 1 | 16 / 20 | 17,760 |
| Each of 4, 5, 7, 8, 9, 10 | 2 | 1 | 24 / 1 | 12 / 10 | 9,120 |

The batch cap is **72,480 fights**, declared before any simulation. All seven definitions and disjoint stage/cohort schedules are written to `batch-plan.json` first. Fixed masters are `202609210 + slotCount`, derived through `tower-party-search-v3`. The dashboard's six-slot coverage option retains all six controls with fourteen finalists and costs at most 9,720 fights; other coverage cohorts cost 9,120. Thorough sampling costs at most 17,760 for any cohort. Search-seed replication increases over the preceding six-slot and five-/seven–ten-slot studies; repeated seeds within a study remain paired comparisons, not independent samples.

Full-party deployments replace both authored contexts identically. Those duplicated trials are retained explicitly to compare against each context's own old controls, and must not be pooled or claimed as contrasting-context robustness. First-cell-only parties still retain different later allies on larger floors. Alternating candidates blend two search methods, so their success cannot establish a pure method advantage.

## Results

The complete predeclared batch ran **72,480 fights**: 36,000 character-discovery fights, 5,280 joint-screening fights and 31,200 confirmation fights. It screened 176 joint candidates and confirmed 88 frozen parties, including all 30 retained controls. Nine complete deployment trios reached confirmation across the seven cohorts. No confirmation results were used to change this selection.

| Slots | All-floor trials per context | Retained wins: original / alternative | New wins: original / alternative | New floor-10 wins | New floor-15 wins | Floors no finalist cleared |
| --- | ---: | --- | --- | --- | --- | --- |
| 4 | 150 | 13–51 / 13–54 | 32–46 / 34–46 | 0/10 | 0/10 | 7, 9, 10, 12, 13, 14, 15 |
| 5 | 150 | 69–88 / 70–90 | 80–89 / 81–89 | 0/10 | 0/10 | 7, 10, 12, 13, 14, 15 |
| 6 | 300 | 200–207 / 200–212 | 200–220 / 200–220 | 20/20 | 0/20 | 12, 13, 14, 15 |
| 7 | 150 | 104–110 / 105–110 | 109–110 / 108–110 | 10/10 | 0/10 | 12, 13, 14, 15 |
| 8 | 150 | 109–133 / 113–143 | 121–149 / 134–149 | 10/10 | 0–9/10 | None |
| 9 | 150 | 111–133 / 130–143 | 132–143 / 136–143 | 10/10 | 0–4/10 | None |
| 10 | 150 | 127–150 / 144–150 | 150 / 150 | 10/10 | 10/10 | None |

Ranges include every frozen party in that category; floor columns include both contexts. Totals describe the fixed fifteen-floor matrix. The budgets, generation seeds and candidate sets differ between slot cohorts, so their totals do not measure the isolated effect of adding an Essence slot. Full-party context repeats are never pooled.

The six-slot study completed **17,760 fights**, and its archive reconstructed without differences. Eight detailed replays matched, and twenty exported floor-10 fights reproduced exactly through normal Tower execution. Every finalist cleared floor 10 in **20/20 per context**. No finalist cleared floors 12–15 under this provisional budget.

The frozen `method-guided-1726684006` trio provides a direct deployment comparison. Its first-cell variant `5e3ddd50c15e` cleared floor 11 in **16/20 with original allies and 19/20 with alternative allies**. Its repeated variant `097a5be24985` and alternating variant `d340c925dc73` each cleared **20/20**. All three retained full floor-1–10 clears, so original-context all-floor wins increased from 216/300 to 220/300. Repeated and alternating policies tied here. This is a small, selected comparison, not proof of a general deployment advantage or an optimal loadout.

The four-slot batch study completed **9,120 fights**. Its strongest retained original-context finalist won **51/150 all-floor trials**, while the strongest new finalist won **46/150**. All eight new finalists won **10/10 on floor 1**, versus the authored control's 7/10. New search did not exceed the retained four-slot best on the all-floor total; those controls remain part of the output. A newer or more complex party is not automatically a better recommendation. These points describe the complete frozen finalist set and do not trigger promotion or reselection.

Five-slot results were mixed: the best new all-floor total was 89/150 in both contexts, compared with retained maxima of 88/150 original and 90/150 alternative. At seven slots, the best new and retained totals tied at 110/150. These experiments do not establish a dependable improvement in those cohorts.

At eight slots, the best new finalist reached 149/150 all-floor wins in both contexts, including 9/10 on floor 15. Retained maxima were 133/150 original and 143/150 alternative, with floor-15 results of 1/10 and 3/10 for that retained party. The matched `method-random-562173658` family isolates deployment: first-cell-only won 121/150 original and 134/150 alternative, while repeat won 147/150 and alternating won 145/150 in both contexts. Its floor-15 result rose from 0/10 to 7/10 under either full-party policy. The best observed candidate and the matched deployment comparison are separate results; neither makes a universal build or method recommendation.

Nine-slot new finalists reached 143/150 in both contexts, improving on the retained original maximum of 133/150 while tying the alternative maximum. Floor-15 results remained mixed at 0–4/10. All eight new ten-slot finalists won 150/150 per context, clearing each floor in 10/10, but the best retained party already matched that total. These saturated results do not establish an advantage over that retained party or guarantee future victories.

## Implementation and verification

`TowerWholeParty.cs` owns schema-3 controls, five-role discovery, deployment generation/selection, reports and the batch command. Existing selection/execution/archive reconstruction dispatch by schema; schemas 1 and 2 retain their serialization and selection behavior. Reports, frozen recipes and detailed replay use the existing archive and production Tower adapter.

Tower Lab exposes whole-party search by default while retaining the earlier four-character scope. Preview includes the real search-seed count, confirmation samples and fight cap. Saved builds identify group and role, and a deployment comparison disclosure shows which variants reached confirmation. Successful result loading clears stale errors from an earlier incomplete run.

The new backend tests cover all 4–10-slot budgets across every floor, five-role mappings, fixed gear/identities, historical controls and seed/budget validation, six-/ten-slot complete archive reconstruction, deployment-report tamper rejection, export-compatible replay, dashboard scope preview, and independent normal-Tower parity for repeated/alternating groups on floors 1/10/15. Backend verification uses `build/run-tests.ps1`.

**161 Tower tests passed** through `build/run-tests.ps1 -Configuration TowerWholePartyVerified -Filter 'FullyQualifiedName~BalanceHarnessTower'` (6 minutes 7 seconds). New checks include six independent persisted normal-Tower parity cases for repeated/alternating deployments. The initial sandbox run could not read the existing NuGet configuration; the authorized runner completed outside that restriction. Tests caught and prompted fixes for the six-slot coverage allowance and cross-context cache reuse; both were corrected before the recorded measurement batch. No production code was changed to make the tests pass.

Every cohort passed complete archive reconstruction. **58 detailed replays** matched, including Controller candidates, discovery/confirmation records, full-party floors 1/10/15, available draw cases and exported-run replay. **80 normal-Tower exported floor-10 fights** reproduced their original reports exactly. The combined audit confirms that historical recipes remain unchanged, all controls reached confirmation, full-party context duplicates agree, and deployment does not change the unchanged first group on floor 1. The seven core study archives contain 96,348 files totaling 1,974,519,083 bytes.

An isolated headless Chrome browser completed a **9,120-fight four-slot whole-party study** through the rendered controls. It verified scope switching, actual cost, partial cancellation, saved results, all twelve frozen parties, four deployment families, a full fifteen-character floor-15 recipe export, JSON report download and matching detailed replay, with no page errors. A relative-URL error in the test downloader was corrected and verification resumed against the same completed archive; the simulation was not rerun. Setup and build screenshots were retained. The new dashboard runs on port 5095; earlier dashboards remain available.

All **60,394 checksum-listed files** in the preceding progression package remain unchanged. The new reader also reconstructed the earlier schema-2 six-slot study's **15,840 archived fights** without differences. An independent schedule audit matched the producing executable and found zero overlap across nine declared plans: seven cohorts, the completed browser study and its cancelled predecessor, totaling 1,770 reserved seeds against 4,653 historical exclusions. These schedules are paired within each study, not independent repetitions of the same outcomes.

Concurrent work changed the Combat Styles catalog after the six-slot snapshot, producing three subsequent catalog versions across the remaining cohorts. Each archive preserves its own exact content, and the producing assemblies remain frozen. The scope audit permits only this explicitly identified catalog difference; all other content, combat settings and execution fingerprints match. These builds select no Combat Styles. An additional **900 normal-Tower fights across all 15 floors** reproduced the six-slot archived reports exactly: 300 for each changed catalog version. This is a recorded cross-cohort scope difference, not a claim that the raw scope hashes match. Concurrent unrelated edits are recorded and left intact.

The batch snapshots content per cohort. When running alongside content edits, use a separate frozen content root to avoid changes between cohorts; archive verification still uses each run's own snapshot. This experiment's catalog difference is audited above, rather than silently discarded.

Evidence is saved under `TestResults/balance/tower-whole-party-20260910/`: `studies/` contains the immutable per-cohort Markdown/JSON reports, seeds, recipes and battles; `verification/` contains replay/export comparisons; `results-summary.json` and `retained-control-comparisons.json` contain the combined descriptive and paired results. The exact producing executable and source, test log/TRX, browser evidence, audit scripts, final documentation and SHA-256 manifest are retained separately. Initial failed test builds remain labeled diagnostics. The dashboard exposes all seven completed cohorts at `http://127.0.0.1:5095/`.

The completed package was sealed with **111,331 checksum-listed files**. The SHA-256 of `checksums.json` is `0A9FB683AFC6F793F9BF3BB9B6E94944114E52A9496F5FB0DAFB8A510B9C4813`. The manifest excludes itself and the operational dashboard/seal logs, as recorded in `seal-policy.json`. Final checks matched the sealed hashes of the combined results, executable, key documentation/source, content/seed audits and test TRX. Live repository documentation can advance; the package's `documentation/` remains the frozen copy from sealing. This documentation follow-up does not modify or reseal that evidence.

To reconstruct a saved study with its producing executable:

```powershell
dotnet TestResults/balance/tower-whole-party-20260910/executable/BalanceHarness.dll tower-party-verify --run TestResults/balance/tower-whole-party-20260910/studies/slots-6
```

## Remaining limits

This is a bounded whole-party search using five-role prototypes and three deployment policies. It does not independently optimize every later character, change composition, adapt builds during a fight, optimize ownership/acquisition/training, or certify optimality. Character shortlists are discovered against fixed allies; joint deployment tests their combinations but does not iteratively update those discovery contexts. Small discovery samples and broad candidate selection remain noisy; final intervals are nominal pointwise estimates without multiplicity correction. No method-superiority or unseen-encounter claim follows automatically.

The [next implementation checklist](Essence-Loadout-Search-Plan.md#next-implementation-joint-refinement-of-retained-parties) refines complete strong retained four-/five-slot parties jointly, with more discovery samples and fresh frozen confirmation, rather than assuming independently selected character winners form the best team. Preserve the current controls and budgets. This follow-up is planned, not implemented. Later-character optimization and iterative ally contexts remain separate implementation work; these results do not justify automatic Tower tuning.

No production content, gear, bosses or combat rules are tuned. Accepted starter evidence, earlier Tower evidence and unrelated working-tree changes remain outside this increment. There are no migrations, production configuration changes, shared-database writes or deployments.
