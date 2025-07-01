import { Injectable, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { Guild, GuildSimple } from '../../../../shared/models/Dtos/guild/guild';
import { GuildInvite } from '../../../../shared/models/Dtos/guild/guildInvite';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildService } from './guild.service';
import { BuildingUpgradeView } from '../../../../shared/models/guilds/buildings/buildingUpgradeView';
import { GuildResourceType } from '../../../../shared/models/Dtos/guild/guildResourceType';

@Injectable({ providedIn: 'root' })
export class GuildStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _guild = signal<Guild | null>(null);
  private readonly _upgrades = signal<BuildingUpgradeView[]>([]);
  private readonly _invites = signal<GuildInvite[]>([]);
  private readonly _allGuilds = signal<GuildSimple[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ─────────── public, read-only selectors ─────────── */
  readonly guild = computed(() => this._guild());
  readonly upgrades = computed(() => this._upgrades());
  readonly invites = computed(() => this._invites());
  readonly allGuilds = computed(() => this._allGuilds());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isInGuild = computed(() => !!this._guild());
  readonly hasInvites = computed(() => this._invites().length > 0);

  constructor(private readonly service: GuildService) {
    this.refresh(); // initial fetch

    /* real-time updates pushed over the socket */
    // effect(() => {
    //   const evt = this.socket.ofType('guild:update')(); // adjust event name/payload
    //   if (!evt) return;

    //   if (evt.guild)   this._guild.set(evt.guild);
    //   if (evt.invites) this._invites.set(evt.invites);
    // });
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
            this._invites.set([]);
            this._allGuilds.set([]);
            this.loadGuildUpgrades();
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
        next: () => {
          const guild = this._guild();

          // Shallow clone to trigger reactivity
          const updatedResources = [...(guild?.resources ?? [])];

          for (const [resourceType, cost] of Object.entries(
            upgrade.nextCost ?? {},
          )) {
            const res = updatedResources.find(
              (r) => r.resource === resourceType,
            );
            if (res) {
              res.amount -= cost;
              if (res.amount < 0) res.amount = 0; // safety
            }
          }

          this._guild.set({
            ...guild!,
            resources: updatedResources,
          });

          // Optimistically bump upgrade level
          upgrade.level += 1;

          // TODO: Recalculate `upgrade.nextCost` if needed, or set to null if max level
          // For now, you may want to just trigger recompute logic
          // this.refreshUpgrades(); // optional, or adjust locally too
        },
        error: (e) => this._error.set(e.message ?? 'Failed to disband guild'),
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
      next: (inv) => this._invites.set(inv),
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
}
