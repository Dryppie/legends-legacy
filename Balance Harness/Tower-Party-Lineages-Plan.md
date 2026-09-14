# Fixed-budget whole-party lineage comparison

Frozen 13 September 2026 before seed allocation or combat. Target: the offline BalanceHarness optimizer. Keep game content, Kharad, the player/gear/Essence budget and the existing 2/3 reliability requirement fixed. The default search policy does not change.

## Evidence and hypothesis

The [retention comparison](Tower-Loadout-Retention-Review.md) failed 0/3. Its broader module pool supplied different loadouts but all six retention nominees won 0/512. Unchanged v13 produced one supported viable primary at 87/512. The next question is whether keeping alternative whole-party lineages available improves reliability without changing the ranked loadout library.

The [saved-history audit](../TestResults/balance/tower-party-lineages-work-20260913/diagnosis.json) assigns a deterministic closest-parent founder label to nine completed v13 arms from three campaigns. At 220–283 of the 289 examined prefixes per arm, all four leading candidates carry the same label. At the final prefix, the best alternatives from other founders range as low as ranks 55–184. These are descriptive observations: the audit changes no measured outcomes, runs no combat and does not replay hypothetical descendants. Convergence also occurs in successful lineages and is not itself proof of failure.

## One optimizer change

Policy `independent-party-lineages-v16` pairs unchanged `loadout-composition-joint` with `lineage-loadout-composition-joint`, in that order. Both evaluate 384 complete candidates on eight shared discovery seeds, use the same first 96 fresh candidates and v13 random-stream initialization, and preserve the existing operators, fresh frequency four, legal budget, exploration logic, ranking and 128-place ranked loadout library. There is no feedback sampling or control-derived construction.

A completed fresh proposal establishes a founder identified by its proposal ID. For a completed nonfresh proposal, compare its entire ordered party recipe with each contributing parent named in its provenance. Count identical Essence IDs at identical character and Essence slots. Inherit the founder of the parent with the most matching positions; ties use ordinal complete-party ID, then proposal ID. Increment that parent's lineage depth. A contribution from a shared module does not union all founder labels. For composition/recombination, this rule follows the closest contributing complete party; it is a structural ancestry label, not proof of biological or statistical independence. Full contributing ancestry remains recorded alongside the label. Rejected, duplicate and incomplete proposals cannot found or supply a lineage.

Change only the main four-place parent beam. Keep the two highest-ranked complete candidates, even when they share a founder. Fill the other two places with the highest-ranked candidates whose founders are not yet represented. When too few founders exist, fill unused places in the original rank order. Keep the existing uniform beam choice, 75% main/25% exploration choice when exploration exists, and existing exploration/recombination logic. Founder labels are recomputed deterministically from completed same-arm parents and validated during shortlist freezing and saved reconstruction.

Keeping two elite places reserves half the main-beam choices for the two strongest candidates instead of dispersing every place across founders. No lineage is restarted on improvement, and the 384-candidate budget remains intact. This preserves an explicit refinement allocation; it does not promise the same realized chain depth as v13, because parent selection is the experimental change. The old v12 behavior archive and v15 diverse-module pool are not used.

## Independent selection, complete controls and reliability

Complete all three paired restarts. Freeze each arm's original top two and top 32 by discovery rank. Screen all 32 candidates in each of six arms on 64 fresh shared seeds, including zero-win arms. Nominate the two with most screening wins, breaking ties by original rank. Screening does not pool discovery or confirmation outcomes.

Carry all **62** complete recipes from the preceding retention comparison as external controls. Preserve original provenance separately. Fixed historical anchor: `team-1abe76ca1891d97a91d484f0a3662048`. Fixed strongest preceding control: `team-a7e5de669c4a17287d84060e8ab6359b`. Neither controls nor held-out outcomes enter generation or founder selection.

Confirm all original nominees, screened nominees, controls and every discovery/screening recipe observed above 50%. Deduplicate exact ordered complete recipes while preserving every origin. Capacity is **96**, sufficient for 62 controls plus up to 24 distinct nominations and ten additional breach recipes. Preserve any overflow and stop before confirmation; never truncate or expand the run. Every admitted recipe receives all 512 fresh shared confirmation trials.

Keep joint alpha .025 across all confirmation rates and .025 across nine paired differences (eighteen discordance intervals, existing multiplier 36). A candidate primary passes only if its adjusted rate lower bound is ≥10%, its paired difference lower bound against the same-restart screened v13 primary is >0, and its paired difference lower bound against the historical anchor is ≥−10 percentage points. Reliability requires **at least two of three** passing restarts. Strong-control comparisons remain separate. Report ordinary/joint family assessments and observed ceiling breaches separately from optimizer reliability. No automatic promotion follows a Pass.

These approximate Wilson intervals cover this frozen selected family, without lifetime repeated-study, full generated-family, acquisition or global-optimality claims.

## Allocation, execution and preservation

| Phase | Fights |
|---|---:|
| Three paired 384 × 8 searches | 18,432 |
| Six 32 × 64 screens | 12,288 |
| At most 96 × 512 confirmation trials | 49,152 |
| Hard maximum | **79,872** |

Limits: 5,400 execution seconds, 4 GiB campaign bytes, zero retries, no resume, extensions or diagnostic combat replays. Record each start/completion durably. Preserve interrupted evidence without restarting. Preparation and completed reconstruction operate under a no-combat guard.

Master seed **2026091310**, namespace `tower-party-lineages-v1`. Exclude every array in the latest ledger, including unused reservations: **477,847** distinct prior values. Allocate 3 generation, 8 discovery, 64 screening and 512 confirmation values: **587** new reservations, **478,434** total. No feedback seeds. Freeze exact arrays, content, sanitized settings, player budget, mechanics, executable, input provenance and this plan before starting.

Campaign: `TestResults/balance/tower-party-lineages-20260913`. Work: `TestResults/balance/tower-party-lineages-work-20260913`. Preserve every candidate, founder/parent trace, screening row, confirmed recipe and zero-win result. Check implementation with the repository test wrapper; reconstruct v13/v14/v15 histories without new fights; verify the completed campaign with its captured executable; independently check rates, paired differences and lineage/library behavior; then update the active Markdown guides. Historical evidence, frozen plans and completed reviews remain unchanged.
