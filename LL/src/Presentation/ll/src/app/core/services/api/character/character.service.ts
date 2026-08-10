import { computed, Injectable, Signal } from '@angular/core';
import { ApiService } from '../api.service';
import {
  BehaviorSubject,
  catchError,
  map,
  Observable,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  throwError,
} from 'rxjs';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';
import { HttpParams } from '@angular/common/http';

export interface WireCindersResponse {
  recipientName: string;
  amount: number;
  remainingCinders: number;
}

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private readonly refresh$ = new Subject<void>();

  readonly currentCharacter: Signal<CharacterDto | null>;
  // readonly currentCharacterId: Signal<string | null>;
  readonly currentCharacterId = computed(
    () => this.currentCharacter()?.id ?? null,
  );
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
  ) {
    this.currentCharacter = this.authService.currentCharacter;

    // this.currentCharacterId = computed(
    //   () => this.currentCharacter()?.id ?? null,
    // );
  }

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.authService.updateCharacter(updatedCharacter);
  }

  getCurrentCharacter(): Signal<CharacterDto | null> {
    return this.authService.currentCharacter;
  }

  searchCharacter(name: string): Observable<CharacterOverviewDto> {
    const params = new HttpParams().set('name', name);
    return this.api.get('Character/Search', params).pipe(
      map((characterOverview) => {
        return characterOverview;
      }),
      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(
          () => new Error('No character with this name exists'),
        );
      }),
    );
  }

  resolveCharacterIdByName(name: string): Observable<string> {
    const params = new HttpParams().set('name', name);
    return this.api.get('Character/resolveName', params).pipe(
      map((characterId) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return characterId;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(
          () => new Error('No character with this name exists'),
        );
      }),
    );
  }

  public getCharacterOverview(): Observable<CharacterOverviewDto> {
    return this.api.get('Character/Overview');
  }

  renameCharacter(newName: string) {
    return this.authService.renameCharacter(newName);
  }

  wireCinders(
    recipientName: string,
    amount: number,
  ): Observable<WireCindersResponse> {
    return this.api.post('Character/Wire', {
      recipientName,
      amount,
      currency: 'Cinders',
    });
  }
}
