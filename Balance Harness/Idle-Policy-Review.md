# Idle harness policy review — 8 September 2026

## Decision

The current `idle-reference-v1` suite is suitable for repeatability checks and controlled combat comparisons. Its fixtures do **not yet justify enforced new-player balance targets**. Keep all six goals in `idle-goals-v1` draft, preserve the existing fixture contract as a control, and add CI verification for execution and evidence integrity.

This is a repository-based review of the cohort and observed results, not owner approval of the numerical targets. The main gaps are the starter Essence choice, the ownership budget and the number of enemies in normal area encounters.

Follow-up: the separate [First Hunt cohort](First-Hunt-Cohort.md) is now implemented with all three starter choices, quest-reward equipment counts and fixed two-enemy later encounters. Its 36 cells and 180 draft checks complement the original controls reviewed below. Fixed random reward outcomes, training and Forge assumptions still need gameplay review; all targets remain draft.

## Fixture review

| Assumption | Evidence | Review finding |
| --- | --- | --- |
| Levels 1, 5 and 10 are meaningful checkpoints | [Area requirements](../LL/src/API/API.LL/Data/world/regions.json), [Trial of Lumo](../LL/src/API/API.LL/Data/quests/region-01/trial-of-lumo.v4.json), [Blood in the Grove](../LL/src/API/API.LL/Data/quests/region-01/blood-in-the-grove.v4.json) and [slot progression](../LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs) | Supported as access/unlock boundaries. The harness assumes quest completion rather than proving the journey to each checkpoint. |
| Mace and shortsword are legal starter weapons | [First Weapon](../LL/src/API/API.LL/Data/quests/onboarding/first-weapon.v2.json), [starter selection service](../LL/src/Infrastructure/Service/Services.LL/Items/StarterEquipmentService.cs) and [equipment catalog](../LL/src/API/API.LL/Data/equipment/equipment-starters.v1.json) | Supported. Both are tier-1 one-handed choices. They cover two weapon profiles, not every starter option. |
| Goblin represents the first guaranteed Essence | [Your First Hunt](../LL/src/API/API.LL/Data/quests/onboarding/training-day.v4.json) offers Goblin Warrior, Hollow Stag and Skeleton | Unsupported. Goblin is an obtainable regional Essence, but it is not a current First Hunt choice. The existing level-1 Goblin loadout is a controlled ownership assumption, not an immediate post-tutorial character. |
| Two items at level 5 and four at level 10 represent ordinary ownership | [Regional equipment rules](../LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json) and [idle acquisition processor](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatAcquisitionRewardProcessor.cs) | Legal, but not demonstrated as typical. The configured regional chance is about 0.116% per victorious encounter: about 864 eligible victories per equipment drop in expectation, before selecting a specific slot/type/quality. This is a drop-process calculation, not a time-to-level estimate. The chapter quests do not guarantee these exact armor/weapon sets. |
| Level-1 Essences, rank-0 gear and no styles/jewelry are a conservative ordinary build | [Soul Archive rewards](../LL/src/API/API.LL/Data/quests/onboarding/soul-archive.v3.json) include a Fury blueprint; Blood in the Grove grants a jewelry chest and an area Essence Token | These omissions isolate variables, but their combined strength is not a validated player budget. The level-10 fixture omits an available jewelry reward and does not model Essence training or Forge investment. |
| One fixed enemy represents entry difficulty | [Area spawn distributions](../LL/src/API/API.LL/Data/world/regions.json) and [creature-count selection](../LL/src/Infrastructure/Service/Services.LL/Spawnings/WeightedSpawnSelector.cs) | Lumo has a 96.9% single-enemy count; Blood Grove and Crystal Creek have a 96.9% two-enemy count. The latter single-enemy cases condition on a 3% count outcome, then select one creature. They cannot estimate area-wide clear rate or typical encounter pressure. |
| Every selected challenge enemy should share a 60–90% clear band | The fixture assigns the labels; current region data does not declare that shared target | Unsupported as an enforcement rule. An ordinary regional enemy need not have an upper clear-rate limit. Author the intended challenge separately from its observed difficulty. |

The fixture's recipes and hash remain unchanged so earlier controlled comparisons remain meaningful. The tool guide now calls out these limitations explicitly. A revised player cohort should receive its own fixture/policy identity and a compatible reference; it should not silently replace this evidence.

## Reference evidence

Validation used source revision `7e798a878` with the new workflow/documentation changes, Release on Windows, .NET runtime and assembly identities recorded in each manifest, master seed 1337 and 100 samples per cell. Two fresh 1,200-battle runs completed with no invalid, cancelled or missing battles. All 12 cells matched with zero gameplay changes, and detailed replay matched.

The candidate also compared successfully with the previously accepted **workflow-validation-only** reference. Code and catalog hashes changed; all 12 cells still matched with zero gameplay changes. Neither reference approves gameplay targets.

| Current control | Observed clear rate | Interpretation |
| --- | --- | --- |
| All six ordinary cells | 100/100 each; 95% Wilson interval approximately 96.30–100% | Meets the draft 90% minimum for these specific inputs. This does not validate the ownership assumptions or omitted spawns. |
| Level-1 mace versus Goblin Warrior | 30/100; interval 21.89–39.58% | Fails the draft 60–90% challenge band. Investigate under the real starter choices before changing enemy coefficients. |
| Level-1 shortsword versus Goblin Warrior | 51/100; interval 41.35–60.58% | Inconclusive against the same band because the interval overlaps its lower boundary. |
| Four later challenge cells | 100/100 each; interval approximately 96.30–100% | Fails the proposed 90% ceiling. This exposes the need to review the challenge selection/target, rather than establishing that these enemies should be strengthened. |

