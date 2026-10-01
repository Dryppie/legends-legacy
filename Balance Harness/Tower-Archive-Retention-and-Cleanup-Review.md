# Tower archive storage and cleanup review

## Second follow-up — 1 October 2026

The user authorized the next four recommendations: compact output in the current balance-pass runner, an overall storage budget and retention preview, another lossless archive-compression pass, and investigation/fixing of duplicated build output with review of old Angular previews.

The filesystem work is complete and verified, reclaiming **58.24 GiB** by per-file accounting: **50.27 GiB** from lossless NTFS compression and **7.98 GiB** from eight obsolete generated Angular previews. Drive free space was **433.4 GiB** at the final check, up from **379.6 GiB** before this pass. That drive-wide change also includes concurrent study activity, isolated builds and other applications; it is distinct from the per-file savings.

| Compressed archive under `TestResults/balance` | Before on disk | After on disk | Files with identical before/after SHA-256 |
| --- | ---: | ---: | ---: |
| `tower-competitive-kharad-resolution-20260912` | 41.53 GiB | 13.44 GiB | 52,046 |
| `tower-competitive-kharad-precision-20260912` | 16.48 GiB | 5.33 GiB | 20,755 |
| `tower-dashboard-20260909` | 16.33 GiB | 5.31 GiB | 46,965 |

All **119,766 files** retain identical bytes, paths and inventory. No raw battle evidence was deleted in this pass. The eight removed August Angular builds were `blueprint-attributes-angular`, `chat-bottom-anchor-angular`, `chat-nearest-anchor-angular`, `collapsed-chat-pin-angular`, `draggable-chat-angular`, `draggable-chat-angular-final`, `guild-overview-angular` and `mobile-popover-angular`, directly under `artifacts`. They contained 10,264 generated files, had no tracked files or tracked references, and were inventoried with hashes before deletion. The active frontend still returned HTTP 200.

The [compression receipt](../TestResults/balance/tower-storage-followup-20261001/completed.json), [preview-removal receipt](../TestResults/balance/tower-storage-followup-20261001/preview-cleanup-completed.json), and [final integrity checks](../TestResults/balance/tower-storage-followup-20261001/final-filesystem-checks.json) record the exact totals. Final verification also matched all **156 previously protected metadata/catalog hashes** and the active study's **866 source pins plus 102 live-catalog pins**.

Preventative code is implemented and tested in the attached **`balance-storage` checkout**, with its `Tower-Storage-Controls-Review.md` and harness README documenting operation. The user explicitly chose to keep these changes isolated until the active floor-ten acceptance study finishes, because it hashes the affected source files. They have not been substituted into that study or its producing runtime.

The implementation is preserved in local branch `codex/balance-storage`, commit `b1834014c33f0bb59927c1e0944486238add83f2`. The [handoff receipt](../TestResults/balance/tower-storage-followup-20261001/implementation-handoff.json) records the checkout, patch digest, exact test receipts, measured comparison and deferred activation decision.

The new runner writes compact fixed-family evaluation archives with compressed materialized inputs, preserving all non-event reports and exact schedules. The measured diagnostic was **22.15% smaller than gzip-per-battle output**, with 32 full report hashes and result rows matching. An overall 1,280-GiB logical TestResults budget, 64-GiB drive headroom, three-GiB phase admission reservation and explicit protected/retired preview prevent unbounded accumulation by participating runs. Unknown historical archives and seed ledgers remain protected. The actual gzip and compact runner paths also passed separate independent end-to-end audits with identical result hashes.

Build duplication was reproduced: redirecting output paths caused the Web SDK to include old generated trees as content. Permanent SDK exclusions reduced the primary API's redirected evaluation from **21,733 items (21,628 generated)** to **105 intended items (zero generated)**. Historical backend outputs remain because archived studies may depend on their binaries. Code verification passed **69 backend tests plus two explicit end-to-end runner invocations, 51 Python tests, and normal/redirected build-item probes**; the generic suite intentionally skipped its live-experiment entry point. No gameplay content, dependency, database migration or deployment changed. Offline storage policy and SDK build-item configuration changes remain in the isolated checkout pending the user's chosen activation boundary.

## Completed follow-up — 1 October 2026

