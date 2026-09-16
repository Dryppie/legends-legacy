# Keep supplied anchors eligible for independent selection

**Reserve two of the four selection places for the two admitted anchors, and give the remaining two places to the best distinct challengers by the existing discovery rank.** Publish this as the opt-in `retained-composition-incumbents-v1` policy. Preserve `retained-composition-v1` and all earlier policies exactly. This changes nomination after discovery; it does not introduce new construction operators or change ability order.

The [completed practical pilot](Tower-Retained-Practical-Pilot-Execution-Review.md) supplies the reason: the stronger anchor ranked third on eight discovery values but missed the four-party shortlist. The other anchor ranked thirteenth. Two nominees with zero discovery wins occupied the diversity places instead. The finalist then won 66/256 confirmations, versus 163/256 and 155/256 for the anchors. Both adjusted comparisons favored the anchors. The pilot remains a failed improvement result; these observations do not measure either anchor on its unobserved selection panel.

## Exact proposed contract

The first version supports the concrete two-anchor case: `improve-supplied`, the single `retained-composition` method, one generation root, one context, exactly two admitted references and two corresponding distinct canonical starting compositions, four shortlist members and one selected finalist. Retain the existing zero-win selection policy, fixed ordinal ability order, legality checks, candidate/proposal limits and generation objective. Reject unsupported shapes before search, rather than silently changing their counts. Single-anchor, multiple-context and multiple-root extensions are outside this implementation.

After discovery completes:

1. Resolve incumbent IDs from the definition's two `Starts`, not from descendants' inherited `ReferenceIds`. Verify each incumbent has an evaluated supplied proposal and a complete discovery measurement. A descendant carrying an anchor's ancestry is still a challenger.
2. Reserve one shortlist place for each distinct incumbent composition. Missing or mismatched incumbents invalidate a purported complete result; duplicates must already have failed admission.
3. Rank all evaluated non-incumbent parties using the unchanged `TowerBossGeneration.Rank` order. Take the best two distinct challenger IDs. If fewer than two exist, do not launch an undersized selection stage or silently refill the places with duplicate anchors; report incomplete evidence.
4. Order the four chosen parties by their existing discovery ranks, preserving the meaning of the current selector's frozen discovery-rank tie-break. Save the shortlist before any selection measurement.
5. Evaluate all four on the same independent selection panel and run the existing selector. An anchor may win. No incumbent preference, new margin, tie-break or confidence rule is introduced in this version.

The new policy replaces the old two diversity nomination places with two guaranteed incumbent places. This deliberately reduces challenger breadth. It protects eligibility, not true performance: noisy selection can still choose a worse challenger, and an equal-win tie can still favor a challenger by discovery rank. Any rule requiring confidence before replacing an incumbent is a separate statistical decision, not part of this fix.

Search proposals, random streams, discovery evaluations, retention, parent choices, rejection accounting and mutation operators must be identical to the existing standalone policy for the same inputs and callback measurements. Keep the existing random namespace; the new version identifies the nomination contract. Completed reports differ in the declared policy version and shortlist; incomplete/cancelled generation keeps its existing status and empty shortlist.

## Fixed future evaluation budget

| Stage | Existing pilot | Proposed nomination policy |
| --- | ---: | ---: |
| Discovery, including both anchors | 64 ×8 = 512 | 64 ×8 = 512 |
| Independent selection | 4 ×32 = 128 | 2 anchors +2 challengers, each ×32 = 128 |
| Confirmation maximum | 3 ×256 = 768 | Selected party +2 anchors, each ×256 = 768 |
| Total maximum | **1,408** | **1,408** |

The unchanged generation allowance is at most 256 proposals for 64 evaluated parties. Anchors are scored within that allowance. No fifth selection place is added. If an anchor is selected, exact-recipe confirmation merging preserves its selected and reference origins, reducing actual confirmation to 512 fights and total execution to 1,152. The 256 unused fight slots cannot fund more proposals, samples or another run.

A future run would require its own prospective authorization and fresh, mutually disjoint generation/discovery/selection/confirmation values, excluded against the full live history union (currently 483,343). No values are chosen, derived or requested here. The old eight-value discovery, 32-value selection and 256-value confirmation panels are not reusable for a new combat decision. Historical confirmation results may motivate this design but must not determine future panel allocation or fit a new threshold.

Returning an anchor is **incumbent retained**, not a newly discovered improvement. Returning a novel challenger only establishes improvement if a separately frozen independent confirmation rule passes. For continuity, a later protocol can retain the pilot's novelty requirement, 10% adjusted win-rate lower bound, at least five observed percentage points against each anchor, and positive seven-component adjusted paired lower bounds. This document does not authorize that run or claim algorithm superiority. Both anchor win rates above 50% remain visible; the generic balance ceiling is separate from practical team strength.

## Minimal implementation boundary

