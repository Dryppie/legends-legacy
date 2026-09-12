# Tower archive storage and cleanup review — 12 September 2026

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
