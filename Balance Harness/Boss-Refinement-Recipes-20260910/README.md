# Exact boss-refinement recipes

These three complete Tower scenarios are unchanged byte copies of frozen confirmation recipes from the [refinement campaign](../../TestResults/balance/tower-boss-refinement-20260910/). Each JSON contains the full required party, ordered mutable Essences, unchanged identity Essences, every equipment item and style flag, character progression, preparation assumptions and all 40 target confirmation seeds. Treat each party as a complete experiment; changing order, identities, teammates, gear or preparation changes its inputs.

| Recipe | Role in the study | Fresh target result |
| --- | --- | --- |
| [Eydis frozen primary](eydis-five-slots-frozen-primary.json) | Primary selected and frozen from discovery | 40/40; anchor 2/40 |
| [Nhalia frozen primary](nhalia-seven-slots-frozen-primary.json) | Primary selected and frozen from discovery | 1/40; anchor 0/40 |
| [Nhalia exploratory six-clear recipe](nhalia-seven-slots-exploratory-six-clears.json) | Exploratory maximum noticed after confirmation | 6/40; anchor 0/40 |

The Eydis result describes this recorded schedule, not a guaranteed clear rate. Its nominal pointwise 95% Wilson interval is 91.2–100%. Nhalia remains unreliable: the frozen primary interval is 0.4–12.9%, and the exploratory interval is 7.1–29.1%. These intervals have no multiplicity adjustment. The exploratory recipe does not replace the frozen Nhalia primary and is not a new recommendation or an optimum.

The exploratory candidate `813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1` and its slot-order counterpart `2ef1b65bfa0b7e7cb1a77660ac2bf8122bfb670e6e5e8074c876772d0f2ae20a` have identical recorded outcomes on every canonical floor/context, confirmation fitness and behavior. The counterpart only swaps character 1 Essence positions 3 and 4. Both used the same 40 target seeds: the evidence stays **6/40**, not 12/80 or two independent confirmations. Their agreement does not establish that Essence order is generally irrelevant.

**Budgets and required party**

| Target | Required characters | Essences per character | Character level | Tier / rank | Gear rarity / quality |
| --- | ---: | ---: | ---: | --- | --- |
| Eydis, floor 7 | 5 | 5 | 40 | 1 / 2 | Uncommon / Standard |
| Nhalia, floor 13 | 10 | 7 | 60 | 2 / 3 | Uncommon / Fine |

All Essences are level 1, unascended and unevolved; ownership is hypothetical. Combat Styles are disabled (`activeStyleId: null`, `useNativeStyle: false` on every item). Attribute roll multiplier is 1. Preparation is `uncleared-no-contributions`. Mutable party slots are 1–5 for Eydis and 1–10 for Nhalia; the existing `identityEssenceIds` arrays, build identities, level, tier, rank and equipment were preserved against the historical anchor recipe. Gear and identity details are authoritative in the linked full JSON files.

The fixed equipment pattern follows absolute character slots; Nhalia repeats slots 1–5 at slots 6–10. Every listed item ID has prefix `plain.` and suffix `.rarity.uncommon`.

| Character slots | Main hand | Chest | Head | Legs |
| --- | --- | --- | --- | --- |
| 1, 6 | maul | heavy_breastplate | heavy_helm | heavy_legplates |
| 2, 7 | staff | light_vest | light_hood | light_leggings |
| 3–4, 8–9 | gauntlets | light_vest | light_hood | light_leggings |
| 5, 10 | greatsword | medium_mail | medium_helm | medium_greaves |

Every character also has Ring `plain.band.rarity.uncommon`, Necklace `plain.amulet.rarity.uncommon` and Relic `plain.vial.rarity.uncommon`. Slots 6–10 apply only to the ten-character Nhalia party.

**Ordered Essences**

The rows below use exact definition IDs with the common `essence.` prefix omitted for readability. Left to right is the saved Essence slot order; character numbers are absolute party slots. Historical role names in build IDs describe the fixed character templates, not measured mechanisms.

**Eydis frozen primary** — [eydis-five-slots-frozen-primary.json](eydis-five-slots-frozen-primary.json)

Candidate `68bcf1597a0cba4906df7986ec66583bfb2b4036963b0ebfa7168a20362e3873`.

| Character | Ordered Essence IDs (prefix `essence.`) |
| ---: | --- |
| 1 | `poisonous_rat` → `web_weaver_spider` → `elder_treant_thornstorm` → `gnoll_shaman` → `horned_wolf` |
| 2 | `venomous_spiderling` → `spider_queen_royal_venom` → `nightshade_blossom` → `web_weaver_spider` → `blue_slime` |
| 3 | `giant_worm` → `enchanted_fairy` → `bog_mite` → `rainbow_slime` → `illusion_fox` |
| 4 | `poisonous_rat` → `nightshade_blossom` → `alpha_wolf` → `elder_treant_thornstorm` → `green_slime` |
| 5 | `enchanted_fairy` → `frost_imp` → `goblin` → `giant_bat` → `hollow_stag` |

