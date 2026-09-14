# Frozen finalist rescreen comparison

Prepared 13 September 2026. This plan implements the [selected rescreen proposal](Tower-Finalist-Rescreen-Plan.md) and freezes with the new package's `protocol.json` before its first fight. Target: the offline BalanceHarness at the currently applied Kharad setting. Preparation and reservation do not mean execution has started.

## Fixed inputs and selection

- Use the unchanged `independent-loadout-composition-v13` generator with `coverage-deep-joint` and `loadout-composition-joint`, three paired restarts, 384 evaluated candidates per arm, 8,192 proposal attempts per arm, fresh frequency four, and eight shared discovery seeds. Preserve existing random streams, parent selection, loadout library, operators, fitness and original nominations.
- Use the complete ten-character floor-5 budget: level 40, tier 1, rank 2, Standard quality, five level-1 unascended/unevolved Essences, exact fixed equipment, neutral identities, no styles or contributions. All 80 eligible Essences use hypothetical ownership. No player budget increase or catalog promotion.
- Content must match the accepted applied candidate byte for byte: Kharad **Health 3.5366243328 / Power 4.4702934848**. Preserve every other content value. The package captures all 16 content hashes, sanitized settings, execution identity and executable dependencies.
- Import exactly the same 20 saved controls from `tower-kharad-v13-calibration-20260913/references.json`. Keep their complete recipes and provenance external to independent generation. Fixed anchor: `team-1abe76ca1891d97a91d484f0a3662048`.
- Retain all 2,304 discovery evaluations and all generated recipes. Freeze original primary/secondary for all six arms. Independently freeze each v13 arm's top 32 distinct recipes, including original ranks and same-arm ancestry, before any rescreen observation.
- Rescreen all 32 candidates from each of the three v13 arms on the same 64 fresh shared seeds. No early stopping. Select by rescreen wins descending, then original discovery rank. Do not pool discovery trials. Zero-win ties retain original order.
- Freeze the confirmation family before its first observation: all 12 original finalists, up to six additional rescreened finalists, all 20 controls, and every discovery/rescreen recipe observed above 50%. Deduplicate only exact normalized recipes while retaining all source associations.
- At most 64 distinct confirmation recipes, each with the same 512 untouched shared seeds. Preserve the complete family and stop with `CapacityExceeded` if it exceeds 64; never drop a breach or truncate. No replacement of primaries after confirmation.

## Resource and seed allocation

| Phase | Maximum starts |
| --- | ---: |
| 2 × 3 × 384 × 8 discovery | 18,432 |
| 3 × 32 × 64 finalist rescreen | 6,144 |
| 64 × 512 confirmation | 32,768 |
| **Total** | **57,344** |

The normal upper count with 38 distinct confirmation recipes and no extra breach nominations is **44,032**. Freeze **5,400 seconds** across execution, **4 GiB** for the complete study package, **zero combat retries, resume, optional extensions or diagnostic replays**. Every start and completion is durably recorded. An interrupted start remains charged; retain the complete failure evidence. Cooperative cancellation/storage checks do not authorize exceeding the declared final bounds. Preparation, backend fixture tests and zero-combat reconstruction are outside campaign fight/time accounting.

Seed allocation uses deterministic master **2026091307** and the new `tower-finalist-rescreen-v1` namespace. Exclude every array in the **476,054-reservation precision ledger**, including unused historical schedules, plus the source template's complete exclusions and schedules. Allocate **587** new disjoint reservations: three generation integers, eight discovery seeds, 64 rescreen seeds and 512 confirmation seeds. Retain the complete **476,641-reservation ledger**, including all reserved values if the campaign does not start or stops early. Never reuse the previous calibration's unused selection reservations.

## Fixed decisions

A rescreened primary passes search reliability only if its adjusted rate lower bound is ≥10%, its paired lower improvement over the same-restart original deeper-v4 primary is >0, and its paired lower difference against the fixed anchor is ≥−10 percentage points. At least **two of three restarts** must satisfy all three.

A separate selection-benefit gate compares each rescreened primary with its same-restart original v13 primary. At least **two of three** differences must have adjusted lower bound >0. An unchanged primary has zero paired benefit. Both gates must pass for the workflow to be eligible for adoption; this experiment performs no automatic default promotion.

Joint alpha: **.025 across the full distinct confirmation rate family**, **.025 across nine paired differences**, using two discordance intervals for each difference. The `.05` Wilson interface uses multipliers `2 × family size` for rates and **36** for discordance intervals. Report ordinary nominated-family acceptance separately at alpha .05 over every nominated rate. Preserve every observed >50% breach. No samples are pooled between phases or studies. These are approximate scoped bounds, not a lifetime repeated-study or near-optimality guarantee.

## Execution and completion

Use the captured `executable/BalanceHarness.dll` from the prepared package. `tower-finalist-rescreen-check` verifies the complete prepared inventory with zero fights. `tower-finalist-rescreen-run` may start the campaign once. `tower-finalist-rescreen-verify` reconstructs completed discovery, all three rescreens, final selection, confirmation, statistics and attempt accounting under a no-combat guard. Never use `run` to reconstruct completed evidence.

Before execution, verify the implementation tests, unchanged archived v13 generation and original nominations, the original diagnosis/source packages, the prepared package and producing identities. Keep execution logs outside the prepared package until the runner finishes sealing it.

After execution, preserve the original and rescreened nominations, every evaluated recipe, every confirmation recipe and origin, compact evidence, complete ledger, resource accounting and final outcome, including a clean failure. The generated family beyond confirmation remains uncertified. Kharad balance acceptance and local application remain separate existing results. No boss edit, service restart, deployment, migration, shared-database action, practical-ownership conclusion or later-floor campaign is part of this comparison.
