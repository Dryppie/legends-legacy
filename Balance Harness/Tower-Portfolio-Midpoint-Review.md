# Captured-v19 midpoint study — complete, candidate for full-family confirmation

Completed and independently verified **14 September 2026** for the offline `LL/tools/BalanceHarness`: the separately frozen **+10% guardian Health/Power** setting meets all selection rules and is **CandidateForFullFamilyConfirmation**. All **253 original recipes × 256 fresh shared seeds = 64,768 fights** completed. Native execution, complete native reconstruction and independent verification returned success. There were **zero retries, replays, resumes, additional execution-time allocations or resource stops**. The [protocol](Tower-Portfolio-Midpoint-Protocol.md) and complete study are sealed. No gameplay application follows; reliability **Fail 1/3** and adoption **Hold** remain unchanged.

## Result and interpretation

The strongest recipe was `team-040e60d3dbc5c127321653c47ed3a9d3`: **70/256 wins = 27.34375%**, with adjusted interval **17.66929–39.75733%**. It was also the strongest observed recipe in the preceding screen, but this result uses only the new schedule.

| Frozen gate | Complete midpoint result | Decision |
| --- | --- | --- |
| All 253 upper bounds ≤50% | Largest upper bound **39.75733%**; zero exceed 50% | Met |
| At least one lower bound ≥10% | **One recipe**, with lower bound **17.66929%** | Met |
| Highest observed win rate in [15%,40%] | **27.34375%** | Met |

Across all 64,768 outcomes there were **1,804 victories, 62,962 defeats and 2 draws**. The median recipe had **1 win / 256**; **119 recipes had zero wins**. No recipe had an observed rate above 50%. The passing viability gate is narrow in coverage: only one recipe establishes its lower-bound requirement. It does not mean most teams are viable.

The +10% setting is a directly tested candidate for the retained 253-recipe family. The prior +8% and +12% evidence is neither pooled nor used to interpolate these bounds. The candidate still needs a separately designed independent complete relevant-family confirmation; it does not certify the broader 43,879-entry inventory or current-checkout gameplay.

## Measurements and verification

| Measurement | Result |
| --- | ---: |
| Conservative setup through launch | **1,822.950499 seconds / 30.38 minutes** |
| Whole native run, including reconstruction and final publication | **1,394.437 seconds / 23.24 minutes** |
| Native performance snapshot before final metadata/inventory work | **1,390.600014 seconds** |
| Complete separate native verifier | **69.875 seconds** |
| Independent outcome, schedule, statistics, journal and inventory audit | **8.313 seconds** |
| Total charged active work through independent verification | **3,295.614979 seconds / 54.93 minutes** |
| Complete study inventory | **6,700 files** |
| Study output, including engineering builds, history copies and captures | **1,736,375,431 bytes / 1.617126 GiB** |
| CPU at the native snapshot | **1,107.078125 seconds** |
| Peak working set | **945.50 MiB** |
| Cumulative managed allocation | **0.643 TiB** |

These measurements remain within **64,768 attempts, 14,400 active seconds and 8 GiB**. Final engineering closure below also charges active documentation/preservation work and counts the shared TRX and repository changes conservatively. Diagnostic commands used **zero fights**; the test/build wrappers plus preparation, separate native verifier and independent audit took **173.310 seconds** in total, well below the 1,800-second diagnostic allowance. The execution command separately includes its preflight, reconstruction and final checks. No sealed-root files were changed after final publication.

The [persisted performance trace](../TestResults/balance/tower-captured-v19-midpoint-20260914/performance.json) records calls, inclusive/exclusive time, CPU, allocations and memory. Summing exclusive durations by final stage name yields:

