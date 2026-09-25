# Prospective incumbent tie selector comparison

22 September 2026. **Frozen design; the single experiment is complete.** The [execution report](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) records **SupportsIncumbentTieForFrozenOutputs**, both archive audits passing, +2.575 points across the 24 frozen outputs and a +1.930-point conditional lower bound. The protocol below and its original sealed planning package are unchanged; this update records status only. The scope is closed and defaults remain unchanged.

Target: offline BalanceHarness. This follows the [optional selector implementation](Tower-Practical-Incumbent-Tie-Implementation.md). The [machine-readable plan](Tower-Practical-Incumbent-Tie-Comparison-Plan.json) declares version `tower-incumbent-tie-comparison-v1`. No values, parties, native preparations or fights were produced by the original planning step. The subsequent [runner implementation and verification](Tower-Practical-Incumbent-Tie-Comparison-Implementation.md) and [captured-package admission](Tower-Practical-Incumbent-Tie-Comparison-Admission.md) retain their separate engineering evidence.

Compare the existing `tower-staged-zero-win-health-v1` selector with `tower-staged-incumbent-tie-v1`, using **24 fresh construction restarts**, one fixed generator and exactly the same search measurements for both selectors. The primary endpoint includes all 24 restarts. Confirm only the restarts where the selected recipes differ; identical outputs contribute an exact zero difference without combat. The existing selector remains the default throughout this experiment.

## Question and fixed scope

Does retaining the designated supplied primary on positive selection ties improve the average strength of the outputs from these 24 declared searches? This isolates the selector. It does not compare generators, assess encounter balance, adopt a new team or establish reliability over every future search root.

Use `retained-composition-incumbents-v1`, the current generator default. Each restart evaluates 46 candidates, including the two supplied teams, with a 256-proposal ceiling, eight discovery trials and four protected nominees measured on 32 selection trials. Generation, discovery ranking, nomination and measurement order are common to both selectors and run **once**, costing **496 fights per restart**. Any incomplete candidate allowance or missing nominee fails the experiment; no replacement restart is allowed.

Both selectors receive the confirmed `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b` team and Anchor040e, canonical ID `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50`. Only the candidate selector sets `stages.selectionPrimaryReferenceId`, fixed to `confirmed-399bc7760fb0cf790a5d8ac4`. The baseline has no selection designation. Neither receives historical fitness or confirmation outcomes. The anchored generator's top-level primary field is absent in both definitions.

Retain the ten level-40 characters, five Essences each, floor 5, one equipment context, two subgroups, fixed identities/equipment and canonical ability order of the confirmed capture. `OwnedCopies=null` remains the acquisition assumption. Use the captured gameplay assemblies, content and settings in `TestResults/anchored-comparison-20260917`, not the changing development gameplay build. Its package manifest SHA-256 is `1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314`; its seed-free template hash is `cc0ae4e8ccacb09ad169386c3f1c25fc1de6c93f9485dd20fe59438f6122a31a`. This borrows its scope and recipes only, not its generator, seeds or outcomes.

The future preparation step must authenticate that package, derive the incumbent-generator template, capture the newly tested harness, verify compatibility with the original gameplay DLLs, pin the resulting complete execution identity, and publish an immutable request/source/runtime inventory. A compatibility failure stops before allocation; it does not authorize substituting current gameplay. Source and runtime capture remains required engineering work, not a completed readiness claim here.

## One common search, two frozen decisions

1. Freeze the design, template, primary identity, tested producing runtime and complete historical exclusion inventory before allocation.
2. Run the 24 searches in fixed restart order. For each, save proposals, discovery measurements, exact nominee order and the full selection matrix once. Invoke each real selector on that same frozen matrix and retain its selected recipe and reason. Do not run two discovery trajectories or two independent selection panels.
3. After all 24 searches complete, freeze all **48 selector outputs**, the ordered recipe identities and every confirmation assignment in one global artifact. No confirmation fight may start before this barrier. Total search charge at the barrier must be **11,904 completed fights**.
4. Compare exact physical recipe identities, including character slots, gear, progression and normalized identities. If both selectors choose the same recipe, record `IdenticalOutput` and an exact difference of zero. Its absolute confirmation wins remain **unmeasured/null**, not fabricated zeros or imported historical rates.
5. For every differing restart, evaluate both selected recipes on its fixed 1,000 fresh paired values. The new output must be the designated primary and the saved selection rows must show a positive top tie; any other selector difference is an integrity failure. Run every differing restart, including unfavorable outcomes. Do not add unselected nominees or extra reference controls: the endpoint is the contrast between the two selected outputs.

