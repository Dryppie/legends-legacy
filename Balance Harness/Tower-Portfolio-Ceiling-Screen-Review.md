# Captured-v19 ceiling correction screen — complete, Unresolved

Completed **14 September 2026** for the offline `LL/tools/BalanceHarness`. All **129,536 fights** finished: **253 complete recipes × four guardian factors × 128 shared seeds**. Native execution, complete native reconstruction and the independent audit all passed. There were **zero retries, replays, additional seed allocations or resource stops**.

**No factor met all frozen selection rules.** The screen result is **Unresolved**, with no selected factor or nomination for full-family confirmation. Reliability remains **Fail 1/3**, adoption **Hold**, and the preceding separate confirmation remains **Fail / Fail**. No gameplay application, migration, configuration promotion or deployment follows.

## Result and interpretation

The same recipe, `team-040e60d3dbc5c127321653c47ed3a9d3`, had the highest observed win count at every factor. Each row below includes all 253 recipes. Intervals use the frozen approximate Bonferroni-Wilson calculation across **all 1,012 cells**, including baseline and unselected factors; draws count as non-wins.

| Guardian Health / Power | Strongest wins / 128 | Observed rate | Adjusted interval | Recipes with upper bound >50% | Recipes with lower bound ≥10% | Selection |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Baseline | 107 | 83.59% | 66.69–92.84% | 122 | 142 | Baseline ineligible |
| +4% | 87 | 67.97% | 50.03–81.81% | 67 | 96 | Ineligible |
| +8% | 51 | 39.84% | 24.43–57.57% | 1 | 28 | Ineligible: ceiling evidence |
| +12% | 21 | 16.41% | 7.16–33.31% | 0 | 0 | Ineligible: viability evidence |

At **+8%**, every observed rate is below 50%, and 28 recipes establish the required viability lower bound. However, the strongest recipe's **57.57% upper bound** fails the whole-family ceiling rule. This does not establish a true ceiling breach at +8%; it leaves that requirement unresolved.

At **+12%**, all 253 upper bounds are below 50%, and the strongest observed rate is within the permitted 15–40% selection range. However, its **7.16% lower bound** is below the required 10%, and no other recipe establishes viability. This does not prove that all recipes have true win rates below 10%.

| Factor | Observed rates >50% | Lower bounds >50% | Zero-win recipes | Total wins / 32,384 | Median wins / 128 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Baseline | 87 | 15 | 62 | 9,657 | 35 |
| +4% | 7 | 1 | 90 | 5,388 | 16 |
| +8% | 0 | 0 | 112 | 2,066 | 1 |
| +12% | 0 | 0 | 166 | 426 | 0 |

For each increased factor, 191 recipes had fewer observed wins than baseline and 62 tied; none had more. Total differences were −4,269, −7,591 and −9,231 wins. These are descriptive comparisons on the shared schedule, not a new paired significance test or a proof of monotonicity outside the measured points.

The grid does not test an intermediate factor, and its 128 samples per cell do not establish either missing bound. Do not interpolate a certified setting, top up this sealed study, reset the family correction, remove recipes or extend its grid. Any further experiment needs a new explicit scope and frozen design. The separate **43,879-entry** retained inventory still requires context/materialization auditing and its own complete acceptance protocol. Current-checkout gameplay remains a separate producing-version decision. This screen does not isolate Pack Howler's causal contribution.

## Execution, preservation and seed history

The user authorized launch after reviewing the [bound preparation](Tower-Portfolio-Ceiling-Binding-Review.md). The prelaunch audit found the same **142 registered history files**, with every path and hash unchanged. It verified the exact prepared inventory, controller sources and producing executable. The following native prepared check passed in **6.531 seconds**.

Execution retained captured-v19 gameplay, all 253 recipes, original identities, roles, Essence order and nominations. Each ten-character lineup comprises **two five-player parties**. The four prepared copies differ only in the declared floor-5 guardian Health/Power scalars. The order remained baseline, +4%, +8%, +12%, then ordinal recipe IDs and the frozen shared seed order. Each factor completed exactly 32,384 durable starts/completions and passed full report and roster reconstruction before the next began.

The [authoritative ledger](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/seed-ledger.json) remains **481,347 distinct reservations**. All 128 previously prepared screen values were used by this complete screen. All **512 unused original v19 confirmation values** remain excluded. Execution allocated nothing. The original v19 experiment remains sealed with its original **86,016 fights, no confirmation, Unresolved / Hold** history; the completed separate confirmation and other sealed studies were not rerun or rewritten.

The [setup receipt](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/setup-charge.json) contains the frozen `launchProtocol`, exact wrapper source/hash, commands, preflight results and checkout snapshot. Its top-level readiness fields and earlier closure describe historical prelaunch states; **execution.json and screen-result.json are the completed status authorities**. The receipt became immutable when its hash was recorded in `started.json` and was not changed afterward. No external balance-output console log or additional post-seal study file was created.

