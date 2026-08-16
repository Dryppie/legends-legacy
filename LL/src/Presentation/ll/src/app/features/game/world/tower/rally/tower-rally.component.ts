import { CommonModule } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  effect,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, finalize, fromEvent, takeUntil } from 'rxjs';
import {
  TowerAttemptResult,
  TowerBattleReport,
  TowerCombatPlayback,
  TowerCombatFrame,
  TowerPlaybackBundle,
  TowerRally,
  TowerRallyApplication,
  TowerRallyParticipant,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { CombatComponent } from '../../../../../shared/components/combat/combat.component';
import { CombatService } from '../../../../../core/services/client-side/combat/combat.service';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../../core/state/combat-state/combatState';
import { TowerPlaybackService } from '../../../../../core/services/client-side/combat/tower-playback.service';

@Component({
  selector: 'app-tower-rally',
  imports: [CommonModule, RouterLink, CharacterTagComponent, CombatComponent],
  templateUrl: './tower-rally.component.html',
  styleUrl: '../tower-page.scss',
})
export class TowerRallyComponent implements OnInit, OnDestroy {
  private readonly tower = inject(WorldTowerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly events = inject(GameEventService);
  private readonly combat = inject(CombatService);
  private readonly playbackPlayer = inject(TowerPlaybackService);
  readonly combatState = inject(CombatStateService);
  private readonly destroyed = new Subject<void>();
  readonly rally = signal<TowerRally | null>(null);
  readonly result = signal<TowerAttemptResult | null>(null);
  readonly loading = signal(true);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly battleType = BattleType.Tower;
  readonly watchingPlayback = signal(false);
  readonly playback = signal<TowerCombatPlayback | null>(null);
  readonly realtimeStatus = this.events.connectionStatus;
  private rallyId = '';
  private lastRealtimeUpdateId: string | null = null;
  private lastCombatFrameUpdateId: string | null = null;
  private lastCombatSequence = -1;
  private lastReconnectCount = this.events.reconnectCount();
  private recoveringFrames = false;
  private pendingRealtimeFrame: TowerCombatFrame | null = null;
  private compactBundle: TowerPlaybackBundle | null = null;
  private serverClockAtSync = 0;
  private monotonicClockAtSync = 0;
  private playbackTimer: ReturnType<typeof setInterval> | null = null;
  private lastFinalizationRefreshAt = 0;

  constructor() {
    effect(
      () => {
        const envelope = this.events.eventEnvelope.WorldTowerRallyUpdated();
        if (
          !envelope?.updateId ||
          envelope.updateId === this.lastRealtimeUpdateId ||
          envelope.payload.rallyId !== this.rallyId
        ) {
          return;
        }

        this.lastRealtimeUpdateId = envelope.updateId;
        this.load(false);
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const envelope =
          this.events.eventEnvelope.WorldTowerCombatFrameUpdated();
        if ((this.playback()?.schemaVersion ?? 1) >= 2) return;
        if (
          !envelope?.updateId ||
          envelope.updateId === this.lastCombatFrameUpdateId ||
          envelope.payload.rallyId !== this.rallyId ||
          envelope.payload.frame.sequence <= this.lastCombatSequence
        ) {
          return;
        }

        this.lastCombatFrameUpdateId = envelope.updateId;
        if (envelope.payload.frame.sequence > this.lastCombatSequence + 1) {
          this.pendingRealtimeFrame = envelope.payload.frame;
          this.recoverMissingFrames();
          return;
        }
        this.applyCombatFrame(envelope.payload.frame);
        if (envelope.payload.frame.isFinal) this.load(false);
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const reconnectCount = this.events.reconnectCount();
        if (reconnectCount <= this.lastReconnectCount) return;
        this.lastReconnectCount = reconnectCount;
        if ((this.playback()?.schemaVersion ?? 1) >= 2) this.load(false);
        else if (this.playback()) this.recoverMissingFrames();
        else this.load(false);
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    fromEvent(document, 'visibilitychange')
      .pipe(takeUntil(this.destroyed))
      .subscribe(() => this.renderCompactPlayback(true));
    this.route.paramMap.pipe(takeUntil(this.destroyed)).subscribe((params) => {
      this.stopCompactPlayback();
      this.compactBundle = null;
      this.lastCombatSequence = -1;
      this.rallyId = params.get('rallyId') ?? '';
      this.load();
    });
  }

  ngOnDestroy(): void {
    this.stopCompactPlayback();
    this.destroyed.next();
    this.destroyed.complete();
  }

  load(showLoading = true): void {
    if (showLoading) this.loading.set(true);
    this.error.set(null);
    this.tower
      .getRally(this.rallyId)
      .pipe(finalize(() => showLoading && this.loading.set(false)))
      .subscribe({
        next: (rally) => {
          if (!rally) {
            void this.router.navigate(['/game/world/tower']);
            return;
          }
          this.rally.set(rally);
          if (rally.attempt?.playback) {
            this.acceptPlayback(
              rally.attempt.playback,
              !this.playback() && !rally.attempt.playback.isCompleted,
            );
          }
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  apply(): void {
    this.runRallyAction('apply', this.tower.applyToRally(this.rallyId));
  }

  accept(application: TowerRallyApplication): void {
    this.runRallyAction(
      `accept-${application.id}`,
      this.tower.acceptApplication(this.rallyId, application.id),
    );
  }

  decline(application: TowerRallyApplication): void {
    this.runRallyAction(
      `decline-${application.id}`,
      this.tower.declineApplication(this.rallyId, application.id),
    );
  }

  leave(): void {
    if (this.action()) return;
    this.action.set('leave');
    this.error.set(null);
    this.tower
      .leaveRally(this.rallyId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (rally) => {
          if (rally.status === 'Cancelled') {
            void this.router.navigate(['/game/world/tower']);
          } else {
            this.rally.set(rally);
          }
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  updateLoadout(): void {
    this.runRallyAction(
      'update-loadout',
      this.tower.updateRallyLoadout(this.rallyId),
    );
  }

  transferLeadership(participant: TowerRallyParticipant): void {
    this.runRallyAction(
      `transfer-leadership-${participant.characterId}`,
      this.tower.transferRallyLeadership(this.rallyId, participant.characterId),
    );
  }

  fillDevelopmentRoster(): void {
    this.runRallyAction(
      'development-fill',
      this.tower.fillDevelopmentRoster(this.rallyId),
    );
  }

  start(): void {
    if (this.action()) return;
    this.action.set('start');
    this.error.set(null);
    this.tower
      .startRally(this.rallyId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (result) => {
          this.result.set(result);
          if (result.playback) this.acceptPlayback(result.playback, true);
          this.load(false);
        },
        error: (error) => {
          this.error.set(this.errorMessage(error));
          this.load();
        },
      });
  }

  viewCombatDetails(): void {
    const currentPlayback = this.playback();
    if (currentPlayback && !currentPlayback.isCompleted) {
      this.watchingPlayback.set(true);
      if (currentPlayback.schemaVersion >= 2) {
        this.renderCompactPlayback(true);
      } else if (currentPlayback.currentFrame) {
        this.combat.applyTowerCombatFrame(currentPlayback.currentFrame, true);
      }
      return;
    }

    const attemptId = this.result()?.attemptId ?? this.rally()?.attempt?.id;
    if (!attemptId || this.action()) return;

    this.action.set('combat-result');
    this.error.set(null);
    this.tower
      .getAttemptCombatResult(attemptId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (combatResult) =>
          this.combat.startTowerBattleSummary({ ...combatResult }),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  closeCombatDetails(): void {
    this.watchingPlayback.set(false);
    this.combat.closeCurrentTowerBattle();
  }

  report(): TowerBattleReport | null {
    return this.rally()?.attempt?.battleReport ?? null;
  }

  private acceptPlayback(
    playback: TowerCombatPlayback,
    openViewer: boolean,
  ): void {
    if (playback.schemaVersion >= 2 && playback.bundleETag) {
      this.playback.set(playback);
      this.serverClockAtSync = playback.serverNow
        ? Date.parse(playback.serverNow)
        : Date.now();
      this.monotonicClockAtSync = performance.now();
      if (openViewer) this.watchingPlayback.set(true);
      this.playbackPlayer
        .getBundle(playback.attemptId, playback.bundleETag)
        .pipe(takeUntil(this.destroyed))
        .subscribe({
          next: (bundle) => {
            if (bundle.schemaVersion !== playback.schemaVersion) {
              this.error.set('The Tower playback format is not supported.');
              return;
            }
            this.compactBundle = bundle;
            this.renderCompactPlayback(true);
            if (!playback.isCompleted) this.startCompactPlayback();
            else this.stopCompactPlayback();
          },
          error: (error) => this.error.set(this.errorMessage(error)),
        });
      return;
    }

    if (playback.currentSequence < this.lastCombatSequence) return;
    this.lastCombatSequence = playback.currentSequence;
    this.playback.set(playback);
    if (openViewer) this.watchingPlayback.set(true);
    if (this.watchingPlayback()) {
      if (playback.currentFrame)
        this.combat.applyTowerCombatFrame(playback.currentFrame, true);
    }
  }

  private recoverMissingFrames(): void {
    const attemptId = this.playback()?.attemptId ?? this.rally()?.attempt?.id;
    if (!attemptId || this.recoveringFrames) return;
    this.recoveringFrames = true;
    this.fetchMissingFrames(attemptId);
  }

  private fetchMissingFrames(attemptId: string): void {
    this.tower
      .getAttemptPlaybackFrames(attemptId, this.lastCombatSequence)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (batch) => {
          for (const frame of batch.frames) this.applyCombatFrame(frame);
          if (batch.hasMore) {
            this.fetchMissingFrames(attemptId);
            return;
          }
          this.recoveringFrames = false;
          const pendingFrame = this.pendingRealtimeFrame;
          this.pendingRealtimeFrame = null;
          if (pendingFrame && pendingFrame.sequence > this.lastCombatSequence) {
            if (pendingFrame.sequence === this.lastCombatSequence + 1) {
              this.applyCombatFrame(pendingFrame);
            } else {
              this.pendingRealtimeFrame = pendingFrame;
              this.recoverMissingFrames();
            }
          }
          if (batch.frames.at(-1)?.isFinal) this.load(false);
        },
        error: () => {
          this.recoveringFrames = false;
          this.load(false);
        },
      });
  }

  private applyCombatFrame(frame: TowerCombatFrame): void {
    if (frame.sequence <= this.lastCombatSequence) return;
    this.lastCombatSequence = frame.sequence;
    this.playback.update((current) =>
      current
        ? {
            ...current,
            currentSequence: frame.sequence,
            currentFrame: frame,
            isCompleted: frame.isFinal,
          }
        : current,
    );
    if (this.watchingPlayback()) this.combat.applyTowerCombatFrame(frame);
  }

  private startCompactPlayback(): void {
    if (this.playbackTimer !== null) return;
    this.playbackTimer = setInterval(
      () => this.renderCompactPlayback(false),
      250,
    );
  }

  private stopCompactPlayback(): void {
    if (this.playbackTimer === null) return;
    clearInterval(this.playbackTimer);
    this.playbackTimer = null;
  }

  private renderCompactPlayback(reset: boolean): void {
    const playback = this.playback();
    const bundle = this.compactBundle;
    if (!playback || playback.schemaVersion < 2 || !bundle) return;

    const serverNow =
      this.serverClockAtSync +
      (performance.now() - this.monotonicClockAtSync);
    const elapsedMilliseconds = Math.max(
      0,
      serverNow - Date.parse(playback.playbackStartedAt),
    );
    const targetTick = Math.min(
      bundle.totalTicks,
      Math.floor((elapsedMilliseconds / 1000) * bundle.ticksPerSecond),
    );
    const frame = this.playbackPlayer.frameAtTick(bundle, targetTick);
    if (frame.sequence !== this.lastCombatSequence || reset) {
      this.lastCombatSequence = frame.sequence;
      this.playback.update((current) =>
        current
          ? {
              ...current,
              currentSequence: frame.sequence,
              currentFrame: frame,
            }
          : current,
      );
      if (this.watchingPlayback())
        this.combat.applyTowerCombatFrame(frame, reset);
    }

    if (
      serverNow >= Date.parse(playback.playbackEndsAt) &&
      !playback.isCompleted &&
      Date.now() - this.lastFinalizationRefreshAt >= 1000
    ) {
      this.lastFinalizationRefreshAt = Date.now();
      this.load(false);
    }
  }

  duration(seconds: number | null | undefined): string {
    const total = Math.max(0, seconds ?? 0);
    const minutes = Math.floor(total / 60);
    const remaining = total % 60;
    return `${minutes}:${remaining.toString().padStart(2, '0')}`;
  }

  openSlots(rally: TowerRally): number[] {
    return Array.from(
      { length: Math.max(0, rally.requiredSlots - rally.participants.length) },
      (_, index) => rally.participants.length + index + 1,
    );
  }

  currentApplication(rally: TowerRally): TowerRallyApplication | null {
    return (
      rally.applications.find(
        (application) => application.isCurrentCharacter,
      ) ?? null
    );
  }

  private runRallyAction(
    label: string,
    request: ReturnType<WorldTowerService['applyToRally']>,
  ): void {
    if (this.action()) return;
    this.action.set(label);
    this.error.set(null);
    request.pipe(finalize(() => this.action.set(null))).subscribe({
      next: (rally) => this.rally.set(rally),
      error: (error) => this.error.set(this.errorMessage(error)),
    });
  }

  private errorMessage(error: unknown): string {
    return (
      (error as { errorMessage?: string })?.errorMessage ??
      'The Expedition action could not be completed.'
    );
  }
}
