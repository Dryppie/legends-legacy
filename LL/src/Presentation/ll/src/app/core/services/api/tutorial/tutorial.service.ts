import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { TutorialState } from '../../../../shared/models/tutorial';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({ providedIn: 'root' })
export class TutorialService {
  constructor(private readonly api: ApiService) {}

  getState(): Observable<TutorialState | null> {
    return this.api.get('Tutorial');
  }

  recordCraftingPageVisited(route: string): Observable<TutorialState | null> {
    return this.api.post('Tutorial/client-step', {
      stepKey: 'craft_equipment',
      triggerType: 'ClientRouteVisited',
      route,
    });
  }

  startTrainingBattle(): Observable<CombatResultDto> {
    return this.api.post('Tutorial/start-training-battle', {});
  }
}
