# V5's local discovery gain did not persist on held-out seeds

16 September 2026. **VerifiedSavedTrajectory**. Read all **280 existing fight records**, with **zero new fights, seeds, preparations, replays or retries**. All 18 reader fixtures and the independent Decimal arithmetic audit passed. The sealed comparison and gameplay implementation remain unchanged.

V5 performed the intended twelve fresh proposals and four local edits. One edit improved the four-seed discovery average by **0.315 percentage points of boss health**, but that same edit left **0.660 points more health** on selection and **0.7240625 points more** on confirmation. Both finalists won **0/32** confirmation fights. This supports retaining the baseline as reference and V5 as exploratory; it establishes no search-strength improvement.

## What the four edits actually did

Proposal numbers in this review are **one-based**; JSON `number` and `parentNumber` fields are zero-based. Slot values below are the saved scenario's literal slot identifiers.

The first twelve V5 recipes and every discovery outcome exactly match the baseline's first twelve. Fresh proposal **8** was the best fresh team in both arms: **61.685%** mean boss health remaining. The baseline's four additional fresh teams did not beat it. This trajectory therefore did not lose the baseline winner by spending four positions on local edits.

Each edit changed exactly one Essence on one slot, kept ordinal ability order, used the best completed parent, and produced a novel legal evaluated candidate. Each succeeded on its first construction check: four checks total, no skipped options, duplicates, rejections or no-ops. The first three edits used proposal 8; after proposal 15 improved the discovery score, the final edit used proposal 15.

| Proposal | Parent | Saved slot | Removed -> added Essence | Parent -> child boss health | Health improvement, pp | Better / worse discovery seeds |
| ---: | ---: | ---: | --- | ---: | ---: | ---: |
| 13 | 8 | 7 | Blackjaw Spider -> Giant Spider | 61.685 -> 62.015% | -0.3300 | 0 / 4 |
| 14 | 8 | 7 | Enchanted Fairy -> Poisonous Rat | 61.685 -> 63.9025% | -2.2175 | 1 / 3 |
| **15** | **8** | **6** | **Venomous Spiderling -> Bog Mite** | **61.685 -> 61.370%** | **+0.3150** | **1 / 3** |
| 16 | 15 | 7 | Viper -> Plague Ghoul | 61.370 -> 61.8675% | -0.4975 | 1 / 3 |

Positive improvement means less boss health remaining. All parent/child discovery win differences were zero. Proposal 15 was the only local child nominated or selected. Its four per-seed health improvements were **-2.14, -0.78, +4.59 and -0.41 points**: one favorable seed outweighed three unfavorable seeds. This is an observation about this recipe and these fights, not a general judgment about either Essence.

## The selected edit across all three stages

The baseline finalist is the **exact parent** of V5's selected local child. Because both were confirmed, their saved held-out results can be compared directly without generating a new fight or substituting an unconfirmed runner-up.

| Stage | Fights per recipe | Parent mean boss health | Child mean boss health | Parent minus child, pp | Child better / worse seeds | Parent / child wins |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Discovery | 4 | 61.685000% | 61.370000% | +0.315000 | 1 / 3 | 0 / 0 |
| Selection | 8 | 63.711250% | 64.371250% | -0.660000 | 2 / 6 | 0 / 0 |
| Confirmation | 32 | 62.0765625% | 62.8006250% | -0.7240625 | 7 / 25 | 0 / 0 |

Parent: `team-3cc2e3787ba64dc988ae781127867159`; child: `team-ba8bf6e288412f474fecfbd98f45b991`. The other three edits have discovery observations only; no selection or confirmation outcomes are imputed for them.

The direction of the small discovery gain reversed on both held-out stages. That is consistent with selection noise from adapting to four reused discovery seeds; this single trajectory does not establish the general cause, an optimal search allocation or a reliable Essence effect. Health is descriptive. The frozen primary win-rate difference remains **0 points**, with adjusted interval **[-22.21794, +22.21794] points**, as reported in the [sealed comparison review](Tower-Fresh-First-Comparison-Execution-Review.md). This is neither evidence of equivalence nor an improvement claim. No new significance test or retrospective endpoint was introduced.

## Why the tool still selected the edited team

Discovery ranking and both top-two nominations reconstructed exactly. The baseline nominated fresh proposals 8 and 6; V5 nominated local proposal 15 and its parent, proposal 8. The shared parent retained both origins, `baseline-rank-1` and `discovery-refinement-rank-2`. Four nominations therefore became three selection recipes, saving eight fights under the frozen merge rule. All **280 completed fights have 280 durable charges**: 64 + 64 discovery, 24 selection and 128 confirmation.

Every selection nominee had zero wins in eight fights. The frozen selection rule then used **original discovery rank**, so V5 correctly selected its discovery rank-one child despite its worse descriptive selection health. The baseline's selected parent also had better selection health than its own runner-up (63.71125% versus 71.74625%). There was no dropped nominee, incorrect merge, ranking bug or lineage failure.

This identifies a specific design limitation: in an all-zero-win selection stage, the current tie-break carries forward the discovery ordering and does not use selection health. Changing that rule after seeing these results would be retrospective selection and would not validate a stronger policy.

