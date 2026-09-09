# Blood Grove attainable-entry review — 8 September 2026

The guaranteed Fury Blueprint and every possible Armor Chest item were tested as a follow-up to the [training/reinforcement investigation](Blood-Grove-Progression-Review.md). **Every cell still produced 0/100 wins on both predeclared seed sets.** Heavy armor improved survival, but neither that reward outcome nor Fury made these retained First Hunt Essences clear the two selected Blood Grove encounters. Combat coefficients, rewards and numerical goals remain unchanged.

## Attainable resource contract

[Soul Archive](../LL/src/API/API.LL/Data/quests/onboarding/soul-archive.v3.json) grants one Fury Blueprint, 500 Cinders and the weapon-selection chest. [Into the Ruins](../LL/src/API/API.LL/Data/quests/region-01/into-lumo-ruins.v2.json) grants one Armor Chest before the level-5 Blood Grove entry. The [blueprint catalog](../LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json) charges 100 Cinders at tier 1. [Fury](../LL/src/API/API.LL/Data/equipment/equipment-styles.v1.json) supports both mace and wand; it cannot be put on these armor items.

| Weapon condition | Blueprint spent / remaining | Cinders spent / remaining | Equipment retained |
| --- | --- | --- | --- |
| Plain control | 0 / 1 | 0 / 500 | One weapon and one Armor Chest item |
| Fury | 1 / 0 | 100 / 400 | The same weapon and one Armor Chest item |

Every build stays at character level 5 with its single First Hunt Essence at level 1, Ascension tier 0 and unevolved. Equipment remains common, Standard, tier 1 and rank 0. No extra armor, regional drops, reinforcement, persistent bonuses or buffs are assumed. The already-earned Lumo Essence Token is **retained**, so this matrix does not exhaust every available entry loadout.

