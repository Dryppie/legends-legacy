# Deep challenger search against the fixed +10% boss

**Closed preparation failure - 15 September 2026:** the [readiness check failed](Tower-Deep-Challenger-Preparation-Review.md). This package is not ready for binding or combat and must not be retried. The original frozen protocol copy remains unchanged in the sealed evidence package.

Frozen 15 September 2026 before new generation roots or combat schedules are allocated. Target: offline `LL/tools/BalanceHarness`, captured-v19 gameplay, the exact isolated guardian Health/Power +10% content from the completed focused study. No live content, default optimizer or old experiment changes.

## Question and chosen method

Can the existing deep search find competitive or stronger teams when its feedback comes from the +10% boss? Use three independent 1,536-candidate starts, with 16,384 proposals maximum per start. Preserve the existing deep method's 384 initial evaluations, operators, top-four elite, four exploration parents, module library, every-fourth fresh proposal and `coverage-joint-<root>` stream. The new policy only exposes this component separately; it introduces no search heuristic.

The saved v19 comparison supports this choice: the deep primary beat the portfolio primary on two of three roots; all observed discovery ceiling breaches came from deep components. This is method selection evidence, not proof that the method is optimal or that the prior 1/3 portfolio comparison now passes.

Use the two teams whose adjusted viability lower bounds reached 10% in the sealed 989-team study as controls. The 61/256 team is the fixed comparison control; the 59/256 team is the second control. Exact scenarios, recipes, participant hashes and provenance are bound during preparation. Neither control, its recipe, confirmation data nor historical measurements enters fresh generation.

## Fixed workload and inference

| Phase | Fixed allocation | Maximum fights |
| --- | --- | ---: |
| Discovery | 3 starts x 1,536 candidates x 8 trials | 36,864 |
| Screening | 3 complete top-32 lists x 64 trials | 6,144 |
| Confirmation | At most 256 unique required teams x 256 trials | 65,536 |
| Total | Zero retries, resumes or combat diagnostics | 108,544 |

Rank discovery with the unchanged outcome/guardian-health/survival/duration/ID ordering. Screen all 32 teams from each start, including zero-win lists. Nominate the two highest screening win counts, resolving ties by original discovery rank, before reading confirmation outcomes.

The required confirmation family is the union of both controls, the original top two and screened top two from every start, and every generated or screened team with an observed win rate above 50%. Deduplicate exact ordered recipes while preserving all origins. If more than 256 teams are required, preserve all of them and stop before confirmation; never trim to capacity. Preserve all 4,608 evaluated recipes and complete provenance even when not selected.

Confirmation uses only its separate fixed 256-trial schedule. Allocate alpha .025 over every confirmation rate and alpha .025 over all paired differences against the fixed 61/256 control. Each paired difference uses two discordance Wilson bounds; the complete comparison count is family size minus one. Report family ceiling/viability, every supported improvement, and discovery recovery separately. Recovery requires at least two of the three pre-confirmation primaries to have an adjusted rate lower bound >=10% and paired lower difference against that control >=-10 percentage points. An identical control recipe has exact self-difference zero.

Family outcome is Fail if any observed confirmation rate exceeds 50% or every upper bound is below 10%; Pass requires every upper bound <=50% and at least one lower bound >=10%; otherwise Inconclusive. Approximate Wilson coverage is conditional on the frozen selected family and independent confirmation schedule. There is no full generated-family, complete retained-family, optimality, practical-acquisition or lifetime repeated-study guarantee. Historical portfolio reliability remains Fail 1/3; adoption remains Hold.

## Reservation and resource boundaries

The study needs exactly **331 fresh values**: three roots, eight discovery seeds, 64 screening seeds and 256 confirmation seeds. Use the existing stable SHA256/32-bit derivation with policy `independent-deep-challenger-v1`, master 2026091501, stage label and zero-based candidate ordinal. Allow at most 100,000 candidates per stage, preserve every rejected candidate and charge before deriving the next candidate. Register Pending durably before the first derivation; preserve unresolved state on failure. Exclude the complete live reservation registry, expected union **481,891**, including original unused512 and unused32. No earlier reservation is transferred or freed. Expected total after authorization is **482,222** if the registry is unchanged.

The earlier user restriction of zero fresh balance seeds remains a boundary for allocation. Preparation, implementation, tests and saved-evidence replay may proceed without allocation. Obtain the specific 331-value exception only after the executable package and review are concrete. A general implementation or diagnostic command must not allocate these values.

New output is capped at **4 GiB**: at most 1 GiB for producing/setup/control/verification records and 3 GiB for the study. The total charged active workload is at most **10,800 seconds**, including measured preparation, binding, run, reconstruction, independent audit and reporting; exclude waiting for the user's allocation decision, and persist every phase start/end. The native run has a stricter 5,400-second limit; remaining commands must also fit the total. No limit is an estimate or a change to a predecessor cap. Record throughput, phase timings, CPU, allocation, memory, storage operations, attempt counts and final bytes.

Use existing compact batches, durable per-attempt charging, nested owned-storage accounting, cancellation, complete inventory hashes and archive reconstruction. Run once after authorization and binding. Any failed binding, diagnostic, execution or verification closes that scope without retry or resume. Preserve partial files and all seed intents/reservations. Completed work is sealed and never rerun.
