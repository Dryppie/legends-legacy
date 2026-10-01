# Floor 7: fixed 0.9% Endless Spring refinement — 2026-09-30

**Latest floor-7 precision result (30 September):** The [fixed 0.9% precision study](Tower-Floor7-Endless-Spring-Precision-20260930.md) **passed both independent 1,024-seed phases and was applied locally** after **245,760 fresh fights / 2,048 reservations**. All **120 recipes / eight actual compositions** remain. Two one-healer routes using **six specialized items each** confirm at **289/1,024 and 260/1,024**; every ceiling passes, largest adjusted upper **39.67%**. Isolated and live parity each matched **122,880 inputs / 960 full historical replays**. **174 Python cases and 194 backend cases pass**, with four intentional skips, plus 16 native study fixtures. The initial reporting-only collector import error was repaired without changing its frozen script or rerunning combat. Exclusions: **923,772**. **Floor 7 is now the latest locally applied Tower change.** Next: read-only floor-8 gear-dependence review and current-catalog qualification before any new study. No floor-8 seeds are allocated; no dungeon or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [rejected 0.75% recovery trial](Tower-Floor7-Endless-Spring-Recovery-20260930.md). Its healer setups won 70/128 and 51/128 and A/haste won 58/128, all above the 44-win ceiling. At 1% recovery the corresponding counts were 21/128, 15/128 and 34/128. Test a smaller 10% reduction from the original recovery coefficient; no monotonic response or predicted win rate is assumed.

## Frozen protocol

Authenticate the preceding publication, SHA `1605775f7a32bc54c9120502d8a30fb61ed2c4f88d72b136a76112c85b0dfe8d`, and `TestResults/tower-floor7-endless-spring-refinement-proposal-20260930.json`, SHA `a87e25f5d5a65847fdfbd29dd63efd073c63e651a69af53ea198a97b95f2e899`. Entry snapshots preserve modified maintained files and the complete **921,596-seed exclusion union**.

Use the original current-live source `TestResults/tower-balance-pass-floor7-mixed-resistance-screen-study-20260929`, manifest `1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31`. Preserve all **120 exact recipes / eight actual compositions**, raw identities, ordered Essences, positions and expected progression: five level-40 characters, five Essences each, T1 Unique/Exceptional/rank-4 gear. Every existing winner and unsuccessful control stays in the family.

One candidate only: **Endless Spring 0.9% MaxHealth per Abundance instead of 1%**, with **Springtide 21.25% target-MaxHealth base + 0.20 source Power per Abundance**, **offense factor 0.60** and **penetration factor 50**, all reconstructed from the original live source. Guardian Health, other scaling, Abundance generation, interval timing, noncritical healing, Heartwood, Tranquil Waters and all other content remain unchanged. The dynamic `{statusScaling}` recovery description is preserved. No rejected candidate becomes the source.

Use the new separate `tower-recovery-pressure-refinement-v1` contract. Keep the existing health-pressure-v1 and 0.75% recovery-v1 contracts strict; it must continue rejecting a recovery edit. Before study allocation, run candidate, application, family and qualification safeguards plus native interval, stack, MaxHealth, rounding and actual-healing/overhealing tests through `build/run-tests.ps1`. Bind the new test assembly to the exact archived production binaries; no rebuilt combat DLL is used for study execution.

Run one complete **128-seed screen: 15,360 fights**. Keep at least **two actual compositions** qualifying with **at most eight specialized items on two characters**, adjusted lower bound **≥10%**, and every one of the 120 recipes' adjusted upper bounds **≤50%**. Use the original approximate 95% simultaneous Bonferroni-Wilson method: qualifying screen recipes need **25–44 wins / 128**.

Only if the whole screen passes, run one independent **160-seed confirmation: 19,200 fights**, identical family and candidate, with count gates **30–57 / 160**. Never pool phases, replace observations, retry native studies, extend panels, select from partial outcomes or add another coefficient. On either gate failure, close this fixed scope. Maximum new scope: **34,560 fights / 288 reservations**; all reserved seeds remain excluded.

Admit each allocation using **twice measured complete-panel time and bytes per fight**, under **672 projected seconds / 80% of 2 GiB / 20,000 fights**, with **840-second native / 900-second owner** limits. The first measured source is the closed 0.75% recovery screen. Before allocation, freeze candidate, protocol, scripts, runtime and content bindings. During execution, read owner stdout only; do not open active archive files.

After each complete phase, independently reconstruct every catalog delta, raw outcome, simultaneous bound, actual composition, equipment budget, disjoint seed and native prepared participant. A passing confirmation additionally requires isolated native parity of **19,200 inputs / 120 complete historical replays** before copying the two verified catalogs locally. Then run backend regression and the same parity against local content. No migration, configuration, database action or deployment. The previous floor-2, floor-4, floor-5 and floor-6 improvements remain intact.

## Completed result

**`NoEligibleRecoveryPressure`.** The fixed **0.9%** recovery screen completed **15,360 fresh fights / 128 reservations**. All **120 recipes** pass the upper-bound ceiling; the largest adjusted upper is **46.82%**. The strongest two actual limited-equipment compositions both specialize one healer (six items): A wins **40/128**, adjusted interval **19.01%–46.82%**; B wins **18/128**, interval **6.42%–28.08%**. Only A meets the required **25-win** screen minimum. **No confirmation or gameplay change followed.** B's observed 14.06% exceeds 10%, but its uncertainty still crosses that threshold; this is insufficient acceptance evidence.

