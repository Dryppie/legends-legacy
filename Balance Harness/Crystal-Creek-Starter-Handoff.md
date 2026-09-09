# Blood Grove to Crystal Creek starter handoff — 8 September 2026

The user authorized a level-10 starter checkpoint, a comparison between Blood Grove and Crystal Creek, and a short playtest checklist. This is an offline investigation for the primary LL game. Existing regional content, the level-5 starter fixture and its 50–90% policy remain fixed. The [local Blood Grove result](Blood-Grove-Local-Validation.md) remains closed and inconclusive under that policy; this work does not append level-5 samples or certify it indirectly.

## Recipe and acquisition audit

**Subsequent policy decision:** the user approved a 50–90% win band for the primary level-10 Crystal Creek build and explicitly excluded duration from acceptance. Both saved seed sets fail the [new policy](../LL/tools/BalanceHarness/Fixtures/idle-crystal-creek-starter-goals.json), because their primary clear-rate intervals lie above 90%. The original fixture and results below remain unchanged; statements about unapproved targets describe the investigation at completion. See [Starter Path Acceptance](Starter-Path-Acceptance.md) for current policy, playtest and hosted CI evidence.

The [new level-10 fixture](../LL/tools/BalanceHarness/Fixtures/idle-crystal-creek-starter.json) retains Goblin Warrior, Shortsword and Heavy Breastplate. Its primary `quest-rewards` build adds one Amulet and Goblin in the second Essence slot. These choices are fixed before outcomes and reuse the existing First Hunt cohort's Amulet/Goblin convention. The Heavy Breastplate and Amulet are possible reward outcomes, not guaranteed selections. This is one attainable conditional build, not a claim about every random chest result or the best second Essence.

- [Blood in the Grove](../LL/src/API/API.LL/Data/quests/region-01/blood-in-the-grove.v4.json) requires four Blood Grove victories and character level 10, then grants one Jewelry Chest and one Blood Grove Essence Token. Crystal Creek's authored gate requires this quest and level 10.
- [Trial of Lumo](../LL/src/API/API.LL/Data/quests/region-01/trial-of-lumo.v4.json) previously grants one Lumo Token. Its [selection catalog](../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/CatalystSelectionCrateCatalog.cs) includes Goblin. The [slot rule](../LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs) unlocks the second slot at level 10. Keep Goblin Warrior in the first slot and retain the Blood Grove Token.
- The [random box catalog](../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/RandomEquipmentBoxCatalog.cs) grants one common tier-1 rank-0 jewelry item. Amulet is a legal Necklace outcome. All selected equipment remains Standard with roll multiplier 1.0; Essences remain level 1, unascended and unevolved.
- [Soul Archive](../LL/src/API/API.LL/Data/quests/onboarding/soul-archive.v3.json) guarantees one Fury Blueprint and 500 Cinders. [Fury conversion](../LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json) costs 100 Cinders at tier 1, leaving 400; the [style catalog](../LL/src/API/API.LL/Data/equipment/equipment-styles.v1.json) permits Shortsword. This is an equipment style, separate from Combat Styles, which remain absent from every build.
- [Reinforcement prices](../LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json) charge 11,150 Cinders and five Parts for the first rank of one tier-1 item. This exceeds the guaranteed starting resources. Random rewards may accumulate while leveling, but their amounts and timing are not established here, so reinforcement and extra drops are excluded from the default checkpoint.

| Preparation step, all at level 10 | Equipment | Essences | Guaranteed resources spent |
| --- | --- | --- | --- |
| `level-only` | Original Shortsword + Heavy Breastplate | Goblin Warrior | None; new rewards retained |
| `jewelry` | Add Amulet | Goblin Warrior | One Jewelry Chest |
| `quest-rewards` — primary | Same three items | Add Goblin in slot two | Jewelry Chest + Lumo Token |
| `fury` — optional upgrade | Apply Fury to Shortsword | Same two Essences | Above + one Fury Blueprint + 100 Cinders |

At the original experiment's completion, no level-10 clear-rate or pacing target was approved. The subsequent decision above establishes a separate 50–90% policy for the primary Crystal Creek cells; stronger Blood Grove return visits remain diagnostic. Enemy coefficients, rewards and the larger regional offense step remain unchanged. Across-area differences include different species, abilities and scaling, so they are descriptive; the coefficient ratio alone is not a measured difficulty ratio.

## Protocol fixed before battles

Run all four builds against both fixed pairings in each area: Blood Grove Ravens / Raven + Blood Zombie, and Crystal Creek Blue Slimes / Blue Slime + Frost Imp. There are **16 cells**. Run **500 trials per cell on each of two fresh master seeds, 618091 and 618092: 16,000 battles total**. Report both seed sets separately, without pooling, selection, coefficient changes or sample extension. Small workflow checks use seeds 618093/618094. The earlier level-5 results supply context only and are not rerun or pooled.

