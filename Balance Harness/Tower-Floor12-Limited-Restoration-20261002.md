# Floor 12: limited Restoration coverage — 2 October 2026

**Subsequent formal acceptance result:** The [fixed Restoration acceptance study](Tower-Floor12-Fixed-Acceptance-20261002.md) **passed full screening and independent confirmation, and was applied locally**. Completed **274,432 fresh fights / 1,024 reservations**, independently recounted, retaining **268 recipes / nine actual compositions / 155 eligible recipes**. Screen: **Pass**, **5** eligible compositions qualify; largest adjusted upper **30.45%**. Confirm: **Pass**, **5** eligible compositions qualify; largest adjusted upper **29.82%**. Only floor-12 guardian offense **11.9 → 6.99125** and penetration **1 → 40** changed. Health **9.414125**, all abilities, every other floor, expected progression and raw loadout ordering remain unchanged. All **137,216 confirmation inputs / 4,288 complete historical replays** matched the isolated candidate bytes now used locally. Post-application preparation, **382 backend cases / zero skips**, and **252 Python tests** pass. Final exclusions: **930,044**. Floor 12 now passes this declared limited-equipment family gate. Next: floor **13**, then **14–15**, followed by the final current-version **1–15 sweep**. Qualify the saved later-floor proposals against the newly accepted floor-12 catalog and preserved combat runtime before native testing; do not repeat the completed floor-12 studies. No migration, environment configuration change or deployment.

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

The original proposal remains an immutable data-only artifact. The expanded family has now **prepared all 268 recipes natively** and passed the independent preparation audit: **zero fights, historical replays or new seeds**. The native fixture passed with zero skips. Native preparation took **5.62 seconds**; the complete owner, including accepted-catalog archive authentication, took **2,522.16 seconds**. [Preparation receipt](../TestResults/tower-floor12-restoration-preparation-20261002/completion.json), SHA-256 `4228488af4039d8c6e3c776b0746d5fd255bf045c838e3a922320a8b279586fe`; prepared manifest `f43d0836aa66ffbe3627263810bf4bc313c29c9db81dba5ab541c1b370d16a07`. Preparation closed at **928,892 exclusions**. This is not combat acceptance. Four subsequent diagnostics are complete; the fixed 0.5875 midpoint is nominated, as recorded below.

## Native integration and continuation

The separate [expanded-family binding](analysis/tower-floor12-restoration-binding.py) and catalog admission dispatch are implemented. Twelve new binding tests, thirteen integration tests and 133 existing regression checks pass (**158 total**), preserving the existing 241-recipe contract. The completed preparation reused the authenticated combat runtime and its 26 passing native guards; no engine edit or rebuild was required. Do not repeat the expensive historical archive authentication or native preparation.

Four separately declared diagnostics have now closed, each retaining all 268 recipes and using 32 fresh seeds. All outcomes, exact schedules, native participant invariance and 27 exact equipment variants were independently checked. The first three settings were not nominated; only the fixed midpoint passed the diagnostic rule. Results are separate and never pooled:

| Offense factor / penetration 40 | Eligible compositions at ≥6/32 | Maximum wins, any recipe | Nominated |
| ---: | ---: | ---: | --- |
| 0.55 | 9 | 17/32 | No |
| 0.575 | 5 | 14/32 | No |
| 0.60 | 0 | 5/32 | No |
| 0.5875 | 4 | 7/32 | Yes, diagnostic only |

This continuation completed **34,304 fresh fights / 128 reservations**, with five native preparation/study fixtures passing and zero skips. Twelve penetration guards passed before each diagnostic (48 executions). The midpoint also passed eight retained-participant/scalar mutation checks; its unchanged variant and nomination implementations reuse 17 authenticated guard checks. Every native variant was checked in each panel. Final exclusions: **929,020**.

The [fixed midpoint report](Tower-Floor12-Restoration-Midpoint-20261002.md) contains the complete nine-composition table. Its [formal acceptance proposal](Tower-Floor12-Fixed-Acceptance-20261002.md) holds offense **0.5875** and penetration **40** fixed, with all 268 recipes and fresh independent 512-seed phases. The strict aggregate/native application contract must be implemented and tested before allocation. **No formal acceptance fight or seed is allocated, and live floor 12 is unchanged.**

Verified native gear comparisons show why item count alone is insufficient: all eighteen healers in the eight-item setup retain **31.95 Critical Chance and 31.95 Ability Haste**, while the twelve-item profile has zero of both. Their Restoration is **426 versus 596.4**, with other combat attributes unchanged in those paired snapshots. This is a verified equipment tradeoff, not an isolated causal estimate. Preserve both profiles and every old control.

Floor 10 remains locally applied and verified. Floors 13–15 retain their saved proposals and historical reviews; finish floor 12 before later-floor native work and the final 1–15 current-version sweep. No migration, configuration change or deployment is included.
