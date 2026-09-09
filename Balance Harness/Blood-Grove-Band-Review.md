# Blood Grove win-rate band revision — 8 September 2026

The user explicitly requested a **50–90% win-rate band**, replacing the initial 65–75% band after reviewing the pressure experiments. The earlier **70% aim** remains the preferred center. This applies separately to the two fixed Blood Grove encounters for the same level-5 Goblin Warrior, one-handed Shortsword and Heavy Breastplate [starter recipe](Blood-Grove-Starter-Reference.md). Other Essences remain diagnostic.

## Current policy

[idle-blood-grove-starter-goals.json](../LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json) now declares policy **`idle-blood-grove-starter-goals-v2`**, with inclusive bounds 50 and 90. Its schema, primary/enforced role, minimum 100 valid trials and exact fixture contract are unchanged. The policy's review reason records the user's decision; the change is not an automatic target inferred from outcomes.

The existing statistical rule remains: the entire pointwise 95% Wilson interval must lie within the band to pass. Overlap with either boundary is inconclusive, and an interval entirely outside the band fails. Thus 70/100 now passes, while 50/100 and 90/100 remain inconclusive. Zero wins and 100/100 still fail. Estimates for the two encounters are not averaged, and these fixed pairs do not certify whole-area difficulty.

The original policy is retained as [idle-blood-grove-starter-goals-v1.json](../LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals-v1.json). The completed coarse/fine investigation commands stay pinned to that historical 65–75% protocol so reproducing them does not silently rewrite their criteria. Use the current goals file with `evaluate` for the revised band. Earlier archived plans, selections, battles and evaluations remain historical evidence.

## Re-evaluation of saved evidence

Six complete saved runs were read through the verified evidence reader and evaluated under v2, without running combat, changing selections or extending samples. New evaluation bundles are retained under ignored `TestResults/balance/blood-grove-band-50-90-review`.

| Saved candidate confirmation | Wins / 1,000 | 95% Wilson interval | Revised result |
| --- | ---: | --- | --- |
| Coarse bonus 0.20, two Ravens | 884 | 86.27–90.24% | Inconclusive |
| Coarse bonus 0.20, Raven + Blood Zombie | 786 | 75.95–81.03% | Pass |
| Fine bonus 0.21, two Ravens | 885 | 86.37–90.33% | Inconclusive |
| Fine bonus 0.21, Raven + Blood Zombie | 741 | 71.30–76.72% | Pass |

**Both observed encounter win rates are inside the new band for each candidate.** Each candidate's aggregate policy result is nevertheless Inconclusive (exit 3), because the Raven interval slightly crosses 90%. Neither candidate has a failed check under v2. The original starter discovery/confirmation and both pressure experiments' original-content confirmations each retain two failed checks (exit 1), with zero observed victories. Across all six evaluations: **two pass, eight fail, two inconclusive, zero invalid**.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --goals LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json --run TestResults/balance/blood-grove-pressure-fine-reference/confirmation/candidate --output TestResults/balance/blood-grove-band-50-90-001
```

Use a new output directory. Re-evaluation does not require rebuilding the old battle executable or replaying combat; each new evaluation retains its policy and source evidence fingerprint.

## Next step and scope

**Later acceptance:** the [separate fixed confirmation](Blood-Grove-Acceptance-Confirmation.md) now passes this unchanged v2 policy at **89.13% Ravens** and **76.21% mixed**, with both full confidence intervals inside 50–90%. It has a new accepted local baseline. The historical evaluations and the original 3,000-trial Inconclusive result below remain unchanged; no policy exception or pooled estimate was used.

The subsequent [local candidate validation](Blood-Grove-Local-Validation.md) completes the scope decision and a separately declared fresh run. Version-12 content applies offense 2.421 to Blood Grove only and explicitly permits the transition to unchanged Crystal Creek. Across 3,000 trials per encounter, the starter reached 89.30% Raven clears (interval 88.14–90.36%, Inconclusive) and 76.73% mixed clears (75.19–78.21%, Pass). All 13 other areas and 32 external control cells stayed unchanged, and all four replays matched. No viable baseline was accepted and no samples were appended. Next, playtest the selected recipe and the next-area handoff while retaining the Raven uncertainty; later statistical work needs a new declared protocol.

This revision changes harness policy, historical-protocol wiring, policy tests and documentation. It does not change the player recipe, production combat coefficients or rewards, confidence-interval method, migrations, deployment or hosted CI configuration. It does not promote a candidate as a viable baseline.

## Verification

- Six saved runs passed evidence validation and were evaluated using the unchanged evaluator with the explicit v2 goals file. Their 12 checks had the expected outcomes and exit codes, with no invalid evidence. The current policy hash is `2615bd0260c2f02865c3c77bb5914807f660721f4822874aa5a8757a78fd535b`; its fixture contract remains unchanged.
- The standalone harness build passed with zero warnings/errors using `dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore -p:BuildProjectReferences=false`, against the existing compiled dependencies.
- Full test-project builds using `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore` encountered concurrent, unrelated combat-style compilation errors: first CS1503 in `StateSyncCommandScopeCatalog.cs`, then CS8057 in `CharacterBuildRules.cs`. A test build using existing compiled dependencies also failed because the newly added `CombatStyleFoundationTests` require the new `Services.LL.CombatStyles` implementation. Those files were left to their ongoing task. Updated policy/pressure tests were therefore not run for this revision; the earlier 85-pass result belongs to the preceding fine-sweep increment.
- Once the concurrent build is ready, run `./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarness'` to verify the updated policy boundaries and historical-protocol coverage. Full backend tests, new reference battles, standalone smoke scripts and hosted CI were not run for this policy-only revision.
- Scoped whitespace checks passed, and all 154 local links across the balance Markdown, tool README and roadmap resolved. The retained v1 policy file matches its original SHA-256 `2bf40c007298dc2683d81ec299ee49efd06777cefb22d117c27c5d713195b88f`. The production regional balance file remains unchanged at SHA-256 `50418780d995e2b04a878cfbc878cbc113bb5c9779b06ca3596e66f6d3126c8d`.

Follow-up verification: the local-candidate increment resolved the concurrent-build limitation and passed **all 2,066 backend tests**, including this revision's policy boundaries and historical-protocol tests, through `./build/run-tests.ps1 -NoBuild` after a successful Release build. Its new battles and version-12 production-content hash are recorded separately in the [local validation report](Blood-Grove-Local-Validation.md); the bullets above describe the earlier policy-only increment.