Each area/encounter shares seeds across its preparation steps. Report per-cell pointwise 95% Wilson win-rate intervals, losses/draws, victory/non-victory duration, and paired win-rate changes between adjacent preparation steps. Compare the two areas descriptively, preserving ordinary regression compatibility rules. Predeclare eight detailed replays: confirmation trial index 0 for `quest-rewards` and `fury` against all four encounters. Reject incomplete or changed evidence and keep the source/fixture/settings/executable identities. No goal evaluation or baseline acceptance is performed.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-handoff --output TestResults/balance/crystal-creek-handoff-001
```

Build Release first, then use a new output directory. Default `--samples 500` runs the full declared budget; `--samples 1` runs 32 workflow battles on the development seeds. Preserve the plan, captured economic sources and fixture, both verified run bundles, paired findings and eight replays.

## Completed comparison

The declared run completed once at `TestResults/balance/crystal-creek-handoff-reference`: **16,000 valid battles**, zero invalid/cancelled/unexecuted battles and zero draws. Both full seed sets completed, all eight preselected detailed replays matched, and all captured sources, fixture, settings and executable identities stayed consistent. No coefficient, recipe or sample count changed after outcomes.

The primary `quest-rewards` build is viable in all four tested pairings. Its confirmation results are:

| Level-10 encounter | Reference wins / 500 | Confirmation wins / 500 | Confirmation win rate and pointwise 95% interval | Median victory | p90 victory |
| --- | ---: | ---: | --- | ---: | ---: |
| Blood Grove: Raven + Raven | 500 | 500 | 100% [99.24–100%] | 39.1 s | 42 s |
| Blood Grove: Raven + Blood Zombie | 500 | 500 | 100% [99.24–100%] | 54 s | 57 s |
| Crystal Creek: Blue Slime + Blue Slime | 496 | 497 | **99.4% [98.25–99.80%]** | **108 s** | 126.1 s |
| Crystal Creek: Blue Slime + Frost Imp | 500 | 500 | **100% [99.24–100%]** | **59.7 s** | 65.1 s |

The independent reference set supports the same pattern: 99.2% Slime-pair clears (97.96–99.69%), with a 104.55-second median victory, and 100% mixed clears. Keep these sets separate. The high level-10 rates do not pass or fail the distinct level-5 band, and they are not a whole-area estimate.

### What preparation changes

| Preparation | Two Slimes: reference / confirmation wins, each out of 500 | Slime + Imp: reference / confirmation wins, each out of 500 |
| --- | --- | --- |
| Level 10, original two items and Goblin Warrior | 0 / 0 | 0 / 0 |
| Add Amulet | 1 / 0 | 113 / 123 |
| Add Goblin in the second slot — primary | 496 / 497 | 500 / 500 |
| Add Fury to Shortsword | 498 / 499 | 500 / 500 |

**Equipping Goblin in the second slot is the decisive tested step for this build.** In confirmation, adding it to the Amulet build gains 99.4 percentage points against two Slimes (paired interval 97.00–99.82 pp) and 75.4 points against Slime + Imp (69.85–79.45 pp). The experiment does not show that any second Essence would have this effect. The Amulet alone lengthens survival substantially but does not make the Slime-pair checkpoint reliable; its mixed-pair confirmation clear rate is 24.6% (21.03–28.56%).

Fury is optional for the demonstrated path. Its confirmation Slime-pair result rises from 497 to 499 wins, an observed paired gain of 0.4 pp whose interval overlaps zero (−0.90–1.69 pp). Median winning duration drops from 108 to 99.1 seconds against two Slimes and from 59.7 to 56 seconds against the mixed pair. These conditional victory-duration summaries are descriptive; no pacing pass/fail policy is approved.

Every preparation step wins all Blood Grove trials at level 10. The original two-item build has median victories of 52.1 / 78 seconds; the second Essence reduces them to 39.1 / 54 seconds. Thus leveling supports the return visit, but the same underprepared build loses every Crystal Creek trial. Actual species and preparation matter more to this comparison than interpreting the 86.33% regional offense step alone.

The subsequent review approves wins as the sole target, so fight duration does not require acceptance. The fixed reward build demonstrates a viable route through these encounters, but wins too often under the new policy. Local UI checks confirm the second-slot unlock, token selection, reward claim and area access; whether unguided players notice and use that slot remains a human-review question. Broader chest outcomes, alternate token choices, full natural spawn coverage and actual acquisition time remain unmeasured. The original level-5 Raven assessment remains Inconclusive and no baseline is promoted.

## Evidence and verification

The ignored local artifact directory retains the pre-run plan, full source fixture, 22 captured combat/economic data files, both verified suite bundles, 32 per-cell findings, 24 adjacent-step paired comparisons and eight detailed replays. Retain the complete directory; it is not guaranteed to exist in another checkout.

| Evidence | SHA-256 |
| --- | --- |
| `plan.json` | `3a9fa38cfd508c7244730e5f502a957ac9f8802b01c0ec327942caf40cf0a0d5` |
| `results.json` | `881e5ddd910eceb5875bd52c88833e0f92cc8862c10905c404dd8514acec0d4e` |
| Fixture contract | `63a44203a7c787d7f0627205a56dd7ff91775bc17a9873bfa38e671d536e41ec` |
| Verified reference bundle fingerprint | `a3c9448a0e916d0756214c6b518150832bde5fcdff4cf49fad88ca056b7db2cf` |
| Verified confirmation bundle fingerprint | `0b003de2bd15df13b169c4a837e39a49a7275be8b646902ba62b74b4460c0939` |

The manifest pins .NET 10.0.11 on Windows X64 and five assembly hashes. The combat service assembly remains `384091b5cd88c5c6c1edc8396f020ce4fc4a60cb570c0e02ceaa940e4b227432`; the new harness is `128513c346d6f82f1baca1239fb821475443ce32f4b281f9e2f1b355a5851888`. Regional content stays at version 12 and SHA-256 `2492fc00c9cc9e92cae562b7b2fd45a3ccc94f8b10691a8d070557420bec16ef`. The earlier Blood Grove local plan/result file hashes remain unchanged; no level-5 reference trials were added.

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore` passed with four existing warnings and no errors. An initial new-test call to an internal settings helper was corrected to read the selected settings independently, matching the existing test pattern.
- **All 96 harness tests passed** via `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. The eight added cases cover authored rewards/gates, actual Shortsword Forge-quote equivalence, paired schedules and separate development seeds, complete saved/replayed evidence, output preservation, cancellation/changed-source rejection, and four independent production idle parity cases for the primary/Fury Crystal Creek encounters.
- `dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll investigate-handoff --output TestResults/balance/crystal-creek-handoff-reference` completed the fixed 16,000-battle run and all eight replays. No new reference samples were added after the result.
- Scoped `git diff --check` and whitespace checks for all four new files passed. All 183 local links across the 13 balance documents, tool README and roadmap resolved. Final executable and regional-content hashes still matched the retained evidence.
- Full backend tests, standalone cohort smoke scripts, frontend tests, hosted CI and human playtesting were not rerun for this offline fixture/tool increment. No required command remains blocked. Production combat/reward data, configuration, migrations and deployment are unchanged by this increment; no external environment was modified.

## Playtest checklist and observed coverage

The [subsequent local playtest](Starter-Path-Acceptance.md#local-playtest) exercised this path through the UI on a disposable character. XP and the fixed Amulet were prepared explicitly; the naturally awarded Relic was recorded. Encounter counters and selected detailed results were captured, not every row requested below. This verifies integration and leaves the original human-experience questions open. The optional Fury branch was not live-tested.

Use a local test build containing regional content version 12. Record the actual build/version, character level, Essence slots, item ranks/styles and quest state before each checkpoint. If the character's random chest outcomes differ, record them and treat that session as a different build. Do not present a prepared level-10 character as evidence of how long progression takes.

1. At Blood Grove entry, use the selected level-5 Goblin Warrior, Shortsword and Heavy Breastplate. Observe five consecutive ordinary encounters. Record each enemy group, win/loss/draw, duration, health left and whether a loss explains what went wrong. Keep the build fixed during this set.
2. At level 10 after claiming Blood in the Grove, equip the fixed Amulet outcome and Goblin from the retained Lumo Token. Check that the second slot, reward claiming and Crystal Creek access work. Revisit Blood Grove for five consecutive encounters, then enter Crystal Creek for five. Observe whether progression feels useful and whether the new area's pressure is understandable. These small, naturally spawned sets check experience; they do not estimate the approved win-rate band.
3. Before making another change, identify the next useful option from the game UI. If testing Fury, record the Forge quote, apply it to the Shortsword in a separate build, and observe another five Crystal Creek encounters. Confirm the price is one Blueprint and 100 Cinders, the change survives reload, and the result communicates the effect. Record reinforcement's cost without assuming it is affordable.
4. Record actual rewards gained, elapsed leveling time if observed, any unplanned gear/training/Combat Style changes, and where the player becomes uncertain about the next action. Preserve losses and the first impressions; do not restart until a favorable set appears.

| Checkpoint | Actual loadout / quest state | Groups and W/L/D | Durations / health left | What caused losses? | Is the next useful action clear? |
| --- | --- | --- | --- | --- | --- |
| Blood Grove entry, level 5 | Fixed starter equipped; four-win objective completed | Counter showed 4 W / 8 L before stopping; partial groups in acceptance review | Captured examples in acceptance review | Blossom-pair loss observed; no complete causal diagnosis | Quest and level requirement displayed; subjective clarity open |
| Blood Grove return, level 10 | Prepared Amulet, Goblin in slot 2; 1,077 HP | 6 W / 0 L; partial groups recorded | Captured wins retained 779–792 HP | No observed losses | Overview directed the character to Crystal Creek |
| Crystal Creek entry, level 10 | Same build, no Fury/Combat Style; area accessible | 6 W / 0 L; partial groups recorded | Captured wins retained 559–977 HP | No observed losses | After five wins, overview directed leveling to 15 |
| Optional Fury comparison | Not live-tested; separate offline evidence above | — | — | — | Human review remains optional |

The reviewed target is now 50–90% wins, with no duration requirement. Human acceptance of entry readiness and guidance remains separate from the recorded UI checks. Player observations do not silently change the existing level-5 statistical assessment or the level-10 policy failure.
