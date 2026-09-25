# Practical Tower: confirmation plan for six frozen challengers

**Execution follow-up — 22 September 2026:** the [single fixed-family confirmation](Tower-Practical-Fixed-Family-Confirmation-Execution.md) completed all **44,000 fights** and both audits. Exact candidate `96b94357…` alone qualified: **76.15%** wins versus **70.64% /64.16%** for the references, with observed gains of **5.51 /11.98 points** and positive adjusted paired bounds. The study is closed; all **550,367 values** remain excluded. The original plan JSON and design below remain unchanged, and its run allowance cannot be reused.

## Historical prospective design

The following snapshot records the original design and its then-pending implementation. The completed execution above supersedes its next-step notices.

22 September 2026. Target: offline BalanceHarness. **ProtocolSpecifiedImplementationRequired.** Evaluate the six catalogued challengers and both supplied references on one fresh common panel of **5,500 trials per recipe: 44,000 fights**. Report every candidate that meets the frozen practical strength criterion against both references. The proposed additional operational cap is **6,000 seconds /3 GiB**, including setup, both audits and closeout.

The design is complete. **Next: implement and verify a separately versioned eight-recipe adapter**, then freeze its runtime, admission request and cumulative charge ledger. This plan is not a runnable request, resource grant or completed experiment. No combat, entropy draw, value allocation, native preparation or backend build occurred. Current recommendations and **539,367 permanent exclusions** remain unchanged; earlier experiment scopes stay closed.

The [machine-readable plan](Tower-Practical-Fixed-Family-Confirmation-Plan.json) contains all eight exact seed-free scenarios, the twelve contrast identities, numerical gates, captured content/settings, sampling, resources and required implementation checks. The [deterministic planner](analysis/practical-fixed-family-confirmation-plan.py) reproduces it from authenticated saved evidence. The [selection-margin review](Tower-Practical-Selection-Margin-Review.md) explains why these existing candidates need measurement before another search change.

## Frozen scope and reporting

| Execution order | Role /source restart | Exact party ID |
| ---: | --- | --- |
| 1 | Candidate /1 | `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c` |
| 2 | Candidate /11 | `c29b42e31e2142245fd342a76dcb35c13331c2aacdbc1352d2cdc126bcfab178` |
| 3 | Candidate /13 | `efd10263519b51b8fbd2d1e499df442c1e78984ad69e031be423dd479a121c4d` |
| 4 | Candidate /17 | `6434390b22450467cb392f2140bf57022e08cc444283cd5cf6afd5a368066556` |
| 5 | Candidate /22 | `71c87a42db073d29f0def13e448fa3f2798460d70b26d98509a4542e6c03f6c5` |
| 6 | Candidate /23 | `db529a010220a522c01c7e9d639f396fdf872466412561178c0352ba53cb40c2` |
| 7 | Designated incumbent reference | `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b` |
| 8 | Other supplied reference | `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50` |

Use the exact [eight-recipe catalogue](../TestResults/selection-margin-audit-final-20260922/candidate-catalogue.json), with manifest SHA-256 `7884c456b59772676c49008249573fc9191623715869fcc647a3dacd43c7305b`. Preserve the captured floor-5 cohort, ten level-40 characters, five Essence slots each, tier 1/rank 2/Standard quality, fixed equipment, two five-person subgroups, canonical ability order, identities, preparation and time. Preserve `OwnedCopies=null`. This is a captured-gameplay strength study; it does not calibrate the current live checkout or resolve current-family context exceptions.

The six candidates are the complete eligible set from the saved census, ordered by source restart. No outcome-based pruning or choice among them precedes confirmation. The other reference appears once even though it won selection in two historical roots. No search, mutation, recipe editing, ability-order tuning or historical confirmation reuse is included. Candidate selection used historical evidence; the new panel must be independent of that evidence under the stated sampling model.

Publish all eight win rates and all twelve signed candidate/reference contrasts, with qualification flags and control identities. If one or more candidates qualify, recommend **every qualifier in frozen catalogue order** as a stronger option for this fixed cohort, retaining both references as controls. Do not select the largest observed win rate, assign a new primary, or claim a unique best team. Candidate/candidate superiority and causal attribution to individual substitutions are outside this endpoint.

