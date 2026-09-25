# Paired affinity-preservation edit diagnosis

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**Endpoint preservation changes what gets removed without improving the measured average pool.** In the captured original pool, 47 edits remove an existing endpoint of a selected affinity that the addition could complete; the preserving pool removes none. However, Pack Howler removals increase from **49 to 73**, and Enchanted Fairy removals from **58 to 61**. These replacements give up abilities beyond the selected poison routes.

The [new diagnosis](../TestResults/affinity-preservation-edit-diagnosis-20260924/diagnosis.json) joins all **285 generated physical root/recipe cells**, representing **408 arm occurrences**. It retains **109 measured physical cells and 176 explicit unknowns**, with both-arm provenance and inclusion weights. It performed **no combat, native preparation, entropy draw or seed reservation**. The recognition decision remains `CompleteDiagnosticOnly`, and the source pilot remains `NoObservedOutputDifferentiation`.

## What the comparison establishes

Both arms still insert Viper in every accepted proposal. Each arm has seventeen generated recipes per root, across twelve roots. The preserving arm has 39 distinct generated recipes across roots, versus 57 for the original arm. These are the actual captured search trajectories, not the earlier combat-free preview; their attempt and removal counts differ from that preview.

The weighted pool estimates remain **−9.025 points** against the benchmark for original v3 and **−9.009 points** for preserving v4. Their **+0.015-point** difference has separate sampling and combat standard errors of **0.807 and 0.300 points**, respectively. Those components are not combined into an interval. The preceding [recognition report](Tower-Affinity-Preservation-Recognition-Execution.md) contains all twelve root estimates and uncertainty details.

| Membership within an arm | Original population / measured / unknown | Preserving population / measured / unknown | Original contribution | Preserving contribution |
| --- | ---: | ---: | ---: | ---: |
| Shared physical recipes | 123 /48 /75 | 123 /48 /75 | −4.700 pp | −4.700 pp |
| Arm-exclusive recipes | 81 /30 /51 | 81 /31 /50 | −4.325 pp | −4.309 pp |
| All generated occurrences | 204 /78 /126 | 204 /79 /125 | −9.025 pp | −9.009 pp |

Shared rows have one physical measurement, carried into both arm summaries with the same weight. Their contributions cancel from the paired pool difference. The 157 measured arm occurrences are therefore **not 157 independent measurements**. The two sets of 81 exclusive recipes are not one-to-one counterfactual replacements.

Each contribution is `sum(observed benchmark gain / inclusion probability) / 204`. Mandatory cells have probability one; sampled remaining cells use their frozen 2/N probabilities. Complete metadata defines each post-hoc group. Contributions for a disjoint partition sum to the arm estimate; removal and signal groups can overlap. The exporter does not calculate an unweighted pooled sample mean, fit a policy score or create group confidence intervals. A group with no observations is labeled unknown.

## Removal costs suggested by the captured mechanics

| Removed feature or Essence | Original occurrences / measured | Preserving occurrences / measured | Original weighted contribution | Preserving weighted contribution |
| --- | ---: | ---: | ---: | ---: |
| Completable selected endpoint | 47 /16 | 0 /0 | −2.435 pp | No occurrences |
| Pack Howler | 49 /21 | 73 /30 | −1.957 pp | −3.108 pp |
| Enchanted Fairy | 58 /14 | 61 /17 | −2.851 pp | −3.293 pp |
| Either Howler or Fairy, deduplicated | 105 /35 | 129 /44 | −4.808 pp | −5.743 pp |

The final row counts each occurrence once even if it removes both Essences. In the preserving arm, 43 of its 44 measured occurrences are below the benchmark and one is above; 85 remain unknown. This is a post-hoc description of selected edits, not a causal estimate of the removed abilities. Owners, additions, other removals, repeated recipes and shared panels remain entangled.

The [captured mechanics snapshot](../TestResults/affinity-preservation-edit-diagnosis-20260924/mechanics.json) supplies concrete reasons to investigate these costs:

