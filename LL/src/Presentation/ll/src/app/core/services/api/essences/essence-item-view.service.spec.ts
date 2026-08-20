import { EssenceDefinitionDto } from '../../../../shared/models/essence-system';
import { EssenceItemViewService } from './essence-item-view.service';

describe('EssenceItemViewService', () => {
  it('preserves the server threat value and multiplier for shared essence details', () => {
    const service = new EssenceItemViewService();
    const definition: EssenceDefinitionDto = {
      id: 'essence.test',
      sourceMonsterId: 'monster.test',
      name: 'Test Essence',
      variantName: 'Test',
      displayName: 'Test Essence',
      description: 'Test essence.',
      rarity: 'Common',
      tagsByCategory: {},
      attributeBonuses: [],
      activeAbility: ability('Active', 256, 1.5),
      passiveAbility: ability('Passive', 20, 0.5),
      evolution: {
        id: 'evolution.test',
        name: 'Test Evolution',
        description: 'Test evolution.',
        requiredAscensionTier: 1,
        requiredCatalystItemId: '',
        addsTags: [],
      },
    };

    const result = service.fromDefinition(definition);

    expect(result.active.threatValue).toBe(256);
    expect(result.active.threatMultiplier).toBe(1.5);
    expect(result.passive.threatValue).toBe(20);
    expect(result.passive.threatMultiplier).toBe(0.5);
  });
});

function ability(
  kind: 'Active' | 'Passive',
  threatValue: number,
  threatMultiplier: number,
) {
  return {
    id: `ability.${kind.toLowerCase()}`,
    kind,
    name: `${kind} Ability`,
    description: 'Test ability.',
    cooldownSeconds: kind === 'Active' ? 10 : 0,
    threatValue,
    threatMultiplier,
    targets: [],
    tags: [],
    effects: [],
  };
}
