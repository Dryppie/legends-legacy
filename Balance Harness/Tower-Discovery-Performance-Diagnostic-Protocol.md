# Frozen discovery performance diagnostic — 14 September 2026

Target: offline `LL/tools/BalanceHarness`. This is a performance/parity repetition, not a balance study. Freeze the machine-readable definition and all input hashes using `TestResults/balance/tower-discovery-performance-20260914-implementation/freeze-diagnostics.ps1` before executing the command below. Never overwrite a frozen definition or retry the measured workload.

## Exact schedule and limits

1. Create neutral archives with the v19 shape: eleven 16-byte-or-smaller files, six directories per candidate (including its root). Grow one owned fixture to **0, 1,024, 4,608, 9,216** baseline candidates, in that order. At each scale run **legacy then owned-storage-v1**, sixteen candidate writes each. Each candidate has the existing inner pre-batch and chunk-completion checks; sample zero additionally has the outer check corresponding to one per 128 starts. Owned sealing also reconciles the final manifest. Retain the sixteen written archives outside the fixture before the next mode, so both start from exactly the same baseline. No timing-driven repetitions. Record every sample, median, nearest-rank p95 (the maximum of sixteen), initialization, final audit, and operation counters. Fixture payloads are neutral text, not valid combat archives; integrity behavior is covered separately by correctness tests and real archives.
2. Under a no-combat guard, reconstruct all **9,216** saved v19 evaluations using the retained generation inputs/mechanics and exact saved measurements. Compare complete generation/recipe/proposal/charge/allocation results, shortlist and screened nominations. Read the sealed package in place; write only new receipts. This is reconstruction, never execution/resume of v19.
3. Reference: use the captured pre-change harness executable with frozen **current-checkout gameplay**, compact archives, `prepared-v1`, one worker, two repetitions. Candidate: use the changed harness with the **identical reference gameplay DLLs and runtime dependencies**, same content/settings, actual bulk campaign and nested outer accountant. The diagnostic cases are saved recipes `team-77416926dedbf2bf6abc7ccb7f782f89`, then `team-1b3d503fe18c185829cf1d18a135ff29`; each uses all eight existing v19 discovery seeds in their original order. Every execution uses the same ordered recipes and schedules. Compare full report digests for both passes; reconstruct the candidate campaign without combat; perform one detailed replay per case in each version.

Exact fights: **2 versions × (2 repetitions × 2 recipes × 8 seeds + 2 detailed replays) = 68**. Zero fresh seeds, candidate-search studies, combat retries, balance feedback or new reservations. Existing bounded publication-rename handling is unchanged; it does not repeat combat.

The measured command has a **1,740-second** cancellation deadline, leaving 60 seconds of the overall **30-minute** workload envelope for command preflight/teardown. Each existing performance invocation retains a **600-second** bound, below its 900-second maximum. New measured output is capped at **3 GiB** with at most **1 GiB** reserved for all build/source/freeze evidence, for **4 GiB total**. Reference and candidate combat packages each have a 256 MiB bound. Correctness tests and compilation are outside diagnostic workload time. A failed stage stops dependent stages and preserves partial evidence; no automatic extension or retry.

## Scope and measurement contract

The reference was built before edits into an isolated artifact directory. Different intermediate paths produced different gameplay DLL hashes in the independent candidate build; the diagnostic execution directory therefore copies **all** reference runtime dependencies byte-for-byte and replaces only BalanceHarness.dll/PDB. The freeze receipt binds both executables, source, copied current content/settings, saved recipe exports and historical inputs. This establishes diagnostic parity and explicitly does **not** establish v19 gameplay equivalence.

The opt-in storage contract treats closed subtrees as immutable between lifecycle audits. Checks include current root metadata, pending artifacts and the active child; new owned outputs are reconciled before sealing. Unknown or modified closed artifacts, extra directories and links are rejected at mandatory full inventory/hash audits before valid completion. External edits to closed subtrees can therefore be detected later than in legacy mode. Legacy defaults and verifiers remain unchanged. Owned execution is non-resumable; durable bytes can be reconstructed without granting execution authority.

Exclusive timings are used for breakdowns; nested inclusive values are never summed. No random value is consumed for profiling. OS cache and competing load are uncontrolled: the first scale is fresh-process, later measurements are warm-process; no OS-cache flush is attempted. Persist parent CPU, allocations and peak memory, child benchmark metrics, timestamps, file counts and bytes. The legacy fixture scanner is the original algorithm with visit counters; the actual combat reference uses the pre-change assembly. Do not interpret a bookkeeping ratio as a historical percentage or a whole-run speedup.

## Commands

From the repository root, after the documented isolated reference/candidate builds and correctness tests:

```powershell
./TestResults/balance/tower-discovery-performance-20260914-implementation/freeze-diagnostics.ps1
dotnet TestResults/balance/tower-discovery-performance-20260914-implementation/execution/BalanceHarness.dll tower-discovery-performance --definition TestResults/balance/tower-discovery-performance-20260914-implementation/diagnostic-definition.json --output TestResults/balance/tower-discovery-performance-20260914-implementation/measured
```

The exact machine-readable definition, freeze receipt and output directory are execute-once evidence. Future repetitions require a separate explicit protocol and output path; these commands must not overwrite this run.

V19 retains all **253** recipes, zero confirmation, reliability **Unresolved**, adoption **Hold**, ordinary/joint assessment **NotRun / NotRun**. Preserve all **480,707** reservations, including the **512 unused confirmation values**. No confirmation preparation, Kharad/content edit, deployment, migration or old-cap increase is authorized by this diagnostic.
