import { Injectable, signal, computed, effect } from '@angular/core';
import { finalize, Observable, of, shareReplay, tap } from 'rxjs';
import { Guild, GuildSimple } from '../../../../shared/models/Dtos/guild/guild';
import { GuildInvite } from '../../../../shared/models/Dtos/guild/guildInvite';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildService } from './guild.service';
import {
  GuildMissionOverview,
  PersonalGuildOrder,
} from '../../../../shared/models/Dtos/guild/guildMission';
import { GuildShopOverview } from '../../../../shared/models/Dtos/guild/guildShop';
import { GuildRole } from '../../../../shared/models/Dtos/guild/guildRole';
import { GuildRolePermission } from '../../../../shared/models/Dtos/guild/guildRolePermission';
import {
  GuildBuilding,
  GuildBuildingOverview,
  GuildBuildingType,
} from '../../../../shared/models/Dtos/guild/guildBuilding';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { GameRealtimeConnection } from '../../real-time/game-realtime/game-realtime-connection.service';
import { AuthService } from '../auth/auth.service';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
  SIDEBAR_NOTIFICATION,
} from '../../client-side/notifications/notification.service';
import { RealtimeSignalDeduper } from '../../real-time/game-realtime/realtime-deduplication';
import { GameRealtimeSignalEventName } from '../../real-time/game-realtime/game-realtime-contracts';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';

type GuildRealtimeEventName = Extract<
  GameRealtimeSignalEventName,
  | 'GuildApplication'
  | 'GuildInviteReceived'
  | 'GuildApplicationRejected'
  | 'GuildBuildingsChanged'
  | 'GuildMissionsChanged'
  | 'GuildDirectoryChanged'
>;

type GuildRealtimeScope = 'any' | 'member' | 'nonMember';

interface GuildRealtimeContext {
  guildId: string | null;
  characterId: string | null;
}

type GuildRealtimePayload = {
  guildId?: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
};

interface GuildRealtimeHandler {
  eventName: GuildRealtimeEventName;
  key: string;
  scope: GuildRealtimeScope;
  matches?: (
    payload: GuildRealtimePayload,
    context: GuildRealtimeContext,
  ) => boolean;
  action?: (
    payload: GuildRealtimePayload,
    context: GuildRealtimeContext,
  ) => void;
}

interface PendingGuildDescription {
  id: number;
  guildId: string;
  description: string;
  previousDescription: string;
}

export function isHandledGuildInitiatorEcho(
  payload: GuildRealtimePayload,
  characterId: string | null,
): boolean {
  return (
    payload.initiatorHandled === true &&
    !!characterId &&
    payload.actorCharacterId === characterId
  );
}

export function normalizeGuildMissionOverview(
  missions: GuildMissionOverview | null,
  guildId: string | null,
): GuildMissionOverview | null {
  if (!missions || !guildId || missions.guildId !== guildId) return null;

  const originalOrders = Array.isArray(missions.personalOrders)
    ? missions.personalOrders
    : [];
  const personalOrders = originalOrders.filter(isValidPersonalGuildOrder);

  return originalOrders === missions.personalOrders &&
    personalOrders.length === originalOrders.length
    ? missions
    : { ...missions, personalOrders };
}

export function normalizeGuild(guild: Guild | null): Guild | null {
  if (!guild) return null;

  const rolePermissions = Array.isArray(guild.rolePermissions)
    ? guild.rolePermissions
    : [];
  const vaultItems = Array.isArray(guild.vaultItems) ? guild.vaultItems : [];

  return rolePermissions === guild.rolePermissions &&
    vaultItems === guild.vaultItems
    ? guild
    : { ...guild, rolePermissions, vaultItems };
}

function isValidPersonalGuildOrder(
  order: PersonalGuildOrder | null | undefined,
): order is PersonalGuildOrder {
  return !!(
    order?.id &&
    order.definition?.id &&
    order.definition.name?.trim() &&
    order.reward
  );
}

