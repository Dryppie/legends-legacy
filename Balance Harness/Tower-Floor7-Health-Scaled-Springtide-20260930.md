# Floor 7: target-Health Springtide trial — 30 September 2026

**Latest floor-7 casualty diagnostic (30 September):** The [target-Health casualty diagnostic](Tower-Floor7-Health-Casualty-Diagnostic-20260930.md) completed **64 exact historical replays / 96,067 audited events**, with **zero new acceptance fights or seeds**. Springtide caused **52/63 slot-1 deaths**; party output fell after those casualties. Native first-cast review found two relevant rules: the target-Health base bypasses source-Power Weaken, and switching the main attribute away from Power removes the engine’s **±20% magnitude variance**. Slot-1 noncritical raw hits rose from roughly **700–725 mean** in the prior logs to **958** with one healer or **1,195** with full Health/Regeneration. **79 fresh Python checks pass**; unchanged **148 backend passes / four skips** were authenticated and reused. Next: frozen, unallocated **17.5% / 20% / 22.5%** target-Health candidates, preserving **0.20 source Power per Abundance**, offense **0.60**, penetration **50**, all **120 recipes / eight compositions**, every gate and independent confirmation. Add native guards for the lower fractions, Weaken and variance behavior before testing. No candidate catalog/panel is materialized or allocated; no gameplay edit. Exclusions remain **920,956**. Floor 7 remains unresolved; floor 4 is the latest applied change. No dungeon or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [residual-pressure diagnostic](Tower-Floor7-Residual-Pressure-Diagnostic-20260930.md), preserving all previous accepted changes.

## Prospective protocol

Test exactly the three frozen plans in `TestResults/tower-floor7-health-scaled-springtide-proposal-20260930.json`: Springtide base **25%, 27.5%, then 30% of each target's Max Health**, with its original **0.20 source Power per Abundance**. From the original current-live source, set floor-7 offense factor **0.60** and penetration factor **50**. Do not stack factors onto the rejected penetration candidate. The existing isolated health-pressure contract permits only these two catalog changes and preserves every other ability, guardian field, floor, catalog and setting.

Preserve all **120 exact recipes / eight actual compositions**, including baseline, mixed resistance, healer, full Health + Regeneration and high-armor controls. Preserve actor/item identities, ordered Essences, positions and level-40 / five-Essence / Unique-Exceptional-rank-4 tier-1 budgets. The current catalog remains unchanged during screening and confirmation.

Before screening, run the existing Python candidate/application/family guards and ten native Springtide cases through `build/run-tests.ps1`. The native cases verify all three target-Health fractions, source-Power Abundance scaling and retention, current versus maximum target Health, mitigation, the shared penetration cap and barriers. Build a separate test runtime and require its production combat assemblies to match the original archive. Run the relevant broader backend regression in that runtime.

Run all three independent **128-seed screens** before selection, **15,360 fights each**. Each requires at least two distinct compositions with **at most eight specialized items on two characters**, simultaneous lower win-rate bounds of at least 10%, and every recipe's upper bound at most 50%. Use 95% Bonferroni-Wilson bounds across all 120 recipes: screen gate **25–44/128**. Select highest second-best eligible-composition lower bound, then smallest maximum upper bound, then lowest target-Health fraction. Independently confirm at most one passing candidate with **160 new seeds / 19,200 fights**, gate **30–57/160**. No fourth candidate, retry, extension or pooling. Maximum scope: **65,280 fresh fights / 544 reservations**, starting at **920,572 exclusions**.

Before each allocation, project twice the preceding measured time and archive size per fight. Require fewer than **672 projected seconds**, less than **80% of 2 GiB**, and at most **20,000 fights per panel**. Native limit **840 seconds**, owner limit **900 seconds**, archive limit **2 GiB**. Monitor only supervisor output while native files are active. Preserve every attempted panel and reservation.

Before interpreting each panel, check the first declared seed's prepared participants for every recipe against the original source: only guardian Power and both penetration values may differ, matching factors 0.60 and 50 within 0.002 float tolerance. Stored penetration is approximately 43.2; effective combat penetration is capped at 40. Independently check these preparations on every recorded fight and reconstruct the exact two-catalog delta. Recount raw outcomes, equipment qualification, intervals, selection, process/resource receipts and the disjoint seed union.

If confirmation passes, require isolated native input/full-replay parity before local application, then relevant regression and live application parity. If no candidate qualifies, close this bracket without a gameplay edit. No search, dungeon, acquisition, supply, migration, configuration, database or deployment work.

## Completed result

