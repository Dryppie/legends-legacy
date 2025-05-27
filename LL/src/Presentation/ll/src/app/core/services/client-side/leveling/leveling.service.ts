import { Injectable } from '@angular/core';
import { CharacterService } from '../../api/character/character.service';
import { take } from 'rxjs';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ProfessionsService } from '../../api/professions/professions.service';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';

@Injectable({
  providedIn: 'root',
})
export class LevelingService {
  constructor(
    private characterService: CharacterService,
    private professionService: ProfessionsService,
  ) {}

  gainExperience(experience: number): void {
    this.characterService
      .getCurrentCharacter()
      .pipe(take(1))
      .subscribe((character) => {
        if (!character) return;
        let currentLevel = character.level;
        let newExperience = character.experience + experience;
        let newLevel = currentLevel;
        let requiredExp = character.experienceUntilNextLevel;

        if (newExperience >= requiredExp) {
          newExperience -= requiredExp;
          newLevel++;
        }

        const updatedCharacter: CharacterDto = {
          ...character,
          experience: newExperience,
          level: newLevel,
        };

        this.characterService.updateCharacter(updatedCharacter);
      });
  }

  gainProfessionExperience(
    professionType: ProfessionType,
    experience: number,
  ): void {
    const profession = this.professionService.getProfession(professionType);
    if (!profession) return;

    profession.experience += experience;

    let leveledUp = false;
    while (profession.experience >= profession.experienceUntilNextLevel) {
      profession.experience -= profession.experienceUntilNextLevel;
      profession.level++;
      leveledUp = true;
    }

    this.professionService.emitUpdate();

    if (leveledUp) {
      this.professionService.refresh(); // sync from backend
    }
  }
}
