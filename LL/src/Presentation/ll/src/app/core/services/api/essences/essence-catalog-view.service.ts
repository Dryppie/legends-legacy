import { Injectable, signal } from '@angular/core';
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
  EssenceCatalogDto,
  EssenceDefinitionDto,
} from '../../../../shared/models/essence-system';
import { EssencesService } from './essences.service';

@Injectable({ providedIn: 'root' })
export class EssenceCatalogViewService {
  private readonly _catalog = signal<EssenceCatalogDto | null>(null);
  private loading = false;

  constructor(private readonly essencesService: EssencesService) {}

  load(): void {
    if (this._catalog() || this.loading) return;

    this.loading = true;
    this.essencesService.getCatalog().subscribe({
      next: (catalog) => this.setCatalog(catalog),
      error: () => {
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      },
    });
  }

  setCatalog(catalog: EssenceCatalogDto): void {
    this._catalog.set(catalog);
    this.loading = false;
  }

  asEssence(item: EssenceItem): Essence {
    const definition = this.getDefinition(item.essenceDefinitionId);
    return definition
      ? this.mapDefinitionToEssence(definition)
      : essenceItemToEssence(item);
  }

  private getDefinition(essenceDefinitionId: string): EssenceDefinitionDto | null {
    return (
      this._catalog()?.essences.find(
        (essence) => essence.id === essenceDefinitionId,
      ) ?? null
    );
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
