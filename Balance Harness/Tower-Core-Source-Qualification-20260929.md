# Tower core sources and first ascension — 29 September 2026

The missing dungeon-core channel is now qualified against the retained paid-run history. Under an explicit **unspent omitted-core** scenario, 120 of 240 current owner/server alternatives have a guaranteed material budget for all five first ascensions. Another 23 could afford all five depending on the missing draws; 97 cannot, even at their upper bound. No historical random roll, inventory credit, XP gain or ascension has been invented or retained.

This is a progression-source result, not a new combat result. The latest actual floor-5 panel remains 0/60, and its supplied Epic control remains 0/60. The repeating gear curve, stronger owned gear and `affinity-creation-with-benchmark-validation-v1` are unchanged. There is no basis here for boss retuning or an acquisition-time claim.

## Paid history and conditional material budgets

The model reads the exact archived 25,920-encounter personal checkpoint, the funded next-entry wave and the paid pending-Mines wave. It checks owner/run identities, entry costs, completion callbacks, successful XP claims and chronological cutoffs. Every retained family mastery completion count agrees with the reconstructed source list: **1,116 paid attempts, 1,100 successful completions, 16 failures and 375 first-family completions** across the 240 alternatives. These overlapping server paths are not independent players or new observations. Failed runs receive no completion cores and do not consume first-completion eligibility.

Production grade-I completions give an integer 3–5 Lesser Cores plus an independent 25% chance of one extra. The resulting probabilities for 3, 4, 5 and 6 are **3/12, 4/12, 4/12 and 1/12**, with mean 4.25. The first successful completion of each region-one family adds six. The display catalog's 3–6 range must not be interpreted as a uniform roll. Exact discrete convolution gives these conditional budgets:

| Successful runs | First families | Owner alternatives | Lesser Core range | Probability of at least 30 |
|---:|---:|---:|---:|---:|
| 1 | 1 | 97 | 9–12 | 0% |
| 3 | 2 | 7 | 21–30 | 0.05787% |
| 4 | 2 | 8 | 24–36 | 39.212% |
| 6 | 2 | 22 | 30–48 | 100% |
| 7 | 1 | 8 | 27–48 | 99.6521% |
| 7 | 2 | 22 | 33–54 | 100% |
| 8 | 2 | 76 | 36–60 | 100% |

Thirty cores fund five initial ascensions at the current five-owned-Essence count; the ten-ascended-Essence catch-up discount is unavailable. Each owner's budget is assessed separately, with no assumed material transfers. Additional-repeat-success bounds in the output cover materials only: they exclude failures, sigils, activity time and any first completion of a previously uncompleted family.

The projection assumes no earlier family completion before the declared history, no omitted-channel spending, and independent production reward draws. Historical `Random.Shared` core draws were not recorded. The current zero **credited** Lesser Core inventory is a deliberate scenario omission, not evidence that real players have no cores. Analytic probabilities are not measured player frequencies.

## Native qualification and retained state

Four disposable production completion/claim controls exercise first and repeat rewards for both families. They use the native completion applier, pending writer, item factory and claimer, verify the six-core first bonus and core range, verify matching `TreasureProgress`, and reject duplicate claims after the run-service-owned claim flag is set. Their fresh random draws remain diagnostic output only. Mastery is an already-awarded boundary in these probes; no historical mastery, XP or equipment is replayed.

For all **1,200 owned Essence references**, disposable native progression and ascension controls reject the current underlevel state, reject one XP below the threshold, reach level 10 with exactly the remaining XP, reject five cores, consume exactly six, and reject another ascension at level 10. Foreign ownership is rejected. Each successful control emits its native ascension notification. Neither hypothetical XP nor probe resources enter the saved owner state.

The original four level-8 Essences need **178,064–246,631 additional Essence XP** each; every new fifth needs **1,296,004**. These are native XP deficits, not farming-hour estimates. Ordinary levels do not scale combat ability strength; the ascension/evolution path does. A future training study must earn XP through declared activity and preserve character growth, concurrent training, prophecies and equipment rewards.