| Phase / Springtide base (recovery fixed at 0.9%) | Best two distinct limited-equipment compositions | Limited compositions meeting minimum | Ceiling failures | Largest adjusted upper |
| --- | --- | ---: | ---: | ---: |
| screen-1 / 21.25% | 40/128 (restorer-specialization); 18/128 (restorer-specialization) | 1 | 0 | 46.82% |

Every phase preserved the 120-recipe / eight-composition family. The two reported leaders are distinct normalized compositions, not reordered representations. All confidence bounds use the complete family. No pooling, retry, sample extension or additional candidate.

### Equipment controls

| Phase / composition | Baseline | One healer | Full Health + Regeneration | Full Resistance + Health | Ability Haste |
| --- | ---: | ---: | ---: | ---: | ---: |
| screen-1 / A | 13/128 | 40/128 | 4/128 | 8/128 | 34/128 |
| screen-1 / B | 6/128 | 18/128 | 0/128 | 0/128 | 16/128 |

A is original composition `dae6cc32…`, B `0d375ed9…`. These are descriptive per-recipe outcomes, not extra samples or proof of an isolated equipment mechanism.

## Verification

Completed **15,360 fresh fights / 128 reservations**, ending at **921,724 exclusions**. Independently reconstructed every candidate catalog delta, recounted all raw outcomes, verified native preparations in every fight, and reproduced equipment qualification, adjusted bounds, selection, resource admission and the disjoint seed union. All study processes closed without timeout, retry or remaining owned child.

**132 fresh Python safeguard cases and 173 distinct backend cases pass**, with **four intentional opt-in skips**, plus **one native study fixture**. The six Python suites include **17 new refinement safeguards**; version-dispatch tests preserve the old 0.75% contract and reject unknown versions. **Eight new native cases** cover 0.9% stack/MaxHealth scaling, rounding, interval boundaries, the 60-stack cap, noncritical healing and actual recovery versus overhealing. Backend tests ran through `build/run-tests.ps1` in both the build and bound runtime; these are the same 173 cases, not 346 distinct tests. The new test assembly runs against the original five production assembly hashes. The scoped build with required NuGet configuration access succeeded on its first attempt this turn.

No verification command remains blocked. Native commands, filters, TRX files and process receipts are preserved under the fresh verification and study owners.

## Next work

Close the 128-seed refinement without extension. Next is a separate, frozen **fixed-setting precision proposal**, `TestResults/tower-floor7-endless-spring-precision-proposal-20260930.json` (SHA `64dad3b625254450c2dac6b6bdda549ded37a54e83fdb1e9f15b1e113a5a1620`). Keep the exact **0.9% recovery / 21.25% target-Health Springtide / 0.20 Power bonus / offense 0.60 / penetration 50**. Use **1,024 fresh seeds per phase**, split into **eight complete-family batches of 128**; only a whole-screen pass admits the independent identical confirmation. The fixed gates become **137–455 / 1,024**. Require two actual limited-equipment compositions and every full-family ceiling. Maximum new scope: **245,760 fights / 2,048 reservations**. No new candidate catalog, panel, seed or replay is allocated by this proposal.

The reason for more samples is uncertainty, not a changed balance target: if B repeated exactly 18/128, the illustrative lower bound would be **9.49% at 512** and **10.66% at 1,024**. Those hypothetical counts are not observations or a guarantee of acceptance. The old failed panel contributes no counts. Freeze a separate eight-batch aggregate contract, preserve the existing four-batch contract, and add corresponding native application guards before allocation. Resource admission and independent audit remain mandatory.

Floor 7 remains unresolved; floor 4 remains the latest applied change. Floors 2, 4 and 6 have accepted limited-equipment routes. After floor 7, review equipment dependence on floors 8–10 and 12–15, then complete the final current-version Tower sweep. No dungeon or acquisition work is implied.

## Changed files and scope

Added the strict recovery candidate helper and seventeen rejection tests; extended version dispatch in the owner CLI/audit; added eight native healing cases. Updated this report, the continuation handoff, balance status, gear coverage, preceding diagnostic notice and both harness guides. All driver, audit, review and verification artifacts are under `TestResults/tower-floor7-endless-spring-refinement-*`. No gameplay catalog changed in this trial. Expected progression, Essence budgets, the search algorithm and other floors remain unchanged. No migrations, configuration, database or deployment changes.

Commands used bundled Python with `-B -X utf8`: prepare; six Python suites; backend build and archived-runtime binding; driver; independent collector; summary/review; publication. Publication checks local Markdown links and `git diff --check`.

## Evidence

- screen-1: `TestResults/tower-balance-pass-floor7-endless-spring-refinement-1-screen-study-20260929`, manifest **`7ef5fdf7b072524fdc17d5f98539c32d5dadd71af03e09d3783e698ba6e2f444`**.
- Independent audit: `TestResults/tower-floor7-endless-spring-refinement-evidence-20260930.json`, SHA **`8134d60f9aba892e1a0b5e2401375c4671c20862a08771130a90dfb94c64831a`**.
- Descriptive summary: `TestResults/tower-floor7-endless-spring-refinement-summary-20260930.json`, SHA **`1da9a0a78711cade88f72c6563ac800ea367c3ce762692c79b54b20eff0454f2`**.
- Reviewed interpretation: `TestResults/tower-floor7-endless-spring-refinement-review-20260930.json`, SHA **`a4da5503db50679b71c2e2712fef480ec8455cbbee1395755444024cca160e21`**.
- Publication: `TestResults/tower-floor7-endless-spring-refinement-publication-check-20260930.json`.
