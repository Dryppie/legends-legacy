import { DatePipe, NgIf } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, finalize, takeUntil } from 'rxjs';
import { ColosseumService } from '../../../../../core/services/api/colosseum/colosseum.service';
import { BattleType } from '../../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { CombatComponent } from '../../../../../shared/components/combat/combat.component';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../../../../core/services/client-side/combat/combat.service';
import { TournamentPlaybackService } from '../../../../../core/services/client-side/combat/tournament-playback.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';
import {
  TournamentBracket,
  TournamentDetails,
  TournamentMatch,
  TournamentPlaybackBundle,
  TournamentPlaybackManifest,
} from '../../../../../shared/models/Dtos/colosseum/tournamentGrounds';
import { HelpLauncherComponent } from '../../../../../shared/help/help-launcher.component';

@Component({
  selector: 'app-tournament-replay',
  imports: [CombatComponent, DatePipe, NgIf, RouterLink, HelpLauncherComponent],
  templateUrl: './tournament-replay.component.html',
})
export class TournamentReplayComponent implements OnInit, OnDestroy {
  readonly battleType = BattleType.Colosseum;
  readonly tournamentId = signal<string | null>(null);
  readonly matchId = signal<string | null>(null);
  readonly details = signal<TournamentDetails | null>(null);
  readonly bracket = signal<TournamentBracket | null>(null);
  readonly replay = signal<CombatResultDto | null>(null);
  readonly playback = signal<TournamentPlaybackManifest | null>(null);
  readonly currentPlaybackTick = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  private readonly destroyed = new Subject<void>();
  private bundle: TournamentPlaybackBundle | null = null;
  private playbackTimer: ReturnType<typeof setInterval> | null = null;
  private playbackClockAtSync = 0;
  private playbackTickAtSync = 0;
  private lastSequence = -1;
  private lastRealtimeUpdateId: string | null = null;
  private lastReconnectCount = 0;

  readonly round = computed(() => {
    const bracket = this.bracket();
    const matchId = this.matchId();
    return (
      bracket?.rounds.find((round) =>
        round.matches.some((match) => match.id === matchId),
      ) ?? null
    );
  });

  readonly match = computed<TournamentMatch | null>(() => {
    const matchId = this.matchId();
    return this.round()?.matches.find((match) => match.id === matchId) ?? null;
  });

  readonly overtimeState = computed<{
    clock: string;
    powerBonus: number;
  } | null>(() => {
    const playback = this.playback();
    if (!playback) return null;

    const currentTick = this.currentPlaybackTick();
    const overtimeStartsAtTick = playback.overtimeStartsAtTick;
    const overtimeDurationTicks = playback.overtimeDurationTicks;
    const ticksPerSecond = playback.ticksPerSecond;

    if (
      !Number.isFinite(currentTick) ||
      !Number.isFinite(overtimeStartsAtTick) ||
      !Number.isFinite(overtimeDurationTicks) ||
      !Number.isFinite(ticksPerSecond) ||
      overtimeStartsAtTick < 0 ||
      overtimeDurationTicks <= 0 ||
      ticksPerSecond <= 0
    ) {
      return null;
    }

    const overtimeEndsAtTick = overtimeStartsAtTick + overtimeDurationTicks;
    if (
      currentTick < overtimeStartsAtTick ||
      currentTick >= overtimeEndsAtTick
    ) {
      return null;
    }

    const remainingSeconds = Math.max(
      0,
      Math.ceil(
        (overtimeEndsAtTick - currentTick) / ticksPerSecond,
      ),
    );
    const minutes = Math.floor(remainingSeconds / 60);
    const seconds = remainingSeconds % 60;
    const overtimePowerIncreaseIntervalTicks =
      playback.overtimePowerIncreaseIntervalTicks;
    const overtimePowerIncreasePercent =
      playback.overtimePowerIncreasePercent;

    let powerBonus = 0;
    if (
      Number.isFinite(overtimePowerIncreaseIntervalTicks) &&
      overtimePowerIncreaseIntervalTicks > 0 &&
      Number.isFinite(overtimePowerIncreasePercent) &&
      overtimePowerIncreasePercent > 0
    ) {
      const overtimeTicks = Math.max(0, currentTick - overtimeStartsAtTick);
      const stacks = Math.floor(
        overtimeTicks / overtimePowerIncreaseIntervalTicks,
      );
      powerBonus = stacks * overtimePowerIncreasePercent;
    }

    return {
      clock: `${minutes}:${seconds.toString().padStart(2, '0')}`,
      powerBonus: Number.isFinite(powerBonus) ? powerBonus : 0,
    };
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly colosseumService: ColosseumService,
    private readonly combatService: CombatService,
    private readonly playbackService: TournamentPlaybackService,
    private readonly eventService: GameEventService,
    public readonly combatStateService: CombatStateService,
  ) {
    this.lastReconnectCount = this.eventService.reconnectCount();
    effect(
      () => {
        const envelope =
          this.eventService.eventEnvelope.TournamentGroundsUpdated();
        if (
          !envelope?.updateId ||
          envelope.updateId === this.lastRealtimeUpdateId ||
          envelope.payload.tournamentId !== this.tournamentId()
        ) {
          return;
        }

        this.lastRealtimeUpdateId = envelope.updateId;
        const tournamentId = this.tournamentId();
        const matchId = this.matchId();
        if (tournamentId && matchId) this.loadMetadata(tournamentId, matchId);
      },
      { allowSignalWrites: true },
    );
    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount <= this.lastReconnectCount) return;
        this.lastReconnectCount = reconnectCount;
        const tournamentId = this.tournamentId();
        const matchId = this.matchId();
        if (tournamentId && matchId) this.loadMetadata(tournamentId, matchId);
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    const tournamentId = this.route.snapshot.paramMap.get('tournamentId');
    const matchId = this.route.snapshot.paramMap.get('matchId');
    this.tournamentId.set(tournamentId);
    this.matchId.set(matchId);

    if (!tournamentId || !matchId) {
      this.error.set('Replay link is missing tournament or match information.');
      return;
    }

    this.load(tournamentId, matchId);
  }

