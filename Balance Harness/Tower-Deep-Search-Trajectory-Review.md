# Deep search trajectory and control-recipe comparison

15 September 2026. **Completed using saved evidence only: zero fights, replays, generated candidates or fresh seed values.** This analysis compares both viable controls with all 4,608 candidates from the [closed deep challenger study](Tower-Deep-Challenger-Measurement-Review.md), reconstructs its recorded selection and module-library decisions, and identifies specific coverage gaps. It establishes no new balance acceptance or search improvement.

## What the evidence establishes

**There is no demonstrated pool, identity or shortlist defect explaining this result.** Both control recipes satisfy the frozen allowed-essence, family, fixed-template and copy constraints. Every control ingredient appears somewhere in the generated candidates. All **33 candidates with at least one discovery win** reached screening; the original and screened nominees were preserved exactly. All **1,372 recorded module-library hashes**, **3,488 recorded module uses** and **5,033 earlier-parent references** agree with the saved data and frozen source rules. No external-control reference entered generation.

The gap is in exploring and assembling whole loadouts, with sparse feedback. The controls' common four-essence combination appeared in **1,278 candidates**, including **777** with it on at least six owners. Yet **none of the 4,608 candidates** reproduced either complete control recipe or any of the controls' **12 exact ordered individual loadouts**. Ignoring ability order, **105 candidates** contained at least one of the controls' ten distinct five-essence sets. Every one of those 105 recorded 0/8 discovery wins and none reached the top-32 screens. Their unknown confirmation performance cannot be inferred from recipe similarity.

This does **not** show that the right ingredients guarantee viability, that ordering is the cause, or that those 105 would win after more trials. It shows where exact coverage is missing and where promising-looking components received little further exploration.

## Fixed scope and the identity check

These counts refer to the captured floor-5 scenario: **ten party slots, each with five Essence slots**, for 50 equipped positions. They describe this frozen fixture, not a claim about every party mode or current live configuration. Allowed pool: 80 Essence definitions; no per-Essence owned-copy cap is set in this definition. Acquisition feasibility remains outside the study.

The first saved-data check stopped after 0.125 seconds because it compared complete controls with raw templates. Raw templates have null `identityEssenceIds`; the frozen `TowerBossDiscovery.Scenario` sets `neutral-identity-slot-1` through `-5` on every generated character. All **109 saved control, shortlisted and selected scenario entries** match the materialized fixed template. The difference was in the audit's comparison boundary, not evidence of a combat-input mismatch. The failed check and its original source remain preserved; the corrected analysis uses the documented scenario construction. No gameplay executable, archive or seed schedule was changed or rerun.

The controls carry Pack Howler on ten owners, Enchanted Fairy and Venomous Spiderling on nine each, and Spider Queen: Royal Venom on eight each. The four occur together on seven owners in the strongest control and six in the other. For this analysis, “shared combination” means only the ingredients occurring on >=8/10 owners in both controls. That definition uses control structure, not candidate outcomes; it is descriptive, not a causal model.

| Root | Initial candidates with shared combination on any owner | Later candidates with it | Candidates with a discovery win | Screened teams with it |
| --- | ---: | ---: | ---: | ---: |
| 1 | 0/384 | 641/1,152 | 11 | 32/32 |
| 2 | 1/384 | 10/1,152 | 0 | 0/32 |
| 3 | 0/384 | 626/1,152 | 22 | 32/32 |

All 1,152 initial candidates across the three roots scored 0/8. Later search lowered median remaining guardian health from approximately 93.7% to 63.3%, 67.1% and 63.0% by root, so it learned to damage the boss more effectively. That is a within-search description, not a causal estimate of adaptive search versus random sampling. Two roots developed the full four-essence combination frequently. Root two converged on fairy/howler/spiderling combinations without Royal Venom in its final four leaders and recorded no wins.

## The missing loadout detail

The strongest control puts Bark Golem alongside the shared four on three owners and Elder Treant: Thornstorm alongside them on two; the second control uses two and three owners respectively. The strongest new challenger instead puts the shared four with Web Weaver Spider on nine owners and Hollow Stag on one. It won 7/256, compared with the controls' 63/256 and 47/256. Those fights were already completed and are not pooled or rerun here.

The table ignores Essence order to track whether the same five ingredients were ever assembled. Counts are candidate observations, not independent combat estimates.

| Fifth Essence alongside the shared four | Owners in strongest / second control | Generated candidates containing this set | Library-presence events | Recorded source uses | Screened candidates |
| --- | ---: | ---: | ---: | ---: | ---: |
| poisonous_rat | 1 / 1 | 11 | 395 | 4 | 0 |
| bark_golem | 3 / 2 | 2 | 100 | 1 | 0 |
| elder_treant_thornstorm | 2 / 3 | 2 | 118 | 0 | 0 |
| wood_nymph | 1 / 0 | 1 | 13 | 0 | 0 |

