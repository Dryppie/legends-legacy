# Earned early Tower clears and supply unlocks — 29 September 2026

Continuation: the [post-unlock entry qualification](Tower-Upgrade-Entry-Qualification-20260929.md) now restores personal funding and prepares all sixteen owners on each earned server path. Ninety-two owner/server combinations can fund an Epic-eligible next entry under the declared coverage policy; no new gear is earned. Full native prophecy runtime restoration remains necessary before reward-producing continuation.

Fifteen of 32 fixed chronological paths cleared floors 1–3 using actual earned checkpoint parties. Native finalization then made Epic supplies eligible for future successful regional dungeon completions, including for an eligible nonparticipant. This establishes a conditional path through the first server supply gate. It does **not** earn the Epic gear, establish a player success probability or measure acquisition time.

The source is the [earned-party archive](Tower-Earned-Party-Progression-20260929.md), pinned at `d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2`. All eight earned-growth alternatives at 25,920 idle encounters continue, with four separately seeded paths each. These are overlapping parties from sixteen personal owners, not 32 independently acquired player groups. The 72-hour label describes assumed idle cadence; prior dungeon combat and party assembly also contribute to the modeled starting clock. No historical Tower win was imported.

## Observed results

Each isolated model server starts with only floor 1 available. A path attempts the next floor only after native finalization records a victory. It stops after four unsuccessful attempts at one floor, or its first floor-3 victory. It receives no extra idle activity, levels, Essences, equipment, donations or manual preparation between attempts.

| Prior idle outcome assumption | Stopped at floor 1 | Stopped at floor 2 | Stopped at floor 3 | Cleared floor 3 |
| --- | ---: | ---: | ---: | ---: |
| Perfect victories | 0 | 5 | 3 | 8 |
| Four victories in five | 4 | 5 | 0 | 7 |
| Total | 4 | 10 | 3 | 15 |

| Floor | Paths reaching it | Combat attempts | Victories |
| --- | ---: | ---: | ---: |
| 1 | 32 | 51 | 28 |
| 2 | 28 | 70 | 18 |
| 3 | 18 | 27 | 15 |

The study made **148 attempts and 78 matching full playback replays, 226 fights total**. Replays cover the first attempt on every reached floor. The four-attempt cap is an evaluation bound, not a production restriction or proof that the stopped parties can never win. Later-floor counts are conditional on reaching those floors; they are not interchangeable population rates.

The eight party alternatives show why a seven-purchase rule would overstate the entry requirement:

| Prior idle assumption | Roster rotation | Personally earned supply counts, in party order | Floor-3 clears / four paths |
| --- | ---: | --- | ---: |
| Perfect | 0 | 4, 7, 0, 7, 7 | 4 |
| Perfect | 1 | 7, 7, 7, 7, 6 | 4 |
| Perfect | 2 | 0, 7, 7, 0, 0 | 0 |
| Perfect | 3 | 7, 0, 0, 0, 0 | 0 |
| Four in five | 0 | 3, 7, 0, 7, 6 | 3 |
| Four in five | 1 | 7, 7, 7, 7, 6 | 4 |
| Four in five | 2 | 0, 0, 7, 0, 0 | 0 |
| Four in five | 3 | 7, 0, 0, 0, 0 | 0 |

These characters are levels 34–36, with four earned Essences and their retained ordinary and supply items. Zero supply purchases on a member does not mean zero equipment. Ownership, exact equipped identities, stronger stored items, ordered Essences and trained levels stay unchanged. The richer alternatives also differ in gear, growth and identities; these observations do not isolate a causal effect of purchase count.

Successful paths consumed about **191.3–326.5 seconds of native Tower playback**. That omits prior acquisition, attendance, recruitment, action input, polling, simulation latency, scheduling and server contention. It is not an estimate of time to earn Epic gear. There are **zero measured player samples**.

## Native persistence and reward boundary

