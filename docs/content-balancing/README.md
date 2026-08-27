# Content balancing

This folder contains the combat architecture, Character Profile, and recommendation-calibration documentation.

## Recommended reading order

1. [`combat-system-analysis.md`](combat-system-analysis.md) — combat preparation and execution consistency across content types.
2. [`character-profile-generator-design.md`](character-profile-generator-design.md) — simulation-informed Character Profile generation and catalog design.
3. [`world-tower-party-profile-calibration-redesign.md`](world-tower-party-profile-calibration-redesign.md) — implemented five-character role-aware discovery, finalist qualification, and exact 5/10/15-character Tower expeditions.
4. [`combat-calibration-current-state-and-gaps.md`](combat-calibration-current-state-and-gaps.md) — implemented state, current passing evidence, remaining proof, and recommended priorities.
5. [`world-tower-profile-catalog-review.md`](world-tower-profile-catalog-review.md) — historical schema-3 catalog review and lessons that still affect approval.
6. [`world-tower-contract-v3-campaign-review.md`](world-tower-contract-v3-campaign-review.md) — historical contract-3 failures, the floor adjustments they motivated, and the passing generator-13 result.
7. [`one-click-balancing-rerun.md`](one-click-balancing-rerun.md) — current schema-7/generator-13 rerun orchestration, dependency-aware reuse, production qualification, candidate certification, and promotion rules.

The source-controlled approved Character Profile catalog remains at [`combat-character-profiles.json`](../../LL/src/API/API.LL/Data/combat/combat-character-profiles.json).

Current operational status: generator-13 campaign `66368b83-07c1-4a7a-baf6-487c65fc8492` reused all five role-aware discovery audits, generated and validated 13 exact-scenario profile sets, passed production smoke, and passed fixed-seed 100-sample certification with zero issues on 2026-08-27. Every floor has at least one exact-context team at an inclusive 5%–20% estimated win rate, and every canonical cohort confidence gate passes. The campaign is ready for human promotion review; the source-controlled approved catalog remains deliberately unchanged.
