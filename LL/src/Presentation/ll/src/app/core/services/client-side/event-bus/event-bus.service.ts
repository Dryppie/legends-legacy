import {
  Injectable,
  signal,
  computed,
  Signal,
  WritableSignal,
} from '@angular/core';
import { EventPayloads } from './eventPayloads';

export const eventPayloadDefaults: {
  [K in keyof EventPayloads]: EventPayloads[K];
} = {
  'colosseum-combat-finished': null,
  // Add defaults for more events here...
};

@Injectable({
  providedIn: 'root',
})
export class EventBusService {
  private readonly _logout = signal(0);
  private readonly _currentAction = signal(0);

  // Exposed signals
  readonly logout = computed(() => this._logout());
  readonly currentAction = computed(() => this._currentAction());

  private signals = new Map<
    keyof EventPayloads,
    WritableSignal<EventPayloads[keyof EventPayloads]>
  >();

  constructor() {
    for (const key of Object.keys(eventPayloadDefaults) as Array<
      keyof EventPayloads
    >) {
      this.signals.set(key, signal(eventPayloadDefaults[key]));
    }
  }

  emitLogout() {
    this._logout.update((value) => value + 1);
  }

  emitFetchCurrentAction() {
    this._currentAction.update((val) => val + 1);
  }

  emit<K extends keyof EventPayloads>(key: K, payload: EventPayloads[K]): void {
    const sig = this.signals.get(key);
    if (!sig) throw new Error(`No signal registered for event: ${String(key)}`);
    sig.set(payload);
  }

  // Listen to an event (use this in a `effect()`)
  on<K extends keyof EventPayloads>(key: K): Signal<EventPayloads[K]> {
    const sig = this.signals.get(key);
    if (!sig) throw new Error(`No signal registered for event: ${String(key)}`);
    return sig;
  }

  // Optionally reset an event signal manually
  clear<K extends keyof EventPayloads>(key: K): void {
    const sig = this.signals.get(key);
    if (!sig) throw new Error(`No signal registered for event: ${String(key)}`);
    sig.set(eventPayloadDefaults[key]);
  }
}
