import { Injectable } from '@angular/core';
import { CharacterService } from '../../api/character/character.service';
import { take } from 'rxjs';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';

@Injectable({
  providedIn: 'root',
})
export class LevelingService {
  constructor(private characterService: CharacterService) {}

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
}
