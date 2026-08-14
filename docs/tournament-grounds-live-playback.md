# Tournament Grounds live playback

Status: **Implemented in the repository — migration and production rollout pending**

Last updated: 2026-08-14

## Match cadence

- Bracket generation assigns every non-bye match a fixed start slot.
- Rounds begin at intervals controlled by `MatchIntervalMinutes`, which defaults to 10 minutes.
- Every non-bye match in the Round of 32, Round of 16, and quarter-final begins together at its round boundary.
- The two semifinal matches are staggered by one interval; the final begins one interval after the second semifinal.
- If a worker starts a match batch late, that delay is cascaded to every remaining slot so the configured cadence is preserved.
- A progression execution starts every match assigned to the next due slot.
- A match remains `Resolving` until its authoritative playback end timestamp.
- Regulation lasts up to five minutes. If neither team has won, the battle enters a visible five-minute overtime phase.
- During overtime, both teams gain another cumulative 10% effective Power every 10 seconds. The first increase occurs 10 seconds into overtime.
- A battle can end early during either phase; ten minutes is the maximum duration.
- If combat ends in a draw, the team with the greater total opponent damage advances. Equal damage falls back to the higher seed so the single-elimination bracket always progresses deterministically.
- Winner/loser updates, bracket advancement, battle-completed events, placements, and rewards occur only after playback ends.
- PostgreSQL advisory locks and persisted background-job execution guards continue to serialize tournament progression across workers.

For a standard 32-team bracket, R32 starts at tournament start, R16 at +10 minutes, the quarter-final at +20, the semifinals at +30 and +40, and the final at +50. Byes do not consume a battle slot.

## Combat and playback

- Tournament combat uses the compact combat-engine capture path with the generic event log disabled.
- The server captures health, barrier, entity totals, and ability totals at checkpoint intervals.
- Static entity and ability data is indexed once in the bundle.
- The bundle is serialized once, Brotli-compressed once, hashed with SHA-256, and stored in `TournamentCombatReplayArtifacts`.
- Replay metadata remains in `TournamentCombatReplays`; bracket and progression queries do not load the artifact bytes.
- The bundle endpoint uses a strong ETag and private immutable caching.

## Spectating and recovery

- Any authenticated player can view a Tournament match; participation is not required.
- `Resolving` matches expose a **Spectate** link in the bracket.
- The browser aligns playback with `ServerNowUtc` and `PlaybackStartedAtUtc`, seeks directly to the current frame, and advances locally.
- Playback metadata carries the authoritative overtime boundary, duration, and Power ramp; the combat view displays an `Overtime` countdown and current Power bonus.
- Refreshing, navigating away and back, tab throttling, and realtime reconnection recover by reloading small metadata and reusing the immutable bundle.
- Completed matches use the same artifact as a replay and start from tick zero.

## Configuration

```json
{
  "Colosseum": {
    "TournamentGrounds": {
      "ProgressionIntervalSeconds": 10,
      "MatchIntervalMinutes": 10,
      "RegulationDurationMinutes": 5,
      "OvertimeDurationMinutes": 5,
      "OvertimePowerIncreaseIntervalSeconds": 10,
      "OvertimePowerIncreasePercent": 10,
      "CombatTicksPerFrame": 10,
      "MaximumBundleUncompressedBytes": 16777216,
      "MaximumBundleCompressedBytes": 4194304
    }
  }
}
```

## Deployment

Apply migration `20260813191644_OptimizeTournamentGroundsPlayback` before deploying the API and Worker binaries. The migration has been generated and validated in the repository but must not be applied from development tooling to a shared environment.