| Location | Proposed change |
| --- | --- |
| [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs) | Add the opt-in version, recognize its single-method contract, and dispatch a small incumbent shortlist helper after unchanged generation. Pass the validated definition so exact start IDs are available. Leave the old shortlist branch untouched. |
| [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) | Validate the narrow new policy shape, two distinct matching starts and references, and existing stage arithmetic before execution. Preserve older validation behavior. |
| [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Allow an explicit `--policy-version retained-composition-incumbents-v1` on `tower-retained-composition-prepare`; retain the existing default and reject block policies on that command. Preparation remains zero combat. |
| [TowerCompositionSearch.cs](../LL/tools/BalanceHarness/TowerCompositionSearch.cs) | Verify the shared composition-only classification recognizes the newly supported policy; change it only if it does not already delegate to the supplied-policy predicate. |
| Tests and fixture data | Add focused nomination/selection/merging regressions and exact old-policy parity checks using synthetic or saved scalar measurements, with no real battles. |
| README and handoff notices | Document opt-in behavior, the unchanged budget and limitations after verification. |

The existing [selection and confirmation policy](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs) already validates and selects legal shortlist members, then merges exact finalist/reference recipes. No selection algorithm change is needed. Its documentation should clarify that a supplied composition can be a shortlisted candidate, while confirmation measurements remain unavailable until the selected party freezes. Reuse the repaired [input projection](../LL/tools/BalanceHarness/TowerBossImprovement.cs); do not manufacture an independent two-method definition when validating the exact 1,408-fight budget.

## Verification required before completion

Use backend tests through `build/run-tests.ps1`, with a scoped filter and an isolated artifacts path. Reuse pinned gameplay dependencies and compile the affected harness/test code; retain source and executable identities. Synthetic integer labels are fixture inputs, not fresh production reservations. No test may call the real combat resolver or reservation candidate allocator.

- **Observed nomination regression:** fabricate legal evaluated parties with two challengers ranked first/second, anchors third/thirteenth, and distant zero-win parties. Assert the new shortlist contains both exact anchors and the two best non-anchor challengers; preserve discovery-rank ordering. The old policy must still select its original two diversity representatives.
- **Identity and completeness:** descendants with inherited reference ancestry do not fill incumbent places; reject duplicate/mismatched starts and references, missing supplied measurements, unsupported policy shapes, and incomplete challenger sets. Test pre-cancellation and proposal exhaustion without selection or extra evaluations.
- **Selection outcomes:** fabricated independent outcomes can select an anchor or a superior challenger. Positive-win ties retain discovery rank; zero-win ties retain the existing health rule. This verifies eligibility and selection mechanics, not combat strength.
- **Accounting and freeze:** synthetic end-to-end studies at an exact 1,408 maximum must complete with the correct 512/128/768 counts for a novel finalist, and 512/128/512 when an anchor merges. Preserve both origins, forbid spending unused capacity and verify saved-study reconstruction with a zero-combat guard.
- **Compatibility:** existing standalone fixtures and supplied scheduling/parity tests still pass. On the same synthetic root and measurements, old/new standalone discovery arms must match exactly, including rejected proposals and ancestry; only the declared policy identity and completed shortlist may differ. The old standalone default and paired block behavior remain unchanged.
- **Native preparation:** run the existing real-content admission path with explicitly synthetic schedules and matching current identity, without executing search or deriving production values. Confirm the two saved anchors remain legal and source recipes are unmodified.

Stop the implementation as unverified if legality, discovery parity, budget, merge or reconstruction checks cannot pass within the bounded engineering scope. Do not launch combat, tune nomination further from outcomes, revive the failed block-search proposal or modify sealed evidence. Passing these checks establishes a working nomination policy only; a later independent study would still be needed to measure its practical benefit.

## Resource approval needed for implementation

The [sealed execution receipt](../TestResults/balance/tower-retained-practical-pilot-execution-20260916/completion.json) leaves 8.1978801384248 engineering seconds and 1,232,681 engineering bytes before this design. It also records 2,813.152607782739 diagnostic seconds and 582,511,715 bytes remaining overall. These are inherited balances, not a new allocation. This design/publication scope is capped at **7 diagnostic seconds /1 MiB**, charged to engineering; it runs no builds, backend tests, preparations, combat, replay or fresh-value derivation. Its [completion receipt](../TestResults/balance/tower-incumbent-selection-design-20260916/completion.json) carries the balances forward.

Request a **180-second /96-MiB transfer from unused run allowance to engineering**, keeping the overall limits unchanged. Component caps would become engineering **1,045 seconds /480 MiB**, run **2,255 seconds /512 MiB**, and unchanged audit **300 seconds /32 MiB**. The proposed implementation-and-verification scope is at most **180 seconds /96 MiB in total**, including edits, static work, builds, failed attempts, corrective iterations, tests, admission, receipts and publication. Use one combined process-tree/storage bound with a cleanup reserve; preserve failures and stop at exhaustion. This request authorizes zero new combat or production values and no later pilot. No transfer is assumed approved by this design.

Current work adds this design, a non-executable proposal/preservation/accounting package and current README/handoff notices. Static checks verify links, budget arithmetic, the sealed pilot, source/runtime pins, unrelated dirty files and scoped `git diff --check`. Backend commands were intentionally not run because their engineering allocation is pending. There are no production edits, gameplay changes, migrations, persistent configuration changes or deployment implications. Adoption remains Hold, V19 reliability Unresolved, and all 483,343 reservations remain preserved.
