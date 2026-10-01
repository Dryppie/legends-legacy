# Floor 10: isolated Power and penetration diagnostic — 1 October 2026

## Completed result

**Hold offense factor 0.50 and penetration factor 40 fixed for formal acceptance testing.** The diagnostic completed **4,448 fresh fights / 16 fresh seeds** across all **278 recipes / five actual compositions**. The two strongest limited-equipment compositions each reached **5/16**, both full-armor controls reached **4/16**, and the highest count anywhere was **6/16** on A/Precision. This is a promising candidate, not accepted balance; the diagnostic's selected leaders have selection bias and the sample is small.

| Recipe/control | Equipment | Wins / 16 | Mean guardian Health remaining |
| --- | --- | ---: | ---: |
| A: c01a33fe… | eight armor items, slots 3 + 9 | 5 | 10.08% |
| B: 52889519… | eight armor items, slots 9 + 11 | 5 | 10.51% |
| A | baseline | 4 | 11.97% |
| B | baseline | 2 | 14.42% |
| A | full armor, 60 items | 4 | 9.49% |
| B | full armor, 60 items | 4 | 9.83% |
| A | Precision, 30 items | 6 | 8.68% |
| retained composition 2 | baseline | 2 | 10.89% |
| retained composition 1 | baseline | 1 | 13.78% |
| authored composition | baseline | 0 | 45.02% |

Of the 245 equipment-eligible recipes, 37 won zero, 64 won once, 75 twice, 45 three times, 20 four times and four five times. All 38 original controls and 240 armor variants remain in the evidence. No prior counts were pooled, and no ceiling or viability acceptance claim is made from sixteen samples.

## Verification and local state

The independent collector recounted every outcome and each recipe's invariant native prepared participants. A separate check compared **all 278 native recipes / 4,170 friendly participants** with their originals: friendly participants are unchanged. Guardian Power is **2,701.2964 → 1,350.6482**; raw Armor/Magic Penetration is **1.2960001 → 51.840004**, capped at **40% when combat mitigation is calculated**. Every other guardian attribute and identity is unchanged. An initial ad-hoc check expected the cap in the raw prepared report; the corrected check distinguishes raw attributes from the combat cap and passes without replaying combat.

The exact isolated catalog delta is only floor-10 offense **7.13 → 3.565** and penetration **1 → 40**. Health remains **12.8371**, and the full ability kit—including the shared Ant King entries—is byte-identical. **Live floor 10 remains at offense 7.13 / penetration 1**. No local gameplay application, engine edit, migration, application configuration change or deployment occurred.

Native study time: **190.44 seconds**. **12 penetration rejection tests** and the native study fixture passed through `build/run-tests.ps1`; the preceding diagnostic added **71 Python checks**. The unchanged **315 backend checks / zero skips** were authenticated and reused. No verification remains blocked. Final exclusions: **927,676**. The broader historical Kharad behavior-manifest failure is still recorded and is not claimed fixed.

Evidence: `TestResults/tower-floor10-penetration-evidence-20261001.json`; native participant comparison: `TestResults/tower-floor10-penetration-native-stats-20261001.json`; declaration, protocol, commands, logs and TRX: `TestResults/tower-floor10-penetration-driver-20261001`. Source study: `TestResults/tower-balance-pass-floor10-penetration-diagnostic-study-20260929`. Latest publication: `TestResults/tower-floor10-pressure-publication-20261001/completion.json`. Preserve these ignored local archives when transferring work.

## Next: fixed candidate acceptance

The frozen, unallocated proposal is `TestResults/tower-floor10-fixed-penetration-acceptance-proposal-20261001.json`, SHA-256 `0c588e56db75fb38416b66f5e021ad89d18096d2711d0889957abd99c7cc2bd1`. Retain the original 278 recipes and use **512 fresh seeds per phase**, split into **32 batches of 16**. Complete screening first; independent confirmation runs only on a pass. Maximum **284,672 fresh study fights / 1,024 reservations** across both phases. Earlier diagnostic outcomes contribute no acceptance observations.

Keep the existing simultaneous 95% approximate Bonferroni-Wilson gates: at least two actual compositions using at most eight specialized items on at most two characters must have a lower bound of at least 10%; every recipe must have an upper bound no greater than 50%. At 512 observations across 278 recipes this means **at least 77 wins on two eligible compositions and no more than 213 wins on every recipe**, in both phases. No second candidate, extension or interim tuning.

The strict floor-10 aggregate/native application contract is **not implemented**, and no acceptance seeds are allocated. Add its rejection tests before running the proposal, retain existing earlier-floor contracts, and use the unchanged original source rather than modifying an already modified candidate. Each batch requires current catalog/runtime bindings and measured resource admission. Only confirmed acceptance plus full input/parity verification can precede a local gameplay edit. Floors 12–15 and the final current-version 1–15 sweep remain after floor 10.

## Frozen candidate and scope

Based on the completed [96-replay pressure diagnosis](Tower-Floor10-Pressure-Diagnostic-20261001.md), test one isolated floor-10 candidate: **offense factor 0.50** (7.13 → 3.565), **penetration factor 40** (1 → 40; native effective typed penetration is capped at 40%). Keep Health 12.8371, defenses, regeneration, all abilities, summons, progression and every raw loadout unchanged. These are Tower guardian scaling changes only. The Mad King's four abilities are also used by the Ant King; their shared catalog entries must remain byte-identical.

The rationale is to narrow the armor advantage while reducing physical pressure on baseline characters. For the saved initial Armor values, applying the existing capped mitigation rule gives approximate candidate/current damage ratios of **0.704 and 0.890** for light/heavy baseline characters and **0.979 and 1.036** for light/heavy full-armor characters. This ignores rounding, conditions, block and reaction feedback and predicts no win rate. Equipment also changes Health, block, regeneration and Tenacity, so this is not a one-stat gear experiment.

Use the existing isolated penetration contract from the unchanged, qualified 278-recipe source. One complete **16-fresh-seed panel across all 278 recipes / five actual compositions: 4,448 fights and 16 reservations maximum**. Retain all 38 original controls and 240 limited-armor variants. Initial exclusions: **927,660**. No new compositions, reordered Essences, dropped controls, interim choices, retry, extension, pooling, second candidate, acceptance, confirmation or application.

Before allocation, authenticate the closed pressure evidence, original source manifest, current catalogs and existing native regression proof. Run the existing penetration-candidate rejection tests. Use the measured preceding 8,896-fight panel to admit this half-sized panel with doubled time/bytes and the existing 80% admission margin: native 840 seconds, owner 900 seconds and 2 GiB output, plus 2 GiB free disk. The doubled time estimate is 339.56 seconds. Observe only supervising stdout while the native study runs.

After closure, independently recount every raw outcome, retain all native prepared participants, verify the exact isolated catalog delta, and verify all 16 seeds are fresh. Report limited-gear leaders and full-party controls separately. This is a diagnostic and cannot establish balance from 16 observations. Hold a promising candidate fixed for a separately declared full-family screen and independent confirmation; do not apply it locally from this panel. Preserve every failed attempt and its reservations.
