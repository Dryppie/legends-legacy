# Floor-15 evaluation after the attribute and equipment changes

The unchanged affinity search produced a promising new composition, but its selection procedure retained the existing benchmark. One generated finalist won **50/128** fresh held-out fights, compared with **37/128** for the strongest measured existing reference and **36/128** for the preselected benchmark. This is an exploratory team result, not a demonstrated improvement in the algorithm's selected output.

## Frozen design

Before seed allocation, the owner declaration selected floor 15 and historical reference 2 (`ea618982571c…`) as benchmark. References 2 and 3 had both won 11/32 in the completed current-runtime screen; reference 2 had lower average boss health remaining. The earlier screen's reference-1 eligibility rule and its recorded floor-3 follow-up decision were preserved. This run is a separately declared floor-15 evaluation.

- Attribute rules 18, equipment release 4 and `healing-v1`; all five combat assembly hashes exactly match the final reference screen.
- 15 characters, 10 Essences each, level 90, Uncommon Fine tier-2/rank-4 gear, baseline rolls, level-1 unascended/unevolved Essences, no styles and hypothetical ownership. Gear remains fixed; all 15 character compositions are eligible for search.
- Supported profile `affinity-creation-with-benchmark-validation-v1`, unchanged proposal policy, 17 proposals, 528 search fights and the existing 60-seed paired validation gate.
- All three references and both generated finalists frozen before the fresh 128-seed panel: 640 held-out fights, **1,168 total**. One root, zero retries, no extensions or optional stopping.
- Limits: 840 fixture seconds, 900 process-owner seconds, 1 GiB study archive. Allocator master 2026092815; domain `affinity-floor15-baseline-evaluation-v1`.

Historical ordered recipes are retained. Search inputs use the existing canonical Essence composition order and neutral character identities. Floor-15 imports required normalizing their character labels to the existing discovery contract; this does not change equipment or Essence membership. The screen's old rates were not treated as measurements of the canonical search inputs: every reference was measured afresh.

## Results

All rows use the same 128 fresh seeds. Gained/lost counts compare paired outcomes against reference 3, the strongest existing reference in this panel. That descriptive designation did not change the algorithm's selection.

| Team | Canonical composition ID | Wins | Clear rate | Gained / lost versus reference 3 | Mean boss health remaining |
| --- | --- | --- | --- | --- | --- |
| Existing reference 1 | `56025a22d864…` | 21/128 | 16.4% | 16 / 32 | 16.79% |
| Existing reference 2: benchmark, retained | `6396cb05afa8…` | 36/128 | 28.1% | 28 / 29 | 9.21% |
| Existing reference 3 | `7b90d85b6dc8…` | 37/128 | 28.9% | 0 / 0 | 9.77% |
| Generated finalist: nominated challenger | `3c674d8cc222…` | 35/128 | 27.3% | 23 / 25 | 8.42% |
| Generated finalist: best held-out result | `6d0e40ce3b07…` | **50/128** | **39.1%** | **36 / 23** | **7.87%** |

The best observed generated team differs from the benchmark only on **character 12**: it replaces `essence.forest_spirit` and `essence.nightshade_blossom` with `essence.spider_queen_royal_venom` and `essence.viper`. The retained proposal records `affinity-create`, benchmark parent, two replacements and one newly activated source-owned Poison affinity. This is a change in Essence membership, not an ordering-only change.

The 16-seed nomination panel favored the other generated finalist, 10/16 versus 7/16 for this one. Its separate validation then found 18/60 wins versus the benchmark's 23/60, with 12 gained and 17 lost wins. The existing gate correctly rejected that challenger and retained the benchmark. Held-out results were never fed back into this decision.

The best observed candidate's advantage over reference 3 is **13 wins, or 10.2 percentage points**. The 36/23 discordant pairs give an unadjusted two-sided exact paired-binomial p-value of approximately 0.117. This does not establish an advantage, especially because the candidate was identified by looking across the held-out panel. There was only one search root. The result supports a specific confirmation experiment; it does not establish general search superiority or justify changing the nomination procedure.

## Decision and next step