**Nhalia frozen primary** — [nhalia-seven-slots-frozen-primary.json](nhalia-seven-slots-frozen-primary.json)

Candidate `7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e`.

| Character | Ordered Essence IDs (prefix `essence.`) |
| ---: | --- |
| 1 | `enchanted_fairy` → `flame_harpy` → `plague_ghoul` → `blue_slime` → `blood_harpy` → `venomous_spiderling` → `venomous_snake` |
| 2 | `enchanted_fairy` → `lumo_sentinel` → `alpha_wolf` → `ravenous_ghoul` → `cinder_beetle` → `web_weaver_spider` → `ice_harpy` |
| 3 | `blood_harpy` → `goblin` → `lumo_sentinel` → `goblin_archer` → `shadow_harpy` → `plague_ghoul` → `spider_queen` |
| 4 | `gnoll_pack_leader` → `flame_imp` → `ravenous_ghoul` → `poisonous_rat` → `raven` → `lumo_sentinel` → `blood_zombie` |
| 5 | `spider_queen_royal_venom` → `moss_lizard` → `shadow_harpy` → `frost_imp` → `glade_panther` → `flame_imp` → `enchanted_fairy` |
| 6 | `enchanted_fairy` → `venomous_snake` → `goblin` → `lumo_sentinel` → `poisonous_rat` → `alpha_wolf` → `rainbow_slime` |
| 7 | `web_weaver_spider` → `blue_slime` → `hobgoblin_brutal_charge` → `wandering_ghost` → `hollow_stag` → `green_slime` → `giant_bat` |
| 8 | `viper` → `blue_slime` → `dire_wolf` → `treant_sapling` → `grave_wisp` → `transparent_slime` → `undead` |
| 9 | `forest_spirit` → `viper` → `grave_wisp` → `goblin_warrior` → `blackjaw_spider` → `glade_panther` → `vampire_bat` |
| 10 | `elder_treant_thornstorm` → `hobgoblin` → `hollow_stag` → `thornback_boar` → `vampire_fledgeling` → `goblin_archer` → `treant_guardian` |

**Nhalia exploratory six-clear recipe** — [nhalia-seven-slots-exploratory-six-clears.json](nhalia-seven-slots-exploratory-six-clears.json)

Candidate `813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1`.

| Character | Ordered Essence IDs (prefix `essence.`) |
| ---: | --- |
| 1 | `enchanted_fairy` → `flame_harpy` → `spider_queen_royal_venom` → `blue_slime` → `blood_harpy` → `venomous_spiderling` → `thornback_boar` |
| 2 | `enchanted_fairy` → `lumo_sentinel` → `alpha_wolf` → `ravenous_ghoul` → `cinder_beetle` → `web_weaver_spider` → `ice_harpy` |
| 3 | `blood_harpy` → `goblin` → `lumo_sentinel` → `plague_ghoul` → `shadow_harpy` → `goblin_archer` → `spider_queen` |
| 4 | `gnoll_pack_leader` → `flame_imp` → `ravenous_ghoul` → `poisonous_rat` → `raven` → `lumo_sentinel` → `blood_zombie` |
| 5 | `giant_worm` → `goblin_warrior` → `shadow_harpy` → `frost_imp` → `glade_panther` → `cave_bat` → `enchanted_fairy` |
| 6 | `enchanted_fairy` → `venomous_snake` → `goblin` → `lumo_sentinel` → `poisonous_rat` → `alpha_wolf` → `rainbow_slime` |
| 7 | `web_weaver_spider` → `blue_slime` → `hobgoblin_brutal_charge` → `wandering_ghost` → `hollow_stag` → `green_slime` → `giant_bat` |
| 8 | `viper` → `blue_slime` → `dire_wolf` → `treant_sapling` → `grave_wisp` → `transparent_slime` → `undead` |
| 9 | `forest_spirit` → `viper` → `grave_wisp` → `goblin_warrior` → `blackjaw_spider` → `glade_panther` → `vampire_bat` |
| 10 | `elder_treant_thornstorm` → `goblin_archer` → `hollow_stag` → `thornback_boar` → `vampire_fledgeling` → `goblin_warrior` → `treant_guardian` |

**All-floor transfer evidence**

Each cell below is clears out of 40 fresh paired encounters. “Best retained” is the highest observed count among all retained references for that exact floor/context; different rows may use different references. It is a descriptive comparison, not a newly selected universal reference. The [provenance record](provenance.json) lists one matching reference ID per cell, the number of references tied at that best count and paired gained/lost counts. Historical pilot observations are separate and are not pooled into these counts.

B means the balanced ally context. P means `previous-05`, the frozen previous-five ally context. Only distinct canonical contexts are shown: aliased contexts reuse the same observations and do not increase sample size. Each transfer cell uses its own archived scenario and the floor’s required party/ally context; the three copied JSON files here are target-floor recipes. Do not reproduce this matrix by merely changing `floorNumber` in a target recipe.

