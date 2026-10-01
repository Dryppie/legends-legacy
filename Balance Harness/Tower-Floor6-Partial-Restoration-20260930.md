# Floor 6: partial healer Restoration — 30 September 2026

**Subsequent result:** The [four-piece composition search](Tower-Floor6-Four-Piece-Search-20260930.md) found two new teams and expanded the family to **116 recipes / seven compositions**. New four-piece teams won **38/128 and 46/128**; the strongest full-Restorer version reached **60/128**. Both new teams cleared the lower-bound minimum, but the full-family ceiling failed, so no confirmation or game change followed. Continue from that measured 116-cell source with a separately declared health calibration; the search recommendation below has now been completed.

Target: primary LL World Tower and offline Balance Harness. This prospective comparison follows the [saved gear assessment](Tower-Floor6-Gear-Assessment-20260929.md). It tests less specialized healer equipment at the unchanged Orsenn health/offense settings **6.153125 / 4.365625**. It does not change game content or the supported search algorithm.

## Frozen protocol

1. Authenticate the historical 38-recipe confirmation (manifest `c628d02858d310977e778410be51d2f32c4cb71e35391dc216156a7d0604e924`) and the accepted floor-5 application. Explicit qualification requires the accepted current catalog, identical target-floor definition and settings, and the declared build. Match **9,728 historical combat inputs and 38 complete replays** before admitting new recipes. No fresh seeds are allocated for qualification; historical archives remain untouched.
2. Import the prepared proposal SHA `e71b8bdda74e400d331ee639ffb4305cfa095ede3793d52e3d26464a24491219`: retain all **38 original recipes**, plus all 15 two-piece and 15 four-piece subsets of the six healer Restoration slots for each of the two established viable compositions. Exactly **98 unique recipes**, still **five actual compositions**. Preserve raw Essence order, identities, party positions and all other equipment. The healer is party slot 2; slots are MainHand, Chest, Head, Ring, Necklace and Relic. Other four characters retain their original gear.
3. Use the unchanged floor-6 budget: five level-40 characters, five level-1 unascended/unevolved Essences each, tier-1 Epic/Fine/rank-3 equipment, rolls 1, no styles. Prepare and validate the whole family without combat.
4. Freeze **128 fresh screen seeds** shared across all 98 recipes (**12,544 fights**). Approximate simultaneous 95% Bonferroni-Wilson bounds use all 98 recipes. Every upper bound must be at most 50%; at least two actual compositions must each have a partial-gear recipe whose lower bound is at least 10%. Actual compositions use per-slot Essence sets; gear variants and identity aliases do not add compositions.
5. Only an eligible screen admits **184 independent confirmation seeds** for all 98 recipes (**18,032 fights**), with the same family-wide ceiling and two-composition partial-gear minimum. Freeze the decision before allocation. No family reduction, pooling, retries, extensions or second confirmation. On a passing confirmation, verify every confirmed input and one full replay per recipe against the same current catalog/build. Otherwise close the scope and retain all results.
6. Each native phase is capped at **20,000 fights, 840 seconds and 2 GiB**, with a bounded **900-second** process owner. Before allocation, projected time and storage must stay below 80% of the envelope, using twice the preceding measured cost. Reserve every seed, including unused reservations, against the complete ledger union (initial **911,919**). Source/runtime/implementation pins and declarations are frozen before combat. Legacy balance-owner directory suffixes remain `20260929`; this scope executes on 30 September.

Maximum fresh study fights: **30,576**, with **312 fresh reservations**. Qualification uses 38 historical-seed replays; passing confirmation adds 98 more. Backend regression combat is separate from study panels. A failed implementation/build can be repaired in fresh receipts before scientific execution; failed scientific panels cannot be retried.

Success would establish viable exact loadouts with fewer specialized items under the declared inventory assumptions. It would not establish ordinary acquisition rates, an approved pacing target or universal balance. Failure should identify the closest partial loadouts and inform a supported composition search before considering an Orsenn damage edit. The accepted floor-5 change and withdrawal of guaranteed selectable supplies remain in force.

## Results

**Completed: `NoEligibleConfirmation`.** The entire 98-recipe screen completed **12,544 fresh fights** with zero retries. Current-build qualification matched **9,728 historical inputs and 38 full replays**. Partial four-piece Restoration was promising for one composition, but the second composition missed the minimum and the retained full-Restorer leader exceeded the adjusted ceiling. The declared rule therefore prevented confirmation. No Orsenn, game catalog or search-policy change followed.

The exact 98-family screen gates were **25–44 wins / 128**; the unused confirmation gates were **33–68 / 184**. Four partial recipes met the individual screen bounds, all belonging to composition A. No two-piece recipe met the minimum.

| Composition | Healer Restoration pieces | Best wins | Family-adjusted interval | Restoration slots of best partial |
| --- | ---: | ---: | --- | --- |
| A | 2 | 5/128 (3.91%) | 0.94–14.82% | MainHand, Necklace |
| A | 4 | 29/128 (22.66%) | 12.50–37.53% | MainHand, Chest, Head, Necklace |
| A | 6 | 46/128 (35.94%) | 23.01–51.29% | Full retained Restorer profile |
| B | 2 | 4/128 (3.13%) | 0.65–13.68% | MainHand, Chest |
| B | 4 | 12/128 (9.38%) | 3.63–22.13% | MainHand, Chest, Head, Necklace |
| B | 6 | 32/128 (25.00%) | 14.26–40.05% | Full retained Restorer profile |