## Decision and next boundary

Close this V5 diagnostic without promoting it or changing the fresh/edit ratio again on the strength of this trajectory. Preserve the baseline reference, fixed ability order and existing default behavior. The observed local improvement did not survive held-out evaluation; the implementation followed its contract.

The next proposed engineering scope is documented in the [zero-win selection plan](Tower-Zero-Win-Selection-Plan.md): keep wins first; only when both nominees have zero wins, prefer lower mean boss health on the selection seeds, then use discovery rank and ordinal ID for exact ties. Positive-win ties retain existing behavior. Implement this as an opt-in version applied equally to both arms, with zero-combat tests through `build/run-tests.ps1` after freezing the exact diagnostic scope. **This rule is planned, not implemented or tested.** Wins remain the primary confirmation endpoint; no completed finalist or result changes. The proposal adds no discovery-parent acceptance rule or fresh/edit allocation change. Any future combat comparison needs a separate frozen request and explicit unused seed/resource authorization.

This review does not revisit the completed filesystem performance work: its measured accounting improvement and parity closure remain as documented, with no whole-run speedup extrapolation. V19 retains **253 recipes**, no confirmation and its **512 unused reserved values**. All **483,046 reservations** remain excluded. V19 reliability remains Unresolved; later reliability Fail 1/3 and deep recovery 0/3 remain unchanged. **Adoption Hold**.

## Verification, resources and reproducibility

- Exact membership and hashes passed for the sealed readiness, execution and **553-file study** inventories. Study hashes were checked again after reading. Nine relevant current sources match the producing V5 pins captured by readiness.
- Recomputed 32 discovery candidates, all 16 V5 proposals, both nomination/selection decisions and all available local parent/child pairs. The independent Decimal pass read all 280 records and agreed on the four discovery pairs plus the selected edge's selection and confirmation pairs.
- **18/18 pure-reader fixtures passed**, including merged-origin ambiguity rejection and paired-seed alignment/missing-seed rejection. The unchanged harness retains **85/85 passing backend tests** from its [implementation verification](Tower-Fresh-First-Refinement-Review.md), run through `build/run-tests.ps1`; the [exact backend command and environment](../TestResults/balance/tower-fresh-first-refinement-20260916/control/tests-command.json) remain sealed. No backend build or test rerun was required for this saved-data analysis.
- Setup measured **0.203 seconds** and analysis **0.672 seconds**. Ten inspection seconds are conservatively charged in full; publication and its sealing allowance are recorded separately. The owned process wrapper exited successfully with no active children. No required verification command was blocked or skipped.
- The checkout snapshot verified **344 pre-existing dirty paths outside the six allowed handoffs** unchanged. Final publication repeats preservation and scoped `git diff --check`; their receipts remain in the evidence package.

The [frozen protocol](Tower-Fresh-First-Trajectory-Protocol.md) permits at most **60 seconds / 1 MiB**, within unchanged cumulative caps of **4,380 seconds / 4,731,174,912 bytes**. Starting usage was 4,037.795272355642 seconds and 4,717,896,657 bytes; setup and analysis added 10.875 charged seconds including inspection. The [completion receipt](../TestResults/balance/tower-fresh-first-trajectory-20260916/completion.json) is authoritative for final publication-inclusive time, bytes and remaining allowance. No fresh values, fights, retries or resource extensions were consumed.

Final receipt figures, added to this live review after sealing: **11.985 charged seconds and 824,306 bytes** for the completed trajectory diagnosis. Cumulative usage is **4,049.7802723556424 seconds / 4,718,720,963 bytes**, leaving **330.2197276443576 seconds / 12,453,949 bytes** at that receipt. The captured review, frozen protocol and sealed evidence remain unchanged. The subsequent selection-plan update is documentation only and supplies no new combat evidence or authorization.

Evidence: [full trajectory](../TestResults/balance/tower-fresh-first-trajectory-20260916/diagnosis.json), [independent arithmetic](../TestResults/balance/tower-fresh-first-trajectory-20260916/independent-arithmetic.json), [verification](../TestResults/balance/tower-fresh-first-trajectory-20260916/verification.json), [reader checks](../TestResults/balance/tower-fresh-first-trajectory-20260916/reader-fixtures.json), [final inventory](../TestResults/balance/tower-fresh-first-trajectory-20260916/files.json).

Executed once from the repository root; completed phases reject retries. These are reproducibility records, not permission to rerun an old package:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-fresh-first-trajectory-20260916'
& $python -B "$work/workflow.py" setup
& $python -B "$work/workflow.py" analyze
& $python -B "$work/workflow.py" publish
```

Changed files: this protocol/review, the new saved-analysis evidence/scripts, and current handoffs in `Tower-Search-Strategy-Reset.md`, `Automatic-Tower-Team-Discovery-Plan.md`, `Automatic-Tower-Team-Discovery-Implementation.md`, `Tower-Coverage-Replication-Plan.md`, `Tower-Balance-Acceptance-Policy.md` and `LL/tools/BalanceHarness/README.md`. Historical notices and sealed packages are preserved. No gameplay, harness policy, configuration, migration or deployment changes.
