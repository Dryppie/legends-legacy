# Gear-aware progression and benchmark review — 28 September 2026

Follow-up: the [frozen calibration is now complete](Tower-Gear-Offense-Calibration-20260928.md). Offense 4.20 was the sole qualifying diagnostic setting, with the best profile winning 22/32. The review and proposal below retain their original scope; the linked record contains execution and audit evidence.

**Recommendation: retain the supported affinity search, preserve the confirmed equipment profiles, and calibrate one offline floor-13 benchmark before comparing algorithms again.** All six currently screened encounter/budget combinations have a gear profile with 32/32 wins. Floor 13 and floor 15 also have separately confirmed profiles with 512/512 and 256/256 wins. These conditions offer little room to distinguish search methods by clear rate.

This review ran **zero fights and allocated zero seeds**. It produced a source-checked review and a concrete calibration proposal; it did not tune production encounters or establish an informative replacement benchmark yet. Target: the primary game's offline Balance Harness and its documentation.

## What the progression rules establish

The approved [acceptance policy](Tower-Balance-Acceptance-Policy.md#required-behavior) anchors four Essences at floor 1, five around floor 5, six around floor 10 and at least seven at floor 11. It requires a strongest viable performance between 10% and 50% within each **declared intended budget**, with uncertainty and search coverage assessed separately. It does not establish a complete level, rarity, quality and reinforcement schedule for every floor.

- [EssenceSlotProgression](../LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs) unlocks four through ten slots at levels 30, 40, 50, 60, 70, 80 and 90. Every tested cohort is already at its slot count's minimum unlock level. Lowering level while preserving that count would make these parties illegal.
- [EquipmentTierBudgetCurve](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentTierBudgetCurve.cs) permits tier 2 at level **50**. Its expected-tier calculation switches at 51; those are different rules. The level-50/tier-2 floor-10 preset meets the equipment requirement.
- [Tower definitions](../LL/src/API/API.LL/Data/world-tower/tower-floors.json) declare party size, guardian settings and recommended Power Rating, without a per-floor level/gear budget. [WorldTowerService](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs) checks character/account availability and rating availability in `GetJoinEligibilityAsync`; a rating below the recommendation produces a roster warning. It is not a progression budget inferred from Power Rating.
- [Ordinary acquisition](../LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json) supplies rank-0 area equipment and rank-1 dungeon equipment in both configured regions. Fine quality and Uncommon rarity are possible, but this does not establish ownership of a complete set of specific specializations by a given Tower floor.
- The harness's [progression presets](../LL/tools/BalanceHarness/TowerPartyProgression.cs) and [earlier review](Tower-Party-Progression-Review.md) explicitly label gear provisional. `LegalPurpose` in [TowerBossDiscoveryContract](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) enforces Essence-count anchors at floors 1/5/10/11; it does not prove that a declared gear budget represents the best players at that stage.

The remaining gameplay input is an explicit intended budget per floor, including acquisition/ownership assumptions. Current source rules establish legality and prices, not player wealth, farming time or the strongest accessible equipment at each Tower checkpoint. The confirmed wins therefore demonstrate saturation of these test cases, not an unconditional balance failure for an approved player cohort.

## Current evidence, with strong gear retained

All rows use Uncommon gear, baseline rolls, level-1 unascended/unevolved Essences, no styles, hypothetical ownership and the production required party size. The 32-seed screen is descriptive historical evidence, separate from the fresh confirmations.

| Floor | Characters | Essences / level | Tier / rank / quality | Original wins / 32 | Best tested gear wins / 32 |
| --- | ---: | --- | --- | ---: | ---: |
| 3 | 5 | 4 / 30 | 1 / 1 / Standard | 28 | 32 |
| 7 | 5 | 5 / 40 | 1 / 2 / Standard | 32 | 32 |
| 8 | 10 | 4 / 30 | 1 / 1 / Standard | 32 | 32 |
| 10 | 15 | 6 / 50 | 2 / 2 / Standard | 32 | 32 |
| 13 | 10 | 7 / 60 | 2 / 3 / Fine | 8 | 32 |
| 15 | 15 | 10 / 90 | 2 / 4 / Fine | 10 | 32 |

Floor 8's four-slot success remains a potential progression concern given the five-slot anchor around floor 5. Floor 10 already uses the six-slot anchor and clears consistently even on original gear. Neither can be made acceptable by dropping known strong results. Other floors have not received this current gear screen; their suitability remains unknown.

The strongest separately confirmed results are [floor-13 resistance-and-health](Tower-Floor13-Gear-Confirmation-20260928.md), **512/512 versus 194/512** on original gear, and [floor-15 armor-and-health](Tower-Gear-Specialization-Evaluation-20260928.md), **256/256 versus 83/256**. Floor-13 armor-and-health itself won 260/512. Using that weaker armor control as the search benchmark would conceal the known resistance result.

Nhalia's authored `Damage` effects and condition-stack consumption use Magical damage in the captured [ability definitions](../LL/src/API/API.LL/Data/combat/abilities.json). That is consistent with the resistance result. Serath has both Physical and Magical effects; the floor-15 armor result should not be generalized into an all-encounter prescription. These observations are a mechanical interpretation, not a measured decomposition of the gear improvement.

## Equipment costs explain why the population budget is still unresolved

The [upgrade policy](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs) charges the [configured rank prices](../LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json) per occupied equipment slot; two-handed weapons count twice. Each retained character has seven items occupying eight slots.

| Exact party | Items / occupied slots | Parts from rank 1 | Cinders from rank 1 | Parts from rank 0 | Cinders from rank 0 |
| --- | --- | ---: | ---: | ---: | ---: |
| Floor 13, all tier 2 / rank 3 | 70 / 80 | 4,800 | 10,704,000 | 5,600 | 12,488,000 |
| Floor 15, all tier 2 / rank 4 | 105 / 120 | 16,800 | 37,464,000 | 18,000 | 40,140,000 |

These are reinforcement costs across the whole party, assuming every starting item already has the required tier, rarity, quality and specialization. They exclude obtaining those items, Essence ownership/training, and time. They do not establish that either budget is too generous or too restrictive for the server's best players. Lowering rank or quality would change the resource question and could not establish an algorithm improvement.

## One bounded calibration proposal

Use **floor 13**, ten characters, seven Essences each, level 60 and the existing Fine tier-2/rank-3 equipment budget. This has the largest fresh gear confirmation panel and needs no invented level or equipment downgrade. Its floor-specific progression budget remains diagnostic; the seven-slot floor-11 anchor does not independently approve floor 13's gear.

Keep all seven archived gear profiles: original, precision, ability-haste, restorer-specialization, armor-and-health, resistance-and-health, and health-and-regeneration. Preserve the complete party, identities, positions, ordered Essences, training, rolls, settings and preparation state. This covers the measured profiles of one composition; it is not an exhaustive gear or Essence search.

On isolated content copies only, vary floor 13's `guardianScaling.offense`:

| Multiplier of current offense | Authored offense value |
| ---: | ---: |
| 1.00 | 3.36 |
| 1.25 | 4.20 |
| 1.50 | 5.04 |
| 2.00 | 6.72 |
| 3.00 | 10.08 |

This is a one-variable diagnostic grid. Health, defenses, penetration, regeneration, abilities and stagger remain fixed. Increasing offense is intended to challenge the demonstrated survival advantage; no monotonic win-rate response is assumed. These values are not proposed production settings.

Use the same **32 historical coverage seeds** in every cell: **7 profiles × 5 settings × 32 = 1,120 fights**, including baseline replays. Retain all 35 results. Select the **lowest multiplier whose best profile has 4–28 wins inclusive**. This descriptive range seeks room to measure improvement; it does not replace the approved 10–50% balance policy. A profile with 29–32 wins disqualifies that setting even if another profile looks informative.

If no setting qualifies, return `NoInformativeBenchmark` and stop. Do not extend the grid, replace seeds, choose a weaker profile or automatically start another algorithm experiment. Before execution, admit the matrix through the existing bounded owner, capture separate content identities, prepare all cells, reconstruct original baseline inputs, and enforce 1,120 attempts with zero retries, a 900-second deadline and 1 GiB output limit.

After a qualifying screen, rebind the supported search's three references and inventory to that content with explicit gear. Include any stronger known controls; if they reveal saturation, the case is unsuitable. A later search still needs a separately frozen fresh schedule and independent validation. Confirmation on the original encounter does not transfer to an altered encounter.

## Saved artifacts and verification

The local [review packet](../TestResults/tower-gear-benchmark-review-20260928/review.json) records calculations, source hashes and six-case results. [floor13-controls.json](../TestResults/tower-gear-benchmark-review-20260928/floor13-controls.json) exports all seven exact seed-free scenarios. [calibration-proposal.json](../TestResults/tower-gear-benchmark-review-20260928/calibration-proposal.json) fixes the grid, historical seed panel, controls, limits and stop rule. Its status is `ProposedNotAdmittedOrExecuted`: it is a review handoff, not a public harness launch format or a completed preparation/experiment.

The local analysis authenticated **nine consumed archive members** against the three previously retained manifest pins, checked all current captured content hashes, and checked the five captured combat assemblies. It matched the three confirmed floor-13 scenarios exactly to their coverage exports after removing only seeds, checked party sizes and minimum levels, and recalculated reinforcement costs. This reuses the previous full native/independent battle audits; it does not claim to have rerun them.

Packet manifest SHA-256: `36781cd2034011bd3dd78f530c541e902bcdd8809e74f1bddc7c904296703abe`.

Changed repository files: this review and `LL/tools/BalanceHarness/AFFINITY-SEARCH.md`. Local calculation code and the review packet are retained under ignored `TestResults/`. Verification: the bundled Python ran `TestResults/tower-gear-benchmark-review-20260928.py`; packet readback, file/link checks and `git diff --check` passed. No backend build or tests were needed because application and harness code are unchanged. No verification command remains blocked. Calibration combat has not run.

No migrations, production configuration changes, deployments or shared-database operations. Existing working-tree changes, including the attribute tooltip edits, are preserved. The latest scientific seed exclusion total remains **834,058**; this review did not allocate any values.
