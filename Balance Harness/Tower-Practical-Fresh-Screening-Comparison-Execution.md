# Fresh-screening comparison: scientific execution

23 September 2026. Target: offline `LL/tools/BalanceHarness`. **Verified and closed: `DoNotPromoteFreshScreening`.** The single [admitted comparison](Tower-Practical-Fresh-Screening-Admission.md) completed **55,672 fights** and passed native reconstruction, the separate native audit and the independent saved-row audit. The candidate added **+96 net wins /12,000**, or **+0.800 percentage points**, with **3/12 positive pairs**. The frozen strength requirements were not met; retain the practical baseline. No default or confirmed-team recommendation changed.

## Primary result and scope

The [verified result](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/result.json) retains all twelve pairs in the **12,000-trial denominator**, following the [frozen protocol](Tower-Practical-Fresh-Screening-Comparison-Plan.md). Baseline outputs won **8,715/12,000 (72.625%)**; candidate outputs won **8,811/12,000 (73.425%)**. Identical selected teams share their physical outcomes within a pair and contribute zero to its difference.

| Frozen strength requirement | Observed | Met? |
| --- | ---: | --- |
| At least 600 net wins, equivalent to +5 percentage points | +96 net wins; +0.800 points | No |
| At least seven positive pairs | 3/12 positive pairs | No |
| Positive one-sided 95% conditional lower bound | −0.6424 percentage points | No |

There were **5 nonidentical pairs** and **7 identical pairs**. The `conditional-range-hoeffding-depletion-v1` bound used margin `0.014423971554551022`, depletion correction `4.850328653119397e-07` and lower bound `-0.006423971554551022` in probability units. The calculation uses the actual **584,171 historical exclusions**, **588 assigned search/root values** and all twelve output pairs. It conditions on these frozen outputs and does not establish reliability across future construction roots.

Each pair used 1,000 common confirmation values. Gains are candidate-only wins; losses are baseline-only wins. Full identities remain in the sealed result and recipes.

| Pair | Baseline output | Candidate output | Baseline wins | Candidate wins | Gains | Losses | Difference, points | Physical recipes |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | `96b94357…` | `96b94357…` | 762 | 762 | 0 | 0 | 0.00 | 3 |
| 2 | `399bc776…` | `399bc776…` | 709 | 709 | 0 | 0 | 0.00 | 3 |
| 3 | `0b22cc0f…` | `96b94357…` | 658 | 763 | 247 | 142 | 10.50 | 4 |
| 4 | `96b94357…` | `96b94357…` | 730 | 730 | 0 | 0 | 0.00 | 3 |
| 5 | `e93ee623…` | `1b449348…` | 590 | 725 | 286 | 151 | 13.50 | 5 |
| 6 | `96b94357…` | `298ab0d1…` | 762 | 681 | 169 | 250 | −8.10 | 4 |
| 7 | `399bc776…` | `399bc776…` | 696 | 696 | 0 | 0 | 0.00 | 3 |
| 8 | `c8f0446c…` | `7c2fe15b…` | 774 | 782 | 174 | 166 | 0.80 | 5 |
| 9 | `96b94357…` | `96b94357…` | 791 | 791 | 0 | 0 | 0.00 | 3 |
| 10 | `399bc776…` | `399bc776…` | 717 | 717 | 0 | 0 | 0.00 | 3 |
| 11 | `96b94357…` | `b7277336…` | 769 | 698 | 152 | 223 | −7.10 | 4 |
| 12 | `96b94357…` | `96b94357…` | 757 | 757 | 0 | 0 | 0.00 | 3 |

Pairs 3 and 5 supplied **+105 and +135 net wins**; pairs 6 and 11 offset them with **−81 and −71**. Pair 8 added eight wins. These observed confirmation differences motivate a stage-level diagnosis of the saved searches; they do not identify which pipeline stage caused the difference or establish future-search reliability.

