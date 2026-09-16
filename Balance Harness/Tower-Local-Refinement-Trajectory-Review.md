# V4 trajectory: two useful local steps, but fresh exploration still found the best teams

16 September 2026. **VerifiedSavedTrajectory**. This diagnosis read the completed comparison's saved records only. **Two of nine V4 edits improved the current best discovery score; neither improvement survived the later fresh-team result.** The baseline's eighth fresh team was stronger than every V4 discovery candidate. V4 evaluated only seven fresh teams within its sixteen-proposal budget. No new seeds, generated teams, preparations, fights or combat replays ran.

## What the local edits accomplished

Every edit changed exactly one Essence in one archived character slot, used the best completed V4 parent, preserved fixed ordinal order and matched its recorded trace. All sixteen proposals were evaluated. There were no duplicate, rejected, no-op or exhausted proposals.

The first useful edit, proposal **5**, replaced Blood Harpy with Spider in slot 7. It reduced mean boss health from **81.165% to 78.1875%**, improving all four discovery seeds. Proposal **11** changed that parent's Spider to Plague Ghoul, reducing mean boss health to **77.740%**; it improved three of four seeds. Those were measured local improvements, with no additional wins.

Positive change below means **more boss health remaining**, hence a worse result. Proposal numbers are one-based; machine-readable indices are zero-based. Slot numbers are the saved simulator's slot identifiers.

| Child proposal | Parent proposal | Slot | Essence removed → added | Change in mean boss health | Better / worse seeds |
| --- | --- | --- | --- | ---: | ---: |
| 5 | 4 | 7 | Blood Harpy → Spider | −2.9775 pp | 4 / 0 |
| 6 | 5 | 7 | Bloodfang Wolf → Blue Slime | +0.3025 pp | 1 / 3 |
| 7 | 5 | 7 | Blood Zombie → Glade Panther | +0.2875 pp | 2 / 2 |
| 9 | 5 | 4 | Alpha Wolf → Transparent Slime | +1.0625 pp | 0 / 4 |
| 10 | 5 | 1 | Blue Slime → Spider Queen | +1.4650 pp | 1 / 3 |
| 11 | 5 | 7 | Spider → Plague Ghoul | −0.4475 pp | 3 / 1 |
| 13 | 12 | 3 | Blackjaw Spider → Undead | +0.8825 pp | 1 / 3 |
| 14 | 12 | 10 | Venomous Spiderling → Poisonous Rat | +0.4975 pp | 1 / 3 |
| 15 | 12 | 1 | Pack Howler → Web Weaver Spider | +0.5725 pp | 1 / 3 |

Across the nine edits, mean worsening was **0.1828 percentage points**. Fourteen paired seed observations improved and twenty-two worsened. These are adaptive, correlated comparisons on four reused discovery seeds; they are not thirty-six independent tests and do not establish the general value of any Essence.

Construction examined **23 options** to produce nine legal novel edits. Fourteen internal skips were recorded: eleven missing-team-role options, two duplicate-family options and one unchanged option. These skips were not extra proposals or fights. Recorded per-edit checks ranged from one to nine against a 4,000-option bound; no construction bound was exhausted. Parent usage was proposal 4 once, proposal 5 five times and proposal 12 three times.

## Why the local gains did not improve the finalist

V4's best score progressed from **81.165%** at fresh proposal 4, to **78.1875%** at local proposal 5, to **77.740%** at local proposal 11, then **72.4275%** at fresh proposal 12. That fresh team overtook the refined lineage immediately. The final three edits did not improve it. Proposal 14, a slightly weaker child in discovery, became the second nominee.

The seven V4 fresh recipes exactly match the baseline's first seven, including their saved recipes and all four discovery outcomes. V4 evaluated them at proposals 1, 2, 3, 4, 8, 12 and 16. V4's best team is the baseline's sixth fresh team, ranked third in the baseline's full discovery set.

The baseline's **eighth** fresh team was its discovery winner at **64.495%** boss health. Its twelfth fresh team ranked second at **71.960%**. Neither was reached by V4's seven-fresh schedule. This directly identifies the observed allocation cost: nine edit evaluations displaced fresh exploration that found stronger candidates in the same saved comparison. It does not prove that fresh exploration always wins or that a particular revised split is optimal.

| Discovery observation | Baseline | V4 |
| --- | ---: | ---: |
| Evaluations | 16 fresh | 7 fresh + 9 local edits |
| Wins / fights | 0 / 64 | 0 / 64 |
| Best mean boss health remaining | 64.495% | 72.4275% |
| Mean boss health across candidates | 85.5095% | 81.7525% |
| Distinct Essences encountered | 35 | 39 |
| Distinct slot loadouts encountered | 96 | 55 |