| Stage | Calls | Exclusive seconds | Share of snapshot |
| --- | ---: | ---: | ---: |
| Combat simulation without checkpoints | 64,768 | 863.175 | 62.07% |
| Outer durable start/completion flush | 129,536 | 180.792 | 13.00% |
| Compact attempt flush | 64,768 | 98.027 | 7.05% |
| Compatible-report hashing | 194,304 | 72.539 | 5.22% |
| Compact read/decompression/deserialization | 8,096 | 32.421 | 2.33% |
| File hashing | 56,152 | 15.726 | 1.13% |
| Owned-storage checks | 3,059 | 12.059 | 0.87% |

Owned-storage checks took **14.308 seconds inclusive**; three final storage audits took **2.491 seconds inclusive**. The whole campaign phase took **1,319.948 seconds**, outer reconstruction **62.141 seconds**, and preflight **5.802 seconds**. Inclusive parent/child times overlap and must not be added. Durable charging remains intact despite its measurable cost.

This is an observed study runtime, **not a paired performance comparison** against the original v19 experiment, the separate confirmation or the preceding four-factor screen. Their fights, factors and archive shapes differ. The earlier performance work retains its original **622.54× accounting-only** measurement, **4.495× broader sixteen-write** measurement below the 5× target, bounded parity result and unavailable historical timing percentages. This new runtime does not change those conclusions.

The native verifier reconstructed all 64,768 saved records and rosters. The frozen independent audit checked all 253 ordered evidence cells, every shared seed, victories/defeats/draws, all intervals within **1e-8**, the final candidate decision, exact `SC` journal sequence, complete file set and hashes, producing executable/source identities and all registered historical paths/hashes. **Both returned exit 0 / Verified.** All 256 new values were used; the 512 unused original v19 values remain excluded. Required commands completed; the initial sandbox test-build denial was resolved as recorded below.

## Implementation and fixed design

`TowerMidpointStudy.cs` adds the fixed content transform, participant checks, materialization, seed validation, binding, readiness verification and complete-family assessment. `TowerMidpointRun.cs` adds the once-only execution controller and completed archive reconstruction. `Program.cs` dispatches the five midpoint commands. `BalanceHarnessTowerMidpointTests.cs` exercises the real controller through synthetic callbacks without entering combat. Existing grid contracts and evaluator caps are unchanged: the schema-1 **64,768-fight** definition fits the existing 100,000-fight limit.

The producing harness was built from the preceding captured controller sources plus the two midpoint files, against all four sealed captured-v19 gameplay assemblies. Only the isolated study copy changes: guardian Health **3.890286766080** and Offense **4.917322833280**, each exactly **1.10 × baseline**. Reapplying the transform to an already scaled input is rejected. All other content, combat settings, recipe identities, roles, character/Essence order and nominations remain captured-v19. Each ten-character lineup consists of **two five-player parties**.

Materialization verified every baseline roster against the preceding reviewed participant hashes, then checked all 253 midpoint rosters. Only guardian Power, MaxHealth and corresponding starting health may differ. Fresh-schedule binding re-prepared all 253 rosters and reproduced their hashes. These operations had guards that reject entry into combat.

The statistical contract retains per-cell alpha **.05 / 1,012**, even though this fresh fixed setting has 253 cells. Draws count as non-wins. A candidate requires all upper bounds ≤50%, at least one lower bound ≥10%, and the strongest observed win rate in [15%,40%]. The independent all-257-win-count check agreed with the native approximate Wilson intervals within **1e-8**; the resulting eligible strongest count is **46–95 wins out of 256**. No prior results are pooled. These fresh conditional bounds do not claim a cumulative 95% guarantee over unlimited adaptive historical studies.

The controller preserves durable start charging before engine entry and completion charging after a returned outcome, the compact archive's attempt charging, cancellation during all phases, owned storage accounting and complete archive verification. The root writer lease and exclusive start marker reject a second run. An interrupted start remains charged; a failure retains partial evidence. The shared deadline covers preflight, execution, reconstruction and final publication; the frozen outer workflow also charges preparation and the separate verifier.

## Preparation, history and tests

The [study root](../TestResults/balance/tower-captured-v19-midpoint-20260914) contains the checkout snapshot, engineering captures and build outputs, frozen script and command hashes, producing executable, materialized inputs, all history copies, ledger and bound protocol. These files count against this new study's output limit. No old package was edited or reopened.

