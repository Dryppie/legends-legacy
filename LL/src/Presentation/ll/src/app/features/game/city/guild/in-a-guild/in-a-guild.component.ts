import { Component, Input, OnInit } from '@angular/core';
import { Tab } from '../../../../../shared/models/sidebar-item';
import { TabComponent } from '../../../../../shared/components/tab/tab.component';
import { NgSwitch, NgSwitchCase } from '@angular/common';
import { GuildMembersComponent } from './guild-members/guild-members.component';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';

@Component({
  selector: 'app-in-a-guild',
  standalone: true,
  imports: [TabComponent, NgSwitch, NgSwitchCase, GuildInfoComponent],
  templateUrl: './in-a-guild.component.html',
  styleUrl: './in-a-guild.component.css',
})
export class InAGuildComponent implements OnInit {
  @Input() guild!: Guild;

  ngOnInit(): void {
    this.setActiveTab(this.tabs[0]?.label || '');
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
