# Protection compatibility: implementation and bounded comparison

The opt-in **`independent-compatible-defense-v9`** completed its frozen comparison against unchanged v8. Independent reliability is **Fail (0/3 passing restarts; 2 required)**. **0 of 6 new-arm finalists recorded a held-out win.** Ordinary and joint-adjusted assessments of the 18-cell tested family are **Inconclusive** and **Inconclusive**. The strongest saved control measured **126/256 (49.219%)**, with joint-adjusted interval **39.45–59.04%**. This observation does not erase the earlier 131/256, 136/256 and 134/256 ceiling breaches; the studies remain separate.

This closes one isolated applicability change and one bounded comparison. It does not establish complete Tower-family acceptance, near-optimality or practical acquisition. All ten preceding packages, previous reliability failures, observed ceiling breaches and the separate **479/1,000 (47.90%) Inconclusive** confirmation remain intact. No samples, restart gates or secondary results were pooled to replace a failed primary.

## Implemented behavior and source scope

[TowerProtectionCompatibility.cs](../LL/tools/BalanceHarness/TowerProtectionCompatibility.cs) derives a conservative incoming damage-type set from the target guardian's native abilities, nested statuses, summons and their abilities. Unarmed Tower guardians and attacking summons contribute Physical; direct and periodic Damage retains its authored type; Poison, Burn and Bleed conditions contribute their engine types. Every owner effect is visited regardless of whether its trigger or predicate fired in a replay. References are overapproximated, so potential damage types are retained without frequency weights. Event and condition identity records do not execute effects; applied conditions are checked at their effect sites.

Unsupported executable operations, unresolved nodes, inherited/ambiguous damage types and unsupported damage-producing conditions mark the audit incomplete. An incomplete or empty threat set preserves the full v8 protection collection. This deliberately avoids guessing delegated, reflected, converted or random damage semantics. It makes no assertion that unsupported future encounters have only the resolved types.

Only an explicitly typed, fixed negative `ModifyDamageTaken` protection route can be excluded, and only when every evidence route qualifies and every type is absent from the complete threat set. Generic, matching, mixed, attribute-based, barrier, cover, condition-based and uncertain routes remain eligible. The actual Kharad audit is **Physical + Magical**, reducing protection nominations **16 → 14** and total coverage **71 → 69**. Poisonous Rat and Smolder Rat lose only their protection nomination; all **80 Essences** remain legal through other categories, uniform/ordinary construction and normal mutation. These identities are audit output, not a source allowlist.

[TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs) accepts exactly `["defense-joint", "compatible-defense-joint"]` for v9. [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) saves optional compatibility metadata, omitted from v1–v8 serialization. [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) validates the complete partition of unchanged v8 coverage and selects filtered coverage only for the new arm, including coverage-aware substitutions. [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) restricts `fresh-compatible-defense` provenance to the independent new arm with no parents or reference ancestry.

Both arms preserve v8's construction/refinement base: 50% core-insertion skip, uniform route, provider/count sampling among eligible entries, category order, family/ownership checks, filler weights, final ordering, operators, ranking and beam/exploration. V7's unconditional insertion is not combined with this experiment. Separate method names use separate deterministic RNG streams; this compares policies and is not an identical-parent causal ablation. The preceding diagnosis found the two nominations in 79/127 fresh v8 parties but none of its three final primaries, so this change never claimed to explain those final defeats.

## Verification before pilot combat

The [preflight](../TestResults/balance/tower-protection-compatibility-20260913/precombat-verification.json) verified all ten prior packages, 69 sealed reviews, current content, both catalogs and four unchanged gameplay assemblies. The new harness has its own producing hash. All **196 relevant regression tests passed** through `build/run-tests.ps1`: prior policy serialization/behavior, conservative unknown fallback, typed/general/mixed routes, nested/periodic/condition/summon damage, deterministic ordering and casing, scarce ownership, all established floor/slot budgets, legal uniform reachability, immutable inputs, provenance, ordinary/compact reconstruction and interrupted/resumed parity.

