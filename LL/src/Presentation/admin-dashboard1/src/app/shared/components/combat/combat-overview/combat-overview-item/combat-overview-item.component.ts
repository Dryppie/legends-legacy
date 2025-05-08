import { Component, Input } from '@angular/core';
import { CombatEvent } from '../../../../models/Dtos/combatEventDto';
import { TicksToSecondsPipe } from '../../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';

@Component({
  selector: 'app-combat-overview-item',
  standalone: true,
  imports: [TicksToSecondsPipe],
  templateUrl: './combat-overview-item.component.html',
  styleUrl: './combat-overview-item.component.css',
})
export class CombatOverviewItemComponent {
  @Input() combatEvent!: CombatEvent;
}
