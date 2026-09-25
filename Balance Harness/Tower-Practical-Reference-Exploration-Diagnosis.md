# Reference exploration: saved-search diagnosis

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **The exploration operator ran successfully, but its candidates usually lost at discovery ranking and always lost at final selection.** All 122 direct proposals were legal, distinct and evaluated on their first construction check. Of these, 114 fell below the nomination win cutoff, one tied the cutoff but lost on the remaining discovery ranking, and seven reached selection. None was selected. This read-only review explains the closed [twelve-pair comparison](Tower-Practical-Reference-Exploration-Comparison-Execution.md); it adds no fights, values, candidates or new policy-performance claim.

The recommended correction is now [implemented and mechanically verified](Tower-Practical-Reference-Exploration-Offset-Implementation.md) as a new opt-in policy with root-dependent, reference-specific initial owner offsets. The original schedule repeatedly omitted slots 8–10 of reference `96b94357…` across all twelve roots. The 325-case regression verified the coverage behavior and preservation of the original policy. The [offset strength comparison](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) subsequently failed all promotion gates. The [saved-stage review](Tower-Practical-Search-Stage-Review.md) now recommends investigating fresh screening before final nomination; the offset experiment remains closed.

## Where the paths converged

The policies did generate different candidate sets in all twelve pairs, sharing only **22–36 of 46 evaluated recipes** per pair. Six pairs nevertheless produced the same five nominees in the same order. Five further pairs changed nominees but selected the same output. Pair 9 alone changed its selected output.

| Pair | Shared evaluated recipes | Direct exploration evaluated | Exploration descendants evaluated | Direct exploration nominated | Same nominees? | Same output? |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| 1 | 34 | 10 | 2 | 0 | Yes | Yes |
| 2 | 33 | 10 | 1 | 2 | No | Yes |
| 3 | 35 | 11 | 0 | 0 | Yes | Yes |
| 4 | 36 | 10 | 0 | 0 | Yes | Yes |
| 5 | 26 | 10 | 8 | 0 | No | Yes |
| 6 | 34 | 10 | 1 | 0 | Yes | Yes |
| 7 | 22 | 10 | 9 | 1 | No | Yes |
| 8 | 36 | 10 | 0 | 0 | Yes | Yes |
| 9 | 25 | 9 | 11 | 2 | No | No |
| 10 | 34 | 10 | 2 | 1 | No | Yes |
| 11 | 23 | 11 | 3 | 1 | No | Yes |
| 12 | 35 | 11 | 0 | 0 | Yes | Yes |

The nomination rule was reconstructed as the three protected references plus the two highest-ranked challengers, ordered by discovery rank. The selector was independently reconstructed from the saved selection panels. **All 24 searches had a unique maximum selection-win count.** The incumbent tie preference never decided an output, so changing that tie rule alone would not change these saved decisions.

## Construction, ranking and inherited influence

| Stage | Direct exploration outcome |
| --- | ---: |
| Scheduled opportunities | 122 |
| Construction checks per opportunity | Exactly 1 for all 122 |
| Legal, distinct recipes evaluated | 122 |
| Lower discovery win count than second-ranked challenger | 114 |
| Equal cutoff win count, excluded by remaining discovery rank | 1 |
| Nominated for 32-trial selection | 7 |
| Selected output | 0 |

The excluded win-count tie occurred in pair 5. That exploration recipe ranked third among challengers. Consequently, secondary ranking explains only one of the 115 direct nomination exclusions; changing discovery tie handling alone cannot explain the broader result. These eight-trial discovery observations are training feedback reused by an adaptive search. A lower discovery count does not establish that an excluded recipe is truly weaker, and the archive supplies no selection or confirmation panel for those excluded recipes.

Exploration also entered the adaptive search: **13 direct candidates immediately reached the top four**, and direct candidates were used as a parent or donor in **44 later proposal attempts**. Across transitive ancestry there were **44 descendant attempts**, of which **37 were evaluated** and seven rejected. No descendant reached nomination. Parent-use counts include rejected proposal attempts; they do not represent additional evaluated recipes. The detailed record preserves ancestry, population appearances, ranks and rejection reasons for every proposal.

The baseline evaluated 194 fresh-construction recipes, all with zero discovery wins. The candidate policy evaluated 72 such recipes, likewise with zero discovery wins, and replaced later fresh opportunities with exploration. Direct exploration recorded **460/976 discovery wins**, so it did add useful training outcomes compared with those fresh recipes. That did not translate into stronger selected outputs. Operator aggregates involve adaptively chosen recipes and shared panels; they are descriptive counts, not independent comparisons of operator quality.

## The seven nominees lost on a fresh selection panel

Every nominated exploration recipe had seven or eight discovery wins out of eight. All then had fewer selection wins than the winning nominee:

| Pair | Exploration recipe | Discovery wins /8 | Selection wins /32 | Winner wins /32 | Selection deficit |
| --- | --- | ---: | ---: | ---: | ---: |
| 2 | `0a60d9a7…` | 7 | 23 | 26 | 3 |
| 2 | `3189b780…` | 7 | 16 | 26 | 10 |
| 7 | `4f870c4a…` | 8 | 25 | 26 | 1 |
| 9 | `93bc8e77…` | 8 | 18 | 22 | 4 |
| 9 | `e064b712…` | 7 | 17 | 22 | 5 |
| 10 | `205eeb13…` | 8 | 25 | 28 | 3 |
| 11 | `8cd985d4…` | 7 | 24 | 26 | 2 |