All three references were confirmed in every pair. The 24 per-arm family-ten views remain descriptive and authorize no team adoption. Previous comparison outcomes were excluded rather than pooled. The captured cohort's separate balance failure and current-gameplay calibration remain unresolved.

## Executed pipelines and allocation

Both arms used `retained-composition-three-references-v1` and the unchanged `tower-staged-incumbent-tie-v1` selector. Baseline direct nomination used **46 ×8 discovery +5 ×32 selection =528 fights**. Candidate fresh screening used **46 ×4 discovery +23 ×8 screening +5 ×32 selection =528 fights**. It froze the three references plus twenty discovery-ranked challengers before screening, ranked fresh screening alone and froze three references plus two challengers before final selection.

The captured floor-5 encounter, ten level-40 characters, five Essences per character, equipment, actor identities, progression, allowed pool, canonical Essence order and `OwnedCopies=null` remained fixed. Only the admitted tested harness files substituted into the captured runtime. Four-trial discovery can change adaptive parents and later recipes, so this compares **complete pipelines**, not screening applied to identical candidate sets.

All **24 outputs froze after exactly 12,672 search fights**, before any confirmation. The twelve physical confirmation unions required **43,000 fights**, for **55,672 total**. Every required union completed, including identical-output pairs. No outcome-dependent early stopping was applied.

The single 16,384-word entropy batch reserved **16,381 fresh values** and assigned **12,588**: 588 construction/search values and 12,000 confirmation values. It encountered **3 historical collisions** and **0 within-batch duplicates**. All **3,793 unused fresh values** remain permanently reserved. There was one scientific launch, no refill, retry, resume, replacement restart or sample extension.

## Verification, accounting and permanent history

Native reconstruction and the separate native audit checked adaptive trajectories, screening membership and nominee freezes, prepared inputs, attempts, global freeze, physical membership and saved outcomes. The independent Python auditor recounted saved rows and recomputed all primary and descriptive statistics. Both audits agreed and added **zero fights and zero values** under the original shared deadline.

| Phase | Seconds | Result |
| --- | ---: | --- |
| Owned native run, including reconstruction and history checks | 3,149.688 | Exit 0; owned process tree empty |
| Separate native audit | 244.953 | Exit 0; owned process tree empty |
| Independent saved-row audit | 104.828 | Exit 0; owned process tree empty |
| Entire scientific owner through completion accounting | 3,506.516 | Complete; 58.44 minutes |
| Separate read-only closeout | 93.422 | Passed; zero fights/values |

The scientific archive retained **1,267,889,961 bytes (1,209.15 MiB)**. Adding the conservatively precharged **600 seconds /512 MiB** gives **4,106.516 seconds /1,804,760,873 bytes (1,721.15 MiB)**, within the cumulative **10,800-second /6-GiB** envelope. Execution fit **10,200 seconds /5.5 GiB** and native execution fit **10,080 seconds /5.25 GiB**. Manifest and completion bytes are included. All three owned Windows Job process trees ended empty.

The [read-only closeout](../TestResults/practical-fresh-screening-comparison-execution-20260923/closeout.json) authenticated admission and the scientific manifest, checked request/audit/process/accounting agreement, independently classified the entire entropy batch and recomputed the endpoint with the screening protocol. A complete live-history scan preserved all **584,171 prior exclusions and 236 prior files**, adding exactly two reservation ledgers. Permanent history is now **600,552 exclusions across 238 files**. Archive hashes were checked again after the scan. Earlier sealed scientific and engineering evidence remains unchanged.

Engineering is separately disclosed under the user's accepted accounting treatment. Complete historical engineering totals remain unknown. The **18,180-second /13,584-MiB** older ledger is preserved. Monitoring, closeout-helper changes, documentation and verification are separate engineering work. The measured closeout duration is not a complete engineering total. Unused scientific allowance is not transferred to another experiment.

## Evidence and completed commands

