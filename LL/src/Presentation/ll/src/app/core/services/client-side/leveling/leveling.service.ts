import { Injectable } from '@angular/core';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ProfessionsService } from '../../api/professions/professions.service';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';
import { CharacterStateService } from '../../api/character/character-state.service';

@Injectable({
  providedIn: 'root',
})
export class LevelingService {
  constructor(
    private readonly state: CharacterStateService,
    private readonly professionService: ProfessionsService,
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

  /* ────────────────────────────────────────────────────────
   *  PROFESSION XP / LEVEL
   * ────────────────────────────────────────────────────────*/
  gainProfessionExperience(type: ProfessionType, xp: number): void {
    this.professionService.addExperience(type, xp);
  }
}
