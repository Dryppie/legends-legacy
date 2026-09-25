# Proposal-affinity pilot 02: saved-stage diagnosis

**Subsequent implementation:** The [v3 affinity-creation operator and coverage export](Tower-Affinity-Creation-Implementation.md) are implemented and verified. The captured preview creates nine distinct recipes across nine owners without fights. The diagnosis and accounting below remain the completed review record.

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The nine proposal differences are explained. Preservation had narrow coverage, and root 5 changed output through nomination competition. The selected candidate recipe was generated identically by both arms.** The closed study still has decision `AbandonThisConfiguration`; this retrospective review adds no fights, entropy, reservations or promotion claim.

The [execution report](Tower-Proposal-Affinity-Pilot-02-Execution.md) contains the scientific results. The new [reviewer](analysis/proposal-affinity-stage-review.py) authenticates every consumed file against the externally pinned science, admission and publication-verification manifests. It recounts all **24 saved search trajectories**, including panel scores, pruning, common-panel ranking, nomination and selection, and checks **6,040 shared training observations** for identical measured outcomes. It uses the previously verified held-out endpoint; it does not repeat the full compressed-battle audit or rescan live reservation history.

## Why only nine proposals changed

Both arms generated benchmark-parent single edits: 204 accepted positions per arm, with **no rejections and one construction check per position**. Every paired position has matching attempt, parent, operator and scheduled owner. The three selected authored affinity routes resolve to:

| Authored route coverage | Distinct essence pair | Active owner |
| --- | --- | ---: |
| Two Venomous Spiderling Poison routes | Venomous Spiderling + Viper | 8 |
| One Spider Queen Royal Venom route | Spider Queen Royal Venom + Viper | 8 |

These are **three routes, two essence pairs and three protected essences on one owner**. They describe authored compatibility, not measured combat synergy. Owner 8 contains Enchanted Fairy, Pack Howler, Spider Queen Royal Venom, Venomous Spiderling and Viper. Preservation therefore allows removal of only Fairy or Howler there; no other owner's build activates these selected affinities.

Owner 8 was scheduled at **18/204 positions (8.82%)**. At nine of those positions the control already removed an unprotected essence, so both policies produced the same recipe. At the other nine, protection changed which essence was removed. Consequently only **9/204 positions (4.41%)**, across seven roots, changed. This is sparse intervention coverage, not a failure to invoke the policy.

The captured generator shuffles parent essences using the same paired random stream, then candidate preservation filters the removal options. The replacement pool respects remaining essence families. It can therefore change the added essence too: root 4 is the only changed position here where both removal and addition differ. This mechanism is read from the pinned producing source; this review does not regenerate proposals or draw random values.

## All changed positions

All edits below affect owner 8. Screens use eight observations; continuation adds eight on a separate panel. Common scores combine those 16. Selection uses 40. Recipe IDs and complete paths are retained in the [machine review](../TestResults/proposal-affinity-stage-review-20260923/review.json).

| Root / wave / position | Control removal → addition | Candidate removal → addition | First screen, control / candidate | Subsequent path |
| --- | --- | --- | --- | --- |
| 2 / 2 / 5 | Viper → Elder Treant Thornstorm | Pack Howler → Elder Treant Thornstorm | 6 / 4 | Control: 12/16 common, nominated, 27/40 selection. Candidate pruned. |
| 3 / 1 / 5 | Royal Venom → Elder Treant | Enchanted Fairy → Elder Treant | 4 / 5 | Both pruned. |
| 3 / 2 / 6 | Royal Venom → Venomous Snake | Pack Howler → Venomous Snake | 3 / 4 | Both pruned. |
| 4 / 1 / 9 | Royal Venom → Shadow Harpy | Enchanted Fairy → Rotroot Shambler | 4 / 5 | Both pruned. |
| 5 / 1 / 6 | Venomous Spiderling → Hobgoblin Brutal Charge | Pack Howler → Hobgoblin Brutal Charge | 5 / 6 | Candidate: 12/16 common, then 4/8 wave-2 screen and pruned. Control pruned in wave 1. |
| 5 / 2 / 7 | Viper → Web Weaver Spider | Enchanted Fairy → Web Weaver Spider | 7 / 6 | Control: 14/16 common, nominated, 26/40 selection. Candidate: 13/16 common, beam rank 3, not nominated. |
| 7 / 2 / 7 | Royal Venom → Blackjaw Spider | Pack Howler → Blackjaw Spider | 6 / 5 | Control: 10/16 common, nominated, 16/40 selection. Candidate pruned. |
| 9 / 1 / 9 | Royal Venom → Frost Imp | Enchanted Fairy → Frost Imp | 4 / 6 | Candidate: 10/16 common, then 6/8 wave-2 screen and pruned. Control pruned in wave 1. |
| 12 / 2 / 1 | Venomous Spiderling → Brown Slime | Enchanted Fairy → Brown Slime | 6 / 7 | Both pruned. |

Across these nine changed positions, candidate recipes won 48/72 initial screen observations and control recipes 45/72. Three recipes in each arm survived their initial screen. Three changed control recipes reached final nomination; **no changed candidate recipe did**. None of the 18 changed variants was selected, so **all 18 same-root held-out measurements remain unknown**. These training counts do not establish which policy's changed recipes generalize better. A later measurement in another root must not be substituted for missing same-root evidence.

## Root 5: an unchanged recipe crosses the nomination boundary

The selected candidate, `dbbca5ce4f3d…`, is wave 2 position 3: **owner 2 replaces Illusion Fox with Frost Imp**. Both arms generated that exact recipe. Both measured 8/8 in its screen and 5/8 in continuation, giving 13/16 common wins and mean guardian health 2.679375.

