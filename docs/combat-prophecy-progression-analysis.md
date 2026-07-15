# Creature, Character, and Prophecy Progression Analysis

## Scope and assumptions

This analysis compares idle-combat creature XP and Cinder rewards with character level requirements and the scalable Prophecy rewards.

The combat model assumes:

- one encounter every 10 seconds, or 360 encounters per hour;
- every encounter is won;
- no combat XP bonuses or defeat-retention bonuses;
- one character receives the XP, with no party split;
- creatures and group sizes follow the committed region weights;
- only direct combat Cinders are included, not item drops, dungeons, Guild rewards, caches, or Marketplace activity.

Real earnings are lower when the character loses and can be higher with progression bonuses.

## Current combat reward flow

The idle planner schedules one encounter immediately when due and then one every 10 seconds. On victory, character XP is the sum of the defeated creatures' `ExperienceReward` values.

Before this rebalance, the shared combat Cinder calculator granted:

```text
Cinders = sum of defeated creature XP × 10
```

This happened on every victory. Cinders were therefore not a conventional chance-based drop: they were a guaranteed reward worth ten times the encounter's XP.

At 360 encounters per hour, even a single 4-XP Goblin produced 14,400 Cinders per hour before group spawns. Late-region groups produced well above 100,000 per hour.

## Creature and area progression

Most areas after Lumo Ruins use group probabilities `[0.03, 0.969, 0.001]`, which means an average of 1.971 creatures per encounter. Creature selection is independently weighted for every spawned slot.

The following values are expected values from the committed creature, region, group-size, and spawn-weight data:

| Area              | Unlock | Creatures/encounter | XP/encounter | XP/hour | Minutes for one level at unlock | Old Cinders/hour | Rebalanced Cinders/hour |
| ----------------- | -----: | ------------------: | -----------: | ------: | ------------------------------: | ---------------: | ----------------------: |
| Training Area     |      1 |               1.000 |         1.00 |     360 |                            17.3 |            3,600 |                     360 |
| Lumo Ruins        |      1 |               1.032 |         4.51 |   1,624 |                             3.8 |           16,235 |                     396 |
| Blood Grove       |      5 |               1.971 |        12.18 |   4,385 |                             2.8 |           43,851 |                   1,072 |
| Crystal Creek     |     10 |               1.971 |        20.30 |   7,308 |                             4.3 |           73,085 |                   1,547 |
| Twilight Clearing |     15 |               1.971 |        26.94 |   9,697 |                             6.5 |           96,973 |                   2,111 |
| Oak Thicket       |     20 |               1.971 |        19.71 |   7,096 |                            15.2 |           70,956 |                   1,563 |
| Old Forest        |     25 |               1.971 |        34.89 |  12,559 |                            13.2 |          125,592 |                   2,653 |
| Bleak Orchard     |     30 |               1.971 |        38.04 |  13,695 |                            17.2 |          136,945 |                   2,882 |
| Rotting Hamlet    |     35 |               1.971 |        40.01 |  14,404 |                            22.1 |          144,041 |                   3,022 |
| Wormburrow Depths |     40 |               1.971 |        45.33 |  16,320 |                            25.4 |          163,199 |                   3,411 |
| Forgotten Ruins   |     45 |               1.971 |        34.30 |  12,346 |                            42.3 |          123,463 |                   2,605 |

### Content irregularities

XP throughput is not monotonic:

- Oak Thicket unlocks after Twilight Clearing but drops expected XP/hour by about 27%.
- Forgotten Ruins is the highest-level area but drops expected XP/hour by about 24% compared with Wormburrow Depths.
- Twilight Clearing's creature weights total `0.9`, although the spawning code normalizes the values and therefore still selects correctly.

These are content-balance discontinuities rather than errors in the XP writer. Players optimizing progression may remain in an earlier area if they can win it reliably. This rebalance does not change creature XP because doing so would alter character, Essence, and potentially objective pacing simultaneously.

## Character XP curve

For levels 1–100, the current formula simplifies to:

```text
XP required for next level = floor(100 + 4.25 × level²)
```

Representative requirements are:

| Level | XP for next level |
| ----: | ----------------: |
|     1 |               104 |
|    10 |               525 |
|    20 |             1,800 |
|    30 |             3,925 |
|    40 |             6,900 |
|    45 |             8,706 |
|    60 |            15,400 |
|   100 |            42,600 |

The requirement is quadratic while creature XP rises only modestly and stops receiving new area content after level 45. Level time therefore expands from a few minutes early on to about 42 minutes at the level-45 unlock area. If a level-100 character remains in Forgotten Ruins, one level takes roughly 3.45 hours at a 100% win rate.

If the character always uses the highest-XP unlocked area, including remaining in an earlier area when a later unlock is worse, the optimistic cumulative timeline is:

| Reached level | Continuous perfect-combat time |
| ------------: | -----------------------------: |
|             5 |                     0.32 hours |
|            10 |                     0.69 hours |
|            20 |                     1.87 hours |
|            30 |                     4.26 hours |
|            40 |                     7.95 hours |
|            45 |                    10.28 hours |
|            60 |                    21.00 hours |
|           100 |        88.47 hours / 3.69 days |

