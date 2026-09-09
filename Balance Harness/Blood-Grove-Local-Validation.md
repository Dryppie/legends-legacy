# Blood Grove local balance validation — 8 September 2026

The user authorized making the selected 0.21 pressure candidate specific to Blood Grove, confirming it on the current engine, checking neighboring areas and recording a regression baseline if validation passes. The exact [starter recipe](Blood-Grove-Starter-Reference.md) stays fixed: level-5 Goblin Warrior, one-handed Shortsword and Heavy Breastplate, without a Combat Style. The [current policy](Blood-Grove-Band-Review.md) remains a 70% aim with an inclusive 50–90% band per encounter and the existing 95% Wilson interval rule.

## Authored change and scope

[region-combat-balance.json](../LL/src/API/API.LL/Data/progression/region-combat-balance.json) advances from version 11 to 12. A data-driven area override sets **Blood Grove offense scaling to 2.421**, exactly the local equivalent of the earlier regional post-tutorial bonus 0.21. The shared regional bonus remains 2.3. Player stats, enemy health/defenses/abilities, placement, Combat Rating metadata and all other areas' scaling remain unchanged. Creature regeneration follows the existing square-root health/offense coupling, so Blood Grove's regeneration scaling becomes approximately 0.73259 of original.

Keeping Crystal Creek unchanged at offense 4.511 creates an **86.33% offense increase from Blood Grove**, exceeding the shared 29% normal-step limit. A second, explicitly reasoned entry sets Crystal Creek's offense transition ceiling to 87%; it changes no creature stats. The provider validates overrides against real, unique placements, positive finite offense values, nonnegative finite transition ceilings and a nonempty reason. It still checks monotonicity and all health/defense/resistance transitions. No shared guard is disabled. The larger next-area step is a remaining gameplay consideration, not a claim that Crystal Creek entry is balanced for this starter.

The candidate is fixed before confirmation. There is no parameter search or new recipe/policy revision in this validation. Historical pressure commands reject area-overridden content so they cannot silently test an ineffective regional parameter; their workflow tests use a copy with the local overrides removed. The new validation retains that same original-content control alongside its captured candidate.

## Budget declared before reference battles

| Stage | Budget | Master seed |
| --- | --- | --- |
| Original/candidate confirmation | 2 content versions × 2 encounters × 3,000 trials = 12,000 battles | 518091 |
| Original and First Hunt controls | 2 content versions × 48 cells × 100 trials = 9,600 battles | 518091 |
| Total | **21,600 battles** | |

The confirmation count is fixed at 3,000 per encounter before running combat, to assess a candidate whose previous Raven estimate was near the upper confidence boundary. Run once, with no sample extension, candidate reselection or coefficient changes after outcomes. Previous experiments and discovery are not pooled into these estimates.

Preflight checks actual derived starter battle seeds against the first 3,000 trials for master seeds 1337, 7331, 940031, 620903, 318091, 318092, 418091, 418092, 418093 and 418094, plus the other local-validation mode. Development checks use separate master seed **518092** and never execute the full run's reserved set. Resolving schedules does not reveal outcomes.

The full validation requires all **13 non-Blood-Grove authored areas** to retain every scaling field, and all **32 non-Blood-Grove control cells** to retain gameplay exactly. The other 16 control cells describe changes inside Blood Grove. Retain four detailed replays, chosen before outcomes: confirmation trial index 0 in both encounters for original and candidate. Verify all snapshots, fixtures, settings and executable identity throughout. A completed validation may fail the gameplay goal; it never promotes a baseline automatically.

