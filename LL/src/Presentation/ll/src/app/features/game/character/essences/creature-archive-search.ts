import { CreatureArchiveEntryDto } from '../../../../shared/models/essence-system';
import {
  essenceDefinitionSearchTerms,
  toSearchText,
} from '../../../../shared/search/essence-search';

export function creatureArchiveSearchText(
  creature: CreatureArchiveEntryDto,
): string {
  return toSearchText([
    creature.name,
    creature.creatureId,
    ...creature.essences.flatMap((essence) => [
      essence.name,
      essence.essenceDefinitionId,
      ...(essence.tags ?? []),
      ...essenceDefinitionSearchTerms(essence.definition),
    ]),
    ...creature.locations.flatMap((location) => [
      location.regionName,
      location.sourceType,
      location.sourceName,
    ]),
    ...creature.tags,
  ]);
}
