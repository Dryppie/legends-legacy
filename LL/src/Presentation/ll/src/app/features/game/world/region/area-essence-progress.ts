import { SoulArchiveDto } from '../../../../shared/models/essence-system';
import {
  Area,
  AreaEssenceProgress,
} from '../../../../shared/models/Dtos/regionDto';
import { TRAINING_GROUNDS_AREA_ID } from '../../../../shared/models/quest';

export function calculateAreaEssenceProgress(
  area: Area,
  archive: SoulArchiveDto | null,
): AreaEssenceProgress | undefined {
  if (
    archive === null ||
    area.id === TRAINING_GROUNDS_AREA_ID ||
    area.creatures.length === 0
  ) {
    return undefined;
  }

  const creatureEssenceIds = Array.from(
    new Set(area.creatures.map((creature) => essenceIdForCreature(creature))),
  );
  const archivedEssenceIds = new Set(
    archive.essences.map((essence) => essence.essenceDefinitionId.toLowerCase()),
  );

  return {
    collected: creatureEssenceIds.filter((id) => archivedEssenceIds.has(id))
      .length,
    total: creatureEssenceIds.length,
  };
}

function essenceIdForCreature(creatureName: string): string {
  return `essence.${creatureName
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')}`;
}
