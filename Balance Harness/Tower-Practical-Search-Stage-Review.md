# Practical search: discovery, nomination and selection review

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **VerifiedSavedSearchStageReview.** The review reconstructs all **48 saved searches** from the separately closed original and owner-offset comparisons. The strongest next hypothesis is **fresh screening before final nomination at the same total fight budget**. Discovery feedback currently decides which two challengers survive, while many candidates tie on its coarse win count and the retained challengers often score lower on fresh selection. This is a design hypothesis, not evidence that a revised pipeline is stronger.

The [original comparison](Tower-Practical-Reference-Exploration-Comparison-Execution.md) and [offset comparison](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) remain closed without promotion. This review used their search-stage observations and already recorded decisions. It created **zero fights, values, parties or native preparations** and did not rank alternatives by confirmation outcomes. The experiments are reported separately; repeated recipes, shared panels and paired arms are not independent replications.

## Where candidates were lost

Each search evaluated 46 recipes on eight common discovery values, then retained three protected references plus two challengers for a fresh 32-value selection panel. Thus **41 of 43 challengers per search** were not measured by that arm at selection. A paired-arm coverage check recovers **four of 984 excluded challenger occurrences in each experiment**, because the other arm nominated the same recipe on the same ordered seeds. All four scored below the excluding arm's selected winner in each experiment. The other **980 occurrences per experiment** have no selection measurement on that pair's panel. Exclusion does not prove they were weaker; occurrences are not distinct teams or independent observations.

| Candidate source | Evaluated | Below nomination win cutoff | Tied cutoff but excluded | Nominated | Selected |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original: direct | 122 | 114 | 1 | 7 | 0 |
| Original: descendant | 37 | 36 | 1 | 0 | 0 |
| Offset: direct | 119 | 101 | 15 | 3 | 0 |
| Offset: descendant | 41 | 35 | 2 | 4 | 0 |

The offset run added fresh-selection evidence for seven exploration-related nominees: three direct proposals and four descendants. None won selection, and each scored below the strongest supplied reference on that selection panel. The three direct nominees fell from **23/24 discovery wins to 67/96 selection wins**; descendants fell from **27/32 to 80/128**. These descriptive counts concern nominees selected using discovery data. They do not identify the amount caused by noise, adaptive selection or recipe quality.

| Offset pair | Recipe | Source | Discovery wins | Selection wins | Deficit to selected winner | Paired gains /losses against winner |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 1 | `eca8a72d…` | descendant | 7/8 | 20/32 | 6 | 4 /10 |
| 3 | `26cc38c6…` | direct | 7/8 | 22/32 | 1 | 6 /7 |
| 5 | `3d38d099…` | direct | 8/8 | 20/32 | 4 | 6 /10 |
| 6 | `2ed8ae02…` | direct | 8/8 | 25/32 | 3 | 3 /6 |
| 9 | `5bf090e6…` | descendant | 6/8 | 19/32 | 4 | 4 /8 |
| 9 | `373a3d8b…` | descendant | 8/8 | 19/32 | 4 | 5 /9 |
| 10 | `de5df10a…` | descendant | 6/8 | 22/32 | 1 | 5 /6 |

This pattern also appears among challengers outside the exploration ancestry. The following table includes all 24 nominated challenger occurrences in each arm, keeping the two experiments separate. References are excluded from these aggregates.

| Search arm | Nominee discovery wins | Nominee selection wins | Challengers above /tied /below best reference on selection |
| --- | ---: | ---: | ---: |
| Original baseline | 170/192 | 523/768 | 5 /1 /18 |
| Original candidate | 172/192 | 533/768 | 4 /1 /19 |
| Offset baseline | 157/192 | 508/768 | 2 /2 /20 |
| Offset candidate | 162/192 | 518/768 | 3 /2 /19 |

The counts support investigating the quality of nomination feedback. They do not establish that a larger shortlist would recover a stronger discarded recipe: the available cross-arm measurements show no such winner, and the remaining discarded occurrences lack their fresh panel. A wider shortlist alone would also change cost.

## Offset search convergence and coarse nomination

Both experiments produced identical nominee lists in six of twelve pairs and identical selected outputs in eleven. The offset pair overlap ranged from **15 to 36 of 46 evaluated recipes**. Diverse generation therefore often converged at nomination or final selection.

