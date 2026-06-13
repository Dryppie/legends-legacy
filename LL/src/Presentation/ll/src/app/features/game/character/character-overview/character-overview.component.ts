import { NgFor, NgIf } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, EMPTY, take } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { AttributeDto } from '../../../../shared/models/Dtos/attributesDto';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { AttributeTypeFormatPipe } from '../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-character-overview',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    NgIf,
    NgFor,
    FormsModule,
    RegularButtonComponent,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
  ],
  templateUrl: './character-overview.component.html',
})
export class CharacterOverviewComponent {
  readonly AttributeType = AttributeType;
  searchValue = signal('');
  character = signal<CharacterOverviewDto | null>(null);
  readonly primaryAttributes = [
    AttributeType.Power,
    AttributeType.Fortitude,
    AttributeType.Precision,
    AttributeType.Spirit,
  ];
  readonly highlightStats = [
    AttributeType.MaxHealth,
    AttributeType.WeaponDamage,
    AttributeType.CritChance,
    AttributeType.Armor,
  ];
  readonly attributeSections: { title: string; attributes: AttributeType[] }[] = [
    {
      title: 'Offense',
      attributes: [
        AttributeType.WeaponDamage,
        AttributeType.CritChance,
        AttributeType.CritDamage,
        AttributeType.ArmorPenetration,
        AttributeType.MagicPenetration,
      ],
    },
    {
      title: 'Defense',
      attributes: [
        AttributeType.Armor,
        AttributeType.Resistance,
        AttributeType.DodgeChance,
        AttributeType.BlockChance,
        AttributeType.DamageReduction,
      ],
    },
    {
      title: 'Recovery',
      attributes: [
        AttributeType.HealingPowerPercent,
        AttributeType.HealthRegeneration,
        AttributeType.LifeSteal,
      ],
    },
    {
      title: 'Utility',
      attributes: [
        AttributeType.Cooldown,
        AttributeType.StatusResistance,
        AttributeType.CrowdControlResistance,
        AttributeType.SummonPower,
        AttributeType.SummonHealth,
      ],
    },
  ];

  constructor(private characterService: CharacterService) {
    this.characterService.characterOverview$
      .pipe(take(1))
      .subscribe((c) => this.character.set(c));
  }

  onSearch() {
    const trimmed = this.searchValue().trim();
    if (!trimmed) return;

    this.characterService
      .searchCharacter(trimmed)
      .pipe(
        catchError((err) => {
          console.error(err.message);
          return EMPTY;
        }),
      )
      .subscribe((character) => {
        this.character.set(character);
      });
  }

  onEnter(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }

  getAttribute(type: AttributeType): AttributeDto {
    const current = this.character();
    return (
      current?.baseCombatAttributes.find((attr) => attr.attributeType === type) ??
      current?.baseAttributes.find((attr) => attr.attributeType === type) ?? {
        attributeType: type,
        value: 0,
      }
    );
  }

  getSectionAttributes(attributes: AttributeType[]): AttributeDto[] {
    return attributes.map((type) => this.getAttribute(type));
  }

  get filledLoadoutSlots(): number {
    return (
      this.character()?.activeEssenceLoadout?.slots.filter(
        (slot) => !!slot.playerEssenceId,
      ).length ?? 0
    );
  }

  get totalLoadoutSlots(): number {
    return this.character()?.activeEssenceLoadout?.slots.length ?? 0;
  }
}
