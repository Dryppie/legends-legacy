# Concrete progression budget draft — 28 September 2026

Superseded equipment intent: the user subsequently specified the [repeating ten-floor rarity/quality/rank curve](Tower-Repeating-Equipment-Curve-20260928.md). Use its version-2 fixture for new intended-budget work. The original draft and recorded observations below remain historical.

Follow-up: the user proceeded with the recommended existing curve for the [completed floor-10/11 offline screen](Tower-Progression-Checkpoint-Screen-20260928.md). Its 4,256 diagnostic fights preserve intended and lower-budget cohorts. The original draft and preview below retain their provisional ownership assumptions; the follow-up does not define a maximum gear budget for real players.

**Recommendation: keep the supported search and use the existing equipment curve as the next explicit test budget.** The [floor-13 geared search](Tower-Floor13-Geared-Search-Evaluation-20260928.md) did not establish an improvement. The useful next decision is which player resources the Tower should be balanced around. This draft makes the existing assumptions executable and reviewable without declaring them approved.

The offline preview prepared **77 complete parties: 11 floors × original gear plus six specialization alternatives**, using current attributes 18, equipment release 4 and healing-v1. It ran **zero fights**, reserved **zero seeds**, and made no balance or search-quality claim. The parties are authored starting compositions; known retained specialists must be added before evaluating balance.

## Proposed curve and its cost

The [earlier progression plan](Boss-Specific-Essence-Loadout-Plan.md#progression-requirements) already records four Essences through floor 4 as a working interpolation, five on floors 5–9, six on floor 10 and at least seven on floor 11. This draft uses seven on floor 11. The user-approved Essence anchors and the provisional equipment assumptions remain distinct.

All rows assume **Uncommon equipment, baseline rolls, no styles, and level-1 unascended/unevolved Essences**. Ownership of every matching item and required Essence copy is hypothetical. Levels are the minimum unlock levels for the specified Essence count. Tier 2 is legal at level 50.

| Floors | Characters per party | Essences / level | Tier / rank / quality | Parts from rank 1 | Cinders from rank 1 |
| --- | ---: | --- | --- | ---: | ---: |
| 1–4 | 5 | 4 / 30 | 1 / 1 / Standard | 0 | 0 |
| 5 | 10 | 5 / 40 | 1 / 2 / Standard | 800 | 1,784,000 |
| 6–7 | 5 | 5 / 40 | 1 / 2 / Standard | 400 | 892,000 |
| 8–9 | 10 | 5 / 40 | 1 / 2 / Standard | 800 | 1,784,000 |
| 10 | 15 | 6 / 50 | 2 / 2 / Standard | 2,400 | 5,352,000 |
| 11 | 10 | 7 / 60 | 2 / 3 / Fine | 4,800 | 10,704,000 |

These are **whole-party reinforcement costs**, not cumulative climbing costs. Each character has seven items occupying eight slots; a two-handed weapon is charged for two slots. Starting at rank 0 instead costs 200 parts / 446,000 Cinders on each floor-1–4 party, 3,600 / 8,028,000 on floor 10, and 5,600 / 12,488,000 on floor 11. The JSON records both starting-rank calculations for every floor.

The costs assume the correct tier, rarity, quality and specialization have already been acquired. They exclude acquisition, Essence training, styles and time. A rank-1 alternative would remove reinforcement expenditure from the rank-1 starting point, but it would change the balance question. Existing results cannot be transferred to that alternative. The current source does not establish how much wealth players should have at each checkpoint.

**Equipment intent remains pending.** For continuity, this prepared draft uses the existing curve. It does not assume that these items cap what strong players can obtain. If the target population includes higher rarity, stronger rolls, styles or trained Essences, those resources need an explicit budget before a production balance decision.

## Delivered implementation

- [Draft fixture](../LL/tools/BalanceHarness/Fixtures/tower-progression-budget-draft.json): explicit, editable budgets for floors 1–11, with provisional ownership/equipment assumptions.
- [Preview implementation](../LL/tools/BalanceHarness/TowerProgressionPreview.cs): validates floor coverage, slot unlocks, tier requirements and the existing Essence anchors; expands complete parties through existing factories; applies every supplied gear profile; prepares all parties through the production Tower preparation path. It calculates reinforcement prices from the current equipment price catalog and actual occupied slots.
- [CLI registration](../LL/tools/BalanceHarness/Program.cs): adds `tower-progression-budget-preview` and help text.
- [Tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionPreviewTests.cs): actual content preparation, cost boundaries, two-handed slot accounting, no combat, empty exported schedules, overwrite refusal, and rejection of missing/illegal checkpoints.
- [Search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md): documents the preview and its limits.

The preview preserves original gear alongside precision, haste, restorer, armor/health, resistance/health and health/regeneration alternatives. It does not choose a weaker specialization to fit a win-rate target. It also corrects the **evaluation budget for floor 8 to five Essences**; the earlier four-Essence screen remains separate below-budget evidence, unchanged.

Source hashes and execution identity accompany the output. Hashes are checked before and after preparation to detect source drift. Each exported scenario has an empty seed schedule. A constant seed is supplied only to satisfy the existing preparation API; it is never used for combat or reserved as a scientific sample. The output is a review packet, not a sealed study or a fresh runnable experiment.

## Next balance work

Once the equipment target is settled, prioritize **floor 10 at six Essences and floor 11 at seven**, with the strongest compatible retained parties and all measured gear alternatives. Floor 10 already has a [current 32/32 diagnostic concern](Tower-Gear-Profile-Integration-20260928.md) at this six-slot budget. Floor 11 needs current intended-budget coverage. The 77 authored parties alone are insufficient for either floor's acceptance assessment.

Retain the lower-budget controls, including floor 8's four-slot success. Adding a slot or choosing another gear profile does not erase that result. Requalify the runtime used for combat, freeze complete controls and a bounded schedule, then evaluate using the existing 10–50% policy and uncertainty rules. Any boss calibration belongs in a separate captured content copy after that baseline. No new algorithm campaign or production tuning follows automatically from this preview.

## Verification and local artifact

Executed:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-progression-preview-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests'
dotnet 'TestResults/tower-progression-preview-build-20260928/bin/BalanceHarness/release/BalanceHarness.dll' tower-progression-budget-preview 'LL/tools/BalanceHarness/Fixtures/tower-progression-budget-draft.json' 'LL/src/API/API.LL' 'LL/tools/BalanceHarness/Fixtures' 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json' 'TestResults/tower-progression-budget-preview-20260928.json'
git diff --check
```

**21 tests passed**, none failed or skipped. The build completed with 45 existing warnings and no errors. The initial sandboxed build could not read the user's NuGet configuration; rerunning the required wrapper with approved access succeeded. No required command remains blocked. The standalone CLI then prepared all 77 parties successfully. JSON readback and source-hash/cost checks passed.

Local [preview JSON](../TestResults/tower-progression-budget-preview-20260928.json): 2,681,855 bytes; SHA-256 `03224420ffd7af3318c069713aaefe590e7782bc202a97373561ee4178c014aa`.

The new build has its own output directory and includes the shared checkout's separate Colosseum edits. Preparation success does not establish combat equivalence to the older sealed experiments; their executable copies and results remain intact. No gameplay files were changed in this increment. No migrations, production configuration changes, deployments or database operations. The latest scientific exclusion total remains **834,295**.
