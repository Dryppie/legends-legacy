# Current-gameplay Kharad calibration: protocol and compatibility inventory

**Subsequent readiness complete — 17 September 2026:** the [captured admission runtime and bounded request](Tower-Current-Family-Admission-Readiness-Review.md) are ready for one 1,800-second /2-GiB enclosing native admission. The real family remains unprepared; context adjudication and screen/confirmation execution adapters remain pending. The sealed design package is unchanged. Its old live-source verification command intentionally sees harness source drift; use the saved-package checks in the later reviews.

**Original design scope — 17 September 2026:** Target: offline `LL/tools/BalanceHarness`. This design step inventories saved recipes, checks current source/content identity and specifies a bounded calibration design. It performed **zero fights, native preparations, fresh allocations, builds or tuning changes**. Candidate `399bc776…` remains the recommended fixed team; both admitted anchors remain required controls. The remainder records that completed design and its then-proposed follow-up.

The [protocol JSON](../TestResults/current-tower-calibration-design-20260917/protocol.json), [inventory summary](../TestResults/current-tower-calibration-design-20260917/inventory-summary.json), [source catalogue](../TestResults/current-tower-calibration-design-20260917/sources.json) and [reader](analysis/current-tower-calibration-design.py) make the design reviewable. This is not a runnable launch request or evidence that the proposed large confirmation will finish within its caps.

## Frozen reference scope

Use the gameplay baseline from the completed 17 September fixed-team confirmation. All **183 admitted source pins**, **16 content hashes**, and **26 retained runtime files** match. The source/content check found no drift; no recompilation or substitution occurred. The retained runtime is under `TestResults/fixed-team-confirmation-admission-20260917-03/runtime`. Its execution hash is `32fc85b3ee4f6dd14a2c0c068599e4ae7345d41d94e01d92491144507e72c36e`; effective settings hash is `f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74`.

The cohort stays fixed: floor-5 Kharad, ten level-40 characters in two five-character subgroups, five level-1 unascended/unevolved Essences per character, tier 1/rank 2, Standard fixed gear, neutral identities, no styles or contributions, and `OwnedCopies=null`. Every proposed loadout uses ascending ordinal Essence IDs. Ability order is not searched or tuned. The [seed-free template](../TestResults/current-tower-calibration-design-20260917/target-template.json) retains all non-Essence fields; each projected cell supplies ten Essence vectors.

This identifies a fixed-cohort balance question. It does not settle acquisition, other identities/equipment, other floors or server-wide search strength. The [preceding review](Tower-Practical-Balance-Readiness-Review.md) establishes the current ceiling concern and why the captured-v19 +10% result cannot transfer directly.

## Complete static inventory and visible limitations

The inventory starts with every **43,879 historical entry and 47,834 origin records** from the retained-family archive. It then processes **255 saved metadata files** from the dated 15–17 September study packages: full family/definition/team scenarios and every proposal in 37 discovery snapshots, including rejected and duplicate proposals. Keeping all proposals conservatively covers later discovery breaches without selecting only favorable measurements. Repeated saved definitions remain distinct source occurrences.

The explicit catalogue is the coverage boundary. Discovery traversed up to four directory levels and selected the family, team, proposal, discovery, definition, nomination and finalist metadata listed in `metadata-candidates.json`; it excluded copied binaries/content and individual battle/report trees. Historical roots before those dates enter through the complete retained archive. This is complete for those enumerated inputs, not a claim that no other local file or unsearched party exists. **All 255 metadata files match retained manifests**, including four externally sealed stopped-study files. The copied role-validity discovery matches its original bytes and uses that original saved context definition. No source-context mapping remains unresolved.

| Inventory quantity | Count / status |
| --- | ---: |
| Source scenario/proposal occurrences, including three explicit required controls | 51,624 |
| Statically compatible occurrences | 51,462 |
| Incompatible occurrences, retained with reasons | 162 |
| Distinct projected canonical input cells | **46,077** |
| Repeated occurrences mapping to those exact projected inputs | 5,385 |
| Cells forced into the larger confirmation sample | **973** |
| Native prepared identities / current native legality | **Pending for the projected family** |

The [entry ledger](../TestResults/current-tower-calibration-design-20260917/entries.jsonl.gz) preserves source paths, exact row pointers, original scenario hashes, historical origins, classifications and projection mappings. The [cell ledger](../TestResults/current-tower-calibration-design-20260917/cells.jsonl.gz) retains every vector, contributing occurrence, forced-control reason and an explicitly null native participant hash. The [incompatibility list](../TestResults/current-tower-calibration-design-20260917/incompatibilities.json) keeps every rejected occurrence visible.

Static checks require the declared floor/time/preparation, ten exact character/gear templates, five known Essences per character, distinct source creatures within each character, and neutral identities. They reproduce the source-level family/capacity rules; **they do not replace production materialization**. SourceCreature matching is case-insensitive, as in the harness. Ownership limits remain intentionally absent.