Eydis improves floor 8 B from the anchor’s 9/40 to 34/40, but a retained reference reaches 40/40 there. It also loses on floor 8 B, floor 9 B, floor 10 P and both floor 11 contexts against at least one retained reference. Floor 11 is 0/40 against a retained best of 28/40 B and 36/40 P. These are material transfer weaknesses.

| Floor / context | Eydis primary | Fresh anchor | Best retained |
| --- | ---: | ---: | ---: |
| 1 / B | 40/40 | 40/40 | 40/40 |
| 2 / B | 40/40 | 40/40 | 40/40 |
| 3 / B | 40/40 | 40/40 | 40/40 |
| 4 / B | 40/40 | 40/40 | 40/40 |
| 5 / B | 40/40 | 40/40 | 40/40 |
| 5 / P | 40/40 | 40/40 | 40/40 |
| 6 / B | 40/40 | 40/40 | 40/40 |
| 7 / B | 40/40 | 2/40 | 2/40 |
| 8 / B | 34/40 | 9/40 | 40/40 |
| 8 / P | 40/40 | 40/40 | 40/40 |
| 9 / B | 39/40 | 36/40 | 40/40 |
| 9 / P | 40/40 | 40/40 | 40/40 |
| 10 / B | 0/40 | 0/40 | 0/40 |
| 10 / P | 0/40 | 0/40 | 4/40 |
| 11 / B | 0/40 | 1/40 | 28/40 |
| 11 / P | 0/40 | 8/40 | 36/40 |
| 12 / B | 0/40 | 0/40 | 0/40 |
| 12 / P | 0/40 | 0/40 | 0/40 |
| 13 / B | 0/40 | 0/40 | 0/40 |
| 13 / P | 0/40 | 0/40 | 0/40 |
| 14 / B | 0/40 | 0/40 | 0/40 |
| 14 / P | 0/40 | 0/40 | 0/40 |
| 15 / B | 0/40 | 0/40 | 0/40 |
| 15 / P | 0/40 | 0/40 | 0/40 |

Both Nhalia recipes clear floors 1–11 on the shown schedules but remain weak on floors 12–15. The frozen primary has 3/40 on floor 12 versus the exploratory recipe’s 1/40; both have 0/40 on floor 14 where a retained reference has 2/40. The primary and exploratory recipe each have only 1/40 on floor 15 P. Their target gains do not establish broad reliability.

| Floor / context | Nhalia frozen primary | Nhalia exploratory | Fresh anchor | Best retained |
| --- | ---: | ---: | ---: | ---: |
| 1 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 2 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 3 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 4 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 5 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 6 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 7 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 8 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 9 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 10 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 10 / P | 40/40 | 40/40 | 40/40 | 40/40 |
| 11 / B | 40/40 | 40/40 | 40/40 | 40/40 |
| 12 / B | 3/40 | 1/40 | 0/40 | 1/40 |
| 13 / B | 1/40 | 6/40 | 0/40 | 0/40 |
| 14 / B | 0/40 | 0/40 | 0/40 | 2/40 |
| 15 / B | 0/40 | 0/40 | 0/40 | 0/40 |
| 15 / P | 1/40 | 1/40 | 0/40 | 0/40 |

**Evidence and reproducibility**

Both study contracts, historical anchors, four methods, three generation restarts, 64 new proposals per arm and complete finalist sets were fixed before confirmation. Discovery used 12 shared samples; confirmation used 40 fresh samples. Restarts and candidates share paired schedules. Their results cannot be pooled as additional independent evidence. Strategy labels such as `focused-progress` were frozen in discovery and do not prove a causal mechanism.

The [provenance record](provenance.json) records each candidate ID, first target trial, source recipe ID and path, exact byte hash, study evidence hashes, protocol identity and complete canonical transfer comparisons. This folder contains compact review copies; the campaign keeps all 95 frozen finalists in its recipe library; these three copies also match their corresponding audit exports byte for byte. Native verification and any ordinary `tower` replays belong to the campaign verification records. This guide does not claim that the exploratory six-clear recipe received a complete ordinary-runner parity check.

A replay of one complete copied recipe uses the captured campaign executable/content and a new output directory. Run from the repository root only when another 40 fights are intended:

```powershell
$campaignPath = 'TestResults/balance/tower-boss-refinement-20260910'
dotnet "$campaignPath/executable/BalanceHarness.dll" tower `
  --content-root "$campaignPath/frozen-root" `
  --scenario 'Balance Harness/Boss-Refinement-Recipes-20260910/eydis-five-slots-frozen-primary.json' `
  --output 'TestResults/balance/eydis-refinement-repeat'
```

This repeats the recorded seeds and checks reproducibility; it does not add fresh confirmation evidence. Use the matching Nhalia JSON for a Nhalia replay. Keep the captured executable, content, settings and recorded runtime/platform identity together; a current rebuild or changed content is a different execution context. The guide’s preparation itself ran no fights.
