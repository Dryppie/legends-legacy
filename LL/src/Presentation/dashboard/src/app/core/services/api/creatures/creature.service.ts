import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { Creature } from '../../../../shared/models/Dtos/creature';

@Injectable({
  providedIn: 'root',
})
export class CreatureService {
  constructor(private apiService: ApiService) {}

  public getCreatures(): Observable<Creature[]> {
    return this.apiService.get('creature');
  }

  public updateCreature(creature: Creature): Observable<any> {
    return this.apiService.post('creature/updateCreature', creature);
  }
}
