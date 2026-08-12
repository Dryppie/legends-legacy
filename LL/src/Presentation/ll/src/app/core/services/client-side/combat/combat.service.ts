import { effect, Injectable, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CombatStateService } from '../../../state/combat-state/combat-state.service';
import { EventBusService } from '../event-bus/event-bus.service';
import { BattleType } from '../../../state/combat-state/combatState';
import {
  BattleOutcome,
  CombatResultDto,
} from '../../../../shared/models/Dtos/combatResultDto';
import { LevelingService } from '../leveling/leveling.service';
import { TowerCombatFrame } from '../../api/world-tower/world-tower.service';

@Injectable({
  providedIn: 'root',
})
export class CombatService {
  private combatEndSubscriptions = new Map<BattleType, Subscription>();

  constructor(
    private combatStateService: CombatStateService,
    private eventBus: EventBusService,
    private levelingService: LevelingService,
  ) {
    effect(
      () => {
        if (this.eventBus.logout()) {
          untracked(() => this.handleLogout());
        }
      },
      { allowSignalWrites: true },
    );
  }

  clearAllCombat() {
    this.clearCurrentCombat(BattleType.IdleCombat);
  }

  clearCurrentCombat(type: BattleType) {
    this.combatStateService.resetCombatState(type);
  }

  startDungeonCombatSimulation(combatResult: CombatResultDto | null): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Dungeon;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startColosseumMatchSimulation(combatResult: CombatResultDto): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Colosseum;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startTowerBattleSummary(combatResult: CombatResultDto): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Tower;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  applyTowerCombatFrame(frame: TowerCombatFrame, reset = false): void {
    const type = BattleType.Tower;
    if (reset) this.clearCurrentCombat(type);

    const result: CombatResultDto = {
      playerTeam: frame.friendly,
      enemyTeam: frame.hostile,
      duration: frame.tick,
      startedAt: new Date(),
      outcome: frame.outcome ?? BattleOutcome.Draw,
      loot: [],
      gatheringRewards: [],
      experienceGained: 0,
      battleType: type,
      entityStats: frame.entityStats,
    };
    this.combatStateService.setCombatActive(type, true);
    this.combatStateService.setPlayerCharacters(type, frame.friendly);
    this.combatStateService.setEnemyCharacters(type, frame.hostile);
    this.combatStateService.setEntityStats(type, frame.entityStats);
    this.combatStateService.setCombatResult(type, result);
    this.combatStateService.setCombatOutcome(
      type,
      frame.isFinal ? frame.outcome : null,
    );
  }

  startTrainingBattleSummary(combatResult: CombatResultDto): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Training;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startCombatSimulation(characterAction: CharacterActionDto): void {
    const combatResult = characterAction.combatSession?.combatResult;
    if (!combatResult) return;
    combatResult.battleType = BattleType.IdleCombat;

    this.combatEndSubscriptions.get(BattleType.IdleCombat)?.unsubscribe();
    this.combatEndSubscriptions.delete(BattleType.IdleCombat);

    // Replace the complete encounter snapshot in one signal write. The previous
    // encounter stays visible until this fully hydrated result is available.
    this.combatStateService.commitEncounter(
      BattleType.IdleCombat,
      combatResult,
      characterAction.nextResolutionAt ?? characterAction.updatedAt,
    );
  }

  applyIdleCombatExperience(experienceGained: number): void {
    if (experienceGained <= 0) return;
    this.levelingService.gainExperience(experienceGained);
  }

  simulateFight(combatResult: CombatResultDto) {
    const type = combatResult.battleType;
    const combatAction = combatResult;
    this.combatStateService.setPlayerCharacters(type, combatResult.playerTeam);
    this.combatStateService.setEnemyCharacters(type, combatResult.enemyTeam);
    this.combatStateService.setCombatResult(type, combatAction);
    this.combatStateService.setEntityStats(type, combatAction.entityStats);

    if (
      type === BattleType.Colosseum ||
      type === BattleType.Dungeon ||
      type === BattleType.Tower ||
      type === BattleType.Training
    ) {
      this.combatStateService.setCombatOutcome(type, combatAction.outcome);
      return;
    }
  }

  skipCurrentColosseum(): void {
    const type = BattleType.Colosseum;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    // Cancel any pending completion before closing the summary.
    this.combatEndSubscriptions.get(type)?.unsubscribe();
    this.combatEndSubscriptions.delete(type);

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);

    this.handleCombatComplete(combatResult);
  }

  skipCurrentDungeonMatch(): void {
    const type = BattleType.Dungeon;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    // Cancel any pending completion before closing the summary.
    this.combatEndSubscriptions.get(type)?.unsubscribe();
    this.combatEndSubscriptions.delete(type);

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);

    this.handleCombatComplete(combatResult);
  }

  closeCurrentTowerBattle(): void {
    const type = BattleType.Tower;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);
    this.handleCombatComplete(combatResult);
  }

  closeCurrentTrainingBattle(): void {
    const type = BattleType.Training;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);

    this.handleCombatComplete(combatResult);
  }

  private handleCombatComplete(combatResult: CombatResultDto) {
    const type = combatResult.battleType;

    if (type === BattleType.Colosseum) {
      this.combatEndSubscriptions.get(type)?.unsubscribe();
      this.combatEndSubscriptions.delete(type);
      this.eventBus.emit('colosseum-combat-finished', {
        outcome: combatResult.outcome, // 'Victory' | 'Defeat' | 'Draw'
      });
    }

    this.combatStateService.setCombatActive(type, false);

    if (type === BattleType.IdleCombat) {
      this.levelingService.gainExperience(combatResult.experienceGained);
      return;
    }

    this.stop(type);
  }

  /** stop & forget a particular fight (e.g. UI tab closed) */
  stop(battleType: BattleType) {
    this.combatEndSubscriptions.get(battleType)?.unsubscribe();
    this.combatEndSubscriptions.delete(battleType);
    this.combatStateService.resetCombatState(battleType);
  }

  handleLogout() {
    this.combatEndSubscriptions.forEach((sub) => sub.unsubscribe());
    this.combatEndSubscriptions.clear();
    this.clearCurrentCombat(BattleType.IdleCombat);
  }
}
