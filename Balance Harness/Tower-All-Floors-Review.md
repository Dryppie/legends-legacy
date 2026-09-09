# All released Tower floors — 9 September 2026

The default catalog and dashboard now cover **every currently released Tower floor, 1–15**. All floors are selected on page load. The existing starter and mixed party cells repeat to fill the production floor's RequiredSlots with distinct participants and correct PartyNumber assignment. Profiles, combat code, seed derivation and balance targets are unchanged.

| Floors | Participants per fight |
| --- | --- |
| 1–4, 6–7 | 5 |
| 5, 8–9, 11–14 | 10 |
| 10, 15 | 15 |

The default schedule is 15 floors × two parties × 20 trials = **600 battles**, master seed 1337. The dashboard still permits subsets. Selecting an older saved reference restores that reference's original floor selection; reload/select the catalog for the complete current floor set. Historical artifacts retain their original scope.

## Verification and measured results

`./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'` passed **49 tests**, zero failures/skips, with five existing compiler/analyzer warnings. The full backend suite was not rerun for this catalog/test-only change; the preceding dashboard increment passed 2,238 tests. No command remains blocked.

Coverage now includes independent persisted normal Tower preparation/playback/outcome parity for the mixed party on every floor, detailed/compact equivalence, full slot and PartyNumber mapping, all-floor repeat/comparison integrity and HTTP execution with a 15-character floor-15 replay. The catalog test compares its floor list with the production released-floor provider so a future release cannot silently go untested.

Two browser-driven full runs completed **600/600 valid trials each**, with **600 paired comparisons and zero changed gameplay or evidence records**. Each of the 30 cells recorded 0 wins / 20 defeats, no draws, with descriptive clear rate 0% and 95% Wilson interval 0–16.11%. The repeated schedule establishes repeatability, not another independent sample set. Every first mixed-party trial was selected for detailed replay, covering all 15 guardians. The dashboard also verified floor 15 directly and showed 30 result/comparison rows.

These remain fixed level-10 starter and level-20 mixed builds, including on the highest floors. Defeats are descriptive observations, not a Tower balance failure or grounds for automatically weakening bosses. No starter 50–90% policy applies.

## Evidence and commands

The updated dashboard remains at `http://127.0.0.1:5093/`. New measurements are saved alongside its existing history, without rewriting previous runs:

- Reference: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-101111-f22d151040ef402992e58fe58567da30/run/`.
- Repeat and comparison: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-101218-8c0ba114a6534d5ba89402881d3b5ba6/run/`.
- Retained build, test log/TRX, 15 replay JSON/log pairs, verification summary and checksums: `TestResults/balance/tower-all-floors-20260909/`.

Both bundles contain frozen recipes/content/execution identity, individual battle results and Markdown/JSON reports. Reopen the dashboard with the retained build:

```powershell
dotnet TestResults/balance/tower-all-floors-20260909/executable/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

For a fresh CLI measurement, the existing command now defaults to every floor:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --output TestResults/balance/tower-all-floors-new
```

## Changed files and remaining limits

`Fixtures/tower-benchmark.json` adds the twelve previously omitted floors. `BalanceHarnessTowerTests.cs`, `BalanceHarnessTowerBenchmarkTests.cs` and `BalanceHarnessTowerDashboardTests.cs` expand production parity, coverage and end-to-end verification. The README and plan describe the current default and retained evidence. No runner, combat implementation, production content, database migration, deployment or runtime configuration change was necessary.

The explicit catalog covers all 15 released floors; it does not invent unreleased floors up to the reward curve's maximum of 100. Future floor releases require a catalog update, enforced by the coverage test. Every fight starts fresh, so this does not simulate a continuous climb, acquisition, scouting or progression-specific builds. The existing execution budgets and exact-build replay requirements remain. Phase 2 integration, approved progression profiles/targets and other adapters remain deferred. Accepted starter and earlier Tower evidence, and unrelated working-tree changes, are preserved.
