# Floor 7: paired pressure diagnostic — 30 September 2026

**Latest floor-7 trial (30 September):** The [Springtide ramp trial](Tower-Floor7-Springtide-Ramp-20260930.md) completed **15,360 fresh fights / 128 reservations**, retaining all **120 recipes / eight actual compositions**. The **0.25 base / 0.35 per-Abundance** candidate failed: two limited-equipment compositions met the minimum, but **41 recipes exceeded the ceiling**. The leading two-character resistance setups won **89/128 and 84/128**, compared with **1/128 each** in the prior independent screen; full resistance won **126/128 and 124/128**. Largest simultaneous upper bound: **99.81%**. **No confirmation or gameplay change followed.** All outcomes were independently recounted. **136 fresh Python tests and one native study test passed**; unchanged **138 backend passes / four skips** were authenticated and reused. Exclusions are **919,932**. Next is a finite, unallocated two-candidate refinement: retain base **0.25**, test per-Abundance **0.45 and 0.55** from the original live source, preserve every recipe and ceiling, then independently confirm at most one passing candidate. Add a separate refinement contract without weakening v3 or the exact v1 guard. Resource admission must precede allocation. No further candidate is implemented or allocated; no dungeon, supply or acquisition work.

Target: primary LL World Tower and offline Balance Harness. Explain the closed [120-recipe mixed-resistance screen](Tower-Floor7-Mixed-Resistance-20260930.md), preserving all previous accepted changes and all failed outcomes.

## Prospective protocol

Use exactly the six saved recipes selected in `TestResults/tower-floor7-pressure-diagnostic-proposal-20260930.json`: baseline, strongest two-character resistance subset, and full resistance for each of the two original winning compositions. Use the first sixteen screen seeds in their original declared order: **96 historical replays**, **zero new seeds or acceptance observations**. Do not select seeds by outcome, reorder Essences, change identities, extend the sample, retry failures, change catalogs or edit the completed source.

Authenticate the latest publication, complete 120-recipe archive, all six 128-seed saved panels, exact recipe pairing, current catalogs, compiled runtime and seed history. Preserve the entire original family; these diagnostic cases do not replace any acceptance controls. Five level-40 characters, five Essences each, Unique / Exceptional / rank-4 tier-1 equipment and all other progression assumptions remain unchanged.

Run the native detailed-replay CLI under the existing Windows process owner. Require the native saved-input/outcome comparison and exact equality of the entire report after removing only the added event log. Reconcile original-party damage, mitigation, healing, regeneration and first deaths, plus guardian damage and restoration. Distinguish original party members from summons. Inspect fixed windows **before 40 seconds**, **40 seconds onward**, and the whole fight; preserve within-tick event ordering when identifying damage before death.

Record Springtide damage per recipient, logged Abundance applications before each hit, Slow/Weaken events, pre-death recovery, original-slot damage output and guardian healing. Ordinary event logs do not universally expose exact stack modifications; report logged Abundance applications separately from exact native stack state. Guardian health trajectories are accounting balances from logged damage/restoration, with their final residual reported, not assumed native snapshots.

Before executing, project twice the measured per-replay time and size from the preceding 80-replay floor-4 recovery diagnostic. Require projections below **960 seconds** and **80% of 2 GiB**. Execution caps: **1,200 seconds overall**, **60 seconds per owned replay**, **64 MiB per log**, **2 GiB total**, **96 attempts**. Stop on technical or parity failure; no retries. Monitor supervisor output only while replay files are active.

Run fresh Python safeguards for the new selection/accounting helper and its reused diagnostic helpers. Authenticate and reuse the unchanged **138 backend passes / four intentional skips**; no C# or runtime rebuild is planned. Independently audit every completed replay and receipt before publication. Exclusions remain **919,804** throughout.

This scope is descriptive. It may justify a finite isolated candidate proposal, but makes no gameplay change and does not admit a new candidate screen or confirmation. No search-policy, dungeon, supply, acquisition, migration, configuration or deployment work.

## Completed result

**All 96 detailed replays reproduced their complete saved reports.** The independent collector audited **144,728 events**, reconstructed every recipient's damage, mitigation, healing, regeneration and first death, and checked all replay process receipts. No new seed or acceptance observation was created.

The fixed sample contains sixteen saved seeds per recipe. A is composition `dae6cc32…`; B is `0d375ed9…`. Counts of deaths before 40 seconds are out of 80 original characters per recipe. Medians describe fights with an original-party death. These are descriptive comparisons, not population estimates or new acceptance results.

