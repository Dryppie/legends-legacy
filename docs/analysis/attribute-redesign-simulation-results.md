# Attribute allocation verification — 25 September 2026

**Historical candidate evidence:** this screen uses the earlier rating-based penetration formula. On 27 September the user selected subtraction of up to 40 percentage points from mitigation. See the [penetration follow-up](attribute-penetration-follow-up-2026-09-27.md) for the implemented rule, fresh checks and changed balance findings. The results below remain valid for their retained executable, not as acceptance evidence for the revised formula.

This is a reproducible release screen, not approval to activate the new rules. The final three studies ran **10,880 production-engine fights** (4,480 current, 1,920 synthetic, 4,480 transition). Four further fights reproduced the first current cell’s detailed exploration and confirmation replays byte for byte. Earlier pilots and a rejected unequal-budget set comparison are retained separately in ignored `TestResults`; they are not included in these totals.

Follow-up: [27 September matched opponents, party support, basic attacks, summons, control and sets](attribute-redesign-follow-up-2026-09-27.md) adds 8,960 fights and corrects the timeout interpretation below.

## What the screen establishes

- Content is copied and hashed before execution. Requests fix 8 exploration seeds and 32 separate confirmation seeds, the selected gear/Essences/Doctrines and a maximum fight budget. PvP plays both sides for every seed; its two mirrors are one statistical cluster.
- Current fixtures cover legal equal-budget specialization exchanges, a clearly labeled non-droppable Power-to-normalized-Armor diagnostic, Region 1/2 idle, a fixed authored dungeon boss group, five-player Tower and mirrored PvP. Essence counts include 0, 1, 4 and 10; Doctrines include Bastion, Conduit, Reaper and Duelist. Common, Epic and Legendary budgets are represented.
- Future tiers 3, 6 and 10 use an explicitly widened **frozen copy** of the catalog and synthetic character levels. They are PvP projections, not authored future regions. Production gear remains limited to the authored tier range.
- Set comparisons hold the number of styled items and total budget fixed: four Arcane pieces versus 3/1, 2/2 or four Aegis pieces. A styled item carries the existing style budget premium, so an unstyled-to-styled exchange was correctly rejected by the equal-budget validator.
- The transition study changes the rules/content regime for both sides of each battle. It is not a causal single-stat effect or a copied-player migration rehearsal.

## Findings and release decision

The candidate does not have demonstrated broad balance acceptance. In the 10-Essence Conduit PvP fixture, haste gained **42.19 percentage points** over the default weapon allocation (approximate paired 95% interval **33.27–51.11**). Precision gained **39.06 pp** (**30.56–47.57**). This makes both strong candidates for further stress testing; it does not establish that haste dominates other Doctrines or encounters.

The solo healer fixture is a material warning about finishing fights: Restoration lost **87.50 pp** (**−96.30 to −78.70**) against its default allocation. This was mainly a transition from wins to draws: 58 wins / 5 losses / 1 draw became 2 wins / 0 losses / 62 draws, with more effective healing and barrier absorption. It lies on a descriptive multi-objective frontier because healing and winning are different objectives. Frontier membership alone therefore cannot certify viability. Investigate effective healing versus overhealing, lost penetration, solo versus party support and fight timing before changing its price. Do not blindly increase its number to erase this result.

Several fixtures sit at all-win, all-loss or all-draw boundaries: the sampled Tower and DoT PvP matchups lost, while basic PvP drew. Their zero win changes cannot distinguish useful allocations. Future calibration needs opponents closer to their difficulty boundary, dedicated summon/control parties, broader hand layouts, multi-wave encounters and stronger coverage of 2/4-piece set effects. The harness accepts additional explicit cases; no global optimum or universal dominance claim is made.

Keep live version 17 until the copied-data rehearsal and an agreed balance acceptance review are complete. No automatic tuning or promotion was performed.

## Reading the estimates

Intervals are exploratory, unadjusted for comparisons across cells. PvE win differences use the paired Bonferroni–Wilson estimator. PvP uses the normal approximation across independent seed clusters; fewer than 30 clusters or constant observed differences intentionally have no interval. Absolute win percentages below count mirrored fights descriptively, while inference clusters them by seed. A draw is not a win. Shorter losses are not treated as improvements in the multi-objective rankings.

Detailed `trials.json` retains prevention, health damage, healing/overhealing, barriers, control, summons and survival telemetry. `rankings.json` reports per-context candidate order and descriptive Pareto membership. `*.replay.json` retains detailed event logs. Artifacts under `TestResults` are local and ignored; preserve the full directories with the manifest’s execution assemblies for a durable archive.