The candidate list uses the same filter as [SelectionCrateService](../LL/src/Infrastructure/Service/Services.LL/Inventories/SelectionCrateService.cs): common base drop definitions restricted to the [Armor Chest's](../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/RandomEquipmentBoxCatalog.cs) head/chest/legs types. Its actual award defaults are Standard quality and attribute-roll multiplier 1.0. The current nine candidates are:

| Profile | Head | Chest | Legs |
| --- | --- | --- | --- |
| Heavy | Heavy Helm | Heavy Breastplate | Heavy Legplates |
| Medium | Medium Helm | Medium Mail | Medium Greaves |
| Light | Light Hood | Light Vest | Light Leggings |

These are nine alternative random-box outcomes, not nine items awarded together or a player-selectable armor menu. The experiment measures each condition separately and does not infer a population-wide win rate by pooling them.

## Fixed matrix and results

The new `investigate-entry` command derives a separate `idle-blood-grove-entry-v1` suite from the unchanged First Hunt fixture. Three starter Essences × two weapons × nine armor outcomes × plain/Fury gives **108 builds**, crossed with Raven + Raven and Raven + Blood Zombie for **216 cells**. Each cell runs 100 samples on discovery master seed **1337** and confirmation master seed **940031**, for **43,200 battles**.

The command saves the full recipe, reward ledger, source hashes, sample budget, seed sets and stopping rule before executing combat. Every variant runs on both sets; there is no selection of a winning variant or increase in samples after observing results. Trials are paired across builds, while confirmation seeds do not overlap discovery seeds within an encounter. Replay selection is also fixed in advance: confirmation trial index 0 for all six starter/weapon choices, both encounters, Heavy Breastplate and both weapon conditions—24 replays.

| Armor profile | Conditions per seed set | Discovery: every cell | Confirmation: every cell | Range of confirmation cell mean defeat times |
| --- | ---: | --- | --- | --- |
| Heavy | 72 cells | 0/100 wins | 0/100 wins | 29.58–43.32 seconds |
| Medium | 72 cells | 0/100 wins | 0/100 wins | 23.43–32.92 seconds |
| Light | 72 cells | 0/100 wins | 0/100 wins | 17.92–24.07 seconds |

All 43,200 battles completed as defeats. There were no draws, invalid, cancelled or missing battles. Each 0/100 clear-rate estimate has a pointwise 95% Wilson interval of about **0–3.7%**. Fury's paired clear-rate change is 0 percentage points with an approximate 95% interval of **−4.78 to +4.78 points** for each matched comparison. Zero observed movement is not proof of an exactly zero population effect. The intervals do not have simultaneous coverage across the matrix, and shared seeds mean these battles are not independent observations to pool together.

The head, chest and leg items within each profile produced identical reported clear-rate and duration metrics under these conditions. They remain separate legal outcomes in the matrix. Defeat duration is a survival measurement, not winning pace or kill time.

## What the selected replays show

All 24 preselected detailed replays matched their saved battle summaries. In every one, the player's active ability fired and **both original enemies remained alive at the player's defeat**. This rules out missing active-ability execution in these examples. It does not establish the cause of every defeat or certify all ability mechanics.

For the Fury mace + Heavy Breastplate examples, all players begin with 436 maximum health. Confirmation Raven-pair seed −1670166326 shows:

| First Hunt Essence | Defeat at | Active uses | Damage to Raven 1 / Raven 2 | Enemy health left |
| --- | ---: | ---: | ---: | --- |
| Goblin Warrior | 30 s | 2 Raging Cleaves | 73 / 73 | 68/137 and 69/137 |
| Hollow Stag | 36 s | 2 Echoing Antlers | 61 / 56 | 81/137 and 87/137 |
| Skeleton | 36 s | 2 Bone Smashes | 69 / 56 | 73/137 and 87/137 |

Both Ravens apply Bad Omen's Vulnerable condition at the opening. Goblin Warrior's Raging Cleave first fires at tick 130, matching its authored cooldown and the normal idle opening-cooldown rule, and damages both enemies. Skeleton's Calcium activates at tick 0; Hollow Core activates as health is lost. None of these examples has direct healing or barrier generation; ordinary health regeneration remains present. Damage totals and final health differ because combat also includes regeneration.

The [engine's basic-attack path](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) selects an attention target on each attack using threat-weighted randomness. Combined with the [authored ability targeting](../LL/src/API/API.LL/Data/combat/abilities.json), this explains why damage can be spread across two living enemies. It is the current targeting behavior, not evidence of a harness targeting bug. The mixed-pair Fury mace examples likewise end with both enemies alive: Raven/Zombie health is 39/130 for Goblin Warrior, 38/188 for Hollow Stag and 26/188 for Skeleton, against maxima 137/219.

Fury does reach the combat input. In the Goblin Warrior mace + Heavy Breastplate example it changes Power from 14.11 to 14.41, Crit Chance from 0 to 0.75 and Crit Damage from 100 to 101.7; health and armor stay unchanged. Tests compare these frozen stats and base stats with the production [Forge quote](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs), rather than assuming that setting a style ID is sufficient.

The evidence suggests a failure to remove an enemy quickly enough while absorbing two attackers. How much of that comes from damage, sustain, opening conditions, targeting or enemy scaling needs controlled alternatives. These observations do not justify changing those systems together or choosing a new numerical target from the observed losses.

## Reproduce and verify

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- investigate-entry --output TestResults/balance/blood-grove-entry-001
```

Add `--no-build` before `--` after building. `--samples 1` runs the complete 432-battle matrix as a workflow check. Output must be a new directory. The full experiment is local, not an additional CI matrix.

Local evidence is retained under ignored `TestResults/balance/blood-grove-entry-reference`: `plan.json`, `suite.json`, the original source fixture, selected nonsecret reward sources, discovery/confirmation bundles, `results.json`, `summary.md` and `replays/`. The results contain per-cell scorecards, paired Fury comparisons against the same armor without Fury, and comparisons against the matching plain Medium Mail build. Both saved runs are fully verified and their artifact hashes recorded; their combat content and execution identities must match. Ordinary regression-comparison compatibility stays strict. The command neither promotes a gameplay baseline nor evaluates or approves new goals.

- Release tool/test build passed, with four warnings and no errors.
- **76 harness tests passed** through `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. Added coverage includes six production idle parity cases, every armor/style budget, live Forge-quote equivalence, complete experiment archiving/replay, pairing rejection, output preservation and cancellation.
- The full 43,200-battle command completed, and all 24 selected detailed replays matched. Tests also execute the whole matrix at one sample per cell.
- Both existing cohort smoke workflows passed again, with zero gameplay changes and valid advisory goal evaluations. Their fixtures and goals are unchanged.
- The five captured source hashes, complete experiment totals, CLI help, 106 local Markdown links and scoped whitespace checks passed.

No production service, combat formula, gameplay configuration, database migration or deployment changes are part of this increment. The full backend suite and hosted CI were not run; hosted CI observation and gameplay target approval remain open.

## Next decision

The scope decision on 8 September 2026 defers the proposed **Lumo Token replacement experiment**. The immediate requirement is a viable starter path; universal Essence viability is not required. No replacement experiment was implemented or run.

The user has since selected **Goblin Warrior + Sword + Heavy Chest for Blood Grove**, retaining a 70% aim and revising the initial band to **50–90%** after the pressure experiments. The [selected starter reference](Blood-Grove-Starter-Reference.md) freezes one-handed Shortsword and Heavy Breastplate at the existing level-5 entry defaults. Its separate 400-battle initial measurement still fails the scoped policy; the [band revision](Blood-Grove-Band-Review.md) records the tuned candidates' current assessments. Fixed-pair defeats do not prove whole-area failure. Preserve this broader matrix as diagnostic evidence. Other cohort goals remain draft; see the [delivery plan](Balance-Harness-Plan-With-Benchmarking.md#next-work-to-complete-phase-2).
