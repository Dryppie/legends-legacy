# Prospective comparison of periodic reference exploration

22 September 2026. **Frozen prospective design — subsequent execution completed and closed.** Target: offline `LL/tools/BalanceHarness`. The [single comparison and both audits](Tower-Practical-Reference-Exploration-Comparison-Execution.md) completed 53,672 fights and returned `DoNotPromoteReferenceExploration`. The [controller](Tower-Practical-Reference-Exploration-Comparison-Implementation.md) and [captured-runtime admission](Tower-Practical-Reference-Exploration-Comparison-Admission.md) implemented and bound this protocol before the draw. The planning record below and its immutable JSON/evidence remain unchanged in scientific scope. The [JSON contract](Tower-Practical-Reference-Exploration-Comparison-Plan.json) and [arithmetic checker](analysis/practical-reference-exploration-design.py) describe `tower-reference-exploration-comparison-v1`. Planning itself produced no candidates, values, native preparations or fights; later work is documented separately.

Compare **12 paired construction restarts**, holding gameplay, inputs, search budgets, nomination and selection fixed. The candidate changes only periodic fresh proposals into bounded reference exploration. The primary target is an average **five-percentage-point** confirmation improvement over the twelve frozen output pairs, with positive evidence in at least seven pairs. A positive result supports those outputs in the captured cohort; it does not automatically change a default or adopt a team.

## Fixed scientific scope

| Item | Declaration |
| --- | --- |
| Baseline generator | `retained-composition-three-references-v1` |
| Candidate generator | `retained-composition-three-reference-exploration-v1` |
| Paired restarts | 12, each running two independent adaptive searches |
| Common selector | `tower-staged-incumbent-tie-v1` |
| Common tie designation | `confirmed-399bc7760fb0cf790a5d8ac4` |
| Discovery per arm | 46 evaluated recipes, at most 256 attempted proposals, eight trials per recipe |
| Selection per arm | All three references plus two ranked challengers; 32 trials each; one selected output |
| Confirmation per pair | 1,000 common trials per distinct physical recipe in the union of both selected outputs and all three references |
| Character scope | Floor 5; ten level-40 characters; two groups of five; five Essences each; rank 2 /tier 1; captured standard equipment and fixed identities |
| Acquisition and order | `OwnedCopies=null`; unchanged allowed pool and canonical Essence order |
| Diagnostics, replay, extension | None |

The exact controls are `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b`, `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50` and `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`. All three are supplied to both algorithms. Historical fitness is not input to either search. The confirmed third reference is not silently made the tie-designated primary.

Use the seed-free `preset/template.json` from the [reconciled three-reference admission](Tower-Practical-Three-Reference-Admission.md), with SHA-256 `cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83`. Preserve its captured gameplay dependencies, 16 content files, settings, fixed recipes and equipment context. The old native package's `files.json` is `001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d`; the reconciled closeout manifest is `65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213`. Authenticate the original appended failure through that receipt as well. These are source bindings, not admission of the new comparison executable.

The existing 275-test generator receipt is authenticated by the planning checker. The future controller needs its own tests and producing-runtime capture. Its harness must be compatible with the preserved gameplay assemblies; current development gameplay is not a substitute when compatibility fails.

## Pairing, execution and the global freeze

1. Before allocation, freeze this protocol, both definitions, all source/runtime/content/settings pins, exact controls and a newly refreshed complete exclusion inventory. Within each pair, definitions must match after normalizing only the generation policy version. Keep the same scenario identity within the pair so a label cannot change combat randomness.
2. Give both arms the same construction root, eight discovery values and 32 selection values. Run pairs 1–12 in order, baseline before candidate within each pair. Each arm measures its own search trajectory and every selection nominee; no search-result cache or outcome sharing between arms is introduced. Each successful arm costs **528 fights**.
3. Save all 24 search archives and the selected physical recipe from each. An incomplete candidate target, missing nominee or invalid input fails the comparison without a replacement root. Discovery failures remain visible even though they cannot contribute a valid scientific endpoint.
4. After all searches complete, durably publish one global freeze of all **24 outputs**, all twelve physical confirmation unions, their ordered panels and the attempt-journal hash. The freeze must record exactly **12,672 completed search fights**, with no unfinished attempt. No confirmation battle or confirmation-dependent choice can precede this barrier.
5. Within each pair, confirm the union of the three controls and both outputs once per physical recipe, in ordinal physical-recipe-hash order, on all 1,000 values. Physical identity includes ordered owners, equipment, progression, Essences and actor identities in the same fixed encounter/context; display labels do not establish identity. No sharing occurs across pairs, whose panels differ.
6. Reconstruct search, selection, freeze, shared evidence and the result through the producing native runtime, then authenticate and independently audit the direct saved outcomes. Publish a complete result only after both audits and owned-process/resource checks pass.

