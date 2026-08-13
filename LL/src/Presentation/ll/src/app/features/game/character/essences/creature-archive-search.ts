import { CreatureArchiveEntryDto } from '../../../../shared/models/essence-system';

export function creatureArchiveSearchText(
  creature: CreatureArchiveEntryDto,
): string {
  return [
    creature.name,
    creature.creatureId,
    ...creature.essences.flatMap((essence) => [
      essence.name,
      essence.essenceDefinitionId,
      ...(essence.tags ?? []),
      ...Object.values(essence.definition.tagsByCategory ?? {}).flat(),
      essence.definition.activeAbility.name,
      ...(essence.definition.activeAbility.tags ?? []),
      essence.definition.passiveAbility.name,
      ...(essence.definition.passiveAbility.tags ?? []),
      ...(essence.definition.evolution.addsTags ?? []),
    ]),
    ...creature.locations.flatMap((location) => [
      location.regionName,
      location.sourceType,
      location.sourceName,
    ]),
    ...creature.tags,
  ]
    .join(' ')
    .toLowerCase();
}