Let `K` be the number of differing restarts, determined solely by search observations and frozen decisions. Both physical recipes need measurement on each such panel. Sharing exact outputs avoids confirmation fights only for converged restarts; it does not increase any other panel, add roots or recycle values. No cross-restart deduplication is allowed because panels differ. Supplied-team adoption and absolute win-rate questions remain outside this comparison.

## Allocation and stopping

One cryptographic draw contains **32,768 little-endian 32-bit words**, interpreted as signed combat/construction values using the existing convention. Classify against the complete historical union and retain the entire batch durably. Skip prior values and repeated fresh words when forming the distinct fresh sequence. A durable Pending reservation precedes the draw; all fresh words, including the unused tail, become permanent exclusions before execution.

The fixed assignment consumes **24,984 fresh distinct values**:

- First, 24 search blocks of 41 values: one construction root, eight discovery values, then 32 selection values. These 984 values precede every confirmation assignment.
- Next, 24 blocks of 1,000 confirmation values, in restart order. Each block belongs to its declared restart even if the two outputs later converge.
- Every remaining fresh value is an unused permanent exclusion. Confirmation values assigned to identical outputs are also unused permanent exclusions.

There is one batch, no refill, no resume, no retry, no sample extension and no replacement root. If the batch cannot supply 24,984 fresh values, retain its reservation and close without a result. Preflight requires `historicalCount + 32,768 <= 1,000,000`, preserving the existing history capacity. The last closed comparison recorded 505,562 exclusions; this is only the precision illustration below. The actual historical union must be inspected and frozen at preflight and used in the bound.

All native attempts are durably journaled and charged before preparation/combat. Cancellation, incomplete search, missing freeze, missing confirmation, audit disagreement or a time/storage overrun produces no positive decision. Preserve partial evidence and unresolved reservations. There is no efficacy/futility peek or transfer of unused allowance from closed experiments. For `K=0`, completing and verifying the common searches closes the run with no confirmation fights.

## Endpoint and decision

For restart `j`, let `d_j` be candidate-selector minus baseline-selector confirmation win rate. For identical outputs, `d_j=0` exactly. For differing outputs, victories count as one, draws and defeats as zero, and the same 1,000 values pair the two recipes. Report their wins, candidate-only gains, baseline-only losses and difference.

The primary estimate is `mean = sum(d_j) / 24`, equivalently total net gained wins divided by **24,000**. That denominator does not change with `K`; the skipped identical-output comparisons are known zeros, not additional measured samples. The mean over differing roots may be reported as a labeled descriptive quantity, never substituted for the primary endpoint.

The useful-effect requirement for this selector-only comparison is **at least one percentage point**, or **240 net gained wins** on that fixed denominator. This is a new prospective design choice: a selector has no additional search-fight cost, so an average extra win per 100 uses is a useful target. The former five-point generator and team-adoption gates remain unchanged. It would be misleading to quietly reuse those closed protocols with a lower threshold.

Return `SupportsIncumbentTieForFrozenOutputs` only if the complete native and independent audits pass, net gained wins are at least 240, the one-sided 95% conditional lower bound below is strictly positive, and **at least three restarts have positive observed differences**. The integer net-win threshold avoids floating-point ambiguity at exactly one point. Do not round values before evaluating the gate. The three-positive condition is a replication safeguard, not a confidence interval over search roots.

