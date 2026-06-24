import { Component, signal } from '@angular/core';
import {
  CombatLogService,
  CombatLogStats,
} from '../../../../core/services/client-side/combat/combat-log/combat-log.service';
import { CommonModule } from '@angular/common';
import { CombatStatsCardComponent } from './combat-stats-card/combat-stats-card.component';

@Component({
  selector: 'app-combat-log',
  standalone: true,
  imports: [CommonModule, CombatStatsCardComponent],
  templateUrl: './combat-log.component.html',
})
export class CombatLogComponent {
  stats = signal<CombatLogStats>({
    wins: 0,
    losses: 0,
    xp: 0,
  });

  constructor(public service: CombatLogService) {
    service.stats$.subscribe((stats) => this.stats.set(stats));
  }
}
