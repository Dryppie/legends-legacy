import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { Observable } from 'rxjs';
import { Creature } from '../../../shared/models/creature';

@Injectable({
  providedIn: 'root',
})
export class CreatureService {
  constructor(private apiService: ApiService) {}

  public getCreatures(): Observable<Creature[]> {
    return this.apiService.get('creature');
  }
  
  public updateCreature(creature:Creature): void {
    console.log("Updating Creature: ", creature);
    this.apiService.post('creature', creature);
  }
}
