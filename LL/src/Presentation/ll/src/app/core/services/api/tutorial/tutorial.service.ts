import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  TutorialCompletion,
  TutorialState,
} from '../../../../shared/models/tutorial';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({ providedIn: 'root' })
export class TutorialService {
  constructor(private readonly api: ApiService) {}

  getState(): Observable<TutorialState | null> {
    return this.api.get('Tutorial');
  }

  startTrainingBattle(): Observable<CombatResultDto> {
    return this.api.post('Tutorial/start-training-battle', {});
  }

  acknowledgeWelcome(): Observable<TutorialState | null> {
    return this.api.post('Tutorial/welcome', {});
  }

  skip(): Observable<TutorialCompletion> {
    return this.api.post('Tutorial/skip', {});
  }
}