The [server fixture](../LL/tests/EssenceSystem.Tests/TowerUnlockServer.cs) uses the existing isolated EF InMemory provider. It calls the real `WorldTowerService.FinalizePlaybackAsync`, repositories, title/achievement services, state synchronization service and supply service with production settings and catalogs. It constructs persisted playback records from fresh production combat outcomes; it does not exercise recruitment, live account eligibility, playback bundle transport, relational transactions or cross-server concurrency. Account eligibility is explicitly assumed. No API host or real database connection is created; announcement records stay in the test outbox.

Every attempt verifies rejection just before playback ends, successful finalization at its end, and a rejected retry with unchanged rewards/state. Native first clears unlock the next floor, award production TowerTokens to the five participants, and grant cosmetic titles without equipping them. Tokens remain unspent. Earned XP is restored from the exact source checkpoint, including its completed dungeon claims, and remains unchanged during this continuation. Failed attempts grant no clear, token or title. Production scouting increases by ten for each of the first three persisted failures per floor that week; the count query excludes the current unsaved failure. Scouting reveals information and does not alter these fixed combat preparations.

The real supply service queries native server progress. Before floor 3 clears, all six eligible probe owners receive a Rare decision for a hypothetical new completed dungeon. After a clear, that decision is Epic for all six, including the earned owner outside the party. Retrying each pre-Tower completion retains its original Rare decision. These probes are eligibility checks only: no sigil is spent, no dungeon is simulated, no chest is claimed/opened and **no new equipment enters owned inventory**. Epic eligibility cannot be credited as seven earned Epic pieces.

## Reproduction and verification

The new [harness adapter](../LL/tools/BalanceHarness/TowerUnlockStudy.cs), [assumption fixture](../LL/tools/BalanceHarness/Fixtures/tower-early-unlock.json), [tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerUnlockTests.cs), [bounded owner](analysis/run-tower-unlock.py) and [independent verifier](analysis/verify-tower-unlock.py) add this continuation without modifying production behavior. The owner first freezes 24 preparations through production equipment/Tower preparation. Combat requires their passing independent audit and explicit manifest pin. Source, content, test runtime and persistence dependencies are frozen and hash checked.

| Artifact | SHA-256 |
| --- | --- |
| [Preparation manifest](../TestResults/tower-unlock-preparation-study-20260929/files.json) | `f0a8c59bfffe98c7f03ff7ee280a95b93f265f66d8fc4b7b1516793e3999436b` |
| [Preparation result](../TestResults/tower-unlock-preparation-study-20260929/result.json) | `8014323df8cd9db88081707ef0e4cad99d507ed57e64bc329a7508978a68bb2d` |
| [Combat manifest](../TestResults/tower-unlock-study-20260929/files.json) | `fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52` |
| [Combat result](../TestResults/tower-unlock-study-20260929/result.json) | `0537f38c2faa74a97450357a87238888da87dfc696d2e6d610912d37000d7c74` |
| [Seed ledger](../TestResults/tower-unlock-owner-20260929/seed-ledger.json) | `4094923d5ca85548df97c30d00046eb26935279bed7c3dd03ecc060be9862dff` |

The declared ceiling was 384 fresh seeds and 480 fights, including at most 96 replays. **236 unconsumed reservations remain excluded** alongside all older reservations; the union is now **877,831**. The admission guard rejects renamed reruns of this closed predecessor union. No retries or extensions were run. A follow-up must declare this ledger as a predecessor, not rename the same experiment.

The [preparation audit](../TestResults/tower-unlock-preparation-owner-20260929/independent-audit.json) checks 2,706 frozen hashes and all 24 parties. The [combat audit](../TestResults/tower-unlock-owner-20260929/independent-audit.json) checks 2,735 hashes, chronological attempts, replay equality, seed exclusions, original checkpoint XP, native tokens/titles/scouting/unlocks, outsider eligibility and immutable prior decisions. [Derived tables](../TestResults/tower-unlock-owner-20260929/analysis.json) retain every party/path result.

