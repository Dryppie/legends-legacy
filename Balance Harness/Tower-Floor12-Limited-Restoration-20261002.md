# Floor 12: limited Restoration coverage — 2 October 2026

## Reason for the next step

The [0.525 pressure midpoint](Tower-Floor12-Pressure-Midpoint-20261002.md) closed without nomination: eligible leaders won 10/32, 7/32 and 6/32, while full twelve-item healer controls reached 16/32 and 15/32. The earlier 0.55 setting kept the strongest control to 7/32 but supplied no eligible composition at 6/32. These small exploratory panels do not prove exact win rates or that another Power value cannot work.

The complete current family contains **eleven raw full-Restoration controls across nine actual compositions**, but no six/eight-item Restoration subsets. Fill this equipment coverage gap before further fine Power tuning. It may offer viable expected-equipment routes at a setting where full controls remain below the ceiling; this is a hypothesis requiring fresh native testing.

## Saved exact family

[Proposal](../TestResults/tower-floor12-limited-restoration-proposal-20261002/proposal.json), SHA-256 `f1fc8de5a60957a9a8242f06816c11d431a681317264b8f532a41b2297c4b84a`.

Keep all **241 existing recipes**, including all 131 original controls, all 110 limited Resistance variants and both projected Restoration references. Add **three exact subsets for each of the nine original baseline/Restoration pairs**:

- Six saved Restoration items on healer slot 2.
- Six saved Restoration items on healer slot 7.
- Four saved items on each healer: Main Hand, Chest, Head and Necklace, eight total.

Total: **268 recipes / nine actual compositions / 155 equipment-eligible recipes**. The two projected references stay as controls and do not count as additional compositions or generate duplicate donor families. Parent selection uses every original paired composition, independent of its observed wins.

Each variant starts from its exact retained baseline and copies only those items from its saved full-Restoration counterpart. Preserve every actor, Essence, identity and item order, all other fields, level 60, tier 2, seven Essences, retained gear rarity/quality/rank and roll multipliers. Eligibility stays at **eight specialized items / two specialized characters maximum**. No guardian setting, ability or game content changes here.

## Implemented and checked

- [Proposal generator](analysis/tower-floor12-limited-restoration.py) reconstructs all raw recipes, authenticates the closed midpoint and original source, and rejects changed controls, donors, progression, ordering or status.
- [Twelve guard tests](analysis/test-tower-floor12-limited-restoration.py) pass.
- [Independent verification](../TestResults/tower-floor12-limited-restoration-proposal-20261002/verification.json) checked every changed field in all 27 real variants and exact retention of all 241 controls. All source pins remained unchanged.

The original proposal remains an immutable data-only artifact. The expanded family has now **prepared all 268 recipes natively** and passed the independent preparation audit: **zero fights, historical replays or new seeds**. The native fixture passed with zero skips. Native preparation took **5.62 seconds**; the complete owner, including accepted-catalog archive authentication, took **2,522.16 seconds**. [Preparation receipt](../TestResults/tower-floor12-restoration-preparation-20261002/completion.json), SHA-256 `4228488af4039d8c6e3c776b0746d5fd255bf045c838e3a922320a8b279586fe`; prepared manifest `f43d0836aa66ffbe3627263810bf4bc313c29c9db81dba5ab541c1b370d16a07`. Preparation closed at **928,892 exclusions**. This is not combat acceptance; the separate diagnostic is now in progress.

## Native integration and continuation

The separate [expanded-family binding](analysis/tower-floor12-restoration-binding.py) and catalog admission dispatch are implemented. Twelve new binding tests, thirteen integration tests and 133 existing regression checks pass (**158 total**), preserving the existing 241-recipe contract. Native preparation and full accepted-catalog authentication have completed successfully for all 268 recipes. The original compiled combat runtime and its 26 passing native guards are authenticated; no engine edit or rebuild is needed. Bind the saved proposal to the unchanged qualified original floor-12 source and authenticate current source, runtime and catalog pins. Prepare **all 268 recipes** natively through `build/run-tests.ps1`, independently verify raw/prepared identities, and keep all older studies and failed attempts intact.

A [fixed full-family diagnostic](Tower-Floor12-Restoration-Diagnostic-20261002.md) is now declared in Markdown and its driver passes ten native-comparison and seven nomination checks; its preparation prerequisite has passed and its one fixed run is in progress. It retains all 268 recipes at offense factor 0.55 / penetration 40, with 32 fresh seeds and 8,576 fights maximum. All native snapshot comparisons and a complete independent recount are mandatory. No extension, retry or second setting is included. The following was the original diagnostic planning guidance: The completed **0.55 Power / 40 penetration** setting is a reasonable starting hypothesis because its strongest old control was 7/32; it is not selected or accepted by this data-only proposal. Freeze setting, complete family, sample count, resources and nomination rule before combat, with no pooling, retries, extensions or dropped controls. Recalculate any later simultaneous acceptance thresholds for 268 recipes. No formal screen, confirmation or application is allocated.

Floor 10 remains locally applied and verified. Floors 13–15 retain their saved proposals and historical reviews; keep native work focused on floor 12 before the final 1–15 current-version sweep. No migration, configuration change or deployment is included.
