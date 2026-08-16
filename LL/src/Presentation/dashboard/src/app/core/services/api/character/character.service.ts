import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { BehaviorSubject, Observable } from 'rxjs';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private currentCharacterSubject =
    new BehaviorSubject<CharacterDto | null>(null);
  private characterOverviewSubject =
    new BehaviorSubject<CharacterOverviewDto | null>(null);

  public characterOverview$ = this.characterOverviewSubject.asObservable();

  constructor(private apiService: ApiService) {}

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.currentCharacterSubject.next(updatedCharacter);
  }

  getCurrentCharacter(): Observable<CharacterDto | null> {
    return this.currentCharacterSubject.asObservable();
  }

  fetchCharacterData(): void {
    this.apiService.get('character').subscribe((response) => {
      this.currentCharacterSubject.next(response?.data ?? response);
    });
  }

  getLeaderboard() {
    return this.apiService.get('Character/Leaderboard');
  }

  public getCharacterOverview() {
    this.apiService.get('Character/Overview').subscribe((character) => {
      this.characterOverviewSubject.next(character);
    });
  }
}
