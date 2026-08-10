import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../../shared/models/Dtos/characterProfession';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';
import { CharacterOverviewComponent } from './character-overview.component';

describe('CharacterOverviewComponent', () => {
  it('updates current character and crafting experience from live state', () => {
    const currentCharacter = signal<CharacterDto>(createCharacter());
    const overview = signal<CharacterOverviewDto>(createOverview());
    const craftingProfession = signal<CharacterProfession>(createCrafting());
    const component = new CharacterOverviewComponent(
      {
        currentCharacter: currentCharacter.asReadonly(),
      } as unknown as CharacterService,
      {
        overview: overview.asReadonly(),
        currentCharacter: currentCharacter.asReadonly(),
        loading: signal(false).asReadonly(),
        error: signal(null).asReadonly(),
        refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
      } as unknown as CharacterStateService,
      {
        getProfession: (type: ProfessionType) =>
          type === ProfessionType.Crafting ? craftingProfession() : undefined,
      } as unknown as ProfessionsService,
      {
        snapshot: { queryParamMap: convertToParamMap({}) },
        queryParamMap: of(convertToParamMap({})),
      } as ActivatedRoute,
      { navigate: jasmine.createSpy('navigate') } as unknown as Router,
    );

    expect(component.character()?.experience).toBe(10);
    expect(component.character()?.craftingExperience).toBe(20);

    currentCharacter.update((character) => ({
      ...character,
      experience: 35,
    }));
    craftingProfession.update((profession) => ({
      ...profession,
      experience: 45,
    }));

    expect(component.character()?.experience).toBe(35);
    expect(component.character()?.craftingExperience).toBe(45);
  });
});

function createCharacter(): CharacterDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 5,
    experience: 10,
    experienceUntilNextLevel: 100,
    cinders: 0,
    soulstones: 0,
    fateEcho: 0,
    sigilFragments: 0,
    guildFavor: 0,
    arenaRating: 0,
  };
}

function createOverview(): CharacterOverviewDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 5,
    experience: 5,
    experienceUntilNextLevel: 100,
    craftingLevel: 2,
    craftingExperience: 15,
    craftingExperienceUntilNextLevel: 75,
    baseAttributes: [],
    baseCombatAttributes: [],
    isOnline: true,
  };
}

function createCrafting(): CharacterProfession {
  return {
    professionType: ProfessionType.Crafting,
    level: 2,
    experience: 20,
    experienceUntilNextLevel: 75,
  };
}
