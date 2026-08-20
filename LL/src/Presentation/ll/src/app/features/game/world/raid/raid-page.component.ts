import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, finalize, interval, startWith, takeUntil } from 'rxjs';
import {
  RaidLane,
  RaidBattlePlanPreview,
  RaidPlaybackBundle,
  RaidReward,
  RaidRun,
  RaidService,
  RaidSignup,
} from '../../../../core/services/api/raid/raid.service';
import { StateSyncCoordinator } from '../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { RaidPlaybackService } from '../../../../core/services/client-side/combat/raid-playback.service';
import { CharacterTagComponent } from '../../../../shared/components/character/character-tag/character-tag.component';

@Component({
  selector: 'app-raid-page',
  imports: [CommonModule, RouterLink, CombatComponent, CharacterTagComponent],
  templateUrl: './raid-page.component.html',
  styleUrl: './raid-page.component.scss',
})
export class RaidPageComponent implements OnInit, OnDestroy {
  private readonly raids = inject(RaidService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly stateSync = inject(StateSyncCoordinator);
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
  readonly playbackStageIndex = signal(-1);
  readonly selectedSignupId = signal<string | null>(null);
  readonly collapsedLanes = signal<Set<RaidLane>>(new Set());
  readonly lanes: RaidLane[] = ['Vanguard', 'Flank', 'Ward'];
  readonly raidPlaybackOrder: RaidLane[] = ['Flank', 'Ward', 'Vanguard'];
  private raidRunId = '';
  private raidSyncCleanup: () => void = () => undefined;
  private playbackTimer: ReturnType<typeof setInterval> | null = null;
  private playbackAdvanceTimer: ReturnType<typeof setTimeout> | null = null;
  private activePlaybackBundle: RaidPlaybackBundle | null = null;
  private activePlaybackLanes: RaidLane[] = [];
  private playbackStartedAt = 0;
  private lastPlaybackFrameSequence = -1;
  private autoPlaybackRequested = false;
  private autoPlaybackStarted = false;
  private playbackGeneration = 0;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntil(this.destroyed)).subscribe((params) => {
      this.raidRunId = params.get('raidId') ?? '';
      this.raidSyncCleanup();
      this.raidSyncCleanup = this.stateSync.register(
        'raids',
        `raid-${this.raidRunId}`,
        async () => this.load(false),
        () => !!this.raidRunId,
      );
      this.load();
    });
    interval(5000)
      .pipe(startWith(0), takeUntil(this.destroyed))
      .subscribe(() => {
        if (this.raid()?.status === 'Resolving') this.load(false);
      });
  }

  ngOnDestroy(): void {
    this.closeRaidPlayback();
    this.raidSyncCleanup();
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
        'Cancel this raid and return its Raid Seal to the player who created it?',
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

  fillDevelopmentRoster(): void {
    this.runAction(
      'development-fill',
      this.raids.fillDevelopmentRoster(this.raidRunId),
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

  raidBattleTitle(): string {
    const lane = this.playbackLane();
    const raid = this.raid();
    if (!lane || !raid) return 'Raid Battle';
    const stage = this.playbackStageIndex() + 1;
    return `${raid.raidBossName} · ${lane} · Battle ${stage}/${this.activePlaybackLanes.length}`;
  }

  raidEnemyName(): string {
    switch (this.playbackLane()) {
      case 'Flank':
        return 'Reinforcements';
      case 'Ward':
        return 'Ward Defenders';
      case 'Vanguard':
        return this.raid()?.raidBossName ?? 'Raid Boss';
      default:
        return 'Hostiles';
    }
  }

  raidWingName(): string {
    return `${this.playbackLane() ?? 'Raid'} Wing`;
  }

  closeRaidPlayback(): void {
    this.playbackGeneration++;
    this.stopPlaybackTimers();
    this.activePlaybackBundle = null;
    this.activePlaybackLanes = [];
    this.playbackLane.set(null);
    this.playbackStageIndex.set(-1);
    this.watchingPlayback.set(false);
    this.combat.closeCurrentRaidBattle();
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

  wing(lane: RaidLane): RaidSignup[] {
    return (
      this.raid()
        ?.signups.filter((signup) => signup.lane === lane)
        .sort(
          (left, right) =>
            (left.wingSlotIndex ?? Number.MAX_SAFE_INTEGER) -
            (right.wingSlotIndex ?? Number.MAX_SAFE_INTEGER),
        ) ?? []
    );
  }

  benched(): RaidSignup[] {
    return this.raid()?.signups.filter((signup) => !signup.lane) ?? [];
  }

  wingSlots(raid: RaidRun): number[] {
    return Array.from({ length: raid.laneSlots }, (_, index) => index);
  }

  slotLabel(slotIndex: number): string {
    return (slotIndex + 1).toString().padStart(2, '0');
  }

  signupAtSlot(
    raid: RaidRun,
    lane: RaidLane,
    slotIndex: number,
  ): RaidSignup | null {
    return (
      raid.signups.find(
        (signup) => signup.lane === lane && signup.wingSlotIndex === slotIndex,
      ) ?? null
    );
  }

  wingAverage(raid: RaidRun, lane: RaidLane): number {
    const signups = raid.signups.filter((signup) => signup.lane === lane);
    if (!signups.length) return 0;
    return Math.round(
      signups.reduce((total, signup) => total + signup.powerRating, 0) /
        signups.length,
    );
  }

  isLaneCollapsed(lane: RaidLane): boolean {
    return this.collapsedLanes().has(lane);
  }

  toggleLane(lane: RaidLane): void {
    this.collapsedLanes.update((current) => {
      const next = new Set(current);
      if (next.has(lane)) next.delete(lane);
      else next.add(lane);
      return next;
    });
  }

  raidStatusLabel(status: RaidRun['status']): string {
    switch (status) {
      case 'Mustering':
        return 'Recruiting';
      case 'Resolving':
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
        return 'InProgress';
      case 'Settled':
      case 'Resolved':
        return 'Succeeded';
      default:
        return 'Cancelled';
    }
  }

  selectSignup(signup: RaidSignup, raid: RaidRun): void {
    if (!raid.canAssign || this.action()) return;
    this.selectedSignupId.update((selected) =>
      selected === signup.characterId ? null : signup.characterId,
    );
  }

  placeSelectedInWing(lane: RaidLane, raid: RaidRun): void {
    const signupId = this.selectedSignupId();
    if (!signupId) return;
    const openSlot = this.wingSlots(raid).find(
      (slotIndex) => !this.signupAtSlot(raid, lane, slotIndex),
    );
    if (openSlot !== undefined) this.moveSignup(signupId, lane, openSlot, raid);
  }

  placeSelectedInSlot(lane: RaidLane, slotIndex: number, raid: RaidRun): void {
    const signupId = this.selectedSignupId();
    if (signupId) this.moveSignup(signupId, lane, slotIndex, raid);
  }

  benchSignup(signup: RaidSignup, raid: RaidRun): void {
    this.moveSignup(signup.characterId, null, null, raid);
  }

  dragSignup(event: DragEvent, signup: RaidSignup, raid: RaidRun): void {
    if (!raid.canAssign || !event.dataTransfer) return;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', signup.characterId);
    this.selectedSignupId.set(signup.characterId);
  }

  allowPartyDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  dropIntoSlot(
    event: DragEvent,
    lane: RaidLane,
    slotIndex: number,
    raid: RaidRun,
  ): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (signupId) this.moveSignup(signupId, lane, slotIndex, raid);
  }

  dropIntoWing(event: DragEvent, lane: RaidLane, raid: RaidRun): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (!signupId) return;
    const openSlot = this.wingSlots(raid).find(
      (slotIndex) => !this.signupAtSlot(raid, lane, slotIndex),
    );
    if (openSlot !== undefined) this.moveSignup(signupId, lane, openSlot, raid);
  }

  dropOnBench(event: DragEvent, raid: RaidRun): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (signupId) this.moveSignup(signupId, null, null, raid);
  }

  distributeBenched(raid: RaidRun): void {
    const openPositions = this.lanes.flatMap((lane) =>
      this.wingSlots(raid)
        .filter((slotIndex) => !this.signupAtSlot(raid, lane, slotIndex))
        .map((slotIndex) => ({ lane, slotIndex })),
    );
    let positionIndex = 0;
    this.savePartyAssignments(
      raid.signups.map((signup) => {
        const position = signup.lane ? null : openPositions[positionIndex++];
        return {
          characterId: signup.characterId,
          lane: signup.lane ?? position?.lane ?? null,
          wingSlotIndex: signup.wingSlotIndex ?? position?.slotIndex ?? null,
        };
      }),
    );
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

  private moveSignup(
    signupId: string,
    lane: RaidLane | null,
    wingSlotIndex: number | null,
    raid: RaidRun,
  ): void {
    if (!raid.canAssign || this.action()) return;
    const occupant = raid.signups.find(
      (signup) =>
        lane !== null &&
        signup.lane === lane &&
        signup.wingSlotIndex === wingSlotIndex &&
        signup.characterId !== signupId,
    );
    if (occupant) {
      this.error.set('That raid party slot is already occupied.');
      return;
    }
    this.savePartyAssignments(
      raid.signups.map((signup) => ({
        characterId: signup.characterId,
        lane: signup.characterId === signupId ? lane : signup.lane,
        wingSlotIndex:
          signup.characterId === signupId
            ? wingSlotIndex
            : signup.wingSlotIndex,
      })),
    );
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
          this.selectedSignupId.set(null);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  private acceptRaid(raid: RaidRun): void {
    if (raid.status === 'Resolving') this.autoPlaybackRequested = true;
    this.raid.set(raid);
    if (
      (raid.status === 'Settled' || raid.status === 'Resolved') &&
      this.autoPlaybackRequested &&
      !this.autoPlaybackStarted
    ) {
      this.autoPlaybackStarted = true;
      queueMicrotask(() => this.startRaidPlayback(this.raidPlaybackOrder));
    }
  }

  private startRaidPlayback(lanes: readonly RaidLane[]): void {
    if (this.action() || !lanes.length) return;
    this.closeRaidPlayback();
    this.activePlaybackLanes = [...lanes];
    this.watchingPlayback.set(true);
    this.loadPlaybackStage(0);
  }

  private loadPlaybackStage(index: number): void {
    const lane = this.activePlaybackLanes[index];
    if (!lane) {
      this.closeRaidPlayback();
      return;
    }

    this.stopPlaybackTimers();
    this.playbackStageIndex.set(index);
    this.playbackLane.set(lane);
    this.action.set(`playback-${lane}`);
    this.error.set(null);
    const generation = this.playbackGeneration;
    this.raids
      .getPlaybackBundle(this.raidRunId, lane)
      .pipe(takeUntil(this.destroyed))
      .subscribe({
        next: (bundle) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.activePlaybackBundle = bundle;
          this.playbackStartedAt = performance.now();
          this.lastPlaybackFrameSequence = -1;
          this.renderPlaybackFrame(true);
          this.playbackTimer = setInterval(
            () => this.renderPlaybackFrame(false),
            250,
          );
        },
        error: (error) => {
          if (generation !== this.playbackGeneration) return;
          this.action.set(null);
          this.error.set(this.errorMessage(error));
          this.closeRaidPlayback();
        },
      });
  }

  private renderPlaybackFrame(reset: boolean): void {
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
    this.playbackAdvanceTimer = setTimeout(() => {
      this.playbackAdvanceTimer = null;
      this.loadPlaybackStage(this.playbackStageIndex() + 1);
    }, 1500);
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
