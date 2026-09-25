# Incumbent tie comparison result

22 September 2026. **SupportsIncumbentTieForFrozenOutputs.** The single prospective comparison completed all 24 searches and 19,904 fights. Both the public native archive verifier and the independent Python saved-row audit passed. Retaining the designated incumbent on positive selection ties gained **2.575 percentage points averaged across the 24 frozen output pairs**, with a **one-sided 95% conditional lower bound of 1.930 points**. All three predefined performance gates passed.

The experiment is closed. It does not automatically change the selector default, adopt a new team, or authorize a repeat. The existing incumbent generator, original selector default and confirmed team recommendation remain unchanged.

## Primary result

| Quantity | Verified result |
| --- | ---: |
| Construction restarts | 24 |
| Identical selector outputs | 20 |
| Differing selector outputs | 4 |
| Candidate-only wins / baseline-only wins | 1,218 / 600 |
| Net gained wins | **618** |
| Fixed primary denominator | **24,000** |
| Mean gain across all frozen output pairs | **+2.575 percentage points** |
| Conditional margin | 0.6450534467 points |
| One-sided 95% conditional lower bound | **+1.9299465533 points** |
| Positive restarts | **4** |
| Required gates | At least 240 net wins, lower bound above zero, at least three positive restarts: **all passed** |

The mean is `618 / 24000`. The twenty identical outputs contribute exact zero contrasts; their absolute confirmation wins remain unmeasured/null. They are not treated as 20,000 additional measured trials. The four differing pairs each received their declared 1,000-value confirmation panel, shared between baseline and candidate.

| Restart | Baseline wins /1,000 | Incumbent-tie wins /1,000 | Candidate-only gains | Baseline-only losses | Difference |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 3 | 466 | 703 | 368 | 131 | +23.7 points |
| 8 | 561 | 725 | 303 | 139 | +16.4 points |
| 15 | 692 | 721 | 206 | 177 | +2.9 points |
| 20 | 543 | 731 | 341 | 153 | +18.8 points |

Every differing candidate output was the predesignated supplied primary, canonical party `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b`. The saved selection rows show the required positive top tie in each case. Full baseline recipe identities and all 24 contributions are in the [verified result](../TestResults/balance/tower-incumbent-tie-comparison-20260922/result.json).

This confidence statement concerns the mean strength contrast of these **24 frozen output pairs**, conditional on their search observations, over the protocol's uniform fresh combat-value population. It does not estimate variability over all future construction roots or establish a benefit on changing development gameplay. The retained scope is floor 5, ten level-40 characters with five Essences each, fixed equipment and canonical ability order, and `OwnedCopies=null`. The comparison does not reassess encounter balance or acquisition feasibility. Individual row differences are descriptive and have no separate significance claims.

The saved `conditional-range-hoeffding-depletion-v1` calculation used `H=505562`, `K=4`, `n=4000`, `N=24000` and `M=4294460750`. Depletion was `0.00000015519992818655987`; the unrounded lower bound was `0.01929946553306356`. The gate used unrounded arithmetic and the integer net-win threshold.

## Execution and permanent exclusions

The [frozen plan](Tower-Practical-Incumbent-Tie-Comparison-Plan.md), [tested runner](Tower-Practical-Incumbent-Tie-Comparison-Implementation.md) and [prepared runtime admission](Tower-Practical-Incumbent-Tie-Comparison-Admission.md) were authenticated before launch. The [prelaunch receipt](../TestResults/incumbent-tie-comparison-prelaunch-20260922.json) rechecked all 260 prepared-package entries and the complete 222-file historical inventory. Original gameplay assemblies, content and effective settings were retained.

One shared search per restart cost **11,904 fights**. All **48 outputs** were durably frozen at that exact search count before any confirmation. The four differing restarts cost **8,000 confirmation fights**, giving **19,904 total**. There were no extra reference controls, retries, refills, replacement roots or sample extensions. Progress monitoring read completion counts rather than confirmation outcomes.

The single 131,072-byte entropy batch contained 32,768 words: four historical collisions, zero duplicate fresh words and **32,764 fresh values permanently reserved**. Of the 24,984 assigned values, 984 belong to search and 24,000 to declared confirmation panels. The **20,000 confirmation values for identical outputs** and **7,780 unassigned fresh tail values** remain permanently excluded. No unused allowance or value is released for reuse.

The closed exclusion union is **538,326 values**, up from 505,562. Both `history-input.json` and `seed-ledger.json` are Complete. The [closeout receipt](../TestResults/incumbent-tie-comparison-audit-20260922/summary.json) independently rechecked that same union across **224 live ledger files**. The final archive contains **21,228 inventoried files plus its manifest**, occupying **519,623,838 bytes (495.552 MiB)**, below the fixed 4-GiB cap.

