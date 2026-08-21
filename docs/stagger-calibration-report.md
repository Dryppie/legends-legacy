# Stagger Calibration Report

## Decision summary

The current Stagger implementation is mechanically stable and is now exercised inside full Tower and Raid combat with progression-valid player attributes and Essence loadouts. Do not change the `0.85` participant exponent, break duration, recovery duration, or damage bonus globally based on the current results.

The full-combat results show that Stagger contribution is working, but break frequency is strongly coupled to encounter duration and survival. Tower groups generally reach one to three breaks with safe uptime. Some Raid bosses kill the group before its expected breaks, while other Raid bosses die so quickly that there is no meaningful break opportunity. Those are encounter-pressure problems first, not evidence that the global Stagger formula is wrong.

The next balance pass should bring Raid and Tower outcomes into their target win-rate, duration, and survival bands before changing thresholds. After that pass, tune individual `BaseThreshold` values or authored `StaggerPower` availability only where a progression band still misses its break target.

## Calibration scope

The deterministic mechanic-isolation suite evaluates:

- Tower floors 5 and 10;
- all five authored Raid boss tiers;
- Raid Plus projections at +3 and +6 for both Raid bosses;
- undersized, reference, and oversized participant cohorts;
- control-light, balanced, and control-heavy contribution profiles;
- 16 deterministic seeds per result row;
- a 1,800-tick, three-minute evaluation window.

This produces 99 aggregated result rows from 1,584 deterministic samples. The suite operates directly on the production `RuntimeStaggerState` and authored `BossStaggerDefinition` objects, but intentionally excludes damage, survivability, and boss kill time.

The profiles use representative current values:

| Profile | Control share | Stagger | Cadence | Success | Target |
|---|---:|---:|---:|---:|---:|
| Control-light | 10% | 25 | 30 seconds | 80% | 0–1 late break |
| Balanced | 20% | 35 | 20 seconds | 80% | 2–3 breaks, first at 30–90 seconds |
| Control-heavy | 40% | 45 | 15 seconds | 85% | 3–4 breaks, first at 15–60 seconds |

`StaggerPower` currently does not change with Essence ascension, so Essence tier is not an independent input to the isolated simulation. It can still change the outcome of a full combat through damage, survival, cooldown interactions, and encounter duration.

## Results

The run produced 31 target exceptions:

- 11 balanced-control break-count exceptions;
- 10 balanced-control first-break timing exceptions;
- 7 control-heavy break-count exceptions;
- 3 control-heavy first-break timing exceptions.

There were no exceptions for party-size spread, Stagger uptime, contribution efficiency, or control-light break frequency.

### Reference participant cohorts

| Encounter | Threshold | Balanced breaks / first tick | Heavy breaks / first tick |
|---|---:|---:|---:|
| Tower floor 5 | 250 at 10 | 1.06 / 934 | 3.94 / 234 |
| Tower floor 10 | 375 at 15 | 1.06 / 891 | 3.88 / 253 |
| Hives' Abyss tier 1 | 150 at 3 | 1.00 / 1,188 | 1.81 / 610 |
| Hives' Abyss tier 2 | 240 at 6 | 0.75 / 1,491 | 2.13 / 481 |
| Hives' Abyss tier 3 | 330 at 9 | 1.00 / 1,205 | 3.00 / 339 |
| Sanguine Horror tier 2 | 240 at 6 | 0.75 / 1,491 | 2.13 / 481 |
| Sanguine Horror tier 3 | 330 at 9 | 1.00 / 1,205 | 3.00 / 339 |
| Hives' Abyss +3 | 173 at 3 | 1.00 / 1,188 | 1.81 / 610 |
| Hives' Abyss +6 | 195 at 3 | 0.88 / 1,358 | 1.56 / 807 |
| Sanguine Horror +3 | 276 at 6 | 0.50 / 1,610 | 2.00 / 572 |
| Sanguine Horror +6 | 312 at 6 | 0.13 / 1,655 | 2.00 / 572 |

### Healthy properties

- Party-size break spread is at most `0.875` breaks across the 67%, 100%, and 133% cohorts. The `0.85` participant exponent is doing its job and should remain unchanged.
- Accepted contribution efficiency remains between `70.3%` and `81.3%`. Recovery immunity is visible without invalidating most control attempts.
- Stagger uptime remains between `0%` and `6.5%`, below the 10% safety band.
- Control-light groups produce no early repeatable breaks.

### Main risk

The current tuning creates a sharp composition breakpoint. Heavy-control Tower groups reach the four-break cap in 88–94% of samples, while balanced groups average only about one break. Small Raid groups remain below three breaks even under the heavy profile.

This means the mechanic currently rewards specialization, but it may feel absent for ordinary groups and capped for dedicated Tower groups. The gap is primarily caused by contribution supply relative to the thresholds, not by participant scaling or recovery.

## Full-combat validation

The authored-encounter calibration now attaches the live Stagger profiles to Tower Floors 5 and 10 and models seven Raid Final Assault samples. It executes the real player snapshot and Essence matrix against the real creature, ability, scaling, overtime, preparation, and Stagger rules.

