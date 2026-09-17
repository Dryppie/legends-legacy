# Practical Tower fixed-team confirmation plan

**Current status — 17 September 2026:** the [completed fixed-team confirmation](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) has completed all 16,500 trials and both audits, returning StrongerFixedTeamConfirmed /AdoptFixedTeam. The exact candidate is recommended and anchors remain controls. The scope is closed. The planning narrative below retains its original pre-implementation perspective; its scientific protocol and JSON are unchanged.

17 September 2026. Target: the offline BalanceHarness. **The prospective plan fixes candidate `399bc776…` and both original anchors at 5,500 paired trials each: 16,500 fights, with one strength decision after completion.** It retains the existing practical numerical rule. A conditional joint-power bound supports this sample at a true gain of at least eight percentage points against each anchor. The proposed additional operational envelope is **2,400 seconds /1 GiB**.

**Planning is complete; the fixed-team controller and its verification remain to be implemented.** This document is not a runnable request or a gameplay allowance. No fight, seed allocation, entropy draw, build, backend test or native reconstruction occurred. Both anchor recommendations and adoption **Hold** remain unchanged; the full **486,374-value** exclusion union is preserved.

The [machine-readable plan](Tower-Practical-Fixed-Team-Confirmation-Plan.json) contains all three exact seed-free scenarios, equipment, character identities, copy requirements, source freeze hashes, content/settings identities, gates, sampling, resource assumptions and implementation checks. The [read-only planning script](analysis/practical-fixed-team-confirmation-plan.py) reproduces it from authenticated evidence. The [adoption-readiness review](Tower-Practical-Primary-Adoption-Readiness.md) explains why the favorable diagnostic is not itself an adoption endpoint.

## Fixed comparison and decision

| Role and execution order | Exact party ID |
| --- | --- |
| Candidate | `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b` |
| Anchor 040e | `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50` |
| Anchor 49f6 | `6825709c4bcd84b0c72c60c552530007bed67128eb518c7c721d7a0188e0172b` |

Use the same declared floor-5 Kharad cohort: ten level-40 characters, five level-1 unascended/unevolved Essences each, two five-character subgroups, tier 1/rank 2, Standard quality, fixed equipment, neutral identities, no styles or contributions, and canonical Essence order. Preserve `OwnedCopies=null`; real-account ownership is not an additional admission requirement for this cohort. The exact candidate differs from 040e only by its two slot-6 replacements; do not edit any recipe after this plan or choose another candidate from the old diagnostic.

Freeze the three recipes and complete protocol before any fresh entropy is available. The outcome is win/non-win, with draws as non-wins. All three teams use the same ordered **5,500-value** panel. There are **zero discovery and selection fights** and no candidate generation.

Apply the [existing practical strength criterion](../LL/tools/BalanceHarness/TowerPracticalSearch.cs), prospectively specified here for a fixed candidate:

* The candidate is legal, distinct from both anchors and supported viable: its adjusted Wilson lower win-rate bound is at least **10%**. At this sample and family, that requires at least **610 wins**.
* Against **each** anchor, define `G` as candidate-only wins and `L` as anchor-only wins on matching panel positions. Require **`G − L >= 275`**, equivalent to at least five observed percentage points, and **`WilsonLower(G,5500,7) − WilsonUpper(L,5500,7) > 0`**.
* The unchanged interval family is **seven quantities**: three team win rates and four gain/loss rates. Use the existing approximate Wilson/Bonferroni critical value `Phi^-1(1 − .05/(2*7))`. Report every rate, both signed comparisons and their bounds. A positive lower bound supports a positive true gain; it does not prove the true gain exceeds five points.
* Only complete, valid evidence with agreeing audits can recommend this candidate as a stronger option for this fixed cohort. Keep both original anchors as historical controls. A completed negative result retains Hold and anchor recommendations and closes the scope; incomplete/invalid evidence cannot pass. Do not reinterpret negative evidence as equivalence.

