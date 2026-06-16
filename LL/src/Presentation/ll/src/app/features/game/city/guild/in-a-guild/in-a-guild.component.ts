import { Component, Input } from '@angular/core';
import { TabComponent } from '../../../../../shared/components/custom-components/tabs/tab/tab.component';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { InviteToGuild } from '../../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { GuildBuildingsComponent } from './guild-buildings/guild-buildings.component';
import { GuildVaultComponent } from './guild-vault/guild-vault.component';
import { GuildRankingsComponent } from './guild-rankings/guild-rankings.component';
import { TabsComponent } from '../../../../../shared/components/custom-components/tabs/tabs.component';
import { NgFor } from '@angular/common';
import { HumanizeEnumPipe } from '../../../../../shared/pipes/enums/humanize-enum.pipe';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-in-a-guild',
  standalone: true,
  imports: [
    NgFor,
    TabComponent,
    GuildInfoComponent,
    TabsComponent,
    GuildBuildingsComponent,
    GuildVaultComponent,
    GuildRankingsComponent,
    HumanizeEnumPipe,
    NumberFormatPipe,
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

  get resourceSummary() {
    return this.guild.resources ?? [];
  }
}