The registered-history audit checked **144 files / 101 distinct hashes** and reproduced the preceding accepted 128-value schedule with an independent allocator before allocating anything. It found **481,347 prior reservations**, with no additional values outside the preceding ledger. The new **256 shared values** were durably reserved before binding using the frozen master **2026091422** and allocator domain `tower-captured-v19-midpoint-v1`. The [new ledger](../TestResults/balance/tower-captured-v19-midpoint-20260914/seed-ledger.json) therefore preserves **481,603 reservations**, including all **512 unused original v19 confirmation values**. Execution cannot allocate seeds.

| Preparation check | Wall seconds | Result |
| --- | ---: | --- |
| Scoped test wrapper, initial sandbox attempt | 1.250 | Stopped before compilation: NuGet.Config access denied |
| Scoped test wrapper with approved access | 35.226 | **81 passed, 0 failed, 0 skipped** |
| Producing captured build | 2.677 | Exit 0, zero warnings/errors |
| Native all-253 materialization | 8.859 | Exit 0, zero fights |
| Native all-257 arithmetic export | 0.063 | Exit 0, independently matched |
| Registered-history audit | 15.875 | Complete union and previous schedule verified |
| Native fresh-schedule binding | 18.296 | Exit 0, zero fights |
| Native prepared check | 5.954 | Exit 0, zero fights |

The test filter covers midpoint, ceiling-controller and ceiling-preparation tests. It verifies incomplete/reordered evidence rejection, direct scaling, participant preservation, missing/reused history refusal, durable interrupted attempts, cancellation, storage exhaustion, failure retention, no resume and no combat during preflight/reconstruction. All tests use pure arithmetic or synthetic events: **zero diagnostic fights**. The successful backend build reported 34 existing warnings outside the new midpoint files; no new midpoint compiler warnings occurred. The denied first attempt is retained, and the same required test command succeeded after automatic approval granted access. There was no production retry.

Preparation's measured native/audit workflow took **55.969 seconds**. The total conservative setup charge at launch was **1,822.950499 seconds**, including engineering and a disclosed **600-second allowance** for the initial review before `scope.json`. The setup receipt became immutable when `started.json` bound its hash. Its `PreparedVerified` status and zero-fight field describe the prelaunch state; **execution.json and result.json are the completed status authorities**.

## Reproducible commands and producing identity

Commands ran from the repository root. The exact workflow is retained as `workflow.py` and bound by `native-protocol.json`. Its `prepare` and `execute` modes are once-only provenance, not rerunnable instructions for this study. The completed-state native verifier performs zero fights, and any later repetition needs separate diagnostic accounting.

```powershell
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessTowerMidpointTests|FullyQualifiedName~BalanceHarnessTowerCeilingControllerTests|FullyQualifiedName~BalanceHarnessTowerCeilingPreparationTests' `
  -ArtifactsPath TestResults/balance/tower-captured-v19-midpoint-20260914/test-build

dotnet build TestResults/balance/tower-captured-v19-midpoint-20260914/compiler/CapturedMidpoint.csproj `
  -c Release -o TestResults/balance/tower-captured-v19-midpoint-20260914/executable

$study = 'TestResults/balance/tower-captured-v19-midpoint-20260914'
$harness = "$study/executable/BalanceHarness.dll"
# The frozen workflow invoked each preparation/execution command once:
dotnet $harness tower-midpoint-materialize $study
dotnet $harness tower-midpoint-bind $study
dotnet $harness tower-midpoint-check $study
dotnet $harness tower-midpoint-run $study
dotnet $harness tower-midpoint-verify $study
```

