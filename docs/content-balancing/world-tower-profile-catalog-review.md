# Historical World Tower Character Profile catalog review

> This file reviews schema-3 campaign `e283b6e9-0463-497e-8ff3-3536ba1fd1b7`. Its detailed tables remain useful historical evidence, but neither this catalog nor the later pre-role-aware schema-4 catalog is eligible for promotion under profile schema/generator 7 and fingerprint contract 3. See [`combat-calibration-current-state-and-gaps.md`](combat-calibration-current-state-and-gaps.md) for the current operational state.

## Review decision

Campaign `e283b6e9-0463-497e-8ff3-3536ba1fd1b7` was a **structural and statistical pass under its historical schema-3 contract**, but it is **retired and must not be approved for the source-controlled production catalog**.

The candidate is complete, reproducible, diverse, production-preparable, and internally valid under the schema-3 contract. It must not become an approved recommendation input because discovery used generic balanced participant attributes and profile selection did not qualify finalists against the actual World Tower guardians.

The later schema-4 campaign `6339d840-f00a-4630-a869-d5ad862a3bd1` did run candidate smoke and 100-sample certification, but promotion was blocked by 29 confidence and profile-spread findings. Investigation of that report led to canonical-role discovery, exact-floor production qualification, profile schema/generator 7, and fingerprint contract 3.

The immediate recommendation is therefore:

1. Start a fresh fingerprint-contract-3 one-click run across floors 1–15; all five role-aware discovery audits must regenerate.
2. Confirm that every selected non-control party carries complete ten-sample production qualification for its exact floors.
3. Review the automatically persisted smoke and 100-sample certification reports.
4. Resolve actual outcome, spread, timeout, or scenario-selection failures; increase samples when only confidence ambiguity remains.
5. Promote the normalized catalog only after all automated gates pass and human review accepts the evidence.

## Evidence reviewed

| Property | Result |
| --- | --- |
| Campaign status | Completed |
| Campaign schema | 3 |
| Catalog wrapper schema/version | 1 / 1 |
| Profile schema/generator | 6 / 6 |
| Discovery audits | 5 of 5 completed |
| Total simulated battles | 380,760 |
| Discovery party sizes | Five characters only |
| Essence-slot contexts | 5, 6, 7, 8, and 9 |
| Finalists per audit | 24 |
| Direct battles per finalist pair | 102: 34 fights across each of three seeds |
| Generated scenario sets | 13 |
| Generated teams | 130; exactly 10 per scenario |
| Catalog validation issues | 0 |
| Direct matchup-evidence records | 74 |
| Invalid team, party, or slot layouts | 0 |
| Invalid matchup intervals | 0 |
| Maximum finalist seed-score spread | 0.070, below the configured 0.150 limit |
| Maximum selected nearest-Essence overlap observed | 0.449, below the configured 0.800 limit |

The campaign artifact is stored under:

`%LOCALAPPDATA%\LegendsLegacy\AdminDashboard\combat-audit-campaigns\e283b6e90463497e8ff33536ba1fd1b7`

The approved catalog at [`LL/src/API/API.LL/Data/combat/combat-character-profiles.json`](../../LL/src/API/API.LL/Data/combat/combat-character-profiles.json) remains unchanged and empty.

## Scenario-by-scenario review

Every scenario has ten Expanded teams, the exact production roster size, the expected equipment context, and one, two, or three explicit parties of five.

`Typical power` is average displayed power per character. It exactly matches the production-derived scenario average; small differences from an authored floor recommendation are caused by discrete, production-valid equipment construction rather than a mismatched profile scenario.

| Floors | Slots / parties | Equipment | Essences | Typical power | Authored recommendation | Structural result | Balance result |
| --- | ---: | --- | ---: | ---: | ---: | --- | --- |
| 1–2 | 5 / 1 | T1 Common Standard | 5 | 160.8 | 161–163 | Pass | Pending shadow |
| 3 | 5 / 1 | T1 Uncommon Standard | 5 | 165.6 | 165 | Pass | Pending shadow |
| 4 | 5 / 1 | T1 Common Standard | 6 | 170.0 | 171 | Pass | Pending shadow |
| 5 | 10 / 2 | T1 Uncommon Standard | 6 | 175.2 | 173 | Pass | Pending shadow |
| 6 | 5 / 1 | T1 Uncommon Standard | 6 | 175.2 | 175 | Pass | Pending shadow |
| 7 | 5 / 1 | T1 Uncommon Standard | 7 | 183.8 | 185 | Pass | Pending shadow |
| 8–9 | 10 / 2 | T1 Rare Standard | 7 | 189.0 | 188–189 | Pass | Pending shadow |
| 10 | 15 / 3 | T1 Epic Standard | 7 | 194.6 | 196 | Pass | Pending shadow |
| 11 | 10 / 2 | T2 Epic Fine | 7 | 233.0 | 233 | Pass | Pending shadow |
| 12 | 10 / 2 | T2 Unique Fine | 7 | 241.4 | 241 | Pass | Pending shadow |
| 13 | 10 / 2 | T2 Unique Fine | 8 | 250.6 | 251 | Pass | Pending shadow |
| 14 | 10 / 2 | T2 Legendary Fine | 8 | 259.6 | 260 | Pass | Pending shadow |
| 15 | 15 / 3 | T2 Legendary Fine | 9 | 268.8 | 269 | Pass | Pending shadow |

