import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import {
  BattleOutcome,
  CombatResultDto,
  CombatSessionDto,
} from '../../../../../shared/models/Dtos/combatResultDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { CombatService } from '../../../client-side/combat/combat.service';
import { CombatLogService } from '../../../client-side/combat/combat-log/combat-log.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CurrencyService } from '../../currency/currency.service';
import { CombatActionHandler } from './combat-action-handler';

describe('CombatActionHandler', () => {
  let combat: jasmine.SpyObj<CombatService>;
  let summary: jasmine.SpyObj<SessionSummaryService>;
  let currency: jasmine.SpyObj<CurrencyService>;
  let combatLog: jasmine.SpyObj<CombatLogService>;
  let handler: CombatActionHandler;

  beforeEach(() => {
    combat = jasmine.createSpyObj('CombatService', [
      'startCombatSimulation',
      'applyIdleCombatExperience',
    ]);
    summary = jasmine.createSpyObj('SessionSummaryService', [
      'loadCombatSince',
    ]);
    currency = jasmine.createSpyObj('CurrencyService', [
      'gainCinders',
      'gainSoulstones',
    ]);
    combatLog = jasmine.createSpyObj('CombatLogService', ['addSession']);
    handler = new CombatActionHandler(combat, summary, currency, combatLog);
  });

  it('applies intermediate rewards without replacing the displayed battle', () => {
    const action = combatAction(true, 400);

    handler.handle(action);

    expect(summary.loadCombatSince).toHaveBeenCalledWith(
      action.combatSession,
      true,
    );
    expect(combat.applyIdleCombatExperience).not.toHaveBeenCalled();
    expect(currency.gainCinders).not.toHaveBeenCalled();
    expect(currency.gainSoulstones).not.toHaveBeenCalled();
    expect(combat.startCombatSimulation).not.toHaveBeenCalled();
    expect(combatLog.addSession).not.toHaveBeenCalled();
  });

  it('renders once and logs the combined session when catch-up completes', () => {
    const action = combatAction(false, 240);
    const combinedSession = combatSession(640);
    summary.loadCombatSince.and.returnValue(combinedSession);

    handler.handle(action);

    expect(combat.startCombatSimulation).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ combatSession: combinedSession }),
    );
    expect(combat.applyIdleCombatExperience).toHaveBeenCalledOnceWith(640);
    expect(currency.gainCinders).toHaveBeenCalledOnceWith(12);
    expect(currency.gainSoulstones).toHaveBeenCalledOnceWith(3);
    expect(combatLog.addSession).toHaveBeenCalledOnceWith(combinedSession);
  });
});

function combatAction(
  hasPendingCombatResolution: boolean,
  experience: number,
): CharacterActionDto {
  return {
    characterActionType: CharacterActionType.Combat,
    lootTableId: 'lumo-ruins',
    updatedAt: new Date('2026-08-11T00:16:40Z'),
    nextResolutionAt: new Date('2026-08-11T00:16:50Z'),
    revision: 'revision',
    isDeleted: false,
    hasPendingCombatResolution,
    combatSession: combatSession(experience),
  };
}

function combatSession(experience: number): CombatSessionDto {
  return {
    from: new Date('2026-08-11T00:00:00Z'),
    to: new Date('2026-08-11T00:16:40Z'),
    combatResult: {
      startedAt: new Date('2026-08-11T00:16:30Z'),
      outcome: BattleOutcome.Victory,
      experienceGained: experience,
    } as CombatResultDto,
    combatSummary: {
      totalBattles: 100,
      wins: 100,
      losses: 0,
      draws: 0,
      totalExperience: experience,
      totalGold: 0,
      totalCinders: 12,
      totalSoulstones: 3,
    },
  };
}
