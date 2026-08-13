# Tournament Grounds live playback

Status: **Implemented in the repository — migration and production rollout pending**

Last updated: 2026-08-13

## Match cadence

- Bracket generation assigns every non-bye match a fixed start slot.
- Slots are separated by `MatchIntervalMinutes`, which defaults to 10 minutes.
- If a worker starts a match late, that delay is cascaded to every remaining slot so catch-up processing never launches matches closer than ten minutes apart.
- A progression execution may start at most one match.
- A match remains `Resolving` until its authoritative playback end timestamp.
- Winner/loser updates, bracket advancement, battle-completed events, placements, and rewards occur only after playback ends.
- PostgreSQL advisory locks and persisted background-job execution guards continue to serialize tournament progression across workers.

For a standard four-team bracket, the three battles start at tournament start, +10 minutes, and +20 minutes. Byes do not consume a battle slot.

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
- Refreshing, navigating away and back, tab throttling, and realtime reconnection recover by reloading small metadata and reusing the immutable bundle.
- Completed matches use the same artifact as a replay and start from tick zero.

## Configuration

```json
{
  "Colosseum": {
    "TournamentGrounds": {
      "ProgressionIntervalSeconds": 10,
      "MatchIntervalMinutes": 10,
      "CombatTicksPerFrame": 10,
      "MaximumBundleUncompressedBytes": 16777216,
      "MaximumBundleCompressedBytes": 4194304
    }
  }
}
```

## Deployment

Apply migration `20260813191644_OptimizeTournamentGroundsPlayback` before deploying the API and Worker binaries. The migration has been generated and validated in the repository but must not be applied from development tooling to a shared environment.
