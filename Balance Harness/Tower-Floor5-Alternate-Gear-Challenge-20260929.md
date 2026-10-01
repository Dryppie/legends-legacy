# Floor 5: alternate-gear challenge — 29 September 2026

**Subsequent calibration:** the [health/gear refinement](Tower-Floor5-Gear-Refinement-20260929.md) preserves this complete source and closes the initial failure, recovery and +4.5% point without a guardian edit. +3%/+4% exceeded the stability ceiling; +4.5% missed the alternate-gear minimum. Next diagnose the profiles' damage/healing difference. Current exclusions are **910,647**; the calibration recommendation below is historical.

**Completed: `NoEligibleGearConfirmation`.** The unchanged supported search found two new compositions. In the complete **103-recipe / fourteen-composition** screen they won **35/96 and 25/96** on health-and-regeneration, but **54/96 and 41/96** on resistance-and-health. Both desired gear profiles met the minimum screening threshold; the strongest recipe exceeded the **33/96 ceiling**. No confirmation or content change followed. Preserve this expanded family for a separately declared calibration. **13,904 fights**, **365 fresh reservations**, **910,007 total exclusions**; no active study remains.

## Prospective scope

Target: primary LL World Tower and the offline Balance Harness. The [latest floor-10 confirmation](Tower-Floor10-Expanded-Calibration-20260929.md) closes the one-composition gap. Broader equipment coverage remains limited: twelve floors have only one qualifying gear profile. A read-only comparison of existing result files identifies floor 5's **health-and-regeneration** profile as the strongest unconfirmed alternative on a single-profile floor: **32/200 and 28/200**, versus the established **resistance-and-health** profile. These historical observations select a hypothesis; they are not pooled into new evidence and do not establish current-runtime strength.

Keep all guardian settings unchanged. Floor 5 remains **health 3.3102803755 / offense 4.4702934848 / defense 2.65 / resistance 2.65 / penetration 1 / regeneration 1**. Whole-Tower SHA: **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Initial exclusion union: **909,642**. Preserve all other floors, including the preceding floor-10 +1% health change.

Use the complete accepted **89-cell / twelve-composition / seven-gear-profile** source `TestResults/tower-balance-pass-floor5-expanded-calibration-confirmation-study-20260929`, manifest **`58d675f7350e6f56730c70f2807edbc71cd64f0d3b792489a5d393ecb9c0cab9`**. Retain every original scenario and exact nominee. The party budget is **ten level-40 characters, five level-1 unascended/unevolved Essences each, tier-1 Epic/Fine/rank-3 equipment**, baseline rolls and no styles; ownership is hypothetical. Changing the gear specialization is an equal-budget choice, not permission to change rarity, rank, quality, tier, level, Essence count or identity.

The supported `affinity-creation-with-benchmark-validation-v1` algorithm stays unchanged. This scope tests **gear coverage**, not non-poison archetypes. A nearby poison composition on the already favored gear does not satisfy the new objective.

## Frozen protocol