**Closed: `NoEligibleHealthSpringtide`. No candidate was selected, confirmed or applied.** All three independent screens failed the two-composition minimum. At 25%, the only winning recipe specialized one healer in composition A: **3/128**, adjusted **0.39%–12.75%**, against the required **25/128**. Every other recipe at 25%, and every recipe at 27.5% and 30%, won **0/128**. No ceiling failed; that does not make these overly difficult candidates acceptable. This rejects the declared bracket, not every possible target-Health design. Floor 7 remains unresolved; floor 4 remains the latest applied balancing change.

| Phase / target-Health base | Best two distinct limited-equipment compositions | Qualifying limited compositions | Ceiling failures | Largest adjusted upper |
| --- | --- | ---: | ---: | ---: |
| screen-1 / 25% | 3/128 (restorer-specialization); 0/128 (baseline) | 0 | 0 | 12.75% |
| screen-2 / 27.5% | 0/128 (mixed-resistance-baseline-slots-5); 0/128 (baseline) | 0 | 0 | 8.87% |
| screen-3 / 30% | 0/128 (mixed-resistance-baseline-slots-1-5); 0/128 (mixed-resistance-baseline-slots-1) | 0 | 0 | 8.87% |

All three screens retained **120 exact recipes / eight actual compositions**. The minimum/ceiling screen gates remained **25–44/128**, and the confirmation gates **30–57/160**. The two leaders above are distinct normalized compositions; equipment and reordered representations do not create additional compositions. Selection followed the prospective rule after all screens closed. No outcome pooling or sample extension.

### Equipment controls

| Phase / composition | Baseline | One healer | Full Health + Regeneration | Full Resistance + Health |
| --- | ---: | ---: | ---: | ---: |
| screen-1 / A | 0/128 | 3/128 | 0/128 | 0/128 |
| screen-1 / B | 0/128 | 0/128 | 0/128 | 0/128 |
| screen-2 / A | 0/128 | 0/128 | 0/128 | 0/128 |
| screen-2 / B | 0/128 | 0/128 | 0/128 | 0/128 |
| screen-3 / A | 0/128 | 0/128 | 0/128 | 0/128 |
| screen-3 / B | 0/128 | 0/128 | 0/128 | 0/128 |

A is original composition `dae6cc32…`, B `0d375ed9…`. These are descriptive per-recipe results within the complete corrected family, not additional samples or independent proof of a gear mechanism. The highest limited-equipment rows and these controls are all retained in the audit.

### Existing-report casualty comparison

Recounted all 128 saved reports for each of four recipes in the original 0.60-Power / 50-penetration screen and the new 25% screen: **1,024 existing reports, zero additional fights or replays**.

| Variant / composition / gear | Median first death | First-death counts by slot 1 / 2 / 3 / 4 / 5 | No original-party death | Mean guardian Health remaining |
| --- | ---: | --- | ---: | ---: |
| original-Power-base / A / restorer-specialization | 48.00s | 40 / 34 / 69 / 60 / 31 | 10 | 12.58% |
| original-Power-base / A / health-and-regeneration | 60.00s | 45 / 40 / 54 / 44 / 21 | 41 | 4.62% |
| original-Power-base / B / health-and-regeneration | 48.60s | 43 / 39 / 31 / 24 / 15 | 47 | 4.42% |
| original-Power-base / B / restorer-specialization | 48.00s | 39 / 23 / 55 / 41 / 20 | 17 | 13.41% |
| 25%-target-Health-base / A / restorer-specialization | 48.00s | 82 / 13 / 39 / 31 / 31 | 3 | 18.08% |
| 25%-target-Health-base / A / health-and-regeneration | 48.00s | 101 / 78 / 101 / 86 / 90 | 0 | 26.21% |
| 25%-target-Health-base / B / health-and-regeneration | 48.00s | 112 / 74 / 71 / 50 / 73 | 0 | 26.71% |
| 25%-target-Health-base / B / restorer-specialization | 48.00s | 102 / 17 / 49 / 24 / 34 | 0 | 21.49% |

Independent historical seed panels, not paired counterfactuals or new acceptance samples. First-death slot counts include every original actor tied at the earliest tick; full-fight totals depend on duration. Median first-death times exclude fights without an original-party death. Ties can make slot counts sum to more than the number of fights.

## Verification and build recovery

Completed **46,080 fresh fights / 384 reservations**, ending at **920,956 exclusions**. The independent collector reconstructed the exact abilities/Tower delta, recounted every raw result, checked native prepared participants in every fight, and reproduced equipment qualification, adjusted bounds, selection, resource admission and the disjoint seed union. All completed native panels had zero retries, no timeout and no remaining owned child.

