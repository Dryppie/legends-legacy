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
import {
  Subject,
  finalize,
  forkJoin,
  interval,
  startWith,
  takeUntil,
} from 'rxjs';
import {
  RaidLane,
  RaidJoinRequest,
  RaidBattlePlanPreview,
  RaidPlaybackBundle,
  RaidReward,
  RaidRun,
  RaidService,
  RaidSignup,
} from '../../../../core/services/api/raid/raid.service';
import { GameEventService } from '../../../../core/services/real-time/game-event.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import {
  RaidCombatFrame,
  RaidPlaybackService,
} from '../../../../core/services/client-side/combat/raid-playback.service';
import { CharacterTagComponent } from '../../../../shared/components/character/character-tag/character-tag.component';
import {
  RaidPlaybackComponent,
  RaidPreparationPlaybackView,
} from './playback/raid-playback.component';
import {
  RaidPartyAssignment,
  RaidPartyBuilderComponent,
} from './party-builder/raid-party-builder.component';

@Component({
  selector: 'app-raid-page',
  imports: [
    CommonModule,
    RouterLink,
    CharacterTagComponent,
    RaidPlaybackComponent,
    RaidPartyBuilderComponent,
  ],
  templateUrl: './raid-page.component.html',
  styleUrls: ['../tower/tower-page.scss', './raid-page.component.scss'],
})
export class RaidPageComponent implements OnInit, OnDestroy {
  private readonly raids = inject(RaidService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly events = inject(GameEventService);
  private readonly combat = inject(CombatService);
  private readonly playbackPlayer = inject(RaidPlaybackService);
  readonly combatState = inject(CombatStateService);
  private readonly destroyed = new Subject<void>();
  readonly raid = signal<RaidRun | null>(null);
  readonly loading = signal(true);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly reward = signal<RaidReward | null>(null);
  readonly battlePlan = signal<RaidBattlePlanPreview | null>(null);
  readonly battleType = BattleType.Raid;
  readonly watchingPlayback = signal(false);
  readonly playbackLane = signal<RaidLane | null>(null);
  readonly showingAllPreparations = signal(false);
  readonly preparationViews = signal<RaidPreparationPlaybackView[]>([]);
  readonly rearguardWaveNumber = signal(1);
  readonly playbackStageIndex = signal(-1);
  readonly lanes: RaidLane[] = ['Rearguard', 'Vanguard', 'MainGuard'];
  readonly raidPlaybackOrder: RaidLane[] = [
    'Rearguard',
    'Vanguard',
    'MainGuard',
    'FinalAssault',
  ];
  private raidRunId = '';
  private lastRealtimeUpdateId: string | null = null;
  private lastReconnectCount = this.events.reconnectCount();
  private playbackTimer: ReturnType<typeof setInterval> | null = null;
  private playbackAdvanceTimer: ReturnType<typeof setTimeout> | null = null;
  private activePlaybackBundle: RaidPlaybackBundle | null = null;
  private readonly preparationBundles = new Map<RaidLane, RaidPlaybackBundle>();
  private activePlaybackLanes: RaidLane[] = [];
  private focusPreparationOnLoad: RaidLane | null = null;
  private lastFocusedPreparationLane: RaidLane | null = null;
  private preparationPhaseDurationMilliseconds = 0;
  private playbackStartedAt = 0;
  private lastPlaybackFrameSequence = -1;
  private lastPlaybackWasWaveTransitionHold = false;
  private autoPlaybackRequested = false;
  private autoPlaybackStarted = false;
  private playbackGeneration = 0;
  private scheduledPlaybackStartedAt: number | null = null;
  private playbackServerClockAtSync = 0;
  private playbackMonotonicClockAtSync = 0;
  private playbackStageOffsetMilliseconds = 0;
  private readonly playbackTransitionMilliseconds = 1500;
  // Keep aligned with RaidService.RearguardWaveTransitionHold.
  private readonly rearguardWaveTransitionHoldMilliseconds = 1000;
  private readonly playbackStageDurations = new Map<RaidLane, number>();

  constructor() {
    effect(() => {
      const envelope = this.events.eventEnvelope.RaidUpdated();
      if (
        !envelope?.updateId ||
        envelope.updateId === this.lastRealtimeUpdateId ||
        envelope.payload.raidRunId !== this.raidRunId
      ) {
        return;
      }

      this.lastRealtimeUpdateId = envelope.updateId;
      this.load(false);
    });

    effect(() => {
      const reconnectCount = this.events.reconnectCount();
      if (reconnectCount <= this.lastReconnectCount) return;
      this.lastReconnectCount = reconnectCount;
      this.load(false);
    });
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntil(this.destroyed)).subscribe((params) => {
      this.raidRunId = params.get('raidId') ?? '';
      this.load();
    });
    interval(5000)
      .pipe(startWith(0), takeUntil(this.destroyed))
      .subscribe(() => {
        const status = this.raid()?.status;
        if (status === 'Resolving' || status === 'Playback') this.load(false);
      });
  }