Following the user's authorization to proceed with all three reviewed actions, cleanup completed and passed verification at **21:35 Copenhagen time**. Drive free space increased from **129.6 GiB to 384.3 GiB**, a measured net gain of **254.7 GiB**. Drive-wide figures include concurrent activity and filesystem allocation; they are distinct from the per-file estimates below.

| Completed action | Space removed or saved |
| --- | ---: |
| Pruned the exact 11,371 reviewed tuning/inconclusive run directories, containing 475,649 files | 112.8 GiB |
| Removed stale Angular Webpack/Babel caches and the dashboard's old cache, containing 1,761 files | 14.4 GiB |
| Applied lossless NTFS compression to the remaining three campaign archives | 127.5 GiB |

Pruning ran before compression, so these action estimates do not overlap. The retained campaign files occupy **63.7 GiB on disk**, with **191.1 GiB of unchanged logical contents**. The active main frontend cache (approximately 13 MiB) was preserved, its development server remained running, and an HTTP check returned **200 OK**.

The three affected directories under `TestResults/balance` are `tower-progression-calibration-20260911`, `tower-retained-build-calibration-20260911-v2`, and `tower-kharad-followup-calibration-20260911`. Removed raw data consists of completed coarse/fine tuning and the original, never-applied, inconclusive floor-2 confirmation. The later passing floor-2 follow-up, other accepted confirmations, recipes, observations, portfolios, seed/exclusion ledgers, content, original executables and historical reports remain.

**These are now partially pruned historical packages.** Their original checksum manifests and seals remain unchanged as historical records. Complete original-package audits cannot pass with the explicitly removed runs absent. Every remaining indexed file was freshly checked against its original SHA-256: **375,601 matches**, with exact remaining file counts and logical lengths verified. All **156 protected metadata/catalog checks** also passed after cleanup. All 375,607 remaining campaign files have NTFS compression enabled; application-visible bytes and filenames are unchanged.

The [completion receipt](../TestResults/balance/tower-storage-cleanup-20261001/completed.json), [approved exact target inventory](../TestResults/balance/tower-storage-cleanup-20261001/approved-candidates.json), [preflight](../TestResults/balance/tower-storage-cleanup-20261001/prepared.json), [per-run deletion log](../TestResults/balance/tower-storage-cleanup-20261001/deleted-runs.jsonl), and [pruning receipt](../TestResults/balance/tower-storage-cleanup-20261001/pruning-completed.json) record authorization, scope, counts, hashes and consequences. Native compression logs and per-campaign verification receipts are retained alongside them. The executor checks resolved workspace paths, rejects links and tracked targets, and requires exact inventory agreement before deletion. Existing execution receipts prevent blindly repeating the cleanup.

Changed files are this review, ignored cleanup scripts/receipts, the explicitly removed generated archives/caches, and NTFS compression attributes on retained archives. No game code/content, application configuration, dependency, database migration or deployment changed. No backend tests or combat simulations were run for this filesystem-only operation. Validation used metadata preflight, full retained-file hashing, protected catalog/metadata checks, target-absence checks and the frontend HTTP check. No required command remains blocked.

## Original review — 12 September 2026

**Status: the recommended first cleanup completed on 12 September 2026 at 14:05 UTC.** Following the user's authorization, 615,826 obsolete files were deleted from this checkout's `TestResults` directory. Measured drive free space increased by **119.6 GiB**, from **71.0 GiB to 190.5 GiB**. Raw evidence for the final Kharad setting and both precision stages, saved teams and historical seed exclusions were retained. The broader cleanup option and older campaigns were not pruned.

GiB are binary units, matching the units Windows Explorer labels “GB.” The original inventory estimated **119.3 GiB** for these deletions. The observed **119.6 GiB** increase is a drive-wide measurement and also reflects filesystem allocation and concurrent activity. The inventory tables below describe storage **before cleanup**; they are retained as the basis for the deletion decision, not a new measurement of remaining files.

**What was saved before cleanup**

| Scope | Logical file sizes | Estimated disk space |
| --- | ---: | ---: |
| All `TestResults` in this checkout | 1,419.4 GiB | **728.5 GiB** |
| Latest `tower-competitive-*` campaign files | 1,050.7 GiB | **359.8 GiB** |
| Other, earlier `TestResults` | 368.7 GiB | **368.7 GiB** |

