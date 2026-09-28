# Repeating Tower equipment curve — 28 September 2026

Follow-up: the [completed checkpoint baseline](Tower-Equipment-Cycle-Baseline-20260928.md) evaluated all 19 retained teams and seven gear choices under this curve. Its 4,256 new-budget fights plus 224 runtime checks passed an independent audit. Floor 10 stayed at 32/32 throughout; all six-Essence floor-11 variants and the strongest four-Essence variant also reached 32/32. Floor-11 calibration with those controls retained is the next recommended step.

The intended offline equipment budget now follows the user's **ten-floor repeating curve**:

| Position within each ten-floor block | Rarity | Quality | Rank | Examples |
| --- | --- | --- | ---: | --- |
| 1–3 | Rare | Standard | 2 | 1–3, 11–13, 21–23 |
| 4–6 | Epic | Fine | 3 | 4–6, 14–16, 24–26 |
| 7–9 | Unique | Exceptional | 4 | 7–9, 17–19, 27–29 |
| 10 | Legendary | Masterpiece | 5 | 10, 20, 30 |

Cycle position is `(floor - 1) % 10 + 1`. Rarity, quality and rank reset at the next block. Character level, equipment tier and Essence count remain separate declarations: the existing checkpoint input uses level 50 / tier 2 / six Essences on floor 10, and level 60 / tier 2 / seven Essences on floor 11. This change does not infer a new tier-per-block or Essence-unlock curve.

## Implementation

- [Version-2 budget fixture](../LL/tools/BalanceHarness/Fixtures/tower-progression-budget-cycle.json) records the four bands and explicit budgets for the currently declared floors 1–11. Its equipment status is `UserDefinedRarityQualityRank`.
- [Equipment curve](../LL/tools/BalanceHarness/TowerProgressionEquipment.cs) resolves any positive floor to its cycle position and applies rarity, quality and rank to a copied complete party. It preserves character level, tier, rolls, ordered Essences, positions, archetypes and gear specializations. New budgets produce seed-free scenarios and explicitly discard transferred strength claims. Production preparation validates every export.
- [Preview](../LL/tools/BalanceHarness/TowerProgressionPreview.cs) supports the new version and reports actual rarity and reinforcement costs. [CLI routing](../LL/tools/BalanceHarness/Program.cs) exposes the preview and retained-party conversion.
- [Search contract](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) accepts the production maximum rank of **5**. No production equipment rule needed changing.
- [Tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionEquipmentTests.cs) cover band boundaries and repeated blocks, real item rarity, rank-5 costs, legal complete parties, retained specializations, empty exported schedules, invalid bands and version-1 compatibility.

The older provisional fixture, legacy slot-based search presets and sealed experiments remain available for reproduction. New intended-budget work should use the version-2 fixture. The cycle resolver covers future floor numbers without inventing unreleased encounters; actual preparation still requires a released floor and explicitly legal level/tier/Essence inputs.

## Prepared inputs

The new preview prepared **77 legal complete parties**: all eleven declared floors, each with original specializations plus the six existing gear alternatives. It ran no fights and reserved no seeds.

The **19 retained base parties** from the completed floor-10/11 screen were also converted and prepared. This retains the three floor-10 parties, all twelve seven-Essence floor-11 finalists, and the four-/six-Essence diagnostic controls. Every item on floor 10 now uses Legendary / Masterpiece / rank 5. Every item on floor 11 uses Rare / Standard / rank 2, including the lower-Essence controls; their separately declared levels and tiers remain intact. The six gear alternatives remain available through `TowerGearProfiles` for the next comparison.

Whole-party reinforcement costs now reflect the new ranks. Starting from matching rank-1 gear, the authored floor-10 party costs **36,000 parts / 80,280,000 Cinders** to reach rank 5; the floor-11 party costs **1,600 parts / 3,568,000 Cinders** to reach rank 2. These calculations exclude acquiring matching rarity, quality and specializations. Ownership and training remain explicit hypothetical assumptions.

Local preview: [tower-equipment-cycle-preview-20260928.json](../TestResults/tower-equipment-cycle-preview-20260928.json), SHA-256 `d48b01c66939c8581c34d1aada5e1b8b34cfdcd86a80d526a2c9a8ab734c5833`.

Local retained-party packet: [tower-equipment-cycle-teams-20260928](../TestResults/tower-equipment-cycle-teams-20260928/result.json), manifest SHA-256 `7fd4df8ad67f6f49a1239049866f7758d2205e7efe78e48282a1d9b12964ddfc`. The source checkpoint manifest and new budget/runtime hashes are recorded before conversion. The local preparation script is `TestResults/prepare-equipment-cycle-teams-20260928.py`.

## Effect on balance work

The earlier [4,256-fight checkpoint screen](Tower-Progression-Checkpoint-Screen-20260928.md) measured different equipment budgets. Its win rates remain valid for those captured inputs, but cannot establish balance under this curve. **Re-establish the floor-10/11 baseline with the updated retained parties and all gear alternatives before calibrating bosses.** Keep the current supported search and the 10–50% balance policy.

No fights, fresh seed allocation, boss tuning or production changes occurred in this increment. The latest scientific seed exclusion union remains **834,295**. No migrations, production configuration changes, database operations or deployments.

## Verification and usage

**54 focused checks passed**, none failed or skipped. The build completed with 45 existing warnings and no errors. The standalone preview and all 19 retained-party conversion commands succeeded. Readback checks matched the requested rarity/quality/rank and preserved the complete source compositions. No required command remains blocked.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-equipment-cycle-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
dotnet 'TestResults/tower-equipment-cycle-build-20260928/bin/BalanceHarness/release/BalanceHarness.dll' tower-progression-budget-preview 'LL/tools/BalanceHarness/Fixtures/tower-progression-budget-cycle.json' 'LL/src/API/API.LL' 'LL/tools/BalanceHarness/Fixtures' 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json' 'TestResults/tower-equipment-cycle-preview-20260928.json'
python -B -X utf8 'TestResults/prepare-equipment-cycle-teams-20260928.py'
git diff --check
```

To prepare another existing team at its floor's new gear budget:

```text
dotnet <BalanceHarness.dll> tower-progression-gear-apply <scenario.json> <Fixtures/tower-progression-budget-cycle.json> <content-root> <new-scenario.json>
```

Both commands refuse output overwrites and perform zero combat. The party-conversion command removes old seeds; a subsequent evaluation needs its own declared schedule. It preserves specialization choices while applying the floor's rarity, quality and rank.
