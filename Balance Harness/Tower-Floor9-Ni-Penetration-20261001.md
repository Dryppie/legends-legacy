# Floor 9: isolated Ni penetration trial — 2026-10-01

Candidate: guardian offense **4.7036132812 → 4.4684326171** (×0.95) and guardian penetration multiplier **1 → 40**. Expected native typed penetration: **0.96 → 38.4 percentage points**. Health, abilities, 10% copy Health, corrected copy defenses, cooldowns and every other catalog value remain unchanged. This is an isolated candidate, not applied game balance.

The candidate follows the corrected-runtime replay diagnosis: lowering offense alone helps heavily specialized Resistance parties too much. Penetration is intended to narrow that defense advantage while the small offense reduction preserves lower-defense parties' opportunities. This mechanism must be tested; it does not imply acceptance.

## Frozen protocol

- Original seed-free corrected-runtime source: `tower-balance-pass-floor9-summon-defense-preparation-study-20260929`, manifest `cc3a2dd3dc02383d061cc7e5b0829aa88ca71b8e0787dc23eb96caf73036661f`.
- All **148 raw recipes**, **five actual compositions**, **115 equipment-eligible recipes** retained in original order. Eligibility: at most eight specialized items on at most two characters.
- Screen: four batches of 32 shared seeds, **128 per recipe / 18,944 fights**. Confirmation is an independent equal-size panel and starts only after a complete passing screen.
- Approximate simultaneous Bonferroni–Wilson bounds over all 148 recipes, alpha 0.05: at least two distinct eligible compositions with lower bound ≥10%, and every recipe's upper bound ≤50%. At 128 samples, qualifying lower threshold is **25 wins**; ceiling threshold is **43 wins**.
- No interim acceptance, sample extension, seed replacement, retry, dropped controls or pooling of historical outcomes. Maximum 37,888 fresh fights / 256 reservations. Initial exclusions: **926,348**.
- Each batch: 20,000-fight, 840-second native, 900-second process-owner and 2-GiB limits. Admit only when doubled prior measured cost is below 80% of native time/byte limits and projected remaining disk plus 2 GiB is available.
- Before allocation, require native typed-damage/cap checks and all 148 original/candidate prepared pairs; only Ni Power, ArmorPenetration and MagicPenetration may differ. Reuse the authenticated corrected production assemblies and broader 766-pass / four-skip proof, retaining the known pre-existing Kharad manifest failure.
- Before any local application, require independent confirmation and complete native input/replay parity against the isolated candidate. No deployment or shared database changes.

Frozen proposal: `TestResults/tower-floor9-ni-penetration-trial-proposal-20261001.json`, SHA-256 `8fc8c16d5d9821b6d83d167d7603f6da337dc91fa8eea3e09e6e6c099206f5fd`.