“Library-presence events” counts recorded coordinated-loadout proposals whose reconstructed 128-entry library contained any ordering of the set. It is not a count of donor draws: refinement and placement also record libraries. “Recorded source uses” includes coordinated proposal traces, including rejected/duplicate proposals and refinement's pre-change source; it does not mean the resulting loadout was evaluated unchanged. Exact ordered control modules were never present.

The Bark Golem set appeared in two root-three candidates at three owner positions total, in one generated ordering; it was available at 100 recorded library events and used once. The Thornstorm set appeared in two candidates, one each from roots one and three, in two orderings; it was available at 118 recorded library events and had no recorded source use. These observations rule out the simple account that neither set could enter the library. They do not show that broader retention, more reuse or another ordering would improve combat.

Other control-like sets were reused: the Hobgoblin Brutal Charge / fairy / howler / spiderling / Web Weaver set occurred in 89 candidates and had 15 recorded source uses, while the Poisonous Rat shared-core set occurred in 11 and had four uses. All scored zero discovery wins. The search did not uniformly ignore every control-like component.

## Ranking and nomination behavior

The frozen discovery ranking is wins first, then lower remaining guardian health, higher survival, shorter victory duration and deterministic ID. Each root's four leading parents and its ranked 128-module library therefore depend heavily on eight-trial observations. The analysis reconstructed chronological libraries from completed prefixes; future proposals and control outcomes cannot enter them.

The root-one original leader scored 1/8, then 0/64 and 0/256. Screening moved its primary to discovery rank 7, which scored 1/8, then 2/64 and 7/256. Root three moved its primary to discovery rank 27, a 0/8 candidate that scored 4/64 and 5/256. Its original leader scored 2/8, then 0/64 and 5/256. Screening thus did rescue a zero-win discovery candidate, but none of these nominees became viable. Root two's nominees remained zero throughout.

Saved evidence supports **sparse feedback and limited exploration of complete loadout alternatives** as hypotheses. It does not isolate their relative contributions, establish a new ranking bug, or determine the strength of unconfirmed candidates. A precise ingredient deficit is also not a gameplay distance: even the nearest candidate differs from the strongest control by at least 11 ingredient replacements when owner and order information is discarded.

## What to do with this result

Do not launch another broad unchanged search on the strength of this audit. Do not inject the controls or hardcode Bark Golem/Thornstorm into independent generation. A useful next engineering target is a **bounded diagnostic that separates whole-loadout composition from ability order**, using a frozen small selection and explicitly reporting that control-derived probes measure sensitivity, not independent discovery recovery. Its implementation and zero-combat preparation should come before any new combat authorization.

Simply repeating broader retention or more leader feedback is not a new solution: the earlier [v15 retention comparison](Tower-Loadout-Retention-Review.md) and [v14 feedback comparison](Tower-Generation-Feedback-Review.md) both failed 0/3. Those studies used earlier scopes and do not prove the mechanisms can never help, but they prevent treating either suggestion as an untested guaranteed fix. This audit defines the coverage gap; it does not select or implement a replacement optimizer.

Historical portfolio reliability remains **Fail 1/3**, the closed deep study's recovery remains **0/3**, and adoption remains **Hold**. Original v19 stays **Unresolved with 253 retained recipes and no internal confirmation**. The earlier focused-family Pass does not certify its 42,890 excluded retained teams. All **482,222 seed reservations**, including unused original512 and unused32, remain unchanged; this process never derives or allocates a seed.

## Verification, commands and preservation

The corrected saved-data analysis took **2.485 seconds**; the independent recount took **4.250 seconds**. The initial failed comparison took **0.125 seconds** and remains charged. Before publication, elapsed work plus the declared 180-second preparation allowance was **14.50/30 minutes**, with **10.20/64 MiB** of new output. The 64 MiB local cap is below the original 4 GiB limit; no predecessor cap was increased. Final timing and byte receipts are in `completion.json`.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$audit = 'TestResults/balance/tower-deep-search-trajectory-audit-20260915'
# The initial raw-template comparison is preserved separately as a failed check.
# The corrected analysis and independent verification below use only saved JSON.
& $py -B "$audit/analyze.py"
& $py -B "$audit/verify.py"
& $py -B "$audit/publish.py"
```

These are the producing commands, not instructions to overwrite this sealed directory. Reproduction requires a separate output location and the exact pinned input files. `analysis.json`, `candidate-features.json` and `verification.json` provide per-candidate results, distances, control modules, roots, parent reuse, nomination counts and verification details. The manifest includes the failed check, corrected source, receipts, report copy and active-document copies. All pinned source/evidence files were hashed before and after analysis; the completed study and preparation packages remain sealed.

Only this review, seven active Markdown handoffs and a separate evidence directory are added or updated. The pre-edit inventory compared 4,392 files; concurrent changes are recorded in `checkout-observations.json` and left untouched. There are no production code edits, so backend tests through `build/run-tests.ps1` were not rerun; the previous 91+3 focused cases remain prior evidence. Relevant verification here is the independent saved-record recount, source/input hashes and scoped `git diff --check`. All planned analysis questions were completed after correcting the initial template comparison. No migrations, configuration changes, gameplay changes or deployment.
