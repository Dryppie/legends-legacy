# Independent team discovery in Tower Lab

**Progression audit and calibration — 11 September 2026:** [floors 2–5 review](Tower-Progression-Floors-2-to-5-Review.md) records independent four-Essence searches on floors 2–4, their separately confirmed linked Health/Power calibration against every known breach, and Kharad's fresh **20.9%** strongest-control check. Compatible controls and calibrated top builds persist for future searches. Floors 6–11 and practical acquisition coverage remain open.

**Fresh search and Kharad follow-up — 11 September 2026:** [new independent searches](Post-Calibration-Tower-Team-Search-Review.md) confirmed Garran's strongest saved team at **34.8%** and found a stronger Kharad team at **59%**, triggering a separate calibration. The [expanded-portfolio follow-up](Kharad-Expanded-Portfolio-Calibration-Review.md) applied another **8% to both Kharad Health and Power**; its strongest of 122 parties confirmed at **25.25%**, and the full family passes. At that stage, main-dashboard searches retained **4 floor-1 / 6 floor-5 controls**. Broader progression, practical Essence access and further independent ceiling searches remain open.

**First retained-build calibration — 11 September 2026:** compatible saved builds now enter new Tower Lab searches automatically as fresh benchmark controls; completed future studies retain their generated finalists. The [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied linked Health/Power factors of **1.06 to Garran** and **1.56 to Kharad** relative to their pre-campaign inputs. The strongest of 57/106 retained parties confirmed at **34% / 24.25%**, respectively, and both frozen families pass the 10–50% policy. This uses the declared full Essence pool including Rare Essences; this historical result is followed by the fresh searches and expanded calibration above.

11 September 2026. Increment 4 of the [automatic discovery plan](Automatic-Tower-Team-Discovery-Plan.md) is implemented in the local offline `LL/tools/BalanceHarness` dashboard. **Discover teams from scratch** is the default team-search mode. The existing historical reference-refinement workflow remains available as a separate mode.

The dashboard uses the [staged workflow](Automatic-Tower-Team-Confirmation-Review.md) already verified in increment 3. This increment changes no combat rules, boss scaling, progression targets or production services. At that original implementation stage Garran used Health **1.6764** / offense **1.6368**; the later calibrated values are recorded above. Browser smoke studies are implementation checks, not the planned balance pilots.

## Setup and preview

From the repository root, launch the local dashboard with:

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-dashboard --content-root LL/src/API/API.LL --catalogs-root LL/tools/BalanceHarness/Fixtures --runs-root TestResults/balance --port 5619
```

Then open [Tower Lab locally](http://127.0.0.1:5619). Build the harness first after changing source. The configured results folder is both the saved-run library and the history-scan root.

Choose the guardian, slot/equipment budget, purpose and experiment seed. The default suggestions are four slots before floor 5, five before floor 10, six at floor 10 and seven afterward. The approved checkpoints remain four at floor 1, five around floor 5, six around floor 10 and at least seven at floor 11; intervening transitions and equipment remain provisional. A lower checkpoint budget requires an explicit diagnostic purpose.

The equipment-only fixture `tower-team-equipment.json` declares the existing five-position equipment pattern without any Essence vectors or actor identities. It matches the supplied benchmark's entry gear, then uses the selected progression level, tier, rank and quality. Later positions may repeat equipment, while **every production RequiredSlots character receives independently generated Essences**. Equipment does not force Essence roles or copy a five-character party. Complete definition imports support custom equipment contexts.

Pool and evidence controls support:

- All currently allowed Essences or an explicit restricted pool.
- Hypothetical ownership or declared copy counts. Missing entries mean zero copies in an owned inventory; legal repeats across characters remain possible.
- The supplied user party as an optional reference only for compatible floor-1/four-slot studies. It is unchecked by default and never enters generation or finalist selection.
- Additional seed-free `BossBenchmarkReference[]` imports with full recipes, context, source and evidence hash. They must match the declared budget; see the [contract guide](Automatic-Tower-Team-Discovery-Implementation.md).
- Additional historical exclusions as a JSON array of combat seed integers.
- A complete independent schema-3 definition for custom contexts, ownership or sampling. Imports preserve explicit schedules and reject overlaps, unknown fields, unsupported modes and incompatible execution/content.

**Review study** shows every character's equipment, allowed pool/ownership, all references and their ordered builds, stage costs, detected historical sources, exclusions and the full downloadable definition. Find teams stays disabled until a valid preview exists. Changing settings invalidates the preview. Starting sends its hash, and the server starts that exact retained definition once. A stale or consumed preview is rejected; it cannot silently launch a different experiment.

The definition is revalidated at start, including changes to local content/execution or recorded seed history. Previewing performs no combat. The local session token and origin protections apply to preview/import/start/cancel; study JSON imports have a 2 MB limit. No production API or external environment is involved.

## Historical seeds and limits

History collection is independent of reference selection. It includes built-in historical schedules, the user benchmark's seeds even when that reference is absent, and recognized ledgers/definitions in the configured results folder. It supports staged studies, earlier boss/party searches, tuning seed ledgers, trial ledgers and normal Tower inputs, including arrays of per-seed inputs. Source paths and hashes appear in the preview and saved job's `preview.json`.

The scan is bounded to four directory levels and 4,000 visited directories, skips directory links and content/executable/source/battle trees, and stops descending when a covering schedule is found. Deeper/outside campaigns need explicit additional exclusions. This is conservative seed import, not a claim that every historical archive has been semantically reconstructed. Corrupt recognized history blocks preparation rather than silently disappearing. The schema's 100,000-exclusion limit remains in effect.

A read-only preview against the repository's full `TestResults/balance` folder succeeded with **84,857 exclusions from 294 sources** at verification time. No fights were executed by that preview. The subsequent [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) use a separately audited and frozen historical union; the preview count above remains this increment’s historical verification result.

## Running and reviewing

Run progress shows completed work by stage and supports cancellation. Attempted/completed counts, unused reservations and the fixed cap remain in the report. Complete execution does not imply accepted balance; cancelled, incomplete and invalid statuses are preserved.

The results panel separately displays:

| Finding | Meaning |
| --- | --- |
| Execution | Whether the experiment finished, exhausted its bounded search, was cancelled or failed. |
| Generated viability | Whether independently selected teams support the 10% lower threshold. A viable reference alone cannot establish this. |
| Frozen family | The predeclared family's 10–50% assessment with fixed-family uncertainty. |
| Overall balance | Includes unresolved earlier above-ceiling findings and execution validity. |
| Evidence | Full staged reconstruction with the matching build, or file integrity only for partial/older execution. |

Generated and reference views share exact duplicate cells while retaining both provenances. Cards show wins, draws, planned/valid samples, pointwise and family-adjusted intervals, and separate **50% ceiling** and **10% viability** checks. A zero-win party can satisfy an upper ceiling without establishing viability; the UI labels that distinction explicitly. Above-50% observations remain visible. Diagnostic findings cannot accept intended progression.

The panel includes paired reference differences in percentage points, selection outcomes, all earlier ceiling concerns and exact full-party exports. Confirmation never selects a replacement winner. Exports are available even for a frozen but partially measured cell and are labeled with the partial study status. Missing frozen finalists are shown as missing evidence, not recommendations.

Each completed study is reconstructed through the staged verifier. One immutable serialized result is cached; every cache hit still checks the complete file inventory and hashes. Changed files invalidate the view. Older execution can be reviewed with file integrity only, with overall balance marked unverified and replay disabled. Its recorded data remains historical; launch the retained matching executable/runtime for full reconstruction. Partial evidence is likewise never shown as verified acceptance.

Report JSON/Markdown downloads and normal Tower replay/export reuse the existing local routes. An explicit replay produces a separate verified combat-log artifact and does not append trials to the sealed study. The former blanket dashboard statement that Tower had no approved target was replaced with the approved policy, while legacy benchmarks retain descriptive semantics.

## Verification

**295 relevant backend tests passed** after integration. Four new API/service tests cover reference-invariant previews, every required character, illegal budgets/pools/ownership, stale preview protection, strict imports, running the exact preview, report/recipe exports, replay, cancellation, cache tampering, history exclusions and older array-shaped inputs. The final history-reader correction also passed those four focused tests. Final build: zero errors, with five existing warnings in unrelated test files.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~BalanceHarnessGoal'
```

Headless Edge/Playwright exercised the actual local API and embedded dashboard: checkpoint validation, default independent mode, reference/equipment preview, definition import, start, completed results, reference filtering, recipe/report downloads, verified replay, cancellation, partial status and switching to historical mode. Desktop **1440 × 1050** and mobile **390 × 844** screenshots were visually inspected; mobile had no page overflow and there were no JavaScript errors. Both dashboard scripts passed syntax checks.

The browser studies use one candidate per method and only six confirmation seeds per cell. They establish workflow behavior, not floor balance. Their complete archives retain their producing executable; a separate ordinary-budget run was cancelled after progress to verify interruption. No full pilot or tuning campaign was run.

Evidence is retained under `TestResults/balance/tower-team-lab-20260911/`: build/test logs and TRX, browser script/log/summary/screenshots, smoke definitions and exports, sealed browser runs, the full-results-root preview and source/document verification hashes. A Windows listener-inspection command was unavailable in the sandbox; the temporary servers were stopped through their originating terminal sessions instead. No required verification remains blocked.

## Changed files and next work

| Area | Files |
| --- | --- |
| New local integration | `TowerDashboardStudies.cs`, `TowerDashboardStudySeeds.cs`: exact preview/start, history import, preserved job status, verified/partial result loading and exports. |
| Existing local routes | `TowerDashboardServer.cs`, `TowerDashboardService.cs`, `TowerDashboardLoadouts.cs`: bounded import endpoints, study discovery, downloads/replays and shared job handling. |
| Interface and equipment | `Dashboard/index.html`, `Dashboard/dashboard.js`, new `Dashboard/studies.js`, `Dashboard/dashboard.css`, new `Fixtures/tower-team-equipment.json`. |
| Verification | New `BalanceHarnessTowerDashboardStudyTests.cs`; retained browser and regression evidence. |
| Documentation | This review and the current discovery, implementation, acceptance, harness and loadout plans/README. |

No migration, production configuration, dependency package, deployment or gameplay-content change is introduced. Existing unrelated edits, the supplied party fixture and sealed experiments were preserved.

[Increment 5: fixed pilots](Automatic-Tower-Team-Pilot-Review.md) is now complete. Floor 1 produced an independently generated 85.9% primary; floor 5 produced five 100% finalists. Both tested full-pool budgets fail the 50% ceiling. Subsequent retained-build calibrations and the [first progression batch](Tower-Progression-Floors-2-to-5-Review.md) now provide scoped passing evidence through floor 5. Floors 6–11, further independent ceiling checks and practical Essence access remain open.
