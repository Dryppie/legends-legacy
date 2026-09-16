# World Tower anchor-loss diagnostic - 16 September 2026

**Decision: NoGoForNewCandidateFromSavedSummaries.** The retained AA/BB reports describe how losses end, but do not support a materially different complete-party intervention. Keep both anchors as practical references. This is a no-go for another candidate experiment from this diagnostic, not a claim that no stronger team exists.

Read-only scope: 512 previously authenticated compact reports (256 shared seeds per anchor), selected by fixed native case IDs and prepared-party hashes. No AB/BA optimization, new seeds, combat, replay, build or native-verification run. Ten level-40 characters with the fixed five-Essence/equipment context remain valid for this analysis; no actual account inventory is required.

## Outcomes on the same seeds

| Shared-seed outcome | Seeds |
| --- | ---: |
| Both won | 128 |
| Only AA won | 51 |
| Only BB won | 44 |
| Both lost | 33 |

AA won 179/256 (69.92%); BB won 172/256 (67.19%). These remain descriptive observations, not proof that AA is stronger. Every one of the 161 defeats ended with zero original characters alive. There were no draws or timeout termination reasons.

## What the saved measurements show

| Measurement | AA wins | AA losses | BB wins | BB losses |
| --- | ---: | ---: | ---: | ---: |
| Kharad health remaining, median % | 0.00 | 18.30 | 0.00 | 15.40 |
| Fight duration, mean seconds | 90.14 | 95.30 | 89.70 | 97.71 |
| Original survivors, mean | 8.25 | 0.00 | 8.20 | 0.00 |
| First death, median seconds among fights with a death | 83.40 | 54.70 | 73.30 | 54.60 |
| Recorded damage to Kharad, mean | 24,014.82 | 19,475.21 | 23,984.42 | 20,068.26 |
| Recorded damage to other targets, mean | 5,552.23 | 5,102.42 | 5,085.28 | 4,938.36 |
| Healing done, mean | 4,794.54 | 4,827.96 | 4,622.12 | 4,959.42 |
| Health regenerated, mean | 6,296.11 | 5,605.94 | 6,229.51 | 5,718.92 |
| Barrier generated, mean | 454.05 | 433.75 | 453.35 | 446.42 |
| Incoming raw damage, mean | 48,730.34 | 60,301.45 | 47,989.67 | 61,736.08 |
| Typed mitigation prevented, mean | 25,355.91 | 29,932.29 | 25,102.55 | 31,686.77 |
| Final health damage, mean | 24,315.35 | 31,659.38 | 23,843.42 | 31,459.79 |

First-death timing has 149/179 AA wins and 145/172 BB wins with an observed death, versus every loss. The remaining wins are censored at fight end and are not assigned a fictional death time. First-death slots vary, with ties retained; the data do not identify one consistently failing character.

On the 95 discordant seeds, the winning party had **4,291 more recorded damage to Kharad**, **11,939.52 less incoming raw damage**, **7,133.73 less final health damage**, **172.32 less healing done**, and **648.76 more regenerated health**, on average. It finished 6.06 seconds sooner. In the 81 pairs where both fights had a death, its first death occurred 19.02 seconds later on average; the other 14 winning fights had no death. These are paired descriptions selected on the outcome, not treatment effects.

On the 33 seeds both anchors lost, median remaining Kharad health was 18.42% for AA and 11.56% for BB. Thus even the common-loss subset does not point uniformly toward AA. The first additional-hostile window lasted 11.9 seconds in all 512 reports. That counter records disappearance of additional hostiles, not why they disappeared; it is not proof of equal add-killing effectiveness.

Damage and mitigation use the ten initial character IDs. Kharad-targeted damage is separated from other target interactions, and those sums reconcile with recorded total damage. Recorded boss damage is not a direct measure of net boss health removed. Healing, regeneration and barrier generation remain separate. Incoming-damage reconciliation flags pass for all original characters. No friendly summons or revivals complicate these aggregates.

## Interpretation and stopping decision

Earlier first deaths and poorer boss progress are associated with losses. Whole-fight totals also change with duration, incoming attacks, damage absorption and who remains alive. Higher prevented damage in losses therefore does not show that more mitigation is harmful, and lower healing in paired winners does not show that healing is unnecessary. The summaries cannot establish whether a particular protection, targeting or damage change would prevent the collapse. They do not isolate an individual Essence effect or recover an event-by-event counterfactual.

No new recipe, operator or candidate experiment is proposed. The failed subgroup candidates and earlier closed approaches remain closed; no retuning, seed reuse, extension or retry is queued. Adoption remains **Hold** and V19 **Unresolved** (253 required recipes). All **483,988 excluded seed values**, including the 512 unused confirmation values, remain excluded.

## Verification, files and resource receipt

The independent reader recomputed 675 metric distributions, all shared-seed groups, censoring counts, termination counts, slot summaries and paired differences directly from the 16 saved chunks. It rechecked their sealed hashes and the pinned native campaign manifest; both bounded process jobs completed with no live descendants. This arithmetic audit does not repeat native combat reconstruction, which was completed in the prior comparison.

Added this review and the evidence/scripts under `TestResults/balance/tower-anchor-loss-diagnostic-20260916`; prepended only a current-status notice to `Tower-Team-Search-Design-Review.md`, retaining its prior bytes in `before-doc.zip`. Game and harness implementation files were not edited. Backend tests, builds, native verification and combat were intentionally not run for this saved-data diagnostic. No migrations, configuration changes or deployment implications.

Applied the approved **30-second run-to-engineering transfer**, with all byte caps and the overall cap unchanged. Charged the full **30 seconds /256 KiB** conservatively, including setup, carried read-only follow-ups, analysis, independent audit, publication and checks. Engineering allowance is now exhausted in time, with **49,912 bytes** remaining; overall remaining allowance is **1,254.535727644363 seconds /23,491,893 bytes**. Prior run/audit consumption is preserved. These remaining balances are not authorization for another run.

Evidence: [analysis](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/analysis.json), [independent audit](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/independent-audit.json), [decision](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/decision.json), [resource receipt](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/completion.json).
