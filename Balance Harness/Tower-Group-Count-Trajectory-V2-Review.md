# Corrected group/count trajectory diagnosis: verified findings

15 September 2026. **IndependentlyVerified.** The corrected saved-data reader passed nine fixtures, completed both arms, and passed an independent recount of **90 proposals, 88 evaluated teams, 83 recorded parent edges, 28 library hashes and 61 module uses**. **Zero new fights, replays, preparations, generated candidates or seeds.** All **482,461 reservations** remain preserved. Adoption remains **Hold**.

## What the search actually did

The group/count finalist **did not descend from a ten-owner build**. Its only fresh root, proposal `00018`, put Pack Howler, Spider Queen Webbed Domain, Venomous Spiderling and Web Weaver Spider together on two characters. The next `loadout-placement` proposal, `00019`, kept the maximum complete-group count at two and ranked first. The baseline finalist was itself a fresh recipe, proposal `00042`.

The ten-owner example was a different fresh build: Cave Bat, Spider Queen Webbed Domain and Venomous Spiderling. It left **96.29% boss health**, ranked **44th of 44**, had no whole-team parent eligibility, no recorded parent/module-contributor uses, and no descendants. Comparing its ten owners with the finalist's two does not describe a mutation path.

Here an **owner** is one character whose five-Essence loadout contains the complete group. These archived Tower recipes contain ten characters; an owner count does not mean equipping the same Essence twice on one character. Proposal suffixes below refer to `group-count-joint-1906069882-proposal-...`; evaluated position is chronological and ranks are final discovery ranks.

| Proposal | Origin or immediate parent | Evaluated position | Maximum complete-group owners | Discovery boss health remaining | Rank |
|---|---|---:|---:|---:|---:|
| `00010` | Fresh ten-owner recipe | 11 | 10 | 96.2900% | 44 |
| `00009` | Fresh Bog Mite / Venomous Snake / Venomous Spiderling group | 10 | 9 | 83.9075% | 12 |
| `00016` | `loadout-refine` from `00009` | 16 | 8 | 81.5275% | 4 |
| `00018` | Fresh four-Essence group described above | 18 | 2 | 83.3550% | 9 |
| `00019` | `loadout-placement` from `00018`; selected finalist | 19 | 2 | 80.7150% | 1 |
| `00040` | `placement` from `00019`; second screen nominee | 40 | 2 | 80.9625% | 2 |
| `00041` | `loadout-refine` from `00040` | 41 | 2 | 81.3625% | 3 |

The eight-owner build was competitive in discovery, but **rank four did not enter the frozen top-two screen**. Both group/count screen nominees won 0/8; the predefined tie rule kept rank one. Reconstruction agreed with the saved nominations, screen and finalist. This diagnosis found no ranking or selection mismatch. The archived `placement` operators move loadouts among characters; ability order stayed fixed.

All discovery trials lost. Each evaluated team had four discovery seeds, so health remaining is a descriptive ranking measurement, not evidence of reliable win-rate superiority. The two leading nominees also share ancestry; this is an observation, not an alternative nomination-policy test.

## Concentration, parent opportunities and mutations

The frozen definition of concentrated is at least five characters carrying the same complete catalogue group. Counts include all groups in the frozen 214-group catalogue, not only a proposal's requested group.

| Saved population | Teams | Best discovery health remaining | Median discovery health remaining | Recorded parent references |
|---|---:|---:|---:|---:|
| Baseline fresh | 19 | 87.0875% | 93.7825% | 23 |
| Group/count fresh, fewer than five owners | 10 | 83.3550% | 93.3800% | 17 |
| Group/count fresh, at least five owners | 9 | 83.9075% | 94.0650% | 6 |
| Group/count all evaluated, at least five owners | 12 | 81.5275% | 94.03125% | 10 |

