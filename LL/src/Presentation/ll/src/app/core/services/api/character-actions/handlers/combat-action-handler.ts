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
    const hasPendingResolution = action.hasPendingCombatResolution ?? false;
    const completedSession = this.summary.loadCombatSince(
      action.combatSession,
      hasPendingResolution,
    );

    if (hasPendingResolution) {
      return;
    }

    const resolvedSession = completedSession ?? action.combatSession;
    const resolvedSummary = resolvedSession.combatSummary;
    this.combat.startCombatSimulation(
      resolvedSession === action.combatSession
        ? action
        : { ...action, combatSession: resolvedSession },
    );
    this.combat.applyIdleCombatExperience(resolvedSummary.totalExperience);
    this.currency.gainCinders(resolvedSummary.totalCinders);
    this.currency.gainSoulstones(resolvedSummary.totalSoulstones);
    this.combatLog.addSession(resolvedSession);
  }
}
