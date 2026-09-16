# Barrier attribution: replacement readiness scope

**Proposed, awaiting budget approval.** Reuse the already built runtime hosts and give the mandatory live history check a dedicated execution window. The [closed attempt](Tower-Barrier-Attribution-Validation-Execution-Review.md) completed no allocation or combat: its only failed launcher test was cancelled during the live registry scan. This proposal preserves that failure and its costs. It does not resume the closed package or change the observer, history scanner, parties, attribution metrics or acceptance gates.

The saved earlier successful comparison executed its full readiness test suite in **31.219 seconds**, with a reported test duration of about **29 seconds**. The cancelled attribution attempt supplied a remaining wrapper allowance of **29.282 seconds** and derived an inner cancellation window of roughly **25 seconds**. These observations support a less constrained, separately budgeted check; they do not predict the next scan's duration or demonstrate a performance regression. Source inspection confirms that `Core.History` performs both production `Refresh` and `Recheck`, each of which scans the complete registry. Neither operation may be omitted.

## Concrete replacement

Use a new direct child of the same complete registry root. Treat the previous package, compiled hosts and source files as immutable inputs. All new authorization, readiness, test, reservation, run and audit output must go to the new package or its own temporary directory. The existing hosts resolve their output package through `LL_DIAGNOSTIC_PACKAGE`, so they can be reused without changing their compiled code or writing into the closed package. Verify every dependency, resolver path and source/build hash before use; an unexplained difference stops this scope. No production or launcher rebuild is proposed.

Reuse the previous **15 passing launcher cases**, two passing independent attribution fixtures and **30 passing observer checks** only while their exact source, compiled artifacts, runtime/content/settings and relevant inputs remain pinned. Rerun native admission for both modes and compare against the saved admitted anchors. Then invoke only `DiagnosticTests.Complete_live_registry_and_original_recovery_are_reconciled` through `build/run-tests.ps1 -NoBuild`, using the preserved test artifacts and the new package/fixture environment. The explicit filter and the exact one-test executed/passed count must be verified. Preserve and restore the shared TRX, retaining both versions.

Give this registry-test process **100 seconds of work time**, with cooperative cancellation at **97 seconds** and **one second reserved for process-tree cleanup**. Before starting, require at least **102 seconds left in the 120-second readiness phase**. Do not silently shrink the test's window to whatever remains after setup. Abort before the test if setup has consumed its reserved capacity. No other test suite, build or benchmark may consume this dedicated window. This is one attempt, without retry or automatic extension.

The test must complete the existing recovery-aware `Refresh` and `Recheck`, retain the original Pending/recovery mapping, require every previously recorded history file and reconcile exactly **483,720** exclusions against the authoritative ledger. Pin its newly produced membership/hash result. Existing known-file checks do not replace this live gate. If the test fails or is cancelled again, close the replacement scope before allocation and report the failure; do not create another automatic readiness variant.

After this gate passes, seal a new readiness record against this replacement authorization, the unchanged original scientific protocol, both hosts and all reused evidence. Run the existing pre-allocation live recheck again during binding; the readiness snapshot is not permission to skip that check. All failures, corrections, temporary files and cancellation/cleanup costs count against the frozen phase ceilings.

## Unchanged diagnostic contract

All experimental details and stopping rules in the [original validation protocol](Tower-Barrier-Attribution-Validation-Protocol.md) remain fixed except for the readiness execution and resource allocation expressly replaced here:

