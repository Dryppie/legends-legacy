# Frozen Web Weaver party: paired diagnostic result

**16 September 2026 — MechanismNotDemonstrated.** The approved comparison completed **192/192 observed fights** on **64 shared fresh seed values**, after native admission and readiness passed. The frozen package did not pass every prespecified diagnostic gate. Close this hypothesis without adoption, further seeds, slot tuning, ability-order changes or an automatic follow-up variant.

The candidate is exactly the [frozen ten-character recipe](../TestResults/balance/tower-coordinated-party-design-20260916/party-design.json): `040e` with Web Weaver Spider replacing Poisonous Rat in slot 3 and Cinder Beetle in slot 10. Both anchors, all remaining builds and the outcome-independent Essence order stayed fixed. This evaluates the combined package, not the individual causal contribution of each replacement.

## Frozen decision

| Party | Victories | Mean loss-penalized late-wave score L | Actual early breaks in first-three opportunities | Fights with friendly death before 60 seconds |
|---|---:|---:|---:|---:|
| anchor-040e | 44/64 (68.75%) | 2.0469 | 89/192 | 18/64 |
| anchor-49f6 | 47/64 (73.44%) | 1.9219 | 93/192 | 16/64 |
| web-weaver-pair | 41/64 (64.06%) | 2.2969 | 82/192 | 20/64 |

Lower L is better. Its first-three-opportunity score penalizes unfinished or missing Seals in a non-victory by four late waves each; a shorter loss cannot receive a spurious advantage from missing exposure. Missing opportunities after victory score zero. An early break means an actual barrier break after at most one emitted wave; all 192 possible opportunities remain the denominator. This is a mechanic-and-outcome composite, not a pure Seal-duration effect.

The primary `040e minus candidate` mean improvement was **-0.250000 waves per fight**, against the frozen minimum **0.5**. Across the 64 pairs, **16 improved, 22 tied, 26 worsened**. The exact one-sided sign tail was **4203587880800/4398046511104 = 0.95578523**, against the frozen maximum **0.05**. This tests the frequency of improvement among non-tied pairs; it is not a confidence bound on the mean or a win-rate superiority test.

| Required gate | Result |
|---|---|
| meanImprovement | Fail |
| exactSign | Fail |
| moreEarlyBreaks | Fail |
| winsVs040e | Fail |
| winsVs49f6 | Fail |
| earlyDeathsVs040e | Fail |
| earlyDeathsVs49f6 | Fail |

Paired victory changes against `040e`: **11 gains /14 losses /39 ties**. Against `49f6`: **9 gains /15 losses /40 ties**. These counts are descriptive; no observed-regression gate proves non-inferiority. The 64-value sample was not a practical confirmation, and no older observations were pooled into it.

Full [decision](../TestResults/balance/tower-coordinated-party-comparison-20260916/decision.json), [per-pair metrics](../TestResults/balance/tower-coordinated-party-comparison-20260916/independent-metrics.json), [whole-fight mechanic evidence](../TestResults/balance/tower-coordinated-party-comparison-20260916/mechanic-fights.json) and [companion summaries](../TestResults/balance/tower-coordinated-party-comparison-20260916/summary.json) are retained. Pulse waves are deduplicated across target applications and evaluated in recorded event order. Whole-fight damage totals do not identify damage dealt within individual Seal windows. Break durations, censoring, pillar outcomes and Resonance exposure remain descriptive rather than causal explanations of wins or losses.

## Readiness and execution integrity

All three parties passed existing native input materialization/preparation, including the game's Essence-family validation, and their prepared participants were saved. The new launcher retained the captured production runtime/content/settings; production code was not rebuilt. The previously passing 16 observer tests and sampled parity evidence were reused only after pin checks.

The first focused backend invocation passed **14/15** tests. Its registry test failed because the adapted required-ledger dictionary key contained mixed Windows separators. The file existed and its bytes were unchanged; normalizing the absolute key fixed the matching error. The initial source/admission/fixture artifacts and failure output were preserved. After the isolated launcher correction and rebuild, the second invocation passed **15/15** tests through `build/run-tests.ps1`. The expected-count check was also corrected from 16 to the actual 15 declared tests. No endpoint, recipe, threshold or budget changed.

The independent Python implementation passed all **11 stored score fixtures and eight decision fixtures**, plus rejection of a pulse after a same-tick barrier end. Native tests also cover missing second/third opportunities, truncation, Pending interruption, literal-only test allocation, once-only launch, attempt counters and exact integer sign tails. Full recovery-aware registry reconciliation passed before allocation, including the original Pending recovery and all **483,656** prior exclusions.

One approved reservation and one combat launch produced 64 fresh accepted values and 192 completed observed fights. No combat retry, resume, duplicate parity fight, historical replay or optional extension occurred. Native reconstruction verified all inputs/reports/traces; the independent audit reproduced reservation derivation, job order, journals, scores, all gates and the final decision with zero verification combat. Process guards confirmed every owned process tree exited. See [native verification](../TestResults/balance/tower-coordinated-party-comparison-20260916/native-verification.json), [independent audit](../TestResults/balance/tower-coordinated-party-comparison-20260916/independent-audit.json) and [preservation checks](../TestResults/balance/tower-coordinated-party-comparison-20260916/preservation.json).

## Resource accounting and closure

The approved **100-second run-to-engineering transfer** was applied once, after reconciling carried run/audit consumption. Component time caps are engineering **1,325**, run **1,975**, audit **300** seconds; their byte caps and the overall **7,980 seconds /5,804,916,736 bytes** remain unchanged. Readiness, binding/run and saved verification/publication stayed within **90/300/90 seconds** and **16/96/16 MiB** respectively. Failed attempts, overwritten readiness output, transient output and publication reserve are charged in the [current completion receipt](../TestResults/balance/tower-coordinated-party-comparison-20260916/completion.json). Unused balances are not authorization for another experiment.

All **483,720** exclusions are now retained, including the prior 483,656 and all 64 new values. V19 retains 253 required recipes and its 512 unused confirmation values; its reliability remains **Unresolved**. This diagnostic and the earlier pilot are **Closed**; adoption stays **Hold**. The frozen package did not pass every prespecified diagnostic gate. Close this hypothesis without adoption, further seeds, slot tuning, ability-order changes or an automatic follow-up variant.

Changed files are this execution review, the new isolated launcher/test/evidence package and current-status notices in the BalanceHarness README and five handoff documents. Relevant commands were the isolated launcher restore/build, native admission, two backend invocations through `build/run-tests.ps1`, independent fixture verification, one reservation/run and the two saved-evidence audits. The initial failed readiness command was corrected and rerun within the same ceiling; no required verification remains unrun. Scoped Git whitespace, links, source/runtime pins, sealed studies and unrelated dirty files were checked. No production code, game content, migrations, persistent configuration or deployment changed.