Compression explains the large difference between logical sizes and disk space. The [performance review](Tower-Competitive-Workload-Performance-Review.md) reported logical archive volume; that volume must not be presented as space recoverable from the drive.

The two saved-team catalogs occupy just **7.6 MiB combined**: [the local catalog](../TestResults/balance/retained-tower-builds.json) is 5,442,055 bytes and [the repository fixture](../LL/tools/BalanceHarness/Fixtures/tower-retained-builds.json) is 2,526,862 bytes. They supply 49 distinct floor-5 recipes across the catalogs, including 32 in the repository fixture. They overlap and must not be added as separate team counts. Keep both: the local file also retains earlier-floor builds and historical schedules.

The complete 2,438-recipe [expanded portfolio](../TestResults/balance/tower-competitive-kharad-refinement-20260912/portfolio.json) is about **55.3 MiB**. Exact team recipes and study metadata are much smaller than per-battle input/report payloads. Keep all relevant seed/exclusion ledgers and definitions as well; the catalogs alone are not a substitute for the complete experimental seed history.

**Largest directories before cleanup**

| Directory under `TestResults/balance` | Estimated disk space | Role |
| --- | ---: | --- |
| `tower-progression-calibration-20260911` | 185.6 GiB | Earlier progression calibration; outside the completed first cleanup. |
| `tower-competitive-kharad-refinement-20260912` | 170.1 GiB | Expanded discovery and final 2,438-recipe confirmation, plus application checks. |
| `tower-competitive-kharad-calibration-20260912` | 86.5 GiB | Rejected first competitive calibration. |
| `tower-retained-build-calibration-20260911-v2` | 63.3 GiB | Earlier retained-build calibration; outside the completed first cleanup. |
| `tower-kharad-followup-calibration-20260911` | 55.1 GiB | Earlier Kharad calibration; outside the completed first cleanup. |
| `tower-competitive-kharad-resolution-20260912` | 41.5 GiB | Final 50,000-fight precision follow-up. |
| `tower-competitive-kharad-20260911` | 22.6 GiB | Initial search comparison and invalid audit. |
| `tower-competitive-kharad-audit-20260912` | 18.5 GiB | Valid replacement quality audit. |
| `tower-competitive-kharad-precision-20260912` | 16.5 GiB | First 20,000-fight precision follow-up. |

The complete inventory is in [sizes.json](../TestResults/balance/tower-storage-audit-20260912/sizes.json). This review does not identify or classify unrelated disk usage outside the checkout's `TestResults` directory.

**Completed first cleanup: 119.6 GiB measured free-space increase**

| Completed raw-run removal | Original disk-space estimate | Retention status |
| --- | ---: | --- |
| `tower-competitive-kharad-20260911/campaign/audit-runs` | **18.4 GiB** | Removed the invalid 70,000-fight audit's raw evidence. Its invalid-status receipt, recipes, definitions and seed history remain. |
| `tower-competitive-kharad-calibration-20260912/runs` | **86.4 GiB** | Removed raw fights from the rejected first calibration. Its failed assessment, observations, full portfolio, definitions, seed ledger, frozen content and producing executable remain. |
| Only the 864 listed `coarse-*` and `fine-*` run directories inside `tower-competitive-kharad-refinement-20260912/runs` | **14.5 GiB** | Removed completed tuning sweeps. The observations, scenarios, protocols, selected setting, and all `confirmation-*` and application/diagnostic runs remain. |
| **Total** | **119.3 GiB** | Actual drive free space increased by **119.6 GiB**, leaving **190.5 GiB available**. |

The [original cleanup inventory](../TestResults/balance/tower-storage-audit-20260912/cleanup-options.json) records the exact two broad directories and 864 screening-run directories, their measured sizes and the consequences. The separate [prepared receipt](../TestResults/balance/tower-storage-audit-20260912/pruning/prepared.json), [per-run deletion log](../TestResults/balance/tower-storage-audit-20260912/pruning/deleted-units.jsonl) and [completion receipt](../TestResults/balance/tower-storage-audit-20260912/pruning/completed.json) record the authorized execution. All 866 targets are absent; they contained 3,394 run directories and 615,826 files. Deletion and post-deletion checks took approximately 222 seconds.

