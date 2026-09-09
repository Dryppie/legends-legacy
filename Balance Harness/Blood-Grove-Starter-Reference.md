# Blood Grove starter reference — 8 September 2026

The selected Blood Grove starter is **Goblin Warrior + Sword + Heavy Chest**, with a preferred clear-rate aim of **approximately 70% and a revised 50–90% accepted band** for each selected encounter. The [band revision](Blood-Grove-Band-Review.md) supersedes the initial 65–75% policy described in the historical results below. Other Essence combinations remain diagnostic. The [fixture](../LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter.json) and [scoped goal policy](../LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json) use the existing suite, replay and evaluation commands.

## Recipe

| Selection | Exact reference |
| --- | --- |
| Character | Level 5, the Blood Grove access boundary |
| Starter Essence | `essence.goblin_warrior`, level 1, Ascension tier 0, unevolved; one equipped Essence |
| Sword | One-handed Shortsword, `plain.shortsword`, MainHand; offhand empty |
| Heavy Chest | Heavy Breastplate, `plain.heavy_breastplate`, Chest |
| Equipment state | Both common, Standard, tier 1, rank 0, attribute-roll multiplier 1.0; no active/native style |
| Unspent guaranteed resources | One Fury Blueprint, 500 starter Cinders and the Lumo Essence Token |
| Encounters | Raven + Raven; Raven + Blood Zombie, retaining the existing ordered pairs |

“Sword” is interpreted as the one-handed Shortsword in the [equipment catalog](../LL/src/API/API.LL/Data/equipment/equipment-starters.v1.json). Heavy Breastplate is a possible Armor Chest outcome. The user selected it for this reference; that does not change the live random reward into a guaranteed heavy chest. No other armor, shield, jewelry, reinforcement, Fury, buffs or persistent bonuses are assumed. These entry defaults are the recipe for the scoped target; they do not establish measured acquisition pacing.

The separate suite ID is `idle-blood-grove-starter-v1`: one build, one checkpoint and two cells, with 100 samples per cell. Both earlier suites and their draft goals remain unchanged. A recipe change requires its own reviewed fixture identity; ordinary comparisons must still reject incompatible recipes. This selection covers Blood Grove only, without choosing the level-1 or level-10 reference builds.

## Fixed initial measurement

Before executing this recipe, the measurement budget is fixed at 100 trials per encounter on discovery master seed **1337** and confirmation master seed **620903**: **400 battles** total. Run both encounters on both sets without selecting variants, increasing samples or changing the recipe after observing results. Confirm that the seed sets do not overlap within an encounter and that archived combat content/execution match. Retain detailed replays of confirmation trial index 0 for both encounters, irrespective of outcome.

Run these commands from the repository root after a Release build, using fresh output directories:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter.json --seed 1337 --output TestResults/balance/blood-grove-starter-reference/discovery
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter.json --seed 620903 --output TestResults/balance/blood-grove-starter-reference/confirmation
```

Read per-cell clear-rate intervals and separate win/non-win durations. Defeat duration is not kill time. These selected pairs do not estimate the area's overall clear rate; an isolated observed win would not establish reliable progression. The initial measurement preceded target approval and did not promote a difficulty baseline. The fixture's pre-policy wording is retained to preserve its contract and archived evidence; the separate goal policy records the subsequent approval.

## Initial results

All **400 battles completed as defeats**, with no draws, invalid, cancelled or missing battles. The seed sets are disjoint within each encounter, their archived combat content and execution identities match, and the archived recipe matches the file hash captured before execution.

| Encounter | Discovery wins / trials | Confirmation wins / trials | Discovery mean defeat time | Confirmation mean defeat time | Median defeat time, both sets |
| --- | --- | --- | --- | --- | --- |
| Raven + Raven | 0/100 | 0/100 | 29.91 s | 29.40 s | 30 s |
| Raven + Blood Zombie | 0/100 | 0/100 | 39.54 s | 39.34 s | 39 s |

Each cell's pointwise 95% Wilson clear-rate interval is approximately **0–3.7%**. Zero observed wins does not prove that victory is mathematically impossible. These intervals do not provide simultaneous coverage across the four measured cells.

Both preselected detailed replays matched their saved summaries. The character starts with 436 maximum health and uses Raging Cleave twice in each example. Both original enemies remain alive at defeat:

| Confirmation example | Seed | Defeat at | Enemy health left |
| --- | --- | --- | --- |
| Raven + Raven, trial index 0 | 728729739 | 30 s | 63/137 and 80/137 |
| Raven + Blood Zombie, trial index 0 | −1940238876 | 39 s | 81/137 and 134/219 |

The replays confirm active-ability execution in these examples; they do not attribute every defeat to a single mechanic. Retained evidence is under ignored `TestResults/balance/blood-grove-starter-reference`: the pre-run plan and recipe, discovery/confirmation bundles and two detailed replays. These local bundles are not committed or guaranteed to exist in another checkout.

## Verification and next decision

- Release test/tool build passed with 26 warnings and no errors. The first sandboxed attempt could not write a generated MSBuild cache; the same build succeeded with expanded permissions.
- **78 harness tests passed** through `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. Two added parity cases independently prepare and resolve this exact build through the normal production idle path, covering both selected encounters. Existing fixture compatibility and experiment tests remain passing.
- Both 200-battle commands completed, and both selected detailed replays matched. The fixture uses existing schema-2 support; no production code or new command was needed.

The initial measurement established the selected build's starting point. The subsequent target decision and policy evaluation are recorded below. Preserve the losing measurement for comparison; a viable gameplay baseline and hosted CI observation remain open.

