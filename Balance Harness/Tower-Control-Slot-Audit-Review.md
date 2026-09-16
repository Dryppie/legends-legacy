# Control-triple slot audit: protection filled the remaining slot

Completed 15 September 2026. The saved completion trace explains why Enchanted Fairy did not join Pack Howler / Spider Queen — Royal Venom / Venomous Spiderling: **the selected group also reserved Cave Bat, leaving one slot; completion processed protection first and filled that slot with Skeleton on all five group owners.** Recurring control was still missing when its turn arrived, but every owner was full. Fairy's recurring-control tag was present and correct; family and owned-copy restrictions were not the obstacle.

This closes the specific construction question from the [trajectory review](Tower-Group-Completion-Trajectory-Review.md). It does not establish that Fairy would make this team stronger. No new teams were constructed or fought.

## Saved teams and slots

Both policies encountered the same four-Essence reserved group at proposal/evaluation 7, on character slots **1, 3, 6, 8 and 9**. Each character started with Cave Bat plus the triple, occupying four of five Essence slots. Here, five owners means five characters in a ten-character Tower lineup comprising two five-player parties; it does not mean multiple copies of an Essence on one character.

| Saved policy | Evaluated teams carrying the triple | Characters carrying it | Fifth Essence on those characters | Distinct carrier loadouts | Discovery rank / 44 | Saved mean guardian health remaining |
| --- | ---: | ---: | --- | ---: | ---: | ---: |
| Diversity | 1 | 5 | Blue Slime on 1, 3, 8; Rotfly Toad on 6, 9 | 2 | 40 | 94.325% |
| Completion | 1 | 5 | Skeleton on all five | 1 | 39 | 93.3625% |

All 88 evaluated recipes and all 92 proposals were inspected, including their saved results. There were no other evaluated triple carriers, and no evaluated character carried the full four-Essence shared control set. The two controls carry the triple on seven characters each; the full shared set appears on seven and six respectively. The earlier trajectory audit also found zero recorded module uses carrying the triple. These saved discovery health observations do not estimate the effect of changing a fifth Essence.

## The exact completion boundary

The saved category order was `protection → recovery → enemy-pressure → attack-enabler → recurring-control`. The trace records one insertion step: Skeleton, selected from **11 compatible protection providers**, on all five owners. The following table applies to each of them.

| Category reached | Free slots on entry | Existing coverage relevant to this step | Recorded addition |
| --- | ---: | --- | --- |
| Protection | 1 | None | Skeleton fills the fifth slot |
| Recovery | 0 | Royal Venom | None |
| Enemy pressure | 0 | Cave Bat and Spiderling | None |
| Attack enabler | 0 | Pack Howler | None |
| Recurring control | 0 | None | None: no free slot |

Fairy is tagged for **enemy pressure and recurring control**. At the initial prefix, it was legally compatible for the missing recurring-control category on all five owners: one free slot, a distinct family and no owned-copy restriction. It was not a protection provider and therefore was not among the 11 choices in the step that actually ran. Its pressure route was already excluded by Cave Bat/Spiderling coverage. When each of Fairy's own categories was reached, slots were full: **10 owner/category encounters**, all blocked by capacity, with the five pressure encounters additionally blocked by existing category coverage. Those ten encounters concern five characters, not ten candidates.

This refines the prior observation that none of 18 unchosen shared-provider events could complete the shared set. That audit examined alternatives within recorded insertion categories. Fairy was not an alternative *protection* provider; the opportunity existed before the last slot was allocated to protection. Changing a tie among those protection providers would not select Fairy.

In the diversity baseline, generic coverage reservations selected Blue Slime first, followed by Hobgoblin, Rotfly Toad, Hobgoblin — Brutal Charge, and Web Weaver Spider. Fairy was not selected as a category provider. The final carrier loadouts contain Blue Slime or Rotfly Toad and no recurring-control provider. Those reservations preserve aggregate requested/satisfied counts, but not each recipient's insertion sequence; this audit does not invent that missing sequence.

