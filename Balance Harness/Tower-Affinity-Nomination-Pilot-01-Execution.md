# Affinity nomination comparison — completed 25 September 2026

Keep original affinity creation with benchmark validation as the supported engineering baseline. The single nomination experiment completed and did not earn advancement. This tuning cycle is closed.

The experimental rule allowed only generated finalists to challenge the benchmark. It changed the nominated challenger on seven of twelve roots: control nominated five generated teams; the experiment nominated twelve. No challenger in either arm passed the unchanged 60-pair validation gate. Both arms therefore retained the exact benchmark on every root.

| Audited result | Value |
| --- | ---: |
| Paired roots | 12 |
| Complete searches | 24 |
| Actual fights | 15,744 |
| Search fights | 12,672 |
| Held-out fights | 3,072 |
| Different final outputs | 0 roots |
| Novel outputs passing validation | 0 |
| Candidate minus supported baseline | 0.00 percentage points |
| Candidate minus benchmark | 0.00 percentage points |
| Frozen decision | `NoObservedOutputDifferentiation` |

Identical outputs shared one physical held-out evaluation per root, as declared. The benchmark won 2,306 of those 3,072 fights (75.07%). The zero method contrasts apply to these twelve roots, where the selected teams were identical; this does not establish equivalence on future roots or other encounters.

## Decision and next work

The [prospective rule](Tower-Affinity-Search-Consolidation.md) required at least +2 points against both control and benchmark, at least three differing roots, and three promising novel outputs. None of those advancement conditions was met. There will be no replacement roots, retuning or follow-up variant in this cycle. Experimental racing v9 remains explicit and is not the supported profile.

Use the [supported profile](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) and preserve benchmark fallback. Next prioritize practical operation, runtime and representative encounter coverage. Any future algorithm experiment needs a new substantive reason and its own fixed design.

## Verification and provenance

One scientific attempt completed, with zero retries. Native reconstruction, independent Python audit, publication checks and final read-only verification all passed. Final verification took 124.187 seconds and finished within the original audit allowance. No default promotion, gameplay change, database migration or deployment occurred.

- Archive: `TestResults/balance/tower-affinity-nomination-pilot-01-20260925`.
- Closeout SHA-256: `1ce005504df5418d1bf2eb908c373d15890d6b64b6c8ec42eab11513ab0625f5`.
- Archive manifest SHA-256: `a085d004823246466ec860876fcfdb370769cc76a71527a09c2b4a32dec79ee1`.
- Final verification: `TestResults/affinity-nomination-pilot-verification-20260925`, manifest SHA-256 `04e14502aa124e386243ecaacbeb7e13227bed5de0d45301368a5caed7e0e763`.
- Execution declaration SHA-256: `9a7e3a64d63ad6f3034b34d817f9d1103eaf5053cf5ca1cf3a9916270866d228`.
- The admitted history contained 815,560 values across 266 files. This attempt assigned 4,380 fresh values and permanently reserved all 16,379 fresh values exposed by its one entropy batch, bringing the excluded total to 831,939.

The owner recorded 995.281 seconds and 1,898,851,980 retained bytes. The existing 10,800-second / 6-GiB ceiling was unchanged. The preceding [implementation and engineering checks](Tower-Affinity-Search-Implementation.md) include 96 passing backend tests, 31 passing Python tests and the full synthetic maximum workload. Both test-wrapper failures remain recorded separately; neither used scientific entropy. Their declared charges were retained.
