# Starter path acceptance — 8 September 2026

This review follows the user's request to proceed with the local playtest, target/baseline decisions and hosted CI. It concerns the primary LL game's selected Goblin Warrior starter. The broader harness remains in Phase 2; an available regression tool is not a claim that every gameplay target passes.

## Reviewed win targets

The user explicitly chose **50–90% wins for the level-10 Amulet + Goblin checkpoint** and said duration does not matter. [The new policy](../LL/tools/BalanceHarness/Fixtures/idle-crystal-creek-starter-goals.json) enforces only the two `handoff-crystal-creek.quest-rewards` cells. The other fourteen cells are declared diagnostics. No target is inferred for returning to Blood Grove, the underprepared builds, optional Fury, other Essences or duration.

The original handoff fixture and both completed 500-trial seed sets remain frozen. Their existing results were evaluated separately, with no additional battles or coefficient changes:

| Encounter | Reference wins | Confirmation wins | Confirmation 95% Wilson interval | Enforced assessment |
| --- | --- | --- | --- | --- |
| Two Blue Slimes | 496/500 (99.2%) | 497/500 (99.4%) | 98.25–99.80% | Fail: above 90% |
| Blue Slime + Frost Imp | 500/500 (100%) | 500/500 (100%) | 99.24–100% | Fail: above 90% |

Both evaluations return exit **1**, with two failed checks and no invalid evidence or coverage issues. Their saved bundle fingerprints remain `a3c9448a0e916d0756214c6b518150832bde5fcdff4cf49fad88ca056b7db2cf` and `0b003de2bd15df13b169c4a837e39a49a7275be8b646902ba62b74b4460c0939`. The policy's canonical hash is `e3112d21b47fcbbdcd860b140b8e250c83ce1608e45604e6e979a10280ab70f0`; fixture hash remains `63a44203a7c787d7f0627205a56dd7ff91775bc17a9873bfa38e671d536e41ec`.

Evaluation artifacts are under ignored `TestResults/balance/crystal-creek-policy-review/{reference,confirmation}`. This is a valid route through the encounters, but it is too reliable under the newly reviewed target. It is not accepted as a passing gameplay baseline. A subsequent tuning investigation must declare its candidate range, budget and fresh confirmation seeds before execution.

Blood Grove's original result stays closed: 89.30% Ravens, Inconclusive because the upper interval is 90.36%; 76.73% Raven + Zombie, Pass. The later [acceptance confirmation](Blood-Grove-Acceptance-Confirmation.md) ran a predeclared 10,000 fresh trials per encounter at unchanged content: **89.13% [88.50–89.73%]** and **76.21% [75.37–77.03%]**, both Pass. A new local `blood-grove-starter-v1.json` baseline was explicitly accepted with no policy exception or pooling. That confirmation passed all 2,112 backend tests and covered Blood Grove only.

**Combined verification — 8 September:** the subsequent [52,000-battle regression](Starter-Regression-Review.md) checks both accepted references against one retained build after the shared Combat Styles changes. All eighteen cells match exactly, all four primary win intervals remain inside 50–90%, four detailed replays match, and all **2,119 backend tests passed on that build**. The accepted manifests, earlier measurements and independent sample counts remain unchanged. The live API and disposable character were not changed or retested by this regression.

**Local recovery — 9 September:** the [corrected evidence package](Starter-Baseline-Package.md) restored all 428,158 files outside the checkout. Four saved suites passed their existing checks and eight fixed replays matched on the original .NET 10.0.11 runtime. Packaging is complete; off-device storage is deferred by user choice and hosted verification remains pending publication. Recovery adds no independent samples and does not recertify the current runtime, live API or local playtest.

**Subsequent tuning:** the [bounded creature-pressure experiment](Crystal-Creek-Tuning-Review.md) completed 120,000 battles using isolated content copies and separate creature abilities, preserving player Essences. Its selected candidate confirms at 91.6% / 50.3%, with both intervals crossing policy boundaries. It remains Inconclusive, has not been applied to the game and does not replace the original above-band reference measurements recorded here. All 32 external controls, eight Blood Grove return cells and four replays passed. The separately declared finer grid below was the next investigation.

