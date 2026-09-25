# Confirmation plan: five challengers against three retained references

23 September 2026. Target: offline `LL/tools/BalanceHarness`. **Frozen protocol; implementation verified, first admission failed before preparation.** Evaluate all five exact challengers from the [saved-stage review](Tower-Practical-Three-Reference-Tie-Stage-Review.md) and all three retained references on **6,500 fresh common values per team: 52,000 fights**. The proposed total study allowance is **7,800 seconds /4 GiB**, including a fully charged **600-second /512-MiB** captured-runtime admission and **7,200 seconds /3.5 GiB** for execution, both audits and publication.

The [frozen JSON](Tower-Practical-Three-Reference-Confirmation-Plan.json) binds all eight seed-free scenarios, fifteen contrasts, family-38 statistics, exact thresholds, sampling, resources and implementation checks. Its SHA-256 is **`c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f`**. The [v1 implementation and fixtures](Tower-Practical-Three-Reference-Confirmation-Implementation.md) are complete. The [failed admission](Tower-Practical-Three-Reference-Confirmation-Admission.md) retains its full charge. The [separate resource amendment](Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.md) proposes an explicit v2 total of 8,400 seconds /4.5 GiB while preserving every scientific field. **Next: implement v2 accounting and its tests before another admission; the original JSON and v1 limits remain unchanged.** This is a design artifact, not a runnable native request. Planning added zero fights, values or native preparations; defaults and all three team recommendations remain unchanged.

The planner's source pins describe the preimplementation snapshot retained in its sealed evidence package. The working controller now intentionally differs. Preserve this JSON and use the saved planning snapshot for historical reproduction; do not rewrite the frozen plan to match implementation source changes.

## Fixed family and complete decision

| Execution order | Role /source | Exact party ID |
| ---: | --- | --- |
| 1 | Candidate /root 8 | `c4ef379d6d1c033f8c68b6487886b2fe8f682111e7288d51dce13f0712dfb408` |
| 2 | Candidate /root 13 | `8a0359aaf54c68112862442a1b0800c3d9449b86cba80210f02806e2161edca2` |
| 3 | Candidate /root 15 | `7135da35cf7c9b72363de257b30e7fa2ee392be0afba97821eea0d04459e7c75` |
| 4 | Candidate /root 21 | `4286b24f34af11ed2be2a1f07367533e14e676f3d64fbf01e50595735195b489` |
| 5 | Candidate /root 22 | `51de5c79dbba9580ba7412a195e3d519dcfbcdfde2ab586e690de0e89cb47b39` |
| 6 | Designated primary | `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b` |
| 7 | Retained reference | `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50` |
| 8 | Retained reference | `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c` |

The five candidates are the complete set of challenger outputs at roots **8, 13, 15, 21 and 22** in the closed `NoSelectorDifferences` comparison. Each led its best reference by one win on 32 selection values, but none has an absolute confirmation result from that comparison. Candidate inclusion uses the entire saved set; no independent result is used to prune or rank it. The three control identities and designated primary remain fixed.

Every scenario preserves the captured floor-5 encounter, ten level-40 characters, five canonically ordered Essences each, tier 1/rank 2/Standard quality, equipment, attribute rolls, identity, party slots, time and preparation. `OwnedCopies=null` remains explicit. The planner checks each party identity against its Essence assignments, all other scenario fields against the captured context, and every Essence against the captured allowed pool. Source physical-party hashes and full seed-free scenario hashes are distinct bindings. Native legality and exact runtime compatibility still require the later admission.

Report all **eight win rates**, **fifteen signed candidate/reference contrasts** and **five qualification flags** after the entire family finishes and both audits agree. Victory is a win; defeat and draw are non-wins. Missing, invalid or faulted reports terminate the attempt as incomplete and are never scored as defeats. Pair outcomes by both value and panel ordinal.

The interval family is **38 quantities =8 recipe win rates +2 gain/loss rates ×15 contrasts**. Use the existing Wilson calculation with `z = Phi^-1(1 − .05/(2×38)) ≈ 3.2125135347` throughout. Nominal family coverage is 95%; the Wilson/Bonferroni construction remains approximate. Five separate smaller-family decisions would change this declared rule.

A candidate qualifies only if its recipe is legal, the full family is verified, and all of these pass:

1. Its adjusted Wilson lower win-rate bound is at least **10%**: **728/6,500 wins** passes this viability gate; 727 fails.
2. Against **each of the three references**, candidate-only wins `G` minus reference-only wins `L` is at least **325**, equivalent to five observed percentage points. A net count of 324 fails.
3. Against each reference, `WilsonLower(G,6500,38) − WilsonUpper(L,6500,38) > 0`. Report the paired interval `[lower(G)−upper(L), upper(G)−lower(L)]`.

At this sample size every feasible pair meeting the 325-net-win threshold also has a positive paired lower bound; the smallest boundary lower is approximately **1.01557 percentage points**. Keep the explicit interval condition and publish its value. A positive bound supports positive improvement, not proof that true improvement exceeds five points.

