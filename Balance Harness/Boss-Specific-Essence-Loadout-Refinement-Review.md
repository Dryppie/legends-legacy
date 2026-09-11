# Boss-specific Essence loadout refinement — 10 September 2026

The bounded refinement found a five-Essence Eydis specialist whose discovery-frozen primary cleared **40/40 fresh confirmation fights**, compared with **2/40** for the strongest retained target control. Nhalia remains unreliable at seven Essences: its frozen primary cleared **1/40**, while the highest observed finalists reached **6/40**. These results use unchanged production combat and fixed progression budgets; they do not establish optimal builds or a generally superior search method.

The implementation adds all 96 historical pilot finalists as portable Tower Lab references, local refinement around explicit historical anchors, boss-specific replacement diagnostics, separate healing/regeneration observations and reference filters. The [implementation guide](Boss-Specific-Essence-Loadout-Implementation.md) describes the code and workflow. [Three complete reviewed recipes](Boss-Refinement-Recipes-20260910/README.md) preserve exact ordered Essences, identities, equipment and all-floor weaknesses.

## Frozen experiment

Both studies, their anchors, all controls, methods, budgets and combat schedules were fixed before the first fight. There were no new screening probes or adaptive follow-up studies. Each method/restart evaluated every shared retained control plus **64 new complete parties**, with 12 discovery samples. Finalists, the primary winner, method/restart winners and strategy labels froze using discovery alone. Every finalist then received 40 fresh samples on every released floor and effective ally context. Separate diagnostic seeds measured a predeclared four-party replacement quartet, with 12 matched samples per party.

| Study | Fixed target party and budget | Retained controls | Finalists | Effective floor/context cells per finalist | Actual study fights |
| --- | --- | ---: | ---: | ---: | ---: |
| Eydis, floor 7 | Five characters; five Essences each; level 40; Uncommon Standard, tier 1, rank 2 | 25 | 42 | 24 | 53,184 |
| Nhalia, floor 13 | Ten characters; seven Essences each; level 60; Uncommon Fine, tier 2, rank 3 | 36 | 53 | 17 | 50,488 |

All Essences retain the existing level-1, unascended/unevolved assumptions and hypothetical ownership. Character identities, training, gear and ally contexts are fixed. Both context files are byte-identical to the original pilot. Lower floors use their actual required party size; later participants outside a study's mutable positions retain their authored builds. Identical resulting contexts share trials and count once.

The four methods are uniform legal random exploration, the legacy proposal pattern under the boss objective, joint refinement and graph refinement. Guided attempts use the frozen anchor and discovery parents, with every fourth attempt sampling globally. Joint proposals can change two characters; graph refinement also starts from the three predeclared mechanism substitutions. Random exploration retains its original distribution and random-number sequence for identical starts and seeds. All arms pay the same actual discovery cost within a study.

Generation seeds were `-1251957636`, `1817198187` and `698122098`. Each study reserved 624 distinct combat seeds. Each definition contains 16,136 exclusion integers: a conservative 15,512-value union from captured historical seed ledgers/trials, plus the other study's 624 reserved seeds. This is local captured-source coverage, not an account-wide seed registry or a count of independently observed historical fights. Both studies' actual schedules were disjoint, as were discovery, confirmation and diagnostic schedules. Historical observations nominate anchors and remain separate from fresh evidence, including their different execution identities.

## Eydis: strong target confirmation, substantial transfer costs

The frozen primary is `68bcf1597a0cba4906df7986ec66583bfb2b4036963b0ebfa7168a20362e3873`, proposed by joint refinement at generation seed `1817198187`.

| Fresh target observation | Wins | Pointwise Wilson 95% interval | Mean remaining guardian health | Mean party survival |
| --- | ---: | --- | ---: | ---: |
| Frozen primary | 40/40 | 91.2–100.0% | 0.00% | 98.50% |
| Historical anchor `e568dfaabbb8…` | 2/40 | 1.4–16.5% | 33.94% | 3.50% |
| Other 24 retained controls | 0/40 each | 0.0–8.8% each | See complete report | See complete report |

