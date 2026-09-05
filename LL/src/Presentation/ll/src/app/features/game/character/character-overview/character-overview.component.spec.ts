import { signal } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
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
  return new CharacterOverviewComponent(characterService,
{
      overview: signal(createOverview()).asReadonly(),
      currentCharacter: signal(createCharacter()).asReadonly(),
      loading: signal(false).asReadonly(),
      error: signal(null).asReadonly(),
      refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
    } as unknown as CharacterStateService,
{
      journal: signal({ quests: [] }).asReadonly(),
    } as unknown as QuestStateService,
{
      snapshot: { queryParamMap: convertToParamMap({}) },
      queryParamMap: of(convertToParamMap({})),
    } as ActivatedRoute,
{ navigate: jasmine.createSpy('navigate') } as unknown as Router
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
    baseAttributes: [],
    baseCombatAttributes: [],
    isOnline: true,
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