On success, report **every qualifier in frozen candidate order followed by all three retained references** as recommended options for this fixed captured cohort. Preserve the designated primary. Do not select the largest observed win rate or infer candidate/candidate superiority. If no candidate qualifies, retain all three reference recommendations with `StrengthNotDemonstrated`; this does not establish equivalence. Incomplete evidence produces no new recommendation. Encounter balance remains `NotAssessed`, and search-method reliability remains `Unresolved`.

## Sample size and conditional power

The planning target is at least **80% conditional probability that one specified candidate passes all three reference comparisons and viability**, assuming its true gain is at least **eight percentage points against each reference** and its true win probability is at least **25%**. These are planning alternatives, not estimates for the five candidates. If such a candidate exists, the bound also applies to at least one qualifier; it does not promise all five qualify or that execution completes.

For family-38 critical value `z`, panel size `n`, assumed true gain `delta`, minimum candidate probability `p` and at most one million historical exclusions:

```text
c(n) = max(.05, z × sqrt(n + z²) / n)
v(n) = .10 + z × sqrt(.10 × .90 / n)
IID joint-pass lower = max(0,
  1 − 3 × exp(−n × (delta − c(n))² / 2)
    − exp(−2n × (p − v(n))²))
Finite-population deduction = n × (n−1) / (2 × (2³² − 1,000,000))
Without-replacement lower = max(0, IID lower − deduction)
```

Use failure bound one when an assumed alternative does not exceed its sufficient threshold. Each paired difference lies in `[-1,1]`, and viability in `[0,1]`. Hoeffding bounds plus a union bound cover three comparisons and viability without assuming independence between references or candidates. Apply the finite-population coupling deduction **once to the common panel event**, not separately for each contrast. The sampling model treats post-freeze cryptographic bits as uniform and independent; conditional on a complete accepted batch, the chosen panel is a uniform ordered sample without replacement from eligible Int32 values. No probability is assigned to legal preparation, batch sufficiency, completion or a useful candidate existing.

At **6,500** values, the IID lower bound is **83.9006%**, the finite-population deduction is **0.4919 percentage points**, and the resulting conservative lower bound is **83.4087%**. The smallest integer panel meeting this sufficient bound is **6,067**. Choosing 6,500 leaves modest slack and a simple seven-slice transport schedule; it is not a sample-optimality claim. The earlier study's 5,500-value choice does not meet this new three-reference target.

| Values per team | Conservative joint-pass lower at the stated alternative |
| ---: | ---: |
| 5,500 | 74.3989% |
| 6,000 | 79.4192% |
| 6,067 | 80.0086% |
| 6,100 | 80.2923% |
| 6,500 | 83.4087% |
| 7,000 | 86.5739% |

Maximum family-adjusted recipe-rate half-width is **1.9907 points**, and maximum paired-interval half-width is **3.9815 points**. Actual widths depend on outcomes. The JSON includes effect-size sensitivity; zero lower power bounds are uninformative, not predictions of failure.

## Sampling, execution and audit contract

Freeze recipes, protocol, content, settings, final producing runtime and request before entropy. Refresh complete authoritative history under owned registry/output leases. The verified planning snapshot preserves **633,313 exclusions across 240 files**; admission must rescan it. Old unused panels, abandoned reservations and the closed selector comparison remain excluded.

After native legality admission and durable entropy intent/start, perform **one cryptographic fill of 52,000 bytes**, parsed as **13,000 signed little-endian Int32 words**. Select the first **6,500 eligible distinct values** after rejecting history and within-batch duplicates. Permanently reserve every eligible tail value as well. No construction seed is needed. The maximum resulting union at the recorded history is **646,313**; reject admission history above **987,000** so all possible reservations fit the one-million-value limit.

Persist the full batch and completion before honoring cancellation. Shortfall is terminal with exposures retained; there is no refill, fallback generator, retry, resume, replay, sample extension or replacement batch. An unresolved entropy start blocks later allocation until separately reconciled. Existing output is refusal, and legacy recovery must reject the new protocol.

Preserve the native **1,000-value per-scenario limit**. Each team uses the same consecutive slices **1,000 +1,000 +1,000 +1,000 +1,000 +1,000 +500**, yielding **56 transport bindings**. Execute teams in the fixed table order and slices in panel order. Only `Scenario.Seeds` changes; each original seed-free scenario remains bound. Slices are transport partitions, not statistical looks.

Journal exactly **52,000 started/completed ordinal attempts** and retain every bound direct report; no cached outcome reuse. Reject missing, duplicate, extra, reordered or mismatched seeds, trials, inputs, recipes or reports. Native reconstruction must verify all 56 inputs and saved reports without combat. A separate independent implementation must recount eight ordered rows, all fifteen contrasts, family 38 and every qualification flag. Both audits must agree before recommendation or qualifying exports are published. Recheck history, exact inventories, process cleanup and final accounting before sealing.

