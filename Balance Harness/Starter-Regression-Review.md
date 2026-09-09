# Combined starter regression review — 8 September 2026

The user asked to check both accepted starter baselines against the latest shared build after concurrent Combat Styles changes. This work targets the primary LL game's offline harness. It preserves the selected builds, 50–90% win policies and accepted baseline manifests.

## Fixed regression check

Build the current backend, run its full tests through `build/run-tests.ps1`, and retain the exact harness/game executable used for the comparison. Capture the current 15-file combat catalog, selected nonsecret settings, both saved fixture definitions and current matching policies before combat. Both checkpoints run against this one captured build/content snapshot.

| Accepted reference | Candidate schedule | Fixed battles | Enforced checks |
| --- | --- | --- | --- |
| Blood Grove starter v1 | Original seed 918091; 10,000 trials in each of its two cells | 20,000 | Both level-5 starter pairs |
| Crystal Creek starter v1 | Original seed 818092; 2,000 trials in each of its sixteen cells | 32,000 | Only the two primary level-10 Amulet/Goblin Creek pairs |

The fixed total is **52,000 regression battles**. Reuse each reference's exact trial identities for paired comparisons; these repetitions are not new independent confirmation samples and are never pooled with the baselines. All fourteen Creek preparation/return diagnostics remain in the comparison. Four outcome-independent detailed replays use trial index 0 in the four primary cells.

Evaluate the unchanged approved policies and retain both compatible comparisons. Report all changed gameplay records and outcomes; do not invent a duration or zero-change policy gate. A policy failure or boundary overlap remains Fail/Inconclusive. Incompatible, corrupt or incomplete evidence is invalid. Do not tune, append samples, replace a seed or promote a baseline automatically. Keep all source archives untouched. Subsequent shared-checkout drift is reported separately from the frozen result; do not chase it by silently repeating this closed run.

This also adds a repeatable local script around the existing CLI, so later builds can perform the same check with a new output directory. It does not publish artifacts, enable a hosted gameplay gate, deploy services, restart the API, apply migrations or modify the test character.

## Result

**Pass on the retained 8 September build.** All 52,000 planned battles are valid, with no invalid, cancelled or missing results. Both comparisons are compatible across all eighteen cells, with **zero changed gameplay records or outcomes**. All four fixed detailed replays match their saved results. The fourteen Creek diagnostic cells remain unchanged without gaining an acceptance target.

| Primary encounter | Wins / trials | Win rate | 95% Wilson interval | Existing 50–90% policy |
| --- | ---: | ---: | ---: | --- |
| Blood Grove: two Ravens | 8,913 / 10,000 | 89.13% | 88.50–89.73% | Pass |
| Blood Grove: Raven + Blood Zombie | 7,621 / 10,000 | 76.21% | 75.37–77.03% | Pass |
| Crystal Creek: two Blue Slimes | 1,488 / 2,000 | 74.40% | 72.44–76.26% | Pass |
| Crystal Creek: Blue Slime + Frost Imp | 1,109 / 2,000 | 55.45% | 53.26–57.62% | Pass |

These reproduce the accepted estimates; the repetitions do not increase their independent sample counts. The level-5 Goblin Warrior/Shortsword/Heavy Breastplate and level-10 Amulet/Goblin recipes remain unchanged, with no Combat Style equipped. Relative to either reference, the comparator records five changed game/harness assemblies and the changed Combat Styles catalog. The other fourteen combat files are identical. This verifies the selected starter recipes through the shared implementation changes; it does not certify Combat Style builds, natural spawn distributions or other activity modes.

The separate post-run working-tree audit found no added, deleted or modified files among the 1,310 captured Core/service/harness source and project files. All fifteen live API combat files, selected settings, two policies and five assemblies in both harness and test output still match the retained run. This audit independently checks the live tree because the corrected run intentionally reads the first attempt's captured content. Future source changes require another explicitly identified run.

## Workflow correction

The first attempt completed its 20,000 Blood Grove fights, then correctly stopped as Incompatible: PowerShell's JSON round-trip changed the saved start timestamp from `2000-01-01T00:00:00+00:00` to `2000-01-01T01:00:00+01:00`. The scenario hash deliberately distinguishes that representation. No compatibility rule was relaxed and the first attempt remains retained under `starter-regression-reference` with its failure/comparison evidence; it supplies no passing combined result.

The script now extracts the saved definition as raw JSON and verifies an exact text copy before combat. The corrected attempt uses the same retained executable, captured content, original seeds and 52,000-battle budget in a new directory, `starter-regression-reference-corrected`. The failed attempt's 20,000 fights are engineering verification only, never pooled samples or a reason to change the selection or target.

