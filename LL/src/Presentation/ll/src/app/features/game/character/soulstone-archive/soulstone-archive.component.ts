import { Component, OnInit } from '@angular/core';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { AsyncPipe, NgIf } from '@angular/common';

@Component({
  selector: 'app-soulstone-archive',
  standalone: true,
  imports: [DefaultHeaderComponent, AsyncPipe, NgIf],
  templateUrl: './soulstone-archive.component.html',
  styleUrl: './soulstone-archive.component.css',
})
export class SoulstoneArchiveComponent implements OnInit {
  readonly character$;

  constructor(private readonly characterService: CharacterService) {
    this.character$ = this.characterService.getCurrentCharacter();
  }

  ngOnInit(): void {}
}
