# Floor 5: isolated ability damage mix — 29 September 2026

**Subsequent result:** the [0.4 / 4.55 ability refinement](Tower-Floor5-Ability-Refinement-20260929.md) is complete. Both gear minima and two compositions passed, but 49/128 and 46/128 leaders exceeded the 44/128 ceiling. No game change followed. The new report supersedes the next recommendation below; this three-candidate scope remains closed historical evidence.

Target: primary LL World Tower and offline Balance Harness. Continue the [failed scalar midpoint and reassessment](Tower-Floor5-Joint-Midpoint-20260929.md). Add explicit catalog candidates and test the relative pressure from Kharad's magical pulse and physical strike. The supported search, player budget and all known recipes remain unchanged.

**Completed: `NoEligibleAbilitySetting`.** All three candidates failed the declared selection requirements. **39,552 fights / 384 fresh reservations**, no confirmation or application. The implementation is complete; live game content is unchanged. Latest exclusions **911,479**; no active study remains.

## Implementation

The [study owner](analysis/run-tower-balance-pass.py) accepts `--ability-candidate <plan.json>` only for fixed-family screen/confirmation phases from an unchanged completed source. It rejects simultaneous scalar changes, imports, current-content refresh and chained candidate sources before allocation. The new [catalog helper](analysis/tower-ability-candidate.py) restricts plans to one to four named Power-damage coefficients belonging exclusively to the selected guardian profile. Each change declares its original/new value and original/new description; source ability/Tower hashes, finite positive coefficients and bounded ratios are required. Timing, targeting, barrier, Resonance and every other field remain fixed. Text edits preserve unrelated formatting.

The owner copies content into a fresh isolated directory and freezes the plan, source manifest, helper and content pins. Independent verification reverses only the declared fields and compares every other ability field and catalog, including added/removed files. It checks the exact recipe family and native settings against the original source. The existing native content loader and application replay check are reused; no combat implementation changed. Fresh [corruption/isolation tests](analysis/test-tower-ability-candidate.py): **17 passed**. Existing reference/import checks: **14 passed**. Fresh backend entry regression through `build/run-tests.ps1 -NoBuild`: **91 passed / four opt-in skips**, using the authenticated preserved runtime.

## Frozen experiment

1. Authenticate the unchanged 103-recipe source `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`, manifest **`46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`**, and preceding midpoint evidence **`163e6ed4c2fdbd56e4f5746dc388e5f20b6840e20037b0482340ca8d8e6ebbbf`**. Initial exclusions **911,095**. Live Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Record the deliberate owner-source implementation change separately from historical pins; preserve the documented C# LF-to-CRLF exception. Do not repin historical requests or rebuild the combat runtime.
2. Prepare all **103 exact recipes / fourteen actual compositions / seven gear profiles** from current unchanged content with zero fights. Preserve raw Essence order, IDs and positions. Count actual compositions by per-slot Essence sets. Budget remains ten level-40 characters, five level-1 unascended/unevolved Essences, tier-1 Epic/Fine/rank-3 gear, baseline rolls and no styles. Ownership remains hypothetical.
3. Run the three candidates below in order, each on **128 independent fresh seeds / 13,184 fights**. Retain every recipe. Every recipe must have at most **44/128** wins; at least two actual compositions must have a recipe at least **26/128**, including viable recipes on both resistance-and-health and health-and-regeneration gear. Preserve all panels regardless of outcome. No interpolation, additional candidates or repeated panel follows a failure.

| Candidate | Seal pulse Power coefficient | Crushing Verdict Power coefficient |
| --- | ---: | ---: |
| pulse-040-verdict-520 | 0.4 (was 0.5) | 5.2 (was 2.6) |
| pulse-040-verdict-650 | 0.4 | 6.5 |
| pulse-040-verdict-780 | 0.4 | 7.8 |

All three retain live guardian health **3.3102803755**, offense **4.4702934848**, and every other guardian/ability field except the two descriptions needed to match the declared percentages. The [existing sixteen-pair diagnostic](Tower-Floor5-Gear-Diagnostic-Recovery-20260929.md) measured early mean Seal damage of 4,647.50/7,364.31 and Crushing Verdict damage of 931.81/869.50 for resistance/regeneration gear. These values motivate a coarse physical-pressure range alongside a smaller magical pulse. They do not predict candidate win rates or prove linear damage substitution: targeting, mitigation, deaths and subsequent actions can change.