| Binding | SHA-256 |
| --- | --- |
| Producing BalanceHarness.dll | `16a2d316afcf227fae0904d5c4bd2144e29f4bf4e2798df381eb918c3f191a8b` |
| Frozen workflow.py | `a032f82900f09069b2c01df47c85433db88ba4e5b9adba7cfff793f73d553d20` |
| Bound protocol.json | `51e51e2853c6c3ba351524bc06093b771d1e0541a78bcd7bfa1e1ac0d6a74af4` |
| New seed-ledger.json | `2d0c1636cdd98877d1742bd37429e80abdd3afa582f0a4a6e12061374ab6f31c` |
| Launch-bound setup-charge.json | `875bdfcbe7c857e924af65f5b4746485fe7c87f45a97068c5a7fc806bfcc3433` |
| Complete midpoint-files.json | `8a23325bea8fe71f62be5f313b6e49cf29814c276572122e1970c2412d0a2300` |
| Preceding screen-files.json | `81a3217fff5d9035efd5540a8e8c42013424dd504e970e19e27bad4a2f5f8da8` |

## Acceptance boundary

The prior four-factor screen remains **Unresolved** and sealed; its +8% and +12% outcomes are separate observations. The original v19 retains **86,016 fights, no confirmation, Unresolved / Hold**. The separate captured-v19 confirmation retains **129,536 fights, reliability Fail 1/3, adoption Hold and ordinary/joint family Fail / Fail**. This midpoint study cannot change those historical decisions or establish search reliability, global optimality, Pack Howler causality, current-checkout gameplay acceptance or practical acquisition coverage.

The eligible midpoint nominates a candidate for a separately designed independent complete relevant-family confirmation. The retained **43,879 recipe/context entries** still need strict materialization/context auditing and a bounded capacity/statistical design. There are no migrations, shared configuration changes, gameplay edits or deployment implications from this offline study.

## Changed files and preservation closure

This task changed seven existing files: the CLI dispatch and six active Markdown handoffs. It added the two fixed midpoint implementation files, their focused test file, frozen protocol and this completion review. The handoffs now point to the 481,603-reservation ledger and retain the older notices as history. No evaluator cap or existing experiment contract changed.

The final checkout comparison used the pre-edit snapshot under the study root. **All 111 earlier review files remain byte-identical**, as do the producing midpoint source files, gameplay assemblies, frozen plan and checked sealed manifest markers. Sixteen existing frontend/UI-verification paths and eighteen new UI-verification paths changed concurrently; those hashes are recorded below, and none were edited by this task. HEAD remained `bfb1023fe8de9ff3cb14c253cec0192d14e4bc98`. Existing dirty work was preserved, and no commit was made.

The scoped `git diff --check` returned **0**; its single line-ending warning concerns the repository's LF/CRLF conversion. New-document local links, all six current notices and the copied/shared **81-pass TRX** were verified. This closure used zero fights and took **0.750 seconds**. No further test or combat repetition was needed after the successful frozen producing build.

<details>
<summary>Checkout and documentation verification receipt</summary>

