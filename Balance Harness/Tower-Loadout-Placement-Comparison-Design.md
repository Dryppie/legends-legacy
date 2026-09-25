# Whole-loadout placement: fresh comparison design

Current status: **The prospective comparison is frozen and verified. Its native study adapter is not implemented and the study is not admitted.** The [machine-readable design](Tower-Loadout-Placement-Comparison-Design.json) specifies v6 whole-loadout placement versus the exact existing v5 allied-action proposer, with the same benchmark-validation selector. No combat, entropy allocation, preparation or runtime qualification occurred in this step.

The target remains the offline `LL/tools/BalanceHarness`. The [native implementation](Tower-Loadout-Placement-Native-Implementation.md) established a complete 238-recipe placement catalogue, deterministic 9+8 sampling on twelve development roots, and unchanged legacy outputs. These establish structural feasibility, not effectiveness.

## Question, comparator and interpretation

Does whole-loadout placement produce better **final selected outputs** than the existing v5 proposer at the same search-fight budget, and do those outputs improve on the strongest fixed benchmark, `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`?

The control is exactly `benchmark-allied-action-affinity-creation-v5`, including its three frozen affinity IDs, endpoint/provider protection and existing edit sampling. It is a development comparator, not a promoted or proven optimal policy. The candidate is exactly `benchmark-subgroup-loadout-placement-v6`: move complete five-Essence lists inside one five-owner subgroup, deduplicate the complete legal catalogue, shuffle distinct recipes once and consume nine then eight. Its complete catalogue has 238 recipes; the control's legal neighborhood has 26. Capacity checks must pass before allocation, and both policies must preflight before either search spends fights. The control's stochastic attempt limit can still underfill a fresh root; that invalidates the study without replacement or fallback to another operator.

This compares the entire proposer package. Neighborhood size, edit radius and sampling law change together with bundle preservation. A favorable result cannot isolate the causal value of any one of those components. Both arms retain ten fixed actors, equipment, styles, floor, copy/family rules, three references and the same selector. The hypothesis concerns this captured scenario; it does not establish encounter balance, robustness across floors or broad search superiority.

The earlier [v4-versus-v5 pilot](Tower-Affinity-Allied-Action-Pilot-01-Execution.md) was inconclusive. V5 returned the benchmark at every root, and its relative gain came from worse control outputs. Consequently this design requires a positive **absolute** benchmark gain as well as a relative gain. That requirement is prospective and does not rewrite the old pilot's decision.

A 12-root pilot retains the existing matched-budget framework and limits the first efficacy expenditure for an unmeasured proposer. It is a screening decision about a larger fresh evaluation, not a powered efficacy study. Evaluating the entire placement family would answer a different question about neighborhood quality, without establishing how reliably the unchanged selector finds a useful output. There is no sample-size or power claim inferred from the generation preview or the old pilot's small observed root variance.

## Fixed allocation and execution

| Item | Frozen allocation |
| --- | ---: |
| Fresh paired search roots | 12 |
| Shared training values per root | 4 panels × 8 |
| Shared nomination values per root | 16 |
| Shared benchmark-validation values per root | 60 |
| Charged search fights per arm/root | 528 |
| Total charged search fights | 12,672 |
| Independent held-out values per root | 256 |
| Held-out fights, depending on identical outputs | 3,072–9,216 |
| Total fight ceiling | 21,888 |
| Distinct fresh assigned values | 4,380 |

The JSON enumerates all half-open allocation ranges. Each search block has 109 positions: one root, four eight-value training panels, sixteen nomination values and sixty validation values. The twelve search blocks occupy positions `[0,1308)`; the twelve held-out panels occupy `[1308,4380)`. These are positions, not allocated seed values. All roots and roles are disjoint; both arms share the same ordered panels within a paired root.

After separate admission, one fixed 65,536-byte entropy exposure produces 16,384 signed 32-bit words. Take the first 4,380 distinct nonhistorical values in exposure order, without screening. Permanently reserve every distinct newly exposed value, including the unused tail; record historical collisions and within-exposure duplicates. Insufficient usable values fail without refill. Reconcile authoritative history and pending recovery receipts at admission and again at launch, including development roots and all unused prior reservations. The last verified registry count of 799,177 values across 264 files is historical context, not a current rescan.

The existing selector stays unchanged. Nominate one nonbenchmark challenger using sixteen values; then use sixty fresh paired values against the fixed benchmark. Pass only when gained wins exceed lost wins and the exact one-sided discordant-pair binomial tail is at most 1/20. Otherwise return the benchmark. This is a provisional search-output gate, not team confirmation.

Freeze all 24 complete search outputs before the first held-out observation. Within each root, evaluate selected control, selected candidate and benchmark on the same 256 values. Deduplicate identical physical recipes only within that root. Identical outputs have an exact zero method contrast, while their benchmark result remains recorded. Repeated recipes at different roots still use their separate panels. Shared physical recipe/panel/seed observations across search arms must agree; each arm retains its full charged search budget.

Any incomplete wave, root, gate, archive, audit or publication invalidates the study. Retain attempts, reservations and full charges. One admitted launch has no scientific retries, replacement roots, outcome-dependent stopping, sample extension or best-pool rescue.

