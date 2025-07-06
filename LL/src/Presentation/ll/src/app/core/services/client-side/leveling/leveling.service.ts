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
    if (!char) return;

    let experience = char.experience + xp;
    let level = char.level;
    let experienceUntilNextLevel = char.experienceUntilNextLevel;

    let leveledUp = false;
    while (experience >= experienceUntilNextLevel) {
      experience -= experienceUntilNextLevel;
      level += 1;
      leveledUp = true;

      // TODO: Update this to a real formula or lookup
      experienceUntilNextLevel = char.experienceUntilNextLevel;
    }

    const updated: CharacterDto = {
      ...char,
      experience,
      level,
      experienceUntilNextLevel,
    };

    this.state.updateCharacter(updated);

    if (leveledUp) {
      // Optional: Sync with backend to avoid discrepancies
      this.state.refresh();
    }
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
