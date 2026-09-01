import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DefaultHeaderComponent } from '../../../shared/components/default-header/default-header.component';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { ToastService } from '../../../core/services/client-side/components/toast/toast.service';
import {
  RealtimeSignalDeduper,
  getRealtimeDeliveryId,
} from '../../../core/services/real-time/game-realtime/realtime-deduplication';
import { GameRealtimeEventRegistry } from '../../../core/services/real-time/game-realtime/game-realtime-event-registry.service';
import { ProphecyProgressed } from '../../../core/services/real-time/game-realtime/game-realtime-contracts';
import {
  PropheciesOverviewDto,
  ProphecyCacheInventoryDto,
  ProphecyGuidanceDestination,
  ProphecyInstanceDto,
  ProphecyRewardSnapshotDto,
  ProphecyService,
  WeeklyRevelationMilestoneDto,
} from '../../../core/services/api/prophecies/prophecy.service';
import { ProphecyNotificationService } from '../../../core/services/api/prophecies/prophecy-notification.service';
import { StateSyncCoordinator } from '../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../../core/services/real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../../../core/services/api/api.service';
import { finalize, Observable, tap } from 'rxjs';

interface RewardDisplayItem {
  key: string;
  label: string;
  amount: string;
  category: string;
  marker: string;
}

