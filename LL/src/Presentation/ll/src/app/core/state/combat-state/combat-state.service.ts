import { Injectable } from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
  SimpleCombatEntityDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { BehaviorSubject, distinctUntilChanged, map, Observable } from 'rxjs';
import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import { EventBusService } from '../../services/client-side/event-bus/event-bus.service';
import { BattleType, CombatState } from './combatState';

@Injectable({
  providedIn: 'root',
})
export class CombatStateService {
  private defaultState: CombatState = {
    playerCharacters: [],
    enemyCharacters: [],
    combatEvents: [],
    combatResult: null,
    combatOutcome: null,
    nextCombat: null,
    isCombatActive: false,
  };

  private lastEventsLength: { [battleType: string]: number } = {};

  private stateMap = new Map<BattleType, BehaviorSubject<CombatState>>();

  constructor(private eventBusService: EventBusService) {
    this.eventBusService.logout$.subscribe(() => {
      this.handleLogout();
    });
  }

  private ensureSubject(type: BattleType): BehaviorSubject<CombatState> {
    if (!this.stateMap.has(type)) {
      this.stateMap.set(
        type,
        new BehaviorSubject<CombatState>({ ...this.defaultState }),
      );
    }
    return this.stateMap.get(type)!;
  }

  // Observables for a given BattleType
  public state$(type: BattleType): Observable<CombatState> {
    return this.ensureSubject(type).asObservable();
  }

  public getState(type: BattleType): CombatState {
    return this.ensureSubject(type).value;
  }

  // private nextCombatSubject = new BehaviorSubject<Date | null>(null);
  // private isCombatActiveSubject = new BehaviorSubject<boolean>(false);

  // public nextCombat$ = this.nextCombatSubject.asObservable();
  // public isCombatActive$ = this.isCombatActiveSubject.asObservable();

  setPlayerCharacters(type: BattleType, characters: SimpleCombatEntityDto[]) {
    this.patchState(type, { playerCharacters: characters });
  }
  setEnemyCharacters(type: BattleType, characters: SimpleCombatEntityDto[]) {
    this.patchState(type, { enemyCharacters: characters });
  }
  addCombatEvent(type: BattleType, event: CombatEvent) {
    const state = this.getState(type);
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
  getPlayerCharacters$(type: BattleType): Observable<SimpleCombatEntityDto[]> {
    return this.state$(type).pipe(map((state) => state.playerCharacters));
  }
  getEnemyCharacters$(type: BattleType): Observable<SimpleCombatEntityDto[]> {
    return this.state$(type).pipe(map((state) => state.enemyCharacters));
  }
  getLastEventsLength(type: BattleType): number {
    return this.lastEventsLength[type];
  }
  setLastEventsLength(type: BattleType, length: number): number {
    return (this.lastEventsLength[type] = length);
  }
  getCombatEvents$(type: BattleType): Observable<CombatEvent[]> {
    return this.state$(type).pipe(map((state) => state.combatEvents));
  }
  getNextCombat$(type: BattleType): Observable<Date | null> {
    return this.state$(type).pipe(map((state) => state.nextCombat));
  }
  getCombatResult$(type: BattleType): Observable<CombatResultDto | null> {
    return this.state$(type).pipe(
      map((state) => state.combatResult),
      distinctUntilChanged(),
    );
  }
  getCombatOutcome$(type: BattleType): Observable<BattleOutcome | null> {
    return this.state$(type).pipe(map((state) => state.combatOutcome));
  }
  getIsCombatActive$(type: BattleType): Observable<boolean> {
    return this.state$(type).pipe(map((state) => state.isCombatActive));
  }

  private patchState(type: BattleType, patch: Partial<CombatState>) {
    const current = this.getState(type);
    this.ensureSubject(type).next({ ...current, ...patch });
  }

  // Reset state for a specific BattleType
  resetCombatState(type: BattleType) {
    this.lastEventsLength[type] = 0;
    this.ensureSubject(type).next({ ...this.defaultState });
  }

  // Reset all types on logout
  handleLogout() {
    this.stateMap.forEach((subject) => subject.next({ ...this.defaultState }));
  }
}
