import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { BehaviorSubject, Observable } from 'rxjs';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private characterOverviewSubject =
    new BehaviorSubject<CharacterOverviewDto | null>(null);

  public characterOverview$ = this.characterOverviewSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
  ) {}

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.authService.updateCharacter(updatedCharacter);
  }

  getCurrentCharacter(): Observable<CharacterDto | null> {
    return this.authService.currentCharacter$;
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
