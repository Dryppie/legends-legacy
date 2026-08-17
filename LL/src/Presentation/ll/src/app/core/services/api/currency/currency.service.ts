import { Injectable } from '@angular/core';
import { CharacterStateService } from '../character/character-state.service';

@Injectable({
  providedIn: 'root',
})
export class CurrencyService {
  constructor(private readonly characterState: CharacterStateService) {}

  gainSoulstones(soulstones: number) {
    const character = this.characterState.currentCharacter();
    if (!character) return;
    this.characterState.updateCharacter({
      ...character,
      soulstones: character.soulstones + soulstones,
    });
  }

  gainCinders(cinders: number) {
    const character = this.characterState.currentCharacter();
    if (!character) return;
    this.characterState.updateCharacter({
      ...character,
      cinders: character.cinders + cinders,
    });
  }
}