Their pooled counts declined from **52/56 discovery wins (92.86%)** to **148/224 selection wins (66.07%)**. This is descriptive evidence of weaker performance on fresh search-stage feedback after discovery-based nomination. It does not isolate sampling noise from adaptive over-selection or identify an optimal replacement scoring rule. No confirmation outcomes were used to choose among these nominees or propose alternative thresholds.

In pair 9, the baseline selected `b7277336…`, generated by recombination at zero-based proposal 41 from the `8287f779…` and `96b94357…` references. The candidate arm never generated that recipe. Its two direct exploration nominees scored 18 and 17 selection wins, below reference `8287f779…` at 22. Thus the saved divergence involves both a changed construction trajectory and losing nominees; it cannot be attributed solely to final selection. The study's already closed confirmation result remains `DoNotPromoteReferenceExploration`.

## A systematic coverage omission

The captured [schedule](../LL/tools/BalanceHarness/TowerReferenceExploration.cs) starts every reference cursor at zero on every root, traverses character slots in sorted order, visits references in sorted reference-ID order and alternates radii two and three. The completed searches provided only 9–11 exploration opportunities each. Reference `96b94357…` was third in the reference cycle and therefore received exactly three visits per root: slots 1–2, then 3–5, then 6–7.

| Reference | Visits to each slot 1–7, across twelve roots | Visits to each slot 8–10 |
| --- | ---: | ---: |
| `399bc776…` | 12 | 11 |
| `8287f779…` | 12 | 3 |
| `96b94357…` | 12 | 0 |

All scheduled edits succeeded, so these counts describe realized **direct exploration** as well. They do not claim that other mutation operators never edited those slots. Nor do they establish that the omitted slots contain stronger changes. The omission follows directly from the fixed starting cursor and the available opportunities. Within-reference cyclic balance over a longer schedule does not ensure coverage across repeated short searches when every schedule restarts at the same position.

## Recommended correction, subsequently implemented

Implement a **separately versioned, opt-in exploration schedule with a root-dependent, reference-specific initial owner offset**. Derive the offset from an independent deterministic random stream, then retain the existing cyclic traversal, radii, reference order, opportunity cadence and construction checks. This gives each slot an opportunity to appear early across roots while preserving deterministic reproduction and the existing per-reference cyclic behavior. Random offsets do not guarantee complete coverage in any finite collection of roots.

Keep the candidate/proposal budgets, three protected references, two challenger nominations, selection panels and selector unchanged for that mechanical revision. Preserve the original policy and its archived schedules byte-for-byte. Before any new scientific comparison, verify reproducibility, slot/radius legality, advancement on rejection, independent random streams and the absence of a permanently excluded owner position across a declared deterministic fixture bank. Such fixtures establish scheduling behavior, not stronger-team discovery.

This correction addresses an observed coverage omission at unchanged opportunity cost. It is **not evidence that coverage alone solves the search-quality problem**. The failure of all seven nominees on selection also leaves feedback quality as a separate research question. A later strength comparison needs its own prospective protocol and untouched evaluation values. Do not widen the shortlist, lower promotion gates or tune new rules against this experiment's confirmation outcomes in order to promote this closed result.

## Verification and retained evidence

The [analyzer](analysis/practical-reference-exploration-diagnosis.py) authenticated the complete scientific archive, execution closeout and admission packages, checked all 24 saved searches against the global output freeze and discovery checkpoints, reconstructed discovery ordering, protected nomination and selection, resolved ancestry and checked scheduled versus realized edits. Shared physical recipes had identical saved discovery/selection observations across arms. It rechecked source-package membership and payload hashes after analysis.

The full supported live-history scan reproduced **567,789 exclusions across 234 files**, including the supported abandoned-reservation recovery. The same inventory and every prior file hash were preserved. The measured initial analysis took **67.125 seconds**. This is engineering work under the accepted separate accounting treatment; it does not reconstruct missing historical engineering totals or revise the old **18,180-second /13,584-MiB** ledger.

- [Detailed diagnosis and proposal records](../TestResults/reference-exploration-diagnosis-20260922/diagnosis.json).
- [Derived stage counts, operator counts and owner coverage](../TestResults/reference-exploration-diagnosis-20260922/stage-summary.json).
- [Preserved full history](../TestResults/reference-exploration-diagnosis-20260922/history-files.json) and [source manifest pins](../TestResults/reference-exploration-diagnosis-20260922/input-manifests.json).
- [Diagnostic tests](analysis/test-practical-reference-exploration-diagnosis.py), [passing test log](../TestResults/reference-exploration-diagnosis-20260922/tests.log) and [final verification](../TestResults/reference-exploration-diagnosis-20260922/verification.json).

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/practical-reference-exploration-diagnosis.py'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/test-practical-reference-exploration-diagnosis.py'
```

The analysis command records the completed read-only run and rejects an existing output directory. The initial analyzer and final analyzer are retained separately; the latter adds the derived stage summary and verifies the manifest's own hash against its original pin during the final inventory check. Final verification independently checked those original pins and reproduced the derived summary. **Eleven diagnostic tests passed**, covering selection boundaries, missing/duplicate/partial measurements, changed fitness, tampered nomination/ancestry/schedule/output, funnel partitioning and explicit zero-coverage slots. Python syntax, report tables, local links and scoped whitespace passed. No backend code changed and no backend tests were repeated; no required command remains blocked.

Changed files are this report, the analyzer and its tests, plus current documentation pointers. The scientific runtime, policies, defaults, confirmed teams, frozen protocol and historical evidence remain unchanged. There are no application configuration changes, migrations, database operations or deployments. The scientific allocation stays closed.
