# V3 trajectory diagnosis: refinement spent budget without improving its parents

16 September 2026. **VerifiedSavedTrajectory**. All three questions are answered from the sealed comparison: generation made progress through fresh construction; selection retained the strongest measured nominees; none of the nine refinement edits improved its parent. No new seeds, generated teams, preparations or fights ran.

## 1. Generation: the improvement came from fresh teams

V3 evaluated seven fresh teams and nine refinement children. Its best initial team, proposal **4**, left **78.40%** mean boss health on the four discovery seeds. Its eventual best, proposal **12**, left **73.60%**. That 4.80-percentage-point improvement came entirely from a new fresh team. Refinement added no improvement over the best fresh team.

The seven V3 fresh recipes are exactly the baseline's first seven, in the same order, on the same discovery seeds. Their saved recipe hashes match. V3 evaluated them at proposals 1, 2, 3, 4, 8, 12 and 16. The baseline's **eighth** fresh recipe, which V3 did not reach within its 16-evaluation budget, became the baseline winner at **66.74%** discovery boss health. V3's winner was the baseline's sixth fresh recipe and ranked third in the baseline's complete discovery set.

| Discovery observation | Baseline | V3 |
| --- | ---: | ---: |
| Evaluated teams | 16 fresh | 7 fresh + 9 edits |
| Wins across 64 discovery fights | 0 | 0 |
| Best mean boss health remaining | 66.74% | 73.60% |
| Mean boss health across candidates | 85.33% | 86.68% |
| Distinct Essences encountered | 35 | 43 |
| Distinct slot loadouts encountered | 96 | 52 |

The immediate explanation in this run is an allocation tradeoff: unsuccessful edits displaced fresh exploration that found the better baseline candidate. Encountering more distinct Essences did not translate into a stronger measured team. This is one deterministic search pair; it does not establish that fresh exploration always outperforms refinement.

## 2. Selection: the saved decisions were correct

The audit recomputed all 32 discovery scores from their raw records and reconstructed the exact ranking and nominations. Discovery already breaks win-rate ties using lower boss health, then survival, victory duration and ID. All discovery wins and survival rates were zero here, so boss health distinguished these candidates.

Both arms nominated their top two discovery recipes. All four nominees then won 0/8 selection fights, so the frozen rule chose each arm's original discovery leader. The separate selection boss-health measurements agreed with those choices:

| Arm | Selected nominee: boss health | Runner-up: boss health | Selected correctly? |
| --- | ---: | ---: | --- |
| Baseline | 62.81% | 72.80% | Yes |
| V3 | 72.74% | 75.03% | Yes |

No better-ranked discovery candidate was discarded, and no better selection-health nominee was passed over. A change to the selection tie break would not have changed either finalist in this run. Only finalists and controls have confirmation results; the audit cannot establish the true confirmation strength of every unselected candidate.

The sealed confirmation result remains baseline **0/32**, V3 **0/32**, with boss health **62.66% / 72.62%** respectively. The primary win-rate difference is 0 percentage points with adjusted interval **[-22.22, 22.22]**. This diagnosis does not reselect a winner or change that result.

## 3. Refinement: all nine edits weakened the measured parent

The base parent for each edit was the best completed same-arm discovery candidate; all additional donor references were valid. Six edits used proposal 4; after fresh construction improved the incumbent, three used proposal 12. Recorded loadout-library contents, hashes, donor provenance, changed slots and novelty bounds all checked out. There were **zero rejected, duplicate or no-op proposals**, and all 16 candidate evaluations completed. The novelty repair worked; the issue here was the usefulness of the edits it produced.

Positive numbers below mean **more boss health left than the parent**, hence a worse result. Proposal numbers are one-based; the machine-readable evidence uses zero-based indices.

| Child proposal | Operator | Parent | Slots changed | Increase in mean boss health |
| --- | --- | ---: | ---: | ---: |
| 5 | Distribute a loadout | 4 | 6 | +14.20 pp |
| 6 | Refine a repeated loadout | 4 | 9 | +5.31 pp |
| 7 | Replace one character's loadout | 4 | 1 | +0.95 pp |
| 9 | Distribute a loadout | 4 | 6 | +10.81 pp |
| 10 | Refine a repeated loadout | 4 | 9 | +3.69 pp |
| 11 | Replace one character's loadout | 4 | 1 | +3.51 pp |
| 13 | Distribute a loadout | 12 | 6 | +16.75 pp |
| 14 | Refine a repeated loadout | 12 | 9 | +22.63 pp |
| 15 | Replace one character's loadout | 12 | 1 | +2.47 pp |

