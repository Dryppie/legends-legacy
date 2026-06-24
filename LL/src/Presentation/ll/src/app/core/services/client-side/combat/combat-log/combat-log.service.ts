import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CombatRecord } from '../../../../../shared/components/combat/combat-log/combat-record';
import {
  BattleOutcome,
  CombatSessionDto,
} from '../../../../../shared/models/Dtos/combatResultDto';

export interface CombatLogStats {
  wins: number;
  losses: number;
  xp: number;
}

@Injectable({ providedIn: 'root' })
export class CombatLogService {
  private readonly maxLogs = 100;
  private readonly _logs = new BehaviorSubject<CombatRecord[]>([]);
  private readonly _stats = new BehaviorSubject<CombatLogStats>({
    wins: 0,
    losses: 0,
    xp: 0,
  });

  readonly logs$ = this._logs.asObservable();
  readonly stats$ = this._stats.asObservable();

  /** Adds a record with timestamp + uuid */
  add(record: CombatRecord) {
    this._logs.next([
      ...this._logs.getValue(),
      {
        ...record,
      },
    ].slice(-this.maxLogs));
  }

  addSession(session: CombatSessionDto) {
    const summary = session.combatSummary;
    if (
      summary.totalBattles <= 0 &&
      summary.wins <= 0 &&
      summary.losses <= 0 &&
      summary.totalExperience <= 0
    ) {
      return;
    }

    const fallback = this.fallbackStatsFromResult(session);
    const current = this._stats.getValue();
    this._stats.next({
      wins: current.wins + (summary.wins || fallback.wins),
      losses: current.losses + (summary.losses || fallback.losses),
      xp: current.xp + summary.totalExperience,
    });
  }

  private fallbackStatsFromResult(
    session: CombatSessionDto,
  ): Pick<CombatLogStats, 'wins' | 'losses'> {
    const summary = session.combatSummary;
    const hasOutcomeCounts =
      (summary.wins ?? 0) + (summary.losses ?? 0) + (summary.draws ?? 0) > 0;

    if (hasOutcomeCounts) {
      return { wins: 0, losses: 0 };
    }

    const outcome = this.normalizeOutcome(session.combatResult?.outcome);
    return {
      wins: outcome === BattleOutcome.Victory ? 1 : 0,
      losses: outcome === BattleOutcome.Defeat ? 1 : 0,
    };
  }

  private normalizeOutcome(outcome: BattleOutcome | number | string | undefined): BattleOutcome | null {
    if (outcome === BattleOutcome.Victory || outcome === 0 || outcome === '0') {
      return BattleOutcome.Victory;
    }

    if (outcome === BattleOutcome.Defeat || outcome === 1 || outcome === '1') {
      return BattleOutcome.Defeat;
    }

    if (outcome === BattleOutcome.Draw || outcome === 2 || outcome === '2') {
      return BattleOutcome.Draw;
    }

    return null;
  }

  clear() {
    this._logs.next([]);
    this._stats.next({
      wins: 0,
      losses: 0,
      xp: 0,
    });
  }
}
