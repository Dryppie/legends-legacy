# Barrier attribution: bounded validation proposal

**Proposed, not authorized or executed.** Validate the new observer on the two unchanged incumbent parties, then describe damage delivered before Seal pulse deadlines. The [implementation](Tower-Barrier-Attribution-Implementation-Review.md) passed 30 synthetic checks, but full-encounter parity and attribution completeness remain unmeasured. The previous diagnostic launcher explicitly accepts only schema 1, so pointing it at the new observer is insufficient.

This scope proposes **12 fresh values and at most 40 combat executions**, conditional on readiness and parity. It requests **60 seconds and 16 MiB transferred from unused run capacity to engineering**, with no increase to overall limits. No transfer, launcher adaptation, build, test, preparation, allocation, combat or replay occurs in this planning step.

## Fixed inputs and runtime identities

Use exactly these saved parties, with their existing equipment, subgroup assignments and ordinal, outcome-independent Essence/ability order:

| Party | Frozen recipe |
|---|---|
| Anchor 040e | [saved scenario](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-cc1f4a8c1bea7fde79d5cc317c2da802858b5260619d0069ea2684fe87200730.json) |
| Anchor 49f6 | [saved scenario](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-f7315130ac1ae89b8d5ec0839f9cd35b2e6d2b7508ed234bf02730e74a361f30.json) |

Retain the captured floor-5 Kharad encounter, ten level-40 characters, five Essences each, existing family legality, rank/tier/style settings, content and inventory assumption. Readiness must materialize and verify exact native inputs against the saved comparison's settings and prepared anchor definitions. Only prospective scenario identity and fresh seed panels may differ from the historical study. This is not another Web Weaver comparison; its failed package stays closed.

The pre-attribution control uses the captured runtime in `tower-mechanic-telemetry-20260916/runtime`. The updated runtime uses the same dependencies and content, replacing only `Services.LL.dll` with the successfully built attribution assembly in `tower-barrier-attribution-20260916/service/bin/Services/release`. The [proposal](../TestResults/balance/tower-barrier-attribution-validation-planning-20260916/proposal.json) and [pins](../TestResults/balance/tower-barrier-attribution-validation-planning-20260916/input-pins.json) fix those bytes. Reconcile them with the last comparison's runtime inventory before execution; any unexpected difference fails readiness. No production rebuild is proposed.

Use separate processes and explicit assembly inventories for the old and updated runtimes. A host must never load the wrong `Services.LL` through a fallback resolver. If separate compatible host binaries are needed, compile them within readiness and pin them before allocation. Native admission and static identity checks must not enter combat.

## One prospective schedule

| Stage | Fixed schedule | Maximum executions |
|---|---|---:|
| Parity | 2 anchors ×4 shared fresh values ×3 modes | 24 |
| Attribution, only after parity passes | 2 anchors ×8 different shared fresh values, updated observer enabled | 16 |
| Total | 12 distinct fresh values; no optional extension | **40** |

The parity modes are the old runtime with observation disabled, updated runtime disabled, and updated runtime with schema-2 attribution enabled. All three receive byte-identical materialized input for each party/value. Require equality of the complete ordinary compact report's canonical JSON hash and serialized bytes under identical serializer settings. Strip no fields and permit no numeric tolerance in this parity comparison. Stop immediately on a mismatch. A passing finite panel establishes sampled parity, not universal equivalence.

Fix the shared collector cap at **65,536 events per observed fight** and retain the existing Seal, Resonance and pillar filters. Explicitly enable `CaptureBarrierDamage`. Bind schema 2, the new event kind and its metadata in the launcher validator; the existing schema-1 validator cannot be reused unchanged. Require completed, untruncated traces with zero drops, monotonic event order within the final tick, valid identities and amounts, and agreement with ordinary-report duration and participant identities. No rerun at a higher cap is permitted.

After authorization, freeze one master and domain `tower-barrier-attribution-validation-v1` before any candidate derivation. Reserve once with disjoint 4/8 panels, a fixed 100,000-candidate ceiling per panel and the existing Pending/Start/Candidate durability requirements. All 12 accepted values remain excluded if later stages stop. No retry, resume, replacement value, historical replay or construction seed is allowed.

The latest authoritative ledger is `tower-coordinated-party-comparison-20260916/binding/seed-ledger.json`. Readiness must perform recovery-aware production registry Refresh/Recheck, retain the original Pending recovery mapping and every required history file, and require the complete **483,720**-value exclusion union. Recheck immediately before derivation. Planning hashes preserve known files but do not substitute for live registry reconciliation. V19's 512 unused confirmation values remain excluded and unused.

## Readiness, accounting and stopping rules

Adapt a new isolated launcher; never modify or relaunch a sealed experiment. Bind protocol/proposal hashes, exact inputs, runtime inventories, schema/filter/cap, schedule, counters and phase ceilings. Reuse the passing 30 observer checks only while their source/dependency pins match. New launcher checks must use literal synthetic allocator callbacks and cover the 24+16 schedule, three-mode runtime selection, authorization mismatch, once-only launch, attempt/completion bounds, parity failure, schema-2 rejection paths and saved-evidence reconstruction. Backend verification uses `build/run-tests.ps1`. No combat is allowed during readiness.

