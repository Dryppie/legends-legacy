import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-combat-stats-card',
  standalone: true,
  imports: [],
  templateUrl: './combat-stats-card.component.html',
})
export class CombatStatsCardComponent {
  @Input() label = '';
  @Input() value: string | number = '';
}
