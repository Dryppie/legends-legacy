import { Component, computed, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { DialogFocusDirective } from '../../../../../shared/directives/dialog-focus/dialog-focus.directive';

@Component({
  selector: 'app-no-guild',
  imports: [NgIf, NgFor, FormsModule, DialogFocusDirective],
  templateUrl: './no-guild.component.html',
})
export class NoGuildComponent {
  private readonly minimumGuildNameLength = 3;

  /* ─────────── read data from the state ─────────── */
  readonly guilds;
  readonly guildInvites = computed(() =>
    this.guildState.invites().filter((i) => i.isInvite),
  );
  readonly guildApplications = computed(() =>
    this.guildState.invites().filter((i) => !i.isInvite),
  );

  /* ─────────── local UI state ─────────── */
  showModal = signal(false);
  guildName = signal('');
  readonly isGuildNameValid = computed(
    () => this.guildName().trim().length >= this.minimumGuildNameLength,
  );

  constructor(private readonly guildState: GuildStateService) {
    this.guilds = guildState.allGuilds;
  }

  /* ─────────── helpers for the template ─────────── */
  isGuildAppliedTo = (guildId: string): boolean =>
    this.guildApplications().some((inv) => inv.guildId === guildId);

  /* ─────────── delegating actions to the state ─────────── */
  acceptInvite = (guildId: string) => this.guildState.acceptInvite(guildId);
  rejectInvite = (guildId: string) => this.guildState.rejectInvite(guildId);
  applyToGuild = (guildId: string) => this.guildState.applyToGuild(guildId);

  create(): void {
    const name = this.guildName().trim();
    if (!this.isGuildNameValid()) return;

    this.guildState.create(name);
    this.closeModal();
  }

  openModal = () => this.showModal.set(true);
  closeModal = () => {
    this.showModal.set(false);
    this.guildName.set('');
  };
}
