# Practical Tower selection diagnostic: completed native execution

17 September 2026. Target: the offline BalanceHarness. **The approved single run completed all 4,640 fights and both built-in audits, returning `NoSelectionMissDemonstrated`.** Enclosing execution, inventory sealing and receipt publication took **248.562 seconds**, with **159,764,592 retained bytes (152.36 MiB)** including oversight. The frozen primary won **712/1,000**, versus **632/1,000** for anchor 040e, **628/1,000** for anchor 49f6 and **598/1,000** for the other challenger. Both anchors remain recommended; adoption remains **Hold**.

The [machine-readable review](Tower-Practical-Selection-Diagnostic-Execution.json) is reproduced by the [execution receipt reader](analysis/practical-selection-execution.py). Native evidence is in `TestResults/balance/tower-practical-selection-diagnostic-20260917`; the separate execution record is in `TestResults/selection-diagnostic-execution-20260917`. The [native report](../TestResults/balance/tower-practical-selection-diagnostic-20260917/diagnostic.md), [result](../TestResults/balance/tower-practical-selection-diagnostic-20260917/result.json) and [seed-free team exports](../TestResults/balance/tower-practical-selection-diagnostic-20260917/teams.json) contain the complete result and recipes.

## What the result establishes

All four nominees and the primary were frozen after 512 discovery and 128 selection fights. Each then received 1,000 trials on the same fresh, post-freeze panel. This supplies the missing independent measurement of the unselected challenger for this run. No other nominee met the preregistered selection-miss criterion; all three had fewer confirmation wins than the primary.

| Frozen nominee | Party ID prefix | Wins / trials | Win rate | Adjusted Wilson interval |
| --- | --- | ---: | ---: | ---: |
| Primary challenger | `399bc776` | 712 / 1,000 | 71.2% | 67.03–75.04% |
| Anchor 040e | `8287f779` | 632 / 1,000 | 63.2% | 58.83–67.36% |
| Anchor 49f6 | `6825709c` | 628 / 1,000 | 62.8% | 58.43–66.97% |
| Other challenger | `bdaffdc6` | 598 / 1,000 | 59.8% | 55.39–64.06% |

The following contrasts preserve the protocol's orientation: **other nominee minus frozen primary**. Gains are other-only wins; losses are primary-only wins.

| Other nominee | Gains / losses | Observed difference | Adjusted paired interval | Selection miss qualifies |
| --- | ---: | ---: | ---: | --- |
| Anchor 040e | 166 / 246 | −8.0 pp | −15.05 to −0.82 pp | No |
| Anchor 49f6 | 182 / 266 | −8.4 pp | −15.67 to −1.00 pp | No |
| Other challenger | 155 / 269 | −11.4 pp | −18.45 to −4.17 pp | No |

These are encouraging results for the already-frozen primary. Its observed advantage over both anchors is visible on this single independent panel, and the displayed adjusted intervals exclude zero in its favor under the stated approximation. The authorized diagnostic answers whether selection missed another nominee; it does **not** apply a replacement-adoption gate, establish reliable search improvement across runs, assess the unconfirmed remainder of the discovery pool, prove optimality or assess encounter balance. Negative selection-miss evidence does not establish equivalence. The intervals use the preregistered approximate family-ten Wilson procedure and the single-batch uniform-bit sampling model.

The subsequent [adoption-readiness review](Tower-Practical-Primary-Adoption-Readiness.md) confirms that the fixed primary meets the practical numerical margins even under the retained family-ten intervals, while the diagnostic's explicit exclusion of a strength/adoption endpoint keeps recommendations unchanged. It identifies a prospective fixed-team confirmation plan as the next useful work and records the above-50% balance concern for the declared intended-progression cohort. No new optimizer, repeat search, confirmation extension or gameplay experiment follows automatically. Older panels retain their original results and are not pooled with these counts. V19 remains **253 required recipes /Unresolved**, including its **512 unused values**.

## Approved execution and resource accounting

The user's subsequent “Please proceed” approved the concrete single-run scope and capped operational-failure risk presented after the [admission package](Tower-Practical-Selection-Diagnostic-Admission.md). The separate [approval receipt](../TestResults/selection-diagnostic-execution-20260917/approval.json) records this disposition. The sealed admission package's pending-decision file remains unchanged as historical evidence.

Before launch, the execution owner revalidated the exact admission inventory, source/runtime/content/history and recovery pins, unscheduled template, request semantics, public native-check receipt and absent output. It then invoked the [frozen command](../TestResults/selection-diagnostic-execution-20260917/command.json) exactly once using the retained producing runtime. No build or request substitution occurred. Native admission repeated under the registry lease before allocation.

