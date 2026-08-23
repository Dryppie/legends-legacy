import { signal } from '@angular/core';
import { CharacterStateService } from '../../api/character/character-state.service';
import { ProfessionsService } from '../../api/professions/professions.service';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { LevelingService } from './leveling.service';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';

describe('LevelingService', () => {
  it('resynchronizes the shared character state after crossing a level boundary', () => {
    const character = signal<CharacterDto>(createCharacter());
    const state = {
      currentCharacter: character.asReadonly(),
      updateCharacter: jasmine.createSpy('updateCharacter'),
      refreshCurrentCharacter: jasmine.createSpy('refreshCurrentCharacter'),
    } as unknown as CharacterStateService;
    const service = new LevelingService(state, {} as ProfessionsService);

    service.gainExperience(15);

    expect(state.refreshCurrentCharacter).toHaveBeenCalled();
    expect(state.updateCharacter).not.toHaveBeenCalled();
  });

  it('keeps using the optimistic current-character update below a level boundary', () => {
    const character = signal<CharacterDto>(createCharacter());
    const state = {
      currentCharacter: character.asReadonly(),
      updateCharacter: jasmine.createSpy('updateCharacter'),
      refreshCurrentCharacter: jasmine.createSpy('refreshCurrentCharacter'),
    } as unknown as CharacterStateService;
    const service = new LevelingService(state, {} as ProfessionsService);

    service.gainExperience(5);

    expect(state.updateCharacter).toHaveBeenCalledWith({
      ...character(),
      experience: 95,
    });
    expect(state.refreshCurrentCharacter).not.toHaveBeenCalled();
  });

  it('delegates profession experience to the canonical profession state service', () => {
    const professionService = jasmine.createSpyObj<ProfessionsService>(
      'ProfessionsService',
      ['addExperience'],
    );
    const service = new LevelingService(
      {} as CharacterStateService,
      professionService,
    );

    service.gainProfessionExperience(ProfessionType.Mining, 125);

    expect(professionService.addExperience).toHaveBeenCalledOnceWith(
      ProfessionType.Mining,
      125,
    );
  });
});

function createCharacter(): CharacterDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 19,
    experience: 90,
    experienceUntilNextLevel: 100,
    cinders: 0,
    soulstones: 0,
    fateEcho: 0,
    sigilFragments: 0,
    guildFavor: 0,
    arenaRating: 0,
  };
}