**Fine confirmation:** the subsequent [42-candidate experiment](Crystal-Creek-Fine-Tuning-Review.md) completed 275,200 battles and selected Barrier 0.44 / Ice Needle 1.7. Fresh confirmation gives **74.40% [72.44–76.26%]** and **55.45% [53.26–57.62%]**, both Pass. Corrected creature-only ownership metadata retains exactly the same gameplay in 36,800 repeated verification fights. Those definitions/mappings are now local API content; all **2,107 backend tests pass**, and the corrected confirmation was explicitly accepted as `TestResults/balance/baselines/crystal-creek-starter-v1.json`, scoped to the two primary Creek cells. This does not change the original measurements, approve a Blood Grove exception, or certify the full natural spawn distribution. The linked review records final verification and baseline retention.

## Archive compatibility and preservation

The audit reproduced a failure to read old 14-file runs after Combat Styles added a fifteenth content file. The reader now validates explicit archive contracts: schema 1 accepts exactly the historical 14-file set or the transitional 15-file set; new schema 2 requires exactly 15 files. Unknown versions, missing/extra paths, traversal paths and changed checksums remain errors. Historical reading never fills missing content from the current checkout. Replay still requires the original assemblies, runtime and platform.

The real historical `blood-grove-starter-reference/discovery` archive now evaluates successfully: its original zero-win measurements correctly produce a balance Fail, rather than an invalid archive. Evidence is under `TestResults/balance/archive-compatibility-review/historical-starter`.

Before rebuilding, the exact handoff executable and dependencies were copied to `TestResults/balance/retained-builds/handoff-128513c346d6` with a 61-file hash manifest. This preserves the available matching binary; it does not retroactively reconstruct missing older builds or prove a recoverable clean source revision. These local ignored artifacts still require deliberate retention when sharing or moving checkouts.

## Local playtest

The user authorized a disposable character. `CheerfulSharkMaster_8783` was created through the local Guest Login UI. The existing level-99 character's build was left intact. The browser is now using the guest session. API port 7050 connects to PostgreSQL on localhost:5432; the frontend is localhost:4200.

Checkpoint preparation was limited to this new account. XP was set to 3,225 at level 1 and later 24,275 at level 5; ordinary victories then invoked production level-up/stat/event handling to reach levels 5 and 10. This does not measure elapsed leveling time. Quest completion, tokens, absorption and equipment actions otherwise used the game UI. The Armor Chest naturally awarded the requested Heavy Breastplate. The Jewelry Chest instead rolled a Relic, so its original item state was preserved and the disposable account's item was prepared as the fixture's standard tier-1 rank-0 Amulet, with +541 Max Health in both its canonical data and legacy attribute mirror, before equipping it. This preparation is not evidence that the Amulet is guaranteed. Local preparation scripts and the original jewelry state are retained under ignored `TestResults/balance/local-playtest`.

The UI journey completed First Hunt with Goblin Warrior, absorption/equipping and explicit idle-loadout assignment, Shortsword choice/equipping, Lumo quest rewards, both level transitions, Blood in the Grove turn-in and Crystal Creek access. The retained Lumo Token allowed a Goblin selection; absorbing it and equipping slot 2 produced **2/2 attuned** with idle auto-use enabled. The final level-10 build had **1,077 Max Health**, the three fixed items, both level-1 Essences, no Fury and no Combat Style. It persisted after reload. Combat was stopped before leaving the character available for user review.

| Checkpoint | Observed counter before stopping | Captured encounter examples | Interpretation |
| --- | --- | --- | --- |
| Blood Grove, level 5 | 4 wins / 8 losses; one more result may have resolved while stopping | Two Nightshade Blossoms: loss, 78 s, 0/436 HP; Blossom + Snake: loss, 66 s; Blood Zombie + Snake: win, 84 s, 1/436 HP; two Ravens: win, 57 s, 25/436 HP | The selected build can earn the four-win quest, but natural species differ from the two fixed benchmark pairings. This small counter is not a policy estimate. |
| Blood Grove return, level 10 | 6 wins / 0 losses | Blood Zombie + Raven: win, 52 s, 779/1,077 HP; Blossom + Raven: win, 45 s, 792 HP; Blood Zombie + Snake: win, 52 s, 781 HP | The prepared progression step supports a successful return. |
| Crystal Creek entry, level 10 | 6 wins / 0 losses | Crystal Wisp + Moss Lizard: win, 45 s, 599/1,077 HP; Frost Imp + Moss Lizard: win, 42 s, 559 HP; one Transparent Slime: win, 27 s, 977 HP | Area access and the five-win quest objective work. These natural examples do not replace the frozen Slime/Imp measurements. |

