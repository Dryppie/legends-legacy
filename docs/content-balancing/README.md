# Content balancing

This folder contains the combat architecture, Character Profile, and recommendation-calibration documentation.

## Recommended reading order

1. [`combat-system-analysis.md`](combat-system-analysis.md) — combat preparation and execution consistency across content types.
2. [`character-profile-generator-design.md`](character-profile-generator-design.md) — simulation-informed Character Profile generation and catalog design.
3. [`world-tower-party-profile-calibration-redesign.md`](world-tower-party-profile-calibration-redesign.md) — implemented five-character role-aware discovery, finalist qualification, and exact 5/10/15-character Tower expeditions.
4. [`combat-calibration-current-state-and-gaps.md`](combat-calibration-current-state-and-gaps.md) — implemented state, current passing candidate evidence, remaining approval work, and recommended priorities.
5. [`world-tower-profile-catalog-review.md`](world-tower-profile-catalog-review.md) — historical schema-3 catalog review and lessons that still affect approval.
6. [`world-tower-contract-v3-campaign-review.md`](world-tower-contract-v3-campaign-review.md) — historical contract-3 evidence, the corrected contract-4 interpretation, and why generator 13 is not promotable.
7. [`one-click-balancing-rerun.md`](one-click-balancing-rerun.md) — current schema-7/generator-23 rerun orchestration, dependency-aware reuse, floor-specific production qualification, candidate certification, and promotion rules.

The source-controlled approved Character Profile catalog remains at [`combat-character-profiles.json`](../../LL/src/API/API.LL/Data/combat/combat-character-profiles.json).

Current operational status: certification contract 4 requires every selected exact-floor legal team's point estimate to remain strictly below 20%, while at least one selected team per floor must be strictly above 5% and below 20%. Generator 23 creates one scenario per floor, four 100-sample CalibrationTeams, and one No-Essence control. Campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` produced 15 valid sets, passed smoke and 100-sample certification with zero issues, and is promotion-ready. Across all floors, the largest team estimate was 19% and every floor had at least one 6%–19% anchor. The source-controlled approved catalog remains unchanged pending human review and explicit promotion.