- The exact two incumbent parties, captured floor-5 Kharad content/settings, legal builds, fixed equipment/subgroups, inventory assumption and outcome-independent ability order remain pinned.
- At most **12 fresh values /40 fights total**: four shared parity values across two parties and three runtime modes (**24 executions**), followed only after exact complete-report byte/canonical parity by eight different shared values across the two observed parties (**16 executions**).
- Keep the old runtime, new runtime with observation off, and new runtime with schema-2 attribution on in separately controlled processes. Keep all existing filters, the **65,536-event** bound, actor/effect identity rules, residual checks, censoring rules and the **six-of-eight complete first-three-Seal fights per anchor** utility gate.
- Preserve the original frozen master **2026091605** and domain `tower-barrier-attribution-validation-v1`. The previous package derived no candidates, so these identify the same unexecuted proposal, not reused balance observations. Before any derivation, verify the absence of prior production binding/attempt artifacts and freeze them in the new authorization. Use the unchanged disjoint 4/8 reservation panels and durable Pending/Start/Candidate journal, once only, with no construction values, retry, resume, replay or extra samples.
- Full native and independent saved-evidence verification remain required. Keep panels labeled and describe contributions without ranking a winner or claiming team strength. A failed integrity gate ends the attempt; insufficient natural coverage ends as `AttributionInsufficient`, without extra values.

The original package remains `DiagnosticInvalid` and closed. Its unused numerical allowance is not automatically live. Approval of this replacement would authorize one new execution package under the same **12/40 ceiling**, not an additional completed experiment or an open-ended continuation. No fresh values or combat are requested outside that ceiling.

## Budget and approval

The [latest receipt](../TestResults/balance/tower-barrier-attribution-validation-20260916/completion.json) leaves **4.1218801384966355 engineering seconds /11,750,273 engineering bytes**. This planning step charges **3 seconds /1 MiB**, leaving **1.1218801384966355 seconds /10,701,697 bytes**. See its [planning receipt](../TestResults/balance/tower-barrier-attribution-readiness-recovery-planning-20260916/completion.json).

Request a once-only **130-second transfer from unused run capacity to engineering**. No byte transfer is needed. Engineering's time cap would change **1,385 →1,515 seconds**, and run's **1,915 →1,785 seconds**. All byte caps, audit caps and overall limits remain unchanged. Prior transfers and all failed-work charges stay applied; do not refund or repeat them. Reconcile the receipt chain before applying the new transfer.

| Phase | Charged component | Time ceiling | Output ceiling |
|---|---|---:|---:|
| Reuse validation, two native admissions, isolated history test and readiness seal | Engineering | 120 seconds | 4 MiB |
| One reservation and parity/diagnostic execution, including pre-allocation recheck and cleanup | Run | 180 seconds | 64 MiB |
| Independent saved-evidence verification | Audit | 45 seconds | 8 MiB |
| Documentation, preservation and closure | Engineering | 10 seconds | 2 MiB |

After planning and the proposed transfer, engineering would have **131.12188013849664 seconds /10,701,697 bytes**, sufficient for its **130-second /6-MiB** combined ceiling. The complete replacement has a **355-second /78-MiB** ceiling, within the remaining overall balance. Carried run use is **258.3269999999902 seconds /257,405,253 bytes** and carried audit use is **134.99800000003597 seconds /5,842,036 bytes**; the proposed phase ceilings fit their remaining allocations. Before execution, reconcile these figures rather than counting cumulative carried fields as new work.

Freeze the deadlines before starting. Preserve process-tree guards, output monitoring, temporary/overwritten-output charges and cleanup/publication reserves. No capacity moves between phases after results. If execution cannot fit these ceilings, stop and close it.

**Approval requested:** transfer 130 seconds from unused run capacity to engineering and authorize this replacement scope, with the dedicated history-check window and the same conditional 12-value /40-fight ceiling. No additional byte transfer, recipe change, history shortcut or performance work is included.

Planning changes are this protocol, its proposal/pins/preservation/accounting package and one current strategy-handoff notice. Static checks cover the saved failure and earlier successful timing evidence, source/build/runtime pins, known history bytes, links, budget arithmetic and unrelated dirty-file preservation. No build, backend test, live scan, native preparation, transfer, derivation, combat or replay occurred in planning. No production source, content, migration, persistent configuration or deployment changed. All 483,720 exclusions remain; adoption stays **Hold**, prior scopes **Closed**, V19 **Unresolved** with 253 required recipes and 512 unused confirmation values.
