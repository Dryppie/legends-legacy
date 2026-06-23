import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CombatRecord } from '../../../../../shared/components/combat/combat-log/combat-record';

@Injectable({ providedIn: 'root' })
export class CombatLogService {
  private readonly maxLogs = 100;
  private readonly _logs = new BehaviorSubject<CombatRecord[]>([]);
  readonly logs$ = this._logs.asObservable();

  /** Adds a record with timestamp + uuid */
  add(record: CombatRecord) {
    this._logs.next([
      ...this._logs.getValue(),
      {
        ...record,
      },
    ].slice(-this.maxLogs));
  }

  clear() {
    this._logs.next([]);
  }
}
