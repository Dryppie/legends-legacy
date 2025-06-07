import { Component, OnInit } from '@angular/core';
import { GuildSimple } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { GuildInvite } from '../../../../../shared/models/Dtos/guild/guildInvite';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';

@Component({
  selector: 'app-no-guild',
  standalone: true,
  imports: [NgIf, NgFor, FormsModule, AsyncPipe],
  templateUrl: './no-guild.component.html',
})
export class NoGuildComponent implements OnInit {
  guilds$!: Observable<GuildSimple[]>;
  guildInvites!: GuildInvite[];
  guildApplications!: GuildInvite[];
  subscription: Subscription = new Subscription();
  showModal = false;
  guildName = '';

  constructor(private guildService: GuildService) {}

  ngOnInit(): void {
    this.guilds$ = this.guildService.allGuilds$;
    this.subscription.add(
      this.guildService.invites$.subscribe((invites) => {
        this.guildInvites = invites.filter((i) => i.isInvite);
        this.guildApplications = invites.filter((i) => !i.isInvite);
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  isGuildAppliedTo(guildId: string): boolean {
    return this.guildApplications.some((invite) => invite.guildId === guildId);
  }

  acceptInvite(guildId: string) {
    this.guildService.acceptInvite(guildId);
  }

  rejectInvite(guildId: string) {
    this.guildService.rejectInvite(guildId);
  }

  applyToGuild(guildId: string) {
    this.guildService.applyToGuild(guildId);
  }

  create() {
    if (this.guildName.trim()) {
      this.guildService.create(this.guildName);

      this.closeModal();
    }
  }

  openModal() {
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.guildName = '';
  }
}
