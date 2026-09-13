# V10 stagger-reservation diagnosis

Completed **13 September 2026**. This retrospective diagnosis used **zero new combats, constructor calls, seeds or replays**. The reusable report verified all **9,216 saved trial records**, **594 cases** and **four previously fixed detailed replays**. The overlay audited **588 proposals**, **576 evaluated recipes**, **105 control nominations** and **all twelve generated finalist ancestries**.

The records contradict a general explanation that the selected v10 parties lost because their initial control reservation was never placed or was mutated below its nominal minimum. All six new-arm finalists retain eight or nine Fairy carriers; their authored minimum is seven. This does **not** establish adequate realized control, a causal explanation for losing, or a replacement policy. The [pilot](Tower-Stagger-Reservation-Review.md) remains **reliability Fail 0/3**, all twelve generated finalists **0/256**, and ordinary/joint family assessments **Inconclusive / Inconclusive**. The strongest control remains **125/256 (48.83%)**; prior observed ceiling breaches remain separate.

## Frozen scope and evidence

The [protocol](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/protocol.json) declared the known pilot results before this analysis and allocated one report run, a 90-second report limit, 30-second overlay limit, 120 seconds combined analysis and 128 MiB total output. It did not allocate another experiment. The [report definition](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/report-definition.json) binds the producing source/execution, manifests, exact replay files and unchanged all-array [471,925-seed ledger](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/seed-ledger.json).

The [reusable report](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/report/report.json), [case metrics](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/report/cases.json), [authored routes](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/report/mechanics.json) and [detailed attribution](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/report/replays.json) retain all observations and missing/ambiguous evidence. Legacy intent strings lack nominated provider IDs; v10 separately records structured `reservations`. The [nomination overlay](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/nominations.json) reads those structured records. It does not infer comparator nominations from final recipes or change the report implementation.

## Placement and ancestry

Of 127 new-arm fresh proposals, 22 used the uniform route and 105 recorded guided control nominations: **Fairy 32, Feral Ghoul 29, Giant Worm 24, Brutal Charge 20**. **104/105** satisfied every requested control placement. Final fresh recipes can contain extra copies from later fill; requested placements are not additional occupied slots.

The sole shortfall is `stagger-reservation-joint--129820605-proposal-00027`: Brutal Charge requested ten and satisfied six. The ordered trace first nominated standard Hobgoblin for enemy pressure on four characters; the final recipe contains those four standard Hobgoblins and six Brutal Charge variants. The [existing family check](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) rejects a different variant from the same family. This supports a localized family conflict explanation. Slot-level attempts are not recorded, so this is not a reconstruction of random placement identities. This root is absent from the six new-arm finalist ancestries.

Across all accepted new-arm mutations there are **173 parent-child edges**: **54** show a decrease in at least one control-provider count and **51** reduce total nominal one-application power. Comparator edges are **174 / 57 / 48**, respectively. Recombination records each parent separately; these are descriptive edge counts, not independent trial rates or tracked inherited copies. The complete [edge ledger](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/parent-child-edges.json) includes every provider, category and core-count delta.

All six new-arm finalist ancestry roots nominated Fairy. The following table shows root **final-recipe** Fairy counts, not necessarily requested counts. Both finalist roles remain separate in the [full ancestry](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/finalist-ancestry.json).

| Restart | Root final Fairy counts | Primary / secondary Fairy counts | Other primary control | Primary nominal power | Primary core-bearing characters |
| --- | --- | --- | --- | ---: | ---: |
| `-76005071` | 8 and 10; two associated roots | 9 / 9 | None | 360 | 5 |
| `-129820605` | 8; requested/satisfied 7 | 9 / 9 | 1 Feral Ghoul, 1 Brutal Charge | 435, including 25 predicate-gated | 4 |
| `-127588879` | 10 | 8 / 8 | None | 320 | 6 |

The third secondary also has one Giant Worm, giving nominal power 355. All twelve generated finalists, including comparators, retain at least seven Fairies. Mutations therefore can reduce reservation counts, but insufficient nominal finalist capacity is not the observed common failure. None of this licenses a mutation repair, new provider weights or a Fairy preference. Roots reaching selection describe this search outcome only.

New-primary equipped category counts, ordered **attack-enabler / enemy-pressure / protection / recovery / recurring-control**, are **6/10/4/10/9**, **2/10/8/8/9**, and **6/10/4/6/8**. The corresponding comparator primary core-bearing counts are **9, 8, 5**. These counts do not measure recipient reach or causal displacement. Every recipe's structural counts remain in the [proposal audit](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/proposal-audit.json); no new objective or feature uses them.

## Nominal power and observed stagger

All four authored providers remain eligible. Power/minimum pairs are Fairy **40/7**, Feral Ghoul **50/5**, Giant Worm **35/8**, and Brutal Charge **25/10**, derived from Kharad's initial threshold **250**. Fairy starts at tick 200 with an authored 80% gate; all four have a separate non-guaranteed runtime control gate of 80%. Brutal Charge requires target health below 30%. Active cast order, targets, deaths, growing thresholds, break/recovery lockout and caps still intervene. No combined probability or timing margin is estimated.

These are the same four fixed samples on seed `-1840085499`, each a defeat. Ten ticks equal one second. Guardian denial below is actual guardian `actionDeniedTicks`, **not** the friendly aggregate field in compact case metrics.

