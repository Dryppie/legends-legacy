# Placement comparison: one final inventory and a qualification path

Current status: one redundant protected-input hash pass is removed. A source-bound, plain-format resource amendment passes its necessary planning check at **1,764 audit/publication seconds against the unchanged 1,800-second cap**. Current-runtime qualification, scientific admission and placement efficacy remain unestablished. Historical assessments and compressed launch guards remain unchanged.

The target is the offline Balance Harness. The immediate objective is evidence about whether whole-loadout placement finds stronger teams. This stage addresses a specific repeated operation in the resource blocker; it does not continue the general cleanup backlog or change candidate generation, selection, fight budgets or decision thresholds.

Previously, `run-proposal-affinity-study.py` hashed all protected output at the native/audit boundary, rehashed those inputs after the two audits and publication worker, then hashed them again while constructing the final manifest. The new `publication_inventory` builds that manifest from fresh hashes and verifies its protected subset against the initial hashes. Each protected file receives two owner hash passes over that lifecycle, instead of three. New audit outputs are also hashed normally. No earlier digest substitutes for reading current contents.

Verification now occurs after copying the provisional result and before publishing completion, manifest and closeout receipts. It rejects missing, renamed or altered protected files, including changes introduced during the result copy. Both audits and the publication worker must still succeed first. Failure can leave an unsealed `result.json`, as other pre-sealing failures already could; it cannot publish a successful completion or closeout. Existing exclusive ownership and external-mutation limitations remain: this is not an atomic filesystem snapshot or protection against arbitrary mutation after hashing.

The exact application-read fixture compares the previous and new finalization algorithms over the same bytes. Both produce identical manifests. The previous pair of operations reads **2,097,203 bytes**; the combined operation reads **1,048,611 bytes**, saving exactly the **1,048,592 protected bytes** once. This is a work-count result for a small synthetic fixture, not measured elapsed savings, physical-device I/O or a whole-study speedup. Separate full routed-owner checks verify two protected-file digest calls in both default and accounted modes.

The [prospective amendment](Tower-Loadout-Placement-Publication-Inventory-Amendment.json) follows the historical probe's actual formula. Its retained `owner.py` explicitly includes one inventory inside the whole-worker cost and projects **two extra owner inventory passes**. With the verified implementation change, the new plain-format model retains the whole worker and projects **one extra pass**. It preserves the historical per-pass coefficient and storage scaling. It does not multiply elapsed time by a compression ratio, discount either independent audit, erase a failed attempt or increase a limit.

The amendment is implemented as pure arithmetic in [the assessment helper](analysis/loadout-placement-publication-inventory.py). It rejects changed limits, arbitrary pass reductions, nonfinite costs, unreconciled historical floors, missing audit terms and invalid byte-equivalence proof. The registration binds the current launcher and tests, historical probe source and receipts, and current read-count fixture.

| Planning component | Historical model | New plain-format amendment |
| --- | ---: | ---: |
| Whole worker and independent audit terms, including margin | 445.000 seconds | 445.000 seconds |
| Extra inventory terms, including margin | 1,240.081 seconds | 620.040 seconds |
| Publication reserve | 120 seconds | 120 seconds |
| Reconstructed probe term | 1,805.081 seconds | 1,185.040 seconds |
| Necessary audit floor after retaining other floors and rounding | **1,806 seconds** | **1,764 seconds** |

The inherited 1,764-second audit floor becomes the binding term. Native time remains 8,020/9,000 seconds; native retained storage remains 5,347,633,246/5,905,580,032 bytes; audit retained storage remains 34,989,702/536,870,912 bytes. The original 1,806-second recovery assessment remains immutable and still fails under its original implementation/model. Its native denominator ceilings, original placement measurements and all historical charges are preserved.

This new necessary check permits **qualification registration**, not execution or admission. It supplies no measured current-runtime forecast, guaranteed upper bound or evidence of search improvement. Changed instrumentation and enclosing work still need reconciliation in that finite qualification. The amendment applies to plain JSON; both compressed preparation/launch guards remain closed. It must be rejected or superseded if an extra equivalent pass is restored or its source bindings change.

Verification passes **933 Python tests in 41 groups**: 15 new integrity/read-work tests, 11 new amendment/owner-pass tests and all 907 current regressions. The worker-log suite had one `TemporaryDirectory` teardown failure with `WinError 32`; the unchanged 28-test group passed on one retry. The failed log is retained. An initial new smoke test incorrectly expected the launch API to return a result instead of printing JSON; its assertions were corrected before final verification. No tests were skipped.

```text
python -B -X utf8 TestResults/loadout-placement-publication-inventory-verification-20260925/run-tests.py
python -B -X utf8 TestResults/loadout-placement-publication-inventory-verification-20260925/resume-tests.py
python -B -X utf8 TestResults/loadout-placement-publication-inventory-verification-20260925/finalize.py --check-only
git diff --check -- <changed files>
```

The [verification package](../TestResults/loadout-placement-publication-inventory-verification-20260925) retains 54 current regression process observations and two regression abrupt-owner-exit proofs. The new tests use intercepted literal worker commands; they add no real scientific process or timing experiment. [Resource specification v28](Tower-Loadout-Placement-Resource-Boundaries-v28.json) contains 25 domains and 197 source references; [inventory v30](Tower-Loadout-Placement-Publication-Inventory-Boundaries.json) contains 42 families and 330 references. Source hashes, exact anchor lines, fixture manifests and historical pins are verified.

The 18 changed source/document files are the launcher, pure assessment helper, two new test drivers, amendment JSON, this report, resource specification/inventory, LF attributes and nine historical status lines. Only line three changes in those nine documents. Accounting modules, process helper, native code, gameplay and prior sealed packages remain unchanged. Future admissions must capture the updated launcher. No migrations, application configuration changes or deployments are required. Backend tests were not rerun; 376 historical passes and their captured runtimes remain authenticated. No required verification command remains blocked.

No combat, native encounter preparation, live-history scan, entropy draw, scientific reservation, qualification or new timing measurement occurred. Cumulative recorded charges remain 79,339.66095319996 seconds and 51,980,910,910 bytes; declared maxima remain 168,240 seconds and 107,122,524,160 bytes. The [new handoff](../TestResults/loadout-placement-publication-inventory-handoff-20260925.json) preserves the old assessments alongside the new source-bound necessary check.

Next prepare a single prospective plain-format current-runtime qualification registration for the unchanged twelve-root comparison, using the existing 900-second/1-GiB qualification ceilings. Bind the producing runtime/PDB/source, unchanged gameplay/content, all placements/references/controls, maximum logical workload and complete audits/publication; preserve the failed pair and retained cost constraints. Reconcile actual current costs before scientific admission. General cleanup completeness is not the default next milestone. The intended subsequent outcome is a valid placement comparison, not an automatic policy promotion.
