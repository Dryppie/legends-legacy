# Three-reference practical search: completed execution

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **Complete /Verified /IncumbentRetained.** The single [admitted search](Tower-Practical-Three-Reference-Admission.md) completed **3,528 fights**, and native reconstruction plus the independent saved-row audit passed. The existing confirmed recipe `96b94357…` was selected; no generated challenger was promoted. The run is **Closed**, with no retry, resume or sample extension.

## Result and interpretation

All three supplied references remain recommended and present in the [seed-free team export](../TestResults/balance/tower-practical-three-reference-20260922/teams.json). The [readable team sheet](../TestResults/balance/tower-practical-three-reference-20260922/practical.md) lists each character's fixed Essence order, equipment, subgroup and required copies. No search primary, selector or gameplay default changed.

| Confirmed recipe | Wins /1,000 | Win rate | Family-ten adjusted interval |
| --- | ---: | ---: | ---: |
| Selected reference `96b94357…` | 762 | 76.20% | 72.22–79.77% |
| Original designated control `399bc776…` | 700 | 70.00% | 65.79–73.90% |
| Original control `8287f779…` | 624 | 62.40% | 58.02–66.59% |

The selected reference's observed paired advantages were **+6.20 points** against `399bc776…` and **+13.80 points** against `8287f779…`. Their adjusted paired intervals were **−0.77 to +13.07** and **+6.66 to +20.72 points**. The first interval includes zero. This 1,000-trial panel does not replace or pool with the earlier [5,500-trial fixed-family confirmation](Tower-Practical-Fixed-Family-Confirmation-Execution.md), which independently qualified the exact recipe.

`IncumbentRetained` follows from selecting an existing reference. It does not mean that every generated alternative was independently confirmed or that references are equivalent. The practical interval family remains ten even though the selected reference is measured only once. Its self-comparison has zero gained/lost wins; the generic gain/loss interval is not additional evidence about a new recipe.

The separate native encounter assessment is **Fail** for this captured floor-5, level-40, rank-2/tier-1 fixed-equipment cohort: all three observed win rates exceed its inclusive **50% ceiling**. That scoped finding does not resolve current-gameplay calibration, V19 reliability, or the **162 current-family context exceptions**. Historical fixed-family studies retain their separate `NotAssessed` balance status.

## Search and selection

The captured request retained ten characters in two groups of five, five Essences per character, fixed equipment, actor identities, the allowed pool, canonical Essence order and `OwnedCopies=null`. It used `retained-composition-three-references-v1`, one construction root, 46 evaluated candidates and at most 256 proposals. Actual generation stopped at the candidate limit after **53 proposals**.

All three references survived discovery nomination alongside the two best eligible challengers. The frozen five-team selection was:

| Nominee, in discovery-shortlist order | Selection wins /32 |
| --- | ---: |
| Challenger `c65168e6…` | 22 |
| Reference `96b94357…` | 25 |
| Designated reference `399bc776…` | 24 |
| Challenger `833ba3ad…` | 25 |
| Reference `8287f779…` | 20 |

The designated reference was outside the positive top tie, so the optional incumbent preference did not change the choice. The existing discovery ordering selected `96b94357…` ahead of the tied challenger. The independent arithmetic reproduced the same output under the legacy selector (`tieRuleChangedChoice=false`). This single root supplies no new causal comparison of selectors or evidence of general search reliability.

| Stage | Completed fights |
| --- | ---: |
| Discovery: 46 ×8 | 368 |
| Selection: 5 ×32 | 160 |
| Confirmation: 3 distinct recipes ×1,000 | 3,000 |
| Diagnostics /replays | 0 |
| Total | **3,528** |

The confirmation family froze after fight **528**, before its first battle. A selected reference needs three distinct confirmation recipes, so 1,000 of the admitted maximum 4,528 fights were unused. Those fights and the remaining time/storage allowance are closed; they do not fund another experiment.

## History, runtime and accounting

The allocator accepted **1,041 new values**: one construction root, eight discovery values, 32 selection values and 1,000 confirmation values. There were **zero rejected candidates** in the allocation journal. The final complete union is **551,408 exclusions across 232 files**, preserving every one of the prior 229 files and 550,367 values. The three added ledgers are the outer reservation, outer complete union and study schedule ledger. Native verification also retained the existing pinned abandoned-reservation recovery.

