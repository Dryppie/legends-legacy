import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';
import { CurrentDungeonComponent } from '../../../shared/components/current-dungeon/current-dungeon.component';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';

@Component({
    selector: 'app-game-header',
    imports: [NgIf, CurrentDungeonComponent, NumberFormatPipe, ShortNumberPipe],
    templateUrl: './game-header.component.html'
})
export class GameHeaderComponent {
  readonly currentCharacter;
  useShortFormat: boolean;

  constructor(
    characterState: CharacterStateService,
    private readonly storage: LocalStorageService,
  ) {
    this.currentCharacter = characterState.currentCharacter;
    this.useShortFormat = this.storage.get<boolean>('useShortFormat') ?? false;
  }

  toggleFormat(): void {
    this.useShortFormat = !this.useShortFormat;
    this.storage.set('useShortFormat', this.useShortFormat);
  }
}
