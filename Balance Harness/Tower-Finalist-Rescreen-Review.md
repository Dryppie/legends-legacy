# Finalist rescreen comparison: verified completion

Completed **13 September 2026** in the offline BalanceHarness at the unchanged applied Kharad setting. **All 43,008 fights completed with zero retries.** Search reliability is **Fail, 1/3**, and the separate selection-benefit gate is **Fail, 1/3**. Both required at least two passing restarts, so adoption remains **Hold**. The complete **36-recipe confirmation family passed** both ordinary and joint balance assessment.

The [frozen execution plan](Tower-Finalist-Rescreen-Execution-Plan.md), [protocol](../TestResults/balance/tower-finalist-rescreen-20260913/protocol.json), [complete machine-readable results](../TestResults/balance/tower-finalist-rescreen-execution-20260913/results.json) and [completion receipt](../TestResults/balance/tower-finalist-rescreen-execution-20260913/final-verification.json) retain the rules, evidence and scope. The earlier [implementation review](Tower-Finalist-Rescreen-Implementation-Review.md) remains an unchanged record of preparation, before this execution.

## What the comparison established

All counts below come from the same **512 fresh confirmation seeds**. Rescreen selections were fixed before those outcomes. Intervals use the predeclared joint adjustment: .025 over 36 rates and .025 over nine paired differences.

| Restart | Selected original rank | Original primary wins | Rescreened primary wins | Adjusted rescreened rate | Paired selection benefit | Reliability | Benefit supported |
| --- | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 — seed 1146732187 | 6 | 19/512 | 72/512 | 14.06%; **9.64–20.07%** | +10.35 percentage points; **+2.44 to +17.86** | Fail: viability lower bound below 10% | Yes |
| 2 — seed 539358808 | 1, unchanged | 0/512 | 0/512 | 0%; **0–2.20%** | 0; **−1.96 to +1.96** | Fail: viability and comparator improvement | No |
| 3 — seed 917497573 | 27 | 68/512 | 78/512 | 15.23%; **10.62–21.38%** | +1.95 percentage points; **−7.01 to +10.84** | Pass | No |

Rescreening produced a supported improvement in restart 1, but its adjusted viability lower bound was **9.64%**, below the fixed 10% threshold. Restart 3 passed viability, deeper-comparator improvement and anchor recovery, but its improvement over the original v13 primary was not supported. Restart 2 retained its original zero-win primary. The identity comparison in restart 2 has exactly zero observed difference; its displayed interval follows the frozen conservative discordance formula.

The original deeper-search primaries won **1, 0 and 0/512**. The fixed anchor won **0/512**. All three new primaries meet the anchor-margin component, illustrating why anchor recovery alone cannot establish competitive strength here. No secondary was substituted after confirmation, and no extra trials were allocated to the near-threshold first restart.

The previous calibration's **Fail 0/3** remains its original result. This fresh opt-in comparison's **Fail 1/3** does not retroactively change it. The causal comparison within this study is the paired original-versus-rescreened result above, not a comparison of counts across historical campaigns.

## Candidate coverage and retained builds

The unchanged v13 discovery evaluated **2,304 parties across six arms**, representing **2,015 distinct complete recipes**. It preserved all original finalists and froze 32 candidates from each v13 restart. All **96 shortlisted candidates** received their complete 64-seed rescreen.

Restart 1's new primary moved from original rank 6: its rescreen won 9/64 versus the original primary's 3/64. Restart 3 selected original rank 27, which had won **0/8 in discovery**, then 15/64 in rescreen versus the original primary's 11/64. These are separate selection observations, never pooled with confirmation. Restart 2 had no winning discovery candidate in either arm and no wins among its 32 rescreen candidates; this does not prove that every unrescreened recipe is unviable.

The confirmation union retained **12 original finalists, four additional rescreened finalists and 20 external controls**, giving 36 distinct recipes. The other two rescreen selections duplicated the second restart's original finalists. No discovery or rescreen observation exceeded 50%, so there were no additional breach nominations or capacity overflow. Every confirmation recipe received all 512 trials.

Both the ordinary and joint 36-recipe assessments are **Pass**. Six recipes have joint-adjusted viability lower bounds at least 10%; none observed a ceiling breach. The strongest confirmation result was the external control `team-a7e5de669c4a17287d84060e8ab6359b`, at **137/512 (26.76%)**, with joint-adjusted interval **20.69–33.85%**. The strongest generated primary and secondary both won 78/512 in restart 3. This assessment covers the nominated family, not all 2,015 newly generated recipes.