No historical scenario was rejected for missing coverage or malformed preparation. Nevertheless, none can receive current approval because its selected sources lack the version-7 production qualification record.

## Discovery audit quality

Each selected non-synthetic source has 2,346 aggregate round-robin battles. The selected source confidence intervals are approximately 0.02–0.04 wide, comfortably inside the configured 0.25 maximum. Direct matchup intervals contain their observed scores, including exact 1.0 and 0.0 outcomes.

| Essences per character | Finalist aggregate score min / median / max | Directional pairs | Perfect pairs | Maximum seed spread |
| ---: | --- | ---: | ---: | ---: |
| 5 | 0.022 / 0.479 / 0.926 | 235 / 276 | 31 / 276 | 0.054 |
| 6 | 0.123 / 0.480 / 0.986 | 245 / 276 | 42 / 276 | 0.054 |
| 7 | 0.034 / 0.473 / 0.994 | 239 / 276 | 58 / 276 | 0.054 |
| 8 | 0.018 / 0.496 / 0.881 | 230 / 276 | 28 / 276 | 0.063 |
| 9 | 0.106 / 0.472 / 0.962 | 231 / 276 | 19 / 276 | 0.070 |

Within each simulator run, every fight receives a distinct deterministic seed and pairings alternate combat sides. A given finalist pair is also evaluated under three distinct base-seed groups. The perfect results are therefore not repeated executions of one identical seed. They still warrant explicit per-seed matchup provenance because aggregate matchup evidence currently cannot show whether a less extreme relationship reverses under one seed group.

## Family review

| Family | Observed discovery behavior | Review |
| --- | --- | --- |
| Meta | Aggregate scores 0.830–0.962 | Good high-performance coverage, but the label is not always literally the best finalist because constrained families are allocated first. |
| Typical | Aggregate scores 0.420–0.484 | Good median coverage and stable across all five audits. |
| WeakButLegal | Aggregate scores 0.034–0.263 | Good lower-bound stress coverage. Diagnostic weighting remains important. |
| Budget | Aggregate scores 0.288–0.926; Common Essences only | Legality passes. At five and six slots it slightly or materially outperforms Meta, which is a meaningful balance finding rather than a validation error. |
| Counter | Direct score 1.000 in every slot bracket; lower 95% bound 0.964 | Strong hard-counter coverage. Too extreme to treat as representative population behavior without Tower shadow results. |
| Countered | Direct score 0.000 in every slot bracket; upper 95% bound 0.036 | Strong vulnerability coverage. Same population caveat as Counter. |
| EqualPowerAdversarial | Direct scores 0.088–0.216 and inverse scores 0.784–0.912 | Successfully finds similarly ranked sources with materially different direct outcomes. Useful for testing whether Power hides matchup dependence. |
| RoleSpecialist / Mixed.RoleSpecialist | Selected party aggregate scores 0.629–0.756 | Role construction and party boundaries pass. Tower outcomes remain unmeasured. |
| Mixed.MetaTypical | Uses independently evidenced Meta and Typical parties | Composition is honest: no invented expedition-level audit claim. |
| NoEssence | Synthetic, zero source battles, neutral confidence | Correct diagnostic control and excluded from population weighting. |

### Budget versus Meta

The five-slot Budget source scored 0.926 versus Meta at 0.924. The six-slot Budget source scored 0.910 versus Meta at 0.830. This happened because constrained families are selected before the generic score bands; Meta means “best remaining qualified source,” not “best source in the audit.”

That behavior guarantees constrained-family coverage but makes the family name misleading and can distort the intended 25% Meta population bucket. The recommended correction is a small allocation solver:

- Reserve several eligible Common-only sources for Budget during finalist selection.
- Select the strongest overall source as Meta.
- Select the strongest distinct reserved Common source as Budget.
- Allocate Counter, Countered, adversarial, Typical, Weak, and specialist sources while preserving uniqueness and overlap limits.

This preserves Budget coverage without redefining Meta as “best after controls took their choices.” A fallback should explicitly report when the globally strongest source must be reassigned to satisfy an impossible portfolio.

## Essence diversity review

