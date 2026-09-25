# Reference exploration comparison: execution

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **Verified and closed: `DoNotPromoteReferenceExploration`.** The single [admitted comparison](Tower-Practical-Reference-Exploration-Comparison-Admission.md) completed **53,672 fights**, native reconstruction, the separate native audit and the independent saved-row audit. Eleven of twelve pairs selected identical teams. The one differing pair favored the baseline by 17 wins, giving an overall candidate-minus-baseline difference of **−0.1417 percentage points**. This experiment does not support promoting the exploration policy. Defaults and confirmed-team recommendations remain unchanged.

## Primary result and scope

The [verified result](../TestResults/balance/tower-reference-exploration-comparison-20260922/result.json) uses the unchanged [frozen protocol](Tower-Practical-Reference-Exploration-Comparison-Plan.md), including all twelve pairs in the **12,000-trial denominator**. Baseline outputs won **8,760/12,000 (73.00%)**; candidate outputs won **8,743/12,000 (72.8583%)**. Shared physical outcomes are intentionally counted in both logical arms when their selected teams are identical.

| Frozen promotion requirement | Observed | Met? |
| --- | ---: | --- |
| At least 600 net wins, equivalent to +5 percentage points | −17 net wins; −0.1417 points | No |
| At least seven positive pairs | 0/12 positive pairs | No |
| Positive one-sided 95% conditional lower bound | −0.7867 percentage points | No |

There was **one active pair** with different selected outputs, eleven identical pairs and no positive pairs. The conditional bound uses `conditional-range-hoeffding-depletion-v1`, with margin `0.006450398652650374`, depletion correction `1.9385642124313957e-8`, and lower bound `−0.007867065319317041` in probability units. It conditions on these twelve frozen output pairs; it is **not a confidence statement about future construction roots**. The observed negative difference does not establish that the policy is always worse. The decision is failure to meet the prespecified promotion rule.

Each row below contains 1,000 common confirmation values. Gains count candidate-only wins; losses count baseline-only wins. Recipe IDs are abbreviated here; complete identities remain in the sealed result and recipe files.

| Pair | Baseline output | Candidate output | Baseline wins | Candidate wins | Gains | Losses | Difference, points | Physical recipes |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | `90bded7e…` | `90bded7e…` | 739 | 739 | 0 | 0 | 0.00 | 4 |
| 2 | `96b94357…` | `96b94357…` | 778 | 778 | 0 | 0 | 0.00 | 3 |
| 3 | `96b94357…` | `96b94357…` | 788 | 788 | 0 | 0 | 0.00 | 3 |
| 4 | `399bc776…` | `399bc776…` | 710 | 710 | 0 | 0 | 0.00 | 3 |
| 5 | `399bc776…` | `399bc776…` | 718 | 718 | 0 | 0 | 0.00 | 3 |
| 6 | `93e1c2ed…` | `93e1c2ed…` | 646 | 646 | 0 | 0 | 0.00 | 4 |
| 7 | `399bc776…` | `399bc776…` | 690 | 690 | 0 | 0 | 0.00 | 3 |
| 8 | `96b94357…` | `96b94357…` | 757 | 757 | 0 | 0 | 0.00 | 3 |
| 9 | `b7277336…` | `8287f779…` | 692 | 675 | 213 | 230 | −1.70 | 4 |
| 10 | `b908d189…` | `b908d189…` | 769 | 769 | 0 | 0 | 0.00 | 4 |
| 11 | `399bc776…` | `399bc776…` | 729 | 729 | 0 | 0 | 0.00 | 3 |
| 12 | `8af43672…` | `8af43672…` | 744 | 744 | 0 | 0 | 0.00 | 4 |

The 24 per-arm family-ten views remain **descriptive only**, with no team adoption or additional promotion endpoint. All three references were measured in every pair, including identical-output pairs. This policy comparison does not resolve the captured cohort's separate balance failure or broader current-gameplay calibration questions.

## Executed contract

The baseline was `retained-composition-three-references-v1`; the candidate was `retained-composition-three-reference-exploration-v1`. Both used the same three supplied references, incumbent-tie selector and designation, captured gameplay/content, floor 5, ten level-40 characters, fixed identities/equipment and allowed pool. Each pair used a shared construction root, eight discovery values and 32 selection values, while both adaptive searches executed independently without cross-arm result caching.

All **24 search outputs froze after 12,672 fights**. Only then did confirmation begin. The twelve physical unions contained seven three-recipe families and five four-recipe families, totaling **41,000 confirmation fights**. The exact total was therefore **53,672**, within the planned 48,672–72,672 range. Identical outputs did not trigger early stopping or removal of controls.

The one 16,384-word entropy batch reserved **16,381 fresh values** and assigned **12,492**: 492 search/root values and 12,000 confirmation values. It encountered **two historical collisions and one within-batch duplicate**. All **3,889 unused fresh values** remain permanently reserved. There was one scientific launch, no refill, no retry, no resume and no extension.

## Verification, accounting and permanent history

The native reconstruction and separate native audit checked the adaptive trajectory, native prepared-input bindings, attempt sequence, global freeze, physical membership and saved outcomes. The independent Python auditor recounted the saved rows and recomputed the primary and descriptive statistics. Both audits agreed, added **zero fights and zero values**, and completed before publication under the original enclosing deadline.