  ngOnDestroy(): void {
    this.stopPlaybackTimer();
    this.destroyed.next();
    this.destroyed.complete();
  }

  startReplay(): void {
    if (this.bundle) {
      this.startCompactPlayback(0);
      return;
    }
    const replay = this.replay();
    if (!replay) return;

    this.colosseumService.startTournamentReplay({ ...replay });
  }

  skipBattle(): void {
    this.stopPlaybackTimer();
    this.colosseumService.skipColosseumMatch();
  }

  teamLabel(
    team:
      | { name: string; seed?: number | null; memberCount?: number | null }
      | null
      | undefined,
  ): string {
    if (!team) return 'Pending';
    const name = team.seed ? `#${team.seed} ${team.name}` : team.name;
    return team.memberCount ? `${name} (${team.memberCount}/3)` : name;
  }

  outcomeLabel(match: TournamentMatch | null): string {
    if (!match) return 'Replay';
    if (match.status === 'Bye') return 'Advanced by bye';
    if (match.status !== 'Completed') return this.enumLabel(match.status);

    const winner =
      match.winnerTeamId === match.playerOne?.teamId
        ? match.playerOne
        : match.playerTwo;
    return winner ? `${winner.name} advanced` : this.enumLabel(match.outcome);
  }

  enumLabel(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .replace(/_/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }

  private load(tournamentId: string, matchId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.colosseumService
      .getTournament(tournamentId)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (details) => this.details.set(details),
        error: (err: Error) =>
          this.error.set(err.message ?? 'Failed to load tournament details'),
      });

    this.colosseumService
      .getTournamentBracket(tournamentId)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (bracket) => this.bracket.set(bracket),
        error: (err: Error) =>
          this.error.set(err.message ?? 'Failed to load tournament bracket'),
      });

    this.loadPlayback(tournamentId, matchId);
  }

  private loadMetadata(tournamentId: string, matchId: string): void {
    this.colosseumService
      .getTournamentBracket(tournamentId)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (bracket) => this.bracket.set(bracket),
      });
    this.colosseumService
      .getTournamentMatchPlayback(tournamentId, matchId)
      .pipe(takeUntil(this.destroyed))
      .subscribe({ next: (playback) => this.playback.set(playback) });
  }

  private loadPlayback(tournamentId: string, matchId: string): void {
    this.colosseumService
      .getTournamentMatchPlayback(tournamentId, matchId)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (playback) => {
          this.playback.set(playback);
          this.playbackService
            .getBundle(tournamentId, matchId, playback.bundleETag)
            .pipe(
              takeUntil(this.destroyed),
              finalize(() => this.loading.set(false)),
            )
            .subscribe({
              next: (bundle) => {
                if (bundle.schemaVersion !== playback.schemaVersion) {
                  this.error.set(
                    'The Tournament playback format is not supported.',
                  );
                  return;
                }
                this.bundle = bundle;
                const liveTick = playback.isCompleted
                  ? 0
                  : Math.min(
                      bundle.totalTicks,
                      Math.max(
                        0,
                        Math.floor(
                          ((Date.parse(playback.serverNowUtc) -
                            Date.parse(playback.playbackStartedAtUtc)) /
                            1000) *
                            bundle.ticksPerSecond,
                        ),
                      ),
                    );
                this.startCompactPlayback(liveTick);
              },
              error: (err: Error) =>
                this.error.set(
                  err.message ?? 'Failed to load tournament playback',
                ),
            });
        },
        error: (err: Error) =>
          this.loadLegacyReplay(tournamentId, matchId, err),
      });
  }

  private loadLegacyReplay(
    tournamentId: string,
    matchId: string,
    playbackError: Error,
  ): void {
    this.colosseumService
      .getTournamentMatchReplay(tournamentId, matchId)
      .pipe(
        takeUntil(this.destroyed),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (replay) => {
          this.replay.set(replay);
          this.colosseumService.startTournamentReplay({ ...replay });
        },
        error: () =>
          this.error.set(
            playbackError.message ?? 'Failed to load tournament replay',
          ),
      });
  }

  private startCompactPlayback(startTick: number): void {
    if (!this.bundle) return;
    this.stopPlaybackTimer();
    this.playbackTickAtSync = startTick;
    this.playbackClockAtSync = performance.now();
    this.lastSequence = -1;
    this.renderCompactPlayback(true);
    if (startTick < this.bundle.totalTicks) {
      this.playbackTimer = setInterval(
        () => this.renderCompactPlayback(false),
        250,
      );
    }
  }

  private renderCompactPlayback(reset: boolean): void {
    const bundle = this.bundle;
    if (!bundle) return;
    const tick = Math.min(
      bundle.totalTicks,
      this.playbackTickAtSync +
        Math.floor(
          ((performance.now() - this.playbackClockAtSync) / 1000) *
            bundle.ticksPerSecond,
        ),
    );
    this.currentPlaybackTick.set(tick);
    const frame = this.playbackService.frameAtTick(bundle, tick);
    if (frame.sequence !== this.lastSequence || reset) {
      this.lastSequence = frame.sequence;
      this.combatService.applyTournamentCombatFrame(frame, reset);
    }
    if (tick >= bundle.totalTicks) this.stopPlaybackTimer();
  }

  private stopPlaybackTimer(): void {
    if (this.playbackTimer === null) return;
    clearInterval(this.playbackTimer);
    this.playbackTimer = null;
  }
}