The selected unique source parties do not show problematic single-Essence concentration.

- Highest global placement share: `essence.bark_golem`, 58 of 1,705 placements, or 3.4%.
- Next-highest: `essence.bog_mite` and `essence.blue_slime`, each 2.2%.
- Highest within one slot context: `essence.nightshade_blossom`, 4.1% in the six-slot portfolio.
- Selected nearest-source overlap never exceeds 0.449.
- Budget teams contain only Common Essences as required.

The strong five- and six-slot Budget results therefore do not appear to be caused by one Essence being copied into most selected teams. They are more likely a combination-synergy or rarity-balance question and should be inspected in the Tower shadow results.

## Power Rating review

Within a scenario, Meta, Typical, WeakButLegal, and Budget usually have exactly the same displayed Power because they equip the same role/equipment budget and the same number of Essences. Across all non-NoEssence families, average per-character Power differs by only 0.0–9.1 points within a scenario, while discovery outcomes span from near-zero to near-certain wins.

This is not proof that the Power algorithm is incorrect: discovery scores measure matchup performance and synergy, while Power primarily represents build budget. It is proof that Power alone cannot justify a recommendation. The certification population, family weighting, and reported outcome interval are essential parts of the player-facing meaning.

The catalog is well constructed for exposing this uncertainty because it contains strong, median, weak, hard-counter, specialist, and no-Essence controls at nearly equal Power. It must now be exercised against the real Tower runtime.

## Findings requiring action

### Historical P0 — Candidate shadow execution was pending

[`WorldTowerProfileShadowCalibrationRunner.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/WorldTowerProfileShadowCalibrationRunner.cs) now validates and executes an in-memory candidate, and [`DiagnosticsController.cs`](../../LL/src/API/API.AdminDashboard/Controllers/V1/DiagnosticsController.cs) exposes campaign candidate shadow and certification endpoints. The later schema-4 campaign proved that these stages run and fail closed. Current campaigns additionally qualify finalists before profile selection and:

- Load the campaign catalog by ID.
- Run `ValidateAsync` and refuse invalid candidates.
- Pass the normalized in-memory catalog to the shadow runner.
- Never write the approved source catalog.
- Include candidate/campaign identity and content hash in report provenance.

### P1 — Meta allocation semantics are misleading

Replace greedy constrained-first selection with reserved-source allocation so Meta remains the strongest overall eligible source while Budget and other constrained families retain guaranteed candidates.

### P1 — Direct matchup stability is aggregated across seeds

Persist per-seed matchup results, not only aggregate pair results. Counter, Countered, and EqualPowerAdversarial admission should require:

- Minimum direct samples per required seed.
- The intended direction in every seed.
- A bounded cross-seed direct-score spread.
- Aggregate Wilson confidence in the intended direction.

### P2 — Population weights are policy assumptions

The default shadow policy assigns 25% Meta, 40% Typical, 20% specialist, and 15% resilience. Budget, Weak, Counter, Countered, and adversarial teams share the Resilience bucket. These weights are not inferred from player telemetry and must be labelled assumptions in certification output.

Run sensitivity variants before approval, for example:

- Default population policy.
- Typical-heavy policy.
- Meta-heavy policy.
- Equal-family stress policy.

A recommendation should not be trusted if a modest, plausible weight change moves its confidence interval outside the target band.

## Historical candidate checklist

| Gate | Current status |
| --- | --- |
| Campaign complete and reproducible | Pass |
| Catalog structurally valid | Pass |
| Exact floor scenario coverage | Pass |
| Exact production preparation rebuild | Pass |
| Source sample and confidence safeguards | Pass |
| Seed stability safeguard | Pass for aggregate source score |
| Direct matchup sampling | Pass in aggregate; per-seed detail missing |
| Essence diversity | Pass |
| Human family review | Conditional pass with Meta/Budget finding |
| Candidate Tower shadow smoke | Not run for this historical schema-3 candidate |
| Candidate 100-sample shadow | Not run for this historical schema-3 candidate |
| Source-controlled approval | On hold |
| Production certification | Not run |

## Current next action

Role-aware discovery, production finalist qualification, candidate shadow calibration, and guarded orchestration are implemented. The next operational steps are:

1. Start a fresh fingerprint-contract-3 campaign; the old audits cannot be reused because their participant preparation differs.
2. Review qualified Meta, Typical, Weak, Budget, and adversarial selection on every floor.
3. Review all family outcome spreads, timeouts, confidence intervals, and differences from the canonical recommended cohort.
4. Address the remaining Meta allocation and per-seed direct-matchup provenance concerns if the new evidence shows they materially affect the selected portfolio.
5. Approve and commit only a candidate whose full 100-sample report passes; archive 500–1,000-sample evidence for release.
