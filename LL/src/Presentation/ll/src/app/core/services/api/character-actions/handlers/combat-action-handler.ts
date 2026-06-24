import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CombatService } from '../../../client-side/combat/combat.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CurrencyService } from '../../currency/currency.service';
import { CombatLogService } from '../../../client-side/combat/combat-log/combat-log.service';

@Injectable({ providedIn: 'root' })
export class CombatActionHandler {
  constructor(
    private readonly combat: CombatService,
    private readonly summary: SessionSummaryService,
    private readonly currency: CurrencyService,
    private readonly combatLog: CombatLogService,
  ) {}

  handle(action: CharacterActionDto): void {
    if (!action.combatSession) return;
    this.combat.startCombatSimulation(action);
    this.summary.loadCombatSince(action.combatSession);
    const summary = action.combatSession.combatSummary;
    if (summary) {
      this.combatLog.addSession(action.combatSession);
      this.currency.gainCinders(summary.totalCinders);
      this.currency.gainSoulstones(summary.totalSoulstones);
    }
  }
}