Keep the supported search unchanged. Before shifting to gear optimization, run one fixed-team confirmation of `6d0e40ce3b07…` against references 2 and 3 on a predeclared fresh paired panel, with the same captured rules, gear and compositions. A practical next bound is 512 seeds per team, **1,536 fights**, with no search, reselection or repeated attempts. Declare its comparison rule before allocating seeds. Use this run's complete ledger as the next exclusion input.

If that independent comparison supports the gain, retain the generated team as a useful benchmark and then study gear from a stable baseline. If it does not, stop this algorithm-tuning branch and move to gear optimization with the supported search retained. No confirmation fights were allocated or executed as part of this 1,168-fight evaluation.

## Verification and accounting

- **20 focused backend checks passed**, with the scientific opt-in skipped. The added test prepares all three complete floor-15 references behind a no-combat guard, checks the explicit second benchmark and verifies unchanged recipes, source policy, equipment budget and 15-by-10 composition size.
- The actual opted-in evaluation ran through `build/run-tests.ps1` and **all four fixture checks passed**. Native verification reconstructed the complete search and all 640 held-out inputs/results.
- Independent Python readback authenticated **1,383 files**, checked content/executable equality, recomputed all allocation candidates and rejection decisions, and recomputed all 640 held-out win counts, health means and paired contrasts. It verified that the proposal and selection policy versions were unchanged. The readback ran zero fights.
- **237 fresh values**: one generation root, 108 search seeds and 128 held-out seeds. They exclude all **832,413** prior values; the new complete exclusion union is **832,650**. No allocation collisions or retries occurred.
- Evaluation time **113.51 seconds**; owner **116.70 seconds**, with all eight processes drained. Archive size **173,341,772 bytes**, including its manifest.
- The initial sandboxed build could not read the user's NuGet configuration; the approved build succeeded. The first projection test caught the imported character-label issue before scientific allocation; the corrected test passed. Existing analyzer warnings remain. Python CLI loading and `git diff --check` passed.

Files changed for this follow-up are `Balance Harness/analysis/run-affinity-floor-evaluation.py`, `LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs` and this report. The launcher now accepts explicit floor/benchmark choices, validates the selected screen and matching runtime before dispatch, and records those choices in the declaration. The fixture supports the full floor-15 budget, authenticates all three screened recipes, and reports comparisons against the strongest measured existing reference. The search implementation and gameplay code were not changed in this follow-up. Earlier working-tree changes, including the user's attribute tooltip edit, were preserved.

No migrations, production configuration changes, deployments or shared database operations.

## Evidence and reproduction

Completed study: `TestResults/balance/tower-floor15-search-20260928/`.
Owner declaration, execution log and independent `audit.py`/readback receipt: `TestResults/tower-floor15-search-owner-20260928/`.
Focused verification: `TestResults/tower-floor15-verification-final-20260928.log`.

Result SHA-256: `8a3bd0eb6bd83fcee87cf4d7680fd2adb9b4d38e6b8f486a3a0d53ef689cf0cf`.
Archive manifest SHA-256: `cb732a486168afb88d7cb3bab123395edbcf20dc6d0b2488fbf3da8f9605361a`.

The executed commands were:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-current-balance-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessTowerBalanceSelectionTests'
python -B -X utf8 'Balance Harness/analysis/run-affinity-floor-evaluation.py' --package TestResults/tower-floor15-search-owner-20260928 --output TestResults/balance/tower-floor15-search-20260928 --artifacts TestResults/tower-current-balance-build-20260928 --floor 15 --benchmark-reference 2 --screen TestResults/tower-current-balance-screen-final-20260928/result.json --master 2026092815 --history TestResults/balance/tower-current-search-final-20260928/seed-ledger.json
python -B -X utf8 'TestResults/tower-floor15-search-owner-20260928/audit.py'
git diff --check
```

The execution used the bundled Python runtime. The study command allocates seeds and fights; completed directories cannot be overwritten or resumed. The audit command only reads the sealed study and writes a receipt outside it. The pinned historical evidence lives locally under ignored `TestResults`; a clean checkout alone cannot reproduce the scientific run.
