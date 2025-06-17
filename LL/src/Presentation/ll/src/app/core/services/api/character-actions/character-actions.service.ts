import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../api/api.service';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';

@Injectable({ providedIn: 'root' })
export class CharacterActionsService {
  constructor(private readonly api: ApiService) {}

  getCurrentAction(): Observable<CharacterActionDto | null> {
    return this.api.get('CharacterActions');
  }

  startCombat(data: StartCombatActionRequest): Observable<boolean> {
    return this.api.post('CharacterActions/StartCombat', data);
  }

  startGathering(nodeId: string): Observable<boolean> {
    return this.api.post('CharacterActions/StartGathering', nodeId);
  }

  startCrafting(data: StartCraftingActionRequest): Observable<boolean> {
    return this.api.post('CharacterActions/StartCrafting', data);
  }

  stop(): Observable<void> {
    return this.api.delete('CharacterActions');
  }
}
