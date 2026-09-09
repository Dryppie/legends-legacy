# Blood Grove acceptance confirmation — 8 September 2026

The user asked to proceed with the remaining Raven acceptance decision after the [Crystal Creek checkpoint passed](Crystal-Creek-Fine-Tuning-Review.md). This is a fixed confirmation of the primary LL game's unchanged level-5 Goblin Warrior/Shortsword/Heavy Breastplate starter. The existing **50–90% win band and whole-interval 95% Wilson rule** remain intact. No acceptance exception, coefficient change, new recipe or duration target is inferred.

## Protocol declared before combat

Run the existing two-cell starter fixture once with **10,000 trials per encounter: 20,000 battles total**. This is the existing runner's maximum per-cell budget and reduces sampling uncertainty around the prior 89.30% Raven estimate. Use one fresh master seed, chosen as the first integer from **918091 through 918190** whose actual 20,000 derived seeds are unique and disjoint from every retained suite schedule under `TestResults/balance`. Seed-only rejection does not observe combat outcomes. Write the exact seed, frozen content/fixture/policy hashes, excluded-schedule inventory and executable identity to `plan.json` before starting.

Use a clean Release build verified through the full backend test runner and retain its exact executable/dependencies as `blood-grove-acceptance-v1`. Capture all 15 current combat files, fixture, policy and selected nonsecret settings before combat; no account or environment secrets. Blood Grove offense stays **2.421** and the current creature-only Crystal Creek changes remain fixed. The original [3,000-trial local validation](Blood-Grove-Local-Validation.md) is historical evidence, not a paired control or a source of pooled observations; its executable differs.

Preflight adjustment before any battle: all current combat files matched the previous Creek verification, but the current game assemblies had changed with concurrent work. Instead of treating the earlier 2,107-test binary as the current build, rebuild and verify the current source, then freeze that new identity. This does not change the sample budget, content, seed-selection rule or acceptance policy. The accepted Creek reference remains tied to its retained identity and is not recertified for the newer build by this Blood Grove-only run.

Evaluate both new cells independently using unchanged policy v2. A pass requires each complete 95% Wilson interval inside 50–90%; boundary overlap remains Inconclusive. Keep draws as non-wins. Replay trial index 0 in each encounter with detailed logs, regardless of outcome. Validate complete evidence and unchanged inputs/executable before and after execution. No candidate search, sample extension, replacement seed, pooling, or further confirmation is scheduled after this result. Failure or uncertainty is a valid stopping outcome.

This confirmation is deliberately chosen after an earlier inconclusive estimate. Its intervals retain the evaluator's pointwise 95% interpretation; they are not a family-wide confidence claim across the history of tuning and repeated reviews. It measures the two fixed encounters, not the natural area-wide spawn distribution.

If both new checks pass, review and explicitly record a new local regression baseline pointing to this complete run, preserving the older Inconclusive assessment. Otherwise leave gameplay acceptance open without weakening the policy. No game-content changes occur, so the previously completed scope/control checks remain the available evidence; this run does not repeat them or certify unmeasured modes. Preserve the existing Creek baseline separately. Do not deploy, restart the API, apply migrations or modify the local test character.

## Result

**Both checks pass.** The seed-only scan selected **918091** immediately: 20,000 unique trial seeds, disjoint from **35,818 distinct seeds across 223 retained suite schedules**. The run completed once with no invalid, cancelled or missing battles. The actual saved schedule matches the preflight schedule exactly; both outcome-independent detailed replays matched.

| Encounter | Wins / losses / draws | Win rate | 95% Wilson interval | Unchanged v2 policy |
| --- | --- | --- | --- | --- |
| Raven + Raven | 8,913 / 1,087 / 0 | **89.13%** | **88.50–89.73%** | Pass |
| Raven + Blood Zombie | 7,621 / 2,369 / 10 | **76.21%** | **75.37–77.03%** | Pass |

The ten draws remain valid non-wins. The evaluation returns exit **0**, with two enforced Pass checks and no issues. No samples were appended, no seed was replaced after outcomes, and the policy/content stayed unchanged. The former 3,000-trial Raven result is still Inconclusive in its original archive; the new independent result satisfies the same policy without an exception.

## Baseline decision

