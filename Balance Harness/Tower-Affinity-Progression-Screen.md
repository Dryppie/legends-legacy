# Affinity search progression reference screen

Completed on 2026-09-25. Floor 3 is the next useful case for evaluating the unchanged supported search. Floor 15 is a useful secondary diagnostic, but its provisional late-game budget and weaker authored anchor need to be accounted for before making search-quality claims.

This phase ran **576 reference fights, not search runs**. It established where a subsequent evaluation can measure improvement; it did not improve or compare search algorithms. The supported profile remains `affinity-creation-with-benchmark-validation-v1` and the closed floor-5 tuning cycle remains closed.

## Fixed inputs and results

All six budgets, 18 ordered recipes, reference identities, settings and the 32-seed panel were frozen before combat. The first reference in each case was preselected. Its inclusive 4–28 wins screening rule identifies room to observe improvement; this is neither a balance target nor a statistical strength test. Every case is retained below.

All equipment is Uncommon with baseline rolls; Essences are level 1, unascended and unevolved. Ownership is hypothetical, with no combat styles or contributions. These are existing progression/catalog budgets, not approved player-population targets.

| Floor | Party size | Essences / character | Level | Gear tier / rank / quality | Reference 1 | Reference 2 | Reference 3 | Preselected-reference result |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 3 | 5 | 4 | 30 | 1 / 1 / Standard | 12/32 | 5/32 | 5/32 | Follow-up candidate |
| 7 | 5 | 5 | 40 | 1 / 2 / Standard | 32/32 | 0/32 | 0/32 | Ceiling |
| 8 | 10 | 4 | 30 | 1 / 1 / Standard | 32/32 | 0/32 | 0/32 | Ceiling |
| 10 | 15 | 6 | 50 | 2 / 2 / Standard | 32/32 | 32/32 | 32/32 | Ceiling |
| 13 | 10 | 7 | 60 | 2 / 3 / Fine | 3/32 | 0/32 | 0/32 | Low-win reference |
| 15 | 15 | 10 | 90 | 2 / 4 / Fine | 8/32 | 15/32 | 29/32 | Follow-up candidate |

The 18 owned child processes exited successfully and drained, with no invalid, cancelled, unrun, drawn or tick-limited fights. Execution and verification inside the runner took 77.84 seconds, excluding input preparation and copying the captured runtime. The run occupied approximately 576 MiB before the small follow-up export. There were no retries and no freshly allocated seeds.

## Reference provenance and limitations

- Floor 3: first three saved recipes from `progression-linked-f3-20260911` in the retained Tower build catalog. After execution, composition inspection identified references 2 and 3 as the same per-character Essence sets, differing only in slot 4's equipped order. They produced identical outcomes. The completed screen remains unchanged and contains **two distinct compositions**, not three independent composition controls.
- Floors 7, 8 and 13: the existing boss-reference catalog's anchor, pilot control and lowest distinct recipe ID with identical equipment, training and character identities. Historical confirmation labels and win rates were not transferred.
- Floors 10 and 15: the first three saved portable controls, applied through the existing progression convention. Only absolute party slots 1–4 change; the remaining authored allies and identity inputs are preserved. No winning first cell was replicated into later cells.

The floor-15 reference-3 control already won 29/32 compared with the authored anchor's 8/32. A search selecting that existing control would demonstrate reference selection, not better generated proposals. Its ten-Essence, level-90 cohort is an explicit diagnostic hypothesis, not a claim about intended floor-15 progression.

The entire screen used authenticated copies of the nomination study's producing assemblies and content. Concurrent armor edits in the working tree were not rebuilt or included. Results apply to that captured combat version; an evaluation of the changed game must bind and check its own complete runtime and content first.

## Implementation and verification

- `analysis/screen-affinity-progression.py` prepares the fixed recipes, authenticates consumed source files, captures runtime/content, freezes inputs and runs the existing `tower` CLI through `build/bounded_windows_process.py`. Each cell has a 90-second process limit; the batch has a ten-minute deadline and 768 MiB output checks. Failure evidence is retained and execution is not resumed or retried.
- `../LL/tools/BalanceHarness/Fixtures/tower-affinity-progression-screen.json` pins the sources, budgets, IDs and selection rules. Local retained evidence under `TestResults` is required; this is an opt-in offline diagnostic, not an automatic CI workload.
- `analysis/test-screen-affinity-progression.py` has 11 passing tests covering authored allies, late progression, duplicate controls, classification boundaries, report tampering, changed runtime/settings/seeds, forged counts, and detecting order-only duplicates without changing equipped order.
- `../LL/tools/BalanceHarness/AFFINITY-SEARCH.md` documents the command and links this result.

Verification checks the producing runtime identity, content hashes, supplied scenarios and effective settings; authenticates all 576 individual battle files; and recounts outcomes against saved scorecards and seed schedules. A second read of every completed cell matched the published rows with zero replay fights. This is a descriptive reader check, not full native search-archive reconstruction or independent statistical confirmation.

Commands run from the repository root:

```text
python -B -X utf8 "Balance Harness/analysis/test-screen-affinity-progression.py"
python -B -X utf8 "Balance Harness/analysis/screen-affinity-progression.py" --output TestResults/affinity-progression-screen-20260925 --execute
```

The available bundled Python executable was used. No backend source was changed and no backend test/build was needed for this phase. No requested verification was blocked. There are no migrations, service configuration changes or deployment implications. The nonsensitive `appsettings.json` written inside the diagnostic artifact contains captured effective Tower settings; its parser-required idle cadence is unused by Tower and was not copied from application secrets.

## Follow-up inputs

`TestResults/affinity-progression-screen-20260925/follow-up-inputs.json` contains seed-free candidate scenarios for floors 3 and 15. These are reviewable inputs, not an admitted search or confirmed teams.

For floor 3, the export keeps the original first reference and chooses the first three **distinct per-character Essence sets** in saved catalog order. This replaces the redundant third input with `f3-a835efa1ee203add93c9`, without inspecting new outcomes or reordering any recipe. That replacement was **not evaluated** in the completed 576-fight screen; its future evaluation must be recorded separately.

Proceed with floor 3 first: check the three distinct references against the eventual combat version, then run the unchanged supported search with fresh evaluation seeds and the existing benchmark fallback. Report generated-candidate performance separately from selection of existing references. Retain floor 15 as a secondary stress case; qualify its reference and progression assumptions before using it as evidence of better search. Do not tune the algorithm or move a failed case's budget based on these screening outcomes.

Retained evidence: `TestResults/affinity-progression-screen-20260925/`.

| File | SHA-256 |
| --- | --- |
| `freeze.json` | `71ea7fadf448b6eee3bddbc663b8cde9c502ba1a058a6c63a66c5c97dffe49d9` |
| `result.json` | `4f2ffaf6ce4347a6fa248e96bf0424aca6685d94d5e846a89da9d11094800b7e` |
| `follow-up-inputs.json` | `a0481a2a3ad789339f3d8f2ea27cf121cbdad31b42cd61857b2f9dc0b1ee7460` |

The source archive manifest is pinned by the case definition at `a085d004823246466ec860876fcfdb370769cc76a71527a09c2b4a32dec79ee1`. The seed panel is the first 32 historical seeds of root 1 in that archive's `study/freeze.json`. Reuse makes this screen unsuitable as fresh confirmation evidence.