| Saved gear | Median first death, A / B | Deaths before 40s, A / B | Mean guardian damage before 40s, A / B |
| --- | ---: | ---: | ---: |
| Baseline | 36s / 36s | 44 / 47 | 7,152 / 7,148 |
| Resistance on two characters | 36s / 36s | 11 / 15 | 7,149 / 7,285 |
| Full resistance | 48s / 48s | 1 / 0 | 6,837 / 6,939 |

**Early damage is the main observed equipment bottleneck.** Springtide contributes **85.8% / 84.8%** of baseline party health damage before 40 seconds, and **81.7% / 81.9%** with two-character resistance. Baseline parties deal slightly more opening damage to Eydis than fully resistant parties. After 40 seconds, however, their mean additional damage minus Eydis's healing is only **215 / 177**, compared with **2,856 / 2,616** for full resistance. The parties lose the damage output needed to finish the fight while Eydis continues healing. These duration-dependent late totals explain the recorded trajectories; they do not isolate every causal contribution.

The two-character setup protects slots **3+4** in A and **2+3** in B. Slot **2** in A and slot **4** in B are among the first casualties in **all sixteen** paired replays. Their death timing moves with the remaining unprotected character, so protecting two characters does not resolve the team's early survival problem. Springtide accounts for **all 26 / 23 first-casualty events** in these two partial-gear samples; simultaneous casualties can exceed the sixteen battle count. The full-family screen remains **1/128** for both limited-equipment leaders and **48/128** for the strongest full-resistance control; this diagnostic does not replace those results.

## Equipment and control effects

The equipment swap changes several attributes. For a light-armored character, maximum Health rises **2,359 → 2,833** and Resistance **23.40 → 54.88**, while Tenacity falls **116.92 → 0** and Health Regeneration **99.44 → 70.21**. Power remains unchanged. In the first-40-second window, all **320 Slow/Weaken applications per full-resistance recipe** land. Baseline A resists **246 of 308**, and B **238 of 302** applications in that window. Application opportunities differ because some baseline characters have already died. These observed event counts support preserving Tranquil Waters while testing the direct damage ramp; weakening it would particularly benefit the already strong full-resistance setup.

Eydis heals exactly **584 Health before 40 seconds** in every recipe in this sample. Greater whole-fight healing in full-resistance runs accompanies their longer survival. The current evidence does not identify early guardian healing as the cause of the equipment gap.

Logged Abundance applications precede the observed Springtide hits at 12, 24 and 36 seconds. Their event magnitudes are recorded as applications, **not exact native stack snapshots**; some stack changes are not universally logged. The proposed coefficient table below is authored arithmetic at a specified stack count, not a replay measurement or win-rate prediction.

Guardian health accounting matches the reported final value on all surviving guardians. The accounting residual is **−42 to 0**, with negative values confined to victories where reported guardian Health is zero. The report preserves those terminal residuals instead of treating the unbounded accounting balance as an exact native Health snapshot.

## Proposed next isolated candidate

Test one **Springtide damage-ramp change**: base Power coefficient **1.0 → 0.25**, per-Abundance Power coefficient **0.20 → 0.35**, with a matching description. Preserve guardian health/offense/penetration, Springtide targeting and cooldown, Abundance generation/cap, Endless Spring healing, Ancient Heartwood, Tranquil Waters and every other catalog definition.

| Abundance stacks | Current total Power coefficient | Proposed | Difference |
| ---: | ---: | ---: | ---: |
| 1 | 1.20 | 0.60 | −50.0% |
| 2 | 1.40 | 0.95 | −32.1% |
| 3 | 1.60 | 1.30 | −18.8% |
| 4 | 1.80 | 1.65 | −8.3% |
| 5 | 2.00 | 2.00 | 0% |
| 6 | 2.20 | 2.35 | +6.8% |
| 8 | 2.60 | 3.05 | +17.3% |

The rationale is to ease the observed early AoE deaths while retaining stronger later pressure against parties that already survive. **This candidate is not implemented, measured, accepted or applied.** Earlier survival gains could still make full resistance too strong; the later increase is a hypothesis, not proof that the ceiling will hold.

