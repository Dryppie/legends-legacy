# Three-reference search: candidate coverage and selection review

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **VerifiedSavedThreeReferenceCoverage.** The review reconstructed all **53 proposals, 46 evaluated recipes and five selection nominees** from the [closed three-reference search](Tower-Practical-Three-Reference-Execution.md). It found no parent-retention, nomination or selection mismatch. It ran **zero fights**, allocated **zero values**, constructed **zero new parties** and preserved **551,408 exclusions across 232 files**.

**Implementation follow-up — 22 September 2026:** The [opt-in periodic reference exploration policy](Tower-Practical-Reference-Exploration-Implementation.md) is implemented and passed 275 scoped backend tests. It replaces periodic fresh construction with bounded exploration around all three references while retaining the six initial fresh opportunities and downstream rules. The hypothesis still needs a prospective comparison and fresh captured-runtime admission; no default or strength recommendation changed. The source checks and zero-construction statement below describe this earlier saved-evidence review, before the separately documented implementation.

## Where discovery capacity went

| Candidate source | Attempts | Evaluated recipes | Discovery fights | Discovery wins | Challenger nominees |
| --- | ---: | ---: | ---: | ---: | ---: |
| Supplied references | 3 | 3 | 24 | 18 | Three protected references |
| Fresh legal construction | 17 | 17 | 136 | 0 | 0 |
| Mutations of retained/supplied teams | 33 | 26 | 208 | 77 | 2 |
| Total | **53** | **46** | **368** | **95** | **2**, plus all references |

All **17 fresh teams went 0/8**. They consumed **36.96%** of the discovery fights, supplied no actual parent or donor, and produced no nominee. These totals describe adaptively gathered observations on one shared eight-trial panel; they are not independent samples of general fresh-team strength and must not be used as a pooled significance test.

Fresh teams were **43–49 Essence replacements from their nearest reference**, out of 50 assignments. Their average pairwise distance was **47.04 replacements**. The 26 evaluated mutations were **1–6 replacements from their nearest reference**, with an average pairwise distance of **6.74**. Distance ignores Essence display order and preserves character ownership. The full candidate set's average distance of **30.79** therefore conceals a split between distant fresh teams and a much smaller neighborhood containing every generated nominee. More raw composition diversity alone is not an established remedy.

This resembles the earlier [six-study coverage diagnosis](Tower-Practical-Search-Candidate-Coverage-Review.md), where fresh construction also supplied no nominee. Those historical studies retain their own scopes and panels; their counts are not pooled with this root. The later [anchored-neighborhood comparison](Tower-Practical-Anchored-Neighborhood-Comparison.md) returned **DoNotPromoteAnchored**. It does not support simply replacing the whole search with one-change neighbors of a single anchor.

## Parent retention was concentrated but followed the declared rule

The producing `TowerSuppliedCompositionSearch.RunCoreAsync` uses the four highest-ranked completed discovery measurements for this baseline policy. Each mutation chooses from those four or, with the declared random branch, the supplied references. The separate `Retain` helper that adds distance-based choices belongs to the supplied-block path; it is **not active** in this three-reference run.

The saved population was reconstructed from the measurements available **before each mutation**, including rejected attempts. The longest unchanged population lasted **25 of 33 mutation decisions**, from proposal indices **17 through 49**. It contained `96b94357…`, `399bc776…`, `77f9a0a9…` and `b66a6ccb…`. The two supplied leaders stayed eligible throughout all 33 mutations. The third reference remained available through the supplied-reference branch after leaving the top four.

| Primary parent | Mutation attempts using it |
| --- | ---: |
| Reference `399bc776…` | 7 |
| Descendant `b66a6ccb…` | 7 |
| Reference `96b94357…` | 6 |
| Reference `8287f779…` | 6 |
| Descendant `77f9a0a9…` | 6 |
| Late challenger `c65168e6…` | 1 |

Only **six distinct teams** were used as primary parents. The initial zero-win fresh team briefly present in the top four was never actually used. Seven `single` mutations were evaluated, but only **two** were immediate one-change neighbors of the confirmed `96b94357…` reference, touching characters **1 and 5**. A single edit of another parent is not necessarily a one-change neighbor of that reference.

The stable population is an observed concentration, not proof that more diverse retained parents would improve outcomes. The eight-trial discovery ranking is noisy, but this archive contains no measurements of the alternative descendants that a different parent rule would have generated.

## Both nominated challengers arrived late

Proposal indices below are zero-based; evaluated-candidate positions are one-based.

