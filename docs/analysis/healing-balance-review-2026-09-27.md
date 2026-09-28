# Healing balance candidate — 27 September 2026

Originally implemented and tested as an opt-in candidate. The user subsequently selected it for local use and manual alpha deployment: the checked-in host settings now select combat 18, equipment 4 and `healing-v1`. Existing inventories and running services were not changed; see the [rollout handoff](attribute-redesign-rollout.md). The evidence supports reducing free healing, but does **not** establish that the entire healing/barrier system is now balanced.

## Changes

| Setting | Baseline | Candidate |
|---|---:|---:|
| Herb Mixture | 210% Power / 14 seconds | 105% Power / 14 seconds |
| Treant Sapling: Sprouting Surge | 250% Power / 16 seconds | 125% Power / 16 seconds |
| Restoration equipment cost | 3 budget per point | 1.5 budget per point |
| Restoration formula | `1 + Restoration / 100` | Unchanged |
| Restoration effective cap | 300 | Unchanged |

The old Sprouting Surge tooltip said **300%**, while its actual base effect was **250%**. Both tooltips now use `{scaling}`, which the existing frontend formatter resolves from the effect coefficient, including ascension scaling.

No changes to targeting, cleanse triggers, cooldowns, critical eligibility, other healing coefficients, barriers, regeneration or life steal. The engine still rolls healing magnitudes by ±20%, then applies Restoration, applicable crits and receiving modifiers. Essence ascension still scales heals and cooldowns. Thus 105% and 125% describe the base coefficients, not guaranteed displayed combat amounts.

## What the equipment price buys

At rank 0, Standard quality and baseline rolls, an ordinary one-unit Restoration specialization buys:

| Rarity / allocation | Release 3 | Release 4 |
|---|---:|---:|
| Common, one unit | 10 | 20 |
| Epic, one unit | 16 | 32 |
| Epic, two-handed weapon | 32 | 64 |

Three ordinary Epic pieces give 96 Restoration; an Epic staff plus two ordinary Epic pieces gives 128. These are specialized pieces, so the purchase replaces other specialization stats. Slot restrictions, core stats, total budgets and every other price remain unchanged.

At equal Power and modifiers, 100 Restoration recovers the **old zero-Restoration** output of the two nerfed heals. It does not restore an existing healer's previous output:

| Investment in tested Epic builds | Old Restoration | New Restoration | New heal per cast / old heal per cast |
|---|---:|---:|---:|
| None | 0 | 0 | 50.0% |
| Ring + necklace | 32 | 64 | 62.1% |
| Six eligible pieces, including staff | 112 | 224 | 76.4% |

Ignoring rounding and the cap, that ratio is `0.5 × (1 + 2r/100) / (1 + r/100)`, where `r` is the old Restoration on identical equipment. Untouched healing and authored barriers receive the cheaper stat without the coefficient reduction; at 112 → 224 Restoration their per-cast multiplier grows by about 52.8%.

## Measured results

The main study ran **5,832 production-engine fights across 47 fixtures**, with four exploration seeds and 32 separate confirmation seeds declared before execution. PvP runs both orientations and treats each seed's two orientations as a statistical cluster. Builds, ordered essences, budgets and seeds are identical across releases. Equipment 3 / baseline abilities is compared with equipment 4 / `healing-v1`. Each entire encounter uses its side's release, including opponents and allies.

Coverage includes individual and combined heals, zero/two/six Restoration pieces, damage hybrids, Duelist/Conduit/Bastion, three-versus-three support, unchanged Forest Spirit healing, Wood Nymph barriers, early Common builds, single dungeon boss encounters, Tower Floor 1 and region-two idle encounters. All essence loadouts are level 1 and unascended; unit tests cover ascension 3 separately. Pure heal/barrier fixtures deliberately isolate a small ability set and are not optimized full loadouts.

Selected confirmation results:

