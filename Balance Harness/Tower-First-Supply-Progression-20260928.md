# First Rare supply progression — 28 September 2026

## Finding

The tested full Common sets can earn the first Rare supply set through region-1 grade-I Goblin Mines at a matched, pre-dungeon four-Essence budget. All **16 level-1-Essence paths** completed their seven-item sets in seven attempts. The sparse quest-armor-plus-ordinary-weapon condition completed **0/16 paths** within twelve attempts at the same Essence budget. This isolates a gear-coverage difference in these declared inputs; it does not establish the minimum required gear, population success rates or normal acquisition time.

The remaining progression question is how a player reaches a useful pre-supply inventory while earning entry resources. The complete Common sets are conditional inventories, not observed level-30 player inventories. Exact default-profile ordinary drops can be rare, and gear and sigils can be earned during the same activity. Their separate expectations must not be added as independent farming bills. No boss, supply cadence, gear curve or search algorithm changed.

This follows the [full-dungeon qualification](Tower-Dungeon-Acquisition-Qualification-20260928.md) and [personal inventory progression model](Tower-Acquisition-Progression-20260928.md). The earlier floor-10→11 carryover evidence and historical archives remain unchanged. There are **zero measured player samples**.

## Reachable inputs and explicit prerequisites

The [fixture](../LL/tools/BalanceHarness/Fixtures/tower-bootstrap.json) declares level 30, server highest cleared Tower floor zero, two region-1 grade-I sources, four fixed recipes and five gear conditions. Production quest dependency closure excludes quests requiring a dungeon completion and their dependent rewards. Prerequisite quest victories and level 30 are supplied assumptions; this is not a simulation of leveling from character creation.

Each recipe uses one selectable First Hunt Essence plus one selectable token each from Lumo Ruins, Crystal Creek and Twilight Clearing, in that order:

| Recipe | First Hunt | Lumo | Crystal | Twilight |
| --- | --- | --- | --- | --- |
| Guardian | Goblin Warrior | Lumo Wisp | Transparent Slime | Wood Nymph |
| Restorer | Hollow Stag | Lumo Wisp | Blue Slime | Wood Nymph |
| Striker | Goblin Warrior | Goblin Archer | Frost Imp | Pixie |
| Controller | Skeleton | Goblin | Frost Imp | Enchanted Fairy |

These are fixed diagnostic recipes, not optimized search results. Actor identity and ordered Essences remain fixed across gear conditions. Weapon/armor archetypes come from the existing four-slot profiles: maul/heavy armor, staff/light armor, gauntlets/light armor and greatsword/medium armor respectively. Each complete set has seven unique items covering eight slots, including band, amulet and vial.

Two production quest gates matter:

- `quest.shenic.roots_remember` requires a Goblin Mines completion before awarding its Old Forest token. It cannot fund a first-clear recipe. Later dependent quests are also excluded.
- `quest.shenic.restless_dead` requires equipping an ordinary area drop. That prerequisite precedes the Twilight token and guaranteed Goblin sigil. The modeled quest progression therefore already assumes an ordinary drop was earned.

The pre-dungeon closure supplies one Goblin Mines sigil, one Catacombs sigil, one random Armor Chest and 500 Cinders. These are credited only as retained, unspent starting rewards. The Armor Chest is **not selectable**: the exact desired default chest-piece outcome has probability **1/42**. The conditional loadouts select that outcome explicitly. A retained quest mace remains owned but unequipped when replaced.

The historical four-slot references are not substituted unchanged. Their ordinary local sources include higher-level areas (for example brown slime and giant bat at level 45, green slime at 40, and cinder beetle at 35), while hobgoblin and goblin shaman have no ordinary idle-area source in the audited catalog. This is a local source limitation, not a claim about every possible trade or special reward. The legal token recipes above remove those dependencies for this diagnostic.

| Gear condition | Starting equipped inventory | Supplied prerequisite |
| --- | --- | --- |
| Quest and drop | Common quest chest piece plus one ordinary two-handed weapon | Exact quest-box outcome and one qualifying ordinary weapon |
| Common | Seven Common rank-0 items | Quest chest piece plus six qualifying ordinary items |
| Uncommon | Seven Uncommon rank-0 items | Seven qualifying ordinary items; quest armor retained |
| Common rank 1 | Seven Common items reinforced once | Common inventory plus production upgrade budget |
| Rare control | Already owns seven target Rare rank-2 items | Synthetic already-owned control; cannot prove acquisition |

