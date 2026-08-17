import {
  EssenceAbilityDto,
  EssenceDefinitionDto,
  EssenceEffectDto,
  EssenceEvolutionDto,
  PlayerEssenceDto,
} from '../models/essence-system';

/**
 * Shared search-text builders for Essences.
 *
 * Every Essence search field in the client funnels through these helpers so a
 * player can look an Essence up by anything printed on its card: its name and
 * variant, its description, its ability names and descriptions, and every tag
 * attached to the Essence, its abilities, or its evolution.
 */

type Searchable = string | null | undefined;

function collectEffectTerms(
  effects: EssenceEffectDto[] | null | undefined,
  depth = 0,
): Searchable[] {
  if (!effects?.length || depth > 4) return [];

  return effects.flatMap((effect) => [
    effect?.type,
    effect?.target,
    effect?.attribute,
    effect?.status,
    ...collectEffectTerms(effect?.nestedEffects, depth + 1),
  ]);
}

export function essenceAbilitySearchTerms(
  ability: EssenceAbilityDto | null | undefined,
): Searchable[] {
  if (!ability) return [];

  return [
    ability.name,
    ability.description,
    ability.targeting,
    ...(ability.tags ?? []),
    ...collectEffectTerms(ability.effects),
  ];
}

export function essenceEvolutionSearchTerms(
  evolution: EssenceEvolutionDto | null | undefined,
): Searchable[] {
  if (!evolution) return [];

  return [
    evolution.name,
    evolution.description,
    ...(evolution.addsTags ?? []),
  ];
}

export function essenceDefinitionSearchTerms(
  definition: EssenceDefinitionDto | null | undefined,
): Searchable[] {
  if (!definition) return [];

  return [
    definition.id,
    definition.name,
    definition.variantName,
    definition.displayName,
    definition.description,
    definition.rarity,
    ...Object.values(definition.tagsByCategory ?? {}).flat(),
    ...(definition.attributeBonuses ?? []).map((bonus) => bonus?.attribute),
    ...essenceAbilitySearchTerms(definition.activeAbility),
    ...essenceAbilitySearchTerms(definition.passiveAbility),
    ...essenceEvolutionSearchTerms(definition.evolution),
  ];
}

export function playerEssenceSearchTerms(
  essence: PlayerEssenceDto | null | undefined,
  definition?: EssenceDefinitionDto | null,
): Searchable[] {
  if (!essence) return [];

  return [
    essence.name,
    essence.essenceDefinitionId,
    ...(essence.tags ?? []),
    ...essenceAbilitySearchTerms(essence.activeAbility),
    ...essenceAbilitySearchTerms(essence.passiveAbility),
    essence.evolveInfo?.name,
    essence.evolveInfo?.description,
    ...essenceDefinitionSearchTerms(definition),
  ];
}

export function toSearchText(terms: Searchable[]): string {
  return terms
    .filter((term): term is string => !!term)
    .join(' ')
    .toLowerCase();
}

export function essenceDefinitionSearchText(
  definition: EssenceDefinitionDto | null | undefined,
): string {
  return toSearchText(essenceDefinitionSearchTerms(definition));
}

export function playerEssenceSearchText(
  essence: PlayerEssenceDto | null | undefined,
  definition?: EssenceDefinitionDto | null,
): string {
  return toSearchText(playerEssenceSearchTerms(essence, definition));
}
