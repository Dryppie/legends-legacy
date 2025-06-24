import { Component, Input } from '@angular/core';
import { TabComponent } from '../../../../../shared/components/tabs/tab/tab.component';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { InviteToGuild } from '../../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { TabsComponent } from '../../../../../shared/components/tabs/tabs.component';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { GuildBuildingsComponent } from './guild-buildings/guild-buildings.component';

@Component({
  selector: 'app-in-a-guild',
  standalone: true,
  imports: [
    TabComponent,
    GuildInfoComponent,
    TabsComponent,
    GuildBuildingsComponent,
  ],
  templateUrl: './in-a-guild.component.html',
})
export class InAGuildComponent {
  @Input() guild!: Guild;

  constructor(private state: GuildStateService) {}

  inviteCharacterByName($event: string) {
    const invite: InviteToGuild = {
      guildId: this.guild.id,
      characterNameOrId: $event,
    };
    this.state.inviteCharacterByName(invite);
  }

  leaveGuild() {
    this.state.leave();
  }

  disbandGuild() {
    this.state.disband();
  }

  rejectApplication($event: string) {
    this.state.rejectApplication($event);
  }

  approveApplication($event: string) {
    this.state.approveApplication($event);
  }
}
