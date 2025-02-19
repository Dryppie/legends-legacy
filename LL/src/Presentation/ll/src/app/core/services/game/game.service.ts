import { Injectable } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private combatActiveSubject = new BehaviorSubject<boolean>(false);
  combatActive$ = this.combatActiveSubject.asObservable();

  private combatVisibleSubject = new BehaviorSubject<boolean>(false);
  combatVisible$ = this.combatVisibleSubject.asObservable();

  constructor(private router: Router) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.hideCombat();
      }
    });
  }

  startCombat() {
    this.combatActiveSubject.next(true);
    this.showCombat();
  }

  endCombat() {
    this.combatActiveSubject.next(false);
    this.hideCombat();
  }

  showCombat() {
    this.combatVisibleSubject.next(true);
  }

  hideCombat() {
    this.combatVisibleSubject.next(false);
  }
}