There is one planned statistical look, after all **16,500** attempted fights complete. No outcome-dependent stopping, interim success/futility, recipe replacement, sample extension, retry, resume, replay or automatic follow-up is included. Resource/integrity/entropy failures are terminal. Search-method reliability, independent discovery and encounter balance are separate questions. This plan neither reopens the larger three-restart proposal nor resolves V19's **253 required recipes /512 unused values /Unresolved** obligations.

## Why 5,500 trials per team

The design target is an **80% conditional probability of satisfying both anchor comparisons and candidate viability** when the true candidate advantage is at least **eight percentage points against each anchor** and its true win probability is at least **25%**. Eight points is a planning alternative near the earlier observed gains, not a known effect or a forecast; those earlier intervals permit smaller true effects. The five-point observed acceptance threshold is unchanged.

The calculation makes no independence assumption between the two anchor comparisons. Let `z` be the family-seven critical value, `n` the panel size, and define sufficient thresholds:

`c(n) = max(.05, z*sqrt(n + z*z)/n)`

`v(n) = .10 + z*sqrt(.10*.90/n)`

For one contrast, an observed gain strictly above `c(n)` suffices for its paired Wilson gate. A candidate observed win rate at least `v(n)` suffices for supported viability. With true gains at least `delta` and candidate rate at least `p`, Hoeffding bounds plus a union bound give the conservative IID lower bound:

`max(0, 1 − 2*exp(−n*(delta−c(n))^2/2) − exp(−2*n*(p−v(n))^2))`

Use a failure bound of one whenever the corresponding true alternative is not above its sufficient threshold. For uniform sampling without replacement from the currently eligible **4,294,480,922** signed 32-bit values, subtract the collision-coupling bound `n*(n−1)/(2*M)` once for the joint event. The calculation assumes the declared sampling model and operational completion, including a complete panel; it assigns no probability to successful execution.

At `n=5500`, the IID lower bound is **83.1674%**. Subtracting **0.3521 percentage points** gives **82.8153%**. The smallest integer sample meeting 80% under this sufficient bound at the current history is **5,152**; 5,500 provides modest slack and a simple fixed batch layout. This is not a globally sample-optimal design.

| Trials per team | Joint-pass lower bound at true +8 pp against both anchors |
| --- | ---: |
| 1,000 | 0%: this bound is uninformative |
| 4,000 | 66.75% |
| 5,000 | 78.63% |
| **5,500** | **82.82%** |
| 6,000 | 86.14% |

The old 1,000-trial size is not automatically adequate. In the permitted IID population with discordance one and a true eight-point gain, the **single necessary contrast** passes with probability **43.73%**; the joint decision cannot have 80% power there. This counterexample is not a claim that the observed team's discordance is one. Its finite-population coupling correction at 1,000 is only about 0.0116 percentage points.

At 5,500 trials, the same conservative joint bound is **33.07%** at a true seven-point gain, **82.82%** at eight points and **99.44%** at ten points. Bounds of zero at five/six points are uninformative, not predictions of zero success. At a true five-point gain the observed-five-point threshold itself limits power; increasing sample size cannot make that boundary behave like a comfortably larger effect. The nominal interval coverage remains approximate. This design does not assume the prior point estimates will repeat.

## Sampling and exclusions

Use one cryptographic fill of **44,000 bytes**, parsed as **11,000 signed little-endian 32-bit words**. After the frozen recipe/request/history identities and durable owned Pending/entropy-intent/start records exist, draw once. Persist the complete bytes and completion receipt before observing cancellation. Exclude all historical values and duplicates within the batch. The panel is the first **5,500 eligible distinct values**; every eligible unused tail value is also permanently reserved. Conditional on the stated uniform-bit model and obtaining a full panel, its ordered values are a uniform sample without replacement from the eligible population.