The original combat verifier failed on comments in production `appsettings.json`. An explicit [verifier-only amendment](../TestResults/tower-unlock-owner-20260929/verifier-amendment-v1.json) adds comment-aware parsing and exact 100ns timestamp handling, allowing only native `AddSeconds` truncation of at most one tick per attempt. It changes no outcome, preparation, settings, reservation or gameplay duration. The failed log and frozen original verifier remain preserved. Original verifier SHA: `92e666ec97abae61992937bda8317402df768a2c09de22f55fbedf6ceb7cc687`; amended SHA: `cd0e6723479bf7a892fa1715b77d0f0471634949b13b18cac954c7b84c1189d9`.

**580 regression tests passed, sixteen intentional opt-in skips**, plus one passing test for each owned study. The [regression log](../TestResults/tower-unlock-regression-20260929.log) and [preserved TRX](../TestResults/tower-unlock-regression-20260929.trx) record the full filter. The initial sandbox NuGet configuration access failure succeeded with authorized wrapper access; two development fixture errors were corrected before freezing. No required check remains blocked. Preparation completed in 4.531 process seconds with 13,977,135 output bytes; combat completed in 17.188 seconds with 19,630,293 bytes. Both stayed within their 300/900-second process limits, 240/840-second native limits, 256 MiB output and 1 MiB log limits, with zero active children. Process time is not player time.

Original commands (existing paths intentionally reject reruns):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-unlock-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessTowerUnlockTests'
# The broader -NoBuild regression filter is retained in the regression TRX/log and closeout record.
$env:PYTHONDONTWRITEBYTECODE='1'
$towerPython='C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $towerPython 'Balance Harness/analysis/run-tower-unlock.py' --mode prepare --owner TestResults/tower-unlock-preparation-owner-20260929 --output TestResults/tower-unlock-preparation-study-20260929 --artifacts TestResults/tower-unlock-build-20260929
& $towerPython 'TestResults/tower-unlock-preparation-owner-20260929/inputs/Balance Harness/analysis/verify-tower-unlock.py' --owner TestResults/tower-unlock-preparation-owner-20260929 --manifest-pin f0a8c59bfffe98c7f03ff7ee280a95b93f265f66d8fc4b7b1516793e3999436b --receipt TestResults/tower-unlock-preparation-owner-20260929/independent-audit.json
& $towerPython 'Balance Harness/analysis/run-tower-unlock.py' --mode combat --owner TestResults/tower-unlock-owner-20260929 --output TestResults/tower-unlock-study-20260929 --artifacts TestResults/tower-unlock-build-20260929 --qualification-owner TestResults/tower-unlock-preparation-owner-20260929 --qualification-pin f0a8c59bfffe98c7f03ff7ee280a95b93f265f66d8fc4b7b1516793e3999436b
& $towerPython 'TestResults/tower-unlock-owner-20260929/amended-verifier-v1/verify-tower-unlock.py' --owner TestResults/tower-unlock-owner-20260929 --manifest-pin fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52 --receipt TestResults/tower-unlock-owner-20260929/independent-audit.json
```

## Next dependency

Continue personal resource and reward histories from these exact checkpoint states and earned server results. Qualify an explicit funded upgrade policy first: remaining per-owner sigils/fragments/currency, entry snapshot level, ordinary items, persistent mastery, pending XP, prophecy state and model time must carry forward. Epic eligibility applies to the server; all eligible owners can benefit without participating in its clear. Older pending chests retain their band. Do not import later archived Rare-only dungeon outcomes after a changed reward/gear state, or reuse the old seven-award terminal condition as a mandate to replace everything. Retain every stronger owned item and evaluate newly earned loadouts before fresh bounded combat.

Later earned levels, additional Essence sources, larger parties and tier-2 equipment remain necessary model dependencies toward floors 10–11. The user-defined repeating gear curve and supported search `affinity-creation-with-benchmark-validation-v1` stay unchanged. No boss tuning, production source/configuration/dependency change, migration, shared database access, API startup or deployment occurred. Original supply rollout requirements still apply. All 251 pre-existing modified/untracked files were unchanged before the three intentional documentation updates; historical archives and unrelated LiveOps/analytics work remain preserved. See [closeout checks](../TestResults/tower-unlock-owner-20260929/final-checks.json).