The source boundaries are [completion's missing-category and capacity checks](../LL/tools/BalanceHarness/TowerGroupCompletionSearch.cs), [group reservation before completion and filler](../LL/tools/BalanceHarness/TowerGroupCountSearch.cs), and [direct coverage extraction/generic filler](../LL/tools/BalanceHarness/TowerPartyCoverage.cs). Captured and checkout hashes are retained in the [freeze record](../TestResults/balance/tower-control-slot-audit-20260915/freeze.json).

## What the authored categories capture

All **66 direct coverage features across 80 allowed Essences** matched the captured ability definitions and both policies' saved metadata exactly. The category extractor behaved as written. These broad tags do not measure targeting quality, timing, reliability or strength.

| Essence | Coverage tags | Captured authored distinction |
| --- | --- | --- |
| Enchanted Fairy | Enemy pressure; recurring control | Corrosion targets a random enemy. Fae's Charm targets all enemies with 80% Stun chance, authored stagger power 40, and an interval trigger with both cooldown and initial delay of 200 ticks. |
| Pack Howler | Attack enabler | Coordinated Attack makes non-summoned allies perform basic attacks; cooldown 150 ticks. |
| Royal Venom | Recovery | The recovery tag comes from Royal Cocoon's conditional, one-use self-heal, with a self-stun/timer sequence. The active Royal Venom applies a self status; direct coverage extraction does not traverse that status. |
| Venomous Spiderling | Enemy pressure | Venom Web directly applies Poison and Slow to the current target. Its basic-attack poison uses EventTarget and a Slow condition; that passive effect is outside this direct selector list. |
| Cave Bat | Enemy pressure | Sonic Screech applies Slow to the current target. Its pressure tag overlaps Spiderling's, although the Essences have different damage/passive effects. |
| Skeleton | Protection | Calcium applies Guard(4) to Self. It supplies no recurring-control tag. |

Full ability definitions for the shared set and all accompanying Essences on control/generated triple owners are retained in [13 authored profiles](../TestResults/balance/tower-control-slot-audit-20260915/authored-abilities.json). Values above are authored inputs, not observed proc rates or combat uptime. No status engine, ability or gameplay content was changed.

## Catalogue reach and next implementation

Seven of the existing 214 catalogue groups contain the triple. The bare triple leaves two slots; each of its six four-Essence supersets leaves one. Their extra Essences are Cave Bat, Web Weaver Spider, Bog Mite, Grave Wisp, Viper and Grave Hound. **Only the Cave Bat superset was visited in either saved prefix.** The bare triple was available in the catalogue but was not tried. No catalogue group contains all four shared-control Essences. This is limited sampling plus competition for remaining slots, not a missing triple definition.

The next scoped implementation should let a reserved group with scarce remaining slots receive a small, deterministic set of **alternative missing-category allocations**, with metadata-based scheduling across groups. It should preserve existing group reservations, slot/family/copy caps and fixed ability execution order, fit within a declared proposal budget, and avoid special-casing Fairy or the control recipe. Start with zero-combat fixtures proving that a single remaining slot can explore each competing missing category and that the existing policy remains reproducible. Provider choice within an already chosen category is a separate issue; the evidence here does not justify hard-coding a stronger provider or changing Kharad.

This audit implements only evidence readers. The alternative allocation policy is the next code task, not an adopted change or a fresh-seed/fight authorization. Its combat value, the value of trying the bare triple, and general search reliability remain unresolved. Detailed machine-readable loadouts, gates, frontiers and catalogue visits are in [the analysis](../TestResults/balance/tower-control-slot-audit-20260915/analysis.json), with [independent verification](../TestResults/balance/tower-control-slot-audit-20260915/verification.json).


## Verification, measurements and reproduction

The nine frozen Python fixtures and independent saved-data verification passed: 92 proposals, 88 evaluations, 25 recorded owner/category frontiers, 80 authored Essence profiles and 66 exact category/evidence features. Both policies match captured authored coverage. All 14,103 indexed files across 34 predecessor packages verified unchanged before and after analysis. No backend source changed; the previous 103 tests through `build/run-tests.ps1` and four metric fixtures remain sealed and were not rerun.

Diagnostic time before publication: **4.251 seconds**, including one conservative second charged for a schema-inspection error before freeze. That inspection shadowed a path variable and produced no diagnostic findings; its record is retained. No frozen check failed or was retried. Carried time: **1181.991 seconds**; carried output: **1,592,999,474 bytes**. The [completion receipt](../TestResults/balance/tower-control-slot-audit-20260915/completion.json) records final measured publication time plus a one-second seal allowance, cumulative time and package bytes. Caps remain 120 seconds / 32 MiB new and 1,800 seconds / 4 GiB cumulative. Zero fights, seeds, preparations, replays and retries.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-control-slot-audit-20260915'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" fixtures
& $python -B "$work/workflow.py" analyze
& $python -B "$work/workflow.py" verify
& $python -B "$work/publish.py"
```

These commands ran once against the [frozen protocol](Tower-Control-Slot-Audit-Protocol.md). Reproduction requires a separately frozen package with carried accounting; do not rerun sealed evidence. Changed files: the protocol and review, six active Markdown handoffs and this isolated evidence-reader package. No gameplay, search-policy, configuration or migration changes; no deployment implications. Unrelated dirty files are preserved.

Preserve **482,596 reservations**, v19's unused 512 and all 253 required recipes. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, adoption **Hold**. No fresh-value approval remains, no ability-order tuning, Kharad changes or 129,536-fight confirmation.