The offset candidate arm had **2–10 challengers tied at the second challenger's discovery win count** in each search. Among all 516 evaluated challengers, **448 were excluded for lower win counts and 44 for tied win counts**, leaving 24 nominees. The tied candidates were ordered by the existing guardian-health, survival, winning-duration and stable-ID criteria. Those secondary measures decide the nomination boundary; fresh win evidence for the excluded candidates is absent.

| Offset pair | Shared evaluated | Same nominees? | Same output? | Cutoff wins | Challengers at cutoff | Selection lead | Changed leave-one-out selections /32 |
| --- | ---: | --- | --- | ---: | ---: | ---: | ---: |
| 1 | 27 | No | Yes | 6/8 | 7 | 2 | 0 |
| 2 | 36 | Yes | Yes | 5/8 | 10 | 0 | 6 |
| 3 | 22 | No | Yes | 6/8 | 3 | 1 | 17 |
| 4 | 36 | Yes | Yes | 7/8 | 3 | 1 | 7 |
| 5 | 15 | No | Yes | 7/8 | 2 | 0 | 5 |
| 6 | 22 | No | No | 7/8 | 4 | 1 | 3 |
| 7 | 36 | Yes | Yes | 7/8 | 3 | 4 | 0 |
| 8 | 36 | Yes | Yes | 6/8 | 5 | 6 | 0 |
| 9 | 22 | No | Yes | 6/8 | 7 | 3 | 0 |
| 10 | 19 | No | Yes | 6/8 | 4 | 1 | 0 |
| 11 | 35 | Yes | Yes | 6/8 | 7 | 0 | 6 |
| 12 | 32 | Yes | Yes | 7/8 | 4 | 5 | 0 |

In pair 6, the baseline never generated candidate output `90bded7e…`. The candidate arm produced it with ordinary recombination at zero-based proposal 41, without direct-exploration ancestry; it ranked second among challengers and won selection **28/32**, ahead of two references at **27/32**. The first-ranked direct exploration nominee won **25/32**. The baseline's two references tied at 27, so the designated incumbent was retained. This output difference involves the changed construction trajectory and nominee set; it is not an isolated tie-policy effect or a direct exploration win.

## How sensitive was final selection?

Using the same frozen five nominees and existing positive-win tie rule, the analyzer compares the first 16 selection trials with the last 16, and separately deletes each of the 32 common trials once. These slices are deterministic descriptions of saved data. They reuse observations, have smaller samples and are not competing validated selectors, independent replications, confidence intervals or an estimate of error probability. Zero-win sliced panels would be left unassessed because their per-trial guardian health is unavailable; none occurred.

| Search arm | Designated maximum ties | One-win leads | Half-panel winners differ | Searches changed by some deletion | Changed deletions |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original baseline | 0 | 2 | 7/12 | 0/12 | 0/384 |
| Original candidate | 0 | 4 | 8/12 | 1/12 | 8/384 |
| Offset baseline | 4 | 3 | 9/12 | 6/12 | 40/384 |
| Offset candidate | 3 | 4 | 9/12 | 6/12 | 44/384 |

This shows sensitivity in the recorded selection stage, especially in the offset experiment. It does not justify broadening incumbent ties or treating one-win leads as ties. The selected output in offset pair 6 changed under three of 32 deletions; that observation does not validate overturning its closed result. These checks also give no guarantee that increasing final samples would solve nomination loss.

## Recommended next design

Prioritize a **separately versioned screening stage using fresh values before choosing the final two challengers**. Start from the practical baseline generator and preserve the three references, final five-member selection, 32 selection trials and incumbent-tie rule. A concrete cost-neutral design to assess prospectively is:

| Stage | Current pipeline | Proposed design point |
| --- | --- | --- |
| Adaptive discovery | 46 recipes ×8 trials =368 fights | 46 recipes ×4 trials =184 fights |
| Fresh screening | None | Three references +20 discovery-ranked challengers, each ×8 fresh trials =184 fights |
| Final selection | Three references +two discovery-ranked challengers, each ×32 trials =160 fights | Three references +two screening-ranked challengers, each ×32 fresh trials =160 fights |
| Total per search | 528 fights | 528 fights |

