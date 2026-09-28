# Attribute allocation follow-up — 27 September 2026

**Historical candidate evidence:** all results in this report predate the later user-requested change to subtract up to 40 percentage points of mitigation. Their frozen executable retains the earlier rating-based penetration. The [penetration follow-up](attribute-penetration-follow-up-2026-09-27.md) supersedes the penetration assumptions for current version 18; this report's rankings and price recommendation must not be treated as validation of the revised formula.

The four final studies below contain **8,960 production-engine fights** across 56 cells. They were run again using the retained executable in `TestResults/attribute-executable-20260927`: every trial file and every detailed replay matched byte for byte. The repeated verification is another 8,960 executions, not additional independent evidence. The 25 September screen remains separate.

## Decisions supported by this screen

- Keep candidate prices unchanged for this implementation. Large win-rate differences against a nearly identical opponent do not by themselves establish a universal pricing error. Speed, haste, precision, penetration, defense and sustain have different useful contexts in these fixtures.
- Haste and precision are strong for active-heavy Conduit and the tested control build. They are not universally superior: removing speed from the tested basic attackers loses, and haste on the summon build usually produces draws.
- Restoration has a useful party-support signal: 37/64 wins versus 32/64 for the reference, a paired change of **+7.81 pp** (unadjusted 95% interval **+0.05 to +15.57**). This single fixture is suggestive, not sufficient to certify a price. The support has two fixed damage allies; only its weapon allocation changes.
- The original solo healer result was a timeout tradeoff, not a survival collapse. Its reference had 58 wins / 5 losses / 1 draw; Restoration had 2 wins / 0 losses / 62 draws. Mean effective healing rose from 1,366 to 2,023, and barrier absorption from 8,131 to 12,948. The loss of wins must be reported together with the gain in survival.
- The original basic PvP cells also drew rather than lost. Regeneration erased basic damage. Changing weapons and replacing regeneration legs with armor was insufficient; omitting the regeneration-bearing relic produced informative basic-attack comparisons. These no-relic, zero-Essence builds are legal but deliberately simplified diagnostics, not representative level-90 player builds.
- Set effects are measurable in a matched encounter: losing the Arcane four-piece threshold hurt the tested caster relative to the full set. This establishes a real threshold effect, not that Arcane should beat every other set or that its reserved budget is optimally priced.
- The Tenacity exchange lost wins in its fixture: exchanging armor for resistance to control has a real defense cost. Inspect prevented duration and control-chain metrics as well as wins before promoting it as a universal upgrade.

No automatic activation or numerical retuning occurred. Live rules remain 17. Population balance, multi-wave attrition, broader party compositions and set price calibration remain distinct from correctness of the implementation. A later rehearsal used a read-only snapshot of the user's existing local database: 87 supported items passed conversion and rollback, while 469 unversioned items and one retired archetype remain explicit blockers. See [the rollout record](attribute-redesign-rollout.md) for the HTTP checks and fixes; no separately supplied backup was needed.

## Method and limits

Each request freezes content, build order, item budgets, opponent composition, 8 exploration seeds and 32 separate confirmation seeds. Both PvP sides are played per seed, so the 64 reported fights represent 32 independent paired seed clusters. New scenarios were prescribed after inspecting the earlier screen, but use new seeds. Confidence intervals are unadjusted across comparisons. Draws are never counted as wins. Constant differences have no estimated interval. The `speed` candidate for daggers and gauntlets equals their default specialty and serves as a no-change control.

The harness now supports up to five PvP participants per side with fixed allies/opponents. All party Doctrines are checked before combat starts. Party health ranking averages original participants and excludes summons; healing, damage and denied enemy actions remain separate objectives. Every candidate still preserves the focal build’s level, tier, rank, quality, roll and ordered Essences.

Failed preflights were retained rather than overwritten: the first role request had an unavailable specialty, a later request used an invalid Conduit channel and stopped after 384 exploration fights, and the first defense request named an unavailable necklace specialty. These incomplete runs are excluded from the totals and inference below. No balance decision was selected using confirmation seeds from those incomplete runs.

## Confirmation results

W/L/D counts below are descriptive mirrored fight counts. Inference clusters the mirrors by seed. All differences are candidate minus reference.

### matched

Artifacts: `TestResults/attribute-allocation-matched-20260927-verified`.
Request hash: `a95716aaa78f606116adad5798ccde74e78e5297b8e591b827b16c5152dbad84`.
Trial SHA-256: `b0132055ce09b26a7760c1610fcc39967c8861647117c040055f7bbcf14b11a1`.

| Cell | Reference W/L/D | Candidate W/L/D | Win Δ pp | Paired 95% interval |
|---|---:|---:|---:|---|
| matched-healer-precision | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-healer-speed | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-healer-haste | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-healer-restoration | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-dot-precision | 31/31/2 | 55/9/0 | 37.50 | 28.70 to 46.30 |
| matched-dot-speed | 31/31/2 | 48/15/1 | 26.56 | 17.78 to 35.35 |
| matched-dot-haste | 31/31/2 | 48/15/1 | 26.56 | 14.13 to 39.00 |
| matched-dot-restoration | 31/31/2 | 4/60/0 | -42.19 | -48.58 to -35.80 |
| matched-basic-precision | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-basic-speed | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-basic-haste | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-basic-restoration | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| matched-ten-essence-precision | 31/31/2 | 59/5/0 | 43.75 | 36.45 to 51.05 |
| matched-ten-essence-speed | 31/31/2 | 34/30/0 | 4.69 | -7.25 to 16.62 |
| matched-ten-essence-haste | 31/31/2 | 64/0/0 | 51.56 | 48.50 to 54.62 |
| matched-ten-essence-restoration | 31/31/2 | 31/33/0 | 0.00 | -9.84 to 9.84 |
| party-support-precision | 32/32/0 | 45/19/0 | 20.31 | 11.67 to 28.96 |
| party-support-speed | 32/32/0 | 33/31/0 | 1.56 | -10.48 to 13.60 |
| party-support-haste | 32/32/0 | 45/19/0 | 20.31 | 8.79 to 31.84 |
| party-support-restoration | 32/32/0 | 37/27/0 | 7.81 | 0.05 to 15.57 |