- **Pack Howler:** `coordinated_attack` performs Basic Attacks for non-summoned allies. `strength_of_the_pack` separately grants owner Power according to living allies, capped at 18%. Replacing the Essence removes both abilities. The observed loss cannot be attributed solely to the attack-grant effect or solely to Power.
- **Enchanted Fairy:** its active ability deals damage and applies Corrosion; another ability periodically attempts Stun. Replacing it can remove damage, debuff application and control together.
- **Viper:** the new modifier increases the owner's Poison damage by 7%, and its active ability deals Physical damage with an additional conditional effect against Poisoned targets. The 7% modifier is neither a seven-point win-rate benefit nor an estimate of its net value after removing another Essence.

The preserving rule protects selected affinity endpoints, so it correctly prevents the first class of removal. It does not protect these other abilities. The data establish that removal composition shifts; they do **not** prove that these shifts explain the near-zero overall policy difference.

Every removal group, including positive and unobserved groups, remains in the [complete tables](../TestResults/affinity-preservation-edit-diagnosis-20260924/diagnosis.md). For example, preserving-arm Illusion Fox removal has five catalogue occurrences and zero measurements, so its outcome remains unknown. Small positive groups are not evidence for an Essence blacklist or fitted removal preference.

## Owner fit and subgroup coverage

Each joined row retains the exact owner, subgroup, equipment, progression, removed/added abilities and authored signals. It compares signals present before and after the edit and counts how many of the five subgroup owners retain each signal. These are structural presence counts, not realized damage, trigger uptime, successful control, role labels or an equipment compatibility score.

Howler removal changes the subgroup's authored `PerformBasicAttack` provider count from **five to four** in every such edit: 49 original and 73 preserving occurrences. Fairy removal changes the authored Corrosion-provider count from **five to four or four to three**. Consequently, a rule that only preserves the *last* provider would not prevent either pattern here. Repeated providers can still affect action frequency or application opportunities; these records do not quantify that marginal value.

All nine edited-owner groups and all four main-hand groups have negative weighted point estimates in both arms. Owner 8 is never edited because the target pairs are already present. These observations provide no independently supported preferred owner or weapon type. Equipment is fixed and confounded with owner loadout; changing it is outside this comparison. The fixed-equipment exports are useful for constructing the next mechanical hypothesis, not for learning an outcome-ranked owner schedule from these twelve roots.

Original v3 contains 177 one-slot and 27 two-slot edits; preserving v4 contains 176 and 28. All six measured original two-slot occurrences and all twelve measured preserving two-slot occurrences are below the benchmark. That small, unequal sample does not establish a causal edit-distance penalty. Both complete distance groups and all unknowns remain in the exported tables.

## Single next implementation step

Implement a **combat-free, opt-in preview of an allied-action-preserving removal rule**, composed with the existing selected-endpoint protection. Derive protected parent Essences from captured authored ability effects that grant Basic Attacks to allies; do not name Pack Howler in the generator or use these measured gains as scores. The immediate scope can use the exact supported `PerformBasicAttack` / `NonSummonedAllies` relationship observed in the captured inventory, with explicit validation of the relationship and no inference from display tags.

For each proposed affinity addition, retain the existing minimum replacement distance and uniform selection among eligible pairs and their legal removals. Exclude removals of the derived ally-action providers. Keep the affinity inventory, benchmark parent, equipment, owner schedule and all other policy parameters fixed. Version the removal rule and record both reasons for protection, legal-choice counts and explicit exhaustion. Existing `TowerAffinityCreation.Opportunities`, policy contracts and deterministic two-wave exports provide the implementation points.

First enumerate and export the complete legal neighborhood and replay all twelve development roots with **zero combat**. Verify whether the rule can still supply nine first-wave and eight second-wave unique candidates. Report reduced coverage, duplicate pressure and incomplete fill honestly; do not relax protection silently or use a favorable observed subgroup to choose replacement roots. If it cannot fill the required budget, it is unsuitable for this comparison as specified.

