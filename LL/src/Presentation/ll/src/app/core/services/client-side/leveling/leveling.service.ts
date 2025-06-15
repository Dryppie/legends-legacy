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
    const char = this.state.currentCharacter(); // sync read
    if (!char) return;

    let { level, experience, experienceUntilNextLevel } = char;

    let newExp = experience + xp;
    let newLevel = level;

    if (newExp >= experienceUntilNextLevel) {
      newExp -= experienceUntilNextLevel;
      newLevel += 1;
      /* If you have a formula/table for the *next* threshold,
         update `experienceUntilNextLevel` here as well. */
    }

    const updated: CharacterDto = {
      ...char,
      experience: newExp,
      level: newLevel,
    };

    /* single call updates the global store; every component that
       depends on `currentCharacter` will react automatically      */
    this.state.updateCharacter(updated);
  }

  /* ────────────────────────────────────────────────────────
   *  PROFESSION XP / LEVEL
   * ────────────────────────────────────────────────────────*/
  gainProfessionExperience(type: ProfessionType, xp: number): void {
    const prof = this.professionService.getProfession(type);
    if (!prof) return;

    prof.experience += xp;

    let leveledUp = false;
    while (prof.experience >= prof.experienceUntilNextLevel) {
      prof.experience -= prof.experienceUntilNextLevel;
      prof.level += 1;
      leveledUp = true;
    }

    this.professionService.emitUpdate(); // toast / signal to UI

    if (leveledUp) {
      /* optional round-trip to backend to make sure numbers are canonical */
      this.professionService.refresh();
    }
  }
}
