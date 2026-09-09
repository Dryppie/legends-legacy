# Combat Styles: implementation verification and initial balance evidence

8 September 2026. Historical engine and initial balance evidence, collected before the global-selection simplification. The earlier full backend suite passed 2,066 tests. See the [current implementation status](implementation-status.md) for the latest scope and verification, and the [game design](game-design.md) and [implementation plan](implementation-plan.md) for the current contract.

The rank-based comparisons below predate bonuses at every mastery level. They remain historical evidence; current level scaling and its verification are documented in the linked implementation status and style guides.

Counterweight results below also describe the former refinement. Current content version `combat-styles.v4` replaces it with [Reprisal](styles/bastion.md#refinements--level-3), which stores a share of enemy damage absorbed by the owner's Barrier and spends no Barrier. These historical checks remain evidence for captured legacy battles; they do not verify Reprisal.

## Runtime and activity coverage

Bastion and Conduit run in the shared combat engine. Effective configuration is an immutable, versioned snapshot; encounter resources and cast bookkeeping live outside compiled ability definitions. Each normal Essence activation retains its owned Essence identity. Passive, summon, basic-attack, status and reaction execution cannot acquire a normal cast's Focus multiplier or contribution eligibility.

Direct healing, Lifesteal, recurring healing and regeneration converge after their normal modifiers and before missing-Health clamping. Regeneration keeps its separate scaling. Converted Barrier uses the existing capped pool, suppresses gain reactions and preserves absorption/break reactions. Counterweight adds a noncritical component to the first attack attempt and excludes that component from Lifesteal and damage-derived hit reactions.

The activity audit found the existing shared snapshot preparation path in raids, region bosses, World Tower, both tournament teams and arena defense. Arena offense and live defense fallback use the shared live preparation path. The optional snapshot and executor fields therefore reach both sides of these modes. Tournament snapshot audit payloads and arena loadout hashes include Combat Styles. Tower, raid, region-boss and tournament playback frames carry cumulative `combatStyles`; arena history and ordinary combat results preserve the same summaries. Summary data remains independent of detailed event logs and `EntityStats`.

Raids and World Tower capture builds at signup or application. A later style change during mustering takes effect for that activity only after its existing build-refresh action; combat uses the captured build until then. Combat Styles preserve these existing commitment rules.

The idle session's stored combat result describes its final encounter. Its Combat Style summary should be presented as **Last encounter**, not as totals for the entire offline catch-up batch.

## Correctness checks

The initial focused backend run passed **80 tests** with no failures:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyle|FullyQualifiedName~FastCombatEngine'
```

Coverage includes conversion before overheal, Wound, regeneration scaling, exact refinement/upgrade thresholds, Shelter capacity and ties, summon ownership, gain-reaction suppression, Counterweight misses/critical hits/Lifesteal, all Conduit curves, contributor cycles, Relay, periodic/passive isolation, shared compiled definitions, and compact summaries. Further focused tests cover post-cost reactions, action prevention, multi-target spending, death, continuous waves/checkpoints, frozen harness recipes and deterministic replay; the final implementation report records their final run alongside the complete repository suite.

The first sandboxed build could not read the user's NuGet configuration. The approved test wrapper with normal configuration access restored packages and built all backend projects. No database was contacted by these engine checks.

## Reproducible balance fixture

The new [Combat Styles fixture](../../LL/tools/BalanceHarness/Fixtures/combat-styles.json) compares seven configurations with otherwise identical equipment and Essences: no style, Bastion at levels 1 and 10, Conduit at levels 1 and 10 with Goblin Warrior as Focus, and Conduit at levels 1 and 10 with faster Vampire Bat as Focus. It uses three or four equipped Essences and Frost Imp, Crystal Wisp, or their paired spawn. Styles use their base form without upgrades to isolate the automatic rank difference. Refinements and upgrades are covered by correctness tests; this fixture does not establish their relative balance.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/combat-styles.json --samples 3 --seed 1337 --output TestResults/balance/combat-styles-smoke-003
```

Choose a new output directory on subsequent runs. The recorded run completed **126 valid battles**, with zero invalid, cancelled or unrun battles. All 126 were victories and none hit the tick limit. The [scorecard](../../TestResults/balance/combat-styles-smoke-003/scorecard.md), frozen inputs, content hashes and individual battle records are local verification artifacts. Master seed 1337 produces paired encounter seeds across alternative builds. Three trials per cell are a workflow and behavior check, with wide uncertainty: each 3/3 clear result has a 95% Wilson interval of approximately 43.85–100%.

For the paired Frost Imp / Crystal Wisp encounter:

| Equipped Essences | Configuration | Median clear time | Mean final Health |
| --- | --- | ---: | ---: |
| 3 | No style | 29.0 s | 46.67% |
| 3 | Bastion level 1 | 29.0 s | 45.95% |
| 3 | Bastion level 10 | 29.0 s | 46.21% |
| 3 | Conduit level 1, Goblin Warrior Focus | 28.2 s | 47.63% |
| 3 | Conduit level 10, Goblin Warrior Focus | 27.1 s | 48.55% |
| 4 | No style | 22.0 s | 72.36% |
| 4 | Bastion level 1 | 22.0 s | 71.77% |
| 4 | Bastion level 10 | 22.0 s | 71.93% |
| 4 | Conduit level 1, Goblin Warrior Focus | 18.5 s | 75.63% |
| 4 | Conduit level 10, Goblin Warrior Focus | 18.5 s | 75.63% |

Bastion's total converted Barrier across the nine three-slot battles rose from **196.50** at level 1 to **216.15** at level 10, exactly the expected 10% rank increase. Actual converted Barrier absorbed rose from **171.00** to **188.10**. The lower final Health than the control reflects its recovery tradeoff; remaining Barrier and protection absorbed must also be considered when comparing survival.

The slower Goblin Warrior Focus averaged **120% / 130%** effect amounts at style levels 1 / 10 with three equipped Essences, and **140% / 150%** with four. The faster Vampire Bat Focus produced **nine zero-Charge casts in each nine-battle configuration**. With three slots its 22 Focus casts averaged approximately **100.9% / 106.8%**; with four slots its 14 casts averaged **100.0% / 103.6%**. Both mastered and untrained variants retained the same opening losses: **38** raw eligible effect points across three-slot battles and **45** across four-slot battles. Rank bonuses did not erase the zero-Charge penalty.

These samples show that individual mastery has an observable but modest effect in these builds, while Focus cadence can matter more than rank. They do not establish acceptable PvP strength, group sustain, hard-enemy clear rates, all refinement matchups, healing suppression or final tuning. No balance targets were accepted and no gameplay values were changed in response to these samples.

## Provisional progression timing

The authored schedule requires **300 total Combat Style XP for level 3** and **14,000 for level 10**. Only the equipped style receives eligible base combat XP; bonuses and character-level caps do not change this schedule.

Area rewards currently target `10,000 × 1.08^difficultyTier` base XP per hour at a ten-second encounter cadence. The provider divides that rate by the expected spawn count, then rounds each actual encounter reward to a whole XP using midpoint rounding away from zero. Applying the authored spawn probabilities and that rounding gives:

| Area | Difficulty tier | XP for 1 / 2 / 3 creatures | Expected rounded XP/hour | Approximate level-3 time | Approximate level-10 time |
| --- | ---: | --- | ---: | ---: | ---: |
| Lumo Ruins | 1 | 29 / 58 / 87 | 10,774.08 | 100.2 s | 78.0 min |
| Crystal Creek | 3 | 18 / 36 / 53 | 12,771.72 | 84.6 s | 65.8 min |
| Area `region_01_area_04` | 4 | 19 / 38 / 58 | 13,482.00 | 80.1 s | 62.3 min |
| Area `region_01_area_07` | 10 | 30 / 61 / 91 | 21,636.00 | 49.9 s | 38.8 min |

These are rate-based estimates for eligible solo wins at the authored cadence, not measured time-to-level guarantees. Defeats, reward retention, party shares, different encounter cadence and actual spawn sequences change realized progression. Milestones occur only after a whole encounter grant: repeatedly winning the fixed single-creature Crystal Creek fixture grants 18 XP each time, so level 3 arrives on encounter **17** (306 XP, 170 seconds at ten-second cadence), and level 10 on encounter **778** (14,004 XP, 129 minutes 40 seconds). Four cap-overflow XP are discarded.

The distinction between a fixed-spawn harness and ordinary spawn sampling materially changes progression estimates. The current schedule is intentionally provisional; the roughly one-to-two-minute first refinement under favorable ordinary conditions and roughly one-hour mastery deserve product playtesting before being treated as accepted pacing.
