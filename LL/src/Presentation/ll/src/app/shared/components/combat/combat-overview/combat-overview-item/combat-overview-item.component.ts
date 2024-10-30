import { Component, Input } from '@angular/core';
import { CombatEvent } from '../../../../models/Dtos/combatEventDto';

@Component({
  selector: 'app-combat-overview-item',
  standalone: true,
  imports: [],
  templateUrl: './combat-overview-item.component.html',
  styleUrl: './combat-overview-item.component.css',
})
export class CombatOverviewItemComponent {
  @Input() combatEvent!: CombatEvent;
}
