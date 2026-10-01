# Floor 5: single-point gear refinement — 29 September 2026

**Subsequent work:** the [detailed diagnostic and joint calibration](Tower-Floor5-Joint-Calibration-20260929.md) completed 32 verified saved-seed replays, identified Seal magical damage as the main gear gap, and rejected three joint settings without an application. Latest exclusions 910,839. This report retains its original closed health-only result.

**Completed: `NoEligibleGearSetting`.** The +4.5% candidate kept the strongest recipe at **43/128**, within the **44/128** ceiling, and two actual compositions met the minimum. Its best health-and-regeneration recipe won **23/128**, below the required **26/128**. No confirmation or guardian edit followed. Floor-5 health/offense stays **3.3102803755 / 4.4702934848**. This refinement ran **13,184 fights / 128 reservations**; all three scopes in this continuation total **65,920 fights / 640 reservations**, including the preserved technical failure. Latest exclusions **910,647**. No active study remains.

## Prospective scope

Target: primary LL World Tower and the offline Balance Harness. The [closed recovery](Tower-Floor5-Gear-Recovery-20260929.md) found +4% health close to its strongest-recipe screen ceiling, with 47/128 versus a limit of 44; its best health-and-regeneration recipe won 32/128 and its second composition's best recipe won 27/128. Test one finer **+4.5% health** setting. This is a fresh, bounded selection/confirmation experiment. Preserve the source-byte failure, all rejected settings and their reservations; do not pool or extend any earlier panel.

Use all **103 exact recipes / fourteen actual compositions / seven gear profiles** from `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`, manifest **`46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`**. No search, reimports, dropped variants, identity changes or Essence permutations. Preserve every raw scenario; count actual compositions by per-slot Essence sets.

Player budget remains ten level-40 characters, five level-1 unascended/unevolved Essences each, tier-1 Epic/Fine/rank-3 equipment, baseline rolls and no styles. Full ownership is hypothetical. Gear progression, stronger retained items, withdrawn supplies, all other floors and the supported search algorithm remain unchanged. No dungeon or acquisition work.

Current floor-5 health/offense: **3.3102803755 / 4.4702934848**. Whole-Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Initial exclusions **910,519**. The only candidate is health **3.4592429924**, rounded to ten decimal places from the original health × 1.045. Offense, defense/resistance 2.65, penetration/regeneration 1 and every other field stay unchanged.

## Frozen protocol

1. Authenticate the source, the recovery's closed evidence, current raw input pins and preserved executable hashes. Continue the already authenticated entry-only LF-to-CRLF exception for the historical affinity-test source; new native requests must match their raw source pins throughout execution. Use `TestResults/tower-floor10-diversity-supported-build-20260929` and `TestResults/tower-floor5-gear-recovery-verification-reuse-20260929.json`. Reuse the verified 91 backend passes / four opt-in skips and 14 Python tests at entry; do not call them rerun. Freeze this protocol, scripts and **19 fresh selection safeguards**. Prepare all 103 exact recipes without fights or allocation.
2. Evaluate the single +4.5% setting on **128 fresh seeds / 13,184 fights** across the complete family. Require every recipe at most **44/128**, at least two actual compositions with a recipe at least **26/128**, and at least one such recipe on **each of health-and-regeneration and resistance-and-health**. There is no second candidate or replacement setting.
3. If eligible, freeze this setting and run one independent complete-family **184-seed / 18,952-fight confirmation**. Apply approximate simultaneous 95% Bonferroni-Wilson bounds over all 103 exact cells: every upper bound at most 50%, at least two actual compositions with a lower bound at least 10%, and qualifying recipes on both required gear profiles. Native `Pass` alone is insufficient; gear variants do not inflate composition counts. The corresponding qualifying observed count is 33–68/184, while weaker cells may have fewer wins provided the diversity and gear requirements hold.
4. Only after all confirmation gates pass, apply the confirmed floor-5 health scalar locally. Verify all **18,952 inputs and 103 full replays** with confirmation seeds and zero new values. Run the relevant 91-test backend suite through `build/run-tests.ps1` after application and retain its TRX. No deployment, migration or configuration change.

Admission projects the 128-seed selection workload from the original complete 96-seed source screen and confirmation from the fresh 128-seed panel; both must stay within **80% of 840 native seconds / 2 GiB**. Each scientific phase is capped at **20,000 fights / 840 native seconds / 900 process seconds / 2 GiB**. Application parity uses 600 native / 660 owner seconds. Maximum **32,136 study fights + 103 conditional replays = 32,239 executions**, **312 fresh reservations**. Stop on technical failure, failed selection, resource rejection or failed confirmation. No retries, added settings, sample extensions, pooling or relaxed gates.

Driver: `TestResults/tower-floor5-gear-refinement-driver-20260929.py`. This scope tests equipment coverage among related poison teams. It does not establish broad non-poison archetypes, pacing targets or ordinary acquisition. Preserve ignored local evidence separately.

## Execution record

