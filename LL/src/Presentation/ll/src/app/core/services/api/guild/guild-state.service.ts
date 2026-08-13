import { Injectable, signal, computed, effect } from '@angular/core';
import { finalize } from 'rxjs';
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
import { GameEventService } from '../../real-time/game-event.service';
import { AuthService } from '../auth/auth.service';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
  SIDEBAR_NOTIFICATION,
} from '../../client-side/notifications/notification.service';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';
import { GameEventName } from '../../real-time/game-event/game-event.map';
import { InventoryStateService } from '../inventory/inventory-state.service';

type GuildRealtimeEventName = Extract<
  GameEventName,
  | 'GuildApplicationMsg'
  | 'GuildInviteReceivedMsg'
  | 'GuildInviteRejectedMsg'
  | 'GuildApplicationRejectedMsg'
  | 'GuildBuildingsChangedMsg'
  | 'GuildStateChangedMsg'
  | 'GuildMembershipChangedMsg'
  | 'GuildDisbandedMsg'
  | 'GuildDirectoryChangedMsg'
>;

type GuildRealtimeScope = 'any' | 'member' | 'nonMember';

interface GuildRealtimeContext {
  guildId: string | null;
  shouldRefresh: boolean;
}

type GuildRealtimePayload = { guildId?: string };

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
  refresh?: boolean;
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
  private readonly eventDeduper = new GameEventDeduper();
  private readonly guildRealtimeHandlers = this.createGuildRealtimeHandlers();
  private hasLoaded = false;
  private lastTokenGuildId: string | null | undefined = undefined;
  private refreshRequestId = 0;

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
    private readonly eventService: GameEventService,
    private readonly auth: AuthService,
    private readonly notificationService: NotificationService,
    private readonly inventoryState: InventoryStateService,
  ) {
    this.refresh(); // initial fetch

    effect(() => {
      const guildId = this._guild()?.id;
      if (!guildId) return;

      void this.eventService
        .subscribeToGuild(guildId)
        .catch((error) =>
          console.warn('Failed to subscribe to guild realtime', error),
        );
    });

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount <= 0 || !this.hasLoaded) return;

        this.refresh();
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        this.handleGuildRealtimeEvents();
      },
      { allowSignalWrites: true },
    );
  }

  private createGuildRealtimeHandlers(): GuildRealtimeHandler[] {
    const inCurrentGuild = (
      payload: GuildRealtimePayload,
      context: GuildRealtimeContext,
    ) => !!payload.guildId && payload.guildId === context.guildId;

    return [
      {
        eventName: 'GuildDirectoryChangedMsg',
        key: 'guild-directory-changed',
        scope: 'any',
        action: () => this.loadAllGuilds(),
      },
      {
        eventName: 'GuildInviteReceivedMsg',
        key: 'guild-invite-received',
        scope: 'nonMember',
        action: () => {
          this.addGuildNotification();
          this.loadMyInvites();
        },
      },
      {
        eventName: 'GuildInviteRejectedMsg',
        key: 'guild-invite-rejected',
        scope: 'nonMember',
        action: () => this.loadMyInvites(),
      },
      {
        eventName: 'GuildApplicationRejectedMsg',
        key: 'guild-application-rejected',
        scope: 'nonMember',
        action: () => this.loadMyInvites(),
      },
      {
        eventName: 'GuildMembershipChangedMsg',
        key: 'guild-membership-changed',
        scope: 'nonMember',
        refresh: true,
      },
      {
        eventName: 'GuildBuildingsChangedMsg',
        key: 'guild-buildings-changed',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
      {
        eventName: 'GuildApplicationMsg',
        key: 'guild-application',
        scope: 'member',
        matches: inCurrentGuild,
        action: () => this.addGuildNotification(),
        refresh: true,
      },
      {
        eventName: 'GuildStateChangedMsg',
        key: 'guild-state-changed',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
      {
        eventName: 'GuildMembershipChangedMsg',
        key: 'guild-membership-changed',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
      {
        eventName: 'GuildDisbandedMsg',
        key: 'guild-disbanded',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
      {
        eventName: 'GuildInviteRejectedMsg',
        key: 'guild-invite-rejected',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
      {
        eventName: 'GuildApplicationRejectedMsg',
        key: 'guild-application-rejected',
        scope: 'member',
        matches: inCurrentGuild,
        refresh: true,
      },
    ];
  }

  private handleGuildRealtimeEvents(): void {
    const context: GuildRealtimeContext = {
      guildId: this._guild()?.id ?? null,
      shouldRefresh: false,
    };

    for (const handler of this.guildRealtimeHandlers) {
      this.processGuildRealtimeHandler(handler, context);
    }

    if (context.shouldRefresh) {
      this.refresh();
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
    context.shouldRefresh ||= !!handler.refresh;
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
    this.hasLoaded = true;
    this._loading.set(true);
    const requestId = ++this.refreshRequestId;

    this.service
      .getMyGuild()
      .pipe(
        finalize(() => {
          if (requestId === this.refreshRequestId) this._loading.set(false);
        }),
      )
      .subscribe({
        next: (responseGuild) => {
          if (requestId !== this.refreshRequestId) return;

          const guild = normalizeGuild(responseGuild);

          const nextGuildId = guild?.id ?? null;
          const previousGuildId = this._guild()?.id ?? null;
          if (previousGuildId !== nextGuildId) {
            this.clearGuildScopedState();
          }
          this.refreshAuthSessionIfGuildChanged(nextGuildId);

          if (guild) {
            this._guild.set(guild);
            this.initializeGuildNotificationCount(
              guild.invites.filter((invite: GuildInvite) => !invite.isInvite)
                .length,
            );
            this._invites.set([]);
            this._allGuilds.set([]);
            this.loadGuildBuildings(guild.id);
            this.loadGuildMissions(guild.id);
            this.loadGuildShop(guild.id);
            this.loadAllGuilds();
          } else {
            this._guild.set(null);
            this.clearGuildScopedState();
            this.loadAllGuilds();
            this.loadMyInvites();
          }
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
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
        next: (buildings) => {
          if (buildings.guildId === this._guild()?.id) {
            this._buildings.set(buildings);
          }
          this.refresh();
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
        next: (buildings) => {
          if (buildings.guildId === this._guild()?.id) {
            this._buildings.set(buildings);
          }
          this.refresh();
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
        next: (buildings) => {
          if (buildings.guildId === this._guild()?.id) {
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
        next: (missions) => this.setMissionOverviewIfCurrent(missions),
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
        next: (missions) => {
          this.setMissionOverviewIfCurrent(missions);
          this.loadGuildShop(this._guild()?.id ?? null);
          this.refresh();
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
        next: (missions) => {
          this.setMissionOverviewIfCurrent(missions);
          this.loadGuildShop(this._guild()?.id ?? null);
          this.refresh();
        },
        error: (e) =>
          this._error.set(
            e.message ?? 'Failed to claim weekly guild mission reward',
          ),
      });
  }

  purchaseShopItem(itemKey: string): void {
    this._loading.set(true);

    this.service
      .purchaseShopItem(itemKey)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.guildId === this._guild()?.id) {
            this._shop.set(response);
          }
          this.inventoryState.applyInventoryGrant(
            response.inventoryGrantId,
            response.inventoryItemsGranted ?? [],
          );
          this.auth.refreshCurrentCharacter();
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to purchase guild shop item'),
      });
  }

  /* ─────────── directory & invites ─────────── */

  private loadAllGuilds(): void {
    this.service.getAllGuilds().subscribe({
      next: (g) => this._allGuilds.set(g),
      error: (e) => this._error.set(e.message ?? 'Failed to load guild list'),
    });
  }

  private loadMyInvites(): void {
    this.service.getMyInvites().subscribe({
      next: (inv) => {
        this._invites.set(inv);
        this.initializeGuildNotificationCount(
          inv.filter((invite: GuildInvite) => invite.isInvite).length,
        );
      },
      error: (e) => this._error.set(e.message ?? 'Failed to load invites'),
    });
  }

  private loadGuildBuildings(expectedGuildId: string): void {
    this.service.getBuildings().subscribe({
      next: (buildings) => {
        if (this._guild()?.id !== expectedGuildId) return;
        this._buildings.set(
          buildings?.guildId === expectedGuildId ? buildings : null,
        );
      },
      error: (e) => this._error.set(e.message ?? 'Failed to load buildings'),
    });
  }

  private loadGuildMissions(expectedGuildId: string): void {
    this.service.getMissions().subscribe({
      next: (missions) => {
        if (this._guild()?.id !== expectedGuildId) return;
        this._missions.set(
          normalizeGuildMissionOverview(missions, expectedGuildId),
        );
      },
      error: (e) => this._error.set(e.message ?? 'Failed to load missions'),
    });
  }

  private loadGuildShop(expectedGuildId: string | null): void {
    if (!expectedGuildId) {
      this._shop.set(null);
      return;
    }

    this.service.getShop().subscribe({
      next: (shop) => {
        if (this._guild()?.id !== expectedGuildId) return;
        this._shop.set(shop?.guildId === expectedGuildId ? shop : null);
      },
      error: (e) => this._error.set(e.message ?? 'Failed to load guild shop'),
    });
  }

  applyToGuild(guildId: string): void {
    this.service.applyToGuild(guildId).subscribe({
      next: () => this.loadMyInvites(),
      error: (e) => this._error.set(e.message ?? 'Failed to apply to guild'),
    });
  }

  invite(payload: InviteToGuild): void {
    this.service.invite(payload).subscribe({
      error: (e) => this._error.set(e.message ?? 'Failed to invite'),
    });
  }

  inviteCharacterByName(payload: InviteToGuild): void {
    this.service.inviteCharacterByName(payload).subscribe({
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
    this._missions.set(
      normalizeGuildMissionOverview(missions, this._guild()?.id ?? null),
    );
  }

  private clearGuildScopedState(): void {
    this._buildings.set(null);
    this._missions.set(null);
    this._shop.set(null);
  }
}