**100 Python safeguards passed. 148 backend tests passed, including ten new Springtide cases; four intentional opt-in fixtures were skipped.** Backend tests ran through `build/run-tests.ps1`. The first build could not read the user NuGet configuration in the sandbox; its owned job closed at the 660-second bound with no children remaining. A permitted retry compiled and passed all 148 tests. Its rebuilt production DLL hashes differed from the archived benchmark, so those DLLs were excluded from balance work. Following the existing procedure, a separate runtime combines the newly compiled test assembly with the exact archived production assemblies. All 148 tests passed again in that runtime, and both native runtime copies match the original combat hashes. This was build recovery before any trial allocation, not a combat retry. All attempts and receipts are preserved. No verification command remains blocked.

The native guards cover all three target-MaxHealth fractions, current versus maximum target Health, original source-Power Abundance scaling and stack retention, mitigation, penetration and barriers. This uses the existing shared effect representation; it adds no per-guardian engine branch.

## Next work

**Next: the frozen, unallocated 64-replay casualty diagnostic**, using the rejected 25% candidate, both original parent compositions, one specialized healer and full Health + Regeneration, and the first sixteen declared saved seeds. Use `TestResults/tower-floor7-health-springtide-diagnostic-proposal-20260930.json`. Preserve all 120 recipes and every acceptance gate. No fresh acceptance fights, new seeds or next gameplay candidate are allocated. Before execution, add a separate plan validator and safeguard tests and perform measured resource admission.

The 1,024-report comparison shows a role shift: with one healer, slot 1 is tied for the earliest original-party death in **40 → 82/128** fights for A and **39 → 102/128** for B. Full Health + Regeneration falls from **62/128 and 66/128** to **0/128**, with no death-free fights in the new panel. These independent seed panels support a focused diagnostic, not a causal effect estimate. Inspect the damage that kills slot 1 and the resulting loss of party output before choosing another coefficient.

Source review identifies an interaction the prior static arithmetic omitted: `GetEffectiveAttribute(Power)` uses condition-adjusted Power, including **20% Weaken** and Power overtime/fury modifiers. A target-MaxHealth base bypasses source-Power adjustments; its separate Abundance bonus still uses effective source Power. The 25% change can therefore alter debuff interactions as well as Health-pool pressure. **This is a verified formula distinction and an unverified explanation of the observed failures.** Reuse the earlier event diagnostic with its Health-snapshot correction as context; its seeds differ. Do not infer a lower percentage or another damage mechanism is balanced without a new, separately declared trial after the diagnostic.

## Changed files and scope

Added the ten-case Springtide theory in `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`. Updated this report, the continuation handoff, balance status, gear coverage, preceding diagnostic notice and both harness guides. Study owners, independent audit, summary, scripts and verification receipts are under `TestResults/tower-floor7-health-springtide-*`. No gameplay catalog was changed by this trial. Expected progression gear, Essence budgets and the supported search remain unchanged. No migration, configuration, database or deployment changes.

Commands used bundled Python with `-B -X utf8`: prepare; Python tests; initial runtime verification; runtime recovery; archived runtime binding; driver; independent collector; descriptive summary; publication. Native command arrays, test filters, process receipts and TRX results are preserved in the corresponding owner directories. Publication checks local Markdown links and `git diff --check`.

## Evidence

- screen-1: `TestResults/tower-balance-pass-floor7-health-springtide-1-screen-study-20260929`, manifest **`e1c5f5f7939a45c4c6e78d84d1e8198141f4368be47f500bdcdfc6599cfb7c0a`**.
- screen-2: `TestResults/tower-balance-pass-floor7-health-springtide-2-screen-study-20260929`, manifest **`ad2fb7a86764d08901f936f8840018c153220ff792491a7221397023ae73051e`**.
- screen-3: `TestResults/tower-balance-pass-floor7-health-springtide-3-screen-study-20260929`, manifest **`b109034467ca6e3327033003d8b900f5af0657cf68d6792325cb817e3a40c11a`**.
- Independent audit: `TestResults/tower-floor7-health-springtide-evidence-20260930.json`, SHA **`2339d07d39cd597c366c1759ffc660182e14295f1f5830c11cb2edb932e05518`**.
- Descriptive comparison: `TestResults/tower-floor7-health-springtide-summary-20260930.json`, SHA **`7a13ece7cac827f7b2ba8c9785c24b464b9e3a363984369c6c34ea5b9f9f8b07`**.
- Reviewed continuation: `TestResults/tower-floor7-health-springtide-conclusion-20260930.json`, SHA **`0ffbf9aa757fa9f5ce278ebaf492741bd9cf2b3779329a31f30a1a8bbeea672a`**.
- Publication: `TestResults/tower-floor7-health-springtide-publication-check-20260930.json`.