```json
{
  "status": "Verified",
  "seconds": 0.75,
  "head": "bfb1023fe8de9ff3cb14c253cec0192d14e4bc98",
  "scopedExistingChanges": {
    "Balance Harness/Automatic-Tower-Team-Discovery-Implementation.md": "1a65bad4ac9296946fdb3e99e51183674c4481ad5b8a9b3492851a4885204ab5",
    "Balance Harness/Automatic-Tower-Team-Discovery-Plan.md": "b32e9573c8cfc0d1ed506221e5c4e7cdce113ad893345010a9dde8ab1cc4a489",
    "Balance Harness/Tower-Balance-Acceptance-Policy.md": "2cb44a39ad60245c3188ad06458cbcd3f01238aab3726b10382ae0e5e4b79c1d",
    "Balance Harness/Tower-Coverage-Replication-Plan.md": "d04631f108bda0e703bcee8e2eb436a898e717adde7355af12ae1b94bdd5b4fd",
    "Balance Harness/Tower-Search-Strategy-Reset.md": "16d5c7e104500d1d48c5a83c5420a21110c4cb8fa059a5f5073e742fb148b287",
    "LL/tools/BalanceHarness/Program.cs": "ac95700d37b9b270667e100ad2d7b4e95f75825792e818db841817e9cc913ddd",
    "LL/tools/BalanceHarness/README.md": "082e36ebbb1ebb43f8e323ec7af1c9b5c3bdf4f9c5b11e6cb3af41aaafadc8d1"
  },
  "scopedNewFiles": {
    "Balance Harness/Tower-Portfolio-Midpoint-Protocol.md": "b6dbddeed1fff5eba6d715bc3f38a843f3f7a91ee68a8f4c32dc1cd0386f99ab",
    "LL/tests/EssenceSystem.Tests/BalanceHarnessTowerMidpointTests.cs": "15c8e246296f88f6258b040e76dc8bc132b296626f3016c5fdd9b08639af0560",
    "LL/tools/BalanceHarness/TowerMidpointRun.cs": "3b6cd57b5cabfe0695f0794c9d152e459abe4191dc8ff4ca08fc7d1958425b3d",
    "LL/tools/BalanceHarness/TowerMidpointStudy.cs": "63c98c0b5123e12fe6a5fd3639489e0f8f237ff2207219285b77081153cb5f69"
  },
  "concurrentExistingChanges": {
    "LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.scss": "fb53ab20b1f45640be8e0bb5e3e6821d3e1794e0dd54d2874f7c13b392fefba2",
    "LL/src/Presentation/ll/src/app/features/game/city/guild/guild.component.html": "def45bb4fb99424237146f7468b937340f6538ee5d15d91dc5f6ea6157ef90c2",
    "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-vault/guild-vault.component.html": "4a2e5719d3a7d29758f90a17626bbaaadbb7764ebf6953249feadc9e0f0a97ce",
    "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/in-a-guild.component.html": "eb92f4109cfa1035dbedc114db5780672b89fe9dc31b379596633a6f9989c8e5",
    "LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/in-a-guild.component.scss": "ebbc3261cddce3e92bc19c47c50af998c733a06df3c631a87e6a423c6cb0fd3d",
    "LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-commodity/market-place-commodity.component.css": "c0280a6bcbcdf6344c442eb9c3c61fe73516a73b0b0fd5951c443e0173b448f8",
    "LL/src/Presentation/ll/src/app/features/game/world/raid/playback/raid-playback.component.scss": "929e3d7c31f57b1babf0f957a55e1901e939379a999adc57d8d42ada3484d6f1",
    "LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.scss": "638dca49ef5e23b7265d0248484f80a12cc3dcfcfd52ec153f0ba18f5d3ee3f9",
    "LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.ts": "5d86f4f44b1cccabed38a0ec0af1a8859cf1a3f1f942b5d4e7703c68dd2f75f0",
    "LL/src/Presentation/ll/src/app/shared/components/default-header/default-header.component.html": "243aa22f9274f27d3816b71bc25df1b72c789ee26e9f575e5ba11374e23093bc",
    "LL/src/Presentation/ll/src/index.html": "f9f3e9df64849e9b41c05d1412b039fc04768255a662bdf7c2a5d777a9b7f8df",
    "LL/src/Presentation/ll/src/main.ts": "026b587183b4924df78ae4cdea15cce958d649e7e3d27fb4e07ac8d33354281f",
    "UI_REWORK_IMPLEMENTATION_PLAN.md": "1beb92b25f9765ce084dfdea6e536695402855bd9650b39392d281cf8d9b9480",
    "docs/ui-rework-verification/README.md": "a15f369a1fad088b558fe85309da1b102a41a5fb9fc104760cc4b6948a46ac0a",
    "docs/ui-rework-verification/live-smoke.cjs": "ab3d5e18910f5c5873232231d32ad898de5790e1fa170cb02b93603cdf9f2728",
    "docs/ui-rework-verification/visual-smoke.cjs": "424e20a66e698ed40e4faa9253ee36564ce29937f867ce939ffb3e487240791e"
  },
  "concurrentNewFiles": {
    "docs/ui-rework-verification/browser-fixtures.cjs": "ac2258e5221205bb8bf2ed7e5266002d4408c6bad522f2aefa5bb6c871f6950c",
    "docs/ui-rework-verification/collections-smoke.cjs": "835021b0a9a14f66d87a948bbb583a674b9fa389b68d89f8d3895075d32ea8fd",
    "docs/ui-rework-verification/encounter-data.cjs": "160ec28623e620e471ef222bb502ddd06f9ccc1bbff9ea8da374d02b8ea9966c",
    "docs/ui-rework-verification/encounter-smoke.cjs": "a5fd469054437bd24dc04ea2c5b6f6e7c4eb9548d3148a4e1f775a25871d5b73",
    "docs/ui-rework-verification/fixture-data.cjs": "e9fb74729fec3b9e19a5e1ad82c58536a5c3f71bbfc630bfb7466994845c9010",
    "docs/ui-rework-verification/guild-guild-mobile.png": "d69ac56c332851cb587daea724ed3c430c487017d481cd96fdccf500e11bf1e5",
    "docs/ui-rework-verification/guild-members-reading.png": "df2b268bf38db89581fe18ecce3ef7e288d9e4a8dc87983de3f6f9dfafcbf287",
    "docs/ui-rework-verification/guild-vault-desktop.png": "fcfec99087afda3e87966415a0edc8be9e7957e4fd72c05b59d07f6fdb349ef1",
    "docs/ui-rework-verification/market-book-desktop.png": "a9c4e72f40c140450561358035112ae55a00901cc3317eebb7136a926714e5b9",
    "docs/ui-rework-verification/market-book-mobile.png": "dca890b3dd2147b87481d94171d126f43519c2c3756286142fa2df62760fb0bb",
    "docs/ui-rework-verification/populated-data.cjs": "bc8ef85193550ef5910ef5a16433207ebfcf13b510eabf99e5e28c874d50f5ff",
    "docs/ui-rework-verification/populated-smoke.cjs": "3d540156a4e61e4b2ae825569d1418f4be4807633a71584ec493326dfe1bd7e0",
    "docs/ui-rework-verification/startup-mobile.png": "cadc9ea71870bb521ef5ca42a46e20cd64273f6d5b2c95f4993a4e7e1e464b24",
    "docs/ui-rework-verification/startup-smoke.cjs": "832fc03d24cafa506b3d5d0273b21d8b02914ccc08c30fb86f67d1c8fef5aa81",
    "docs/ui-rework-verification/tournament-semifinals-desktop.png": "6407d5951039491e961cec34740105c04fb029308a66b109189ee4eb06ebc09c",
    "docs/ui-rework-verification/tournament-semifinals-mobile.png": "868be1f4f677a4083c210c576776edceb954648cdfd03b79814cbac8a9f0a6ae",
    "docs/ui-rework-verification/tower-floor-desktop.png": "fbcfed55a8719dd7edfd31df5e79726bacc4d49f528cea3b5923afbabe61b51c",
    "docs/ui-rework-verification/tower-floor-mobile.png": "f5869207e2f4ee63991afb35ec88546590ab367e8c31045f672df6326a7ab32d"
  },
  "earlierReviewsUnchanged": 111,
  "verifiedMarkers": {
    "TestResults/balance/tower-search-portfolio-20260914/final-files.json": "fd6131f46073986c5c6b66d25f62e0f0b34cc8c4034b94d7ce83fb404a3f2a03",
    "TestResults/balance/tower-portfolio-confirmation-20260914/final-files.json": "0bc29b4db8106ce5fce65ab8c4b4ea644019bc45fc75b3d30b00a28d247ebe75",
    "TestResults/balance/tower-ceiling-screen-preparation-20260914/evidence-files.json": "21a0759185d49095206989b46f2bcd43825a7900e2d18c9eca0db16459edd30a",
    "TestResults/balance/tower-captured-v19-ceiling-screen-20260914/screen-files.json": "81a3217fff5d9035efd5540a8e8c42013424dd504e970e19e27bad4a2f5f8da8",
    "TestResults/balance/tower-captured-v19-midpoint-20260914/midpoint-files.json": "8a23325bea8fe71f62be5f313b6e49cf29814c276572122e1970c2412d0a2300"
  },
  "verifiedNewDocumentLinks": 5,
  "tests": {
    "total": "81",
    "executed": "81",
    "passed": "81",
    "failed": "0",
    "error": "0",
    "timeout": "0",
    "aborted": "0",
    "inconclusive": "0",
    "passedButRunAborted": "0",
    "notRunnable": "0",
    "notExecuted": "0",
    "disconnected": "0",
    "warning": "0",
    "completed": "0",
    "inProgress": "0",
    "pending": "0"
  },
  "diffCheckExitCode": 0,
  "diffCheckWarnings": 1,
  "producingFilesMatch": true,
  "frozenPlanMatch": true,
  "ownFiles": [
    "Balance Harness/Automatic-Tower-Team-Discovery-Implementation.md",
    "Balance Harness/Automatic-Tower-Team-Discovery-Plan.md",
    "Balance Harness/Tower-Balance-Acceptance-Policy.md",
    "Balance Harness/Tower-Coverage-Replication-Plan.md",
    "Balance Harness/Tower-Search-Strategy-Reset.md",
    "LL/tools/BalanceHarness/README.md",
    "LL/tools/BalanceHarness/Program.cs",
    "LL/tools/BalanceHarness/TowerMidpointStudy.cs",
    "LL/tools/BalanceHarness/TowerMidpointRun.cs",
    "LL/tests/EssenceSystem.Tests/BalanceHarnessTowerMidpointTests.cs",
    "Balance Harness/Tower-Portfolio-Midpoint-Protocol.md",
    "Balance Harness/Tower-Portfolio-Midpoint-Review.md"
  ],
  "currentOwnHashes": {
    "Balance Harness/Automatic-Tower-Team-Discovery-Implementation.md": "1a65bad4ac9296946fdb3e99e51183674c4481ad5b8a9b3492851a4885204ab5",
    "Balance Harness/Automatic-Tower-Team-Discovery-Plan.md": "b32e9573c8cfc0d1ed506221e5c4e7cdce113ad893345010a9dde8ab1cc4a489",
    "Balance Harness/Tower-Balance-Acceptance-Policy.md": "2cb44a39ad60245c3188ad06458cbcd3f01238aab3726b10382ae0e5e4b79c1d",
    "Balance Harness/Tower-Coverage-Replication-Plan.md": "d04631f108bda0e703bcee8e2eb436a898e717adde7355af12ae1b94bdd5b4fd",
    "Balance Harness/Tower-Search-Strategy-Reset.md": "16d5c7e104500d1d48c5a83c5420a21110c4cb8fa059a5f5073e742fb148b287",
    "LL/tools/BalanceHarness/README.md": "082e36ebbb1ebb43f8e323ec7af1c9b5c3bdf4f9c5b11e6cb3af41aaafadc8d1",
    "LL/tools/BalanceHarness/Program.cs": "ac95700d37b9b270667e100ad2d7b4e95f75825792e818db841817e9cc913ddd",
    "LL/tools/BalanceHarness/TowerMidpointStudy.cs": "63c98c0b5123e12fe6a5fd3639489e0f8f237ff2207219285b77081153cb5f69",
    "LL/tools/BalanceHarness/TowerMidpointRun.cs": "3b6cd57b5cabfe0695f0794c9d152e459abe4191dc8ff4ca08fc7d1958425b3d",
    "LL/tests/EssenceSystem.Tests/BalanceHarnessTowerMidpointTests.cs": "15c8e246296f88f6258b040e76dc8bc132b296626f3016c5fdd9b08639af0560",
    "Balance Harness/Tower-Portfolio-Midpoint-Protocol.md": "b6dbddeed1fff5eba6d715bc3f38a843f3f7a91ee68a8f4c32dc1cd0386f99ab"
  },
  "currentOwnBytes": 592821,
  "sharedTrxBytes": 132636,
  "reviewHashBoundary": "This review is omitted from the hashes because this closure is appended to it. All other scoped file hashes are final."
}
```