| Saved sample | First break / first death | All break ticks | Power credited through first break | Total credited power | Guardian denied ticks |
| --- | --- | --- | --- | ---: | ---: |
| New primary, restart 1 | 400 / 429 | 400, 800 | Fairy 250; 7 events, 7 carriers | 588 | 58 |
| New primary, restart 2 | 200 / 420 | 200 | Fairy 200 + Feral Ghoul 50; 6 events/carriers | 580 | 29 |
| New primary, restart 3 | 600 / 420 | 600 | Fairy 250; 7 events, 4 carriers | 290 | 29 |
| Fixed anchor | 600 / 729 | 600, 1000 | Fairy 250; 7 events, 4 carriers | 588 | 58 |

Fairy carriers with any credited contribution are **9/9, 7/9, 4/8**, and **7/8**. Capped positive events number **2, 0, 1, 2** across these samples. The second new primary's Feral Ghoul contributes at ticks 150 and 450; its Brutal Charge contributes nothing and the guardian's minimum logged health remains **63.98%**. The anchor's Brutal Charge contributes 25 at tick 900. This agrees with a conditional activation limitation without identifying a general eligibility defect. Failed gates and rejected zero contributions are not separately logged, so missing applications cannot be assigned to particular causes.

One new primary breaks at 20 seconds yet still loses, while the anchor first breaks at 60 seconds and survives longer. These examples disprove neither a possible benefit of earlier control nor another control design; they show why a first-break deadline cannot serve as a proven remedy. Applied power, completed breaks and denied actions remain separate measurements.

## Recovery and survivability

The report reconciles restored healing, regeneration, final health damage and first death **for every initial friendly recipient** in all four replays. It retains ten-second windows, provider/owner recipients and application timing. Regeneration is not assigned marginally to a modifier provider, and damage can include overkill.

| Saved sample | Healing / regeneration before 40 s | Healing / regeneration before first death | Total healing / regeneration |
| --- | --- | --- | --- |
| New primary, restart 1 | 490 / 2,414 | 610 / 2,414 | 2,622 / 5,924 |
| New primary, restart 2 | 399 / 2,544 | 426 / 2,544 | 778 / 4,254 |
| New primary, restart 3 | 693 / 2,361 | 693 / 2,361 | 1,571 / 4,479 |
| Fixed anchor | 381 / 2,304 | 2,616 / 4,866 | 4,229 / 6,966 |

The anchor has less restored healing before 40 seconds than every new primary in this sample. Its much larger pre-death totals cover a longer survival period, so they are not evidence for a fitted early-healing quota. Coverage can be present and observed yet limited: the second new primary has eight recovery-bearing characters, but its four Elder Treant instances restore only to their four owners; its Spider Queen recovery route has no observed application. The third primary's two Blue Slimes reach ten recipients, yet that sample also loses. No single count proves sufficient support.

Across the full 256 saved validation trials, median first-death times are **42.9, 42.0, 42.9 seconds** for the new primaries and **70.9 seconds** for the anchor. Median restored healing is **2,411.5 / 1,496 / 1,418.5 / 3,798**, respectively. The [validation summary](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/validation-summary.json) retains all eighteen cases and every original result. These are descriptive medians; no new confidence calculation, pooling or primary substitution occurred.

Direct coverage attribution has **zero ambiguous matches**, while unmatched application counts remain **151, 4, 98, 203**. Nested status, summon and equipment activity can remain unmatched. These counts are visible limitations, not assumed zero activity or failed reconciliation. All credited control events used here have unique provider attribution.

## Decision and verification

Supported: one localized family-placement shortfall; count erosion on some mutation edges; separation between equipped nominal capacity and realized control/support. Contradicted as a common explanation for the selected new-arm failures: missing initial reservations or finalists below their nominated nominal minimum. Unresolved: marginal value of more/earlier control, recovery allocation, core displacement, stochastic realization, conditional-provider eligibility and the cause of the persistent discovery gap.

No new search policy is selected. The next bounded step is to assess **at most one independently source-justified hypothesis**, with an explicit option to reject it, before any implementation or new pilot. Saved control recipes/counts/IDs/ancestry/fitness/held-out measurements must remain outside generation; these descriptive comparisons cannot become weights, eligibility filters, fitness or fitted deadlines. The [active handoff](Tower-Coverage-Replication-Plan.md) carries that boundary.

The report ran once in **26.65 seconds**; the overlay took **0.67 seconds including its corrected schema-label attempt**, for **27.32 seconds** of measured analysis. Final verification and documentation processing are recorded separately; initial preparation time was not instrumented. The [verification receipt](../TestResults/balance/tower-stagger-reservation-diagnosis-20260913/final-verification.json) checks report manifests, independent ancestry/count and recipient reconciliation, all **17 prior packages**, **75 prior sealed reviews**, **2,237 C# sources**, **five assemblies**, **16 content files**, **two catalogs**, the unchanged ledger, Markdown links/anchors and `git diff --check`. The prior **243/243 tests** are hash-verified; no backend tests were rerun because source and assemblies are unchanged. The temporary overlay expected `Enemy` instead of the saved `Hostile` label; this was corrected before overlay outputs, with no report rerun. No commands remain blocked.

Only this new evidence package/review and active Markdown were written. Kharad stays **Health 3.04881408 / Power 3.85370128** with the exact floor-5 ten-character budget, fixed gear and level-1 unascended/unevolved Essences under hypothetical ownership. There are **no source/gameplay changes, migrations, configuration changes, deployments, promotions, acquisition claims or floor expansion**.