## Measurements and resource accounting

| Measurement | Result |
| --- | ---: |
| Charged setup through launch | **1,332.858 seconds / 22.21 minutes** |
| Whole native run command, including final publication and inventory verification | **2,841.187 seconds / 47.35 minutes** |
| Native performance snapshot, before final metadata/inventory work | **2,836.127 seconds** |
| Separate complete native verifier | **123.375 seconds** |
| Independent all-cell, schedule, interval, selection, journal and hash audit | **4.297 seconds** |
| Total charged active work through the independent audit | **4,301.764 seconds / 71.70 minutes** |
| Complete study files | **11,793** |
| External immutable setup files | **15 / 13,659,649 bytes** |
| Total new balance output, including external setup and its retained copies | **2,215,794,944 bytes / 2.063620 GiB** |
| Remaining output allowance at verification | **6,374,139,648 bytes** |
| CPU time at the native snapshot | **2,266.109 seconds** |
| Peak process working set | **827.55 MiB** |
| Cumulative managed allocation at the native snapshot | **1.396 TiB** |

All work remained within **129,536 attempts, 14,400 active seconds and 8 GiB**. The setup charge includes the previous 1,080.202 seconds plus launch checks and a disclosed conservative **240-second allowance** for the initial instruction/source review before the measured wrapper. Human waiting between completed preparation and launch was not charged. The closure below conservatively includes the subsequent completion review and preservation work.

The [performance trace](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/performance.json) persists detailed calls, inclusive/exclusive durations, CPU, allocations and memory. Summing **exclusive** durations by final stage name gives:

| Instrumented stage | Calls | Exclusive seconds | Share of 2,836.127-second snapshot |
| --- | ---: | ---: | ---: |
| Combat simulation without checkpoints | 129,536 | 1,834.922 | 64.70% |
| Outer durable start/completion flushes | 259,072 | 366.251 | 12.91% |
| Compact archive attempt flushes | 129,536 | 197.694 | 6.97% |
| Compatible-report hashing | 388,608 | 140.417 | 4.95% |
| Compact read/decompression/deserialization | 16,192 | 65.455 | 2.31% |
| File hashing | 120,924 | 31.565 | 1.11% |
| Owned-storage checks | 6,161 | 13.455 | 0.47% |

Owned-storage checks took **15.867 seconds inclusive**; nine final storage audits took **5.145 seconds inclusive**. Parent/child inclusive durations overlap and must not be added. Durable attempt charging remains intact despite its measurable cost. The factor execution phases measured **708.875, 733.268, 649.207 and 614.831 seconds**; the four outer factor reconstructions together took **118.908 seconds**. Final native metadata, inventory and verification continue after the performance snapshot and are included in the measured command duration.

This is a measured full-screen runtime, **not a paired reference/candidate performance experiment**. Different fights, recipes, archive topology and guardian factors prevent attributing a speedup against the historical 292.69-minute v19 run or the 52.46-minute separate confirmation. Historical v19 timing percentages remain unavailable. The earlier [accounting measurements](Tower-Discovery-Performance-Review.md) and [parity closure](Tower-Discovery-Parity-Closure-Review.md) retain their original scope and limitations; this result does not revise those ratios or retroactively satisfy a different benchmark threshold.

## Verification and reproducible provenance

All three native commands returned **exit 0**. The run's execution status is Complete while its statistical selection status is Unresolved; these are different results. The separate verifier fully reconstructed all 129,536 saved records, prepared rosters, definitions, execution identities, outcomes, attempts and final inventory.

The independent audit required all 1,012 evidence cells to have the identical ordered 128-value schedule, counted each outcome, checked the full `SC` journal sequence and reproduced every interval using Python `statistics.NormalDist` within **1e-8** of the harness's Acklam approximation. It independently reproduced the factor assessments and null selection, checked every final inventory hash, producing executable/source hashes, registered-history hashes and old sealed manifest markers. It also recorded concurrent checkout changes without editing them. Both native and independent verification performed **zero combat**.

These are the commands that ran from the repository root; the execution command is provenance and must not be repeated on this completed study:

```powershell
$controller = 'TestResults/balance/tower-ceiling-controller-20260914/final-capture/executable/BalanceHarness.dll'
$study = 'TestResults/balance/tower-captured-v19-ceiling-screen-20260914'
dotnet $controller tower-ceiling-check $study   # Before launch; 6.531 s, exit 0
dotnet $controller tower-ceiling-run $study     # Once only; 2841.187 s, exit 0
dotnet $controller tower-ceiling-verify $study  # Zero fights; 123.375 s, exit 0
```

The frozen wrapper and independent calculation are retained in `setup-charge.json` → `launchProtocol.source`, with their source hash. Do not replay the wrapper: it includes the once-only combat launch. The final command above is the read-only completed-state reconstruction command, distinct from the preparation-only check. Any future diagnostic repetition needs its own scope and accounting.

