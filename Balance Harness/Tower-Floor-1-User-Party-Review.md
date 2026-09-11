# Floor 1: user-authored five-character benchmark

**Latest tuning:** [linked Health/Power tuning](Tower-Floor-1-Linked-Tuning-Review.md) restores Garran's original Health 1.27 / offense 1.24, then scales both by **1.32** to **1.6764 / 1.6368**. This exact party confirmed at **291/1,000 wins (29.1%; 95% interval 26.37–31.99%)** on fresh seeds. The preceding [offense-only calibration](Tower-Floor-1-Tuning-Review.md) recorded 394/1,000 at Health 1.27 / offense 2.0. Both that result and the original 100% benchmark below remain separate historical evidence; their sealed packages are unchanged.

11 September 2026. The exact user-authored party won **1,000/1,000 battles** against floor 1, with no defeats or draws. The pointwise 95% Wilson interval is **99.62–100%**. At the modeled entry budget below, this is an **upper-bound breach** of the approved [10–50% Tower target](Tower-Balance-Acceptance-Policy.md), not accepted difficulty. No boss or build was changed to obtain this result.

This is one fixed floor-1 test party. It is neither the default party for other floors nor a complete progression benchmark. The user's longer-term preference remains automatic generation of effective complete teams for each boss and budget; this example is an optional floor-1 reference for that work.

## Exact party and equipment assumptions

The user specified all twenty Essence selections. Their character positions and Essence order are preserved exactly in the [runnable scenario](../LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json).

| Character | Ordered Essences | Assumed weapon / armor |
| --- | --- | --- |
| 1 | Transparent Slime → Blue Slime → Treant Sapling → Illusion Fox | Maul; Heavy Breastplate, Heavy Helm, Heavy Legplates |
| 2 | Vampire Bat → Nightshade Blossom → Frost Imp → Illusion Fox | Staff; Light Vest, Light Hood, Light Leggings |
| 3 | Nightshade Blossom → Illusion Fox → Blue Slime → Venomous Spiderling | Gauntlets; Light Vest, Light Hood, Light Leggings |
| 4 | Forest Spirit → Illusion Fox → Enchanted Fairy → Venomous Spiderling | Gauntlets; Light Vest, Light Hood, Light Leggings |
| 5 | Enchanted Fairy → Illusion Fox → Venomous Spiderling → Nightshade Blossom | Greatsword; Medium Mail, Medium Helm, Medium Greaves |

All five characters are **level 30**, with four **level-1, unascended, unevolved Essences**, seven **Uncommon Standard tier-1/rank-1** equipment items and baseline rolls. Each also wears a Band, Amulet and Vial. The equipment comes from the existing `tower-curve.json` balanced entry positions: guardian, restorer, striker, striker and controller. These are declared equipment assumptions because the user supplied Essences only; no role label restricts what the supplied builds can do.

Ownership is hypothetical. Equipment and Combat Styles, scouting, contributions and other player bonuses are absent. Floor 1 starts uncleared; every battle uses full prepared health, default resources, production ability cooldowns and the 600-second limit. These results are conditional on this gear/level budget, not a measurement of acquisition timing or a continuous climb.

## Results

| Metric | Observation |
| --- | ---: |
| Valid battles | 1,000 / 1,000 |
| Victories / defeats / draws | 1,000 / 0 / 0 |
| Win rate | 100% |
| Pointwise 95% Wilson interval | 99.62–100% |
| Mean winning duration | 98.71 seconds |
| Median / 90th-percentile duration | 99.0 / 99.1 seconds |
| Shortest / longest fight | 90.8 / 113.0 seconds |
| All five original characters survived | 888 / 1,000 battles |
| Fewest original survivors | 3 |
| Mean remaining party health | 66.82% of the original members' combined final maximum health |

This party demonstrates viability well above the 10% floor and also violates the 50% ceiling. A floor can fail the upper bound from one such fixed party; establishing that all strong builds stay below the ceiling requires wider coverage. Do not average this result with weaker parties or suppress it during automatic team selection.

The test does not isolate the contribution of Illusion Fox, any other Essence, equipment or a specific interaction. The artifact's per-character damage/healing totals are descriptive ability statistics; matched substitutions would be a separate experiment. No additional variants or gear sweeps were run after observing the result.

## Protocol and verification

Before combat, the experiment froze this one party, its equipment mapping, exactly 1,000 unique seeds and an allowance of at most three detailed replays: the first trial of each observed outcome. The SHA-256 seed namespace is `tower-floor-1-user-party-20260911-v1`. The schedule excludes **79,183 distinct integers** from captured historical exclusions and the latest coverage expansion's schedules/ledgers. This is conservative: those metadata include some non-seed integers. All source hashes and the exact schedule are recorded; this is not an account-wide seed registry.

The run completed its fixed sample count without search, build substitutions, seed replacement or sample extension. **One detailed replay**, `tower.0001`, matched saved preparation, combat and Tower outcome; victory was the only observed outcome. Replay is a repeated observation, so the statistical sample remains 1,000. Total combat executions were **1,001**.

The audit checked all **1,000 battle hashes and reports**, every frozen input's ordered party/gear and seed, all **16 content hashes**, all **five execution assembly hashes**, exclusion-source hashes, counts, Wilson endpoints, durations and detailed replay equality. `analyze.py --verify-existing` reconstructs the saved analysis without running combat or writing files.

Relevant backend checks passed: **106 tests, zero failures/skips**. The initial normal script build could not read the sandbox-restricted user NuGet configuration. Building from the existing restored assets succeeded with **zero warnings/errors**, then tests ran through the repository entry point:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests'
```

No required verification remains blocked. The initial failure and successful recovery logs are preserved. The native legacy `scorecard.md` still contains its historical sentence about no approved Tower target; this review applies the user's subsequently approved 10–50% policy. No automatic Tower acceptance evaluator was added by this standalone test.

## Reproduction and retained files

The evidence package is [tower-floor-1-user-party-20260911](../TestResults/balance/tower-floor-1-user-party-20260911/): frozen `scenario.json` and `protocol.json`, source/exclusion hashes, retained executable, captured combat content and inputs, all battles, scorecard, detailed replay, test logs/TRX, `analysis.json`, audit scripts and a checksum inventory. The live fixture and captured recipe have SHA-256 `d9766b095941eed4101a6ae822323438cb889da512bcd0f534b0de648a083a13` at publication. Execution uses the recorded .NET 10.0.12 runtime and assembly hashes.

Run the checked-in scenario with the current harness/content into a new directory:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- tower --scenario LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json --content-root LL/src/API/API.LL --output TestResults/balance/tower-floor-1-user-party-rerun
```

Reusing its 1,000 seeds reproduces this schedule and provides no fresh statistical confirmation. Exact historical replay uses the retained executable and content:

```powershell
dotnet TestResults/balance/tower-floor-1-user-party-20260911/executable/BalanceHarness.dll replay --run TestResults/balance/tower-floor-1-user-party-20260911/run --battle tower.0001 --detailed
```

Changed deliverables for this benchmark were the standalone scenario, this review and links/scope notes in the harness README and loadout plans. It changed no optimizer defaults, other-floor cohorts, production combat/content, configuration, migrations, account state or deployment. The acceptance evaluator and independent team search were subsequently [implemented through Tower Lab](Automatic-Tower-Team-Lab-Review.md); their [fixed floor-1/floor-5 pilots](Automatic-Tower-Team-Pilot-Review.md) are complete, while the broader progression audit remains pending. This historical benchmark is preserved separately from those later implementation checks.
