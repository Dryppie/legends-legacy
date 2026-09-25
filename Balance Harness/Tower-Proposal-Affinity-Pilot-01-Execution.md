# Proposal-affinity pilot 01: failure before allocation

**Subsequent status — 23 September 2026:** Separately declared [pilot 02 completed and passed all audits](Tower-Proposal-Affinity-Pilot-02-Execution.md), reaching `AbandonThisConfiguration`. Its runtime retained all 27 admitted files, including the producing PDB. Every consumed admission and earlier failure described below remains closed and fully charged.

23 September 2026. Target: the offline Balance Harness.

**The single admitted launch failed before entropy allocation or combat.** It produced no search or efficacy result. The worker exited with code 2 after **26.953 seconds**, without timeout or active descendants. The failed study is retained at `TestResults/balance/tower-proposal-affinity-pilot-01-20260923`; it has not been retried or resumed.

The [failure receipt](../TestResults/balance/tower-proposal-affinity-pilot-01-20260923/failure.json) reports `Changed study artifact: source/runtime.json`. Admission bound 27 runtime files, including `BalanceHarness.pdb`. The shared executable copier retained the 26 runnable dependency files and omitted the producing symbols. Every copied file matched its admitted hash, but the resulting inventory failed exact comparison. The admitted runtime itself was intact.

This failure happened before the call to `Reserve`. There is no entropy intent, entropy file, allocation, pending reservation, seed ledger, attempt journal, search archive or result. The [read-only closeout](../TestResults/proposal-affinity-pilot-01-failure-closeout-20260923/verification.json) independently rescanned the complete registry under the allocation lease and confirmed **672,220 historical values across the same 246 files**. No recovery reservation is needed. The outcome says nothing about the effectiveness of the candidate proposal policy.

The closeout's external manifest SHA-256 is **`c095d252617a9ed1a85db096b586aa0d1a532ff4acb59c5d928def480d8e586f`**. Its [failed-study inventory](../TestResults/proposal-affinity-pilot-01-failure-closeout-20260923/failed-study-files.json) has SHA-256 `f2044e42689039d8f6f52c66457e892ecef10a0199e03af519265d16b93fa13a`; the failure receipt hash is `5762e4f8a3b683ebef75d2596092146d53aabe5db7ed795c784a0e73b741691a`. The admitted request hash remains `afed0ef3ad02f999cccecd195f806965d05a96994063071bc63c2feac68ec33c`. All preceding admission packages and scientific archives remain unchanged.

The failed study retains its full declared **10,800-second /6-GiB** charge, without refund or reuse. The failure closeout had a separate **120-second /64-MiB** allowance, used 53.735 seconds and retained 66,150 bytes. Including the preceding recorded admission/probe/review allowances, the cumulative charged envelope is **15,360 seconds /11,140,071,424 bytes**. This is declared accounting, not a claim that the failed worker consumed those resources; development builds and literal tests remain separate engineering work. The failed study physically retained **44,134,784 bytes**.

The repair is local to proposal-study retention. `TowerProposalStudyProtocol.RetainRuntime` first authenticates the admitted inventory, retains the executable dependency closure, rejects any required asset omitted by admission, then copies additional pinned runtime files—including symbols—within the same remaining byte allowance. It verifies the exact final inventory before returning. `TowerProposalStudyRun` uses this helper before reservation. The shared copier and other study protocols are unchanged.

The owned process fixture now includes `BalanceHarness.pdb` in its admitted runtime, matching the production admission that exposed the bug. The previous fixture used only the shared copier's output when constructing its manifest, so its expected and actual inventories both omitted the PDB and could not detect this failure.

**All 34 targeted backend tests pass.** Six new retention cases cover inventories with and without symbols, aggregate byte limits, altered symbols, omitted dependency metadata, escaping paths, cancellation and refusal to reuse an existing runtime directory. The existing study and resource-envelope tests also pass. The [backend log](../TestResults/proposal-runtime-retention-backend-approved-20260923.log) and [TRX](../TestResults/proposal-runtime-retention-backend-20260923.trx) are retained.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessProposalRuntimeRetentionTests|FullyQualifiedName~BalanceHarnessProposalResourceTests|FullyQualifiedName~BalanceHarnessProposalStudyTests' -ArtifactsPath 'TestResults/proposal-runtime-retention-build-20260923'
```

The sandbox initially denied access to the existing NuGet configuration; the approved wrapper rerun passed. Existing unrelated build warnings remain. The real scientific launch failed as described above, so its native audit, independent audit and publication were never reached.

Both updated owned fixtures also pass. The [complete fixture](../TestResults/tower-proposal-owned-fixture-runtime-retention-20260923/verification.json) retained the admitted PDB and completed all 18,816 literal reports, native reconstruction, independent Python audit, publication and final native verification in 264.079 seconds. Its closeout pin is `a3bd2326a780cbdec85be633e403b93e896dee9654c88b31f6bd5c0a000410a8`; its archive manifest pin is `e6471ed58f3e7a57130877ee648d0c256f810d15180994b6c8b694fdcaea7926`. The [intentional failure fixture](../TestResults/tower-proposal-owned-fixture-runtime-retention-failure-20260923/verification.json) passed the runtime-copy boundary, retained one charged literal attempt and all 16,384 exposed literal values, and correctly refused publication. Neither fixture ran real combat or production entropy. The [engineering handoff](../TestResults/proposal-runtime-retention-handoff-20260923.json) binds the repaired sources, test TRX, fixture pins and unchanged scientific failure.

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 build/test-proposal-affinity-study-owned.py --fixture-host TestResults/proposal-runtime-retention-build-20260923/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll --source-fixture TestResults/proposal-study-fixture-final-20260923 --output TestResults/tower-proposal-owned-fixture-runtime-retention-20260923 --resource-envelope tower-proposal-resource-envelope-v2
```

The failure fixture used the same command with its separate `tower-proposal-owned-fixture-runtime-retention-failure-20260923` output and `--mode attempt-failure`. All verification commands for the repair completed successfully; no repair check remains blocked. These fixtures do not qualify the rebuilt harness against the captured gameplay dependencies or authorize reuse of the consumed scientific admission.

Changed implementation files are `TowerProposalStudyProtocol.cs`, `TowerProposalStudyRun.cs`, `ProposalStudyFixtureHost.cs` and the new `BalanceHarnessProposalRuntimeRetentionTests.cs`, plus this execution report and current-status links. No gameplay defaults, scientific decision thresholds, migrations, application configuration or deployment changed.

The repaired test-build harness SHA-256 is `aeec5f125fdccc0a682a93fb053ba90e19f5ec09627c60101a5983264e1e272f`. This build still needs qualification against the captured gameplay runtime. A subsequent scientific attempt requires a separately declared study and admission that preserve this failed launch and carry its charges; the consumed admission and existing output cannot be reused.