| Recipe | Origin | Proposal /evaluated position | Discovery | Selection | Later primary-parent uses |
| --- | --- | ---: | ---: | ---: | ---: |
| `c65168e6…` | Single edit of `96b94357…` | 49 /43 | 7/8 | 22/32 | 1 |
| `833ba3ad…` | Cross-character edit of `c65168e6…` | 51 /45 | 6/8 | 25/32 | 0 |
| Selected reference `96b94357…` | Supplied | 2 /3 | 7/8 | 25/32 | 6 |

`c65168e6…` changes character 5 from Venomous Spiderling to Feral Ghoul. Its child `833ba3ad…` also replaces character 1's Venomous Spiderling with Kobold Skirmisher and character 2's Flame Harpy with Plague Ghoul. These are descriptions of frozen recipes, not validated recommendations or isolated causal effects of those Essences.

Both challengers entered the top four immediately after their discovery measurement. The child had no later population appearance because the only remaining proposal was fresh construction; there were **no mutation turns left**. Its lack of descendants is a finite-budget observation, not an erroneous retention decision. Extending the old run cannot be justified by this retrospective observation, and the run remains closed.

All three references and the two highest-ranked eligible generated teams were correctly nominated. **41 generated recipes were not nominated**, and **none of the 43 generated recipes received confirmation** because the selected output was a reference. A count of genuinely stronger teams lost at nomination or selection is therefore **not identifiable** from this archive.

## Selection is sensitive, but there is no validated replacement winner

The confirmed reference and `833ba3ad…` each won **25/32** selection fights. They shared **19 wins**, each won **six fights the other lost**, and both lost one. Thus the equal totals hide 12 discordant outcomes. The frozen discovery ordering correctly retained the reference. The originally designated reference `399bc776…` had 24 wins and was outside the actual top tie, so its optional tie preference did not alter the original result.

Deleting each shared selection position once, without changing the frozen shortlist or importing confirmation scores, produced:

| Selected recipe after one paired-position deletion | Cases out of 32 |
| --- | ---: |
| Actual selected reference `96b94357…` | 24 |
| Tied challenger `833ba3ad…` | 6 |
| Designated reference `399bc776…` | 2 |

The choice changes in **8/32** finite perturbations. In two cases the designated reference enters a new positive top tie. These are descriptive sensitivity checks, not independent experiments, bootstrap confidence intervals or probabilities that a team is best. Each perturbed panel still has positive win counts, so no unavailable per-trial health statistic is needed.

The challenger's lower selection guardian-health average (**2.809**, versus **5.871**) would favor it under a different health-based positive-tie rule. Its absent confirmation prevents validating that choice. The earlier selector comparison remains separate evidence; changing the tie rule to pick this particular challenger would fit the rule to a known observation. Keep the declared selector for the next generation comparison.

## Recombination has a concrete efficiency issue, with limited impact here

| Mutation operator | Attempts | Evaluated | Duplicate /illegal | Challenger nominees |
| --- | ---: | ---: | ---: | ---: |
| Single | 7 | 7 | 0 /0 | 1 |
| Double | 7 | 7 | 0 /0 | 0 |
| Cross-character | 7 | 5 | 0 /2 | 1 |
| Whole-character | 6 | 6 | 0 /0 | 0 |
| Recombine | 6 | 1 | 5 /0 | 0 |

Recombination independently copies whole character loadouts from either parent. If their loadouts differ on `d` characters, it can form at most `2^d` distinct mixtures, of which two reproduce a parent. **Two of the six attempts paired parents differing on only one character, making a new mixture impossible.** Four duplicates reproduced a parent; the fifth matched a different previously evaluated team. The sole evaluated recombination had parents differing on four characters and scored 2/8.

A later engineering change could reject donor pairs with fewer than two differing character loadouts and explicitly charge bounded attempts to avoid known duplicate mixtures. This would change the random trajectory and needs versioned behavior. It is secondary to the exploration proposal: duplicate/illegal proposals consumed **zero fights**, and the run still evaluated all 46 candidates after only 53 of 256 permitted attempts. Eliminating these duplicates does not recover the 136 fights spent on fresh teams.

## Concrete next implementation scope

Add an **opt-in, separately versioned exploration policy** for the three-reference practical search. The proposed intervention is restricted to the periodic fresh-construction branch:

