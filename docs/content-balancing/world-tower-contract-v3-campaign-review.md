# World Tower fingerprint-contract-3 campaign review

## Current decision

Generator-13 campaign `66368b83-07c1-4a7a-baf6-487c65fc8492` is **not eligible for promotion**. It passed catalog validation, production smoke, and the obsolete certification-contract-3 interpretation, but that interpretation did not enforce the intended universal 20% cap.

Certification contract 4 is the current rule:

- Every selected exact-context legal team must have an estimated win rate strictly below 20%. One team at exactly 20% or above fails the floor, regardless of family or population weight.
- At least one selected team per floor must have an estimated win rate strictly above 5% and strictly below 20%.
- A team at exactly 5% does not provide the required anchor.
- Wilson intervals, population weights, weighted means, and cross-team spread remain visible diagnostics; the two gates above use point estimates.

Reassessment of the generator-13 report under this corrected rule fails all 15 floors. Generator 23 replaces the grouped, legacy-family portfolio with floor-specific CalibrationTeams and enforces the corrected rule before catalog emission. Campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` is the first complete passing candidate; the approved catalog and Tower recommendations remain unchanged until it is reviewed and explicitly promoted.

The sections below retain the earlier evidence because it explains the certification-contract changes, anchor design, reserve search, and authored floor adjustments. Any previous statement that generator 13 was passing is historical contract-3 evidence only.

## Historical generator-7 decision

Campaign `e6b1d2a5-3f66-4b19-85ff-cde9e53af32e` is the first complete role-aware, production-qualified World Tower campaign under fingerprint contract 3 and historical profile schema/generator 7/7. Later generators supersede its catalog selection behavior with target-aware anchors and bounded reserve search.

The campaign proves that the orchestration, discovery, qualification, catalog generation, smoke, and certification paths execute successfully. Its original certification-contract-1 report failed with 27 issues. Certification contract 2 removed population averages and profile spread as promotion gates. Certification contract 3 historically applied an inclusive any-one-team 5%–20% rule; contract 4 supersedes it with the universal strict-below-20% cap and strict `>5%` anchor described above. Campaign `72d8c717-7b5e-4b02-8dc9-eb59d5785094` re-evaluated the same discovery/catalog evidence under contract 3 and remained **not eligible for promotion** with nineteen issues.

Do not export this historical catalog into the approved source-controlled catalog.

## Historical campaign facts

| Property | Result |
| --- | --- |
| Source checkpoint | `55c07fee51ec2700a30bcf571313f9490c8c03af` (`balancing`) |
| Campaign ID | `e6b1d2a5-3f66-4b19-85ff-cde9e53af32e` |
| Campaign status | `Completed` |
| Discovery reuse | None |
| Catalog reuse | None |
| Discovery audits | 5 of 5 completed |
| Discovery battles | 380,760 |
| Profile schema / generator | 7 / 7 |
| Exact scenario sets | 13 |
| Generated teams | 130 |
| Catalog validation | Pass; zero issues |
| Candidate smoke | Pass |
| Original candidate certification | Contract 1 fail; 27 issues |
| Contract-2 re-evaluation | Fail; 15 issues |
| Contract-3 re-evaluation | Fail; 19 issues |
| Promotion ready | No |

The evidence bundle is stored under:

`%LOCALAPPDATA%\LegendsLegacy\AdminDashboard\combat-audit-campaigns\e6b1d2a53f664b1985ffcde9e53af32e`

## Original contract-1 certification result

| Issue type | Count | Interpretation |
| --- | ---: | --- |
| `ProfileOutcomeSpreadTooWide` | 13 | Material equal-context build-performance divergence |
| `CanonicalConfidenceOutsideTarget` | 10 | Five point estimates outside their target and five otherwise acceptable estimates with inconclusive 100-sample intervals |
| `ProfileConfidenceOutsideTarget` | 4 | Three weighted point estimates outside their target and one otherwise acceptable estimate with an inconclusive interval |

There were no structural coverage, minimum-sample, timeout, stale-content, scenario-matching, or monotonicity failures.

### Profile spread

The 25% equal-context spread gate failed on floors 1–3 and 6–15 except floor 4 and floor 5. Observed weighted-profile spreads were:

| Floor | Spread | Lowest family and win rate | Highest family and win rate |
| ---: | ---: | --- | --- |
| 1 | 83% | Weak-but-Legal, 17% | Equal-Power Adversarial, 100% |
| 2 | 96% | Weak-but-Legal, 4% | Budget, 100% |
| 3 | 100% | Countered, 0% | Meta, 100% |
| 6 | 63% | Weak-but-Legal, 37% | Equal-Power Adversarial, 100% |
| 7 | 100% | Equal-Power Adversarial, 0% | Meta, 100% |
| 8 | 27% | Counter, 73% | Budget, 100% |
| 9 | 100% | Weak-but-Legal, 0% | Budget, 100% |
| 10 | 100% | Counter, 0% | Typical, 100% |
| 11 | 84% | Countered, 16% | Meta, 100% |
| 12 | 100% | Counter, 0% | Mixed Role Specialist, 100% |
| 13 | 98% | Budget, 0% | Meta, 98% |
| 14 | 100% | Counter, 0% | Mixed Meta/Typical, 100% |
| 15 | 98% | Budget, 0% | Mixed Meta/Typical, 98% |

These results remain valuable build-diversity diagnostics, but the clarified recommendation contract does not require teams at equal Power to have similar outcomes.

### Canonical point-estimate failures

The following canonical estimates are outside their target before considering confidence width:

| Floor | Cohort | Estimate | Target |
| ---: | --- | ---: | ---: |
| 11 | Below recommended | 26% | 0%–20% |
| 14 | Below recommended | 27% | 0%–20% |
| 14 | Recommended | 71% | 40%–70% |
| 15 | Below recommended | 22% | 0%–20% |
| 15 | Recommended | 83% | 40%–70% |

Floors 2, 5, 11, 12, and 13 also produced five canonical confidence issues whose point estimates remain inside their intended bands. Those should be revisited with more samples only after the substantive point-estimate and floor-13 profile failures are resolved.

### Weighted-profile point-estimate failures

| Floor | Estimate | Target |
| ---: | ---: | ---: |
| 9 | 78% | 80%–100% |
| 11 | 86% | 40%–70% |
| 15 | 80% | 40%–70% |

Floor 12's weighted estimate is 68%, inside its 40%–70% target, but its 100-sample confidence interval extends slightly above the upper bound.

## Contract-2 re-evaluation

Certification contract 2 requires at least one exact-context team to independently satisfy the floor's Wilson 95% target interval, minimum sample count, timeout limit, and production-runtime contract. Family, population weight, the weighted mean, and cross-team spread do not affect that decision.

Campaign `db118f5e-e968-4113-be25-3f17e6cdc53d` reused the five compatible audits and schema-7 catalog, reran smoke and 100-sample certification, and produced:

- Report schema / certification contract 2 / 2.
- Qualifying teams on every floor from 1 through 11.
- No qualifying team on floors 12, 13, 14, or 15.
- Eleven canonical confidence findings.
- No profile-spread or weighted-population failures.

Floor 12 Meta, floor 14 Budget, and floor 15 Mixed Role Specialist have point estimates inside the 40%–70% target, but their 100-sample Wilson intervals cross a boundary. More samples can resolve those three findings if the estimates remain stable. Floor 13 has no selected team with a point estimate inside the target: its nearest results are Equal-Power Adversarial at 23% and Typical at 81%.

## Contract-3 re-evaluation

Certification contract 3 uses a fixed inclusive 5%–20% estimated-win-rate band for every profile team on every floor. Wilson intervals remain recorded but do not determine whether a profile team is in band. Minimum samples, timeout limits, and production-runtime evidence still apply. Family, population weight, the weighted mean, other teams, and cross-team spread do not affect the any-one-team decision.

Campaign `72d8c717-7b5e-4b02-8dc9-eb59d5785094` reused all five compatible audits and the schema-7 catalog, passed candidate smoke, and produced report schema / certification contract 3 / 3:

| Floor | Qualifying team |
| ---: | --- |
| 3 | Budget, 16% |
| 9 | Counter, 11% |
| 11 | Countered, 16% |
| 12 | Weak-but-Legal, 14% |
| 15 | Equal-Power Adversarial, 6% |

Floors 1, 2, 4–8, 10, 13, and 14 have no team whose estimate is between 5% and 20%. The report therefore contains ten `NoProfileTeamMeetsTarget` findings and nine `CanonicalConfidenceOutsideTarget` findings. Floors 3 and 9 pass both the profile and canonical gates; floors 11, 12, and 15 have a qualifying profile team but remain blocked by canonical evidence.

## Generator-13 historical contract-3 result

Campaign `66368b83-07c1-4a7a-baf6-487c65fc8492` reused the five role-aware audits and completed on 2026-08-27 with profile schema/generator 7/13. It generated 13 valid profile sets and 142 teams, produced zero catalog issues, passed production smoke, and passed fixed-seed 100-sample certification under contract 3. The seed manifest is `world-tower-certification-v1`, shared by anchor confirmation and certification rather than derived from campaign identity. This result is not a pass under contract 4.

The historical candidate includes these authored floor-definition adjustments from the pre-tuning baseline:

| Floor | Adjustment |
| ---: | --- |
| 2 | Guardian offense multiplier `1.44 → 1.38` |
| 4 | Guardian offense multiplier `1.59 → 2.40` |
| 5 | Guardian offense multiplier `1.50 → 1.58` |
| 11 | Guardian offense multiplier `3.50 → 3.72` |
| 12 | Guardian health multiplier `2.80 → 2.65`; offense multiplier `3.20 → 3.40` |
| 13 | Guardian offense multiplier `3.40 → 3.36` |
| 14 | Guardian health multiplier `3.85 → 3.98`; offense multiplier `3.40 → 3.43`; stagger threshold `250 → 300` |
| 15 | Guardian offense multiplier `3.30 → 3.50` |

These values are supported by the campaign evidence below. They are not an instruction to mutate recommendations during a rerun; content changes remain ordinary reviewed source changes and invalidate the relevant campaign fingerprints.

| Floor | Canonical below / recommended / stronger | Example qualifying family | Estimated win rate |
| ---: | --- | --- | ---: |
| 1 | 0% / 100% / 100% | Weak-but-Legal | 5% |
| 2 | 0% / 91% / 100% | Calibration Anchor | 8% |
| 3 | 0% / 100% / 100% | Calibration Anchor | 10% |
| 4 | 0% / 97% / 100% | Calibration Anchor | 6% |
| 5 | 8% / 100% / 100% | Calibration Anchor | 18% |
| 6 | 0% / 100% / 100% | Calibration Anchor | 14% |
| 7 | 0% / 100% / 100% | Calibration Anchor | 18% |
| 8 | 3% / 100% / 100% | Calibration Anchor | 6% |
| 9 | 3% / 99% / 100% | Counter | 19% |
| 10 | 0% / 100% / 100% | Calibration Anchor | 6% |
| 11 | 8% / 55% / 73% | Calibration Anchor | 9% |
| 12 | 12% / 50% / 100% | Budget | 8% |
| 13 | 0% / 51% / 90% | Calibration Anchor | 13% |
| 14 | 12% / 50% / 94% | Calibration Anchor | 19% |
| 15 | 4% / 60% / 70% | Calibration Anchor | 18% |

### Contract-4 reassessment

The archived 100-sample team results show why the contract correction is material:

| Floor | Selected teams | Strict anchors (`>5%`, `<20%`) | Teams at or above 20% | Highest estimate | Highest family |
| ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 11 | 0 | 9 | 100% | Countered |
| 2 | 11 | 1 | 8 | 100% | Budget |
| 3 | 11 | 1 | 8 | 100% | Equal-Power Adversarial |
| 4 | 11 | 2 | 6 | 100% | Meta |
| 5 | 11 | 1 | 9 | 100% | Equal-Power Adversarial |
| 6 | 11 | 2 | 8 | 100% | Equal-Power Adversarial |
| 7 | 11 | 1 | 7 | 100% | Meta |
| 8 | 11 | 1 | 9 | 100% | Budget |
| 9 | 11 | 1 | 7 | 100% | Budget |
| 10 | 11 | 1 | 5 | 100% | Typical |
| 11 | 11 | 3 | 7 | 100% | Mixed Role Specialist |
| 12 | 10 | 1 | 4 | 91% | Meta |
| 13 | 11 | 1 | 4 | 99% | Mixed Meta/Typical |
| 14 | 11 | 1 | 4 | 99% | Mixed Meta/Typical |
| 15 | 11 | 1 | 6 | 95% | Mixed Meta/Typical |

All 15 floors in the generator-13 catalog fail because each has at least one selected team at or above 20%. Floor 1 additionally fails the anchor requirement: its former 5% result lies exactly on the now-exclusive lower boundary. That catalog must not be exported or promoted, and the campaign did not modify the approved catalog or Tower recommendations.

Generator-23 campaign `c27ad5ef-8483-4b30-b413-62e92f0443a1` supplies the superseding evidence: 15 floor-specific sets, 75 teams, zero validation or certification issues, every team below 20%, and at least one strict-band anchor on every floor. Its maximum estimate is 19% on floor 11. It is ready for human catalog review, not automatic promotion.