Against the anchor on the same 40 seeds, the primary gained 38 victories and lost none. Its mean victorious duration was 74.295 seconds. The anchor's earlier **2/20** remains a separate historical sample. All 17 new finalists won at least three fresh trials; four reached 40/40. The primary was frozen before those confirmation outcomes, rather than chosen from that four-way observed maximum.

Relative to the anchor, the primary changes member 1's Forest Spirit to Poisonous Rat, and member 3's Web Weaver Spider, Blood Harpy and Green Slime to Enchanted Fairy, Rainbow Slime and Illusion Fox. Member 3 retains Bog Mite. Exact positions and complete gear are in the reviewed recipe.

Target success does not make this an all-floor replacement. On balanced floor 8 it improved from the anchor's 9/40 to 34/40, but another retained recipe cleared 40/40. On balanced floor 9 it improved from 36/40 to 39/40. On floor 11 it fell to 0/40 in both effective contexts, compared with the anchor's 1/40 and 8/40. The largest loss against any retained control was **0/40 versus 36/40** in the alternate floor-11 context, with zero gained and 36 lost paired victories against `126553034bac…`. Five non-target cells had a lower clear count than at least one retained control. The recipe guide preserves every floor and context; its provenance record and campaign analysis also retain draw counts.

## Nhalia: rare clears, with a separate exploratory candidate

The frozen primary is `7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e`, proposed by joint refinement at generation seed `-1251957636`.

| Fresh target observation | Wins | Pointwise Wilson 95% interval | Mean remaining guardian health |
| --- | ---: | --- | ---: |
| Frozen primary | 1/40 | 0.4–12.9% | 30.19% |
| Historical anchor `a4eaec8b4a9a…` | 0/40 | 0.0–8.8% | 34.40% |
| All 36 retained controls | 0/40 each | 0.0–8.8% each | See complete report |
| Exploratory finalist `813457e85da4…` | 6/40 | 7.1–29.1% | 28.35% |

Nine of the 17 new finalists recorded at least one victory. Two finalists reached 6/40: `813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1` and `2ef1b65bfa0b7e7cb1a77660ac2bf8122bfb670e6e5e8074c876772d0f2ae20a`. They differ only in member 1's Blue Slime/Royal Venom ordering and have identical recorded outcomes, progress, behavior and fitness across all declared confirmation cells. They are one observed behavioral result here, not two independent replications; their samples must not be pooled.

Both recipes were in the discovery-frozen finalist set, but neither was the frozen primary or a method/restart winner. Highlighting their 6/40 result after confirmation is exploratory selection. It does not replace the predeclared primary result or support an adjusted success-rate claim. A future study can freeze one as a historical hypothesis and test it on unused seeds.

Relative to the historical anchor, the reviewed exploratory recipe replaces Plague Ghoul and Venomous Snake with Royal Venom and Thornback Boar on member 1, and changes member 10's Hobgoblin/Goblin Archer positions to Goblin Archer/Goblin Warrior. Full ordered parties are preserved in the recipe guide. The primary and exploratory recipe both lose the retained best's two victories on floor 14: **0/40 versus 2/40**. This study does not justify increasing production difficulty or changing healing rules.

## Equal-cost method observations

Each entry is the fresh target clear count out of 40 for that method/restart's **discovery-frozen winner**, in the three generation-seed order given above. A method can select a shared retained control.

| Method | Eydis frozen winners | Nhalia frozen winners |
| --- | --- | --- |
| Random | 2, 14, 3 | 0, 0, 0 |
| Legacy proposal pattern | 27, 26, 19 | 2, 0, 0 |
| Joint refinement | 19, 40, 38 | 1, 0, 0 |
| Graph refinement | 39, 38, 26 | 0, 1, 0 |

Every Eydis arm used 1,068 discovery fights; every Nhalia arm used 1,200. The difference between studies is their retained-control count. Equal cost applies to discovery evaluations; confirmation uses a shared frozen finalist allocation. Rejected/duplicate proposals cost no fight. Random receives the same retained warm controls as guided methods, and its Nhalia winner is the same retained recipe at all three restarts. Restarts share paired combat schedules, so their outcomes cannot be summed into larger independent samples. These two bosses and three restarts provide descriptive evidence, with complete paired method comparisons retained, without establishing method superiority.

