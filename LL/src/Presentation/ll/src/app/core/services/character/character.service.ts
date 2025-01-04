import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { BehaviorSubject } from 'rxjs';
import { CharacterOverviewDto } from '../../../shared/models/Dtos/characterDto';

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private characterOverviewSubject =
    new BehaviorSubject<CharacterOverviewDto | null>(null);

  public characterOverview$ = this.characterOverviewSubject.asObservable();

  constructor(private apiService: ApiService) {}

  public getCharacterOverview() {
    this.apiService.get('Character/Overview').subscribe((character) => {
      this.characterOverviewSubject.next(character);
    });
  }
}