@Component({
  selector: 'app-prophecies-page',
  imports: [
    DefaultHeaderComponent,
    NgClass,
    NgFor,
    NgIf,
    OverlayModule,
    RouterLink,
  ],
  templateUrl: './prophecies-page.component.html',
  styleUrl: './prophecies-page.component.scss',
})
export class PropheciesPageComponent implements OnInit, OnDestroy {
  private readonly guidanceRoutes: Record<
    ProphecyGuidanceDestination,
    string[]
  > = {
    WorldCombat: ['/game/world/shenic'],
    Dungeons: ['/game/world/dungeon'],
    Essences: ['/game/character/essences'],
    SoulArchive: ['/game/character/soulstone-archive'],
    Gathering: ['/game/world/shenic'],
    Crafting: ['/game/professions/crafting'],
  };
  readonly overview = signal<PropheciesOverviewDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  private readonly now = signal(Date.now());
  readonly weeklyFavorMarkers = [1, 2, 4, 6];
  readonly weeklyTrackEndPercent = 96;
  readonly rewardTooltipPositions: ConnectedPosition[] = [
    {
      originX: 'start',
      originY: 'bottom',
      overlayX: 'start',
      overlayY: 'top',
      offsetY: 8,
    },
    {
      originX: 'end',
      originY: 'bottom',
      overlayX: 'end',
      overlayY: 'top',
      offsetY: 8,
    },
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -8,
    },
    {
      originX: 'end',
      originY: 'top',
      overlayX: 'end',
      overlayY: 'bottom',
      offsetY: -8,
    },
  ];
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private readonly initialProgressEventId: string | null;
  private clockIntervalId: ReturnType<typeof setInterval> | null = null;
  private readonly unregisterStateSync: () => void;
  private requestEpoch = 0;
  hoveredRewardOverflowId: string | null = null;
  hoveredWeeklyMilestoneFavor: number | null = null;

  readonly dailyProphecies = computed(
    () => this.overview()?.dailyProphecies ?? [],
  );
  readonly dailyRerollsRemaining = computed(
    () => this.overview()?.dailyRerollsRemaining ?? 0,
  );
  readonly activeDailyProphecy = computed(
    () => this.overview()?.activeDailyProphecy ?? null,
  );
  readonly greaterProphecy = computed(
    () => this.overview()?.greaterProphecy ?? null,
  );
  readonly weeklyRevelation = computed(
    () => this.overview()?.weeklyRevelation ?? null,
  );
  readonly caches = computed(() => this.overview()?.caches ?? []);
  readonly ownedCaches = computed(() =>
    this.caches().filter((cache) => cache.quantity > 0),
  );
  readonly readyProphecies = computed(() => {
    const overview = this.overview();
    if (!overview) return [];

    return [...overview.dailyProphecies, overview.greaterProphecy].filter(
      (prophecy) => this.canClaim(prophecy),
    );
  });
  readonly readyMilestones = computed(
    () =>
      this.weeklyRevelation()?.milestones.filter(
        (milestone) => milestone.isUnlocked && !milestone.isClaimed,
      ) ?? [],
  );
  readonly readyClaimCount = computed(
    () => this.readyProphecies().length + this.readyMilestones().length,
  );

  constructor(
    private readonly prophecyService: ProphecyService,
    private readonly toast: ToastService,
    private readonly prophecyNotificationService: ProphecyNotificationService,
    private readonly authService: AuthService,
    private readonly eventService: GameRealtimeEventRegistry,
    private readonly domainVersions: DomainVersionTracker,
    stateSync: StateSyncCoordinator,
  ) {
    this.unregisterStateSync = stateSync.register(
      'prophecies',
      'prophecies-page',
      () => this.synchronize(),
    );
    this.initialProgressEventId = getRealtimeDeliveryId(
      this.eventService.eventEnvelope.ProphecyProgressed(),
    );

    effect(
      () => {
        const characterId = this.authService.currentCharacter()?.id;
        const envelope =
          this.eventService.eventEnvelope.ProphecyProgressed();
        const update = envelope?.payload;
        const eventId = getRealtimeDeliveryId(envelope);

        if (
          !characterId ||
          !update ||
          (!!eventId && eventId === this.initialProgressEventId) ||
          update.characterId !== characterId ||
          !this.eventDeduper.shouldProcess('prophecy-progressed', envelope)
        ) {
          return;
        }

        this.applyProgressUpdate(update);
      },
    );
  }

  ngOnInit(): void {
    this.clockIntervalId = setInterval(() => this.now.set(Date.now()), 30_000);
    this.refresh();
  }

  ngOnDestroy(): void {
    this.unregisterStateSync();
    if (this.clockIntervalId) {
      clearInterval(this.clockIntervalId);
      this.clockIntervalId = null;
    }
  }

  refresh(): void {
    this.synchronize().subscribe({ error: () => undefined });
  }

  private synchronize(): Observable<PropheciesOverviewDto> {
    const requestEpoch = ++this.requestEpoch;
    this.loading.set(true);
    this.error.set(null);

    return this.prophecyService.getOverview().pipe(
      tap({
        next: (overview) => {
          if (requestEpoch !== this.requestEpoch) return;
          this.overview.set(overview);
          this.syncNotificationCount();
        },
        error: (error) => {
          if (requestEpoch !== this.requestEpoch) return;
          this.error.set(error?.message ?? 'Failed to load prophecies.');
        },
      }),
      finalize(() => {
        if (requestEpoch === this.requestEpoch) {
          this.loading.set(false);
        }
      }),
    );
  }

  accept(prophecy: ProphecyInstanceDto): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    this.prophecyService.acceptProphecy(prophecy.id).subscribe({
      next: (result) => {
        if (
          !this.applyVersionedProphecy(result, (overview) =>
            this.overview.set(overview),
          )
        ) {
          this.loading.set(false);
          return;
        }
        this.syncNotificationCount();
        this.toast.showToast('Prophecy accepted', prophecy.title, true);
        this.loading.set(false);
      },
      error: (error) => {
        const message = error?.message ?? 'Failed to accept prophecy.';
        this.error.set(message);
        this.toast.showToast('Prophecy failed', message, false);
        this.loading.set(false);
      },
    });
  }

  rerollDailyProphecies(): void {
    if (this.loading() || !this.canReroll() || !this.canAffordReroll()) return;
    const cost = this.overview()?.nextDailyRerollCost ?? 0;
    this.loading.set(true);
    this.error.set(null);

    this.prophecyService.rerollDailyProphecies().subscribe({
      next: (result) => {
        if (
          !this.applyVersionedProphecy(result, (overview) =>
            this.overview.set(overview),
          )
        ) {
          this.loading.set(false);
          return;
        }
        this.syncNotificationCount();
        this.toast.showToast(
          'Prophecies rerolled',
          cost > 0
            ? `${cost} Fate Echo spent.`
            : 'Your free daily reroll has been used.',
          true,
        );
        this.loading.set(false);
      },
      error: (error) => {
        const message = error?.message ?? 'Failed to reroll daily prophecies.';
        this.error.set(message);
        this.toast.showToast('Reroll failed', message, false);
        this.loading.set(false);
      },
    });
  }

  claim(prophecy: ProphecyInstanceDto): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    this.prophecyService.claimProphecy(prophecy.id).subscribe({
      next: (result) => {
        const response = result.data;
        if (!this.applyVersionedProphecy(result, () => undefined)) {
          this.loading.set(false);
          return;
        }
        const current = this.overview();
        if (current) {
          this.overview.set({
            ...current,
            dailyProphecies: current.dailyProphecies.map((item) =>
              item.id === response.prophecy.id ? response.prophecy : item,
            ),
            activeDailyProphecy:
              current.activeDailyProphecy?.id === response.prophecy.id
                ? response.prophecy
                : current.activeDailyProphecy,
            greaterProphecy:
              current.greaterProphecy.id === response.prophecy.id
                ? response.prophecy
                : current.greaterProphecy,
            weeklyRevelation: response.weeklyRevelation,
            caches: this.updateCacheInventory(current.caches, response.reward),
          });
        }
        this.syncNotificationCount();
        this.toast.showToast(
          'Prophecy claimed',
          this.rewardSummary(response.reward),
          true,
        );
        this.loading.set(false);
      },
      error: (error) => {
        const message = error?.message ?? 'Failed to claim prophecy.';
        this.error.set(message);
        this.toast.showToast('Claim failed', message, false);
        this.loading.set(false);
      },
    });
  }

  claimMilestone(milestone: WeeklyRevelationMilestoneDto): void {
    if (this.loading() || !milestone.isUnlocked || milestone.isClaimed) return;
    this.loading.set(true);
    this.error.set(null);

    this.prophecyService
      .claimWeeklyMilestone(milestone.favorRequired)
      .subscribe({
        next: (result) => {
          const response = result.data;
          if (!this.applyVersionedProphecy(result, () => undefined)) {
            this.loading.set(false);
            return;
          }
          const current = this.overview();
          if (current) {
            this.overview.set({
              ...current,
              weeklyRevelation: response.weeklyRevelation,
              caches: this.updateCacheInventory(
                current.caches,
                response.reward,
              ),
            });
          }
          this.syncNotificationCount();
          this.toast.showToast(
            'Weekly Revelation claimed',
            this.rewardSummary(response.reward),
            true,
          );
          this.loading.set(false);
        },
        error: (error) => {
          const message = error?.message ?? 'Failed to claim weekly milestone.';
          this.error.set(message);
          this.toast.showToast('Revelation failed', message, false);
          this.loading.set(false);
        },
      });
  }

  openCache(cache: ProphecyCacheInventoryDto): void {
    if (this.loading() || cache.quantity <= 0) return;
    this.loading.set(true);
    this.error.set(null);

    this.prophecyService.openCache(cache.itemId).subscribe({
      next: (result) => {
        const response = result.data;
        if (!this.applyVersionedProphecy(result, () => undefined)) {
          this.loading.set(false);
          return;
        }
        const current = this.overview();
        if (current) {
          this.overview.set({
            ...current,
            caches: response.caches,
          });
        }

        this.syncNotificationCount();
        this.toast.showToast(
          'Cache opened',
          this.rewardSummary(response.reward),
          true,
        );
        this.loading.set(false);
      },
      error: (error) => {
        const message = error?.message ?? 'Failed to open prophecy cache.';
        this.error.set(message);
        this.toast.showToast('Cache failed', message, false);
        this.loading.set(false);
      },
    });
  }

  private applyVersionedProphecy<T>(
    result: VersionedMutationResult<T>,
    apply: (data: T) => void,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'prophecies',
        result.domainVersions['prophecies'],
      )
    ) {
      return false;
    }

    this.requestEpoch += 1;
    apply(result.data);
    return true;
  }

  private applyProgressUpdate(update: ProphecyProgressed): void {
    const current = this.overview();
    if (!current) {
      this.toast.showToast(
        update.completed ? 'Prophecy completed' : 'Prophecy progressed',
        this.progressToastMessage(update),
        true,
      );
      return;
    }

    const patch = (prophecy: ProphecyInstanceDto): ProphecyInstanceDto =>
      prophecy.id === update.prophecyId
        ? {
            ...prophecy,
            status: update.status,
            currentValue: update.currentValue,
            completedAt: update.completed
              ? new Date().toISOString()
              : prophecy.completedAt,
          }
        : prophecy;

    this.overview.set({
      ...current,
      dailyProphecies: current.dailyProphecies.map(patch),
      activeDailyProphecy: current.activeDailyProphecy
        ? patch(current.activeDailyProphecy)
        : current.activeDailyProphecy,
      greaterProphecy: patch(current.greaterProphecy),
    });

    this.syncNotificationCount();
    this.toast.showToast(
      update.completed ? 'Prophecy completed' : 'Prophecy progressed',
      this.progressToastMessage(update),
      true,
    );
  }

  private progressToastMessage(update: ProphecyProgressed): string {
    const amount = update.amountGained > 0 ? `+${update.amountGained} ` : '';
    return `${update.title}: ${amount}${update.currentValue}/${update.targetValue}`;
  }

  private updateCacheInventory(
    caches: ProphecyCacheInventoryDto[],
    reward: ProphecyRewardSnapshotDto,
    consumedCacheItemId?: string,
  ): ProphecyCacheInventoryDto[] {
    const quantityChanges = new Map<string, number>();
    const addQuantityChange = (itemId: string, quantity: number): void => {
      const key = itemId.toLowerCase();
      quantityChanges.set(key, (quantityChanges.get(key) ?? 0) + quantity);
    };

    if (consumedCacheItemId) {
      addQuantityChange(consumedCacheItemId, -1);
    }

    if (reward.cacheItemId) {
      addQuantityChange(reward.cacheItemId, 1);
    }

    const cacheItemIds = new Set(
      caches.map((cache) => cache.itemId.toLowerCase()),
    );
    for (const item of reward.items) {
      if (cacheItemIds.has(item.itemId.toLowerCase())) {
        addQuantityChange(item.itemId, item.quantity);
      }
    }

    return caches.map((cache) => {
      const quantityChange =
        quantityChanges.get(cache.itemId.toLowerCase()) ?? 0;
      return quantityChange === 0
        ? cache
        : { ...cache, quantity: Math.max(0, cache.quantity + quantityChange) };
    });
  }

  progressPercent(prophecy: ProphecyInstanceDto): number {
    if (prophecy.targetValue <= 0) return 0;
    return Math.min(
      100,
      Math.round((prophecy.currentValue / prophecy.targetValue) * 100),
    );
  }

  canAccept(prophecy: ProphecyInstanceDto): boolean {
    return prophecy.status === 'Offered' && !this.activeDailyProphecy();
  }

  canReroll(): boolean {
    const overview = this.overview();
    return (
      this.dailyProphecies().length === 3 &&
      this.dailyProphecies().every(
        (prophecy) => prophecy.status === 'Offered',
      ) &&
      !this.activeDailyProphecy() &&
      !!overview &&
      overview.dailyRerollsUsed < overview.dailyRerollLimit
    );
  }

  canAffordReroll(): boolean {
    const overview = this.overview();
    if (!overview || overview.nextDailyRerollCost == null) return false;
    return overview.fateEcho >= overview.nextDailyRerollCost;
  }

  rerollButtonLabel(): string {
    const cost = this.overview()?.nextDailyRerollCost;
    return cost ? `Reroll All · ${cost} Fate Echo` : 'Reroll All · Free';
  }

  canClaim(prophecy: ProphecyInstanceDto): boolean {
    return (
      prophecy.status !== 'Claimed' &&
      prophecy.status !== 'Declined' &&
      prophecy.status !== 'Expired' &&
      (prophecy.status === 'Completed' || this.isObjectiveComplete(prophecy))
    );
  }

  canContinue(prophecy: ProphecyInstanceDto): boolean {
    return (
      prophecy.status === 'Accepted' && !this.isObjectiveComplete(prophecy)
    );
  }

  isObjectiveComplete(prophecy: ProphecyInstanceDto): boolean {
    return (
      prophecy.targetValue > 0 && prophecy.currentValue >= prophecy.targetValue
    );
  }

  prophecyStatusLabel(prophecy: ProphecyInstanceDto): string {
    if (prophecy.status === 'Accepted' && this.isObjectiveComplete(prophecy))
      return 'Completed';
    return prophecy.status;
  }

  greaterProphecyLabel(prophecy: ProphecyInstanceDto): string {
    if (prophecy.status === 'Claimed') return 'Greater Prophecy Claimed';
    if (this.canClaim(prophecy)) return 'Greater Prophecy Complete';
    return 'Greater Prophecy';
  }

  rewardLines(reward: ProphecyRewardSnapshotDto): string[] {
    return this.rewardItems(reward).map(
      (item) => `${item.amount} ${item.label}`,
    );
  }

  rewardSummary(reward: ProphecyRewardSnapshotDto): string {
    const lines = this.rewardLines(reward);
    return lines.length > 0 ? lines.join(', ') : 'No rewards listed';
  }

  rewardPreview(
    reward: ProphecyRewardSnapshotDto,
    limit = 3,
  ): RewardDisplayItem[] {
    return this.rewardItems(reward).slice(0, limit);
  }

  rewardOverflowCount(reward: ProphecyRewardSnapshotDto, limit = 3): number {
    return Math.max(0, this.rewardItems(reward).length - limit);
  }

  rewardOverflowItems(
    reward: ProphecyRewardSnapshotDto,
    limit = 3,
  ): RewardDisplayItem[] {
    return this.rewardItems(reward).slice(limit);
  }

  weeklyProgressPercent(): number {
    const favor = this.weeklyRevelation()?.propheticFavor ?? 0;
    return Math.min(
      this.weeklyTrackEndPercent,
      Math.round((favor / 7) * this.weeklyTrackEndPercent),
    );
  }

  weeklyMilestonePercent(milestone: WeeklyRevelationMilestoneDto): number {
    return Math.min(
      this.weeklyTrackEndPercent,
      Math.max(0, (milestone.favorRequired / 7) * this.weeklyTrackEndPercent),
    );
  }

  weeklyFavorMarkerPercent(marker: number): number {
    return Math.min(
      this.weeklyTrackEndPercent,
      Math.max(0, (marker / 7) * this.weeklyTrackEndPercent),
    );
  }

  weeklyMilestoneAlignment(milestone: WeeklyRevelationMilestoneDto): string {
    return 'items-center text-center -translate-x-1/2';
  }

  showWeeklyMilestoneTooltip(favorRequired: number): void {
    this.hoveredWeeklyMilestoneFavor = favorRequired;
  }

  hideWeeklyMilestoneTooltip(favorRequired: number): void {
    if (this.hoveredWeeklyMilestoneFavor === favorRequired) {
      this.hoveredWeeklyMilestoneFavor = null;
    }
  }

  isWeeklyMilestoneTooltipOpen(favorRequired: number): boolean {
    return this.hoveredWeeklyMilestoneFavor === favorRequired;
  }

  showRewardOverflow(prophecyId: string): void {
    this.hoveredRewardOverflowId = prophecyId;
  }

  hideRewardOverflow(prophecyId: string): void {
    if (this.hoveredRewardOverflowId === prophecyId) {
      this.hoveredRewardOverflowId = null;
    }
  }

  isRewardOverflowOpen(prophecyId: string): boolean {
    return this.hoveredRewardOverflowId === prophecyId;
  }

  dailyResetText(): string {
    const periodEnd = this.dailyProphecies()[0]?.periodEnd;
    return periodEnd ? this.timeRemaining(periodEnd) : 'Soon';
  }

  milestoneActionLabel(milestone: WeeklyRevelationMilestoneDto): string {
    if (milestone.isClaimed) return 'Claimed';
    if (milestone.isUnlocked) return 'Claim';
    return 'Locked';
  }

  rewardItems(reward: ProphecyRewardSnapshotDto): RewardDisplayItem[] {
    const items: RewardDisplayItem[] = [];
    this.addReward(
      items,
      'cinders',
      reward.cinders,
      'Cinders',
      'Currency',
      'Ci',
    );
    this.addReward(
      items,
      'characterExperience',
      reward.characterExperience,
      'Character XP',
      'Progress',
      'XP',
    );
    this.addReward(
      items,
      'essenceExperience',
      reward.essenceExperience,
      'Essence XP',
      'Essence',
      'EX',
    );
    this.addReward(
      items,
      'soulstones',
      reward.soulstones,
      'Soulstones',
      'Currency',
      'So',
    );
    this.addReward(
      items,
      'sigilFragments',
      reward.sigilFragments,
      'Sigil Fragments',
      'Dungeon',
      'Sf',
    );
    this.addReward(
      items,
      'propheticFavor',
      reward.propheticFavor,
      'Prophetic Favor',
      'Weekly',
      'Fa',
    );
    this.addReward(
      items,
      'fateEcho',
      reward.fateEcho,
      'Fate Echo',
      'Prophecy',
      'Fe',
    );

    if (reward.cacheItemId) {
      items.push({
        key: `cache-${reward.cacheItemId}`,
        label: this.formatId(reward.cacheItemId),
        amount: '1',
        category: 'Cache',
        marker: 'Ca',
      });
    }

    for (const item of reward.items) {
      if (item.quantity <= 0) continue;
      items.push({
        key: `item-${item.itemId}`,
        label: this.formatId(item.itemId),
        amount: item.quantity.toString(),
        category: 'Item',
        marker: 'It',
      });
    }

    return items;
  }

  cacheContents(cache: ProphecyCacheInventoryDto): string[] {
    return cache.possibleRewards?.length
      ? cache.possibleRewards
      : ['Prophecy rewards'];
  }

  timeRemaining(end: string): string {
    const ms = new Date(end).getTime() - this.now();
    if (ms <= 0) return 'Expired';
    const hours = Math.floor(ms / 3_600_000);
    const minutes = Math.floor((ms % 3_600_000) / 60_000);
    const days = Math.floor(hours / 24);
    if (days > 0) return `${days}d ${hours % 24}h`;
    return `${hours}h ${minutes}m`;
  }

  guidanceRoute(prophecy: ProphecyInstanceDto): string[] {
    return (
      this.guidanceRoutes[prophecy.guidance.destination] ??
      this.guidanceRoutes.WorldCombat
    );
  }

  statusClasses(prophecy: ProphecyInstanceDto): string {
    if (prophecy.status === 'Accepted')
      return 'll-prophecy-status ll-prophecy-status-active';
    if (prophecy.status === 'Completed' || this.isObjectiveComplete(prophecy))
      return 'll-prophecy-status ll-prophecy-status-active';
    if (prophecy.status === 'Claimed')
      return 'll-prophecy-status ll-prophecy-status-claimed';
    if (prophecy.status === 'Declined' || prophecy.status === 'Expired')
      return 'll-prophecy-status ll-prophecy-status-declined';
    return 'll-prophecy-status';
  }

  formatId(value: string): string {
    return value
      .replace(/^item\./, '')
      .replace(/[_:.]/g, ' ')
      .replace(/\b\w/g, (char) => char.toUpperCase());
  }

  trackById(
    index: number,
    value: { id?: string; favorRequired?: number },
  ): string {
    return value.id ?? `${value.favorRequired ?? index}`;
  }

  trackByCacheId(index: number, value: ProphecyCacheInventoryDto): string {
    return value.itemId || `${index}`;
  }

  trackRewardItem(index: number, value: RewardDisplayItem): string {
    return value.key || `${index}`;
  }

  trackByLabel(index: number, value: string): string {
    return value || `${index}`;
  }

  trackByNumber(index: number, value: number): number {
    return value || index;
  }

  private addReward(
    items: RewardDisplayItem[],
    key: string,
    amount: number,
    label: string,
    category: string,
    marker: string,
  ): void {
    if (amount <= 0) return;

    items.push({
      key,
      label,
      amount: amount.toString(),
      category,
      marker,
    });
  }

  private syncNotificationCount(): void {
    this.prophecyNotificationService.syncFromOverview(this.overview());
  }
}
