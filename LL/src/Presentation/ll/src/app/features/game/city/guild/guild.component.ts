import { Component, OnInit } from '@angular/core';
import { NoGuildComponent } from './no-guild/no-guild.component';
import { InAGuildComponent } from './in-a-guild/in-a-guild.component';
import { NgIf } from '@angular/common';
import { GuildStateService } from '../../../../core/services/api/guild/guild-state.service';

@Component({
  selector: 'app-guild',
  standalone: true,
  imports: [NoGuildComponent, InAGuildComponent, NgIf],
  templateUrl: './guild.component.html',
})
export class GuildComponent implements OnInit {
  readonly guild;

  constructor(private state: GuildStateService) {
    this.guild = this.state.guild;
  }
  ngOnInit(): void {
    this.state.refresh();
  }
}
