import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AbilityStats } from '../../../models/Dtos/combatResultDto';
import { AbilityTagsComponent } from '../../essences/ability-tags/ability-tags.component';
import { EssenceDescriptionComponent } from '../../essences/essence-description/essence-description.component';
import { PopoverComponent } from '../../custom-components/popover/popover.component';

@Component({
  selector: 'app-combat-ability-name',
  host: { class: 'block min-w-0' },
  imports: [
    NgIf,
    PopoverComponent,
    EssenceDescriptionComponent,
    AbilityTagsComponent,
  ],
  templateUrl: './combat-ability-name.component.html',
})
export class CombatAbilityNameComponent {
  @Input({ required: true }) ability!: AbilityStats;
}
