# Floor 10: limited armor equipment comparison — 1 October 2026

## Completed result

**Floor 10 still has a large equipment gap in this diagnostic.** All **245 recipes within the eight-item/two-character budget won 0/32**. The two historical winners using sixty specialized items across all fifteen characters won **5/32 and 2/32**. All 38 original controls were retained; the 240 new variants covered every one-character and two-character subset of both winners. No gameplay value changed and no confirmation was attempted.

| Composition | Equipment | Wins | Mean guardian Health remaining | Mean reported duration |
| --- | --- | ---: | ---: | ---: |
| c01a33fe… | baseline | 0/32 | 40.22% | 55.38s |
| c01a33fe… | eight-item armor | 0/32 | 34.65% | 60.31s |
| c01a33fe… | full armor | 5/32 | 8.86% | 74.06s |
| 52889519… | baseline | 0/32 | 39.46% | 57.06s |
| 52889519… | eight-item armor | 0/32 | 34.76% | 61.03s |
| 52889519… | full armor | 2/32 | 11.09% | 75.00s |

The best limited recipes by wins, remaining guardian Health and cell ID specialize slots **12 and 14** for `c01a33fe…`, and **2 and 3** for `52889519…`. Both use eight items. Baseline parties leave approximately 39–40% guardian Health, these limited recipes approximately 35%, and full armor approximately 9–11%. These are descriptive whole-fight comparisons, not proof of which attack or mechanic causes the difference. Zero wins in this sample does not establish impossibility. The 32-seed diagnostic neither replaces the old independent confirmation nor establishes current limited-equipment acceptance; the old 72/256 and 68/256 observations are not pooled into it.

## Verification and implementation

- Independently recounted all **9,728 historical floor-10 outcomes**, authenticated the preceding floor-9 publication, and retained every raw recipe and identity. Historical qualification matched **9,728 inputs / 38 full replays** on current catalogs and the corrected production runtime. The entire **278-recipe family** prepared natively without allocating seeds.
- The fresh panel completed **8,896 fights / 32 reservations**, with zero retries, replacements, omitted controls or extensions. Native study time: **339.56 seconds**, within the 840-second limit. The predeclared doubled estimate was **586.2 seconds**, within the 80% admission margin. Independent recount matched every outcome, reported mean guardian Health and each recipe's fixed prepared participants. Final exclusions: **927,660**.
- **315 targeted backend cases / zero skips** passed both on the fresh build and with the preserved combat assemblies, through `build/run-tests.ps1`. **524 Python checks passed**, including 97 qualification/family tests. The initial sandboxed build could not read the existing NuGet configuration; the authorized build in a separate preserved output directory succeeded. No required verification remains blocked. The known pre-existing Kharad behavior-manifest failure remains recorded in the broader historical proof; this scoped suite does not claim to fix it.
- `tower-catalog-qualification.py` adds the exact floor-10 family and requires an independently accepted Kodoku summon parent when qualifying through the later Ni catalog. `run-tower-balance-pass.py` admits that family only in seed-free preparation. Python and native application tests cover the new family and reject unsupported parent chains, budgets and recipe changes. Status, handoff, gear coverage and both harness guides now point here.
- No game catalog, combat engine, migration, application configuration, database or deployment change. Floor 9 remains applied; floor-10 health **12.8371**, offense **7.13** and its full kit remain fixed.

## Evidence and continuation

Independent evidence: `TestResults/tower-floor10-limited-armor-screen-evidence-20261001.json`, SHA-256 `3c68e957d4e12606ea96173530d3a78e59927f8e98ef1bbcc5feb5c22d427a9f`.

Qualification/preparation: `TestResults/tower-floor10-gear-qualification-20261001/completion.json`. Native build and command/TRX receipts: `TestResults/tower-floor10-gear-runtime-verification-20261001-repair1`. Python receipts: `TestResults/tower-floor10-gear-tests-20261001`. Frozen screen declaration, protocol, command and native TRX: `TestResults/tower-floor10-limited-armor-screen-driver-20261001`. Publication: `TestResults/tower-floor10-gear-publication-20261001/completion.json`. Local ignored `TestResults` evidence must be preserved separately from a clean checkout.

**Continuation completed:** The [96-replay pressure diagnosis](Tower-Floor10-Pressure-Diagnostic-20261001.md) and following [isolated Power/penetration diagnostic](Tower-Floor10-Penetration-Diagnostic-20261001.md) are complete. At offense factor 0.50 / penetration factor 40, two limited-equipment compositions win 5/16 each, both full-armor controls 4/16 and the strongest overall control 6/16. Hold that candidate fixed for the separately proposed full-family screening and independent confirmation. No gameplay application or acceptance allocation has occurred; the strict floor-10 aggregate contract remains to be implemented. Latest exclusions: 927,676. The original screen and all its evidence remain closed and unpooled.

## Frozen scope

Target: primary LL World Tower and the offline Balance Harness. Keep floor-10 health **12.8371**, offense **7.13**, the full guardian kit and all current catalogs unchanged. Preserve expected progression: fifteen level-50 characters, six level-1 unascended/unevolved Essences each, tier-2 Legendary/Masterpiece/rank-5 equipment, baseline rolls and no styles. Preserve every raw Essence order, actor/item identity and party position.

The complete historical confirmation contains **38 recipes / five actual compositions / seven gear profiles**, with **9,728 archived fights**. Its two accepted compositions won **72/256 and 68/256**, both with armor/health specialization on **60 items across fifteen characters**. Every other gear profile won zero. Those results are historical evidence, not a current limited-equipment acceptance.

Retain all 38 original recipes. For each of the two distinct accepted compositions, test all fifteen one-character subsets and all 105 two-character subsets. Copy only each selected character's saved Chest, Head, Legs and Necklace from the full armor/health recipe onto its exact baseline. This adds **240 recipes**, each using four or eight specialized items on one or two characters: **278 recipes total**. No search or new composition generation.

Before new combat, authenticate the preceding floor-9 publication, all historical floor-10 outcomes, current catalogs and corrected runtime. Qualify all **9,728 historical input hashes / 38 full historical replays**, admitting only the independently accepted floor-8 summons transition retained by floor 9. Then prepare the entire 278-recipe family natively without combat or new seeds.

Run one complete, separately frozen **32-fresh-seed panel across all 278 recipes: 8,896 fights / 32 reservations maximum**. Initial exclusions: **927,628**. Use the existing native 840-second / owner 900-second / 2-GiB limits and 80% admission margin with doubled measured time/bytes, plus a 2-GiB disk reserve. No retry, replacement, extension, interim selection, omitted control, pooled historical observation, confirmation or application.

This panel is **diagnostic only**. Independently recount every raw outcome and verify fixed prepared participants within each recipe. Report every retained control and the strongest limited-equipment recipe for every actual composition. It cannot establish formal balance or authorize automatic retuning. Any following acceptance panel or targeted mechanical diagnosis requires a separate frozen scope; none is allocated by this protocol.

Implementation adds a strict floor-10 limited-armor family validator and an explicit accepted-summons parent for the later Ni catalog. Older qualification contracts remain intact. Backend verification runs through `build/run-tests.ps1`. No migration, application configuration change, database operation, dungeon/acquisition work or deployment.
