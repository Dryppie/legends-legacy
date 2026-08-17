import {
  EssenceDefinitionDto,
  PlayerEssenceDto,
} from '../models/essence-system';
import {
  essenceDefinitionSearchText,
  playerEssenceSearchText,
} from './essence-search';

function createDefinition(): EssenceDefinitionDto {
  return {
    id: 'essence.thornback_boar',
    sourceMonsterId: 'monster.thornback_boar',
    name: 'Thornback Boar Essence',
    variantName: 'Bristling',
    displayName: 'Bristling Thornback Boar Essence',
    description: 'Retaliates against attackers with a spray of quills.',
    rarity: 'Rare',
    tagsByCategory: { Mechanic: ['Mechanic.Retaliation'] },
    attributeBonuses: [
      {
        attribute: 'Endurance',
        modifierKind: 'Flat',
        baseValue: 3,
        currentValue: 4,
      },
    ],
    activeAbility: {
      id: 'ability.thorned_rush',
      kind: 'Active',
      name: 'Thorned Rush',
      description: 'Charges forward, impaling the first enemy hit.',
      cooldownSeconds: 12,
      targeting: 'Enemy',
      tags: ['Physical'],
      effects: [
        {
          id: 'effect.bleed',
          type: 'ApplyStatus',
          target: 'Enemy',
          currentValue: 5,
          status: 'Bleeding',
          nestedEffects: [
            {
              id: 'effect.bleed.tick',
              type: 'DamageOverTime',
              target: 'Enemy',
              currentValue: 2,
              attribute: 'Might',
            },
          ],
        },
      ],
    },
    passiveAbility: {
      id: 'ability.bristling_hide',
      kind: 'Passive',
      name: 'Bristling Hide',
      description: 'Reflects a portion of melee damage taken.',
      cooldownSeconds: 0,
      targeting: 'Self',
      tags: ['Buff'],
      effects: [],
    },
    evolution: {
      id: 'evolution.impaler',
      name: 'Impaler',
      description: 'Quills pierce armour outright.',
      requiredAscensionTier: 3,
      requiredCatalystItemId: 'item.quill',
      addsTags: ['Mechanic.Execute'],
    },
  };
}

describe('essenceDefinitionSearchText', () => {
  it('matches on name, variant, description, rarity, and every tag source', () => {
    const searchable = essenceDefinitionSearchText(createDefinition());

    expect(searchable).toContain('bristling');
    expect(searchable).toContain('spray of quills');
    expect(searchable).toContain('rare');
    expect(searchable).toContain('mechanic.retaliation');
    expect(searchable).toContain('physical');
    expect(searchable).toContain('buff');
    expect(searchable).toContain('mechanic.execute');
  });

  it('matches on ability descriptions and effect details', () => {
    const searchable = essenceDefinitionSearchText(createDefinition());

    expect(searchable).toContain('impaling the first enemy');
    expect(searchable).toContain('reflects a portion of melee damage');
    expect(searchable).toContain('bleeding');
    expect(searchable).toContain('damageovertime');
  });

  it('tolerates a missing definition', () => {
    expect(essenceDefinitionSearchText(null)).toBe('');
    expect(essenceDefinitionSearchText(undefined)).toBe('');
  });
});

describe('playerEssenceSearchText', () => {
  const playerEssence = {
    id: 'player-essence-1',
    essenceDefinitionId: 'essence.thornback_boar',
    name: 'Thornback Boar Essence',
    tags: ['Role.Defensive'],
    activeAbility: {
      name: 'Thorned Rush',
      description: 'Charges forward, impaling the first enemy hit.',
      tags: ['Physical'],
      effects: [],
    },
    passiveAbility: {
      name: 'Bristling Hide',
      description: 'Reflects a portion of melee damage taken.',
      tags: ['Buff'],
      effects: [],
    },
    evolveInfo: {
      name: 'Impaler',
      description: 'Quills pierce armour outright.',
    },
  } as unknown as PlayerEssenceDto;

  it('matches on the owned Essence name, tags, and ability descriptions', () => {
    const searchable = playerEssenceSearchText(playerEssence);

    expect(searchable).toContain('thornback boar essence');
    expect(searchable).toContain('role.defensive');
    expect(searchable).toContain('impaling the first enemy');
    expect(searchable).toContain('quills pierce armour');
  });

  it('folds in the shared definition when one is supplied', () => {
    const searchable = playerEssenceSearchText(
      playerEssence,
      createDefinition(),
    );

    expect(searchable).toContain('spray of quills');
    expect(searchable).toContain('mechanic.retaliation');
    expect(searchable).toContain('rare');
  });
});