Ordinary items require the stated archetype, default specialization, rarity, Standard-or-better quality and no blueprint variant. Their descriptor uses the production minimum random roll of 0.95 and Standard quality as a conservative lower bound. Quest and supply items use their production 1.0 roll. No donation or dismantling credit is used.

The reinforced condition costs **40 Parts and 89,200 Cinders**, counting all eight occupied slots. After the 500 quest Cinders, **88,700 additional earned Cinders** are required. This budget is declared, not measured affordable. The unreinforced Common condition already succeeded in Mines, so these costs are not a necessary first-set bill established by the study.

Each gear/recipe combination is tested with Essence levels 1 and 10 at ascension zero: **40 cells** in total. Production training costs are 1,296,004 XP per Essence, or 5,184,016 total credited Essence XP across four. Combat XP is credited to each equipped Essence rather than divided between them; the sum is not required character XP. No cores, ascension or evolution are supplied. All **160 matched level-1/level-10 path pairs** had identical combat summaries. Production ability scaling at ascension zero and the prepared loadouts explain this result; it does not imply that ascension or Essence progression generally has no benefit.

## Earned-inventory procedure

The [cohort builder](../LL/tools/BalanceHarness/TowerBootstrapCohorts.cs) reads production equipment, quest, token, upgrade and XP rules. The [study](../LL/tools/BalanceHarness/TowerBootstrapStudy.cs) reuses production dungeon generation, actions, preparation and combat through the existing [runner](../LL/tools/BalanceHarness/DungeonAcquisitionRunner.cs), including routes, Vigor, rest, failure and completion callbacks.

Every successful acquisition attempt awards exactly one item through the production supply catalog. Purchase order is fixed before execution: weapon, chest, head, legs, necklace, ring, relic. The next run uses the resulting personal inventory. All displaced items remain owned; stronger owned items are retained. A failed attempt consumes its entry sigil and awards no item. Paths stop at seven Rare-or-stronger equipped items or twelve attempts. Unfinished paths are censored at that cap, not extrapolated to eventual success.

Each cell/source has four predeclared paths. Sources are alternative histories, not combined inventories. Rare controls perform one probe per path, earn no study equipment and are reported separately. Mastery stays zero; character/Essence levels remain fixed. Ordinary dungeon loot and reward/mastery persistence are excluded; production completion callbacks are recorded and supply issuance has separate service tests. These omissions limit inference about real progression.

## Observed outcomes

The table uses **only Essence level 1**. The level-10 condition repeats the same outcomes and is not extra independent evidence. Each row/source combines four recipes with four paths each; these small diagnostic counts are not population rates.

| Starting gear | Mines completed sets / paths | Mines attempts / successful runs | Catacombs completed sets / paths | Catacombs attempts / successful runs |
| --- | ---: | ---: | ---: | ---: |
| Quest and drop | 0/16 | 192 / 1 | 0/16 | 192 / 0 |
| Full Common | 16/16 | 112 / 112 | 13/16 | 149 / 109 |
| Full Uncommon | 16/16 | 112 / 112 | 14/16 | 139 / 110 |
| Full Common rank 1 | 16/16 | 112 / 112 | 13/16 | 147 / 109 |

Every completed Mines path took seven attempts. Completed Catacombs paths took seven to eleven. One successful sparse Mines attempt earned one item but did not lead to a completed set within the cap. Already-owned Rare controls won **16/16 Mines probes and 12/16 Catacombs probes** per Essence level; in Catacombs, guardian/restorer each won 4/4, striker/controller each 2/4. Those probes are not acquisition successes.

Across the entire matrix, there were **2,374 diagnostic attempts**, including 64 already-owned control probes, **176 completed acquisition paths**, **80 censored paths**, and **1,330 individually earned supply items**. The study executed **18,472 room combats**, including 80 full-run qualification replays. All replays matched complete normalized output. These totals intentionally count both training conditions for execution accounting, not for additional statistical evidence.

| Starting gear | Completed-path combat-only minutes, Mines | Completed-path combat-only minutes, Catacombs |
| --- | ---: | ---: |
| Full Common | 20.72–56.61 | 44.81–102.53 |
| Full Uncommon | 20.34–55.62 | 39.16–88.06 |
| Full Common rank 1 | 20.63–55.86 | 44.96–110.55 |