Five of the nine concentrated fresh teams never entered a reconstructed beam/exploration parent pool; six had no recorded parent use. These two measures differ: a loadout library can donate from a measured team outside the whole-team parent pool. Parent references include module contributors and rejected/duplicate proposals; they are not extra fights or independent trials. The fresh and all-evaluated rows overlap.

Mutation sometimes reduced group coverage. For example, refining `00009` reduced nine owners to eight and lowered saved boss health by 2.38 percentage points. A later distribution from `00016` reduced the maximum eight to zero and raised health by 12.98 points; that child had two parents. Other children also improved or worsened. The complete [edge table](../TestResults/balance/tower-group-count-trajectory-v2-20260915/edges.json) preserves every changed group and available fitness difference. Multi-parent differences cannot isolate a group's causal effect, and making a child does not delete its archived parent. There is no basis here to force all mutations to retain high counts.

## What the short search did not cover

There were **19 fresh recipes per arm**. The group/count arm scheduled **17 distinct guided groups at one requested count each**, plus two uniform fresh recipes. It did not systematically vary count and filler Essences for the same chosen group. Group identity, repetition count, remaining Essences and placement therefore vary together in these observations. The complete fresh schedule and ranks are retained in the [readable tables](../TestResults/balance/tower-group-count-trajectory-v2-20260915/trajectory-tables.md) and [analysis](../TestResults/balance/tower-group-count-trajectory-v2-20260915/analysis.json).

The protocol derived control comparisons from the saved recipes without feeding controls to generation. The most widely repeated control catalogue group was **Pack Howler + Spider Queen Royal Venom + Venomous Spiderling**, present together on seven characters in each control.

| Catalogue group | Control owners, 040e / 49f | Baseline maximum owners | Group/count maximum owners | Group/count teams containing it | Library snapshots containing a carrier | Recorded carrier uses | Group/count finalist owners |
|---|---:|---:|---:|---:|---:|---:|---:|
| Howler / Royal Venom / Spiderling | 7 / 7 | 0 | 1 | 3 | 14 / 14 | 1 | 0 |
| Howler / Spiderling / Web Weaver | 1 / 0 | 0 | 2 | 12 | 11 / 14 | 1 | 2 |

Neither exact triple was selected directly as the guided group. **A superset of the first triple was scheduled:** the very first group/count fresh recipe chose Grave Hound plus Howler, Royal Venom and Spiderling, on one character. Thus the triple was constructed, but never replicated above one owner in evaluated teams. Its carrier remained in all fourteen recorded candidate library snapshots, and one saved `loadout-compose` used a carrier module on one target character. Wholesale library eviction does not explain its missing replication.

The outcome-independent shared-set rule selected ingredients appearing on at least eight characters in both controls: **Enchanted Fairy, Pack Howler, Royal Venom and Venomous Spiderling**. All four occurred together on seven characters in control 040e and six in control 49f, but **on zero characters in every evaluated team in either generated arm**. This is a coverage gap, not proof that the four ingredients cause the controls' performance or permission to hardcode their builds.

## Engineering implication and remaining limits

The concrete next engineering step is to give selected catalogue groups a few **count and filler variations within the existing total candidate budget**. Start with zero-combat fixtures proving that the same group receives distinct legal variations, counts are recorded, generation is deterministic, and controls and ability ordering do not enter selection. This would address the observed sampling limitation without assuming more copies are always stronger. No search-policy change or new experiment is implemented or authorized by this report.

This diagnosis only explains the existing short search. It cannot establish which unseen group, count, filler or nomination policy would win, or whether the eight-owner rank-four build would outperform the finalist on confirmation. It did not simulate a counterfactual search or add screening fights. The previously completed comparison remains **0/32 baseline versus 0/32 group/count**, with descriptive confirmation health **87.86% versus 81.86%**; control health was **28.51% and 31.84%**. No established win-rate improvement follows from this reader correction. See the [comparison execution review](Tower-Group-Count-Comparison-Execution-Review.md) for its paired uncertainty interval.

## Correction, verification and preservation

