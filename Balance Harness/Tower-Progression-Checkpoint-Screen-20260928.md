# Floor-10/11 progression checkpoint screen — 28 September 2026

Subsequent budget change: the user's [repeating ten-floor equipment curve](Tower-Repeating-Equipment-Curve-20260928.md) supersedes the gear assumptions used here. Re-establish the baseline with the prepared new-budget parties before applying the calibration recommendation below. This completed screen retains its original scope and results.

**Recommendation: retain the supported search and calibrate the bosses against the retained strong teams.** The completed 4,256-fight diagnostic found that every tested floor-10 combination cleared 32/32. On floor 11, 80 of 84 seven-Essence combinations exceeded the 50% ceiling, and six-Essence controls also reached 32/32. Selecting a weaker reference would conceal these results.

The user's instruction to proceed after the [budget draft](Tower-Progression-Budget-Draft-20260928.md) carries the existing equipment curve into this offline pass. It does not establish acquisition feasibility or a maximum gear budget for real players. The original draft and preview bytes remain unchanged.

## Results

Each row below is a separate team/gear combination with the same **32 previously used seeds**. These are diagnostic observations, not fresh confirmation or a formal balance acceptance decision. Results are not pooled across parties, profiles or budgets.

| Floor / cohort | Team × gear combinations | Observed wins per combination | Combinations above 16/32 | Combinations at 32/32 |
| --- | ---: | --- | ---: | ---: |
| 10, intended six Essences | 3 × 7 = 21 | 32/32 throughout | 21 | 21 |
| 11, intended seven Essences | 12 × 7 = 84 | 8–32/32 | 80 | 37 |
| 11, diagnostic six Essences | 2 × 7 = 14 | 19–32/32 | 14 | 7 |
| 11, diagnostic four Essences | 2 × 7 = 14 | 0–5/32 | 0 | 0 |

The floor-11 seven-Essence authored control won 14/32 on its original gear, which could look acceptable in isolation. Its resistance-and-health variant won 31/32. The same profile scored **31–32/32 across all twelve seven-Essence finalists**. Known stronger compositions and gear therefore materially change the balance conclusion.

The six-Essence repeated deployment won 32/32 even with original gear; the alternating deployment won 29/32, rising to 32/32 with resistance-and-health or health-and-regeneration. This is a current below-target progression concern. Six- and seven-Essence cohorts also differ in level, rank and quality, so this is not an isolated measurement of an Essence slot's effect.

The published four-Essence primary won 1/32 on original gear and 5/32 with resistance-and-health. Its anchor reached at most 1/32. These observations preserve evidence of some low-budget clears without establishing that four Essences are reliably sufficient. Historical results under older attributes/content do not transfer to this screen.

All outcomes, including two draws, remain in the reports. Draws occurred in seven-Essence case 04 with precision gear and case 09 with original gear. Mean battle durations by cell ranged from 32.41–53.73 seconds on floor 10; 48.42–86.85 for seven-Essence floor 11; 65.22–88.07 for six-Essence floor 11; and 104.02–134.21 for four-Essence floor 11. These are descriptive means, with no approved pacing threshold.

### Complete win-count matrix

Every entry is wins out of 32. Columns preserve the original profile order. Exact parties and full source identities are in the captured `request.json` and `cells.json`.

| Case | Original | Precision | Haste | Restorer | Armor/health | Resistance/health | Health/regen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Floor 10 authored | 32 | 32 | 32 | 32 | 32 | 32 | 32 |
| Floor 10 retained 1 | 32 | 32 | 32 | 32 | 32 | 32 | 32 |
| Floor 10 retained 2 | 32 | 32 | 32 | 32 | 32 | 32 | 32 |
| Floor 11 seven 01 | 14 | 13 | 8 | 17 | 20 | 31 | 21 |
| Floor 11 seven 02 | 31 | 30 | 26 | 31 | 32 | 32 | 32 |
| Floor 11 seven 03 | 24 | 20 | 17 | 30 | 31 | 32 | 31 |
| Floor 11 seven 04 | 22 | 17 | 16 | 30 | 32 | 32 | 32 |
| Floor 11 seven 05 | 31 | 32 | 31 | 32 | 32 | 32 | 32 |
| Floor 11 seven 06 | 31 | 32 | 28 | 30 | 32 | 32 | 32 |
| Floor 11 seven 07 | 32 | 30 | 24 | 32 | 32 | 32 | 32 |
| Floor 11 seven 08 | 32 | 30 | 30 | 32 | 32 | 32 | 32 |
| Floor 11 seven 09 | 26 | 18 | 23 | 31 | 31 | 32 | 32 |
| Floor 11 seven 10 | 32 | 30 | 31 | 32 | 32 | 32 | 32 |
| Floor 11 seven 11 | 26 | 22 | 31 | 23 | 32 | 32 | 32 |
| Floor 11 seven 12 | 22 | 26 | 25 | 30 | 31 | 32 | 31 |
| Floor 11 six 1 | 32 | 29 | 28 | 32 | 32 | 32 | 32 |
| Floor 11 six 2 | 29 | 28 | 19 | 31 | 31 | 32 | 32 |
| Floor 11 four primary | 1 | 0 | 1 | 1 | 2 | 5 | 2 |
| Floor 11 four anchor | 0 | 0 | 0 | 0 | 0 | 0 | 1 |

## Frozen scope and retained evidence

Current captured rules: attributes **18**, equipment release **4**, ability profile **healing-v1**. All gear is Uncommon with baseline rolls and no styles; Essences are level 1, unascended and unevolved. Ownership is hypothetical. Full parties, positions, item/actor identities and ordered Essences are preserved; gear alternatives change only declared specializations.