</details>

## Frozen workflow completion receipt

Transcribed from the successful once-only `workflow.py execute` tool output. The wrapper wrote no additional files after the native final inventory was sealed.

```json
{
  "event": "final-independent-verification",
  "status": "Verified",
  "studyStatus": "CandidateForFullFamilyConfirmation",
  "factor": 1.1,
  "fights": 64768,
  "retries": 0,
  "newSeedsDuringExecution": 0,
  "reservations": 481603,
  "screenSeedsUsed": 256,
  "oldV19Unused": 512,
  "maximumWins": 70,
  "strongest": [
    {
      "id": "team-040e60d3dbc5c127321653c47ed3a9d3",
      "wins": 70,
      "rate": 0.2734375,
      "lower": 0.1766928789554799,
      "upper": 0.397573309727828,
      "confidence": 0.9999505928853755
    }
  ],
  "observedAboveCeiling": 0,
  "upperAboveCeiling": 0,
  "supportedViable": 1,
  "zeroWinRecipes": 119,
  "totalWins": 1804,
  "totalDraws": 2,
  "medianWins": 1,
  "runCommandSeconds": 1394.4370000000054,
  "verifyCommandSeconds": 69.875,
  "independentSeconds": 8.312999999994645,
  "chargedActiveSeconds": 3295.614979,
  "bytes": 1736375431,
  "files": 6700,
  "manifestHash": "8a23325bea8fe71f62be5f313b6e49cf29814c276572122e1970c2412d0a2300",
  "ledgerHash": "2d0c1636cdd98877d1742bd37429e80abdd3afa582f0a4a6e12061374ab6f31c",
  "setupHash": "875bdfcbe7c857e924af65f5b4746485fe7c87f45a97068c5a7fc806bfcc3433",
  "protocolHash": "51e51e2853c6c3ba351524bc06093b771d1e0541a78bcd7bfa1e1ac0d6a74af4",
  "boundary": "Native immutable artifacts retain measurements. Transcribe this verification receipt into engineering completion Markdown; no post-seal root writes."
}
```


