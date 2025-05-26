import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import {
  BehaviorSubject,
  catchError,
  Observable,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private readonly refresh$ = new Subject<void>();

  /** cached, shared stream of professions */
  private readonly characterOverviewObservable$ = this.refresh$.pipe(
    // make the first request immediately
    startWith(void 0),
    // hit the API whenever refresh$ emits
    switchMap(() => this.getCharacterOverview().pipe()),
    // keep the latest value for all current & future subscribers
    shareReplay(1),
  );

  /** Public readonly stream.  Subscribe or use it with the async-pipe. */
  get characterOverview$(): Observable<CharacterOverviewDto> {
    return this.characterOverviewObservable$;
  }

  private characterOverviewSubject =
    new BehaviorSubject<CharacterOverviewDto | null>(null);

  // public characterOverview$ = this.characterOverviewSubject.asObservable();

  constructor(
    private api: ApiService,
    private authService: AuthService,
  ) {}

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.authService.updateCharacter(updatedCharacter);
  }

  getCurrentCharacter(): Observable<CharacterDto | null> {
    return this.authService.currentCharacter$;
  }

  getLeaderboard() {
    return this.api.get('Character/Leaderboard');
  }

  public getCharacterOverview(): Observable<CharacterOverviewDto> {
    return this.api.get('Character/Overview');
  }
}
