import { Component, OnInit } from '@angular/core';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-no-guild',
  standalone: true,
  imports: [NgIf],
  templateUrl: './no-guild.component.html',
  styleUrl: './no-guild.component.css',
})
export class NoGuildComponent implements OnInit {
  guilds: Guild[] = [];

  constructor(private guildService: GuildService) {}
  ngOnInit(): void {
    this.guildService.getAll().subscribe((guilds) => {
      this.guilds = guilds;
    });
  }

  openCreateGuildModal() {
    throw new Error('Method not implemented.');
  }
}
