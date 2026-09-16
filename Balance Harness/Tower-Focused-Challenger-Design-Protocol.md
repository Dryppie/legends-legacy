# Focused challenger selection: saved-data protocol

Frozen **15 September 2026**, before selection execution. Target: offline BalanceHarness analysis. The user requested a smaller, useful challenge of the existing +10% guardian Health/Power candidate instead of immediately testing all 43,879 retained teams. This scope produces the exact proposed team list and bounds; **no combat, preparation, allocation, rebinding, retry or deployment is authorized by it**.

The executable selection specification and raw input hashes are in `TestResults/balance/tower-focused-challenger-design-20260915/protocol.json`, created by the `freeze` command below before `analyze`. The schema reconnaissance preceding the freeze did not select teams or execute diagnostics. Historical files and requests remain immutable.

## Fixed selection

1. Retain all 560 existing anchors, all 361 older full-family stage-two teams, and all primary/secondary nominees from the eight enumerated baseline searches. Retain every observed ceiling breach (>50% wins) in their saved discovery, rescreen and confirmation evidence, the older complete baseline assessment, the separate 253-team confirmation, the four-factor screen and midpoint study. The union is deduplicated by the previously verified recipe/context binding; no required cell is discarded to fit a cap.
2. Add the two highest-discovery-rate teams per source/method/root. Break ties by ascending bound cell hash. These provide lineage coverage even when an arm has only zero wins.
3. For each Essence occurring in the family, add its two strongest carriers. Rank by the best **single** baseline observation's ordinary 95% Wilson lower bound, then rate, sample count and cell hash. Essence presence is a composition proxy; this does not establish interaction coverage or causal mechanics.
4. From the remaining teams whose largest available baseline sample is at most eight fights, add the top 128 by upper Wilson bound, lower bound, rate, sample count and cell hash. Round interval endpoints to 12 decimal places for stable ranking. Never sum observations from overlapping schedules.
5. Add 128 disjoint remainder teams by ascending SHA256 of `tower-focused-challenger-remainder-v1`, newline, and bound cell hash. This fixed hash sample provides dispersion and an omission check; it is not a certified probability sample or a proof about untested teams.

All historical outcomes are selection evidence only. The older pre-baseline discovery counts are superseded for ranking by the complete 17,821-team baseline assessment. Later baseline discovery definitions must match that assessment's captured content. Stronger-factor outcomes are kept separate and never pooled with baseline observations.

## Limits and verification

Exactly one selection and one independent saved-data verification: **zero fights, zero preparations, zero fresh seeds, zero retries**, 600 seconds per phase, at most 30 minutes combined diagnostic work and **128 MiB** new retained analysis output (within the existing 4-GiB ceiling). Stop and preserve failures; do not modify the rules after viewing the selection to obtain a preferred count.

Verify input hashes and prior indexed hashes where available; independently reconstruct selections, count/provenance/coverage, fixed-look interval arithmetic, and all preserved reservation records. Compare the dirty checkout with a saved baseline and report concurrent work without reverting it. No backend code changes are planned, so no backend test invocation or combat integration cases belong in this data-only scope.

## Proposed later study, not executed here

The full selected union would receive **256 fights per team**, using the same fixed captured-v19 gameplay and isolated +10% content. Use conservative two-sided Bonferroni-Wilson intervals with alpha .025 over the complete selected family, consistent with existing stage-two arithmetic. All upper bounds must be at most 50%, with at least one lower bound reaching 10%. An observed rate above 50% rejects acceptance; all upper bounds below 10% reject viability; other outcomes remain inconclusive. Wilson coverage is approximate. Any favorable result applies **only to this focused set**, never the complete retained family or all legal teams.

The unchanged generic study capacity requires at most 1,953 teams at 256 fights each, below 500,000 fights. If selection exceeds capacity, report that fact without trimming mandatory cells. Proposed later ceilings: **three hours, 8 GiB of new output, zero retries**, including new setup, execution, verification and reporting. These are a new study proposal, not changes to an existing cap or this turn's diagnostic authorization.

A later separately authorized contract may explicitly bind the existing unused 256-value second schedule. This scope does not reassign it. Preserve all **481,891 reservations**, the 32 unused first-stage values, the original unused 512 and the original complete-family protocol/request. After any future consumption, neither request may treat those values as fresh. The exhaustive study remains unexecuted. Search reliability **Fail 1/3**, adoption **Hold**, and sealed v19 **Unresolved / no internal confirmation** remain unchanged.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-focused-challenger-design-20260915'
& $py -B "$work/analyze.py" freeze
& $py -B "$work/analyze.py" analyze
& $py -B "$work/verify.py"
```

Commands use create-only evidence files and cannot overwrite a completed execution. These reproduce the recorded workflow; they are not a request to retry it. Read existing outputs after completion.