| Cohort | Characters | Essences / level | Tier / rank / quality | Purpose |
| --- | ---: | --- | --- | --- |
| Floor 10 | 15 | 6 / 50 | 2 / 2 / Standard | Intended test budget |
| Floor 11 | 10 | 7 / 60 | 2 / 3 / Fine | Intended test budget |
| Floor 11 | 10 | 6 / 50 | 2 / 2 / Standard | Below-target diagnostic |
| Floor 11 | 10 | 4 / 30 | 1 / 1 / Standard | Below-target diagnostic |

The family was fixed before execution:

- Floor 10 includes the exact current coverage reference plus both six-Essence whole-party deployments previously measured at 20/20 on floor 11: repeated `097a5be24985…` and alternating `d340c925dc73…`.
- Floor 11 includes **all twelve** seven-Essence finalists from the [whole-party study](Tower-Whole-Party-Review.md). Each uses its historically higher-clear floor-11 ally context, with ties choosing balanced. Only the authored finalist selected `previous-05`; all others selected balanced. This is historical control selection, with no transferred confirmation.
- The same repeated/alternating six-Essence deployments and the exact [published four-Essence primary/anchor](Boss-Expansion-Recipes-20260911/README.md) remain separate floor-11 diagnostic cohorts. No Essences are added to make them match the intended budget.
- All seven gear choices apply to every complete party. No profile is dropped after observing the screen.

The source whole-party package's checksum pin is `0a9fb683afc6f793f9bf3bb9b6e94944114e52a9496f5fb0dafb8a510b9c4813`. Consumed reports, contexts and definitions were authenticated against it. The prior gear coverage manifest remains `f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746`; published four-Essence exports retain their documented byte hashes.

## Runtime qualification and verification

The new build uses an isolated output directory and includes unrelated shared-checkout changes. Before evaluating new cases, **all 224 floor-10 input hashes and complete reports** matched the saved coverage reference and its six gear alternatives. Those 224 repeats are included in the 4,256 cap. This establishes compatibility for that panel, not universal equivalence across all game systems.

All 133 parties were prepared before combat. The fixed owner allowed 900 seconds, the native fixture 840 seconds, output at most 2 GiB, and zero retries or adaptive extension. The study completed in **125.01 seconds**, its owner in **127.30 seconds**, and all **eight processes drained**. The sealed archive is **136,593,777 bytes**. No fresh seeds were allocated; the latest scientific exclusion union remains **834,295**.

Native verification reconstructed every trial's recipe, input and cache identity. The independent Python audit authenticated **4,494 files**, rebuilt all imported source recipes and specialization changes, checked every raw report, paired gain/loss and summary, and independently reproduced all 224 runtime parity comparisons. It passed. Its initial run stopped because its parser assumed every original equipment ID contained an explicit specialization; the parser was corrected to support existing default IDs. This read-only repair changed no experiment data and repeated no fights.

**21 ordinary focused checks passed**, with the combat fixture intentionally skipped until configured. The separately owned combat fixture then passed. The build had 45 existing warnings and no errors. Python syntax, CLI, whitespace and report readback checks passed. No required verification remains blocked.

## Next step

Use an offline **linked Health/Power calibration**, starting with floor 11 because its six-Essence clears directly challenge the intended seven-Essence checkpoint. Preserve the full seven-Essence family, strong gear variants and lower-budget controls when comparing settings. Keep the approved 10–50% criterion; the earlier search-benchmark 4–28/32 screening band is not a balance target.

Current floor-11 baseline scaling is Health **2.90**, offense **3.72**; floor 10 is Health **1.64**, offense **0.92**. Derive any grid from those captured baselines, freeze its cap before execution, and stop if no setting qualifies. If intended- and lower-budget teams cannot be separated through that control, report that result and investigate the encounter mechanics instead of imposing an artificial Essence restriction. Use fresh confirmation and the existing uncertainty policy before accepting a chosen setting. This screen selects no production setting and starts no calibration automatically.

## Files and commands

Added [native checkpoint fixture](../LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionCheckpointTests.cs), [bounded owner](analysis/screen-tower-progression-checkpoints.py), [independent auditor](analysis/verify-tower-progression-checkpoints.py), and this record. Updated the budget draft's follow-up note and the [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md). No search-policy or production gameplay code changed in this increment. No migrations, production configuration edits, deployments or database operations. Separate Colosseum/equipment-startup work remains untouched.

Local study: `TestResults/tower-checkpoint-screen-20260928/`.
Owner and audit: `TestResults/tower-checkpoint-owner-20260928/`.
Manifest SHA-256: `c488b7987ca1ac44dbc06ad43203aff01d03ebe32dedd4bc09d4d3c98a8e0dda`.
Result SHA-256: `b0a51ade7cf379ed4cb436d8b35c179895921a72e6162b12d90d24c41199a061`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-checkpoint-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessProgressionCheckpointTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests'
python -B -X utf8 'Balance Harness/analysis/screen-tower-progression-checkpoints.py' --package TestResults/tower-checkpoint-owner-20260928 --output TestResults/tower-checkpoint-screen-20260928 --artifacts TestResults/tower-checkpoint-build-20260928
python -B -X utf8 'Balance Harness/analysis/verify-tower-progression-checkpoints.py' --study TestResults/tower-checkpoint-screen-20260928 --owner TestResults/tower-checkpoint-owner-20260928 --manifest-pin c488b7987ca1ac44dbc06ad43203aff01d03ebe32dedd4bc09d4d3c98a8e0dda --receipt TestResults/tower-checkpoint-owner-20260928/independent-audit.json
git diff --check
```

Python above refers to the bundled Python runtime. Completed output paths cannot be reused; future read-only audits need a new receipt path. The commands record this completed screen and do not allocate a fresh experiment.
