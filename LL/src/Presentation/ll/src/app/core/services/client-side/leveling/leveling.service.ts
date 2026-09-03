import { Injectable } from '@angular/core';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { CharacterStateService } from '../../api/character/character-state.service';

@Injectable({
  providedIn: 'root',
})
export class LevelingService {
  constructor(
    private readonly state: CharacterStateService,
  ) {}

  /* ────────────────────────────────────────────────────────
   *  CHARACTER XP / LEVEL
   * ────────────────────────────────────────────────────────*/
  gainExperience(xp: number): void {
    const char = this.state.currentCharacter();
    if (!char || xp <= 0) return;

    const experience = char.experience + xp;
    const experienceUntilNextLevel = char.experienceUntilNextLevel;

    if (experienceUntilNextLevel <= 0) {
      this.state.refreshCurrentCharacter();
      return;
    }

    if (experience >= experienceUntilNextLevel) {
      this.state.refreshCurrentCharacter();
      return;
    }

    const updated: CharacterDto = {
      ...char,
      experience,
    };

    this.state.updateCharacter(updated);
  }

}
