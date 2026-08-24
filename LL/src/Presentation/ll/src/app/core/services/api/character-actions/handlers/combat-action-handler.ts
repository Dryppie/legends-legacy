import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../../client-side/combat/combat.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CurrencyService } from '../../currency/currency.service';
import { CombatLogService } from '../../../client-side/combat/combat-log/combat-log.service';
import { LevelingService } from '../../../client-side/leveling/leveling.service';
import { GatheringType } from '../../../../../shared/models/enums/gatheringType';
import { ProfessionType } from '../../../../../shared/models/Dtos/characterProfession';

@Injectable({ providedIn: 'root' })
export class CombatActionHandler {
  constructor(
    private readonly combat: CombatService,
    private readonly summary: SessionSummaryService,
    private readonly currency: CurrencyService,
    private readonly combatLog: CombatLogService,
    private readonly leveling: LevelingService,
  ) {}

  handle(action: CharacterActionDto): void {
    if (!action.combatSession) return;
    const hasPendingResolution = action.hasMoreDueWork ?? false;
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
    this.applyGatheringExperience(
      resolvedSession.combatResult.gatheringRewards,
    );
    this.currency.gainCinders(resolvedSummary.totalCinders);
    this.currency.gainSoulstones(resolvedSummary.totalSoulstones);
    this.combatLog.addSession(resolvedSession);
  }

  private applyGatheringExperience(
    rewards: NonNullable<CombatResultDto['gatheringRewards']> = [],
  ): void {
    const experienceByProfession = new Map<ProfessionType, number>();

    for (const reward of rewards ?? []) {
      const professionType = this.toProfessionType(reward.toolType);
      const experience = reward.experienceGained ?? 0;
      if (!professionType || experience <= 0) continue;

      experienceByProfession.set(
        professionType,
        (experienceByProfession.get(professionType) ?? 0) + experience,
      );
    }

    for (const [professionType, experience] of experienceByProfession) {
      this.leveling.gainProfessionExperience(professionType, experience);
    }
  }

  private toProfessionType(
    gatheringType: GatheringType,
  ): ProfessionType | null {
    switch (gatheringType) {
      case GatheringType.Mining:
        return ProfessionType.Mining;
      case GatheringType.Woodcutting:
        return ProfessionType.Woodcutting;
      case GatheringType.Skinning:
        return ProfessionType.Skinning;
      default:
        return null;
    }
  }
}