No backend source was changed in this execution task. The producing controller and focused test sources retain the **70-test verified build**, originally checked through `build/run-tests.ps1`; no source change justified rerunning that suite. Required execution and verification commands all completed. Concurrent frontend work and pre-existing combat-engine/test edits were preserved.

| Binding | SHA-256 |
| --- | --- |
| [Completed inventory](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/screen-files.json) | `81a3217fff5d9035efd5540a8e8c42013424dd504e970e19e27bad4a2f5f8da8` |
| [Frozen protocol](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/protocol.json) | `2497175ae4e323937fe561495666b1bf1eb2c1e5398bc5281bce4a570606f319` |
| Seed ledger | `ed43c2bd3a0ccd6f9b488f6817f04a07e7c35e72eec4a59283707d1db41b3c34` |
| Bound launch/setup receipt | `ce82375569660a320746ca27f183df0bdbaf136bb22f29f20f7d297beef9e6fb` |
| Durable attempt journal | `b1914c013e5b22eb645d25da9dd7038e2fe9fbcae9ee1ab65d58b9a342542ce9` |
| Producing BalanceHarness DLL | `272e861a725bd3bbfec0ccb4f3595f50b3d4b53056bca29d6c0d2d34f6c0bc32` |

The changed engineering documents are this review and the active discovery implementation/plan, acceptance policy, coverage handoff, search reset and harness README. Historical reviews, frozen protocols and all completed packages remain unchanged. No new gameplay content, migration, service configuration or deployment is part of this completion.


## Final documentation and preservation closure

The final read-only check confirmed the unchanged native inventory and all registered-history paths/hashes after documentation updates. All six handoffs link this completed result. Scoped and whole-checkout `git diff --check` passed; Git reported only line-ending normalization notices. Existing and concurrent frontend work was preserved. For conservative global accounting, the receipt below additionally charges the full final sizes of the six handoffs and this new review, although the handoffs already existed. All retained balance-output bytes remain unchanged.

<details>
<summary>Point-in-time closure receipt and preserved concurrent changes</summary>

