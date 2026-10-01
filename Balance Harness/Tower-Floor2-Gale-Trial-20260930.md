# Tower floor 2 Crimson Gale trial — 30 September 2026

**Subsequent result:** The [two-character armor search](Tower-Floor2-Two-Armor-Search-20260930.md) completed the reference measurement and supported search recommended below on the unchanged live catalog. It found two new actual compositions and retained **115 recipes / seven compositions**. Two-character nominees won **18/128 and 12/128**; the strongest full-armor recipe won **59/128**. Both acceptance gates failed; no confirmation or game change. Latest exclusions **914,265**. Continue from that complete 115-recipe source; preserve this Gale trial as rejected historical evidence.

Target: primary LL World Tower and offline Balance Harness. This is a separate prospective scope following the completed [damage diagnostic](Tower-Floor2-Damage-Diagnostic-20260930.md). The diagnostic verified 64 historical replays and identified the opening Dive/Gale sequence as pressure on insufficiently protected characters. Gale and its Bleed account for approximately 42% of first-15-second damage in both tested two-armored-character recipes. This motivates a controlled candidate; it does not predict acceptance.

## Frozen candidate and stopping rule

- Exactly one isolated candidate: Crimson Gale's direct Power coefficient **0.65 → 0.50**, with its description updated from 65% to 50%. Preserve its Bleed(10), cooldown, targeting and every other ability. Preserve floor-2 health **2.6618600366**, offense **2.5333570679** and every other guardian value. The smaller initial hit may leave weakened characters alive longer while preserving Velka's lowest-health hunting mechanic. This magnitude is a test choice, not an estimated optimum.
- Start from the complete current-catalog 98-recipe mixed-armor screen, manifest `0d0be643c70c46fae49e735c8d303571e3e59769892248e06d77e202e91bd3bc`. Keep all five actual compositions, 38 original recipes and 60 mixed variants, including every full-armor ceiling control. Preserve raw Essence order, IDs, positions, equipment and budgets. Use the existing narrowly validated ability-candidate mechanism; no source refresh, family expansion, search or scalar adjustment.
- One **128-seed screen / 12,544 fights**. Use approximate simultaneous 95% Bonferroni-Wilson bounds across all 98 recipes. Require every upper bound ≤50% and at least two distinct actual compositions with lower bound ≥10% using mixed variants with armor-and-health equipment on at most two of five characters. Integer screen gates: **25–44 wins / 128**. Repeated variants of one composition count once.
- Only if that complete screen passes, one independent **184-seed confirmation / 18,032 fights**, redeclaring the same candidate from the unchanged original source. Same family criteria; integer gates **33–68 / 184**. No extension, retry, second candidate, compensation parameter or redeclaration of the failed mixed-armor confirmation. A failed screen or confirmation closes this scope without a live edit.
- Retain the existing **20,000-fight / 840-second / 2-GiB** native phase caps and 900-second owner cap. Before each allocation, require twice measured source per-fight time/bytes to fit within 80% of the envelope. Initial exclusions **913,772**; maximum **312 fresh reservations / 30,576 fresh fights**. Diagnostic replay seeds are not new reservations. Freeze source, implementation, candidate, protocol and ledger pins before combat.
- If independently confirmed, apply only the exact two declared ability fields locally, run relevant backend checks through `build/run-tests.ps1`, and require all **18,032 native inputs / 98 complete replays** to match the accepted candidate. Do not deploy. If parity fails, preserve evidence and report it; do not present an unverified change as accepted.

Before execution, verify the existing 17 Python ability-candidate safeguards and the four Velka catalog/mechanic tests on the unchanged build. The prior diagnostic's 17 Python passes and the latest 97 backend passes / four opt-in skips remain separate evidence. The supported search, expected progression curve, eight-specialized-item target and withdrawal of guaranteed supplies stay unchanged.

## Completed result

**Closed `NoEligibleGaleConfirmation`.** The isolated 0.50 candidate completed all **12,544 fresh fights / 98 recipes / five actual compositions**. It established two qualifying mixed-gear compositions in this screen, but failed the retained-family ceiling. **No confirmation or live edit ran. Crimson Gale remains 0.65.**

| Best recipe | A wins / 128 | B wins / 128 |
| --- | ---: | ---: |
| One armored character | 14 | 17 |
| Two armored characters | 31 | 25 |
| Three armored characters | 51 | 51 |
| Four armored characters | 63 | 61 |
| All five armored | 75 | 65 |

Both two-character leaders use **slots 3+4**, with adjusted intervals **13.67%–39.22%** and **10.22%–34.09%**. They meet the declared minimum. The strongest full-armor control reaches **75/128 (58.59%)**, adjusted interval **43.37%–72.34%**, far above the required upper bound of 50% (maximum 44 wins). The second full-armor control reaches 65/128. Several three- and four-character variants also fail the ceiling. This is a complete-family failure despite improvement in the intended gear profile.

