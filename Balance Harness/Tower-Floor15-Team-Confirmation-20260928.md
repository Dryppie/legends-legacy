# Floor-15 fixed-team confirmation

**Decision: strength not demonstrated. Retain the existing benchmark and supported search.** The generated Spider Queen/Viper team won 134/512 fresh fights, compared with 144/512 and 135/512 for the two existing references. Its exploratory 50/128 advantage did not repeat. The candidate is not promoted, and this confirmation is not extended.

## Frozen question and comparison rule

The previous floor-15 search identified candidate `6d0e40ce3b07ede6836e24ab13ddfc54699537c5350d32256e398e50c09e8e78` after inspecting its held-out results. This follow-up froze that exact candidate and both stronger references before allocating any new seeds. No new search or nomination occurred.

All teams retain the previous study's exact compositions, gear, character identities and Essence encoding. The only scenario change is a fresh combat-seed panel. The candidate differs from the benchmark on character 12: Forest Spirit and Nightshade Blossom are replaced by Spider Queen Royal Venom and Viper.

Conditions remain floor 15, 15 characters, 10 Essences each, level 90, Uncommon Fine tier-2/rank-4 baseline gear, level-1 unascended/unevolved Essences, no styles, hypothetical ownership and an uncleared floor without contributions. Attribute rules 18, equipment release 4 and `healing-v1` are captured. The five combat assemblies exactly match the exploratory evaluation.

The predeclared confirmation requires **both** comparisons to meet:

1. At least five percentage points of observed improvement: at least 26 net gained wins out of 512.
2. Exact one-sided paired-binomial p-value at most 0.025, using Bonferroni adjustment for the two reference comparisons.

The observed-gain threshold is a practical point-estimate requirement; it is not a claim that the true improvement is at least five percentage points. The two comparisons use common seeds and need not be independent for this adjustment. The usual interpretation models results from distinct fresh pseudorandom seeds as independent draws under the captured combat model.

The panel was fixed at **512 paired seeds per team, 1,536 total fights**. No old discovery data were pooled into confirmation, no early stopping occurred, and neither reselection nor an additional panel was allowed.

## Results

| Team | Composition ID | Wins | Clear rate | Mean boss health remaining |
| --- | --- | --- | --- | --- |
| Generated candidate | `6d0e40ce3b07…` | 134/512 | 26.17% | 9.969% |
| Existing reference 2: benchmark | `6396cb05afa8…` | **144/512** | **28.13%** | 10.025% |
| Existing reference 3 | `7b90d85b6dc8…` | 135/512 | 26.37% | 10.760% |

| Candidate compared with | Gained / lost wins | Observed change | One-sided exact p | Qualifies |
| --- | --- | --- | --- | --- |
| Reference 2 | 87 / 97 | -1.95 percentage points | 0.7913 | No |
| Reference 3 | 96 / 97 | -0.20 percentage points | 0.5572 | No |

The result does not establish that the teams are equivalent or that better compositions cannot exist. It provides no evidence for promoting this specific candidate. Lower mean boss health does not override the predeclared clear-rate criterion.

The original search decision remains `BenchmarkRetained`. Confirmation concerns the strength of one fixed team, not the reliability of the search algorithm across roots or floors. No algorithm change was made or inferred from the failed confirmation.

## Next implementation step

Close this candidate-confirmation branch. Retain reference 2 (`6396cb05afa8…`) as the working baseline and reference 3 as a comparison control. Keep the supported affinity search unchanged.

Move to a bounded gear experiment using the current equipment catalog. Initially hold Essence compositions fixed and vary legal gear profiles within the same level, tier, rank, quality and ownership assumptions. This will isolate the effect of gear choices before considering a combined Essence-and-gear search. The next experiment should predeclare its candidates, fight budget and comparison rule; this confirmation allocates no gear-study seeds or fights.

The exact seed-free baseline and control scenarios are retained in the completed study's `teams.json`; they are not rewritten or promoted into production defaults.

## Implementation and checks