| Fixture | Baseline | Candidate | Interpretation |
|---|---:|---:|---|
| Both heals, zero Restoration, mean effective healing | 2,794 | 959 | Less healing per cast and fewer casts before death; this is not a per-cast ratio. |
| Damage hybrid, zero Restoration, wins | 64/64 | 61/64 | Hybrid remains strong in this opponent matchup. |
| Damage hybrid, six Restoration pieces, wins | 64/64 | 43/64 | Substantial reduction from the old stacked healing build. |
| Three-versus-three support, six Restoration pieces, wins | 2/64 | 13/64 | Cheaper Restoration strengthens mixed support, including unchanged healing and barriers. |
| Wood Nymph barrier-only, six pieces, absorbed damage/fight | 988 | 1,797 | Barrier build survives longer and absorbs more; requires separate review. |
| Early idle, each of four variants, clears | 32/32 | 32/32 | These single encounters remain completable. |

Approximate paired 95% intervals, clustering mirrored seeds: zero-Restoration damage hybrid win change **−4.7 percentage points [−9.8, +0.4]**; six-piece damage hybrid **−32.8 [−47.1, −18.5]**; six-piece party support **+17.2 [+7.7, +26.6]**. These intervals concern the fixed fixtures, not all players or all builds; multiple fixtures were inspected and no population acceptance thresholds were assumed.

The isolated one/two-heal builds lost every high-level physical matchup under both releases. Their healing/survival measurements are useful; their win rates cannot establish healer viability. The Tower party lost all trials under both releases. Dungeon and region-two fixtures cleared all trials; region-two fights ended before these heals mattered. Those outcome ceilings/floors are explicitly inconclusive for tuning.

### Early PvP diagnostic

All four early PvP variants with the Vial timed out at 600 seconds in both releases. A subsequent **576-fight diagnostic** removed only the relic from both builds, using the same seed schedule. Without Restoration, Herb Mixture changed from 64/64 draws to 32/64 draws; Sprouting Surge changed from 64/64 to 63/64. With two Restoration pieces, both still drew 64/64.

Removing the regeneration relic exposes some of the nerf but does not resolve early-game stalemates. Low damage, defenses, base regeneration and healing together need a separate balance pass. This follow-up was selected after inspecting the main study; it is exploratory evidence, not a new independent confirmation sample.

### Decision

The study's recommendation was to keep the candidate inactive pending review of unchanged heals/barriers and early-game stalemates. The user subsequently chose to include the healing changes in the rollout configuration. It achieves the intended reduction in uninvested healing and makes Restoration much easier to buy; the observed limitations still apply. No additional global healing multiplier or automatic regeneration/barrier adjustment was made.

No real player data is needed to reproduce these calculations or fights. A release decision still benefits from progression, acquisition and full-party coverage beyond these fixed fixtures. Route attrition, every evolved/ascended combination, maximum-quality/cap builds and live build-selection behavior were not simulated here.

## Selection and rollout

Ability coefficients and equipment prices are separate selectors. The candidate combination is:

```json
{
  "AttributeRedesign": { "LiveVersion": 18 },
  "EquipmentBalance": { "LiveVersion": 4 },
  "Combat": { "AbilityBalanceProfile": "healing-v1" }
}
```

These are now the checked-in settings for the selected rollout. Services use them after rebuild/restart unless environment configuration overrides them. Combat 17 / equipment 1 remains the fallback when host configuration is absent; combat 18 without an explicit equipment selector continues to select equipment 2. Omitting the ability profile retains baseline coefficients. All combat/catalog/award hosts must select the same intended release and restart through the manual deployment process; the catalog is loaded at startup.

The shared ability catalog applies the coefficient changes to **creatures using these ability IDs as well as player essences**. Selecting equipment 4 only affects newly awarded equipment; existing gear retains its recorded release until explicit migration. Activating the weaker heals before converting old Restoration gear would temporarily give those players the nerf without the cheaper stat.