## Reproduce

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- validate-blood-grove --output TestResults/balance/blood-grove-local-001
```

Build Release first and use a new output directory. Default `--samples 100` is the declared full validation; `--samples 1` runs a 216-battle workflow check. The base/control count is 1–100; confirmation uses 30 times that count. Keep the plan, captured source/fixtures, original/candidate runs, goal evaluations, paired comparisons and detailed replays together. The CLI returns 0 for a complete valid investigation even if its nested evaluation fails or remains inconclusive; inspect the evaluation before accepting any baseline.

## Completed reference result

The declared run completed once under `TestResults/balance/blood-grove-local-reference`, using master seed 518091 and **21,600 valid battles**. There were no invalid, cancelled or unexecuted battles. The original regional content produced zero wins in both 3,000-trial starter cells; each original interval is 0–0.13% and fails v2.

| Local candidate encounter | Wins / losses / draws | Clear rate | Pointwise 95% Wilson interval | v2 result |
| --- | --- | ---: | --- | --- |
| Raven + Raven | 2,679 / 321 / 0 | **89.30%** | 88.14–90.36% | Inconclusive |
| Raven + Blood Zombie | 2,302 / 695 / 3 | **76.73%** | 75.19–78.21% | Pass |

Both point estimates are inside the approved 50–90% band. The Raven interval still overlaps the upper boundary, so the aggregate evaluation is **Inconclusive: one pass, zero fail, one inconclusive, zero invalid**, with evaluation exit code 3. The validation command itself returned 0 because the investigation was complete and valid. The three mixed-encounter draws are valid non-wins, not tick-limit draws. Median victory durations were 57 seconds against Ravens and 81 seconds against the mixed pair; pacing has no approved target here.

All **13 other authored areas** retained every scaling field. All **32 control cells outside Blood Grove** retained gameplay exactly. The 16 Blood Grove control cells changed in all 1,600 paired trials (four cells in the original suite, twelve in First Hunt), as expected from the local stat adjustment. All four preselected detailed replays matched, and the captured content, fixtures, settings and executable identities remained stable throughout.

**No viable regression baseline was accepted.** The local candidate remains implemented for review, but the conditional acceptance requirement has not been met. Experimental manifests under the comparisons identify original-content controls only. The fixed run is closed: no extra samples were appended, no candidate was reselected, and historical estimates were not pooled. This establishes a large improvement over the original losing starter, while leaving Raven upper-bound uncertainty visible. Playtesting the selected recipe and the Blood Grove-to-Crystal Creek handoff is the next gameplay review; the unchanged Crystal Creek strength is not certified for this starter. Any later statistical investigation needs its own declared protocol.

## Evidence and verification

Retain the complete ignored output directory with its captured candidate/original content, fixtures, six run bundles, evaluations, paired comparisons and replays. These artifacts are local and are not guaranteed to exist in another checkout. No accepted-baseline manifest was created.

| Evidence | SHA-256 |
| --- | --- |
| `plan.json` | `fcb1804e9602ba3836a93d4b0b9dd2166b41af9015444211528b74825f3c489f` |
| `results.json` | `c9507abe66d1625ab1c07432bab57ec33e96547a2636f4193178c268aa44abdf` |
| Version-12 regional balance content | `2492fc00c9cc9e92cae562b7b2fd45a3ccc94f8b10691a8d070557420bec16ef` |
| v2 policy contract | `2615bd0260c2f02865c3c77bb5914807f660721f4822874aa5a8757a78fd535b` |
| Starter fixture contract | `e820b30cf1bdd8763c4ea8c9fdbf138a870611bdfebe728f71454bb348e8a87f` |

The confirmation manifest records .NET 10.0.11 on Windows X64 and all five executable hashes. It includes the current Combat Style implementation/content; this fixed starter uses no Combat Style. The `Services.LL` assembly hash is `384091b5cd88c5c6c1edc8396f020ce4fc4a60cb570c0e02ceaa940e4b227432`; the harness hash is `73411b98bb34ad358545d68c454e0787312813292d831fce304a43d641ea65b8`. Earlier pressure evidence belongs to its own executable identity.

- The Release test/tool build passed with four existing warnings and no errors. The initial targeted run exposed a stronger-enemy comparison fixture that changed the regional curve without updating the new fixed local multiplier. Its overlay now scales both consistently, preserving the production monotonicity guard.
- **All 2,066 backend tests passed**, with zero failures/skips, through `./build/run-tests.ps1 -NoBuild` after rebuilding the fix. This includes current/historical policy tests, both pressure workflows, the new fixed local-validation workflow/cancellation checks, provider mapping and all-area stat checks, override rejection and unchanged transition guards. It also resolves the concurrent-build verification limitation recorded in the preceding band revision.
- `dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll validate-blood-grove --output TestResults/balance/blood-grove-local-reference` completed the declared full budget and replay/control checks. No further reference or smoke samples were run after its outcome.
- Scoped `git diff --check` and whitespace checks for all four new files passed. All 165 local links across the 12 balance documents, tool README and roadmap resolved. The final artifact executables still matched the confirmation manifest.
- Hosted CI, standalone cohort smoke scripts, frontend checks and a human playthrough were not run for this backend/content increment. No required command remains blocked. This change adds no database migration or environment setting and changes no hosted CI configuration. The authored regional content is version 12; it will affect ordinary production scaling when shipped. Nothing was deployed or applied to an external environment.

Subsequent work: the [level-10 starter handoff](Crystal-Creek-Starter-Handoff.md) measures the selected equipment with quest rewards and an optional Fury upgrade in both areas. It demonstrates a conditional Amulet/Goblin path into Crystal Creek and records a pacing concern, with a prepared human playtest checklist. That separate 16,000-battle run does not rerun or pool this level-5 reference, change its Inconclusive result, or accept a baseline.

Later acceptance: the separately declared [20,000-battle confirmation](Blood-Grove-Acceptance-Confirmation.md) uses fresh seed 918091 and unchanged Blood Grove content on a newer verified build. Its **89.13% Ravens [88.50–89.73%]** and **76.21% mixed [75.37–77.03%]** both pass v2. That new run has an explicitly accepted local baseline. This original 21,600-battle investigation remains closed and Inconclusive; no exception, pooling or sample extension was applied to it.