## Current legal allocations and set thresholds

Artifact directory: `TestResults/attribute-allocation-current-20260925-release-screen-v2`.
Request hash: `096e4b47a7cd146ed95c7a3caabf4667c5707c2d012fdcd8d9b32412f2275271`.
Trial-file SHA-256: `934a44634f962e5d6e8af7b50e4bc3aba5ebf41642935d6f223758113ad27700`.

| Cell | Reference wins | Candidate wins | Change (pp) | Paired 95% interval | Duration Δ ticks |
|---|---:|---:|---:|---|---:|
| early-basic-idle-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 8.41 |
| early-basic-idle-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 8.91 |
| early-basic-idle-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 2.38 |
| early-basic-idle-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 10.03 |
| four-essence-dungeon-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 4.59 |
| four-essence-dungeon-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 16.88 |
| four-essence-dungeon-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 5.19 |
| four-essence-dungeon-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 24.22 |
| four-essence-tower-precision | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | -2.50 |
| four-essence-tower-speed | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | 7.56 |
| four-essence-tower-haste | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | 7.94 |
| four-essence-tower-restoration | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | 0.00 |
| ten-essence-region2-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 0.16 |
| ten-essence-region2-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -0.25 |
| ten-essence-region2-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -12.91 |
| ten-essence-region2-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | 0.88 |
| ten-essence-pvp-precision | 57.81% | 96.88% | 39.06 | 30.56 to 47.57 | -98.52 |
| ten-essence-pvp-speed | 57.81% | 60.94% | 3.12 | -10.75 to 17.00 | 10.16 |
| ten-essence-pvp-haste | 57.81% | 100.00% | 42.19 | 33.27 to 51.11 | -99.92 |
| ten-essence-pvp-restoration | 57.81% | 68.75% | 10.94 | -0.48 to 22.36 | 103.28 |
| diagnostic-power-to-armor | 57.81% | 71.88% | 14.06 | -1.95 to 30.07 | 53.22 |
| healer-pvp-precision | 90.62% | 100.00% | 9.38 | 1.22 to 17.53 | -695.88 |
| healer-pvp-speed | 90.62% | 98.44% | 7.81 | -1.11 to 16.73 | -390.42 |
| healer-pvp-haste | 90.62% | 95.31% | 4.69 | -3.38 to 12.75 | 149.88 |
| healer-pvp-restoration | 90.62% | 3.12% | -87.50 | -96.30 to -78.70 | 1055.53 |
| dot-pvp-precision | 0.00% | 0.00% | 0.00 | Unavailable | -1.27 |
| dot-pvp-speed | 0.00% | 0.00% | 0.00 | Unavailable | 32.66 |
| dot-pvp-haste | 0.00% | 0.00% | 0.00 | Unavailable | -24.11 |
| dot-pvp-restoration | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-precision | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-speed | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-haste | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-restoration | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| sets-arcane-three | 100.00% | 100.00% | 0.00 | Unavailable | 159.64 |
| sets-split-two-two | 100.00% | 100.00% | 0.00 | Unavailable | 170.31 |
| sets-aegis-four | 100.00% | 100.00% | 0.00 | Unavailable | 207.67 |

## Synthetic future tiers

Artifact directory: `TestResults/attribute-allocation-future-20260925-release-screen`.
Request hash: `adeffe9cff8946208dc08a9e1b53b90d887a8ac08302e61a6b65a4bb3f10d8da`.
Trial-file SHA-256: `ce34c842cfcc3d97d17c2a6e3311a1125fcd82e0e6e0d73c4af4d6163971d068`.