For every observed fight, preserve raw report and trace, then independently reconstruct native accounting from the saved JSON. Match each barrier's target/effect/activation/application order. Reconcile accepted amount against precise damage contributions and timeout remainder using the implementation's fixed rounding tolerance. Reject unexplained residuals for ended watched barriers, including possible non-damage spending: they make complete attribution unavailable, not evidence against a particular attacker. Keep missing/unfinished barriers explicitly censored. Never replace their unknown remainder with zero or hide them from the denominator.

Use actual event order for same-tick consumption and pulse applications. Deduplicate target applications using activation/effect/tick. Report attacker-level contributions and optional effect-level contributions separately; null effect IDs stay unknown and display labels are not canonical ability IDs. Require every attacker to resolve to a known participant or explicitly identified summon. Health overflow and other barrier contributions must not be counted toward Seal.

Publish the first three Seal opportunities per fight, including absent and unfinished opportunities, with accepted amount, actual end/pulses, contributions before each pulse, and remaining barrier inferred only while the account reconciles. Show absolute amounts as well as shares. Preserve whole-fight context separately. For opportunity comparisons use fight-level summaries; do not treat individual hits or pulses as independent samples. Keep parity and diagnostic panels labeled, with the eight-value diagnostic panel as the primary descriptive panel. Do not pool outcomes with historical trials, rank the anchors by this small panel, nominate a winner or tune a replacement from an exploratory maximum.

The prospective utility criterion is **at least 6 of the 8 diagnostic fights per anchor with all first three Seal opportunities present, ended and reconciled**. Report exact eligible/total counts and reasons for exclusions. This gate concerns whether the instrument delivers usable timing data, not team strength. Natural missing or unfinished opportunities may produce `AttributionInsufficient`; they do not justify extra values. Any malformed trace, unexplained ended-barrier residual, parity mismatch, identity/history/counter discrepancy or resource ceiling stops the run as `DiagnosticInvalid`. Cancellation also ends the once-only run. If integrity and coverage pass, close as `DescriptiveAttributionComplete`.

Passing this scope authorizes no new recipe or comparison. Its useful output is a concrete description of which contributions arrive before a pulse deadline and which arrive too late. If that description remains incomplete or does not support a specific party mechanism, stop instead of producing another small search variant. Any later replacement requires its own frozen complete recipe, independent evaluation, outcome and mechanism gates, and stopping rule.

## Resource transfer and phase ceilings

The prior [implementation receipt](../TestResults/balance/tower-barrier-attribution-20260916/completion.json) leaves **1.667880138498731 engineering seconds /4,994,879 engineering bytes**. This planning step conservatively charges **1.5 seconds /1 MiB**, leaving **0.167880138498731 seconds /3,946,303 bytes**. Its [receipt](../TestResults/balance/tower-barrier-attribution-validation-planning-20260916/completion.json) carries all balances forward. These remnants cannot fund readiness.

Requested once-only transfer, applied only after explicit approval and carried-usage reconciliation:

| Component | Current cap | Proposed cap |
|---|---:|---:|
| Engineering time | 1,325 seconds | 1,385 seconds |
| Run time | 1,975 seconds | 1,915 seconds |
| Engineering output | 654,311,424 bytes (624 MiB) | 671,088,640 bytes (640 MiB) |
| Run output | 385,875,968 bytes (368 MiB) | 369,098,752 bytes (352 MiB) |

Audit caps remain 300 seconds /33,554,432 bytes. Overall limits remain **7,980 seconds /5,804,916,736 bytes**. Do not reset consumed amounts, refund earlier conservative charges or apply an earlier transfer again. Before applying this new transfer, reconcile carried run/audit consumption through the receipt chain and require sufficient unused source capacity; otherwise stop before builds or allocation.

| Proposed phase | Component charged | Time ceiling | Output ceiling |
|---|---|---:|---:|
| Launcher readiness, including corrections and failed attempts | Engineering | 50 seconds | 16 MiB |
| One reservation and parity/diagnostic run, including cleanup | Run | 180 seconds | 64 MiB |
| Independent saved-evidence verification | Audit | 45 seconds | 8 MiB |
| Documentation, preservation and final publication | Engineering | 10 seconds | 2 MiB |

The two engineering phases together use at most 60 seconds /18 MiB; after the proposed transfer, available engineering capacity would be 60.167880138498731 seconds /20,723,519 bytes. All four phase ceilings total **285 seconds /90 MiB**, within the remaining overall balance. Preserve separate phase limits; no moving unused capacity after outcomes. Count transient and overwritten output, prior shared test-result backups, process cleanup and publication reserves. Reuse pinned runtime dependencies in place where feasible. Use the existing bounded process-tree guard and stop before exceeding any component or cumulative ceiling.

**Approval requested:** transfer 60 seconds and 16 MiB from unused run capacity to engineering, perform bounded readiness, then reserve exactly 12 fresh values and execute at most 40 fights only after every prerequisite gate passes, followed by the fixed audit/publication. This is one diagnostic scope, not an open-ended continuation.

Planning changes are this protocol, its proposal/pins/preservation/receipt package, and one current notice in the strategy handoff. Static verification checks pinned artifacts, current source hashes against the successful build, known seed-history files, links, preserved dirty files and budget arithmetic. Builds, tests, native preparation, live registry reconciliation and combat are intentionally deferred until approval; none is claimed complete here. No production source, game content, migrations, persistent configuration or deployment changes. Adoption stays **Hold**, prior pilots/diagnostics **Closed**, V19 **Unresolved** with 253 required recipes and no confirmation.
