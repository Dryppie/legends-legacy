import { Injectable, signal, computed, effect } from '@angular/core';
import { finalize } from 'rxjs';
import { Guild, GuildSimple } from '../../../../shared/models/Dtos/guild/guild';
import { GuildInvite } from '../../../../shared/models/Dtos/guild/guildInvite';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildService } from './guild.service';
import { BuildingUpgradeView } from '../../../../shared/models/guilds/buildings/buildingUpgradeView';
import { GuildResourceType } from '../../../../shared/models/Dtos/guild/guildResourceType';
import { GameEventService } from '../../real-time/game-event.service';

@Injectable({ providedIn: 'root' })
export class GuildStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _guild = signal<Guild | null>(null);
  private readonly _upgrades = signal<BuildingUpgradeView[]>([]);
  private readonly _invites = signal<GuildInvite[]>([]);
  private readonly _allGuilds = signal<GuildSimple[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _guildNotificationCount = signal(0);
  private guildNotificationsSeen = false;
  private lastGuildBuildingUpgradeEvent: unknown;
  private lastGuildApplicationEvent: unknown;
  private lastGuildInviteEvent: unknown;
  private lastGuildInviteRejectedEvent: unknown;
  private lastGuildApplicationRejectedEvent: unknown;
  private lastGuildStateChangedEvent: unknown;
  private lastGuildMembershipChangedEvent: unknown;
  private lastGuildDisbandedEvent: unknown;
  private lastGuildDirectoryChangedEvent: unknown;

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
    this._guildNotificationCount(),
  );

  constructor(
    private readonly service: GuildService,
    private readonly eventService: GameEventService,
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
        const guildId = this._guild()?.id;
        const invite = this.eventService.event.GuildInviteReceivedMsg();
        const inviteRejected = this.eventService.event.GuildInviteRejectedMsg();
        const applicationRejected =
          this.eventService.event.GuildApplicationRejectedMsg();
        const guildStateChanged =
          this.eventService.event.GuildStateChangedMsg();
        const membershipChanged =
          this.eventService.event.GuildMembershipChangedMsg();
        const guildDisbanded = this.eventService.event.GuildDisbandedMsg();
        const directoryChanged =
          this.eventService.event.GuildDirectoryChangedMsg();

        if (
          directoryChanged &&
          directoryChanged !== this.lastGuildDirectoryChangedEvent
        ) {
          this.lastGuildDirectoryChangedEvent = directoryChanged;
          this.loadAllGuilds();
        }

        if (!guildId && invite && invite !== this.lastGuildInviteEvent) {
          this.lastGuildInviteEvent = invite;
          this.addGuildNotification();
          this.loadMyInvites();
          return;
        }

        if (
          !guildId &&
          inviteRejected &&
          inviteRejected !== this.lastGuildInviteRejectedEvent
        ) {
          this.lastGuildInviteRejectedEvent = inviteRejected;
          this.loadMyInvites();
          return;
        }

        if (
          !guildId &&
          applicationRejected &&
          applicationRejected !== this.lastGuildApplicationRejectedEvent
        ) {
          this.lastGuildApplicationRejectedEvent = applicationRejected;
          this.loadMyInvites();
          return;
        }

        if (
          !guildId &&
          membershipChanged &&
          membershipChanged !== this.lastGuildMembershipChangedEvent
        ) {
          this.lastGuildMembershipChangedEvent = membershipChanged;
          this.refresh();
          return;
        }

        if (!guildId) return;

        const buildingUpgrade = this.eventService.event.GuildBuildingUpgradedMsg();
        const application = this.eventService.event.GuildApplicationMsg();

        if (
          buildingUpgrade &&
          buildingUpgrade !== this.lastGuildBuildingUpgradeEvent &&
          buildingUpgrade.guildId === guildId
        ) {
          this.lastGuildBuildingUpgradeEvent = buildingUpgrade;
          this.refresh();
          return;
        }

        if (
          application &&
          application !== this.lastGuildApplicationEvent &&
          application.guildId === guildId
        ) {
          this.lastGuildApplicationEvent = application;
          this.addGuildNotification();
          this.refresh();
          return;
        }

        if (
          guildStateChanged &&
          guildStateChanged !== this.lastGuildStateChangedEvent &&
          guildStateChanged.guildId === guildId
        ) {
          this.lastGuildStateChangedEvent = guildStateChanged;
          this.refresh();
          return;
        }

        if (
          membershipChanged &&
          membershipChanged !== this.lastGuildMembershipChangedEvent &&
          membershipChanged.guildId === guildId
        ) {
          this.lastGuildMembershipChangedEvent = membershipChanged;
          this.refresh();
          return;
        }

        if (
          guildDisbanded &&
          guildDisbanded !== this.lastGuildDisbandedEvent &&
          guildDisbanded.guildId === guildId
        ) {
          this.lastGuildDisbandedEvent = guildDisbanded;
          this.refresh();
          return;
        }

        if (
          inviteRejected &&
          inviteRejected !== this.lastGuildInviteRejectedEvent &&
          inviteRejected.guildId === guildId
        ) {
          this.lastGuildInviteRejectedEvent = inviteRejected;
          this.refresh();
          return;
        }

        if (
          applicationRejected &&
          applicationRejected !== this.lastGuildApplicationRejectedEvent &&
          applicationRejected.guildId === guildId
        ) {
          this.lastGuildApplicationRejectedEvent = applicationRejected;
          this.refresh();
        }
      },
      { allowSignalWrites: true },
    );
  }

  /* ─────────── high-level commands ─────────── */

  /** Call once at login or when character changes */
  refresh(): void {
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
          const currentGuild = this._guild();
          if (!currentGuild) return;

          const updatedResources = [...currentGuild.resources];

          for (const donation of donations) {
            const existing = updatedResources.find(
              (r) => r.resource === donation.type,
            );
            if (existing) {
              existing.amount += donation.amount;
            } else {
              updatedResources.push({
                resource: donation.type,
                amount: donation.amount,
              });
            }
          }

          this._guild.set({ ...currentGuild, resources: updatedResources });
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
      next: () => {
        const pending: GuildInvite = {
          guildId,
          guildName: '',
          characterId: '',
          characterName: '',
          isInvite: false,
        };
        this._invites.set([...this._invites(), pending]);
      },
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
      next: () =>
        this._invites.set(this._invites().filter((i) => i.guildId !== guildId)),
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
      next: () => {
        const g = this._guild();
        if (!g) return;

        g.invites = g.invites.filter((i) => i.characterId !== characterId);
        this._guild.set({ ...g }); // trigger change
      },
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
    this.guildNotificationsSeen = true;
    this._guildNotificationCount.set(0);
  }

  private addGuildNotification(): void {
    this.guildNotificationsSeen = false;
    this._guildNotificationCount.update((count) => count + 1);
  }

  private initializeGuildNotificationCount(count: number): void {
    if (this.guildNotificationsSeen || this._guildNotificationCount() > 0)
      return;

    this._guildNotificationCount.set(count);
  }
}