@Injectable({ providedIn: 'root' })
export class GuildStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _guild = signal<Guild | null>(null);
  private readonly _buildings = signal<GuildBuildingOverview | null>(null);
  private readonly _missions = signal<GuildMissionOverview | null>(null);
  private readonly _shop = signal<GuildShopOverview | null>(null);
  private readonly _invites = signal<GuildInvite[]>([]);
  private readonly _allGuilds = signal<GuildSimple[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private readonly guildRealtimeHandlers = this.createGuildRealtimeHandlers();
  private readonly hasLoaded = signal(false);
  private lastTokenGuildId: string | null | undefined = undefined;
  private refreshRequestId = 0;
  private guildSync$: Observable<unknown> | null = null;
  private guildSyncTargetRevision = 0;
  private buildingRequestId = 0;
  private missionRequestId = 0;
  private shopRequestId = 0;
  private directoryRequestId = 0;
  private inviteRequestId = 0;
  private descriptionMutationId = 0;
  private pendingDescription: PendingGuildDescription | null = null;
  private activeMissionViews = 0;
  private missionsDirty = false;
  private guildIdentityInitialized = false;
  private reconciledGuildSubscriptionId: string | null = null;

  /* ─────────── public, read-only selectors ─────────── */
  readonly guild = computed(() => this._guild());
  readonly buildings = computed(() => this._buildings());
  readonly missions = computed(() => this._missions());
  readonly shop = computed(() => this._shop());
  readonly invites = computed(() => this._invites());
  readonly allGuilds = computed(() => this._allGuilds());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isInGuild = computed(() => !!this._guild());
  readonly hasInvites = computed(() => this._invites().length > 0);
  readonly claimableDailyOrderCount = computed(() => {
    const guildId = this._guild()?.id ?? null;
    const missions = normalizeGuildMissionOverview(this._missions(), guildId);
    return (
      missions?.personalOrders.filter((order) => order.canClaimReward).length ??
      0
    );
  });
  readonly guildNotificationCount = computed(() =>
    this.notificationService.count(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Guild,
    ),
  );

  constructor(
    private readonly service: GuildService,
    private readonly eventService: GameRealtimeEventRegistry,
    private readonly realtime: GameRealtimeConnection,
    private readonly auth: AuthService,
    private readonly notificationService: NotificationService,
    private readonly inventoryState: InventoryStateService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register(
      'guild',
      'guild',
      (context) => this.synchronize(context.targetRevision),
      () => this.hasLoaded(),
    );
    this.stateSync.register(
      'guild-buildings',
      'guild-buildings',
      () => this.synchronizeBuildings(),
      () => this.hasLoaded() && !!this._guild(),
    );
    this.stateSync.register(
      'guild-missions',
      'guild-missions',
      () => this.synchronizeMissions(),
      () => this.hasLoaded() && !!this._guild(),
    );
    this.stateSync.register(
      'guild-shop',
      'guild-shop',
      () => this.synchronizeShop(),
      () => this.hasLoaded() && !!this._guild(),
    );
    this.stateSync.register(
      'guild-membership',
      'guild-membership',
      (context) => this.synchronize(context.targetRevision),
      () => this.hasLoaded(),
    );
    this.stateSync.register(
      'guild-invites',
      'guild-invites',
      () => this.synchronizeInvites(),
      () => this.hasLoaded() && !this._guild(),
    );
    this.stateSync.register(
      'guild-directory',
      'guild-directory',
      () => this.synchronizeDirectory(),
      () => this.hasLoaded(),
    );
    this.refresh(); // initial fetch

    effect(() => {
      const guildId = this._guild()?.id ?? null;
      void this.updateGuildRealtimeSubscription(guildId);
    });

    effect(
      () => {
        this.handleGuildRealtimeEvents();
      },
      { allowSignalWrites: true },
    );
  }

  private async updateGuildRealtimeSubscription(
    guildId: string | null,
  ): Promise<void> {
    if (!guildId) this.reconciledGuildSubscriptionId = null;

    try {
      await this.realtime.setGuildSubscription(guildId);
    } catch (error) {
      console.warn('Failed to update guild realtime subscription', error);
      return;
    }

    if (
      !guildId ||
      this._guild()?.id !== guildId ||
      this.reconciledGuildSubscriptionId === guildId
    ) {
      return;
    }

    this.reconciledGuildSubscriptionId = guildId;
    await this.stateSync.reconcile({ afterCurrent: true });
  }

  donateVaultItem(equipmentInstanceId: string): Observable<void> {
    return this.service.donateVaultItem(equipmentInstanceId);
  }

  borrowVaultItem(vaultItemId: string): Observable<void> {
    return this.service.borrowVaultItem(vaultItemId);
  }

  returnVaultItem(vaultItemId: string): Observable<void> {
    return this.service.returnVaultItem(vaultItemId);
  }

  withdrawVaultItem(vaultItemId: string): Observable<void> {
    return this.service.withdrawVaultItem(vaultItemId);
  }

  private createGuildRealtimeHandlers(): GuildRealtimeHandler[] {
    const inCurrentGuild = (
      payload: GuildRealtimePayload,
      context: GuildRealtimeContext,
    ) => !!payload.guildId && payload.guildId === context.guildId;

    return [
      {
        eventName: 'GuildDirectoryChanged',
        key: 'guild-directory-changed',
        scope: 'any',
        action: () => this.loadAllGuilds(),
      },
      {
        eventName: 'GuildInviteReceived',
        key: 'guild-invite-received',
        scope: 'nonMember',
        action: () => {
          this.addGuildNotification();
          this.loadMyInvites();
        },
      },
      {
        eventName: 'GuildApplicationRejected',
        key: 'guild-application-rejected',
        scope: 'nonMember',
        action: () => this.loadMyInvites(),
      },
      {
        eventName: 'GuildBuildingsChanged',
        key: 'guild-buildings-changed',
        scope: 'member',
        matches: inCurrentGuild,
        action: (payload, context) => {
          if (!isHandledGuildInitiatorEcho(payload, context.characterId)) {
            this.loadGuildBuildings(context.guildId);
          }
          this.loadGuildShop(context.guildId);
        },
      },
      {
        eventName: 'GuildMissionsChanged',
        key: 'guild-missions-changed',
        scope: 'member',
        matches: inCurrentGuild,
        action: (payload, context) => {
          if (!isHandledGuildInitiatorEcho(payload, context.characterId)) {
            this.markMissionsChanged(context.guildId);
          }
        },
      },
      {
        eventName: 'GuildApplication',
        key: 'guild-application',
        scope: 'member',
        matches: inCurrentGuild,
        action: () => this.addGuildNotification(),
      },
    ];
  }

  private handleGuildRealtimeEvents(): void {
    const context: GuildRealtimeContext = {
      guildId: this._guild()?.id ?? null,
      characterId: this.auth.currentCharacter()?.id ?? null,
    };

    for (const handler of this.guildRealtimeHandlers) {
      this.processGuildRealtimeHandler(handler, context);
    }
  }

  private processGuildRealtimeHandler(
    handler: GuildRealtimeHandler,
    context: GuildRealtimeContext,
  ): void {
    if (!this.isGuildHandlerInScope(handler.scope, context.guildId)) return;

    const envelope = this.eventService.eventEnvelope[handler.eventName]();
    const payload = envelope?.payload as GuildRealtimePayload | undefined;
    if (!payload) return;
    if (handler.matches && !handler.matches(payload, context)) return;
    if (!this.eventDeduper.shouldProcess(handler.key, envelope)) return;
    handler.action?.(payload, context);
  }

  private isGuildHandlerInScope(
    scope: GuildRealtimeScope,
    guildId: string | null,
  ): boolean {
    switch (scope) {
      case 'member':
        return !!guildId;
      case 'nonMember':
        return !guildId;
      case 'any':
        return true;
    }
  }

  /* ─────────── high-level commands ─────────── */

  /** Call once at login or when character changes */
  refresh(): void {
    this.synchronize().subscribe({ error: () => undefined });
  }

  private synchronize(targetRevision = 0): Observable<unknown> {
    if (this.guildSync$ && this.guildSyncTargetRevision >= targetRevision) {
      return this.guildSync$;
    }

    this.hasLoaded.set(true);
    this._loading.set(true);
    const requestId = ++this.refreshRequestId;

    const request$ = this.service.getMyGuild().pipe(
      tap({
        next: (responseGuild) => {
          if (requestId !== this.refreshRequestId) return;
          this.applyGuildSnapshot(responseGuild);
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      }),
      finalize(() => {
        if (this.guildSync$ === request$) {
          this.guildSync$ = null;
          this.guildSyncTargetRevision = 0;
        }
        if (requestId === this.refreshRequestId) this._loading.set(false);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    this.guildSync$ = request$;
    this.guildSyncTargetRevision = targetRevision;
    return request$;
  }

  private applyGuildSnapshot(responseGuild: Guild | null): void {
    let guild = normalizeGuild(responseGuild);

    if (guild && this.pendingDescription?.guildId === guild.id) {
      guild = { ...guild, description: this.pendingDescription.description };
    }

    const nextGuildId = guild?.id ?? null;
    const previousGuildId = this._guild()?.id ?? null;
    const guildIdentityChanged = previousGuildId !== nextGuildId;
    if (guildIdentityChanged) {
      this.clearGuildScopedState();
      if (this.guildIdentityInitialized) {
        for (const scope of [
          'guild',
          'guild-buildings',
          'guild-missions',
          'guild-shop',
        ] as const) {
          this.stateSync.resetScope(scope);
        }
        void this.stateSync.reconcile();
      }
    }
    this.guildIdentityInitialized = true;
    this.refreshAuthSessionIfGuildChanged(nextGuildId);

    if (guild) {
      this._guild.set(guild);
      this.initializeGuildNotificationCount(
        guild.invites.filter((invite: GuildInvite) => !invite.isInvite).length,
      );
      this._invites.set([]);
      if (guildIdentityChanged) {
        this.loadGuildBuildings(guild.id);
        if (
          this.activeMissionViews > 0 ||
          this._missions()?.guildId !== guild.id
        ) {
          this.loadGuildMissions(guild.id);
        } else {
          this.missionsDirty = true;
        }
        this.loadGuildShop(guild.id);
        this.loadAllGuilds();
      }
    } else {
      this._guild.set(null);
      this.clearGuildScopedState();
      this.loadAllGuilds();
      this.loadMyInvites();
    }

    for (const scope of [
      'guild',
      'guild-buildings',
      'guild-missions',
      'guild-shop',
      'guild-membership',
      'guild-invites',
      'guild-directory',
    ] as const) {
      this.stateSync.activate(scope, scope);
    }
  }

  refreshNotificationCount(): void {
    this.refresh();
  }

  /* ─────────── guild lifecycle ─────────── */

  create(name: string): void {
    this._loading.set(true);

    this.service
      .create(name)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => this.refresh(),
        error: (e) => this._error.set(e.message ?? 'Failed to create guild'),
      });
  }

  leave(): void {
    this._loading.set(true);

    this.service
      .leave()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this._guild.set(null);
          this.clearGuildScopedState();
          this.refresh();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to leave guild'),
      });
  }

  disband(): void {
    this._loading.set(true);

    this.service
      .disband()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this._guild.set(null);
          this.clearGuildScopedState();
          this.refresh();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to disband guild'),
      });
  }

  constructBuilding(buildingType: GuildBuildingType): void {
    this._loading.set(true);

    this.service
      .constructBuilding(buildingType)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          const buildings = response.data;
          if (
            !this.domainVersions.isCurrent(
              'guild-buildings',
              response.domainVersions['guild-buildings'],
            )
          ) {
            return;
          }
          if (buildings.guildId === this._guild()?.id) {
            this.buildingRequestId += 1;
            this._buildings.set(buildings);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to construct guild building'),
      });
  }

  upgradeBuilding(building: GuildBuilding): void {
    if (!building.id) return;

    this._loading.set(true);

    this.service
      .upgradeBuilding(building.id)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          const buildings = response.data;
          if (
            !this.domainVersions.isCurrent(
              'guild-buildings',
              response.domainVersions['guild-buildings'],
            )
          ) {
            return;
          }
          if (buildings.guildId === this._guild()?.id) {
            this.buildingRequestId += 1;
            this._buildings.set(buildings);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to upgrade guild building'),
      });
  }

  setBuildingTarget(building: GuildBuilding): void {
    this._loading.set(true);

    this.service
      .setBuildingTarget(building.definition.type)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          const buildings = response.data;
          if (
            !this.domainVersions.isCurrent(
              'guild-buildings',
              response.domainVersions['guild-buildings'],
            )
          ) {
            return;
          }
          if (buildings.guildId === this._guild()?.id) {
            this.buildingRequestId += 1;
            this._buildings.set(buildings);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to set guild building target'),
      });
  }

  selectMission(optionId: string): void {
    this._loading.set(true);

    this.service
      .selectMission(optionId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          if (
            this.domainVersions.isCurrent(
              'guild-missions',
              response.domainVersions['guild-missions'],
            )
          ) {
            this.setMissionOverviewIfCurrent(response.data);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to select guild mission'),
      });
  }

  claimOrderReward(orderId: string): void {
    this._loading.set(true);

    this.service
      .claimOrderReward(orderId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          if (
            this.domainVersions.isCurrent(
              'guild-missions',
              response.domainVersions['guild-missions'],
            )
          ) {
            this.setMissionOverviewIfCurrent(response.data);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to claim guild order'),
      });
  }

  claimWeeklyMissionReward(): void {
    this._loading.set(true);

    this.service
      .claimWeeklyMissionReward()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          if (
            this.domainVersions.isCurrent(
              'guild-missions',
              response.domainVersions['guild-missions'],
            )
          ) {
            this.setMissionOverviewIfCurrent(response.data);
          }
        },
        error: (e) =>
          this._error.set(
            e.message ?? 'Failed to claim weekly guild mission reward',
          ),
      });
  }

  activateMissionsView(): void {
    this.activeMissionViews += 1;
    const guildId = this._guild()?.id ?? null;
    if (this.activeMissionViews === 1 && this.missionsDirty && guildId) {
      this.loadGuildMissions(guildId);
    }
  }

  deactivateMissionsView(): void {
    this.activeMissionViews = Math.max(0, this.activeMissionViews - 1);
  }

  purchaseShopItem(itemKey: string): void {
    this._loading.set(true);

    this.service
      .purchaseShopItem(itemKey)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          const response = result.data;
          if (
            this.domainVersions.isCurrent(
              'guild-shop',
              result.domainVersions['guild-shop'],
            ) &&
            response.guildId === this._guild()?.id
          ) {
            this.shopRequestId += 1;
            this._shop.set(response);
          }
          this.inventoryState.applyInventoryGrant(
            response.inventoryGrantId,
            response.inventoryItemsGranted ?? [],
            result.domainVersions['inventory'],
          );
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to purchase guild shop item'),
      });
  }

  /* ─────────── directory & invites ─────────── */

  private loadAllGuilds(): void {
    this.synchronizeDirectory().subscribe({ error: () => undefined });
  }

  private loadMyInvites(): void {
    this.synchronizeInvites().subscribe({ error: () => undefined });
  }

  private synchronizeDirectory(): Observable<GuildSimple[]> {
    const requestId = ++this.directoryRequestId;
    return this.service.getAllGuilds().pipe(
      tap({
        next: (guilds) => {
          if (requestId === this.directoryRequestId) {
            this._allGuilds.set(guilds);
          }
        },
        error: (e) => {
          if (requestId === this.directoryRequestId) {
            this._error.set(e.message ?? 'Failed to load guild list');
          }
        },
      }),
    );
  }

  private synchronizeInvites(): Observable<GuildInvite[]> {
    const requestId = ++this.inviteRequestId;
    return this.service.getMyInvites().pipe(
      tap({
        next: (invites) => {
          if (requestId !== this.inviteRequestId) return;
          this._invites.set(invites);
          this.initializeGuildNotificationCount(
            invites.filter((invite: GuildInvite) => invite.isInvite).length,
          );
        },
        error: (e) => {
          if (requestId === this.inviteRequestId) {
            this._error.set(e.message ?? 'Failed to load invites');
          }
        },
      }),
    );
  }

  private loadGuildBuildings(expectedGuildId: string | null): void {
    this.synchronizeBuildings(expectedGuildId).subscribe({
      error: () => undefined,
    });
  }

  private synchronizeBuildings(
    expectedGuildId = this._guild()?.id ?? null,
  ): Observable<GuildBuildingOverview | null> {
    if (!expectedGuildId) {
      this._buildings.set(null);
      return of(null);
    }

    const requestId = ++this.buildingRequestId;
    return this.service.getBuildings().pipe(
      tap({
        next: (buildings) => {
          if (
            requestId !== this.buildingRequestId ||
            this._guild()?.id !== expectedGuildId
          ) {
            return;
          }
          this._buildings.set(
            buildings?.guildId === expectedGuildId ? buildings : null,
          );
        },
        error: (e) => {
          if (
            requestId === this.buildingRequestId &&
            this._guild()?.id === expectedGuildId
          ) {
            this._error.set(e.message ?? 'Failed to load buildings');
          }
        },
      }),
    );
  }

  private loadGuildMissions(expectedGuildId: string): void {
    this.synchronizeMissions(expectedGuildId, true).subscribe({
      error: () => undefined,
    });
  }

  private synchronizeMissions(
    expectedGuildId = this._guild()?.id ?? null,
    force = false,
  ): Observable<GuildMissionOverview | null | undefined> {
    if (!expectedGuildId) {
      this._missions.set(null);
      this.missionsDirty = false;
      return of(null);
    }
    if (!force && this.activeMissionViews === 0) {
      this.missionsDirty = true;
      return of(undefined);
    }

    const requestId = ++this.missionRequestId;
    return this.service.getMissions().pipe(
      tap({
        next: (missions) => {
          if (
            requestId !== this.missionRequestId ||
            this._guild()?.id !== expectedGuildId
          ) {
            return;
          }
          this._missions.set(
            normalizeGuildMissionOverview(missions, expectedGuildId),
          );
          this.missionsDirty = false;
        },
        error: (e) => {
          if (
            requestId === this.missionRequestId &&
            this._guild()?.id === expectedGuildId
          ) {
            this.missionsDirty = true;
            this._error.set(e.message ?? 'Failed to load missions');
          }
        },
      }),
    );
  }

  private markMissionsChanged(guildId: string | null): void {
    if (!guildId) return;

    this.missionsDirty = true;
    if (this.activeMissionViews > 0) {
      this.loadGuildMissions(guildId);
    }
  }

  private loadGuildShop(expectedGuildId: string | null): void {
    this.synchronizeShop(expectedGuildId).subscribe({ error: () => undefined });
  }

  private synchronizeShop(
    expectedGuildId = this._guild()?.id ?? null,
  ): Observable<GuildShopOverview | null> {
    if (!expectedGuildId) {
      this._shop.set(null);
      return of(null);
    }

    const requestId = ++this.shopRequestId;
    return this.service.getShop().pipe(
      tap({
        next: (shop) => {
          if (
            requestId !== this.shopRequestId ||
            this._guild()?.id !== expectedGuildId
          ) {
            return;
          }
          this._shop.set(shop?.guildId === expectedGuildId ? shop : null);
        },
        error: (e) => {
          if (
            requestId === this.shopRequestId &&
            this._guild()?.id === expectedGuildId
          ) {
            this._error.set(e.message ?? 'Failed to load guild shop');
          }
        },
      }),
    );
  }

  applyToGuild(guildId: string): void {
    this.service.applyToGuild(guildId).subscribe({
      next: () => this.loadMyInvites(),
      error: (e) => this._error.set(e.message ?? 'Failed to apply to guild'),
    });
  }

  invite(payload: InviteToGuild): void {
    this.service.invite(payload).subscribe({
      next: () => this.refresh(),
      error: (e) => this._error.set(e.message ?? 'Failed to invite'),
    });
  }

  inviteCharacterByName(payload: InviteToGuild): void {
    this.service.inviteCharacterByName(payload).subscribe({
      next: () => this.refresh(),
      error: (e) => this._error.set(e.message ?? 'Failed to invite character'),
    });
  }

  acceptInvite(guildId: string): void {
    this.service.acceptInvite(guildId).subscribe({
      next: () => this.refresh(),
      error: (e) => this._error.set(e.message ?? 'Failed to accept invite'),
    });
  }

  rejectInvite(guildId: string): void {
    this.service.rejectInvite(guildId).subscribe({
      next: () => this.loadMyInvites(),
      error: (e) => this._error.set(e.message ?? 'Failed to reject invite'),
    });
  }

  approveApplication(characterId: string): void {
    this.service.approveApplication(characterId).subscribe({
      next: () => this.refresh(),
      error: (e) =>
        this._error.set(e.message ?? 'Failed to approve application'),
    });
  }

  rejectApplication(characterId: string): void {
    this.service.rejectApplication(characterId).subscribe({
      next: () => this.refresh(),
      error: (e) =>
        this._error.set(e.message ?? 'Failed to reject application'),
    });
  }

  changeMemberRole(characterId: string, role: GuildRole): void {
    this.service.changeMemberRole(characterId, role).subscribe({
      next: () => this.refresh(),
      error: (e) =>
        this._error.set(e.message ?? 'Failed to change guild member role'),
    });
  }

  kickMember(characterId: string): void {
    this.service.kickMember(characterId).subscribe({
      next: () => this.refresh(),
      error: (e) => this._error.set(e.message ?? 'Failed to kick guild member'),
    });
  }

  updateRolePermissions(permissions: GuildRolePermission): void {
    this.service.updateRolePermissions(permissions).subscribe({
      next: () => this.refresh(),
      error: (e) =>
        this._error.set(e.message ?? 'Failed to update role permissions'),
    });
  }

  updateDescription(description: string): void {
    const guild = this._guild();
    const mutationId = ++this.descriptionMutationId;

    if (guild) {
      this.pendingDescription = {
        id: mutationId,
        guildId: guild.id,
        description,
        previousDescription: guild.description ?? '',
      };
      this._guild.set({ ...guild, description });
    }

    this.service.updateDescription(description).subscribe({
      next: () => {
        if (mutationId !== this.descriptionMutationId) return;
        this.pendingDescription = null;
        this.refresh();
      },
      error: (e) => {
        if (mutationId !== this.descriptionMutationId) return;
        const pending = this.pendingDescription;
        if (pending?.id === mutationId) {
          const currentGuild = this._guild();
          if (currentGuild?.id === pending.guildId) {
            this._guild.set({
              ...currentGuild,
              description: pending.previousDescription,
            });
          }
          this.pendingDescription = null;
        }
        this._error.set(e.message ?? 'Failed to update the guild description');
      },
    });
  }

  /* ─────────── optional optimistic helpers ─────────── */
  setGuild(guild: Guild | null) {
    if ((this._guild()?.id ?? null) !== (guild?.id ?? null)) {
      this.clearGuildScopedState();
    }
    this._guild.set(normalizeGuild(guild));
  }
  setInvites(inv: GuildInvite[]) {
    this._invites.set(inv);
  }
  setAllGuilds(gs: GuildSimple[]) {
    this._allGuilds.set(gs);
  }

  markGuildNotificationsSeen(): void {
    this.notificationService.markSeen(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Guild,
    );
  }

  private addGuildNotification(): void {
    this.notificationService.increment(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Guild,
    );
  }

  private initializeGuildNotificationCount(count: number): void {
    this.notificationService.initializeCount(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Guild,
      count,
    );
  }

  private refreshAuthSessionIfGuildChanged(nextGuildId: string | null): void {
    if (this.lastTokenGuildId === nextGuildId) return;

    this.lastTokenGuildId = nextGuildId;
    if (!this.auth.isAuthenticated()) return;

    this.auth.refreshSession().subscribe({
      error: () => undefined,
    });
  }

  private setMissionOverviewIfCurrent(
    missions: GuildMissionOverview | null,
  ): void {
    this.missionRequestId += 1;
    this._missions.set(
      normalizeGuildMissionOverview(missions, this._guild()?.id ?? null),
    );
  }

  private clearGuildScopedState(): void {
    this.buildingRequestId += 1;
    this.missionRequestId += 1;
    this.shopRequestId += 1;
    this.inviteRequestId += 1;
    this.descriptionMutationId += 1;
    this.pendingDescription = null;
    this.missionsDirty = false;
    this._buildings.set(null);
    this._missions.set(null);
    this._shop.set(null);
  }
}