All fifteen parties pass production preparation again: **150 friendly members**, unchanged gear and unascended Essences. The output embeds the entire latest continuation unchanged, including both personal refresh boundaries, the population boundary, all 188 Tower attempts, server rewards and waiting-only clocks. The 15,934 eligible-branch equipment references, seventeen held server paths and total **25,831 equipment references across 512 owner/server alternatives** survive.

## Reproduction and verification

- Implementation: [source model](../LL/tools/BalanceHarness/TowerCoreSourceStudy.cs), [native controls](../LL/tools/BalanceHarness/TowerCoreSourceProbe.cs), [frozen plan](../LL/tools/BalanceHarness/Fixtures/tower-core-source.json), [tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessCoreSourceTests.cs).
- Bounded execution: [owner](analysis/run-tower-core-source.py), [independent auditor](analysis/verify-tower-core-source.py), [request and frozen inputs](../TestResults/tower-core-source-owner-20260929/request.json).
- Output: [result](../TestResults/tower-core-source-study-20260929/result.json), [independent audit](../TestResults/tower-core-source-owner-20260929/independent-audit.json), [corruption checks](../TestResults/tower-core-source-owner-20260929/auditor-negative-checks.json), [closeout](../TestResults/tower-core-source-owner-20260929/final-checks.json).

Manifest **`0f492878ef0e90e67c0ade6f2553d94798f43920247459759b85e3208cb9e4fc`**; result **`0532ef101b4500158e0efcea6cc437c1424caa1643a3d1b772ce4a32aee0c0f6`**. The owned native process finishes in **12.547 seconds** with zero active children. The independent audit passes without amendment and checks **4,905 frozen hashes**. Twenty-two detached corruptions are rejected without altering any archived output. Preserve this completed study and its `tower-core-source-build-20260929` runtime; do not rerun or extend it.

Backend verification used `build/run-tests.ps1`: **821 relevant regressions pass, 28 intentional opt-in skips**, plus the passing owned qualification. Eight focused tests pass separately. The first sandboxed build was blocked by local NuGet.Config access; the wrapper succeeded with escalation. A preliminary in-memory repository return-type mismatch was fixed before freezing; its failed log/TRX remain. No required verification remains blocked.

An optional `Get-CimInstance Win32_Process` I/O diagnostic was denied by the sandbox. Normal process inspection and the owned process receipt remained available; this did not block any correctness or preservation check.

No combat seeds were reserved or consumed. The latest exclusion ledger remains [the five-Essence return ledger](../TestResults/tower-fifth-return-combat-owner-20260929/seed-ledger.json), SHA **`5feb38e45e17328e9cac768139c7b9133fe20b0803a82fbab43d99912c9fd1cc`**, with **885,164** values including every prior unused reservation. No production source/content, dependency, configuration, migration, shared database, API host or deployment changed. Unrelated concurrent work is preserved.

## Next progression work

Continue from the unchanged owners in the five-Essence return, using this source ledger for explicit material scenarios. Implement a separately receipted conservative reconstruction of omitted cores, or collect them only through newly paid native completions; never reset old claim flags or call fresh probe draws historical loot. A conservative reconstruction must explicitly continue withholding unrecorded historical `TreasureProgress` and its downstream benefits, so it is not mistaken for a complete production-economy replay. The current projection itself is not spendable stock.

Run forward earned training from the saved mutable runtimes, conserving native activity XP, prophecy claims, core spending and every stronger owned item. Ascend only personally owned, actually trained Essences using that scenario's individually funded inventory. Start with the affordable ascensions rather than requiring all five by assumption. Freeze changed preparations before any new bounded Tower evaluation. Keep historical resonance, quest-start assumptions, failures, unused seeds and both personal refresh boundaries explicit. Earned floor-10/11 progression and measured player acquisition pace remain unfinished.
