import { CreatureArchiveEntryDto } from '../../../../shared/models/essence-system';
import { creatureArchiveSearchText } from './creature-archive-search';

describe('creatureArchiveSearchText', () => {
  it('includes tags from every Essence and ability tag source', () => {
    const creature = {
      name: 'Thornback Boar',
      creatureId: 'monster.thornback_boar',
      locations: [],
      tags: ['Species.Beast'],
      essences: [
        {
          name: 'Thornback Boar Essence',
          essenceDefinitionId: 'essence.thornback_boar',
          tags: ['Role.Defensive'],
          definition: {
            tagsByCategory: { Mechanic: ['Mechanic.Retaliation'] },
            activeAbility: { name: 'Thorned Rush', tags: ['Physical'] },
            passiveAbility: { name: 'Bristling Hide', tags: ['Buff'] },
            evolution: { addsTags: ['Mechanic.Execute'] },
          },
        },
      ],
    } as unknown as CreatureArchiveEntryDto;

    const searchable = creatureArchiveSearchText(creature);

    expect(searchable).toContain('species.beast');
    expect(searchable).toContain('role.defensive');
    expect(searchable).toContain('mechanic.retaliation');
    expect(searchable).toContain('physical');
    expect(searchable).toContain('buff');
    expect(searchable).toContain('mechanic.execute');
  });
});
