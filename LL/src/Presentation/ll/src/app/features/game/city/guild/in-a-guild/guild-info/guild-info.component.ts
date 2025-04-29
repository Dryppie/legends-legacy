import { Component, Input, OnInit } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { CharacterDto } from '../../../../../../shared/models/Dtos/characterDto';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-guild-info',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './guild-info.component.html',
  styleUrl: './guild-info.component.css',
})
export class GuildInfoComponent implements OnInit {
  @Input() guild!: Guild;
  character$!: Observable<CharacterDto | null>;
  constructor(private characterService: CharacterService) {}

  ngOnInit(): void {
    this.character$ = this.characterService.getCurrentCharacter();
  }
}
