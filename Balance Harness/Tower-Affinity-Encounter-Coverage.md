# Supported affinity search: encounter compatibility — 25 September 2026

The unchanged supported affinity search completed all six selected encounter cases. All 15 released floors in the captured content prepared legal parties, and all six complete search archives passed native reconstruction. This closes a compatibility gap beyond the original floor-5 workload. It does not establish stronger teams or general search quality.

## Coverage and result

All 45 projected reference parties (three per floor) prepared successfully before any combat. The six full searches covered party sizes 5, 10 and 15 and the mechanics listed below. Each search spent exactly 528 fights; 3,168 fights started and completed, with no replacement runs or new allocated seeds. All six returned `BenchmarkRetained`.

| Floor | Guardian / case | Party size | Search time | Reference validation wins | Challenger validation wins |
| --- | --- | ---: | ---: | ---: | ---: |
| 3 | Morrowmaw: summons and summon consumption | 5 | 9.72 s | 60/60 | 60/60 |
| 5 | Kharad: barriers, statuses and summons | 10 | 23.14 s | 49/60 | 42/60 |
| 7 | Eydis: guardian healing | 5 | 8.12 s | 60/60 | 60/60 |
| 8 | Kodoku: healing and regeneration suppression | 10 | 15.86 s | 60/60 | 59/60 |
| 10 | The Mad King: health-dependent damage modifiers | 15 | 19.64 s | 60/60 | 60/60 |
| 15 | Serath: statuses and damage modification | 15 | 22.85 s | 0/60 | 0/60 |

Times measure the native search, excluding preparation and subsequent archive reconstruction. Each validation panel contains 60 paired seeds for its nominated challenger and reference. These are reused diagnostic schedules, not fresh confirmation panels.

Several cases have a win-rate ceiling, while floor 15 has no validation wins. The fixed budget therefore provides limited discrimination of search quality. Floor 15's mean remaining guardian health was 63.47% for the reference and 63.64% for the challenger. Those observations do not justify ranking or promoting teams. The next useful quality evaluation needs intended progression budgets and independently specified encounter references, with a separate fresh evaluation design; another algorithm variant is not justified by this fixture.

## Fixed diagnostic setup

The source was `TestResults/balance/tower-affinity-nomination-pilot-01-20260925/search/root-01/control`, externally pinned by root manifest SHA-256 `e4fd91265a96720d2c4aa5fe1fd56309213d9d695fc88f18bd8c55eff73edec1`.

The fixture preserved the supported policy, original three selected damage affinities, historical panel and root seeds, full exclusion list, captured content, equipment budget and level-40/five-Essence progression. Target floor, party size, reference labels, encounter mechanics and execution identity were explicitly rebound. All projected references were labeled diagnostic; the source's confirmation did not transfer to these encounter contexts.

The three source references have identical first groups of five. Taking those would collapse the required three-reference comparison. Five-character cases therefore use their distinct final groups of five. Ten-character cases preserve the full parties, and fifteen-character cases append the first five existing loadouts with distinct neutral character identities. Saved Essence order is preserved. This is an engineering projection, not a proposed player build or intended progression setup.

The case list was fixed before execution: floors 3, 5, 7, 8, 10 and 15, at most 3,168 fights, a ten-minute cancellation deadline and 1.5 GiB output checks. All 15 projected plans and all 45 preparations preceded combat. Retained output was 606,183,581 bytes. The completed suite took approximately 2 minutes 13 seconds.

## Changed files and verification

- `LL/tools/BalanceHarness/Fixtures/tower-affinity-encounter-coverage.json`: fixed cases, required mechanic signals and resource limits.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityEncounterTests.cs`: explicit diagnostic projection, all-floor preparation, six native searches, complete archive reconstruction and durable per-case results. The combat fixture is skipped when unconfigured.
- `LL/tools/BalanceHarness/AFFINITY-SEARCH.md`: usage instructions, projection rules and interpretation limits.
- This report records the completed evidence and next decision.

The configured run passed seven tests through `build/run-tests.ps1 -NoBuild`: six projection checks and the complete encounter fixture. The default unconfigured run passed six checks and skipped combat. Each case authenticated all 528 archived trials and reconstructed the same search summary. Source archive pins remained unchanged. No commands remain blocked.

The test project built with existing restored references using `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false`. Build output contained existing analyzer warnings and no errors.

Evidence is under `TestResults/affinity-encounter-coverage-20260925`: `definition.json`, fifteen frozen plans, `preflight.json`, six sealed search archives, six `result-floor-*.json` receipts and `coverage.json` with status `CompatibilityVerified`. The test log is `TestResults/affinity-encounter-coverage-20260925.log`.

No search policy, gameplay behavior, migration, deployment or gameplay configuration changed. No team was confirmed or promoted.
