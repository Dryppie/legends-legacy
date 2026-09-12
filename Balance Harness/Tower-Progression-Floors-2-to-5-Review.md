# Tower progression audit and linked calibration: floors 2–5

**Competitive search update — 12 September 2026:** [stronger Kharad searches](Tower-Competitive-Build-Search-Review.md) confirmed new teams at **948/1,000** and **1,000/1,000** on the previously passing content. Its earlier scoped Pass is superseded for the expanded build portfolio. Retained-build improvement and the history-capacity extension are implemented; the fresh quality audit and separate linked calibration are documented in the new review. Near-optimality remains unestablished.

11 September 2026. The first progression batch independently generated complete teams for floors 2–4 at **four Essences per character**, and checked Kharad again at **five**. Floors 2–4 failed their original settings. A separately frozen calibration then covered every earlier above-ceiling candidate, every shortlisted party and every compatible reference before local application.

Four slots through floor 4 is an explicit working interpolation of the approved floor-1/floor-5 checkpoints. The experiments use the full 80-Essence pool, including Rare Essences, with hypothetical ownership and the existing neutral equipment budget. Practical acquisition timing remains unverified.

**Server-wide requirement clarified after this batch:** the user requires benchmarks among the strongest achievable builds because Tower progression will involve the best players. The scoped passes below establish performance for the tested portfolios; they remain provisional for that competitive requirement. Near-optimal build quality is unestablished. The [strongest-build follow-up](Automatic-Tower-Team-Discovery-Plan.md#server-wide-progression-strongest-build-evidence) prioritizes retained-build improvement, more reliable independent discovery and challenger searches on the applied settings. Original assessments and sealed archives remain unchanged.

## Results

| Floor | Party / slots each | Strongest original confirmation | Strongest calibration / fresh check | Adjusted interval | Assessment |
| --- | --- | ---: | ---: | --- | --- |
| 2: Velka | 5 / 4 | 1000/1000 (100%) | 31/150 (20.67%) | 10.68–36.20% | Pass |
| 3: Morrowmaw | 5 / 4 | 1000/1000 (100%) | 147/400 (36.75%) | 28.56–45.78% | Pass |
| 4: Vaelor | 5 / 4 | 992/1000 (99.2%) | 81/400 (20.25%) | 14.22–28.00% | Pass |
| 5: Kharad | 10 / 5 | 209/1000 (20.9%) | 209/1000 (20.90%) | 17.61–24.63% | Pass |

Each assessment applies the existing family-adjusted 95% Wilson policy to its entire frozen per-floor portfolio: **every upper bound ≤50%, at least one lower bound ≥10%**. Weak parties do not average away a stronger party. These are scoped portfolio results, not proof of a global optimum.

Confirmation draws count as non-wins. Original search draw totals: floor 2: 2, floor 3: 0, floor 4: 22, floor 5: 0. Applied-setting confirmation draw totals: floor 2: 1, floor 3: 2, floor 4: 17.

Kharad uses its already applied Health **2.106** / offense **2.661984**. This search found no earlier above-ceiling candidates. Its two new generated finalists won 78/1000 and 26/1000; generated-only viability was **Inconclusive**. Its saved strongest control supports the overall pass, so reliable rediscovery of that best build remains unestablished.

## Applied linked scaling

| Floor | Factor from captured baseline | Health before → applied | Power before → applied |
| --- | ---: | --- | --- |
| 2: Velka | 1.41075 | 1.45 → 2.045587 | 1.38 → 1.946835 |
| 3: Morrowmaw | 1.175 | 0.86 → 1.010500 | 3.79 → 4.453250 |
| 4: Vaelor | 1.175 | 1.11 → 1.304250 | 2.4 → 2.820000 |

Only Health and offense/Power change for passing floors. Their ratio is preserved up to six-decimal rounding; defense, resistance, penetration, regeneration, staggering, abilities and party requirements remain fixed. Garran stays at Health **1.776984** / offense **1.735008**; its latest earlier check was 34.8%. Kharad remains unchanged by this calibration.

## Frozen discovery and calibration

The [discovery protocol](../TestResults/balance/tower-progression-audit-20260911/protocol.json) excluded **91,947** historical seeds. Each floor generated **768 complete parties** across random and constructive-joint methods with three restarts, tested 16 shortlisted parties on 64 separate seeds, then froze finalists before references entered. Every final party/control received 1,000 new paired confirmation seeds. References have zero generation ancestry.

Floors 2–4 each included all **40 exact-budget historical confirmation controls** from the eight audited final generalist/specialist archives. Kharad included all **six persisted independent specialists**. Boss content, engine, equipment and stage allocations stayed fixed throughout each search.

The [separate calibration protocol](../TestResults/balance/tower-progression-calibration-20260911/protocol.json) froze all original confirmation parties, every shortlist member, and every distinct earlier above-ceiling candidate. Its portfolios contain **663 / 173 / 90** parties for floors 2 / 3 / 4. Distinct earlier breach parties covered: **floor 2: 623**, **floor 3: 133**, **floor 4: 50**. Stage-level breach records may repeat the same party; the full shortlist is retained even when a party never exceeded the ceiling.

All parties received eight paired coarse seeds at linked factors **1, 1.25, 1.5, 2, 3, 4, 6, 8**. A predeclared rule chose an adjacent bracket and eleven evenly spaced fine settings. All original generated finalists plus the strongest other coarse performers, up to twelve, received 64 fresh paired seeds at each fine setting. Parties omitted from this fine stage still entered confirmation.

The large floor-2 family uses **150 paired confirmation samples per party (99,450 combats)**, the largest uniform count below the existing 100,000-combat confirmation limit. Its predeclared fine target is 20%, with an eligible range of 15–30%, to allow for wider adjusted intervals. Floors 3 and 4 use **400 samples**, targeting 30% within 15–40%. This changes sampling precision and tuning targets, never the final 10–50% acceptance rule. Coarse/fine costs are counted separately in the total campaign reservation.

Each floor started calibration only after its original failed study completed and reconstructed. One chosen setting was frozen before its confirmation. No confirmation outcomes selected a setting within that experiment, no party was removed, and no samples were added. Historical failed results remain unchanged.

### Separate Velka follow-up

The first floor-2 calibration chose factor **1.375** but remained **Inconclusive**: a party omitted from its fine sweep confirmed at **55/150 (36.67%)**, with an adjusted interval of **23.05–52.80%**. Its full-family confirmation also recorded 4 draws. It was never applied. The upper uncertainty bound breaches the acceptance limit even though the observed rate is below 50%. The full-family check exposed this concern instead of accepting the smaller fine portfolio.

A [new frozen protocol](../TestResults/balance/tower-progression-calibration-20260911/floor-2-followup/protocol.json) then selected twelve screening controls before any new combat: the original generated finalist, every upper-unsupported party, and the highest prior confirmation results until twelve were included. These twelve received new coarse and fine schedules. Relative coarse factors were **1, 1.005, 1.01, 1.02, 1.04, 1.06, 1.08, 1.10** against the unapproved first candidate. A new eleven-point fine grid targeted 25%, with eligible results 15–35%.

The follow-up froze relative factor **1.026**, or **1.41075** against Velka's captured live baseline. Every one of the **663 original parties** then received **150 entirely new paired confirmation seeds**. The first inconclusive samples were not pooled, overwritten or reused. The current result in the table comes from this separate passing confirmation.

## Saved builds and verification

All new search finalists and all imported floor-2–4 controls now persist in the [main local retained-build index](../TestResults/balance/retained-tower-builds.json). The ten strongest freshly calibrated recipes per tuned floor are also retained. Exact-party deduplication leaves **floor 1: 4**, **floor 2: 50**, **floor 3: 52**, **floor 4: 50**, **floor 5: 8** compatible controls. Future previews exclude the new seed ledgers and still have zero supplied generation starts. The complete larger calibration portfolios remain in the archived evidence.

This additional retention is local to the configured results folder. The checked-in portable fixture remains unchanged; a fresh checkout needs the retained index and its supporting archives to reuse these new controls. Saved observations nominate controls for fresh confirmation and do not supply generation parents or count as new evidence.

Exact ordered Essence lists, equipment and fixed character identities are exported in the [floor-2 calibrated builds](../TestResults/balance/tower-progression-calibration-20260911/published/floor-2-builds.md), [floor-3 calibrated builds](../TestResults/balance/tower-progression-calibration-20260911/published/floor-3-builds.md), [floor-4 calibrated builds](../TestResults/balance/tower-progression-calibration-20260911/published/floor-4-builds.md) and [fresh Kharad builds](../TestResults/balance/tower-progression-audit-20260911/published/floor-5-builds.md).

The four searches completed **163,682/176,496 reserved combats**, including their detailed outcome replays. All four archives reconstructed through the CLI without new combat. Independent arithmetic checks reproduced every confirmation outcome and adjusted interval, stage accounting, generated provenance and recipe exports.

The calibration and separate Velka follow-up completed **398,216/398,728 reserved combats**, including verification. The normal-Tower evaluator reconstructed all four full confirmation families, preserving the first inconclusive result. Independent audits reproduced the coarse brackets, fine portfolios, selected settings, outcomes and intervals, and checked coverage of every original breach.

After application, **140 exact current-content reports** matched saved confirmation reports across every original generated finalist. All **60 unaffected-floor before/after pairs** matched, including Garran and Kharad. **32 detailed replays** matched preparation and outcomes. Repeated identities are application checks, not new confirmation evidence.

Relevant backend checks passed through `build/run-tests.ps1` before and after application: **25 tests per run**, covering retention, Tower Lab and World Tower behavior. The captured producing executable matches the current compiled combat assemblies; concurrent unbuilt application changes remain outside this measurement. No required verification was blocked.

An audit wrapper was initially launched before the final confirmation metadata existed, and both launches stopped before auditing stage outcomes. It was corrected to read the already completed observation files, require exact equality with the eventual confirmation wrapper, and wait for the independent archive assessment before recording a conclusion. The successful reruns preserve the initial error logs; no combat, setting or seed schedule changed.

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerRetainedBuildTests|FullyQualifiedName~BalanceHarnessTowerDashboard|FullyQualifiedName~WorldTowerTests'
```

Changed live files are limited to the passing scaling fields in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), the local retained-build index, exact exports, this review and the linked planning/README notes. There are no new runtime dependencies, configuration schemas, migrations, shared-database changes or deployment. Unrelated working-tree edits are preserved.

New Tower Lab runs read the local catalog. An already running game API caches the catalog and requires a restart to load file changes; this offline task did not restart or deploy it.

## Completed archive publication

Both evidence packages are sealed. The discovery package contains **167,656 files**; the calibration package, including the separate Velka follow-up, contains **621,985 files**. Their checksum manifests match the recorded seal hashes:

| Package | Seal | Checksum-manifest SHA-256 |
| --- | --- | --- |
| Independent searches | [Discovery seal](../TestResults/balance/tower-progression-audit-20260911/package-seal.json) | `738d95af8b488456a9e7eec57f9480132d0ed7fd09782f4c02df96c3cc92cdb6` |
| Linked calibrations and application checks | [Calibration seal](../TestResults/balance/tower-progression-calibration-20260911/package-seal.json) | `5f0e8385c31b834e3923849355c5ac347d9950ef30f29479c85dc84c9a641838` |

The [verification summary](../TestResults/balance/tower-progression-calibration-20260911/verification-summary.json) records the completed checks. The packages retain their original documentation snapshots. Later edits to live plans and this review do not rewrite those snapshots or reopen the completed experiments.

## Remaining progression coverage

Continue with floors **6–9 at five Essences**, **floor 10 at six**, then **floor 11 at seven**, keeping lower-budget Serevin teams as separate diagnostics. These intermediate groupings remain explicit working assumptions. Carry saved specialists forward as controls, independently search for stronger teams, and keep any subsequent calibration separately frozen. The full Tower progression curve and practical Essence access are not yet established.

This batch's final previews contained **97,838 historical exclusions**, approaching its then-current 100,000-entry history limit. The subsequent [competitive search increment](Tower-Competitive-Build-Search-Review.md) extends cumulative history to 1,000,000 without changing per-study combat caps or discarding old seeds.

The [next-batch checklist](Automatic-Tower-Team-Discovery-Plan.md#next-batch-history-capacity-and-floors-611) records the implementation boundaries and verification required before those new studies. No floor-6–11 campaign or history-capacity change is included in this completed batch.
