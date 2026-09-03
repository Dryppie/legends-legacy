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
    const combatSession = action.combatSession;
    const combatResult = combatSession?.combatResult;
    if (
      !combatSession ||
      !combatResult?.playerTeam?.length ||
      !combatResult.enemyTeam?.length
    ) {
      return;
    }
    const hasPendingResolution = action.hasMoreDueWork ?? false;
    const completedSession = this.summary.loadCombatSince(
      combatSession,
      hasPendingResolution,
    );

    if (hasPendingResolution) {
      return;
    }

    const resolvedSession = completedSession ?? combatSession;
    const resolvedSummary = resolvedSession.combatSummary;
    this.combat.startCombatSimulation(
      resolvedSession === combatSession
        ? action
        : { ...action, combatSession: resolvedSession },
    );
    this.combat.applyIdleCombatExperience(resolvedSummary.totalExperience);
    this.currency.gainCinders(resolvedSummary.totalCinders);
    this.currency.gainSoulstones(resolvedSummary.totalSoulstones);
    this.combatLog.addSession(resolvedSession);
  }

}
