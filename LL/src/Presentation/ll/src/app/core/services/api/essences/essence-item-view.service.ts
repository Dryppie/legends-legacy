import { Injectable } from '@angular/core';
import {
  Ability,
  AttackType,
  DamageType,
  Essence,
} from '../../../../shared/models/essence';
import { EffectTag } from '../../../../shared/models/enums/effectType';
import { isAbilityTargetSelector } from '../../../../shared/models/enums/targeting';
import {
  EssenceItem,
  essenceItemToEssence,
} from '../../../../shared/models/item';
import {
  EssenceAbilityDto,
  EssenceDefinitionDto,
} from '../../../../shared/models/essence-system';

@Injectable({ providedIn: 'root' })
export class EssenceItemViewService {
  asEssence(item: EssenceItem): Essence {
    return item.essence
      ? this.fromDefinition(item.essence)
      : essenceItemToEssence(item);
  }

  fromDefinition(definition: EssenceDefinitionDto): Essence {
    return {
      id: definition.id,
      name: definition.displayName,
      active: this.mapAbility(definition.activeAbility),
      passive: this.mapAbility(definition.passiveAbility),
      attributeModifiers: [],
    };
  }

  private mapAbility(ability: EssenceAbilityDto): Ability {
    const tags = ability.tags ?? [];

    return {
      name: ability.name,
      description: ability.description,
      tags: [...tags],
      attackTypes: this.filterEnumValues(tags, AttackType, 'Attack'),
      damageTypes: this.filterEnumValues(tags, DamageType, 'Damage'),
      effectTags: this.filterEnumValues(tags, EffectTag, 'Effect'),
      targets: (ability.targets ?? []).filter(isAbilityTargetSelector),
      cooldown: ability.cooldownSeconds * 10,
      effects: ability.effects ?? [],
    };
  }

  private filterEnumValues<T extends Record<string, string>>(
    values: string[],
    enumType: T,
    category: string,
  ): T[keyof T][] {
    const allowedValues = new Set(Object.values(enumType));
    return values
      .filter(
        (value) => !value.includes('.') || value.startsWith(`${category}.`),
      )
      .map((value) => value.slice(value.lastIndexOf('.') + 1))
      .filter((value): value is T[keyof T] => allowedValues.has(value));
  }
}
