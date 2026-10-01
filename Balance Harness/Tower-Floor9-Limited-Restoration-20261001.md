# Floor 9: limited-Restoration diagnostic — 1 October 2026

**Follow-up completed:** [The offense calibration](Tower-Floor9-Restoration-Offense-20261001.md) nominated original offense ×1.00 with penetration ×40 for fresh formal acceptance testing. All 15,648 diagnostic fights are audited; no live setting changed. The next-step text below preserves the earlier checkpoint.

## Completed result

**Both panels completed: 5,216 fresh fights / 32 reservations, independently audited.** Eight Restoration items split across the two healers produced promising routes; concentrating six items on one healer remained weak. These are descriptive results, not acceptance. The current isolated Ni candidate remains rejected and no live catalog changed.

| Saved composition | Baseline | Six items, slot 2 | Six items, slot 7 | Eight items, slots 2 + 7 | Twelve-item parent | Full Health/Regeneration |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| reference-1 | 0/32 | 2/32 | 1/32 | **10/32** | 5/32 | 9/32 |
| reference-2 | 0/32 | 0/32 | 0/32 | **5/32** | 2/32 | 12/32 |
| reference-3 | 0/32 | 1/32 | 1/32 | **15/32** | 8/32 | 22/32 |
| 5b6c297c… | 0/32 | 1/32 | 3/32 | **11/32** | 9/32 | 24/32 |
| 4a9f8b9f… | 0/32 | 1/32 | 4/32 | **11/32** | 9/32 | 20/32 |

Each comparison uses the same 32 seeds. Every original control was retained. Four-item sets use MainHand, Chest, Head and Necklace; the other equipment stays at the baseline. The observed advantage over the twelve-item parents is not proof of a universal item-count rule or a causal explanation of which omitted item matters.

**Next:** retain all **163 recipes / five compositions / 130 eligible recipes** and calibrate guardian offense at **×1.00, ×1.05 and ×1.10 relative to the original live offense**, with penetration held at ×40. These are about 5.3%, 10.5% and 15.8% stronger than the tested ×0.95 candidate. Preserve Health, copied defenses, summon Health and the whole kit. The saved proposal is unimplemented and unallocated: two sixteen-seed batches per setting, six batches total (**15,648 diagnostic fights / 96 fresh seeds**). Complete the whole grid before selection. Its descriptive selection margins are at least 6/32 wins on two distinct eligible compositions and no recipe above 12/32; a selected setting would still require fresh formal screening and independent confirmation. No live change now.

The existing generic penetration owner permits offense below 1 only. The follow-up must add explicit fixed-candidate dispatch for this declared grid while preserving that generic guard and the old 148-recipe acceptance contract. Do not bypass validation or reuse the rejected candidate's results for acceptance.

## Verification and retained evidence

- **474 Python checks** pass. **337 native backend checks** pass through `build/run-tests.ps1`, including diagnostic boundaries, existing application guards, typed damage/cap checks, Ni mechanics and corrected summon defenses. The unchanged broader proof retains 766 passes / four skips and its known pre-existing Kharad behavior-manifest failure.
- Native verification completed **341 preparations**: both catalogs for all 163 recipes plus 15 independently reconstructed gear variants. All 148 retained candidate inputs match the preceding Ni preparation exactly; new variants change only the declared healer equipment. Corrected production assembly hashes are unchanged.
- The zero-fight source preparation and both study fixtures passed. Native batch times: **60.282s and 57.373s**; doubled resource and disk admission passed. Exactly **32 new reservations**; final exclusions **926,508**. No fight retry, seed replacement, extension or dropped controls.
- The original reporting collector had a generated syntax error: an unanchored text match selected `one=io.read(` inside `done=io.read(`. The original pinned collector is preserved; a separate repaired collector anchors the intended line and independently recounts all existing reports. **No native fight was repeated and no archive or declaration changed.**
- All **102 live catalogs remain unchanged**. This continuation changes offline family/diagnostic support, native/Python tests and documentation. No engine change, migration, configuration change or deployment.

Evidence: `TestResults/tower-floor9-limited-restoration-evidence-20261001.json`, SHA-256 `eb1708e3d0e2ae459c47737954f544ea9697982089b09fb6129138f79deb75f5`.

Declaration: `TestResults/tower-floor9-limited-restoration-driver-20261001/declaration.json`, SHA-256 `afd0421f78c324bacba4f52c05412061b19bbfec4b4bf7caecdfebc548c1aa87`.

Runtime proof: `TestResults/tower-floor9-limited-restoration-runtime-verification-20261001/completion.json`. Python proof: `TestResults/tower-floor9-limited-restoration-tests-20261001/completion.json`. Independent collector: `TestResults/tower-floor9-limited-restoration-collect-repair1-20261001.py`.

Next proposal: `TestResults/tower-floor9-restoration-offense-proposal-20261001.json`, SHA-256 `4ec5f4d30fee27a851ce7781d92b901cfc7108f70703f475152aa715c1b769d6`. No candidate preparation, follow-on fight or seed allocated. Floor 9 remains unresolved, followed by floors 10 and 12–15 and the final current-version 1–15 sweep.

## Frozen protocol

The rejected offense ×0.95 / penetration ×40 candidate remains isolated. The previous 148-recipe screen found eligible routes at only 0–4/128 wins, while four twelve-item Restoration parties won 27–37/128. This diagnostic tests whether exact six-item and eight-item subsets help; it cannot accept or apply the rejected candidate.

- Retain every original recipe in its original order: **148 controls / five actual compositions**. Add exactly **15 variants**, three per composition: six Restoration items on healer slot 2, six on slot 7, or four each on both (MainHand, Chest, Head, Necklace). **163 recipes total / 130 eligible** under the unchanged maximum of eight specialized items on two characters.
- Preserve actor identities, raw Essence arrays, party/equipment order, minimum-level expected progression, 10% copy Health, corrected summon defenses, Ni's kit and all unrelated catalog data.
- Native preparation must verify all 163 original/candidate pairs and independently reconstruct all 15 subsets, with no combat or seed allocation. Reuse the authenticated corrected production assemblies.
- Run **two complete sixteen-seed panels**, sharing each panel across every recipe: **5,216 fresh diagnostic fights / 32 reservations total**. Initial exclusions: **926,476**. No retry, seed replacement, extension, dropped controls or historical outcome pooling.
- Maximum per batch: 20,000 fights, 840 native seconds, 900 owner seconds, 2 GiB. Before each allocation, require doubled measured cost below 80% of time/byte limits and remaining projected disk plus 2 GiB. No interim selection.
- Independently recount every raw outcome and prepared participant after both panels. Report all five paired baseline / one-healer / eight-item / twelve-item comparisons and all retained controls. These are descriptive counts only. No confirmation or application.

Frozen proposal: `TestResults/tower-floor9-limited-restoration-proposal-20261001.json`, SHA-256 `0c60c17b2b10aa0428a4d118ba8a30cf4f40c72d240ed7449955de7a3a37a5af`.