## Repeatable check and retained evidence

[build/check-starter-balance.ps1](../build/check-starter-balance.ps1) captures one compiled build and selected content/settings, validates both accepted archives and approved policies before combat, preserves the exact saved fixture JSON, then runs the fixed comparisons, evaluations and replays. It rejects an existing output directory and detects altered frozen inputs. The aggregate result follows the existing win policies; gameplay differences are reported separately. It does not promote baselines or create a hosted gate. See the [usage instructions](../LL/tools/BalanceHarness/README.md#combined-starter-regression).

The completed evidence lives under ignored `TestResults/balance/starter-regression-reference-corrected`: frozen plan, executable/content/fixtures, baseline evaluations, candidate archives, comparisons, evaluations, four replays, aggregate results and `post-run-working-tree.json`. The executable and dependencies, pre-combat protocol, original and corrected scripts, correction record and full-test TRX are retained under `TestResults/balance/retained-builds/starter-regression-v1`. Its original 66-file manifest is unchanged; `correction-retention.json` separately hashes the two correction files. Preserve the failed attempt too.

| Evidence | SHA-256 |
| --- | --- |
| Corrected run `plan.json` | `5af0b9f16ca7eb53f836f0c0ab7c5a253c5079e424f4646a3f1a824dbceb2270` |
| Corrected run `results.json` | `35a6de2f4f88eeae46a3301867443b2122ee45e36ff43c257f87e9b9f5c7aad2` |
| Blood Grove candidate bundle fingerprint | `167fd62bb8c2e70d309052e2d611cf2a062e96b7b9cf567c7f42c938773fa3bb` |
| Creek candidate bundle fingerprint | `ee28706d936757d9bd531ba7ebc3f333b4e46636ef3f2914627b096f4d5bf33d` |
| Retained harness assembly | `f3761117e09eca96c05a06dee0b81884f2afe8284ffe6cc161985315191f2f58` |
| Retained combat service assembly | `97cf1cbe745972ec0b53890bab10e27893ce7a179b691dfdf81a9ae12ed01259` |
| Corrected execution script | `20b229b467fa8d428a966a4a39915884eace46e360a0141478e8f312bbbefd35` |
| Post-run working-tree audit | `7327fee2852d2a371266a907b805faf6ff01e679ea4f8b7a61a29c8498b69b2c` |
| Original retention manifest | `b6de8de185ff78f98a103af6c7089eff4b7eab62410afdbebfe56aea8bc0473e` |
| Correction retention manifest | `6bf18b045eea6bf27f5f0e978069b02103b4d2326fcd60ad5290e1297ddff980` |

Both accepted baseline manifests and their historical evidence remain unchanged. The [9 September package](Starter-Baseline-Package.md) now preserves both accepted references, their complete evidence and exact executables, with successful independent recovery on .NET 10.0.11 / Windows X64. The user chose local storage for now; off-device storage is deferred and hosted verification of unpublished changes remains pending publication.

## Verification and scope

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore`: passed with zero warnings/errors using the approved build path.
- `./build/run-tests.ps1 -NoBuild`: **2,119 passed, zero failed/skipped**. The five tested harness/game dependencies match the retained binaries.
- `./build/check-starter-balance.ps1 -OutputDirectory TestResults/balance/starter-regression-reference-corrected -HarnessDirectory TestResults/balance/retained-builds/starter-regression-v1/net10.0 -ContentRoot TestResults/balance/starter-regression-reference/content`: completed with exit 0; both archived baselines and both candidates pass their unchanged policies.
- PowerShell parsing, exact raw fixture-copy verification, reused-output rejection, retained-file/historical checksum checks, scoped whitespace and local documentation links passed. The first incompatible run and the correction are retained above.

Changed repository files are the reusable script, this review, the main harness plan, starter acceptance, Blood Grove acceptance and Creek fine-tuning follow-ups, tool README and post-alpha roadmap. No C# gameplay implementation, combat content, fixture/policy, migration, environment configuration, deployment, API restart or test-character change was made in this increment. Frontend tests and hosted CI were not rerun for this local harness/documentation work; no required verification command remains blocked.

**Subsequent recovery verification — 9 September:** the [portable package](Starter-Baseline-Package.md) retains this run, its first incompatible attempt, both accepted references and their supporting evidence/executables. All 428,158 files restore outside the checkout; four saved suites pass their existing checks and eight fixed replays match using an isolated .NET 10.0.11 runtime after the system updated to 10.0.12. These are recovery checks with no new independent samples or baseline promotion. The user chose local storage for now; hosted verification after publication remains separate.
