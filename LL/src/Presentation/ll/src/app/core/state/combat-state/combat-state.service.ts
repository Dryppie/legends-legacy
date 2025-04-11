import { Injectable } from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
  SimpleCombatEntityDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { BehaviorSubject } from 'rxjs';
import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import { EventBusService } from '../../services/client-side/event-bus/event-bus.service';

@Injectable({
  providedIn: 'root',
})
export class CombatStateService {
  constructor(private eventBusService: EventBusService) {
    this.eventBusService.logout$.subscribe(() => {
      this.handleLogout();
    });
  }

  private playerCharactersSubject = new BehaviorSubject<
    SimpleCombatEntityDto[]
  >([]);
  private enemyCharactersSubject = new BehaviorSubject<SimpleCombatEntityDto[]>(
    [],
  );
  private combatEventsSubject = new BehaviorSubject<CombatEvent[]>([]);
  private combatResultSubject = new BehaviorSubject<CombatResultDto | null>(
    null,
  );
  private combatOutcomeSubject = new BehaviorSubject<BattleOutcome | null>(
    null,
  );
  private nextCombatSubject = new BehaviorSubject<Date | null>(null);
  private isCombatActiveSubject = new BehaviorSubject<boolean>(false);

  public playerCharacters$ = this.playerCharactersSubject.asObservable();
  public enemyCharacters$ = this.enemyCharactersSubject.asObservable();

  public combatEvents$ = this.combatEventsSubject.asObservable();
  public combatResult$ = this.combatResultSubject.asObservable();
  public combatOutcome$ = this.combatOutcomeSubject.asObservable();

  public nextCombat$ = this.nextCombatSubject.asObservable();
  public isCombatActive$ = this.isCombatActiveSubject.asObservable();

  setPlayerCharacters(characters: SimpleCombatEntityDto[]) {
    this.playerCharactersSubject.next(characters);
  }

  setEnemyCharacters(characters: SimpleCombatEntityDto[]) {
    this.enemyCharactersSubject.next(characters);
  }

  addCombatEvent(event: CombatEvent) {
    const currentEvents = this.combatEventsSubject.value;
    // Push the new event into a clone of the array
    this.combatEventsSubject.next([...currentEvents, event]);
  }

  setCombatActive(isActive: boolean) {
    this.isCombatActiveSubject.next(isActive);
  }

  setCombatResult(result: CombatResultDto | null) {
    this.combatResultSubject.next(result);
  }

  setCombatOutcome(outcome: BattleOutcome | null) {
    this.combatOutcomeSubject.next(outcome);
  }

  setNextCombatIn(nextCombatIn: Date) {
    this.nextCombatSubject.next(nextCombatIn);
  }

  resetCombatState() {
    this.playerCharactersSubject.next([]);
    this.enemyCharactersSubject.next([]);
    this.combatEventsSubject.next([]);
    this.isCombatActiveSubject.next(false);
    this.combatResultSubject.next(null);
    this.combatOutcomeSubject.next(null);
    this.nextCombatSubject.next(null);
  }

  handleLogout() {
    this.resetCombatState();
  }
}