The enclosing run took **1,112.641 seconds (18 minutes 32.641 seconds)** under its 4,500-second /4-GiB cap. Native work, including admission, reservation, execution, reconstruction and history closeout, reported 1,109.2967725 seconds under its 4,440-second /3,968-MiB allowance. The Windows Job finished with exit 0, no timeout and zero active descendants. One scientific launch and zero retries are recorded in the [completion receipt](../TestResults/balance/tower-incumbent-tie-comparison-20260922/completion.json).

## Authentication and independent audits

| Pinned artifact | SHA-256 |
| --- | --- |
| Prepared package manifest | `ac6486e38aec867c5df5c1d2c7931edde13736d288de7d6a00dd87a408719776` |
| Concrete request | `76be13baf174373093c2ba60a41554dca37049d8f2e489ba60cc5df3577f7426` |
| Completed outer archive manifest | `c078df5d9f3f3074c763153e183c3544c462aefb50302867a5d854a3f9c4f909` |
| Completed study manifest | `2cbea9bf0eac25d853aff9114bb95da017ec10dcfdca7ac15dcba04ae453cff3` |

The manifest pin was saved outside the scientific archive before the post-run audits. The separate [audit evidence inventory](../TestResults/incumbent-tie-comparison-audit-20260922/files.json) has its own [external manifest pin](../TestResults/incumbent-tie-comparison-audit-pin-20260922.json). The [native public archive audit](../TestResults/incumbent-tie-comparison-audit-20260922/native-audit.json) used the producing executable retained inside the completed study. It checked the final completion/ownership receipts and reconstructed generation, actual selectors, prepared inputs, global freeze, reservations, direct outcomes and arithmetic.

The [independent Python audit](../TestResults/incumbent-tie-comparison-audit-20260922/independent-audit.json) authenticated the separately pinned full archive, read the direct compressed battle outcomes, independently checked panel order and both choices, recounted paired gains/losses and reproduced the primary endpoint, bound and decision. It returned `IndependentSavedRowsVerified`. It did not invoke native code or execute combat.

Both read-only audits ran under owned Windows Jobs with separate 900-second engineering allowances. The public native audit took **91.203 seconds**; the independent audit took **41.813 seconds**. Both exited 0 with no timeout or remaining descendants. Their time is outside the completed scientific launch and creates no additional search allowance. Both audits added **zero fights and zero values**.

Equivalent commands from the repository root are shown below. The actual invocation used the bundled Python runtime because Python is not on PATH. The scientific launch command is historical evidence and must not be repeated against this closed scope.

```powershell
python -B 'TestResults/incumbent-tie-comparison-admission-20260922-02/prepare.py' verify --manifest-sha256 ac6486e38aec867c5df5c1d2c7931edde13736d288de7d6a00dd87a408719776
python -B 'TestResults/incumbent-tie-comparison-admission-20260922-02/launcher.py' --request 'TestResults/incumbent-tie-comparison-admission-20260922-02/request.json' --harness 'TestResults/incumbent-tie-comparison-admission-20260922-02/runtime/BalanceHarness.dll'
dotnet 'TestResults/balance/tower-incumbent-tie-comparison-20260922/study/executable/BalanceHarness.dll' tower-incumbent-tie-comparison-verify 'TestResults/balance/tower-incumbent-tie-comparison-20260922'
python -B 'TestResults/incumbent-tie-comparison-admission-20260922-02/independent-audit.py' 'TestResults/balance/tower-incumbent-tie-comparison-20260922' --manifest-sha256 c078df5d9f3f3074c763153e183c3544c462aefb50302867a5d854a3f9c4f909
```

The prelaunch `prepare.py verify` command now rejects this request because its output exists, as intended. Completed evidence is audited through the two archive-verification commands. All audit receipts and documentation are outside the immutable scientific archive.

## Implication and next step

This prospective result supports the incumbent-tie rule for the captured scope and frozen outputs. It strengthens the earlier retrospective hypothesis without changing the old racing/anchored decisions or their five-point gates. The confirmed supplied primary remains the existing team recommendation.

The subsequent [practical-search preset](Tower-Practical-Incumbent-Tie-Preset.md) makes the designated incumbent explicit and prepares this selector for newly declared searches. It preserves legacy definitions and archive replay; strict leaders and zero-win ordering are unchanged. Any default-policy change remains a separate reviewed decision. Broader generalization would require a separately declared scope; this completed experiment supplies no retry or extension budget.

This step added the execution report, updated the related status/handoff Markdown and retained local experiment/audit evidence. It changed no C# implementation or runtime binary, so backend tests were not rerun; the previously tested producing binary was used throughout. No verification command remained blocked. There are no application configuration changes, migrations, database operations or service deployments.