  ngOnDestroy(): void {
    this.closeRaidPlayback();
    this.destroyed.next();
    this.destroyed.complete();
  }

  load(showLoading = true): void {
    if (!this.raidRunId) return;
    if (showLoading) this.loading.set(true);
    this.raids
      .getRaid(this.raidRunId)
      .pipe(finalize(() => showLoading && this.loading.set(false)))
      .subscribe({
        next: (raid) => {
          if (!raid) {
            void this.router.navigate(this.worldPath());
            return;
          }
          this.acceptRaid(raid);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  join(): void {
    this.runAction('join', this.raids.join(this.raidRunId));
  }

  approveRequest(request: RaidJoinRequest): void {
    if (this.action()) return;
    this.runAction(
      `approve-${request.characterId}`,
      this.raids.approveSignup(this.raidRunId, request.characterId),
    );
  }

  removeSignup(
    signup: Pick<RaidSignup, 'characterId' | 'characterName'> | RaidJoinRequest,
    pending = false,
  ): void {
    const action = pending ? 'Decline' : 'Remove';
    if (
      this.action() ||
      !window.confirm(
        `${action} ${signup.characterName}${pending ? "'s request" : ' from this raid'}?`,
      )
    )
      return;
    this.runAction(
      `remove-${signup.characterId}`,
      this.raids.removeSignup(this.raidRunId, signup.characterId),
    );
  }

  leave(): void {
    if (this.action()) return;
    this.action.set('leave');
    this.raids
      .leave(this.raidRunId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (raid) => {
          if (
            raid.status === 'Cancelled' ||
            !raid.signups.some((x) => x.isCurrentCharacter)
          ) {
            void this.router.navigate(this.worldPath());
          } else {
            this.raid.set(raid);
          }
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  refreshLoadout(): void {
    this.runAction('loadout', this.raids.refreshLoadout(this.raidRunId));
  }

  cancelRaid(): void {
    if (
      this.action() ||
      !window.confirm(
        'Cancel this raid? All participants will be removed from the muster.',
      )
    )
      return;
    this.action.set('cancel');
    this.error.set(null);
    this.raids
      .cancel(this.raidRunId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (raid) => {
          this.raid.set(raid);
          void this.router.navigate(this.worldPath());
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  transferLeadership(signup: RaidSignup): void {
    if (
      this.action() ||
      !window.confirm(`Transfer raid leadership to ${signup.characterName}?`)
    )
      return;
    this.runAction(
      `transfer-${signup.characterId}`,
      this.raids.transferLeadership(this.raidRunId, signup.characterId),
    );
  }

  commence(): void {
    this.autoPlaybackRequested = true;
    this.runAction('commence', this.raids.commence(this.raidRunId));
  }

  previewBattlePlan(): void {
    if (this.action()) return;
    this.action.set('battle-plan');
    this.error.set(null);
    this.raids
      .previewBattlePlan(this.raidRunId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (preview) => this.battlePlan.set(preview),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  watchPlayback(lane: RaidLane): void {
    this.startRaidPlayback([lane]);
  }

  canWatchPlayback(lane: RaidLane): boolean {
    return (
      this.raid()?.laneResults.some(
        (result) => result.lane === lane && result.hasPlayback,
      ) ?? false
    );
  }

  replayRaid(): void {
    this.startRaidPlayback(this.raidPlaybackOrder);
  }

  raidDifficultyLabel(plusLevel: number): string {
    return plusLevel === 0 ? 'Regular' : `+${plusLevel}`;
  }

  raidBattleTitle(): string {
    const lane = this.playbackLane();
    const raid = this.raid();
    if (!lane || !raid) return 'Raid Battle';
    const stage = this.playbackStageIndex() + 1;
    const totalStages = this.activePlaybackLanes.some(
      (activeLane) => activeLane !== 'FinalAssault',
    )
      ? 2
      : 1;
    return `${raid.raidBossName} · ${this.raidEncounterName(lane)} · Battle ${stage}/${totalStages}`;
  }

  raidEnemyName(): string {
    switch (this.playbackLane()) {
      case 'Rearguard':
        return `Reinforcements · Wave ${this.rearguardWaveNumber()}`;
      case 'Vanguard':
        return 'Guardian';
      case 'MainGuard':
        return 'Boss Projection';
      case 'FinalAssault':
        return this.raid()?.raidBossName ?? 'Raid Boss';
      default:
        return 'Hostiles';
    }
  }

  raidWingName(): string {
    const lane = this.playbackLane();
    return lane === 'FinalAssault'
      ? 'Combined Raid'
      : `${this.raidEncounterName(lane)} Party`;
  }

  raidEncounterName(lane: RaidLane | null): string {
    switch (lane) {
      case 'MainGuard':
        return 'Main Guard';
      case 'FinalAssault':
        return 'Final Assault';
      case 'Rearguard':
        return 'Rearguard';
      case 'Vanguard':
        return 'Vanguard';
      default:
        return 'Raid';
    }
  }

  preparationSummaryLocked(): boolean {
    const lane = this.playbackLane();
    if (!lane || lane === 'FinalAssault') return false;

    const views = this.preparationViews();
    return (
      views.find((view) => view.lane === lane)?.completed === true &&
      views.some((view) => !view.completed)
    );
  }

  closeRaidPlayback(): void {
    this.playbackGeneration++;
    this.stopPlaybackTimers();
    this.activePlaybackBundle = null;
    this.preparationBundles.clear();
    this.activePlaybackLanes = [];
    this.focusPreparationOnLoad = null;
    this.lastFocusedPreparationLane = null;
    this.preparationPhaseDurationMilliseconds = 0;
    this.playbackLane.set(null);
    this.showingAllPreparations.set(false);
    this.preparationViews.set([]);
    this.rearguardWaveNumber.set(1);
    this.playbackStageIndex.set(-1);
    this.watchingPlayback.set(false);
    this.scheduledPlaybackStartedAt = null;
    this.playbackStageOffsetMilliseconds = 0;
    this.lastPlaybackWasWaveTransitionHold = false;
    this.playbackStageDurations.clear();
    this.combat.closeCurrentRaidBattle();
  }

  focusPreparation(lane: RaidLane): void {
    if (!this.showingAllPreparations() && this.playbackLane() === lane) return;
    const view = this.preparationViews().find((item) => item.lane === lane);
    const bundle = this.preparationBundles.get(lane);
    if (!view || !bundle) return;

    this.showingAllPreparations.set(false);
    this.playbackLane.set(lane);
    this.lastFocusedPreparationLane = lane;
    this.activePlaybackBundle = bundle;
    this.lastPlaybackFrameSequence = view.frame.sequence;
    this.lastPlaybackWasWaveTransitionHold = view.isWaveTransitionHold;
    if (view.frame.waveNumber !== null)
      this.rearguardWaveNumber.set(view.frame.waveNumber);
    this.combat.applyRaidCombatFrame(view.frame, true);
  }

  showAllPreparations(): void {
    if (!this.preparationViews().length || this.showingAllPreparations())
      return;
    this.showingAllPreparations.set(true);
    this.playbackLane.set(null);
    this.activePlaybackBundle = null;
    this.lastPlaybackFrameSequence = -1;
    this.combat.closeCurrentRaidBattle();
  }

  showOnePreparation(): void {
    if (!this.showingAllPreparations()) return;
    const lane =
      this.lastFocusedPreparationLane ?? this.preparationViews()[0]?.lane;
    if (lane) this.focusPreparation(lane);
  }

  claim(): void {
    if (this.action()) return;
    this.action.set('claim');
    this.raids
      .claim(this.raidRunId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (reward) => {
          this.reward.set(reward);
          this.load(false);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  hasPendingJoinRequest(raid: RaidRun): boolean {
    return raid.joinRequests.some((request) => request.isCurrentCharacter);
  }

  raidStatusLabel(status: RaidRun['status']): string {
    switch (status) {
      case 'Mustering':
        return 'Recruiting';
      case 'Resolving':
      case 'Playback':
        return 'In battle';
      case 'Settled':
      case 'Resolved':
        return 'Complete';
      default:
        return 'Closed';
    }
  }

  raidStatusBadge(status: RaidRun['status']): string {
    switch (status) {
      case 'Mustering':
        return 'Rallying';
      case 'Resolving':
      case 'Playback':
        return 'InProgress';
      case 'Settled':
      case 'Resolved':
        return 'Succeeded';
      default:
        return 'Cancelled';
    }
  }

  autoBalanceParties(raid: RaidRun): void {
    const parties = this.lanes.map((lane) => ({
      lane,
      totalPower: 0,
      count: 0,
    }));
    const assignments = [...raid.signups]
      .sort(
        (left, right) =>
          right.powerRating - left.powerRating ||
          left.characterName.localeCompare(right.characterName),
      )
      .map((signup) => {
        const party = [...parties]
          .filter((candidate) => candidate.count < raid.laneSlots)
          .sort(
            (left, right) =>
              left.totalPower - right.totalPower ||
              left.count - right.count ||
              this.lanes.indexOf(left.lane) - this.lanes.indexOf(right.lane),
          )[0];
        const assignment = {
          characterId: signup.characterId,
          lane: party?.lane ?? null,
          wingSlotIndex: party?.count ?? null,
        };
        if (party) {
          party.count++;
          party.totalPower += signup.powerRating;
        }
        return assignment;
      });
    this.savePartyAssignments(assignments);
  }

  resetParties(raid: RaidRun): void {
    this.savePartyAssignments(
      raid.signups.map((signup) => ({
        characterId: signup.characterId,
        lane: null,
        wingSlotIndex: null,
      })),
    );
  }

  updatePartyAssignments(assignments: readonly RaidPartyAssignment[]): void {
    this.savePartyAssignments(assignments);
  }

  participantName(characterId: string): string {
    return (
      this.raid()?.signups.find((x) => x.characterId === characterId)
        ?.characterName ?? 'Unknown'
    );
  }

  closesIn(value: string): string {
    const ms = new Date(value).getTime() - Date.now();
    if (ms <= 0) return 'closed';
    const hours = Math.floor(ms / 3_600_000);
    const minutes = Math.max(1, Math.floor((ms % 3_600_000) / 60_000));
    return `${hours}h ${minutes}m`;
  }

  worldPath(): string[] {
    return ['/game/world', this.raid()?.region === 2 ? 'meran' : 'shenic'];
  }

  worldName(): string {
    return this.raid()?.region === 2 ? 'Meran' : 'Shenic';
  }

  private runAction(
    key: string,
    request: ReturnType<RaidService['join']>,
  ): void {
    if (this.action()) return;
    this.action.set(key);
    this.error.set(null);
    request.pipe(finalize(() => this.action.set(null))).subscribe({
      next: (raid) => this.acceptRaid(raid),
      error: (error) => this.error.set(this.errorMessage(error)),
    });
  }

  private savePartyAssignments(
    assignments: ReadonlyArray<{
      characterId: string;
      lane: RaidLane | null;
      wingSlotIndex: number | null;
    }>,
  ): void {
    if (this.action()) return;
    this.action.set('parties');
    this.error.set(null);
    this.raids
      .updateParties(this.raidRunId, assignments)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (raid) => {
          this.acceptRaid(raid);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  private acceptRaid(raid: RaidRun): void {
    if (raid.status === 'Resolving' || raid.status === 'Playback')
      this.autoPlaybackRequested = true;
    this.raid.set(raid);
    if (
      raid.status === 'Playback' &&
      this.autoPlaybackRequested &&
      !this.autoPlaybackStarted
    ) {
      this.autoPlaybackStarted = true;
      queueMicrotask(() =>
        this.startRaidPlayback(this.raidPlaybackOrder, raid),
      );
    }
  }

  private startRaidPlayback(
    lanes: readonly RaidLane[],
    schedule?: RaidRun,
  ): void {
    if (this.action() || !lanes.length) return;
    this.closeRaidPlayback();
    this.setPlaybackSchedule(schedule);
    const preparationLane = lanes.find((lane) => lane !== 'FinalAssault');
    const includesPreparations = preparationLane !== undefined;
    this.activePlaybackLanes = includesPreparations
      ? [
          ...this.lanes,
          ...(lanes.includes('FinalAssault')
            ? (['FinalAssault'] as const)
            : []),
        ]
      : ['FinalAssault'];
    this.focusPreparationOnLoad =
      lanes.length === 1 && preparationLane ? preparationLane : null;
    this.watchingPlayback.set(true);
    if (includesPreparations) this.loadPreparationPhase();
    else this.loadFinalAssault(0);
  }

  private loadPreparationPhase(): void {
    this.stopPlaybackTimers();
    this.playbackStageIndex.set(0);
    this.playbackLane.set(null);
    this.showingAllPreparations.set(true);
    this.action.set('playback-preparations');
    this.error.set(null);
    const generation = this.playbackGeneration;
    forkJoin(
      this.lanes.map((lane) =>
        this.raids.getPlaybackBundle(this.raidRunId, lane),
      ),
    )
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (bundles) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.lanes.forEach((lane, index) => {
            const bundle = bundles[index];
            this.preparationBundles.set(lane, bundle);
            this.playbackStageDurations.set(
              lane,
              this.playbackDurationMilliseconds(lane, bundle),
            );
          });
          this.preparationPhaseDurationMilliseconds = Math.max(
            ...this.lanes.map(
              (lane) => this.playbackStageDurations.get(lane) ?? 0,
            ),
          );
          const stageElapsedMilliseconds =
            this.scheduledPlaybackElapsedMilliseconds() ?? 0;
          if (
            stageElapsedMilliseconds >=
            this.preparationPhaseDurationMilliseconds +
              this.playbackTransitionMilliseconds
          ) {
            queueMicrotask(() => this.advanceAfterPreparations());
            return;
          }

          this.playbackStartedAt =
            performance.now() -
            Math.min(
              stageElapsedMilliseconds,
              this.preparationPhaseDurationMilliseconds,
            );
          this.lastPlaybackFrameSequence = -1;
          this.lastPlaybackWasWaveTransitionHold = false;
          this.renderPreparationFrames(true);
          if (
            this.focusPreparationOnLoad &&
            stageElapsedMilliseconds < this.preparationPhaseDurationMilliseconds
          ) {
            this.focusPreparation(this.focusPreparationOnLoad);
          }
          if (
            stageElapsedMilliseconds < this.preparationPhaseDurationMilliseconds
          ) {
            this.playbackTimer = setInterval(
              () => this.renderPreparationFrames(false),
              250,
            );
          }
        },
        error: (error) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.error.set(this.errorMessage(error));
          this.closeRaidPlayback();
        },
      });
  }

  private renderPreparationFrames(reset: boolean): void {
    const elapsedMilliseconds = Math.max(
      0,
      performance.now() - this.playbackStartedAt,
    );
    const views = this.lanes.flatMap((lane) => {
      const bundle = this.preparationBundles.get(lane);
      if (!bundle) return [];
      const durationMilliseconds = this.playbackStageDurations.get(lane) ?? 0;
      const laneElapsedMilliseconds = Math.min(
        elapsedMilliseconds,
        durationMilliseconds,
      );
      const playbackPosition =
        lane === 'Rearguard'
          ? this.playbackPlayer.playbackPositionAtElapsed(
              bundle,
              laneElapsedMilliseconds,
              this.rearguardWaveTransitionHoldMilliseconds,
            )
          : {
              combatTick: Math.min(
                bundle.totalTicks,
                Math.floor(
                  (laneElapsedMilliseconds / 1000) * bundle.ticksPerSecond,
                ),
              ),
              isWaveTransitionHold: false,
            };
      const frame = this.playbackPlayer.frameAtTick(
        bundle,
        playbackPosition.combatTick,
        playbackPosition.isWaveTransitionHold,
      );
      const completed = elapsedMilliseconds >= durationMilliseconds;
      return [
        this.toPreparationView(
          lane,
          frame,
          laneElapsedMilliseconds,
          durationMilliseconds,
          completed,
          playbackPosition.isWaveTransitionHold,
        ),
      ];
    });
    this.preparationViews.set(views);

    const focusedLane = this.playbackLane();
    if (focusedLane && !this.showingAllPreparations()) {
      const focused = views.find((view) => view.lane === focusedLane);
      if (
        focused &&
        (reset ||
          focused.frame.sequence !== this.lastPlaybackFrameSequence ||
          focused.isWaveTransitionHold !==
            this.lastPlaybackWasWaveTransitionHold)
      ) {
        this.lastPlaybackFrameSequence = focused.frame.sequence;
        this.lastPlaybackWasWaveTransitionHold = focused.isWaveTransitionHold;
        if (focused.frame.waveNumber !== null)
          this.rearguardWaveNumber.set(focused.frame.waveNumber);
        this.combat.applyRaidCombatFrame(focused.frame, reset);
      }
    }

    if (
      elapsedMilliseconds < this.preparationPhaseDurationMilliseconds ||
      this.playbackAdvanceTimer !== null
    )
      return;

    if (this.playbackTimer !== null) clearInterval(this.playbackTimer);
    this.playbackTimer = null;
    const scheduledElapsed = this.scheduledPlaybackElapsedMilliseconds();
    const transitionDelay =
      scheduledElapsed === null
        ? this.playbackTransitionMilliseconds
        : Math.max(
            0,
            this.preparationPhaseDurationMilliseconds +
              this.playbackTransitionMilliseconds -
              scheduledElapsed,
          );
    this.playbackAdvanceTimer = setTimeout(() => {
      this.playbackAdvanceTimer = null;
      this.advanceAfterPreparations();
    }, transitionDelay);
  }

  private advanceAfterPreparations(): void {
    if (this.activePlaybackLanes.includes('FinalAssault')) {
      this.loadFinalAssault(
        this.preparationPhaseDurationMilliseconds +
          this.playbackTransitionMilliseconds,
      );
      return;
    }
    this.finishRaidPlayback();
  }

  private loadFinalAssault(stageOffsetMilliseconds: number): void {
    this.stopPlaybackTimers();
    this.showingAllPreparations.set(false);
    this.preparationViews.set([]);
    this.combat.closeCurrentRaidBattle();
    this.playbackStageIndex.set(
      this.activePlaybackLanes.some((lane) => lane !== 'FinalAssault') ? 1 : 0,
    );
    this.playbackLane.set('FinalAssault');
    this.action.set('playback-FinalAssault');
    this.error.set(null);
    const generation = this.playbackGeneration;
    this.raids
      .getPlaybackBundle(this.raidRunId, 'FinalAssault')
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (bundle) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.activePlaybackBundle = bundle;
          const durationMilliseconds = this.playbackDurationMilliseconds(
            'FinalAssault',
            bundle,
          );
          this.playbackStageDurations.set('FinalAssault', durationMilliseconds);
          this.playbackStageOffsetMilliseconds = stageOffsetMilliseconds;
          const elapsedMilliseconds = Math.max(
            0,
            (this.scheduledPlaybackElapsedMilliseconds() ??
              stageOffsetMilliseconds) - stageOffsetMilliseconds,
          );
          if (
            elapsedMilliseconds >=
            durationMilliseconds + this.playbackTransitionMilliseconds
          ) {
            queueMicrotask(() => this.finishRaidPlayback());
            return;
          }
          this.playbackStartedAt =
            performance.now() -
            Math.min(elapsedMilliseconds, durationMilliseconds);
          this.lastPlaybackFrameSequence = -1;
          this.renderFinalAssaultFrame(true);
          if (elapsedMilliseconds < durationMilliseconds) {
            this.playbackTimer = setInterval(
              () => this.renderFinalAssaultFrame(false),
              250,
            );
          }
        },
        error: (error) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.error.set(this.errorMessage(error));
          this.closeRaidPlayback();
        },
      });
  }

  private renderFinalAssaultFrame(reset: boolean): void {
    const bundle = this.activePlaybackBundle;
    if (!bundle) return;
    const elapsedMilliseconds = Math.max(
      0,
      performance.now() - this.playbackStartedAt,
    );
    const targetTick = Math.min(
      bundle.totalTicks,
      Math.floor((elapsedMilliseconds / 1000) * bundle.ticksPerSecond),
    );
    const frame = this.playbackPlayer.frameAtTick(bundle, targetTick);
    if (reset || frame.sequence !== this.lastPlaybackFrameSequence) {
      this.lastPlaybackFrameSequence = frame.sequence;
      this.combat.applyRaidCombatFrame(frame, reset);
    }
    if (targetTick < bundle.totalTicks || this.playbackAdvanceTimer !== null)
      return;

    if (this.playbackTimer !== null) clearInterval(this.playbackTimer);
    this.playbackTimer = null;
    const durationMilliseconds =
      this.playbackStageDurations.get('FinalAssault') ??
      (bundle.totalTicks / bundle.ticksPerSecond) * 1000;
    const scheduledElapsed = this.scheduledPlaybackElapsedMilliseconds();
    const transitionDelay =
      scheduledElapsed === null
        ? this.playbackTransitionMilliseconds
        : Math.max(
            0,
            this.playbackStageOffsetMilliseconds +
              durationMilliseconds +
              this.playbackTransitionMilliseconds -
              scheduledElapsed,
          );
    this.playbackAdvanceTimer = setTimeout(() => {
      this.playbackAdvanceTimer = null;
      this.finishRaidPlayback();
    }, transitionDelay);
  }

  private finishRaidPlayback(): void {
    const completedScheduledPlayback = this.scheduledPlaybackStartedAt !== null;
    this.closeRaidPlayback();
    if (completedScheduledPlayback) this.load(false);
  }

  private toPreparationView(
    lane: RaidLane,
    frame: RaidCombatFrame,
    elapsedMilliseconds: number,
    durationMilliseconds: number,
    completed: boolean,
    isWaveTransitionHold: boolean,
  ): RaidPreparationPlaybackView {
    const friendlyAlive = frame.friendly.filter(
      (entity) => entity.health > 0,
    ).length;
    const hostileHealth = frame.hostile.reduce(
      (total, entity) => total + Math.max(0, entity.health),
      0,
    );
    const hostileMaxHealth = frame.hostile.reduce(
      (total, entity) => total + Math.max(0, entity.maxHealth),
      0,
    );
    const encounterProgress =
      hostileMaxHealth > 0
        ? Math.max(
            0,
            Math.min(100, 100 - (hostileHealth / hostileMaxHealth) * 100),
          )
        : 100;
    const progressPercent =
      lane === 'Rearguard' && frame.waveNumber !== null
        ? Math.min(
            100,
            ((frame.waveNumber - 1) / 10) * 100 + encounterProgress / 10,
          )
        : hostileMaxHealth > 0
          ? encounterProgress
          : Math.min(
              100,
              (elapsedMilliseconds / Math.max(1, durationMilliseconds)) * 100,
            );
    const status = completed
      ? frame.outcome === 'Victory'
        ? 'Objective complete'
        : 'Party defeated'
      : lane === 'Rearguard' && frame.waveNumber !== null
        ? `Wave ${frame.waveNumber} of 10`
        : 'Engaged';
    return {
      lane,
      frame,
      progressPercent,
      elapsedSeconds: elapsedMilliseconds / 1000,
      durationSeconds: durationMilliseconds / 1000,
      friendlyAlive,
      friendlyTotal: frame.friendly.length,
      hostileHealth,
      hostileMaxHealth,
      status,
      completed,
      isWaveTransitionHold,
    };
  }

  private setPlaybackSchedule(schedule?: RaidRun): void {
    const playbackStartedAt = schedule?.playbackStartedAt
      ? Date.parse(schedule.playbackStartedAt)
      : Number.NaN;
    const serverNow = schedule?.serverNow
      ? Date.parse(schedule.serverNow)
      : Number.NaN;
    if (!Number.isFinite(playbackStartedAt) || !Number.isFinite(serverNow)) {
      this.scheduledPlaybackStartedAt = null;
      return;
    }

    this.scheduledPlaybackStartedAt = playbackStartedAt;
    this.playbackServerClockAtSync = serverNow;
    this.playbackMonotonicClockAtSync = performance.now();
  }

  private scheduledPlaybackElapsedMilliseconds(): number | null {
    if (this.scheduledPlaybackStartedAt === null) return null;
    return Math.max(
      0,
      this.playbackServerClockAtSync +
        (performance.now() - this.playbackMonotonicClockAtSync) -
        this.scheduledPlaybackStartedAt,
    );
  }

  private playbackDurationMilliseconds(
    lane: RaidLane,
    bundle: RaidPlaybackBundle,
  ): number {
    const durationTicks =
      this.raid()?.laneResults.find((result) => result.lane === lane)
        ?.durationTicks ?? 0;
    const combatDurationMilliseconds =
      (durationTicks / bundle.ticksPerSecond) * 1000;
    return lane === 'Rearguard'
      ? this.playbackPlayer.playbackDurationMilliseconds(
          bundle,
          this.rearguardWaveTransitionHoldMilliseconds,
        )
      : combatDurationMilliseconds;
  }

  private stopPlaybackTimers(): void {
    if (this.playbackTimer !== null) clearInterval(this.playbackTimer);
    if (this.playbackAdvanceTimer !== null)
      clearTimeout(this.playbackAdvanceTimer);
    this.playbackTimer = null;
    this.playbackAdvanceTimer = null;
  }

  private errorMessage(error: any): string {
    return (
      error?.errorMessage ?? error?.error?.errorMessage ?? 'Raid action failed.'
    );
  }
}