## Endpoints and ordered decision

For root `i`, let `d_i` be candidate-minus-control held-out win-rate difference and `g_i` candidate-minus-benchmark difference. The primary endpoint is the equal-root mean of `d_i`; the absolute-benefit requirement uses the equal-root mean of `g_i`. Retain all twelve values, mean, median, worst result and standard deviation.

Use the existing descriptive root interval `mean ± 2.200985160082949 × sampleSD / √12`, with eleven degrees of freedom. Separately report paired-seed standard errors and covariance conditional on the frozen outputs. Battles, generated recipes and repeated appearances of a recipe are not independent search-root replications. The small root sample and possible mixture of exact fallbacks and novel outputs do not support a precise power or coverage guarantee. The interval is reported regardless of whether it crosses a decision threshold; the go decision is not a significance test.

Apply these rules in order, after complete verification:

1. Incomplete or invalid work: `InvalidStudy`.
2. Either mean contrast at or below −2 percentage points: `AbandonThisConfiguration`, regardless of any promising-root count.
3. No differing final outputs: `NoObservedOutputDifferentiation`.
4. `LargerFreshEvaluationWarranted` only when **both mean gains are at least +2 points**, at least three roots have different outputs, and at least three roots return a candidate output outside the three fixed references with at least +3 points observed gain over the benchmark.
5. Otherwise: `Inconclusive`.

The +2-point effect thresholds are proposed practical pilot criteria, not proven optimal settings. The additional three-root condition prevents one unusual root from being the only apparent discovery success. Novelty means “different from the three fixed physical references”; it does not mean globally unseen or previously unevaluated. The +3-point per-root flag is descriptive and does not qualify any team. Identical outputs can still have a nonzero absolute benchmark contrast, which remains visible even when the policy comparison reports no differentiation.

The integer boundary oracle avoids ambiguity: over twelve equal 256-value panels, +2 points requires at least **62 net wins**, and −2 points at most **−62**; +3 points in one root requires at least **8 net wins**. Tests cover both sides of each boundary and prevent promising roots from overriding the harm rule. All decisions end this pilot. A go result warrants a separately designed fresh evaluation; it never adopts a policy or confirms an individual team. An inconclusive result provides no automatic top-up or repeated-pilot permission.

Keep both arms' gate counts, exact tails, fallbacks, chosen recipes and all-root novelty/regressions. Record proposal attempts, rejections, duplicate rates, catalogue and permutation provenance, distinct generated/selected physical recipes, elapsed time, peak/retained bytes, charged fights and permanent exposure accounting. These diagnostics cannot replace a failed selected-output endpoint with an ungated nominee, favorable subgroup or best retrospectively chosen recipe.

## Runtime binding and implementation work

The proposed study version is `tower-loadout-placement-comparison-v1`. Individual racing versions already exist: v7 for the control and v8 for placement, both using `tower-racing-benchmark-validation-v1`. The mixed generation export remains v6. The new comparison, study runner, auditor and owned launcher still need explicit support; old version allowlists must continue rejecting this study.

The design binds the captured input from the sealed preview and authenticates its only prior-context change: the benchmark reference's diagnostic scenario ID. Every other context field matches the old generation context. The normalized comparison scope hash is `8cd7fdb0049b1e05320ae7921e1895703272a9951e5f2d171006eb458bd68b86`. Its ASCII/native hashing bridge is checked against the existing native historical scope hash. Qualification must still produce and bind the actual native plan using the tested runtime.

Keep the normalized comparison scope hash distinct from the full per-root placement catalogue hash. The latter includes generation and exclusion metadata and remains an input to the existing root-derived shuffle. Recipes are invariant across the saved roots; their complete scope-bound catalogue hashes need not be. Preserve the explicit Python/native physical-hash encoding bindings established by the preview. Never alter historic hashes to make formats agree.

| Component | Required change |
| --- | --- |
| `TowerProposalComparison` and a new placement comparison partial | Exact v5/v6 policy pairing, fixed design reconstruction, 109-value paired binding and strict version dispatch. |
| Pair validation | Shared scope/panels, but v7 has the captured affinity inventory and v8 has **null** inventory. Existing preservation pair validation requires equal inventories, so it cannot be reused unchanged or weakened globally. |
| `TowerProposalStudy` / protocol / archive | Register the new study, expose both arms' gate evidence, keep all-output freeze and failure/publication barriers, and retain/audit complete placement provenance. |
| Decision code | Add the exact new decision branch in C# and the independent Python auditor. The legacy selector branch ignores novelty; the other legacy branch makes benchmark harm conditional on novelty. Neither implements this design unchanged. |
| Independent audit and owned launcher | Recognize only the new explicit version, reconstruct placement recipes/waves and both hash formats, enforce null candidate inventory, budgets, complete exposure and exact analysis. |
| Tests | Full twelve-root literal-evaluator success/failure fixtures, decision boundaries, mixed inventory, panel isolation, early-held-out rejection, missing recipes, rehashed tampering, failure retention and legacy byte compatibility. |