4. Rank eligible settings by highest minimum wins across the two required gear profiles, then most qualifying compositions, highest second-composition wins, strongest rate closest to 30%, lower physical coefficient, and label. If none qualifies, close **`NoEligibleAbilitySetting`**. Do not extend the grid or relax the requirements.
5. Freeze one selected plan and run a separate **184-fresh-seed / 18,952-fight complete-family confirmation**, starting again from the original prepared source and reapplying exactly that plan. Require approximate simultaneous 95% Bonferroni-Wilson upper bounds at most 50% for every recipe, at least two actual compositions with lower bounds at least 10%, and viable recipes on both required gear profiles. The qualifying count range is 33–68/184. Do not pool panels or substitute another candidate if confirmation fails.
6. Only after full acceptance, apply the exact confirmed ability catalog locally. Verify **all 18,952 confirmed inputs and 103 complete replays**, unchanged Tower/other catalogs, exact coefficient/description deltas and the relevant backend regressions through `build/run-tests.ps1`. No deployment, database, configuration or migration action.

Maximum **58,504 study fights + 103 conditional application replays = 58,607 executions**, **568 new reservations**. Each native phase stays within **20,000 fights / 840 seconds / 2 GiB**, with a 900-second owner allowance and 80% projected time/output admission before allocation. Application retains 600/660-second limits. Fresh paths use `floor5-ability-mix`; no retries, archive overwrites, seed reuse or sample extensions. Completed or failed scope artifacts remain immutable. Conditional confirmation/application is skipped when its prerequisites fail.

Before execution, freeze the protocol copy, candidate plans, driver/application sources and implementation/test receipts in the local control archive. This is an offline calibration scope, not a new search policy or an acquisition claim. The 89-cell historical confirmation still cannot establish acceptance for the expanded family.

## Reusing the implementation

The plan format is `tower-ability-coefficients-v1`: `floor`, `sourceAbilitiesSha256`, `sourceTowerSha256`, and `changes`. Each change contains `abilityId`, `effectId`, `from`, `to`, `descriptionFrom`, and `descriptionTo`. Both source hashes bind the unchanged prepared content. Unexpected fields, stale originals, non-damage effects, shared abilities/profiles and duplicate ability changes are rejected. Ratios must be between 0.25 and 4. Description wording remains an explicit human-reviewed declaration; the helper verifies its exact delta rather than interpreting prose.

For a separately declared future scope, use the usual owner command with `--mode screen` or `--mode confirm`, a fresh `--name`, the unchanged prepared `--source`, the preserved `--artifacts`, and `--ability-candidate <plan.json>`. Selection and confirmation each start from that same original source and redeclare the same selected plan. An ability-modified study cannot become an implicit baseline. Current-content preparation and ordinary scalar studies retain their existing behavior. The new mode does not combine ability changes with scalar changes or recipe imports.

Historical study owners remain captured under their original owner directories. This implementation deliberately changes the current Python owner; its new checksum is recorded in the new declaration. It does not change or repin historical requests, source manifests or combat binaries. Backend commands use the preserved runtime through `build/run-tests.ps1 -NoBuild`; current source/runtime compatibility is recorded separately in the declaration.


## Completed results

| Seal / Verdict coefficients | Strongest recipe | Second composition | Qualifying compositions | Best resistance | Best regeneration |
| --- | ---: | ---: | ---: | ---: | ---: |
| 0.4 / 5.2 | 33/128 | 31/128 | 2 | 18/128 | 29/128 |
| 0.4 / 6.5 | 17/128 | 6/128 | 0 | 5/128 | 6/128 |
| 0.4 / 7.8 | 6/128 | 2/128 | 0 | 2/128 | 1/128 |

The strongest-recipe ceiling is 44/128; each qualifying composition and required gear profile needs a recipe with at least 26/128. The first setting meets the ceiling, two-composition minimum and regeneration minimum but fails the resistance minimum. Both leading compositions use armor-and-health. This demonstrates a changed gear preference in the observed panel, not a confirmed balanced setting or broad archetype diversity. These remain the same related poison compositions found earlier. No recipe was dropped to obtain the result.

The two stronger physical-strike settings fail as well. The frozen scope closes without independent confirmation, local application, application replays or post-application regression. All earlier scalar and diagnostic archives remain preserved. These panels use distinct fresh seeds; do not pool their outcomes or treat them as a precise interpolation curve.