The earlier unchanged-catalog screen measured slots 3+4 at **12/128 and 7/128**; the new candidate measures **31/128 and 25/128** on independent fresh seeds. Those are separate panels, not paired observations or a formal estimate of the treatment effect. The candidate is promising for lower specialization but unsuitable for application as tested. The second partial composition only just meets its minimum while the strongest control substantially overshoots; a routine global difficulty increase has no demonstrated margin to solve both requirements.

Native execution completed in **132.66 seconds**, with zero retries. Resource admission projected 258.44 seconds and approximately 312 MB. Fresh reservations: **128**; final exclusion union **913,900**. All 102 live JSON catalogs, floor-2 scalars, live Gale coefficient, accepted floor-5/floor-6 changes and supported search remain unchanged. Preserve the failed candidate archive and every reservation; do not reuse its unused confirmation or promote its isolated content into an ordinary source.

## Next: supported composition search at two-character armor

Use the **unchanged 0.65 catalog** and `affinity-creation-with-benchmark-validation-v1` to seek stronger compositions at **`mixed-armor-baseline-slots-3-4`**, preserving the full-family ceiling. This targets the gap between partial and full armor without assuming that another small scalar adjustment will close it. The current live-catalog source has only two measured compositions at that exact profile; the supported search needs three.

A seed-free, unmeasured third-reference proposal is saved at `TestResults/tower-floor2-two-armor-reference-proposal-20260930.json`. It projects the strongest remaining distinct saved composition onto the exact slots-3+4 equipment template. The source composition's full-armor recipe won **24/128** in the unchanged source, but its new mixed recipe has **no measured outcome**. Preparing it would retain all 98 recipes and add exactly one, for **99 recipes / five actual compositions**. Count this as another gear variant, not a new composition.

Next, freeze resource limits and seed budgets, prepare that exact reference with the existing `--gear-reference` mechanism, and measure the complete 99-recipe family before starting the supported three-reference search. Retain every exact finalist, unsuccessful recipe and original full-armor control when screening the resulting family; recompute its family correction and admit its actual size against resource caps. Do not drop controls to fit the envelope. No next-study combat or seeds have been allocated.

Use original source `TestResults/tower-balance-pass-floor2-mixed-armor-screen-study-20260929`, manifest **`0d0be643c70c46fae49e735c8d303571e3e59769892248e06d77e202e91bd3bc`**. Source reference: `projected-reference/0f81ee3b078acb9d8ac3cc47a88286f77aeb6298eba531c9d1a29f249c4117ed`. Equipment template: `mixed-armor/92b0481cf6d81eac7f5c224cd99eca08de7fa53ec63c83de10882a0503ec4cf2`. Preserve raw Essence order, identity fields, positions and budgets. The rejected 0.50 study is evidence, not the future current-catalog source.

## Verification and evidence

**17 fresh ability-candidate safeguards and four fresh Velka backend tests passed** before this trial. Its owned native screen fixture also passed through `build/run-tests.ps1`. The preceding diagnostic separately passed 17 Python safeguards and verified 64 historical replays; the unchanged 97 broader backend passes / four skips were authenticated and reused. Combined new work in this continuation: **34 Python passes, four focused backend passes, one native study fixture, 64 diagnostic replays and 12,544 fresh study fights**. No verification remains blocked.

The independent collector authenticated the complete archive, recounted every saved outcome, checked exact candidate scope, all 98 simultaneous intervals, actual-composition counting, at-most-two-character eligibility, clean process completion, disjoint seeds and unchanged live data. There is no C# or live game-data edit, migration, configuration change or deployment. Maintained implementation changes are confined to the diagnostic helper, its expected-party-count parameter and tests; the ability trial uses the existing candidate mechanism. Both reports, prior-result notice, status, handoff and harness guides are updated.

- Trial evidence: `TestResults/tower-floor2-gale-evidence-20260930.json`, SHA **`891850818a13a354e17766e4f0ac249903809983bf19d37a9cab3f9fe560ab5b`**.
- Screen: `TestResults/tower-balance-pass-floor2-gale-screen-study-20260929`, manifest **`79f31e4dd386c28303c5bfe80f0a353ec36da50825649fe15dfe03b25b15ea5e`**; audit **`03c6db84962ec8f233b8e885f64455736e94864d9bacc9744e58179a3fbb7c96`**.
- Latest ledger: `TestResults/tower-balance-pass-floor2-gale-screen-owner-20260929/seed-ledger.json`, SHA **`3a754b297237ba5adce461f26512be3064f7bf4d60076f1bfa48381c9b79d35c`**. Retain all ancestors and later reservations.
- Control: `TestResults/tower-floor2-gale-driver-20260930`; candidate, frozen protocol, declaration, resource admission, command, logs, TRX, assessment and completion remain immutable.
- Publication: `TestResults/tower-floor2-diagnostic-gale-publication-check-20260930.json`.
- Unmeasured third-reference proposal: `TestResults/tower-floor2-two-armor-reference-proposal-20260930.json`, SHA **`c176cfa33286bbe7d1780dd32926913ecb70aaf203d915fa3e7fb561f606bf6c`**. It adds no fight, seed or new actual composition.