No production combat, reward, configuration, database migration or deployment changes were made. The full backend suite, the standalone cohort smoke scripts and hosted CI were not rerun for this fixture-only increment.

## Initial 65–75% target and historical evaluation — 8 September 2026

The user approved an aim near **70%**, explicitly choosing an initial **65–75% band** rather than a minimum to exceed. The policy applies separately to Raven + Raven and Raven + Blood Zombie for the exact level-5 recipe above. Do not average the two encounters to hide a weak or overly easy case. This remains a fixed-encounter target, not an area-wide clear-rate claim. Pacing, health and other builds have no newly approved goals.

The historical [v1 policy](../LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals-v1.json) contains one reviewed, enforced primary goal expanded into two checks. It pins fixture contract `e820b30cf1bdd8763c4ea8c9fdbf138a870611bdfebe728f71454bb348e8a87f`, uses inclusive 65/75 percent bounds and a minimum of 100 valid trials per cell. Its `reviewReason` records the initial user decision. Explicit evaluation returns exit 1 for a clear miss and 3 for inconclusive evidence. Existing default policies and hosted CI remain advisory and unchanged.

The evaluator requires the entire pointwise 95% Wilson interval to lie within the band. Thus 70/100 is **inconclusive**, 700/1,000 **passes**, and both 0/100 and 100/100 **fail**. These are statistical examples, not additional battle results. The 100-trial minimum supports diagnosing large misses; near-target confirmation needs a larger predeclared budget. Use 1,000 trials per encounter as the next confirmation planning budget and reserve fresh seeds before tuning. Do not extend samples repeatedly until a candidate passes. Intervals retain their per-check coverage, without simultaneous correction across the two encounters.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --goals LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals-v1.json --run TestResults/balance/blood-grove-starter-reference/confirmation --output TestResults/balance/blood-grove-starter-evaluation-001
```

Both existing saved runs were evaluated without rerunning combat or changing their archives. Each produced **two failed checks, zero invalid checks, and the expected balance-failure exit code 1**. Each observed clear rate is 0%, with a 95% interval of approximately 0–3.7%, wholly below the 65% lower bound. Complete verified evaluation bundles are retained locally under `TestResults/balance/blood-grove-starter-reference/target-70-discovery` and `target-70-confirmation`.

The policy increment's Release build passed with four warnings and no errors; **79 harness tests passed** through the repository runner. Added coverage pins the policy to its exact recipe/cells and verifies below-target failure, near-target uncertainty, adequately sampled success, above-target failure and insufficient samples. Production balance, rewards, database migrations, deployment and CI configuration are unchanged. Full backend tests, standalone smoke scripts and hosted CI were not run for this policy change.

The first [bounded tuning experiment](Blood-Grove-Pressure-Review.md) is complete. It tested 23 legal values of the regional post-tutorial offense bonus across 18,200 battles, with the recipe and policy fixed. The selected bonus 0.2 confirmed at 88.4% Raven-pair and 78.6% mixed-pair clears, both above the working band. All 16 Lumo control cells stayed identical, but six early Shenic areas changed. Four selected replays matched; no production value or viable baseline was promoted. That increment reached 83 passing harness tests.

The separate [fine sweep](Blood-Grove-Fine-Pressure-Review.md) then completed 24,600 battles on 11 values from 0.30 through 0.20. Bonus 0.21 confirmed at 88.5% Raven-pair clears (Fail under v1) and 74.1% mixed-pair clears (Inconclusive under v1). The discovery jump between 0.22 and 0.21 aligns with integer Raven damage thresholds. All 16 Lumo cells stayed identical; the same six areas changed and four replays matched. That increment reached 85 passing tests and recommended a second tuning parameter under the initial band.

## Revised 50–90% band

The user subsequently widened the accepted band to **50–90%**, retaining the 70% aim and this exact recipe. [Policy v2 and the saved-run re-evaluation](Blood-Grove-Band-Review.md) now supersede the earlier next-step recommendation. Both 0.21 point estimates fit the revised band; the mixed-pair check passes and the Raven-pair check remains inconclusive because its interval reaches 90.33%. Review the candidate and its wider area effects before choosing production scope. No extra combat samples, production change or viable-baseline promotion accompanied the policy revision.

## Blood Grove-only implementation and fresh confirmation

The user subsequently authorized making the 0.21 candidate local to Blood Grove and confirming it on the current engine. The [local validation report](Blood-Grove-Local-Validation.md) records version-12 content with Blood Grove offense 2.421, unchanged scaling in the other 13 areas and an explicit transition ceiling into unchanged Crystal Creek. The recipe and v2 policy are unchanged.

The fixed 21,600-battle validation includes 3,000 fresh trials per encounter for both original and candidate content. The original still records zero wins. The local candidate records **2,679/3,000 Raven wins (89.30%, interval 88.14–90.36%, Inconclusive)** and **2,302/3,000 mixed wins (76.73%, interval 75.19–78.21%, Pass)**. All 32 control cells outside Blood Grove stay identical and all four fixed replays match. All 2,066 backend tests pass. No viable baseline was accepted because the Raven interval overlaps 90%; the fixed run is closed without sample extension. Next, playtest this recipe and the larger Blood Grove-to-Crystal Creek transition. No deployment or database change was made.

The subsequent [level-10 checkpoint and handoff comparison](Crystal-Creek-Starter-Handoff.md) retains the sword/heavy chest and adds an Amulet plus Goblin in the second slot. Its fresh 16,000-battle investigation demonstrates a conditional Crystal Creek path (99.4% / 100% primary confirmation clears) and identifies second-slot preparation and pacing as the next human review. It leaves this level-5 recipe, policy and statistical assessment unchanged.
