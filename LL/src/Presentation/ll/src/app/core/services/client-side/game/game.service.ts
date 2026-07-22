import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CombatLogService } from '../combat/combat-log/combat-log.service';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private combatActiveSubject = new BehaviorSubject<boolean>(false);
  combatActive$ = this.combatActiveSubject.asObservable();

  constructor(private combatLogService: CombatLogService) {}

  resumeCombat() {
    this.combatActiveSubject.next(true);
  }

  endCombat() {
    this.combatActiveSubject.next(false);
    this.combatLogService.clear();
  }
}
