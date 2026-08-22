import { signal } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
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
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import {
  CharacterOverviewComponent,
  estimateEssenceThreatPerSecond,
} from './character-overview.component';

describe('CharacterOverviewComponent', () => {
  it('totals nominal threat generation from the attuned Essence loadout', () => {
    expect(
      estimateEssenceThreatPerSecond({
        id: 'loadout-1',
        name: 'Tank',
        autoUseActivities: [],
        slots: [
          {
            slotIndex: 0,
            definition: createEssenceDefinition(4.5, 2),
          },
          {
            slotIndex: 1,
            definition: createEssenceDefinition(3, 1.25),
          },
          { slotIndex: 2 },
        ],
      }),
    ).toBe(10.75);
  });

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
    expect(
      component.attributeSections.find((section) => section.title === 'Utility')
        ?.attributes,
    ).toContain(AttributeType.Threat);

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
    component.ngOnDestroy();
  });

  it('loads character suggestions after typing two characters', fakeAsync(() => {
    const characterService = jasmine.createSpyObj<CharacterService>(
      'CharacterService',
      ['suggestCharacterNames'],
      { currentCharacter: signal(createCharacter()).asReadonly() },
    );
    characterService.suggestCharacterNames.and.returnValue(
      of(['Ember', 'Ember Knight']),
    );
    const component = createComponent(characterService);

    component.onSearchValueChange('em');
    tick(200);

    expect(characterService.suggestCharacterNames).toHaveBeenCalledOnceWith(
      'em',
    );
    expect(component.characterSuggestions()).toEqual(['Ember', 'Ember Knight']);
    expect(component.showCharacterSuggestionPanel()).toBeTrue();
    component.ngOnDestroy();
  }));

  it('fills the overview search from a selected suggestion', () => {
    const component = createComponent();
    const event = jasmine.createSpyObj<Event>('Event', ['preventDefault']);

    component.selectCharacterSuggestion(event, 'Ember Knight');

    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.searchValue()).toBe('Ember Knight');
    expect(component.showCharacterSuggestionPanel()).toBeFalse();
    component.ngOnDestroy();
  });
});

function createComponent(
  characterService: CharacterService = jasmine.createSpyObj<CharacterService>(
    'CharacterService',
    ['suggestCharacterNames'],
    { currentCharacter: signal(createCharacter()).asReadonly() },
  ),
): CharacterOverviewComponent {
  return new CharacterOverviewComponent(
    characterService,
    {
      overview: signal(createOverview()).asReadonly(),
      currentCharacter: signal(createCharacter()).asReadonly(),
      loading: signal(false).asReadonly(),
      error: signal(null).asReadonly(),
      refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
    } as unknown as CharacterStateService,
    {
      getProfession: () => undefined,
    } as unknown as ProfessionsService,
    {
      snapshot: { queryParamMap: convertToParamMap({}) },
      queryParamMap: of(convertToParamMap({})),
    } as ActivatedRoute,
    { navigate: jasmine.createSpy('navigate') } as unknown as Router,
  );
}

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

function createEssenceDefinition(
  activeThreatPerSecond: number,
  passiveThreatPerSecond: number,
) {
  const ability = (
    kind: 'Active' | 'Passive',
    estimatedThreatPerSecond: number,
  ) => ({
    id: `ability.${kind.toLowerCase()}`,
    kind,
    name: `${kind} Ability`,
    description: 'Test ability.',
    cooldownSeconds: kind === 'Active' ? 10 : 0,
    estimatedThreatPerSecond,
    targets: [],
    tags: [],
    effects: [],
  });

  return {
    id: 'essence.test',
    sourceMonsterId: 'monster.test',
    name: 'Test Essence',
    variantName: 'Test',
    displayName: 'Test Essence',
    description: 'Test essence.',
    rarity: 'Common',
    tagsByCategory: {},
    attributeBonuses: [],
    activeAbility: ability('Active', activeThreatPerSecond),
    passiveAbility: ability('Passive', passiveThreatPerSecond),
    evolution: {
      id: 'evolution.test',
      name: 'Test Evolution',
      description: 'Test evolution.',
      requiredAscensionTier: 1,
      requiredCatalystItemId: '',
      addsTags: [],
    },
  };
}