The current boundaries are visible in [racing validation](../LL/tools/BalanceHarness/TowerProposalPolicies.cs), [preservation pair validation](../LL/tools/BalanceHarness/TowerAffinityPreservationComparison.cs), [comparison binding](../LL/tools/BalanceHarness/TowerAlliedActionComparison.cs), [study decisions](../LL/tools/BalanceHarness/TowerProposalStudy.cs) and [resource versions](../LL/tools/BalanceHarness/TowerProposalStudyResources.cs). The new design records the inspected implementation source hashes; backend sources are unchanged in this step.

## Resource admission remains a separate gate

| Partition | Hard seconds | Hard retained bytes |
| --- | ---: | ---: |
| Separate qualification | 900 | 1,073,741,824 |
| Scientific native work | 9,000 | 5,905,580,032 |
| Both audits and publication | 1,800 | 536,870,912 |
| Scientific total | 10,800 | 6,442,450,944 |

Keep `tower-proposal-resource-envelope-v2`, factor-of-two measurement margin and the 120-second publication reserve. Apply raw-measurement margins before taking upward-only maxima with already margined inherited floors. Compare each time/storage partition separately; spare native capacity cannot enlarge the audit partition. The inherited floors, rounded upward, are **6,694 / 1,764 seconds** and **5,168,096,099 / 34,989,702 bytes** for native/audit work. The audit floor leaves only **36 seconds** below its cap. That is a risk to future admission, not a current-runtime forecast or a reason to raise limits after observing costs.

Qualification must measure new metadata and retention costs, including the full placement catalogue and derivations, scenario-hash bindings, repeated reconstruction, symbols/sources/runtime, both audits and publication. Take maxima with inherited admission floors, the frozen audit probe, completed pilot phase costs scaled to the 21,888 ceiling, current guarded preparations and upward-only old/new owned-fixture ratios. Apply storage growth before audit-byte scaling. No faster timing may reduce inherited floors, and no timing retry may select a favorable measurement. A failed forecast remains not admitted; any resource amendment is separately versioned prospectively before entropy.

Authenticate producing assembly/PDB/source correspondence and captured gameplay dependencies. Rebuild the same inventory. Compare materialized inputs and prepared participants under a guard that throws on combat entry, covering all 238 placements, the three references, saved control waves and required legacy coverage using saved development values. Reconstruct the complete generation preview and literal study fixtures. No prospective roots may be previewed or screened.

Only qualified episode labels/start time, accumulated history exclusions and harness execution identity may be rebound at admission, with a separately pinned scope and the original design retained. Actor/gameplay fields, content, gear, inventory, references, policy, selector and scientific rules remain frozen. Neither the historical v5 admission nor the generation-only preview qualifies the new study. Qualification and scientific execution each require their own declaration and full allowance charge when started; neither has started here.

## Verification, accounting and next step

Added this report, the [machine-readable design](Tower-Loadout-Placement-Comparison-Design.json), [design builder/verifier](analysis/loadout-placement-comparison-design.py), and [18-test suite](analysis/test-loadout-placement-comparison-design.py). Three LF rules preserve the new JSON/Python files. Nine current-status documents change only line three. Their historical bodies, all 194 inherited immutable pins and all 229 inspected harness C# files remain unchanged.

All **18 tests passed**, covering exact policy/context bindings, complete disjoint value layout, matched fights, resource partitions, relative-only false success, novelty and harmful-result precedence, strict JSON, source tampering and twelve altered-design boundaries. Creation and verification from retained evidence alone passed. The [sealed design package](../TestResults/loadout-placement-comparison-design-20260924/files.json) retains the consumed source artifacts; the [verification package](../TestResults/loadout-placement-comparison-design-verification-20260924/files.json) retains logs and publication checks. Backend tests were not rerun because no backend implementation changed; the prior 350-backend-test and 15-preview-verifier receipts remain authenticated. No required command remains blocked.

```powershell
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-comparison-design.py"
python -B -X utf8 "Balance Harness/analysis/loadout-placement-comparison-design.py" create --output "TestResults/loadout-placement-comparison-design-20260924"
python -B -X utf8 "Balance Harness/analysis/loadout-placement-comparison-design.py" verify --evidence-root "TestResults/loadout-placement-comparison-design-20260924/evidence"
```

The single bounded design publication charged its full **180 seconds / 67,108,864 bytes** at start, including failure. It completed without retry in **1.203 seconds before sealing**, retaining **32,615,302 bytes**. Ordinary synthetic tests and publication verification are separate engineering work. Cumulative recorded charges are **53,839.661 seconds / 38,559,138,110 bytes**; cumulative declared maxima are **142,740 seconds / 93,700,751,360 bytes**. Future qualification/scientific ceilings are prospective, not executed charges. No history rescan is claimed.

**Next: implement the exact comparison adapter, native/independent audit decision branch and owned-runner support, then validate synthetic end-to-end fixtures and legacy compatibility.** That implementation must not allocate fresh values or run combat. Current-runtime qualification and resource admission follow only after those checks pass. No migrations, application configuration changes, gameplay/default changes or deployments are involved.
