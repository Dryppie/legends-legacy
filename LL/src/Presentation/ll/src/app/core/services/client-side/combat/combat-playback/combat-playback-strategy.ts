import { Observable } from 'rxjs';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { CombatEvent } from '../../../../../shared/models/Dtos/combatEventDto';
import { InjectionToken } from '@angular/core';
import { BattleType } from '../../../../state/combat-state/combatState';

export interface CombatPlaybackStrategy {
  stream(result: CombatResultDto, now: () => number): Observable<CombatEvent>;
}

export const PLAYBACK_STRATEGIES = new InjectionToken<
  Record<BattleType, CombatPlaybackStrategy>
>('PLAYBACK_STRATEGIES');