This is an upper-bound throughput model, but it shows that always-on 10-second combat can compress the entire current progression range into a small number of real days. That is an XP pacing concern separate from the Cinder multiplier and should be addressed with an explicit target for online/offline progression before changing creature XP values.

The curve has coefficient changes after level 100 because it uses `ceil(level / 100)` internally. That behavior is outside the current region-content range and should be reviewed before content is extended significantly beyond level 100.

## Prophecy comparison

Prophecy character XP is intentionally expressed as a share of the next-level requirement:

- Daily Common/Uncommon/Rare: 4% / 6% / 8% of a level, with minimum floors.
- Weekly Uncommon/Rare/Epic: 30% / 40% / 50% of a level, with minimum floors.

This keeps Prophecy XP relevant even when combat level time expands. At level 45:

- a Rare daily grants 696 XP, equivalent to about 3.4 minutes of perfect Forgotten Ruins combat;
- a Rare weekly grants 3,482 XP, equivalent to about 16.9 minutes of that combat;
- the corresponding old Cinder rewards were 1,255 and 6,270;
- combat itself produced about 123,463 Cinders per hour, making those rewards worth only 0.6 and 3.0 minutes of combat Cinders.

Both Cinder systems were anchored to XP, but at different multipliers: combat used `10×`, while high-level Prophecies used `1.8×`. More importantly, Prophecy Cinders inherited the quadratic next-level XP curve and therefore grew from hundreds to tens of thousands even though the authored minimum rewards and most Cinder sinks did not.

The XP floors are extremely generous before the percentage calculation overtakes them:

| Level |     Daily Common | Daily Uncommon | Daily Rare | Weekly Uncommon | Weekly Rare | Weekly Epic |
| ----: | ---------------: | -------------: | ---------: | --------------: | ----------: | ----------: |
|     1 | 52.9% of a level |          76.9% |     101.0% |          442.3% |      567.3% |      692.3% |
|    10 |            10.5% |          15.2% |      20.0% |           87.6% |      112.4% |      137.1% |
|   20+ |               4% |             6% |         8% |             30% |         40% |         50% |

From level 20 onward, five dailies plus the weekly contribute between 50% and 90% of one level. At the first few levels, the fixed floors can award several complete levels, particularly from the weekly Prophecy. This implementation leaves XP unchanged because the requested corrective action is Cinder inflation; the early XP floors should be reviewed separately rather than being altered as a side effect of the currency rebalance.

## Implemented rebalance

### Combat Cinders

Combat now grants 20% of the defeated group's creature XP, rounded up, with a minimum of one Cinder for a victory that has positive creature XP:

```text
Cinders = max(1, ceil(sum of defeated creature XP × 20%))
```

The percentage and minimum are configuration values under `Combat:CinderRewards`. The same shared calculator is used by idle and dungeon combat.

This reduces expected region income from approximately 16,235–163,199 Cinders/hour to 396–3,411 Cinders/hour. The result still rises with harder creatures and larger groups, but it no longer gives ten currency units for every XP point every ten seconds.

### Prophecy Cinders

Prophecy Cinders no longer derive from resolved character XP. Each recipe keeps its existing minimum, grows by 1% of that minimum per character level after level 1, rounds to the nearest five, and caps at +200%:

```text
growth basis points = min(20,000, (character level - 1) × 100)
Cinders = round-to-5(minimum Cinders × (10,000 + growth basis points) / 10,000)
```

| Level |  Daily Common old → new |    Daily Rare old → new |     Weekly Rare old → new |
| ----: | ----------------------: | ----------------------: | ------------------------: |
|     1 |               105 → 105 |               195 → 195 |             1,050 → 1,050 |
|    20 |               130 → 125 |               260 → 230 |             1,295 → 1,250 |
|    30 |               285 → 135 |               565 → 250 |             2,825 → 1,355 |
|    45 |               625 → 150 |             1,255 → 280 |             6,270 → 1,510 |
|    60 |             1,110 → 165 |             2,220 → 310 |            11,090 → 1,670 |
|   100 |             3,065 → 210 |             6,135 → 390 |            30,670 → 2,090 |
|  201+ | unbounded → 315 maximum | unbounded → 585 maximum | unbounded → 3,150 maximum |

At level 45, the rebalanced Rare daily's 280 Cinders represent about 6.5 minutes of Forgotten Ruins combat Cinders, while its XP represents about 3.4 minutes of combat XP. Prophecies therefore remain a noticeable currency bonus without replacing combat as the primary repeatable source.

## Risks and follow-up work

- Existing Cinder balances and authored Marketplace prices were accumulated under a much more generous source. No legacy balance conversion is included.
- Long-duration idle combat still produces meaningful currency because 360 encounters occur every hour. Observe daily rather than per-encounter totals.
- Oak Thicket and Forgotten Ruins should be separately retuned if each new area is intended to improve XP/hour.
- The level formula's behavior at century boundaries deserves its own review before raising the practical level/content ceiling.
- Cinder sinks should be measured after the source reduction before their prices are lowered. Changing sources and sinks simultaneously would make the result difficult to evaluate.