```json
{
  "status": "VerifiedCompleteStatisticalUnresolved",
  "checkedUtc": "2026-09-14T14:44:20.434974+00:00",
  "activeSecondsConservativeThroughDocumentation": 4703.37454330002,
  "activeTimeBoundary": "Updated setup charge plus continuous wall time since launch ready, including final verification and review; includes a one-second publication allowance. No future user waiting is charged.",
  "remainingGlobalSeconds": 9696.62545669998,
  "maximumFights": 129536,
  "started": 129536,
  "completed": 129536,
  "retries": 0,
  "newSeedsThisExecution": 0,
  "reservations": 481347,
  "screenSeedsUsed": 128,
  "originalV19SeedsStillUnused": 512,
  "studyFiles": 11793,
  "externalSetupFiles": 15,
  "registeredHistoryFiles": 142,
  "registeredHistoryPathSetAndHashes": "Unchanged through closure",
  "completedInventoryHash": "81a3217fff5d9035efd5540a8e8c42013424dd504e970e19e27bad4a2f5f8da8",
  "producingHarnessHash": "272e861a725bd3bbfec0ccb4f3595f50b3d4b53056bca29d6c0d2d34f6c0bc32",
  "nativeRunExitCode": 0,
  "nativeRunCommandSeconds": 2841.187,
  "nativeVerifyExitCode": 0,
  "nativeVerifyCommandSeconds": 123.375,
  "independentAuditExitCode": 0,
  "independentAuditSeconds": 4.297,
  "independentAuditChargedSeconds": 4301.763860300038,
  "independentIntervalTolerance": 1e-08,
  "selection": "Unresolved",
  "selectedFactor": null,
  "allArchivesPreviouslyReconstructed": 129536,
  "immutableNativeInventory": "Unchanged; no post-seal files or receipt mutations",
  "balanceOutputBytes": 2215794944,
  "engineeringHandoffBytesChargedConservatively": 441159,
  "newReviewBytes": 38930,
  "totalConservativelyChargedOutputBytes": 2216275033,
  "remainingGlobalBytes": 6373659559,
  "maximumGlobalBytes": 8589934592,
  "scopedDiffCheckExitCode": 0,
  "globalDiffCheckExitCode": 0,
  "gitWarnings": "Only LF-to-CRLF working-copy notices; no whitespace errors.",
  "handoffHashes": {
    "Balance Harness/Automatic-Tower-Team-Discovery-Implementation.md": "870c45fddd8eff5f548ff9d8f55c3a2b82fda0c65a82a33e8c405fa3485b6d79",
    "Balance Harness/Automatic-Tower-Team-Discovery-Plan.md": "b8609335b8aa45272a6581ea490beef6e4a3d4882d67c135d80b42763dc248c6",
    "Balance Harness/Tower-Balance-Acceptance-Policy.md": "9f004ae8f9b99a4dc681f9563783dda8c73e120c05a413809d255b44dd62553d",
    "Balance Harness/Tower-Coverage-Replication-Plan.md": "899b8bedea614e08b4a98c1ebe54c6946bc8d308241ded2e333a9e99e635f6ec",
    "Balance Harness/Tower-Search-Strategy-Reset.md": "30b666e4bd53a09d7344d9e48586c6c0b5ad65e873050a2419cf360f2bf68070",
    "LL/tools/BalanceHarness/README.md": "5c6a4613de03facfb0aed4260561d09e1b6b94e06ec199469f4fc817f2014482"
  },
  "reviewBodyHashBeforeClosure": "7a79291327d9ae55a17065e9124bd4234caf6295093e07eee97cd1b743970ff3",
  "preservedHarnessAndFocusedTestSources": "Unchanged from launch snapshot",
  "concurrentExistingChangesPreserved": [
    {
      "path": "LL/src/Presentation/ll/src/app/app.component.html",
      "before": "74e8a6f4562e9ca573849864e51b5a986389a1720d6f4cf373d77c16de9c1da4",
      "after": "33646422756c427045eb33adf351dc5d8c73ffab7d350f09c5efd92e17b39e5c"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/app.component.spec.ts",
      "before": "b1d69a32b96513713f3da98c50f31cccd139f189af9194943924b32b15686798",
      "after": "b27d77521943af20d91d3deda8f4b3ec58338976a9f11f5d9558d311f5a17c2a"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/app.component.ts",
      "before": "1b6ca6374ebd5fe6b8c1fd62a94ff5226e40b229bc706148325ce8b430750a8e",
      "after": "a03aeeb54f850e975f36e268bf06f40110a090b6ea44010fc1b05d7a0f4d2222"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/error-pages/not-found-page/not-found-page.component.html",
      "before": "4b04b1316097ba2c6c124cb6229f929f11054d3deff8eff80f9b78bc6b8542e3",
      "after": "88095cea965900c273291c0a1a6706056241a3c7d11bdc0be354a1f8f7e7a72e"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/achievements/achievements.component.html",
      "before": "c700da8df8e79e36c41f1b80dd6e6c0d3f7ca884520f9966f72dd9f13971fd26",
      "after": "a213b310cb052a56861ef1b3dfc26c3fa8674d45962d036203328ca34ef84425"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.html",
      "before": "22443b1d6dd82558a5f4de22a02069b8bd798725fa5fa30ce39f6659e2a93341",
      "after": "068a002fe04391eefab94b6adde8c46db5378d435c64539cdc0f1b084b4b6925"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.scss",
      "before": "6b7321983e1854b889043f0aaaaecd3769bfed4f2375fcd4cf50689d3d3e1f6d",
      "after": "9c37f7632a7dc743e02c266eb4c2172a82df55adcbcdf746842f1c2551b8d5af"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.spec.ts",
      "before": "cb2d4e4371dc321233243b7d83ec12ee20dd8f4c8c5a77eda6fc72d6084e462a",
      "after": "2447c0b3447e6c3158827679bd611dfe32e902fd04bbd65c7870ca624d7be8c8"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.ts",
      "before": "135ca5071142d174c4e26cc96efd5453ab478479b5e4600b97e6b8a3929b02a6",
      "after": "d9584cdfa6a07a636dfd253e478a54b768d5698c0ebf38a044fc7ecc26c9d031"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/combat-styles/combat-style-overview.component.ts",
      "before": "795dc54a68e8d1bad0fe9996ddc78370e2ff0a4b1c3f9e09b301033edbff7d0e",
      "after": "c41fc32e21aedab0ad4b9d116088fae0a30e70c625743b32af61fa0ce90a0072"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/combat-styles/combat-styles.component.spec.ts",
      "before": "2269da6e4b5eec5a254e271e4aac85baf0ce649e4811e25a61e95e69913f362c",
      "after": "355f8f9327f177158a9682fea0b064597d6b1c123fc752eb3af30a88f689fecf"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/essences/essences.component.html",
      "before": "8b167691320e4231dd6c9f925678ba8681fc8f4391932bffed9bc641c9af666a",
      "after": "5a69f04f2d9723153dbbc071146c574e066b24619b7898cb07395702a687e3db"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.html",
      "before": "999c503a6cb0e506b50c78525a026a815e6f43b6fe37026205fd046ccb80f576",
      "after": "e56dc2c1d1780031828d5f4dab67e47ce73785e11aefe894312d56fa14029d06"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.scss",
      "before": "768673aee6bc43ccec5222d297d08e60a18b523818a83a6c8b3a6daab153a46e",
      "after": "90130e18734b5724dcba37b18c8312c33c7ab8f5c8b31c79b814d6f1997b526b"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-archive.component.html",
      "before": "693c838ff6bcd967df9789f4e4179e32686942ba5cdd2cd1bed79852d1d06d8d",
      "after": "728ceec2fab792461982b2e4043015e9f6f34b7a16a714ed5857a3edee0e7544"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-upgrade-card/soulstone-upgrade-card.component.html",
      "before": "a05a2520c1d5b1a08cd072238f912b764f1b3e82c53361fa95b6162cb08ef474",
      "after": "2adce46b4832f8881edbd3aebd1bfe44fc80ba3caa6e41fceeb0834a32e933ab"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/colosseum/arena-battle/arena-battle.component.html",
      "before": "c5d687d7c1eba18987fd538c52ebb0ee592e8d5557416f9f91abe798a7235c06",
      "after": "5290f70ac0b7abdee25e7f8a03ad8350502cac98873201e752bfd3244fcda02f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/colosseum/champions-market/champions-market.component.scss",
      "before": "b11df086649acca53876b3294d71a295777a8937dfb952801d73bfada2f2170b",
      "after": "e5d12608336d2a2ee2e9649104cbaa84f7cac4f8144a6ee28fdaca68a57c4375"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/colosseum/colosseum.component.html",
      "before": "507dab29a9dad37810f5451c0b6123795f93bc06db69e7d9efda9a72971260ea",
      "after": "6f5312b86d4508c9b84c9585dffaf3cab34aac5b08b8622c0f715b1cfa33ce50"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-grounds/tournament-grounds.component.scss",
      "before": "ff338f358e88b34f7b7c016ec1bdf5a6cfffe2de21d5f32bf7ed4a110eb47649",
      "after": "d0a7600c26f3dee7a872f9ea9c4d71a1d3af01f789111ac876cf679f21baf7b4"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/guild.component.html",
      "before": "c42f7ecb7bf3d0fd1953490222f47f2272fb917f1b65778ad4389e5afbafa50b",
      "after": "5bf30bfed8ac90cdff2d57caf1079390db825d849e2701228f0afe55567a5ff8"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/guild-buildings.component.scss",
      "before": "f3d3618fccc090ae29f48a05f9fda3eaa56f79ca1cf9b9b7eddd5fb7a1bd180d",
      "after": "2bd1137188e16ce9ac8ca5a74cb209b600eb4693223a97b52dda36a0edd4c4f3"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-missions/guild-missions.component.scss",
      "before": "07f7320263885b0878ec332e217924c359e9558e2d322b6b6d15f483b1964bf2",
      "after": "3ceda8ce2a934e2bc20dc1649f0a7784d8bd37ddd6b2c9d065ddb308218275b6"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-shop/guild-shop.component.scss",
      "before": "9226e44a347cfde4d328fd88045fccde5985d57bef7992c07e606e1f403136f5",
      "after": "cc6a85238270154ff95c0ce16c1a12bf1490812d9a0d74ecae482a7de682daa0"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-vault/guild-vault.component.html",
      "before": "0cfb7eb5aafc0c44111d30304a5ee4ef2b34c157c1842a7bec46546e3fac5c5f",
      "after": "31fc29d47b1543eb42fdc21836cc3057d99239344d154c20bbf4d50e0e54d8c4"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/guild/public-guild/public-guild.component.html",
      "before": "4c9f7d3d112f6bd3b5a00927e3b991b251e3a4975b92811d0729057da201cef2",
      "after": "00b1e64651c5a8455962ec7f549a1d433b218c745ff82ed00320294c9b570c83"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-buy/market-place-buy.component.html",
      "before": "c686e7415f66df38f4f78789d505e5e6b04dee8e21c352c12430809f98e5e099",
      "after": "ba103595fbfb0acff7628ad8206865e41038daffbc656793d8f443f3d556414f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-sell/market-place-sell.component.html",
      "before": "57808e3845846eb4595391e6ccb9f163afee238a2fda0c0f56bd97c3b968ca03",
      "after": "90a2fd95b967a7974621c68a05b9b36419dbeed5843cfa161b5e0acbaba9ddea"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place.component.html",
      "before": "1000c0507d2061b958e8cd4b0deecd547fe0c3c4bd59af51d56d1b3602c2089a",
      "after": "57d7b2bc51754645d08eb85c0f007a403b71a9855a795f3f917e15fa2d6cf09e"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/city/tavern/tavern.component.html",
      "before": "e38e90ef0ac9a9e7e66a9254cbf771186e6de32e447aa8bf131c62de204dcd8b",
      "after": "6217f4a4333dec8c408c007532d1351f519b35882161135b29993c1c2131dd0b"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/prophecies/prophecies-page.component.html",
      "before": "dca3758c8ac1c3f3b22a961d2cbf4a312505dc43187206415cdb8a255afc494f",
      "after": "3fe251b679ca7e43c60a9f798d0203115a3a0240e62d444b2c428a3002fc0434"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.html",
      "before": "90ba177d7b22efcfa6e36e6ffe006899129e68d44a8311ed3b4d6323faa46af9",
      "after": "d21a81d20a9661aca7836d7d68c9fb765d6271a50e9bfb76b2d26b1e6109fb80"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html",
      "before": "b9f19565a4b20410df53ce306d5630dfd09844715f1ae8376e9425bc0a110f3a",
      "after": "0c5f8dbc63a9b5f6c334e249e989536a02a3b35d566bc3ee04fb850dfcb3b9ba"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/raid/playback/raid-playback.component.scss",
      "before": "8e231227f4ebb533b4e4d40d657d170d78e8200c47b5714a7e008d650c9ab7d0",
      "after": "d91b58b67da13d5dc5e59d14f6513a3169c8cbe81c3ed05182772e94215d3b54"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/region-boss/region-boss.component.scss",
      "before": "f617bbacb2acf3e4a16defd040d464b9a47676863f9006b715fffb5d48560ad1",
      "after": "cf94c448cca8bde17cd74bfe0da30d4a4919f9585b2bd88e256cd67da536b557"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.scss",
      "before": "a4cc82bacfb7ac5136e2f59b195dde1ece73a9103dafa1258e60471aba9bcd44",
      "after": "99c4e2cbc3414486a1886601999df74ce2590c7ddaf5310f783b109b3726dd1f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/region/raids/raids.component.scss",
      "before": "defc1dfb134a29c29a5bb21992a5700ddec8f0a44cb3036ce383546416ba4f4d",
      "after": "cc5e021c3bcc27eda80e573ece8e9b31bafd0108f0a372741c53281d6e4fe946"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/region/region.component.scss",
      "before": "4228e3b830bc9472530f729c51102328c650a579d5819c69f54ea39389f80eb7",
      "after": "c1814459d89024bfcb54a87236d31cb32ebfb030336e1161badffbcc28e05c38"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.html",
      "before": "00ddee76dea008d409da173fd3c1664b24a1377165f94963b9092a51ea3a5dda",
      "after": "f76e135f788870ec4e2ceaabbb9bfbb5d94a088bdde386d2fabf3f313283dbb9"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.scss",
      "before": "6a0a99dae98d2afa35e3b60e2278f7613e3795661fd1e34a64cc2807c102e1a8",
      "after": "2dd93fb32716b2dc79dcae582d8ebaa25a62f56ffffbc903bdc39a48cbeb83de"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/game/world/tower/tower-page.scss",
      "before": "3e689e64427e4bac643da3b8bab989bfbb5321e37558553243a2e5463aa6398f",
      "after": "f347902b89ed204a9643619c4cf7f1d0e499afe01cd109572e05e3c87e8b9e77"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/public/landing/login/login.component.html",
      "before": "150307a7d921f97c31b459af48976a9769c10e3c3455c5cbbe4912a5011167e7",
      "after": "7a2d2386e810a8c580b8dd547ce6fb091211123b6007c0a8185a3ef6861b7c33"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/features/public/landing/signup/signup.component.html",
      "before": "e97694b9ce79dfb707b3779c97ddcbdd5766a721fbb8fd053400dc98de5407ce",
      "after": "09533df247a9b31aa35c3852ef1634fd115e38f75af70bca869b3a206fd0215a"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.html",
      "before": "9c8b5230c2ff1cd9247cbe6c44197ab5f4cd39a2246efe1d9dea04f25a170cbc",
      "after": "b995c982af4f2f5648a48b18e19a3c9d6d22a1e17fa0a66f8835e28069ca62db"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.scss",
      "before": "a24c9fd83d27b1109d8a98539b75bd6da0a56678168ad185a8d6fae7c628d1d4",
      "after": "303d25e6f825953a3b8c6f9f9c599512cafc7ab64182353a852868cb5b4cba15"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.spec.ts",
      "before": "88b90876072ed415644ae6d8519518b90f86660afcb01174cb66fd99e0740739",
      "after": "de3f950f6e6d3c94f74bda41d6ebcb594da17bfc70793d419fd0cc81906cd618"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.ts",
      "before": "0423479eea37664e56e19be33dd0d335189124968f4e8f18c1df9adc9cb34a0e",
      "after": "f1d290fb90f481c50b4a8982331d87b428aaf4bbd1291d0114895cd1b350258f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.html",
      "before": "39d99d8586a24aa09189eb470c1542a89f6cbda086d33ea98021ec4693f87231",
      "after": "189efbbafba557e6221042c352e35cd73d16c733a2e5b85d5f79d34084181001"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.ts",
      "before": "f1a9140c63812c195c453015145181d0250a6bc4662f66efd0ff8fae03eb695c",
      "after": "7014da6857243f60c77c529d04f17639517c822a66533e13658ab22550480613"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/game-header/game-header.component.html",
      "before": "e70ded770ad7f84a9e6dd6cb68a984bf948f16223efd6ae00ce2af5778741103",
      "after": "d766f4ef65be137ffbf733f8c1546b5465e429ef4a16b072a1b9f3f4a0a8c230"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/loot-tracker/loot-tracker.component.html",
      "before": "43b1ba9addafe1deba36860d47673375367d6071df08698454f5990a9bb1027f",
      "after": "30b9ca045bbd3c84b4677b57eb70e4f661e360ad83989397c32484a6d3082f43"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/loot-tracker/loot-tracker.component.spec.ts",
      "before": "9376828fa1b51f813cac3b8644f3a4cd014f3e8e7ad3f39626317959ce7e38d3",
      "after": "3a2d5c549912b496dce654e8073f89c2f7253f81ddb54653e4770c1e62b573ba"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/loot-tracker/loot-tracker.component.ts",
      "before": "038403f8224661a1f60a476b18e03750322987f4b18a07ba250f9b3b0a19743f",
      "after": "6452bdf44e2005ee77f4a21db13a5b251ea79f851ded25f282a6af30cff6655f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/layout/dashboard/sidebar/sidebar.component.html",
      "before": "45a069a6f727ab4d24ac07327559298ec22b3a27689f4d7699c79b1b0d9f27c2",
      "after": "b3c004e9a30546e9e6159b37882acf56c433cf2a15d464174e7bc74d07844b0d"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/combat/combat-area-card/combat-area-card.component.html",
      "before": "84569ccf577ee9e6933044e0bd5375720243181f29e37eac7166cc02caa6ac5a",
      "after": "4e0958396246a5059e34a4d33f5ec2e9e1437af338031728a91dc9c2ad4afd85"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/combat/combat.component.html",
      "before": "774df779941c37d40d835e5dca6eed4347e581640208f406c5b0eb8760f4bd63",
      "after": "421677f762852dcccbb0dcf7c98cd8699ef1e556d95f2db08a6b3127cf2edf58"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/current-action/current-action.component.html",
      "before": "2bd8ddf6213d02c0fbdd664069186dbdd9e115e4b0a689d16eb61985458cde64",
      "after": "50a2a51e701f4d5443a0e13c9bcd1c1a5108a31fb712ccc3f21a3fc550cb36f4"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component.html",
      "before": "0bb5aa0db8a9a2711926dad34a5a0ec502e0cc9e91aeec245e0dcddd330e2929",
      "after": "2854747586bfc33ef25b6d3f91df398eb6a95ac7858ab95030da3ce4823289e0"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/tabs.component.html",
      "before": "67a1dd298688b665cc7c7e99b04aee9afe47d672d8410cd8131c606b4d5d6e5e",
      "after": "f8840af18d39ff13810c534d896ebb1e2739a09b709f9366bd1d302e5f3dad97"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/default-header/default-header.component.html",
      "before": "40c88aec4d5c1e1ea06752f1ded12b0bfd02f16a859e5c3ea22980e2c3d0a850",
      "after": "b4d424aee7be7132ea32f53c761703dabd4587249aa55ea9f215d22c0932a575"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/default-header/default-header.component.ts",
      "before": "3d78d2c57f79e332ac3323d2da9fe5c28020104d9246b35bd30da072ceddecd2",
      "after": "b9740a391a8e42458c459c17d8532d68129df630457b4c3b784b2030460babf3"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/dungeons/dungeon-card/dungeon-card.component.html",
      "before": "f09ff7dd11d52ee40f9480b0b7e67b7e70993264bc154f44d4096283aecb1d3a",
      "after": "d17f1a9bbe028f827076427a26d19969b3c518b5f2e64c6327cb1be63f66d91d"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/dungeons/dungeon-card/dungeon-card.component.scss",
      "before": "9bd5e88bcb6696c57fdbb531ab1c7164c87ba87b1edb16d510523497fe6c559e",
      "after": "6eefc7dbbe02cda80b2ec84db61a73f03e94a089b0a3a00c85601f634f378f71"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/equipment-overview/equipment-overview.component.html",
      "before": "a2a2370d0c0234075a91284b1a8b6e46704d9f63fa104a48c3fcf87dc8593095",
      "after": "9b28a45ee0f4eac955d97d2c7c26248bffb8826c708a973fe62070630a11704e"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/generic-leaderboard/generic-leaderboard.component.html",
      "before": "1852a359bff62595d18a05a75e7dc5be016b1fa34e81328ed7392985b9a123e0",
      "after": "284b4eac06b766f14135a24d0961d4116c4259d49145c33d9e6c2b1c5202f03f"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/generic-leaderboard/leaderboard-podium/leaderboard-podium.component.html",
      "before": "5e6367d31fdb9b02c7604df7037271f3c1dde5c7ea9553c44be080639e68b741",
      "after": "7e6103d3dd294d86708a3549ae90897027c59834bce3f5a47af8abbd378ac3f3"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/components/modal-container/modal-container.component.html",
      "before": "ee743135581b4d2f803c396ddc0bc5e3cf68c0ae0e7d8ca12fdcd1efc74097d5",
      "after": "27dbb98a70f98a960462f299bc596e475afc1d4afc5f75bf4ee530da7032177c"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/directives/sticky-scroll/sticky-scroll.directive.spec.ts",
      "before": "6e489be97841a3eca1fc75a4b9bd73772043414d1f7196cda81dcf88874fc427",
      "after": "3bad3ffdc92b49a25e9a0d238e09d0ec6bac672cf8b914be12d3d73b296c265c"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/directives/sticky-scroll/sticky-scroll.directive.ts",
      "before": "0c6c19617c232088419552b0ac9b8895c3a4639a468a57dbbfd6f00611d200f1",
      "after": "3b6629cd6613b078669f7cef1db6191a4bcb9c498b4fd0650c1debac10e2bcb2"
    },
    {
      "path": "LL/src/Presentation/ll/src/app/shared/help/help-drawer.component.ts",
      "before": "cb169b82ebe291a60347a19a34ab49ce39811f806017dc9c51dbfd34e51c33d6",
      "after": "dc9a2c3cbb5e4b39a216a10ecf67378325b28d47247e395cf0edb9ed055dc14b"
    },
    {
      "path": "LL/src/Presentation/ll/src/styles.css",
      "before": "9ca3f39e38cd78603435bec2b85ea21554893834e3cd31126c71b936b78d629c",
      "after": "bc48166dde5e0004d6e4cdb054e167cddcbce04b8da46bfab7f95089f9914978"
    },
    {
      "path": "LL/src/Presentation/ll/tailwind.config.js",
      "before": "c0fc6ae3e2a07bca84b01d51009b12f9167537e253f51488f7ce2cea80a36fa5",
      "after": "eed7ad9c6ccbdd2e0fc8ef3c9bd78b1728ad88644d973cf05a04d40368fc8368"
    },
    {
      "path": "UI_REWORK_IMPLEMENTATION_PLAN.md",
      "before": "bbfb4e9a7f134ade3078da4e66b11026fe243c34b9708d458c5c079574cb7b35",
      "after": "ce4166f927659c1f6b51113654c2725938d05258b2931857efc4e668669771ad"
    }
  ],
  "concurrentNewFilesPreserved": [
    {
      "path": "docs/ui-rework-verification/README.md",
      "hash": "c2d41269aaa008fd0ce7becd6096b8eeba45ad459c3ba7d97df3742cce490ffb"
    },
    {
      "path": "docs/ui-rework-verification/essences-desktop.png",
      "hash": "1368de7db0bf45c8d429cabdde0edcbf472193dabc188596b28b667d6685119a"
    },
    {
      "path": "docs/ui-rework-verification/essences-mobile.png",
      "hash": "8029e8b280b688e6b3030da7ebbc908dd9d4e7d67c2a3af09a20ad2b5ab0f837"
    },
    {
      "path": "docs/ui-rework-verification/inventory-desktop.png",
      "hash": "8ab6dc4f68facc66dcc3203089263ef25db154e9103c752996ba1fca7715c769"
    },
    {
      "path": "docs/ui-rework-verification/inventory-mobile.png",
      "hash": "a536970b40ab54f1f8a03830ac9a4efeb4bd7ee01f5262b44fa9b925be32fbbf"
    },
    {
      "path": "docs/ui-rework-verification/live-smoke.cjs",
      "hash": "9547101bae818d5f51e91b37e1874a3266f1c3ca1b972bbd0685ff9e4d45bd07"
    },
    {
      "path": "docs/ui-rework-verification/login-mobile.png",
      "hash": "020b87bd43545ee2298548ef97aec567e5e3fa16d9e1fde2c2b0e272ecb16aac"
    },
    {
      "path": "docs/ui-rework-verification/overview-desktop.png",
      "hash": "30c4131f9eb4b20562242a36a01daa98c5ef0b0772d7175b22ce67c608b94095"
    },
    {
      "path": "docs/ui-rework-verification/overview-loot.png",
      "hash": "d87e0e1bcba5cf28d5434edd92648a201edae3104f4c12d39a8ad8e296c40bda"
    },
    {
      "path": "docs/ui-rework-verification/overview-mobile.png",
      "hash": "fb2e423b64032361c6a8e1e702fa866927f501a793067a4856b316873540dbc9"
    },
    {
      "path": "docs/ui-rework-verification/visual-smoke.cjs",
      "hash": "bd09dc08f17b9b65ab456947405838785a512a428b82d6418866d4d7decf3245"
    }
  ],
  "historicalReviewAndSealMarkers": "Unchanged",
  "sourceCodeChangesByThisTask": [],
  "migrationConfigurationDeploymentChanges": []
}
```

</details>