Only the two highest-ranked challengers join all three references on the final selection panel. Common-panel ranking compares win rate, lower guardian health, higher survival, shorter victory duration and finally recipe ID.

| Final challenger rank | Control: recipe / wins / guardian health | Candidate: recipe / wins / guardian health |
| ---: | --- | --- |
| 1 | `c62b6ca80ae2…` / 14/16 / 3.09125 | `85d21a69bae7…` / 13/16 / 2.460625 |
| 2 | `85d21a69bae7…` / 13/16 / 2.460625 | **`dbbca5ce4f3d…` / 13/16 / 2.679375** |
| 3 | **`dbbca5ce4f3d…` / 13/16 / 2.679375** | `40eedcb16bb1…` / 13/16 / 3.3025 |
| 4 | `523fce98a68b…` / 12/16 / 11.0075 | `523fce98a68b…` / 12/16 / 11.0075 |

Preservation replaces the control's Viper-removing Web Weaver proposal (`c62b…`) with the Fairy-removing version (`40ee…`). Their 14/16 versus 13/16 scores and the secondary health ordering shift the unchanged Frost Imp recipe from rank 3 to rank 2. Root 5's other changed proposal survived wave 1 in the candidate arm but was pruned during the wave-2 screen, before this common-panel ranking.

| Final selection nominee | Control wins /40 | Candidate wins /40 |
| --- | ---: | ---: |
| Fixed benchmark `96b943…` | **29 — selected** | 29 |
| Unchanged challenger `85d21a…` | 28 | 28 |
| Other reference `8287f7…` | 28 | 28 |
| Primary reference `399bc7…` | 27 | 27 |
| Changed control challenger `c62b6c…` | 26 | Not nominated |
| Unchanged Frost Imp challenger `dbbca5…` | Not nominated | **31 — selected** |

Both selected outputs are **unique positive maxima**. The existing primary-reference tie rule does not decide either outcome. The candidate's observed +2/40 selection lead then reverses to **188/256 versus 203/256** on the paired held-out panel: 37 gains, 52 losses, net −15. This root accounts for the entire candidate–control difference in the pilot. The trace demonstrates how nomination changed; it cannot attribute a general combat benefit or harm to preserving Poison affinities, because the changed recipes were never held out.

## Next implementation

The next useful change is a separately versioned **affinity-creation proposal operator**, accompanied by a zero-fight coverage export. The existing preservation policy only constrains removals when an affinity is already active in its fixed parent. A new operator should explicitly form a declared producer/modifier pair on a legal owner, including a missing partner or a bounded coordinated replacement when required.

Before another combat study, the export should show selected route IDs and deduplicated pairs, eligible owners, active versus newly created affinities, legal replacement options, accepted/rejected attempts, unchanged recipes and realized distance from the parent. This would make intervention coverage reviewable before spending a fight budget. Authored routes must remain metadata, rather than being represented as proven synergy. Family, slot, owned-copy and attempt bounds still apply.

This is a development direction, not a claim that affinity creation will improve win rate. Keep racing and selection fixed for a future controlled generator comparison; do not tune a nomination cutoff or selection margin to rescue root 5. This pilot's held-out values are already consumed evidence and cannot qualify a revised mechanism. No subsequent combat study is declared or launched by this review.

## Implementation, verification and accounting

Changes are the new [review script](analysis/proposal-affinity-stage-review.py), [eight regression tests](analysis/test-proposal-affinity-stage-review.py), a small explicit report-version parameter in the shared [stage reconstruction helper](analysis/adaptive-racing-stage-review.py), this report and current-status links. The helper keeps its original adaptive-plan contract and defaults; proposal reports use their own `tower-proposal-racing-v2` envelope. The reviewer derives the saved primary and benchmark references and checks their compatibility with that helper. It retains attempted versus accepted position alignment and never converts missing held-out measurements to losses.

All **20 tests passed**: eight proposal-review tests plus twelve existing stage-review tests. They cover the complete retained trajectories, route/pair deduplication, absent held-out measurements, rejection-induced attempt misalignment, inconsistent shared outcomes, recipe/score/panel/nomination corruption and saved reference preference. Commands used the bundled Python runtime:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/test-proposal-affinity-stage-review.py'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-stage-review.py'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/proposal-affinity-stage-review.py'
```

The one formal review completed in **4.156 seconds**, retaining **853,038 bytes**, under a separately declared **180-second /64-MiB** engineering allowance with a hard wall-clock watchdog. Its complete allowance is charged: cumulative recorded charges are now **19,515.172 seconds /15,402,306,053 bytes**; cumulative declared maxima are **28,860 seconds /19,931,332,608 bytes**. Prior charges and scientific limits remain intact. Unit tests and exploratory development reads are separate engineering work.

The [sealed artifact manifest](../TestResults/proposal-affinity-stage-review-20260923/files.json) has external SHA-256 **`80111cc6050e6a1cb43d540023c78ec17cd9c9206c91dc6ee94e5b6157b27f5b`**. It binds the review, declaration and both producing Python files. The review records the three external source-manifest pins, the scientific closeout pin and each consumed file hash. Inputs are rechecked before sealing. The last separately verified reservation inventory remains 688,598 values across 248 files; this review neither reads the live registry nor claims a fresh inventory scan.

No command was blocked. No backend build was required for these Python-only analysis changes. There are no migrations, application configuration changes, deployments or gameplay-default changes. The sealed study, admission, prior verification and execution handoff were not edited.