The incompatible reasons overlap: **111** occurrences differ in character ID/equipment/budget and identity, **51** contain repeated source creatures, and **21** differ in floor/schema and slot count or Essence availability. Of the 111, **90 are historical retained entries with non-neutral identity vectors and different character IDs**. Their UTC instants match the target; timestamp normalization does not resolve their identity difference. The other 21 are incompatible later saved scenario occurrences and carry forced-control provenance. They remain a coverage issue to adjudicate before a complete executable family is frozen. Do not relabel them weak or omit their findings to obtain a Pass.

Canonical projection changes the ordered historical input in **49,791 occurrences**. It is a prospective fixed-order recipe transformation, not a claim of equal historical combat behavior. Matching projection keys prove equality of the proposed JSON inputs under this template only; they are not native recipe or participant hashes. Retain the original order through its authenticated source. A future admission pass must check every projected cell and preserve every contributing origin when native aliases are merged.

The forced sample includes the historical 989-team focused set after projection, all compatible later saved full scenarios, later discovery proposals with an observed clear fraction above 50%, and the three confirmed fixed teams. These sets overlap and project to 973 distinct cells. This conservative rule also retains nominees without requiring them to have breached. The historical focused set already included the earlier retained anchor/breach union. No historical score becomes a new confirmation observation.

## Proposed small setting screen

Change only Kharad floor 5's guardian **health** and **offense** scalars in isolated content copies, multiplying both by the same decimal factor. Baseline values are **3.5366243328 /4.4702934848**. Leave all other fields and live content byte-identical. Verify parsed content differences and materialized inputs before combat.

| Element | Prospective rule |
| --- | --- |
| Factors | **1.00, 1.05, 1.10, 1.15**; baseline is a control, only the other three are eligible for selection |
| Teams | Exact confirmed candidate and both anchors; no replacement or search |
| Trials | **256 shared fresh values**, used for all 12 setting/team cells |
| Combat ceiling | **3,072 attempts**, zero retries |
| Bounds | Two-sided approximate Wilson intervals, alpha .05 divided by **12** |
| Selection | Choose the lowest eligible factor whose three upper bounds are all ≤50% and at least one lower bound is ≥10% |
| Otherwise | **Unresolved**, preserve evidence and stop; no interpolation, new grid, pooling or sample extension |

At 256 trials under that fixed correction, a cell needs **at least 40 wins** to support 10% viability and **at most 105 wins** to support the 50% ceiling. These are interval-derived checks, not substitute gameplay targets. Selection considers all three teams; the strongest may change with the setting.

The +10% value comes from the older captured-scope candidate. ±5 percentage points supplies a small symmetric bracket without assuming its transfer or a monotonic response. The design does not predict that any factor will qualify. The fresh baseline is a simultaneous control; its earlier 5,500 observations are not reused as screen trials. A selected factor is **CandidateForCurrentFamilyConfirmation**, never a balance Pass or a live-content application.

## Prospective relevant-family confirmation

First complete current native admission and settle the incompatible contexts. Freeze the exact admitted family, all origins, mandatory-control mapping, runtime and isolated selected content before confirmation. **The 46,077 static cells are a design ceiling, not an already admitted family.** A compatible projection that fails production legality or preparation blocks readiness; it is not silently dropped. Incompatible contexts need an explicit scope disposition, and broader progression acceptance remains blocked while required coverage is unresolved.

The proposed two-stage rule follows the existing staged statistical method, with a new current-family contract required for capacity and scope:

- **First stage:** 32 fresh trials for each of the **45,104 non-anchor projections**. Alpha .025 is divided over the complete first-stage family. A cell whose adjusted upper bound is ≤50% is resolved. At this design size, **0 or 1 win out of 32** resolves the ceiling. Any observed rate above 50% rejects acceptance before stage two.
- **Second stage:** every one of the **973 forced controls**, plus all unresolved first-stage cells, receives **512 different fresh trials**. Maximum second-stage family **4,096**. Freeze selection before any second-stage outcome. If capacity is exceeded, retain all unresolved cells and return **Inconclusive**; no thinning or higher cap.
- **Final decision:** alpha .025 divided over the complete selected second-stage family; use second-stage observations alone for advanced cells. All included cells must support the ceiling and the cohort must have at least one supported viable party. At the worst-case 4,096-cell correction, second-stage viability requires **82/512** and ceiling support requires **at most 204/512**. Any observed second-stage rate above 50% rejects acceptance. Missing, invalid or incomplete evidence cannot pass.

With the static counts unchanged, the attempt reservation is **1,443,328 +2,097,152 =3,540,480** fights. No reduction in actual attempts refunds budget for new recipes or samples. A native alias change requires a reviewed, pre-outcome executable family and recomputed reservation within this ceiling; it is not an outcome-dependent family reduction. The statistical claim is approximate coverage for one frozen staged experiment, not exact coverage, global optimality or a lifetime repeated-study guarantee.

This is a substantial workload. A favorable three-team screen cannot replace it, and its maximum cost must be acceptable before launch. Current evidence does not establish completion time or the fraction that will advance. If the bounded design is unaffordable, keep the candidate setting unpromoted and report that limitation.