## Final resource closure

Final accounting includes the full byte sizes of all changed repository files and the shared TRX, in addition to their retained copies where present. A conservative **1 MiB allowance** covers this whole completion review, and **60 seconds** covers its final write and final metadata checks. These are disclosed upper bounds, not extra workload authorization. They remain inside the frozen envelope. No sealed study file was added or changed.

```json
{
  "status": "CompleteVerified",
  "utc": "2026-09-14T15:39:13.884065+00:00",
  "measuredActiveSecondsBeforeFinalWrite": 3593.54229,
  "conservativeActiveSecondsIncludingFinalWrite": 3653.54229,
  "maximumActiveSeconds": 14400,
  "studyFiles": 6700,
  "studyBytes": 1736375431,
  "externalEngineeringAndSharedTrxBytes": 709306,
  "reviewByteAllowance": 1048576,
  "conservativeTotalBytes": 1738133313,
  "maximumBytes": 8589934592,
  "diagnosticCommandSecondsIncludingClosureChecks": 174.0598917,
  "diagnosticFights": 0,
  "maximumDiagnosticSeconds": 1800,
  "newStudyFights": 64768,
  "maximumStudyFights": 64768,
  "newReservations": 256,
  "allReservations": 481603,
  "unusedOriginalV19ConfirmationReservations": 512,
  "retries": 0,
  "replays": 0,
  "resume": false,
  "gameplayApplied": false,
  "reliability": "Fail 1/3",
  "adoption": "Hold",
  "statisticalResult": "CandidateForFullFamilyConfirmation"
}
```
