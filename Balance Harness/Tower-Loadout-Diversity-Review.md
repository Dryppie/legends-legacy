# V11 elite-loadout diversity: completed bounded pilot

Completed **13 September 2026**. The separately frozen `independent-loadout-diversity-v11` comparison used **9,224 fights**, with **zero retries or lost attempts**. The predeclared independent reliability gate is **Fail: 0/3 passing restarts, with 2 required**. 0 of 6 new-method finalists recorded a held-out win. The strongest saved control measured **131/256 (51.17%)**, with joint-adjusted interval **41.33–60.92%**. Ordinary and joint family assessments are **Fail / Fail**.

This is evidence for the declared search comparison, not a new complete Tower-family acceptance or a near-optimality result. All recipes and zero-win outcomes remain saved. Earlier studies and every observed ceiling breach remain separate. Boss settings, production source, catalogs and defaults are unchanged.

The strongest control’s **131/256 (51.17%)** is an observed ceiling breach. Its joint interval **41.33–60.92%** crosses 50%, so a repeatable rate above 50% is not established. The observation still prevents balance acceptance for this family; it is not a reason to retune Kharad or extend this sample.

## Frozen comparison and independent boundary

The [implemented rule](Tower-Loadout-Diversity-Implementation-Review.md) preserves `stagger-reservation-joint` as the comparator. Only `loadout-diversity-joint` changes the four-member elite parent selector: first representatives of distinct per-character unordered loadouts in existing combat rank, then unselected ordered recipes in rank order to fill capacity. Slot identity, exact IDs and quantities remain part of the signature. Ordered recipes keep distinct measurements/cache identities, and ordinary final ranking is unchanged. Both methods retain v10 construction, reservation counts, providers, operators and the existing exploration/recombination algorithms. Separate method RNG streams are paired on combat schedules; this is not an identical-parent operator ablation.

The [protocol](../TestResults/balance/tower-loadout-diversity-20260913/protocol.json) and [precombat plan snapshot](../TestResults/balance/tower-loadout-diversity-20260913/implementation-plan.md) froze three paired restarts, 96 evaluated parties per arm and eight shared fresh discovery seeds. Before held-out outcomes, the top two recipes per arm were selected with unchanged ranking: rank one is primary and rank two exploratory. Six saved controls and the fixed anchor `team-1abe76ca1891d97a91d484f0a3662048` remain outside generation. Controls cannot supply recipes, IDs, counts, ancestry, fitness or validation features to the independent search. Reference removal/reordering preserved the exact generation input hash.

The pilot excluded all **471,925** prior seed reservations and added **269** disjoint reservations: three generation, eight discovery, 256 validation and two unused stage seeds. The [all-array ledger](../TestResults/balance/tower-loadout-diversity-20260913/seed-ledger.json) now contains **472,194** distinct seeds. Unused reservations remain excluded.

## Results and uncertainty

All 576 independent complete parties were measured on their eight discovery seeds. The following validation results use 256 disjoint shared seeds per exact recipe. Primaries and the anchor were not replaced after observing outcomes.

| Method | Restart seed | Primary wins | Primary joint interval | Secondary wins |
| --- | ---: | ---: | ---: | ---: |
| `stagger-reservation-joint` | -124577593 | 0/256 | 0.00–3.84% | 0/256 |
| `loadout-diversity-joint` | -124577593 | 0/256 | 0.00–3.84% | 0/256 |
| `stagger-reservation-joint` | -1868210019 | 0/256 | 0.00–3.84% | 0/256 |
| `loadout-diversity-joint` | -1868210019 | 0/256 | 0.00–3.84% | 0/256 |
| `stagger-reservation-joint` | 2142977178 | 0/256 | 0.00–3.84% | 0/256 |
| `loadout-diversity-joint` | 2142977178 | 0/256 | 0.00–3.84% | 0/256 |

| Saved control | Wins | Observed rate | Joint-adjusted interval |
| --- | ---: | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 131/256 | 51.17% | 41.33–60.92% |
| `team-38248d838d1db9634fd82536c177df0a` | 90/256 | 35.16% | 26.35–45.10% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 74/256 | 28.91% | 20.80–38.64% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 72/256 | 28.12% | 20.12–37.81% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 57/256 | 22.27% | 15.11–31.55% |
| `team-693ffa8ec0b654154a06722aba06a968` | 41/256 | 16.02% | 10.02–24.62% |

The [independent analysis](../TestResults/balance/tower-loadout-diversity-20260913/analysis.json) verifies all raw schedules, compact records, summaries, journals and frozen recipe associations. Joint nominal alpha .05 is split .025 over the full 18-cell rate family and .025 over six paired comparisons; each discordance component uses .025/12. The ordinary evaluator separately uses .05/family. Wilson coverage is approximate; repeated closed studies are not pooled and provide no lifetime confidence guarantee.