### roles

Artifacts: `TestResults/attribute-allocation-roles-20260927-verified`.
Request hash: `3f81ce4cf5c2355fbc35342721e6fa529054b154413181976942c99981e31d60`.
Trial SHA-256: `55942ebdc8e81bb9275ce73638233fe3ae521d3abab95d914f07722737103e1a`.

| Cell | Reference W/L/D | Candidate W/L/D | Win Δ pp | Paired 95% interval |
|---|---:|---:|---:|---|
| basic-dagger-precision | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-dagger-speed | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-dagger-haste | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-dagger-restoration | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-maul-precision | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-maul-speed | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-maul-haste | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-maul-restoration | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-gauntlets-precision | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-gauntlets-speed | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-gauntlets-haste | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| basic-gauntlets-restoration | 0/0/64 | 0/0/64 | 0.00 | Unavailable |
| summons-precision | 16/16/32 | 32/19/13 | 25.00 | 15.16 to 34.84 |
| summons-speed | 16/16/32 | 32/18/14 | 25.00 | 16.20 to 33.80 |
| summons-haste | 16/16/32 | 2/0/62 | -21.88 | -30.61 to -13.14 |
| summons-restoration | 16/16/32 | 0/16/48 | -25.00 | -33.80 to -16.20 |
| control-precision | 32/32/0 | 61/3/0 | 45.31 | 40.18 to 50.44 |
| control-speed | 32/32/0 | 59/5/0 | 42.19 | 35.80 to 48.58 |
| control-haste | 32/32/0 | 61/3/0 | 45.31 | 40.18 to 50.44 |
| control-restoration | 32/32/0 | 13/51/0 | -29.69 | -38.33 to -21.04 |

### basic-without-relic

Artifacts: `TestResults/attribute-allocation-basic-without-relic-20260927-verified`.
Request hash: `f83ace25038df55496ea92c17b4e2125dce6ed5db341a7e1b55c301433331f17`.
Trial SHA-256: `9ddf64f789f67d5c146ea1f9fc350b254f99e17c2dec4bb449e36447492e85f2`.

| Cell | Reference W/L/D | Candidate W/L/D | Win Δ pp | Paired 95% interval |
|---|---:|---:|---:|---|
| no-relic-basic-dagger-precision | 32/32/0 | 2/62/0 | -46.88 | -53.00 to -40.75 |
| no-relic-basic-dagger-speed | 32/32/0 | 32/32/0 | 0.00 | Unavailable |
| no-relic-basic-dagger-haste | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-dagger-restoration | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-maul-precision | 32/32/0 | 8/56/0 | -37.50 | -45.12 to -29.88 |
| no-relic-basic-maul-speed | 32/32/0 | 64/0/0 | 50.00 | Unavailable |
| no-relic-basic-maul-haste | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-maul-restoration | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-gauntlets-precision | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-gauntlets-speed | 32/32/0 | 32/32/0 | 0.00 | Unavailable |
| no-relic-basic-gauntlets-haste | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| no-relic-basic-gauntlets-restoration | 32/32/0 | 0/64/0 | -50.00 | Unavailable |

### defense-sets

Artifacts: `TestResults/attribute-allocation-defense-sets-20260927-verified`.
Request hash: `fa1c5fe7384f80ee66ab80009a66d538ea630dfc3f57fff5cb08d10ab0638784`.
Trial SHA-256: `bbb2d72e45b72b9772e6cd77dae9b941070d4abc91daa3defca5dcd57811e191`.

| Cell | Reference W/L/D | Candidate W/L/D | Win Δ pp | Paired 95% interval |
|---|---:|---:|---:|---|
| control-tenacity | 55/9/0 | 35/29/0 | -31.25 | -44.99 to -17.51 |
| matched-sets-arcane-three | 32/32/0 | 0/64/0 | -50.00 | Unavailable |
| matched-sets-split-two-two | 32/32/0 | 4/60/0 | -43.75 | -49.57 to -37.93 |
| matched-sets-aegis-four | 32/32/0 | 0/64/0 | -50.00 | Unavailable |

## Reproduction

Use a new `outputDirectory` in each checked-in request. Run the retained `BalanceHarness.dll` with `attribute-allocation-study <request-path>`. `files.json` beside the retained executable records every copied runtime dependency; each study manifest binds the five gameplay/harness assembly hashes. Content snapshots, requests, builds, trials, estimates, rankings and detailed replays are retained under the artifact directories above. These artifacts are ignored local files; preserve the directories together for handoff.

Checked-in requests: `LL/tools/BalanceHarness/Fixtures/attribute-allocation-{matched,roles,basic-without-relic,defense-sets}.json`. No current content was modified by these studies.