The [complete build index](../TestResults/balance/tower-finalist-rescreen-execution-20260913/exports/saved-builds.md) and [machine-readable export](../TestResults/balance/tower-finalist-rescreen-execution-20260913/exports/saved-builds.json) retain **all 36 complete seed-free recipes**, source associations and confirmation measurements, including every zero-win finalist and control. All [2,304 evaluated recipes](../TestResults/balance/tower-finalist-rescreen-20260913/all-evaluated-recipes.json), the [frozen shortlist](../TestResults/balance/tower-finalist-rescreen-20260913/shortlist.json), [original/new nominations](../TestResults/balance/tower-finalist-rescreen-20260913/comparison.json) and all three compact rescreens remain in the sealed campaign. Local exports do not promote recipes into catalogs or independent generation.

## Execution and verification

| Phase | Completed fights | Execute seconds |
| --- | ---: | ---: |
| Discovery | 18,432 | 1,342.44 |
| Restart 1 rescreen | 2,048 | 51.49 |
| Restart 2 rescreen | 2,048 | 39.21 |
| Restart 3 rescreen | 2,048 | 51.09 |
| Complete-family confirmation | 18,432 | 385.88 |
| **Total, including runner overhead** | **43,008** | **1,872.29** |

The sealed campaign contains **1,192,451,472 bytes (about 1.11 GiB)**. Execution stayed within **57,344 fights / 5,400 seconds / 4 GiB**, with zero retries, interruptions, diagnostic repeats or extensions. The full attempt journal is exactly 43,008 start/completion pairs. The [476,641-reservation ledger](../TestResults/balance/tower-finalist-rescreen-20260913/seed-ledger.json) is unchanged: this execution used the 587 values already allocated during preparation and added **zero new reservations**. Every historical array, used or unused, remains excluded from future fresh allocation.

The captured run command completed with exit **1**, its documented completed-negative result. The captured verifier completed with exit **0**, reconstructing discovery, all rescreens, nominations, confirmation, statistics, resource accounting and unchanged package inventory with **zero new fights**. Independent Python `NormalDist` calculations reproduced every joint rate interval and all nine paired differences within `1e-8`; complete recipe exports were checked against the sealed family.

Executed commands, from the repository root:

```powershell
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-check --run TestResults/balance/tower-finalist-rescreen-20260913
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-run --run TestResults/balance/tower-finalist-rescreen-20260913
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-verify --run TestResults/balance/tower-finalist-rescreen-20260913
python -B TestResults/balance/tower-finalist-rescreen-execution-20260913/analyze.py
git -c core.safecrlf=false diff --check
```

Python used the available runtime's explicit path. No required command was blocked. Backend tests were not rerun because this step changed no C# source: the preceding **170 passing tests**, producing source, executable and content identities were checked before execution and preserved afterward. The new completion receipt checks nine preceding evidence/work packages and all unchanged source/content, and seals this result's scripts, logs, exports and documentation. Historical review, plan, implementation and readiness guards retain their original meaning; do not edit them to accept later states.

For future reconstruction, run **only** the captured `tower-finalist-rescreen-verify` command above. The campaign is complete; do not use `run`, `check`, the preparation-only readiness verifier, or a new allocation to reconstruct it. The supplemental `analyze.py verify` mode checks its saved derivations without writing or fighting.

## Decision and next boundary

**Keep the rescreen opt-in; do not adopt it as the default or extend this experiment.** It repairs a nomination error in one restart, but it has not established the required repeatable improvement or reliable party recovery. Both failed gates and the positive first-restart finding must remain visible.

The next substantive work is improving how search finds strong parties **during generation**. Use the saved generation histories, all 96 rescreen records and complete confirmation to assess the usefulness of the current eight-seed feedback and the failure to produce a winning shortlist in restart 2. That assessment should lead to one concrete optimizer change and a separately frozen comparison; it should not assume which untested recipe, larger sample count or operator will solve the problem. No follow-on campaign is allocated by this review.

Kharad remains **Health 3.5366243328 / Power 4.4702934848**. The prior **17,821-recipe precision Pass** and its 18 supported viable teams remain unchanged, separate from this 36-recipe result. No gameplay source, content, default policy, catalog, migration, production configuration, service, deployment or later-floor study changed. Practical ownership, complete new-family coverage and floors 6–11 remain open.
