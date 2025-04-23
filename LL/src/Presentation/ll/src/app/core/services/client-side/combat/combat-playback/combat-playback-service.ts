import { Inject, Injectable } from '@angular/core';
import { Observable, Subject, takeUntil } from 'rxjs';
import {
  CombatPlaybackStrategy,
  PLAYBACK_STRATEGIES,
} from './combat-playback-strategy';
import { BattleType } from '../../../../state/combat-state/combatState';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { CombatEvent } from '../../../../../shared/models/Dtos/combatEventDto';

@Injectable({ providedIn: 'root' })
export class CombatPlaybackService {
  private destroy$ = new Subject<void>();

  constructor(
    @Inject(PLAYBACK_STRATEGIES)
    private strategies: Record<BattleType, CombatPlaybackStrategy>,
  ) {}

  /** Start a new fight and return an Observable the UI can subscribe to */
  play(result: CombatResultDto): Observable<CombatEvent> {
    // this.stop(); // cancel any running fight
    const strategy = this.strategies[result.battleType];
    if (!strategy)
      throw new Error(`No playback strategy for ${result.battleType}`);

    return strategy
      .stream(result, () => Date.now())
      .pipe(takeUntil(this.destroy$));
  }

  stop(): void {
    this.destroy$.next(); // shuts down _every_ running stream
  }
}
