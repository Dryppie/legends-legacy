import { Component, Inject } from '@angular/core';
import { ESSENCE_ABILITY_DATA } from './essence-ability-data.token';
import { EssenceAbilityData } from './essenceAbilityData';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-ability-tooltip',
  standalone: true,
  imports: [NgIf],
  templateUrl: './ability-tooltip.component.html',
})
export class AbilityTooltipComponent {
  constructor(@Inject(ESSENCE_ABILITY_DATA) public data: EssenceAbilityData) {}
}
