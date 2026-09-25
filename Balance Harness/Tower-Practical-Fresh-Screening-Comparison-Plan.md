# Fresh screening before practical-search nomination

22 September 2026. Target: offline `LL/tools/BalanceHarness`. This is a prospective comparison of two complete search pipelines at the same **528-fight search budget**. The [machine contract](Tower-Practical-Fresh-Screening-Comparison-Plan.json) is frozen as `tower-practical-fresh-screening-comparison-v1`, SHA-256 `13fc98e45afb7a96403918ffe0233fd1dcb375a52fb9446b63053911cc94c263`. The [implementation report](Tower-Practical-Fresh-Screening-Implementation.md) tracks engineering verification separately. The [captured-runtime admission](Tower-Practical-Fresh-Screening-Admission.md) and [single scientific execution](Tower-Practical-Fresh-Screening-Comparison-Execution.md) are complete; the result is `DoNotPromoteFreshScreening`. The frozen JSON contract remains unchanged, and the allocation is closed.

The [48-search review](Tower-Practical-Search-Stage-Review.md) found coarse discovery nomination ties and sensitivity in final selection. Fresh screening is a hypothesis about better allocation of limited fights. The closed original and offset exploration comparisons do not establish its benefit. The screening width follows the budget identity below; confirmation outcomes were not used to tune it.

## Pipelines and stage boundaries

Both arms use `retained-composition-three-references-v1`: three exact supplied references, 46 evaluated recipes, at most 256 proposals, the same construction operators and a common construction root per pair. They keep the captured floor-5 encounter, ten level-40 characters, five Essences per character, one fixed equipment context, actor identities, content and effective settings. The three reference identities and designated incumbent are pinned in the JSON contract.

| Stage | Baseline: `tower-practical-direct-nomination-v1` | Candidate: `tower-practical-fresh-screening-v1` |
| --- | --- | --- |
| Adaptive discovery | 46 ×8 =368 fights | 46 ×4 =184 fights |
| Fresh screening | None | Three references +20 discovery-ranked challengers; 23 ×8 =184 fights |
| Final selection | Three references +two discovery-ranked challengers; 5 ×32 =160 fights | Three references +two screening-ranked challengers; 5 ×32 =160 fights |
| Total search | **528** | **528** |

After complete candidate discovery, freeze all 23 screen members in discovery rank order, with the bound definition, discovery hash, ordered fresh screening seeds and 184-fight boundary. Persist that checkpoint before any screening fight. Measure every member on all eight fresh values. Rank screening results alone by descending win rate, ascending remaining guardian health, descending survival, ascending winning duration and ordinal party ID. Protect all three references even when they score below challengers. Freeze those references and the highest two challengers, in screening rank order, before final selection.

The ordinary generator still records its historical five-member discovery shortlist. That is an audited discovery checkpoint; the candidate's explicit screening result supplies the final five selection inputs. No discovery score is blended into screening fitness. No screen, nominee or missing measurement is replaced, shortened or filled from discovery after a failure.

Final selection remains `tower-staged-incumbent-tie-v1`, with 32 fresh trials per nominee. Its existing positive-win incumbent preference and zero-win behavior are unchanged. Screening order supplies its final rank tie-breaker in the candidate arm. After twelve pairs complete both searches, freeze all 24 selected outputs and the physical confirmation families before any confirmation fight.

Four-trial feedback can change adaptive parents and later recipes even under the same generator and root. This experiment therefore compares the **complete pipelines**, not screening applied to an identical candidate set. Equal fight counts do not guarantee equal elapsed time.

## Freshness and confirmation

One 16,384-word entropy batch supplies distinct fresh signed 32-bit values after full historical exclusion. Reserve every fresh word, including the unused tail. No refill, retry, replacement restart, resume or sample extension is permitted.

Assign twelve 49-value blocks first. Each block contains, in order:

1. One shared construction root.
2. Eight baseline discovery values; candidate discovery uses their first four.
3. Eight separate candidate screening values.
4. Thirty-two common selection values.

Then assign twelve 1,000-value common confirmation panels. Search reserves **588 values**, and the total assignment is **12,588**. Every distinct role panel is disjoint; the documented within-pair sharing is intentional. The same recipe in different search arms is measured independently. Confirmation measures the physical union of the three controls and both selected outputs once per pair, requiring three to five recipes.

The experiment uses **12,672 search fights** and **36,000–60,000 confirmation fights**, for **48,672–72,672** total. No partial pair is analyzed as a substitute for completion. The current history is 584,171 exclusions across 236 files; admission and immediate prelaunch checks must rescan the entire supported registry, including reconciled abandoned reservations.

## Frozen endpoint and decision

Use the mean paired confirmation win-rate difference across all twelve output pairs, conditional on the complete search. Identical outputs contribute zero and remain in the 12,000-trial denominator. Let `k` be the number of nonidentical output pairs, `n=1000k`, `H` the historical exclusion count and `net` the total paired gains minus losses:

```text
mean = net / 12000
depletion = n(n-1) / ((2^32 - H - 588) × 12000)
margin = sqrt(2n × ln(20)) / 12000 + depletion
lower = max(-k/12, mean - margin)
```

This retains `conditional-range-hoeffding-depletion-v1`, with the new reserved search count. Support requires all three gates: **net ≥600**, **lower >0**, and **at least seven positive pairs**. The decisions are `SupportsFreshScreeningForFrozenOutputs`, `DoNotPromoteFreshScreening`, or `NoSelectedOutputDifferences`. The descriptive per-arm comparisons retain their ten-quantity practical interval family and make no team-adoption claim. Support is conditional evidence for these frozen outputs; it does not automatically change a default or establish general search superiority.

## Resource and execution boundary

The proposed cumulative allowance is **10,800 seconds /6 GiB**, including **600 seconds /512 MiB** for admission. Execution, its two saved-row audits and publication share **10,200 seconds /5.5 GiB**; native execution is limited to **10,080 seconds /5.25 GiB**. A single suspended Windows Job owns each child tree before it starts, and failure preserves partial evidence and all exposed reservations. Independent and native audits share the remaining deadline rather than receiving new allowances.

Before allocation, capture the tested harness alongside the original gameplay dependencies, content and settings; verify input preparation, screening paths, source bindings and the independent auditor against that runtime; and seal a newly bound request. Earlier captured executables and requests cannot be reused for this version. The existing launch command accepts the new request version only after those checks; this design document is not runtime admission.

Engineering work remains separately disclosed. Complete historical engineering totals remain unknown, and the reconciled **18,180-second /13,584-MiB** prior-charge ledger is preserved. No unused scientific allowance transfers into this implementation. No migration, application configuration change or deployment is involved.