Execution used the admitted captured runtime and content, including the tested three-reference harness and preserved gameplay dependencies. The request bytes, template, versions, selector designation and limits were unchanged. Its execution hash is `8fbd85a5e3c669fcb588dbf4d021fa7616d6dff2a1a8885d7298897fb26f03a0`.

The enclosing wrapper took **416.938 seconds** from preflight through audit, history closeout and sealing. It retained **159,547,483 bytes (152.16 MiB)** before the small external pin. Both are below the remaining **1,200-second /1,536-MiB** limit. Adding the conservative **600-second /512-MiB** admission charge gives **1,016.938 seconds /approximately 664.16 MiB**, below the cumulative **1,800 seconds /2 GiB**. The external pin is covered by reserved closeout headroom.

| Owned process | Seconds | Outcome |
| --- | ---: | --- |
| Read-only admission/history preflight | 55.359 | Passed |
| Public allocation and search, including native publication | 247.688 | Passed |
| Public saved-archive native audit | 20.031 | Passed |
| Independent saved-row audit | 44.672 | Passed |

The enclosing total additionally includes authentication, full-history closeout and evidence publication. All process receipts report exit 0, no timeout and zero active descendants. Audits added zero fights and zero values. Earlier engineering work, including wrapper development, helper checks and documentation, remains separately disclosed under the accepted treatment; no complete historical engineering total is inferred or reset.

## Evidence and verification

- [Native result](../TestResults/balance/tower-practical-three-reference-20260922/result.json) and [study](../TestResults/balance/tower-practical-three-reference-20260922/study/study.json).
- [Independent audit](../TestResults/three-reference-practical-execution-20260922/independent-audit.json), [completion receipt](../TestResults/three-reference-practical-execution-20260922/completion.json), and [execution package pin](../TestResults/three-reference-practical-execution-20260922-pin.json).
- [Final full-history file inventory](../TestResults/three-reference-practical-execution-20260922/live-history-files.json).
- [Once-only execution helper](analysis/run-three-reference-practical.py), [independent audit](analysis/audit-three-reference-practical.py), and [audit regression checks](analysis/test-three-reference-practical-audit.py).

```text
Scientific archive files.json SHA-256:
92c0245d89afbf1a8776705577fbe18dc86c4410e8c2a7ef6a76f570978fc389

Study files.json SHA-256:
63be8d8f501ed91ef0529fe773ad8ed68a8f7e54d464e0a81d00ec4e65d7a114

Execution evidence files.json SHA-256:
e3481e635b790726e6f7d4c0b4af07597ab17e22b86074a3c30b1bb4aa49968d
```

The independent audit read every compressed battle report and reconciled its outcome with trial rows, measurement cells and confirmation evidence. It checked stage panels, durable attempts, freeze dependencies and chronology, all three references, five nominees, selection arithmetic, every exported rate, adjusted intervals, all paired contrasts, recommendation gates, exact seed-free recipes, required copies, subgroup mapping and complete prior history membership. Native reconstruction separately checked allocation derivation and producing-runtime evidence.

Verification commands were the four Python audit regression tests, syntax parsing, the once-only wrapper's pinned preflight, public `tower-practical-search-allocate-run`, public `tower-practical-search-verify`, independent Python audit, and scoped source/document checks. A mistyped endpoint expectation in the initial Python test invocation was corrected before launch; all four cases then passed. The scientific run and both audits passed without correction or repetition. Backend tests were not repeated because no C# source changed; the [implementation verification](Tower-Practical-Three-Reference-Search.md#verification-and-changed-files) remains pinned. No required command is blocked.

Changed files are the three analysis helpers, this execution report, the admission/implementation/reuse/state pointers, and the harness README and practical-search guide. Captured evidence is under `TestResults`. There are no application configuration changes, migrations, database operations or deployments.

The [saved-evidence coverage and selection review](Tower-Practical-Three-Reference-Coverage-Review.md) is complete. All 17 fresh teams went 0/8, while both generated nominees came from the confirmed reference's lineage near the end of the run. Retention and selection reconstructed correctly; unconfirmed alternatives have unknown strength. Its [opt-in periodic reference exploration hypothesis is now implemented](Tower-Practical-Reference-Exploration-Implementation.md), with 275 scoped backend tests passing and no scientific search or new registry values. Its benefit still needs a prospective comparison and new captured-runtime admission. This run remains closed and its unused allowance is not transferred.