## Next recommendation

Keep the ability-specific approach and test **less physical compensation**, rather than another health fraction or a stronger strike. The 0.4/5.2 panel has ceiling headroom and viable armor/regeneration recipes but insufficient resistance coverage. One separate **0.4 Seal / 4.55 Crushing Verdict** candidate is a reasonable next hypothesis: it keeps the smaller magical pulse while reducing the tested physical hit by 12.5%. This value is untested and is not inferred as an optimum or a measured interpolated win rate.

Use a separately frozen 128-seed panel across the entire family, retaining the 44-win ceiling, 26-win minimum, two actual compositions and both required gear profiles. Only a full pass can proceed to fresh independent confirmation and checked local application. Retain the newly observed armor leaders too. No candidate or seeds are allocated for this recommendation. If it fails, review the tradeoff from the saved panels before extending tuning; no automatic follow-on grid belongs to this closed scope.

The supported search and expected progression curve remain unchanged. Ordinary equipment acquisition and broad non-poison viability are still unestablished. This work changes the offline study owner, adds a focused catalog helper and its tests, and updates the report, handoff/status and harness guides. It makes no gameplay, configuration, dependency, migration, database or deployment change.

## Evidence and verification

- Evidence: `TestResults/tower-floor5-ability-mix-evidence-20260929.json`, SHA **`3d054363ed792416a0e8cfe0a1032aa7b3e0a577bfe7c24c9b398834089a93cc`**.
- preparation: `TestResults/tower-balance-pass-floor5-ability-mix-preparation-study-20260929`, manifest **`4691c601951bea57c13f246ada914fe590c6b841923aab0081072c7680048222`**, audit **`28e4b76b4d034c28c4c11c25f8735f6650f6a3808a3bbd79991705cd8515d765`**.
- pulse-040-verdict-520: `TestResults/tower-balance-pass-floor5-ability-mix-pulse-040-verdict-520-study-20260929`, manifest **`7ef3c80d29cd1e56b84036331af2a497bd8c60961b8f6d6b755b148aad3b4c99`**, audit **`9c266ac2e26f2c0cb52b72a1039d6df9ee65d7ea13c10e0bc25fca3e0792d0c9`**.
- pulse-040-verdict-650: `TestResults/tower-balance-pass-floor5-ability-mix-pulse-040-verdict-650-study-20260929`, manifest **`20e2859da4796e034f482d771b1d3bb5f9d48d26718c8d26cdca0a657647d440`**, audit **`a53bf6193df1d116054e7759622bd969ec3a5261d638d42e2c2923b64f37577f`**.
- pulse-040-verdict-780: `TestResults/tower-balance-pass-floor5-ability-mix-pulse-040-verdict-780-study-20260929`, manifest **`575f27f10db11d650eb5d21bcf9694e352d1bce410a0cd305300e0d97ce3e4f3`**, audit **`acaa9346498a226299ddf9ce0bf72e40bb0ba5f729f342ee2905d6511b0f60e6`**.
- Latest ledger: `TestResults/tower-balance-pass-floor5-ability-mix-pulse-040-verdict-780-owner-20260929/seed-ledger.json`, SHA **`33760103294e8c40796ed1c472245fe76a51a9ea0c758c76f8f4ae996ff706a9`**. Exclusions **911,095 -> 911,479**, including all 384 new reservations and all historical unused values.
- Full verification reconstructed every result and both declared ability deltas across **40,100 new archive members**, checked **767 current input/runtime pins**, and verified all native processes exited with zero remaining descendants. Native preparation plus selection took **899.0222642 seconds**.
- Fresh backend entry regression ran through `build/run-tests.ps1 -NoBuild` using the preserved authenticated runtime: **91 passed / four opt-in skips**. The **17 candidate tests**, **14 reference/import tests** and **12 selection/source checks** passed. Each of the four owned native phases passed. These are fresh checks for this scope; the combat binaries were not rebuilt.
- Conditional confirmation/application commands were intentionally skipped after failed selection. No command is blocked. Publication verification checks the updated documents and local links in `TestResults/tower-floor5-ability-mix-publication-check-20260929.json`; `git diff --check` also passes.

Live Tower SHA remains **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**; ability catalog SHA remains **`0e9024dee31e73538839d7538aa23769c14553b665eb7322195c955a6707b311`**. The candidate catalogs and frozen protocol copy belong only to their archived studies.