Counters were observed during ongoing idle combat; individual details were not captured for every battle. This is a partial observational log, not the originally proposed complete five-row-per-checkpoint record. No set was restarted to obtain a better result. Optional Fury was not applied or live-tested; its separate offline comparison remains the available evidence. Automated UI observations are distinct from the user's subjective gameplay acceptance. Duration is retained only as descriptive telemetry.

The overview recommended Crystal Creek after the reward claim and changed to “Reach character level 15 to unlock Moonlit Graves” after the five-win objective. The second-slot unlock and quest text were present, but this guided check does not establish whether an unguided player notices and uses the slot. Two concrete UI issues remain for a separate fix:

- The random Armor Chest panel displayed “CHOOSE ARMOR / Select the reward…” without offering choices.
- At the observed 1,000 × 920 viewport, the floating chat bar covered the Blood in the Grove turn-in button. Keyboard activation claimed the reward successfully; this was not a failed quest transaction.

The local frontend and API were already running in a shared dirty checkout with concurrent Combat Styles work. Their live behavior is useful integration evidence, but they were not pinned to the retained harness executable and are not a reproducible statistical run. Random jewelry outcomes and broader natural spawn coverage remain separate gaps.

## Hosted CI and local verification

The first [hosted Balance harness run](https://github.com/Dryppie/legends-legacy/actions/runs/34266122671) **passed** on `main`, commit `412c63e9ce31bbea3306b75063c4f6280f37f2c8`. It was manually dispatched after the user signed in. The Ubuntu job passed 83 tests, both cohort smoke comparisons with zero changed gameplay records, both detailed replays and artifact upload. It verifies the published workflow; the current dirty checkout is not part of that remote revision.

Artifact `balance-harness-34266122671-1` (ID `10071967837`) contains 1,294,764 bytes, SHA-256 `356691fa51b020efc12324b02c23bef6d208b90c589e569d9a94c9e9afa9ddce`. GitHub reports expiry on 15 September 2026 at 19:00:04 UTC. The seven-day retention is operational smoke evidence, not durable accepted-baseline storage.

The local workflow filter now includes `CombatStyleHarness` alongside `BalanceHarness`, so the existing style-fixture checks are not silently excluded. This does not close the audit's separate request for independent normal-gameplay Combat Styles parity.

- Release build with `--no-restore`: passed, four existing warnings, zero errors.
- `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CombatStyleHarness'`: **108 passed, zero failed/skipped**.
- Both `build/smoke-balance.ps1` workflows at three samples per cell: **72 control and 216 First Hunt battles**, all valid; zero changed gameplay records in both comparisons, both detailed replays matched, draft evaluations remained advisory.
- Both new Crystal Creek evaluations and the real historical starter evaluation: expected balance Fail/exit 1, no invalid evidence.
- Scoped `git diff --check`, new-file whitespace checks and all 91 local link targets in the five updated documents: passed. Frozen Blood Grove/Crystal Creek plan and result hashes and regional content still match their recorded evidence.

Changed files in this increment are the harness archive contract and its `OfflineContent`/`RunBundle`/`SuiteBundle` consumers; `BalanceGoals` and `GoalEvaluator`; the new Crystal Creek goal fixture; comparison/goal tests; the hosted test filter; and this review, the handoff report, main plan, tool README and post-alpha roadmap. Existing fixture/result measurements are preserved. No required verification command remains blocked; the full backend and frontend test suites were not rerun for this scoped harness increment.

No new production combat coefficients, migrations, shared-database changes or deployments were made. The only database writes in this increment are disposable local playtest account/gameplay state and its explicitly recorded checkpoint preparation.