## Mechanism diagnostics and recovery limits

Schema 2 freezes the diagnostic before discovery and checks typed activation, target, condition and summon routes. A and B are legal whole-Essence replacements; compatibility fields named enabler/consumer identify those replacements and do not prove an enabler/consumer relationship. Every variant below cleared **0/12** separately reserved matched trials. The win-rate interaction contrast was zero in both studies.

Eydis A withdraws member 3's existing Bog Mite healing-denial package for Skeleton pressure. B replaces member 2's Blue Slime recovery/barrier package with Gnoll Shaman's summoned all-allied barrier. These substitutions alter multiple effects; they are not isolated Wound or barrier interventions.

| Eydis diagnostic | Guardian health | Friendly reported healing | Friendly effective regeneration | Guardian reported healing |
| --- | ---: | ---: | ---: | ---: |
| Anchor | 36.89% | 2,303.50 | 3,954.25 | 1,968.92 |
| A | 44.40% | 2,332.75 | 4,088.83 | 2,265.67 |
| B | 34.56% | 1,443.17 | 3,881.58 | 1,696.42 |
| A + B | 40.52% | 1,444.58 | 4,004.00 | 1,944.08 |

Nhalia A replaces member 7's Blue Slime with Gnoll Shaman. B replaces member 10's Treant Guardian regeneration/barrier package with Rotroot Shambler pressure. Other healing providers, including Blue Slime on members 1 and 8, plus equipment/base regeneration, remain active.

| Nhalia diagnostic | Guardian health | Friendly reported healing | Friendly effective regeneration | Guardian reported healing | Guardian effective regeneration |
| --- | ---: | ---: | ---: | ---: | ---: |
| Anchor | 35.34% | 6,187.58 | 8,000.08 | 2,861.17 | 11.33 |
| A | 36.97% | 4,945.83 | 7,680.42 | 2,547.42 | 11.83 |
| B | 34.28% | 6,052.67 | 7,908.42 | 2,815.42 | 4.00 |
| A + B | 35.59% | 5,511.33 | 7,719.08 | 2,670.25 | 4.25 |

Values are means per target fight. Lower observed recovery and boss feedback did not produce diagnostic clears. Nhalia's primary actually reports more friendly healing, regeneration and guardian healing than its anchor while gaining one victory. Raw recovery totals do not establish a simple monotonic relationship with success.

`HealingDone` is a qualified engine-reported measure: direct, periodic and lifesteal paths do not uniformly measure effective restoration. Regeneration is reported separately as effective restoration. Aggregates cover initial participants and omit summons; schema-1 historical recovery fields remain unavailable, not zero. Barriers, healing totals and guardian recovery are observations, not deaths prevented or causal attribution. The retained detailed diagnostic audit separates event-level measurements and attribution gaps.

The [detailed mechanism audit](../TestResults/balance/tower-boss-refinement-20260910/analysis/mechanism-audit.md) reconstructed the first reserved seed for each of the eight quartet variants; all were defeats, and all 32 event/summary recovery reconciliations passed. Eydis logged 6/0/8/0 guardian Wound applications for anchor/A/B/A+B, consistent with withdrawing Bog Mite in A. The new slot-2 Totemic Ward barrier providers were recorded while the original slot-1 wards remained. Different defeat durations, unavailable active-stack reconstruction and missing counterfactual healing prevent a causal healing-suppression or death-prevention estimate.

Nhalia's logs retain adjacent direct/periodic healing, lifesteal and regeneration followed by Undertow healing in every variant. For example, the baseline shows regeneration of 64 followed by guardian healing of 13 at tick 99, and lifesteal of 16 followed by guardian healing of 3 at tick 140. These sequences are consistent with the authored feedback route; the logs lack causal parent-event IDs. Another 9/7/9/9 guardian feedback events in anchor/A/B/A+B have no immediately adjacent qualifying recovery event and remain unattributed. These eight repeated replays add mechanism observations, not independent win-rate samples.

