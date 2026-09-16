# World Tower mechanic diagnostic: complete

**The approved diagnostic completed all 72 executions. All 12 parity inputs produced identical complete ordinary-report bytes and canonical hashes across the old runtime, the new runtime without observation, and the new runtime with observation. Independent saved-evidence verification passed.** This establishes sampled parity and usable observations, not universal equivalence or a stronger team.

The main finding is that these teams already break most Seal barriers, while no pillar was killed. Permanent Resonance lock was uncommon and usually very late. The useful next question concerns earlier Seal breaks and timely pillar damage while retaining party survival; the data do not justify treating permanent lock as the dominant explanation for losses.

## What was observed

The fixed panel used both unchanged anchors and the measured challenger on four shared parity values, then twelve separate shared diagnostic values. The 48 observed fights below count each team/value once. The 24 unobserved parity executions are excluded from mechanic summaries. No historical confirmation outcome is pooled into this diagnostic.

| Team | Seal break / timeout / active at combat end | Median completed Seal duration | Pillars spawned | Timed expiry / owner-death cleanup / active | Locked fights |
| --- | ---: | ---: | ---: | ---: | ---: |
| Anchor 040e | 74 /4 /7 | 4.0 s | 120 | 106 /6 /8 | 2/16 |
| Anchor 49f6 | 76 /4 /3 | 3.9 s | 120 | 100 /14 /6 | 1/16 |
| Challenger a0f9 | 70 /5 /6 | 3.8 s | 116 | 96 /10 /10 | 2/16 |

Across the panel, **220 of 249 Seal starts ended in a break**, 13 timed out and 16 remained active at combat end. Of the 233 completed contributions, 220 broke. Completed duration is the end-tick minus start-tick interval; it excludes the censored contributions rather than pretending that combat end was an expiry. The observed traces contain **3,799 linked target application attempts across 452 distinct pulse waves**. These are application counts, not damage totals or independent samples.

**Zero of 356 pillars was killed.** There were 302 timed expiries, 30 owner-death cleanups and 24 members still active at combat end. All 151 recorded timed group resolutions had both members surviving to the deadline. The trace independently distinguishes these routes, and its member timestamps/counts reconcile with the ordinary compact statistics. This directly resolves the ambiguity in the old aggregate expiry counter for this new panel; it does not retrospectively assign reasons to the old 5,454 end events.

Resonance locked in **5/48 fights**, all losses. Three ended 0.1 seconds after first lock; the other two ended 1.0 and 11.1 seconds after lock. Total time at five stacks was **12.4 seconds**. Eighteen of the 23 losses occurred without lock. All 13 Seal timeouts occurred in losses; winning fights broke all 125 of their Seal contributions. These are descriptions conditioned on the eventual outcome: weakening damage output and deaths can cause late timeouts/lock, so the associations do not establish causation.

| Team | Parity-panel outcome context | Diagnostic-panel outcome context |
| --- | ---: | ---: |
| Anchor 040e | 2/4 wins | 4/12 wins |
| Anchor 49f6 | 3/4 wins | 8/12 wins |
| Challenger a0f9 | 4/4 wins | 4/12 wins |

These small outcome counts supply context for the mechanic observations. No winner was selected, no superiority threshold was evaluated, and no recipe was changed. The closed practical pilot's result remains **ImprovementNotDemonstrated**. Its earlier 256-value confirmation remains separate; adoption stays **Hold**, V19 reliability **Unresolved**.

## Execution and verification

Execution followed the [prospective protocol](Tower-Mechanic-Diagnostic-Protocol.md). The target, ten-character cohort, fixed equipment, five Essences per character, subgroups, neutral identities and ordinal outcome-independent ability order were unchanged. Inventory sufficiency remains assumed through `OwnedCopies=null`. No search, construction root, tuning, historical replay, retry, resume or extra sample was run.

Readiness verified the producing old/new execution identities in separate processes and identical prepared input/participant hashes for all three recipes. The full recovery-aware history scan reconciled **192 files /483,640 exclusions**, including the original Pending source and explicit recovery mapping. Binding repeated the full live reconciliation immediately before allocation. The pinned master/domain then durably reserved exactly **16 values**, with 16 candidates and zero collisions. The complete exclusion union is now **483,656**; V19's 253 required recipes and 512 unused confirmation values remain intact.

The once-only run completed 12 old-runtime baseline fights, 24 new-runtime parity fights, then 36 observed diagnostic fights. Attempt/completion journals match all 72 saved reports; 48 complete, untruncated traces passed validation at the fixed 65,536-event cap. Both the native saved-input/report reconstruction and the separately implemented Python audit passed without combat. The audit independently checks recorded allocation derivations, frozen schedules and recipes, counters, full parity, trace relationships, group survivors, stack/lock history and compact-statistic reconciliation.

The focused launcher tests ran through:

```powershell
build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-mechanic-diagnostic-20260916/tests -Filter 'FullyQualifiedName~DiagnosticTests'
```

**Final result: 12 passed, zero failed or skipped.** The prior 16 telemetry checks were reused under matching source/runtime pins. The first readiness invocation passed ten checks and failed two: a mixed-slash authoritative-ledger path failed required-source matching, and indented JSON made the interrupted-fight fixture's journal multiline. Both isolated-launcher issues were corrected; both launchers rebuilt without warnings and all twelve checks passed on the second invocation within the original 90-second readiness ceiling. No reservation or combat occurred before readiness passed. All failed output and costs are retained.

No required verification remains unrun. Production rebuilds, broad backend tests and additional combat were outside this scope. Code changes are confined to the isolated evidence package's launchers/tests/workflow; production code and gameplay content are unchanged. This review, README and five handoff notices carry the result. There are no migrations, persistent application configuration changes or deployments.

## Evidence and accounting

- [Per-fight mechanic observations](../TestResults/balance/tower-mechanic-diagnostic-20260916/mechanic-fights.json) and [labeled panel summaries](../TestResults/balance/tower-mechanic-diagnostic-20260916/mechanic-summary.json).
- [Sampled parity receipt](../TestResults/balance/tower-mechanic-diagnostic-20260916/parity.json), [native verification](../TestResults/balance/tower-mechanic-diagnostic-20260916/native-verification.json) and [independent audit](../TestResults/balance/tower-mechanic-diagnostic-20260916/independent-audit.json).
- [Final preservation/accounting receipt](../TestResults/balance/tower-mechanic-diagnostic-20260916/completion.json), including failed readiness, overwritten artifacts, all output and publication costs.

The approved ceilings remain readiness **90 seconds /24 MiB**, binding/run **180 seconds /64 MiB**, and saved-evidence verification/publication **60 seconds /16 MiB**. All phases fit; no transfer or increase was applied. Overall limits remain **7,980 diagnostic seconds /5,804,916,736 bytes**, with prior consumption carried forward. The diagnostic is now **DescriptiveDiagnosticComplete** and closed; unused capacity does not authorize another fight or fresh value.

## Next useful step

Use the saved per-entity damage and targeting evidence plus current legal ability definitions to design one complete-party hypothesis for **earlier Seal removal and/or killing a pillar before its deadline**, while preserving the observed barrier-breaking capability and essential healing. Compare the damage/timing requirements before choosing which mechanism to target; no pillar kills alone does not establish that chasing them is more cost-effective than reducing Seal exposure. Coordinated changes across characters remain in scope for that design, with ability order fixed.

That next step should be a bounded source/evidence design review, not another automatic search or sample extension. This diagnostic has not identified a validated replacement team, attributed effects to individual Essences, or demonstrated improved independent performance.