## Proposed study resource envelope

| Allocation | Seconds | Storage ceiling |
| --- | ---: | ---: |
| Captured-runtime admission, fully charged | 600 | 512 MiB |
| Native execution maximum | 6,000 | 3,072 MiB |
| Both audits and publication reserve | 1,200 | 512 MiB |
| **Total new study** | **7,800** | **4,096 MiB** |

After the admission charge, the execution owner has **7,200 seconds /3.5 GiB**. Its native work is capped at 6,000 seconds /3 GiB; both audits and publication share the original remaining deadline. They receive no fresh execution allowance. The envelope covers copied runtime/content, preparation, reservation, reports, journals, temporary writes, logs, cleanup, final completion and manifest bytes. Hidden Windows Jobs must own process trees before execution, and completion requires empty trees and held leases.

Sizing uses the [closed 55,672-fight screening owner](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/completion.json), which measured **3,506.516 seconds /1,267,889,961 bytes** including both audits and publication. Scaling by `52000/55672` gives **3,275.234 seconds /1,184,262,789 bytes**. Doubling gives **6,550.468 seconds /2,368,525,578 bytes**, inside the remaining owner envelope. Twice the scaled native time is **5,883.883 seconds**, below 6,000; twice the scaled audit time is **653.420 seconds**, below the 1,200-second reserve.

These are linear sizing illustrations, not bounds or a feasibility claim for the new adapter. Different recipe costs, preparation, history growth and temporary output can exceed them. Implementation must measure relevant fixtures, capture the final runtime and validate the caps before the separate admission. No performance probe was run during planning.

“Cumulative” in this plan means this new study's full admission allowance plus execution envelope. Earlier studies remain separately charged; their unused allowances cannot be transferred or refunded. The accepted engineering treatment preserves the **18,180-second /13,584-MiB** prior ledger separately; complete historical engineering totals remain unknown. Planning, helper tests and publication are separately disclosed engineering work, not a claim of total project cost.

## Implementation handoff and verification

Proposed native version: **`tower-practical-three-reference-confirmation-v1`**. The [existing controller](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs) pins six candidates, two references, twelve contrasts, family 32 and 5,500 samples. Eight total teams alone does not make it compatible with this new five/three family. Add a separate version/profile using existing battle/archive and owned-process primitives, preserving all old commands, hashes and replay semantics.

Required fixtures cover exact roles/order/scenarios, all fifteen signed contrasts, 728/325 boundaries, every-reference qualification, zero/one/multiple qualifiers, incomplete families, seven slices and 56 bindings, entropy interruptions and full-tail retention, paired-report identity, audit disagreement, owned deadlines/storage and old-protocol parity. Run backend fixtures through `build/run-tests.ps1` when implementing the adapter. Then pin producing source/runtime and prepare one separately bounded captured-runtime admission; this plan itself starts neither stage.

The [deterministic planner](analysis/practical-three-reference-confirmation-plan.py) authenticates five sealed input packages, five planning source files and its shared history helper. It validates all eight exact scenarios, fifteen contrasts, resource sums and the full live exclusion inventory, then rechecks source hashes. Arithmetic checks compare the family-38 critical value with `statistics.NormalDist`, independently enumerate **2,983 small multinomial cells**, and verify all **6,501 discordance thresholds** and **6,501 viability counts**, including the minimum sample-size boundary.

**Eight planning tests passed.** They also reject missing/reordered candidates, altered references, scheduled or changed scenarios, noncanonical ability order, out-of-pool Essences and source mismatches. The initial test run exposed the captured allowed-pool schema as objects rather than strings; the validator was corrected to use their `id` fields before freezing the plan. The initial log is retained. [Test source](analysis/test-practical-three-reference-confirmation-plan.py), [test log](../TestResults/three-reference-confirmation-plan-20260923/tests.log), [planning receipt](../TestResults/three-reference-confirmation-plan-20260923/planning.json), [reproduction log](../TestResults/three-reference-confirmation-plan-20260923/reproduction.log) and [publication verification](../TestResults/three-reference-confirmation-plan-20260923/verification.json) record the checks.

```powershell
python -B -X utf8 'Balance Harness/analysis/test-practical-three-reference-confirmation-plan.py'
python -B -X utf8 'Balance Harness/analysis/practical-three-reference-confirmation-plan.py' --write
python -B -X utf8 'Balance Harness/analysis/practical-three-reference-confirmation-plan.py' --check 'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json'
python -B -X utf8 'TestResults/three-reference-confirmation-plan-20260923/verify.py'
```

Commands above record completed work using bundled Python; the one-shot write refuses an existing plan. Planning and complete live-history verification took **79.485 seconds**, measured separately from other engineering work. No gameplay source or runtime changed, so backend tests were not repeated. No required command was blocked. Changed repository files are the JSON/Markdown plan, planner and tests, plus six current documentation pointers. No configuration changes, migrations, database operations, deployments or infrastructure changes occurred.