The Raid baseline deliberately represents ideal preparation so repeated runs are comparable:

- Guardian Break and Signature Disruption are both fully completed.
- The resulting defense, resistance, damage-reduction, Power, and signature-cooldown adjustments are applied to the boss.
- Rearguard survivors and random boss variants are excluded from this first baseline.
- Tier 1, 2, and 3 use their authored 3-, 6-, and 9-player rosters; Raid Plus retains the boss's authored roster size.
- Five-slot party patterns are expanded across larger rosters. Three-player Raid rosters are explicit: balanced uses offense/offense/sustain, offense-heavy uses three offense, sustain-heavy uses offense/sustain/sustain, control-oriented uses offense/control/control, and summon-oriented uses offense/summon/summon.

The complete matrix contains 18 encounters, 996 aggregated results, and 2,988 deterministic seeded samples. Floor 7 adds five non-Stagger Tower rows to the assessed cohort. The Stagger-enabled subset remains 45 rows: 10 Tower composition rows and 35 Raid composition rows.

Across those rows:

| Content | Stagger rows | Break range | Average breaks | Maximum uptime | Maximum cap rate |
|---|---:|---:|---:|---:|---:|
| Tower | 10 | 1.00–4.00 | 2.60 | 7.18% | 100% |
| Raid | 35 | 0.00–4.00 | 1.03 | 5.56% | 100% |

The role- and composition-aware report produced 112 total target-band exceptions. Of these, 39 are Stagger metrics: 21 break-count, 13 first-break timing, and 5 break-cap exceptions. There are no Stagger uptime exceptions. The other 73 exceptions concern ordinary encounter outcomes such as win rate, survival, duration, timeout, intended composition success, or build sensitivity. Pure sustain rows in solo Idle and Dungeon content remain visible as observations but are not counted as completion failures.

All 45 Stagger-enabled composition rows remain visible as telemetry. Complete Stagger target bands are applied only to compositions authored as `Expected`; alternative, countered, and challenge routes no longer create false Stagger failures merely because they omit control. Raid compositions remain fully expected for now. Balanced, offense-heavy, sustain-heavy, and summon-oriented three-player groups intentionally contain no control member, while the control-oriented roster retains two control members.

Representative full-combat observations:

- After its first pressure-tuning pass, Tower Floor 5 produces one to 3.33 breaks in the three-seed full matrix; control-oriented groups are nearest the four-break cap. Its remaining composition spread should be resolved before changing the Stagger threshold.
- Tower Floor 10 produces three breaks for most compositions; control-oriented groups reach the four-break cap. Its zero win rate and 100% timeout rate must be addressed before interpreting the cap as a final Stagger-tuning problem.
- At Hives' Abyss Tier 1, the control-oriented composition reaches the four-break cap while the four compositions without a control member produce no break; every assessed composition still loses. Stagger role identity is now visible, while boss pressure remains the dominant completion failure.
- Hives' Abyss Tier 2 and Tier 3 show sharp composition-dependent outcomes and one control-oriented cap result. Their general combat curve needs tuning before their thresholds are finalized.
- The Sanguine Horror and Raid Plus samples often end too quickly to reach a break. Their 100% win rates are the primary signal; lowering their Stagger thresholds first would mask an undertuned encounter.

The control-oriented fixture now starts with Enchanted Fairy and Giant Worm. Both are progression-valid by the Region 1 completion anchor and carry authored Stagger-capable abilities. This makes the control label test an actual control contribution path rather than merely a balanced-attribute party with no usable `StaggerPower`.

## Recommended tuning order

1. Confirm which player-accessible Stun and Freeze abilities are intended to participate and author `StaggerPower` for any missing candidates.
2. Tune Tower and Raid encounter pressure until expected cohorts enter their provisional win-rate, duration, timeout, and survival bands.
3. Re-run the full-combat matrix with focused sample counts for every changed boss.
4. If otherwise healthy encounters still miss their break target, tune `BaseThreshold` per progression band before changing the global exponent.
5. Re-run the isolated suite after every content change and reject changes that push party-size spread above one break or Stagger uptime above 10%.

## Reproduction

Run the complete Stagger suite:

```powershell
pwsh -File build/run-encounter-calibration.ps1 -Configuration Release -StaggerOnly
```

Focus the report when tuning a single encounter or profile:

```powershell
pwsh -File build/run-encounter-calibration.ps1 -Configuration Release -StaggerOnly `
  -EncounterId tower.floor-05 `
  -CohortId reference `
  -StaggerProfileId balanced `
  -Samples 64
```

Generated JSON and Markdown artifacts are written beneath `artifacts/balance-calibration/stagger/` by default.

Run the full authored-encounter matrix, including Tower and Raid Stagger telemetry:

```powershell
pwsh -File build/run-encounter-calibration.ps1 -Configuration Release `
  -OutputDirectory artifacts/balance-calibration/full-combat-stagger
```
