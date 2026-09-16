# Saved group/count diagnosis stopped on a reader bug

15 September 2026. **StoppedReaderValidation.** Eight Python reader fixtures passed, but the one allowed primary analysis failed while reading an intentionally rejected proposal. **No complete trajectory analysis or independent recount was produced. Zero new fights, replays, preparations, generated candidates or seeds.** The previously verified 512-fight comparison and all **482,461 reservations** remain intact; adoption stays **Hold**.

## What failed

The saved baseline proposal `joined-mechanics-joint-1906069882-proposal-00025` uses `loadout-refine` and has result **`duplicate-family`**. Character slot 9 contains Rotroot Shambler twice. The harness correctly retained the rejected proposal as evidence and did not spend a fight evaluating it.

The new Python analysis tried to count groups on every proposed parent/child edge. It called the strict legal-recipe reader before checking whether the proposal was evaluated. That reader rejected the duplicated Essence and raised `ValueError: Noncanonical or duplicate Essences.` This is a defect in the new analysis reader, not a failed combat archive or evidence that the harness accepted an illegal team.

The eight fixtures cover canonical rejection, ranking, groups, ancestry, hashing and screen selection, but omit the combined path where **an invalid rejected recipe must remain readable as archived evidence**. The independent recount was not launched because the primary analysis produced no final outputs. No reader was patched and retried in this package.

Evidence: [native rejected proposal](../TestResults/balance/tower-group-count-trajectory-20260915/rejected-proposal.json), [failure context](../TestResults/balance/tower-group-count-trajectory-20260915/failure-context.json), [analysis log](../TestResults/balance/tower-group-count-trajectory-20260915/analyze.log), [durable failure receipt](../TestResults/balance/tower-group-count-trajectory-20260915/analyze-failure.json), [eight passing fixtures](../TestResults/balance/tower-group-count-trajectory-20260915/fixtures.log).

## Correction prepared, not executed

The [unapplied reader patch](../TestResults/balance/tower-group-count-trajectory-20260915/reader-fix.patch) separates reading recorded Essence lists from validating accepted legal recipes. The all-edge diagnostic can preserve raw rejected recipes and count group presence; accepted measured teams still pass the strict canonical/family checks. A new regression fixture preserves duplicate IDs in a rejected recipe while continuing to reject them when legal-team validation is requested. Rejected candidates without a saved measurement retain a null fitness difference.

The patch has **not been applied or tested**. A corrected diagnostic needs a separate explicit freeze, the amended fixture expectations and the raw rejected record as a fixture. Do not edit or rerun this failed package. The [frozen protocol](Tower-Group-Count-Trajectory-Protocol.md) specifies “zero retries” and “stop dependent analysis on failure or a limit”; preserving this failure follows that boundary.

## What remains unresolved

This attempt does not establish whether concentrated teams scored poorly, were unavailable as parents, lost groups through mutation, or missed the control-relevant groups. No complete ancestry, ranking, library-use or control-coverage result from this reader is available. The earlier comparison still establishes only the reported observations: finalists 0/32 and 0/32, mean boss health 87.86% and 81.86%, controls 28.51% and 31.84%; group repetition reached ten during discovery but at most two in the group/count finalist. Those facts do not identify the cause.

Next apply the prepared reader distinction in a separately frozen corrected diagnosis, include the rejected-proposal regression and rerun the saved-data checks once under its own bounded protocol. No combat or fresh values are needed for that work. Do not change ranking, retention, gameplay or budgets based on this incomplete diagnosis.

## Verification, resource accounting and changed files

Initial and final preservation checks verify **5838 indexed files across 13 sealed packages**. The failed source/scripts and the input hashes remain unchanged. The eight fixtures passed in 0.110 seconds; the failed analysis command charged 0.219 seconds and was not retried. Additional measured diagnostic time before publication is **2.580 / 120 seconds**, cumulative **250.159 / 1,800 seconds**. The failure's time is included, not discarded. Final output and timing totals are saved in [completion.json](../TestResults/balance/tower-group-count-trajectory-20260915/completion.json); the separate output cap is 32 MiB within the existing 4 GiB allowance.

Commands executed once:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-count-trajectory-20260915'
& $py -B "$w/workflow.py" freeze
& $py -B "$w/workflow.py" fixtures
& $py -B "$w/workflow.py" analyze  # exit 1; preserved, no retry
& $py -B "$w/publish-failure.py"
```

`workflow.py verify` was not run because the primary analysis failed. No tool approval blocked a command. The prior **56 backend tests through `build/run-tests.ps1`** and native archive reconstruction are retained by hash; neither was rerun for this Python/Markdown-only scope. No backend source was edited.

New files are the frozen protocol, readers/fixtures, unapplied correction, preserved failure/evidence package and this report. The three active search plans and harness README now point to this stopped diagnosis. Unrelated checkout work is preserved. No configuration changes, migrations, deployment, gameplay/content tuning, ability-order change, old-cap increase, v19 change or large confirmation. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