The full evaluation reports **30 pass, 5 fail, 25 inconclusive, 0 invalid** across 60 draft checks. Enforcement remains `Advisory` and the command returns 0. Of the 25 inconclusive checks, 24 are continuous baseline-change checks with constant observed differences; the current interval policy does not interpret that as zero population uncertainty. The remaining inconclusive check is the shortsword challenge band.

## Goal review and sampling

| Draft goal | Current proposal | Disposition before enforcement |
| --- | --- | --- |
| Ordinary reliability | Clear rate ≥90%; at least 100 attempts | Retain as an experience proposal. Validate the actual starter choices and normal encounter composition before applying it to player progression. |
| Challenge readiness | Clear rate 60–90%; at least 100 attempts | Keep draft. Decide which authored encounters should intentionally permit repeated failure and whether each needs an upper bound. Do not tune ordinary enemies merely to fit the current label. |
| Winning pace | Mean winning duration ≤60 seconds; at least 30 victories | Retain as a pacing proposal. It is conditional on winning and does not measure time between encounters, rewards or progression. Read it alongside clear rate. |
| Clear-rate movement | Candidate minus baseline ≥−5 percentage points; at least 100 pairs | Retain as a practical-change proposal. A reference must be accepted for the same cohort, sample count and seeds. Meeting the minimum count alone does not guarantee enough precision near the bound. |
| Winning-pace movement | Shared-win mean duration change ≤+5 seconds; at least 30 shared wins | Keep draft. The eligible sample falls when either build loses; constant observed changes remain inconclusive under the current method. |
| Health movement | Mean remaining-health change ≥−10 percentage points; at least 30 pairs | Keep diagnostic. Health can trade off against speed and other effects; this metric cannot enforce a gate. |

Use three samples per cell for the CI workflow only. It produces 36 battles per run and 60 expected inconclusive draft checks. Preserve the goal sample minimums; do not weaken them to make the smoke report pass.

Use a predeclared 100-sample reference budget for the current review. Any later increase should be selected for the decision's required precision and confirmed with reserved seeds, not repeated until a check passes. The existing intervals are per check, without a correction across all 60 checks. The Bonferroni adjustment inside a paired clear-rate interval does not address this wider multiple-check issue.

## CI contract and verification

[balance-harness.yml](../.github/workflows/balance-harness.yml) runs the harness correctness tests through [run-tests.ps1](../build/run-tests.ps1), then [smoke-balance.ps1](../build/smoke-balance.ps1) on pull requests affecting backend/tool inputs and on manual dispatch.

The script creates two identical runs and a disposable, explicitly labeled repeatability manifest inside a new output directory. It verifies complete comparison with zero evidence/gameplay changes, detailed replay and valid draft evaluation. It neither reads nor replaces a reviewed baseline. This is a same-revision workflow check; it does not compare the pull request against its base branch. Existing correctness tests exercise changed-content detection, corrupt evidence and enforced evaluator outcomes.

Execution/integrity/replay failures fail CI. Draft balance failures and inconclusive findings remain advisory. If the source policy later contains enforced goals, this smoke script refuses it and requires an explicit CI policy decision rather than silently downgrading enforcement. A job summary identifies what passed and all evidence is uploaded with seven-day retention, including partial outputs on failure. The overall job has a 15-minute timeout and the smoke step a three-minute timeout. These are initial operational limits, not measured hosted-runner guarantees.

Local validation on 8 September 2026:

- Backend test project built successfully; 46 harness tests passed through `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`.
- Default smoke: 72 total battles across two runs, 12 matching cells, detailed replay, 60 advisory inconclusive checks, about 2.75 seconds and 3 MB of retained evidence before compression.
- Reference workflow: 2,400 total battles, 12 matching cells, detailed replay, the 30/5/25/0 evaluation above, about 10.52 seconds.
- Reusing an existing output directory was rejected without modifying its summary.
- An injected native command failure propagated as an error and retained a failure summary; draft balance failure remained advisory in the full reference run.
- PowerShell syntax, workflow YAML/paths/permissions/retention, all 51 local Markdown links across the four updated documents and whitespace checks passed.
- The initial sandboxed build could not write a generated assembly cache. The same build succeeded with expanded execution permissions; it reported 26 warnings and no errors.

Full local bundles are ignored under `TestResults/balance/phase2-smoke-validation`, `phase2-reference-review` and `phase2-reference-history`. Retain them if this local review needs to be audited; they are not committed or guaranteed to exist in another checkout. Hosted GitHub Actions execution remains to be observed after the workflow is pushed. No deployment, database migration or gameplay configuration change is part of this work.

## Next decision

The separately versioned First Hunt cohort now covers the three actual choices, guaranteed starter weapons, fixed possible quest-box outcomes and two-enemy later encounters. The follow-up [Blood Grove progression review](Blood-Grove-Progression-Review.md) tested unascended levels 1/10 and gear ranks 0/1/5: every cell still lost on both seed sets, and training alone changed no combat summaries. The [attainable-entry review](Blood-Grove-Entry-Review.md) also found no wins after testing Fury and all nine Armor Chest outcomes. The scope decision on 8 September 2026 defers Lumo Token replacements: require a viable starter progression path, with other Essence results remaining diagnostic. The user has selected Goblin Warrior, Sword and Heavy Chest for Blood Grove and approved a 70% clear-rate aim with a 65–75% initial working band. Its [separate reference](Blood-Grove-Starter-Reference.md) now has a fixed recipe and reviewed policy; the initial evidence fails that band. Next, run a focused tuning experiment and confirm a viable regression reference. Keep the original controls and their draft policies. Broader target approval and hosted CI validation remain open.
