# Floor 9: fixed Restoration acceptance study — 1 October 2026

## Frozen protocol

The complete offense diagnostic nominated one setting: **original Ni offense ×1.00, penetration ×40**. Keep every original recipe in order: **163 raw recipes / five actual compositions / 130 equipment-eligible recipes**. Preserve minimum-level expected progression, the repeating gear curve, Essence/actor/equipment order, 10% copy Health, corrected inherited defense and the full kit.

Run **eight 64-seed batches for screening** (512 samples per recipe, 83,456 fights). Only a complete passing screen permits **eight independent 64-seed confirmation batches**. Both phases were specified before allocation: maximum **166,912 fresh fights / 1,024 reservations**. Exclude all **926,604** preceding reservations. No diagnostic or historical outcomes transfer. No interim selection, retries, extensions, replacement seeds, extra candidates or dropped controls.

Each phase uses the unchanged approximate simultaneous 95% Bonferroni-Wilson bounds across all 163 recipes: at least two distinct equipment-eligible compositions with lower bound at least 10%, and every recipe with upper bound at most 50%. At 512 samples, this requires at least **76 wins** per qualifying recipe and at most **215 wins** on every recipe. Eligible means at most eight specialized items across at most two characters; recipe labels do not create compositions.

Admit each batch only below doubled measured time/byte limits and remaining projected disk plus 2 GiB. Limits stay 840 native seconds / 900 owner seconds / 2 GiB, with admission below 80%. A failed admission or execution stops the frozen trial without replacement. Verify every archive, raw outcome and native prepared participant. During native runs, observe only supervisor stdout; do not inspect active files or edit pinned inputs.

Only after independently passing both phases may local application proceed. First match all **83,456 confirmation inputs** and **5,216 full historical replays** against isolated content (first four saved seeds per batch for each recipe). After local application, verify the same parity and relevant backend regression. No deployment or database changes.

Implementation adds a separate exact candidate and aggregate version. Existing diagnostics, the rejected 0.95/148-recipe contract and generic penetration guard remain unchanged. **507 Python checks and 364 native tests / zero skips pass**; **326 zero-fight preparations** match every nominated participant and confirm that only guardian ArmorPenetration/MagicPenetration change. Production assembly hashes remain unchanged. The historical broader 766-pass/four-skip proof and known pre-existing Kharad manifest failure are retained.

Proposal: `TestResults/tower-floor9-restoration-acceptance-proposal-20261001.json`, SHA-256 `31d2fd371ad65b4c101a5f13d3f0bc73115efdbcdfcb459e5e5ae04db1b80c29`.