The 23-member screen follows the budget identity `46×4 +23×8 +5×32 =528`; it was not selected by fitting confirmation outcomes. Freeze its membership before screening and use screening measurements alone to rank its challengers for final nomination. Freeze the final five before selection. Every stage needs disjoint, correctly paired values. No recipe omitted from the saved archive is assigned an invented fresh score.

This proposal trades coarser adaptive discovery for a broader fresh assessment before final nomination. Four-trial discovery can worsen parent choices and candidate generation, so the comparison would test the **complete pipeline**, not isolate screening on an unchanged candidate set. This review originally proposed the design point before a protocol or implementation existed. The [separately frozen design](Tower-Practical-Fresh-Screening-Comparison-Plan.md) and [versioned implementation](Tower-Practical-Fresh-Screening-Implementation.md) now supply stage contracts, failure/partial-panel tests, both audit paths and an equal-budget prospective endpoint. The [captured-runtime admission](Tower-Practical-Fresh-Screening-Admission.md) and [single comparison](Tower-Practical-Fresh-Screening-Comparison-Execution.md) have now completed with `DoNotPromoteFreshScreening`. Its execution report records the fresh result separately from this earlier diagnosis. The [subsequent screening-stage review](Tower-Practical-Fresh-Screening-Stage-Review.md) is now complete and identifies reference-tie handling as the next separate hypothesis. Old allocations cannot be reused, and this saved-stage diagnosis still makes no strength claim.

Fresh-legal construction also deserves cost review: it produced **0 discovery wins across 194 and 195 baseline recipes**, respectively, while each exploration arm retained 72 such recipes with zero wins. These counts explain why broad random construction is a poor observed source in this captured cohort, but removing all exploration could harm unobserved search coverage. The prior replacement experiments failed their strength gates. Keep operator changes separate from the proposed screening hypothesis rather than bundling them into another offset variant.

## Verification and evidence

The [analyzer](analysis/practical-search-stage-review.py) authenticated six sealed scientific/admission/execution packages; checked every search against its freeze and checkpoint; reconstructed protected nomination, ranking and selection; resolved evaluated-parent ancestry; and mapped battle IDs through the saved trial journal to verify ordered common seeds and disjoint stages. It checked shared-recipe observations and rechecked all package memberships and hashes after analysis. It imports only hash-pinned, saved ranking/selection and history helpers.

The full supported history scan preserved **584,171 exclusions across 236 files**, including abandoned-reservation recovery. The successful review took **109.031 seconds**. An initial engineering attempt stopped before publishing a review because it treated recipe-specific battle IDs as common panel IDs. That was corrected to validate the underlying ordered seeds, with a regression test. Neither attempt ran combat or changed scientific evidence.

**Eleven diagnostic tests passed**, covering literal tie and strict-leader behavior, half-panel and leave-one-out answers, unknown zero-win subsets, distinct battle IDs sharing seeds, misaligned seeds, missing/wrong-stage/duplicate/partial/nonboolean observations, saved nomination/output/ancestry tampering and the known offset output. Verification checks the report tables, source syntax, links, whitespace, frozen plan and all 201 producing source documents. Backend tests were not repeated because no C# changed. No required command remains blocked.

- [Detailed review and all saved-stage records](../TestResults/practical-search-stage-review-20260922/review.json).
- [Source manifest pins](../TestResults/practical-search-stage-review-20260922/input-manifests.json), [saved helper pins](../TestResults/practical-search-stage-review-20260922/helper-pins.json) and [preserved permanent history](../TestResults/practical-search-stage-review-20260922/history-files.json).
- [Diagnostic tests](analysis/test-practical-search-stage-review.py), [test log](../TestResults/practical-search-stage-review-20260922/tests.log), [analysis log](../TestResults/practical-search-stage-review-20260922/analysis.log) and [final verification](../TestResults/practical-search-stage-review-20260922/verification.json).

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/practical-search-stage-review.py'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/test-practical-search-stage-review.py'
```

The analysis command records the completed review and refuses to overwrite its output. Engineering remains separately disclosed. These receipts do not establish complete historical engineering totals; the **18,180-second /13,584-MiB** ledger remains unchanged. The report, analyzer, tests and current documentation pointers are the changed repository files. Both scientific allocations remain closed. Defaults, confirmed-team recommendations and the captured cohort's separate balance failure are unchanged. No application configuration changes, migrations, database actions or deployments occurred.
