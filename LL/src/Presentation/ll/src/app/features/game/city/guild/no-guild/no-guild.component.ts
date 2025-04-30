import { Component, OnInit } from '@angular/core';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { NgFor, NgIf } from '@angular/common';
import { GuildInvite } from '../../../../../shared/models/Dtos/guild/guildInvite';

@Component({
  selector: 'app-no-guild',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './no-guild.component.html',
  styleUrl: './no-guild.component.css',
})
export class NoGuildComponent implements OnInit {
  guilds: Guild[] = [];
  guildInvites: GuildInvite[] = [];

  constructor(private guildService: GuildService) {}
  ngOnInit(): void {
    this.guildService.getAll().subscribe((guilds) => {
      this.guilds = guilds;
    });
    this.guildService.getMyInvites().subscribe((guildInvites) => {
      console.log(guildInvites);
      this.guildInvites = guildInvites;
    });
  }

  acceptInvite(guildId: string) {
    this.guildService.acceptInvite(guildId).subscribe();
  }

  openCreateGuildModal() {
    this.guildService.create('Testing').subscribe();
  }
}