Every edit worsened average discovery boss health. Across their 36 paired seed comparisons, 34 were worse and two better; these are correlated, reused discovery observations, not 36 independent tests. Mean worsening was **13.92 pp** for distribution, **10.55 pp** for repeated-loadout refinement and **2.31 pp** for whole-character replacement.

The recorded changes explain why the word "refinement" can be misleading here: each distribution copied a full loadout into six slots; each repeated-loadout refinement changed two Essences in all nine matching slots. Even the single-slot replacements changed four or five Essences at once.

For example, proposal 14 replaced **Enchanted Fairy and Viper** with **Hobgoblin Brutal Charge and Nightshade Blossom** in nine slots, worsening mean boss health by **22.63 pp**, and worsening it on all four discovery seeds. Proposal 6 replaced **Alpha Wolf and Blood Zombie** with **Blue Slime and Hobgoblin Brutal Charge** in nine slots and worsened it by **5.31 pp**. These are whole-edit observations; they do not isolate the causal value of any individual Essence.

## Evidence-supported follow-up

Keep the current ranking and selection rules. The smallest useful follow-up is an **opt-in local refinement policy that changes one Essence in one slot per proposal**, preserves team-role and legality constraints, and records the exact change. Start with zero-combat construction checks and preserve the existing fresh sequence, candidate/proposal caps and fixed ability order so mutation scope is the intended difference. Retain the sealed V3 implementation for comparison.

This is a hypothesis supported by the observed size and cost of failed edits, not proof that smaller edits improve combat strength. The baseline's successful fresh exploration also makes evaluation allocation a relevant later question; avoid changing mutation scope and allocation simultaneously in the first diagnostic comparison. No new policy or combat run is authorized by this diagnosis.

## Verification, resources and reproduction

The audit verified **556 sealed study files**, read all **288 existing combat records**, recomputed **32 discovery measurements**, checked all nominations/finalists, and reconstructed all **16 V3 proposals** and their recorded loadout libraries. **Twelve reader fixtures passed**. It compared arithmetic means with independent sum/count calculations. **729 unrelated dirty files** passed preservation checks. The final publication preservation receipt checks them again.

No backend code changed. The existing 45 passing backend tests from `build/run-tests.ps1` remain reused evidence; no backend build or test run was repeated. No required diagnostic command was blocked. Earlier schema-only inspection encountered a wrong JSON nesting assumption; the frozen reader uses the verified `generation.arms` schema. It did not change or replay any study.

The [frozen protocol](Tower-Refinement-V3-Trajectory-Protocol.md) caps this scope at **60 seconds / 4 MiB**, within the unchanged cumulative **4,080 seconds / 4 GiB + 192 MiB** ceilings. Exact time/output charges, including the conservative five-second inspection allowance, are in the [completion receipt](../TestResults/balance/tower-refinement-v3-trajectory-20260916/completion.json). The [machine-readable diagnosis](../TestResults/balance/tower-refinement-v3-trajectory-20260916/diagnosis.json) contains every candidate, per-seed parent/child difference and Essence change. The evidence package also retains scripts, fixture results, source pins, process-exit evidence and preservation checks.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-v3-trajectory-20260916'
# Executed once; completed phases reject retries.
& $python -B "$work/workflow.py" setup
& $python -B "$work/workflow.py" analyze
& $python -B "$work/workflow.py" publish
```

Changed files: new diagnostic scripts/evidence, this protocol/review and six active Markdown handoffs. No harness/search/gameplay source, configuration, migration or deployment changes. All **482,956 reservations** remain reserved, including the old failed V3 allocation's 45 values, the earlier failure's 40, and V19's separate 512 unused values and 253 recipes. V19 reliability Unresolved; later Fail 1/3, deep recovery 0/3; adoption Hold.