| Phase | Seconds | Result |
| --- | ---: | --- |
| Owned native run, including reconstruction/history checks | 3,163.250 | Exit 0; complete process tree empty |
| Separate native audit | 259.485 | Exit 0; complete process tree empty |
| Independent saved-row audit | 110.375 | Exit 0; complete process tree empty |
| Entire scientific owner through completion accounting | 3,540.141 | Complete; 59.00 minutes |
| Separate read-only closeout | 64.391 | Passed; zero fights/values |

The scientific archive retained **1,206,641,176 bytes (1,150.74 MiB)**. Conservatively adding the admitted **600-second /512-MiB** prior charge gives **4,140.141 seconds /1,743,512,088 bytes (1,662.74 MiB)**. These fit the cumulative **10,800-second /6-GiB** envelope; execution itself fit **10,200 seconds /5.5 GiB**, and the native child fit **10,080 seconds /5.25 GiB**. The launcher included completion and manifest bytes in its retained-byte accounting. All three owned Windows Job process trees ended empty.

The [read-only closeout](../TestResults/reference-exploration-comparison-execution-20260922/closeout.json) authenticated admission and the exact scientific manifest, checked request/audit/process/accounting agreement, independently classified the full entropy batch and recomputed the primary endpoint. Its complete live-history scan preserved all **551,408 prior values and 232 prior files**, adding exactly the two completed reservation ledgers. The permanent union is now **567,789 exclusions across 234 files**. It rechecked the archive hashes after the scan. The scientific archive and all older evidence remain unchanged.

Earlier engineering work remains separately disclosed under the user's accepted treatment. Incomplete older totals remain unknown; the historical **18,180-second /13,584-MiB** ledger is preserved. Read-only monitoring, closeout helper development, verification and documentation are engineering work, with available receipts retained separately. The measured closeout duration above is not a claim to complete engineering totals, and unused scientific allowance is not transferred to another run.

## Evidence and commands

- [Scientific completion](../TestResults/balance/tower-reference-exploration-comparison-20260922/completion.json), [manifest](../TestResults/balance/tower-reference-exploration-comparison-20260922/files.json) and [external pin](../TestResults/reference-exploration-comparison-execution-20260922/scientific-pin.json).
- [Native receipt](../TestResults/balance/tower-reference-exploration-comparison-20260922/native-receipt.json), [native audit process](../TestResults/balance/tower-reference-exploration-comparison-20260922/native-audit-process.json), [independent audit](../TestResults/balance/tower-reference-exploration-comparison-20260922/independent-audit.json) and [independent process](../TestResults/balance/tower-reference-exploration-comparison-20260922/independent-audit-process.json).
- [Global output freeze](../TestResults/balance/tower-reference-exploration-comparison-20260922/study/outputs-freeze.json), [allocation](../TestResults/balance/tower-reference-exploration-comparison-20260922/allocation.json) and [complete live-history inventory](../TestResults/reference-exploration-comparison-execution-20260922/live-history-files.json).
- [Closeout helper](analysis/closeout-reference-exploration-comparison.py), [closeout log](../TestResults/reference-exploration-comparison-execution-20260922/closeout.log) and [engineering verification](../TestResults/reference-exploration-comparison-execution-20260922/verification.json).

```text
Scientific manifest: 482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e
Admission manifest:  55d47a7cf3b560baebf78435eb9089677fc880ce6f7d9e7f010dec22b4d03361
Request SHA-256:     58f787b5a9347ca6a3a225c04ec822c97235d9a93bec0a8a8ca49294487e51db
```

The saved admission verifier passed immediately before the single launch. These commands record the completed work; the launcher must not be run again. The admission verifier requires an absent scientific output and is consequently a historical preflight, not a post-execution verification command. Closeout authenticates the sealed admission directly and writes outside the scientific registry.

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'TestResults/reference-exploration-comparison-admission-20260922/run-reference-exploration-comparison.py' --request 'TestResults/reference-exploration-comparison-admission-20260922/request.json' --harness 'TestResults/reference-exploration-comparison-admission-20260922/runtime/BalanceHarness.dll'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/closeout-reference-exploration-comparison.py' --manifest-sha256 '482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e' --output 'TestResults/reference-exploration-comparison-execution-20260922'
```

The closeout command rejects overwriting its existing receipt. It never launches combat. Python syntax, updated local links and scoped whitespace checks passed; all **201 compiled source files** still match the admitted snapshot, and the frozen plan JSON is unchanged. Backend tests were not repeated because this execution added no C# changes; the preceding 98-case regression, 24-case final controller check and 14 Python cases remain linked from the implementation report. No required execution or closeout command was blocked.

Changed files are this report, the read-only closeout helper and eight current documentation pointers. The external engineering evidence directory retains the progress helper, preflight/launcher/closeout logs, pins, source hashes and verification receipt. No application configuration changes, migrations, database actions or deployments occurred.

## Next step

The [read-only diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md) found that all 122 direct exploration proposals were evaluated, seven reached nomination and none won selection; it also found omitted owner slots in the third reference. The [separate owner-offset policy](Tower-Practical-Reference-Exploration-Offset-Implementation.md) was implemented and mechanically verified. Its [separately admitted comparison](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) has now completed 51,672 fights and both audits with `DoNotPromoteReferenceExplorationOffset`: eleven identical-output pairs and +0.175 percentage points overall. All offset promotion gates failed. Both experiments are closed; their confirmation outcomes are not pooled. The [saved discovery/nomination/selection review](Tower-Practical-Search-Stage-Review.md) is complete; next is a prospective design for fresh screening at an unchanged total search budget. No team or default is adopted from either comparison's descriptive views.