This is one narrow, mechanically motivated hypothesis: preserving an ally-facing action source may avoid paying a broad action cost for a local poison modifier. It remains **unproven and informed by post-hoc diagnosis**. It also leaves Fairy and other removal costs unresolved and may merely shift losses again. Therefore a feasible preview would justify only a separately designed fresh matched-budget proposer test, with racing and validation fixed, rather than promotion. No new rule, native preview, resource admission or combat comparison was implemented or launched in this diagnosis.

## Implementation and verification

Added [the paired diagnosis adapter](analysis/affinity-preservation-edit-diagnosis.py) and [25 regression tests](analysis/test-affinity-preservation-edit-diagnosis.py). The adapter reuses authenticated pure helpers from the sealed earlier edit diagnosis. It reconstructs the published recognition summary, then verifies each arm's plan/policy, accepted batches, exact parent/party/owner identities, legal minimal edits, target routes, activation metadata, preserving eligibility counts, fixed physical context and frozen provenance.

The tests use a sealed published engineering fixture with literal outcomes. They cover complete physical/arm membership, shared-cell identity, weighted reconciliation, changed removal/protection counts, plan and equipment drift, duplicated/missing/cross-root measurements, nonfinite contrasts, false weights, imputed unknowns, source mutation, mechanics coverage and old-score isolation. Three initial test setup errors selected a rejected proposal without edit metadata; the corrected cases select an accepted proposal. Both logs are retained. **All 25 final tests passed; the production diagnosis completed once.**

The new adapter is separately versioned. Earlier source code, scientific archives, corrected and failed recognition reviews, and their charges remain unchanged. Nine existing guides receive only a line-three current-status update; their historical bodies are byte-preserved. Two new `.gitattributes` LF rules preserve this adapter and its test hashes across Windows checkouts.

Commands used the bundled Python interpreter with `-B -X utf8`:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-affinity-preservation-edit-diagnosis.py'
python -B -X utf8 'Balance Harness/analysis/affinity-preservation-edit-diagnosis.py' --output 'TestResults/affinity-preservation-edit-diagnosis-20260924'
```

The second command is a completed single-use invocation and refuses an existing output. Scoped whitespace, syntax, source hashes, report links, prior evidence and historical-body checks are recorded in the [verification closeout](../TestResults/affinity-preservation-edit-diagnosis-verification-20260924/closeout.json). No required command remains blocked. No C# source changed, so backend tests were not rerun. There are **no migrations, application configuration changes, deployments, gameplay edits or default-policy changes**.

## Evidence and accounting

The diagnosis declared **180 seconds /64 MiB** before processing the cohort, charged in full including failure. It completed in **6.031 seconds**, retaining **6,990,192 bytes**. Cumulative recorded charges are **39,760.718 seconds /30,916,202,527 bytes**; cumulative declared maxima are **102,180 seconds /63,367,544,832 bytes**. Tests and documentation checks are separate engineering work.

The inherited last complete live-history verification is **766,415 excluded values across 260 files**. This read-only diagnosis does not rescan it or allocate anything. Full direct-battle reconstruction remains supported by the pinned scientific audits; this adapter authenticates consumed files and reconstructs the joined endpoints without another battle audit.

| Evidence | SHA-256 |
| --- | --- |
| [Diagnosis manifest](../TestResults/affinity-preservation-edit-diagnosis-20260924/files.json) | `cdc7dd541f7507693929cf03a5aca7bc173959e545e8ce2c7ce4e94d006a0a5f` |
| Corrected recognition review | `498669c35f09c67f8bb9ef1d7e202b966bc83663deee1df1b6875ae3170ed4b6` |
| Recognition scientific archive | `dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39` |
| Paired generation archive | `1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4` |
| Frozen catalogue | `c9b448243b4cd2edb530678740c733e485257550de7810db6fb5296420330529` |
| Prior execution verification | `28af4f0229b4ee83540808e5fa0a147be15305c7054e67d721d7c3042629a5c2` |

The JSON export retains every consumed manifest/member pin, complete structural signal changes, all joined outcomes or nulls, group denominators and exact accounting. Unrelated working-tree changes were preserved.