V4's average candidate was stronger on this descriptive health measure, but the goal was to find the strongest finalist. The average did not compensate for missing the baseline's best fresh recipes. No local edit improved V4's final score over its best fresh candidate.

## Nominations were correct; the selection tie break had one descriptive disagreement

All 32 discovery measurements were reconstructed from raw records. Both arms nominated their exact top two under the frozen rank: win rate, lower boss health, survival, victory duration, then ID. No better-ranked discovery candidate was discarded.

All four nominees won **0/8** selection fights. The frozen selection rule therefore retained each arm's original discovery leader. It does not use selection boss health to break that tie.

| Nominee | Discovery rank | Selection boss health | Selected? |
| --- | ---: | ---: | --- |
| Baseline fresh proposal 8 | 1 | 62.57875% | Yes |
| Baseline fresh proposal 12 | 2 | 71.95125% | No |
| V4 fresh proposal 12 | 1 | 73.11250% | Yes |
| V4 local proposal 14 | 2 | 72.16500% | No |

For V4, the local runner-up left **0.9475 percentage points less boss health** during selection, reversing its discovery ordering. A hypothetical health-based tie break would have selected it. That is a descriptive observation, not an implementation error or evidence of higher confirmation win rate. The runner-up has no confirmation result; do not assign it the finalist's results or reselect after seeing confirmation.

The sealed confirmation result remains baseline **0/32** versus V4 **0/32**, with mean boss health **62.485% versus 72.2175%**. The primary paired win-rate difference remains **0 percentage points**, adjusted interval **[−22.22, +22.22]**. Neither this diagnosis nor the small selection-health reversal establishes a V4 strength improvement.

## Decision and smallest useful follow-up

Keep the baseline as the reference and keep V4 opt-in. The local implementation did produce useful steps, so the result does not justify declaring single-Essence refinement ineffective. It also does not justify spending nine of sixteen evaluations on it by default.

If search development continues, the next focused change should reserve more evaluations for fresh construction and place a smaller local-refinement tail after that exploration. Specify that allocation before testing and start with zero-combat schedule, cap and determinism fixtures. Keep the local operator, ability order, nomination and selection rules fixed to isolate allocation. Do not hard-code the observed winning eighth recipe or the two successful Essence substitutions. No particular fresh/refinement split is established by this one trajectory.

A selection-health tie-break change is a separate hypothesis; the saved 0.9475-point reversal alone does not justify combining it with the allocation change. No changed policy, additional combat or fresh seed allocation is implemented or authorized by this diagnosis. Further combat would require its own explicit approval.

## Verification, resources and reproduction

The frozen reader verified the readiness and execution seals, all **556 study files**, **288 existing combat records**, **32 discovery scores**, both nomination/selection decisions and all **nine local edit traces**. **Sixteen pure-reader fixtures passed.** A separate Decimal sum/count pass independently checked the 128 discovery records and all nine paired differences. Relevant source hashes matched the captured producing sources. **768 pre-existing dirty files** passed preservation checks; publication checks them again.

No backend source changed, so the existing 69 passing tests run through `build/run-tests.ps1` were reused without another build or test run. No required diagnostic command was blocked. The completed comparison and every older sealed study remain unchanged.

The [protocol](Tower-Local-Refinement-Trajectory-Protocol.md) freezes a 60-second / 1-MiB ceiling within the unchanged cumulative 4,260-second / 4,613,734,400-byte limits. The [completion receipt](../TestResults/balance/tower-local-refinement-trajectory-20260916/completion.json) records exact time and final output, including a ten-second inspection charge and one second reserved for publication sealing. The [diagnosis](../TestResults/balance/tower-local-refinement-trajectory-20260916/diagnosis.json) retains every candidate and edit; the [independent arithmetic check](../TestResults/balance/tower-local-refinement-trajectory-20260916/independent-arithmetic.json) retains exact decimal differences.

Executed once; completed phases reject retries:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-local-refinement-trajectory-20260916'
& $python -B "$work/workflow.py" setup
& $python -B "$work/workflow.py" analyze
& $python -B "$work/workflow.py" publish
```

Changed files: the new diagnostic scripts/evidence, this review, its protocol and six active Markdown handoffs. No search/gameplay source, configuration, migrations or deployment changes. All **483,001 seed reservations** remain excluded, including prior failed allocations and V19's 512 unused values. V19 retains its 253 recipes and no confirmation; reliability Unresolved, later Fail 1/3, deep recovery 0/3 and adoption **Hold**.