The maximum new reservation is **11,000 values**, giving at most **497,374** total exclusions at today's history. Actual collisions can reduce that number. No construction seed/master is required. Do not reuse the old diagnostic panel, its **1,048 unused exposed tail values**, or any earlier value. Refresh the complete history under the registry lease at actual admission; the 486,374 total is a planning snapshot, not a replacement for that scan. Recheck the power correction if eligible population size changes materially, without deriving candidate panels during planning.

If the single batch yields fewer than 5,500 eligible values, retain all exposed values and stop incomplete: no refill or replacement batch. An interruption after entropy starts but before durable completion may leave blocking Pending history. Existing practical/diagnostic recovery formats must reject this distinct protocol; no automatic recovery or resume is claimed. The new controller must preserve all evidence and ownership information for any later separately scoped recovery review.

## Execution path and implementation boundary

Source inspection found an existing fixed-family runner, [TowerCompactBalanceRun](../LL/tools/BalanceHarness/TowerCompactBalanceRun.cs), but its output is the **10–50% balance endpoint**, not this strength decision. [TowerBalanceRuns](../LL/tools/BalanceHarness/TowerBalanceRuns.cs) adapts verified archives to that balance evaluator. Ordinary practical search reruns discovery, and the selection diagnostic fixes four nominees, 1,000 trials and Hold. None is an existing integrated command for this proposed fixed-team strength/sampling contract.

Implement a small, separately versioned `tower-practical-fixed-team-confirmation-v1` controller using [TowerLoadoutArchive](../LL/tools/BalanceHarness/TowerLoadoutArchive.cs) gzip reports, [TowerBattleRunner](../LL/tools/BalanceHarness/TowerBattleRunner.cs) native preparation, and the established owned-process, exact-inventory, history, entropy and phase controls. Reuse shared primitives without relaxing existing contracts or introducing an optimizer. The exact request/command schema and producing runtime must be frozen after implementation; the JSON here is explicitly **not** accepted by any current native command.

**Native scenario partition:** `TowerBattleRunner.CreateInput` accepts at most **1,000 seeds per scenario**. Do not raise that limit. Partition the one frozen panel into **1,000 + 1,000 + 1,000 + 1,000 + 1,000 + 500** consecutive values for each team. Run the candidate, then 040e, then 49f6, each in this same six-slice order: **18 transport recipe bindings** total. Only `Scenario.Seeds` varies; preserve every other field, including scenario/build IDs, character identities and start time. The complete panel and seed-free recipe hashes remain bound separately. These are execution batches, not adaptive sampling stages or six statistical looks. Reassemble all slices into the original three 5,500-trial cells before applying the family-seven decision.

Required durable sequence: freeze inputs and owners; perform native legality/content/history admission; reserve the single panel; execute and journal every attempt; seal reports; reconstruct all chunk recipes, native prepared inputs and outcome bindings without combat; run a separate direct-outcome count audit; recheck history; publish only agreeing results and seed-free exports; seal exact inventories and record final time/bytes. Both audit paths must reject combat or entropy callbacks. One parent owns the registry/output leases and watched worker for the whole operation. Existing output means refusal, not overwrite or resume.