Preparation and the complete 128-seed selection panel passed their native fixtures and archive audits through `build/run-tests.ps1`. Both used exactly the original **103 recipes / fourteen compositions / seven gear profiles**. The strongest two compositions won **43/128 and 33/128**, both on resistance-and-health. Their health-and-regeneration variants won **23/128 and 15/128**. Thus the ceiling and composition-count gates passed, but the required alternate-gear gate failed. The candidate was not confirmed, applied or replayed for application parity.

For context, the completed 128-seed panels from this continuation were:

| Health increase | Strongest resistance-and-health recipe | Best health-and-regeneration recipe | Required strongest maximum / gear minimum | Result |
| --- | ---: | ---: | --- | --- |
| +3% | 57/128 | 33/128 | 44/128 / 26/128 | Strongest too high |
| +4% | 47/128 | 32/128 | 44/128 / 26/128 | Strongest too high |
| +4.5% | 43/128 | 23/128 | 44/128 / 26/128 | Alternate gear below minimum |

These panels use different fresh seeds. Their differences include sampling uncertainty; they do not prove the exact response to a half-percentage-point health change or that every health-only setting is infeasible. None passes all frozen selection gates, and no observations are pooled into acceptance evidence. The earlier +2% 64-seed screen was also rejected. The initial technically failed +3% panel remains inadmissible and is not included in this table.

### Saved-report review and next work

A read-only review authenticates **256 saved reports** for the leading `6b30bf98…` composition on the same 128 seeds at +4.5%, comparing only the two required gear profiles:

| Paired outcome | Seeds |
| --- | ---: |
| Both gear profiles win | 20 |
| Resistance-and-health only wins | 23 |
| Health-and-regeneration only wins | 3 |
| Both lose | 82 |

Health-and-regeneration's mean displayed duration is **117.32 seconds**, versus **122.17 seconds** for resistance-and-health; mean guardian health remaining is **11.88%**, versus **7.61%**. All losses are recorded as defeats, not time limits. These are descriptive summaries from already saved fights, not confirmation or causal attribution. Event logs are absent, so the archive cannot identify which incoming damage, healing or regeneration events caused the gap.

**Next: diagnose the damage and healing difference between the two gear profiles, then use that evidence to design a small joint health/offense calibration if warranted.** A bounded replay diagnostic can use saved seeds without creating new strength evidence. Keep all 103 recipes and both gear acceptance gates. The current evidence supports investigating survival pressure; it does not yet justify a particular offense adjustment. Further health-only increments risk weakening the alternate gear before a complete-family setting qualifies. No new diagnostic, scalar grid, seeds or combat is queued by this report.

The old 89-cell confirmation remains historical accepted evidence at the unchanged live setting; it does not establish balance for the expanded 103-cell family. Broad non-poison archetypes, other floors' gear concentration, pacing targets and ordinary acquisition remain separate gaps. Do not redirect this continuation to dungeons, replace withdrawn supplies or restart search-algorithm experimentation.

### Verification and evidence

**19 fresh selection safeguards** passed. Both native fixtures passed and their process receipts confirm exit zero, no timeout and zero active descendants. Collection checked **658 unchanged input/runtime pins**, exact family retention, all **13,184 attempts/completions** and **128 new reservations**. Native phases used **344.66 seconds**. The 91 backend passes / four opt-in skips and 14 Python tests were authenticated and reused at entry; they were not rerun. Conditional confirmation, application parity and post-application regression were not run because selection failed. No production code, guardian data, configuration, migration, database or deployment changed.

- Complete refinement selection: `TestResults/tower-balance-pass-floor5-gear-refinement-selection-study-20260929`, manifest **`f6ce1d02e51dd3047fa0011f9f64f481da1e777735097b7ebae1712a8ee5adb1`**. This archived candidate uses health **3.4592429924** and is **not applied or accepted**; live health stays 3.3102803755.
- Reconciled evidence: `TestResults/tower-floor5-gear-refinement-evidence-20260929.json`, SHA **`1b19308373fc636aa19539ca06990aa8dc4197825549b1a19b9065187894c8e0`**.
- Paired saved-report review: `TestResults/tower-floor5-gear-refinement-paired-review-20260929.json`, SHA **`c28e1ecc0a134aa22a904261c59111762fd3eb7b3f1d423962e8b3a1a73e60ed`**; zero fights, zero new seeds, not used for acceptance.
- Latest ledger: `TestResults/tower-balance-pass-floor5-gear-refinement-selection-owner-20260929/seed-ledger.json`, SHA **`19a3beec08fb12591a11eb3df1b32da6f38020d2779b8a122108a9777cd5722c`**. Keep every linked ancestor and later ledger; union **910,647**.
- Whole-Tower SHA remains **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Preserve the earlier +1% floor-10 application and all other accepted settings.

The [initial calibration](Tower-Floor5-Gear-Calibration-20260929.md) consumed **13,184 fights / 128 values**, including 6,592 fights in a failed source-byte check. The [recovery](Tower-Floor5-Gear-Recovery-20260929.md) consumed **39,552 fights / 384 values** and closed without a stable setting. Combined with this refinement: **65,920 executions / 640 reservations**, no confirmation or parity replay. Every closed scope, raw source pin, rejected candidate and failed archive remains preserved; no historical input was repinned to make a failed run appear successful.
