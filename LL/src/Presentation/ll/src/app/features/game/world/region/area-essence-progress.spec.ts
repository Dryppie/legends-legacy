import { SoulArchiveDto } from '../../../../shared/models/essence-system';
import { Area } from '../../../../shared/models/Dtos/regionDto';
import { TRAINING_GROUNDS_AREA_ID } from '../../../../shared/models/quest';
import { calculateAreaEssenceProgress } from './area-essence-progress';

describe('calculateAreaEssenceProgress', () => {
  const area: Area = {
    id: 'region_01_area_01',
    name: 'Lumo Ruins',
    levelRequirement: 1,
    creatures: ['Lumo Wisp', 'Goblin Archer', 'Goblin Warrior'],
  };

  it('counts distinct archived creature essences in the area', () => {
    const archive = {
      essences: [
        { essenceDefinitionId: 'essence.lumo_wisp' },
        { essenceDefinitionId: 'ESSENCE.GOBLIN_ARCHER' },
        { essenceDefinitionId: 'essence.from_another_area' },
      ],
      essenceDust: 0,
    } as SoulArchiveDto;

    expect(calculateAreaEssenceProgress(area, archive)).toEqual({
      collected: 2,
      total: 3,
    });
  });

  it('reports completion when every configured creature essence is archived', () => {
    const archive = {
      essences: area.creatures.map((creature) => ({
        essenceDefinitionId: `essence.${creature.toLowerCase().replace(/ /g, '_')}`,
      })),
      essenceDust: 0,
    } as SoulArchiveDto;

    expect(calculateAreaEssenceProgress(area, archive)).toEqual({
      collected: 3,
      total: 3,
    });
  });

  it('does not display collection progress before the archive loads', () => {
    expect(calculateAreaEssenceProgress(area, null)).toBeUndefined();
  });

  it('does not treat the quest-only training area as an Essence collection', () => {
    expect(
      calculateAreaEssenceProgress(
        { ...area, id: TRAINING_GROUNDS_AREA_ID },
        { essences: [], essenceDust: 0 },
      ),
    ).toBeUndefined();
  });
});