- Added `Balance Harness/analysis/run-affinity-team-confirmation.py`. It pins the completed source archive and the three explicit team IDs, checks the tested runtime against the source, freezes the comparison rule and uses the existing Windows process owner to run the opt-in backend fixture without rebuilding.
- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs`. It authenticates source recipes, preserves gear/settings/identities, prepares the teams before reservation, performs the 1,536 fights and reconstructs every archived input, cache identity and report binding. Exact integer arithmetic determines the paired-test decision.
- Changed three helper methods in `BalanceHarnessAffinityFloorEvaluationTests.cs` from private to internal so confirmation can reuse its archive creation and sealing code. No search policy, combat implementation or production API was changed.
- Reused the existing bounded seed allocator in two predeclared 256-value blocks. Both finish before combat. A parent Pending ledger remains in place until the whole 512-value panel is durable. Tests inject a second-block interruption and verify that partial allocation cannot appear complete. This avoids changing the older allocator's limits or historical contracts.
- Wrote the captured, sanitized combat settings into the confirmation content archive. The first preflight had stopped on the missing settings file; its retained completion record has **zero attempts, zero fights and no reservation files**. The corrected preflight passed. No allocated panel or combat run was retried.
- **25 focused backend checks passed**, with two scientific opt-ins skipped. The actual confirmation then passed **all 11 checks** in its fixture, including the scientific operation. Existing analyzer warnings remain. The build used approved access to the local NuGet configuration.
- Independent Python readback authenticated **1,658 files**, recomputed both allocation/rejection journals, verified exact source recipes and execution, checked all **1,536 raw reports**, and independently recomputed both exact integer tails and the final decision. It ran zero fights.
- Python CLI loading and `git diff --check` passed. No requested verification remains blocked.

No migrations, production configuration changes, deployments or shared database operations. Earlier working-tree changes and the user's attribute tooltip edits were preserved.

## Accounting and retained evidence

**512 fresh seeds** were disjoint from **832,650** historical exclusions. The complete exclusion union is now **833,162**. No allocation collisions occurred. Use the completed confirmation ledger for any subsequent scientific allocation.

The evaluation completed in **126.07 seconds**; the owner completed in **132.58 seconds** and drained all eight processes. The archive contains **146,156,392 bytes**, including its manifest. Limits were 840 fixture seconds, 900 owner seconds and 1 GiB. Exactly 1,536 scientific fights were attempted and completed. Ordinary engineering-test combats are separate.

- Completed study: `TestResults/balance/tower-floor15-confirmation-final-20260928/`.
- Owner declaration, log, process receipt and independent audit: `TestResults/tower-floor15-confirmation-owner-final-20260928/`.
- Latest ledger: `TestResults/balance/tower-floor15-confirmation-final-20260928/seed-ledger.json`.
- Source exploratory archive: `TestResults/balance/tower-floor15-search-20260928/`.
- Preserved zero-allocation preflight failure: `TestResults/balance/tower-floor15-confirmation-20260928/` and its matching owner directory.
- Verification log: `TestResults/tower-floor15-confirmation-verification-final-20260928.log`.

Result SHA-256: `df9b0880e6608ef8f53b7d2583038d06717db28e4b3b4d27b2f16c79317e5db8`.
Archive manifest SHA-256: `af804279840211ec7a3895e365c98549eadbbe4ea0d84b8aa3d4e14780a9226b`.
Source manifest SHA-256: `cb732a486168afb88d7cb3bab123395edbcf20dc6d0b2488fbf3da8f9605361a`.

Executed commands, using the bundled Python runtime:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-current-balance-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessAffinityTeamConfirmationTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceSelectionTests'
python -B -X utf8 'Balance Harness/analysis/run-affinity-team-confirmation.py' --package TestResults/tower-floor15-confirmation-owner-final-20260928 --output TestResults/balance/tower-floor15-confirmation-final-20260928 --artifacts TestResults/tower-current-balance-build-20260928
python -B -X utf8 'TestResults/tower-floor15-confirmation-owner-final-20260928/audit.py'
git diff --check
```

The scientific command used master 2026092851 and domains `affinity-floor15-fixed-team-confirmation-v1/block-1` and `/block-2`. It refuses existing output directories. The audit only reads the sealed study and writes its receipt outside it. The pinned scientific history is local ignored evidence; a clean checkout alone cannot reproduce this run.