1. Authenticate the source manifest and full archive. Preserve its historical owner snapshot and record the two test-source differences; no historical input is repinned. The archived runtime differs from the current preserved floor-10 build. Require complete target-floor/catalog equality and native combat-setting parity, then prepare all 89 exact recipes on the current runtime with zero fights/seeds. Other-floor changes are refreshed explicitly. Current runtime/source pins and the preceding 91-test / 14-Python-test receipts must match before reuse; these ordinary suites are not described as rerun here.
2. Screen all 89 original recipes on **32 fresh seeds**, **2,848 fights**. Select the three distinct actual compositions at **health-and-regeneration**, ranked by wins, remaining guardian health and cell ID. Freeze them and use the first as benchmark.
3. Run **one supported search at health-and-regeneration**: 528 search fights plus all five exact nominees on **128 fresh seeds**, **1,168 fights**. Retain both finalists and all three exact projected references regardless of internal promotion. Do not rerun the preceding resistance-and-health searches.
4. Retain all 89 originals; add both exact finalists, every finalist across the seven existing gear profiles, and every exact projected reference. Deduplicate only identical whole scenarios. Cap the family at **108 exact recipes** (89 + 2 exact finalists + 14 gear projections + 3 references). Preserve ordered Essence lists and actor/item/Essence identities. Count actual compositions by per-slot Essence sets without rewriting scenarios.
5. Before a complete **96-seed expanded screen**, admit projected time/bytes from the reference screen at **80% of 840 native seconds / 2 GiB**. Screen eligibility requires every recipe at most **33/96 wins**, at least two actual compositions with a recipe at least **22/96**, and at least one such recipe on **each of health-and-regeneration and resistance-and-health**. A composition-only pass is insufficient. If the complete import exceeds 108 cells or the gate fails, close without confirmation or tuning.
6. If eligible, use the same 80% resource margins to admit one independent **184-seed complete-family confirmation**, at most **19,872 fights**. Across the full exact family, require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, at least two actual compositions with lower bounds at least 10%, and qualifying recipes on **both named gear profiles**. These conditions may be satisfied by overlapping composition sets; gear variants never inflate the distinct composition count. No samples are pooled, extended or replaced.
7. If accepted, verify all confirmed inputs and one complete report per exact cell against unchanged current content. Use confirmation seeds only and allocate zero new values. No guardian or other game-data edit occurs, regardless of outcome.

Maximum **34,256 study fights + 108 conditional replays = 34,364 executions**, **549 fresh reservations** (32 reference + 109 search + 128 nominees + 96 screen + 184 confirmation). Each scientific phase remains below **20,000 fights / 840 native seconds / 900 process seconds / 2 GiB**. Conditional parity verification uses the existing **600 native / 660 owner seconds**. No retries, extra search roots, dropped recipes, identity optimization, Essence permutations, algorithm changes, guardian adjustments, migrations, database operations or deployments. All failed and unused prior reservations remain excluded.

Driver: `TestResults/tower-floor5-alternate-gear-driver-20260929.py`. Runtime: `TestResults/tower-floor10-diversity-supported-build-20260929`. Freeze this protocol, the driver, source/runtime checks and gear-specific selection guards before allocation. Evidence in ignored `TestResults` is local and must be preserved separately. No dungeon, supply or acquisition work enters this scope.

## Execution record

The declared scope completed without retries. Its captured `protocol.md`, driver, collector, conditional parity driver and selection-check receipt remain frozen. Source authentication preserved the historical owner and recorded the two expected test-source differences. The current runtime/source verification reused the preceding floor-10 receipts only after all **542 current pins** matched. Native preparation established current target-floor/catalog and combat-setting compatibility without combat or allocation.

| Phase | Exact recipes | Fresh reserved values | Fights | Result |
| --- | ---: | ---: | ---: | --- |
| Current-runtime preparation | 89 | 0 | 0 | Verified |
| Reference screen | 89 | 32 | 2,848 | Three distinct health-and-regeneration references frozen |
| Supported search and exact nominees | 5 nominees | 237 | 1,168 | Two new compositions retained |
| Expanded screen | 103 | 96 | 9,888 | Closed: strongest recipe above selection ceiling |
| **Total** | **103 retained recipes** | **365** | **13,904** | **No confirmation or parity replays** |

Native phases used **354.49 seconds** in total. The expanded screen used **234.57 native seconds**, below its limit. All four native fixtures passed through `build/run-tests.ps1`; every process receipt records exit zero, no timeout and no active descendants. No confirmation values were reserved.

The supported search's internal fresh paired validation recorded **13 gained / 4 lost wins** and returned `ChallengerNeedsConfirmation`. Its separate 128-seed nominee panel measured **50/128 and 32/128** for the new finalists, versus **18/128, 17/128 and 12/128** for the three references. This provisional result did not bypass the complete-family gate.

### Complete expanded screen

The two new compositions are `6b30bf98…` and `bc569faf…`. Results below come exclusively from the same fresh 96-seed expanded screen; do not pool them with the search or reference panel.

