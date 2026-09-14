# Fixed-budget generation feedback comparison

Frozen design, 2026-09-13. This is an opt-in offline optimizer experiment at the applied Kharad setting. It does not change game content, player budget, production services, default search policy or acceptance rules.

## Evidence and question

The completed [finalist rescreen review](Tower-Finalist-Rescreen-Review.md) found reliability in only one of three restarts. Reassessing finalists improved one selection but did not solve generation. The saved-history diagnosis at `TestResults/balance/tower-generation-feedback-work-20260913/diagnosis.json` checks the source manifest and measures actual parent use alongside later, independent outcomes; it runs no fights and allocates no seeds.

In restart one, original rank two scored 2/8, then 3/64 and 17/512, while receiving 13 parent uses; rank six scored 2/8, then 9/64 and 72/512, with four parent uses. In restart three, candidates at ranks 27 and 28 initially scored 0/8 and later 78/512. These are selected observations, not a representative estimate or a counterfactual replay of a changed search. Restart two had no discovery or rescreen wins. Feedback may improve selection pressure; it cannot guarantee reaching missing competitive regions.

The question is whether reassessing promising candidates *during* generation improves independently confirmed teams at an equal fight budget.

## One optimizer correction

The explicit `independent-generation-feedback-v14` policy pairs unchanged `loadout-composition-joint` with `feedback-loadout-composition-joint`, in that order. Each of three independent generation seeds initializes the same v13 random stream in both methods. Construction, mutations, whole-loadout operators, ownership constraints, parent/exploration sizes, fresh frequency four, loadout library size 128 and original eight-trial fitness stay the same.

The comparator evaluates 384 candidates on eight shared discovery seeds: 3,072 fights. The feedback arm evaluates 320 candidates on those eight seeds and spends 512 fights on feedback: also 3,072. Both construct the same first 96 evaluated candidates before refinement. Keeping 320 candidates retains substantial late search; the saved successes frequently arose after candidate 240.

At exactly 96, 160, 224 and 288 evaluated candidates, freeze the four highest-ranked candidates that have never received feedback. Probe each on the same fixed 32 feedback seeds, disjoint from discovery, final screening, confirmation and historical reservations. Each round freezes all four IDs before observing any result; each complete candidate receives feedback once. Thus every new arm performs sixteen probes.

For later parent selection, exploration ordering and loadout-library ranking, combine that candidate's eight original and 32 feedback observations into a 40-trial score. Win rate, health and survival use trial weights; victory duration uses win weights. Unprobed candidates retain their eight-trial score. Original observations and every separate 32-trial probe remain in the report. Repeated use of the fixed feedback schedule is adaptive training data and carries no held-out inference claim. References and held-out outcomes cannot enter generation.

## Identical final selection and complete confirmation

After both methods finish all three restarts, freeze each arm's top 32 by its final training rank (pooled where feedback exists). Preserve its original top two nominations. Every arm's 32 candidates receive 64 fresh shared screening trials. Rank solely by these 64 win counts, using frozen training rank for ties. Freeze two finalists per arm; rank one is primary. Screening proceeds for all six arms, including zero-win arms. No pooling of training or confirmation outcomes into this selection.

Import all 36 complete recipes from the preceding rescreen campaign's `comparison.json` as external controls. Preserve source provenance separately. The historical anchor remains `team-1abe76ca1891d97a91d484f0a3662048`; additionally report comparison against the strongest preceding control, `team-a7e5de669c4a17287d84060e8ab6359b`. Saved outcomes never supply fitness or new measurements.

The confirmation family contains all twelve original nominations, all twelve screened nominations, all 36 controls and every generated or screened recipe observed above 50% in any original, feedback or screening batch. Deduplicate exact ordered complete recipes while preserving every origin. Capacity is 64 complete recipes. Preserve overflow and stop before confirmation; never truncate or reassign its reserve. Every admitted recipe receives all 512 fresh paired confirmation trials, including zero-win candidates and controls.

## Frozen decision rule

Use joint alpha .025 over the complete confirmation rate family and .025 over nine paired comparisons (18 discordance intervals, existing Wilson implementation with multiplier 36). For each feedback primary, compare with the same-restart **screened v13 primary**, the fixed historical anchor and the strongest preceding control. Report the latter separately; it does not change the established reliability gate.

Reliability requires at least two of three feedback primaries to satisfy all three conditions: confirmation rate lower bound at least 10%; paired improvement lower bound versus screened v13 above zero; paired difference lower bound versus historical anchor at least minus ten percentage points. Individual confirmation observations above 50% remain ceiling breaches. The ordinary and joint family assessments stay separate from optimizer reliability. A passing optimizer result is eligible for later review; nothing is promoted automatically.

These are approximate Wilson intervals for this frozen selected family. There is no lifetime repeated-study coverage, global optimality, acquisition, or full generated-family acceptance claim.

## Allocation, execution and preservation

| Phase | Completed fights when executed |
|---|---:|
| Three paired generation restarts, including feedback | 18,432 |
| Six complete 32-recipe screens at 64 trials | 12,288 |
| Confirmation, at most 64 recipes at 512 trials | 32,768 |
| Hard maximum | 63,488 |

Hard limits are 5,400 execution seconds, 4 GiB retained package bytes, zero combat retries, no resume, extensions or diagnostic combat replays. Execution records every start and completion in a durable journal. Interrupted work remains evidence and cannot restart the same package. Preparation and completed verification run under a no-combat guard.

Use master seed 2026091308 and namespace `tower-generation-feedback-v1`. Exclude the complete latest ledger, including unused reservations: 476,641 distinct values before this study. Allocate 3 generation, 8 discovery, 32 feedback, 64 screening and 512 confirmation seeds: 619 new distinct values, 477,260 total. The master seed is an allocation input, not a combat seed. Freeze exact arrays, source hashes, content, sanitized offline settings, budget, mechanics, executable files and this plan before starting.

Prepared output: `TestResults/balance/tower-generation-feedback-20260913`. Implementation, command logs and independent calculations: `TestResults/balance/tower-generation-feedback-work-20260913`. Save complete seed-free discovery recipes, feedback ancestry, all screening results and every confirmation recipe. Reconstruct the whole completed campaign with its captured executable and zero new fights, independently check the intervals, and update the active Markdown guides with the measured outcome. Historical evidence packages remain unchanged.
