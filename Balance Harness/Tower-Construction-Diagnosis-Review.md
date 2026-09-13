# Initial-party construction diagnosis

The dominant structural gap in the fresh-party constructor is its **50% chance of skipping mechanic-core insertion**, rather than lack of space after coverage placement. On 256 fresh generation seeds, a trace matched the unchanged constructor exactly, with **zero combat calls**. Among 2,220 characters on the 222 guided routes, only **116 (5.23%)** had no compatible pair/triple after coverage placement. In contrast, **1,052 incomplete characters** had a legal insertion but were skipped by the coin flip; **971** remained incomplete after the remaining slots were filled.

This identifies a precise constructor behavior, not a proven combat improvement. The [next experiment plan](Tower-Construction-Completion-Plan.md) proposes removing only that skip on the guided fresh-construction route. No search variant, combat protocol, balance setting or acceptance default was changed or run in this diagnosis.

## Historical evidence

The analysis reads all 18 relevant coverage/provider/collective arms across the v4 pilot, its replication, v5 and v6. It includes **1,728 evaluated parties**, of which **764** are accepted fresh-coverage proposals. All 764 fresh parties recorded zero discovery wins. **3,715 of their 7,640 characters (48.63%)** contained at least one complete authored mechanic pair/triple. Of the 764 parties, 680 used guided construction and 84 used its uniform route. These counts are computed directly from accepted proposals, with rejected attempts kept in their original packages.

Only three later refined parties recorded any discovery win, all from one v4 restart at evaluations 85, 91 and 94, at ancestry depths 8, 9 and 9. Each had a complete structural core on every character. However, **three fresh parties also had complete cores on all ten characters and still recorded zero discovery wins**. Full core coverage is neither established as a requirement nor sufficient for success. The historical counts are descriptive across different seeds and adaptive paths; they are not pooled win-rate evidence or a new reliability estimate.

The [full diagnosis](../TestResults/balance/tower-construction-diagnosis-20260913/diagnosis.json) retains every analyzed party's source, method, seed, operation, ancestry depth, discovery fitness and structural counts. Core membership is derived solely from the same 48 authored mechanic hypotheses. Future generation must not import the winning recipes, Essence IDs/counts, historical fitness or validation outcomes used in this retrospective diagnosis.

## Constructor trace and independent checks

Source inspection of [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) showed that `FreshCoverage` reserves coverage first, then considers core insertion only for characters selected by `random.Next(2) == 0`. The initial question was whether earlier coverage placement frequently prevented insertion. The trace distinguishes that mechanism from the skipped attempts.

An external diagnostic driver copied the constructor body and added observation-only snapshots after coverage placement and core insertion. It delegates the existing helpers to the unchanged harness. For every seed, its complete choice hash—including recipe, ordering, intent and rejection—matched `FreshCoverage` exactly. Independent Python checks recomputed core membership, capacity, family compatibility and eligible-core counts from the snapshots. Every traced insertion attempt's eligibility count matched those independent checks.

The [trace design](../TestResults/balance/tower-construction-diagnosis-20260913/trace-design.json) and [protocol](../TestResults/balance/tower-construction-diagnosis-20260913/trace-protocol.json) fixed 256 generation seeds and at most 512 constructor calls, a 60-second limit, a 100-MiB package cap and **zero allowed combat calls**. A runtime combat-call guard remained at zero. The new ledger excludes all **470,465** previously reserved seeds, including unused reservations, then records the 256 constructor-only seeds. There is no combat schedule or new battle result in this package.

| Constructor observation | Count |
| --- | ---: |
| Guided fresh parties | 222 |
| Uniform fresh parties | 34 |
| Guided characters | 2,220 |
| Already had a complete core after coverage | 56 |
| Core insertion attempted | 1,086 |
| Attempt blocked by no legal compatible core | 58 |
| Characters with no legal compatible core, attempted or skipped | 116 |
| Legal insertion skipped while character was incomplete | 1,052 |
| Those skipped characters still incomplete after fill | 971 |
| Characters complete after core phase | 1,052 |
| Characters complete after fill | 1,133 |

All 116 blocked characters lacked capacity for any complete core; none lacked every family-compatible option independently of capacity. This is specific to the current floor-5 pool, five-slot budget and hypothetical ownership. It does not establish the same proportions for scarce inventories, other floors or different content.

The data supports testing the skip decision before changing construction order. It does not justify imposing a saved Essence quota, preferentially selecting a known winning pair, removing the uniform route, increasing gear or training, or declaring that structural completion guarantees activation, timing, useful recipients or combat value.

## Preserved balance state

Kharad remains **Health 3.04881408 / Power 3.85370128**. The target remains ten level-40, tier-1, rank-2 Standard characters with five level-1 unascended/unevolved Essences each, exact fixed gear, no styles/contributions and hypothetical ownership including Rare. Practical acquisition remains unverified.

The latest [six-control confirmation](Tower-Control-Ceiling-Confirmation-Review.md) stays **Inconclusive**: its selected control won **479/1,000 (47.90%)**, adjusted interval **43.76–52.07%**. The earlier **131/256 (51.17%)** observed breach and v6 **Fail** remain preserved. This diagnosis adds no samples, changes no assessment and resolves neither the ceiling nor search reliability.

## Verification and retained work

All five preceding experiment packages and their sealed reviews verified unchanged. Every existing harness/gameplay source, content file, execution assembly and both catalogs remain unchanged. The diagnostic driver restored with its explicit source-free NuGet configuration and scoped approved access, and the final build passed with zero warnings/errors. An initial nullable-analysis warning in the diagnostic helper was corrected before tracing; no failed or repeated constructor run occurred.

The trace completed **512 constructor calls in 1.20 seconds**, with **256 exact matches**, no rejected recipes and **zero fights**. The new scripts independently check historical structure and trace snapshots. Backend combat tests were not rerun because no harness or gameplay code changed; the constructor-specific parity and independent structural checks are the relevant verification for this read-only diagnosis. No required command remains blocked. Final hashes and size are in [final verification](../TestResults/balance/tower-construction-diagnosis-20260913/final-verification.json).

Changed artifacts are this review, the concrete [completion experiment plan](Tower-Construction-Completion-Plan.md), active handoff/status documentation and the local diagnostic scripts/driver/evidence. There are no migrations, configuration, database or deployment changes. The latest [seed ledger](../TestResults/balance/tower-construction-diagnosis-20260913/seed-ledger.json) must be the exclusion source for future preparation, including its constructor-only seeds. No further combat experiment has been frozen or started.
