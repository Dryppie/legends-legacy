import { Component } from '@angular/core';
import {
  CombatLogService,
  CombatLogStats,
} from '../../../../core/services/client-side/combat/combat-log/combat-log.service';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-combat-log',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './combat-log.component.html',
})
export class CombatLogComponent {
  readonly stats;

  constructor(public service: CombatLogService) {
    this.stats = toSignal(service.stats$, {
      initialValue: { wins: 0, losses: 0, xp: 0 } as CombatLogStats,
    });
  }
}
