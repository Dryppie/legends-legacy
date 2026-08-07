import { Component, Inject } from '@angular/core';
import { ESSENCE_ABILITY_DATA } from './essence-ability-data.token';
import { EssenceAbilityData } from './essenceAbilityData';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-ability-tooltip',
  imports: [NgIf],
  templateUrl: './ability-tooltip.component.html',
})
export class AbilityTooltipComponent {
  constructor(@Inject(ESSENCE_ABILITY_DATA) public data: EssenceAbilityData) {}

  get hasBaseValue(): boolean {
    return Math.abs(this.data.base ?? 0) > Number.EPSILON;
  }

  get displayedTotal(): string {
    return (this.data.total ?? '').replace(/(?<=\d)-(?=\d)/g, '–');
  }

  get rollDisplay(): string {
    return this.data.rollDisplay || (this.data.hasRange ? '±20%' : 'Fixed');
  }
}
