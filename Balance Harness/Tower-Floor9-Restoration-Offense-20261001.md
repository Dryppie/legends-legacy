# Floor 9: Restoration offense calibration — 1 October 2026

## Completed result

**All six panels completed: 15,648 fresh fights / 96 reservations, independently audited. Original offense ×1.00 with penetration ×40 is the sole nominated setting. Floor 9 is not yet accepted or applied.**

| Offense relative to original | Eight-item wins: reference 1 / 2 / 3 / 5b6c… / 4a9f… | Eligible compositions at least 6/32 | Highest wins, any recipe | Nomination |
| --- | --- | ---: | ---: | --- |
| **1.00** | **3 / 2 / 6 / 6 / 6** | **3** | **11/32** | **Selected for fresh acceptance testing** |
| 1.05 | 0 / 0 / 4 / 2 / 1 | 0 | 9/32 | No |
| 1.10 | 0 / 0 / 0 / 1 / 2 | 0 | 3/32 | No |

The eight-item setups use MainHand, Chest, Head and Necklace on healer slots 2 and 7, retaining baseline rings/relics and the rest of the party. Every original control remains. Each setting uses 32 shared seeds across its recipes; settings use disjoint fresh seeds. These small descriptive samples nominate a candidate and do not establish exact win probabilities or acceptance. At the nominated setting, original guardian offense is unchanged; only penetration differs from live content.

## Next bounded acceptance study

Hold this single setting fixed. Implement a separate strict acceptance version for the complete **163-recipe / five-composition / 130-eligible-recipe** family. Use **512 fresh seeds per recipe in screening**, then **512 independent seeds in confirmation only after screening passes**. Each phase has eight 64-seed batches; maximum **166,912 fresh fights / 1,024 reservations**. No diagnostic results transfer. The larger sample resolves the limited-route minimum more precisely without relaxing either balance bound.

The unchanged approximate simultaneous 95% Bonferroni-Wilson criteria require at least two distinct eligible compositions with a recipe at least **76/512 wins**, and **every recipe at most 215/512**. Preserve the eight-specialized-item/two-character equipment rule. No retry, extension, extra candidate, interim selection, seed replacement or dropped controls. After complete independent confirmation only, require all **83,456 confirmation inputs** and **5,216 historical full replays**, then local application parity and relevant backend regression. No external deployment.

Measured doubled runtime for a 64-seed batch is **498.78 seconds**, below the 672-second admission ceiling; projected bytes are about 591 MB. Recheck resource and disk admission before each allocation. The new acceptance contract is **proposed, not implemented or allocated**.

Proposal: `TestResults/tower-floor9-restoration-acceptance-proposal-20261001.json`, SHA-256 `31d2fd371ad65b4c101a5f13d3f0bc73115efdbcdfcb459e5e5ae04db1b80c29`.

## Verification and changes

- **492 Python checks pass**, including exact candidate dispatch, full-grid selection, old acceptance rejection and older diagnostic bounds. An older CLI regression expected a maximum of four panel indices; it was updated for six while the older diagnostic still rejects indices beyond its own four-panel contract. The full suite then passed.
- **349 native tests / zero skips pass** through `build/run-tests.ps1`, on both fresh test compilation and the authenticated corrected production runtime. **978 zero-fight preparations** check every original/candidate pair at every setting. No production assembly changed. The earlier broader 766-pass/four-skip proof is retained, including its known pre-existing Kharad behavior-manifest failure; it was not rerun or claimed fixed.
- All **six native study fixtures pass**. Every one of the 15,648 outcomes and prepared participant sets was independently recounted. No battle retry, replacement seed or extension. Final exclusion union: **926,604**.
- Added separate Python candidate/diagnostic helpers and tests, explicit owner dispatch, native candidate/batch/preparation checks, and this report/status/handoff updates. Existing generic penetration and old acceptance guards remain unchanged.
- All **102 live catalogs** remain unchanged. No combat-engine change, migration, configuration change or deployment. No required verification command remains blocked.

Verification commands: `python -B -X utf8 TestResults/tower-floor9-restoration-offense-tests-repair1-20261001.py`; the native runtime script invokes `build/run-tests.ps1` for build and bound-runtime checks; `python -B -X utf8 TestResults/tower-floor9-restoration-offense-driver-20261001.py`; `python -B -X utf8 TestResults/tower-floor9-restoration-offense-collect-20261001.py`. Exact native commands are archived in the runtime-verification directory. Do not rerun completed study owners.

Evidence: `TestResults/tower-floor9-restoration-offense-evidence-20261001.json`, SHA-256 `c8aaa9bf007dcfcc7b11dd3a07e017f0c7222a63c59205933b7c2f834fc65c06`.

Declaration: `TestResults/tower-floor9-restoration-offense-driver-20261001/declaration.json`, SHA-256 `8d383e3248b55e2a42d0d6d38aea7e671fb8593f6e24d1199bc13fe5b41a4468`.

Runtime proof: `TestResults/tower-floor9-restoration-offense-runtime-verification-20261001/completion.json`. Python proof: `TestResults/tower-floor9-restoration-offense-tests-20261001-repair1/completion.json`. Publication: `TestResults/tower-floor9-restoration-offense-publication-20261001/completion.json`.

Floor 9 remains unresolved, followed by floors 10 and 12–15 and the final current-version 1–15 sweep.

## Frozen protocol

Test the exact proposed offense factors **1.00, 1.05, 1.10 relative to original live Ni**, with penetration fixed at **40**. Health, 10% copy Health, corrected summon defenses, kit and all other catalogs stay unchanged. Retain every raw recipe and its original identity/order: **163 recipes, five actual compositions, 130 eligible recipes** under the eight-specialized-item/two-character rule.

- Two sixteen-seed batches per setting, ordered 1.00/1.00/1.05/1.05/1.10/1.10; **15,648 diagnostic fights / 96 fresh reservations maximum**. Initial exclusions: **926,508**.
- Complete the entire grid before selection. No retry, extension, replacement seeds, discarded controls or historical outcome pooling. No confirmation or application in this diagnostic.
- Each batch requires doubled measured cost below 80% of the 840-second native / 900-second owner / 2-GiB limits, plus remaining projected disk and 2 GiB.
- Independently recount every raw report and match native prepared participants to the zero-fight qualification at that exact setting.
- Descriptive nomination only: every recipe at most **12/32 wins** and at least **two distinct eligible compositions** with a recipe at least **6/32 wins**. Choose the lowest qualifying offense. These margins cannot establish acceptance; any nomination needs later fresh formal screening and independent confirmation.

The new version has separate strict dispatch and native guards. The generic penetration guard and the rejected 0.95 candidate's old acceptance contract remain unchanged. Native qualification: **349 tests, zero skips, 978 preparations**; Python checks: **492**. No study fights or seeds were used during preparation.

Frozen proposal: `TestResults/tower-floor9-restoration-offense-proposal-20261001.json`, SHA-256 `4ec5f4d30fee27a851ce7781d92b901cfc7108f70703f475152aa715c1b769d6`.