## One complete strength decision

Victory is a win; defeat and draw are non-wins. Missing, invalid or faulted trials end the attempt incomplete and must never be converted into non-wins. Pair outcomes by the same value and panel ordinal.

The simultaneous family has **32 quantities**: eight recipe win rates, plus gain/loss rates for twelve candidate/reference contrasts. Use the existing [Wilson implementation](../LL/tools/BalanceHarness/TowerBalanceEvaluator.cs#L172), with family 32 throughout: `z = Phi^-1(1 − .05/(2*32)) ≈ 3.1628179634`. Nominal family coverage is 95%; the Wilson/Bonferroni construction remains approximate. Six separate family-seven decisions would not preserve this declared family.

Each legal candidate qualifies only after the **entire family** is complete and both audits agree, and only if:

1. Its adjusted Wilson lower win-rate bound is at least **10%**: **621 wins** at `n=5500`, family 32. A count of 620 fails.
2. Against **each** reference, candidate-only wins `G` minus reference-only wins `L` is at least **275**, equivalent to five observed percentage points. A net count of 274 fails.
3. Against **each** reference, `WilsonLower(G,5500,32) − WilsonUpper(L,5500,32) > 0`. Report the paired interval as `[lower(G)−upper(L), upper(G)−lower(L)]`.

At this sample size, every feasible pair meeting the 275-net-gain threshold also has a positive paired lower bound; the smallest boundary lower bound is approximately **+0.73537 percentage points**. Keep the explicit interval condition and report the intervals. Positive lower bounds support positive true improvement, not a claim that true improvement exceeds five points.

There is one final statistical look after **44,000 completed, valid attempts**. No interim success/futility, partial-family qualification, sample extension, retry, resume, replay, replacement batch or automatic follow-on study is included. Resource, ownership, entropy or integrity failure is terminal. A complete result with no qualifier retains the incumbent and both supplied reference recommendations; it does not establish equivalence. Incomplete evidence produces no new recommendation. Balance remains `NotAssessed`; search-method reliability and V19 remain `Unresolved`.

## Sample size, precision and conditional power

The design target is at least **80% conditional probability that one specified candidate passes both reference comparisons and viability**, provided its true gain is at least **eight percentage points against each reference**, and its true win probability is at least **25%**. This implies at least one qualifier with that probability if at least one candidate meets those assumptions. It does **not** promise that all six qualify, that such a candidate exists, or that the run completes. Eight points is a planning alternative, not an effect estimate for these unmeasured candidates.

The calculation follows the [earlier fixed-team plan's sufficient-event approach](Tower-Practical-Fixed-Team-Confirmation-Plan.md), recalculated for family 32 and a conservative history ceiling. For panel size `n`, define:

```text
c(n) = max(.05, z*sqrt(n + z*z)/n)
v(n) = .10 + z*sqrt(.10*.90/n)
IID joint-pass lower = max(0,
  1 − 2*exp(−n*(delta − c(n))^2/2)
    − exp(−2*n*(p − v(n))^2))
```

Use failure bound one whenever the assumed true alternative is not above its sufficient threshold. Each paired difference lies in `[-1,1]`; viability lies in `[0,1]`. The Hoeffding bounds and union bound require no independence between references or between candidates. The sampling assumption supplies independent draws for the IID calculation. For a uniform sample without replacement from `M` eligible signed Int32 values, subtract `n*(n−1)/(2*M)` once for the joint event. This collision-coupling penalty transfers the IID lower bound to the declared finite population.

The plan uses **1,000,000 exclusions** for this conservative power calculation, even though the recorded union is 539,367 and actual admission must leave room for all 11,000 possible reservations. Thus `M >= 4,293,967,296` under the history limit. No estimate of historical seed quality is required. Conditional on a full single-batch panel under uniform independent bits, the ordered accepted values are uniformly sampled without replacement. Operational completion is an explicit condition, not a modeled probability.

At `n=5500`, `c=.05`, `v≈.11279423`, and the IID lower bound is **83.1674%**. The finite-population deduction is about **0.35216 percentage points**, giving **82.8152%**. The smallest integer sample meeting the target under this sufficient bound is **5,152**. Choosing 5,500 provides modest slack and the already established six-slice transport layout. It is not a sample-optimality claim.

| Trials per recipe | Conservative joint-pass lower bound at true +8 pp against both references |
| ---: | ---: |
| 1,000 | 0%: uninformative bound |
| 4,000 | 66.47% |
| 5,000 | 78.63% |
| 5,152 | 80.00% |
| **5,500** | **82.82%** |
| 6,000 | 86.14% |

At 5,500 trials the same lower bound is **33.07%** at true +7 points and **99.44%** at +10 points. Bounds of zero at +5 or +6 points are uninformative, not predicted failure rates. The five-point observed acceptance threshold prevents treating an effect exactly at that boundary as comfortably detectable. Maximum adjusted Wilson half-width is **2.1304 points** for a recipe rate and **4.2609 points** for a paired interval; actual widths depend on outcomes. The paired interval is centered on the difference of Wilson centers, not exactly on the raw observed gain.

## Sampling, execution and audit contract

Freeze the exact recipes, protocol, captured gameplay/content/settings, producing runtime, request, complete history and owners before entropy. Refresh all history under the registry lease; the [latest verified closeout](../TestResults/incumbent-tie-practical-closeout-20260922/closeout.json) records **227 ledgers /539,367 values**, not a substitute for that future scan. Preserve earlier unused exposures, V19's **512 unused values /253 required recipes**, and the authenticated `AbandonedPermanentlyReserved` recovery receipt.

After native legality admission and durable Pending/entropy intent/start, perform **one cryptographic fill of 44,000 bytes**, parsed into **11,000 signed little-endian words**. The panel is the first **5,500 eligible distinct values** after rejecting complete history and within-batch duplicates. Permanently reserve every eligible unused tail value. Maximum union at the recorded history is **550,367**; reject admission history above **989,000** so all possible reservations fit the one-million-value limit. No construction master or new search seed is needed.

Persist the entire batch and entropy completion before honoring cancellation. Shortfall ends incomplete with all exposures retained; there is no refill or fallback generator. A started but unresolved entropy operation blocks future allocation. Old recovery formats must reject this new version; implementation does not imply automatic recovery or resume.

Keep the native **1,000-seed per-scenario limit**. Each of eight recipes uses the same ordered slices **1,000 + 1,000 + 1,000 + 1,000 + 1,000 + 500**, yielding **48 transport bindings**. Execute recipes in the table's fixed order and slices in panel order. Only `Scenario.Seeds` changes; preserve all other fields and separately bind the full panel and seed-free scenario. Slices are transport partitions, not six statistical looks.

The proposed version is **`tower-practical-fixed-family-confirmation-v1`**. Implement a small adapter over existing native battle/archive, owned-process, history and phase primitives. The [old controller](../LL/tools/BalanceHarness/TowerFixedTeamConfirmation.cs#L33) fixes three teams/family seven, and its [archive auditor](../LL/tools/BalanceHarness/TowerFixedTeamConfirmationArchive.cs#L117) assumes three rows. Keep their command and archive semantics intact. This plan's JSON is intentionally not accepted by those commands.

Journal all **44,000** started/completed attempts, with unique ordinals, input/cache/recipe identities and complete bound outcome reports; no cached outcomes. A parent owns the registry/output leases and watched worker throughout. Existing output means refusal. Native reconstruction must verify all 48 inputs and all reports without combat. A separately implemented direct-outcome audit must reconstruct eight ordered rows, all twelve contrasts, family 32 and all qualification flags. Reject missing, duplicate, extra or reordered panel/report data; do not silently zip mismatched rows. Neither audit can call combat or entropy. Publish recommendations and seed-free exports only after agreeing audits, rechecked history, exact inventories and final enclosing receipts.

## Proposed resource envelope and cumulative accounting

The additional **100-minute /3-GiB** proposal covers setup and captured runtime/content copies, admission/reservation, report retention, both audits, temporary writes, publication, outer logs, process cleanup and closeout. Time is enclosing wall time; storage is enclosing high-water usage. Phase caps are nontransferable.

| Allocation | Seconds | Storage ceiling |
| --- | ---: | ---: |
| Admission and reservation | 240 | 256 MiB |
| Combat and reports | 3,600 | 2,304 MiB |
| Native reconstruction, independent audit and publication | 1,800 | 256 MiB |
| Closeout reserve | 60 | 16 MiB |
| Enclosing setup/overhead headroom | 300 | 240 MiB |
| **Additional total** | **6,000** | **3,072 MiB** |

The completed [16,500-fight fixed-team execution](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) measured **806.937 seconds /403,451,440 bytes** including oversight. Its combat phase took **630.108 seconds**, with **347,556,260 bytes** of growth. Scaling that phase by `44000/16500` gives **1,680.289 seconds /926,816,693 bytes**. Hold other recorded bytes fixed at **55,895,180**.

For audit sizing, use the larger of the scaled old fixed-team audit and the latest practical run's two saved-report audits scaled by `44000/3496`: **748.264 seconds**. Use the larger recorded admission/preflight (**56.891 seconds**) and the old enclosing overhead (**54.540 seconds**). The combined illustration is **2,539.984 seconds /982,711,873 bytes**; doubling gives **5,079.968 seconds /1,965,423,747 bytes**, inside the proposed total. Twice each time component also fits its allocation; twice combat storage fits its phase.

These are sizing assumptions, not performance bounds or measured feasibility for the new controller. Report sizes, eight-recipe preparation, history/journal growth, transient writes and cleanup can exceed them. Implementation must record relevant fixture observations, pin the final runtime and address capped failure risk before native admission. No performance probe or gameplay run is included in this design step.

The final request must reconcile unique applicable prior charge receipts and freeze **`CumulativeSeconds = PriorSeconds + 6000`** and **`CumulativeBytes = PriorBytes + 3221225472`**. Closed allowances remain fully charged irrespective of measured use; no refund, transfer, double counting or zero-default prior cost is allowed. The old fixed-team chain's **4,800 seconds /2,304 MiB**, latest practical run's **1,200 seconds /1,024 MiB**, and its separate correction's **180 seconds /16 MiB** are known charges to preserve. These are not the complete intervening ledger: selector comparisons and other applicable preparation/engineering must also be reconciled. A final numeric cumulative cap is a required admission artifact; this non-runnable design does not invent it from partial receipts.

## Verification and implementation handoff

The planner authenticates both small sealed evidence packages and six pinned source/observation files, totaling **22 files**. It checks exact eight-recipe uniqueness/order, source scenarios, the 32-quantity family, 44,000 attempts, 48 bindings, single-batch/history arithmetic and phase totals. It rechecks all consumed hashes before returning.

Arithmetic verification compares family-32 critical values with `statistics.NormalDist`, independent small multinomial sums across **2,983 outcome-count cells**, all **5,501 discordance thresholds**, and all **5,501 viability counts**. It also runs the existing precision calculator's regression checks. The numerical plan is reproduced from authenticated inputs; [verification evidence](../TestResults/fixed-family-plan-verification-20260922.json) records independent boundary/power checks and Markdown/source checks.

```powershell
python -B -X utf8 "Balance Harness/analysis/practical-fixed-family-confirmation-plan.py" --self-test
python -B -X utf8 "Balance Harness/analysis/practical-fixed-family-confirmation-plan.py" --check "Balance Harness/Tower-Practical-Fixed-Family-Confirmation-Plan.json"
```

The next adapter fixtures must cover exact frozen recipes and captured gameplay; zero/one/multiple qualifiers with no new-primary selection; both-reference and integer boundaries; chunks/ordinals/tampering; collisions/unused tail/shortfall and every entropy interruption; incomplete-family refusal; independent/native disagreement; ownership, limits and immutable publication; and old command/archive parity. Run backend fixtures through `build/run-tests.ps1` when implementing that adapter. Freeze a native request and producing runtime only after these pass.

Changes in this step are the plan, JSON and deterministic planner, plus current Markdown pointers. Backend source and scientific archives are unchanged. No backend tests were needed for this design-only change; no required verification command remains blocked. No application configuration changes, migrations or deployment are involved.
