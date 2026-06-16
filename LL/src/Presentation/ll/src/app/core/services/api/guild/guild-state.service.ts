import { Injectable, signal, computed, effect } from '@angular/core';
import { finalize } from 'rxjs';
import { Guild, GuildSimple } from '../../../../shared/models/Dtos/guild/guild';
import { GuildInvite } from '../../../../shared/models/Dtos/guild/guildInvite';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildService } from './guild.service';
import { BuildingUpgradeView } from '../../../../shared/models/guilds/buildings/buildingUpgradeView';
import { GuildResourceType } from '../../../../shared/models/Dtos/guild/guildResourceType';
import { GameEventService } from '../../real-time/game-event.service';
import { AuthService } from '../auth/auth.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
  SIDEBAR_NOTIFICATION,
} from '../../client-side/notifications/notification.service';

@Injectable({ providedIn: 'root' })
export class GuildStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _guild = signal<Guild | null>(null);
  private readonly _upgrades = signal<BuildingUpgradeView[]>([]);
  private readonly _invites = signal<GuildInvite[]>([]);
  private readonly _allGuilds = signal<GuildSimple[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private lastGuildBuildingUpgradeUpdateId: string | null = null;
  private lastGuildApplicationUpdateId: string | null = null;
  private lastGuildInviteUpdateId: string | null = null;
  private lastGuildInviteRejectedUpdateId: string | null = null;
  private lastGuildApplicationRejectedUpdateId: string | null = null;
  private lastGuildStateChangedUpdateId: string | null = null;
  private lastGuildMembershipChangedUpdateId: string | null = null;
  private lastGuildDisbandedUpdateId: string | null = null;
  private lastGuildDirectoryChangedUpdateId: string | null = null;
  private hasLoaded = false;

  /* ─────────── public, read-only selectors ─────────── */
  readonly guild = computed(() => this._guild());
  readonly upgrades = computed(() => this._upgrades());
  readonly invites = computed(() => this._invites());
  readonly allGuilds = computed(() => this._allGuilds());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isInGuild = computed(() => !!this._guild());
  readonly hasInvites = computed(() => this._invites().length > 0);
  readonly guildNotificationCount = computed(() =>
    this.notificationService.count(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Guild,
    ),
  );

  constructor(
    private readonly service: GuildService,
    private readonly eventService: GameEventService,
    private readonly inventoryState: InventoryStateService,
    private readonly auth: AuthService,
    private readonly notificationService: NotificationService,
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
        const guildId = this._guild()?.id;
        const inviteEnvelope =
          this.eventService.eventEnvelope.GuildInviteReceivedMsg();
        const inviteRejectedEnvelope =
          this.eventService.eventEnvelope.GuildInviteRejectedMsg();
        const applicationRejectedEnvelope =
          this.eventService.eventEnvelope.GuildApplicationRejectedMsg();
        const guildStateChangedEnvelope =
          this.eventService.eventEnvelope.GuildStateChangedMsg();
        const membershipChangedEnvelope =
          this.eventService.eventEnvelope.GuildMembershipChangedMsg();
        const guildDisbandedEnvelope =
          this.eventService.eventEnvelope.GuildDisbandedMsg();
        const directoryChangedEnvelope =
          this.eventService.eventEnvelope.GuildDirectoryChangedMsg();
        const invite = inviteEnvelope?.payload;
        const inviteRejected = inviteRejectedEnvelope?.payload;
        const applicationRejected = applicationRejectedEnvelope?.payload;
        const guildStateChanged = guildStateChangedEnvelope?.payload;
        const membershipChanged = membershipChangedEnvelope?.payload;
        const guildDisbanded = guildDisbandedEnvelope?.payload;
        const directoryChanged = directoryChangedEnvelope?.payload;
        let shouldRefresh = false;

        if (
          directoryChanged &&
          this.shouldProcessEvent(
            directoryChangedEnvelope,
            this.lastGuildDirectoryChangedUpdateId,
          )
        ) {
          this.lastGuildDirectoryChangedUpdateId = this.getEventId(
            directoryChangedEnvelope,
          );
          this.loadAllGuilds();
        }

        if (
          !guildId &&
          invite &&
          this.shouldProcessEvent(inviteEnvelope, this.lastGuildInviteUpdateId)
        ) {
          this.lastGuildInviteUpdateId = this.getEventId(inviteEnvelope);
          this.addGuildNotification();
          this.loadMyInvites();
        }

        if (
          !guildId &&
          inviteRejected &&
          this.shouldProcessEvent(
            inviteRejectedEnvelope,
            this.lastGuildInviteRejectedUpdateId,
          )
        ) {
          this.lastGuildInviteRejectedUpdateId = this.getEventId(
            inviteRejectedEnvelope,
          );
          this.loadMyInvites();
        }

        if (
          !guildId &&
          applicationRejected &&
          this.shouldProcessEvent(
            applicationRejectedEnvelope,
            this.lastGuildApplicationRejectedUpdateId,
          )
        ) {
          this.lastGuildApplicationRejectedUpdateId = this.getEventId(
            applicationRejectedEnvelope,
          );
          this.loadMyInvites();
        }

        if (
          !guildId &&
          membershipChanged &&
          this.shouldProcessEvent(
            membershipChangedEnvelope,
            this.lastGuildMembershipChangedUpdateId,
          )
        ) {
          this.lastGuildMembershipChangedUpdateId = this.getEventId(
            membershipChangedEnvelope,
          );
          shouldRefresh = true;
        }

        if (!guildId) {
          if (shouldRefresh) this.refresh();
          return;
        }

        const buildingUpgradeEnvelope =
          this.eventService.eventEnvelope.GuildBuildingUpgradedMsg();
        const applicationEnvelope =
          this.eventService.eventEnvelope.GuildApplicationMsg();
        const buildingUpgrade = buildingUpgradeEnvelope?.payload;
        const application = applicationEnvelope?.payload;

        if (
          buildingUpgrade &&
          this.shouldProcessEvent(
            buildingUpgradeEnvelope,
            this.lastGuildBuildingUpgradeUpdateId,
          ) &&
          buildingUpgrade.guildId === guildId
        ) {
          this.lastGuildBuildingUpgradeUpdateId = this.getEventId(
            buildingUpgradeEnvelope,
          );
          shouldRefresh = true;
        }

        if (
          application &&
          this.shouldProcessEvent(
            applicationEnvelope,
            this.lastGuildApplicationUpdateId,
          ) &&
          application.guildId === guildId
        ) {
          this.lastGuildApplicationUpdateId =
            this.getEventId(applicationEnvelope);
          this.addGuildNotification();
          shouldRefresh = true;
        }

        if (
          guildStateChanged &&
          this.shouldProcessEvent(
            guildStateChangedEnvelope,
            this.lastGuildStateChangedUpdateId,
          ) &&
          guildStateChanged.guildId === guildId
        ) {
          this.lastGuildStateChangedUpdateId = this.getEventId(
            guildStateChangedEnvelope,
          );
          shouldRefresh = true;
        }

        if (
          membershipChanged &&
          this.shouldProcessEvent(
            membershipChangedEnvelope,
            this.lastGuildMembershipChangedUpdateId,
          ) &&
          membershipChanged.guildId === guildId
        ) {
          this.lastGuildMembershipChangedUpdateId = this.getEventId(
            membershipChangedEnvelope,
          );
          shouldRefresh = true;
        }

        if (
          guildDisbanded &&
          this.shouldProcessEvent(
            guildDisbandedEnvelope,
            this.lastGuildDisbandedUpdateId,
          ) &&
          guildDisbanded.guildId === guildId
        ) {
          this.lastGuildDisbandedUpdateId =
            this.getEventId(guildDisbandedEnvelope);
          shouldRefresh = true;
        }

        if (
          inviteRejected &&
          this.shouldProcessEvent(
            inviteRejectedEnvelope,
            this.lastGuildInviteRejectedUpdateId,
          ) &&
          inviteRejected.guildId === guildId
        ) {
          this.lastGuildInviteRejectedUpdateId = this.getEventId(
            inviteRejectedEnvelope,
          );
          shouldRefresh = true;
        }

        if (
          applicationRejected &&
          this.shouldProcessEvent(
            applicationRejectedEnvelope,
            this.lastGuildApplicationRejectedUpdateId,
          ) &&
          applicationRejected.guildId === guildId
        ) {
          this.lastGuildApplicationRejectedUpdateId = this.getEventId(
            applicationRejectedEnvelope,
          );
          shouldRefresh = true;
        }

        if (shouldRefresh) this.refresh();
      },
      { allowSignalWrites: true },
    );
  }

  /* ─────────── high-level commands ─────────── */

  /** Call once at login or when character changes */
  refresh(): void {
    this.hasLoaded = true;
    this._loading.set(true);

    this.service
      .getMyGuild()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (guild) => {
          if (guild) {
            this._guild.set(guild);
            this.initializeGuildNotificationCount(
              guild.invites.filter((invite: GuildInvite) => !invite.isInvite)
                .length,
            );
            this._invites.set([]);
            this._allGuilds.set([]);
            this.loadGuildUpgrades();
            this.loadAllGuilds();
          } else {
            this._guild.set(null);
            this.loadAllGuilds();
            this.loadMyInvites();
          }
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
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
          this.refresh();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to disband guild'),
      });
  }

  donate(donations: { type: GuildResourceType; amount: number }[]) {
    this._loading.set(true);

    this.service
      .donate(donations)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this.refresh();
          this.inventoryState.load(true);
          this.auth.refreshCurrentCharacter();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to donate to guild'),
      });
  }

  upgradeGuildBuilding(upgrade: BuildingUpgradeView) {
    this._loading.set(true);

    this.service
      .upgradeGuildBuilding(upgrade.definition.id)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => this.refresh(),
        error: (e) =>
          this._error.set(e.message ?? 'Failed to upgrade guild building'),
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

  private loadGuildUpgrades(): void {
    this.service.getUpgrades().subscribe({
      next: (inv) => this._upgrades.set(inv),
      error: (e) => this._error.set(e.message ?? 'Failed to load upgrades'),
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

  /* ─────────── optional optimistic helpers ─────────── */
  setGuild(guild: Guild | null) {
    this._guild.set(guild);
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

  private shouldProcessEvent(
    event: { updateId?: string; occurredAt?: string } | null,
    lastUpdateId: string | null,
  ): boolean {
    const updateId = this.getEventId(event);
    return !updateId || updateId !== lastUpdateId;
  }

  private getEventId(
    event: { updateId?: string; occurredAt?: string } | null,
  ): string | null {
    return event?.updateId ?? event?.occurredAt ?? null;
  }
}