1. Preserve the three supplied evaluations and six initial fresh evaluations. Preserve the baseline mutation branch, four-parent ranking, operator cycle, sample counts and all downstream decisions.
2. Replace the subsequent every-fourth-proposal fresh event with a bounded perturbation of a supplied reference. Visit the three exact reference IDs in ordinal order, independent of observed fitness or historical strength. Balance character visits for each reference.
3. Use two or three distinct character replacements per proposal, with the radius alternating **per reference visit**. Keeping a per-reference counter avoids coupling the three-reference cycle to the radius cycle. Require actual canonical recipe distance to equal the declared radius; retain fixed identities, equipment, family legality, allowed pool and any ownership constraint.
4. Use bounded construction checks, ordinary deduplication and durable provenance. Rejections consume the declared proposal opportunity. Do not add hidden refills, combat, retry or resume. Record the reference, radius and edited owners so saved-evidence verification can reconstruct the schedule.
5. Preserve all three references among five nominees, the existing tie designation, one selected output, three/four distinct confirmation recipes and the family-ten improvement rule. Keep legacy policies and archive reconstruction byte-compatible; importing the new policy must be explicit throughout validation, execution, result classification and recovery.

The radius and schedule are prospective design choices, not parameters estimated to be optimal by this root. The intervention keeps broad exploration in the initial six candidates and retains adaptive mutations and recombination. It is distinct from the earlier policy that spent all 44 challenger places on one-change neighbors of one designated anchor. It also leaves parent diversity, duplicate-donor handling and the selector unchanged so the first comparison addresses one generation change.

For this recorded 53-attempt schedule, the changed branch would have occupied **11 proposal positions**. That arithmetic does not predict 11 accepted new recipes, later parents, nomination or strength under the new trajectory. No replacement candidates were constructed in this review.

Implementation verification should cover exact reference/radius/character scheduling through rejections, canonical distance and legality, candidate/fight caps, no hidden retry, protected nomination, old-version parity, producing-runtime reconstruction and interrupted-allocation recovery. Run backend tests through `build/run-tests.ps1`. After implementation, declare a separate comparison using multiple paired roots, unchanged captured gameplay and shared panels within each pair, independent confirmation, an effect threshold, precision assessment and a fixed total time/storage/fight allowance. This root has informed the hypothesis and cannot validate it. The completed experiment's unused allowance remains closed.

## Evidence, verification and changed files

The [analyzer](analysis/practical-three-reference-coverage.py) reuses the existing coverage arithmetic and the selector arithmetic frozen with the successful execution. It authenticates the scientific and execution manifest pins, both saved audit results and **24 consumed inputs**, and checks four algorithm source files against the producing source capture. It reconstructs chronological parent populations, canonical IDs, legal recipes, ancestry, changes, stage panels, nominations and selection before describing them. The complete live registry matched the execution's pinned 232-file inventory and 551,408-value union. All consumed inputs were rechecked before publication.

- [Summary and candidate-level measurements](../TestResults/three-reference-search-coverage-20260922/summary.json).
- [Full per-proposal trajectory](../TestResults/three-reference-search-coverage-20260922/trajectory.json).
- [Input pins](../TestResults/three-reference-search-coverage-20260922/inputs.json), [completion](../TestResults/three-reference-search-coverage-20260922/completion.json) and [external package pin](../TestResults/three-reference-search-coverage-20260922-pin.json).
- [Eight new regression checks](analysis/test-practical-three-reference-coverage.py), plus the three existing coverage-arithmetic checks.

```text
Analysis package files.json SHA-256:
3909a1471bd61241e59ce9ee622cb879d57efa3c1329b933839af467754fa30c
```

All **11 regression tests** passed. The first invocation of the new fixtures exposed an incorrect expected sensitivity count: omitting a shared position can bring a designated reference into the top tie. The fixture was corrected to include that case; the analyzer and selector were unchanged. The saved-evidence analysis completed in **48.297 seconds**, retaining **435,885 bytes** before its external pin. This read-only engineering work remains separately disclosed; no historical engineering total is inferred and no scientific allowance is transferred.

Commands, with the configured Python executable:

```text
python -B -X utf8 "Balance Harness/analysis/test-practical-three-reference-coverage.py"
python -B -X utf8 "Balance Harness/analysis/practical-search-coverage.py" --self-test
python -B -X utf8 "Balance Harness/analysis/practical-three-reference-coverage.py"
```

Source syntax, package authentication, report facts, local links and scoped whitespace were also checked. No required command is blocked. Native archive reconstruction and backend tests were not repeated: the existing audits are authenticated, and this review changed no C# code.

Changed files are the new analyzer and tests, this report, and current pointers in the execution, implementation, state, README and practical-search guides. No application configuration, migrations, database operations or deployments are involved. The recommendation of the exact confirmed team remains as recorded; captured floor-5 balance remains Fail, V19 remains Unresolved, and the 162 current-family context exceptions remain separate work.