| Cell | Reference wins | Candidate wins | Change (pp) | Paired 95% interval | Duration Δ ticks |
|---|---:|---:|---:|---|---:|
| synthetic-tier-3-precision | 60.94% | 96.88% | 35.94 | 26.88 to 44.99 | -161.39 |
| synthetic-tier-3-speed | 60.94% | 34.38% | -26.56 | -40.47 to -12.66 | 59.27 |
| synthetic-tier-3-haste | 60.94% | 100.00% | 39.06 | 30.56 to 47.57 | -143.33 |
| synthetic-tier-3-restoration | 60.94% | 37.50% | -23.44 | -35.87 to -11.00 | 260.55 |
| synthetic-tier-6-precision | 57.81% | 98.44% | 40.62 | 31.36 to 49.89 | -178.12 |
| synthetic-tier-6-speed | 57.81% | 29.69% | -28.12 | -42.68 to -13.57 | 107.86 |
| synthetic-tier-6-haste | 57.81% | 98.44% | 40.62 | 31.36 to 49.89 | -134.30 |
| synthetic-tier-6-restoration | 57.81% | 42.19% | -15.62 | -31.14 to -0.11 | 297.22 |
| synthetic-tier-10-precision | 71.88% | 96.88% | 25.00 | 15.16 to 34.84 | -219.61 |
| synthetic-tier-10-speed | 71.88% | 26.56% | -45.31 | -56.41 to -34.22 | 102.09 |
| synthetic-tier-10-haste | 71.88% | 100.00% | 28.12 | 18.35 to 37.90 | -165.31 |
| synthetic-tier-10-restoration | 71.88% | 37.50% | -34.38 | -47.89 to -20.86 | 355.66 |

## Version 17 to 18 transition

Artifact directory: `TestResults/attribute-allocation-transition-20260925`.
Request hash: `731c3ac6e8f36422ee5a90be5940d91e057bb53e2f68ec2acad774909d6ad9fd`.
Trial-file SHA-256: `fb021b368c9b2c4f558918feddb382b7217ac6794aa6581b49ae7c373698f065`.

| Cell | Reference wins | Candidate wins | Change (pp) | Paired 95% interval | Duration Δ ticks |
|---|---:|---:|---:|---|---:|
| early-basic-idle-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -8.44 |
| early-basic-idle-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -8.97 |
| early-basic-idle-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -15.81 |
| early-basic-idle-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -6.44 |
| four-essence-dungeon-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -38.00 |
| four-essence-dungeon-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -34.81 |
| four-essence-dungeon-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -46.12 |
| four-essence-dungeon-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -20.00 |
| four-essence-tower-precision | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | -49.09 |
| four-essence-tower-speed | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | -54.75 |
| four-essence-tower-haste | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | -48.06 |
| four-essence-tower-restoration | 0.00% | 0.00% | 0.00 | -13.57 to 13.57 | -48.44 |
| ten-essence-region2-precision | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -10.97 |
| ten-essence-region2-speed | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -10.28 |
| ten-essence-region2-haste | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -23.00 |
| ten-essence-region2-restoration | 100.00% | 100.00% | 0.00 | -13.57 to 13.57 | -8.62 |
| ten-essence-pvp-precision | 73.44% | 95.31% | 21.88 | 11.15 to 32.60 | -471.42 |
| ten-essence-pvp-speed | 73.44% | 53.12% | -20.31 | -33.41 to -7.22 | -342.81 |
| ten-essence-pvp-haste | 73.44% | 100.00% | 26.56 | 17.78 to 35.35 | -438.86 |
| ten-essence-pvp-restoration | 73.44% | 54.69% | -18.75 | -31.77 to -5.73 | -241.95 |
| diagnostic-power-to-armor | 73.44% | 71.88% | -1.56 | -15.81 to 12.69 | -297.08 |
| healer-pvp-precision | 0.00% | 100.00% | 100.00 | Unavailable | -1850.45 |
| healer-pvp-speed | 0.00% | 98.44% | 98.44 | 95.38 to 101.50 | -1388.48 |
| healer-pvp-haste | 0.00% | 95.31% | 95.31 | 88.55 to 102.07 | -789.33 |
| healer-pvp-restoration | 0.00% | 3.12% | 3.12 | -1.14 to 7.39 | -15.34 |
| dot-pvp-precision | 4.69% | 0.00% | -4.69 | -9.82 to 0.44 | -369.98 |
| dot-pvp-speed | 4.69% | 0.00% | -4.69 | -9.82 to 0.44 | -378.25 |
| dot-pvp-haste | 4.69% | 0.00% | -4.69 | -9.82 to 0.44 | -400.61 |
| dot-pvp-restoration | 4.69% | 0.00% | -4.69 | -9.82 to 0.44 | -368.78 |
| basic-pvp-precision | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-speed | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-haste | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| basic-pvp-restoration | 0.00% | 0.00% | 0.00 | Unavailable | 0.00 |
| sets-arcane-three | 100.00% | 100.00% | 0.00 | Unavailable | 9.27 |
| sets-split-two-two | 100.00% | 100.00% | 0.00 | Unavailable | 17.16 |
| sets-aegis-four | 100.00% | 100.00% | 0.00 | Unavailable | 80.03 |
