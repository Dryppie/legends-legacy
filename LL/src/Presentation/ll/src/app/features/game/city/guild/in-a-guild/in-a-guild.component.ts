import { Component, Input } from '@angular/core';
import { TabComponent } from '../../../../../shared/components/tabs/tab/tab.component';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { InviteToGuild } from '../../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { TabsComponent } from '../../../../../shared/components/tabs/tabs.component';

@Component({
  selector: 'app-in-a-guild',
  standalone: true,
  imports: [TabComponent, GuildInfoComponent, TabsComponent],
  templateUrl: './in-a-guild.component.html',
})
export class InAGuildComponent {
  @Input() guild!: Guild;

  constructor(private guildService: GuildService) {}

  inviteCharacterByName($event: string) {
    const invite: InviteToGuild = {
      guildId: this.guild.id,
      characterNameOrId: $event,
    };
    this.guildService.inviteCharacterByName(invite);
  }

  leaveGuild() {
    this.guildService.leave();
  }

  disbandGuild() {
    this.guildService.disband();
  }

  rejectApplication($event: string) {
    this.guildService.rejectApplication($event);
  }

  approveApplication($event: string) {
    this.guildService.approveApplication($event);
  }
}