The earlier [stopped diagnosis](Tower-Group-Count-Trajectory-Review.md) remains sealed, including its failure receipt and unapplied-in-that-package patch. This separate package applies that prepared patch: `recorded_recipe()` preserves raw archived rejected lists, while `recipe()` still enforces canonical unique IDs on accepted recipes. The analysis continues to enforce accepted-team size and family legality. A ninth fixture reads the actual rejected duplicate-family proposal, preserves its duplicated Rotroot Shambler as evidence, and verifies that strict legal validation still rejects it. Rejected proposals get no new fitness measurement; a duplicate may refer to a saved score, explicitly without another trial.

All nine fixtures passed before one primary analysis and one independent recount. The recount reread the native saved records and agreed on ranks, group counts, ancestry, parent edges, module sources and hashes, control coverage, nominations and screen selection. See [verification.json](../TestResults/balance/tower-group-count-trajectory-v2-20260915/verification.json), [fixtures.json](../TestResults/balance/tower-group-count-trajectory-v2-20260915/fixtures.json), and the [frozen protocol](Tower-Group-Count-Trajectory-V2-Protocol.md).

| Measured diagnostic phase | Seconds | Result |
|---|---:|---|
| Freeze and initial preservation | 1.187 | Passed |
| Nine fixtures | 0.110 | Passed |
| Primary analysis | 0.578 | Passed |
| Independent recount | 1.016 | Passed |

These phases total **2.891 seconds**. Final publication/preservation time is additionally charged in [completion.json](../TestResults/balance/tower-group-count-trajectory-v2-20260915/completion.json), against **120 additional seconds / 32 MiB** and cumulative **1,800 seconds / 4 GiB**. The previous cumulative **250.331 seconds**, including the failed diagnosis, is retained. This is saved-data analysis time, not a new combat-performance speedup or a rerun of v19.

Initial and final checks preserve **5879 indexed files across 14 sealed packages**, including the failed diagnosis and the three completed comparison packages. Frozen inputs and the captured backend sources are checked by hash. The prior 56 backend tests through `build/run-tests.ps1` and native archive reconstruction remain preserved; neither was rerun for this Python/Markdown-only correction. Final checkout drift, scoped Markdown whitespace checks, output bytes and seal inventory are saved alongside the receipts.

The commands below were executed once, in order, from the repository root. `freeze.json` records the exact inputs/scripts; `publication-freeze.json` records the publisher/template, verified outputs and pre-publication documents. These are a command record, **not instructions to rerun a sealed package**. Reproduction requires a separately authorized output directory and freeze, preserving the archived copies.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-count-trajectory-v2-20260915'
& $py -B "$w/workflow.py" freeze
& $py -B "$w/workflow.py" fixtures
& $py -B "$w/workflow.py" analyze
& $py -B "$w/workflow.py" verify
& $py -B "$w/freeze-publication.py"
& $py -B "$w/publish.py"
```

No required command was blocked or omitted. Zero retries. Full machine-readable evidence: [all candidates and all 214 group counts](../TestResults/balance/tower-group-count-trajectory-v2-20260915/nodes.json), [all parent edges](../TestResults/balance/tower-group-count-trajectory-v2-20260915/edges.json), [chronological pools/library uses and roots](../TestResults/balance/tower-group-count-trajectory-v2-20260915/traces.json), [fresh/control/count summaries](../TestResults/balance/tower-group-count-trajectory-v2-20260915/analysis.json), and [file seal](../TestResults/balance/tower-group-count-trajectory-v2-20260915/files.json).

Changed files are this review and the corrected protocol/evidence package, plus current notices/handoffs in the search strategy reset, automatic discovery plan, coverage replication plan and harness README. Unrelated checkout changes remain intact. No backend/gameplay source, configuration, migrations or deployment changes. No Kharad tuning, ability-order change, old-cap increase, sealed-v19 modification or 129,536-fight confirmation. All reservations, including v19's 512 unused confirmation values, remain preserved. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, and adoption **Hold** are unchanged.