The first test run caught an overly broad fallback on identity-node timing caveats; executable effects and non-executing identity metadata were distinguished before freezing. The failed test receipts and final passing receipt are retained. No pilot combat occurred while resolving this issue.

All [six historical v8 arms](../TestResults/balance/tower-protection-compatibility-20260913/v8-comparator-reconstruction.json) reconstructed exactly without combat. The [three v9 comparator arms](../TestResults/balance/tower-protection-compatibility-20260913/v9-comparator-verification.json) then matched the corresponding old v8 arms exactly. The other probe arms used **288 synthetic zero-win evaluations**, on already excluded generation seeds, solely for compatibility. They added no battles or seeds and never entered fresh search evidence. Removing or reversing all six references left independent generation inputs identical. The **48 cores, 66 original coverage entries and 71 v8 entries** all matched the sealed baseline.

## Frozen allocation and decision

Target: offline BalanceHarness, Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. Budget: ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, exact fixed gear, no styles or contributions. The full 80-Essence pool includes Rare under hypothetical ownership; acquisition remains unverified.

The [protocol](../TestResults/balance/tower-protection-compatibility-20260913/protocol.json), [design](../TestResults/balance/tower-protection-compatibility-20260913/experiment-design.json) and [copied plan](../TestResults/balance/tower-protection-compatibility-20260913/implementation-plan.md) froze before pilot combat. Protocol SHA-256: `f276a80ae457404c8d3b37da4b9789a0e3cc2729d9f626ab35ec2a7dde527d48`.

| Phase | Frozen allocation | Fights |
| --- | --- | ---: |
| Discovery | 2 methods × 3 restarts × 96 parties × 8 shared fresh seeds | 4,608 |
| Validation | Top 2 per arm and all 6 controls, 18 distinct recipes × 256 fresh seeds | 4,608 |
| Fixed diagnostics | 4 historical parity replays, then 3 new primaries and original anchor on first validation seed | 8 |
| **Total** | **No retries or optional extension** | **9,224** |

The cap was 9,224 fights, 600 execute seconds, 1 GiB per package and 512 MiB per campaign, with at most 2,048 proposals per arm. The [ledger](../TestResults/balance/tower-protection-compatibility-20260913/seed-ledger.json) excludes **471,387 historical seeds from every array**, including unused and constructor-only reservations. The 269 fresh reservations are mutually disjoint: 3 generation, 8 discovery, 256 validation and 2 unused stage seeds. The latest all-array exclusion union is **471,656**. No constructor tracing was performed.

Each rank-one primary and rank-two exploratory finalist was [frozen before validation](../TestResults/balance/tower-protection-compatibility-20260913/validation-selection.json). Reliability requires at least two of three new primaries to satisfy adjusted rate lower ≥10%, paired lower improvement over the same-restart **v8 defense primary** >0, and paired lower difference from original anchor `team-1abe76ca1891d97a91d484f0a3662048` ≥−10 percentage points. No secondary or alternate control may replace these comparisons.

Joint nominal alpha .05 splits .025 over the final rate family and .025 over six paired comparisons; discordance bounds use component alpha .025/12. The ordinary evaluator separately uses .05 divided by the full family. Wilson coverage is approximate, with no lifetime repeated-study guarantee, pooling or held-out reranking. Any observed rate >50% remains a breach; each intended-budget cohort needs supported ≥10% viability.

## Held-out results

Wins out of 256, primary / secondary:

| Generation seed | Unchanged v8 defense | Compatible defense | New primary gate |
| ---: | ---: | ---: | --- |
| -988509643 | 0 / 0 | 0 / 0 | Fail |
| 1383973713 | 0 / 0 | 0 / 0 | Fail |
| -1693253229 | 0 / 0 | 0 / 0 | Fail |

Joint-adjusted intervals; difference columns are percentage points:

| Generation seed | New primary rate | Difference vs v8 primary | Difference vs fixed anchor |
| ---: | ---: | ---: | ---: |
| -988509643 | 0.00–3.84% | -3.57–3.57% | -38.67–-17.84% |
| 1383973713 | 0.00–3.84% | -3.57–3.57% | -38.67–-17.84% |
| -1693253229 | 0.00–3.84% | -3.57–3.57% | -38.67–-17.84% |

| Saved control | Wins | Observed rate | Joint-adjusted interval |
| --- | ---: | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 126/256 | 49.219% | 39.45–59.04% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 75/256 | 29.297% | 21.14–39.04% |
| `team-38248d838d1db9634fd82536c177df0a` | 83/256 | 32.422% | 23.90–42.29% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 67/256 | 26.172% | 18.43–35.75% |
| `team-693ffa8ec0b654154a06722aba06a968` | 44/256 | 17.188% | 10.95–25.95% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 64/256 | 25.000% | 17.42–34.50% |

All cells, six paired comparisons and every gate component are retained in [analysis.json](../TestResults/balance/tower-protection-compatibility-20260913/analysis.json). Observed validation ceiling breaches: **0**; discovery observations above 50%: **0**. The legacy `coverageCharacterCounts` field retains the original 66-entry classification for historical comparison.

Search evaluated **576 parties from 590 proposals**. [Compatibility findings](../TestResults/balance/tower-protection-compatibility-20260913/compatibility-findings.json) describe continued ordinary reachability of the two Essences and verify collective substitutions against the filtered categories. This is descriptive adaptive search evidence, not a causal mutation estimate or a reason to add another feature. [Trace findings](../TestResults/balance/tower-protection-compatibility-20260913/trace-findings.json) retain the six new-arm recipes and ancestry. [Replay diagnostics](../TestResults/balance/tower-protection-compatibility-20260913/replay-diagnostics.json) retain the four preselected new detailed replays and all held-out summaries without additional battles.

## Accounting, commands and handoff

All **9,224 fights completed**, with **zero retries or lost attempts**, in **254.82 seconds** of measured phases (**258.42 seconds** for execution). About **353.4 MiB** was retained before final documentation and receipt. Both campaigns reconstructed without combat, four old detailed reports matched, and all four new detailed summaries matched compact records. Independent manifests, journals, schedules, recipes, frozen selection, native intervals and resource accounting passed; the [final receipt](../TestResults/balance/tower-protection-compatibility-20260913/final-verification.json) records exact hashes and final bytes.

Verification commands completed: `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore`; `build/run-tests.ps1 -NoBuild -Configuration Release` with the [13-class filter](../TestResults/balance/tower-protection-compatibility-20260913/regression-filter.txt); offline driver restore/build, `Driver.dll freeze` and one `Driver.dll execute`; Python `-B` preflight, analysis, replay/trace/compatibility checks and final verification; `git diff --check`. The [test receipt](../TestResults/balance/tower-protection-compatibility-20260913/regression-tests.trx) records **196/196 passing**. Backend build had five existing unrelated warnings and no errors; the driver build had zero warnings/errors. Initial sandbox denial reading NuGet configuration was resolved with scoped approved restore using the source-free configuration. No required command remains blocked.

Changed files: five harness sources linked above; new [BalanceHarnessTowerProtectionCompatibilityTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerProtectionCompatibilityTests.cs) and the existing discovery/bulk test files; this review, active plans/policy/handoffs and README. All **18 complete seed-free recipes** and measurements are saved in [validated-builds.json](../TestResults/balance/tower-protection-compatibility-20260913/validated-builds.json), without catalog promotion. Gameplay content/source, fixed gear and defaults remain unchanged. There are **no migrations, configuration changes, database changes or deployments**.

This experiment is closed. Do not extend its sample, retry it or automatically launch another hypothesis. Further diagnosis can use the saved construction and replay evidence without new combat; no next experiment is frozen. Retuning, default/catalog promotion and floors 6–11 remain deferred. Use the [active handoff](Tower-Coverage-Replication-Plan.md) for the latest status and exclusion union.
