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
    character.soulstones += soulstones;
    this.characterState.updateCharacter(character);
  }

  gainCinders(cinders: number) {
    const character = this.characterState.currentCharacter();
    if (!character) return;
    character.cinders += cinders;
    this.characterState.updateCharacter(character);
  }
}
