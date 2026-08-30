# Encounter Scale-Probe Performance Baseline v1

## Status

This is an opt-in developer-workstation diagnostic baseline for balance schema 23 and encounter scale-probe algorithm v2. It is not a release-certification policy, server capacity promise, or production gameplay requirement.

## Frozen workload

The workload uses seed `8471`, Release configuration, all ten Region 1 floors, and balanced P75 parties at 5, 10, and 15 players. Each floor/size batch contains three deterministic rosters with five production-combat trials per roster.

That produces:

- 30 floor/size batches;
- 15 trials per batch;
- 450 newly executed scale-probe trials;
- authored and hypothetical sizes measured through the same temporary-definition production-combat path;
- deliberately reduced non-probe search/validation settings so the measurement focuses on encounter execution.

Earlier pipeline stages execute before the probes and provide practical runtime warm-up. Authored evidence is rerun for this baseline instead of reused because the party-family evaluator uses one trial per roster while the scale probe uses five.

## Reference host

| Field | Value |
| --- | --- |
| Framework/runtime | .NET 10.0.11 |
| Operating system | Microsoft Windows 10.0.26200 |
| Process architecture | X64 |
| Logical processors | 32 |
| Server GC | False |
| Stopwatch frequency | 10,000,000 |

The artifact records this context on every run. Results from materially different hosts should establish their own evidence before adopting these thresholds.

## Calibration panel

Three identical unbudgeted runs established the initial variance panel:

| Run | Wall Time | Allocated | Aggregate Throughput | Process Peak | Managed High-Water Estimate |
| --- | ---: | ---: | ---: | ---: | ---: |
| `20260829T123236818Z-3d613324` | 1,382.96 ms | 1,204.12 MiB | 117,255 ticks/s | 115.68 MiB | 32.53 MiB |
| `20260829T123345482Z-f0fd20ac` | 1,388.73 ms | 1,204.13 MiB | 116,768 ticks/s | 116.38 MiB | 32.66 MiB |
| `20260829T123418778Z-24c57c6c` | 1,428.34 ms | 1,200.61 MiB | 113,530 ticks/s | 113.48 MiB | 32.83 MiB |

Total wall time varied by 3.3%, total allocations by 0.3%, and aggregate throughput by 3.3% across the panel.

| Players | Batches | Mean / Maximum ms per Trial | Mean / Maximum MiB per Trial | Mean / Minimum Batch Throughput |
| ---: | ---: | ---: | ---: | ---: |
| 5 | 30 | 1.13 / 1.61 | 1.10 / 1.40 | 266,837 / 206,695 ticks/s |
| 10 | 30 | 3.00 / 5.33 | 2.61 / 3.83 | 129,059 / 88,007 ticks/s |
| 15 | 30 | 5.20 / 10.85 | 4.31 / 7.43 | 77,375 / 43,036 ticks/s |

## Workstation budget v1

The following opt-in thresholds preserve headroom above the observed worst batches without pretending to be universal hardware-independent limits:

| Metric | Threshold |
| --- | ---: |
| Maximum wall time per trial | 15 ms |
| Maximum allocated memory per trial | 10 MiB |
| Minimum batch throughput | 30,000 simulated ticks/s |
| Maximum process peak working set | 192 MiB |

A fourth identical run with these thresholds completed in 1,439.40 ms, allocated 1,204.13 MiB, sustained 112,657 aggregate ticks/s, reached a 115.11 MiB process peak, and reported `WithinBudget` for every measured batch and the suite overall.

Allocation is measured on the synchronous combat thread. Process peak working set is the operating-system high-water mark for the entire balance process, not isolated ownership by one encounter. Managed high-water is the larger heap observation immediately before or after each batch. These limitations are retained in the generated report.

## Reproduction

From the repository root:

```powershell
.\build\run-scale-probe-performance-baseline.ps1
```

Use `-Repetitions 3` to gather another variance panel. The latest and immutable history artifacts are written beneath `balance-output/schema23-performance-baseline`; generated outputs remain ignored by Git.

Budget violations diagnose performance regressions but never change party-family, encounter, or release-certification verdicts.
