# Refinement novelty: verification closure

16 September 2026. Target: offline `LL/tools/BalanceHarness`. This closes the captured-data verification that failed to launch in the previous scope; it does not rebuild, retry or modify that sealed package.

## Frozen diagnostic

Evidence: `TestResults/balance/tower-refinement-novelty-closure-20260916`. One execution, **20 diagnostic seconds / 640 KiB new output**, including freeze, execution, audit and publication. Existing cumulative limits remain **3,600 seconds / 4 GiB**. Previous receipt: `tower-refinement-novelty-v2-20260916/completion.json`; usage 3,339.081469455593 seconds, with 727,276 bytes remaining after its seal. Count every new retained artifact, and all executed phases including failures. No retry, rebuild, test-suite rerun, fight, combat replay, preparation, fresh seed or cap increase.

The candidate is the already tested `BalanceHarness.dll`, SHA-256 `157a1147e5291e42f630a3f4c04898a61df1d22cc20e4d21ef15c58aa144e987`. The previous scope passed 28 tests through `build/run-tests.ps1`; reuse that evidence without claiming new tests. Pin the candidate, source/test evidence, runtime configuration, dependency manifest, captured inputs and gameplay dependencies before launch.

PowerShell 7.6.6 already hosts .NET 10.0.12. Use that existing managed host to invoke the candidate assembly's compiled entry point, with its existing `HarnessJson.UseCompactOutput()` scope. This avoids the absent standalone runtime configuration and reduces trace whitespace; no candidate code or input changes. Assert .NET major version 10, compact serialization active, and the selected assembly path/hash before invocation. Record the PowerShell runtime and loaded assembly identities. Keep the existing combat-start guard inside the compiled diagnostic. Do not execute any PowerShell-generated combat or new diagnostic algorithm.

Exactly four generation checks are contained in the frozen entry point:

1. V1 reconstruction using the 14 saved discovery observations must equal the saved generation hash.
2. V2 synthetic reconstruction must equal `885d726a9264832ff4711668c97c2bfe6fd25398538b28ebdd3bb9f8d45e6934`.
3. One v3 search with the same captured inputs and deterministic synthetic scores.
4. One v3 search with reversed metadata; require the same result hash as the preceding v3 run.

Keep all four complete compact traces and report actual proposal/evaluation counts. The compiled diagnostic enforces at most 16 proposals, canonical order, ordinary validity, distinct evaluated parties, completed same-arm ancestry and bounded distribution construction. A shortfall below 16 teams is an unresolved limitation, not permission for another run or a new strength claim.

Phase bounds: freeze/pin checks 3 seconds; owned-process execution 8 seconds; independent saved-output audit 5 seconds; publication 4 seconds. The audit independently checks party legality, required roles, counts, ancestry and the new distribution construction traces, and hashes the sealed failed study and prior evidence. It also checks the current dirty-file baseline, allowing only the six active Markdown handoffs. On first failure, preserve all evidence, skip dependent phases and publish the limitation. No deletion of sealed evidence or output to recover space.

Publish a new completion review and update six active handoffs. Preserve all 482,866 reservations, including the failed combat comparison's unused 40 values, v19's unused 512 values and all 253 v19 recipes. Keep ability order fixed, boss/content unchanged and adoption Hold. The comparison launcher remains on v1; no real comparison is authorized by this scope.
