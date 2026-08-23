import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  EventEmitter,
  OnDestroy,
  OnInit,
  effect,
  inject,
  Input,
  isDevMode,
  Output,
  signal,
} from '@angular/core';
import { finalize, Subscription, timer } from 'rxjs';
import {
  RegionBossService,
  RegionBossStatus,
  RegionBossPlaybackBundle,
} from '../../../../core/services/api/region-boss/region-boss.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { RegionBossPlaybackService } from '../../../../core/services/client-side/combat/region-boss-playback.service';
import { GameRealtimeEventRegistry } from '../../../../core/services/real-time/game-realtime/game-realtime-event-registry.service';
import { RealtimeSignalDeduper } from '../../../../core/services/real-time/game-realtime/realtime-deduplication';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CombatReviveCountdown } from '../../../../shared/models/Dtos/combatResultDto';
import { LocalDatePipe } from '../../../../shared/pipes/local-date/local-date.pipe';

@Component({
  selector: 'app-region-boss',
  imports: [CommonModule, CombatComponent, LocalDatePipe],
  templateUrl: './region-boss.component.html',
  styleUrl: './region-boss.component.scss',
})
export class RegionBossComponent implements OnInit, OnDestroy {
  @Input() regionId: number | null = null;
  @Input() embedded = false;
  @Output() readonly leave = new EventEmitter<void>();
  @Output() readonly playbackViewChange = new EventEmitter<boolean>();

  private readonly service = inject(RegionBossService);
  private readonly combat = inject(CombatService);
  private readonly playbackPlayer = inject(RegionBossPlaybackService);
  private readonly realtimeEvents = inject(GameRealtimeEventRegistry);
  private readonly realtimeDeduper = new RealtimeSignalDeduper();
  readonly combatState = inject(CombatStateService);
  readonly battleType = BattleType.RegionBoss;
  readonly events = signal<RegionBossStatus[]>([]);
  readonly loading = signal(false);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly currentTime = signal(Date.now());
  readonly lastUpdatedAt = signal<number | null>(null);
  readonly watchingPlayback = signal(false);
  readonly currentPlaybackEvent = signal<RegionBossStatus | null>(null);
  readonly currentPlaybackFrame = signal<
    RegionBossPlaybackBundle['frames'][number] | null
  >(null);
  readonly reviveCountdowns = computed<CombatReviveCountdown[]>(() => {
    const frame = this.currentPlaybackFrame();
    if (!frame || frame.isFinal) return [];

    const ticksPerSecond = Math.max(
      1,
      this.activePlaybackBundle?.ticksPerSecond ?? 10,
    );
    return (frame.context?.downed ?? []).map((downed) => ({
      entityId: downed.entityId,
      remainingSeconds: downed.remainingTicks / ticksPerSecond,
    }));
  });
  readonly developmentMode = isDevMode();
  private pollSubscription: Subscription | null = null;
  private clockSubscription: Subscription | null = null;
  private playbackTimer: ReturnType<typeof setInterval> | null = null;
  private activePlaybackBundle: RegionBossPlaybackBundle | null = null;
  private playbackRunId: string | null = null;
  private playbackLoadRunId: string | null = null;
  private dismissedPlaybackRunId: string | null = null;
  private serverClockAtSync = 0;
  private monotonicClockAtSync = 0;
  private playbackStartedAt = 0;
  private lastPlaybackSequence = -1;

  constructor() {
    effect(() => {
      const envelope = this.realtimeEvents.eventEnvelope.RegionBossUpdated();
      if (
        !envelope?.updateId ||
        !this.realtimeDeduper.shouldProcess('region-boss', envelope)
      ) {
        return;
      }

      this.load();
    });
  }

  ngOnInit(): void {
    this.pollSubscription = timer(0, 15_000).subscribe(() => this.load());
    this.clockSubscription = timer(0, 1_000).subscribe(() =>
      this.currentTime.set(Date.now()),
    );
  }

  ngOnDestroy(): void {
    this.pollSubscription?.unsubscribe();
    this.clockSubscription?.unsubscribe();
    this.stopPlaybackTimer();
    if (this.watchingPlayback()) this.playbackViewChange.emit(false);
    this.combat.closeCurrentRegionBossBattle();
  }

