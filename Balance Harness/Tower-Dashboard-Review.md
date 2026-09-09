# Local Tower dashboard — 9 September 2026

The offline harness now has a browser button for generating the declared character profiles and running Tower benchmarks. `tower-dashboard` serves an embedded UI on IPv4 loopback. It calls the existing benchmark, verified comparison and replay methods, so production preparation, RequiredSlots, deterministic schedules and outcome interpretation remain shared with the command workflow.

## Workflow and scope

Select catalog/floors/parties, preview equipment and Essences, choose samples/seed and optionally a saved reference, then run. The page shows progress/cancel, verified results, candidate-minus-reference deltas, Markdown/JSON downloads and detailed replay with event filtering and pagination. Reference selection restores the saved schedule. Every operation writes a fresh folder; source evidence stays read-only. Cancellation saves partial evidence and does not claim a complete measurement.

Profiles rematerialize from current equipment/Essence balance content. Intended recipe/progression changes still require catalog edits. The current catalog remains the five declared profiles, two party cells and floors 1/3/5 from the [benchmark review](Tower-Benchmark-Review.md). Win rates remain descriptive; no starter target, Tower target or baseline approval is inferred.

## Verification

Commands ran through the required backend entry point:

```powershell
./build/run-tests.ps1
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTowerDashboardTests'
```

The full suite passed **2,238 tests**, with no failures/skips and five existing compiler/analyzer warnings. The four dashboard integration tests passed again after the final UI adjustment. They use real HTTP against a temporary loopback server, covering setup/assets/initial empty job, benchmark/reference comparison, legal ten-character replay and report download, output preservation, invalid selections, session/origin/host rejection, single-worker admission, cancellation with saved completed trials, and tampered-evidence rejection. Existing Tower parity and benchmark tests ran as part of the full suite. No verification command remains blocked.

Browser checks used the local in-app browser:

- Four complete six-battle measurements (floors 1/3/5, starter/mixed, one sample, seed 1337). Both saved comparisons had six pairs and **zero changed gameplay records**, including the final UI build compared with the initial reference. Assembly differences are disclosed by the comparison.
- Two successful detailed `floor-5.mixed/tower.0001` replays, including the final build: seed **-1981892632**, ten participants, Defeat, **74.40 seconds**, **85.40%** guardian health, **2,291 events**. Filtering found 18 barrier-break events and 898 damage events; paging advanced from 1–100 to 101–200.
- Cancellation preserved **500/600** planned trials and labeled the benchmark incomplete. These partial samples are cancellation evidence, not an accepted balance measurement.
- Desktop layout inspected, no browser console errors on the final build. Initial browser testing exposed an empty JSON response for no active job; this was fixed and added to the HTTP regression test. Replay refresh now preserves the selected fight, and the log identifies its scenario/seed. Mobile breakpoints were not separately exercised.

All complete smoke cells had 0/1 wins. This tiny repeated schedule checks the workflow and repeatability; it does not add independent balance confidence or certify Tower difficulty.

## Retained evidence

Local root: `TestResults/balance/tower-dashboard-20260909/`.

| Path under root | Purpose |
| --- | --- |
| `browser-runs/dashboard-20260909-091511-3cae76fccc6143229bcc8cf40e9f38e0/run` | Initial six-battle reference |
| `browser-runs/dashboard-20260909-091754-b25620c81f52408392e822af374da8f9/run` | Same-build repeat and verified comparison |
| `browser-runs/dashboard-20260909-091952-4c4c0f36591c4d55b9e7a58d56f12f7a/run` | Cancelled 500/600 benchmark |
| `browser-runs/dashboard-20260909-092039-38a4843c7d1e46659a6f65790dc44ec4/run` | Final-build six-battle run |
| `browser-runs/dashboard-20260909-092052-26cedc2284794c42be74580feb1aa2af/replay.json` | Final-build verified ten-character replay |
| `browser-runs/dashboard-20260909-092205-f491caa4138f4d7bba3b6e3a777928f7/run` | Final-build comparison with initial reference |
| `executable-verified/`, `executable-final/` | Matching retained builds before/after final UI adjustment |
| `full-tests.log`, `dashboard-tests.log`, `dashboard-tests.trx` | Full-suite log and final focused test evidence |
| `verification.json`, `checksums.json` | Operation/measurement summary and file hashes |

The earlier `executable/` is an initial UI build; no browser measurements use it. Existing first-slice/benchmark and accepted starter evidence were not overwritten or repackaged. Full-suite TRX was overwritten by the runner's final focused test execution; the full-suite output remains in `full-tests.log`.

To reopen these results with the final retained build:

```powershell
dotnet TestResults/balance/tower-dashboard-20260909/executable-final/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

## Files and design decisions

`TowerDashboardService.cs` owns selection validation, bounded saved-run discovery and one cancellable operation. `TowerDashboardServer.cs` hosts explicit routes with loopback/session/origin checks, verified evidence reads and fixed download formats. `Dashboard/index.html`, `dashboard.css` and `dashboard.js` provide the embedded UI without a frontend dependency toolchain. `Program.cs` adds the command; `BalanceHarness.csproj` adds the ASP.NET Core shared framework and embedded assets, removing the redundant configuration package reference. `BalanceHarnessTowerDashboardTests.cs` covers the HTTP boundary. The README and implementation plan describe usage, results and remaining work.

## Remaining limits and implications

The harness now requires the **ASP.NET Core 10 shared runtime**. No production API, authentication system, migration, shared database, deployment or production configuration was changed. This is local tooling, not a hosted multi-user dashboard. Code changes require rebuild/restart. New content is captured per run; exact historical replay still needs the retained executable/runtime/platform.

Discovery stops at two levels/2,000 directories under the configured results root; choose a nearer `--runs-root` for deeper archives. A comparison's reference must be available there to recompute it in the page. Only the current replay has a page download endpoint; prior JSON remains on disk. Catalog validation can hide invalid catalog files; use the CLI for detailed validation diagnostics. There is no queue, automatic resume, visual recipe editor, optimizer, scouting coverage, acquisition simulation or full Tower journey. Phase 2 integration remains deferred. Approved Tower audiences/targets and broader adapters remain future increments.
