# Equipment balance releases

Equipment prices, allocation shares and authored weights can now change without changing combat formulas or rebuilding historical items. The game loads an exact equipment release; existing items retain their recorded stats and release until an operator explicitly converts them.

## Available releases

| Equipment release | Combat rules | Content | Penetration price |
|---|---|---|---|
| 1 | 17 | `*.legacy-v1.json` | Historical physical 3.5 / magical 4 |
| 2 | 18 | Existing `*.v1.json` | 1.5 for both |
| 3 | 18 | Separate `*.v3.json` | 4 for both |
| 4 | 18 | Separate `*.v4.json`; Restoration price 1.5 | 4 for both |

Release 3 contains the reviewed penetration price. The checked-in host configuration now selects combat 18 and equipment release 4, which retains that price. The 40 percentage point penetration cap remains a combat rule. Running services and existing inventories still require the [manual rollout procedure](attribute-redesign-rollout.md).

Release 4 copies release 3 and halves Restoration's price from 3 to 1.5. The user selected it with the `healing-v1` ability profile; both are now explicit in host configuration. Test results and remaining balance limitations are documented in [the healing review](healing-balance-review-2026-09-27.md). Equipment release selection does not automatically select an ability profile.

`LL/src/API/API.LL/Data/equipment/equipment-releases.json` lists releases. Release 3's `equipment-starters.v3.json` contains the complete ordinary attribute price table under `balance.attributeCosts`. Higher prices buy fewer points for a fixed budget. Its `balance.coreShare` is 0.7, leaving 0.3 for specialization. `balance.identityShare` is 0.1; it reserves part of the 0.15 style bonus for set identity. Existing item/core/style weight dictionaries still describe how each allocation is distributed. Tier, rarity, quality, rank and roll multipliers still apply. Caps retain the existing overflow rules.

As requested on 28 September, set attribute bonuses grant fixed authored modifiers once their piece thresholds are met. The internal identity reservation still accounts for the set's share of item power, but no longer scales the granted modifier. A subsequent request restored the original set bonuses and concise descriptions, using Restoration, equivalent Ability Haste and Tenacity where attributes were renamed. Phoenix now grants **8% Restoration**. Original percentage-of-total modifiers also return, such as Warden's **+10% total Max Health**, rather than the intermediate flat 108.11 Health. More pieces unlock later thresholds without amplifying already active modifiers. This is a shared combat/content correction, not a new item-price release; existing item descriptors require no conversion. The release-2/3/4 set catalogs use the same restored definitions. Old simulation results retain their original code/content identity and should not be presented as measurements of the revised behavior.

Item displays use **Gear Power** and omit the internal profile/allocation breakdown. See [the fixed set-bonus change record](fixed-set-bonuses-2026-09-28.md) and [the final restored bonuses](restored-set-bonuses-2026-09-28.md).

## Creating the next balance

Run from the repository root:

```powershell
./build/new-equipment-balance.ps1 -Version 5 -SourceVersion 4
```

This copies the four content files, updates the starter header and registers the candidate. It refuses to overwrite an existing release. Edit the **new** starter's prices, core/specialization weights and shares, and its style weights as needed. Every new release must explicitly price all ordinary equipment attributes. Prices must be finite and positive; core share must be between zero and one, and identity share cannot exceed the style budget.

Keep released files and their registry entries immutable. Copy a release to change it; retain older files for upgrades, archived comparisons and rollback. Releases 1 and 2 have fixed filenames for compatibility with old harness archives. Routine rebalance keeps definition IDs, archetypes, rarity, native styles and specialization IDs compatible. A removed definition blocks conversion instead of guessing a replacement. Adding new attributes, changing legal slots, combat caps/formulas, set-proc behavior or the tier curve is still a code/content design change, not an attribute price adjustment. Do not change existing set-proc IDs/behavior as part of a price release: historical equipped sets share that combat-facing identity.

## Comparing releases

Use the existing `attribute-allocation-study` harness command. Add these fields to a study request:

```json
{
  "referenceRulesVersion": 18,
  "referenceEquipmentBalanceVersion": 2,
  "candidateEquipmentBalanceVersion": 3,
  "allowBudgetChanges": false
}
```

The request must still contain its content/output directories, cells, disjoint exploration/confirmation seeds and maximum battle count. Use identical reference/candidate builds to isolate a release change. Opponents and allies also use their side's release, so this measures the release across the encounter. The harness freezes and hashes both releases' files, records both versions and budgets, and executes the production combat engine. Raw diagnostic exchanges are priced against the selected release. Omit diagnostic exchanges when comparing ordinary authored equipment.

```powershell
dotnet run --project LL/tools/BalanceHarness --configuration Release --no-build -- attribute-allocation-study LL/tools/BalanceHarness/Fixtures/equipment-release-comparison.json
```

The included `equipment-release-comparison.json` is a small 32-fight execution smoke check using identical physical and magical builds across releases 2 and 3. Change its output directory before rerunning. Expand its encounter coverage and seeds for a balance decision.

If changing total base/style budgets intentionally, set `allowBudgetChanges: true`; otherwise unequal nominal budgets are rejected. Keep this distinction visible when interpreting results. Study results are evidence for the tested fixtures, not automatic approval to activate a release.

## Previewing, applying and rolling back existing gear

The SuperAdmin LiveOps routes remain under `/api/liveops/equipment-migration`:

1. `GET audit?sourceBalanceVersion=2&page=0&pageSize=100` lists matching instances and unclaimed dungeon rewards. `sourceBalanceVersion`, `matchingInstances` and `matchingPendingRewards` identify the requested cohort; older `legacy*` count fields remain as compatibility aliases. Unversioned gear remains a separate blocker.
2. `POST preview` accepts `target`, optional `definitionId`, and `targetBalanceVersion: 3`. The response includes frozen before/after descriptors, source/result hashes, budgets, and mapping details.
3. `POST apply` sends a fresh `operationId`, the same target, `sourceHash`, the preview's `after.state.definitionId`, `targetBalanceVersion`, and `expectedResultHash` from the preview's `resultHash`. Reuse the same operation ID and request to retry an uncertain response. Explicit rebalances require the result hash; changed source or target stats require another preview.
4. `POST {operationId}/rollback` restores the frozen before descriptor. Roll back the most recent receipt first. Changed/reinforced/traded items or a used specialization choice require reconciliation; rollback does not overwrite subsequent progression.

Later releases preserve the current definition/specialization, ownership, identity, tier, rarity, quality, rank, roll and styles. Only the initial legacy attribute conversion creates a specialization choice. An unused, still-eligible choice carries forward; ordinary rebalances create no new choice. A choice uses the item's current equipment release, and older superseded receipts cannot spend it.

Run the HTTP rehearsal against an isolated local copy:

```powershell
./build/rehearse-equipment-migration.ps1 -BaseUri http://127.0.0.1:55442 `
  -OutputDirectory <new-evidence-directory> -UseDevelopmentOperator `
  -SourceBalanceVersion 2 -TargetBalanceVersion 3 -ApplyAndRollback
```

Omit `-ApplyAndRollback` for previews only. The script records each request before applying, verifies retries and hash-exact rollback, and does not activate a release. Quiesce item mutations/combat for any operator conversion window; existing command transactions, locks and scheduled-combat guards remain enforced. Historical combat snapshots are not rewritten.

## Deployment implications

The additive EF migration `20260927132322_VersionEquipmentRebalances` adds receipt revisions and a carried choice allowance. It preserves existing legacy receipts' allowance and replaces the single-active-receipt index with uniqueness per item/revision. Its schema downgrade requires rolling back later equipment conversions first. The migration must be applied through the normal authorized deployment workflow before running the updated migration service; this implementation does not apply it to a shared or production database.

`EquipmentBalance:LiveVersion` independently selects newly awarded equipment. The host JSON now explicitly selects 4. When the selector is omitted, combat 17 selects equipment 1 and combat 18 selects equipment 2. Selecting equipment 3 or 4 requires `AttributeRedesign:LiveVersion=18`. All award-producing hosts must use the same configuration. Selection is read at startup; publish the content and restart the affected hosts through the normal release process. Environment overrides take precedence over JSON.

Existing gear remains frozen and upgrades through its original catalog. Converting existing items is a separate explicit operator action. No new EF migration or bespoke conversion script is needed for a routine later price/weight release.

## Verification

- Backend regression through `build/run-tests.ps1`: **427 passed, one expected PostgreSQL skip**. Logs: `TestResults/equipment-rebalance-regression.log`.
- Final affected checks after tightening the carried-choice guard: **60 passed**. Logs: `TestResults/equipment-rebalance-final-tests.log`.
- Disposable PostgreSQL rehearsal through `build/run-postgres-equipment-rehearsal.ps1`: **one integration test passed**, including existing receipt backfill, transaction interruption, concurrent retries, successive conversion of both inventory and pending rewards, out-of-order rollback rejection, exact original restoration and schema Down/Up. Log: `TestResults/equipment-rebalance-postgres.log`.
- Included release-comparison fixture: **32 production-engine fights completed**. Both loadouts retained equal nominal budgets; their Epic two-handed weapons changed from 40 to 24 penetration, with cap overflow repriced accordingly. Frozen inputs, hashes, builds and replays: `TestResults/equipment-release-comparison-20260927`.
- `new-equipment-balance.ps1` smoke-tested in TEMP: creates release 4, preserves source files and rejects overwrites. HTTP rehearsal script passes PowerShell syntax parsing.
- Game API Release build passed. `dotnet ef migrations has-pending-model-changes ... --configuration Release --no-build` reports no pending model changes. `git -c core.safecrlf=false diff --check` passed.

No commands remain blocked. The full unrelated backend suite and frontend suite were not rerun for this backend-only change. The updated HTTP rehearsal script was syntax checked; the repeated conversion behavior was exercised directly against real PostgreSQL, not through a newly launched HTTP host. No shared database migration, production conversion or release activation was performed.

## Changed file groups

- `EquipmentBalanceSettings`, `EquipmentBalance`, allocator/evaluator and JSON catalog/provider: release configuration, exact version resolution and selectable prices/shares.
- `AttributeRulesSelection`, service registration, upgrade policy/service: independent award selection and upgrades using the item's recorded release.
- Equipment migration domain, service, repository, CQRS requests and LiveOps controller: version filters, result hashes, repeated receipts, preserved specialization, carried credit and reverse rollback.
- `20260927132322_VersionEquipmentRebalances`, designer, model snapshot and receipt configuration: additive receipt schema/index changes.
- `equipment-releases.json` and four `*.v3.json` content files: inactive penetration-price candidate.
- Harness `OfflineContent`, `AttributeAllocationStudy`, diagnostics and the new comparison fixture: frozen release-to-release studies using the production engine.
- Release creation/rehearsal scripts and the migration, upgrade, allocation and PostgreSQL tests: repeatable authoring and verification.