The outer timer began before revalidation and execution receipt writes. A hidden, suspended process was assigned to an owned Windows job before being resumed. The owner sampled combined native and oversight storage and enforced the same **1,380-second /512-MiB** additional envelope with closeout reserves. Native controls independently enforced the unchanged phase ceilings. The process completed with **exit code 0**, **no timeout**, **zero active processes** in the job and **zero retries**. The job accounted for four processes. Its measured duration was **247.406 seconds**; native completion recorded **247.020 seconds** before final sealing. Enclosing duration through receipt publication was **248.562 seconds**.

| Phase | Measured seconds | Time cap | Output growth at phase close | Growth cap |
| --- | ---: | ---: | ---: | ---: |
| Admission and reservation | 30.987 | 180 | 60,305,617 bytes | 96 MiB |
| Search and nomination | 21.870 | 120 | 15,060,918 bytes | 64 MiB |
| Confirmation | 159.863 | 660 | 83,033,498 bytes | 256 MiB |
| Audit and publication | 34.152 | 360 | 230,213 bytes | 16 MiB |

Phase growth is the boundary observation, not peak memory or an OS disk quota; the audit receipt precedes the final manifest. Final native output is **159,203,511 bytes**, and execution oversight is **561,081 bytes**. The outer sampled high-water before its final receipt was **159,763,984 bytes**; the final **159,764,592-byte** total includes that receipt. All fixed caps passed. This measures one completed native workload; it is not a worst-case completion guarantee.

The entire additional **1,380 seconds /512 MiB** is charged and closed despite lower measured use. Prior closed probe/preparation charges remain **480 seconds /576 MiB**, giving cumulative charges of **1,860 seconds /1,088 MiB**. Unused allowance is not refunded or transferred. The run itself performed native reconstruction and an independent direct-outcome audit before publication. There was **no extra public verifier, standalone audit, replay or second attempt**.

## Reservations and retained identities

Search reserved **41** values. One **8,192-byte** cryptographic batch exposed **2,048** eligible distinct words; the first **1,000** formed the confirmation panel. All **1,048 unused tail values** remain excluded. Total new reservations are **2,089**, and the complete permanent union grew from **484,285 to 486,374**. Both final reservation artifacts report **Complete**; this run leaves no new Pending reservation. Earlier recovery records remain unchanged.

| Identity | SHA-256 |
| --- | --- |
| Native canonical request | `a7989e15bf3a39d86397a0856288948f5003c975525c4f2bee90bd3908944e07` |
| Frozen request file | `d0baa036cd43c0e70edc8f33dd9b8db375ddaad1e9c77f33ddd43e039ee02136` |
| Original admission manifest | `3dbb77e0097c1f8fa791fdbe8b37894a55c84b0b65643d93a18039206dbf4661` |
| Native publication manifest | `b73a6fc344214693dddfc14d74c4114ab4782d0ae80456ebd019c1103f149552` |
| Study archive manifest | `89ea5e9997f8e78e7e73cf82923c27dcd1903a0eccacc8c67549ba37e5858783` |
| Execution oversight manifest | `a14c73e7d330d284d1072fd62424843be4c8bd7a70c43782bd6fc70d54d824df` |

The native publication manifest covers **4,802 entries** plus itself; the study manifest covers **4,771 entries**. Oversight covers **nine entries** plus its manifest and final receipt. The final native, worker and independent-audit results agree exactly, and the native audit records zero new fights. The attempt journal contains exactly **4,640 Started /4,640 Completed** entries.

## Verification and changed files

```powershell
python -B -X utf8 "Balance Harness/analysis/practical-selection-execution.py" verify
```

The bundled Python interpreter was used because `python` is absent from this session's PATH. This mode only reads inventories and receipts; it does not rerun combat, reconstruction or a scientific audit. The enclosing launch already authenticated the native publication inventory within its allowance. The reader subsequently reproduced the saved JSON, authenticated exact native/oversight membership and hashes, and checked receipt consistency. All **41 admission files**, **202 required history hashes**, producing source/runtime/content and recovery pins remained unchanged. All **439 local Markdown links** and whitespace checks passed. Of **6,696 baseline files**, eight received the intended Markdown updates and **6,688 remained byte-identical**. The execution helper matches its retained producing copy. The former admission reader intentionally requires absent gameplay output and should no longer be invoked as a current-state check; its successful prelaunch revalidation is retained in oversight.

Added the execution helper, this review, its JSON and the two execution evidence directories. Updated the harness README, practical guide, readiness/current assessment and historical implementation/resource/probe/admission pointers. Backend and gameplay source, runtime, content, prior evidence and frozen admission files were preserved. Backend tests were not rerun because no backend source changed; the approved native command and both built-in audits passed. No required verification command was blocked. No migrations, application configuration changes or deployments are required.
