import { computed, effect, Injectable, signal } from '@angular/core';
import { finalize, forkJoin, Observable, tap } from 'rxjs';
import { ColosseumService } from './colosseum.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import {
  ArenaDefenseStatus,
  ColosseumStatus,
} from '../../../../shared/models/Dtos/colosseum/colosseumStatus';
import { StartArenaBattleResponse } from '../../../../shared/models/Dtos/colosseum/startArenaBattleResponse';
import {
  ChampionMarket,
  ChampionMarketItem,
  ChampionMarketItemView,
  ChampionMarketPurchaseResponse,
  ChampionMarketView,
} from '../../../../shared/models/Dtos/colosseum/championMarket';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { CharacterStateService } from '../character/character-state.service';
import { ArenaBattleCompleted } from '../../real-time/game-realtime/game-realtime-contracts';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
  SIDEBAR_NOTIFICATION,
} from '../../client-side/notifications/notification.service';
import { RealtimeSignalDeduper } from '../../real-time/game-realtime/realtime-deduplication';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { VersionedMutationResult } from '../api.service';

@Injectable({ providedIn: 'root' })
export class ColosseumStateService {
  private readonly _allOpponents = signal<ArenaOpponentPreview[]>([]);
  private readonly _opponents = signal<ArenaOpponentPreview[]>([]);
  private readonly _arenaTicketStatus = signal<ArenaTicketStatus | null>(null);
  private readonly _status = signal<ColosseumStatus | null>(null);
  private readonly _championMarket = signal<ChampionMarket | null>(null);
  private readonly _rankings = signal<LeaderboardEntry[]>([]);
  private readonly _previousMatches = signal<ColosseumMatchResult[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly hasLoaded = signal(false);
  private notificationLoading = false;
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private statusRequestEpoch = 0;
  private ticketRequestEpoch = 0;
  private opponentsRequestEpoch = 0;
  private rankingsRequestEpoch = 0;
  private matchesRequestEpoch = 0;
  private marketRequestEpoch = 0;

  readonly opponents = computed(() => this._opponents());
  readonly arenaTicketStatus = computed(() => this._arenaTicketStatus());
  readonly status = computed(() => this._status());
  readonly championMarket = computed(() => this.deriveChampionMarket());
  readonly rankings = computed(() => this._rankings());
  readonly previousMatches = computed(() => this._previousMatches());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly notificationCount = computed(() =>
    this.notificationService.count(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Colosseum,
    ),
  );

  constructor(
    private readonly colosseumService: ColosseumService,
    private readonly combatService: CombatService,
    private readonly eventService: GameRealtimeEventRegistry,
    private readonly characterState: CharacterStateService,
    private readonly notificationService: NotificationService,
    private readonly toastService: ToastService,
    private readonly inventoryState: InventoryStateService,
    private readonly stateSync: StateSyncCoordinator,
  ) {
    this.stateSync.register(
      'colosseum',
      'colosseum',
      () => this.reconcileArena(),
      () => this.hasLoaded(),
    );
    effect(
      () => {
        const envelope = this.eventService.eventEnvelope.ArenaBattleCompleted();
        const event = envelope?.payload;
        const characterId = this.characterState.currentCharacterId();
        if (
          !event ||
          !this.eventDeduper.shouldProcess(
            'arena-battle-completed',
            envelope,
          ) ||
          !characterId ||
          !this.isParticipant(event, characterId) ||
          event.characterId === characterId
        ) {
          return;
        }

        this.applyArenaRating(event, characterId);
        this.addNotification();
      },
      { allowSignalWrites: true },
    );
  }

  refresh(): void {
    this.hasLoaded.set(true);
    this.stateSync.activate('colosseum', 'colosseum');
    this.loadStatus();
    this.loadArenaTicketStatus();
    this.loadArenaOpponents();
    this.loadColosseumRankings();
    this.loadColosseumMatchResults();
    this.loadChampionMarket();
  }

  private reconcileArena(): Observable<unknown> {
    this._loading.set(true);
    this._error.set(null);
    const epochs = {
      status: ++this.statusRequestEpoch,
      opponents: ++this.opponentsRequestEpoch,
      rankings: ++this.rankingsRequestEpoch,
      matches: ++this.matchesRequestEpoch,
    };

    return forkJoin({
      status: this.colosseumService.getStatus(),
      opponents: this.colosseumService.getArenaOpponents(),
      rankings: this.colosseumService.getColosseumRankings(),
      matches: this.colosseumService.getColosseumMatchResults(),
    }).pipe(
      tap({
        next: ({ status, opponents, rankings, matches }) => {
          if (epochs.status === this.statusRequestEpoch) {
            this._status.set(status);
            this.syncNotificationCount(status);
          }
          if (epochs.opponents === this.opponentsRequestEpoch) {
            this._allOpponents.set(opponents);
            this.pickRandomOpponents();
          }
          if (epochs.rankings === this.rankingsRequestEpoch) {
            this._rankings.set([...rankings].sort((a, b) => a.rank - b.rank));
          }
          if (epochs.matches === this.matchesRequestEpoch) {
            this._previousMatches.set(
              [...matches].sort(
                (a, b) =>
                  new Date(b.playedAt).getTime() -
                  new Date(a.playedAt).getTime(),
              ),
            );
          }
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to reconcile arena state'),
      }),
      finalize(() => this._loading.set(false)),
    );
  }

  refreshNotificationCount(): void {
    if (this.notificationLoading) return;

    const requestEpoch = ++this.statusRequestEpoch;
    this.notificationLoading = true;
    this.colosseumService
      .getStatus()
      .pipe(finalize(() => (this.notificationLoading = false)))
      .subscribe({
        next: (status) => {
          if (requestEpoch !== this.statusRequestEpoch) return;
          this._status.set(status);
          this.syncNotificationCount(status);
        },
        error: (err) => {
          if (requestEpoch === this.statusRequestEpoch) {
            this._error.set(
              err.message ?? 'Failed to load colosseum notifications',
            );
          }
        },
      });
  }

  loadStatus(): void {
    const requestEpoch = ++this.statusRequestEpoch;
    this.colosseumService.getStatus().subscribe({
      next: (status) => {
        if (requestEpoch !== this.statusRequestEpoch) return;
        this._status.set(status);
        this.syncNotificationCount(status);
      },
      error: (err) => {
        if (requestEpoch === this.statusRequestEpoch) {
          this._error.set(err.message ?? 'Failed to load colosseum status');
        }
      },
    });
  }

  loadArenaTicketStatus(): void {
    const requestEpoch = ++this.ticketRequestEpoch;
    this.colosseumService.getArenaTicketStatus().subscribe({
      next: (status) => {
        if (requestEpoch !== this.ticketRequestEpoch) return;
        this._arenaTicketStatus.set(status);
      },
      error: (err) => {
        if (requestEpoch === this.ticketRequestEpoch) {
          this._error.set(err.message ?? 'Failed to load arena tickets');
        }
      },
    });
  }

  loadArenaOpponents(): void {
    const requestEpoch = ++this.opponentsRequestEpoch;
    this.colosseumService.getArenaOpponents().subscribe({
      next: (data) => {
        if (requestEpoch !== this.opponentsRequestEpoch) return;
        this._allOpponents.set(data);
        this.pickRandomOpponents();
      },
      error: (err) => {
        if (requestEpoch === this.opponentsRequestEpoch) {
          this._error.set(err.message ?? 'Failed to load arena opponents');
        }
      },
    });
  }

  loadColosseumRankings(): void {
    const requestEpoch = ++this.rankingsRequestEpoch;
    this.colosseumService.getColosseumRankings().subscribe({
      next: (data) => {
        if (requestEpoch !== this.rankingsRequestEpoch) return;
        this._rankings.set([...data].sort((a, b) => a.rank - b.rank));
      },
      error: (err) => {
        if (requestEpoch === this.rankingsRequestEpoch) {
          this._error.set(err.message ?? 'Failed to load arena rankings');
        }
      },
    });
  }

  loadColosseumMatchResults(): void {
    const requestEpoch = ++this.matchesRequestEpoch;
    this.colosseumService.getColosseumMatchResults().subscribe({
      next: (data) => {
        if (requestEpoch !== this.matchesRequestEpoch) return;
        this._previousMatches.set(
          [...data].sort(
            (a, b) =>
              new Date(b.playedAt).getTime() - new Date(a.playedAt).getTime(),
          ),
        );
      },
      error: (err) => {
        if (requestEpoch === this.matchesRequestEpoch) {
          this._error.set(err.message ?? 'Failed to load arena match results');
        }
      },
    });
  }

  loadChampionMarket(): void {
    const requestEpoch = ++this.marketRequestEpoch;
    this.colosseumService.getChampionMarket().subscribe({
      next: (market) => {
        if (requestEpoch === this.marketRequestEpoch) {
          this._championMarket.set(market);
        }
      },
      error: (err) => {
        if (requestEpoch === this.marketRequestEpoch) {
          this._error.set(err.message ?? "Failed to load champion's market");
        }
      },
    });
  }

  updateDefenseSnapshot(): void {
    this._loading.set(true);
    this._error.set(null);

    this.colosseumService
      .updateDefenseSnapshot()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) =>
          this.applyOwnedColosseumMutation(result, (defenseStatus) =>
            this.applyDefenseStatus(defenseStatus),
          ),
        error: (err) =>
          this._error.set(err.message ?? 'Failed to update arena defense'),
      });
  }

  purchaseChampionMarketItem(itemId: string, quantity = 1): void {
    this._loading.set(true);
    this._error.set(null);

    this.colosseumService
      .purchaseChampionMarketItem(itemId, quantity)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          if (
            this.applyOwnedColosseumMutation(result, (response) =>
              this.applyChampionMarketPurchase(response),
            )
          ) {
            this.showChampionMarketPurchaseToast(itemId, result.data);
          }
        },
        error: (err) => {
          const message =
            err.message ?? "Failed to purchase champion's market item";
          this._error.set(message);
          this.toastService.showToast('Purchase failed', message, false);
        },
      });
  }

  pickRandomOpponents(): void {
    this._opponents.set(
      this._allOpponents()
        .map((opponent) => ({ ...opponent }))
        .sort(() => Math.random() - 0.5)
        .slice(0, 6)
        .sort((a, b) => b.opponentRating - a.opponentRating),
    );
  }

  startArenaBattle(enemyId: string): void {
    this._loading.set(true);
    this._error.set(null);

    this.colosseumService
      .startArenaBattle(enemyId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => this.applyStartBattleResponse(response),
        error: (err) => {
          const message = err.message ?? 'Failed to start arena battle';
          this._error.set(message);
          this.toastService.showToast('Challenge failed', message, false);
        },
      });
  }

  skipColosseumMatch(): void {
    this.colosseumService.skipColosseumMatch();
  }

  private applyStartBattleResponse(
    result: VersionedMutationResult<StartArenaBattleResponse>,
  ): void {
    if (
      !this.applyOwnedColosseumMutation(result, (response) => {
        const state = response.state;
        this.statusRequestEpoch += 1;
        this.opponentsRequestEpoch += 1;
        this.rankingsRequestEpoch += 1;
        this.matchesRequestEpoch += 1;
        this._status.set(state.status);
        this._allOpponents.set(state.opponents);
        this.pickRandomOpponents();
        this._rankings.set(
          [...state.rankings].sort((left, right) => left.rank - right.rank),
        );
        this._previousMatches.set(
          [...state.previousMatches].sort(
            (left, right) =>
              new Date(right.playedAt).getTime() -
              new Date(left.playedAt).getTime(),
          ),
        );
        this.applyTicketStatus(response.arenaTicketStatus);
        this.syncNotificationCount(state.status);
        this.combatService.startColosseumMatchSimulation(response.battle);
      })
    ) {
      return;
    }

    this.characterState.applyVersionedCharacter({
      data: { character: result.data.state.character },
      domainVersions: result.domainVersions,
    });
  }

  private applyTicketStatus(status: ArenaTicketStatus): void {
    this.ticketRequestEpoch += 1;
    this._arenaTicketStatus.set(status);
  }

  private isParticipant(
    event: ArenaBattleCompleted,
    characterId: string,
  ): boolean {
    return event.characterId === characterId || event.enemyId === characterId;
  }

  private applyArenaRating(
    event: ArenaBattleCompleted,
    characterId: string,
  ): void {
    const arenaRating =
      event.characterId === characterId
        ? event.characterRatingAfter
        : event.enemyRatingAfter;
    const character = this.characterState.currentCharacter();

    if (character) {
      this.characterState.updateCharacter({
        ...character,
        arenaRating,
      });
    }

    const status = this._status();
    if (status) {
      this.statusRequestEpoch += 1;
      this._status.set({
        ...status,
        rating: arenaRating,
      });
    }
  }

  private addNotification(): void {
    this.notificationService.increment(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Colosseum,
    );
  }

  private syncNotificationCount(status: ColosseumStatus | null): void {
    this.notificationService.setCount(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Colosseum,
      this.countStatusActions(status),
    );
  }

  private countStatusActions(status: ColosseumStatus | null): number {
    if (!status) return 0;

    const hasCappedTickets =
      status.maxTickets > 0 && status.tickets >= status.maxTickets;
    if (!hasCappedTickets) return 0;

    const defenseNeedsUpdate =
      !status.defenseStatus?.isValid || status.defenseStatus.isOutdated;

    return [
      hasCappedTickets,
      status.dailyFirstWinAvailable,
      defenseNeedsUpdate,
    ].filter(Boolean).length;
  }

  private showChampionMarketPurchaseToast(
    itemId: string,
    response: ChampionMarketPurchaseResponse | null,
  ): void {
    const item = this._championMarket()?.items.find(
      (marketItem) => marketItem.id === itemId,
    );
    const itemName = item?.name ?? "Champion's Market item";
    const rewards = this.formatChampionMarketRewards(response);

    this.toastService.showToast(
      'Purchase complete',
      `${itemName} purchased${rewards}.`,
      true,
    );
  }

  private formatChampionMarketRewards(
    response: ChampionMarketPurchaseResponse | null,
  ): string {
    if (!response) return '';

    const rewards = [
      response.cindersGranted > 0 ? `${response.cindersGranted} Cinders` : null,
      response.soulstonesGranted > 0
        ? `${response.soulstonesGranted} Soulstones`
        : null,
      response.sigilFragmentsGranted > 0
        ? `${response.sigilFragmentsGranted} Sigil Fragments`
        : null,
      response.rewardItemQuantity > 0
        ? `${response.rewardItemQuantity} ${response.rewardItemName ?? response.rewardItemId ?? 'items'}`
        : null,
    ].filter((reward): reward is string => reward !== null);

    const spent = `${response.glorySpent} Glory spent`;
    return rewards.length > 0
      ? `: ${rewards.join(', ')}; ${spent}`
      : `: ${spent}`;
  }

  private applyChampionMarketPurchase(
    response: ChampionMarketPurchaseResponse,
  ): void {
    const character = this.characterState.currentCharacter();
    this.applyGloryBalance(response.gloryRemaining);
    this.applyChampionMarketItemPurchase(response);
    this.inventoryState.applyInventoryGrant(
      response.inventoryGrantId,
      response.inventoryItemsGranted ?? [],
    );

    if (character) {
      this.characterState.updateCharacter({
        ...character,
        cinders: character.cinders + response.cindersGranted,
        soulstones: character.soulstones + response.soulstonesGranted,
        sigilFragments:
          character.sigilFragments + response.sigilFragmentsGranted,
      });
    }
  }

  private applyDefenseStatus(defenseStatus: ArenaDefenseStatus): void {
    const status = this._status();
    if (!status) return;

    this.statusRequestEpoch += 1;
    this._status.set({ ...status, defenseStatus });
    this.syncNotificationCount(this._status());
  }

  private applyOwnedColosseumMutation<T>(
    result: VersionedMutationResult<T>,
    apply: (response: T) => void,
  ): boolean {
    const version = result.domainVersions['colosseum'];
    if (version === undefined) {
      apply(result.data);
      return true;
    }

    const currentVersion = this.stateSync.latestRevision('colosseum');
    if (version < currentVersion) return false;
    if (version > currentVersion + 1) {
      this.stateSync.rejectMutationResponse('colosseum', version);
      return false;
    }

    apply(result.data);
    this.stateSync.acceptSnapshotResponse({ colosseum: version }, [
      'colosseum',
    ]);
    return true;
  }

  private applyChampionMarketItemPurchase(
    response: ChampionMarketPurchaseResponse,
  ): void {
    const market = this._championMarket();
    if (!market) return;

    this.marketRequestEpoch += 1;
    this._championMarket.set({
      ...market,
      glory: response.gloryRemaining,
      items: market.items.map((item) => {
        if (item.id !== response.itemId) return item;

        const remainingWeeklyPurchases =
          item.weeklyPurchaseLimit == null
            ? item.remainingWeeklyPurchases
            : Math.max(0, item.remainingWeeklyPurchases - response.quantity);
        const remainingLifetimePurchases =
          item.lifetimePurchaseLimit == null
            ? item.remainingLifetimePurchases
            : Math.max(0, item.remainingLifetimePurchases - response.quantity);

        return {
          ...item,
          remainingWeeklyPurchases,
          remainingLifetimePurchases,
        };
      }),
    });
  }

  private applyGloryBalance(glory: number): void {
    const status = this._status();
    if (status) {
      this.statusRequestEpoch += 1;
      this._status.set({
        ...status,
        glory,
      });
    }

    const market = this._championMarket();
    if (market) {
      this.marketRequestEpoch += 1;
      this._championMarket.set({
        ...market,
        glory,
      });
    }
  }

  private deriveChampionMarket(): ChampionMarketView | null {
    const market = this._championMarket();
    if (!market) return null;

    const status = this._status();
    const rating =
      status?.rating ??
      this.characterState.currentCharacter()?.arenaRating ??
      0;
    const glory = status?.glory ?? market.glory;

    return {
      ...market,
      glory,
      items: market.items.map((item) =>
        this.deriveChampionMarketItem(item, glory, rating),
      ),
    };
  }

  private deriveChampionMarketItem(
    item: ChampionMarketItem,
    glory: number,
    rating: number,
  ): ChampionMarketItemView {
    const cannotPurchaseReason = this.getCannotPurchaseReason(
      item,
      glory,
      rating,
    );

    return {
      ...item,
      canPurchase: cannotPurchaseReason === null,
      cannotPurchaseReason,
    };
  }

  private getCannotPurchaseReason(
    item: ChampionMarketItem,
    glory: number,
    rating: number,
  ): string | null {
    if (item.remainingWeeklyPurchases <= 0) return 'Weekly limit reached';
    if (item.remainingLifetimePurchases <= 0) return 'Already purchased';
    if (glory < item.gloryCost) return 'Not enough Glory';
    if (item.requiredRating != null && rating < item.requiredRating) {
      return `Requires ${item.requiredRating} rating`;
    }
    if (
      item.requiredRankTier &&
      item.requiredRankMinRating != null &&
      rating < item.requiredRankMinRating
    ) {
      return `Requires ${this.formatRankTier(item.requiredRankTier)}`;
    }

    return null;
  }

  private formatRankTier(value: string): string {
    return value
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, (letter) => letter.toUpperCase());
  }
}
