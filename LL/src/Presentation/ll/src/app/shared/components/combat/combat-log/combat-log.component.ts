import { Component, computed, signal } from '@angular/core';
import { CombatLogService } from '../../../../core/services/client-side/combat/combat-log/combat-log.service';
import { CommonModule } from '@angular/common';
import { CombatStatsCardComponent } from './combat-stats-card/combat-stats-card.component';
import { CombatRecord } from './combat-record';
import { BattleOutcome } from '../../../models/Dtos/combatResultDto';

@Component({
  selector: 'app-combat-log',
  standalone: true,
  imports: [CommonModule, CombatStatsCardComponent],
  templateUrl: './combat-log.component.html',
})
export class CombatLogComponent {
  logs = signal<CombatRecord[]>([]);
  constructor(public service: CombatLogService) {
    service.logs$.subscribe((arr) => this.logs.set(arr));
  }

  readonly stats = computed(() => {
    const list = this.logs();
    return {
      wins: list.filter((l) => l.outcome === BattleOutcome.Victory).length,
      losses: list.filter((l) => l.outcome === BattleOutcome.Defeat).length,
      // gold: list.reduce((s, l) => s + (l.gold || 0), 0),
      xp: list.reduce((s, l) => s + (l.xp || 0), 0),
    } as const;
  });
}