These ranges sum engine combat seconds over complete acquisition paths, including their failed attempts. They omit gear/training/resource acquisition, player input, navigation, animations, breaks and waiting. They exclude censored paths and are **not full acquisition times or calendar forecasts**. Computer execution took about 21 seconds under the process owner; that runtime is unrelated to gameplay duration.

## Resource interpretation

Production ordinary equipment chance is 1/864 per eligible victory. Exact-definition demand is much narrower: the qualifying Common maul condition alone has an expectation of about **410,005 eligible victories** under the frozen category, handedness, archetype, profile, rarity, quality and variant rules. This is an individual geometric expectation, not a required or typical path to useful gear. Do not sum individual item expectations to estimate a complete set, or conclude that players must insist on these exact default-profile items. A useful next model accepts suitable actual drops and carries every earned item forward.

Every attempt consumes one matching sigil. After the retained quest sigil, each additional matching sigil needs **10 fragments through assembly**, or **24 expected eligible idle hours through random targeted drops alone**, assuming perfect eligible idle victories and the production ten-second encounter cadence. Random sigils drop with probability 1/4,320 and split uniformly between the two families. Seven no-failure attempts therefore require six more matching sigils: **60 fragments OR 144 conditional random-only eligible idle hours**. Assembly cost is an alternative source, not a second simultaneous payment. Each actual path records its additional demand, including failures.

The earlier acquisition model's 84-hour seven-sigil scenario counted either regional family without starting stock. The 144-hour value here targets one family after one quest sigil; the source assumptions differ. Neither is normal-player acquisition time. Sigils may already have dropped while the player earned equipment, level and quest prerequisites. This study does not double-credit that activity and does not add the separate gear/sigil expectations into a total. Other eligible sources and affordable spending remain to be modeled with their actual costs.

## Bounded workflow and retained evidence

The [owner](analysis/run-tower-bootstrap.py) snapshots all consumed content, source and runtime files before execution, retaining their original-to-archived mapping. It uses the existing suspended Windows Job process owner and required backend wrapper. The frozen declaration caps acquisition paths at twelve attempts, the process at 900 seconds, native work at 840 seconds, total room combats at 205,824, per-run actions at 64, study output at 256 MiB and process log at 1 MiB. Maximum diagnostic attempts were 3,136, plus 80 qualification replays. Actual study output was 154,054,713 bytes. No search, combat retry, sample extension or tuning occurred.

The study reserved **6,240 fresh seed values** against the previous 836,359-value exclusion union. It consumed 962 distinct layout/combat values; **5,278 unconsumed reservations remain excluded**. The full union is now **842,599**. Shared panels across gear/training cells support matched comparisons and do not create independent replications. Preserve the [new ledger](../TestResults/tower-bootstrap-owner-20260928/seed-ledger.json), SHA-256 `6449f56ac6750e565f7d64cd81c8b0ebf2dd3ac72dd44ca3595e29a014f398a2`, and its historical links for future allocation.

Evidence, stored locally under ignored `TestResults`:

- [Frozen declaration](../TestResults/tower-bootstrap-owner-20260928/declaration.json), [request](../TestResults/tower-bootstrap-owner-20260928/request.json), [source map](../TestResults/tower-bootstrap-owner-20260928/source-map.json), [process receipt](../TestResults/tower-bootstrap-owner-20260928/process.json).
- [Results](../TestResults/tower-bootstrap-study-20260928/result.json), SHA-256 `e66685cf2d88ba068e0e0c1c46e2fb1baeffb42b2ffd32420f68d67f18cbf75c`; [cells](../TestResults/tower-bootstrap-study-20260928/cells.json), with per-item prerequisites and training/upgrade budgets. Per-attempt full reports and personal path histories are alongside them.
- [Output manifest](../TestResults/tower-bootstrap-study-20260928/files.json), trusted pin **`776dad71cce6622f9a216fbdf8bf2adffa9b500e7a93e1c42cfc0b790bb297f2`**.
- [Original frozen-verifier audit](../TestResults/tower-bootstrap-owner-20260928/independent-audit.json), [supplemental read-only audit](../TestResults/tower-bootstrap-owner-20260928/supplemental-audit.json), and [final independent audit](../TestResults/tower-bootstrap-owner-20260928/final-independent-audit.json).
- [Study TRX](../TestResults/tower-bootstrap-owner-20260928/study-tests.trx), [study log](../TestResults/tower-bootstrap-owner-20260928/study.log), [regression TRX](../TestResults/tower-bootstrap-build-20260928/regression-tests.trx), [regression log](../TestResults/tower-bootstrap-regression-20260928.log).