No new EF migration is required by this candidate. Equipment conversion uses the previously implemented versioned receipt flow and its existing schema prerequisite. No database migration, inventory conversion or deployment was performed. Rollback must account for both selectors and any completed item conversions; simply deselecting the ability profile while retaining release-4 gear would combine old strong heals with cheap Restoration.

## Changed files

- `LL/src/API/API.LL/Data/combat/ability-balance.healing-v1.json`: two candidate ability definitions.
- `LL/src/API/API.LL/Data/combat/abilities.json`: two dynamic tooltip descriptions; baseline coefficients unchanged.
- `LL/src/API/API.LL/Data/equipment/equipment-*.v4.json` and `equipment-releases.json`: separate price release, retaining release 3.
- `JsonAbilityCatalogProvider.cs`: optional validated profile loading; normal catalog validation and compilation still apply. Rejects incompatible rules, unsafe IDs, empty/duplicate/unknown replacements and ownership/kind changes.
- `LL/tools/BalanceHarness/OfflineContent.cs` and `AttributeAllocationStudy.cs`: independent profile selectors, frozen content hashes and manifest fields. Profiles are part of the decision whether two sides can share a provider.
- `HealingBalanceCandidateTests.cs`, `AttributeAllocationStudyTests.cs`, `EquipmentMigrationTests.cs`: coefficient/ascension/multiplier/catalog isolation, budget/price, frozen profile and 3→4 migration coverage.
- `LL/tools/BalanceHarness/Fixtures/healing-balance-candidate.json` and `docs/analysis/healing-balance-review.py`: reproducible bounded fixtures and evidence summary.
- This report and `docs/analysis/equipment-rebalancing.md`: findings, selector and release documentation.

## Verification and reproduction

- Focused candidate, catalog and migration tests: **42 passed** initially.
- Broader combat, tooltip, equipment, ascension, combat-style and harness accounting regressions through `build/run-tests.ps1`: **503 passed** (`TestResults/healing-regressions.log`).
- After adding the explicit 3→4 migration regression: final focused run **43 passed** (`TestResults/healing-final-tests.log`).
- Release JSON comparison confirms that only the version and Restoration price differ from release 3; styles, sets and named content match.
- Main study: `TestResults/healing-balance-candidate-20260927-v2`; follow-up: `TestResults/healing-early-without-relic-20260927`. Each contains frozen requests, content hashes, build snapshots, trials, paired estimates and first-seed replays. `healing-summary.md` contains all fixture rows.
- Existing unrelated compiler warnings remain. No verification remains blocked. The frontend test suite and database rehearsal were not rerun because no frontend logic or schema was changed.

The first study attempt stopped during preflight because ordinary Common definition IDs have no `.rarity.common` suffix; the generator was corrected before any fights started. Unit-test expectations were also corrected to account for the engine's pre-existing magnitude variance. The successful evidence is in the `v2` directory.

From the repository root, with Python and .NET available, use fresh output paths:

```powershell
python docs/analysis/healing-balance-review.py prepare TestResults/healing-new-request.json --output TestResults/healing-new
dotnet run --project LL/tools/BalanceHarness --configuration Release -- attribute-allocation-study TestResults/healing-new-request.json
python docs/analysis/healing-balance-review.py summarize TestResults/healing-new

python docs/analysis/healing-balance-review.py prepare-no-relic TestResults/healing-no-relic-request.json --output TestResults/healing-no-relic
dotnet run --project LL/tools/BalanceHarness --configuration Release --no-build -- attribute-allocation-study TestResults/healing-no-relic-request.json
python docs/analysis/healing-balance-review.py summarize TestResults/healing-no-relic

./build/run-tests.ps1 -Filter 'FullyQualifiedName~HealingBalanceCandidateTests|FullyQualifiedName~AttributeAllocationStudyTests|FullyQualifiedName~EquipmentMigrationTests'
```

This machine used the bundled Python executable at `C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe`. Harness commands were executed against the Release assembly built by the test wrapper. Generation and execution refuse to overwrite existing artifacts.