Minimum fixture checks cover the exact three parties and unchanged cohort; unknown/altered schemas; all 18 chunk bindings and the 1,000-seed limit; no cache reuse; 16,500 matched attempts; missing/reordered/extra reports; both contrast orientations and the exact **275** net-gain /**610** viability boundaries; native/independent disagreement; entropy collision/duplicate/shortfall/tail behavior; every Pending/publication interruption; process death and phase/cumulative limits; and old practical/diagnostic parity. Use `build/run-tests.ps1` with literal reports and no gameplay experiment. Capturing fixture resource observations is useful; it does not establish full native combat feasibility.

## Proposed operational envelope

Propose **40 minutes /1 GiB of fresh additional operational allowance**, including request admission, setup, copied runtime/content, registration, attempts/reports, both audits, publication, outer logs/oversight and closeout. It is a cost cap, not a completion guarantee or an allowance granted by this planning step.

| Nontransferable phase | Seconds | Output growth ceiling |
| --- | ---: | ---: |
| Admission and reservation | 180 | 128 MiB |
| Combat and report retention | 1,440 | 768 MiB |
| Native reconstruction, independent audit and publication | 600 | 64 MiB |
| **Phase sum** | **2,220** | **960 MiB** |
| Closeout reserve | 2 | 4 MiB |
| Other enclosing setup/overhead headroom | 178 | 60 MiB |
| **Additional total** | **2,400** | **1,024 MiB** |

The completed diagnostic measured **248.562 seconds /159,764,592 bytes** for 4,640 fights including enclosing oversight. Its 4,000-fight confirmation phase took **159.863 seconds**; its audit phase took **34.152 seconds**. It retained **92,503,745 battle bytes**, with a largest compressed report of **22,145 bytes**, and **67,260,847 other bytes** including oversight.

A deliberately labeled sizing illustration holds admission/other overhead fixed, scales confirmation by `16500/4000` and audit by `16500/4640`, and removes the search phase. It gives **813.556 seconds**. Holding other bytes fixed and charging every future fight at the observed largest report gives **432,653,347 bytes (412.61 MiB)**. Doubling these illustrations gives **1,627.112 seconds /825.22 MiB**, within the proposed total; twice each time component and twice the battle-byte proxy also fit their relevant phase caps.

These are **stress assumptions, not measured upper bounds**. The new controller/runtime, larger full-panel journal, 18 chunk bindings, metadata, transient writes and failure cleanup remain unmeasured. Chunking keeps individual input seed lists at or below the measured 1,000-value size. Before gameplay admission, verify the implementation and producing identity, retain compatible resource observations, and explicitly address capped failure risk; do not claim that the linear model proves completion or silently enlarge a phase after failure. No extra performance probe or public verifier is authorized by this plan.

The closed diagnostic/probe/preparation chain remains fully charged at **1,860 seconds /1,088 MiB**, despite lower actual consumption. If this future additional allowance is granted with no other intervening charges, illustrative cumulative limits would be **4,260 seconds /2,112 MiB**, with those prior charges carried forward. Any applicable intervening preparation/engineering/probe charges must be recorded at admission; they are not assumed zero, refunded or hidden inside phase headroom. Older studies and V19 remain separate closed obligations.

## Next step and verification

**Subsequent implementation — 17 September 2026:** the [fixed-team controller and fixtures](Tower-Practical-Fixed-Team-Confirmation-Implementation-Review.md) now implement this plan. Next is its separately frozen native request/runtime and final admission accounting. This update does not change the plan JSON, sampling design, proposal or gameplay authorization. The original planning closeout below records its then-current source pins and verification.

The next concrete work is **implementing and verifying the fixed-team controller**, including the six-slice native adapter and one-batch reservation boundary. Freeze the resulting request/runtime and final resource accounting afterward, before any gameplay decision. Another optimizer or more redesign of the conditional power calculation is not needed to make this plan executable.

```powershell
python -B -X utf8 "Balance Harness/analysis/practical-fixed-team-confirmation-plan.py"
```

The bundled Python interpreter was used. Checks authenticate the earlier readiness review and sealed evidence; pin inspected source; reproduce the full plan byte-for-byte; compare critical values to `statistics.NormalDist`; compare recurrence probabilities with independent small-case multinomial sums; check **5,225** sufficient-event discordance rows and **5,501** viability counts; and verify exact recipes, batch/attempt/reservation arithmetic and cumulative/phase accounting. An independent closed-form check reproduces the final power bound. All **429 local Markdown links** and whitespace checks passed. Of **5,172 baseline files**, six received the intended Markdown changes and **5,166 remained byte-identical**. Backend tests were not rerun because backend source did not change. No required verification command was blocked.

Added this plan, its JSON and deterministic reader; updated the current README, practical guide, assessment/readiness and handoff pointers. Policies, old decisions, producing/gameplay source and sealed archives remain unchanged. No migrations, application configuration changes or deployments are required.
