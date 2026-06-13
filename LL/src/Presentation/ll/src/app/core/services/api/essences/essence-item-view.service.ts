import { Injectable } from '@angular/core';
import {
  AttributeModifier,
  ModifierType,
} from '../../../../shared/models/Dtos/attributesDto';
import {
  Ability,
  AttackType,
  DamageType,
  Essence,
} from '../../../../shared/models/essence';
import { EffectTag } from '../../../../shared/models/enums/effectType';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { Targeting } from '../../../../shared/models/enums/targeting';
import { EssenceItem, essenceItemToEssence } from '../../../../shared/models/item';
import {
  EssenceAbilityDto,
  EssenceDefinitionDto,
} from '../../../../shared/models/essence-system';

@Injectable({ providedIn: 'root' })
export class EssenceItemViewService {
  asEssence(item: EssenceItem): Essence {
    return item.essence
      ? this.mapDefinitionToEssence(item.essence)
      : essenceItemToEssence(item);
  }

  private mapDefinitionToEssence(definition: EssenceDefinitionDto): Essence {
    return {
      id: definition.id,
      name: definition.name,
      active: this.mapAbility(definition.activeAbility),
      passive: this.mapAbility(definition.passiveAbility),
      attributeModifiers: definition.attributeBonuses.map<AttributeModifier>(
        (bonus) => ({
          attributeType: bonus.attribute as AttributeType,
          amount: bonus.currentValue,
          modifierType: this.mapModifierType(bonus.modifierKind),
        }),
      ),
    };
  }

  private mapAbility(ability: EssenceAbilityDto): Ability {
    const tags = ability.tags ?? [];

    return {
      name: ability.name,
      description: ability.description,
      attackTypes: this.filterEnumValues(tags, AttackType),
      damageTypes: this.filterEnumValues(tags, DamageType),
      effectTags: this.filterEnumValues(tags, EffectTag),
      targeting: ability.targeting ? [ability.targeting as Targeting] : [],
      cooldown: ability.cooldownSeconds * 10,
      effects: ability.effects ?? [],
    };
  }

  private filterEnumValues<T extends Record<string, string>>(
    values: string[],
    enumType: T,
  ): T[keyof T][] {
    const allowedValues = new Set(Object.values(enumType));
    return values.filter((value): value is T[keyof T] =>
      allowedValues.has(value),
    );
  }

  private mapModifierType(modifierKind: string): ModifierType {
    switch (modifierKind) {
      case ModifierType.Multiplicative:
        return ModifierType.Multiplicative;
      case ModifierType.Additive:
      case 'Percent':
        return ModifierType.Additive;
      default:
        return ModifierType.Flat;
    }
  }
}