The existing `tower-ability-coefficients-v3` contract deliberately requires proportional base/per-stack changes and must keep that guard. Implement and test a separate, narrow contract for the explicit floor-7 tradeoff, with exact source hashes, updated description and rejection of all unrelated changes. Then retain all **120 exact recipes / eight actual compositions**, including every full-resistance ceiling control, in a newly declared **128-seed screen** and conditional independent **160-seed confirmation**. Acceptance still requires two distinct compositions with at most eight specialized items on two characters and all simultaneous upper bounds at or below 50%. Gates remain **25–44/128**, then **30–57/160**. Fresh resource admission precedes allocation. No panel or seed for that proposal is allocated here.

## Verification and accounting

**41 fresh Python tests passed**: sixteen resistance-diagnostic checks, twenty mixed-armor diagnostic checks and five base diagnostic checks. They cover immutable recipe/seed selection, same-tick death ordering, exclusive 40-second boundaries, raw identity and Essence order, actual equipment counts, full saved-report equality, guardian reconciliation and honest stack/health labels. The previous **138 backend passes / four intentional skips** were authenticated and reused; this scope changed no C#, compiled runtime or gameplay catalog.

All 96 native invocations matched the saved combat input/result and Tower outcome, and their complete reports matched after removing only the added event log. Every owned process exited successfully with no timeout or remaining child process. **136.719 summed process seconds**, **252,601,039 archive bytes**, **96 attempts / 96 completed replays**, **zero retries**. All limits were met. The six full historical recipe panels were also recounted: **768 saved outcomes**, not additional fights.

Exclusions remain **919,804**. No new study fights, seeds, migrations, application configuration, database actions, deployments, search changes, dungeon work, acquisition work or supply changes. No verification command remains blocked.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
TestResults/tower-floor7-pressure-diagnostic-prepare-20260930.py
Balance Harness/analysis/test-tower-resistance-diagnostic.py
Balance Harness/analysis/test-tower-mixed-armor-diagnostic.py
Balance Harness/analysis/test-tower-gear-diagnostic.py
Balance Harness/analysis/diagnose-tower-resistance.py --plan TestResults/tower-floor7-pressure-diagnostic-proposal-20260930.json --entry TestResults/tower-floor7-pressure-diagnostic-entry-20260930/entry.json --artifacts TestResults/tower-floor4-health-scaled-hall-precision-runtime-20260930 --protocol "Balance Harness/Tower-Floor7-Pressure-Diagnostic-20260930.md" --output TestResults/tower-floor7-pressure-diagnostic-20260930
TestResults/tower-floor7-pressure-diagnostic-collect-20260930.py
TestResults/tower-floor7-pressure-mechanism-review-20260930.py
```

Native commands are recorded individually as structured argument arrays. They use `dotnet <verified BalanceHarness.dll> tower-loadout-replay --run <saved evaluation> --battle <saved ID> --detailed` under `build/bounded_windows_process.py`. These are replay CLI calls, not backend test invocations. Existing backend evidence was generated through the required `build/run-tests.ps1`.

Changed maintained files: the new `diagnose-tower-resistance.py` helper and its tests, this report, the preceding mixed-resistance report's follow-up notice, handoff, status, gear-coverage notice and two harness guides. The older diagnostic contracts remain unchanged. Gear ownership and broad archetype coverage remain unestablished.

## Evidence

- Closed replay archive: `TestResults/tower-floor7-pressure-diagnostic-20260930`, manifest **`b4c68195009f91a91c16324e9b4317f7421efed1851e63ab798f9ad47b571a80`**. It retains the prospective protocol, declarations, every command/process receipt, native detailed log, summaries and completion.
- Independent evidence: `TestResults/tower-floor7-pressure-diagnostic-evidence-20260930.json`, SHA **`fb0e4474325bc1d4134bf741dadb3d966cf3c276d1be860b1e8330c5d982b373`**.
- Mechanism review: `TestResults/tower-floor7-pressure-mechanism-review-20260930.json`, SHA **`4d3cf3138ce23e5559008f3c8df37b48af60ea0be3e8c6defd094e8b29bc18b7`**.
- Unallocated ramp proposal: `TestResults/tower-floor7-springtide-ramp-proposal-20260930.json`, SHA **`3d20aa1ea773f15996d123abe9abcc5bd6246a1571039288934616e58798bd85`**.
- Entry/pre-edit archive: `TestResults/tower-floor7-pressure-diagnostic-entry-20260930`.
- Publication: `TestResults/tower-floor7-pressure-diagnostic-publication-check-20260930.json`.
