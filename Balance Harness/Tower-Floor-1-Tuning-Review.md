# Floor 1 tuning for the user-authored party

11 September 2026. This historical experiment increased Garran's floor-specific offense multiplier from **1.24** to **2.0**. The user's unchanged five-character party confirmed at **394/1,000 wins (39.4%)**, with a pointwise 95% Wilson interval of **36.42–42.46%**. Both the estimate and its interval fit the approved **10–50%** target. That value was applied locally at publication; nothing was deployed.

**Superseded local setting, 11 September 2026:** following approval of [linked Health/Power tuning](Boss-Specific-Essence-Loadout-Plan.md#approved-boss-tuning-controls), the user requested resetting Garran to original Health 1.27 / offense 1.24 and adjusting from there. The separate [linked experiment](Tower-Floor-1-Linked-Tuning-Review.md) now applies Health **1.6764** / offense **1.6368**, with **291/1,000** fixed-party wins. All results below remain historical evidence for the offense-only experiment; its sealed package is unchanged. Independent-team coverage remains open.

## Change and scope

The only gameplay edit is `floors[0].guardianScaling.offense` in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). This uses the existing floor-specific scaling path. The offense scaling input rises by **61.29%**; actual damage continues to use the existing abilities and mitigation rules. Health remains 1.27, defense/resistance remain 1.09, and every other floor definition and content file remains unchanged.

The [user-authored scenario](../LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json) is unchanged: exact character positions and Essence order, level 30, four level-1 unascended/unevolved Essences, seven Uncommon Standard tier-1/rank-1 equipment items, baseline rolls and no styles, scouting or contributions. [The original benchmark](Tower-Floor-1-User-Party-Review.md) records the complete party and equipment assumptions.

This is **acceptance for the specified party and budget**. It does not establish that every other legal build stays below 50%, accept the full Tower progression curve or install this party as a default for other floors. Automatic team generation and the [Tower acceptance evaluator](Tower-Balance-Acceptance-Policy.md) were subsequent work and are now [implemented through Tower Lab](Automatic-Tower-Team-Lab-Review.md). Their [fixed balance pilots](Automatic-Tower-Team-Pilot-Review.md) are now complete; the measurements below retain their original content and protocol.

## Fixed tuning protocol

Before the first tuning fight, the experiment verified the earlier 1,000-battle evidence package and froze the original content, executable, exact party, stage schedules and a **4,699-combat maximum**. All stage seeds exclude the captured historical exclusions and the preceding user-party benchmark. Discovery and confirmation are disjoint; candidates within a stage share seeds for paired comparisons.

Only offense could change. The nine coarse values were **1.24, 1.36, 1.48, 1.60, 1.80, 2.00, 2.40, 3.20 and 4.80**, with 50 battles each. The predeclared refinement rule chose the adjacent interval straddling a 30% discovery aim, then tested eleven evenly spaced values on 100 different paired seeds each. It selected the observation closest to 30% inside the declared eligibility band, breaking ties by larger sample size and then lower offense. That selection froze before fresh confirmation.

| Coarse offense | Wins / 50 | Draws |
| --- | ---: | ---: |
| 1.24 | 50 | 0 |
| 1.36 | 50 | 0 |
| 1.48 | 50 | 0 |
| 1.60 | 50 | 0 |
| 1.80 | 48 | 0 |
| 2.00 | 20 | 1 |
| 2.40 | 0 | 0 |
| 3.20 | 0 | 0 |
| 4.80 | 0 | 0 |

The fine grid ran from 2.00 through 2.40 in 0.04 increments. Its wins out of 100 were **32, 24, 21, 11, 6, 6, 3, 1, 1, 0 and 0**, with no draws. **2.00** was selected from discovery. The later 39.4% confirmation did not trigger another search, sample extension or replacement candidate.

## Fresh confirmation and local reproduction

Both the original and selected scaling ran on the same **1,000 previously unused seeds**:

| Scaling | Wins | Defeats | Draws | Win rate | Pointwise 95% Wilson |
| --- | ---: | ---: | ---: | ---: | --- |
| Original offense 1.24 | 1,000 | 0 | 0 | 100% | 99.62–100% |
| Selected offense 2.00 | 394 | 604 | 2 | **39.4%** | **36.42–42.46%** |

Draws count as non-wins. All planned trials were valid. The selected setting lost 606 winning seeds relative to the original and gained none, as expected for this difficulty increase. Mean winning duration was **114.48 seconds**; mean defeat/draw duration was **147.35 seconds**. Duration was descriptive, not an acceptance target.

After confirmation passed, the single scalar was changed in the checked-in catalog. Running that local content on the same 1,000 candidate identities reproduced **every saved battle report exactly**, including the 394 victories, 604 defeats and two draws. This is a parity check, not another independent 1,000-sample confirmation.

## Regression and verification

Every floor from **2 through 15** received five paired before/after trials using its existing balanced progression-curve party, not the user's floor-1 party. All **70 paired complete reports matched exactly**. The structural content audit also verifies that the floor-1 offense field is the only gameplay change; shared abilities, player Essences and the other fifteen captured content files are byte-identical to the original snapshot.

Seven detailed replays matched preparation, combat and Tower outcomes: the first victory from original confirmation, and the first defeat, victory and draw from both candidate confirmation and local parity. The draw selected by the frozen rule was `tower.0024`; the first candidate victory was `tower.0005`.

Actual experiment cost was **4,697/4,699 combats**: 450 coarse discovery, 1,100 fine discovery, 2,000 paired confirmation, 1,000 local parity, 140 other-floor regression and seven detailed replays. The artifact audit reconstructs all **51 runs and 4,690 saved battles**, verifies their hashes, complete ordered parties and gear, content/assembly identities, seed separation, counts, Wilson intervals, discovery selection, local parity and replays. Repeated observations are not pooled as fresh samples.

**115 relevant backend tests passed**, with zero failures/skips. The cached build succeeded with zero warnings/errors. Its initial sandbox attempt could not update an existing generated static-web-assets cache; the same local build and tests completed outside the sandbox. No required verification remains blocked.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~WorldTowerTests'
```

## Retained evidence and reproduction

The [tuning package](../TestResults/balance/tower-floor-1-tuning-20260911/) retains the protocol, exact seed ledger, original/variant content, executable, stage recipes, selection, every battle and manifest, confirmation/parity/regression summaries, detailed replays, backend logs/TRX, orchestration/audit scripts and checksum inventory. Earlier sealed evidence remains unchanged. The native legacy scorecards retain their original wording; this review applies the subsequently approved 10–50% target.

The [exact confirmation recipe](../TestResults/balance/tower-floor-1-tuning-20260911/scenarios/confirmation-candidate.json) reproduces the accepted seed schedule against its retained offense-only snapshot, since current live content has since changed:

```powershell
dotnet TestResults/balance/tower-floor-1-tuning-20260911/executable/BalanceHarness.dll tower --scenario TestResults/balance/tower-floor-1-tuning-20260911/scenarios/confirmation-candidate.json --content-root TestResults/balance/tower-floor-1-tuning-20260911/variants/selected --output TestResults/balance/tower-floor-1-tuned-rerun
```

Use a new output directory. Reusing this schedule reproduces the observation; it does not create fresh confirmation. The retained `audit.py --verify-existing` reconstructs the saved audit without combat or writes, but also asserts the original publication's live offense 2.0; that assertion intentionally no longer matches current content. Preserve the script and use retained snapshots for historical reproduction. Exact historical replay uses the retained executable with `replay --run <saved-run> --battle <trial-id> --detailed`.

Changed files comprise the single floor-1 catalog value and this review plus documentation follow-ups. No engine or ability code, appsettings, migrations or account state changed. The catalog change is ready for the normal game content release process, but no deployment, service restart or external environment change was performed.