## Sampling, resources and execution route

Propose **one post-freeze cryptographic batch of 2,048 signed 32-bit words**, after admission/family/runtime freeze and a fresh full-history scan. Preserve the exact transcript; reserve every newly exposed value, including unused tails. Select the first **800** eligible distinct values in fixed order: **256 screen**, **32 confirmation first stage**, **512 confirmation second stage**. The panels are disjoint, unused against the entire historical union, and shared across cells within each stage. Screening can select a setting but cannot change the later schedules. If the batch supplies too few eligible values, close before combat without a redraw. If screening fails, the unused confirmation values remain reserved.

This is a proposed allocation contract; no batch or schedule was created. The recorded union is **497,371**, and the existing limit is **1,000,000**. Even charging all 2,048 words would leave **500,581** headroom. A fresh launch-time registry scan is still mandatory; the current task does not claim one. Original V19's unused 512 and the fixed-team run's unused 5,497 stay excluded.

| Proposed scope | Whole-scope wall limit | Whole-scope new-output limit | Phase ceilings / result |
| --- | ---: | ---: | --- |
| Seed-free native admission | 1,800 s | 2 GiB | Zero fights and fresh values; preparation, alias/context checks and audit included |
| Small screen | 1,800 s | 1 GiB | Setup/history/admission 300 s; combat 900 s; audit/publication 600 s |
| Full staged confirmation | 86,400 s | 64 GiB | Setup/history/admission 3,600 s; combat 72,000 s; audit/publication 10,800 s |

These are **proposed independent ceilings**, totaling at most **90,000 seconds /67 GiB**, not approved balances or completion forecasts. The whole-output bound includes copied runtime/content, logs, journals, exports, audits and closing metadata. Abort on the first phase, wall, byte, integrity or attempt boundary; durable starts count, retries/resumes/replays are zero, and unspent allowances are not transferred. Engineering implementation/editing remains separately unmetered and must be disclosed when any concrete execution approval is requested. Existing closed allowances remain fully charged; no historical cap or source contract changes here.

The current code already supplies `TowerBalanceEvaluator` Wilson arithmetic, compact archives, durable attempt accounting, staged selection, content validation and saved-evidence verification. Reuse those components. **No existing public command implements this complete protocol unchanged**:

- Generic `TowerStagedBalance` permits 10,000/20,000 cells and at most 500,000 attempts; this design exceeds both.
- `TowerCompleteFamily` binds exactly 43,879 captured cells, 560 anchors, captured source seals and 256 second-stage samples. Its old request also lost its unused schedule to the focused study. Do not modify or reuse that contract.
- The fixed-team confirmation binds its exact three teams, one original content setting and 5,500 trials. It cannot perform this screen or allocation by substituting inputs.

A new narrowly versioned adapter must bind this current family, the isolated factor settings, the proposed allocation and the two separate decisions. Its tests must verify preservation of old routes, content-diff restrictions, canonical identity/aliases, incompatible-input failure, staged capacity stops, complete family accounting and native/independent audit parity. Backend tests must use `build/run-tests.ps1`. No implementation or backend test was part of this design step.

## Admission follow-up and original design verification

**The design's implementation and readiness deliverables are complete:** the [seed-free adapter](Tower-Current-Family-Admission-Implementation-Review.md) passed 172 distinct implementation cases; the [captured runtime](Tower-Current-Family-Admission-Readiness-Review.md) then passed all 45 adapter cases with the original gameplay assemblies. The bounded request and enclosing launcher are prepared. **Next: one native admission within 1,800 seconds /2 GiB overall**, establishing native identities and preparation cost. No real preparation has run, and the 162 context exceptions still need disposition before family freeze. Screen and large confirmation remain separate future scopes.

The [verification receipt](../TestResults/current-tower-calibration-design-20260917/final-checks.json) records input and manifest authentication, full static-inventory reproduction, independent aggregate/interval/resource checks, baseline preservation, local links, syntax and whitespace. Two preliminary static drafts remain under `draft-01` and `draft-02`: the first exposed the copied-context lookup gap; the second preceded explicit forced-control classification. Neither ran native code or allocated values. Final classification resolves the lookup using authenticated original bytes.

The following commands record the completed design's verification before the adapter changed harness source. They are historical invocations, not current instructions: live-source pins now differ, and the finalizer writes into its package. Preserve the sealed inputs and use the [completed implementation package's read-only seal check](Tower-Current-Family-Admission-Implementation-Review.md#verification-and-retained-evidence) for current evidence verification.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $py -B -X utf8 'Balance Harness/analysis/current-tower-calibration-design.py' verify
& $py -B -X utf8 'TestResults/current-tower-calibration-design-20260917/finish-checks.py'
```

Added this design, the static reader and the review package; updated current Markdown handoffs. Backend/gameplay source, historical scientific JSON, sealed evidence and active content are preserved. No required check remains blocked. No migration, configuration change, dependency change or deployment is required. Native legality, controller implementation, execution feasibility and broader coverage remain explicit future gates rather than claimed results.
