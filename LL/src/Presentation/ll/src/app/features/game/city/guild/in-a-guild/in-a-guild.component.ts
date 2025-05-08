import { Component, Input, OnInit } from '@angular/core';
import { Tab } from '../../../../../shared/models/sidebar-item';
import { TabComponent } from '../../../../../shared/components/tab/tab.component';
import { NgSwitch, NgSwitchCase } from '@angular/common';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { InviteToGuild } from '../../../../../shared/models/requestDtos/guilds/inviteToGuild';

@Component({
  selector: 'app-in-a-guild',
  standalone: true,
  imports: [TabComponent, NgSwitch, NgSwitchCase, GuildInfoComponent],
  templateUrl: './in-a-guild.component.html',
  styleUrl: './in-a-guild.component.css',
})
export class InAGuildComponent implements OnInit {
  @Input() guild!: Guild;

  constructor(private guildService: GuildService) {}

  ngOnInit(): void {
    this.setActiveTab(this.tabs[0]?.label || '');
  }

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

  tabs: Tab[] = [
    {
      label: 'Info',
      items: [],
    },
    {
      label: 'Buildings',
      items: [],
    },
    {
      label: 'Armory',
      items: [],
    },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
