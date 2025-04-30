import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { Subscription } from 'rxjs';
import { GuildRole } from '../../../../../../shared/models/Dtos/guild/guildRole';

@Component({
  selector: 'app-guild-info',
  standalone: true,
  imports: [NgFor, NgIf, NgClass],
  templateUrl: './guild-info.component.html',
  styleUrl: './guild-info.component.css',
})
export class GuildInfoComponent implements OnInit {
  @Input() guild!: Guild;

  @Output() inviteEvent = new EventEmitter<string>();

  id!: string;
  leaderRole: GuildRole = GuildRole.Leader;
  subscriptions: Subscription = new Subscription();
  constructor(private characterService: CharacterService) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.characterService.getCurrentCharacter().subscribe((character) => {
        if (character) this.id = character.id;
      }),
    );
  }

  invite() {
    this.inviteEvent.emit('');
  }
}
