# Whole-loadout placement resource readiness

**Status: prequalification is not ready; runtime qualification and scientific admission have not started.** The implementation remains verified, but its historical cost fixtures are not comparable. A resource forecast must not silently treat a different fixture workload as the proposer's overhead.

## What the retained receipts show

The [sealed precheck](../TestResults/loadout-placement-resource-precheck-20260924-v2/assessment.json) authenticates the implementation handoff and 212 inherited pins. It follows the older audit probe's manifest pin through the authenticated admission forecast. It reads saved costs and context only; it does not sample timing, prepare native combat, allocate entropy, reserve values or run a campaign.

| Fixture property | Previous v5 comparison | New placement comparison |
| --- | ---: | ---: |
| Actors | 5 | 10 |
| Essence slots per actor | 4 | 5 |
| Completed literal requests | 17,280 | 18,816 |
| Native phase seconds | 176.468 | 514.766 |
| Native phase bytes | 604,468,859 | 1,128,844,717 |
| Audit phase seconds before final closeout | 39.344 | 66.140 |

Actor templates, reference recipes and the physical context differ. The test evaluator also deliberately changed for the placement fixture: references draw during training/nomination, while generated teams win, to force useful pass/fallback and differing-output coverage. This is appropriate functional coverage, but it is not a matched cost experiment. See the retained fixture sources in the verification package. Functional test success does not resolve this qualification gap.

Applying the existing upward-only formula to the unmatched raw cost ratios gives the following **sensitivity calculation only**. It uses the completed 16,256-fight allied-action pilot, scales to 21,888, applies margin two, preserves inherited floors, scales the frozen audit probe after storage growth, and retains the 120-second publication reserve.

| Partition | Unmatched sensitivity | Frozen cap |
| --- | ---: | ---: |
| Native time | 6,694 seconds | 9,000 seconds |
| Audit/publication time | 2,772 seconds | 1,800 seconds |
| Native storage | 9,543,428,837 bytes | 5,905,580,032 bytes |
| Audit/publication storage | 34,989,702 bytes | 536,870,912 bytes |

These values explain why a direct substitution would reject admission. They do **not** establish the actual placement overhead or prove that a properly qualified implementation cannot fit. The machine-readable assessment explicitly sets `qualifiedCurrentForecast` to null and `usableForAdmission` to false. Do not use this calculation to justify enlarging the envelope.

## Concrete next step

The [prospective matched-fixture plan](Tower-Loadout-Placement-Matched-Fixture-Plan.json) fixes the repair before new cost measurements. Both fixtures must use the same ten actors, five slots, reference/context/content/settings, saved synthetic values, literal evaluator rules and current runtime. The baseline retains the existing v4-versus-v5 study; the candidate retains the frozen v5-versus-v6 study. Their shared v5 trajectory must agree after removing only version-specific evidence bindings.

Record all phase costs and request counts, including held-out deduplication, catalogues, source/runtime retention, both audits and publication. Use the declared raw phase-ratio rule without selecting a normalization after observing costs. Retain the earlier placement cost observations as upward-only floors for new placement measurements; do not discard the slower measurement or silently overwrite either existing fixture. Incomplete fixtures remain failed evidence.

After those engineering checks, execute the separately declared 900-second / 1-GiB guarded qualification from the frozen design. That still requires producing assembly/PDB/source correspondence, unchanged gameplay dependencies/content/inventory, all 238 placements, three references, saved controls and legacy coverage. Reconcile live history and apply all inherited floors and resource partitions before publishing an admission. No prospective roots may be screened. Neither this precheck nor the new fixture plan authorizes a scientific launch.

## Verification, changes and accounting

Added the pure [precheck/verifier](analysis/loadout-placement-resource-precheck.py), its [20-test suite](analysis/test-loadout-placement-resource-precheck.py), this report and the prospective fixture plan. Tests cover physical/context mismatches, immutable margins and caps, inherited floors, publication accounting, invalid costs, incomplete/retried receipts, duplicate JSON keys, retained-only replay, altered assessments and the probe's transitive pin. All 20 passed, and replay using only the retained evidence passed. No backend implementation changed; the existing 380-backend-test, 75-Python-test and owned-fixture receipts remain authenticated.

The first publication attempt stopped because the helper expected a direct audit-probe pin in the latest handoff. That older pin is instead carried by the authenticated admission forecast. The corrected helper follows that chain without weakening authentication. The [failed package](../TestResults/loadout-placement-resource-precheck-20260924/files.json) remains sealed. The second package completed in 0.281 seconds before sealing, retaining 8,895,482 bytes at that point. This was a technical evidence-packaging correction, not a timing resample or qualification retry.

Both bounded precheck attempts charge their full 180 seconds / 67,108,864 bytes: 360 seconds / 134,217,728 bytes in total. Cumulative recorded charges are **54,379.661 seconds / 38,760,464,702 bytes**; cumulative declared maxima are **143,280 seconds / 93,902,077,952 bytes**. The 900-second qualification and scientific execution allowances remain unstarted. No history rescan is claimed.

```text
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-resource-precheck.py" -v
python -B -X utf8 "Balance Harness/analysis/loadout-placement-resource-precheck.py" create --output "TestResults/loadout-placement-resource-precheck-20260924-v2"
python -B -X utf8 "TestResults/loadout-placement-resource-precheck-20260924-v2/helper.py" verify --output "TestResults/loadout-placement-resource-precheck-20260924-v2"
```

Nine current-status documents change only line three; their historical bodies are preserved. New LF rules protect the helper, tests and prospective JSON plan. No migration, application configuration change, gameplay-default change or deployment is involved. Qualification was deliberately not run with incomparable evidence; no required precheck command remains blocked.
