import { TestBed } from '@angular/core/testing';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import {
  BattleOutcome,
  CombatResultDto,
} from '../../../../shared/models/Dtos/combatResultDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { CombatStateService } from '../../../state/combat-state/combat-state.service';
import { BattleType } from '../../../state/combat-state/combatState';
import { EventBusService } from '../event-bus/event-bus.service';
import { LevelingService } from '../leveling/leveling.service';
import { CombatService } from './combat.service';

describe('CombatService', () => {
  let service: CombatService;
  let state: CombatStateService;
  let eventBus: EventBusService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CombatService,
        CombatStateService,
        EventBusService,
        {
          provide: LevelingService,
          useValue: jasmine.createSpyObj('LevelingService', ['gainExperience']),
        },
      ],
    });

    service = TestBed.inject(CombatService);
    state = TestBed.inject(CombatStateService);
    eventBus = TestBed.inject(EventBusService);
  });

  it('keeps idle combat active when the completed First Hunt state is cleared', () => {
    service.startTrainingBattleSummary(combatResult(BattleType.Training));
    service.closeCurrentTrainingBattle();

    const idleResult = combatResult(BattleType.IdleCombat);
    service.startCombatSimulation(combatAction(idleResult));

    // Region teardown clears the training slot again when navigation opens the
    // regular combat route. It must not affect the idle-combat slot.
    service.stop(BattleType.Training);

    expect(state.getIsCombatActive(BattleType.Training)()).toBeFalse();
    expect(state.getIsCombatActive(BattleType.IdleCombat)()).toBeTrue();
    expect(state.getCombatResult(BattleType.IdleCombat)()).toBe(idleResult);
  });

  it('does not clear a new idle encounter after an earlier logout', () => {
    eventBus.emitLogout();
    TestBed.flushEffects();

    const idleResult = combatResult(BattleType.IdleCombat);
    service.startCombatSimulation(combatAction(idleResult));
    TestBed.flushEffects();

    expect(state.getIsCombatActive(BattleType.IdleCombat)()).toBeTrue();
    expect(state.getCombatResult(BattleType.IdleCombat)()).toBe(idleResult);
  });

  it('keeps the last idle encounter when a zero-encounter result arrives', () => {
    const displayedResult = combatResult(BattleType.IdleCombat);
    service.startCombatSimulation(combatAction(displayedResult));

    const emptyResult = combatResult(BattleType.IdleCombat);
    emptyResult.playerTeam = [];
    emptyResult.enemyTeam = [];
    service.startCombatSimulation(combatAction(emptyResult));

    expect(state.getIsCombatActive(BattleType.IdleCombat)()).toBeTrue();
    expect(state.getCombatResult(BattleType.IdleCombat)()).toBe(
      displayedResult,
    );
  });

  it('opens and closes a Tower combat result in its own state slot', () => {
    const result = combatResult(BattleType.Tower);

    service.startTowerBattleSummary(result);

    expect(state.getIsCombatActive(BattleType.Tower)()).toBeTrue();
    expect(state.getCombatResult(BattleType.Tower)()).toBe(result);
    expect(state.getCombatOutcome(BattleType.Tower)()).toBe(
      BattleOutcome.Victory,
    );

    service.closeCurrentTowerBattle();

    expect(state.getIsCombatActive(BattleType.Tower)()).toBeFalse();
    expect(state.getCombatResult(BattleType.Tower)()).toBeNull();
  });

  it('applies and closes Raid playback in its own state slot', () => {
    service.applyRaidCombatFrame({
      sequence: 0,
      tick: 10,
      friendly: [combatant('raider')],
      hostile: [combatant('raid-boss')],
      entityStats: [],
      events: [],
      isFinal: true,
      outcome: BattleOutcome.Victory,
    });

    expect(state.getIsCombatActive(BattleType.Raid)()).toBeTrue();
    expect(state.getEnemyCharacters(BattleType.Raid)()[0].id).toBe('raid-boss');
    expect(state.getCombatOutcome(BattleType.Raid)()).toBe(
      BattleOutcome.Victory,
    );

    service.closeCurrentRaidBattle();

    expect(state.getIsCombatActive(BattleType.Raid)()).toBeFalse();
    expect(state.getCombatResult(BattleType.Raid)()).toBeNull();
  });

  it('applies and closes Region Boss playback in its own state slot', () => {
    service.applyRegionBossCombatFrame({
      sequence: 0,
      tick: 10,
      friendly: [combatant('player')],
      hostile: [combatant('mad-king')],
      entityStats: [],
      events: [],
      isFinal: false,
      outcome: null,
    });

    expect(state.getIsCombatActive(BattleType.RegionBoss)()).toBeTrue();
    expect(state.getEnemyCharacters(BattleType.RegionBoss)()[0].id).toBe(
      'mad-king',
    );

    service.closeCurrentRegionBossBattle();

    expect(state.getIsCombatActive(BattleType.RegionBoss)()).toBeFalse();
    expect(state.getCombatResult(BattleType.RegionBoss)()).toBeNull();
  });
});

function combatAction(result: CombatResultDto): CharacterActionDto {
  const nextResolutionAt = new Date('2026-08-09T12:00:10Z');

  return {
    characterActionType: CharacterActionType.Combat,
    lootTableId: 'lumo-ruins',
    updatedAt: nextResolutionAt,
    nextResolutionAtUtc: nextResolutionAt,
    revision: 'lumo-combat',
    isDeleted: false,
    combatSession: {
      from: new Date('2026-08-09T12:00:00Z'),
      to: nextResolutionAt,
      combatResult: result,
      combatSummary: {
        totalBattles: 1,
        wins: 1,
        losses: 0,
        draws: 0,
        totalExperience: 0,
        totalGold: 0,
        totalCinders: 0,
        totalSoulstones: 0,
      },
    },
  };
}

function combatResult(battleType: BattleType): CombatResultDto {
  return {
    playerTeam: [combatant('player')],
    enemyTeam: [combatant('enemy')],
    duration: 10,
    startedAt: new Date('2026-08-09T12:00:00Z'),
    outcome: BattleOutcome.Victory,
    loot: [],
    gatheringRewards: [],
    experienceGained: 0,
    battleType,
    entityStats: [],
  };
}

function combatant(id: string) {
  return {
    id,
    name: id,
    imagePath: '',
    health: 10,
    maxHealth: 10,
    barrier: 0,
    level: 1,
  };
}