| Gear profile | `6b30bf98…` wins / 96 | `bc569faf…` wins / 96 |
| --- | ---: | ---: |
| Resistance and health | **54** | **41** |
| Health and regeneration | **35** | **25** |
| Ability haste | 20 | 23 |
| Armor and health | 11 | 8 |
| Restorer specialization | 6 | 10 |
| Baseline | 2 | 1 |
| Precision | 1 | 0 |

The strongest original resistance-and-health recipes won **31/96 and 27/96**. Four distinct compositions had a recipe at least 22/96; health-and-regeneration, resistance-and-health and ability-haste each had such a recipe. The maximum of **54/96 (56.25%)** fails the frozen ceiling, and the leading health-and-regeneration recipe's **35/96** also exceeds it. These are selection observations, not family-adjusted acceptance intervals or a confirmed true win rate above 50%.

The new recipes change actual Essences at the same progression budget:

- `6b30bf98…` replaces Forest Spirit and Lumo Wisp on party slot 2 with Spider Queen Royal Venom and Viper.
- `bc569faf…` replaces Lumo Wisp and Transparent Slime on party slot 6 with Venomous Spiderling and Viper.

All **89 original scenarios**, both exact finalists, all three exact references and all seven gear projections remain represented. Nineteen provenance entries add fourteen distinct scenarios after whole-scenario deduplication. The read-only recipe review verifies the ten-character level-40, five-Essence, Epic/Fine/rank-3 budget across all 103 recipes and preserves raw Essence order and identities across gear projections. These remain related poison builds; this result expands the search's equipment candidates without establishing non-poison archetypes or ordinary ownership.

### Verification and retained evidence

**13 fresh seed-free selection checks passed**, including rejection of a native pass confined to one gear profile and rejection of treating gear variants as distinct compositions. The **91 passed backend regressions / four opt-in skips** and **14 Python tests** were authenticated and reused from the preceding unchanged-runtime verification; they were **not rerun** in this scope. Collection reconciled all phase manifests, exact nominees, budgets and **688 unchanged input/runtime pins**. No production code, guardian data, configuration or migration changed, and nothing was deployed.

- Complete expanded source: `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`; manifest SHA **`46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`**.
- Reconciled evidence: `TestResults/tower-floor5-alternate-gear-evidence-20260929.json`; SHA **`8c6c3976381d12cfd5d1522f258fffc0f6960494e302db3532f4875691d647ad`**. Its zero confirmed-viability counts mean no confirmation was run, not that the historical accepted family lost its previous result.
- Recipe review: `TestResults/tower-floor5-alternate-gear-recipe-review-20260929.json`; SHA **`4f7ff01f6ef7f95a9beefe00a682cd6b3e67be126b756ca0dab4442dc895726e`**.
- Latest ledger: `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-owner-20260929/seed-ledger.json`; SHA **`f549be36fa395ec270b4476763a07aa59b535244bda038521c2c3c4f015c7952`**. Combine every linked ancestor and subsequent ledger; exclusion union **910,007**.
- Current Tower remains SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Floor-5 health/offense remains **3.3102803755 / 4.4702934848**.

### Next Tower work

Use the complete **103-cell expanded screen** as the retained recipe source for a **separately declared floor-5 calibration**, beginning with a bounded health-only grid at unchanged offense. The objective is to bring the strongest resistance-and-health recipe below the family ceiling while retaining confirmed viability on both resistance-and-health and health-and-regeneration. Whether a scalar adjustment can achieve both is unproven; require fresh whole-family selection and independent confirmation with both gear gates. Do not fall back to the 89-cell family, drop stronger variants, rerun the search, reimport its finalists, extend these panels or weaken the gate.

The earlier 89-cell confirmation remains historical accepted evidence at the current setting. It does not establish balance for the newly expanded 103-cell family, which has not passed acceptance. All-floors minimum-composition summaries must retain that qualification. Broad archetype coverage, other floors' gear concentration, pacing targets and ordinary acquisition remain separate gaps. No next calibration or seeds are allocated by this closed report.
