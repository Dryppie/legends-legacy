import { Component, OnInit } from '@angular/core';
import { GuildService } from '../../../../core/services/api/guild/guild.service';
import { NoGuildComponent } from './no-guild/no-guild.component';
import { InAGuildComponent } from './in-a-guild/in-a-guild.component';
import { Observable } from 'rxjs';
import { Guild } from '../../../../shared/models/Dtos/guild/guild';
import { AsyncPipe, NgIf } from '@angular/common';

@Component({
  selector: 'app-guild',
  standalone: true,
  imports: [NoGuildComponent, InAGuildComponent, AsyncPipe, NgIf],
  templateUrl: './guild.component.html',
  styleUrl: './guild.component.css',
})
export class GuildComponent implements OnInit {
  guild$!: Observable<Guild>;

  constructor(private guildService: GuildService) {}
  ngOnInit(): void {
    this.guild$ = this.guildService.getMyGuild();
  }
}