After reviewing the completed evaluation, replay checks and passing backend tests, explicitly accepted **`TestResults/balance/baselines/blood-grove-starter-v1.json`** against the new `blood-grove-acceptance-reference/confirmation` bundle. Its reason pins the unchanged level-5 recipe, offense 2.421, 10,000 trials per cell and fresh seed. The run report's `BaselineAccepted: false` records the state before this separate manual CLI acceptance; the immutable baseline manifest records the subsequent decision.

At this acceptance step, Blood Grove and Crystal Creek each had a scoped passing local reference on their recorded builds. The Creek reference and its executable remained unchanged; this newer Blood Grove build alone did not establish Creek regression compatibility. Other Essences, chest outcomes, natural spawn combinations and activity modes were not newly certified. The combined regression below and the [9 September package](Starter-Baseline-Package.md) subsequently completed the shared-build comparison and local recovery work. Off-device storage is deferred by user choice; a hosted gameplay gate remains a separate decision.

A final working-tree check after confirmation/acceptance found a concurrent change to `combat-styles/combat-styles.v1.json`. The captured file, accepted bundle and passing test evidence remain intact; the exact drift is recorded in `post-run-working-tree.json`. No new battles or samples were added. This acceptance concerns the verified snapshot and does not certify the subsequently edited checkout.

## Evidence and verification

| Evidence | SHA-256 |
| --- | --- |
| Frozen `plan.json` | `850daa1479df3b30a4696bfbb790b1a5a02d6a10bd3328a533b2309364196056` |
| Completed `results.json` | `a765637c95ca08ec8a40face60236bf39da88f73e9b56444d702c20410deacc7` |
| Confirmation bundle fingerprint | `fb8d2227e40936b5dedd58f44ca80b1bc804b54e31590382b03e430b82570f49` |
| Accepted baseline manifest | `be07ed88b71c1b949551866f0f236f5fa40c6e8fc27413e40b724d9c742c6182` |
| Retained harness executable | `a882b053c51e4b80fda92a820b2c1abb4b3e7ce4d27fa2f0d62088ffc6cabf21` |
| Retained combat service assembly | `6a36450912054746ba5898f8bf4ab29ee8d94d02615efe35bea4f6f414779104` |

The complete evidence is under ignored `TestResults/balance/blood-grove-acceptance-reference`, including the frozen plan/inventory, captured content/settings/fixture/policy, confirmation, evaluation, two replays and execution script. Its executable, dependencies, pre-combat protocol and full-test TRX are under `TestResults/balance/retained-builds/blood-grove-acceptance-v1`, with a 65-file retention manifest. Keep both directories and the accepted baseline together. Replay requires this exact .NET 10.0.11 / Windows X64 execution identity; a Git revision alone does not reconstruct the shared dirty source tree. These are retained local artifacts, not durable published storage.

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore`: passed, four existing warnings, zero errors. The approved unsandboxed build path avoided the generated-cache permission problem found in the preceding increment.
- `./build/run-tests.ps1 -NoBuild`: **2,112 passed, zero failed/skipped**. The retained five harness/game assemblies match the tested dependencies; the test binary hashes were checked before preserving the TRX.
- The existing `suite`, `evaluate`, two `replay` calls and separate `baseline accept` command all completed successfully. No new harness or game implementation was needed. The fixed orchestration script is retained as `TestResults/balance/confirm-blood-grove-acceptance.ps1` and with the run.
- Current content, captured settings/fixtures, execution and the pre-combat plan remained unchanged during the run. Historical Blood Grove and Creek evidence and the accepted Creek baseline remain intact. Documentation links, scoped whitespace and recorded checksums were verified after the review update.

Changed repository files are this review and the linked harness plan, starter acceptance, historical local/Band/Creek follow-up notes, tool README and post-alpha roadmap. No gameplay code, combat coefficients, fixture/policy, migrations, environment configuration, deployment, API restart or test-character changes were made in this increment. Frontend tests and hosted CI were not rerun for this artifact/documentation work; no required command remains blocked.

**Subsequent combined regression:** the [current-build check](Starter-Regression-Review.md) repeated the accepted Blood Grove and Creek schedules on one newer retained build. All 52,000 battles and four detailed replays match, all four primary goals pass, and all 2,119 backend tests pass. This closes the compatibility check for that recorded build without changing this accepted baseline, original confirmation budget or historical Inconclusive evidence.