The [independent verifier](analysis/verify-tower-bootstrap.py) checks 1,840 input hashes, all output hashes, production preparation for 17,848 diagnostic rooms, paths, ownership, rewards, retained gear, failures, time/resource arithmetic and exclusions without new fights. After the original passing audit, it was strengthened with independent checks of 160 ordinary-drop probability rows and 40 Armor Chest rows; the original verifier snapshot and receipt remain intact. Later concurrent source edits cannot silently replace the archived producing inputs. This is not blanket certification of the current LiveOps working tree.

## Verification and changed files

Added the cohort builder, study, fixture, [backend tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessBootstrapTests.cs), bounded owner, independent verifier and this report. Updated the handoff and the three prior acquisition/supply reports. Reused the existing dungeon runner and supply catalog unchanged. No production code was edited in this continuation.

The final regression passed **364 tests**, with **two intentional opt-in study skips**. The explicitly owned study separately passed its one test. Initial sandbox compilation could not access the local NuGet configuration; the required wrapper succeeded with escalation. An initial seed-free test exposed a missing legacy quest prefix in dependency closure; it was fixed and verified before any combat. No required command remains blocked. No frontend code changed or required frontend tests.

Recorded commands (use fresh output directories and new excluded seeds for any future study; do not rerun into these archives):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-bootstrap-build-20260928 -Filter 'FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon' *> TestResults/tower-bootstrap-regression-20260928.log
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/run-tower-bootstrap.py' --owner TestResults/tower-bootstrap-owner-20260928 --output TestResults/tower-bootstrap-study-20260928 --artifacts TestResults/tower-bootstrap-build-20260928
& $python 'Balance Harness/analysis/verify-tower-bootstrap.py' --owner TestResults/tower-bootstrap-owner-20260928 --manifest-pin 776dad71cce6622f9a216fbdf8bf2adffa9b500e7a93e1c42cfc0b790bb297f2 --receipt TestResults/tower-bootstrap-owner-20260928/final-independent-audit.json
```

The user-defined repeating curve, floor-10→11 stronger-gear retention and supported search `affinity-creation-with-benchmark-validation-v1` are unchanged. Guardian content remains SHA-256 `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. All unrelated concurrent edits and historical archives were preserved, with no staging or commits. No dependencies, application configuration, migrations, deployment, API startup, seeding or database operations were introduced. The original supply feature's future item-seed rollout requirements remain separate.

## Next work

Model stochastic pre-dungeon inventory coverage using useful actual drops, random quest-box outcomes, earned Essence choices and retained sigils from the **same** activity history. Keep individual ownership, supplied assumptions and failure costs explicit. Test intermediate inventories only where the resulting equipment changes require bounded combat evidence; the current two-item/full-set comparison does not identify a minimum threshold.

Then combine those histories with measured activity/completion data when available, or explicitly conditional activity scenarios. Include affordable fragment, quest, cache, guild and market sources only with eligibility and costs. A desired farming-hours target remains a product decision, not an inference from seven successful clears. No chest-cadence or boss retuning is justified merely to hide an unresolved acquisition cost.

## Subsequent joint activity model

The [29 September continuation](Tower-Joint-Activity-Progression-20260929.md) now obtains ordinary gear and both sigil families through the production idle reward processor in one personal history. It samples random quest armor, keeps all ordinary profiles/qualities/styles, counts the pre-dungeon quest gate and equips legal items using a fixed stat-budget policy. Under a Mines-only spending policy, seven supply items were earned by 13/16 perfect-idle and 9/16 four-in-five histories at the declared checkpoints. All ten unfinished histories had exhausted Goblin sigils but retained Catacombs sigils, making a both-family policy the next comparison.

That study adds 237 funded attempts, 2,465 room combats, 40 matching replays, an independent audit and 367 passing regression tests. Its cadence hours are conditional activity amounts, not measured player pace. This report's exact-profile controls, results and archived pins remain unchanged; no boss, supply or gear-curve setting was adjusted.
