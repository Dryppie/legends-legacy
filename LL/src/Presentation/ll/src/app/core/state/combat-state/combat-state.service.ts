import {
  computed,
  effect,
  Injectable,
  signal,
  WritableSignal,
} from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import { EventBusService } from '../../services/client-side/event-bus/event-bus.service';
import { BattleType, CombatState } from './combatState';

@Injectable({
  providedIn: 'root',
})
@Injectable({ providedIn: 'root' })
export class CombatStateService {
  private readonly defaultState: CombatState = {
    playerCharacters: [],
    enemyCharacters: [],
    combatEvents: [],
    combatResult: null,
    combatOutcome: null,
    nextCombat: null,
    isCombatActive: false,
    entityStats: [],
  };

  private readonly stateMap = new Map<
    BattleType,
    WritableSignal<CombatState>
  >();
  private readonly lastEventsLength: { [type: string]: number } = {};

  constructor(private readonly eventBus: EventBusService) {
    effect(() => {
      queueMicrotask(() => {
        if (this.eventBus.logout()) {
          this.handleLogout();
        }
      });
    });
  }

  private ensureState(type: BattleType): WritableSignal<CombatState> {
    if (!this.stateMap.has(type)) {
      this.stateMap.set(type, signal({ ...this.defaultState }));
    }
    return this.stateMap.get(type)!;
  }

  // ----------------------
  // Mutators
  // ----------------------

  setPlayerCharacters(type: BattleType, characters: SimpleCombatEntityDto[]) {
    this.patchState(type, { playerCharacters: characters });
  }

  setEnemyCharacters(type: BattleType, characters: SimpleCombatEntityDto[]) {
    this.patchState(type, { enemyCharacters: characters });
  }

  addCombatEvent(type: BattleType, event: CombatEvent) {
    const state = this.ensureState(type)();
    this.patchState(type, { combatEvents: [...state.combatEvents, event] });
  }

  setCombatActive(type: BattleType, isActive: boolean) {
    this.patchState(type, { isCombatActive: isActive });
  }

  setCombatResult(type: BattleType, result: CombatResultDto | null) {
    this.patchState(type, { combatResult: result });
  }

  setCombatOutcome(type: BattleType, outcome: BattleOutcome | null) {
    this.patchState(type, { combatOutcome: outcome });
  }

  setNextCombatIn(type: BattleType, nextCombatIn: Date) {
    this.patchState(type, { nextCombat: nextCombatIn });
  }

  setLastEventsLength(type: BattleType, length: number): number {
    return (this.lastEventsLength[type] = length);
  }

  setEntityStats(type: BattleType, entityStats: EntityStats[]) {
    this.patchState(type, { entityStats });
  }

  getLastEventsLength(type: BattleType): number {
    return this.lastEventsLength[type] || 0;
  }

  // ----------------------
  // Selectors
  // ----------------------

  getPlayerCharacters(type: BattleType) {
    return computed(() => this.ensureState(type)().playerCharacters);
  }

  getEnemyCharacters(type: BattleType) {
    return computed(() => this.ensureState(type)().enemyCharacters);
  }

  getCombatEvents(type: BattleType) {
    return computed(() => this.ensureState(type)().combatEvents);
  }

  getNextCombat(type: BattleType) {
    return computed(() => this.ensureState(type)().nextCombat);
  }

  getCombatResult(type: BattleType) {
    return computed(() => this.ensureState(type)().combatResult);
  }

  getCombatOutcome(type: BattleType) {
    return computed(() => this.ensureState(type)().combatOutcome);
  }

  getIsCombatActive(type: BattleType) {
    return computed(() => this.ensureState(type)().isCombatActive);
  }

  getEntityStats(type: BattleType) {
    return computed(() => this.ensureState(type)().entityStats);
  }

  // ----------------------
  // Internal
  // ----------------------

  private patchState(type: BattleType, patch: Partial<CombatState>) {
    const state = this.ensureState(type);
    state.update((current) => ({ ...current, ...patch }));
  }

  resetCombatStateForNextBattle(type: BattleType) {
    this.lastEventsLength[type] = 0;
    this.setCombatOutcome(type, null);
    this.setCombatResult(type, null);
  }

  resetCombatState(type: BattleType) {
    this.lastEventsLength[type] = 0;
    this.ensureState(type).set(structuredClone(this.defaultState));
  }

  handleLogout() {
    this.stateMap.forEach((signal) => signal.set({ ...this.defaultState }));
    Object.keys(this.lastEventsLength).forEach(
      (key) => delete this.lastEventsLength[key],
    );
  }
}