Each arm still has a logical confirmation family of three or four recipes. Shared controls are measured once per pair and referenced by both logical views; the physical pair union contains three, four or five recipes. Shared evidence must use exact trial references and charge each physical fight once. Do not fabricate two complete ordinary study archives with duplicate physical-fight accounting. Use a comparison-specific archive containing both searches and one confirmation union per pair.

The family-ten practical rate and paired-control calculations may be reproduced in each logical view, but they are **descriptive within this comparison**. Repeating them over 24 arms does not create experiment-wide family-ten error control. They are not additional promotion endpoints or a multiple-candidate adoption procedure. No best root or recipe is selected for an adoption claim from these panels.

## Primary endpoint and decisions

For pair `r`, count `G_r` where exploration wins and baseline loses, and `L_r` where baseline wins and exploration loses. Use the same confirmation value in each paired comparison. The primary mean is

```text
net  = sum_r (G_r - L_r)
mean = net / 12000
```

Every planned pair stays in the denominator. Identical selected physical recipes contribute exact zero difference, using their shared evidence; they do not disappear from the endpoint. Their absolute wins are measured because all confirmation unions still run. Different outputs need not create five union members: for example, two different selected controls still give a three-recipe union. The differing-output count and the physical-fight count must therefore be computed separately.

Return `SupportsReferenceExplorationForFrozenOutputs` only when the complete comparison and both audits are valid, **net ≥600**, the one-sided 95% conditional lower bound below is strictly positive, and **at least seven of twelve pairs have `G_r > L_r`**. Use integer counts for the five-point gate; do not gate on rounded percentages. Seven positive pairs is a replication safeguard, not a confidence statement about future roots.

A verified complete run with differing outputs that fails a gate returns `DoNotPromoteReferenceExploration`. If all twelve selected outputs coincide, return `NoSelectedOutputDifferences`, with exact zero mean/lower bound; that is not proof of equivalence on future searches. All declared confirmation unions still complete even when fewer than seven pairs differ. Invalid evidence returns `IntegrityFailure`; interrupted or incomplete execution returns `IncompleteComparison`. Neither permits a performance conclusion, another root, a replacement method, extra trials or outcome-dependent extension.

Report coverage, construction exhaustion, duplicates, parent diversity, nomination, runtime and storage as diagnostics. They cannot replace the strength endpoint. Keep all roots, including negative differences, visible. The [diagnostic root](Tower-Practical-Three-Reference-Coverage-Review.md) that motivated the policy and all earlier panels remain excluded from this experiment's evidence.

## Conditional precision

The estimand is the mean strength difference of the **twelve frozen output pairs**, conditional on all search values and observations, over the fresh 32-bit population after those search values. It excludes uncertainty over future construction roots. Do not condition this claim on the realized confirmation values, unused entropy tail, control-only outcomes or successful completion. Later experiments use their own updated excluded population.

Let `H` be the complete pre-draw history count; `M=2^32-H-492`; `R=12`; `B=1000`; `N=12000`; `K` be the number of differing output pairs; `n=1000*K`; and `alpha=.05`.

```text
depletion = n(n-1) / (M N)
margin    = sqrt(2 n log(1/alpha)) / N + depletion
lower     = max(-K/R, mean - margin)
```

When `K=0`, set `depletion=margin=mean=lower=0`. The fixed denominator and marginal fresh-value population remain the same when controls or identical outputs consume confirmation fights. There is one primary contrast. Per-root uncertainty may be described without extra significance gates.