## Verification and reproducibility

The completed package is `TestResults/balance/tower-boss-refinement-20260910/`. It retains the producing executable, production content and harness catalog snapshots, source snapshot, definitions, schedules, proposals, frozen selection, every trial and all 95 exact target recipes. Generated analysis includes 57,636 canonical finalist/control comparison rows, all method comparisons, every diagnostic seed and recovery observations.

| Accounting stage | Actual fights |
| --- | ---: |
| Discovery | 27,216 |
| Fresh all-floor confirmation | 76,360 |
| Diagnostic quartets | 96 |
| Normal-Tower exported primary recipe checks | 80 |
| Detailed replays | 25 |
| **Total / predeclared maximum** | **103,777 / 103,832** |

There were no cache hits, probes, retries or uncharged combat checks. The 55 remaining fights were unused. Replays reproduce existing seeds and do not add independent confirmation evidence. Both frozen primaries reproduced all 40 target fights through ordinary Tower recipe execution. Detailed replays cover the eight first-seed quartet variants and observed target/transfer victories, defeats, draws and transfer regressions. The exploratory Nhalia recipe has native archive/recipe reconstruction but was not included in the 80-fight normal-Tower primary check.

Both new archives reconstructed completely. The current reader also reconstructed all three schema-1 pilot studies and the implementation smoke, preserving 39,632 historical archived fights with no new simulations. **66 focused backend tests passed** through `build/run-tests.ps1`, including references, budget/identity validation, diagnostics, deterministic proposal behavior, cancellation, exports and replay. The build had zero errors and five pre-existing unrelated test warnings. The later presentation-only build passed with zero warnings/errors; JavaScript syntax and scoped whitespace checks passed.

The final seal checks every campaign file, archive inventory, frozen input hash and actual-combat reservation. It also checks all 44,807 checksum-listed files from the original pilot, all 16 current production content files and the original producing source/executable. `seal.json` records the manifest checksum; `verification/preservation.json` records independent accounting and the separately captured CSS change.

Browser checks covered retained-reference counts of 25/35/36, historical evidence, recipe export, filters and completed results. The actual 42-finalist Eydis result displayed 17 new parties, 25 references, one anchor, all recovery rows and four diagnostic variants without JavaScript errors. Initial native verification of that large archive took approximately 197 seconds; this remains a responsiveness limitation. Expanded diagnostic notes initially overflowed a 390-pixel viewport. One CSS wrapping rule fixed it, verified at 390- and 1,440-pixel widths. That presentation change is captured separately from the immutable producing source/executable, so experimental provenance remains exact.

Run read-only package verification from the repository root:

```powershell
# Use an available Python 3 executable.
python TestResults/balance/tower-boss-refinement-20260910/audit-seal.py --verify

# Validate saved normal-Tower/replay parity without executing more combat.
dotnet TestResults/balance/tower-boss-refinement-20260910/verification-driver/runner/Audit.dll verify TestResults/balance/tower-boss-refinement-20260910

# Reconstruct one complete study with its producing executable.
dotnet TestResults/balance/tower-boss-refinement-20260910/executable/BalanceHarness.dll tower-boss-verify --run TestResults/balance/tower-boss-refinement-20260910/studies/floor-7-slots-5
```

Do not rerun write modes inside a sealed package. Deterministically repeating its seeds reproduces evidence; it does not create a fresh confirmation sample. New experiments need new directories, frozen inputs and unused combat schedules that exclude this campaign as well as earlier work.

Changes are confined to the offline harness, its tests and local documentation/artifacts. Production content and combat remain unchanged. No migrations, configuration changes, database actions or deployment are required. No verification command remains blocked.

## Follow-up justified by these results

Retain the Eydis primary as a target specialist alongside its floor-11 counterexamples. Freeze one Nhalia 6/40 ordering as a new historical hypothesis before testing it on fresh seeds, and preserve the failed recovery substitutions as evidence. Kodoku's four-slot pilot result remains historical; this campaign did not rerun it. Further search, cross-boss method replication and dashboard archive-loading performance are separate work, rather than reasons to extend this completed campaign after viewing its outcomes.