For a verified complete run with `K>0` that fails any performance gate, return `DoNotPromoteIncumbentTie`. For verified `K=0`, return `NoSelectorDifferences`: no difference occurred in these searches, not proof of equivalence in future searches. Fewer than three differing restarts cannot pass the replication condition; their declared panels still run if `K>0`. Incomplete and invalid runs report `IncompleteComparison` or `IntegrityFailure` without a performance claim. Even a positive result does not automatically switch the default or adopt a team.

## Conditional precision, including sampling without replacement

The estimand is the mean strength difference of the **24 frozen output pairs** over a uniform fresh 32-bit combat value, conditional on the complete search values/measurements and frozen choices. Fresh here means absent from the pre-run history and the 984 search values. The updated registry used by later experiments is a separate population. This estimand excludes variation over future construction roots. Do not condition the statistical claim on the realized confirmation values, unused entropy tail, or successful completion of confirmation.

Let `H` be the historical union count frozen before the new draw; `M = 2^32 - H - 984` is the fresh population after the search values. Let `B=1000`, `R=24`, `N=R*B=24000`, `n=K*B` and `alpha=.05`. Define

```text
depletion = n(n-1) / (M N)
margin    = sqrt(2 n log(1/alpha)) / N + depletion
lower     = max(-K/R, mean - margin)
```

For `K=0`, `margin=0`, `mean=lower=0`. The lower bound is a single primary contrast; descriptive per-root differences receive no extra significance labels. This directly handles distinct sampling and avoids extrapolating the earlier comparisons' birthday-coupling subtraction to 24,000 values, where that subtraction could exhaust alpha.

Derivation for this protocol: enumerate only the `n` active paired trial slots in fixed restart/trial order, ignoring unobserved inactive assignments. Conditional on search, their assigned values remain a uniform ordered sample without replacement from the `M` values. For slot `i`, its already-fixed output pair defines `f_i(s)` in `[-1,1]`. Write `Y_i=f_i(S_i)` and let `mu_i` be the mean of `f_i` over the initial fresh population. Removing `i-1` previously observed values changes that mean by at most `2(i-1)/M`: the initial mean is the weighted average of removed and remaining means, both in `[-1,1]`. Thus the total possible drift of the conditional means from `sum(mu_i)` is at most `n(n-1)/M`.