A restart passes only if its new primary has adjusted rate lower bound at least 10%, paired improvement lower bound above zero against the same-restart comparator, and paired difference lower bound at least minus ten percentage points against the fixed anchor. At least two of three must pass. Secondary outcomes cannot substitute for the primary.

The pilot records **1 validation observations above 50%** and **0 discovery observations above 50%**. Any such observation remains a ceiling breach even when its interval crosses 50%. Intended-cohort viability and complete-family coverage remain required independently of search reliability.

## Observational diagnostics

The predeclared [saved-prefix audit](../TestResults/balance/tower-loadout-diversity-20260913/loadout-findings.json) covers **333 parent decisions** without invoking a constructor or battle runner. Each row uses only that arm’s already completed discovery measurements. The comparator’s diversity alternative is local arithmetic, not a simulated v11 history. Full C# archive reconstruction verifies the actual parent choices.

| Method | Restart seed | Parent decisions | Ordinary beam with duplicate loadouts | Applied beam with duplicate loadouts | Distinct evaluated loadouts / ordered recipes |
| --- | ---: | ---: | ---: | ---: | ---: |
| `stagger-reservation-joint` | -124577593 | 56 | 6 | 6 | 89/96 |
| `loadout-diversity-joint` | -124577593 | 57 | 31 | 0 | 90/96 |
| `stagger-reservation-joint` | -1868210019 | 55 | 50 | 50 | 90/96 |
| `loadout-diversity-joint` | -1868210019 | 55 | 21 | 0 | 89/96 |
| `stagger-reservation-joint` | 2142977178 | 55 | 23 | 23 | 90/96 |
| `loadout-diversity-joint` | 2142977178 | 55 | 27 | 0 | 90/96 |

The [reservation audit](../TestResults/balance/tower-loadout-diversity-20260913/reservation-findings.json) checks both methods’ saved count/placement traces; [new-finalist ancestry](../TestResults/balance/tower-loadout-diversity-20260913/trace-findings.json) preserves all six new-arm finalist associations. Counts and signature diversity do not establish combat equivalence, causality or a provider’s marginal benefit. The [four fixed detailed replay summaries](../TestResults/balance/tower-loadout-diversity-20260913/replay-diagnostics.json) match their compact records exactly. The three new primaries and fixed anchor used the first validation seed, chosen before outcomes; no adaptive replay was added.

## Verification and preservation

The frozen allocation was 4,608 discovery + at most 4,608 validation + eight repeated diagnostic fights, capped at **9,224 fights**, **600 execute seconds**, **1 GiB per package**, **512 MiB per campaign**, and **zero retries**. The completed pilot used 4,608 discovery fights, 4,608 validation fights and eight diagnostics in **252.30 execute seconds** (248.74 measured phase seconds). Both full campaigns reconstructed without additional battles; four historical detailed reports and four new detailed summaries matched.

No production or test source changed. The sealed implementation’s **264/264** backend result is reused after verifying all 2,239 C# source hashes and five producing assemblies; backend tests were not rerun. The local pilot driver built with zero warnings/errors. Its first source-free restore encountered sandbox denial reading the user NuGet configuration; the scoped approved restore succeeded. No commands remain blocked. The [verification commands](../TestResults/balance/tower-loadout-diversity-20260913/verification-commands.json) record the exact calls.

The [final integrity receipt](../TestResults/balance/tower-loadout-diversity-20260913/final-verification.json) preserves all **20 prior packages and 78 sealed reviews**, verifies current/frozen content and both catalogs, and checks all current documentation links and `git diff --check`. The [validated-builds artifact](../TestResults/balance/tower-loadout-diversity-20260913/validated-builds.json) saves all 18 seed-free recipes with their source associations, including unsuccessful finalists. It does not promote them to a shared catalog.

Kharad remains **Health 3.04881408 / Power 3.85370128**: ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, exact fixed gear, no styles/contributions and all 80 eligible Essences under hypothetical ownership. Practical acquisition remains unverified. No gameplay changes, migrations, configuration changes or deployments were made.

## Next boundary

Prepare a bounded zero-combat diagnosis of the saved v11 campaign before selecting another search hypothesis. Reuse every saved discovery/validation record and the four fixed detailed replays; distinguish parent occupancy, ordered variants, inherited reservations and realized combat behavior. Keep attribution, targeting, chance gates and survivorship limits explicit. No constructor rerun, additional replay, fitted recipe/count, new policy or campaign is selected. Keep controls outside independent generation. Do not retune Kharad, strengthen gear, promote defaults/catalogs or expand floors from this pilot. Preserve the new observed ceiling breach as unresolved; any fresh confirmation must have its own separately frozen allocation and must not pool this pilot or earlier studies.