A is original composition `8801c2d4e8e92f468f83cfca0200cb49fbafc80717872215b161e26ad0096d66`; B is `7f859e243932b4d1737e0a3e41f22d35394e19c64163dcf55154bdf4b2cea0c5`. The other qualifying A four-piece variants won **28, 28 and 27 / 128**. These are gear variants of one actual composition, not four new teams. The 51.29% upper bound prevents this screen's acceptance; the observed strongest win rate was 35.94%, not above 50%. Do not pool this panel with the older 256-seed confirmation or call the partial variants independently confirmed.

The 38-recipe historical accepted family remains preserved and now has explicit current-runtime parity. The 98-recipe screen is a measured continuation source, not a newly accepted family. Initial exclusions **911,919**, final **912,047**: exactly **128** new reservations. No 184-seed confirmation panel or final 98-replay application check was allocated. Qualification plus the screen used **12,582 battle executions**, excluding backend regression combat.

## Implementation and verification

- Added `analysis/tower-catalog-qualification.py` and its 11 guard tests. Admission binds the historical source, applied confirmation receipts, unchanged target floor/settings, current catalogs, runtime and test assembly. It reconstructs every proposed subset and rejects changes to original recipes, raw identities, Essence order, budgets, subsets or unrelated catalogs.
- Extended `analysis/check-tower-balance-application.py` and `BalanceHarnessTowerBalanceApplicationTests.cs` with an explicit qualification plan. Ordinary application checks remain strict. Historical inputs and one complete battle per retained recipe must still match; qualification has a distinct status and never rewrites history.
- Extended `analysis/run-tower-balance-pass.py` with `--qualified-family`, restricted to seed-free floor-6 preparation with no other modifications. The original implicit catalog-refresh and rejected-candidate guards remain in place. Preparation imports the authenticated proposal only after replay qualification.
- **97 backend tests passed / four opt-in skips**, using `build/run-tests.ps1` with the Tower regression classes and all six Kharad cases. **42 Python tests passed**: 11 qualification, 14 reference/import and 17 ability-candidate. The qualification, preparation and screen each passed their owned native fixture. Every saved screen outcome, interval, family member, process receipt and seed reservation was independently reconstructed.
- The initial build was blocked from reading the user's NuGet configuration. The same wrapper completed with the required local access: **62 warnings, zero errors**. A pre-allocation validation attempt caught a mistaken Restorer profile label in the new helper; the label was corrected and all 11 guard tests passed before declaration/qualification. No scientific fight or seed was retried. The read-only collector was corrected to count skipped TRX test outcomes rather than the runner's zero-valued `notExecuted` counter. No verification remains blocked.

Screen native time was **128.99 seconds** (131.72 seconds including its bounded process), versus a conservative admission projection of 255.81 seconds. All owned processes exited successfully with zero active children. No migrations, configuration changes, databases, deployments or dungeon changes.

## Evidence and continuation

- Evidence: `TestResults/tower-floor6-partial-restoration-evidence-20260930.json`, SHA **`3430d7e4bf2a35216c99bb00777508023da8d19e308ab8cc11af7bd557f099e9`**.
- Frozen declaration, original protocol, qualification plan, resource admission, complete screen assessment and test receipts: `TestResults/tower-floor6-partial-restoration-driver-20260930/`.
- Qualification: `TestResults/tower-floor6-partial-restoration-qualification-owner-20260930/`; result SHA **`7fe7df27e933be8b28a4ce93643602e31ec736562e14a84a749dc35c123cacdd`**.
- Isolated runtime: `TestResults/tower-floor6-partial-restoration-build-20260930`.
- Preparation: `TestResults/tower-balance-pass-floor6-partial-restoration-preparation-study-20260929`, manifest **`6fc8d8a66f89fda9454594151a6d00cf5788b7ab1dcf84bb995e64284b32239c`**.
- Screen: `TestResults/tower-balance-pass-floor6-partial-restoration-screen-study-20260929`, manifest **`74af8b61be5b614a07f57543db972aa7f1cb133cc25796a9aa4d963f234032b5`**, audit **`50df73ce27769ed4f92e39b6767a4a9399b970215f6a345d3be492af3a627e69`**.
- Latest ledger: `TestResults/tower-balance-pass-floor6-partial-restoration-screen-owner-20260929/seed-ledger.json`, SHA **`de1472a073cb3e0fb6a4e32d053c49f7c1c9271af3ad6388c4465c26f586c277`**. Include every ancestor and later reservation.
- Live Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**. All 102 live JSON catalogs stayed unchanged.

**Recommended next work:** run a separately bounded supported composition search at the promising four-piece profile (MainHand, Chest, Head, Necklace), seeking a second strong actual composition. First prepare and measure a third distinct reference at that exact gear profile: this screen has only A and B measured there, while the supported search requires three measured parents. Preserve all 98 recipes, both full-Restorer ceiling controls, raw identities and prior failed scopes. Retain every exact search nominee and evaluate the expanded whole family before a fresh independent confirmation. Do not rerun this screen, lower its acceptance standard, alter the search algorithm, or weaken Orsenn simply to make existing recipes pass. The original accepted families and acquisition limitations remain distinct from this unsuccessful expanded-family screen.