This cleanup kept the final 609,500-fight confirmation archives, both precision follow-ups, valid replacement audit, compressed search archives, local application checks, detailed replays, content/executable snapshots and saved team catalogs. Current final-setting archives remain available for their normal validation. The earlier pruned experiments have intentionally lost full raw replay and end-to-end historical revalidation. Their original reports remain unchanged as historical summaries; the external pruning receipts record the loss. Do not describe those pruned packages as complete, independently reverified archives.

**More aggressive option: not performed**

Before cleanup, individual battle reports, materialized Tower inputs and scorecard JSON across the latest competitive campaigns accounted for about **355.5 GiB**. Removing that entire original category would leave approximately **4.3 GiB of other competitive campaign files**, before choosing representative full archives to retain. The 355.5 GiB figure **includes** the completed first cleanup; it is not additional recovery. The original estimates imply approximately **236.2 GiB** of that category remains after the first cleanup, including final acceptance evidence that was deliberately kept.

The saved teams can still be used for future battles if catalogs, ordered recipes, content/budget context and seed history are retained. However, this option also removes raw final acceptance evidence. Existing `TowerBundle.ReadSaved` verification expects the full inputs, scorecard and every indexed battle report, and replay expects original input/result evidence. Those commands would no longer work for pruned runs. Stored assessments would remain historical summaries of previously verified work, not a replacement for the original independently verifiable evidence.

Before such a broader cleanup, prepare and validate a compact retention package containing exact builds, the full relevant portfolio, seed ledgers, verified per-trial outcome evidence, reports, protocols, producing content/source/executables and selected replay/performance archives. Report which verification capabilities it retains. A future compact archive implementation is separate engineering work; deleting apparently duplicated JSON files does not automatically convert old archives into that format.

The remaining earlier `TestResults` include substantial additional space, but their acceptance/replay dependencies have not been classified for deletion here. Do not include those 368.7 GiB in the first cleanup estimate or remove the entire `TestResults` tree: it contains the local saved-team catalog and historical exclusions.

**Measurement and verification**

- The [read-only audit script](../TestResults/tower-storage-audit-20260912.py) inspected metadata for **4,225,090 files in 252,345 directories**, using logical file lengths and `GetCompressedFileSizeW`. It read no bulk battle payloads and reported zero access errors or skipped links. The scan took approximately 374 seconds; its own output directory was excluded.
- The [cleanup-plan generator](../TestResults/tower-storage-cleanup-plan-20260912.py) aggregates measured directory subtotals, checks targets remain inside the workspace's `TestResults`, and records consequences. It contains no deletion, moving or compression operation.
- Measurements and exact reviewed paths are retained under `TestResults/balance/tower-storage-audit-20260912`. These immutable inventory artifacts describe files before cleanup, not current storage or a second run of the campaigns. The original preparation checks now encounter intentionally missing targets and must not be treated as a repeatable cleanup command.
- The [scoped PowerShell cleanup executor](../TestResults/tower-prune-reviewed-archives-20260912.ps1) checked resolved workspace paths, directory links, tracked-file exclusions, modification times and exact file-count/logical-size agreement with the inventory before deletion. It used only the reviewed target list and recorded each deletion. Its existing receipts prevent an unreviewed repeat execution.
- Post-deletion verification found all **866 targets absent** and **5,146 protected file checksums unchanged**. The protected files include both build catalogs, selected root metadata/portfolios/seed ledgers, the completion manifest, historical reviews, and manifest/result indexes for **2,508 retained normal runs**: 2,438 final confirmation runs, 20 precision runs and 50 resolution runs. This was not a new hash scan of every retained battle payload or a full combat revalidation.
- Both catalogs still supply **49 distinct floor-5 recipe hashes combined**: 44 local, 32 in the repository fixture, with 27 shared. The complete expanded portfolio and historical seed exclusions remain available for future work.
- Changed files for this cleanup are this review and the ignored `TestResults` cleanup executor/receipts, together with the explicitly removed raw archives. No game code, content, configuration, migrations, database or deployment changes. No backend tests or combat simulations were needed or run. Preparation, deletion and post-deletion verification completed without errors.