  load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.service
      .getStatus(this.regionId ?? undefined)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (events) => this.acceptEvents(events),
        error: (error) =>
          this.error.set(
            error?.errorMessage ?? 'Could not load Region Boss events.',
          ),
      });
  }

  signup(event: RegionBossStatus): void {
    this.mutate(`signup-${event.eventId}`, this.service.signup(event.eventId));
  }

  withdraw(event: RegionBossStatus): void {
    this.mutate(
      `withdraw-${event.eventId}`,
      this.service.withdraw(event.eventId),
    );
  }

  claim(grantId: string): void {
    this.action.set(`claim-${grantId}`);
    this.service
      .claim(grantId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: () => this.load(),
        error: (error) =>
          this.error.set(error?.errorMessage ?? 'Could not claim the reward.'),
      });
  }

  spawnDevelopment(regionValue: string, signupCountValue: string): void {
    if (this.action()) return;
    const regionId = Number.parseInt(regionValue, 10);
    const additionalSignupCount = Number.parseInt(signupCountValue, 10);
    if (
      regionId <= 0 ||
      additionalSignupCount < 0 ||
      additionalSignupCount > 95
    ) {
      this.error.set(
        'Enter a valid region and between 0 and 95 simulated participants.',
      );
      return;
    }

    this.action.set('development-spawn');
    this.error.set(null);
    this.service
      .spawnDevelopment(regionId, additionalSignupCount)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (event) =>
          this.acceptEvents([
            event,
            ...this.events().filter((item) => item.eventId !== event.eventId),
          ]),
        error: (error) =>
          this.error.set(
            error?.errorMessage ??
              'Could not spawn the local Region Boss event.',
          ),
      });
  }

  progress(event: RegionBossStatus): number {
    return (event.run?.currentBossProgressBasisPoints ?? 0) / 100;
  }

  activeEvent(): RegionBossStatus | null {
    return (
      this.events().find(
        (event) =>
          event.isSignedUp &&
          !!event.run &&
          (event.status === 'Matching' ||
            event.status === 'Resolving' ||
            event.status === 'Playback'),
      ) ?? null
    );
  }

  upcomingEvents(): RegionBossStatus[] {
    return this.events()
      .filter(
        (event) =>
          event.status === 'Scheduled' || event.status === 'SignupOpen',
      )
      .sort(
        (left, right) =>
          Date.parse(left.encounterStartsAtUtc) -
          Date.parse(right.encounterStartsAtUtc),
      );
  }

  settledEvents(): RegionBossStatus[] {
    const sevenDaysAgo = this.currentTime() - 7 * 24 * 60 * 60 * 1_000;
    return this.events()
      .filter(
        (event) =>
          event.status === 'Settled' &&
          !!event.run &&
          Date.parse(event.encounterStartsAtUtc) >= sevenDaysAgo,
      )
      .sort(
        (left, right) =>
          Date.parse(right.encounterStartsAtUtc) -
          Date.parse(left.encounterStartsAtUtc),
      )
      .slice(0, 6);
  }

  encountersThisWeek(): number {
    const now = new Date(this.currentTime());
    const dayOffset = (now.getUTCDay() + 6) % 7;
    const weekStart = Date.UTC(
      now.getUTCFullYear(),
      now.getUTCMonth(),
      now.getUTCDate() - dayOffset,
    );
    const weekEnd = weekStart + 7 * 24 * 60 * 60 * 1_000;
    return this.events().filter(
      (event) =>
        event.status !== 'Cancelled' &&
        Date.parse(event.encounterStartsAtUtc) >= weekStart &&
        Date.parse(event.encounterStartsAtUtc) < weekEnd,
    ).length;
  }

  displayRegionId(): number {
    return this.regionId ?? this.events()[0]?.regionId ?? 1;
  }

  displayBossName(): string {
    return (
      this.activeEvent()?.name ??
      this.upcomingEvents()[0]?.name ??
      this.settledEvents()[0]?.name ??
      'Region Boss'
    );
  }

  timeUntil(value: string): string {
    const remainingSeconds = Math.max(
      0,
      Math.ceil((Date.parse(value) - this.currentTime()) / 1_000),
    );
    if (remainingSeconds < 60) return `${remainingSeconds}s`;
    const minutes = Math.floor(remainingSeconds / 60);
    if (minutes < 60) return `${minutes}m ${remainingSeconds % 60}s`;
    const hours = Math.floor(minutes / 60);
    return `${hours}h ${minutes % 60}m`;
  }

  timeSinceLastUpdate(): string {
    const updatedAt = this.lastUpdatedAt();
    if (updatedAt === null) return 'Updating…';
    const seconds = Math.max(
      0,
      Math.floor((this.currentTime() - updatedAt) / 1_000),
    );
    if (seconds < 60) return `Updated ${seconds}s ago`;
    return `Updated ${Math.floor(seconds / 60)}m ago`;
  }

  partyCount(event: RegionBossStatus): number {
    return Math.max(1, Math.ceil(event.signupCount / 5));
  }

  isClosingSoon(event: RegionBossStatus): boolean {
    const remaining = Date.parse(event.signupClosesAtUtc) - this.currentTime();
    return event.status === 'SignupOpen' && remaining <= 60_000;
  }

  bossHealthPercent(event: RegionBossStatus): number {
    const maxHealth = this.bossMaxHealth(event);
    if (maxHealth <= 0) return 0;
    return Math.max(
      0,
      Math.min(100, (this.bossHealthRemaining(event) / maxHealth) * 100),
    );
  }

  bossLevel(event: RegionBossStatus): number {
    return (
      this.liveBossFrame(event)?.context?.waveNumber ??
      event.run?.currentBossLevel ??
      1
    );
  }

  bossHealthRemaining(event: RegionBossStatus): number {
    return (
      this.liveBossFrame(event)?.hostile?.[0]?.health ??
      event.run?.currentBossHealthRemaining ??
      0
    );
  }

  bossMaxHealth(event: RegionBossStatus): number {
    return (
      this.liveBossFrame(event)?.hostile?.[0]?.maxHealth ??
      event.run?.currentBossMaxHealth ??
      0
    );
  }

  totalDeaths(event: RegionBossStatus): number {
    return (
      event.run?.members.reduce(
        (total, member) => total + (member.result?.deaths ?? 0),
        0,
      ) ?? 0
    );
  }

  resultLabel(event: RegionBossStatus): string {
    return (event.run?.highestLevelDefeated ?? 0) > 0 ? 'Victory' : 'Defeated';
  }

  rewardLabel(event: RegionBossStatus): string {
    const cinders = event.rewards.reduce(
      (total, reward) => total + reward.cinders,
      0,
    );
    const soulstones = event.rewards.reduce(
      (total, reward) => total + reward.soulstones,
      0,
    );
    if (cinders === 0 && soulstones === 0) return 'No rewards';
    return `${cinders.toLocaleString()} cinders · ${soulstones.toLocaleString()} soulstones`;
  }

  loadPlayback(runId: string): void {
    const event = this.events().find((item) => item.run?.runId === runId);
    if (!event) {
      this.error.set('Could not find this Region Boss battle.');
      return;
    }

    this.dismissedPlaybackRunId = null;
    this.startPlayback(event, event.status === 'Playback');
  }

  playbackTitle(): string {
    const event = this.currentPlaybackEvent();
    const level = this.currentPlaybackFrame()?.context?.waveNumber;
    return `${event?.name ?? 'Region Boss'}${level ? ` · Level ${level}` : ''}`;
  }

  closePlayback(): void {
    this.dismissedPlaybackRunId = this.playbackRunId;
    this.stopPlaybackTimer();
    this.watchingPlayback.set(false);
    this.playbackViewChange.emit(false);
    this.combat.closeCurrentRegionBossBattle();
  }

  private liveBossFrame(
    event: RegionBossStatus,
  ): RegionBossPlaybackBundle['frames'][number] | null {
    if (
      event.status !== 'Playback' ||
      this.currentPlaybackEvent()?.eventId !== event.eventId
    ) {
      return null;
    }

    return this.currentPlaybackFrame();
  }

  private mutate(
    key: string,
    request: ReturnType<RegionBossService['signup']>,
  ): void {
    if (this.action()) return;
    this.action.set(key);
    this.error.set(null);
    request.pipe(finalize(() => this.action.set(null))).subscribe({
      next: (event) =>
        this.acceptEvents(
          this.events().map((item) =>
            item.eventId === event.eventId ? event : item,
          ),
        ),
      error: (error) =>
        this.error.set(error?.errorMessage ?? 'The Region Boss action failed.'),
    });
  }

  private acceptEvents(events: RegionBossStatus[]): void {
    this.events.set(events);
    this.lastUpdatedAt.set(Date.now());
    const liveEvent = events.find(
      (event) =>
        event.status === 'Playback' &&
        event.isSignedUp &&
        event.run?.hasPlayback,
    );
    if (
      liveEvent?.run &&
      liveEvent.run.runId !== this.dismissedPlaybackRunId &&
      liveEvent.run.runId !== this.playbackRunId &&
      liveEvent.run.runId !== this.playbackLoadRunId
    ) {
      this.startPlayback(liveEvent, true);
    }
  }

  private startPlayback(
    event: RegionBossStatus,
    synchronizeToEvent: boolean,
  ): void {
    const runId = event.run?.runId;
    if (!runId || this.playbackLoadRunId === runId) return;

    this.playbackLoadRunId = runId;
    this.action.set(`playback-${runId}`);
    this.error.set(null);
    this.playbackPlayer
      .getBundle(runId)
      .pipe(
        finalize(() => {
          this.playbackLoadRunId = null;
          if (this.action() === `playback-${runId}`) this.action.set(null);
        }),
      )
      .subscribe({
        next: (bundle) => {
          if (!bundle.frames.length) {
            this.error.set('The Region Boss battle contains no combat frames.');
            return;
          }

          this.stopPlaybackTimer();
          this.activePlaybackBundle = bundle;
          this.playbackRunId = runId;
          this.currentPlaybackEvent.set(event);
          this.lastPlaybackSequence = -1;
          this.serverClockAtSync = synchronizeToEvent
            ? Date.parse(event.serverNowUtc)
            : Date.now();
          this.monotonicClockAtSync = performance.now();
          this.playbackStartedAt =
            synchronizeToEvent && event.playbackStartsAtUtc
              ? Date.parse(event.playbackStartsAtUtc)
              : this.serverClockAtSync;
          this.watchingPlayback.set(true);
          this.playbackViewChange.emit(true);
          this.renderPlayback(true);
          if (!this.currentPlaybackFrame()?.isFinal) {
            this.playbackTimer = setInterval(
              () => this.renderPlayback(false),
              250,
            );
          }
        },
        error: (error) =>
          this.error.set(
            error?.errorMessage ?? 'Could not load combat playback.',
          ),
      });
  }

  private renderPlayback(reset: boolean): void {
    const bundle = this.activePlaybackBundle;
    if (!bundle) return;

    const serverNow =
      this.serverClockAtSync + (performance.now() - this.monotonicClockAtSync);
    const elapsedMilliseconds = Math.max(0, serverNow - this.playbackStartedAt);
    const targetTick = Math.min(
      bundle.totalTicks,
      Math.floor((elapsedMilliseconds / 1000) * bundle.ticksPerSecond),
    );
    const frame = this.playbackPlayer.frameAtTick(bundle, targetTick);
    if (frame.sequence === this.lastPlaybackSequence && !reset) return;

    this.lastPlaybackSequence = frame.sequence;
    const playbackFrame = bundle.frames.find(
      (item) => item.sequence === frame.sequence,
    );
    this.currentPlaybackFrame.set(
      playbackFrame
        ? {
            ...playbackFrame,
            friendly: frame.friendly,
            hostile: frame.hostile,
            entityStats: frame.entityStats,
            events: frame.events,
          }
        : null,
    );
    if (this.watchingPlayback()) {
      this.combat.applyRegionBossCombatFrame(frame, reset);
    }
    if (frame.isFinal) this.stopPlaybackTimer();
  }

  private stopPlaybackTimer(): void {
    if (this.playbackTimer === null) return;
    clearInterval(this.playbackTimer);
    this.playbackTimer = null;
  }
}
