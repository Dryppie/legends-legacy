# Expected progression gear: acquisition requirements — 28 September 2026

**Follow-up:** the [Tower equipment supply implementation](Tower-Equipment-Supplies-Implementation-20260928.md) now adds guaranteed, selectable preparation gear through completed dungeons. The report below records the preceding acquisition audit and its captured build. Elapsed acquisition time and combat with earned inventories remain to be validated.

**User decision: the repeating gear curve represents expected progression gear.** Parties should reasonably obtain that gear when reaching each floor. Legal benchmark construction alone does not establish this requirement. The [completed floor-10 controls](Tower-Floor10-Lower-Essence-Controls-20260928.md) found no five-Essence counterexample in their fixed family; acquisition is now the main unresolved progression assumption.

The current-build [budget preview](../TestResults/tower-expected-gear-20260928/budget-preview.json) prepares **77 complete party variants across all eleven declared floor budgets**, with zero combat and zero seed reservations. The new [acquisition auditor](analysis/audit-expected-gear.py) verifies the preview's source hashes, independently checks reinforcement arithmetic and evaluates every ordinary dungeon grade against the requested rarity and quality. Its [full output](../TestResults/tower-expected-gear-20260928/acquisition-preview.json) explicitly records `ExpectedProgressionGear` and `AcquisitionRequirementsIdentified`, not acquisition acceptance.

## Availability by equipment band

These are probabilities **per awarded equipment item**, conditional on the listed grade. They do not imply that grade is accessible or beatable before the target floor. “At least” includes higher rarity and quality; both figures ignore fit, specialization, styles, rolls and duplicates.

| Requested band | Novice: exact / at least | Veteran: exact / at least | Champion: exact / at least |
| --- | ---: | ---: | ---: |
| Rare / Standard | 7% / 14% | 42% / 87.5% | 0% / 87.5% |
| Epic / Fine | 0.5% / 0.75% | 3.5% / 6% | 21% / 37.5% |
| Unique / Exceptional | 0% / 0% | 0.2% / 0.25% | 1.4% / 2% |
| Legendary / Masterpiece | 0% / 0% | 0% / 0% | 0.05% / 0.05% |

Champion's zero exact Rare rate is not a missing path to stronger gear: its drops start at Epic. Conversely, the requested Legendary/Masterpiece floor-10 band remains **1 in 2,000 equipment drops**, even when higher gear is allowed. Its 105-item authored party would average 210,000 qualifying-channel equipment drops if every qualifying item were usable. This is an optimistic count of rarity/quality matches, not an expected full-loadout completion time. Other channels and additional dungeon rewards are not included.

Ordinary area drops cannot supply Epic, Unique or Legendary gear. Rank reinforcement preserves rarity and quality; higher bands therefore require a source for new gear rather than merely enough reinforcement currency.

## Standalone party reinforcement requirements

These amounts upgrade hypothetical dungeon rank-1 gear to each floor's requested rank. They exclude acquiring the gear and earning the resources. They describe each authored party separately and **must not be summed as progression expenditure**: characters and gear can be reused, party sizes vary, and ownership routes have not yet been modeled.

| Floor | Characters / items | Tier | Target rank | Reinforcement Parts | Cinders |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1–3, each | 5 / 35 | 1 | 2 | 400 | 892,000 |
| 4 | 5 / 35 | 1 | 3 | 1,200 | 2,676,000 |
| 5 | 10 / 70 | 1 | 3 | 2,400 | 5,352,000 |
| 6 | 5 / 35 | 1 | 3 | 1,200 | 2,676,000 |
| 7 | 5 / 35 | 1 | 4 | 2,800 | 6,244,000 |
| 8–9, each | 10 / 70 | 1 | 4 | 5,600 | 12,488,000 |
| 10 | 15 / 105 | 2 | 5 | 36,000 | 80,280,000 |
| 11, standalone reference | 10 / 70 | 2 | 2 | 1,600 | 3,568,000 |

The floor-11 reference band is not a gear reset. A party that already owns eligible floor-10 Legendary/Masterpiece/rank-5 equipment retains it. The [carried-equipment calibration and application](Tower-Carried-Equipment-Checked-Application-20260928.md) already account for that stronger transition budget. A real progression model must track individual ownership and incremental spend, not charge a fresh replacement set at every floor.

## Implementation requirements recorded before the supply change

The [supply implementation](Tower-Equipment-Supplies-Implementation-20260928.md) now provides the targetable route described below. See the [current handoff](Tower-Continuation-Handoff-20260928.md) for remaining resource/time and earned-inventory validation; do not implement a duplicate acquisition path from this historical recommendation.

Keep the user-defined curve as the target. Build a targetable, bounded acquisition path for the required rarity, quality, equipment slot and specialization. It must be available through content that can be completed **before** the floor requiring the gear. Rewards for clearing that same floor cannot be the sole prerequisite for reaching its expected budget.

That path needs explicit resource income, reward cadence, expected effort and ownership/trading assumptions. Cover party growth to 10 and 15 characters, the tier-1→2 transition at floor 10, and carrying gear into floor 11. Then evaluate combat with inventories produced by that path, preserving the confirmed teams as references and keeping acquisition evidence separate from battle win-rate evidence.

The current preview does not set new reward rates, add a crafting system, promise an acquisition time, or claim to model every quest/event/trading route. The local floor-10/11 balance remains validated for its declared equipment budgets; normal-player availability is **not yet validated**. No further search-algorithm experiment or scalar boss sweep is indicated by the completed controls.

## Verification and scope

The native preview reports **11 floors, 77 prepared parties, zero fights and zero reserved seeds**. The Python audit authenticates all native source hashes, preserves the expected-gear interpretation and checks every native rank-cost row against production prices. The full probabilities and per-grade completion rates are retained in its JSON output. The numerical equipment curve is unchanged.

Acquisition-preview SHA-256: `b38f4b7243910a5a626d23b120c31d367e3a25ec7892f651d2096ce37b9abf1f`.

Final readback verified all **37 source hashes** and all **90 rarity/quality/grade probability cases**, including that higher-gear eligibility never lowers the probability. Python syntax, documentation links and whitespace checks passed. The 129 focused backend checks and 15 enabled scientific-fixture checks supporting the current build are recorded in the floor-10 controls report. No required command remains blocked or unrun.

```powershell
dotnet 'TestResults/tower-floor10-controls-build-20260928/bin/BalanceHarness/release/BalanceHarness.dll' tower-progression-budget-preview 'LL/tools/BalanceHarness/Fixtures/tower-progression-budget-cycle.json' 'LL/src/API/API.LL' 'LL/tools/BalanceHarness/Fixtures' 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json' 'TestResults/tower-expected-gear-20260928/budget-preview.json'
python -B -X utf8 'Balance Harness/analysis/audit-expected-gear.py' --preview 'TestResults/tower-expected-gear-20260928/budget-preview.json' --output 'TestResults/tower-expected-gear-20260928/acquisition-preview.json'
```

These document the completed run; outputs cannot be overwritten. Python denotes the bundled runtime. Preview artifacts under ignored `TestResults` are local and absent from a clean checkout. Added the auditor and this report; no gameplay content, application configuration, dependencies, migrations, database operations, deployments or service restarts changed.