The conditional-range concentration inequality is given in [Kuang Yang's martingale lecture, Theorem 6.2](https://jhc.sjtu.edu.cn/~kuanyang/teaching/CS3341/notes/lec06.pdf). Its application here is derived as follows. Enumerate only active paired assignments; their marginal values form a uniform ordered sample without replacement from `M`. Each fixed recipe pair defines a function in `[-1,1]`. Removing `i-1` earlier active values shifts its conditional expectation by at most `2(i-1)/M`; summing gives `n(n-1)/M`. Centered increments have conditional range length two. The upper-tail bound at `t=sqrt(2n log(1/alpha))`, plus this drift and division by `N`, gives the formula. Other recorded assignments are integrated out for this claim. The global freeze is essential to keeping these functions fixed.

Illustrative arithmetic uses the last verified **H=551,408**, not a substituted live admission count:

| Differing pairs K | Conditional margin | Seven-positive gate possible? | Aggregate-gates power lower bound at stipulated true +7 points |
| ---: | ---: | --- | ---: |
| 0 | 0 | No | Not applicable |
| 3 | 1.117 points | No | 99.993% |
| 6 | 1.580 points | No | 99.177% |
| 7 | 1.707 points | Yes | 98.366% |
| 9 | 1.935 points | Yes | 95.922% |
| 12 | 2.235 points | Yes | 90.922% |

The power column covers only the aggregate mean and lower-bound gates, conditional on a stipulated true mean and `K`. It excludes replication, completion and future-root variation, and is not a forecast based on old outcomes. For `g=max(.05,margin)` and a stipulated mean `theta`, the lower-tail counterpart yields `1-exp(-(N*(theta-g-depletion))^2/(2n))` when `theta>g+depletion`, otherwise zero. At exactly the target true mean of five points, this conservative power guarantee is zero. The trial count is not a promise to resolve every useful effect, and no extension follows from an inconclusive result.

Twelve pairs broaden the earlier three-root generator comparisons while bounding the test. Their repeated combat measurements do not turn them into 12,000 independent search roots. General search reliability and current-gameplay calibration need separate evidence.

## Allocation and resource envelope

Draw one **16,384-word** 32-bit entropy batch after successful admission. Reject historical and duplicate words without refill. Assign the first **12,492** accepted fresh values: first twelve search blocks of 41 values (one shared construction root, eight discovery, 32 selection), then twelve confirmation blocks of 1,000. The construction roots, stage panels and different pairs are disjoint; the declared within-pair reuse is intentional. If insufficient accepted values exist, fail with the recorded draw rather than refill. Permanently exclude every fresh value from the entire batch, including unused tail and interrupted assignments. Require `H≤983,616` so even the whole batch fits the existing million-value registry ceiling.

If `U_r` is a pair's physical union size, actual completed cost is

```text
search fights       = 12 * 2 * (46*8 + 5*32) = 12672
confirmation fights = 1000 * sum_r U_r, where 3 <= U_r <= 5
total fights        = 48672 .. 72672
```

At four union members for every pair, cost is **60,672 fights**. Equal outputs do not erase the controls' cost. Logical per-arm counts and physical billed counts must be reported separately. Failed attempted battles remain charged; `started` must equal `completed` for a complete comparison. Fewer accepted candidates or skipped controls cannot be presented as a successful cheaper run.

The proposed **new** cumulative envelope is **10,800 seconds /6 GiB**, consisting of a conservatively charged **600-second /512-MiB** admission allowance and **10,200 seconds /5.5 GiB** for one scientific execution, both audits and publication. The native child is capped at **10,080 seconds /5.25 GiB**, leaving 120 seconds /256 MiB of enclosing headroom. Native and enclosing accounting must share the remaining execution deadline; later audits do not receive a fresh full budget. Disk checks include retained evidence and temporary output, and copied runtime files remain charged in their respective phase. Exceeding any limit stops the owned process tree and closes the attempt.

Proportional scaling of the closed 3,528-fight run's 416.938 seconds /159,547,483 bytes gives approximately **8,588 seconds /3.061 GiB** at the maximum fight count. This deliberately simple estimate includes that run's fixed overhead and is not a completion guarantee; the new archive/controller must be admitted under its own limits. No real search is launched to tune this estimate. Builds, synthetic fixtures, planning and helper development remain separately disclosed under the user's existing engineering-accounting decision; no missing historical total or prior-charge ledger is reset.

These limits are a concrete proposal for a new request, not a transfer of any closed allowance or a launch authorization. Changing the protocol or its limits requires a new version before any scientific draw. The existing scientific studies remain closed.

## Controller and admission requirements recorded before execution

The existing `TowerAllocationComparison` requires three restarts, two references and its old selector. `TowerIncumbentTieComparison` requires one common search and two selectors. Neither is a valid launcher for this protocol, and their guards remain intact.

Implement a separately versioned controller using the existing generation, measurement, selection, physical-recipe identity, battle storage, owned-process and reservation patterns. Reuse those lower-level components where applicable; do not claim the missing controller exists. It must retain two search archives per pair, one global freeze and one physical confirmation archive per pair. The verifier reconstructs both searches and actual selection from saved rows, checks logical-to-physical evidence mappings and deduplication, reconciles every attempt, and reproduces the endpoint without combat. A separately implemented saved-row auditor authenticates direct battle outcomes, panel alignment, all gains/losses, costs, unused reservations, precision and the decision.

Required fixtures include both selectors staying identical across arms; policy-only input differences; three protected nominees within each five-team shortlist; paired root/stage mapping; convergence on controls and on a novel recipe; different selected controls with a three-member union; four/five-member unions; no cross-root reuse; exact 600-net-win and seven-positive boundaries; negative roots; missing/reordered evidence; changed recipe identities; interrupted allocation/search/freeze/confirmation; global barrier violations; re-sealed trace/selection tampering; deadline/storage failure; and zero-combat native/archive verification. Backend tests run through `build/run-tests.ps1`.

After implementation passes, perform one bounded seed-free admission:

1. Authenticate the reconciled source capture, exact template and all gameplay/content/settings hashes. Capture the tested comparison harness and its complete source/symbol/runtime inventory; confirm it preserves the intended gameplay scope.
2. Validate both policy definitions with temporary validation labels, equal search costs, all three exact references, the selector designation and legal native input preparation. Keep a combat-entry guard active; labels are not allocated scientific values. Resolve compatibility failures before publishing an admission result.
3. Refresh the complete live history, including pinned abandoned-reservation recoveries, and verify both native and independent interpretations. Bind the exact request to that inventory, new plan, source/runtime hashes and cumulative limits. Require a new absent scientific output directory.
4. Freeze the immutable request, admission result, helper sources, runtime/content inventory, process receipts and remaining-budget ledger. Record all failed work without deleting it or converting it into a successful admission. No entropy is drawn by this step.

Only a passing admission could make the scientific run concrete. This planning task did not supply that result. The separately documented controller implementation, captured-runtime admission and single owned comparison with both audits have since completed. Their later evidence does not alter this frozen design or reopen its closed allocation.

## Planning verification

**Ten arithmetic tests passed** through:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/practical-reference-exploration-design.py' --self-test --output 'TestResults/reference-exploration-comparison-design-20260922/arithmetic.json'
```

The checker validates costs, the fixed denominator, convergence and shared controls, integer/replication boundaries, missing evidence, precision, resource headroom and small-population depletion bounds. It authenticates the consumed admission/closeout/template files and all eleven pinned generator source/test files against the prior 275-test receipt. The [arithmetic report](../TestResults/reference-exploration-comparison-design-20260922/arithmetic.json) explicitly records `admissionReady=false` and `comparisonRunnerImplemented=false`. This is source authentication, not a full current registry scan or a native runtime check.

Planning changed this protocol, its JSON contract and checker, plus documentation pointers. Source syntax, new local links and scoped whitespace passed. No backend code changed during planning, so backend tests were not repeated. No required planning check was blocked; controller and native admission checks were future work at that point and their subsequent completion is documented separately. Planning performed no application configuration change, migration, database operation, deployment or scientific reservation. Defaults, confirmed-team recommendations, the captured balance Fail and unresolved broader calibration questions remain as previously recorded.