- [Scientific completion](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/completion.json), [manifest](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/files.json) and [external pin](../TestResults/practical-fresh-screening-comparison-execution-20260923/scientific-pin.json).
- [Native receipt](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/native-receipt.json), [native audit process](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/native-audit-process.json), [independent audit](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/independent-audit.json) and [independent audit process](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/independent-audit-process.json).
- [Global output freeze](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/study/outputs-freeze.json), [allocation](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/allocation.json) and [complete live-history inventory](../TestResults/practical-fresh-screening-comparison-execution-20260923/live-history-files.json).
- [Prelaunch verification](../TestResults/practical-fresh-screening-comparison-execution-20260923/prelaunch-verification.log), [launcher log](../TestResults/practical-fresh-screening-comparison-execution-20260923/launcher.log), [closeout helper](analysis/closeout-reference-exploration-comparison.py), [seven closeout checks](../TestResults/practical-fresh-screening-comparison-execution-20260923/closeout-tests.log) and [publication verification](../TestResults/practical-fresh-screening-comparison-execution-20260923/verification.json).

```text
Scientific manifest: 031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042
Admission manifest:  e6eae89b36a30b4bf167c988ff3202c1148880765f559c0d314e67f7d4818caa
Request SHA-256:     9e7c7a8140cbc1d0006178bb99a264887d268a70448845da84c71fc9f24e1756
```

These commands record completed work. The launcher must not be invoked again. The saved admission verifier passed immediately before launch; it requires an absent scientific output and is a historical preflight. Closeout authenticates admission directly and refuses to overwrite its receipt. Python used the bundled runtime.

```powershell
python -B -X utf8 'TestResults/practical-fresh-screening-admission-20260922/prepare.py' verify --screening --live --manifest-sha256 'e6eae89b36a30b4bf167c988ff3202c1148880765f559c0d314e67f7d4818caa'
python -B -X utf8 'TestResults/practical-fresh-screening-admission-20260922/run-reference-exploration-comparison.py' --request 'TestResults/practical-fresh-screening-admission-20260922/request.json' --harness 'TestResults/practical-fresh-screening-admission-20260922/runtime/BalanceHarness.dll'
python -B -X utf8 'Balance Harness/analysis/closeout-reference-exploration-comparison.py' --screening --manifest-sha256 '031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042' --output 'TestResults/practical-fresh-screening-comparison-execution-20260923'
python -B -X utf8 'TestResults/practical-fresh-screening-comparison-execution-20260923/test-closeout.py'
python -B -X utf8 'TestResults/practical-fresh-screening-comparison-execution-20260923/verify-closeout.py'
```

The read-only helper now binds the exact screening admission and request, reconstructs the **12,588-value** assignment and uses **588** search/root values in the depletion correction. Seven checks cover those boundaries, incomplete process trees and the original/offset saved-auditor compatibility. Publication verifies all twelve report rows, local links, Python syntax, scoped whitespace, the frozen plan and all **202 producing source documents**. No C# or captured runtime changed, so the preceding **154 backend and 41 Python implementation tests** were not repeated. No required command was blocked. An optional `Get-CimInstance Win32_Process` listing was denied by local permissions; the admitted owner's three process receipts supply the required exit and empty-tree checks.

Changed repository files are this report, ten documentation follow-ups and the read-only closeout helper. External evidence retains logs, pins, source snapshots and verification receipts. No application configuration changes, migrations, database operations or deployments occurred.

## Next step

Retain the practical baseline and close this screening experiment without promotion or extension. The [saved-stage review](Tower-Practical-Fresh-Screening-Stage-Review.md) is now complete across all 24 searches. It explains the gains and losses, including two challenger ties with a nonprimary reference and a strict two-win selection lead. Next is a separate positive-tie preference for all retained references on the direct-nomination baseline; no new selector or strength claim has been adopted.

The original and offset comparison decisions remain closed and unchanged. This execution adds no automatic team adoption or default change.
