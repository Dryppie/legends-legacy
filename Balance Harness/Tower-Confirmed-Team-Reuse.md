# Reusing the confirmed Tower team

**Reuse exercised — 22 September 2026:** The [three-reference practical execution](Tower-Practical-Three-Reference-Execution.md) used this exact bundle, completed **3,528 fights** and passed both audits. It retained `96b94357…` with **762/1,000 wins**, preserving both original controls. Current history is **551,408 exclusions across 232 files**. The zero-fight preparation and its historical measurements below remain separate evidence.

22 September 2026. Target: offline BalanceHarness. **ReadyForExplicitReuse.** The [prepared team bundle](../TestResults/confirmed-team-reuse-checked-20260922/teams.json) contains the explicitly selected `96b94357…` recipe and both original reference controls, `399bc776…` and `8287f779…`. All three exact recipes passed native preparation with the captured runtime, effective settings and content. This work ran **zero fights**, allocated **zero values**, and preserved all **550,367 exclusions**.

The selected recipe's confirmed win rate remains **76.15%**, versus **70.64% /64.16%** for its controls on the completed study's panel. Those are historical results from the [fixed-family confirmation](Tower-Practical-Fixed-Family-Confirmation-Execution.md), not new measurements.

## Ready-to-use files

| Input | File |
| --- | --- |
| Selected recipe, exact seed-free bytes | [96b94357… scenario](../TestResults/confirmed-team-reuse-checked-20260922/scenarios/96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c.json) |
| Designated incumbent control | [399bc776… scenario](../TestResults/confirmed-team-reuse-checked-20260922/scenarios/399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b.json) |
| Other reference control | [8287f779… scenario](../TestResults/confirmed-team-reuse-checked-20260922/scenarios/8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50.json) |
| All three teams, roles, subgroup mapping and required copies | [teams.json](../TestResults/confirmed-team-reuse-checked-20260922/teams.json) |
| Exact gameplay context | [scope.json](../TestResults/confirmed-team-reuse-checked-20260922/scope.json) |
| Explicit import request | [request.json](../TestResults/confirmed-team-reuse-checked-20260922/request.json) |
| Completed native compatibility check | [native-check.json](../TestResults/confirmed-team-reuse-checked-20260922/native-check.json) |
| Successful handoff receipt | [reuse.json](../TestResults/confirmed-team-reuse-checked-20260922/reuse.json) |

The source recipe, character identities, equipment, progression and canonical Essence order are preserved byte for byte. Slot 8 replaces Bark Golem with Viper, and slot 9 replaces Bark Golem with Flame Imp, relative to the designated incumbent. The validated result concerns the complete party. Its recipe does not certify account ownership or the separate effect of each substitution.

## Import and verification commands

The [preparer](analysis/prepare-confirmed-team-reuse.py) accepts a `tower-confirmed-team-reuse-v1` request with exactly six fields:

| Field | Meaning |
| --- | --- |
| `version` | `tower-confirmed-team-reuse-v1` |
| `archiveRoot` | Absolute path to the completed fixed-family confirmation archive |
| `archiveManifestSha256` | Independently retained SHA-256 of that archive's `files.json` |
| `candidatePartyId` | Explicit exact qualifying candidate ID; no inferred winner |
| `runtimeRoot` | Absolute path to the intended captured executable files |
| `contentRoot` | Absolute path to the intended content and `appsettings.json` |

From the repository root, these commands use the concrete [input request](../TestResults/confirmed-team-reuse-request-20260922.json). Preparation requires a new output directory; the completed package must not be overwritten.

```powershell
python -B 'Balance Harness/analysis/prepare-confirmed-team-reuse.py' prepare 'TestResults/confirmed-team-reuse-request-20260922.json' 'TestResults/confirmed-team-reuse-next'
```

To check the already prepared package without repeating native preparation:

```powershell
python -B 'Balance Harness/analysis/prepare-confirmed-team-reuse.py' verify 'TestResults/confirmed-team-reuse-checked-20260922' --manifest-sha256 84177584cafed74cdde9d6f4b51b986dda040affd410f47e799b2eabe882eb33
```