The centered increments `Y_i - E[Y_i | earlier active assignments]` have conditional mean zero and conditional range length at most two. Conditional Hoeffding iteration gives an upper-tail probability at most `exp(-t^2/(2n))`. Apply it with `t=sqrt(2n log(1/alpha))`, add the finite-population drift allowance and divide by `N`. The conditional-range inequality is stated in [Kuang Yang's martingale lecture, Theorem 6.2](https://jhc.sjtu.edu.cn/~kuanyang/teaching/CS3341/notes/lec06.pdf); the depletion application above is derived for this experiment. Its assumptions require immutable output pairs and uniform fresh assignments. `K` may be search-dependent because it is fixed before confirmation.

Using the illustrative `H=505562`:

| Differing restarts K | Total fights | Conditional margin | Point estimate must reach | Aggregate power lower bound at a true +2-point mean |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 11,904 | 0 | Cannot pass | Not applicable |
| 3 | 17,904 | 0.559 points | At least 1.000 point | 99.993% |
| 4 | 19,904 | 0.645 points | At least 1.000 point | 99.925% |
| 8 | 27,904 | 0.912 points | At least 1.000 point | 97.266% |
| 12 | 35,904 | 1.117 points | More than 1.117 points | 84.573% |
| 24 | 59,904 | 1.581 points | More than 1.581 points | 18.985% |

The last column bounds only passing the mean and lower-bound gates, conditional on `K` and a stipulated true aggregate mean. It **does not include** the three-positive-restarts condition, successful execution or future-root variation, and is not a forecast from the old outcomes. The calculation applies the lower-tail counterpart with `g=max(.01,margin)` and returns `1-exp(-(N*(theta-g-depletion))^2/(2n))` when `theta>g+depletion`, otherwise zero. The full comparison is not guaranteed to resolve one-point effects; many disagreements make the bound more conservative. No sample extension follows from an inconclusive result.

## Resource envelope

The search charge is fixed at **11,904 fights**. Confirmation costs `2,000*K`, giving a hard ceiling of **59,904 fights**. This is one common search per restart, not two full study runs. Both selector invocations and archive reconstruction add no combat.

The proposed owned run has a **4,500-second /4-GiB** combined envelope, including reservation, execution, native verification and closeout. The native process receives 4,440 seconds /3,968 MiB, leaving 60 seconds /128 MiB for the enclosing owner. One hidden Windows Job owns the process tree; native and enclosing checks enforce their respective caps. This is a new comparison envelope; the older 2,400-second comparison contracts are not expanded or reused.

The earlier anchored run took about 845 seconds and retained about 812 MiB for 16,976 fights. Simple proportional scaling suggests roughly 50 minutes and 2.8 GiB at the new worst-case fight count, leaving headroom under the proposed caps. This is a planning estimate, not a completion guarantee. With four differing restarts the fight count is 19,904, much closer to the old workload. The run must still stop on its fixed deadline; unused time and storage are not transferable. Builds, fixtures, seed-free preparation and the separate saved-row arithmetic audit are engineering work and must be reported separately.

## Implementation and evidence required next

Implement a **new comparison runner and verifier** for this version. The old generator-comparison commands intentionally reject the new selector. Do not weaken their guards or synthesize an apparently complete ordinary study by omitting its required reference controls.

The runner should reuse existing discovery measurement and real selector entry points, but retain one common search archive per restart and a comparison-specific confirmation archive. Save the request/template/runtime pins; complete history inventory and reservation journal; one entropy batch and deterministic mapping; each search's proposals, measurements and shortlist; both selector outputs/reasons; the global 48-output freeze; exact active confirmation family; attempted/completed fight journal; direct outcomes; result; native reconstruction receipt; and final file inventory. An exact output identity supplies a zero difference only; it never supplies an invented absolute win count.

The native verifier must replay saved search inputs and measurements through both selectors, rebuild the active family and all count/identity checks, reconstruct the endpoint, and verify the global barrier without executing combat. A separate saved-row auditor must authenticate its inputs and independently reproduce pair alignment, gains/losses, `K`, all 24 endpoint contributions, unused reservations, finite-population margin and decision. No current archive or historical decision is rewritten.

Required implementation fixtures include: zero/one/many differing roots; exact 240-win boundary; fewer than three positive roots; negative differing-root outcomes; strict leads and zero-win selector equality; same-party convergence; complete physical identity comparison; common measurement hashes; malformed/missing roots; reused or reordered panels; interrupted allocation/search/confirmation; global barrier violation; tampered primary/selector/version; deadline/storage failure; and rejection of unsupported generation/content. Backend fixtures run through `build/run-tests.ps1`. A captured-runtime compatibility check and complete preflight are required before any scientific launch.

## Planning verification and changed files

Added this protocol, its JSON contract and [seed-free arithmetic checker](analysis/practical-incumbent-tie-design.py). Updated the selector implementation's next step and the harness README/practical guide. **Seven arithmetic tests passed**: costs and exact convergence, worst-case precision, integer/replication gates, the fixed endpoint denominator, missing evidence rejection, exhaustive small-population depletion checks and the aggregate power bound. The [generated arithmetic table](../TestResults/incumbent-tie-design-20260922/arithmetic.json) records all calculated values. The source package/template hashes were checked; that is not a full runtime admission or current history scan.

```powershell
& 'C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -B -X utf8 'Balance Harness/analysis/practical-incumbent-tie-design.py' --self-test
& 'C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -B -X utf8 'Balance Harness/analysis/practical-incumbent-tie-design.py' --output TestResults/incumbent-tie-design-20260922/arithmetic.json
```

The output path must be new; later reproductions use a different output file. No backend code changed in this planning step, so the prior **206 passing backend tests** were not rerun. No required planning command was blocked. No migrations, application configuration changes, deployments, real preparation, entropy draws or experimental fights occurred. Next: implement and test this comparison contract, then prepare its concrete captured runtime and launch package.
