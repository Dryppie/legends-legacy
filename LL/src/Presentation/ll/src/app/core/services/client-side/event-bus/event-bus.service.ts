import { computed, Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class EventBusService {
  private readonly _logout = signal(false);
  private readonly _currentAction = signal(0);

  // Exposed signals
  readonly logout = computed(() => this._logout());
  readonly currentAction = computed(() => this._currentAction());

  emitLogout() {
    this._logout.set(true);
  }

  emitFetchCurrentAction() {
    this._currentAction.update((val) => val + 1);
  }
}
