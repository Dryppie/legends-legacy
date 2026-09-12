# Conditional balance checks

`balance-paths.json` is the central policy. Patterns are PowerShell wildcards;
`*` matches across directories. Markdown-only changes are ignored. Gameplay
paths select harness correctness tests and both existing three-sample idle smoke
suites. Harness tests/dashboard changes select correctness tests only; harness
C# and fixture changes also select smoke runs. Build/dependency/policy changes
select both checks conservatively.

The dedicated Balance harness workflow owns these checks on PR updates and
pushes to main or releases/**. PRs use the merge-base diff, pushes use the full
before/after diff, and renames include both old and new paths. Missing base
history runs both checks. Every event gets a small detector job, even when the
expensive job is skipped. Its summary explains the selection.

Use Actions > Balance harness > Run workflow to force both checks. Full studies
and accepted-baseline comparisons remain manual; smoke runs check repeatability
and advisory evaluation, not balance acceptance. A relevant merge can run again
on the release branch to check the integrated revision.

The backend gate excludes Category=BalanceHarness. Add
`[Trait("Category", "BalanceHarness")]` to new harness test classes; the policy
test checks the existing harness naming convention. Ordinary gameplay regression
tests remain in the backend gate, even when they test damage or balance rules.
Local `build/run-tests.ps1` still runs everything by default. Examples:

```powershell
./build/test-balance-impact.ps1
./build/get-balance-impact.ps1 -Base HEAD~1 -Head HEAD
./build/run-tests.ps1 -Filter 'Category!=BalanceHarness'
./build/run-tests.ps1 -Filter 'Category=BalanceHarness'
```

When moving or adding gameplay logic, update the path policy and its test cases.
The policy deliberately covers game data, shared primitives and gameplay areas
broadly; controllers, unrelated persistence, admin services and frontend code
are outside it. Add a path if a change there affects harness inputs or execution.

Backend and LiveOps still each own their ordinary backend correctness gate;
neither runs harness tests. Consolidating those publication pipelines is separate
from balance selection. The independent balance workflow does not block Docker
publication on pushes. Require its `Idle workflow / advisory balance` PR check
in branch protection if it must block merging (no repository settings are
changed by this implementation).