Preparation authenticates 14 consumed source artifacts against the caller-pinned manifest, requires the completed result and both saved audit receipts to agree, checks qualification and control membership, and copies the three authenticated seed-free exports. It does not reconstruct the 44,000-fight archive again. The source manifest pin is the trust input; a `Verified` field in an arbitrary JSON file is insufficient.

The [native helper](analysis/check-confirmed-team-reuse.ps1) loads the captured harness, compares its complete execution identity and effective settings, and prepares all three recipes with the production loader. Python checks all 26 retained executable files and 16 content files before native entry and rechecks inputs before publication. A local sentinel enables preparation only; it is never written into reusable recipes or reserved in history. A combat-entry guard is active. Framework dependencies are explicitly loaded at the captured .NET version and their file hashes are retained.

The helper runs in an owned Windows process tree with a 180-second preparation deadline and a 32-MiB retained-output bound. It publishes the success receipt only after all checks pass, then seals the package. The observed successful preparation took **1.781 seconds**; its process tree ended with zero active descendants. A failed preparation keeps its diagnostic files and has no successful handoff receipt. Verification checks the pinned package inventory and all consumed source/target pins without preparing parties again.

## Search compatibility and next integration

This is a **three-team input handoff**, with `searchRequest=false`. Existing `tower-practical-allocated-search-v1` accepts exactly two supplied teams; it cannot consume this bundle or a third reference. Its candidate counts, nomination rules, intervals, selectors and defaults were not changed. The selected recipe is an explicit input choice in this bundle, not an automatic new search primary or a deployment.

The [separately versioned three-reference practical search](Tower-Practical-Three-Reference-Search.md) and [captured-runtime native admission](Tower-Practical-Three-Reference-Admission.md) have now been exercised in the completed execution linked above. The preset retains all three supplied teams through nomination and confirmation and accounts for five nominees, up to four confirmation recipes and ten interval quantities. Execution preserved all 550,367 prior exclusions and added exactly 1,041. The original two-reference allocation version remains unchanged.

Compatibility was established for the captured runtime and content. The current `LL/src/API/API.LL/Data` files also matched all 16 captured content hashes during the reuse review; that alone does not establish compatibility of current development assemblies or effective settings. The historical fixed-family balance remains NotAssessed; the later practical search reports Fail for its captured floor-5 cohort. V19 remains Unresolved, and the separate 162 current-family context exceptions remain open. The executed run is closed; no further scientific run is queued.

## Verification and changed files

The [verification receipt](../TestResults/confirmed-team-reuse-verification-20260922/verification.json) records **11 passing Python tests**, successful native preparation of all three real recipes, and two native rejection checks for changed execution/settings identities. Fixture coverage includes changed source pins, nonqualifiers and controls selected as candidates, missing controls, altered recipes, changed runtime/content, existing outputs, mid-check changes, incomplete publication, unsafe member paths and altered package inventory. All 229 authoritative history file pins remain unchanged.

Two preliminary engineering preparations failed before recipe preparation: PowerShell first lacked the ASP.NET shared dependency, then attempted to inspect a native DLL as a managed assembly. Both records are retained at [first process receipt](../TestResults/confirmed-team-reuse-20260922/native-process.json) and [second process receipt](../TestResults/confirmed-team-reuse-final-20260922/native-process.json). The helper now loads the matching managed framework and records native DLL pins separately. Neither failure ran combat, allocated values or reopened the scientific study. Engineering work remains separately disclosed under the accepted accounting treatment.

Verification commands were the Python fixture suite, PowerShell syntax parsing, concrete preparation, pinned read-only verification, the two negative native checks, source/history hash checks and scoped Markdown/whitespace checks. No required command remains blocked. Backend tests were not rerun because no C# controller or gameplay code changed.

Changes are the Python preparer, PowerShell native helper, Python tests, this guide, the new evidence/package files, and current pointers in the execution/state reviews and two harness guides. There are no application configuration changes, migrations, database operations or deployments.
