import { CommonModule, NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { CombatOverviewItemComponent } from './combat-overview-item/combat-overview-item.component';
import { CombatEvent } from '../../../models/Dtos/combatEventDto';
import { StickyScrollDirective } from '../../../directives/sticky-scroll/sticky-scroll.directive';

@Component({
  selector: 'app-combat-overview',
  standalone: true,
  imports: [
    NgFor,
    CombatOverviewItemComponent,
    StickyScrollDirective,
    CommonModule,
  ],
  templateUrl: './combat-overview.component.html',
  styleUrl: './combat-overview.component.css',
})
export class CombatOverviewComponent {
  @Input() combatEvents: CombatEvent[] = [];
  @Input() isLoading = false;
}
